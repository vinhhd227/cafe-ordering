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
    component: () => import("@/layout/Layout.vue"),
    children: [
      {
        path: "",
        redirect: { name: "dashboard" },
      },
      {
        path: "dashboard",
        name: "dashboard",
        component: () => import("@/views/Dashboard.vue"),
        meta: { requiresAuth: true },
      },
      {
        path: "orders",
        meta: { requiresAuth: true, requiredClaim: "order.read" },
        children: [
          {
            path: "",
            name: "orders",
            component: () => import("@/views/orders/Kanban.vue"),
          },
          {
            path: "list",
            name: "ordersList",
            component: () => import("@/views/orders/List.vue"),
          },
          {
            path: "create",
            name: "ordersCreate",
            component: () => import("@/views/orders/Create.vue"),
          },
          {
            path: ":id",
            name: "ordersDetail",
            component: () => import("@/views/orders/Detail.vue"),
          },
        ],
      },
      {
        path: "tables",
        meta: { requiresAuth: true, requiredClaim: "table.read" },
        children: [
          {
            path: "",
            name: "tables",
            component: () => import("@/views/tables/List.vue"),
          },
        ],
      },
      {
        path: "menu",
        name: "menu",
        component: () => import("@/views/Menu.vue"),
        meta: { requiresAuth: true, requiredClaim: "menu.read" },
      },
      {
        path: "products",
        meta: { requiresAuth: true, requiredClaim: "product.read" },
        children: [
          {
            path: "",
            name: "products",
            component: () => import("@/views/products/List.vue"),
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
        meta: { requiresAuth: true, requiredClaim: "product.read" },
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
        meta: { requiresAuth: true, requiredClaim: "staff.read" },
      },
      {
        path: "customer",
        name: "customer",
        component: () => import("@/views/Customer.vue"),
        meta: { requiresAuth: true },
      },
      {
        path: "user",
        name: "user",
        component: () => import("@/views/users/List.vue"),
        meta: { requiresAuth: true, requiredClaim: "user.read" },
      },
      {
        path: "user/:id",
        name: "userDetail",
        component: () => import("@/views/users/Detail.vue"),
        meta: { requiresAuth: true, requiredClaim: "user.read" },
      },
      {
        path: "roles",
        name: "roles",
        component: () => import("@/views/roles/List.vue"),
        meta: { requiresAuth: true, adminOnly: true },
      },
      {
        path: "profile",
        name: "profile",
        component: () => import("@/views/Profile.vue"),
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
