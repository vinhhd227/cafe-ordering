// stores/auth.js
import { defineStore } from 'pinia'
import {login, register} from '@/services/auth.service.js'

export const useAuthStore = defineStore('auth', {
    state: () => ({
        accessToken: null,
        user: null,
    }),
    getters: {
        isAuthenticated: (state) => !!state.accessToken,
    },

    actions: {
        async login(payload) {
            const res = await login(payload)
            this.accessToken = res.data.accessToken
            this.user = res.data.user
            return res
        },

        async refreshToken() {
            const res = await api.post('/auth/refresh')
            this.accessToken = res.data.accessToken
        },

        logout() {
            this.accessToken = null
            this.user = null
            api.post('/auth/logout')
        },
    },
})
