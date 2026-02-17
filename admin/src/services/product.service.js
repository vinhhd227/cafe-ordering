import api from './axios'

export const getProducts = (params) => api.get('/products', { params })
