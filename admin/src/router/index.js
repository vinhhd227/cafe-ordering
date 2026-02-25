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
        meta: {
          requiresAuth: true,
          role: "admin",
        },
      },
      {
        path: "orders",
        name: "orders",
        component: () => import("@/views/Orders.vue"),
        meta: {
          requiresAuth: true,
        },
      },
      {
        path: "menu",
        name: "menu",
        component: () => import("@/views/Menu.vue"),
        meta: {
          requiresAuth: true,
        },
      },
      {
        path: "products",
        meta: { requiresAuth: true },
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
        path: "staff",
        name: "staff",
        component: () => import("@/views/Staff.vue"),
        meta: {
          requiresAuth: true,
        },
      },
      {
        path: "customer",
        name: "customer",
        component: () => import("@/views/Customer.vue"),
        meta: {
          requiresAuth: true,
        },
      },
      {
        path: "user",
        name: "user",
        component: () => import("@/views/User.vue"),
        meta: {
          requiresAuth: true,
        },
      },
      {
        path: "profile",
        name: "profile",
        component: () => import("@/views/Profile.vue"),
        meta: {
          requiresAuth: true,
        },
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
];



const router = createRouter({
    history: createWebHistory(import.meta.env.BASE_URL),
    routes,
    scrollBehavior: () => ({top: 0}),
})

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

    // check role
    // if (to.meta.role && auth.user?.role !== to.meta.role) {
    //     return { name: 'error403' }
    // }
})

export default router;
