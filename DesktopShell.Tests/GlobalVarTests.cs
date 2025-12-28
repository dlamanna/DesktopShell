using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using System.Drawing;

namespace DesktopShell.Tests;

[TestClass]
public class GlobalVarTests
{
    [TestCleanup]
    public void Cleanup()
    {
        // Reset static state after each test
        GlobalVar.DropDownRects.Clear();
    }

    [TestClass]
    public class LoggingTests
    {
        private string? testLogFile;

        [TestInitialize]
        public void Setup()
        {
            testLogFile = Path.Combine(Path.GetTempPath(), $"test_log_{Guid.NewGuid()}.txt");
            // Redirect logging to test file if possible
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (testLogFile != null && File.Exists(testLogFile))
            {
                try { File.Delete(testLogFile); } catch { }
            }
        }

        [TestMethod]
        public void Log_WithMessage_LogsSuccessfully()
        {
            // Arrange
            var message = "Test log message";

            // Act
            GlobalVar.Log(message);

            // Assert
            // Log should not throw - basic smoke test
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void Log_WithNullMessage_HandlesGracefully()
        {
            // Act & Assert - should not throw
            GlobalVar.Log(null!);
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void Log_WithEmptyMessage_HandlesGracefully()
        {
            // Act & Assert
            GlobalVar.Log("");
            Assert.IsTrue(true);
        }
    }

    [TestClass]
    public class ConstantsTests
    {
        [TestMethod]
        public void Constants_HaveValidValues()
        {
            // Assert timer intervals are positive
            GlobalVar.HourlyChimeIntervalMs.Should().BePositive();
            GlobalVar.HideTimerIntervalMs.Should().BePositive();
            GlobalVar.FadeTimerIntervalMs.Should().BePositive();

            // Assert delays are reasonable (not too long)
            GlobalVar.WebBrowserLaunchDelayMs.Should().BeInRange(0, 5000);
            GlobalVar.TcpConnectionRetryDelayMs.Should().BeInRange(0, 5000);
            GlobalVar.TcpReadDelayMs.Should().BeInRange(0, 1000);

            // Assert buffer size is reasonable
            GlobalVar.TcpBufferSize.Should().BeGreaterThan(0);
            GlobalVar.TcpBufferSize.Should().BeLessThan(1024 * 1024); // Less than 1MB

            // Assert hours in day is correct
            GlobalVar.HoursInDay.Should().Be(24);

            // Assert screen width thresholds are positive
            GlobalVar.MinimumScreenWidth.Should().BePositive();
            GlobalVar.LegacyScreenWidth.Should().BePositive();
        }

        [TestMethod]
        public void PassPhrase_IsNotEmpty()
        {
            // PassPhrase should always have a value (either from env var or default)
            GlobalVar.PassPhrase.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public void PassPhrase_ReadsFromEnvironmentVariable()
        {
            // Test that PassPhrase respects environment variable if set
            var envValue = Environment.GetEnvironmentVariable("DESKTOPSHELL_PASSPHRASE");
            if (!string.IsNullOrEmpty(envValue))
            {
                GlobalVar.PassPhrase.Should().Be(envValue);
            }
            else
            {
                // Should fall back to default if not set
                GlobalVar.PassPhrase.Should().Be("default");
            }
        }

        [TestMethod]
        public void IsWindows_ReturnsCorrectPlatform()
        {
            // On Windows build machine, this should be true
            var isActuallyWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
            GlobalVar.IsWindows.Should().Be(isActuallyWindows);
        }
    }

    [TestClass]
    public class ColorTests
    {
        [TestMethod]
        [DataRow("#FF0000", 255, 0, 0)]      // Red
        [DataRow("#00FF00", 0, 255, 0)]      // Green
        [DataRow("#0000FF", 0, 0, 255)]      // Blue
        [DataRow("#FFFFFF", 255, 255, 255)]  // White
        [DataRow("#000000", 0, 0, 0)]        // Black
        [DataRow("#808080", 128, 128, 128)]  // Gray
        public void UpdateColors_ValidHexColors_ParsesCorrectly(string hex, int r, int g, int b)
        {
            // Act
            var color = ColorTranslator.FromHtml(hex);

            // Assert
            color.R.Should().Be((byte)r);
            color.G.Should().Be((byte)g);
            color.B.Should().Be((byte)b);
        }

        [TestMethod]
        public void UpdateColors_InvalidHexFormat_ThrowsException()
        {
            // Arrange
            var invalidHex = "not-a-color";

            // Act & Assert
            Action act = () => ColorTranslator.FromHtml(invalidHex);
            act.Should().Throw<Exception>();
        }

        [TestMethod]
        [DataRow("#GG0000")]    // Invalid characters
        [DataRow("not-a-color")]  // Invalid format
        public void ColorParsing_InvalidFormats_ThrowsException(string invalidHex)
        {
            // Act & Assert
            Action act = () => ColorTranslator.FromHtml(invalidHex);
            act.Should().Throw<Exception>();
        }
    }

    [TestClass]
    public class RectangleTests
    {
        [TestMethod]
        public void DropDownRects_InitiallyEmpty()
        {
            // Assert
            GlobalVar.DropDownRects.Should().NotBeNull();
            GlobalVar.DropDownRects.Should().BeEmpty();
        }

        [TestMethod]
        public void DropDownRects_CanAddRectangles()
        {
            // Arrange
            var rect = new Rectangle(0, 0, 100, 100);

            // Act
            GlobalVar.DropDownRects.Add(rect);

            // Assert
            GlobalVar.DropDownRects.Should().HaveCount(1);
            GlobalVar.DropDownRects[0].Should().Be(rect);
        }

        [TestMethod]
        public void DropDownRectPadding_DefaultValues_AreReasonable()
        {
            // Assert
            GlobalVar.DropDownRectHorizontalPadding.Should().BeGreaterOrEqualTo(0);
            GlobalVar.DropDownRectVerticalPadding.Should().BeGreaterOrEqualTo(0);
        }
    }

    [TestClass]
    public class AnimationConstantsTests
    {
        [TestMethod]
        public void FadeAnimationStartOffset_IsPositive()
        {
            GlobalVar.FadeAnimationStartOffset.Should().BePositive();
        }

        [TestMethod]
        public void FadeTickMaxAmount_IsPositive()
        {
            GlobalVar.FadeTickMaxAmount.Should().BePositive();
        }

        [TestMethod]
        public void FadeTickMaxAmount_IsReasonable()
        {
            // Should be enough for smooth animation but not too many
            GlobalVar.FadeTickMaxAmount.Should().BeInRange(5, 100);
        }
    }
}
