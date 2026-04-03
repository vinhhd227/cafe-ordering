import { defineStore } from 'pinia'
import {
  getNotifications,
  getUnreadCount,
  markRead as markReadApi,
  markAllRead as markAllReadApi,
} from '@/services/notification.service'

export const useNotificationStore = defineStore('notifications', () => {
  const items = ref([])
  const totalCount = ref(0)
  const unreadCount = ref(0)
  const loading = ref(false)
  const soundEnabled = ref(true)
  // Suppress SSE toast while the current user is in the middle of placing an order
  const creatingOrder = ref(false)

  async function fetchNotifications(page = 1, pageSize = 20) {
    loading.value = true
    try {
      const res = await getNotifications({ page, pageSize })
      items.value = res.data.items ?? []
      totalCount.value = res.data.totalCount ?? 0
      unreadCount.value = res.data.unreadCount ?? 0
    } catch {
      // silent fail — badge vẫn giữ giá trị cũ
    } finally {
      loading.value = false
    }
  }

  async function fetchUnreadCount() {
    try {
      const res = await getUnreadCount()
      unreadCount.value = res.data ?? 0
    } catch { /* silent */ }
  }

  async function markRead(id) {
    try {
      await markReadApi(id)
      const n = items.value.find(n => n.id === id)
      if (n) { n.isRead = true; n.readAt = new Date().toISOString() }
      if (unreadCount.value > 0) unreadCount.value--
    } catch { /* silent */ }
  }

  async function markAllRead() {
    try {
      await markAllReadApi()
      items.value.forEach(n => { n.isRead = true })
      unreadCount.value = 0
    } catch { /* silent */ }
  }

  // Gọi khi SSE báo có đơn mới — refetch để có notification mới nhất
  async function onNewOrder() {
    await fetchNotifications()
  }

  return {
    items,
    totalCount,
    unreadCount,
    loading,
    soundEnabled,
    creatingOrder,
    fetchNotifications,
    fetchUnreadCount,
    markRead,
    markAllRead,
    onNewOrder,
  }
})
