using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using Serilog;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;
using System.Text;
using System.Threading.RateLimiting;
using BankingService.Data;
using BankingService.Services;
using BankingService.Middleware;

using Microsoft.AspNetCore.Http.Features;  // ADD THIS LINE
using Azure.Monitor.OpenTelemetry.Exporter; // ADD THIS LINE

var builder = WebApplication.CreateBuilder(args);

var env = builder.Environment;
var config = builder.Configuration;
var dbProvider = config["Database:Provider"];


// Configure Serilog
var loggerConfig = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");

// Only add Application Insights in non-development environments
// Only add Application Insights if configured
var appInsightsConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
if (!string.IsNullOrEmpty(appInsightsConnectionString))
{
    loggerConfig.WriteTo.ApplicationInsights(
        appInsightsConnectionString,
        TelemetryConverter.Traces);
}

Log.Logger = loggerConfig.CreateLogger();
builder.Host.UseSerilog();

// Configure OpenTelemetry based on config
if (config.GetValue<bool>("OpenTelemetry:UseAzureMonitor", false))
    {
        builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService(
            serviceName: builder.Configuration["OpenTelemetry:ServiceName"] ?? "BankingService",
            serviceVersion: "1.0.0"))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddAzureMonitorTraceExporter(options =>
            {
                options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
            }));
    }

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger configuration with JWT support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Banking Service API", 
        Version = "v1",
        Description = "Production-grade banking REST API"
    });
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Database configuration
builder.Services.AddDbContext<BankingDbContext>(options =>
{
    if (dbProvider?.ToLower() == "sqlite")
    {
        // Use SQLite for local development
        options.UseSqlite(config.GetConnectionString("DefaultConnection") 
            ?? "Data Source=banking.db");
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
    else
    {
        // Use SQL Server for production with resilience
        options.UseSqlServer(config.GetConnectionString("SqlServer"), sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
            sqlOptions.CommandTimeout(30);
            sqlOptions.MigrationsAssembly("BankingService");
        });
    }
});


// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? 
    "YourSuperSecretKeyThatIsAtLeast32CharactersLongForHS256Algorithm"; //throw new InvalidOperationException("JWT Key must be configured in Key Vault");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "BankingService";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "BankingServiceClient";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Register services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ICardService, CardService>();
builder.Services.AddScoped<IStatementService, StatementService>();

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<BankingDbContext>("database");

// 6. Add rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
});

// Add CORS
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
        ?? new[] { "https://localhost:5001" };
    
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// 8. Add request size limits
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10_485_760; // 10 MB
});

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 10_485_760; // 10 MB
});

var app = builder.Build();


// Configure the HTTP request pipeline
if (dbProvider?.ToLower() == "sqlite")
{
    try {
    // Ensure database is created and migrated
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<BankingDbContext>();
        await db.Database.MigrateAsync();
        Log.Information("Database initialized successfully");
    }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to initialize database");
        throw;
    }
    
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware order is important
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseRateLimiter();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHealthChecks("/ready");

// Graceful shutdown
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
{
    Log.Information("Application is shutting down gracefully...");
});

lifetime.ApplicationStopped.Register(() =>
{
    Log.Information("Application has stopped");
    Log.CloseAndFlush();
});

Log.Information("Banking Service starting up...");
app.Run();
Log.Information("Banking Service shut down complete");