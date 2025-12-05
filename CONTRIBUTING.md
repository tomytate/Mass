# Contributing to Mass Suite

## Development Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)
- Git

### Clone and Build

```bash
git clone https://github.com/masssuite/mass.git
cd mass
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
| `release/*` | Release preparation |

### Workflow

1. Create feature branch from `develop`
2. Implement changes with tests
3. Open pull request to `develop`
4. Code review and approval
5. Squash merge to `develop`

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
5. Internal methods
6. Private methods

### Comments

- Remove inline comments that restate code
- Keep XML docs for public API types only
- No TODO/FIXME markers in production code

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

### Integration Tests

- Test cross-component flows
- Use TestContainers for databases
- Mark with `[Trait("Category", "Integration")]`

## Pull Request Checklist

- [ ] Code compiles without warnings
- [ ] All tests pass
- [ ] No TODO/FIXME markers added
- [ ] Documentation updated (if applicable)
- [ ] Follows C# 14 conventions
- [ ] Breaking changes documented

## Code Review

### Reviewers Check

- Correctness: Does it work?
- Clarity: Is it readable?
- Completeness: Are edge cases handled?
- Consistency: Does it match existing patterns?

### Response Time

- Initial review: 24 hours
- Follow-up: 8 hours

## Release Process

1. Create `release/vX.Y.Z` branch
2. Update version in `Directory.Build.props`
3. Update CHANGELOG.md
4. Create pull request to `main`
5. Tag release after merge
6. CI/CD publishes artifacts
