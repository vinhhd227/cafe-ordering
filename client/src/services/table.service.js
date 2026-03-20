import api from './axios'

export const getPublicTables = () => api.get('/tables')
