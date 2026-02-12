# Code Quality and Linting Rules

This document defines code quality standards, linting rules, and best practices for the Banking Service project.

## Code Analysis Tools

### Enabled Analyzers
- **Microsoft.CodeAnalysis.NetAnalyzers** - Built-in .NET analyzers
- **StyleCop.Analyzers** - C# style and consistency rules
- **SonarAnalyzer.CSharp** - Code quality and security rules

### EditorConfig Settings

Create `.editorconfig` in root:

```ini
# EditorConfig is awesome: https://EditorConfig.org

root = true

# All files
[*]
charset = utf-8
insert_final_newline = true
trim_trailing_whitespace = true
indent_style = space

# Code files
[*.{cs,csx,vb,vbx}]
indent_size = 4
tab_width = 4

# XML project files
[*.{csproj,vbproj,vcxproj,vcxproj.filters,proj,projitems,shproj}]
indent_size = 2

# JSON files
[*.json]
indent_size = 2

# YAML files
[*.{yml,yaml}]
indent_size = 2

# Markdown files
[*.md]
trim_trailing_whitespace = false

# C# files
[*.cs]

# Organize usings
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = false

# this. preferences
dotnet_style_qualification_for_field = false:warning
dotnet_style_qualification_for_property = false:warning
dotnet_style_qualification_for_method = false:warning
dotnet_style_qualification_for_event = false:warning

# Language keywords vs BCL types preferences
dotnet_style_predefined_type_for_locals_parameters_members = true:warning
dotnet_style_predefined_type_for_member_access = true:warning

# Parentheses preferences
dotnet_style_parentheses_in_arithmetic_binary_operators = always_for_clarity:suggestion
dotnet_style_parentheses_in_relational_binary_operators = always_for_clarity:suggestion
dotnet_style_parentheses_in_other_binary_operators = always_for_clarity:suggestion
dotnet_style_parentheses_in_other_operators = never_if_unnecessary:suggestion

# Modifier preferences
dotnet_style_require_accessibility_modifiers = always:warning
dotnet_style_readonly_field = true:warning

# Expression-level preferences
dotnet_style_object_initializer = true:suggestion
dotnet_style_collection_initializer = true:suggestion
dotnet_style_explicit_tuple_names = true:warning
dotnet_style_prefer_inferred_tuple_names = true:suggestion
dotnet_style_prefer_inferred_anonymous_type_member_names = true:suggestion
dotnet_style_prefer_auto_properties = true:suggestion
dotnet_style_prefer_conditional_expression_over_assignment = true:suggestion
dotnet_style_prefer_conditional_expression_over_return = true:suggestion

# Null-checking preferences
dotnet_style_coalesce_expression = true:suggestion
dotnet_style_null_propagation = true:suggestion
dotnet_style_prefer_is_null_check_over_reference_equality_method = true:warning

# C# Code Style Rules

# var preferences
csharp_style_var_for_built_in_types = false:warning
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_style_var_elsewhere = false:warning

# Expression-bodied members
csharp_style_expression_bodied_methods = false:none
csharp_style_expression_bodied_constructors = false:none
csharp_style_expression_bodied_operators = false:none
csharp_style_expression_bodied_properties = true:suggestion
csharp_style_expression_bodied_indexers = true:suggestion
csharp_style_expression_bodied_accessors = true:suggestion

# Pattern matching preferences
csharp_style_pattern_matching_over_is_with_cast_check = true:suggestion
csharp_style_pattern_matching_over_as_with_null_check = true:suggestion

# Null-checking preferences
csharp_style_throw_expression = true:suggestion
csharp_style_conditional_delegate_call = true:suggestion

# Modifier preferences
csharp_preferred_modifier_order = public,private,protected,internal,static,extern,new,virtual,abstract,sealed,override,readonly,unsafe,volatile,async:suggestion

# Expression-level preferences
csharp_prefer_braces = true:warning
csharp_style_deconstructed_variable_declaration = true:suggestion
csharp_prefer_simple_default_expression = true:suggestion
csharp_style_pattern_local_over_anonymous_function = true:suggestion
csharp_style_inlined_variable_declaration = true:suggestion

# C# Formatting Rules

# New line preferences
csharp_new_line_before_open_brace = all
csharp_new_line_before_else = true
csharp_new_line_before_catch = true
csharp_new_line_before_finally = true
csharp_new_line_before_members_in_object_initializers = true
csharp_new_line_before_members_in_anonymous_types = true
csharp_new_line_between_query_expression_clauses = true

# Indentation preferences
csharp_indent_case_contents = true
csharp_indent_switch_labels = true
csharp_indent_labels = no_change

# Space preferences
csharp_space_after_cast = false
csharp_space_after_keywords_in_control_flow_statements = true
csharp_space_between_method_call_parameter_list_parentheses = false
csharp_space_between_method_declaration_parameter_list_parentheses = false
csharp_space_between_parentheses = false
csharp_space_before_colon_in_inheritance_clause = true
csharp_space_after_colon_in_inheritance_clause = true
csharp_space_around_binary_operators = before_and_after
csharp_space_between_method_declaration_empty_parameter_list_parentheses = false
csharp_space_between_method_call_name_and_opening_parenthesis = false
csharp_space_between_method_call_empty_parameter_list_parentheses = false

# Wrapping preferences
csharp_preserve_single_line_statements = false
csharp_preserve_single_line_blocks = true

# Naming Conventions

# Constant fields are PascalCase
dotnet_naming_rule.constant_fields_should_be_pascal_case.severity = warning
dotnet_naming_rule.constant_fields_should_be_pascal_case.symbols = constant_fields
dotnet_naming_rule.constant_fields_should_be_pascal_case.style = pascal_case_style

dotnet_naming_symbols.constant_fields.applicable_kinds = field
dotnet_naming_symbols.constant_fields.applicable_accessibilities = *
dotnet_naming_symbols.constant_fields.required_modifiers = const

# Private fields are _camelCase
dotnet_naming_rule.private_fields_should_be_camel_case_with_underscore.severity = warning
dotnet_naming_rule.private_fields_should_be_camel_case_with_underscore.symbols = private_fields
dotnet_naming_rule.private_fields_should_be_camel_case_with_underscore.style = camel_case_with_underscore_style

dotnet_naming_symbols.private_fields.applicable_kinds = field
dotnet_naming_symbols.private_fields.applicable_accessibilities = private

dotnet_naming_style.camel_case_with_underscore_style.capitalization = camel_case
dotnet_naming_style.camel_case_with_underscore_style.required_prefix = _

# Interfaces start with I
dotnet_naming_rule.interface_should_be_begins_with_i.severity = warning
dotnet_naming_rule.interface_should_be_begins_with_i.symbols = interface
dotnet_naming_rule.interface_should_be_begins_with_i.style = begins_with_i

dotnet_naming_symbols.interface.applicable_kinds = interface
dotnet_naming_symbols.interface.applicable_accessibilities = public, internal, private, protected, protected_internal, private_protected

dotnet_naming_style.begins_with_i.required_prefix = I
dotnet_naming_style.begins_with_i.capitalization = pascal_case

# Types are PascalCase
dotnet_naming_rule.types_should_be_pascal_case.severity = warning
dotnet_naming_rule.types_should_be_pascal_case.symbols = types
dotnet_naming_rule.types_should_be_pascal_case.style = pascal_case_style

dotnet_naming_symbols.types.applicable_kinds = class, struct, interface, enum
dotnet_naming_symbols.types.applicable_accessibilities = public, internal, private, protected, protected_internal, private_protected

dotnet_naming_style.pascal_case_style.capitalization = pascal_case

# Async methods end with Async
dotnet_naming_rule.async_methods_should_end_with_async.severity = warning
dotnet_naming_rule.async_methods_should_end_with_async.symbols = async_methods
dotnet_naming_rule.async_methods_should_end_with_async.style = end_with_async

dotnet_naming_symbols.async_methods.applicable_kinds = method
dotnet_naming_symbols.async_methods.applicable_accessibilities = *
dotnet_naming_symbols.async_methods.required_modifiers = async

dotnet_naming_style.end_with_async.required_suffix = Async
dotnet_naming_style.end_with_async.capitalization = pascal_case
```

