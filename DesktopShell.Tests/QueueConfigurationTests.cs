using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace DesktopShell.Tests;

[TestClass]
public class QueueConfigurationTests
{
    private string? oldEnabled;
    private string? oldBaseUrl;
    private string? oldClientId;
    private string? oldClientSecret;

    [TestInitialize]
    public void SaveEnv()
    {
        oldEnabled = Environment.GetEnvironmentVariable(GlobalVar.EnvQueueEnabled);
        oldBaseUrl = Environment.GetEnvironmentVariable(GlobalVar.EnvQueueBaseUrl);
        oldClientId = Environment.GetEnvironmentVariable(GlobalVar.EnvCfAccessClientId);
        oldClientSecret = Environment.GetEnvironmentVariable(GlobalVar.EnvCfAccessClientSecret);
    }

    [TestCleanup]
    public void RestoreEnv()
    {
        Environment.SetEnvironmentVariable(GlobalVar.EnvQueueEnabled, oldEnabled);
        Environment.SetEnvironmentVariable(GlobalVar.EnvQueueBaseUrl, oldBaseUrl);
        Environment.SetEnvironmentVariable(GlobalVar.EnvCfAccessClientId, oldClientId);
        Environment.SetEnvironmentVariable(GlobalVar.EnvCfAccessClientSecret, oldClientSecret);
    }

    [TestMethod]
    public void QueueConstants_AreExpected()
    {
        GlobalVar.EnvQueueEnabled.Should().Be("DESKTOPSHELL_QUEUE_ENABLED");
        GlobalVar.EnvQueueBaseUrl.Should().Be("DESKTOPSHELL_QUEUE_BASEURL");
        GlobalVar.EnvCfAccessClientId.Should().Be("DESKTOPSHELL_CF_ACCESS_CLIENT_ID");
        GlobalVar.EnvCfAccessClientSecret.Should().Be("DESKTOPSHELL_CF_ACCESS_CLIENT_SECRET");

        GlobalVar.HeaderCfAccessClientId.Should().Be("CF-Access-Client-Id");
        GlobalVar.HeaderCfAccessClientSecret.Should().Be("CF-Access-Client-Secret");
    }

    [TestMethod]
    public void IsQueueConfiguredForAccess_WhenDisabled_ReturnsFalse()
    {
        Environment.SetEnvironmentVariable(GlobalVar.EnvQueueEnabled, "0");
        Environment.SetEnvironmentVariable(GlobalVar.EnvCfAccessClientId, "id");
        Environment.SetEnvironmentVariable(GlobalVar.EnvCfAccessClientSecret, "secret");

        GlobalVar.IsQueueConfiguredForAccess(out var reason).Should().BeFalse();
        reason.Should().Contain(GlobalVar.EnvQueueEnabled);
    }

    [TestMethod]
    public void IsQueueConfiguredForAccess_WhenEnabledButMissingSecret_ReturnsFalse()
    {
        Environment.SetEnvironmentVariable(GlobalVar.EnvQueueEnabled, "1");
        Environment.SetEnvironmentVariable(GlobalVar.EnvCfAccessClientId, "id");
        // Set to empty string (not null) so the process-scope value wins even if
        // the machine/user scopes have a configured secret on the dev box.
        Environment.SetEnvironmentVariable(GlobalVar.EnvCfAccessClientSecret, "");

        GlobalVar.IsQueueConfiguredForAccess(out var reason).Should().BeFalse();
        reason.Should().Contain(GlobalVar.EnvCfAccessClientSecret);
    }

    [TestMethod]
    public void IsQueueConfiguredForAccess_WhenEnabledAndSecretsPresent_ReturnsTrue()
    {
        Environment.SetEnvironmentVariable(GlobalVar.EnvQueueEnabled, "1");
        Environment.SetEnvironmentVariable(GlobalVar.EnvCfAccessClientId, "id");
        Environment.SetEnvironmentVariable(GlobalVar.EnvCfAccessClientSecret, "secret");

        GlobalVar.IsQueueConfiguredForAccess(out var reason).Should().BeTrue();
        reason.Should().BeNull();
    }
}
