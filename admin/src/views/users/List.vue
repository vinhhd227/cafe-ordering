<script setup>
import {
  getUsers,
  createUser,
  activateUser,
  deactivateUser,
} from '@/services/user.service'

const { t } = useI18n();
const cache  = useTableCache('users')
const router = useRouter()
const { can } = usePermission()

// --- Table state ---
const users        = ref([])
const loading      = ref(false)
const errorMessage = ref('')
const rows         = ref(20)
const first        = ref(0)
const totalRecords = ref(0)
const searchTimer  = ref(null)

// --- Filters ---
const search       = ref('')
const roleFilter   = ref(null)
const statusFilter = ref(null)

const filterPanel       = ref(null)
const activeFilterCount = computed(() =>
  [roleFilter.value, statusFilter.value].filter(v => v !== null).length
)
const hasActiveFilters  = computed(() => activeFilterCount.value > 0)

const clearFilters = () => {
  roleFilter.value   = null
  statusFilter.value = null
  first.value        = 0
  loadUsers(1)
}

// --- Summary stats ---
const stats = ref({ total: 0, active: 0, admins: 0, staff: 0 })

// --- Dialog: Add User ---
const showAddDialog = ref(false)
const addForm       = ref({ username: '', fullName: '', role: 'Staff' })
const addLoading    = ref(false)
const addError      = ref('')

// --- Dialog: Temp Password (after create) ---
const showTempPasswordDialog = ref(false)
const tempPasswordData       = ref({ username: '', temporaryPassword: '' })

// --- Dialog: Deactivate Confirm ---
const confirmDeactivateUser = ref(null)
const deactivateLoading     = ref(false)

// --- Constants ---
const roleFilterOptions = [
  { label: 'Admin', value: 'Admin' },
  { label: 'Staff', value: 'Staff' },
]

const statusFilterOptions = computed(() => [
  { label: t('users.filter.statusActive'),   value: true  },
  { label: t('users.filter.statusInactive'), value: false },
])

const roleSelectOptions = [
  { label: 'Admin', value: 'Admin' },
  { label: 'Staff', value: 'Staff' },
]

// --- Helpers ---
const roleSeverity = (role) => {
  if (role === 'Admin') return 'danger'
  if (role === 'Staff') return 'info'
  return 'secondary'
}

const formatDate = (dateStr) =>
  new Date(dateStr).toLocaleDateString('vi-VN')

const initials = (fullName) =>
  (fullName ?? '')
    .split(' ')
    .filter(Boolean)
    .map((w) => w[0])
    .slice(0, 2)
    .join('')
    .toUpperCase()

const extractError = (err) =>
  err?.response?.data?.errors?.join('; ') ||
  err?.response?.data?.message ||
  t('users.errors.generic')

// --- Data Loading ---
const loadUsers = async (page = 1) => {
  loading.value      = true
  errorMessage.value = ''
  try {
    const res      = await getUsers({
      page,
      pageSize: rows.value,
      search:   search.value.trim() || undefined,
      role:     roleFilter.value    ?? undefined,
      isActive: statusFilter.value  ?? undefined,
    })
    const data         = res?.data ?? {}
    users.value        = data.items ?? []
    totalRecords.value = data.total  ?? 0
  } catch (err) {
    errorMessage.value = extractError(err)
  } finally {
    loading.value = false
  }
}

const loadStats = async () => {
  try {
    const [totalRes, activeRes, adminRes, staffRes] = await Promise.all([
      getUsers({ page: 1, pageSize: 1 }),
      getUsers({ page: 1, pageSize: 1, isActive: true }),
      getUsers({ page: 1, pageSize: 1, role: 'Admin' }),
      getUsers({ page: 1, pageSize: 1, role: 'Staff' }),
    ])
    stats.value = {
      total:  totalRes?.data?.total  ?? 0,
      active: activeRes?.data?.total ?? 0,
      admins: adminRes?.data?.total  ?? 0,
      staff:  staffRes?.data?.total  ?? 0,
    }
  } catch {
    // non-critical
  }
}

