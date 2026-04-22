import api from './axios'

export const getUsers = (params) =>
  api.get('/admin/users', { params })

export const getUserById = (id) =>
  api.get(`/admin/users/${id}`)

export const createUser = (payload) =>
  api.post('/staff/accounts', payload)

export const updateUser = (id, payload) =>
  api.put(`/admin/users/${id}`, payload)

export const activateUser = (id) =>
  api.put(`/admin/users/${id}/activate`, {})

export const deactivateUser = (id) =>
  api.put(`/admin/users/${id}/deactivate`, {})

export const changeUserRole = (id, role) =>
  api.put(`/admin/users/${id}/roles`, { role })

export const resetUserPassword = (id) =>
  api.post(`/admin/users/${id}/reset-password`, {})

export const uploadAvatar = (id, file) => {
  const form = new FormData()
  form.append('file', file)
  return api.post(`/admin/users/${id}/avatar`, form, {
    headers: { 'Content-Type': 'multipart/form-data' },
  })
}