## Code Quality Rules

### Critical Rules (ERROR level)

#### 1. Security Rules
```csharp
// ❌ WRONG - Never log sensitive data
_logger.LogInformation("User password: {Password}", password);

// ✅ CORRECT
_logger.LogInformation("User login attempt: {Email}", email);
```

```csharp
// ❌ WRONG - SQL injection vulnerability
var query = $"SELECT * FROM Users WHERE Email = '{email}'";

// ✅ CORRECT - Use EF Core parameterized queries
var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
```

#### 2. Null Reference Rules
```csharp
// ❌ WRONG - Potential null reference
public string GetUserName(User user)
{
    return user.FirstName + " " + user.LastName;
}

// ✅ CORRECT - Null checking
public string GetUserName(User? user)
{
    if (user == null)
        return "Unknown";
    return $"{user.FirstName} {user.LastName}";
}

// ✅ BETTER - Null-conditional operator
public string GetUserName(User? user)
{
    return user != null ? $"{user.FirstName} {user.LastName}" : "Unknown";
}
```

#### 3. Async/Await Rules
```csharp
// ❌ WRONG - Blocking async call
public void DoWork()
{
    var result = _service.GetDataAsync().Result; // Deadlock risk!
}

// ✅ CORRECT - Async all the way
public async Task DoWorkAsync()
{
    var result = await _service.GetDataAsync();
}
```

