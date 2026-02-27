<script setup>
import { computed, nextTick, onMounted, ref, watch } from 'vue'
import { onBeforeRouteLeave } from 'vue-router'
import { usePermission } from '@/composables/usePermission'
import { useTableCache } from '@/composables/useTableCache'
import AppTable from '@/components/AppTable.vue'
import {
  listTables,
  createTable,
  updateTable,
  toggleActive,
  deleteTable,
  markAvailable,
  closeSession,
} from '@/services/table.service'
import QRCode from 'qrcode'

const cache = useTableCache('tables')
const { can } = usePermission()

// ── Table state ────────────────────────────────────────────────────
const loading      = ref(false)
const errorMessage = ref('')
const tables       = ref([])
const rows         = ref(20)
const first        = ref(0)

// ── Filters ────────────────────────────────────────────────────────
const statusFilter   = ref(null)
const activeFilter   = ref(null)

const statusOptions = [
  { label: 'Available', value: 'Available' },
  { label: 'Occupied',  value: 'Occupied'  },
  { label: 'Cleaning',  value: 'Cleaning'  },
]

const activeOptions = [
  { label: 'Active',   value: true  },
  { label: 'Inactive', value: false },
]

// ── Summary ────────────────────────────────────────────────────────
const summary = computed(() => {
  const all = tables.value
  return {
    total:     all.length,
    available: all.filter(t => t.status === 'Available').length,
    occupied:  all.filter(t => t.status === 'Occupied').length,
    cleaning:  all.filter(t => t.status === 'Cleaning').length,
  }
})

// ── Filtered rows (client-side) ─────────────────────────────────
const filtered = computed(() => {
  return tables.value.filter(t => {
    if (statusFilter.value !== null && t.status !== statusFilter.value) return false
    if (activeFilter.value !== null && t.isActive !== activeFilter.value) return false
    return true
  })
})

const pagedRows = computed(() => {
  return filtered.value.slice(first.value, first.value + rows.value)
})

// ── Load ───────────────────────────────────────────────────────────
const load = async () => {
  loading.value = true
  errorMessage.value = ''
  try {
    const res = await listTables()
    tables.value = res.data ?? []
  } catch (err) {
    errorMessage.value = err?.response?.data?.title ?? 'Failed to load tables.'
  } finally {
    loading.value = false
  }
}

// ── Actions ────────────────────────────────────────────────────────
const handleToggleActive = async (row) => {
  try {
    await toggleActive(row.id)
    await load()
  } catch (err) {
    errorMessage.value = err?.response?.data?.title ?? `Failed to update table ${row.code}.`
  }
}

const handleMarkAvailable = async (row) => {
  try {
    await markAvailable(row.id)
    await load()
  } catch (err) {
    errorMessage.value = err?.response?.data?.title ?? `Failed to mark table ${row.code} available.`
  }
}

const handleCloseSession = async (row) => {
  if (!row.activeSessionId) return
  try {
    await closeSession(row.activeSessionId)
    await load()
  } catch (err) {
    errorMessage.value = err?.response?.data?.title ?? `Failed to close session on table ${row.code}.`
  }
}

const handleDelete = async (row) => {
  try {
    await deleteTable(row.id)
    await load()
  } catch (err) {
    errorMessage.value = err?.response?.data?.title ?? `Failed to delete table ${row.code}.`
  }
}

// ── Add table dialog ───────────────────────────────────────────────
const showAddDialog = ref(false)
const newNumber     = ref(null)
const newCode       = ref('')
const addError      = ref('')
const addLoading    = ref(false)

const openAddDialog = () => {
  newNumber.value     = null
  newCode.value       = ''
  addError.value      = ''
  showAddDialog.value = true
}

const handleAddTable = async () => {
  if (!newNumber.value || newNumber.value <= 0) {
    addError.value = 'Table number must be greater than 0.'
    return
  }
  if (!newCode.value.trim()) {
    addError.value = 'Table code is required.'
    return
  }
  addLoading.value = true
  addError.value   = ''
  try {
    await createTable(newNumber.value, newCode.value.trim())
    await load()
    showAddDialog.value = false
    newNumber.value     = null
    newCode.value       = ''
  } catch (err) {
    addError.value =
      err?.response?.data?.errors?.join(', ') ||
      err?.response?.data?.title ||
      'Failed to create table. Code or number may already exist.'
  } finally {
    addLoading.value = false
  }
}

// ── Edit dialog ────────────────────────────────────────────────────
const showEditDialog = ref(false)
const editRow        = ref(null)
const editNumber     = ref(null)
const editCode       = ref('')
const editError      = ref('')
const editLoading    = ref(false)

