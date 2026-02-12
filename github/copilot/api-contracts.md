# API Contracts and Examples

Complete API contract definitions with request/response examples for AI-assisted development.

## Base URL
```
http://localhost:5000/api
```

## Standard Response Wrapper

All API responses use this wrapper:

```json
{
  "success": true,
  "data": { /* response data */ },
  "message": "Operation completed successfully",
  "errors": null
}
```

Error response:
```json
{
  "success": false,
  "data": null,
  "message": "Error message",
  "errors": ["Validation error 1", "Validation error 2"]
}
```

## Authentication Endpoints

### Sign Up

**Endpoint**: `POST /api/auth/signup`

**Request**:
```json
{
  "email": "user@example.com",
  "password": "SecurePass123",
  "firstName": "John",
  "lastName": "Doe"
}
```

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAt": "2026-01-15T10:30:00Z",
    "user": {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "email": "user@example.com",
      "firstName": "John",
      "lastName": "Doe",
      "createdAt": "2026-01-14T10:30:00Z"
    }
  },
  "message": "User registered successfully"
}
```

**Error Responses**:
- 400 Bad Request: Email already exists
- 400 Bad Request: Validation errors

**Validation Rules**:
- Email: Required, valid email format
- Password: Required, minimum 8 characters
- FirstName: Required
- LastName: Required

---

### Login

**Endpoint**: `POST /api/auth/login`

**Request**:
```json
{
  "email": "user@example.com",
  "password": "SecurePass123"
}
```

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAt": "2026-01-15T10:30:00Z",
    "user": {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "email": "user@example.com",
      "firstName": "John",
      "lastName": "Doe",
      "createdAt": "2026-01-14T10:30:00Z"
    }
  },
  "message": "Login successful"
}
```

**Error Responses**:
- 401 Unauthorized: Invalid credentials

---

## Account Endpoints

**Authentication Required**: All account endpoints require Bearer token

**Header**:
```
Authorization: Bearer {token}
```

### Create Account

**Endpoint**: `POST /api/accounts`

**Request**:
```json
{
  "type": 0,
  "currency": "USD",
  "initialDeposit": 1000.00
}
```

**Account Types**:
- 0: Checking
- 1: Savings
- 2: Investment

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "id": "7d9e8f2a-1234-5678-90ab-cdef12345678",
    "accountNumber": "1234567890",
    "type": 0,
    "balance": 1000.00,
    "currency": "USD",
    "status": 0,
    "createdAt": "2026-01-14T10:30:00Z"
  },
  "message": "Account created successfully"
}
```

**Account Statuses**:
- 0: Active
- 1: Frozen
- 2: Closed

**Validation Rules**:
- Type: Required, valid enum value
- Currency: Required, max 3 characters
- InitialDeposit: Range 0.01 to max value

---

### Get All User Accounts

**Endpoint**: `GET /api/accounts`

**Response** (200 OK):
```json
{
  "success": true,
  "data": [
    {
      "id": "7d9e8f2a-1234-5678-90ab-cdef12345678",
      "accountNumber": "1234567890",
      "type": 0,
      "balance": 1500.00,
      "currency": "USD",
      "status": 0,
      "createdAt": "2026-01-14T10:30:00Z"
    },
    {
      "id": "8e0f9b3c-2345-6789-01bc-def234567890",
      "accountNumber": "9876543210",
      "type": 1,
      "balance": 5000.00,
      "currency": "USD",
      "status": 0,
      "createdAt": "2026-01-14T11:00:00Z"
    }
  ],
  "message": null
}
```

---

### Get Account by ID

**Endpoint**: `GET /api/accounts/{accountId}`

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "id": "7d9e8f2a-1234-5678-90ab-cdef12345678",
    "accountNumber": "1234567890",
    "type": 0,
    "balance": 1500.00,
    "currency": "USD",
    "status": 0,
    "createdAt": "2026-01-14T10:30:00Z"
  },
  "message": null
}
```

**Error Responses**:
- 404 Not Found: Account not found or unauthorized

---

## Transaction Endpoints

### Transfer Money

**Endpoint**: `POST /api/transactions/transfer`

