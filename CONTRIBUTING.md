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
- .NET SDK 8.0 or later
- UiPath Orchestrator access for testing

### Building the Project

```powershell
.\Build-Deploy.ps1 -BuildOnly   # Build only
.\Build-Deploy.ps1              # Build and deploy to module directory
```

### Running Tests

There are two test layers.

**1. Unit tests (xUnit) — no Orchestrator access required.** Fast, deterministic
tests of the extracted pure logic (pagination, OData escaping, CSV round-trips,
version/compare, auth-flow selection, etc.). This is what CI runs on every push:

```powershell
dotnet test
```

**2. Live integration tests (Pester) — require connected Orchestrator drives.**
These exercise end-to-end behavior against a real tenant and share **one mutable,
disposable** tenant. Do **not** run them with a bare `Invoke-Pester -Path Tests\`:
the files are order-dependent (some wipe the tenant, others assume it is
populated), so a bare run cascades into false mass failures. Use the supplied
runner, which resets and re-imports the fixture before each file:

```powershell
Import-OrchConfig
# -Tenant is the DISPOSABLE tenant (it WILL be wiped); -RefDrive is a read-only reference.
.\Tests\Invoke-AllTests.ps1 -Tenant Orch2 -RefDrive Orch1
```

See `Tests\Invoke-AllTests.ps1` (and `Tests\README.md`) for `-Filter`,
`-Exclude`, and `-SkipReset` / `-SkipImport` options.

## Coding Guidelines

### PowerShell Code

- Follow [PowerShell Practice and Style Guide](https://poshcode.gitbook.io/powershell-practice-and-style/)
- Use approved verbs for cmdlet names (`Get-Verb` to list)
- Document cmdlets via the external PlatyPS help (Markdown under `docs\help\en-US\`,
  compiled to `UiPathOrch.dll-Help.xml`) — this module uses MAML external help, not
  comment-based help. Add or update the cmdlet's `.md` page when you add or change a cmdlet
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
- Add or update the cmdlet's PlatyPS help page under `docs\help\en-US\`
- Include examples in the help page
- Update the guides under `docs\` if significant features are added

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
