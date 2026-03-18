<script setup>
const { t } = useI18n()
const router = useRouter()
const store = useNotificationStore()
const toast = useToast()
const overlay = ref(null)

// ── Sound ──────────────────────────────────────────────────────────
function playBeep() {
  if (!store.soundEnabled) return
  try {
    const ctx = new AudioContext()
    const osc = ctx.createOscillator()
    const gain = ctx.createGain()
    osc.connect(gain)
    gain.connect(ctx.destination)
    osc.type = 'sine'
    osc.frequency.value = 880
    gain.gain.value = 0.08
    osc.start()
    gain.gain.exponentialRampToValueAtTime(0.0001, ctx.currentTime + 0.4)
    osc.stop(ctx.currentTime + 0.4)
  } catch { /* AudioContext unavailable */ }
}

// ── SSE ────────────────────────────────────────────────────────────
const { connected: sseConnected } = useOrderSse({
  onOrderCreated(order) {
    store.add(order)
    if (store.creatingOrder) return // suppress toast when current user just placed this order
    playBeep()
    toast.add({
      severity: 'info',
      summary: t('notifications.newOrder'),
      detail: buildDetail(order),
      life: 6000,
    })
  },
})

// ── Helpers ────────────────────────────────────────────────────────
function buildDetail(order) {
  const parts = []
  if (order.tableCode) parts.push(`${t('notifications.table')} ${order.tableCode}`)
  if (order.items?.length) parts.push(t('notifications.itemCount', { n: order.items.length }))
  const amount = order.finalAmount ?? order.totalAmount
  if (amount) parts.push(new Intl.NumberFormat('vi-VN').format(amount) + 'đ')
  return parts.join(' · ')
}

function formatTime(at) {
  return new Date(at).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })
}

// ── Actions ────────────────────────────────────────────────────────
function toggleOverlay(event) {
  overlay.value?.toggle(event)
}

function openOrder(orderId) {
  store.markRead(orderId)
  overlay.value?.hide()
  router.push({ name: 'ordersDetail', params: { id: orderId } })
}
</script>

<template>
  <div class="tw:relative">
    <!-- Bell button -->
    <button
      type="button"
      class="tw:relative tw:flex tw:h-9 tw:w-9 tw:items-center tw:justify-center tw:rounded-lg tw:transition-colors hover:tw:bg-black/5"
      :title="t('notifications.title')"
      @click="toggleOverlay"
    >
      <iconify
        :icon="store.unreadCount > 0 ? 'ph:bell-ringing-bold' : 'ph:bell-bold'"
        class="tw:text-lg"
        :class="store.unreadCount > 0 ? 'tw:text-emerald-400' : ''"
      />
      <!-- Badge -->
      <span
        v-if="store.unreadCount > 0"
        class="tw:absolute tw:top-0.5 tw:right-0.5 tw:flex tw:h-4 tw:min-w-4 tw:items-center tw:justify-center tw:rounded-full tw:bg-red-500 tw:px-0.5 tw:text-[9px] tw:font-bold tw:text-white tw:leading-none"
      >
        {{ store.unreadCount > 99 ? '99+' : store.unreadCount }}
      </span>
    </button>

    <!-- Overlay panel -->
    <prime-popover ref="overlay">
      <div class="tw:w-80">
        <!-- Header -->
        <div class="tw:flex tw:items-center tw:justify-between tw:mb-3">
          <div class="tw:flex tw:items-center tw:gap-2">
            <p class="tw:text-sm tw:font-semibold">{{ t('notifications.title') }}</p>
            <!-- Connection indicator -->
            <span
              class="tw:h-1.5 tw:w-1.5 tw:rounded-full tw:shrink-0"
              :class="sseConnected ? 'tw:bg-emerald-400' : 'tw:bg-amber-400'"
              :title="sseConnected ? 'Live' : 'Reconnecting...'"
            />
          </div>
          <div class="tw:flex tw:items-center tw:gap-0.5">
            <!-- Sound toggle -->
            <button
              type="button"
              class="tw:flex tw:h-7 tw:w-7 tw:items-center tw:justify-center tw:rounded-lg tw:transition-colors app-text-muted hover:tw:bg-black/5"
              :title="t('notifications.sound')"
              @click="store.soundEnabled = !store.soundEnabled"
            >
              <iconify
                :icon="store.soundEnabled ? 'ph:speaker-high-bold' : 'ph:speaker-slash-bold'"
                class="tw:text-sm"
              />
            </button>
            <!-- Mark all read -->
            <button
              v-if="store.unreadCount > 0"
              type="button"
              class="tw:flex tw:h-7 tw:w-7 tw:items-center tw:justify-center tw:rounded-lg tw:transition-colors app-text-muted hover:tw:bg-black/5"
              :title="t('notifications.markAllRead')"
              @click="store.markAllRead()"
            >
              <iconify icon="ph:check-circle-bold" class="tw:text-sm" />
            </button>
            <!-- Clear -->
            <button
              v-if="store.items.length > 0"
              type="button"
              class="tw:flex tw:h-7 tw:w-7 tw:items-center tw:justify-center tw:rounded-lg tw:transition-colors app-text-muted hover:tw:bg-black/5"
              :title="t('notifications.clear')"
              @click="store.clear()"
            >
              <iconify icon="ph:trash-bold" class="tw:text-sm" />
            </button>
          </div>
        </div>

        <!-- Empty state -->
        <div
          v-if="store.items.length === 0"
          class="tw:py-8 tw:text-center app-text-muted tw:text-sm"
        >
          <iconify icon="ph:bell-slash-bold" class="tw:text-2xl tw:mb-2 tw:mx-auto tw:block" />
          {{ t('notifications.empty') }}
        </div>

        <!-- Notification list -->
        <div v-else class="tw:space-y-0.5 tw:max-h-80 tw:overflow-y-auto tw:-mx-3 tw:px-3">
          <div
            v-for="noti in store.items"
            :key="noti.id"
            class="tw:flex tw:items-start tw:gap-3 tw:rounded-xl tw:p-2.5 tw:cursor-pointer tw:transition-colors hover:tw:bg-black/5"
            :class="!noti.read ? 'app-card tw:border' : ''"
            @click="openOrder(noti.order.id)"
          >
            <!-- Icon -->
            <div
              class="tw:mt-0.5 tw:flex tw:h-7 tw:w-7 tw:shrink-0 tw:items-center tw:justify-center tw:rounded-lg"
              :class="noti.read ? 'tw:bg-white/5' : 'tw:bg-emerald-500/15'"
            >
              <iconify
                icon="ph:receipt-bold"
                class="tw:text-sm"
                :class="noti.read ? 'app-text-muted' : 'tw:text-emerald-400'"
              />
            </div>
            <!-- Text -->
            <div class="tw:min-w-0 tw:flex-1">
              <p
                class="tw:text-xs tw:font-semibold tw:truncate"
                :class="noti.read ? 'app-text-muted' : 'tw:text-emerald-400'"
              >
                {{ t('notifications.newOrder') }} · {{ noti.order.orderNumber }}
              </p>
              <p class="tw:text-xs app-text-muted tw:mt-0.5 tw:truncate">{{ buildDetail(noti.order) }}</p>
            </div>
            <!-- Time -->
            <span class="tw:text-[10px] app-text-muted tw:shrink-0 tw:mt-0.5 tw:tabular-nums">
              {{ formatTime(noti.at) }}
            </span>
          </div>
        </div>
      </div>
    </prime-popover>
  </div>
</template>
