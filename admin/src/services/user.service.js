import api from './axios'

export const getUsers = (params) =>
  api.get('/admin/users', { params })

export const getUserById = (id) =>
  api.get(`/admin/users/${id}`)

// Delegates to existing POST /api/staff/accounts
export const createUser = (payload) =>
  api.post('/staff/accounts', payload)

export const updateUser = (id, payload) =>
  api.put(`/admin/users/${id}`, payload)

export const activateUser = (id) =>
  api.put(`/admin/users/${id}/activate`)

// Delegates to existing PUT /api/staff/accounts/{id}/deactivate
export const deactivateUser = (id) =>
  api.put(`/staff/accounts/${id}/deactivate`)

export const changeUserRole = (id, role) =>
  api.put(`/admin/users/${id}/roles`, { role })
