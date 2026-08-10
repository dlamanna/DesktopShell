using DesktopShell;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace DesktopShell.Tests
{
    /// <summary>
    /// The queue poll interval. This is a request per interval per machine against a
    /// Durable Object, so the bounds matter as much as the value.
    /// </summary>
    [TestClass]
    public class QueuePollingTests
    {
        private static void SetInterval(string? value)
        {
            Environment.SetEnvironmentVariable(GlobalVar.EnvQueuePollSeconds, value);
        }

        [TestCleanup]
        public void Cleanup() => SetInterval(null);

        [TestMethod]
        public void Unset_DefaultsToTwentySeconds()
        {
            SetInterval(null);
            GlobalVar.QueuePollSeconds.Should().Be(20);
        }

        [TestMethod]
        public void Zero_DisablesPolling()
        {
            // 0 is a real choice: the startup drain still runs, nothing polls after.
            SetInterval("0");
            GlobalVar.QueuePollSeconds.Should().Be(0);
        }

        [TestMethod]
        public void TooSmall_IsFlooredNotObeyed()
        {
            // A mistyped 1 would be ~86,000 requests a day per machine.
            SetInterval("1");
            GlobalVar.QueuePollSeconds.Should().Be(5);
        }

        [TestMethod]
        public void TooLarge_IsCapped()
        {
            SetInterval("999999");
            GlobalVar.QueuePollSeconds.Should().Be(3600);
        }

        [TestMethod]
        public void Nonsense_FallsBackToTheDefault()
        {
            // Env vars are typed by hand and by scripts; neither is reliable.
            foreach (var junk in new[] { "soon", "", "   ", "20s", "twenty" })
            {
                SetInterval(junk);
                GlobalVar.QueuePollSeconds.Should().Be(20, $"input was '{junk}'");
            }
        }

        [TestMethod]
        public void Negative_DisablesRatherThanThrowing()
        {
            SetInterval("-5");
            GlobalVar.QueuePollSeconds.Should().Be(0);
        }

        [TestMethod]
        public void Whitespace_IsTrimmed()
        {
            SetInterval("  45  ");
            GlobalVar.QueuePollSeconds.Should().Be(45);
        }
    }
}
