# GitHub Copilot Instructions for Banking Service

## Code Generation Guidelines

### General Principles
- Follow clean architecture principles (domain, application, infrastructure layers)
- All code must handle errors gracefully
- Log all operations with appropriate log levels
- Include input validation for all public methods
- Write self-documenting code with clear naming

### Security Requirements (CRITICAL)
- NEVER hardcode credentials, API keys, or secrets
- Always validate and sanitize user inputs
- Use parameterized queries for database operations (prevent SQL injection)
- Implement rate limiting on authentication endpoints
- Encrypt sensitive data at rest and in transit
- Follow OWASP Top 10 security practices
- All financial operations must be auditable

### Banking Domain Rules
- All monetary amounts use decimal types (never float)
- Currency must always be specified
- Transactions must be atomic and idempotent
- Balance calculations must be precise
- All operations require audit logging
- Failed transactions must rollback completely

### Code Style
- Max method length: 20 lines
- Max class length: 200 lines
- Required: Documentation comments for public APIs
- Use dependency injection
- Prefer composition over inheritance

### Testing Requirements
- Minimum 80% code coverage
- All public methods must have unit tests
- Critical paths need integration tests
- Use test data builders for complex objects
- Mock external dependencies

### API Design
- RESTful conventions
- Consistent error response format
- API versioning in URL (/api/v1/)
- OpenAPI/Swagger documentation required
- Pagination for list endpoints

### Naming Conventions
- Classes: PascalCase (UserAccount, TransferService)
- Methods: camelCase (processTransfer, validateAccount)
- Constants: UPPER_SNAKE_CASE (MAX_TRANSFER_AMOUNT)

### Forbidden Patterns
- ❌ Catching generic exceptions without handling
- ❌ Console.log in production code
- ❌ Direct database queries in controllers
- ❌ Business logic in controllers or DTOs
- ❌ Mutable global state
- ❌ Using deprecated libraries or frameworks