import api from './axios'

export const getRoles = (params) =>
  api.get('/admin/roles', { params })

export const getRoleById = (id) =>
  api.get(`/admin/roles/${id}`)

export const createRole = (payload) =>
  api.post('/admin/roles', payload)

export const updateRole = (id, payload) =>
  api.put(`/admin/roles/${id}`, payload)

export const deleteRole = (id) =>
  api.delete(`/admin/roles/${id}`)

export const getRolePermissions = (id) =>
  api.get(`/admin/roles/${id}/permissions`)

export const setRolePermissions = (id, permissions) =>
  api.put(`/admin/roles/${id}/permissions`, { permissions })
