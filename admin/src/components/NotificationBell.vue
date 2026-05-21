<script setup>
const { t } = useI18n()
const router = useRouter()
const store = useNotificationStore()
const toast = useToast()
const overlay = ref(null)

// ── Push Notifications ─────────────────────────────────────────────
const { isSupported: pushSupported, isSubscribed, loading: pushLoading, toggle: togglePush, permission: pushPermission } = usePushNotifications()

async function handlePushToggle() {
  if (!pushSupported) {
    toast.add({ severity: 'warn', summary: t('notifications.pushNotSupported'), life: 4000 })
    return
  }
  await togglePush()
  if (pushPermission.value === 'denied') {
    toast.add({ severity: 'warn', summary: t('notifications.pushDenied'), life: 5000 })
  }
}

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
  async onOrderCreated(order) {
    await store.onNewOrder()
    if (store.creatingOrder) return
    playBeep()
  },
})

// Fetch notifications khi component mount (giới hạn 10 cho bell)
onMounted(() => store.fetchNotifications(1, 10))

// ── Helpers ────────────────────────────────────────────────────────
function formatTime(at) {
  return new Date(at).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })
}

function typeIcon(type) {
  const map = {
    ORDER_CREATED: 'ph:receipt-bold',
    ORDER_CANCELLED: 'ph:x-circle-bold',
    ORDER_COMPLETED: 'ph:check-circle-bold',
    PAYMENT_RECEIVED: 'ph:currency-circle-dollar-bold',
    MANUAL_ORDER_CREATED: 'ph:pencil-bold',
    LOW_STOCK: 'ph:warning-bold',
    SYSTEM_ALERT: 'ph:bell-bold',
  }
  return map[type] ?? 'ph:bell-bold'
}

// ── Actions ────────────────────────────────────────────────────────
function toggleOverlay(event) {
  overlay.value?.toggle(event)
  if (overlay.value?.visible === false) {
    store.fetchNotifications(1, 10)
  } else {
    expandedIds.value = new Set()
  }
}

function goToNotificationsPage() {
  overlay.value?.hide()
  router.push({ name: 'notifications' })
}

async function openNotification(item) {
  if (_longPressed) { _longPressed = false; return }
  await store.markRead(item.id)
  overlay.value?.hide()
  if (item.url) router.push(item.url)
}

// ── Long press expand ──────────────────────────────────────────────
const expandedIds = ref(new Set())
let _longPressTimer = null
let _longPressed = false

function onPointerDown(noti) {
  _longPressed = false
  _longPressTimer = setTimeout(() => {
    _longPressed = true
    expandedIds.value = new Set([...expandedIds.value, noti.id])
  }, 500)
}

function onPointerUp() {
  clearTimeout(_longPressTimer)
}

function onPointerMove() {
  clearTimeout(_longPressTimer)
}

function collapse(id) {
  const s = new Set(expandedIds.value)
  s.delete(id)
  expandedIds.value = s
}
</script>

