using Api.Core.Entities.Identity;
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

    // === Database ===
    services.AddDbContext<AppDbContext>((provider, options) =>
    {
      var interceptor = provider.GetRequiredService<EventDispatchInterceptor>();

      options.UseNpgsql(connectionString);
      options.AddInterceptors(interceptor);
    });

    // === Identity Services ===
    services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
      {
        // Password settings
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;

        // Lockout settings
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        // User settings
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = false; // Set true in production
      })
      .AddEntityFrameworkStores<AppDbContext>()
      .AddDefaultTokenProviders();

    // === Identity Abstraction Services ===
    services.AddScoped<IIdentityService, IdentityService>();
    services.AddScoped<IJwtService, JwtService>();

    // === Repositories ===
    services.AddScoped(typeof(IRepositoryBase<>), typeof(EfRepository<>));
    services.AddScoped(typeof(IReadRepositoryBase<>), typeof(EfReadRepository<>));
    services.AddScoped(typeof(IReadRepository<>), typeof(EfReadRepository<>));

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
