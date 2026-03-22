using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopShell.AI;

public interface ICliRunner
{
    Task<(string? Output, int ExitCode)> RunAsync(string executable, string[] arguments, int timeoutMs, string? stdinContent = null);
}

public interface IResponsePresenter
{
    void Notify(string title, string message);
    Task<string> SaveResponseAsync(string title, string content);
    void OpenInEditor(string filePath);
}

public class AICommandHandler
{
    private const int TimeoutMs = 600_000; // 10 min per step

    private readonly ICliRunner _cli;
    private readonly IResponsePresenter _presenter;

    private static readonly string ClaudeCli =
        Environment.GetEnvironmentVariable("DESKTOPSHELL_CLAUDE_CLI") ?? "claude";
    private static readonly string CodexCli =
        Environment.GetEnvironmentVariable("DESKTOPSHELL_CODEX_CLI") ?? "codex";

    public AICommandHandler(ICliRunner cli, IResponsePresenter presenter)
    {
        _cli = cli;
        _presenter = presenter;
    }

    public async Task HandleClaudeAsync(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            _presenter.Notify("Claude", "No prompt provided");
            return;
        }

        _presenter.Notify("Claude", "Thinking...");

        try
        {
            var (output, exitCode) = await _cli.RunAsync(ClaudeCli, ["-p", prompt], TimeoutMs);

            if (exitCode != 0)
            {
                GlobalVar.Log($"### AICommandHandler::HandleClaudeAsync() - claude exited with code {exitCode}: {output}");
                _presenter.Notify("Claude", "Failed (see log)");
                return;
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                _presenter.Notify("Claude", "Empty response received");
                return;
            }

            string path = await _presenter.SaveResponseAsync("claude", output);
            _presenter.OpenInEditor(path);
            _presenter.Notify("Claude", "Response ready");
        }
        catch (TimeoutException)
        {
            _presenter.Notify("Claude", "Timed out");
        }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or FileNotFoundException)
        {
            GlobalVar.Log($"### AICommandHandler::HandleClaudeAsync() - {e.GetType()}: {e.Message}");
            _presenter.Notify("Claude", "claude not found on PATH");
        }
        catch (Exception e)
        {
            GlobalVar.Log($"### AICommandHandler::HandleClaudeAsync() - {e.GetType()}: {e.Message}");
            _presenter.Notify("Claude", "Failed (see log)");
        }
    }

    public async Task HandleCodexAsync(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            _presenter.Notify("Codex", "No prompt provided");
            return;
        }

        _presenter.Notify("Codex", "Thinking...");

        try
        {
            var (output, exitCode) = await _cli.RunAsync(CodexCli, ["exec", "--ephemeral", "--skip-git-repo-check", prompt], TimeoutMs);

            if (exitCode != 0)
            {
                GlobalVar.Log($"### AICommandHandler::HandleCodexAsync() - codex exited with code {exitCode}: {output}");
                _presenter.Notify("Codex", "Failed (see log)");
                return;
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                _presenter.Notify("Codex", "Empty response received");
                return;
            }

            string path = await _presenter.SaveResponseAsync("codex", output);
            _presenter.OpenInEditor(path);
            _presenter.Notify("Codex", "Response ready");
        }
        catch (TimeoutException)
        {
            _presenter.Notify("Codex", "Timed out");
        }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or FileNotFoundException)
        {
            GlobalVar.Log($"### AICommandHandler::HandleCodexAsync() - {e.GetType()}: {e.Message}");
            _presenter.Notify("Codex", "codex not found on PATH");
        }
        catch (Exception e)
        {
            GlobalVar.Log($"### AICommandHandler::HandleCodexAsync() - {e.GetType()}: {e.Message}");
            _presenter.Notify("Codex", "Failed (see log)");
        }
    }

    public async Task HandleClaudexAsync(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            _presenter.Notify("Claudex", "No prompt provided");
            return;
        }

        try
        {
            // Step 1: Claude
            _presenter.Notify("Claudex", "Step 1/3: Claude thinking...");
            var (responseA, exitCodeA) = await _cli.RunAsync(ClaudeCli, ["-p", prompt], TimeoutMs);

            if (exitCodeA != 0 || string.IsNullOrWhiteSpace(responseA))
            {
                GlobalVar.Log($"### AICommandHandler::HandleClaudexAsync() - Step 1 failed: exitCode={exitCodeA}, output={responseA}");
                _presenter.Notify("Claudex", "Failed at step 1");
                return;
            }

            // Step 2: Codex reviews Claude's response (piped via stdin to avoid arg length limits)
            _presenter.Notify("Claudex", "Step 2/3: Codex reviewing...");
            string step2Stdin = $"Review and enhance the following response:\n\n{responseA}";
            var (responseB, exitCodeB) = await _cli.RunAsync(CodexCli, ["exec", "--ephemeral", "--skip-git-repo-check", "-"], TimeoutMs, stdinContent: step2Stdin);

            if (exitCodeB != 0 || string.IsNullOrWhiteSpace(responseB))
            {
                GlobalVar.Log($"### AICommandHandler::HandleClaudexAsync() - Step 2 failed: exitCode={exitCodeB}, output={responseB}");
                _presenter.Notify("Claudex", "Failed at step 2");
                return;
            }

            // Step 3: Claude synthesizes final response
            _presenter.Notify("Claudex", "Step 3/3: Claude synthesizing...");
            string step3Prompt = $"Given the original prompt, Claude's response, and Codex's review, synthesize a final trimmed response:\n\nOriginal prompt: {prompt}\nClaude's response: {responseA}\nCodex's review: {responseB}";
            var (finalOutput, exitCodeC) = await _cli.RunAsync(ClaudeCli, ["-p", step3Prompt], TimeoutMs);

            if (exitCodeC != 0 || string.IsNullOrWhiteSpace(finalOutput))
            {
                GlobalVar.Log($"### AICommandHandler::HandleClaudexAsync() - Step 3 failed: exitCode={exitCodeC}, output={finalOutput}");
                _presenter.Notify("Claudex", "Failed at step 3");
                return;
            }

            string path = await _presenter.SaveResponseAsync("claudex", finalOutput);
            _presenter.OpenInEditor(path);
            _presenter.Notify("Claudex", "Response ready");
        }
        catch (TimeoutException)
        {
            _presenter.Notify("Claudex", "Timed out");
        }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or FileNotFoundException)
        {
            GlobalVar.Log($"### AICommandHandler::HandleClaudexAsync() - {e.GetType()}: {e.Message}");
            _presenter.Notify("Claudex", "claude/codex not found on PATH");
        }
        catch (Exception e)
        {
            GlobalVar.Log($"### AICommandHandler::HandleClaudexAsync() - {e.GetType()}: {e.Message}");
            _presenter.Notify("Claudex", "Failed (see log)");
        }
    }
}

