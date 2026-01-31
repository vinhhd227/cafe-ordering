import api from './axios'

export const login = (payload) => api.post('/auth/login', payload)
export const register = (payload) => api.post('/auth/register', payload)
export const checkUsername = (payload) => api.post('/auth/check-username', payload)
