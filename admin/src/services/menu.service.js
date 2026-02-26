import api from './axios'

export const getAdminMenu = () => api.get('/admin/menu')
