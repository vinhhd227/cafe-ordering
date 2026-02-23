import api from './axios'

/**
 * Đặt order mới trong một session
 * @param {{ sessionId: string, items: Array<{ productId, productName, unitPrice, quantity }> }} payload
 */
export const placeOrder = (payload) => api.post('/orders', payload)
