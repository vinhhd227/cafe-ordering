<script setup>
const { t } = useI18n()
const router = useRouter()
const store = useNotificationStore()
const toast = useToast()
const confirm = useConfirm()

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

const readCount = computed(() => items.value.filter(n => n.isRead).length)

async function handleDeleteRead() {
  confirm.require({
    message: t('notifications.list.deleteReadConfirm'),
    header: t('notifications.list.deleteRead'),
    icon: 'ph:trash-bold',
    rejectProps: { severity: 'secondary', outlined: true },
    acceptProps: { severity: 'danger' },
    accept: async () => {
      const { deleteReadNotifications } = await import('@/services/notification.service')
      const res = await deleteReadNotifications()
      const deleted = res.data ?? 0
      items.value = items.value.filter(n => !n.isRead)
      totalCount.value = Math.max(0, totalCount.value - deleted)
      toast.add({ severity: 'success', summary: t('notifications.list.deleteReadDone', { n: deleted }), life: 3000 })
    },
  })
}

async function handleMarkAllRead() {
  await store.markAllRead()
  items.value.forEach(n => { n.isRead = true })
  toast.add({ severity: 'success', summary: t('notifications.list.allReadDone'), life: 2000 })
}

// ── Context menu (popover per card) ───────────────────────────────
const menuRef = ref(null)
const menuTarget = ref(null)

function openMenu(event, item) {
  event.stopPropagation()
  menuTarget.value = item
  menuRef.value.toggle(event)
}

async function handleMarkRead(item) {
  menuRef.value.hide()
  if (item.isRead) return
  await store.markRead(item.id)
  const local = items.value.find(n => n.id === item.id)
  if (local) { local.isRead = true; local.readAt = new Date().toISOString() }
}

async function handleDelete(item) {
  menuRef.value.hide()
  await import('@/services/notification.service').then(m => m.deleteNotification(item.id))
  items.value = items.value.filter(n => n.id !== item.id)
  totalCount.value = Math.max(0, totalCount.value - 1)
  if (!item.isRead) store.unreadCount = Math.max(0, store.unreadCount - 1)
  toast.add({ severity: 'success', summary: t('notifications.list.deleteDone'), life: 2000 })
}

// ── Tabs ───────────────────────────────────────────────────────────
const activeTab = ref('0')
const unreadItems = computed(() => items.value.filter(n => !n.isRead))

