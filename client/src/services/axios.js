import axios from 'axios'
import { useAuthStore } from '@/stores/auth'

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  timeout: 30000,
  withCredentials: true,
  headers: { 'Content-Type': 'application/json' },
})

// Attach Bearer token
api.interceptors.request.use((config) => {
  const { accessToken } = useAuthStore()
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`
  }
  return config
})

// Unwrap data; handle 401 refresh; throw on error
api.interceptors.response.use(
  (res) => res.data,
  async (error) => {
    const original = error.config

    if (
      error.response?.status === 401 &&
      !original._retry &&
      !original.url?.includes('/auth/refresh') &&
      !original.url?.includes('/auth/login')
    ) {
      original._retry = true
      try {
        await useAuthStore().refreshToken()
        return api(original)
      } catch {
        return Promise.reject(error)
      }
    }

    throw error.response?.data ?? error
  }
)

export default api
