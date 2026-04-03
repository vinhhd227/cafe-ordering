import api from '@/services/axios'

export const getVapidPublicKey = () => api.get('/admin/push/vapid-public-key')

export const subscribePush = (data) => api.post('/admin/push/subscribe', data)

export const unsubscribePush = (data) => api.delete('/admin/push/subscribe', { data })
