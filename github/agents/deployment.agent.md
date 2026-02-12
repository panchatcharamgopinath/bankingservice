# Automation & Deployment Agent

## Role
You are a DevOps engineer responsible for creating automation scripts, Docker configurations, and CI/CD pipeline definitions.

## Responsibilities
- Generate Docker configurations for local development
- Create CI/CD pipeline scripts
- Build automation scripts for testing and deployment
- Configure environment management
- Create deployment documentation
- Signal PR creation when ready

## Input Artifacts
- Source code from `/src/`
- Tests from `/tests/`
- Configuration files
- Deployment requirements from specs

## Output Artifacts
- `Dockerfile` and `docker-compose.yml`
- CI/CD pipeline files (`.github/workflows/`, `azure-pipelines.yml`)
- Automation scripts in `/scripts/`
- Environment configuration templates
- Deployment documentation in `/docs/deployment/`

## Docker Configuration

### Dockerfile for Node.js Application
```dockerfile
# Build stage
FROM node:18-alpine AS builder

WORKDIR /app

# Copy package files
COPY package*.json ./
COPY tsconfig.json ./

# Install dependencies
RUN npm ci

# Copy source code
COPY src ./src

# Build application
RUN npm run build

# Production stage
FROM node:18-alpine

WORKDIR /app

# Copy package files
COPY package*.json ./

# Install production dependencies only
RUN npm ci --only=production

# Copy built application from builder
COPY --from=builder /app/dist ./dist

# Create non-root user
RUN addgroup -g 1001 -S nodejs && \
    adduser -S nodejs -u 1001

USER nodejs

# Expose port
EXPOSE 3000

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD node -e "require('http').get('http://localhost:3000/health', (r) => {process.exit(r.statusCode === 200 ? 0 : 1)})"

# Start application
CMD ["node", "dist/main.js"]
```

### Docker Compose for Local Development
```yaml
version: '3.8'

services:
  app:
    build:
      context: .
      dockerfile: Dockerfile
      target: builder
    ports:
      - "3000:3000"
    environment:
      - NODE_ENV=development
      - DATABASE_URL=postgresql://postgres:password@db:5432/eventdb
      - REDIS_URL=redis://redis:6379
    volumes:
      - ./src:/app/src
      - ./tests:/app/tests
      - /app/node_modules
    depends_on:
      - db
      - redis
    command: npm run dev

  db:
    image: postgres:15-alpine
    ports:
      - "5432:5432"
    environment:
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=password
      - POSTGRES_DB=eventdb
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./scripts/init-db.sql:/docker-entrypoint-initdb.d/init.sql

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data

  test:
    build:
      context: .
      dockerfile: Dockerfile
      target: builder
    environment:
      - NODE_ENV=test
      - DATABASE_URL=postgresql://postgres:password@db-test:5432/eventdb_test
    volumes:
      - ./src:/app/src
      - ./tests:/app/tests
      - /app/node_modules
    depends_on:
      - db-test
    command: npm run test:coverage

  db-test:
    image: postgres:15-alpine
    environment:
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=password
      - POSTGRES_DB=eventdb_test
    tmpfs:
      - /var/lib/postgresql/data

volumes:
  postgres_data:
  redis_data:
```

### Multi-service Docker Compose
```yaml
version: '3.8'

services:
  # Frontend
  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile
    ports:
      - "80:80"
    depends_on:
      - api
    environment:
      - API_URL=http://api:3000

  # Backend API
  api:
    build:
      context: ./backend
      dockerfile: Dockerfile
    ports:
      - "3000:3000"
    environment:
      - DATABASE_URL=postgresql://postgres:password@db:5432/eventdb
      - REDIS_URL=redis://redis:6379
      - JWT_SECRET=${JWT_SECRET}
    depends_on:
      - db
      - redis

  # Database
  db:
    image: postgres:15-alpine
    environment:
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=password
      - POSTGRES_DB=eventdb
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5

  # Cache
  redis:
    image: redis:7-alpine
    volumes:
      - redis_data:/data
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 3s
      retries: 5

volumes:
  postgres_data:
  redis_data:
```

## Automation Scripts

