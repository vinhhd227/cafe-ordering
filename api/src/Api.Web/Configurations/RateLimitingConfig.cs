using System.Threading.RateLimiting;

namespace Api.Web.Configurations;

public static class RateLimitingConfig
{
  public static IServiceCollection AddOrderRateLimiting(
    this IServiceCollection services, IConfiguration configuration)
  {
    var permitLimit   = configuration.GetValue<int>("RateLimiting:PermitLimit", 10);
    var windowSeconds = configuration.GetValue<int>("RateLimiting:WindowSeconds", 60);

    services.AddRateLimiter(options =>
    {
      options.AddPolicy("OrdersPerIp", ctx =>
        RateLimitPartition.GetSlidingWindowLimiter(
          partitionKey: GetClientIp(ctx),
          factory: _ => new SlidingWindowRateLimiterOptions
          {
            PermitLimit          = permitLimit,
            Window               = TimeSpan.FromSeconds(windowSeconds),
            SegmentsPerWindow    = 4,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit           = 0
          }));

      options.OnRejected = async (ctx, token) =>
      {
        ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await ctx.HttpContext.Response.WriteAsync(
          "Quá nhiều yêu cầu. Vui lòng thử lại sau.", token);
      };
    });

    return services;
  }

  private static string GetClientIp(HttpContext ctx) =>
    ctx.Request.Headers["CF-Connecting-IP"].FirstOrDefault()
    ?? ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
    ?? ctx.Connection.RemoteIpAddress?.ToString()
    ?? "unknown";
}
