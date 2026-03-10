import api from './axios'

export const getPromotions = (params) => api.get('/admin/promotions', { params })
export const getPromotion  = (id)     => api.get(`/admin/promotions/${id}`)
export const createPromotion = (data) => api.post('/admin/promotions', data)
export const updatePromotion = (id, data) => api.put(`/admin/promotions/${id}`, data)
export const deletePromotion = (id)   => api.delete(`/admin/promotions/${id}`)
export const togglePromotion = (id, activate) => api.put(`/admin/promotions/${id}/toggle`, { activate })
export const validatePromotion = (code, orderAmount) =>
  api.get(`/promotions/validate/${code}`, { params: orderAmount != null ? { orderAmount } : undefined })
