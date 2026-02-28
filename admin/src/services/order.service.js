import api from './axios'

export const getOrders = (status) =>
  api.get('/admin/orders', { params: status ? { status } : {} })

export const updateOrderStatus = (orderId, status) =>
  api.patch(`/admin/orders/${orderId}/status`, { status })

export const getOrderById = (orderId) =>
  api.get(`/admin/orders/${orderId}`)

export const updatePayment = (orderId, paymentStatus, paymentMethod, amountReceived, tipAmount) =>
  api.patch(`/admin/orders/${orderId}/payment`, { paymentStatus, paymentMethod, amountReceived, tipAmount })

export const mergeOrders = (primaryOrderId, secondaryOrderIds) =>
  api.post('/admin/orders/merge', { primaryOrderId, secondaryOrderIds })

export const splitOrder = (orderId, items) =>
  api.post(`/admin/orders/${orderId}/split`, { items })

// quantity = 0 removes the item; quantity > 0 adds/updates
export const updateOrderItem = (orderId, productId, quantity) =>
  api.put(`/admin/orders/${orderId}/items/${productId}`, { quantity })

export const createOrder = (sessionId, items) =>
  api.post('/orders', { sessionId, items })