// ── Grouping ───────────────────────────────────────────────────────
const groupedItems = computed(() => {
  const now = new Date()
  const fiveMinAgo = new Date(now - 5 * 60 * 1000)
  const todayStart = new Date(now.getFullYear(), now.getMonth(), now.getDate())

  const groups = [
    { key: 'new',    label: t('notifications.list.groupNew'),    items: [] },
    { key: 'today',  label: t('notifications.list.groupToday'),  items: [] },
    { key: 'before', label: t('notifications.list.groupBefore'), items: [] },
  ]

  for (const noti of items.value) {
    const d = new Date(noti.createdAt)
    if (d >= fiveMinAgo)   groups[0].items.push(noti)
    else if (d >= todayStart) groups[1].items.push(noti)
    else                   groups[2].items.push(noti)
  }

  return groups.filter(g => g.items.length > 0)
})

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
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-primary-300">{{ t('notifications.list.breadcrumb') }}</p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">{{ t('notifications.list.title') }}</h1>
        <p class="tw:mt-2 tw:text-sm tw:text-muted">{{ t('notifications.list.subtitle') }}</p>
      </div>
      <div class="tw:flex tw:gap-2">
        <prime-button
          v-if="readCount > 0"
          severity="danger"
          outlined
          size="small"
          @click="handleDeleteRead"
        >
          <iconify icon="ph:trash-bold" />
          <span>{{ t('notifications.list.deleteRead') }}</span>
        </prime-button>
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
    </div>

    <!-- Tabs header -->
    <prime-tabs v-model:value="activeTab">
      <prime-tab-list :pt="{ root: { class: 'tw:bg-transparent!' } }">
        <prime-tab value="0">{{ t('notifications.list.tabAll') }}</prime-tab>
        <prime-tab value="1">{{ t('notifications.list.tabUnread') }}</prime-tab>
      </prime-tab-list>
    </prime-tabs>

    <!-- Error -->
    <prime-alert v-if="errorMessage" severity="error" variant="accent" closable @close="errorMessage = ''">
      {{ errorMessage }}
    </prime-alert>

    <!-- Loading -->
    <div v-if="loading && items.length === 0" class="tw:flex tw:justify-center tw:py-16">
      <iconify icon="ph:circle-notch-bold" class="tw:text-3xl tw:animate-spin tw:text-muted" />
    </div>

    <template v-else>

      <!-- Tab: Tất cả -->
      <template v-if="activeTab === '0'">
        <div v-if="items.length === 0" class="tw:flex tw:flex-col tw:items-center tw:py-20 tw:gap-3 tw:text-muted">
          <iconify icon="ph:bell-slash-bold" class="tw:text-5xl" />
          <p class="tw:text-sm">{{ t('notifications.list.empty') }}</p>
        </div>
        <template v-else>
          <div v-for="group in groupedItems" :key="group.key" class="tw:space-y-2">
            <!-- Group label ngoài card -->
            <p class="tw:px-1 tw:text-xs tw:font-semibold tw:text-muted">{{ group.label }}</p>
            <!-- Mỗi notification = 1 card -->
            <div
              v-for="noti in group.items"
              :key="noti.id"
              :class="[appCard, !noti.isRead ? 'noti-unread' : '']"
              class="tw:rounded-xl tw:flex tw:items-start tw:gap-4 tw:p-4 tw:cursor-pointer tw:transition-colors tw:hover:bg-white/5!"
              @click="openNotification(noti)"
            >
              <div class="tw:mt-0.5 tw:flex tw:h-9 tw:w-9 tw:shrink-0 tw:items-center tw:justify-center tw:rounded-xl" :class="noti.isRead ? 'tw:bg-white/5' : 'tw:bg-primary-500/15'">
                <iconify :icon="typeIcon(noti.type)" class="tw:text-base" :class="noti.isRead ? 'tw:text-muted' : 'tw:text-primary-400'" />
              </div>
              <div class="tw:flex-1 tw:min-w-0">
                <div class="tw:flex tw:items-center tw:justify-between tw:gap-2">
                  <p class="tw:text-sm tw:font-semibold" :class="noti.isRead ? 'tw:text-muted' : ''">{{ noti.title }}</p>
                  <div class="tw:flex tw:items-center tw:gap-1.5 tw:shrink-0">
                    <span v-if="!noti.isRead" class="tw:block tw:h-2 tw:w-2 tw:rounded-full tw:bg-primary-400" />
                    <span class="tw:text-xs tw:text-muted tw:tabular-nums">{{ formatTime(noti.createdAt) }}</span>
                  </div>
                </div>
                <p class="tw:text-xs tw:text-muted tw:mt-1">{{ noti.body }}</p>
              </div>
              <div class="tw:flex tw:items-center tw:shrink-0">
                <prime-button :class="btnIcon" severity="secondary" text size="small" @click.stop="openMenu($event, noti)">
                  <iconify icon="ph:dots-three-bold" />
                </prime-button>
              </div>
            </div>
          </div>
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
        </template>
      </template>

      <!-- Tab: Chưa đọc -->
      <template v-else-if="activeTab === '1'">
        <div v-if="unreadItems.length === 0" class="tw:flex tw:flex-col tw:items-center tw:py-20 tw:gap-3 tw:text-muted">
          <iconify icon="ph:check-circle-bold" class="tw:text-5xl" />
          <p class="tw:text-sm">{{ t('notifications.list.emptyUnread') }}</p>
        </div>
        <div v-else class="tw:space-y-2">
          <div
            v-for="noti in unreadItems"
            :key="noti.id"
            :class="[appCard, 'noti-unread']"
            class="tw:rounded-xl tw:flex tw:items-start tw:gap-4 tw:p-4 tw:cursor-pointer tw:transition-colors tw:hover:bg-white/5!"
            @click="openNotification(noti)"
          >
            <div class="tw:mt-0.5 tw:flex tw:h-9 tw:w-9 tw:shrink-0 tw:items-center tw:justify-center tw:rounded-xl tw:bg-primary-500/15">
              <iconify :icon="typeIcon(noti.type)" class="tw:text-base tw:text-primary-400" />
            </div>
            <div class="tw:flex-1 tw:min-w-0">
              <div class="tw:flex tw:items-center tw:justify-between tw:gap-2">
                <p class="tw:text-sm tw:font-semibold">{{ noti.title }}</p>
                <div class="tw:flex tw:items-center tw:gap-1.5 tw:shrink-0">
                  <span class="tw:block tw:h-2 tw:w-2 tw:rounded-full tw:bg-primary-400" />
                  <span class="tw:text-xs tw:text-muted tw:tabular-nums">{{ formatTime(noti.createdAt) }}</span>
                </div>
              </div>
              <p class="tw:text-xs tw:text-muted tw:mt-1">{{ noti.body }}</p>
            </div>
            <div class="tw:flex tw:items-center tw:shrink-0">
              <prime-button :class="btnIcon" severity="secondary" text size="small" @click.stop="openMenu($event, noti)">
                <iconify icon="ph:dots-three-bold" />
              </prime-button>
            </div>
          </div>
        </div>
      </template>

    </template>

    <prime-confirm-dialog />

    <!-- Shared notification context menu -->
    <prime-popover ref="menuRef">
      <div v-if="menuTarget" class="tw:flex tw:flex-col tw:gap-0.5 tw:min-w-44">
        <prime-button v-if="!menuTarget.isRead" severity="secondary" text size="small" fluid @click="handleMarkRead(menuTarget)">
          <iconify icon="ph:check-bold" class="tw:shrink-0" />
          <span>{{ t('notifications.list.markRead') }}</span>
        </prime-button>
        <prime-button severity="danger" text size="small" fluid @click="handleDelete(menuTarget)">
          <iconify icon="ph:trash-bold" class="tw:shrink-0" />
          <span>{{ t('notifications.list.delete') }}</span>
        </prime-button>
      </div>
    </prime-popover>

  </section>
</template>

<style scoped>
@layer utilities {
  .noti-unread {
    background-color: rgb(16 185 129 / 0.08) !important;
  }
}
</style>
