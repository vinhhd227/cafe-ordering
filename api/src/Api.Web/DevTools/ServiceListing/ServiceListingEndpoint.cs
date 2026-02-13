namespace Api.Web.DevTools.ServiceListing;

public static class ServiceListingEndpoint
{
  public static IEndpointRouteBuilder MapServiceListing(
    this IEndpointRouteBuilder endpoints)
  {
    endpoints.MapGet("/dev/services", async (
        HttpContext context,
        IHostEnvironment env,
        ServiceListingOptions options,
        IEnumerable<ServiceDescriptor> services) =>
      {
        // Security check - only in Development
        if (!options.EnableInProduction && env.IsProduction())
        {
          return Results.NotFound();
        }

        // Authorization check (if required)
        if (options.RequireAuthorization && !context.User.Identity?.IsAuthenticated == true)
        {
          return Results.Unauthorized();
        }

        // Apply filters
        var filteredServices = ApplyFilters(services, options, context);

        // Generate HTML
        var html = GenerateHtml(filteredServices.ToList(), context.Request.Query["search"]);

        return Results.Content(html, "text/html");
      })
      .WithName("DevTools_ServiceListing")
      .WithTags("DevTools")
      .ExcludeFromDescription(); // Hide from Swagger

    return endpoints;
  }

  private static IEnumerable<ServiceDescriptor> ApplyFilters(
    IEnumerable<ServiceDescriptor> services,
    ServiceListingOptions options,
    HttpContext context)
  {
    var filtered = services.AsEnumerable();

    // Filter by lifetime
    if (options.FilterByLifetime.HasValue)
    {
      filtered = filtered.Where(s => s.Lifetime == options.FilterByLifetime.Value);
    }

    // Filter by interfaces only
    if (options.ShowOnlyImplementedInterfaces)
    {
      filtered = filtered.Where(s => s.ServiceType.IsInterface);
    }

    // Custom filter
    if (options.CustomFilter != null)
    {
      filtered = filtered.Where(options.CustomFilter);
    }

    // Search query
    var search = context.Request.Query["search"].ToString();
    if (!string.IsNullOrEmpty(search))
    {
      filtered = filtered.Where(s =>
        s.ServiceType.FullName?.Contains(search, StringComparison.OrdinalIgnoreCase) == true ||
        GetImplementationType(s).Contains(search, StringComparison.OrdinalIgnoreCase));
    }

    return filtered;
  }

  private static string GenerateHtml(List<ServiceDescriptor> services, string? searchTerm)
  {
    // ... HTML generation code từ phần trước
    return "..."; // Implement như version 2 ở trên
  }

  private static string GetImplementationType(ServiceDescriptor service)
  {
    if (service.ImplementationType != null)
    {
      return service.ImplementationType.FullName ?? service.ImplementationType.Name;
    }

    if (service.ImplementationFactory != null)
    {
      return "Factory";
    }

    if (service.ImplementationInstance != null)
    {
      return service.ImplementationInstance.GetType().FullName ?? "Instance";
    }

    return "Unknown";
  }
}