```csharp
// ❌ WRONG - Async void (except event handlers)
public async void ProcessData()
{
    await _service.SaveAsync();
}

// ✅ CORRECT - Return Task
public async Task ProcessDataAsync()
{
    await _service.SaveAsync();
}
```

#### 4. Disposal Rules
```csharp
// ❌ WRONG - Not disposing
public void UseResource()
{
    var stream = File.OpenRead("file.txt");
    // Use stream
}

// ✅ CORRECT - Using statement
public void UseResource()
{
    using var stream = File.OpenRead("file.txt");
    // Use stream
} // Automatically disposed
```

### Warning Level Rules

#### 1. Naming Conventions
```csharp
// ❌ WRONG
private BankingDbContext context;
public interface myService { }
public class my_class { }

// ✅ CORRECT
private readonly BankingDbContext _context;
public interface IMyService { }
public class MyClass { }
```

#### 2. Code Organization
```csharp
// ❌ WRONG - Multiple public classes in one file
public class UserService { }
public class AccountService { }

// ✅ CORRECT - One public class per file
// File: UserService.cs
public class UserService { }

// File: AccountService.cs
public class AccountService { }
```

#### 3. Exception Handling
```csharp
// ❌ WRONG - Catching generic exception without rethrowing
try
{
    await _service.ProcessAsync();
}
catch (Exception)
{
    return null; // Swallows exception!
}

// ✅ CORRECT - Specific exceptions or log and rethrow
try
{
    await _service.ProcessAsync();
}
catch (InvalidOperationException ex)
{
    _logger.LogError(ex, "Invalid operation");
    throw;
}
```

#### 4. String Formatting
```csharp
// ❌ WRONG
string message = "Hello " + firstName + " " + lastName;

// ✅ CORRECT - String interpolation
string message = $"Hello {firstName} {lastName}";

// ✅ CORRECT - For logging (structured logging)
_logger.LogInformation("User registered: {FirstName} {LastName}", firstName, lastName);
```

### Suggestion Level Rules

#### 1. var Usage
```csharp
// ✅ Use var when type is obvious
var account = new Account();
var users = await _context.Users.ToListAsync();

// ✅ Use explicit type when not obvious
string userName = GetUserName();
decimal balance = CalculateBalance();
```

#### 2. Expression Bodies
```csharp
// ✅ GOOD - For simple properties
public string FullName => $"{FirstName} {LastName}";

// ✅ GOOD - For simple methods
public bool IsActive() => Status == AccountStatus.Active;

// ❌ AVOID - For complex logic
public decimal CalculateInterest() => 
    Balance * InterestRate * (DateTime.UtcNow - CreatedAt).Days / 365;

// ✅ BETTER - Use block body for complex logic
public decimal CalculateInterest()
{
    var days = (DateTime.UtcNow - CreatedAt).Days;
    return Balance * InterestRate * days / 365;
}
```

#### 3. Object Initialization
```csharp
// ❌ LESS PREFERRED
var user = new User();
user.FirstName = "John";
user.LastName = "Doe";
user.Email = "john@example.com";

// ✅ PREFERRED - Object initializer
var user = new User
{
    FirstName = "John",
    LastName = "Doe",
    Email = "john@example.com"
};
```

## Project-Specific Rules

### 1. Controller Rules

```csharp
// ✅ REQUIRED - All controllers must:
[Authorize] // 1. Be authorized (unless public endpoint)
[ApiController] // 2. Have ApiController attribute
[Route("api/[controller]")] // 3. Have route attribute
public class AccountsController : ControllerBase // 4. Inherit from ControllerBase
{
    // 5. Use dependency injection
    private readonly IAccountService _service;
    private readonly ILogger<AccountsController> _logger;

    // 6. Constructor injection
    public AccountsController(IAccountService service, ILogger<AccountsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // 7. All methods must be async
    // 8. Return ActionResult<ApiResponse<T>>
    // 9. Use try-catch with proper error handling
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<AccountDto>>>> GetAccounts()
    {
        try
        {
            var userId = GetUserId();
            var result = await _service.GetUserAccountsAsync(userId);
            return Ok(new ApiResponse<List<AccountDto>>
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting accounts");
            return BadRequest(new ApiResponse<List<AccountDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    // 10. Helper methods should be private
    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim!);
    }
}
```

