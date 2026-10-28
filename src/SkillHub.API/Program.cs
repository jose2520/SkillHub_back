using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using SkillHub.API.Middleware;
using SkillHub.API.OpenApi;
using SkillHub.Application;
using SkillHub.Infrastructure;
using SkillHub.Infrastructure.Persistence.Seeders;

var builder = WebApplication.CreateBuilder(args);

// 1. Clean Architecture Dependency Injection
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// 2. Centralized Exception Handling (RFC 7807 ProblemDetails)
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// 3. REST Controllers with Unified JSON Validation Error Responses
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(
                    entry => string.IsNullOrWhiteSpace(entry.Key) ? "request" : entry.Key,
                    entry => entry.Value!.Errors
                        .Select(err => string.IsNullOrWhiteSpace(err.ErrorMessage)
                            ? "The provided value is invalid."
                            : err.ErrorMessage)
                        .ToArray());

            var payload = new
            {
                type = "https://httpstatuses.com/400",
                title = "Validation Error",
                status = StatusCodes.Status400BadRequest,
                code = "Validation.Failed",
                message = "One or more request parameters or JSON fields are invalid.",
                detail = "One or more request parameters or JSON fields are invalid.",
                instance = context.HttpContext.Request.Path.Value,
                traceId = context.HttpContext.TraceIdentifier,
                errors,
                timestamp = DateTime.UtcNow
            };

            return new BadRequestObjectResult(payload)
            {
                ContentTypes = { "application/problem+json" }
            };
        };
    });

// 4. Explicit CORS Whitelist for Frontend Clients
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173", "http://127.0.0.1:5173", "http://localhost:3000"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// 5. JWT Bearer Authentication & Role-Based Authorization
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"]
    ?? throw new InvalidOperationException("JWT signing key ('Jwt:Key') must be configured.");
var jwtIssuer = jwtSection["Issuer"] ?? "SkillHubAPI";
var jwtAudience = jwtSection["Audience"] ?? "SkillHubClient";

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/problem+json; charset=utf-8";

                var payload = JsonSerializer.Serialize(new
                {
                    type = "https://httpstatuses.com/401",
                    title = "Unauthorized",
                    status = 401,
                    code = "Auth.TokenRequired",
                    message = "A valid JWT Bearer token is required to access this endpoint.",
                    detail = "A valid JWT Bearer token is required to access this endpoint.",
                    instance = context.Request.Path.Value,
                    traceId = context.HttpContext.TraceIdentifier,
                    timestamp = DateTime.UtcNow
                });

                await context.Response.WriteAsync(payload);
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/problem+json; charset=utf-8";

                var payload = JsonSerializer.Serialize(new
                {
                    type = "https://httpstatuses.com/403",
                    title = "Forbidden",
                    status = 403,
                    code = "Auth.InsufficientRole",
                    message = "You do not have permission to perform this operation.",
                    detail = "You do not have permission to perform this operation.",
                    instance = context.Request.Path.Value,
                    traceId = context.HttpContext.TraceIdentifier,
                    timestamp = DateTime.UtcNow
                });

                await context.Response.WriteAsync(payload);
            }
        };
    });

builder.Services.AddAuthorization();

// 6. OpenAPI / Swagger UI & Scalar API Reference
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SkillHub API",
        Version = "v1",
        Description = "Clean Architecture REST API for SkillHub workshop catalog, enrollment rules, Excel bulk import, and PDF enrollment certificates."
    });

    options.OrderActionsBy(apiDesc =>
        apiDesc.ActionDescriptor.AttributeRouteInfo?.Name ?? apiDesc.RelativePath ?? string.Empty);
    options.DocumentFilter<OpenApiDocumentOrderFilter>();
    options.SchemaFilter<OpenApiRequestExamplesFilter>();

    var xmlFilename = $"{typeof(Program).Assembly.GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste your JWT token obtained from POST /api/v1/auth/login."
    });

    options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", doc),
            new List<string>()
        }
    });
});

var app = builder.Build();

// 7. Automatic Database Migration & Initial Excel Seeding (Instructors, Participants, Workshops)
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<IDbSeeder>();
    await seeder.SeedAsync();
}

// 8. HTTP Request Pipeline
app.UseExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SkillHub API v1");
    c.RoutePrefix = "swagger";
    c.EnablePersistAuthorization();
    c.InjectJavascript("/swagger/swagger-order.js");
});

app.MapGet("/swagger/swagger-order.js", () => Results.Content(
    """
    (function () {
      var originalOnLoad = window.onload;
      window.onload = function () {
        var originalBundle = window.SwaggerUIBundle;
        if (originalBundle) {
          var wrappedBundle = function (cfg) {
            cfg.operationsSorter = function (a, b) {
              var idA = (a.get("operation") && a.get("operation").get("operationId")) || "";
              var idB = (b.get("operation") && b.get("operation").get("operationId")) || "";
              return idA.localeCompare(idB);
            };
            return originalBundle(cfg);
          };
          Object.assign(wrappedBundle, originalBundle);
          window.SwaggerUIBundle = wrappedBundle;
        }
        if (originalOnLoad) {
          originalOnLoad();
        }
      };
    })();
    """,
    "application/javascript; charset=utf-8"))
    .AllowAnonymous()
    .ExcludeFromDescription();

app.MapScalarApiReference(options =>
{
    options.WithOpenApiRoutePattern("/swagger/v1/swagger.json");
    options.WithTitle("SkillHub API - Scalar");
    options.WithTheme(ScalarTheme.Moon);
});

if (!app.Environment.IsDevelopment() && builder.Configuration.GetValue("EnableHttpsRedirection", false))
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors("FrontendPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new
{
    name = "SkillHub API",
    version = "v1",
    status = "Online",
    architecture = "Clean Architecture (.NET 10 / PostgreSQL)",
    documentation = new
    {
        swagger = "/swagger",
        scalar = "/scalar/v1"
    },
    timestamp = DateTime.UtcNow
}))
.AllowAnonymous()
.ExcludeFromDescription();

app.MapControllers();

app.Run();

public partial class Program;
