import api from './axios'

export const listZones      = ()              => api.get('/admin/zones')
export const getZone        = (id)            => api.get(`/admin/zones/${id}`)
export const createZone     = (data)          => api.post('/admin/zones', data)
export const updateZone     = (id, data)      => api.put(`/admin/zones/${id}`, data)
export const deleteZone     = (id)            => api.delete(`/admin/zones/${id}`)
export const activateZone   = (id)            => api.put(`/admin/zones/${id}/activate`, {})
export const deactivateZone = (id)            => api.put(`/admin/zones/${id}/deactivate`, {})
