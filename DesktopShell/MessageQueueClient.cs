using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopShell;

internal static class MessageQueueClient
{
    private static readonly HttpClient httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(8),
    };

    private static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    internal static bool IsEnabled => GlobalVar.QueueEnabled &&
        !string.IsNullOrWhiteSpace(GlobalVar.CfAccessClientId) &&
        !string.IsNullOrWhiteSpace(GlobalVar.CfAccessClientSecret);

    private static string NormalizeTargetName(string name)
    {
        return (name ?? "").Trim().ToLowerInvariant();
    }

    private static string DescribeAuthConfig()
    {
        bool hasId = !string.IsNullOrWhiteSpace(GlobalVar.CfAccessClientId);
        bool hasSecret = !string.IsNullOrWhiteSpace(GlobalVar.CfAccessClientSecret);
        return $"cfAccessClientId={(hasId ? "set" : "missing")}, cfAccessClientSecret={(hasSecret ? "set" : "missing")}";
    }

    internal static bool TryEnqueue(string targetName, string command)
    {
        try
        {
            EnqueueAsync(targetName, command, CancellationToken.None).GetAwaiter().GetResult();
            return true;
        }
        catch (Exception e)
        {
            GlobalVar.Log($"### MessageQueueClient::TryEnqueue - target='{targetName}' - {e}");
            return false;
        }
    }

    internal static async Task ProcessPendingOnceOnStartupAsync(Shell shell, CancellationToken cancellationToken)
    {
        if (!IsEnabled)
        {
            GlobalVar.Log($"### MessageQueueClient: queue disabled for pull. queueEnabled={GlobalVar.QueueEnabled}, {DescribeAuthConfig()}");
            return;
        }

        string me = NormalizeTargetName(System.Net.Dns.GetHostName());
        if (string.IsNullOrWhiteSpace(me))
        {
            GlobalVar.Log("### MessageQueueClient: hostname empty, skipping pull");
            return;
        }

        GlobalVar.Log($"^^^ MessageQueueClient: startup pull begin. me='{me}', baseUrl='{GlobalVar.QueueBaseUrl}'");

        IReadOnlyList<PulledMessage> messages;
        try
        {
            messages = await PullAsync(me, max: 50, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            GlobalVar.Log($"### MessageQueueClient::ProcessPendingOnceOnStartup - pull failed: {e}");
            return;
        }

        if (messages.Count == 0)
        {
            GlobalVar.Log($"^^^ MessageQueueClient: startup pull returned 0 messages. me='{me}'");
            return;
        }

        GlobalVar.Log($"^^^ MessageQueueClient: startup pull returned {messages.Count} messages. me='{me}'");

        var acks = new List<AckItem>(messages.Count);

        foreach (var msg in messages)
        {
            string? command = null;
            try
            {
                command = DecodeCommand(msg);
                if (string.IsNullOrWhiteSpace(command))
                {
                    GlobalVar.Log($"### Pulled message {msg.Id} had empty command");
                    continue;
                }

                await RunOnUiThreadAsync(shell, () => shell.ProcessCommand(command)).ConfigureAwait(false);
                acks.Add(new AckItem(msg.Id, msg.LeaseId));
            }
            catch (Exception e)
            {
                GlobalVar.Log($"### MessageQueueClient: failed to process message {msg.Id}: {e}");
                // Don't ack if we didn't successfully execute.
            }
        }

        if (acks.Count == 0)
        {
            return;
        }

        try
        {
            await AckAsync(me, acks, cancellationToken).ConfigureAwait(false);
            GlobalVar.Log($"^^^ MessageQueueClient: acked {acks.Count} messages. me='{me}'");
        }
        catch (Exception e)
        {
            GlobalVar.Log($"### MessageQueueClient::ProcessPendingOnceOnStartup - ack failed: {e}");
        }
    }

    private static async Task RunOnUiThreadAsync(Control control, Action action)
    {
        if (control.IsDisposed)
        {
            return;
        }

        if (control.InvokeRequired)
        {
            var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            control.BeginInvoke(new Action(() =>
            {
                try
                {
                    action();
                    tcs.TrySetResult(null);
                }
                catch (Exception e)
                {
                    tcs.TrySetException(e);
                }
            }));
            await tcs.Task.ConfigureAwait(false);
            return;
        }

        action();
    }

    private static async Task EnqueueAsync(string targetName, string command, CancellationToken cancellationToken)
    {
        if (!IsEnabled)
        {
            throw new InvalidOperationException("Queue is not enabled");
        }

        string normalizedTarget = NormalizeTargetName(targetName);
        if (string.IsNullOrWhiteSpace(normalizedTarget))
        {
            throw new ArgumentException("targetName is empty", nameof(targetName));
        }

        var id = Guid.NewGuid().ToString("N");
        var envelope = BuildEnvelope(normalizedTarget, command, id);

        var url = $"{GlobalVar.QueueBaseUrl}/v1/send/{Uri.EscapeDataString(normalizedTarget)}";
        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(envelope, jsonOptions), Encoding.UTF8, "application/json"),
        };
        AddAuthHeaders(req);
        req.Headers.Add("Idempotency-Key", id);

        GlobalVar.Log($"^^^ MessageQueueClient: enqueue begin. to='{normalizedTarget}', id='{id}', url='{url}'");
        using var resp = await httpClient.SendAsync(req, cancellationToken).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            var body = "";
            try { body = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false); } catch { }
            throw new HttpRequestException($"Queue send failed: {(int)resp.StatusCode} {resp.ReasonPhrase} {body}");
        }

        GlobalVar.Log($"^^^ MessageQueueClient: enqueue ok. to='{normalizedTarget}', id='{id}', status={(int)resp.StatusCode}");
    }

    private static async Task<IReadOnlyList<PulledMessage>> PullAsync(string targetName, int max, CancellationToken cancellationToken)
    {
        string normalizedTarget = NormalizeTargetName(targetName);
        var url = $"{GlobalVar.QueueBaseUrl}/v1/pull/{Uri.EscapeDataString(normalizedTarget)}?max={max}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        AddAuthHeaders(req);

        GlobalVar.Log($"^^^ MessageQueueClient: pull begin. target='{normalizedTarget}', url='{url}'");
        using var resp = await httpClient.SendAsync(req, cancellationToken).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            var body = "";
            try { body = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false); } catch { }
            throw new HttpRequestException($"Queue pull failed: {(int)resp.StatusCode} {resp.ReasonPhrase} {body}");
        }

        await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var payload = await JsonSerializer.DeserializeAsync<PullResponse>(stream, jsonOptions, cancellationToken).ConfigureAwait(false);
        var messages = payload?.Messages ?? Array.Empty<PulledMessage>();
        GlobalVar.Log($"^^^ MessageQueueClient: pull ok. target='{normalizedTarget}', status={(int)resp.StatusCode}, messages={messages.Count}");
        return messages;
    }

    private static async Task AckAsync(string targetName, IReadOnlyList<AckItem> acks, CancellationToken cancellationToken)
    {
        string normalizedTarget = NormalizeTargetName(targetName);
        var url = $"{GlobalVar.QueueBaseUrl}/v1/ack/{Uri.EscapeDataString(normalizedTarget)}";
        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(new AckRequest(acks), jsonOptions), Encoding.UTF8, "application/json"),
        };
        AddAuthHeaders(req);

        GlobalVar.Log($"^^^ MessageQueueClient: ack begin. target='{normalizedTarget}', count={acks.Count}, url='{url}'");
        using var resp = await httpClient.SendAsync(req, cancellationToken).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            var body = "";
            try { body = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false); } catch { }
            throw new HttpRequestException($"Queue ack failed: {(int)resp.StatusCode} {resp.ReasonPhrase} {body}");
        }

        GlobalVar.Log($"^^^ MessageQueueClient: ack ok. target='{normalizedTarget}', status={(int)resp.StatusCode}, count={acks.Count}");
    }

    private static object BuildEnvelope(string targetName, string command, string id)
    {
        string from = NormalizeTargetName(System.Net.Dns.GetHostName());
        if (TryGetQueueKey(out var key))
        {
            var enc = Encrypt(command, key);
            return new
            {
                id,
                to = targetName,
                from,
                createdUtc = DateTimeOffset.UtcNow,
                enc,
            };
        }

        return new
        {
            id,
            to = targetName,
            from,
            createdUtc = DateTimeOffset.UtcNow,
            command,
        };
    }

    private static void AddAuthHeaders(HttpRequestMessage request)
    {
        request.Headers.TryAddWithoutValidation(GlobalVar.HeaderCfAccessClientId, GlobalVar.CfAccessClientId);
        request.Headers.TryAddWithoutValidation(GlobalVar.HeaderCfAccessClientSecret, GlobalVar.CfAccessClientSecret);
    }

    private static string? DecodeCommand(PulledMessage msg)
    {
        if (!string.IsNullOrWhiteSpace(msg.Command))
        {
            return msg.Command;
        }

        if (msg.Enc == null)
        {
            return null;
        }

        if (!TryGetQueueKey(out var key))
        {
            throw new InvalidOperationException("Pulled encrypted message but DESKTOPSHELL_QUEUE_KEY_B64 is not configured");
        }

        return Decrypt(msg.Enc, key);
    }

    private static bool TryGetQueueKey(out byte[] key)
    {
        key = Array.Empty<byte>();
        var b64 = GlobalVar.QueueKeyBase64;
        if (string.IsNullOrWhiteSpace(b64))
        {
            return false;
        }

        try
        {
            key = Convert.FromBase64String(b64);
            return key.Length is 16 or 24 or 32;
        }
        catch
        {
            return false;
        }
    }

    private static object Encrypt(string plaintext, byte[] key)
    {
        byte[] nonce = RandomNumberGenerator.GetBytes(12);
        byte[] plainBytes = Encoding.UTF8.GetBytes(plaintext);
        byte[] cipherBytes = new byte[plainBytes.Length];
        byte[] tag = new byte[16];

        using var aes = new AesGcm(key, tagSizeInBytes: 16);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        return new
        {
            alg = "A256GCM",
            nonce = Convert.ToBase64String(nonce),
            ciphertext = Convert.ToBase64String(cipherBytes),
            tag = Convert.ToBase64String(tag),
        };
    }

    private static string Decrypt(EncryptedPayload enc, byte[] key)
    {
        if (enc.Alg is not "A256GCM")
        {
            throw new InvalidOperationException($"Unsupported alg: {enc.Alg}");
        }

        byte[] nonce = Convert.FromBase64String(enc.Nonce);
        byte[] cipherBytes = Convert.FromBase64String(enc.Ciphertext);
        byte[] tag = Convert.FromBase64String(enc.Tag);
        byte[] plainBytes = new byte[cipherBytes.Length];

        using var aes = new AesGcm(key, tagSizeInBytes: 16);
        aes.Decrypt(nonce, cipherBytes, tag, plainBytes);
        return Encoding.UTF8.GetString(plainBytes);
    }

    internal sealed record PullResponse(IReadOnlyList<PulledMessage> Messages);

    internal sealed record PulledMessage(
        string Id,
        string LeaseId,
        DateTimeOffset? CreatedUtc,
        string? Command,
        EncryptedPayload? Enc);

    internal sealed record EncryptedPayload(
        string Alg,
        string Nonce,
        string Ciphertext,
        string Tag);

    internal sealed record AckRequest(IReadOnlyList<AckItem> Acks);

    internal sealed record AckItem(string Id, string LeaseId);
}
