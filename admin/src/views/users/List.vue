<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter, onBeforeRouteLeave } from 'vue-router'
import {
  getUsers,
  createUser,
  activateUser,
  deactivateUser,
} from '@/services/user.service'
import AppTable from '@/components/AppTable.vue'
import { useTableCache } from '@/composables/useTableCache'
import { usePermission } from '@/composables/usePermission'
import { btnIcon } from "@/layout/ui";

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

const statusFilterOptions = [
  { label: 'Active',   value: true  },
  { label: 'Inactive', value: false },
]

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
  'Something went wrong.'

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
</script>

<template>
  <section class="tw:space-y-6">

    <!-- ── Page Header ───────────────────────────────────────────── -->
    <div class="tw:flex tw:flex-wrap tw:items-end tw:justify-between tw:gap-4">
      <div>
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300">Users</p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">User management</h1>
        <p class="tw:mt-2 tw:text-sm app-text-muted">
          Maintain access, roles, and account status.
        </p>
      </div>
      <prime-button
        v-if="can('user.create')"
        severity="success"
        size="small"
        @click="openAddDialog"
      >
        <iconify icon="ph:user-plus-bold" />
        <span>Add user</span>
      </prime-button>
    </div>

    <!-- ── Summary Stats ─────────────────────────────────────────── -->
    <div class="tw:grid tw:grid-cols-2 tw:gap-3 tw:md:grid-cols-4">
      <prime-card class="app-card tw:rounded-xl tw:border">
        <template #content>
          <p class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] app-text-subtle">Total users</p>
          <p class="tw:mt-2 tw:text-2xl tw:font-semibold">{{ stats.total }}</p>
        </template>
      </prime-card>
      <prime-card class="app-card tw:rounded-xl tw:border">
        <template #content>
          <p class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] tw:text-emerald-400">Active</p>
          <p class="tw:mt-2 tw:text-2xl tw:font-semibold">{{ stats.active }}</p>
        </template>
      </prime-card>
      <prime-card class="app-card tw:rounded-xl tw:border">
        <template #content>
          <p class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] tw:text-red-400">Admins</p>
          <p class="tw:mt-2 tw:text-2xl tw:font-semibold">{{ stats.admins }}</p>
        </template>
      </prime-card>
      <prime-card class="app-card tw:rounded-xl tw:border">
        <template #content>
          <p class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] tw:text-blue-400">Staff</p>
          <p class="tw:mt-2 tw:text-2xl tw:font-semibold">{{ stats.staff }}</p>
        </template>
      </prime-card>
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
      @page="(e) => loadUsers(e.page + 1)"
    >
      <template #toolbar-left>
        <div class="tw:flex tw:items-center tw:gap-2">
          <!-- Search -->
          <prime-input-text
            v-model="search"
            placeholder="Search users…"
            class="app-input tw:w-64"
          />

          <!-- Filter toggle button -->
          <prime-button
            :severity="hasActiveFilters ? 'success' : 'secondary'"
            :outlined="!hasActiveFilters"
            v-tooltip.top="'Filters'"
            @click="filterPanel.toggle($event)"
          >
            <iconify icon="ph:funnel-bold" />
            <prime-badge
              v-if="activeFilterCount > 0"
              :value="activeFilterCount"
              severity="danger"
              class="tw:ml-1 tw:scale-90"
            />
          </prime-button>

          <!-- Filter popover -->
          <prime-popover ref="filterPanel">
            <div class="tw:flex tw:flex-col tw:gap-4">
              <p class="tw:text-sm tw:font-semibold">Filter users</p>

              <div class="tw:space-y-1.5">
                <label class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest">Role</label>
                <prime-select
                  v-model="roleFilter"
                  :options="roleFilterOptions"
                  option-label="label"
                  option-value="value"
                  placeholder="All roles"
                  show-clear
                  class="app-input tw:w-full"
                />
              </div>

              <div class="tw:space-y-1.5">
                <label class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest">Status</label>
                <prime-select
                  v-model="statusFilter"
                  :options="statusFilterOptions"
                  option-label="label"
                  option-value="value"
                  placeholder="All statuses"
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
                <span>Clear filters</span>
              </prime-button>
            </div>
          </prime-popover>
        </div>
      </template>

      <!-- User: avatar + username + fullName -->
      <prime-column header="User" style="min-width: 14rem">
        <template #body="{ data }">
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
      </prime-column>

      <!-- Email -->
      <prime-column header="Email">
        <template #body="{ data }">
          <span class="tw:text-sm app-text-muted">{{ data.email || '—' }}</span>
        </template>
      </prime-column>

      <!-- Role -->
      <prime-column header="Role" style="min-width: 8rem">
        <template #body="{ data }">
          <prime-tag
            v-for="role in data.roles"
            :key="role"
            :value="role"
            :severity="roleSeverity(role)"
            class="tw:mr-1"
          />
          <span v-if="!data.roles?.length" class="app-text-muted tw:text-xs">—</span>
        </template>
      </prime-column>

      <!-- Status -->
      <prime-column header="Status">
        <template #body="{ data }">
          <prime-tag
            :value="data.isActive ? 'Active' : 'Inactive'"
            :severity="data.isActive ? 'success' : 'danger'"
          />
        </template>
      </prime-column>

      <!-- Created date -->
      <prime-column header="Created">
        <template #body="{ data }">
          <span class="tw:text-xs app-text-muted">{{ formatDate(data.createdAt) }}</span>
        </template>
      </prime-column>

      <!-- Actions -->
      <prime-column header="Actions" style="min-width: 8rem">
        <template #body="{ data }">
          <div class="tw:flex tw:gap-2">
           

            <!-- Quick toggle active -->
            <prime-button
              v-if="can('user.deactivate')"
              :severity="data.isActive ? 'danger' : 'success'"
              outlined
              size="small"
              v-tooltip.top="data.isActive ? 'Deactivate' : 'Activate'"
              @click="handleToggleActive(data)"
            >
              <iconify :icon="data.isActive ? 'ph:prohibit-bold' : 'ph:check-circle-bold'" />
            </prime-button>
             <!-- View / Edit detail -->
            <prime-button
              severity="secondary"
              outlined
              size="small"
              v-tooltip.top="'View / Edit'"
              @click="router.push({ name: 'userDetail', params: { id: data.id } })"
            >
              <iconify icon="ph:arrow-right-bold" />
            </prime-button>
          </div>
        </template>
      </prime-column>
    </AppTable>

    <!-- ===== Add User Dialog ===== -->
    <prime-dialog
      v-model:visible="showAddDialog"
      header="Add user"
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
          <label class="tw:text-sm tw:font-medium">Username</label>
          <prime-input-text
            v-model="addForm.username"
            class="app-input tw:w-full"
            placeholder="e.g. barista01"
          />
        </div>
        <div class="tw:space-y-1">
          <label class="tw:text-sm tw:font-medium">Full name</label>
          <prime-input-text
            v-model="addForm.fullName"
            class="app-input tw:w-full"
            placeholder="e.g. Nguyen Van A"
          />
        </div>
        <div class="tw:space-y-1">
          <label class="tw:text-sm tw:font-medium">Role</label>
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
          <span>Cancel</span>
        </prime-button>
        <prime-button
          severity="success"
          size="small"
          :loading="addLoading"
          @click="submitAddUser"
        >
          <iconify icon="ph:user-plus-bold" />
          <span>Create</span>
        </prime-button>
      </template>
    </prime-dialog>

    <!-- ===== Temp Password Dialog ===== -->
    <prime-dialog
      v-model:visible="showTempPasswordDialog"
      header="Account created"
      :modal="true"
      style="width: 26rem"
    >
      <div class="tw:space-y-4 tw:pt-2">
        <p class="tw:text-sm app-text-muted">
          Share these credentials securely. The password cannot be retrieved again.
        </p>
        <div class="tw:rounded-xl tw:border tw:p-4 tw:space-y-3 app-card">
          <div class="tw:space-y-0.5">
            <p class="tw:text-[10px] tw:uppercase tw:tracking-widest app-text-subtle">Username</p>
            <p class="tw:font-mono tw:font-semibold">{{ tempPasswordData.username }}</p>
          </div>
          <div class="tw:space-y-0.5">
            <p class="tw:text-[10px] tw:uppercase tw:tracking-widest app-text-subtle">Temporary password</p>
            <div class="tw:flex tw:items-center tw:gap-2">
              <p class="tw:font-mono tw:font-bold tw:text-xl tw:tracking-widest tw:text-emerald-300">
                {{ tempPasswordData.temporaryPassword }}
              </p>
              <prime-button
                severity="secondary"
                outlined
                size="small"
                v-tooltip.top="'Copy'"
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
          <span>Done</span>
        </prime-button>
      </template>
    </prime-dialog>

    <!-- ===== Deactivate Confirm Dialog ===== -->
    <prime-dialog
      :visible="!!confirmDeactivateUser"
      header="Deactivate account"
      :modal="true"
      style="width: 24rem"
      @update:visible="(v) => { if (!v) confirmDeactivateUser = null }"
    >
      <div class="tw:pt-2">
        <p class="tw:text-sm app-text-muted">
          Deactivate <strong class="tw:font-semibold">{{ confirmDeactivateUser?.username }}</strong>?
          They will be immediately logged out and cannot log in until reactivated.
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
          <span>Cancel</span>
        </prime-button>
        <prime-button
          severity="danger"
          size="small"
          :loading="deactivateLoading"
          @click="confirmAndDeactivate"
        >
          <iconify icon="ph:prohibit-bold" />
          <span>Deactivate</span>
        </prime-button>
      </template>
    </prime-dialog>

  </section>
</template>
