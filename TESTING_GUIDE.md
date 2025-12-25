# 🧪 DesktopShell Testing Guide

## Table of Contents
- [Running Tests](#running-tests)
- [Code Coverage](#code-coverage)
- [Writing New Tests](#writing-new-tests)
- [Test Categories](#test-categories)
- [CI/CD Setup](#cicd-setup)

## Running Tests

### Command Line
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test
dotnet test --filter "FullyQualifiedName~GlobalVarTests"

# Run in watch mode (auto-rerun on changes)
dotnet watch test
```

### VS Code
1. Open Test Explorer (beaker icon in sidebar)
2. Click "Run All Tests" or run individual tests
3. View results inline

## Code Coverage

### Generate Coverage Report
```bash
# Install report generator (one-time)
dotnet tool install -g dotnet-reportgenerator-globaltool

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults

# Generate HTML report
reportgenerator -reports:"./TestResults/**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html

# Open report
start coveragereport/index.html  # Windows
```

### Coverage Goals
- **Target**: 70%+ overall coverage
- **Critical code**: 90%+ (GlobalVar utilities, Settings parsing)
- **UI code**: 50%+ (forms are harder to test)

## Writing New Tests

### Test Structure (AAA Pattern)
```csharp
[TestMethod]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange - Set up test data
    var input = "test data";
    
    // Act - Execute the method
    var result = MyClass.MyMethod(input);
    
    // Assert - Verify the result
    result.Should().Be("expected");
}
```

### Using FluentAssertions
```csharp
// Better readability than Assert.AreEqual
result.Should().NotBeNull();
result.Should().Be(expected);
result.Should().Contain("substring");
result.Should().HaveCount(5);
list.Should().ContainInOrder("a", "b", "c");
```

### Using Moq for Mocking
```csharp
// Mock file system operations
var mockFileSystem = new Mock<IFileSystem>();
mockFileSystem.Setup(fs => fs.ReadAllText(It.IsAny<string>()))
              .Returns("mock content");
```

## Test Categories

### ✅ Easy to Test (High Priority)
**GlobalVar utility methods:**
- [x] `SplitWords()` - string parsing
- [x] `IsInField()` - rectangle collision detection
- [ ] `GetSetting()` - settings file parsing
- [ ] `Log()` - logging functionality
- [ ] `SendRemoteCommand()` - TCP communication (with mocks)
- [ ] `UpdateColors()` - color conversion
- [ ] `SetCentered()` - screen positioning calculations

**Data Classes:**
- [x] `Combination` record - keyword/filepath pairs
- [x] `WwwBrowser` record - browser configurations
- [ ] `WebCombo` validation - website combo validation

**Settings:**
- [ ] `Settings.ScanSettings()` - parsing settings.ini
- [ ] `Settings.WriteSettings()` - writing settings
- [ ] Color conversion (hex to Color)

### ⚠️ Moderate Difficulty
**File Operations (use temp files):**
- [ ] `PopulateCombos()` - reading shortcuts.txt
- [ ] `PopulateWebSites()` - reading websites.txt
- [ ] Settings file I/O

**Command Processing:**
- [ ] `HardCodedCombos()` - command regex matching
- [ ] Web search URL building

### 🔴 Challenging (Lower Priority)
**UI/Forms (requires UI testing framework):**
- Timer-based animations
- Form visibility logic
- Mouse cursor detection
- Multi-monitor support

**System Integration:**
- Process launching (`GlobalVar.Run()`)
- TCP server communication
- Sound playback

## Example Test Suite

### GlobalVarTests.cs
```csharp
[TestClass]
public class GlobalVarTests
{
    [TestMethod]
    public void GetSetting_ValidLineNumber_ReturnsSettingValue()
    {
        // Arrange
        string tempFile = TestHelpers.CreateTempSettingsFile(
            TestHelpers.GetSampleSettingsContent()
        );
        
        try
        {
            // Act
            string? result = GlobalVar.GetSetting(0);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("=");
        }
        finally
        {
            TestHelpers.CleanupTempFile(tempFile);
        }
    }
    
    [TestMethod]
    public void SendRemoteCommand_ValidCommand_SendsSuccessfully()
    {
        // Arrange
        var mockSocket = new Mock<Socket>();
        // ... setup mock
        
        // Act & Assert
        // Test TCP sending without actual network
    }
}
```

### SettingsTests.cs
```csharp
[TestClass]
public class SettingsTests
{
    [TestMethod]
    public void ScanSettings_ValidFile_ParsesCorrectly()
    {
        // Test color parsing
        // Test boolean parsing
        // Test path parsing
    }
    
    [TestMethod]
    [DataRow("#FF0000", 255, 0, 0)]
    [DataRow("#00FF00", 0, 255, 0)]
    [DataRow("#0000FF", 0, 0, 255)]
    public void ColorParsing_ValidHex_ConvertsCorrectly(
        string hex, int r, int g, int b)
    {
        var color = ColorTranslator.FromHtml(hex);
        color.R.Should().Be(r);
        color.G.Should().Be(g);
        color.B.Should().Be(b);
    }
}
```

## CI/CD Setup

### GitHub Actions (Free!)
The `.github/workflows/dotnet-ci.yml` file runs automatically on:
- Every push to `master`/`main`/`develop`
- Every pull request

**What it does:**
1. ✅ Builds the project
2. ✅ Runs all tests
3. ✅ Generates code coverage report
4. ✅ Comments coverage % on PRs
5. ✅ Uploads to Codecov (if configured)

### Setup Steps:
1. **Codecov (optional, free for public repos):**
   - Sign up at https://codecov.io with GitHub
   - Add your repo
   - Get token from Settings → Copy token
   - Add to GitHub: Settings → Secrets → New secret
     - Name: `CODECOV_TOKEN`
     - Value: (paste token)

2. **Coverage Badge:**
   Add to README.md:
   ```markdown
   [![codecov](https://codecov.io/gh/YOUR_USERNAME/DesktopShell/branch/master/graph/badge.svg)](https://codecov.io/gh/YOUR_USERNAME/DesktopShell)
   ```

### Local Pre-commit Hook (Optional)
```bash
# .git/hooks/pre-commit
#!/bin/sh
dotnet test --no-build
if [ $? -ne 0 ]; then
    echo "Tests failed. Commit aborted."
    exit 1
fi
```

## Test Coverage Strategy

### Phase 1: Core Utilities (Target: 80%)
- GlobalVar static methods
- Settings parsing
- Data records validation

### Phase 2: Command Processing (Target: 70%)
- Regex matching
- URL building
- File parsing

### Phase 3: Integration Tests (Target: 50%)
- End-to-end command flows
- File I/O with temp files
- Mock process launching

### Phase 4: UI Tests (Target: 30%)
- Form visibility logic (without actual UI)
- Animation state machine
- Mock timer callbacks

## Best Practices

1. **Test naming**: `MethodName_Scenario_ExpectedResult`
2. **One assert per test**: Focus on one behavior
3. **Use test helpers**: Reduce duplication
4. **Clean up resources**: Use `finally` blocks
5. **Mock external dependencies**: File system, network, processes
6. **Test edge cases**: null, empty, negative values
7. **Test error conditions**: Exceptions, invalid input

## Resources
- [MSTest Documentation](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-mstest)
- [FluentAssertions Docs](https://fluentassertions.com/introduction)
- [Moq Quickstart](https://github.com/moq/moq4/wiki/Quickstart)
- [Code Coverage Tools](https://github.com/coverlet-coverage/coverlet)
