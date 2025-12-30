using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using DesktopShell.Tests.TestHelpers;
using System.Text.RegularExpressions;

namespace DesktopShell.Tests;

[TestClass]
public static partial class SettingsTests
{
    [TestClass]
    public class ColorParsingTests
    {
        [TestMethod]
        [DataRow("#FF0000", 255, 0, 0, DisplayName = "Red")]
        [DataRow("#00FF00", 0, 255, 0, DisplayName = "Green")]
        [DataRow("#0000FF", 0, 0, 255, DisplayName = "Blue")]
        [DataRow("#FFFFFF", 255, 255, 255, DisplayName = "White")]
        [DataRow("#000000", 0, 0, 0, DisplayName = "Black")]
        [DataRow("#FF8800", 255, 136, 0, DisplayName = "Orange")]
        public void ColorTranslator_ValidHexColor_ParsesCorrectly(
            string hex, int r, int g, int b)
        {
            // Act
            var color = ColorTranslator.FromHtml(hex);

            // Assert
            color.R.Should().Be((byte)r);
            color.G.Should().Be((byte)g);
            color.B.Should().Be((byte)b);
        }

        [TestMethod]
        [DataRow("#GGGGGG", DisplayName = "Invalid characters")]
        [DataRow("not-a-color", DisplayName = "Invalid format")]
        [DataRow("#ZZZZZZ", DisplayName = "Invalid hex digits")]
        public void ColorTranslator_InvalidHexColor_ThrowsException(string invalidHex)
        {
            // Act & Assert
            Action act = () => ColorTranslator.FromHtml(invalidHex);
            act.Should().Throw<Exception>();
        }
    }

    [TestClass]
    public partial class RegexPatternsTests
    {
        [GeneratedRegex("^(#)?([a-fA-F0-9]){6}$")]
        private static partial Regex HexColorRegex();

        [TestMethod]
        public void HexColorRegex_ValidFormats_Matches()
        {
            // Assert - valid formats
            HexColorRegex().IsMatch("#FF0000").Should().BeTrue();
            HexColorRegex().IsMatch("#ff0000").Should().BeTrue();
            HexColorRegex().IsMatch("#AbCdEf").Should().BeTrue();
            HexColorRegex().IsMatch("FF0000").Should().BeTrue();  // Without #
        }

        [TestMethod]
        public void HexColorRegex_InvalidFormats_DoesNotMatch()
        {
            // Assert - invalid formats
            HexColorRegex().IsMatch("#GGGGGG").Should().BeFalse();  // Invalid chars
            HexColorRegex().IsMatch("#FF00").Should().BeFalse();    // Too short
            HexColorRegex().IsMatch("#FF00000").Should().BeFalse(); // Too long
            HexColorRegex().IsMatch("not-color").Should().BeFalse();
        }
    }

    [TestClass]
    public class BooleanParsingTests
    {
        [TestMethod]
        [DataRow("true", true)]
        [DataRow("True", true)]
        [DataRow("TRUE", true)]
        [DataRow("false", false)]
        [DataRow("False", false)]
        [DataRow("FALSE", false)]
        public void BooleanParsing_ValidStrings_ParsesCorrectly(
            string input, bool expected)
        {
            // Act
            var result = bool.Parse(input);

            // Assert
            result.Should().Be(expected);
        }

        [TestMethod]
        [DataRow("yes")]
        [DataRow("no")]
        [DataRow("1")]
        [DataRow("0")]
        [DataRow("")]
        public void BooleanParsing_InvalidStrings_ThrowsException(string input)
        {
            // Act & Assert
            Action act = () => bool.Parse(input);
            act.Should().Throw<FormatException>();
        }

        [TestMethod]
        [DataRow("true", true)]
        [DataRow("false", false)]
        [DataRow("invalid", false)]
        [DataRow("", false)]
        public void TryParseBool_VariousInputs_HandlesGracefully(
            string input, bool expectedDefault)
        {
            // Act
            var success = bool.TryParse(input, out var result);

            // Assert
            if (input == "true" || input == "false")
            {
                success.Should().BeTrue();
            }
            else
            {
                success.Should().BeFalse();
                result.Should().Be(expectedDefault);
            }
        }
    }

