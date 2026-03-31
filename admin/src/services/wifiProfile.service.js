import api from './axios'

export const listWifiProfiles = () => api.get('/admin/wifi-profiles')

export const createWifiProfile = (payload) => api.post('/admin/wifi-profiles', payload)

export const deleteWifiProfile = (id) => api.delete(`/admin/wifi-profiles/${id}`)
