import api from './axios'

export const listRecipes   = (params)    => api.get('/admin/recipes', { params })
export const getRecipe     = (id)        => api.get(`/admin/recipes/${id}`)
export const createRecipe  = (data)      => api.post('/admin/recipes', data)
export const updateRecipe  = (id, data)  => api.put(`/admin/recipes/${id}`, data)
export const deleteRecipe  = (id)        => api.delete(`/admin/recipes/${id}`)
export const chatRecipe    = (id, data)  => api.post(`/admin/recipes/${id}/chat`, data)
