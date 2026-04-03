namespace Api.Web.Configurations;

public static class LoggerConfigs
{
  public static WebApplicationBuilder AddLoggerConfigs(this WebApplicationBuilder builder)
  {
    // UseSerilog replaces all default logging providers (console, debug, etc.)
    // WriteTo sinks are configured via appsettings.json / appsettings.*.json
    builder.Host.UseSerilog((ctx, cfg) => cfg
      .ReadFrom.Configuration(ctx.Configuration)
      .Enrich.FromLogContext()
      .Enrich.WithProperty("Application", ctx.HostingEnvironment.ApplicationName));

    return builder;
  }
}
