using System.Media;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopShell;

public class TCPServer
{
    private readonly TcpListener? tcplistener;
    private readonly int portNum;
    private readonly CancellationTokenSource cts = new();
    private readonly CancellationToken token;

    public TCPServer()
    {
        string rawHostName = Dns.GetHostName();
        string hostName = GlobalVar.NormalizeHostName(rawHostName);
        token = cts.Token;

        bool foundHostPort = false;

        foreach (KeyValuePair<string, string> hostPair in GlobalVar.HostList)
        {
            string tempHostName = GlobalVar.NormalizeHostName(hostPair.Key);
            if (tempHostName.Equals(hostName))
            {
                GlobalVar.Log($"^^^ Starting server on port: {hostPair.Value}");
                if (!int.TryParse(hostPair.Value, out portNum))
                {
                    GlobalVar.Log($"### Error trying to parse port number as int: {hostPair.Value}");
                    return;
                }

                foundHostPort = true;
                break;
            }
        }

        if (!foundHostPort || portNum <= 0)
        {
            GlobalVar.Log($"### TCPServer not started: host '{hostName}' (raw='{rawHostName}') missing/invalid port in hostlist.txt");
            return;
        }

        try
        {
            tcplistener = new TcpListener(IPAddress.Any, portNum);
            var listenThread = new Thread(ListenForClients);
            listenThread.IsBackground = true;
            listenThread.Start();
        }
        catch (Exception e)
        {
            GlobalVar.ToolTip("Server Error", $"TCPServer::TCPServer() {e.Message}");
        }
    }

