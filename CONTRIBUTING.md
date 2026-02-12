# Contributing to Banking Service

Thank you for your interest in contributing to the Banking Service project! This document provides guidelines for both human developers and AI assistants (GitHub Copilot, Cursor, etc.).

## Table of Contents

- [Getting Started](#getting-started)
- [Development Workflow](#development-workflow)
- [Code Standards](#code-standards)
- [AI-Assisted Development](#ai-assisted-development)
- [Testing Requirements](#testing-requirements)
- [Pull Request Process](#pull-request-process)
- [Documentation](#documentation)

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Git
- Docker (optional, for containerized development)
- VS Code, Visual Studio 2022, or Rider

### Setup

1. **Fork and clone the repository**
   ```bash
   git clone https://github.com/yourusername/banking-service.git
   cd banking-service
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Run the application**
   ```bash
   cd BankingService
   dotnet run
   ```

4. **Run tests**
   ```bash
   cd BankingService.Tests
   dotnet test
   ```

## Development Workflow

### Branch Strategy

- `main` - Production-ready code
- `develop` - Integration branch for features
- `feature/{feature-name}` - New features
- `bugfix/{bug-name}` - Bug fixes
- `hotfix/{issue}` - Critical production fixes

### Creating a Feature

1. **Create a branch**
   ```bash
   git checkout develop
   git pull origin develop
   git checkout -b feature/my-new-feature
   ```

2. **Develop your feature**
   - Follow code standards (see `.github/system-instructions.md`)
   - Write tests for new functionality
   - Update documentation

3. **Run quality checks**
   ```bash
   # Format code
   dotnet format
   
   # Run tests
   dotnet test
   
   # Build in release mode
   dotnet build --configuration Release
   ```

4. **Commit your changes**
   ```bash
   git add .
   git commit -m "feat: add user notification service"
   ```

### Commit Message Convention

Follow [Conventional Commits](https://www.conventionalcommits.org/):

- `feat:` - New feature
- `fix:` - Bug fix
- `docs:` - Documentation changes
- `style:` - Code style changes (formatting)
- `refactor:` - Code refactoring
- `test:` - Adding or updating tests
- `chore:` - Maintenance tasks

**Examples**:
```
feat: add email notification service
fix: resolve null reference in transaction service
docs: update API documentation for cards endpoint
test: add integration tests for statement generation
refactor: extract account validation logic
```

## Code Standards

### Architecture Patterns

This project follows Clean Architecture principles:

```
Controllers → Services → Data Layer
     ↓           ↓            ↓
   HTTP    Business Logic  Database
```

**Key Principles**:
1. Controllers handle HTTP only
2. Services contain business logic
3. Keep models separate from DTOs
4. Use dependency injection
5. Follow SOLID principles

### Coding Style

We use EditorConfig to enforce consistent coding style. Key rules:

- **Indentation**: 4 spaces
- **Braces**: Required for all blocks
- **Naming**:
  - Classes, methods, properties: `PascalCase`
  - Private fields: `_camelCase`
  - Interfaces: `IPascalCase`
  - Async methods: `MethodNameAsync`
- **Files**: One public class per file
- **Usings**: System directives first, sorted

### Required Attributes

**Controllers**:
```csharp
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MyController : ControllerBase
```

**Models**:
```csharp
[Key]
public Guid Id { get; set; }

[Required]
[MaxLength(100)]
public string Name { get; set; } = string.Empty;
```

**DTOs**:
```csharp
[Required]
[EmailAddress]
public string Email { get; set; } = string.Empty;
```

## AI-Assisted Development

### Using GitHub Copilot

This repository includes AI governance files in `.github/`:

- `system-instructions.md` - Core patterns and conventions
- `domain-services.md` - Service architecture and business rules
- `lint-rules.md` - Code quality standards
- `testing-guide.md` - Testing patterns and requirements
- `api-contracts.md` - API specifications

### Prompting Best Practices

**Good Prompts**:
```
"Generate a controller for loan management following the existing account controller pattern"

"Add unit tests for the TransactionService.TransferAsync method covering success and failure scenarios"

"Create a DTO for loan application with validation attributes"
```

**AI Will Know**:
- Naming conventions
- Code structure patterns
- Testing requirements
- Security patterns
- Error handling approach

### Verification Checklist

When using AI-generated code, verify:

- [ ] Follows naming conventions
- [ ] Has proper error handling
- [ ] Includes logging
- [ ] Has authorization checks
- [ ] Validates user ownership
- [ ] Includes tests
- [ ] Updates documentation

## Testing Requirements

### Coverage Requirements

- **Controllers**: 90%+
- **Services**: 85%+
- **Overall**: 80%+

### Test Types Required

For each new feature:

1. **Unit Tests** - Service logic
2. **Integration Tests** - API endpoints
3. **Edge Cases** - Boundary conditions

### Test Naming

```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedResult()
{
    // Arrange
    // Act
    // Assert
}
```

### Running Tests

```bash
# All tests
dotnet test

# With coverage
dotnet test /p:CollectCoverage=true

# Specific test
dotnet test --filter "FullyQualifiedName~TransactionServiceTests"
```

## Pull Request Process

### Before Submitting

1. **Code Quality**
   ```bash
   dotnet format
   dotnet build --configuration Release
   ```

2. **Tests Pass**
   ```bash
   dotnet test
   ```

3. **Coverage Check**
   ```bash
   dotnet test /p:CollectCoverage=true
   ```

4. **Documentation Updated**
   - Update README.md if adding features
   - Update API contracts if changing endpoints
   - Add XML comments for public APIs

### PR Title Format

```
[TYPE] Brief description

Examples:
[FEAT] Add loan management service
[FIX] Resolve race condition in transfers
[DOCS] Update authentication guide
```

### PR Description Template

```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Testing
- [ ] Unit tests added/updated
- [ ] Integration tests added/updated
- [ ] Manual testing completed

## Checklist
- [ ] Code follows style guidelines
- [ ] Self-review completed
- [ ] Comments added for complex logic
- [ ] Documentation updated
- [ ] No new warnings
- [ ] Tests pass locally
- [ ] Dependent changes merged
```

### Review Process

1. Automated checks must pass:
   - Build succeeds
   - Tests pass
   - Code formatted correctly
   - No linting errors

2. Code review by maintainer:
   - Logic correctness
   - Test coverage
   - Security considerations
   - Performance impact

3. Address feedback and update PR

4. Approval and merge

## Documentation

### What to Document

1. **Public APIs** - XML comments
   ```csharp
   /// <summary>
   /// Creates a new account for the user.
   /// </summary>
   /// <param name="userId">The user's unique identifier</param>
   /// <param name="request">Account creation details</param>
   /// <returns>The created account</returns>
   public async Task<AccountDto> CreateAccountAsync(Guid userId, CreateAccountRequest request)
   ```

2. **Complex Logic** - Inline comments
   ```csharp
   // Calculate opening balance by reversing transactions in the period
   var openingBalance = currentBalance - deposits + withdrawals;
   ```

3. **Business Rules** - Update domain-services.md

4. **API Changes** - Update api-contracts.md

### Documentation Standards

- Use clear, concise language
- Include examples where helpful
- Keep README.md up to date
- Document breaking changes

## Adding New Features

### Checklist for New Endpoints

1. **Model** (if needed)
   - [ ] Create entity class
   - [ ] Add to DbContext
   - [ ] Configure in OnModelCreating
   - [ ] Add migration (if applicable)

2. **DTOs**
   - [ ] Create Request DTO with validation
   - [ ] Create Response DTO
   - [ ] Add to existing DTO files

3. **Service**
   - [ ] Define interface
   - [ ] Implement service
   - [ ] Add business logic
   - [ ] Add logging
   - [ ] Register in Program.cs

4. **Controller**
   - [ ] Create controller
   - [ ] Add endpoints
   - [ ] Add authorization
   - [ ] Add error handling

5. **Tests**
   - [ ] Unit tests for service
   - [ ] Integration tests for controller
   - [ ] Edge case tests

6. **Documentation**
   - [ ] Update README.md
   - [ ] Update api-contracts.md
   - [ ] Add XML comments

### Example: Adding a Loan Feature

```csharp
// 1. Model
public class Loan
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Amount { get; set; }
    
    public LoanStatus Status { get; set; }
}

// 2. DTO
public class CreateLoanRequest
{
    [Required]
    [Range(1000, 1000000)]
    public decimal Amount { get; set; }
    
    [Required]
    [Range(1, 30)]
    public int TermInYears { get; set; }
}

// 3. Service Interface
public interface ILoanService
{
    Task<LoanDto> CreateLoanAsync(Guid userId, CreateLoanRequest request);
}

// 4. Service Implementation
public class LoanService : ILoanService
{
    private readonly BankingDbContext _context;
    private readonly ILogger<LoanService> _logger;
    
    public async Task<LoanDto> CreateLoanAsync(Guid userId, CreateLoanRequest request)
    {
        // Implementation
    }
}

// 5. Controller
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class LoansController : ControllerBase
{
    private readonly ILoanService _service;
    
    [HttpPost]
    public async Task<ActionResult<ApiResponse<LoanDto>>> CreateLoan([FromBody] CreateLoanRequest request)
    {
        // Implementation
    }
}

// 6. Register Service
builder.Services.AddScoped<ILoanService, LoanService>();
```

## Security Guidelines

### Critical Rules

1. **Never log sensitive data**
   ```csharp
   // ❌ WRONG
   _logger.LogInformation("Password: {Password}", password);
   
   // ✅ CORRECT
   _logger.LogInformation("User login attempt: {Email}", email);
   ```

2. **Always validate user ownership**
   ```csharp
   var account = await _context.Accounts
       .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId);
   
   if (account == null)
       throw new InvalidOperationException("Not found or unauthorized");
   ```

3. **Use parameterized queries** (EF Core does this automatically)

4. **Validate all input**
   ```csharp
   [Required]
   [Range(0.01, double.MaxValue)]
   public decimal Amount { get; set; }
   ```

## Performance Guidelines

1. **Use async/await consistently**
2. **Eager load related data when needed**
3. **Project only required fields**
4. **Add database indexes for frequently queried fields**

## Getting Help

- Review existing code for patterns
- Check `.github/` documentation files
- Ask in pull request comments
- Review test files for examples

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

## Questions?

Open an issue for:
- Feature requests
- Bug reports
- Documentation improvements
- General questions

Thank you for contributing to Banking Service!