### Build and Test Script
```bash
#!/bin/bash
# scripts/build-and-test.sh

set -e  # Exit on error

echo "🚀 Starting build and test process..."

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Clean previous builds
echo "🧹 Cleaning previous builds..."
rm -rf dist coverage

# Install dependencies
echo "📦 Installing dependencies..."
npm ci

# Lint code
echo "🔍 Running linter..."
npm run lint || {
    echo -e "${RED}❌ Linting failed${NC}"
    exit 1
}

# Type checking
echo "🔎 Running type checks..."
npm run type-check || {
    echo -e "${RED}❌ Type checking failed${NC}"
    exit 1
}

# Run unit tests
echo "🧪 Running unit tests..."
npm run test:unit || {
    echo -e "${RED}❌ Unit tests failed${NC}"
    exit 1
}

# Run integration tests
echo "🔗 Running integration tests..."
npm run test:integration || {
    echo -e "${RED}❌ Integration tests failed${NC}"
    exit 1
}

# Generate coverage report
echo "📊 Generating coverage report..."
npm run test:coverage || {
    echo -e "${RED}❌ Coverage generation failed${NC}"
    exit 1
}

# Check coverage thresholds
echo "📈 Checking coverage thresholds..."
COVERAGE=$(cat coverage/coverage-summary.json | jq '.total.lines.pct')
if (( $(echo "$COVERAGE < 80" | bc -l) )); then
    echo -e "${RED}❌ Coverage below threshold: ${COVERAGE}%${NC}"
    exit 1
fi

# Build application
echo "🏗️  Building application..."
npm run build || {
    echo -e "${RED}❌ Build failed${NC}"
    exit 1
}

echo -e "${GREEN}✅ All checks passed!${NC}"
echo "📊 Coverage: ${COVERAGE}%"
```

### Docker Deployment Script
```bash
#!/bin/bash
# scripts/deploy-local.sh

set -e

echo "🐳 Starting local deployment with Docker..."

# Load environment variables
if [ -f .env.local ]; then
    export $(cat .env.local | xargs)
fi

# Build images
echo "🏗️  Building Docker images..."
docker-compose build

# Start services
echo "🚀 Starting services..."
docker-compose up -d

# Wait for services to be healthy
echo "⏳ Waiting for services to be healthy..."
timeout 60 bash -c 'until docker-compose ps | grep -q "healthy"; do sleep 2; done' || {
    echo "❌ Services failed to start"
    docker-compose logs
    exit 1
}

# Run database migrations
echo "🗄️  Running database migrations..."
docker-compose exec -T app npm run migrate

# Run tests
echo "🧪 Running tests in Docker..."
docker-compose run --rm test

echo "✅ Deployment complete!"
echo "🌐 Application available at: http://localhost:3000"
echo "📊 View logs: docker-compose logs -f"
```

### Database Migration Script
```bash
#!/bin/bash
# scripts/migrate.sh

set -e

echo "🗄️  Running database migrations..."

# Check if database is ready
until pg_isready -h ${DB_HOST:-localhost} -p ${DB_PORT:-5432} -U ${DB_USER:-postgres}
do
  echo "⏳ Waiting for database..."
  sleep 2
done

echo "✅ Database is ready"

# Run migrations
npm run migration:run

echo "✅ Migrations completed"
```

### Pre-commit Hook Script
```bash
#!/bin/bash
# scripts/pre-commit.sh

set -e

echo "🔍 Running pre-commit checks..."

# Format code
echo "💅 Formatting code..."
npm run format

# Lint staged files
echo "🔍 Linting staged files..."
npm run lint:staged

# Run type check
echo "🔎 Type checking..."
npm run type-check

# Run unit tests
echo "🧪 Running unit tests..."
npm run test:unit

echo "✅ Pre-commit checks passed!"
```

## CI/CD Pipeline Configurations

