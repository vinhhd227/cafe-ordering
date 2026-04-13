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
      ["order.createManual"] = "Create manual orders (staff-entered)",
      ["order.read"]         = "View orders and details",
      ["order.update"]       = "Update order status",
      ["order.delete"]       = "Delete orders",

      // Categories
      ["category.create"]    = "Create new categories",
      ["category.read"]      = "View category list",
      ["category.update"]    = "Edit category information",
      ["category.delete"]    = "Delete categories",

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
      ["user.resetPassword"] = "Reset user password",

      // Tables
      ["table.create"]       = "Create tables",
      ["table.read"]         = "View table status",
      ["table.update"]       = "Update table information",

      // Expenses
      ["expense.read"]       = "View expense list and P&L summary",
      ["expense.create"]     = "Record new expenses",
      ["expense.update"]     = "Edit expense records",
      ["expense.delete"]     = "Delete expense records",
      
      // Promotions
      ["promotion.read"]     = "View promotion list",
      ["promotion.create"]   = "Create new promotions",
      ["promotion.update"]   = "Edit promotion information",
      ["promotion.delete"]   = "Delete promotions",

      // Notification config
      ["notification.config"] = "Manage notification types and retention settings",

      // Reports
      ["report.read"]        = "View reports and analytics",

      // App access (system-level — controls which app a role can log into)
      ["admin.access"]       = "Access the admin management site",
      ["customer.access"]    = "Access the customer ordering site",

      // Zone
      ["zone.create"]        = "Create zones",
      ["zone.read"]          = "View zone list",
      ["zone.update"]        = "Edit zone information",
      ["zone.delete"]        = "Delete zones",

      // Recipes
      ["recipe.read"]        = "View recipes and formulas",
      ["recipe.create"]      = "Create new recipes",
      ["recipe.update"]      = "Edit recipe information",
      ["recipe.delete"]      = "Delete recipes",

      // Utilities
      ["utility.read"]       = "Access utility tools (menu design, WiFi QR)",
      ["utility.create"]     = "Save WiFi profiles and other utility data",
      ["utility.update"]     = "Edit saved utility data",
      ["utility.delete"]     = "Delete saved utility data",
    };

  /// <summary>Returns the description for a permission value, or null if unknown.</summary>
  public static string? GetDescription(string permissionValue) =>
    All.TryGetValue(permissionValue, out var desc) ? desc : null;
}
