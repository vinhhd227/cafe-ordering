<script setup>
const { t } = useI18n()
const router = useRouter()
const store = useNotificationStore()
const toast = useToast()

// ── Table state ────────────────────────────────────────────────────
const loading = ref(false)
const errorMessage = ref('')
const items = ref([])
const totalCount = ref(0)
const rows = ref(20)
const first = ref(0)

// ── Load ───────────────────────────────────────────────────────────
const loadItems = async (page = 1) => {
  loading.value = true
  errorMessage.value = ''
  try {
    const res = await import('@/services/notification.service').then(m =>
      m.getNotifications({ page, pageSize: rows.value })
    )
    items.value = res.data.items ?? []
    totalCount.value = res.data.totalCount ?? 0
    // Đồng bộ unreadCount về store (badge ở bell)
    store.unreadCount = res.data.unreadCount ?? store.unreadCount
  } catch {
    errorMessage.value = t('notifications.list.loadError')
  } finally {
    loading.value = false
  }
}

onMounted(() => loadItems(1))

// ── Actions ────────────────────────────────────────────────────────
async function openNotification(item) {
  if (!item.isRead) {
    await store.markRead(item.id)
    // Cập nhật local list luôn
    const local = items.value.find(n => n.id === item.id)
    if (local) { local.isRead = true; local.readAt = new Date().toISOString() }
  }
  if (item.url) router.push(item.url)
}

async function handleMarkAllRead() {
  await store.markAllRead()
  items.value.forEach(n => { n.isRead = true })
  toast.add({ severity: 'success', summary: t('notifications.list.allReadDone'), life: 2000 })
}

// ── Helpers ────────────────────────────────────────────────────────
function formatTime(at) {
  const d = new Date(at)
  const now = new Date()
  const diffMs = now - d
  const diffMin = Math.floor(diffMs / 60000)
  if (diffMin < 1) return t('notifications.list.justNow')
  if (diffMin < 60) return t('notifications.list.minutesAgo', { n: diffMin })
  const diffH = Math.floor(diffMin / 60)
  if (diffH < 24) return t('notifications.list.hoursAgo', { n: diffH })
  return d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' })
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
</script>

<template>
  <section class="tw:space-y-8">

    <!-- Header -->
    <div class="tw:flex tw:flex-wrap tw:items-end tw:justify-between tw:gap-4">
      <div>
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300">{{ t('notifications.list.breadcrumb') }}</p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">{{ t('notifications.list.title') }}</h1>
        <p class="tw:mt-2 tw:text-sm app-text-muted">{{ t('notifications.list.subtitle') }}</p>
      </div>
      <prime-button
        v-if="store.unreadCount > 0"
        severity="secondary"
        outlined
        size="small"
        @click="handleMarkAllRead"
      >
        <iconify icon="ph:check-circle-bold" />
        <span>{{ t('notifications.markAllRead') }}</span>
      </prime-button>
    </div>

    <!-- Error -->
    <prime-alert v-if="errorMessage" severity="error" variant="accent" closable @close="errorMessage = ''">
      {{ errorMessage }}
    </prime-alert>

    <!-- Loading -->
    <div v-if="loading && items.length === 0" class="tw:flex tw:justify-center tw:py-16">
      <iconify icon="ph:circle-notch-bold" class="tw:text-3xl tw:animate-spin app-text-muted" />
    </div>

    <!-- Empty state -->
    <div v-else-if="!loading && items.length === 0" class="tw:flex tw:flex-col tw:items-center tw:py-20 tw:gap-3 app-text-muted">
      <iconify icon="ph:bell-slash-bold" class="tw:text-5xl" />
      <p class="tw:text-sm">{{ t('notifications.list.empty') }}</p>
    </div>

    <!-- List -->
    <div v-else :class="appCard" class="tw:rounded-2xl tw:overflow-hidden tw:divide-y tw:divide-white/5">
      <div
        v-for="noti in items"
        :key="noti.id"
        class="tw:flex tw:items-start tw:gap-4 tw:p-4 tw:cursor-pointer tw:transition-colors hover:tw:bg-white/3"
        :class="!noti.isRead ? 'tw:bg-emerald-500/5' : ''"
        @click="openNotification(noti)"
      >
        <!-- Icon -->
        <div
          class="tw:mt-0.5 tw:flex tw:h-9 tw:w-9 tw:shrink-0 tw:items-center tw:justify-center tw:rounded-xl"
          :class="noti.isRead ? 'tw:bg-white/5' : 'tw:bg-emerald-500/15'"
        >
          <iconify
            :icon="typeIcon(noti.type)"
            class="tw:text-base"
            :class="noti.isRead ? 'app-text-muted' : 'tw:text-emerald-400'"
          />
        </div>

        <!-- Content -->
        <div class="tw:flex-1 tw:min-w-0">
          <div class="tw:flex tw:items-start tw:justify-between tw:gap-2">
            <p
              class="tw:text-sm tw:font-semibold"
              :class="noti.isRead ? 'app-text-muted' : ''"
            >
              {{ noti.title }}
            </p>
            <span class="tw:text-xs app-text-muted tw:shrink-0 tw:tabular-nums">
              {{ formatTime(noti.createdAt) }}
            </span>
          </div>
          <p class="tw:text-xs app-text-muted tw:mt-1">{{ noti.body }}</p>
        </div>

        <!-- Unread dot -->
        <div class="tw:mt-2 tw:h-2 tw:w-2 tw:shrink-0">
          <span
            v-if="!noti.isRead"
            class="tw:block tw:h-2 tw:w-2 tw:rounded-full tw:bg-emerald-400"
          />
        </div>
      </div>
    </div>

    <!-- Pagination -->
    <div v-if="totalCount > rows" class="tw:flex tw:justify-center">
      <prime-paginator
        :rows="rows"
        :totalRecords="totalCount"
        :first="first"
        :rowsPerPageOptions="[20, 50, 100]"
        @page="(e) => { first = e.first; rows = e.rows; loadItems(e.page + 1) }"
        template="FirstPageLink PrevPageLink PageLinks NextPageLink LastPageLink RowsPerPageDropdown"
      />
    </div>

  </section>
</template>