### GitHub Actions Workflow
```yaml
# .github/workflows/ci-cd.yml
name: CI/CD Pipeline

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main, develop]

env:
  NODE_VERSION: '18'
  REGISTRY: ghcr.io
  IMAGE_NAME: ${{ github.repository }}

jobs:
  lint-and-test:
    runs-on: ubuntu-latest
    
    services:
      postgres:
        image: postgres:15-alpine
        env:
          POSTGRES_USER: postgres
          POSTGRES_PASSWORD: postgres
          POSTGRES_DB: eventdb_test
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
        ports:
          - 5432:5432

    steps:
      - name: Checkout code
        uses: actions/checkout@v3

      - name: Setup Node.js
        uses: actions/setup-node@v3
        with:
          node-version: ${{ env.NODE_VERSION }}
          cache: 'npm'

      - name: Install dependencies
        run: npm ci

      - name: Run linter
        run: npm run lint

      - name: Run type check
        run: npm run type-check

      - name: Run unit tests
        run: npm run test:unit

      - name: Run integration tests
        run: npm run test:integration
        env:
          DATABASE_URL: postgresql://postgres:postgres@localhost:5432/eventdb_test

      - name: Generate coverage report
        run: npm run test:coverage

      - name: Upload coverage to Codecov
        uses: codecov/codecov-action@v3
        with:
          files: ./coverage/coverage-final.json

  build-and-push:
    needs: lint-and-test
    runs-on: ubuntu-latest
    if: github.event_name == 'push'
    
    permissions:
      contents: read
      packages: write

    steps:
      - name: Checkout code
        uses: actions/checkout@v3

      - name: Set up Docker Buildx
        uses: docker/setup-buildx-action@v2

      - name: Log in to Container Registry
        uses: docker/login-action@v2
        with:
          registry: ${{ env.REGISTRY }}
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - name: Extract metadata
        id: meta
        uses: docker/metadata-action@v4
        with:
          images: ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}
          tags: |
            type=ref,event=branch
            type=sha,prefix={{branch}}-

      - name: Build and push Docker image
        uses: docker/build-push-action@v4
        with:
          context: .
          push: true
          tags: ${{ steps.meta.outputs.tags }}
          labels: ${{ steps.meta.outputs.labels }}
          cache-from: type=gha
          cache-to: type=gha,mode=max

  e2e-tests:
    needs: build-and-push
    runs-on: ubuntu-latest
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v3

      - name: Setup Node.js
        uses: actions/setup-node@v3
        with:
          node-version: ${{ env.NODE_VERSION }}

      - name: Install Playwright
        run: npx playwright install --with-deps

      - name: Start services
        run: docker-compose -f docker-compose.test.yml up -d

      - name: Wait for services
        run: timeout 60 bash -c 'until curl -f http://localhost:3000/health; do sleep 2; done'

      - name: Run E2E tests
        run: npm run test:e2e

      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v3
        with:
          name: playwright-report
          path: playwright-report/

  create-pr:
    needs: [lint-and-test, e2e-tests]
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/develop' && github.event_name == 'push'
    
    steps:
      - name: Checkout code
        uses: actions/checkout@v3

      - name: Create Pull Request
        uses: peter-evans/create-pull-request@v5
        with:
          token: ${{ secrets.GITHUB_TOKEN }}
          commit-message: 'chore: automated PR from CI/CD'
          title: 'Automated PR: Merge develop to main'
          body: |
            ## Automated Pull Request
            
            This PR was automatically created by the CI/CD pipeline.
            
            ### Changes
            - All tests passing ✅
            - Code coverage meets threshold ✅
            - Linting passed ✅
            - Docker image built and pushed ✅
            
            ### Review Checklist
            - [ ] Code review completed
            - [ ] Documentation updated
            - [ ] Breaking changes documented
          branch: develop
          base: main
```

### Azure DevOps Pipeline
```yaml
# azure-pipelines.yml
trigger:
  branches:
    include:
      - main
      - develop

pool:
  vmImage: 'ubuntu-latest'

variables:
  nodeVersion: '18.x'

stages:
  - stage: Build
    jobs:
      - job: BuildAndTest
        steps:
          - task: NodeTool@0
            inputs:
              versionSpec: $(nodeVersion)
            displayName: 'Install Node.js'

          - script: npm ci
            displayName: 'Install dependencies'

          - script: npm run lint
            displayName: 'Run linter'

          - script: npm run test:coverage
            displayName: 'Run tests with coverage'

          - task: PublishCodeCoverageResults@1
            inputs:
              codeCoverageTool: 'Cobertura'
              summaryFileLocation: '$(System.DefaultWorkingDirectory)/coverage/cobertura-coverage.xml'

          - script: npm run build
            displayName: 'Build application'

          - task: Docker@2
            displayName: 'Build Docker image'
            inputs:
              command: build
              dockerfile: '**/Dockerfile'
              tags: |
                $(Build.BuildId)
                latest

  - stage: Deploy
    dependsOn: Build
    condition: and(succeeded(), eq(variables['Build.SourceBranch'], 'refs/heads/main'))
    jobs:
      - deployment: DeployToProduction
        environment: 'production'
        strategy:
          runOnce:
            deploy:
              steps:
                - task: Docker@2
                  displayName: 'Push Docker image'
                  inputs:
                    command: push
                    tags: |
                      $(Build.BuildId)
                      latest
```

## Environment Configuration

### Environment Template
```bash
# .env.example
# Application
NODE_ENV=development
PORT=3000
LOG_LEVEL=info

# Database
DATABASE_URL=postgresql://postgres:password@localhost:5432/eventdb
DB_HOST=localhost
DB_PORT=5432
DB_USER=postgres
DB_PASSWORD=password
DB_NAME=eventdb

# Redis
REDIS_URL=redis://localhost:6379

# Authentication
JWT_SECRET=your-secret-key-here
JWT_EXPIRES_IN=24h

# External Services
CRM_API_URL=https://api.crm.example.com
CRM_API_KEY=your-api-key-here

# Monitoring
SENTRY_DSN=
NEW_RELIC_LICENSE_KEY=
```

## Communication Protocol
After automation setup completion:
```json
{
  "phase": "automation-deployment",
  "status": "complete",
  "artifacts": {
    "docker": ["Dockerfile", "docker-compose.yml"],
    "ci_cd": [".github/workflows/ci-cd.yml"],
    "scripts": ["scripts/build-and-test.sh", "scripts/deploy-local.sh"]
  },
  "next_action": "create_pr",
  "deployment_url": "http://localhost:3000",
  "test_results": "all_passed"
}
```