import api from './axios'

export const getOrders = ({
  status, paymentStatus, paymentMethod, orderNumber, minAmount, maxAmount, tableCode,
  page = 1, pageSize = 20, dateFrom, dateTo,
} = {}) =>
  api.get('/admin/orders', {
    params: { status, paymentStatus, paymentMethod, orderNumber, minAmount, maxAmount, tableCode,
      page, pageSize, dateFrom, dateTo },
  })

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

export const createOrder = (sessionId, items, guestCount = null) =>
  api.post('/orders', { sessionId, items, ...(guestCount != null && { guestCount }) })

export const applyPromotionAdmin = (orderId, code) =>
  api.post(`/admin/orders/${orderId}/promotions`, { code })

export const removePromotionAdmin = (orderId, promotionId) =>
  api.delete(`/admin/orders/${orderId}/promotions/${promotionId}`, {})

export const autoApplyPromotions = (orderId) =>
  api.post(`/admin/orders/${orderId}/promotions/auto`, {})

// Replace all items in a Pending order (also updates guestCount)
export const editOrderItems = (orderId, items, guestCount = null) =>
  api.put(`/admin/orders/${orderId}/items`, { items, guestCount })
