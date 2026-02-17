import api from './axios'

export const getCategory = () => api.get('/categories')
