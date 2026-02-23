import api from './axios'

export const getMenu = () => api.get('/menu')
