import {createRouter, createWebHistory} from 'vue-router'

const router = createRouter({
    history: createWebHistory(import.meta.env.BASE_URL),
    routes: [
        {
            path: '/admin/login',
            name: 'adminLogin',
            component: () => import('@/views/admin/Login.vue')
        },
        {
            path: '/admin/forgot-password',
            name: 'adminForgotPassword',
            component: () => import('@/views/admin/ForgotPassword.vue')
        },
        {
            path: '/admin/register',
            name: 'adminRegister',
            component: () => import('@/views/admin/Register.vue')
        },
        {
            path: '/login',
            name: 'customerLogin',
            component: () => import('@/views/client/Login.vue')
        },
        {
            path: '/register',
            name: 'customerRegister',
            component: () => import('@/views/client/Register.vue')
        },
        {
            path: '/admin',
            component: () => import('@/layouts/admin/Layout.vue'),
            children: [
                {
                    path: '',
                    redirect: {name: 'adminDashboard'},
                },
                {
                    path: 'dashboard',
                    name: 'adminDashboard',
                    component: () => import('@/views/admin/Dashboard.vue'),
                },
            ],
        },
        {
            path: '/order',
            name: 'Order',
            component: () => import('@/views/Order.vue')
        },
        {
            path: '/',
            redirect: {name: 'home'},
            component: () => import('@/layouts/client/Layout.vue'),
            children: [
                {
                    path: 'home',
                    name: 'home',
                    component: () => import('@/views/Home.vue'),
                },
                {
                    path: 'about',
                    name: 'about',
                    component: () => import('@/views/About.vue'),
                },
            ],
        },
        {
            path: '/policy',
            name: 'policy',
            component: () => import('@/views/Policy.vue')
        },
        {
            path: '/403',
            name: 'error403',
            component: () => import('@/views/Error403.vue')
        },
        {
            path: '/:pathMatch(.*)*',
            name: 'error404',
            component: () => import('@/views/Error404.vue')
        },
    ],
    scrollBehavior() {
        return {top: 0}
    },
})
export default router
