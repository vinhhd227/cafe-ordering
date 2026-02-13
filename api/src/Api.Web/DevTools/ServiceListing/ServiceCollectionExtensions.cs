namespace Api.Web.DevTools.ServiceListing;

public static class ServiceCollectionExtensions
{
  /// <summary>
  ///   Add service listing capability
  ///   Only use in Development environment
  /// </summary>
  public static IServiceCollection AddServiceListing(
    this IServiceCollection services,
    Action<ServiceListingOptions>? configureOptions = null)
  {
    // Configure options
    var options = new ServiceListingOptions();
    configureOptions?.Invoke(options);
    services.AddSingleton(options);

    // Capture service descriptors
    var serviceDescriptors = services.ToList();
    services.AddSingleton<IEnumerable<ServiceDescriptor>>(serviceDescriptors);

    return services;
  }
}
