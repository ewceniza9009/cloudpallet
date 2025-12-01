// ---- File: src/Presentation/WMS.Api/Program.cs [REFACTORED with Secret Management comment] ----
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;
using Serilog;
using System.Text;
using System.Text.Json.Serialization;
using WMS.Api.Common;
using WMS.Api.Hubs;
using WMS.Api.Infrastructure;
using WMS.Api.Services;
using WMS.Application;
using WMS.Application.Abstractions.Security;
using WMS.Application.Common.Behaviors;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Domain.Shared; // For IClock
using WMS.Infrastructure;
using WMS.Infrastructure.Persistence;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// --- Add configuration for production secrets ---
if (!builder.Environment.IsDevelopment())
{
    // Example: Add Azure Key Vault (ensure Microsoft.Extensions.Configuration.AzureKeyVault is installed)
    // var keyVaultUri = builder.Configuration["KeyVault:Uri"];
    // if (!string.IsNullOrEmpty(keyVaultUri))
    // {
    //     // Requires Azure.Identity package
    //     builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());
    // }

    // Or rely on Environment Variables set in the hosting environment (e.g., Azure App Service Configuration)
    // builder.Configuration.AddEnvironmentVariables();
}


builder.Host.UseSerilog((context, config) => config.ReadFrom.Configuration(context.Configuration));

builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration);

builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(WMS.Application.DependencyInjection).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly); // Register handlers in API project (like notification handlers)
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

// --- START: ADD IDENTITY SERVICES ---
builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
{
    // Configure password requirements as needed for production
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 4; // Increase for production
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
    .AddEntityFrameworkStores<WmsDbContext>()
    .AddDefaultTokenProviders();
// --- END: ADD IDENTITY SERVICES ---

builder.Services.AddSingleton<IClock, SystemClock>(); // Register SystemClock for dependency injection
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddHostedService<TemperatureSimulationService>();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>(); // Add custom validation exception handler

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); // Serialize enums as strings
        options.JsonSerializerOptions.Converters.Add(new DateTimeConverter()); // Add the custom DateTime converter
    });
builder.Services.AddProblemDetails(); // Add standard ProblemDetails support
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Cold Storage WMS API", Version = "v1" });
    // Configure Swagger to use JWT Bearer authentication
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter JWT with Bearer into field (e.g., 'Bearer {token}')",
        Name = "Authorization",
        Type = SecuritySchemeType.Http, // Changed from ApiKey to Http for Bearer
        BearerFormat = "JWT",
        Scheme = "Bearer" // Use "Bearer" scheme
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                // IMPORTANT: Ensure "JwtSettings:SecretKey" comes from a secure source in production
                Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"] ?? throw new InvalidOperationException("JWT Secret Key not configured")))
        };

        // Allow token to be passed via query string for SignalR WebSocket connections
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                // Only apply for SignalR hubs and specific API endpoints if needed (like barcode image)
                if (!string.IsNullOrEmpty(accessToken) &&
                     (path.StartsWithSegments("/hubs") ||
                      path.StartsWithSegments("/api/Lookups/barcode-image") ||
                      path.StartsWithSegments("/api/reports/custom"))) // Allow for custom report PDF download
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

// Configure Authorization Policies based on UserRole enum
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole(UserRole.Admin.ToString()));
    options.AddPolicy("OperatorPolicy", policy => policy.RequireRole(UserRole.Admin.ToString(), UserRole.Operator.ToString()));
    options.AddPolicy("FinancePolicy", policy => policy.RequireRole(UserRole.Admin.ToString(), UserRole.Finance.ToString()));
    // Add other policies as needed
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        // Restrict origins for production
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:4200", "http://localhost:17283"];
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Initialize Database (Schema & Seed)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<WmsDbContext>();
        var dataPath = Path.Combine(Directory.GetCurrentDirectory(), "wms-frontend", "src", "assets", "mock-data");

        if (app.Environment.IsDevelopment())
        {
             // Export data from local SQL Server to JSON for seeding production
             // Ensure we don't overwrite if we don't intend to, but for now we want to capture latest
             // Note: Directory.GetCurrentDirectory() in dotnet run might be the project folder or the root.
             // We need to be careful with the path.
             // If running from x:\wms, then "wms-frontend" is a sibling of "src".
             // If running from x:\wms\src\Presentation\WMS.Api, then we need to go up.
             // Let's assume running from x:\wms as per previous commands.
             
             // We can use a relative path that works from the project root
             if (!Directory.Exists(dataPath))
             {
                 // Try to find it relative to the execution directory
                 var potentialPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../wms-frontend/src/assets/mock-data"));
                 if (Directory.Exists(Path.GetDirectoryName(potentialPath))) 
                 {
                    dataPath = potentialPath;
                 }
             }

             await JsonDataSeeder.ExportDataAsync(context, dataPath);
             Log.Information($"Data exported to {dataPath}");
        }
        
        // In Production (Neon), we use EnsureCreated to bypass migration tool issues
        if (app.Environment.IsProduction())
        {
            await context.Database.EnsureCreatedAsync();
            await JsonDataSeeder.SeedAsync(context, dataPath);
            
            Log.Information("Database seeded from JSON data.");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while initializing the database.");
    }
}

app.UseExceptionHandler(); // Use custom and default exception handling
app.UseSerilogRequestLogging(); // Log HTTP requests
app.UseHttpsRedirection();

app.UseRouting();

app.UseCors(); // Apply CORS policy

app.UseAuthentication(); // Must come before Authorization
app.UseAuthorization();

app.MapControllers(); // Map attribute-routed controllers

// Map SignalR Hubs
app.MapHub<TemperatureHub>("/hubs/temperature");
app.MapHub<DockHub>("/hubs/docks");
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();