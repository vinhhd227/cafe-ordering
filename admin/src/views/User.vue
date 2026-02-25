<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { useAuthStore } from '@/stores/auth'
import {
  getUsers,
  createUser,
  updateUser,
  activateUser,
  deactivateUser,
  changeUserRole,
} from '@/services/user.service'

// --- Auth / Permissions ---
const auth = useAuthStore()
const canManage = computed(() => (auth.user?.roles ?? []).includes('Admin'))

// --- Table state ---
const users = ref([])
const loading = ref(false)
const errorMessage = ref('')
const rows = ref(20)
const first = ref(0)
const totalRecords = ref(0)
const searchTimer = ref(null)

// --- Filters ---
const search = ref('')
const roleFilter = ref(null)
const statusFilter = ref(null)

// --- Summary stats ---
const stats = ref({ total: 0, active: 0, admins: 0, staff: 0 })

// --- Dialog: Add User ---
const showAddDialog = ref(false)
const addForm = ref({ username: '', fullName: '', role: 'Staff' })
const addLoading = ref(false)
const addError = ref('')

// --- Dialog: Temp Password (after create) ---
const showTempPasswordDialog = ref(false)
const tempPasswordData = ref({ username: '', temporaryPassword: '' })

// --- Dialog: Edit User ---
const showEditDialog = ref(false)
const editForm = ref({ id: null, username: '', fullName: '', email: '' })
const editLoading = ref(false)
const editError = ref('')

// --- Dialog: Change Role ---
const showRoleDialog = ref(false)
const roleForm = ref({ id: null, username: '', role: 'Staff' })
const roleLoading = ref(false)
const roleError = ref('')

// --- Dialog: Deactivate Confirm ---
const confirmDeactivateUser = ref(null)
const deactivateLoading = ref(false)

// --- Constants ---
const roleFilterOptions = [
  { label: 'All roles', value: null },
  { label: 'Admin', value: 'Admin' },
  { label: 'Staff', value: 'Staff' },
]

const statusFilterOptions = [
  { label: 'All statuses', value: null },
  { label: 'Active', value: true },
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
  loading.value = true
  errorMessage.value = ''
  try {
    const res = await getUsers({
      page,
      pageSize: rows.value,
      search: search.value.trim() || undefined,
      role: roleFilter.value ?? undefined,
      isActive: statusFilter.value ?? undefined,
    })
    const data = res?.data ?? {}
    users.value = data.items ?? []
    totalRecords.value = data.total ?? 0
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
      total: totalRes?.data?.total ?? 0,
      active: activeRes?.data?.total ?? 0,
      admins: adminRes?.data?.total ?? 0,
      staff: staffRes?.data?.total ?? 0,
    }
  } catch {
    // stats are non-critical
  }
}

onMounted(() => {
  loadUsers()
  loadStats()
})

watch([search, roleFilter, statusFilter], () => {
  clearTimeout(searchTimer.value)
  searchTimer.value = setTimeout(() => {
    first.value = 0
    loadUsers(1)
  }, 400)
})

// --- Add User ---
const openAddDialog = () => {
  addForm.value = { username: '', fullName: '', role: 'Staff' }
  addError.value = ''
  showAddDialog.value = true
}

