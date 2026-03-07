/**
 * useOrderSse — kết nối SSE tới /api/admin/orders/stream
 *
 * Dùng fetch() thay vì EventSource vì endpoint cần Bearer token,
 * mà EventSource API của browser không hỗ trợ custom headers.
 *
 * @param {Object} callbacks
 * @param {Function} callbacks.onOrderCreated  - nhận OrderDto khi order mới được tạo
 * @param {Function} callbacks.onOrderUpdated  - nhận OrderDto khi order được cập nhật
 * @param {Function} [callbacks.onConnected]   - khi kết nối thành công
 * @param {Function} [callbacks.onDisconnected] - khi mất kết nối (trước khi reconnect)
 * @param {Function} [callbacks.onError]       - khi hết số lần retry
 */
export function useOrderSse({ onOrderCreated, onOrderUpdated, onConnected, onDisconnected, onError } = {}) {
  const authStore = useAuthStore()

  const connected = ref(false)
  const retryCount = ref(0)
  const MAX_RETRIES = 5
  const RETRY_DELAY_MS = 3000

  let abortController = null
  let retryTimeout = null
  let stopped = false

  const BASE_URL = import.meta.env.VITE_API_BASE_URL || '/api'

  // ── SSE text/event-stream parser ─────────────────────────────────
  // Format: "event: name\ndata: json\n\n"
  function parseEvents(chunk, buffer) {
    // Accumulate in buffer, split by double newline (event boundary)
    const text = buffer + chunk
    const events = []
    const parts = text.split('\n\n')
    // Phần cuối chưa hoàn chỉnh (chưa có \n\n) → giữ lại trong buffer
    const remaining = parts.pop()

    for (const part of parts) {
      if (!part.trim()) continue
      const lines = part.split('\n')
      let eventName = 'message'
      let data = ''
      for (const line of lines) {
        if (line.startsWith('event:')) {
          eventName = line.slice(6).trim()
        } else if (line.startsWith('data:')) {
          data = line.slice(5).trim()
        }
        // ignore ":" comments (heartbeat)
      }
      if (data) events.push({ eventName, data })
    }

    return { events, remaining }
  }

  // ── Core connect function ─────────────────────────────────────────
  async function connect() {
    if (stopped) return

    abortController = new AbortController()
    let buffer = ''

    try {
      const token = authStore.accessToken
      if (!token) {
        // Chưa có token — đợi rồi thử lại
        scheduleRetry()
        return
      }

      const response = await fetch(`${BASE_URL}/admin/orders/stream`, {
        headers: {
          Authorization: `Bearer ${token}`,
          Accept: 'text/event-stream',
        },
        signal: abortController.signal,
      })

      if (!response.ok) {
        if (response.status === 401) {
          // Token hết hạn — thử refresh rồi reconnect
          try {
            await authStore.doRefreshToken()
          } catch {
            onError?.('Authentication failed. Please refresh the page.')
            return
          }
          scheduleRetry(0) // reconnect ngay
          return
        }
        throw new Error(`SSE connect failed: ${response.status}`)
      }

      // Kết nối thành công
      connected.value = true
      retryCount.value = 0
      onConnected?.()

      const reader = response.body.pipeThrough(new TextDecoderStream()).getReader()

      // eslint-disable-next-line no-constant-condition
      while (true) {
        const { done, value } = await reader.read()
        if (done || stopped) break

        const { events, remaining } = parseEvents(value, buffer)
        buffer = remaining

        for (const { eventName, data } of events) {
          try {
            const order = JSON.parse(data)
            if (eventName === 'order_created') onOrderCreated?.(order)
            else if (eventName === 'order_updated') onOrderUpdated?.(order)
          } catch {
            // ignore malformed JSON
          }
        }
      }
    } catch (err) {
      if (err?.name === 'AbortError' || stopped) return
      console.warn('[SSE] Connection error:', err?.message)
    }

    // Kết nối kết thúc (server restart, network issue, ...)
    connected.value = false
    onDisconnected?.()

    scheduleRetry()
  }

  function scheduleRetry(delay = RETRY_DELAY_MS) {
    if (stopped) return
    retryCount.value += 1
    if (retryCount.value > MAX_RETRIES) {
      onError?.('Connection lost. Please refresh the page.')
      return
    }
    retryTimeout = setTimeout(connect, delay)
  }

  function disconnect() {
    stopped = true
    clearTimeout(retryTimeout)
    abortController?.abort()
    connected.value = false
  }

  // Khi access token thay đổi (refresh) → reconnect với token mới
  watch(() => authStore.accessToken, (newToken, oldToken) => {
    if (newToken && newToken !== oldToken && !stopped) {
      clearTimeout(retryTimeout)
      abortController?.abort()
      // reset retry count vì đây là token mới hợp lệ
      retryCount.value = 0
      setTimeout(connect, 100) // nhỏ delay để abort hoàn tất
    }
  })

  // Auto-connect khi composable được mount
  onMounted(() => {
    stopped = false
    connect()
  })

  onUnmounted(() => {
    disconnect()
  })

  return { connected, retryCount }
}
