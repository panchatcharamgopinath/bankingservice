# Banking Service REST API

A production-grade banking service built with C# and .NET 8, featuring comprehensive authentication, account management, transactions, cards, and statements.

## Features

- **Authentication**: JWT-based authentication with secure password hashing
- **Account Management**: Support for Checking, Savings, and Investment accounts
- **Transactions**: Deposits, withdrawals, and transfers with transaction history
- **Cards**: Debit and credit card management with daily limits
- **Statements**: Generate account statements for specified date ranges
- **Security**: Role-based access control, input validation, and secure password handling
- **Logging**: Structured logging with Serilog
- **Health Checks**: Endpoint monitoring and database connectivity checks
- **Containerization**: Docker support with multi-stage builds

## Technology Stack

- .NET 8.0
- Entity Framework Core with SQLite
- JWT Authentication
- Serilog for logging
- Swagger/OpenAPI
- xUnit for testing
- Docker & Docker Compose

## Project Structure

```
BankingService/
├── BankingService/
│   ├── Controllers/         # API controllers
│   ├── Services/           # Business logic services
│   ├── Models/             # Data models
│   ├── DTOs/               # Data transfer objects
│   ├── Data/               # Database context
│   ├── Middleware/         # Custom middleware
│   ├── Program.cs          # Application entry point
│   └── appsettings.json    # Configuration
├── BankingService.Tests/
│   └── *Tests.cs           # Unit and integration tests
├── Dockerfile
├── docker-compose.yml
└── .env.example
```

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- Docker (optional, for containerized deployment)

### Local Development

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd BankingService
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

4. **Access Swagger UI**
   Open your browser and navigate to: `http://localhost:5000/swagger`

### Docker Deployment

1. **Using Docker Compose**
   ```bash
   docker-compose up -d
   ```

2. **Access the API**
   The service will be available at: `http://localhost:5000`

3. **View logs**
   ```bash
   docker-compose logs -f banking-service
   ```

4. **Stop the service**
   ```bash
   docker-compose down
   ```

### Running Tests

```bash
cd BankingService.Tests
dotnet test
```

For test coverage:
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## API Endpoints

### Authentication
- `POST /api/auth/signup` - Register a new user
- `POST /api/auth/login` - Login and receive JWT token

### Accounts
- `POST /api/accounts` - Create a new account
- `GET /api/accounts` - Get all user accounts
- `GET /api/accounts/{id}` - Get specific account

### Transactions
- `POST /api/transactions/deposit` - Deposit money
- `POST /api/transactions/withdraw` - Withdraw money
- `POST /api/transactions/transfer` - Transfer between accounts
- `GET /api/transactions/account/{accountId}` - Get account transactions

### Cards
- `POST /api/cards` - Create a new card
- `GET /api/cards/account/{accountId}` - Get account cards
- `PUT /api/cards/{cardId}/block` - Block a card

### Statements
- `POST /api/statements/generate` - Generate account statement

### Health & Monitoring
- `GET /health` - Health check endpoint
- `GET /ready` - Readiness probe

## Configuration

### Environment Variables

Create a `.env` file based on `.env.example`:

```bash
JWT_KEY=YourSuperSecretKeyThatIsAtLeast32CharactersLongForHS256
JWT_ISSUER=BankingService
JWT_AUDIENCE=BankingServiceClient
ASPNETCORE_ENVIRONMENT=Development
```

### appsettings.json

Key configuration sections:
- `ConnectionStrings`: Database connection
- `Jwt`: JWT token settings
- `Serilog`: Logging configuration

## Authentication Flow

1. **Sign Up**: Create a new user account
   ```json
   POST /api/auth/signup
   {
     "email": "user@example.com",
     "password": "SecurePass123",
     "firstName": "John",
     "lastName": "Doe"
   }
   ```

2. **Login**: Authenticate and receive JWT token
   ```json
   POST /api/auth/login
   {
     "email": "user@example.com",
     "password": "SecurePass123"
   }
   ```

3. **Use Token**: Include the token in subsequent requests
   ```
   Authorization: Bearer <your-jwt-token>
   ```

## Sample Usage

### 1. Create Account
```bash
curl -X POST http://localhost:5000/api/accounts \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "type": 0,
    "currency": "USD",
    "initialDeposit": 1000
  }'
```

### 2. Transfer Money
```bash
curl -X POST http://localhost:5000/api/transactions/transfer \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "fromAccountId": "<source-account-id>",
    "toAccountNumber": "1234567890",
    "amount": 100,
    "description": "Payment"
  }'
```

## Logging

Logs are written to:
- Console (structured format)
- Files in `logs/` directory (rolling daily)

Log levels: Trace, Debug, Information, Warning, Error, Fatal

## Health Checks

The service includes comprehensive health checks:

- **Database connectivity**: Verifies EF Core DbContext is accessible
- **Service readiness**: Checks if the service is ready to accept requests

Access health endpoints:
- `GET /health` - Overall health status
- `GET /ready` - Readiness status

## Security Features

- JWT-based authentication
- Password hashing with SHA-256
- Input validation on all endpoints
- SQL injection protection via EF Core
- CORS support (configurable)
- Rate limiting ready (can be added)

## Error Handling

The service implements global error handling middleware that:
- Catches unhandled exceptions
- Returns consistent error responses
- Logs errors with context
- Provides appropriate HTTP status codes

## Performance Considerations

- Database connection pooling
- Async/await pattern throughout
- Transaction management for critical operations
- Indexed database columns for performance
- Docker multi-stage builds for smaller images

## Production Checklist

- [ ] Change JWT secret key
- [ ] Configure HTTPS
- [ ] Set up persistent database storage
- [ ] Configure CORS for specific origins
- [ ] Enable rate limiting
- [ ] Set up monitoring and alerting
- [ ] Configure log aggregation
- [ ] Set up automated backups
- [ ] Review and harden security settings
- [ ] Perform load testing

## Troubleshooting

### Database Issues
If you encounter database errors:
```bash
# Delete the database and let it recreate
rm banking.db
dotnet run
```

### Docker Issues
```bash
# Rebuild containers
docker-compose down
docker-compose build --no-cache
docker-compose up -d
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Write tests for new features
4. Ensure all tests pass
5. Submit a pull request

## License

This project is licensed under the MIT License.

## Support

For issues and questions:
- Create an issue in the repository
- Check existing documentation
- Review test cases for usage examples