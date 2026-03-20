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

/**
 * Validate a promo code and preview estimated discount (no DB write).
 * @param {string} code
 * @param {number|null} orderAmount
 */
export const validatePromotion = (code, orderAmount) =>
  api.get(`/promotions/validate/${code}`, {
    params: orderAmount != null ? { orderAmount } : undefined,
  })

export const getPublicPromotions = () => api.get('/promotions/public')

/**
 * Apply a promotion to an existing Pending order.
 * @param {number} orderId
 * @param {string} code
 * @param {string} sessionId
 */
export const applyPromotion = (orderId, code, sessionId) =>
  api.post(`/orders/${orderId}/promotions`, { code, sessionId })

/**
 * Remove a promotion from a Pending order.
 * @param {number} orderId
 * @param {number} promotionId
 * @param {string} sessionId
 */
export const removePromotion = (orderId, promotionId, sessionId) =>
  api.delete(`/orders/${orderId}/promotions/${promotionId}`, { data: { sessionId } })
