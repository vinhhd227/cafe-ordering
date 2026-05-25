import api from './axios'

// GET /api/products  â†’ PagedResult<List<ProductSummaryDto>>
export const getProducts = (params) => api.get('/products', { params })

// GET /api/products/:id  â†’ ProductDto
export const getProduct = (id) => api.get(`/products/${id}`)

// POST /api/products  â†’ 201 { id }
export const createProduct = (payload) => api.post('/products', payload)

// PUT /api/products/:id  â†’ 204 No Content
export const updateProduct = (id, payload) => api.put(`/products/${id}`, payload)

// PATCH /api/products/:id/toggle-active  → 204 No Content
export const toggleProductActive = (id) => api.patch(`/products/${id}/toggle-active`, {})

// PATCH /api/products/:id/unassign-category  → 204 No Content
export const unassignCategory = (id) => api.patch(`/products/${id}/unassign-category`, {})

// PATCH /api/products/:id/assign-category  → 204 No Content
export const assignCategory = (id, categoryId) => api.patch(`/products/${id}/assign-category`, { categoryId })

// PUT /api/products/:id/variant-groups  â†’ 204 No Content
export const replaceVariantGroups = (id, payload) => api.put(`/products/${id}/variant-groups`, payload)

// GET /api/admin/products/tree  â†’ List<ProductTreeCategoryDto>
export const getProductTree = () => api.get('/admin/products/tree')

export const getProductVariants = (id) => api.get(`/products/${id}/variants`)

export const replaceProductVariants = (id, payload) => api.put(`/products/${id}/variants`, payload)
