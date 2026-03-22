using DesktopShell.AI;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopShell.Tests.AI;

[TestClass]
public partial class AICommandHandlerTests
{
    #region Claude Tests

    [TestMethod]
    public async Task HandleClaude_Success_SavesAndOpens()
    {
        var cli = new FakeCliRunner("Hello from Claude!", exitCode: 0);
        var presenter = new FakeResponsePresenter();
        var handler = new AICommandHandler(cli, presenter);

        await handler.HandleClaudeAsync("test prompt");

        cli.LastExecutable.Should().Be("claude");
        cli.LastArguments.Should().ContainInOrder("-p", "test prompt");
        presenter.SavedFiles.Should().HaveCount(1);
        presenter.SavedFiles[0].Title.Should().Be("claude");
        presenter.SavedFiles[0].Content.Should().Be("Hello from Claude!");
        presenter.OpenedFiles.Should().HaveCount(1);
        presenter.Notifications.Should().Contain(n => n.Message == "Response ready");
    }

    [TestMethod]
    public async Task HandleClaude_EmptyPrompt_ShowsError()
    {
        var cli = new FakeCliRunner("output", exitCode: 0);
        var presenter = new FakeResponsePresenter();
        var handler = new AICommandHandler(cli, presenter);

        await handler.HandleClaudeAsync("   ");

        cli.Invocations.Should().Be(0);
        presenter.Notifications.Should().Contain(n => n.Message == "No prompt provided");
    }

    [TestMethod]
    public async Task HandleClaude_EmptyResponse_ShowsError()
    {
        var cli = new FakeCliRunner("", exitCode: 0);
        var presenter = new FakeResponsePresenter();
        var handler = new AICommandHandler(cli, presenter);

        await handler.HandleClaudeAsync("test");

        presenter.Notifications.Should().Contain(n => n.Message == "Empty response received");
        presenter.SavedFiles.Should().BeEmpty();
    }

    [TestMethod]
    public async Task HandleClaude_NonZeroExit_ShowsError()
    {
        var cli = new FakeCliRunner("error output", exitCode: 1);
        var presenter = new FakeResponsePresenter();
        var handler = new AICommandHandler(cli, presenter);

        await handler.HandleClaudeAsync("test");

        presenter.Notifications.Should().Contain(n => n.Message == "Failed (see log)");
        presenter.SavedFiles.Should().BeEmpty();
    }

    [TestMethod]
    public async Task HandleClaude_Timeout_ShowsError()
    {
        var cli = new FakeCliRunner(throwTimeout: true);
        var presenter = new FakeResponsePresenter();
        var handler = new AICommandHandler(cli, presenter);

        await handler.HandleClaudeAsync("test");

        presenter.Notifications.Should().Contain(n => n.Message == "Timed out");
    }

    #endregion

    #region Codex Tests

    [TestMethod]
    public async Task HandleCodex_Success_SavesAndOpens()
    {
        var cli = new FakeCliRunner("Hello from Codex!", exitCode: 0);
        var presenter = new FakeResponsePresenter();
        var handler = new AICommandHandler(cli, presenter);

        await handler.HandleCodexAsync("test prompt");

        cli.LastExecutable.Should().Be("codex");
        cli.LastArguments.Should().ContainInOrder("exec", "--ephemeral", "test prompt");
        presenter.SavedFiles.Should().HaveCount(1);
        presenter.SavedFiles[0].Title.Should().Be("codex");
        presenter.OpenedFiles.Should().HaveCount(1);
        presenter.Notifications.Should().Contain(n => n.Message == "Response ready");
    }

    [TestMethod]
    public async Task HandleCodex_EmptyPrompt_ShowsError()
    {
        var cli = new FakeCliRunner("output", exitCode: 0);
        var presenter = new FakeResponsePresenter();
        var handler = new AICommandHandler(cli, presenter);

        await handler.HandleCodexAsync("");

        cli.Invocations.Should().Be(0);
        presenter.Notifications.Should().Contain(n => n.Message == "No prompt provided");
    }