**Request**:
```json
{
  "fromAccountId": "7d9e8f2a-1234-5678-90ab-cdef12345678",
  "toAccountNumber": "9876543210",
  "amount": 500.00,
  "description": "Rent payment"
}
```

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "id": "9f1a2b3c-3456-7890-12cd-ef3456789012",
    "transactionNumber": "TXN20260114103000001234",
    "fromAccountNumber": "1234567890",
    "toAccountNumber": "9876543210",
    "amount": 500.00,
    "type": 2,
    "status": 1,
    "description": "Rent payment",
    "createdAt": "2026-01-14T10:30:00Z",
    "completedAt": "2026-01-14T10:30:01Z"
  },
  "message": "Transfer completed successfully"
}
```

**Transaction Types**:
- 0: Deposit
- 1: Withdrawal
- 2: Transfer
- 3: Payment
- 4: Fee

**Transaction Statuses**:
- 0: Pending
- 1: Completed
- 2: Failed
- 3: Cancelled

**Error Responses**:
- 400 Bad Request: Insufficient funds
- 400 Bad Request: Account not found
- 400 Bad Request: Invalid amount

**Validation Rules**:
- FromAccountId: Required, valid GUID
- ToAccountNumber: Required, existing account
- Amount: Required, range 0.01 to max value
- Description: Optional, max 500 characters

---

### Deposit Money

**Endpoint**: `POST /api/transactions/deposit`

**Request**:
```json
{
  "accountId": "7d9e8f2a-1234-5678-90ab-cdef12345678",
  "amount": 1000.00,
  "description": "Salary deposit"
}
```

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "id": "a02b3c4d-4567-8901-23de-f45678901234",
    "transactionNumber": "TXN20260114103500001235",
    "fromAccountNumber": null,
    "toAccountNumber": "1234567890",
    "amount": 1000.00,
    "type": 0,
    "status": 1,
    "description": "Salary deposit",
    "createdAt": "2026-01-14T10:35:00Z",
    "completedAt": "2026-01-14T10:35:01Z"
  },
  "message": "Deposit completed successfully"
}
```

---

### Withdraw Money

**Endpoint**: `POST /api/transactions/withdraw`

**Request**:
```json
{
  "accountId": "7d9e8f2a-1234-5678-90ab-cdef12345678",
  "amount": 200.00,
  "description": "ATM withdrawal"
}
```

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "id": "b13c4d5e-5678-9012-34ef-56789012345",
    "transactionNumber": "TXN20260114104000001236",
    "fromAccountNumber": "1234567890",
    "toAccountNumber": null,
    "amount": 200.00,
    "type": 1,
    "status": 1,
    "description": "ATM withdrawal",
    "createdAt": "2026-01-14T10:40:00Z",
    "completedAt": "2026-01-14T10:40:01Z"
  },
  "message": "Withdrawal completed successfully"
}
```

**Error Responses**:
- 400 Bad Request: Insufficient funds

---

### Get Account Transactions

**Endpoint**: `GET /api/transactions/account/{accountId}`

**Response** (200 OK):
```json
{
  "success": true,
  "data": [
    {
      "id": "9f1a2b3c-3456-7890-12cd-ef3456789012",
      "transactionNumber": "TXN20260114103000001234",
      "fromAccountNumber": "1234567890",
      "toAccountNumber": "9876543210",
      "amount": 500.00,
      "type": 2,
      "status": 1,
      "description": "Rent payment",
      "createdAt": "2026-01-14T10:30:00Z",
      "completedAt": "2026-01-14T10:30:01Z"
    },
    {
      "id": "a02b3c4d-4567-8901-23de-f45678901234",
      "transactionNumber": "TXN20260114103500001235",
      "fromAccountNumber": null,
      "toAccountNumber": "1234567890",
      "amount": 1000.00,
      "type": 0,
      "status": 1,
      "description": "Salary deposit",
      "createdAt": "2026-01-14T10:35:00Z",
      "completedAt": "2026-01-14T10:35:01Z"
    }
  ],
  "message": null
}
```

---

## Card Endpoints

### Create Card

**Endpoint**: `POST /api/cards`

**Request**:
```json
{
  "accountId": "7d9e8f2a-1234-5678-90ab-cdef12345678",
  "type": 0,
  "dailyLimit": 2000.00
}
```

**Card Types**:
- 0: Debit
- 1: Credit

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "id": "c24d5e6f-6789-0123-45f0-67890123456",
    "cardNumber": "****-****-****-5678",
    "cardHolderName": "John Doe",
    "type": 0,
    "expiryDate": "2029-01-14T00:00:00Z",
    "dailyLimit": 2000.00,
    "status": 0,
    "createdAt": "2026-01-14T10:45:00Z"
  },
  "message": "Card created successfully"
}
```

**Card Statuses**:
- 0: Active
- 1: Blocked
- 2: Expired
- 3: Cancelled

**Note**: Card number is masked. Full number only shown at creation time (in production, even that would be masked).

**Validation Rules**:
- AccountId: Required, valid GUID
- Type: Required, valid enum value
- DailyLimit: Range 100 to 50000

---

### Get Account Cards

**Endpoint**: `GET /api/cards/account/{accountId}`

**Response** (200 OK):
```json
{
  "success": true,
  "data": [
    {
      "id": "c24d5e6f-6789-0123-45f0-67890123456",
      "cardNumber": "****-****-****-5678",
      "cardHolderName": "John Doe",
      "type": 0,
      "expiryDate": "2029-01-14T00:00:00Z",
      "dailyLimit": 2000.00,
      "status": 0,
      "createdAt": "2026-01-14T10:45:00Z"
    }
  ],
  "message": null
}
```

---

### Block Card

