# Contributing to Mass Suite

Thank you for your interest in contributing to Mass Suite! This document provides guidelines and instructions for contributing to the project.

## Code of Conduct

Be respectful and constructive in all interactions.

## Development Workflow

1. **Fork** the repository
2. **Clone** your fork locally
3. **Create** a feature branch from `develop`
4. **Make** your changes
5. **Test** thoroughly
6. **Commit** with descriptive messages (Conventional Commits format)
7. **Push** to your fork
8. **Submit** a pull request to `develop`

## Commit Message Format

We use [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

**Types**: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `chore`, `ci`

**Examples**:
- `feat(launcher): add plugin discovery system`
- `fix(core): resolve domain event inheritance issue`
- `docs(readme): update installation instructions`

## Coding Standards

- Follow `.editorconfig` conventions
- Use C# 14 features where appropriate
- Maintain 80%+ test coverage for new code
- Use file-scoped namespaces
- Enable nullable reference types
- Write self-documenting code
- Add XML documentation for public APIs

## Project Structure

```
src/
├── Mass.Launcher/    # Platform launcher
├── Mass.Core/        # Core abstractions
├── ProUSB/          # ProUSB tool
└── MassBoot/        # MassBoot server

tests/               # Unit & integration tests
plugins/             # First-party plugins
```

## Building

```powershell
dotnet restore
dotnet build
```

## Testing

```powershell
dotnet test
```

## Pull Request Process

1. Update documentation if needed
2. Add/update tests for changes
3. Ensure all tests pass
4. Update CHANGELOG.md with PR number
5. Request review from maintainers
6. Address review feedback
7. Squash commits before merge (if needed)

## Plugin Development

See `/docs/plugin-development/` for plugin SDK documentation.

## Questions?

Open an issue for discussion before starting large changes.
