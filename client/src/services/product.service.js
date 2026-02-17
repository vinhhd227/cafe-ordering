import api from './axios'

export const getProducts = () => api.get('/categories')
export const getMenu = () => api.get('/menu')
export const getProduct = (id) => api.get(`/product/${id}`)
