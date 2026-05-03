using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.DataProtection;
using System.Text.Json.Serialization;
using LogLens.Infrastructure.Data;
using LogLens.Application.Interfaces;
using LogLens.Application.Services;
using LogLens.API.Endpoints;
using LogLens.API.Hubs;
using LogLens.API.Middleware;
using LogLens.Infrastructure.BackgroundServices;
using LogLens.Infrastructure.Queue;
using LogLens.Infrastructure.Repositories;
using LogLens.Infrastructure.Services;
using LogLens.ML.Clustering;
using LogLens.ML.Forecasting;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Serilog setup
builder.Host.UseSerilog((ctx, lc) =>
    lc.WriteTo.Console()
      .ReadFrom.Configuration(ctx.Configuration));

// EF Core with Npgsql
builder.Services.AddDbContext<LogLensDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<LogLensDbContext>());

// health checks with detailed probes
builder.Services.AddHealthChecks()
    .AddDbContextCheck<LogLensDbContext>("PostgreSQL", failureStatus: HealthStatus.Unhealthy, tags: new[] { "db" })
    .AddCheck("memory", () => 
    {
        var memory = GC.GetTotalMemory(false);
        return memory < 1024 * 1024 * 1024 ? HealthCheckResult.Healthy() : HealthCheckResult.Degraded("High memory usage");
    }, tags: new[] { "system" });

// SignalR
builder.Services.AddSignalR()
    .AddMessagePackProtocol();

// Add CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var allowedOrigins = builder.Configuration
              .GetSection("AllowedOrigins")
              .Get<string[]>() 
              ?? new[] { "https://loglens-frontend.vercel.app" };

        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "LogLens API",
        Version = "v1",
        Description = "Distributed log aggregation and predictive analytics platform",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "LogLens Team"
        }
    });
    c.EnableAnnotations();
});

// controllers
builder.Services.AddControllers();
builder.Services.AddDataProtection();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddMemoryCache();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwt = builder.Configuration.GetSection("Jwt");
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwt["Issuer"],
        ValidAudience = jwt["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwt["Key"]!))
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
});

// dependency injection
builder.Services.AddSingleton<LogChannel>();
builder.Services.AddSingleton<ILogQueueService, LogQueueService>();
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IServiceRegistryService, ServiceRegistryService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IApiKeyCipher, DataProtectionApiKeyCipher>();

builder.Services.AddScoped<ILogRepository, LogRepository>();
builder.Services.AddScoped<IIncidentRepository, IncidentRepository>();
builder.Services.AddScoped<IForecastRepository, ForecastRepository>();
builder.Services.AddScoped<IAlertRepository, AlertRepository>();

// Add ML services
builder.Services.AddSingleton<IncidentClusteringService>();
builder.Services.AddSingleton<WarningForecastService>();
builder.Services.AddScoped<IIncidentClusteringService, IncidentClusteringApplicationService>();
builder.Services.AddScoped<IForecastService, ForecastApplicationService>();
builder.Services.AddSingleton<ILogSanitizer, LogSanitizer>();
builder.Services.AddScoped<ILogAnalyticsService, LogAnalyticsService>();
builder.Services.AddScoped<IRiskAnalysisService, RiskAnalysisService>();
builder.Services.AddScoped<IIncidentService, IncidentService>();

// background processing
builder.Services.AddHostedService<LogBatchInserter>();
builder.Services.AddHostedService<MlProcessingService>();

var app = builder.Build();

// Apply migrations at startup and fail fast on DB issues.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LogLensDbContext>();
    var apiKeyCipher = scope.ServiceProvider.GetRequiredService<IApiKeyCipher>();
    await ApplyMigrationsSafelyAsync(db);
    await RepairLegacyApiKeySchemaAsync(db);
    await SeedData.SeedAsync(db, apiKeyCipher);
}

