import api from './axios'

export const login = (payload) => api.post('/auth/login', payload)
export const register = (payload) => api.post('/auth/register', payload)
export const checkUsername = (payload) => api.post('/auth/check-username', payload)
export const refresh = () => api.post('/auth/refresh', null, { _skipAuthRefresh: true })
export const logout = () => api.post('/auth/logout', null, { _skipAuthRefresh: true })
