import { defineStore } from 'pinia'

export const useNotificationStore = defineStore('notifications', () => {
  const items = ref([])
  const soundEnabled = ref(true)
  // Suppress SSE toast while the current user is in the middle of placing an order
  const creatingOrder = ref(false)

  const unreadCount = computed(() => items.value.filter(n => !n.read).length)

  function add(order) {
    // Tránh duplicate
    if (items.value.some(n => n.order.id === order.id)) return
    items.value.unshift({
      id: `noti-${order.id}-${Date.now()}`,
      order,
      read: false,
      at: new Date(),
    })
    // Giữ tối đa 50 notifications
    if (items.value.length > 50) items.value.length = 50
  }

  function markRead(orderId) {
    const n = items.value.find(n => n.order.id === orderId)
    if (n) n.read = true
  }

  function markAllRead() {
    items.value.forEach(n => { n.read = true })
  }

  function clear() {
    items.value = []
  }

  return { items, soundEnabled, unreadCount, creatingOrder, add, markRead, markAllRead, clear }
})
