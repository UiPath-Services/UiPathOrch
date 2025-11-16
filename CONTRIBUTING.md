# Contributing to UiPathOrch

Thank you for your interest in contributing to UiPathOrch! This document provides guidelines for contributing to this project.

## Code of Conduct

By participating in this project, you agree to maintain a respectful and collaborative environment.

## How to Contribute

### Reporting Bugs

If you find a bug, please create an issue with the following information:

- **Description**: Clear description of the issue
- **Steps to Reproduce**: Detailed steps to reproduce the problem
- **Expected Behavior**: What you expected to happen
- **Actual Behavior**: What actually happened
- **Environment**: PowerShell version, OS, Orchestrator version
- **Error Messages**: Any error messages or logs

### Suggesting Enhancements

We welcome suggestions for new features or improvements:

- Check if the enhancement has already been suggested
- Provide a clear description of the proposed feature
- Explain why this enhancement would be useful
- Include examples of how it would be used

### Pull Requests

1. **Fork the repository** and create your branch from `master`
2. **Make your changes** following the coding guidelines below
3. **Test your changes** thoroughly
4. **Update documentation** if needed
5. **Commit your changes** with clear commit messages
6. **Push to your fork** and submit a pull request

#### Pull Request Guidelines

- Provide a clear description of the changes
- Reference any related issues
- Ensure all tests pass
- Follow the existing code style
- Keep changes focused and atomic

## Development Setup

### Prerequisites

- PowerShell 7.4.2 or later
- .NET SDK (for C# development)
- UiPath Orchestrator access for testing

### Building the Project

```powershell
# Navigate to the project directory
cd OrchProvider

# Build the project
dotnet build
```

### Running Tests

```powershell
# Run tests (if available)
# TODO: Add test instructions when test suite is available
```

## Coding Guidelines

### PowerShell Code

- Follow [PowerShell Practice and Style Guide](https://poshcode.gitbook.io/powershell-practice-and-style/)
- Use approved verbs for cmdlet names (`Get-Verb` to list)
- Include comment-based help for all cmdlets
- Use meaningful parameter names
- Validate parameters appropriately

### C# Code

- Follow standard C# coding conventions
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Keep methods focused and concise

### Commit Messages

Write clear, concise commit messages in English:

- Use present tense ("Add feature" not "Added feature")
- Keep the first line under 72 characters
- Provide detailed description in the body if needed
- Reference issues when applicable (e.g., "Fixes #123")

Example:
```
Add support for bulk asset operations

- Implement Get-OrchAsset with -Recurse parameter
- Add wildcard pattern matching
- Update documentation

Fixes #42
```

## Documentation

- Update README.md for user-facing changes
- Add or update cmdlet help documentation
- Include examples in help documentation
- Update manual if significant features are added

## Review Process

1. Maintainers will review your pull request
2. Address any feedback or requested changes
3. Once approved, a maintainer will merge your contribution

## Questions?

If you have questions about contributing, feel free to:

- Open an issue for discussion
- Contact the maintainers
- Check existing documentation and issues

## License

By contributing to UiPathOrch, you agree that your contributions will be licensed under the Apache License 2.0.

---

Thank you for contributing to UiPathOrch! 🎉
