// stores/auth.js
import { defineStore } from 'pinia'
import { login as loginApi, logout as logoutApi, refreshToken as refreshTokenApi } from '@/services/auth.service.js'

const parseJwt = (token) => {
    try {
        const base64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')
        return JSON.parse(decodeURIComponent(escape(atob(base64))))
    } catch {
        return {}
    }
}

const userFromToken = (token) => {
    const p = parseJwt(token)
    return {
        id: p.sub ?? '',
        username: p.username ?? '',
        fullName: p.fullName ?? '',
    }
}

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
            const res = await loginApi(payload)
            this.accessToken = res.accessToken
            this.user = userFromToken(res.accessToken)
            return res
        },

        async refreshToken() {
            const res = await refreshTokenApi()
            this.accessToken = res.accessToken
            this.user = userFromToken(res.accessToken)
        },

        async hydrateFromRefresh() {
            try {
                await this.refreshToken()
            } catch {
                this.accessToken = null
                this.user = null
            }
        },

        async logout() {
            try { await logoutApi() } catch { /* ignore */ }
            this.accessToken = null
            this.user = null
        },
    },
})
