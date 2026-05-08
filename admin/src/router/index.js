import {createRouter, createWebHistory} from 'vue-router'
import {useAuthStore} from '@/stores/auth'

const routes = [
  {
    path: "/policy",
    name: "policy",
    component: () => import("@/views/Policy.vue"),
  },
  {
    path: "/login",
    name: "login",
    component: () => import("@/views/auth/Login.vue"),
  },
  {
    path: "/register",
    name: "register",
    component: () => import("@/views/auth/Register.vue"),
  },
  {
    path: "/forgot-password",
    name: "forgotPassword",
    component: () => import("@/views/auth/ForgotPassword.vue"),
  },
  /* ================= ADMIN ================= */
  {
    path: "/",
    component: () => import("@/layout/LayoutShell.vue"),
    children: [
      {
        path: "",
        redirect: { name: "dashboard" },
      },
      {
        path: "dashboard",
        name: "dashboard",
        component: () => import("@/views/Dashboard.vue"),
        meta: { requiresAuth: true, section: "nav.groups.overview", pageTitle: "dashboard.title" },
      },
      {
        path: "reports",
        name: "reports",
        component: () => import("@/views/reports/Index.vue"),
        meta: { requiresAuth: true, requiredClaim: "report.read", section: "nav.groups.overview", pageTitle: "report.title" },
      },
      {
        path: "reports/daily",
        name: "reportsDaily",
        component: () => import("@/views/reports/Daily.vue"),
        meta: { requiresAuth: true, requiredClaim: "report.read", section: "nav.groups.overview", pageTitle: "report.daily.title" },
      },
      {
        path: "orders",
        meta: { requiresAuth: true, requiredClaim: "order.read", section: "nav.groups.operations" },
        children: [
          {
            path: "",
            name: "orders",
            component: () => import("@/views/orders/Kanban.vue"),
            meta: { pageTitle: "orders.title" },
          },
          {
            path: "list",
            name: "ordersList",
            component: () => import("@/views/orders/List.vue"),
            meta: { pageTitle: "orders.title" },
          },
          {
            path: "create",
            name: "ordersCreate",
            component: () => import("@/views/orders/Create.vue"),
            meta: { pageTitle: "orders.create.title" },
          },
          {
            path: ":id",
            name: "ordersDetail",
            component: () => import("@/views/orders/Detail.vue"),
          },
          {
            path: ":id/edit",
            name: "ordersEdit",
            component: () => import("@/views/orders/Edit.vue"),
            meta: { pageTitle: "orders.edit.title" },
          },
          {
            path: "create-manual",
            name: "ordersCreateManual",
            component: () => import("@/views/orders/CreateManual.vue"),
            meta: { requiresAuth: true, requiredClaim: "order.createManual", pageTitle: "orders.create.title" },
          },
          {
            path: ":id/edit-manual",
            name: "ordersEditManual",
            component: () => import("@/views/orders/EditManual.vue"),
            meta: { pageTitle: "orders.edit.title" },
          },
        ],
      },
      {
        path: "tables",
        meta: { requiresAuth: true, requiredClaim: "table.read", section: "nav.groups.operations" },
        children: [
          {
            path: "",
            name: "tables",
            component: () => import("@/views/tables/List.vue"),
          },
        ],
      },
      {
        path: "zones",
        meta: { requiresAuth: true, requiredClaim: "zone.read", section: "nav.groups.operations" },
        children: [
          {
            path: "",
            name: "zones",
            component: () => import("@/views/zones/List.vue"),
          },
          {
            path: ":id",
            name: "zoneDetail",
            component: () => import("@/views/zones/Detail.vue"),
          },
        ],
      },
      {
        path: "recipes",
        meta: { requiresAuth: true, requiredClaim: "recipe.read", section: "nav.groups.operations" },
        children: [
          {
            path: "",
            name: "recipes",
            component: () => import("@/views/recipes/List.vue"),
          },
          {
            path: ":id",
            name: "recipeDetail",
            component: () => import("@/views/recipes/Detail.vue"),
          },
          {
            path: ":id/edit",
            name: "recipeEdit",
            component: () => import("@/views/recipes/Edit.vue"),
            meta: { requiresAuth: true, requiredClaim: "recipe.update" },
          },
          {
            path: "new",
            name: "recipeNew",
            component: () => import("@/views/recipes/Edit.vue"),
            meta: { requiresAuth: true, requiredClaim: "recipe.create" },
          },
        ],
      },
      {
        path: "menu",
        name: "menu",
        component: () => import("@/views/Menu.vue"),
        meta: { requiresAuth: true, requiredClaim: "menu.read", section: "nav.groups.operations" },
      },
      {
        path: "products",
        meta: { requiresAuth: true, requiredClaim: "product.read", section: "nav.groups.operations" },
        children: [
          {
            path: "",
            name: "products",
            component: () => import("@/views/products/List.vue"),
            meta: { hideBottomNav: true },
          },
          {
            path: "categories/:id",
            name: "categoryProducts",
            component: () => import("@/views/products/CategoryProducts.vue"),
            meta: { hideBottomNav: true },
          },
          {
            path: "create",
            name: "productsCreate",
            component: () => import("@/views/products/Create.vue"),
          },
          {
            path: ":id",
            name: "productsDetail",
            component: () => import("@/views/products/Detail.vue"),
          },
        ],
      },
      {
        path: "categories",
        meta: { requiresAuth: true, requiredClaim: "product.read", section: "nav.groups.operations" },
        children: [
          {
            path: "",
            name: "categories",
            component: () => import("@/views/categories/List.vue"),
          },
          {
            path: "create",
            name: "categoriesCreate",
            component: () => import("@/views/categories/Create.vue"),
          },
          {
            path: ":id",
            name: "categoriesDetail",
            component: () => import("@/views/categories/Detail.vue"),
          },
        ],
      },
      {
        path: "staff",
        name: "staff",
        component: () => import("@/views/Staff.vue"),
        meta: { requiresAuth: true, requiredClaim: "staff.read", section: "nav.groups.people" },
      },
      {
        path: "customer",
        name: "customer",
        component: () => import("@/views/Customer.vue"),
        meta: { requiresAuth: true, section: "nav.groups.people" },
      },
      {
        path: "user",
        name: "user",
        component: () => import("@/views/users/List.vue"),
        meta: { requiresAuth: true, requiredClaim: "user.read", section: "nav.groups.people" },
      },
      {
        path: "user/:id",
        name: "userDetail",
        component: () => import("@/views/users/Detail.vue"),
        meta: { requiresAuth: true, requiredClaim: "user.read", section: "nav.groups.people" },
      },
      {
        path: "roles",
        name: "roles",
        component: () => import("@/views/roles/List.vue"),
        meta: { requiresAuth: true, adminOnly: true, section: "nav.groups.people" },
      },
      {
        path: "expenses",
        name: "expenses",
        component: () => import("@/views/expenses/List.vue"),
        meta: { requiresAuth: true, requiredClaim: "expense.read", section: "nav.groups.operations" },
      },
      {
        path: "promotions",
        name: "promotions",
        component: () => import("@/views/promotions/List.vue"),
        meta: { requiresAuth: true, requiredClaim: "promotion.read", section: "nav.groups.operations" },
      },
      {
        path: "utilities/menu-design",
        name: "menuDesign",
        component: () => import("@/views/utilities/MenuDesign.vue"),
        meta: { requiresAuth: true, requiredClaim: "utility.read", section: "nav.groups.utilities", pageTitle: "utilities.menuDesign.title" },
      },
      {
        path: "utilities/wifi-qr",
        name: "wifiQr",
        component: () => import("@/views/utilities/WifiQr.vue"),
        meta: { requiresAuth: true, requiredClaim: "utility.read", section: "nav.groups.utilities", pageTitle: "utilities.wifiQr.title" },
      },
      {
        path: "utilities/payment-qr",
        name: "paymentQr",
        component: () => import("@/views/utilities/PaymentQr.vue"),
        meta: { requiresAuth: true, requiredClaim: "utility.read", section: "nav.groups.utilities", pageTitle: "utilities.paymentQr.title" },
      },
      {
        path: "settings/printers",
        name: "printers",
        component: () => import("@/views/printers/List.vue"),
        meta: { requiresAuth: true, requiredClaim: "printer.read", section: "nav.groups.settings", pageTitle: "printers.title" },
      },
      {
        path: "settings/notification-configs",
        name: "notificationConfigs",
        component: () => import("@/views/settings/NotificationConfigs.vue"),
        meta: { requiresAuth: true, requiredClaim: "notification.config", section: "nav.groups.settings", pageTitle: "notificationConfigs.title" },
      },
      {
        path: "notifications",
        name: "notifications",
        component: () => import("@/views/notifications/List.vue"),
        meta: { requiresAuth: true, pageTitle: "notifications.list.title" },
      },
      {
        path: "profile",
        name: "profile",
        component: () => import("@/views/Profile.vue"),
        meta: { requiresAuth: true },
      },
      {
        path: "mobile-profile",
        name: "mobileProfile",
        component: () => import("@/views/MobileProfile.vue"),
        meta: { requiresAuth: true },
      },
    ],
  },

  /* ================= ERROR ================= */
  {
    path: "/error",
    children: [
      {
        path: "403",
        name: "error403",
        component: () => import("@/views/error/403.vue"),
      },
      {
        path: "404",
        name: "error404",
        component: () => import("@/views/error/404.vue"),
      },
    ],
  },
  {
    path: "/:pathMatch(.*)*",
    redirect: { name: "error404" },
  },
]

const router = createRouter({
    history: createWebHistory(import.meta.env.BASE_URL),
    routes,
    scrollBehavior: () => ({top: 0}),
})

const userCan = (user, claim) => {
  if (!user) return false
  if (user.roles.includes('Admin')) return true
  return user.permissions.includes(claim)
}

router.beforeEach(async (to) => {
    const auth = useAuthStore()

    if (!auth.hydrated) {
        await auth.hydrateFromRefresh()
    }

    // đã đăng nhập thì không vào login/register
    if (auth.isAuthenticated && (to.name === 'login' || to.name === 'register')) {
        return { name: 'dashboard' }
    }

    // cần đăng nhập
    if (to.meta.requiresAuth && !auth.isAuthenticated) {
        return { name: 'login' }
    }

    if (auth.isAuthenticated) {
        // admin only route
        if (to.meta.adminOnly && !auth.user?.roles?.includes('Admin')) {
            return { name: 'error403' }
        }

        // required claim (inherit từ parent route qua matched)
        const requiredClaim = to.matched
            .map(r => r.meta?.requiredClaim)
            .find(c => !!c)

        if (requiredClaim && !userCan(auth.user, requiredClaim)) {
            return { name: 'error403' }
        }
    }
})

export default router
