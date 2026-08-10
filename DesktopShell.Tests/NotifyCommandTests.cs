using DesktopShell;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DesktopShell.Tests
{
    /// <summary>
    /// The `notify` command carries a status line to the on-screen overlay, and it
    /// arrives over the network from scripts on other machines. That is precisely the
    /// input that goes malformed unattended, so the parser gets its own tests.
    /// </summary>
    [TestClass]
    public class NotifyCommandTests
    {
        [TestMethod]
        public void Parse_PublisherAndText_DefaultsToWorking()
        {
            var got = Shell.ParseNotify("castaway stream stalled");

            got.Should().NotBeNull();
            got!.Value.State.Should().Be("working");
            got.Value.Publisher.Should().Be("castaway");
            got.Value.Text.Should().Be("stream stalled");
        }

        [TestMethod]
        public void Parse_LeadingState_IsRecognised()
        {
            var got = Shell.ParseNotify("warn castaway stream stalled - recasting");

            got!.Value.State.Should().Be("warn");
            got.Value.Publisher.Should().Be("castaway");
            got.Value.Text.Should().Be("stream stalled - recasting");
        }

        [TestMethod]
        public void Parse_StateIsCaseInsensitive()
        {
            Shell.ParseNotify("DONE backup finished")!.Value.State.Should().Be("done");
        }

        [TestMethod]
        public void Parse_MessageStartingWithAnOrdinaryWord_IsNotMistakenForAState()
        {
            // "working" is a state; "workshop" is not, and must stay the publisher.
            var got = Shell.ParseNotify("workshop lathe finished");

            got!.Value.State.Should().Be("working");
            got.Value.Publisher.Should().Be("workshop");
            got.Value.Text.Should().Be("lathe finished");
        }

        [TestMethod]
        public void Parse_TextCaseIsPreserved()
        {
            // The overlay shows this verbatim; "4.2 GB" must not arrive as "4.2 gb".
            Shell.ParseNotify("done backup Complete - 4.2 GB")!.Value.Text
                .Should().Be("Complete - 4.2 GB");
        }

        [TestMethod]
        public void Parse_ExtraWhitespace_IsIgnored()
        {
            var got = Shell.ParseNotify("  warn   torrent    import   failed  ");

            got!.Value.State.Should().Be("warn");
            got.Value.Publisher.Should().Be("torrent");
            got.Value.Text.Should().Be("import failed");
        }

        [TestMethod]
        public void Parse_PublisherWithNoText_IsRejected()
        {
            // A publisher name on its own is not a message, and publishing an empty
            // line would put a blank box on screen over a game.
            Shell.ParseNotify("castaway").Should().BeNull();
            Shell.ParseNotify("warn castaway").Should().BeNull();
        }

        [TestMethod]
        public void Parse_EmptyInput_IsRejected()
        {
            Shell.ParseNotify("").Should().BeNull();
            Shell.ParseNotify("   ").Should().BeNull();
            Shell.ParseNotify(null!).Should().BeNull();
        }

        [TestMethod]
        public void Parse_TextContainingShellMetacharacters_SurvivesIntact()
        {
            // Passed to the publisher as an argument LIST, never as a command string,
            // so quotes and ampersands are data rather than syntax.
            var got = Shell.ParseNotify(@"warn torrent ""S03E07"" & 100% failed");

            got!.Value.Text.Should().Be(@"""S03E07"" & 100% failed");
        }
    }
}
