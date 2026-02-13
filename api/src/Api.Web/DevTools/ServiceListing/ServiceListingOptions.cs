namespace Api.Web.DevTools.ServiceListing;

public class ServiceListingOptions
{
  /// <summary>
  ///   Endpoint path (default: /dev/services)
  /// </summary>
  public string Path { get; set; } = "/dev/services";

  /// <summary>
  ///   Enable in production (default: false)
  /// </summary>
  public bool EnableInProduction { get; set; } = false;

  /// <summary>
  ///   Show only implemented interfaces
  /// </summary>
  public bool ShowOnlyImplementedInterfaces { get; set; } = false;

  /// <summary>
  ///   Filter by lifetime
  /// </summary>
  public ServiceLifetime? FilterByLifetime { get; set; }

  /// <summary>
  ///   Custom filter predicate
  /// </summary>
  public Func<ServiceDescriptor, bool>? CustomFilter { get; set; }

  /// <summary>
  ///   Require authorization
  /// </summary>
  public bool RequireAuthorization { get; set; } = false;
}
