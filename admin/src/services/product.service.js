import api from './axios'

// GET /api/products  → PagedResult<List<ProductSummaryDto>>
export const getProducts = (params) => api.get('/products', { params })

// GET /api/products/:id  → ProductDto
export const getProduct = (id) => api.get(`/products/${id}`)

// POST /api/products  → 201 { id }
export const createProduct = (payload) => api.post('/products', payload)

// PUT /api/products/:id  → 204 No Content
export const updateProduct = (id, payload) => api.put(`/products/${id}`, payload)