const openEditDialog = (row) => {
  editRow.value        = row
  editNumber.value     = row.number
  editCode.value       = row.code
  editError.value      = ''
  showEditDialog.value = true
}

const handleEditTable = async () => {
  if (!editNumber.value || editNumber.value <= 0) {
    editError.value = 'Table number must be greater than 0.'
    return
  }
  if (!editCode.value.trim()) {
    editError.value = 'Table code is required.'
    return
  }
  editLoading.value = true
  editError.value   = ''
  try {
    await updateTable(editRow.value.id, { number: editNumber.value, code: editCode.value.trim() })
    await load()
    showEditDialog.value = false
  } catch (err) {
    editError.value =
      err?.response?.data?.errors?.join(', ') ||
      err?.response?.data?.title ||
      'Failed to update table. Code or number may already exist.'
  } finally {
    editLoading.value = false
  }
}

// ── QR dialog ──────────────────────────────────────────────────────
const showQrDialog = ref(false)
const qrRow        = ref(null)
const qrCanvas     = ref(null)
const qrUrl        = ref('')

const openQrDialog = async (row) => {
  qrRow.value        = row
  qrUrl.value        = `${import.meta.env.VITE_ORDERING_BASE_URL ?? ''}/table/${row.code}`
  showQrDialog.value = true
  await nextTick()
  if (qrCanvas.value) {
    await QRCode.toCanvas(qrCanvas.value, qrUrl.value, {
      width: 240,
      margin: 2,
      color: { dark: '#000000', light: '#ffffff' },
    })
  }
}

const downloadQr = () => {
  if (!qrCanvas.value) return
  const link = document.createElement('a')
  link.download = `table-${qrRow.value.code}.png`
  link.href = qrCanvas.value.toDataURL('image/png')
  link.click()
}

// ── Tag helpers ────────────────────────────────────────────────────
const statusTag = (status) => {
  switch (status) {
    case 'Available': return { label: 'Available', severity: 'success' }
    case 'Occupied':  return { label: 'Occupied',  severity: 'info'    }
    case 'Cleaning':  return { label: 'Cleaning',  severity: 'warn'    }
    default:          return { label: status,       severity: 'secondary' }
  }
}

const activeTag = (isActive) =>
  isActive
    ? { label: 'Active',   severity: 'success' }
    : { label: 'Inactive', severity: 'danger'  }

// ── Filter helpers ─────────────────────────────────────────────────
const activeFilterCount = computed(() => {
  let n = 0
  if (statusFilter.value !== null) n++
  if (activeFilter.value !== null) n++
  return n
})

const clearFilters = () => {
  statusFilter.value = null
  activeFilter.value = null
  first.value = 0
}

const filterPanel = ref(null)

// ── Cache ──────────────────────────────────────────────────────────
onMounted(() => {
  const cached = cache.restore()
  if (cached) {
    rows.value         = cached.rows         ?? 20
    first.value        = cached.first        ?? 0
    statusFilter.value = cached.statusFilter ?? null
    activeFilter.value = cached.activeFilter ?? null
  }
  load()
})

onBeforeRouteLeave(() => {
  cache.save({ rows: rows.value, first: first.value, statusFilter: statusFilter.value, activeFilter: activeFilter.value })
})

watch([statusFilter, activeFilter], () => { first.value = 0 })
</script>

