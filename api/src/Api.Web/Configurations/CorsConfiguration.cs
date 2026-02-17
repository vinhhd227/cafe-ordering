namespace Api.Web.Configurations;

public static class CorsConfiguration
{
  public const string PolicyName = "DefaultCors";

  public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
  {
    var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                         ?? new[] { "http://localhost:3000" };

    services.AddCors(options =>
    {
      options.AddPolicy(PolicyName, policy =>
      {
        policy.WithOrigins(allowedOrigins)
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials();
      });
    });

    return services;
  }
}
