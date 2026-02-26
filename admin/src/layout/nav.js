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
      { label: "Orders",     icon: "ph:receipt-bold",     to: { name: "orders"     }, requiredClaim: "order.read"   },
      { label: "Menu",       icon: "ph:fork-knife-bold",  to: { name: "menu"       }, requiredClaim: "menu.read"    },
      { label: "Products",   icon: "ph:package-bold",     to: { name: "products"   }, requiredClaim: "product.read" },
      { label: "Categories", icon: "ph:tag-bold",         to: { name: "categories" }, requiredClaim: "product.read" },
    ],
  },
  {
    label: "People",
    items: [
      { label: "Staff",     icon: "ph:users-bold",        to: { name: "staff"    }, requiredClaim: "staff.read" },
      { label: "Customers", icon: "ph:address-book-bold", to: { name: "customer" }                              },
      { label: "Users",     icon: "ph:user-gear-bold",    to: { name: "user"     }, requiredClaim: "user.read"  },
      { label: "Roles",     icon: "ph:shield-bold",       to: { name: "roles"    }, adminOnly: true             },
    ],
  },
]
