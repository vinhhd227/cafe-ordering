import api from './axios'

export const getOrders = (status) =>
  api.get('/admin/orders', { params: status ? { status } : {} })

export const updateOrderStatus = (orderId, status) =>
  api.patch(`/admin/orders/${orderId}/status`, { status })
