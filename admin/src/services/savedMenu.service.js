import api from './axios'

export const listSavedMenus = () => api.get('/admin/saved-menus')

export const createSavedMenu = (payload) => api.post('/admin/saved-menus', payload)

export const deleteSavedMenu = (id) => api.delete(`/admin/saved-menus/${id}`)
