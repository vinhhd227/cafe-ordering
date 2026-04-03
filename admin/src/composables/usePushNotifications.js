import { getVapidPublicKey, subscribePush, unsubscribePush } from '@/services/push.service'

const isSupported = typeof window !== 'undefined'
  && 'serviceWorker' in navigator
  && 'PushManager' in window
  && 'Notification' in window

function urlBase64ToUint8Array(base64String) {
  const padding = '='.repeat((4 - (base64String.length % 4)) % 4)
  const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/')
  const rawData = atob(base64)
  return Uint8Array.from([...rawData].map((c) => c.charCodeAt(0)))
}

export function usePushNotifications() {
  const permission = ref(isSupported ? Notification.permission : 'denied')
  const isSubscribed = ref(false)
  const loading = ref(false)

  const checkSubscription = async () => {
    if (!isSupported) return
    try {
      const reg = await navigator.serviceWorker.ready
      const sub = await reg.pushManager.getSubscription()
      isSubscribed.value = !!sub
    } catch {
      isSubscribed.value = false
    }
  }

  const subscribe = async () => {
    if (!isSupported || loading.value) return
    loading.value = true
    try {
      // Đăng ký Service Worker nếu chưa có
      await navigator.serviceWorker.register('/sw.js')
      const reg = await navigator.serviceWorker.ready

      // Lấy VAPID public key từ server
      const { data: vapidKey } = await getVapidPublicKey()

      // Subscribe push
      const sub = await reg.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: urlBase64ToUint8Array(vapidKey),
      })

      const json = sub.toJSON()
      await subscribePush({
        endpoint: sub.endpoint,
        p256dh: json.keys.p256dh,
        auth: json.keys.auth,
      })

      isSubscribed.value = true
      permission.value = Notification.permission
    } finally {
      loading.value = false
    }
  }

  const requestAndSubscribe = async () => {
    if (!isSupported) return false
    const result = await Notification.requestPermission()
    permission.value = result
    if (result === 'granted') {
      await subscribe()
      return true
    }
    return false
  }

  const unsubscribe = async () => {
    if (!isSupported || loading.value) return
    loading.value = true
    try {
      const reg = await navigator.serviceWorker.ready
      const sub = await reg.pushManager.getSubscription()
      if (sub) {
        await unsubscribePush({ endpoint: sub.endpoint })
        await sub.unsubscribe()
      }
      isSubscribed.value = false
    } finally {
      loading.value = false
    }
  }

  const toggle = async () => {
    if (isSubscribed.value) {
      await unsubscribe()
    } else {
      await requestAndSubscribe()
    }
  }

  const playChime = () => {
    try {
      const ctx = new AudioContext()
      // 3 nốt C5-E5-G5 — dễ nhận ra, khác biệt với tiếng beep đơn của SSE
      const notes = [523.25, 659.25, 783.99]
      notes.forEach((freq, i) => {
        const osc = ctx.createOscillator()
        const gain = ctx.createGain()
        osc.connect(gain)
        gain.connect(ctx.destination)
        osc.type = 'sine'
        osc.frequency.value = freq
        const t = ctx.currentTime + i * 0.18
        gain.gain.setValueAtTime(0, t)
        gain.gain.linearRampToValueAtTime(0.18, t + 0.01)
        gain.gain.exponentialRampToValueAtTime(0.001, t + 0.45)
        osc.start(t)
        osc.stop(t + 0.45)
      })
    } catch { /* AudioContext unavailable */ }
  }

  onMounted(async () => {
    if (!isSupported) return
    // Đăng ký SW ngầm khi app load
    try { await navigator.serviceWorker.register('/sw.js') } catch { /* ignore */ }
    await checkSubscription()
    // Lắng nghe SW postMessage để play chime khi có push
    navigator.serviceWorker.addEventListener('message', (event) => {
      if (event.data?.type === 'PUSH_NEW_ORDER') playChime()
    })
  })

  return {
    isSupported,
    permission,
    isSubscribed,
    loading,
    toggle,
    requestAndSubscribe,
    unsubscribe,
  }
}
