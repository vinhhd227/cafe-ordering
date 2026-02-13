import {createRouter, createWebHistory} from 'vue-router'
import {useAuthStore} from '@/stores/auth'

const routes = [{
    path: '/policy',
    name: 'policy',
    component: () => import('@/views/Policy.vue'),
},
    {
        path: '/login',
        name: 'login',
        component: () => import('@/views/Login.vue'),

    },
    {
        path: '/register',
        name: 'register',
        component: () => import('@/views/Register.vue'),

    },
    {
        path: '/forgot-password',
        name: 'forgotPassword',
        component: () => import('@/views/ForgotPassword.vue'),

    },
    /* ================= ADMIN ================= */
    {
        path: '/',
        component: () => import('@/layout/Layout.vue'),
        children: [
            {
                path: '',
                redirect: {name: 'dashboard'},
            },
            {
                path: 'dashboard',
                name: 'dashboard',
                component: () => import('@/views/Dashboard.vue'),
                meta: {
                    requiresAuth: true,
                    role: 'admin',
                },
            },

        ],
    },

    /* ================= ERROR ================= */
    {
        path: '/403',
        name: 'error403',
        component: () => import('@/views/Error403.vue'),
    },
    {
        path: '/:pathMatch(.*)*',
        name: 'error404',
        component: () => import('@/views/Error404.vue'),
    },
]



const router = createRouter({
    history: createWebHistory(import.meta.env.BASE_URL),
    routes,
    scrollBehavior: () => ({top: 0}),
})

router.beforeEach((to) => {
    const auth = useAuthStore()

    // cần đăng nhập
    if (to.meta.requiresAuth && !auth.isAuthenticated) {
        return { name: 'login' }
    }

    // check role
    if (to.meta.role && auth.user?.role !== to.meta.role) {
        return { name: 'error403' }
    }
})

export default router;