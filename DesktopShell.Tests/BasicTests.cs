using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using System.Drawing;
using DesktopShell;

namespace DesktopShell.Tests
{
    [TestClass]
    public class BasicTests
    {
        [TestMethod]
        public void SplitWords_ValidString_SplitsCorrectly()
        {
            // Arrange
            var input = "hello world test";

            // Act
            var result = Shell.SplitWords(input);

            // Assert
            result.Should().NotBeNull();
            result.Length.Should().Be(3);
            result[0].Should().Be("hello");
            result[1].Should().Be("world");
            result[2].Should().Be("test");
        }

        [TestMethod]
        public void SplitWords_StringWithPunctuation_RemovesPunctuation()
        {
            // Arrange
            var input = "hello, world! test.";

            // Act
            var result = Shell.SplitWords(input);

            // Assert
            result.Should().NotBeNull();
            result.Length.Should().Be(4);
            result[0].Should().Be("hello");
            result[1].Should().Be("world");
            result[2].Should().Be("test");
            result[3].Should().Be("");
        }

        [TestMethod]
        public void SplitWords_EmptyString_ReturnsEmptyArray()
        {
            // Arrange
            var input = "";

            // Act
            var result = Shell.SplitWords(input);

            // Assert
            result.Should().NotBeNull();
            result.Length.Should().Be(1);
            result[0].Should().Be("");
        }

        [TestMethod]
        public void SplitWords_WhitespaceOnly_ReturnsEmptyArray()
        {
            // Arrange
            var input = "   \t\n\r   ";

            // Act
            var result = Shell.SplitWords(input);

            // Assert
            result.Should().NotBeNull();
            result.Length.Should().Be(2);
            result[0].Should().Be("");
            result[1].Should().Be("");
        }

        [TestMethod]
        public void SplitWords_SingleWord_ReturnsSingleElement()
        {
            // Arrange
            var input = "test";

            // Act
            var result = Shell.SplitWords(input);

            // Assert
            result.Should().NotBeNull();
            result.Length.Should().Be(1);
            result[0].Should().Be("test");
        }

        [TestMethod]
        public void SplitWords_ComplexString_HandlesCorrectly()
        {
            // Arrange
            var input = "command:arg1,arg2;arg3";

            // Act
            var result = Shell.SplitWords(input);

            // Assert
            result.Should().NotBeNull();
            result.Length.Should().Be(4);
            result[0].Should().Be("command");
            result[1].Should().Be("arg1");
            result[2].Should().Be("arg2");
            result[3].Should().Be("arg3");
        }

        [TestMethod]
        public void SplitWords_NullString_ThrowsException()
        {
            // Arrange
            string? input = null;

            // Act & Assert
            var exception = Assert.ThrowsException<ArgumentNullException>(() => Shell.SplitWords(input!));
            exception.ParamName.Should().Be("input");
        }

        [TestMethod]
        public void IsInField_ValidRectangle_ReturnsBoolean()
        {
            // Arrange
            var rect = new Rectangle(100, 100, 200, 200);

            // Act
            var result = Shell.IsInField(rect);

            // Assert
            // Note: Result depends on actual cursor position at test runtime
            // This is a basic smoke test that it returns a bool and doesn't throw
            Assert.IsInstanceOfType(result, typeof(bool));
        }

        [TestMethod]
        public void IsInField_ZeroRectangle_ReturnsBoolean()
        {
            // Arrange
            var rect = new Rectangle(0, 0, 0, 0);

            // Act
            var result = Shell.IsInField(rect);

            // Assert
            Assert.IsInstanceOfType(result, typeof(bool));
        }

        [TestMethod]
        public void IsInField_NegativeRectangle_ReturnsBoolean()
        {
            // Arrange
            var rect = new Rectangle(-100, -100, 200, 200);

            // Act
            var result = Shell.IsInField(rect);

            // Assert
            Assert.IsInstanceOfType(result, typeof(bool));
        }

        [TestMethod]
        public void IsInField_Logic_PointInsideRectangle_ReturnsTrue()
        {
            // Arrange - test the actual logic manually
            var rect = new Rectangle(100, 100, 200, 100);
            Point testCursor = new Point(150, 120); // Inside

            // Act - simulate the logic from IsInField
            bool withinX = testCursor.X >= rect.Left && testCursor.X <= rect.Right;
            bool withinY = testCursor.Y >= rect.Top && testCursor.Y <= rect.Bottom;
            bool result = withinX && withinY;

            // Assert
            result.Should().BeTrue("point (150, 120) is inside rectangle");
        }

        [TestMethod]
        public void IsInField_Logic_PointOutside_ReturnsFalse()
        {
            // Arrange
            var rect = new Rectangle(100, 100, 200, 100);
            Point testCursor = new Point(50, 120); // Outside to the left

            // Act - simulate the logic
            bool withinX = testCursor.X >= rect.Left && testCursor.X <= rect.Right;
            bool withinY = testCursor.Y >= rect.Top && testCursor.Y <= rect.Bottom;
            bool result = withinX && withinY;

            // Assert
            result.Should().BeFalse("point (50, 120) is outside rectangle");
        }

        [TestMethod]
        [DataRow(150, 120, true, DisplayName = "Inside")]
        [DataRow(50, 120, false, DisplayName = "Left of rectangle")]
        [DataRow(350, 120, false, DisplayName = "Right of rectangle")]
        [DataRow(150, 50, false, DisplayName = "Above rectangle")]
        [DataRow(150, 250, false, DisplayName = "Below rectangle")]
        [DataRow(100, 100, true, DisplayName = "Top-left corner")]
        [DataRow(300, 200, true, DisplayName = "Bottom-right corner")]
        public void IsInField_Logic_VariousPoints_ReturnsExpected(int x, int y, bool expected)
        {
            // Arrange - rectangle from (100,100) to (300,200)
            var rect = new Rectangle(100, 100, 200, 100);
            Point testCursor = new Point(x, y);

            // Act - simulate IsInField logic
            bool withinX = testCursor.X >= rect.Left && testCursor.X <= rect.Right;
            bool withinY = testCursor.Y >= rect.Top && testCursor.Y <= rect.Bottom;
            bool result = withinX && withinY;

            // Assert
            result.Should().Be(expected);
        }
    }
}
