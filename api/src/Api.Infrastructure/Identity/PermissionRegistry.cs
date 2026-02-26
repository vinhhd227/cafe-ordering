namespace Api.Infrastructure.Identity;

/// <summary>
/// Central registry of all known permissions in the system.
/// Each entry maps a permission value (e.g. "order.create") to a human-readable description.
/// Used for seeding, validation, and UI display.
/// </summary>
public static class PermissionRegistry
{
  public static readonly IReadOnlyDictionary<string, string> All =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      // Menu
      ["menu.read"]          = "View menu items",

      // Orders
      ["order.create"]       = "Place new orders",
      ["order.read"]         = "View orders and details",
      ["order.update"]       = "Update order status",
      ["order.delete"]       = "Delete orders",

      // Products
      ["product.create"]     = "Create new products",
      ["product.read"]       = "View product list",
      ["product.update"]     = "Edit product information",
      ["product.delete"]     = "Delete products",

      // Staff
      ["staff.create"]       = "Create staff accounts",
      ["staff.read"]         = "View staff list",
      ["staff.update"]       = "Edit staff information",
      ["staff.deactivate"]   = "Deactivate staff accounts",

      // Users
      ["user.read"]          = "View user accounts",
      ["user.create"]        = "Create user accounts",
      ["user.update"]        = "Edit user information",
      ["user.deactivate"]    = "Deactivate user accounts",

      // Tables
      ["table.create"]       = "Create tables",
      ["table.read"]         = "View table status",
      ["table.update"]       = "Update table information",
    };

  /// <summary>Returns the description for a permission value, or null if unknown.</summary>
  public static string? GetDescription(string permissionValue) =>
    All.TryGetValue(permissionValue, out var desc) ? desc : null;
}