**Endpoint**: `PUT /api/cards/{cardId}/block`

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "id": "c24d5e6f-6789-0123-45f0-67890123456",
    "cardNumber": "****-****-****-5678",
    "cardHolderName": "John Doe",
    "type": 0,
    "expiryDate": "2029-01-14T00:00:00Z",
    "dailyLimit": 2000.00,
    "status": 1,
    "createdAt": "2026-01-14T10:45:00Z"
  },
  "message": "Card blocked successfully"
}
```

**Error Responses**:
- 404 Not Found: Card not found

---

## Statement Endpoints

### Generate Statement

**Endpoint**: `POST /api/statements/generate`

**Request**:
```json
{
  "accountId": "7d9e8f2a-1234-5678-90ab-cdef12345678",
  "startDate": "2026-01-01T00:00:00Z",
  "endDate": "2026-01-31T23:59:59Z"
}
```

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "account": {
      "id": "7d9e8f2a-1234-5678-90ab-cdef12345678",
      "accountNumber": "1234567890",
      "type": 0,
      "balance": 2300.00,
      "currency": "USD",
      "status": 0,
      "createdAt": "2026-01-14T10:30:00Z"
    },
    "startDate": "2026-01-01T00:00:00Z",
    "endDate": "2026-01-31T23:59:59Z",
    "openingBalance": 1000.00,
    "closingBalance": 2300.00,
    "totalDeposits": 2000.00,
    "totalWithdrawals": 700.00,
    "transactions": [
      {
        "id": "9f1a2b3c-3456-7890-12cd-ef3456789012",
        "transactionNumber": "TXN20260114103000001234",
        "fromAccountNumber": "1234567890",
        "toAccountNumber": "9876543210",
        "amount": 500.00,
        "type": 2,
        "status": 1,
        "description": "Rent payment",
        "createdAt": "2026-01-14T10:30:00Z",
        "completedAt": "2026-01-14T10:30:01Z"
      },
      {
        "id": "a02b3c4d-4567-8901-23de-f45678901234",
        "transactionNumber": "TXN20260114103500001235",
        "fromAccountNumber": null,
        "toAccountNumber": "1234567890",
        "amount": 1000.00,
        "type": 0,
        "status": 1,
        "description": "Salary deposit",
        "createdAt": "2026-01-14T10:35:00Z",
        "completedAt": "2026-01-14T10:35:01Z"
      }
    ]
  },
  "message": "Statement generated successfully"
}
```

**Validation Rules**:
- AccountId: Required, valid GUID
- StartDate: Required
- EndDate: Required, must be after StartDate

---

## Health Check Endpoints

### Health Check

**Endpoint**: `GET /health`

**Response** (200 OK):
```json
{
  "status": "Healthy"
}
```

---

### Readiness Check

**Endpoint**: `GET /ready`

**Response** (200 OK):
```json
{
  "status": "Healthy"
}
```

---

## Error Response Formats

### Validation Error (400)
```json
{
  "success": false,
  "data": null,
  "message": "Validation failed",
  "errors": [
    "Email is required",
    "Password must be at least 8 characters"
  ]
}
```

### Unauthorized (401)
```json
{
  "success": false,
  "data": null,
  "message": "Invalid credentials",
  "errors": null
}
```

### Not Found (404)
```json
{
  "success": false,
  "data": null,
  "message": "Resource not found",
  "errors": null
}
```

### Internal Server Error (500)
```json
{
  "success": false,
  "data": null,
  "message": "An internal error occurred",
  "errors": null
}
```

---

## Common Patterns

### Pagination (Future Enhancement)
```json
{
  "success": true,
  "data": {
    "items": [ /* array of items */ ],
    "pageNumber": 1,
    "pageSize": 20,
    "totalPages": 5,
    "totalCount": 100
  },
  "message": null
}
```

### Filtering (Future Enhancement)
```
GET /api/transactions/account/{accountId}?startDate=2026-01-01&endDate=2026-01-31&type=Transfer
```

---

## Testing Examples with cURL

### Sign Up
```bash
curl -X POST http://localhost:5000/api/auth/signup \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "SecurePass123",
    "firstName": "John",
    "lastName": "Doe"
  }'
```

### Login
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "SecurePass123"
  }'
```

### Create Account (with token)
```bash
curl -X POST http://localhost:5000/api/accounts \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -d '{
    "type": 0,
    "currency": "USD",
    "initialDeposit": 1000
  }'
```

### Transfer Money
```bash
curl -X POST http://localhost:5000/api/transactions/transfer \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -d '{
    "fromAccountId": "7d9e8f2a-1234-5678-90ab-cdef12345678",
    "toAccountNumber": "9876543210",
    "amount": 500,
    "description": "Rent payment"
  }'
```

---

## API Versioning (Future)

When implementing API versioning:
- URL versioning: `/api/v2/accounts`
- Header versioning: `Accept: application/vnd.banking.v2+json`
- Query parameter: `/api/accounts?api-version=2.0`

---

## Rate Limiting (Future)

Response headers:
```
X-Rate-Limit-Limit: 1000
X-Rate-Limit-Remaining: 999
X-Rate-Limit-Reset: 1610000000
```

Rate limit exceeded (429):
```json
{
  "success": false,
  "data": null,
  "message": "Rate limit exceeded. Try again in 60 seconds.",
  "errors": null
}
```