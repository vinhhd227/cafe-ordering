import api from './axios'

export const listTables    = ()                     => api.get('/admin/tables')
export const createTable   = (code, zoneId = null)           => api.post('/admin/tables', { code, zoneId })
export const updateTable   = (id, { code, zoneId = null })   => api.put(`/admin/tables/${id}`, { code, zoneId })
export const toggleActive  = (id)                    => api.patch(`/admin/tables/${id}/toggle-active`, {})
export const deleteTable   = (id)                    => api.delete(`/admin/tables/${id}`)
export const markAvailable = (id)                    => api.put(`/tables/${id}/available`, {})
export const closeSession  = (sessionId)             => api.put(`/sessions/${sessionId}/close`, {})

export const getOrCreateSession = (tableId) =>
  api.get(`/tables/${tableId}/session`)
