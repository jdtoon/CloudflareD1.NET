# Contributing to CloudflareD1.NET

Thank you for your interest in contributing to CloudflareD1.NET! This document provides guidelines and instructions for contributing to the project.

## Code of Conduct

This project adheres to a code of conduct. By participating, you are expected to uphold this code. Please report unacceptable behavior to the project maintainers.

## How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check existing issues to avoid duplicates. When creating a bug report, include:

- **Clear title and description**
- **Steps to reproduce** the issue
- **Expected vs actual behavior**
- **Environment details** (.NET version, OS, etc.)
- **Code samples** or test cases if applicable

### Suggesting Enhancements

Enhancement suggestions are welcome! Please provide:

- **Clear use case** for the enhancement
- **Expected behavior** and benefits
- **Possible implementation** approach if you have ideas

### Pull Requests

1. **Fork the repository** and create your branch from `dev`
2. **Follow coding standards** (see below)
3. **Add tests** for new functionality
4. **Update documentation** as needed
5. **Ensure all tests pass** before submitting
6. **Write clear commit messages**

## Development Setup

### Prerequisites

- .NET SDK 6.0 or later
- Visual Studio 2022, VS Code, or Rider
- Git
- Node.js 18+ (for documentation)

### Getting Started

```bash
# Clone your fork
git clone https://github.com/YOUR-USERNAME/CloudflareD1.NET.git
cd CloudflareD1.NET

# Build the solution
dotnet build

# Run tests
dotnet test

# Run the sample
cd samples/ConsoleApp.Sample
dotnet run
```

### Project Structure

```
CloudflareD1.NET/
├── src/
│   └── CloudflareD1.NET/        # Main library
├── tests/
│   └── CloudflareD1.NET.Tests/  # Unit tests
├── samples/
│   └── ConsoleApp.Sample/       # Example applications
├── docs/                         # Docusaurus documentation
├── .github/
│   └── workflows/               # CI/CD pipelines
└── README.md
```

## Coding Standards

### C# Style Guide

- Follow [Microsoft's C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use **4 spaces** for indentation (no tabs)
- Use **meaningful names** for variables, methods, and classes
- Add **XML documentation** for all public APIs
- Use **async/await** for asynchronous operations
- Prefer **expression-bodied members** where appropriate

### Example

```csharp
/// <summary>
/// Executes a SQL query and returns the results.
/// </summary>
/// <param name="sql">The SQL query to execute.</param>
/// <param name="parameters">Optional parameters for the query.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>The query result containing rows and metadata.</returns>
public async Task<D1QueryResult> QueryAsync(
    string sql, 
    object? parameters = null, 
    CancellationToken cancellationToken = default)
{
    // Implementation
}
```

### Testing

- Write **unit tests** for new functionality
- Aim for **high code coverage**
- Use **descriptive test names**
- Follow **AAA pattern** (Arrange, Act, Assert)

```csharp
[Fact]
public async Task QueryAsync_WithValidSql_ReturnsResults()
{
    // Arrange
    var client = CreateTestClient();
    
    // Act
    var result = await client.QueryAsync("SELECT * FROM users");
    
    // Assert
    Assert.NotNull(result);
    Assert.True(result.Success);
}
```

## Documentation

### Code Documentation

- Add XML documentation to all public members
- Include examples in documentation where helpful
- Update README.md for significant changes

### Docusaurus Documentation

Documentation is in the `docs/` folder using Docusaurus.

```bash
cd docs
npm install
npm start  # Opens browser at http://localhost:3000
```

When adding features, update:
- API reference documentation
- Usage examples
- Getting started guide if applicable

## Git Workflow

### Branches

- `main`/`master` - Stable production releases
- `dev` - Active development (default branch for PRs)
- Feature branches - `feature/your-feature-name`
- Bug fix branches - `fix/issue-description`

### Commit Messages

Use clear, descriptive commit messages:

```
Add support for time travel queries

- Implement QueryAtTimestampAsync method
- Add timestamp validation
- Update documentation with examples
- Add unit tests for time travel functionality
```

Format:
- First line: Brief summary (50 chars or less)
- Blank line
- Detailed description if needed
- Reference issues: `Fixes #123` or `Closes #456`

## Release Process

1. Update version in `CloudflareD1.NET.csproj`
2. Update `CHANGELOG.md`
3. Create PR to `main` from `dev`
4. After merge, tag release: `git tag v1.0.0`
5. GitHub Actions will automatically publish to NuGet

## Getting Help

- Check the [documentation](https://yourusername.github.io/CloudflareD1.NET/)
- Search [existing issues](https://github.com/yourusername/CloudflareD1.NET/issues)
- Ask in [GitHub Discussions](https://github.com/yourusername/CloudflareD1.NET/discussions)

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

## Recognition

Contributors will be recognized in:
- The project README
- Release notes
- The documentation site

Thank you for contributing to CloudflareD1.NET! 🎉