onMounted(() => {
  const cached = cache.restore()
  if (cached) {
    search.value       = cached.search       ?? ''
    rows.value         = cached.rows         ?? 20
    first.value        = cached.first        ?? 0
    roleFilter.value   = cached.roleFilter   ?? null
    statusFilter.value = cached.statusFilter ?? null
    const page = rows.value > 0 ? Math.floor(first.value / rows.value) + 1 : 1
    loadUsers(page)
  } else {
    loadUsers()
  }
  loadStats()
})

onBeforeRouteLeave(() => {
  cache.save({
    search:       search.value,
    rows:         rows.value,
    first:        first.value,
    roleFilter:   roleFilter.value,
    statusFilter: statusFilter.value,
  })
})

watch([search], () => {
  clearTimeout(searchTimer.value)
  searchTimer.value = setTimeout(() => {
    first.value = 0
    loadUsers(1)
  }, 400)
})

onBeforeUnmount(() => {
  clearTimeout(searchTimer.value)
})

watch([roleFilter, statusFilter], () => {
  first.value = 0
  loadUsers(1)
})

// --- Add User ---
const openAddDialog = () => {
  addForm.value  = { username: '', fullName: '', role: 'Staff' }
  addError.value = ''
  showAddDialog.value = true
}

const submitAddUser = async () => {
  addLoading.value = true
  addError.value   = ''
  try {
    const res = await createUser(addForm.value)
    tempPasswordData.value       = res.data
    showAddDialog.value          = false
    showTempPasswordDialog.value = true
    loadUsers(1)
    loadStats()
  } catch (err) {
    addError.value = extractError(err)
  } finally {
    addLoading.value = false
  }
}

const copyTempPassword = () => {
  navigator.clipboard?.writeText(tempPasswordData.value.temporaryPassword)
}

// --- Activate / Deactivate ---
const handleToggleActive = (user) => {
  if (user.isActive) {
    confirmDeactivateUser.value = user
  } else {
    doActivate(user)
  }
}

const doActivate = async (user) => {
  try {
    await activateUser(user.id)
    user.isActive = true
    loadStats()
  } catch (err) {
    errorMessage.value = extractError(err)
  }
}

const confirmAndDeactivate = async () => {
  if (!confirmDeactivateUser.value) return
  deactivateLoading.value = true
  try {
    await deactivateUser(confirmDeactivateUser.value.id)
    confirmDeactivateUser.value.isActive = false
    confirmDeactivateUser.value = null
    loadStats()
  } catch (err) {
    errorMessage.value = extractError(err)
  } finally {
    deactivateLoading.value = false
  }
}

// ── Widget visibility ──────────────────────────────────────────────
const { isVisible: wVisible, toggle: wToggle, hiddenCount: wHidden, widgets: wDefs, colsPerRow: wCols, setColsPerRow: wSetCols } =
  useWidgetSettings('users', [
    { id: 'total',  label: t('users.widgets.totalUsers.label'), preview: '24', description: t('users.widgets.totalUsers.description') },
    { id: 'active', label: t('users.widgets.active.label'),     preview: '20', description: t('users.widgets.active.description'),     labelClass: 'tw:text-emerald-400' },
    { id: 'admins', label: t('users.widgets.admins.label'),     preview: '3',  description: t('users.widgets.admins.description'),     labelClass: 'tw:text-red-400' },
    { id: 'staff',  label: t('users.widgets.staff.label'),      preview: '17', description: t('users.widgets.staff.description'),      labelClass: 'tw:text-blue-400' },
  ], { defaultCols: 4 })
const W_COLS_CLASS = { 1: 'tw:grid-cols-1', 2: 'tw:grid-cols-2', 3: 'tw:grid-cols-3', 4: 'tw:grid-cols-4' }
const wColsClass = computed(() => W_COLS_CLASS[wCols.value] ?? 'tw:grid-cols-2')