### 2. Service Rules

```csharp
// ✅ REQUIRED - All services must:
public class AccountService : IAccountService // 1. Implement interface
{
    // 2. Readonly fields
    private readonly BankingDbContext _context;
    private readonly ILogger<AccountService> _logger;

    // 3. Constructor injection
    public AccountService(BankingDbContext context, ILogger<AccountService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // 4. All methods async
    // 5. Log important operations
    // 6. Validate user ownership
    public async Task<AccountDto> CreateAccountAsync(Guid userId, CreateAccountRequest request)
    {
        // 7. Log start of operation
        _logger.LogInformation("Creating account for user: {UserId}", userId);

        // 8. Validate user exists
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new InvalidOperationException("User not found");

        // 9. Business logic
        var account = new Account
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AccountNumber = GenerateAccountNumber(),
            Type = request.Type,
            Balance = request.InitialDeposit
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        // 10. Log completion
        _logger.LogInformation("Account created: {AccountId}", account.Id);

        // 11. Map to DTO
        return MapToAccountDto(account);
    }

    // 12. Private helper methods
    private string GenerateAccountNumber() => DateTime.UtcNow.Ticks.ToString()[^10..];
    
    private AccountDto MapToAccountDto(Account account) => new()
    {
        Id = account.Id,
        AccountNumber = account.AccountNumber,
        Balance = account.Balance
    };
}
```

### 3. Model Rules

```csharp
public class Account
{
    // ✅ REQUIRED
    [Key] // 1. Primary key attribute
    public Guid Id { get; set; }
    
    [Required] // 2. Required fields marked
    public Guid UserId { get; set; }
    
    [ForeignKey(nameof(UserId))] // 3. Foreign key attribute
    public virtual User User { get; set; } = null!; // 4. Virtual navigation property
    
    [Required]
    [MaxLength(20)] // 5. String length limits
    public string AccountNumber { get; set; } = string.Empty; // 6. Default value
    
    [Column(TypeName = "decimal(18, 2)")] // 7. Decimal precision
    public decimal Balance { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // 8. Default timestamps
    
    // 9. Collections initialized
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
```

### 4. DTO Rules

```csharp
// ✅ Request DTOs
public class CreateAccountRequest
{
    [Required] // 1. Validation attributes
    public AccountType Type { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue)] // 2. Range validation
    public decimal InitialDeposit { get; set; }
    
    [MaxLength(500)] // 3. String length
    public string? Description { get; set; } // 4. Nullable for optional
}

// ✅ Response DTOs
public class AccountDto
{
    public Guid Id { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public AccountStatus Status { get; set; }
    // Never include sensitive data like passwords, CVV
}
```

## Automated Checks

### Pre-commit Checks
```bash
# Run before committing
dotnet format
dotnet build
dotnet test
```

### CI/CD Checks
```yaml
# In GitHub Actions workflow
- name: Check formatting
  run: dotnet format --verify-no-changes

- name: Build
  run: dotnet build --configuration Release

- name: Run tests
  run: dotnet test --configuration Release --no-build

- name: Code analysis
  run: dotnet build /p:RunAnalyzers=true /p:TreatWarningsAsErrors=true
```

## Code Metrics Targets

- **Cyclomatic Complexity**: < 10 per method
- **Lines per Method**: < 50
- **Class Coupling**: < 10
- **Maintainability Index**: > 75
- **Test Coverage**: > 80%

## Documentation Requirements

```csharp
/// <summary>
/// Creates a new account for the specified user.
/// </summary>
/// <param name="userId">The unique identifier of the user</param>
/// <param name="request">Account creation details</param>
/// <returns>The created account details</returns>
/// <exception cref="InvalidOperationException">Thrown when user not found</exception>
public async Task<AccountDto> CreateAccountAsync(Guid userId, CreateAccountRequest request)
{
    // Implementation
}
```

## Suppressing Warnings

Only when absolutely necessary:
```csharp
#pragma warning disable CA1062 // Validate arguments of public methods
public void Method(object param)
{
    // Code that can't easily validate param
}
#pragma warning restore CA1062
```

## Tools for Enforcement

1. **dotnet format** - Code formatting
2. **Roslyn Analyzers** - Code analysis
3. **SonarQube** - Code quality (optional)
4. **ReSharper** - Code analysis and refactoring (optional)

Run code formatting:
```bash
dotnet format
```

Run with analyzer:
```bash
dotnet build /p:RunAnalyzers=true /p:TreatWarningsAsErrors=true
```