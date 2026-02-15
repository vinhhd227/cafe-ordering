using Api.Infrastructure.Services;

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