    private void ListenForClients()
    {
        if (tcplistener == null)
        {
            return;
        }

        try
        {
            tcplistener.Start();

            while (!token.IsCancellationRequested)
            {
                TcpClient? acceptedClient = null;
                try
                {
                    acceptedClient = tcplistener.AcceptTcpClient();
                    var clientThread = new Thread(HandleClientComm);
                    clientThread.IsBackground = true;
                    clientThread.Start(acceptedClient);
                }
                catch (SocketException) when (token.IsCancellationRequested)
                {
                    // Listener was stopped during shutdown.
                    return;
                }
                catch (ObjectDisposedException) when (token.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception e)
                {
                    GlobalVar.Log($"### TCPServer::ListenForClients() - {e.Message}");
                    try
                    {
                        acceptedClient?.Close();
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }
        }
        catch (Exception e)
        {
            GlobalVar.Log($"### TCPServer::ListenForClients() fatal - {e.Message}");
        }
    }

    private static string TrimPassPhrase(string inString)
    {
        if (string.IsNullOrWhiteSpace(inString))
        {
            return "";
        }

        // Require the passphrase at the start of the message (prevents embedding it later in the payload).
        if (inString.StartsWith(GlobalVar.PassPhrase, StringComparison.Ordinal))
        {
            return inString[GlobalVar.PassPhrase.Length..];
        }

        return $"spike{inString}";
    }

    private static string ReadStream(TcpClient tcpClient, Stream clientStream, CancellationToken token)
    {
        var buffer = new byte[1024];
        var collected = new List<byte>(256);
        var idle = Stopwatch.StartNew();

        while (!token.IsCancellationRequested && tcpClient.Connected)
        {
            try
            {
                int bytesRead = clientStream.Read(buffer, 0, buffer.Length);
                if (bytesRead <= 0)
                {
                    return "";
                }

                idle.Restart();

                for (int i = 0; i < bytesRead; i++)
                {
                    byte b = buffer[i];
                    if (b == (byte)'\n')
                    {
                        string raw = Encoding.ASCII.GetString(collected.ToArray());
                        string line = raw.TrimEnd('\r');
                        string receivedString = TrimPassPhrase(line.Trim());
                        GlobalVar.Log($"$$$ TCPServer::ReadStream() - {receivedString}");
                        return receivedString;
                    }

                    collected.Add(b);

                    if (collected.Count >= GlobalVar.TcpBufferSize)
                    {
                        string raw = Encoding.ASCII.GetString(collected.ToArray());
                        string receivedString = TrimPassPhrase(raw.Trim());
                        GlobalVar.Log($"$$$ TCPServer::ReadStream() (buffer full) - {receivedString}");
                        return receivedString;
                    }
                }
            }
            catch (IOException)
            {
                // Timeout: if we've already received some bytes and then go idle, treat it as a complete message.
                if (collected.Count > 0 && idle.ElapsedMilliseconds >= GlobalVar.TcpMessageIdleTimeoutMs)
                {
                    string raw = Encoding.ASCII.GetString(collected.ToArray());
                    string receivedString = TrimPassPhrase(raw.Trim());
                    GlobalVar.Log($"$$$ TCPServer::ReadStream() (idle) - {receivedString}");
                    return receivedString;
                }

                Thread.Sleep(GlobalVar.TcpReadDelayMs);
            }
            catch (Exception e)
            {
                GlobalVar.Log($"### TCPServer::ReadStream() - {e.Message}");
                return "";
            }
        }

        return "";
    }

    private void HandleClientComm(object client)
    {
        SoundPlayer myPlayer = new();
        string soundLocation = $"{GlobalVar.GetSoundFolderLocation()}\\remote.wav";
        var tcpClient = (TcpClient)client;

        Stream? clientStream = null;
        try
        {
            clientStream = GlobalVar.CreateInboundCommandStream(tcpClient);
        }
        catch (Exception e)
        {
            GlobalVar.Log($"### TCPServer::HandleClientComm - Failed to create inbound stream: {e.GetType()}: {e.Message}");
            try { tcpClient.Close(); } catch { }
            return;
        }

        using (clientStream)
        {
            bool isCommunicationOver = false;

            do
            {
                string receivedString = ReadStream(tcpClient, clientStream, token);
                if (receivedString.Equals(""))
                {
                    Thread.Sleep(GlobalVar.WebBrowserLaunchDelayMs);
                    continue;
                }

                switch (receivedString)
                {
                    case "ack":
                        try
                        {
                            if (GlobalVar.IsWindows)
                            {
                                using (myPlayer = new SoundPlayer(soundLocation))
                                {
                                    myPlayer.PlaySync();
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            if (e.GetType() == typeof(FileNotFoundException))
                            {
                                GlobalVar.Log($"### Error playing sound at location: {myPlayer.SoundLocation}");
                            }
                            else
                            {
                                GlobalVar.Log($"### Error playing sound: {e.Message}");
                            }
                        }
                        GlobalVar.Log("$$$ Ack received, closing connection");
                        isCommunicationOver = true;
                        break;
                    case "ringdoorbell":
                        ///TODO: Add ring doorbell functionality here to pull up video stream or snapshot
                        GlobalVar.ToolTip("Ring", "");
                        GlobalVar.WriteRemoteCommand(clientStream, "ack", includePassPhrase: true);
                        break;
                    case string a when a.Contains("spike"):
                        int idx = receivedString.IndexOf("spike", StringComparison.OrdinalIgnoreCase) + "spike".Length;
                        GlobalVar.Log($"$$$ Got fake command: {receivedString[idx..]}");
                        GlobalVar.WriteRemoteCommand(clientStream, "lol", includePassPhrase: false);
                        break;
                    default:
                        //GlobalVar.SendRemoteCommand(tcpClient, "ack\r\n");
                        GlobalVar.ShellInstance?.ProcessCommand(receivedString);
                        GlobalVar.WriteRemoteCommand(clientStream, "ack", includePassPhrase: true);
                        isCommunicationOver = true;
                        break;
                }
            } while (!isCommunicationOver && tcpClient.Connected);
        }

        if (tcpClient != null)
        {
            GlobalVar.Log("@@@ TCPServer::HandleClientComm - Closing tcpClient");
            tcpClient.Close();
        }
    }

    public void CloseServer()
    {
        cts.Cancel();
        tcplistener?.Stop();
    }

    ~TCPServer()
    {
        cts.Cancel();
        tcplistener?.Stop();
    }
}