public class CliRunner : ICliRunner
{
    /// <summary>
    /// Resolves a bare command name (e.g. "codex") to a full path by searching PATH
    /// for common Windows executable extensions. Needed because Process.Start with
    /// UseShellExecute=false won't resolve .cmd/.ps1 shims (e.g. npm-installed CLIs).
    /// </summary>
    private static string ResolveExecutable(string executable)
    {
        // Already a rooted path or has an extension — use as-is
        if (Path.IsPathRooted(executable) || Path.HasExtension(executable))
            return executable;

        string[] extensions = [".exe", ".cmd", ".bat"];
        string? pathVar = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathVar))
            return executable;

        foreach (string dir in pathVar.Split(Path.PathSeparator))
        {
            foreach (string ext in extensions)
            {
                string candidate = Path.Combine(dir.Trim(), executable + ext);
                if (File.Exists(candidate))
                    return candidate;
            }
        }

        return executable; // fall through — let Process.Start produce the error
    }

    public async Task<(string? Output, int ExitCode)> RunAsync(string executable, string[] arguments, int timeoutMs, string? stdinContent = null)
    {
        using var cts = new CancellationTokenSource(timeoutMs);
        string resolvedExe = ResolveExecutable(executable);

        var psi = new ProcessStartInfo
        {
            FileName = resolvedExe,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = stdinContent != null
        };

        foreach (string arg in arguments)
            psi.ArgumentList.Add(arg);

        using var process = new Process { StartInfo = psi };
        process.Start();

        // Write stdin if provided, then close to signal EOF
        if (stdinContent != null)
        {
            await process.StandardInput.WriteAsync(stdinContent);
            process.StandardInput.Close();
        }

        // Read stdout and stderr in parallel to avoid deadlocks
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cts.Token);
        var stderrTask = process.StandardError.ReadToEndAsync(cts.Token);

        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            throw new TimeoutException($"{executable} timed out after {timeoutMs}ms");
        }

        string stdout = await stdoutTask;
        string stderr = await stderrTask;

        if (process.ExitCode != 0 && !string.IsNullOrWhiteSpace(stderr))
        {
            GlobalVar.Log($"### CliRunner stderr ({executable}): {stderr}");
        }

        return (string.IsNullOrWhiteSpace(stdout) ? stderr : stdout, process.ExitCode);
    }
}

public class ResponsePresenter : IResponsePresenter
{
    private static readonly string TempDir = Path.Combine(Path.GetTempPath(), "DesktopShell");

    public void Notify(string title, string message)
    {
        GlobalVar.ToolTip(title, message);
    }

    public async Task<string> SaveResponseAsync(string title, string content)
    {
        Directory.CreateDirectory(TempDir);
        string fileName = $"{title}_{DateTime.Now:yyyyMMdd_HHmmss}.md";
        string filePath = Path.Combine(TempDir, fileName);
        await File.WriteAllTextAsync(filePath, content);
        return filePath;
    }

    public void OpenInEditor(string filePath)
    {
        try
        {
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }
        catch (Exception e)
        {
            GlobalVar.Log($"### ResponsePresenter::OpenInEditor() - {e.GetType()}: {e.Message}");
            GlobalVar.ToolTip("AI", $"Response saved: {filePath}");
        }
    }
}
