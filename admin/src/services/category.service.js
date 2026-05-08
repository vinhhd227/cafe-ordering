import api from './axios'

export const getCategory = () => api.get('/categories')
export const getCategoryById = (id) => api.get(`/categories/${id}`)
export const createCategory = (payload) => api.post('/categories', payload)
export const updateCategory = (id, payload) => api.put(`/categories/${id}`, payload)

// PATCH /api/categories/:id/toggle-active  → 204 No Content
export const toggleCategoryActive = (id) => api.patch(`/categories/${id}/toggle-active`, {})

// PUT /api/categories/reorder  → 204 No Content
export const reorderCategories = (ids) => api.put('/categories/reorder', { ids })

// DELETE /api/categories/:id  → 204 No Content
export const deleteCategory = (id) => api.delete(`/categories/${id}`)
