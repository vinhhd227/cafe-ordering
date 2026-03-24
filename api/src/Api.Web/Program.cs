using Api.Infrastructure;
using Api.Web.Configurations;
using ILogger = Microsoft.Extensions.Logging.ILogger;

var builder = WebApplication.CreateBuilder(args);

// === Logging ===
builder.AddLoggerConfigs();

var logger = Log();

// === Services ===

// Infrastructure (DbContext, Repositories, External Services)
builder.Services.AddInfrastructure(builder.Configuration, logger);

// Mediator + FluentValidation
builder.Services.AddAppMediator();

// Authentication + Authorization
builder.Services.AddAuth(builder.Configuration);

// CORS
builder.Services.AddCorsPolicy(builder.Configuration);

// Rate limiting
builder.Services.AddOrderRateLimiting(builder.Configuration);

// FastEndpoints + Swagger
builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument(o =>
{
  o.DocumentSettings = s =>
  {
    s.Title = "Cafe Ordering API";
    s.Version = "v1";
  };
  o.TagDescriptions = t =>
  {
    t["Authentication"] = "🔒 Register, login, and token management";
    t["Menu"]           = "🍽️ Public menu for ordering";
    t["Orders"]         = "🧾 Order placement and management";
    t["Products"]       = "☕ Menu item management";
    t["Categories"]     = "🗂️ Product category management";
    t["System"]         = "⚙️ Health and infrastructure endpoints";
  };
});

var app = builder.Build();

// === Middleware Pipeline ===

// CORS
app.UseCors(CorsConfiguration.PolicyName);

// Auth
app.UseAuthentication();
app.UseAuthorization();

// FastEndpoints + Swagger + Migrate + Seed
await app.UseAppMiddlewareAndSeedDatabase();

app.Run();

// === Helper ===
static ILogger Log()
{
  using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
  return loggerFactory.CreateLogger("Program");
}

// Make Program accessible for WebApplicationFactory in test projects
public partial class Program { }
