// Service Worker — Web Push Notifications
// Handles push events and notification clicks

self.addEventListener('push', (event) => {
  let data = {}
  try {
    data = event.data?.json() ?? {}
  } catch {
    data = { title: 'Thông báo', body: event.data?.text() ?? '' }
  }

  const title = data.title ?? 'Cafe Ordering'
  const options = {
    body: data.body ?? '',
    icon: '/apple-touch-icon-v2.png',
    badge: '/apple-touch-icon-v2.png',
    tag: data.url ?? 'default',       // group notifications by URL
    renotify: true,
    data: { url: data.url ?? '/' },
  }

  event.waitUntil(
    Promise.all([
      self.registration.showNotification(title, options),
      clients.matchAll({ type: 'window', includeUncontrolled: true }).then((windowClients) => {
        windowClients
          .filter(c => c.url.startsWith(self.registration.scope))
          .forEach(c => c.postMessage({ type: 'PUSH_NEW_ORDER' }))
      }),
    ])
  )
})

self.addEventListener('notificationclick', (event) => {
  event.notification.close()
  const path = event.notification.data?.url ?? '/'
  const fullUrl = self.registration.scope.replace(/\/$/, '') + path

  event.waitUntil(
    clients.matchAll({ type: 'window', includeUncontrolled: true }).then((windowClients) => {
      // Tìm tab admin đang mở
      const appClient = windowClients.find(c => c.url.startsWith(self.registration.scope))
      if (appClient) {
        appClient.focus()
        return appClient.navigate(fullUrl)
      }
      // Không có tab → mở tab mới
      return clients.openWindow(fullUrl)
    })
  )
})
