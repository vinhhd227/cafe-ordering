export const navGroups = [
  {
    label: "Overview",
    items: [
      { label: "Dashboard", icon: "ph:squares-four-bold", to: { name: "dashboard" } },
    ],
  },
  {
    label: "Operations",
    items: [
      { label: "Orders",   icon: "ph:receipt-bold",     to: { name: "orders"   } },
      { label: "Menu",     icon: "ph:fork-knife-bold",  to: { name: "menu"     } },
      { label: "Products", icon: "ph:package-bold",     to: { name: "products" } },
    ],
  },
  {
    label: "People",
    items: [
      { label: "Staff",     icon: "ph:users-bold",        to: { name: "staff"    } },
      { label: "Customers", icon: "ph:address-book-bold", to: { name: "customer" } },
      { label: "Users",     icon: "ph:user-gear-bold",    to: { name: "user"     } },
    ],
  },
];