const columns = computed(() => [
  { key: 'user',        header: t('users.table.colUser'),    width: '14rem' },
  { field: 'email',     header: t('users.table.colEmail') },
  { field: 'roles',     header: t('users.table.colRole'),    width: '8rem' },
  { field: 'isActive',  header: t('users.table.colStatus') },
  { field: 'createdAt', header: t('users.table.colCreated') },
  { key: 'actions',     header: t('users.table.colActions'), width: '8rem', toggleable: false },
])

// ── Mobile drawer ──────────────────────────────────────────────
const drawerUser = ref(null);
const drawerVisible = ref(false);
const openDrawer = (row) => { drawerUser.value = row; drawerVisible.value = true; };
</script>

<template>
  <section class="tw:space-y-6">

    <!-- ── Page Header ───────────────────────────────────────────── -->
    <div class="tw:flex tw:flex-wrap tw:items-end tw:justify-between tw:gap-4">
      <div>
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300">{{ t('users.breadcrumb') }}</p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">{{ t('users.title') }}</h1>
        <p class="tw:mt-2 tw:text-sm app-text-muted">
          {{ t('users.subtitle') }}
        </p>
      </div>
      <div class="tw:flex tw:items-center tw:gap-2">
        <widget-settings-button
          :widgets="wDefs"
          :hidden-count="wHidden"
          :cols-per-row="wCols"
          @toggle="wToggle"
          @update:cols-per-row="wSetCols"
        />
        <prime-button
          v-if="can('user.create')"
          severity="success"
          size="small"
          @click="openAddDialog"
        >
          <iconify icon="ph:user-plus-bold" />
          <span>{{ t('users.addUser') }}</span>
        </prime-button>
      </div>
    </div>

    <!-- ── Summary Stats ─────────────────────────────────────────── -->
    <div :class="['tw:grid tw:gap-3', wColsClass]">
      <widget-stat v-if="wVisible('total')"  :label="t('users.widgets.totalUsers.label')" :value="stats.total" />
      <widget-stat v-if="wVisible('active')" :label="t('users.widgets.active.label')"     :value="stats.active" label-class="tw:text-emerald-400" />
      <widget-stat v-if="wVisible('admins')" :label="t('users.widgets.admins.label')"     :value="stats.admins" label-class="tw:text-red-400" />
      <widget-stat v-if="wVisible('staff')"  :label="t('users.widgets.staff.label')"      :value="stats.staff"  label-class="tw:text-blue-400" />
    </div>

    <!-- ── Error Banner ──────────────────────────────────────────── -->
    <prime-alert
      v-if="errorMessage"
      severity="error"
      variant="accent"
      closable
      @close="errorMessage = ''"
    >{{ errorMessage }}</prime-alert>

    <!-- ── Table ──────────────────────────────────────────────────── -->
    <AppTable
      v-model:first="first"
      v-model:rows="rows"
      :value="users"
      :loading="loading"
      :totalRecords="totalRecords"
      :rowsPerPageOptions="[10, 20, 50]"
      :columns="columns"
      @page="(e) => loadUsers(e.page + 1)"
    >
      <template #mobile-card="{ data }">
        <div class="tw:rounded-xl tw:border tw:border-slate-200 tw:dark:border-white/10 tw:bg-white tw:dark:bg-white/5 tw:p-3 tw:flex tw:flex-col tw:gap-2">
          <div class="tw:flex tw:items-start tw:justify-between tw:gap-1">
            <div class="tw:flex tw:items-center tw:gap-3">
              <div class="tw:h-8 tw:w-8 tw:rounded-full tw:flex tw:items-center tw:justify-center tw:bg-emerald-500/20 tw:text-emerald-300 tw:text-xs tw:font-bold tw:flex-shrink-0">
                {{ initials(data.fullName) }}
              </div>
              <div>
                <p class="tw:text-sm tw:font-semibold tw:leading-snug">{{ data.username }}</p>
                <p class="tw:text-xs app-text-muted">{{ data.fullName }}</p>
              </div>
            </div>
            <prime-tag
              :value="data.isActive ? t('users.status.active') : t('users.status.inactive')"
              :severity="data.isActive ? 'success' : 'danger'"
              class="tw:text-[11px]! tw:px-1.5! tw:py-0.5! tw:flex-shrink-0"
            />
          </div>
          <div class="tw:border-t tw:border-slate-200 tw:dark:border-white/10 tw:pt-2">
            <prime-button severity="secondary" outlined size="small" fluid @click="openDrawer(data)">
              <iconify icon="ph:dots-three-bold" />
              <span>{{ t('common.moreActions') }}</span>
            </prime-button>
          </div>
        </div>
      </template>

      <template #toolbar-left>
        <div class="tw:flex tw:items-center tw:gap-2">
          <!-- Search -->
          <prime-input-text
            v-model="search"
            :placeholder="t('users.table.searchPlaceholder')"
            class="app-input tw:w-64"
          />

          <!-- Filter toggle button -->
          <prime-button
            :severity="hasActiveFilters ? 'success' : 'secondary'"
            :outlined="!hasActiveFilters"
            v-tooltip.top="t('users.filter.tooltip')"
            @click="filterPanel.toggle($event)"
            :class="btnIcon"
          >
            <span class="tw:relative tw:inline-flex">
              <iconify icon="ph:funnel-bold" />
              <prime-badge
                v-if="activeFilterCount > 0"
                :value="activeFilterCount"
                severity="danger"
                class="tw:absolute! tw:-top-2.5! tw:-right-2.5! tw:scale-75! tw:origin-top-right!"
              />
            </span>
          </prime-button>

          <!-- Filter popover -->
          <prime-popover ref="filterPanel">
            <div class="tw:flex tw:flex-col tw:gap-4">
              <p class="tw:text-sm tw:font-semibold">{{ t('users.filter.title') }}</p>

              <div class="tw:space-y-1.5">
                <label class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest">{{ t('users.filter.roleLabel') }}</label>
                <prime-select
                  v-model="roleFilter"
                  :options="roleFilterOptions"
                  option-label="label"
                  option-value="value"
                  :placeholder="t('users.filter.rolePlaceholder')"
                  show-clear
                  class="app-input tw:w-full"
                />
              </div>

              <div class="tw:space-y-1.5">
                <label class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest">{{ t('users.filter.statusLabel') }}</label>
                <prime-select
                  v-model="statusFilter"
                  :options="statusFilterOptions"
                  option-label="label"
                  option-value="value"
                  :placeholder="t('users.filter.statusPlaceholder')"
                  show-clear
                  class="app-input tw:w-full"
                />
              </div>

              <prime-button
                v-if="hasActiveFilters"
                severity="danger"
                outlined
                size="small"
                @click="clearFilters"
              >
                <iconify icon="ph:x-bold" />
                <span>{{ t('users.filter.clearFilters') }}</span>
              </prime-button>
            </div>
          </prime-popover>
        </div>
      </template>

      <template #col-user="{ data }">
        <div class="tw:flex tw:items-center tw:gap-3">
          <div
            class="tw:h-9 tw:w-9 tw:rounded-full tw:flex tw:items-center tw:justify-center
                   tw:bg-emerald-500/20 tw:text-emerald-300 tw:text-xs tw:font-bold tw:flex-shrink-0"
          >
            {{ initials(data.fullName) }}
          </div>
          <div>
            <p class="tw:text-sm tw:font-medium">{{ data.username }}</p>
            <p class="tw:text-xs app-text-muted">{{ data.fullName }}</p>
          </div>
        </div>
      </template>

      <template #col-email="{ data }">
        <span class="tw:text-sm app-text-muted">{{ data.email || '—' }}</span>
      </template>

      <template #col-roles="{ data }">
        <prime-tag
          v-for="role in data.roles"
          :key="role"
          :value="role"
          :severity="roleSeverity(role)"
          class="tw:mr-1"
        />
        <span v-if="!data.roles?.length" class="app-text-muted tw:text-xs">—</span>
      </template>

      <template #col-isActive="{ data }">
        <prime-tag
          :value="data.isActive ? t('users.status.active') : t('users.status.inactive')"
          :severity="data.isActive ? 'success' : 'danger'"
        />
      </template>

      <template #col-createdAt="{ data }">
        <span class="tw:text-xs app-text-muted">{{ formatDate(data.createdAt) }}</span>
      </template>

      <template #col-actions="{ data }">
        <div class="tw:flex tw:justify-end tw:gap-2">
          <prime-button
            v-if="can('user.deactivate')"
            :severity="data.isActive ? 'danger' : 'success'"
            outlined
            size="small"
            v-tooltip.top="data.isActive ? t('users.tooltips.deactivate') : t('users.tooltips.activate')"
            @click="handleToggleActive(data)"
            :class="btnIcon"
          >
            <iconify :icon="data.isActive ? 'ph:toggle-left-bold' : 'ph:toggle-right-bold'" />
          </prime-button>
          <prime-button
            severity="secondary"
            outlined
            size="small"
            v-tooltip.top="t('users.tooltips.viewEdit')"
            @click="router.push({ name: 'userDetail', params: { id: data.id } })"
            :class="btnIcon"
          >
            <iconify icon="ph:arrow-right-bold" />
          </prime-button>
        </div>
      </template>
    </AppTable>

    <!-- ── Mobile action drawer ───────────────────────────────────── -->
    <prime-drawer
      v-model:visible="drawerVisible"
      position="bottom"
      :style="{ height: 'auto' }"
      :pt="{ root: { class: 'tw:rounded-t-2xl' } }"
    >
      <template #header>
        <div class="tw:flex tw:items-center tw:gap-2">
          <span class="tw:font-medium">{{ drawerUser?.username }}</span>
          <prime-tag
            v-if="drawerUser"
            :value="drawerUser.isActive ? t('users.status.active') : t('users.status.inactive')"
            :severity="drawerUser.isActive ? 'success' : 'danger'"
            class="tw:text-[11px]! tw:px-1.5! tw:py-0.5!"
          />
        </div>
      </template>
      <div v-if="drawerUser" class="tw:flex tw:flex-col tw:gap-2 tw:pb-4">
        <prime-button
          v-if="can('user.deactivate')"
          :label="drawerUser.isActive ? t('users.tooltips.deactivate') : t('users.tooltips.activate')"
          :severity="drawerUser.isActive ? 'danger' : 'success'"
          outlined fluid
          @click="handleToggleActive(drawerUser); drawerVisible = false"
        >
          <template #icon>
            <iconify :icon="drawerUser.isActive ? 'ph:toggle-left-bold' : 'ph:toggle-right-bold'" />
          </template>
        </prime-button>
        <prime-button
          :label="t('users.tooltips.viewEdit')"
          severity="secondary"
          outlined fluid
          @click="router.push({ name: 'userDetail', params: { id: drawerUser.id } }); drawerVisible = false"
        >
          <template #icon><iconify icon="ph:arrow-right-bold" /></template>
        </prime-button>
      </div>
    </prime-drawer>

    <!-- ===== Add User Dialog ===== -->
    <prime-dialog
      v-model:visible="showAddDialog"
      :header="t('users.addDialog.header')"
      :modal="true"
      style="width: 28rem"
    >
      <div class="tw:space-y-4 tw:pt-2">
        <prime-alert
          v-if="addError"
          severity="error"
          variant="accent"
          :closable="false"
        >{{ addError }}</prime-alert>

        <div class="tw:space-y-1">
          <label class="tw:text-sm tw:font-medium">{{ t('users.addDialog.username') }}</label>
          <prime-input-text
            v-model="addForm.username"
            class="app-input tw:w-full"
            :placeholder="t('users.addDialog.usernamePlaceholder')"
          />
        </div>
        <div class="tw:space-y-1">
          <label class="tw:text-sm tw:font-medium">{{ t('users.addDialog.fullName') }}</label>
          <prime-input-text
            v-model="addForm.fullName"
            class="app-input tw:w-full"
            :placeholder="t('users.addDialog.fullNamePlaceholder')"
          />
        </div>
        <div class="tw:space-y-1">
          <label class="tw:text-sm tw:font-medium">{{ t('users.addDialog.role') }}</label>
          <prime-select
            v-model="addForm.role"
            :options="roleSelectOptions"
            optionLabel="label"
            optionValue="value"
            class="app-input tw:w-full"
          />
        </div>
      </div>

      <template #footer>
        <prime-button
          severity="secondary"
          outlined
          size="small"
          @click="showAddDialog = false"
        >
          <iconify icon="ph:x-bold" />
          <span>{{ t('common.cancel') }}</span>
        </prime-button>
        <prime-button
          severity="success"
          size="small"
          :loading="addLoading"
          @click="submitAddUser"
        >
          <iconify icon="ph:user-plus-bold" />
          <span>{{ t('users.addDialog.create') }}</span>
        </prime-button>
      </template>
    </prime-dialog>

    <!-- ===== Temp Password Dialog ===== -->
    <prime-dialog
      v-model:visible="showTempPasswordDialog"
      :header="t('users.tempPasswordDialog.header')"
      :modal="true"
      style="width: 26rem"
    >
      <div class="tw:space-y-4 tw:pt-2">
        <p class="tw:text-sm app-text-muted">
          {{ t('users.tempPasswordDialog.notice') }}
        </p>
        <div class="tw:rounded-xl tw:border tw:p-4 tw:space-y-3 app-card">
          <div class="tw:space-y-0.5">
            <p class="tw:text-[10px] tw:uppercase tw:tracking-widest app-text-subtle">{{ t('users.tempPasswordDialog.username') }}</p>
            <p class="tw:font-mono tw:font-semibold">{{ tempPasswordData.username }}</p>
          </div>
          <div class="tw:space-y-0.5">
            <p class="tw:text-[10px] tw:uppercase tw:tracking-widest app-text-subtle">{{ t('users.tempPasswordDialog.tempPassword') }}</p>
            <div class="tw:flex tw:items-center tw:gap-2">
              <p class="tw:font-mono tw:font-bold tw:text-xl tw:tracking-widest tw:text-emerald-300">
                {{ tempPasswordData.temporaryPassword }}
              </p>
              <prime-button
                severity="secondary"
                outlined
                size="small"
                v-tooltip.top="t('users.tooltips.copy')"
                @click="copyTempPassword"
                :class="btnIcon"
              >
                <iconify icon="ph:copy-bold" />
              </prime-button>
            </div>
          </div>
        </div>
      </div>

      <template #footer>
        <prime-button
          severity="success"
          size="small"
          @click="showTempPasswordDialog = false"
        >
          <iconify icon="ph:check-bold" />
          <span>{{ t('users.tempPasswordDialog.done') }}</span>
        </prime-button>
      </template>
    </prime-dialog>

    <!-- ===== Deactivate Confirm Dialog ===== -->
    <prime-dialog
      :visible="!!confirmDeactivateUser"
      :header="t('users.deactivateDialog.header')"
      :modal="true"
      style="width: 24rem"
      @update:visible="(v) => { if (!v) confirmDeactivateUser = null }"
    >
      <div class="tw:pt-2">
        <p class="tw:text-sm app-text-muted">
          {{ t('users.deactivateDialog.confirmText', { username: confirmDeactivateUser?.username }) }}
        </p>
      </div>

      <template #footer>
        <prime-button
          severity="secondary"
          outlined
          size="small"
          @click="confirmDeactivateUser = null"
        >
          <iconify icon="ph:x-bold" />
          <span>{{ t('common.cancel') }}</span>
        </prime-button>
        <prime-button
          severity="danger"
          size="small"
          :loading="deactivateLoading"
          @click="confirmAndDeactivate"
        >
          <iconify icon="ph:prohibit-bold" />
          <span>{{ t('users.deactivateDialog.deactivate') }}</span>
        </prime-button>
      </template>
    </prime-dialog>

  </section>
</template>