const submitAddUser = async () => {
  addLoading.value = true
  addError.value = ''
  try {
    const res = await createUser(addForm.value)
    tempPasswordData.value = res.data
    showAddDialog.value = false
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

// --- Edit User ---
const openEditDialog = (user) => {
  editForm.value = {
    id: user.id,
    username: user.username,
    fullName: user.fullName,
    email: user.email ?? '',
  }
  editError.value = ''
  showEditDialog.value = true
}

const submitEditUser = async () => {
  editLoading.value = true
  editError.value = ''
  try {
    await updateUser(editForm.value.id, {
      fullName: editForm.value.fullName,
      email: editForm.value.email || null,
    })
    showEditDialog.value = false
    loadUsers()
  } catch (err) {
    editError.value = extractError(err)
  } finally {
    editLoading.value = false
  }
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

// --- Change Role ---
const openRoleDialog = (user) => {
  roleForm.value = {
    id: user.id,
    username: user.username,
    role: user.roles?.[0] ?? 'Staff',
  }
  roleError.value = ''
  showRoleDialog.value = true
}

const submitChangeRole = async () => {
  roleLoading.value = true
  roleError.value = ''
  try {
    await changeUserRole(roleForm.value.id, roleForm.value.role)
    showRoleDialog.value = false
    loadUsers()
    loadStats()
  } catch (err) {
    roleError.value = extractError(err)
  } finally {
    roleLoading.value = false
  }
}
</script>

<template>
  <section class="tw:space-y-6">
    <!-- Page Header -->
    <div class="tw:flex tw:flex-wrap tw:items-end tw:justify-between tw:gap-4">
      <div>
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300">Users</p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">User management</h1>
        <p class="tw:mt-2 tw:text-sm app-text-muted">
          Maintain access, roles, and account status.
        </p>
      </div>
      <prime-button
        v-if="canManage"
        label="Add user"
        icon="pi pi-user-plus"
        severity="success"
        size="small"
        @click="openAddDialog"
      />
    </div>

    <!-- Summary Stats -->
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

    <!-- Error Banner -->
    <prime-message
      v-if="errorMessage"
      severity="error"
      size="small"
      variant="simple"
      :closable="true"
      @close="errorMessage = ''"
    >
      {{ errorMessage }}
    </prime-message>

    <!-- Main Table Card -->
    <prime-card class="app-card tw:rounded-2xl tw:border">
      <template #content>
        <!-- Toolbar -->
        <div class="tw:flex tw:flex-wrap tw:items-center tw:gap-3">
          <prime-input-text
            v-model="search"
            placeholder="Search users..."
            class="app-input tw:w-64"
          />
          <prime-select
            v-model="roleFilter"
            :options="roleFilterOptions"
            optionLabel="label"
            optionValue="value"
            class="app-input tw:w-36"
          />
          <prime-select
            v-model="statusFilter"
            :options="statusFilterOptions"
            optionLabel="label"
            optionValue="value"
            class="app-input tw:w-40"
          />
        </div>

        <!-- Data Table -->
        <prime-data-table
          class="tw:mt-6"
          :value="users"
          :loading="loading"
          :paginator="true"
          :rows="rows"
          :first="first"
          :totalRecords="totalRecords"
          :rowsPerPageOptions="[10, 20, 50]"
          @page="(e) => { first = e.first; rows = e.rows; loadUsers(e.page + 1) }"
          responsiveLayout="scroll"
        >
          <!-- User column: avatar + username + fullName -->
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

          <!-- Actions (Admin only) -->
          <prime-column v-if="canManage" header="Actions" style="min-width: 11rem">
            <template #body="{ data }">
              <div class="tw:flex tw:gap-2">
                <prime-button
                  icon="pi pi-pencil"
                  severity="secondary"
                  outlined
                  size="small"
                  v-tooltip.top="'Edit'"
                  @click="openEditDialog(data)"
                />
                <prime-button
                  :icon="data.isActive ? 'pi pi-ban' : 'pi pi-check-circle'"
                  :severity="data.isActive ? 'danger' : 'success'"
                  outlined
                  size="small"
                  v-tooltip.top="data.isActive ? 'Deactivate' : 'Activate'"
                  @click="handleToggleActive(data)"
                />
                <prime-button
                  icon="pi pi-shield"
                  severity="secondary"
                  outlined
                  size="small"
                  v-tooltip.top="'Change role'"
                  @click="openRoleDialog(data)"
                />
              </div>
            </template>
          </prime-column>
        </prime-data-table>
      </template>
    </prime-card>

    <!-- ===== Add User Dialog ===== -->
    <prime-dialog
      v-model:visible="showAddDialog"
      header="Add user"
      :modal="true"
      style="width: 28rem"
    >
      <div class="tw:space-y-4 tw:pt-2">
        <prime-message
          v-if="addError"
          severity="error"
          size="small"
          variant="simple"
          :closable="false"
        >{{ addError }}</prime-message>

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
          label="Cancel"
          severity="secondary"
          outlined
          size="small"
          @click="showAddDialog = false"
        />
        <prime-button
          label="Create"
          severity="success"
          size="small"
          :loading="addLoading"
          @click="submitAddUser"
        />
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
                icon="pi pi-copy"
                severity="secondary"
                outlined
                size="small"
                v-tooltip.top="'Copy'"
                @click="copyTempPassword"
              />
            </div>
          </div>
        </div>
      </div>

      <template #footer>
        <prime-button
          label="Done"
          severity="success"
          size="small"
          @click="showTempPasswordDialog = false"
        />
      </template>
    </prime-dialog>

    <!-- ===== Edit User Dialog ===== -->
    <prime-dialog
      v-model:visible="showEditDialog"
      header="Edit user"
      :modal="true"
      style="width: 28rem"
    >
      <div class="tw:space-y-4 tw:pt-2">
        <prime-message
          v-if="editError"
          severity="error"
          size="small"
          variant="simple"
          :closable="false"
        >{{ editError }}</prime-message>

        <div class="tw:space-y-1">
          <label class="tw:text-sm tw:font-medium">Username</label>
          <prime-input-text
            :model-value="editForm.username"
            class="app-input tw:w-full tw:opacity-50 tw:cursor-not-allowed"
            readonly
          />
        </div>
        <div class="tw:space-y-1">
          <label class="tw:text-sm tw:font-medium">Full name</label>
          <prime-input-text
            v-model="editForm.fullName"
            class="app-input tw:w-full"
          />
        </div>
        <div class="tw:space-y-1">
          <label class="tw:text-sm tw:font-medium">Email <span class="app-text-muted tw:font-normal">(optional)</span></label>
          <prime-input-text
            v-model="editForm.email"
            class="app-input tw:w-full"
            placeholder="user@example.com"
          />
        </div>
      </div>

      <template #footer>
        <prime-button
          label="Cancel"
          severity="secondary"
          outlined
          size="small"
          @click="showEditDialog = false"
        />
        <prime-button
          label="Save"
          severity="success"
          size="small"
          :loading="editLoading"
          @click="submitEditUser"
        />
      </template>
    </prime-dialog>

    <!-- ===== Change Role Dialog ===== -->
    <prime-dialog
      v-model:visible="showRoleDialog"
      header="Change role"
      :modal="true"
      style="width: 24rem"
    >
      <div class="tw:space-y-4 tw:pt-2">
        <prime-message
          v-if="roleError"
          severity="error"
          size="small"
          variant="simple"
          :closable="false"
        >{{ roleError }}</prime-message>

        <p class="tw:text-sm app-text-muted">
          Changing role for <strong class="tw:font-semibold">{{ roleForm.username }}</strong>.
        </p>
        <div class="tw:space-y-1">
          <label class="tw:text-sm tw:font-medium">New role</label>
          <prime-select
            v-model="roleForm.role"
            :options="roleSelectOptions"
            optionLabel="label"
            optionValue="value"
            class="app-input tw:w-full"
          />
        </div>
      </div>

      <template #footer>
        <prime-button
          label="Cancel"
          severity="secondary"
          outlined
          size="small"
          @click="showRoleDialog = false"
        />
        <prime-button
          label="Confirm"
          severity="warning"
          size="small"
          :loading="roleLoading"
          @click="submitChangeRole"
        />
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
          label="Cancel"
          severity="secondary"
          outlined
          size="small"
          @click="confirmDeactivateUser = null"
        />
        <prime-button
          label="Deactivate"
          severity="danger"
          size="small"
          :loading="deactivateLoading"
          @click="confirmAndDeactivate"
        />
      </template>
    </prime-dialog>
  </section>
</template>