    #endregion

    #region Claudex Pipeline Tests

    [TestMethod]
    public async Task HandleClaudex_AllStepsSucceed_ShowsFinalResponse()
    {
        var cli = new FakeCliRunner(["Step 1 response", "Step 2 response", "Final synthesized response"]);
        var presenter = new FakeResponsePresenter();
        var handler = new AICommandHandler(cli, presenter);

        await handler.HandleClaudexAsync("test prompt");

        cli.Invocations.Should().Be(3);
        presenter.SavedFiles.Should().HaveCount(1);
        presenter.SavedFiles[0].Title.Should().Be("claudex");
        presenter.SavedFiles[0].Content.Should().Be("Final synthesized response");
        presenter.OpenedFiles.Should().HaveCount(1);
        presenter.Notifications.Should().Contain(n => n.Message == "Response ready");
    }

    [TestMethod]
    public async Task HandleClaudex_Step1Fails_StopsWithError()
    {
        var cli = new FakeCliRunner(exitCodes: [1], outputs: ["error"]);
        var presenter = new FakeResponsePresenter();
        var handler = new AICommandHandler(cli, presenter);

        await handler.HandleClaudexAsync("test prompt");

        cli.Invocations.Should().Be(1);
        presenter.Notifications.Should().Contain(n => n.Message == "Failed at step 1");
        presenter.SavedFiles.Should().BeEmpty();
    }

    [TestMethod]
    public async Task HandleClaudex_Step2Fails_StopsWithError()
    {
        var cli = new FakeCliRunner(exitCodes: [0, 1], outputs: ["Step 1 OK", "error"]);
        var presenter = new FakeResponsePresenter();
        var handler = new AICommandHandler(cli, presenter);

        await handler.HandleClaudexAsync("test prompt");

        cli.Invocations.Should().Be(2);
        presenter.Notifications.Should().Contain(n => n.Message == "Failed at step 2");
        presenter.SavedFiles.Should().BeEmpty();
    }

    [TestMethod]
    public async Task HandleClaudex_Step3ReceivesBothResponses()
    {
        var cli = new FakeCliRunner(["Claude says this", "Codex says that", "Final answer"]);
        var presenter = new FakeResponsePresenter();
        var handler = new AICommandHandler(cli, presenter);

        await handler.HandleClaudexAsync("my prompt");

        // Step 3's stdin should contain both prior responses and the original prompt
        cli.StdinInputs.Should().HaveCountGreaterOrEqualTo(2);
        string step3Stdin = cli.StdinInputs[^1]!;
        step3Stdin.Should().Contain("my prompt");
        step3Stdin.Should().Contain("Claude says this");
        step3Stdin.Should().Contain("Codex says that");
    }

    [TestMethod]
    public async Task HandleClaudex_EmptyPrompt_ShowsError()
    {
        var cli = new FakeCliRunner("output", exitCode: 0);
        var presenter = new FakeResponsePresenter();
        var handler = new AICommandHandler(cli, presenter);

        await handler.HandleClaudexAsync("  ");

        cli.Invocations.Should().Be(0);
        presenter.Notifications.Should().Contain(n => n.Message == "No prompt provided");
    }

    #endregion

    #region Regex Routing Tests

    [TestMethod]
    public void ClaudexRegex_MatchesClaudexPrefix_NotCodex()
    {
        var regex = ClaudexCommandRegex();
        regex.IsMatch("claudex foo").Should().BeTrue();
        regex.IsMatch("codex foo").Should().BeFalse();
        regex.IsMatch("claude foo").Should().BeFalse();
    }

    [TestMethod]
    public void CodexRegex_MatchesCodexPrefix_NotClaudex()
    {
        var regex = CodexCommandRegex();
        regex.IsMatch("codex foo").Should().BeTrue();
        regex.IsMatch("claudex foo").Should().BeFalse();
    }

