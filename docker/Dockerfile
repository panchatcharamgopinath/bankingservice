# Multi-stage build for Banking Service API
# Production-ready Dockerfile with security hardening

# ============================================================================
# Stage 1: Build
# ============================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy solution and project files
COPY ["bankingservice.sln", "./"]
COPY ["src/BankingService.csproj", "src/"]
COPY ["test/BankingServiceTest.csproj", "test/"]

# Restore dependencies
RUN dotnet restore "src/BankingService.csproj"

# Copy all source files
COPY . .

# Build the application
WORKDIR "/src/src"
RUN dotnet build "BankingService.csproj" \
    -c Release \
    -o /app/build \
    --no-restore

# ============================================================================
# Stage 2: Test
# ============================================================================
FROM build AS test
WORKDIR /src/test
RUN dotnet test "BankingServiceTest.csproj" \
    -c Release \
    --no-build \
    --verbosity normal \
    --logger "trx;LogFileName=test-results.trx"

# ============================================================================
# Stage 3: Publish
# ============================================================================
FROM build AS publish
WORKDIR "/src/src"
RUN dotnet publish "BankingService.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    --no-build \
    /p:UseAppHost=false

# ============================================================================
# Stage 4: Runtime (Final)
# ============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final

# Install required packages for health checks and debugging
RUN apk add --no-cache \
    curl \
    ca-certificates \
    tzdata

# Set timezone to UTC
ENV TZ=UTC

WORKDIR /app

# Create non-root user and group
RUN addgroup -g 10001 appgroup && \
    adduser -D -u 10001 -G appgroup appuser && \
    mkdir -p /app/tmp /app/logs && \
    chown -R appuser:appgroup /app

# Switch to non-root user
USER appuser:appgroup

# Copy published files from publish stage
COPY --from=publish --chown=appuser:appgroup /app/publish .

# Environment variables
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    COMPlus_EnableDiagnostics=0

# Expose ports
EXPOSE 8080

# Health check using curl
HEALTHCHECK --interval=30s \
    --timeout=5s \
    --start-period=10s \
    --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Labels for image metadata
LABEL maintainer="banking-team@company.com" \
      version="1.0.0" \
      description="Banking Service API" \
      org.opencontainers.image.source="https://github.com/your-org/banking-service" \
      org.opencontainers.image.vendor="Your Company" \
      org.opencontainers.image.title="Banking Service API"

# Entry point
ENTRYPOINT ["dotnet", "BankingService.dll"]