    [TestClass]
    public class PathValidationTests
    {
        [TestMethod]
        [DataRow(@"C:\Windows\System32")]
        [DataRow(@"C:\Program Files")]
        [DataRow(@"D:\My Documents")]
        public void PathValidation_ValidWindowsPaths_AreValid(string path)
        {
            // Act
            var hasInvalidChars = path.IndexOfAny(Path.GetInvalidPathChars()) >= 0;

            // Assert
            hasInvalidChars.Should().BeFalse();
        }

        [TestMethod]
        [DataRow(@"C:\Invalid|Path")]
        public void PathValidation_InvalidWindowsPaths_AreInvalid(string path)
        {
            // Act
            var hasInvalidChars = path.IndexOfAny(Path.GetInvalidPathChars()) >= 0;

            // Assert
            hasInvalidChars.Should().BeTrue();
        }

        [TestMethod]
        public void PathCombine_WithValidParts_BuildsCorrectPath()
        {
            // Act
            var result = Path.Combine(@"C:\Users", "TestUser", "Documents");

            // Assert
            result.Should().Be(@"C:\Users\TestUser\Documents");
        }
    }

    [TestClass]
    public class ScreenEnabledParsingTests
    {
        [TestMethod]
        [DataRow("true,false,true", new[] { true, false, true })]
        [DataRow("false,false,false", new[] { false, false, false })]
        [DataRow("true,true", new[] { true, true })]
        [DataRow("false", new[] { false })]
        public void ScreenEnabledParsing_ValidFormats_ParsesCorrectly(
            string input, bool[] expected)
        {
            // Act
            var result = input.Split(',')
                              .Select(s => bool.Parse(s.Trim()))
                              .ToArray();

            // Assert
            result.Should().Equal(expected);
        }

        [TestMethod]
        public void ScreenEnabledParsing_EmptyString_ReturnsEmptyArray()
        {
            // Arrange
            var input = "";

            // Act
            var result = input.Split(',', StringSplitOptions.RemoveEmptyEntries)
                              .Select(s => bool.TryParse(s.Trim(), out var b) ? b : false)
                              .ToArray();

            // Assert
            result.Should().BeEmpty();
        }
    }

    [TestClass]
    public class PositionParsingTests
    {
        [TestMethod]
        [DataRow("100,200", 100, 200)]
        [DataRow("0,0", 0, 0)]
        [DataRow("-10,50", -10, 50)]
        [DataRow("1920,1080", 1920, 1080)]
        public void PositionParsing_ValidFormats_ParsesCorrectly(
            string input, int expectedX, int expectedY)
        {
            // Act
            var parts = input.Split(',');
            var x = int.Parse(parts[0]);
            var y = int.Parse(parts[1]);

            // Assert
            x.Should().Be(expectedX);
            y.Should().Be(expectedY);
        }

        [TestMethod]
        [DataRow("100")]       // Missing Y
        [DataRow("100,")]      // Incomplete
        [DataRow(",200")]      // Missing X
        [DataRow("abc,def")]   // Invalid numbers
        [DataRow("")]          // Empty
        public void PositionParsing_InvalidFormats_ThrowsException(string input)
        {
            // Act & Assert
            Action act = () =>
            {
                var parts = input.Split(',');
                int.Parse(parts[0]);
                int.Parse(parts[1]);
            };
            act.Should().Throw<Exception>();
        }
    }

    [TestClass]
    public class FilePathTests
    {
        [TestMethod]
        public void DirectoryGetCurrentDirectory_ReturnsValidPath()
        {
            // Act
            var currentDir = Directory.GetCurrentDirectory();

            // Assert
            currentDir.Should().NotBeNullOrEmpty();
            Directory.Exists(currentDir).Should().BeTrue();
        }

        [TestMethod]
        public void PathGetTempPath_ReturnsValidPath()
        {
            // Act
            var tempPath = Path.GetTempPath();

            // Assert
            tempPath.Should().NotBeNullOrEmpty();
            Directory.Exists(tempPath).Should().BeTrue();
        }
    }
}