    [TestMethod]
    public void ClaudeRegex_MatchesClaudePrefix_NotClaudex()
    {
        var regex = ClaudeCommandRegex();
        regex.IsMatch("claude foo").Should().BeTrue();
        regex.IsMatch("claudex foo").Should().BeFalse();
    }

    [TestMethod]
    public void RemoteCommand_DoesNotMatchAICommands()
    {
        // AI commands use spaces (no colon), so they don't conflict with RemoteCommand
        var remoteRegex = RemoteCommandRegex();
        remoteRegex.IsMatch("claude foo").Should().BeFalse();
        remoteRegex.IsMatch("codex foo").Should().BeFalse();
        remoteRegex.IsMatch("claudex foo").Should().BeFalse();
        remoteRegex.IsMatch("desktop: rescan").Should().BeTrue();
    }

    // Expose the regex patterns for testing (mirrors ShellForm's patterns)
    [System.Text.RegularExpressions.GeneratedRegex("^claudex ")]
    private static partial System.Text.RegularExpressions.Regex ClaudexCommandRegex();

    [System.Text.RegularExpressions.GeneratedRegex("^codex ")]
    private static partial System.Text.RegularExpressions.Regex CodexCommandRegex();

    [System.Text.RegularExpressions.GeneratedRegex("^claude ")]
    private static partial System.Text.RegularExpressions.Regex ClaudeCommandRegex();

    [System.Text.RegularExpressions.GeneratedRegex("(^[a-zA-Z]+:){1}")]
    private static partial System.Text.RegularExpressions.Regex RemoteCommandRegex();

    #endregion
}

#region Test Doubles

public class FakeCliRunner : ICliRunner
{
    private readonly List<string?> _outputs;
    private readonly List<int> _exitCodes;
    private readonly bool _throwTimeout;
    private int _callIndex;

    public int Invocations => _callIndex;
    public string? LastExecutable { get; private set; }
    public string[]? LastArguments { get; private set; }
    public List<string?> StdinInputs { get; } = [];

    public FakeCliRunner(string? output = null, int exitCode = 0, bool throwTimeout = false)
    {
        _outputs = [output];
        _exitCodes = [exitCode];
        _throwTimeout = throwTimeout;
    }

    public FakeCliRunner(string[] sequentialOutputs)
    {
        _outputs = [.. sequentialOutputs];
        _exitCodes = Enumerable.Repeat(0, sequentialOutputs.Length).ToList();
    }

    public FakeCliRunner(int[] exitCodes, string[] outputs)
    {
        _exitCodes = [.. exitCodes];
        _outputs = [.. outputs];
    }

    public Task<(string? Output, int ExitCode)> RunAsync(string executable, string[] arguments, int timeoutMs, string? stdinContent = null)
    {
        if (_throwTimeout)
            throw new TimeoutException("Timed out");

        LastExecutable = executable;
        LastArguments = arguments;
        StdinInputs.Add(stdinContent);

        int idx = _callIndex;
        _callIndex++;

        string? output = idx < _outputs.Count ? _outputs[idx] : null;
        int exitCode = idx < _exitCodes.Count ? _exitCodes[idx] : 0;

        return Task.FromResult((output, exitCode));
    }
}

public class FakeResponsePresenter : IResponsePresenter
{
    public record NotificationRecord(string Title, string Message);
    public record SavedFile(string Title, string Content, string Path);

    public List<NotificationRecord> Notifications { get; } = [];
    public List<SavedFile> SavedFiles { get; } = [];
    public List<string> OpenedFiles { get; } = [];

    public void Notify(string title, string message)
    {
        Notifications.Add(new NotificationRecord(title, message));
    }

    public Task<string> SaveResponseAsync(string title, string content)
    {
        string fakePath = $"/tmp/fake/{title}.md";
        SavedFiles.Add(new SavedFile(title, content, fakePath));
        return Task.FromResult(fakePath);
    }

    public void OpenInEditor(string filePath)
    {
        OpenedFiles.Add(filePath);
    }
}

#endregion
