import api from '@/services/axios'

export const getNotifications = (params) => api.get('/admin/notifications', { params })
export const getUnreadCount = () => api.get('/admin/notifications/unread-count')
export const markRead = (id) => api.put(`/admin/notifications/${id}/read`, {})
export const markAllRead = () => api.put('/admin/notifications/read-all', {})
export const deleteNotification = (id) => api.delete(`/admin/notifications/${id}`)
export const deleteReadNotifications = () => api.delete('/admin/notifications/read')
export const getNotificationConfigs = () => api.get('/admin/notification-configs')
export const updateNotificationConfig = (id, data) => api.put(`/admin/notification-configs/${id}`, data)
export const getNotificationSettings = () => api.get('/admin/notification-settings')
export const updateNotificationSettings = (data) => api.put('/admin/notification-settings', data)
