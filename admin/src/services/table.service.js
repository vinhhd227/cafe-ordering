import api from './axios'

export const listTables    = ()                     => api.get('/admin/tables')
export const createTable   = (number, code)          => api.post('/admin/tables', { number, code })
export const updateTable   = (id, { number, code })  => api.put(`/admin/tables/${id}`, { number, code })
export const toggleActive  = (id)                    => api.patch(`/admin/tables/${id}/toggle-active`, {})
export const deleteTable   = (id)                    => api.delete(`/admin/tables/${id}`)
export const markAvailable = (id)                    => api.put(`/tables/${id}/available`, {})
export const closeSession  = (sessionId)             => api.put(`/sessions/${sessionId}/close`, {})

export const getOrCreateSession = (tableId) =>
  api.get(`/tables/${tableId}/session`)

export const createCounterSession = () =>
  api.post('/admin/sessions/counter')