<template>
  <!-- ── Add table dialog ─────────────────────────────────────────── -->
  <prime-dialog
    v-model:visible="showAddDialog"
    header="Add table"
    :modal="true"
    :style="{ width: '24rem' }"
  >
    <div class="tw:space-y-4">
      <div class="tw:space-y-1.5">
        <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">Table code</label>
        <prime-input-text
          v-model="newCode"
          placeholder="e.g. F1-01, BM-02"
          class="app-input tw:w-full tw:font-mono"
          @keyup.enter="handleAddTable"
        />
        <p class="tw:text-[11px] app-text-subtle">Format tự do — ví dụ: F1-01 (tầng 1 bàn 1)</p>
      </div>
      <div class="tw:space-y-1.5">
        <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">Table number</label>
        <prime-input-number
          v-model="newNumber"
          :min="1"
          :use-grouping="false"
          placeholder="e.g. 1"
          class="app-input tw:w-full"
          @keyup.enter="handleAddTable"
        />
        <p class="tw:text-[11px] app-text-subtle">Dùng để sắp xếp thứ tự bàn</p>
      </div>
      <p v-if="addError" class="tw:text-xs tw:text-red-400">{{ addError }}</p>
    </div>
    <template #footer>
      <prime-button severity="secondary" outlined @click="showAddDialog = false">Cancel</prime-button>
      <prime-button severity="success" :loading="addLoading" @click="handleAddTable">
        <iconify icon="ph:plus-bold" />
        <span>Add table</span>
      </prime-button>
    </template>
  </prime-dialog>

  <!-- ── Edit dialog ──────────────────────────────────────────────── -->
  <prime-dialog
    v-model:visible="showEditDialog"
    header="Edit table"
    :modal="true"
    :style="{ width: '24rem' }"
  >
    <div class="tw:space-y-4">
      <div class="tw:space-y-1.5">
        <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">Table code</label>
        <prime-input-text
          v-model="editCode"
          placeholder="e.g. F1-01"
          class="app-input tw:w-full tw:font-mono"
          @keyup.enter="handleEditTable"
        />
      </div>
      <div class="tw:space-y-1.5">
        <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">Table number</label>
        <prime-input-number
          v-model="editNumber"
          :min="1"
          :use-grouping="false"
          class="app-input tw:w-full"
          @keyup.enter="handleEditTable"
        />
      </div>
      <p v-if="editError" class="tw:text-xs tw:text-red-400">{{ editError }}</p>
    </div>
    <template #footer>
      <prime-button severity="secondary" outlined @click="showEditDialog = false">Cancel</prime-button>
      <prime-button severity="primary" :loading="editLoading" @click="handleEditTable">
        <iconify icon="ph:check-bold" />
        <span>Save changes</span>
      </prime-button>
    </template>
  </prime-dialog>

  <!-- ── QR dialog ────────────────────────────────────────────────── -->
  <prime-dialog
    v-model:visible="showQrDialog"
    :header="`QR — ${qrRow?.code ?? ''}`"
    :modal="true"
    :style="{ width: '20rem' }"
  >
    <div class="tw:flex tw:flex-col tw:items-center tw:gap-4">
      <div class="tw:rounded-xl tw:bg-white tw:p-3">
        <canvas ref="qrCanvas" />
      </div>
      <p class="tw:text-xs tw:font-mono app-text-muted tw:text-center tw:break-all tw:px-2">
        {{ qrUrl }}
      </p>
    </div>
    <template #footer>
      <prime-button severity="secondary" outlined @click="showQrDialog = false">Close</prime-button>
      <prime-button severity="primary" @click="downloadQr">
        <iconify icon="ph:download-bold" />
        <span>Download PNG</span>
      </prime-button>
    </template>
  </prime-dialog>

  <section class="tw:space-y-8">
    <!-- ── Header ────────────────────────────────────────────────── -->
    <div class="tw:flex tw:flex-wrap tw:items-end tw:justify-between tw:gap-4">
      <div>
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300">Operations</p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">Tables</h1>
        <p class="tw:mt-2 tw:text-sm app-text-muted">Manage dining tables, sessions, and availability.</p>
      </div>
      <prime-button
        v-if="can('table.create')"
        severity="success"
        size="small"
        @click="openAddDialog"
      >
        <iconify icon="ph:plus-bold" />
        <span>Add table</span>
      </prime-button>
    </div>

    <!-- ── Summary stats ─────────────────────────────────────────── -->
    <div class="tw:grid tw:grid-cols-2 tw:gap-3 tw:lg:grid-cols-4">
      <prime-card class="app-card tw:rounded-xl tw:border">
        <template #content>
          <p class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] app-text-subtle">Total</p>
          <p class="tw:mt-2 tw:text-2xl tw:font-semibold">{{ summary.total }}</p>
        </template>
      </prime-card>
      <prime-card class="app-card tw:rounded-xl tw:border">
        <template #content>
          <p class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] tw:text-emerald-400">Available</p>
          <p class="tw:mt-2 tw:text-2xl tw:font-semibold">{{ summary.available }}</p>
        </template>
      </prime-card>
      <prime-card class="app-card tw:rounded-xl tw:border">
        <template #content>
          <p class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] tw:text-blue-400">Occupied</p>
          <p class="tw:mt-2 tw:text-2xl tw:font-semibold">{{ summary.occupied }}</p>
        </template>
      </prime-card>
      <prime-card class="app-card tw:rounded-xl tw:border">
        <template #content>
          <p class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] tw:text-yellow-400">Cleaning</p>
          <p class="tw:mt-2 tw:text-2xl tw:font-semibold">{{ summary.cleaning }}</p>
        </template>
      </prime-card>
    </div>

    <!-- ── Error ──────────────────────────────────────────────────── -->
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
      :value="pagedRows"
      :loading="loading"
      :totalRecords="filtered.length"
      :rowsPerPageOptions="[10, 20, 50]"
      @page="(e) => (first = e.first)"
    >
      <template #toolbar-left>
        <div class="tw:flex tw:items-center tw:gap-2">
          <prime-button
            :severity="activeFilterCount > 0 ? 'success' : 'secondary'"
            :outlined="activeFilterCount === 0"
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

          <prime-popover ref="filterPanel">
            <div class="tw:flex tw:flex-col tw:gap-4">
              <p class="tw:text-sm tw:font-semibold">Filter tables</p>

              <div class="tw:space-y-1.5">
                <label class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest">Status</label>
                <prime-select
                  v-model="statusFilter"
                  :options="statusOptions"
                  option-label="label"
                  option-value="value"
                  placeholder="All statuses"
                  show-clear
                  class="app-input tw:w-full"
                />
              </div>

              <div class="tw:space-y-1.5">
                <label class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest">Active</label>
                <prime-select
                  v-model="activeFilter"
                  :options="activeOptions"
                  option-label="label"
                  option-value="value"
                  placeholder="All"
                  show-clear
                  class="app-input tw:w-full"
                />
              </div>

              <prime-button
                v-if="activeFilterCount > 0"
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

      <!-- Code (primary identifier) -->
      <prime-column field="code" header="Code" style="min-width: 8rem">
        <template #body="{ data }">
          <span class="tw:font-mono tw:font-semibold tw:text-sm">{{ data.code }}</span>
        </template>
      </prime-column>

      <prime-column field="number" header="#" style="min-width: 4rem">
        <template #body="{ data }">
          <span class="tw:text-sm app-text-muted">{{ data.number }}</span>
        </template>
      </prime-column>

      <prime-column field="status" header="Status" style="min-width: 8rem">
        <template #body="{ data }">
          <prime-tag
            :value="statusTag(data.status).label"
            :severity="statusTag(data.status).severity"
          />
        </template>
      </prime-column>

      <prime-column field="isActive" header="Active" style="min-width: 7rem">
        <template #body="{ data }">
          <prime-tag
            :value="activeTag(data.isActive).label"
            :severity="activeTag(data.isActive).severity"
          />
        </template>
      </prime-column>

      <prime-column field="activeSessionId" header="Session" style="min-width: 8rem">
        <template #body="{ data }">
          <span v-if="data.activeSessionId" class="tw:text-xs app-text-muted tw:font-mono">
            {{ data.activeSessionId.slice(0, 8) }}…
          </span>
          <span v-else class="app-text-subtle">—</span>
        </template>
      </prime-column>

      <prime-column header="Actions" style="min-width: 18rem">
        <template #body="{ data }">
          <div class="tw:flex tw:flex-wrap tw:gap-2">
            <!-- Close session -->
            <prime-button
              v-if="data.status === 'Occupied' && data.activeSessionId"
              severity="info"
              outlined
              size="small"
              v-tooltip.top="'Close session'"
              @click="handleCloseSession(data)"
            >
              <iconify icon="ph:x-circle-bold" />
              <span>Close session</span>
            </prime-button>

            <!-- Mark available -->
            <prime-button
              v-if="data.status === 'Cleaning'"
              severity="success"
              outlined
              size="small"
              v-tooltip.top="'Mark as available'"
              @click="handleMarkAvailable(data)"
            >
              <iconify icon="ph:check-circle-bold" />
              <span>Available</span>
            </prime-button>

            <!-- Edit -->
            <prime-button
              v-if="can('table.create')"
              severity="secondary"
              outlined
              size="small"
              v-tooltip.top="'Edit table'"
              @click="openEditDialog(data)"
            >
              <iconify icon="ph:pencil-bold" />
            </prime-button>

            <!-- QR -->
            <prime-button
              severity="secondary"
              outlined
              size="small"
              v-tooltip.top="'Show QR code'"
              @click="openQrDialog(data)"
            >
              <iconify icon="ph:qr-code-bold" />
            </prime-button>

            <!-- Toggle active -->
            <prime-button
              v-if="data.status !== 'Occupied'"
              severity="secondary"
              outlined
              size="small"
              @click="handleToggleActive(data)"
            >
              <iconify :icon="data.isActive ? 'ph:toggle-right-bold' : 'ph:toggle-left-bold'" />
              <span>{{ data.isActive ? 'Deactivate' : 'Activate' }}</span>
            </prime-button>

            <!-- Delete -->
            <prime-button
              v-if="data.status !== 'Occupied' && can('table.delete')"
              severity="danger"
              outlined
              size="small"
              v-tooltip.top="'Delete table'"
              @click="handleDelete(data)"
            >
              <iconify icon="ph:trash-bold" />
            </prime-button>
          </div>
        </template>
      </prime-column>
    </AppTable>
  </section>
</template>
