import api from './axios'

/**
 * Đặt order mới trong một session
 * @param {{ sessionId: string, items: Array<{ productId, productName, unitPrice, quantity }> }} payload
 */
export const placeOrder = (payload) => api.post('/orders', payload)

/**
 * Update/add/remove an item in a Pending order.
 * quantity = 0 removes the item.
 * @param {number} orderId
 * @param {number} productId
 * @param {number} quantity
 * @param {string} sessionId - must match order's session for ownership check
 */
export const updateOrderItem = (orderId, productId, quantity, sessionId) =>
  api.put(`/orders/${orderId}/items/${productId}`, { quantity, sessionId })
