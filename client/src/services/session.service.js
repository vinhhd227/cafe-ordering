import api from './axios'

export const getOrCreateSession = (tableId) => api.get(`/tables/${tableId}/session`)
export const getSessionSummary  = (sessionId) => api.get(`/sessions/${sessionId}/summary`)
