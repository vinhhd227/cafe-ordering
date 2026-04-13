export const navGroups = [
  {
    labelKey: "nav.groups.overview",
    items: [
      {
        labelKey: "nav.dashboard",
        icon: "ph:squares-four-bold",
        to: { name: "dashboard" },
      },
      {
        labelKey: "nav.reports",
        icon: "ph:chart-bar-bold",
        requiredClaim: "report.read",
        matchNames: ["reports", "reportsDaily"],
        children: [
          { labelKey: "nav.reportsSummary", icon: "ph:chart-line-bold", to: { name: "reports" } },
          { labelKey: "nav.reportsDaily",   icon: "ph:calendar-check-bold", to: { name: "reportsDaily" } },
        ],
      },
    ],
  },
  {
    labelKey: "nav.groups.operations",
    items: [
      {
        labelKey: "nav.orders",
        icon: "ph:receipt-bold",
        requiredClaim: "order.read",
        matchPrefix: "orders",
        children: [
          { labelKey: "nav.ordersKanban", icon: "ph:kanban-bold", to: { name: "orders" } },
          { labelKey: "nav.ordersList",   icon: "ph:list-bold",   to: { name: "ordersList" } },
        ],
      },
      {
        labelKey: "nav.expenses",
        icon: "ph:shopping-cart-bold",
        to: { name: "expenses" },
        requiredClaim: "expense.read",
      },
      {
        labelKey: "nav.tables",
        icon: "ic:round-table-bar",
        to: { name: "tables" },
        requiredClaim: "table.read",
      },
      {
        labelKey: "nav.zones",
        icon: "ph:map-pin-bold",
        to: { name: "zones" },
        requiredClaim: "zone.read",
      },
      {
        labelKey: "nav.menu",
        icon: "ph:fork-knife-bold",
        to: { name: "menu" },
        requiredClaim: "menu.read",
      },
      {
        labelKey: "nav.products",
        icon: "ph:package-bold",
        to: { name: "products" },
        requiredClaim: "product.read",
      },
      {
        labelKey: "nav.recipes",
        icon: "ph:notebook-bold",
        to: { name: "recipes" },
        requiredClaim: "recipe.read",
      },
      {
        labelKey: "nav.categories",
        icon: "ph:tag-bold",
        to: { name: "categories" },
        requiredClaim: "category.read",
      },
      {
        labelKey: "nav.promotions",
        icon: "ph:percent-bold",
        to: { name: "promotions" },
        requiredClaim: "promotion.read",
      },
    ],
  },
  {
    labelKey: "nav.groups.utilities",
    items: [
      {
        labelKey: "nav.menuDesign",
        icon: "ph:book-open-bold",
        to: { name: "menuDesign" },
        requiredClaim: "utility.read",
      },
      {
        labelKey: "nav.wifiQr",
        icon: "ph:wifi-high-bold",
        to: { name: "wifiQr" },
        requiredClaim: "utility.read",
      },
      {
        labelKey: "nav.paymentQr",
        icon: "ph:qr-code-bold",
        to: { name: "paymentQr" },
        requiredClaim: "utility.read",
      },
    ],
  },
  {
    labelKey: "nav.groups.people",
    items: [
      {
        labelKey: "nav.staff",
        icon: "ph:users-bold",
        to: { name: "staff" },
        requiredClaim: "staff.read",
      },
      {
        labelKey: "nav.customers",
        icon: "ph:address-book-bold",
        to: { name: "customer" },
        requiredClaim: "customer.read"
      },
      {
        labelKey: "nav.users",
        icon: "ph:user-gear-bold",
        to: { name: "user" },
        requiredClaim: "user.read",
      },
      {
        labelKey: "nav.roles",
        icon: "ph:shield-bold",
        to: { name: "roles" },
        adminOnly: true,
      },
    ],
  },
  {
    labelKey: "nav.groups.settings",
    items: [
      {
        labelKey: "nav.notificationConfigs",
        icon: "ph:bell-ringing-bold",
        to: { name: "notificationConfigs" },
        requiredClaim: "notification.config",
      },
    ],
  },
];
