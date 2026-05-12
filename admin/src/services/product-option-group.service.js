import api from './axios'

export const getProductOptionGroups = () => api.get('/product-option-groups')
export const getProductOptionGroupById = (id) => api.get(`/product-option-groups/${id}`)
export const createProductOptionGroup = (payload) => api.post('/product-option-groups', payload)
export const updateProductOptionGroup = (id, payload) => api.put(`/product-option-groups/${id}`, payload)
export const deleteProductOptionGroup = (id) => api.delete(`/product-option-groups/${id}`)
export const toggleProductOptionGroupActive = (id) => api.patch(`/product-option-groups/${id}/toggle-active`, {})
export const assignOptionGroupsToProduct = (productId, groupIds) =>
  api.put(`/products/${productId}/assigned-option-groups`, { groupIds })
export const toggleOptionValueStock = (groupId, valueId) =>
  api.patch(`/product-option-groups/${groupId}/values/${valueId}/toggle-stock`, {})
export const linkGroupToProducts = (groupId, productIds) =>
  api.post(`/product-option-groups/${groupId}/link-products`, { productIds })
export const unlinkGroupFromProduct = (groupId, productId) =>
  api.delete(`/product-option-groups/${groupId}/linked-products/${productId}`)
