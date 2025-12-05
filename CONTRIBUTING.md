# Contributing to Mass Suite

Thank you for your interest in contributing to Mass Suite!

## Development Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)
- Git

### Clone and Build

```bash
git clone https://github.com/tomytate/Mass.git
cd Mass
dotnet restore
dotnet build
```

### Run Tests

```bash
dotnet test
```

## Branching Strategy

| Branch | Purpose |
|--------|---------|
| `main` | Production-ready code |
| `develop` | Integration branch |
| `feature/*` | New features |
| `fix/*` | Bug fixes |

### Workflow

1. Fork the repository
2. Create feature branch from `main`
3. Implement changes with tests
4. Open pull request to `main`
5. Code review and approval
6. Squash merge

## Coding Conventions

### C# 14 Features

Use modern language features where they improve clarity:

```csharp
// Primary constructors
public class Service(ILogger logger) { }

// Collection expressions
List<string> items = ["a", "b", "c"];

// Pattern matching
if (result is { Success: true, Data: var data })
```

### Naming

| Type | Convention | Example |
|------|------------|---------|
| Classes | PascalCase | `WorkflowExecutor` |
| Interfaces | IPascalCase | `IUsbBurner` |
| Methods | PascalCase | `ExecuteAsync` |
| Parameters | camelCase | `cancellationToken` |
| Private fields | _camelCase | `_logger` |

### File Organization

Within each file:
1. Using directives (sorted, trimmed)
2. Namespace
3. Types (records → interfaces → classes)

Within each type:
1. Fields
2. Constructors
3. Properties
4. Public methods
5. Private methods

## Testing Requirements

### Unit Tests

- Cover all public methods
- Use xUnit with FluentAssertions
- Mock external dependencies

```csharp
[Fact]
public async Task ExecuteAsync_ValidWorkflow_ReturnsSuccess()
{
    // Arrange
    var executor = new WorkflowExecutor(Mock.Of<ILogService>());
    
    // Act
    var result = await executor.ExecuteAsync(workflow);
    
    // Assert
    result.Success.Should().BeTrue();
}
```

## Pull Request Checklist

- [ ] Code compiles without warnings
- [ ] All tests pass
- [ ] Documentation updated (if applicable)
- [ ] Follows C# 14 conventions

## Questions?

Open an issue at [github.com/tomytate/Mass/issues](https://github.com/tomytate/Mass/issues)

---

Thank you for contributing! 🚀
