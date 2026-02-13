// stores/auth.js
import { defineStore } from 'pinia'
import { login, refresh, register, logout as logoutRequest } from '@/services/auth.service.js'
import api from '@/services/axios'

export const useAuthStore = defineStore('auth', {
    state: () => ({
        accessToken: null,
        user: null,
        expiresAt: null,
        refreshTimer: null,
        refreshAttempts: 0,
        hydrated: false,
        hydrating: false,
        refreshing: false,
    }),
    getters: {
        isAuthenticated: (state) => !!state.accessToken,
    },

    actions: {
        async login(payload) {
            const res = await login(payload)
            this.accessToken = res.data.accessToken
            this.user = {
                firstName: res.data.firstName,
                lastName: res.data.lastName,
                roles: res.data.roles,
            }
            this.expiresAt = res.data.expiresAt
            this.scheduleTokenRefresh()
            return res
        },

        async refreshToken() {
            if (this.refreshing && this._refreshPromise) {
                return this._refreshPromise
            }
            this.refreshAttempts += 1
            if (this.refreshAttempts > 1) {
                this.logout()
                throw new Error('Refresh attempt exceeded')
            }

            this.refreshing = true
            this._refreshPromise = (async () => {
                const res = await refresh()
                this.accessToken = res.data.access_token
                this.expiresAt = res.data.expires_at
                if (res.data.first_name || res.data.last_name || res.data.roles) {
                    this.user = {
                        firstName: res.data.first_name,
                        lastName: res.data.last_name,
                        roles: res.data.roles ?? this.user?.roles,
                    }
                }
                this.scheduleTokenRefresh()
                this.refreshAttempts = 0
                return res
            })()
                .catch((err) => {
                    this.clearTokenRefresh()
                    throw err
                })
                .finally(() => {
                    this.refreshing = false
                    this._refreshPromise = null
                })

            return this._refreshPromise
        },

        async hydrateFromRefresh() {
            if (this.hydrated) return
            if (this.hydrating && this._hydratePromise) {
                return this._hydratePromise
            }
            this.hydrating = true
            this._hydratePromise = (async () => {
                try {
                    await this.refreshToken()
                } catch (err) {
                    this.accessToken = null
                } finally {
                    this.hydrated = true
                    this.hydrating = false
                }
            })()
            return this._hydratePromise
        },

        clearTokenRefresh() {
            if (this.refreshTimer) {
                clearTimeout(this.refreshTimer)
                this.refreshTimer = null
            }
        },

        scheduleTokenRefresh() {
            this.clearTokenRefresh()
            if (!this.expiresAt) return
            const expiresAtMs = new Date(this.expiresAt).getTime()
            const refreshAtMs = Math.max(expiresAtMs - 30_000, Date.now() + 1_000)
            const delay = refreshAtMs - Date.now()
            this.refreshTimer = setTimeout(() => {
                this.refreshToken()
            }, delay)
        },

        logout() {
            this.accessToken = null
            this.user = null
            this.expiresAt = null
            this.clearTokenRefresh()
            this.refreshAttempts = 0
            this.refreshing = false
            this._refreshPromise = null
            logoutRequest()
        },
    },
})
