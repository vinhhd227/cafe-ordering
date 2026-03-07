// stores/auth.js
import { defineStore } from 'pinia'
import { login, refresh, register, logout as logoutRequest } from '@/services/auth.service.js'
import api from '@/services/axios'

const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'

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
  const raw = p[ROLE_CLAIM]
  const perms = p.permission
  return {
    id: p.sub ?? '',
    username: p.username ?? '',
    fullName: p.fullName ?? '',
    roles: Array.isArray(raw) ? raw : raw ? [raw] : [],
    permissions: Array.isArray(perms) ? perms : perms ? [perms] : [],
  }
}

export const useAuthStore = defineStore('auth', {
    state: () => ({
        accessToken: null,
        refreshToken: null,
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
            this.refreshToken = res.data.refreshToken
            localStorage.setItem('refreshToken', res.data.refreshToken)
            this.user = userFromToken(res.data.accessToken)
            localStorage.setItem('user', JSON.stringify(this.user))
            this.expiresAt = res.data.expiresAt
            this.scheduleTokenRefresh()
            return res
        },

        async doRefreshToken() {
            // Dedup: nếu đang refresh thì trả về promise hiện tại
            if (this.refreshing && this._refreshPromise) {
                return this._refreshPromise
            }

            // Set refreshing = true TRƯỚC khi tăng attempts để tránh race condition:
            // nếu 2 request cùng bị 401, cả 2 thấy refreshing = false đồng thời
            // → cả 2 cùng vào, attempts = 2 → logout ngay mà không thực sự refresh
            this.refreshing = true
            this.refreshAttempts += 1

            if (this.refreshAttempts > 1) {
                this.refreshing = false
                this.logout()
                throw new Error('Refresh attempt exceeded')
            }

            // Recover refresh token from localStorage on page reload
            if (!this.refreshToken) {
                this.refreshToken = localStorage.getItem('refreshToken')
            }

            // Snapshot token ngay lúc này để tránh bị overwrite
            // nếu có concurrent call nào đó thay đổi this.refreshToken
            const tokenToUse = this.refreshToken

            this._refreshPromise = (async () => {
                const res = await refresh(tokenToUse)
                this.accessToken = res.data.accessToken
                this.refreshToken = res.data.refreshToken
                localStorage.setItem('refreshToken', res.data.refreshToken)
                this.expiresAt = res.data.expiresAt
                this.user = userFromToken(res.data.accessToken)
                localStorage.setItem('user', JSON.stringify(this.user))
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
            this.refreshAttempts = 0  // clean slate on every page load
            this.hydrating = true
            this._hydratePromise = (async () => {
                try {
                    await this.doRefreshToken()
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
                this.doRefreshToken()
            }, delay)
        },

        logout() {
            this.accessToken = null
            this.refreshToken = null
            localStorage.removeItem('refreshToken')
            localStorage.removeItem('user')
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
