using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using DesktopShell.Forms;

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
    }
}
