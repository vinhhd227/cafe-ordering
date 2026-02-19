using Api.Infrastructure.Identity;
using Api.Infrastructure.Services;
using Api.UseCases.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Api.Infrastructure;

public static class DependencyInjection
{
  public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration,
    ILogger logger)
  {
    var connectionString = configuration.GetConnectionString("DefaultConnection");

    // === Event Dispatch ===
    services.AddScoped<EventDispatchInterceptor>();
    services.AddScoped<IDomainEventDispatcher, MediatorDomainEventDispatcher>();

    // === Business Database — schema "business" ===
    // Products, Categories, Customers, Orders
    services.AddDbContext<AppDbContext>((provider, options) =>
    {
      var interceptor = provider.GetRequiredService<EventDispatchInterceptor>();
      options.UseNpgsql(connectionString, o =>
        o.MigrationsHistoryTable("__EFMigrationsHistory", "business"));
      options.AddInterceptors(interceptor);
    });

    // === Identity Database — schema "identity" (cùng DB, khác schema) ===
    // Users, Roles, Claims, Tokens
    services.AddDbContext<AppIdentityDbContext>(options =>
    {
      options.UseNpgsql(connectionString, o =>
        o.MigrationsHistoryTable("__EFMigrationsHistory", "identity"));
    });

    // === Identity Services ===
    services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
      {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;

        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = false;
      })
      .AddEntityFrameworkStores<AppIdentityDbContext>()
      .AddDefaultTokenProviders();

    // === Identity Abstraction Services ===
    services.AddScoped<IIdentityService, IdentityService>();
    services.AddScoped<IJwtService, JwtService>();

    // === Repositories ===
    services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
    services.AddScoped(typeof(IRepositoryBase<>), typeof(EfRepository<>));
    services.AddScoped(typeof(IReadRepository<>), typeof(EfReadRepository<>));
    services.AddScoped(typeof(IReadRepositoryBase<>), typeof(EfReadRepository<>));

    // === Current User ===
    services.AddHttpContextAccessor();
    services.AddScoped<ICurrentUserService, CurrentUserService>();

    // === Email ===
    services.Configure<SmtpSettings>(configuration.GetSection("SmtpSettings"));
    services.AddScoped<IEmailSender, EmailSender>();

    // === File Storage ===
    services.Configure<FileStorageSettings>(configuration.GetSection("FileStorage"));
    services.AddScoped<IFileStorageService, FileStorageService>();

    logger.LogInformation("{Project} services registered", "Infrastructure");

    return services;
  }
}
