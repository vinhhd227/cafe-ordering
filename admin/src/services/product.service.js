import api from './axios'

// GET /api/products  → PagedResult<List<ProductSummaryDto>>
export const getProducts = (params) => api.get('/products', { params })

// GET /api/products/:id  → ProductDto
export const getProduct = (id) => api.get(`/products/${id}`)

// POST /api/products  → 201 { id }
export const createProduct = (payload) => api.post('/products', payload)

// PUT /api/products/:id  → 204 No Content
export const updateProduct = (id, payload) => api.put(`/products/${id}`, payload)

// PATCH /api/products/:id/toggle-active  → 204 No Content
export const toggleProductActive = (id) => api.patch(`/products/${id}/toggle-active`, {})

// PUT /api/products/:id/option-groups  → 204 No Content
export const replaceAttributeGroups = (id, payload) => api.put(`/products/${id}/option-groups`, payload)

// GET /api/admin/products/tree  → List<ProductTreeCategoryDto>
export const getProductTree = () => api.get('/admin/products/tree')
