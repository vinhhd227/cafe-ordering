import axios from 'axios'
import { useAuthStore } from '@/stores/auth'

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  timeout: 10000,
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json',
  },
})

api.interceptors.request.use((config) => {
  const auth = useAuthStore()
  if (auth.accessToken) {
    config.headers = config.headers ?? {}
    config.headers.Authorization = `Bearer ${auth.accessToken}`
  }
  // Remove Content-Type for bodyless requests (PUT/PATCH with no data),
  // otherwise the backend returns 415 on token-refresh retries.
  if (config.data === undefined || config.data === null) {
    delete config.headers['Content-Type']
  }
  return config
})

api.interceptors.response.use(
    res => res,
    async error => {
      const auth = useAuthStore()
      const original = error.config

      if (
        error.response?.status === 401 &&
        !original._retry &&
        !original._skipAuthRefresh &&
        !original.url?.includes('/auth/refresh')
      ) {
        original._retry = true
        try {
          await auth.doRefreshToken()
          return api(original)
        } catch (refreshError) {
          return Promise.reject(refreshError)
        }
      }

      return Promise.reject(error)
    }
)


export default api