// Swagger UI in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LogLens API V1");
        c.RoutePrefix = "swagger";
    });
}

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionMiddleware>();
app.UseCors("AllowFrontend");
app.UseMiddleware<ApiKeyMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));
app.MapHealthChecks("/health/detailed", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter
});

app.MapAuthEndpoints();
app.MapServiceEndpoints();
app.MapUserEndpoints();
app.MapLogEndpoints();
app.MapHub<LogHub>("/loghub");

app.Run();

// Custom health check response writer
static async Task HealthCheckResponseWriter(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    var response = new
    {
        status = report.Status.ToString(),
        checks = report.Entries.ToDictionary(
            x => x.Key,
            x => new { status = x.Value.Status.ToString(), exception = x.Value.Exception?.Message }
        ),
        duration = report.TotalDuration
    };
    
    await context.Response.WriteAsJsonAsync(response);
}

static async Task ApplyMigrationsSafelyAsync(LogLensDbContext db)
{
    var historyExists = await TableExistsAsync(db, "__EFMigrationsHistory");
    var incidentsExists = await TableExistsAsync(db, "incidents");
    var logsExists = await TableExistsAsync(db, "logs");
    var servicesExists = await TableExistsAsync(db, "services");
    var usersExists = await TableExistsAsync(db, "users");
    var apiKeysExists = await TableExistsAsync(db, "api_keys");

    if (historyExists && incidentsExists && logsExists && servicesExists && usersExists && apiKeysExists)
    {
        Console.WriteLine("[LogLens] Existing schema detected with empty migration history. Skipping migrations and continuing startup.");
        return;
    }

    db.Database.Migrate();
}

static async Task<bool> TableExistsAsync(LogLensDbContext db, string tableName)
{
    var connection = db.Database.GetDbConnection();
    await using var command = connection.CreateCommand();
    command.CommandText = @"
        SELECT EXISTS (
            SELECT 1
            FROM pg_catalog.pg_class c
            JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
            WHERE n.nspname = 'public'
              AND c.relname = @tableName
        );";

    var parameter = command.CreateParameter();
    parameter.ParameterName = "@tableName";
    parameter.Value = tableName;
    command.Parameters.Add(parameter);

    if (connection.State != System.Data.ConnectionState.Open)
    {
        await connection.OpenAsync();
    }

    var result = await command.ExecuteScalarAsync();
    return result is bool exists && exists;
}

static async Task<bool> ColumnExistsAsync(LogLensDbContext db, string tableName, string columnName)
{
    var connection = db.Database.GetDbConnection();
    await using var command = connection.CreateCommand();
    command.CommandText = @"
        SELECT EXISTS (
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = @tableName
              AND column_name = @columnName
        );";

    var tableParameter = command.CreateParameter();
    tableParameter.ParameterName = "@tableName";
    tableParameter.Value = tableName;
    command.Parameters.Add(tableParameter);

    var columnParameter = command.CreateParameter();
    columnParameter.ParameterName = "@columnName";
    columnParameter.Value = columnName;
    command.Parameters.Add(columnParameter);

    if (connection.State != System.Data.ConnectionState.Open)
    {
        await connection.OpenAsync();
    }

    var result = await command.ExecuteScalarAsync();
    return result is bool exists && exists;
}

static async Task RepairLegacyApiKeySchemaAsync(LogLensDbContext db)
{
    var apiKeysTableExists = await TableExistsAsync(db, "api_keys");
    if (!apiKeysTableExists)
    {
        return;
    }

    var ciphertextColumnExists = await ColumnExistsAsync(db, "api_keys", "RawApiKeyCiphertext");
    if (ciphertextColumnExists)
    {
        return;
    }

    await db.Database.ExecuteSqlRawAsync(@"
        ALTER TABLE api_keys
        ADD COLUMN IF NOT EXISTS ""RawApiKeyCiphertext"" character varying(2048);
    ");

    Console.WriteLine("[LogLens] Repaired legacy api_keys schema by adding RawApiKeyCiphertext.");
}