<template>
  <div class="tw:relative">
    <!-- Bell button -->
    <prime-button
      variant="text"
      class="tw:relative tw:flex tw:px-1.5! tw:items-center tw:justify-center tw:rounded-lg tw:transition-colors tw:hover:bg-black/5"
      :title="t('notifications.title')"
      @click="toggleOverlay"
    >
      <iconify
        :icon="store.unreadCount > 0 ? 'ph:bell-ringing-bold' : 'ph:bell-bold'"
        class="tw:text-lg"
        :class="store.unreadCount > 0 ? 'tw:text-primary-400' : 'tw:text-muted'"
      />
      <!-- Badge -->
      <span
        v-if="store.unreadCount > 0"
        class="tw:absolute tw:top-0.5 tw:right-0 tw:flex tw:h-4 tw:min-w-4 tw:items-center tw:justify-center tw:rounded-full tw:bg-red-500 tw:px-0.5 tw:text-[9px] tw:font-bold tw:text-white tw:leading-none"
      >
        {{ store.unreadCount > 99 ? '99+' : store.unreadCount }}
      </span>
    </prime-button>

    <!-- Overlay panel -->
    <prime-popover ref="overlay" :pt="{ root: { class: 'tw:bg-white! tw:dark:bg-neutral-900/90! tw:border! tw:border-slate-200! tw:shadow-sm! tw:dark:border-white/15! tw:dark:shadow-xl! tw:dark:backdrop-blur-xl!' } }">
      <div class="tw:w-80">
        <!-- Header -->
        <div class="tw:flex tw:items-center tw:justify-between tw:mb-3">
          <div class="tw:flex tw:items-center tw:gap-2">
            <p class="tw:text-sm tw:font-semibold">{{ t('notifications.title') }}</p>
            <!-- Connection indicator -->
            <span
              class="tw:h-1.5 tw:w-1.5 tw:rounded-full tw:shrink-0"
              :class="sseConnected ? 'tw:bg-primary-400' : 'tw:bg-amber-400'"
              :title="sseConnected ? 'Live' : 'Reconnecting...'"
            />
          </div>
          <div class="tw:flex tw:items-center tw:gap-0.5">
            <!-- Push notifications toggle -->
            <button
              v-if="pushSupported"
              type="button"
              class="tw:flex tw:h-7 tw:w-7 tw:items-center tw:justify-center tw:rounded-lg tw:transition-colors tw:hover:bg-black/5"
              :class="isSubscribed ? 'tw:text-primary-400' : 'tw:text-muted'"
              :title="isSubscribed ? t('notifications.pushDisable') : t('notifications.pushEnable')"
              :disabled="pushLoading"
              @click="handlePushToggle"
            >
              <iconify
                :icon="pushLoading ? 'ph:circle-notch-bold' : (isSubscribed ? 'ph:device-mobile-bold' : 'ph:device-mobile-slash-bold')"
                class="tw:text-sm"
                :class="pushLoading ? 'tw:animate-spin' : ''"
              />
            </button>
            <!-- Sound toggle -->
            <button
              type="button"
              class="tw:flex tw:h-7 tw:w-7 tw:items-center tw:justify-center tw:rounded-lg tw:transition-colors tw:text-muted tw:hover:bg-black/5"
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
              class="tw:flex tw:h-7 tw:w-7 tw:items-center tw:justify-center tw:rounded-lg tw:transition-colors tw:text-muted tw:hover:bg-black/5"
              :title="t('notifications.markAllRead')"
              @click="store.markAllRead()"
            >
              <iconify icon="ph:check-circle-bold" class="tw:text-sm" />
            </button>
          </div>
        </div>

        <!-- Loading -->
        <div v-if="store.loading" class="tw:py-8 tw:flex tw:justify-center">
          <iconify icon="ph:circle-notch-bold" class="tw:text-2xl tw:animate-spin tw:text-muted" />
        </div>

        <!-- Empty state -->
        <div
          v-else-if="store.items.length === 0"
          class="tw:py-8 tw:text-center tw:text-muted tw:text-sm"
        >
          <iconify icon="ph:bell-slash-bold" class="tw:text-2xl tw:mb-2 tw:mx-auto tw:block" />
          {{ t('notifications.empty') }}
        </div>

        <!-- Notification list -->
        <div v-else class="tw:space-y-0.5 tw:max-h-80 tw:overflow-y-auto tw:-mx-3 tw:px-3">
          <div
            v-for="noti in store.items"
            :key="noti.id"
            class="tw:flex tw:items-start tw:gap-3 tw:rounded-xl tw:p-2.5 tw:mb-1 tw:cursor-pointer tw:transition-colors tw:border"
            :class="!noti.isRead
              ? 'tw:bg-primary-50 tw:border tw:border-primary-200 tw:hover:bg-primary-100 tw:dark:bg-primary-500/10 tw:dark:border-primary-500/25 tw:dark:hover:bg-primary-500/15'
              : 'tw:bg-slate-50 tw:hover:bg-slate-100 tw:dark:bg-white/3 tw:dark:hover:bg-white/6 tw:border-slate-200 tw:dark:border-white/10'"
            @click="openNotification(noti)"
            @pointerdown="onPointerDown(noti)"
            @pointerup="onPointerUp"
            @pointermove="onPointerMove"
            @contextmenu.prevent
          >
            <!-- Icon -->
            <div
              class="tw:mt-0.5 tw:flex tw:h-7 tw:w-7 tw:shrink-0 tw:items-center tw:justify-center tw:rounded-lg"
              :class="noti.isRead ? 'tw:bg-white/5' : 'tw:bg-primary-500/15'"
            >
              <iconify
                :icon="typeIcon(noti.type)"
                class="tw:text-sm"
                :class="noti.isRead ? 'tw:text-muted' : 'tw:text-primary-400'"
              />
            </div>
            <!-- Text -->
            <div class="tw:min-w-0 tw:flex-1">
              <p
                class="tw:text-xs tw:font-semibold"
                :class="[noti.isRead ? 'tw:text-muted' : 'tw:text-primary-400', expandedIds.has(noti.id) ? '' : 'tw:truncate']"
              >
                {{ noti.title }}
              </p>
              <p
                class="tw:text-xs tw:text-muted tw:mt-0.5"
                :class="expandedIds.has(noti.id) ? 'tw:whitespace-pre-wrap tw:break-words' : 'tw:truncate'"
              >
                {{ noti.body }}
              </p>
              <button
                v-if="expandedIds.has(noti.id)"
                type="button"
                class="tw:mt-2 tw:flex tw:items-center tw:gap-1 tw:text-[10px] tw:text-muted tw:hover:text-primary-400 tw:transition-colors"
                @click.stop="collapse(noti.id)"
              >
                <iconify icon="ph:caret-up-bold" class="tw:text-[9px]" />
                <span>Thu gọn</span>
              </button>
            </div>
            <!-- Time + delete -->
            <div class="tw:flex tw:flex-col tw:items-end tw:justify-between tw:shrink-0 tw:self-stretch">
              <button
                type="button"
                class="tw:flex tw:h-4 tw:w-4 tw:items-center tw:justify-center tw:rounded tw:text-muted tw:hover:text-red-400 tw:transition-colors"
                :title="t('notifications.delete')"
                @click.stop="store.deleteNotification(noti.id)"
              >
                <iconify icon="ph:x-bold" class="tw:text-[10px]" />
              </button>
              <span class="tw:text-[10px] tw:text-muted tw:tabular-nums">
                {{ formatTime(noti.createdAt) }}
              </span>
            </div>
          </div>
        </div>

        <!-- Footer: xem tất cả -->
        <div v-if="store.totalCount > 0" class="tw:mt-3 tw:pt-3 tw:border-t tw:border-white/10 tw:text-center">
          <button
            type="button"
            class="tw:text-xs tw:text-muted tw:hover:text-primary-400 tw:transition-colors tw:flex tw:items-center tw:gap-1 tw:mx-auto"
            @click="goToNotificationsPage"
          >
            <span>{{ t('notifications.viewAll') }}</span>
            <iconify icon="ph:arrow-right-bold" class="tw:text-[10px]" />
          </button>
        </div>
      </div>
    </prime-popover>

  </div>
</template>
