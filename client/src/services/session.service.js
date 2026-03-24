import api from './axios'

export const getOrCreateSession = (tableId, token) =>
  api.get(`/tables/${tableId}/session`, { params: token ? { token } : {} })
export const getSessionSummary  = (sessionId) => api.get(`/sessions/${sessionId}/summary`)
