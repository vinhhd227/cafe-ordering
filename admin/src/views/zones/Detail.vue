<script setup>
import { getZone, updateZone, activateZone, deactivateZone } from '@/services/zone.service'
import { listTables, updateTable } from '@/services/table.service'
import { btnIcon } from '@/layout/ui'
import { usePermission } from '@/composables/usePermission'

const router = useRouter()
const route = useRoute()
const toast = useToast()
const { can } = usePermission()

const zoneId = computed(() => Number(route.params.id))

// ── State ────────────────────────────────────────────────────────
const zone = ref(null)
const allTables = ref([])
const loading = ref(false)
const error = ref('')

const tablesInZone = computed(() =>
  allTables.value.filter((t) => t.zoneId === zoneId.value)
)

const unzonedTables = computed(() =>
  allTables.value.filter((t) => t.zoneId === null || t.zoneId === undefined)
)

// ── Load ─────────────────────────────────────────────────────────
const load = async () => {
  loading.value = true
  error.value = ''
  try {
    const [zoneRes, tablesRes] = await Promise.all([
      getZone(zoneId.value),
      listTables(),
    ])
    zone.value = zoneRes.data
    allTables.value = tablesRes.data ?? []
  } catch (err) {
    error.value = err?.response?.data?.title ?? 'Failed to load zone.'
  } finally {
    loading.value = false
  }
}

// ── Assign / Remove ───────────────────────────────────────────────
const assigning = ref(null) // tableId being assigned
const removing = ref(null)  // tableId being removed

const assignTable = async (table) => {
  assigning.value = table.id
  try {
    await updateTable(table.id, { code: table.code, zoneId: zoneId.value })
    await load()
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Failed', detail: err?.response?.data?.title ?? 'Could not assign table.', life: 3000 })
  } finally {
    assigning.value = null
  }
}

const removeTable = async (table) => {
  removing.value = table.id
  try {
    await updateTable(table.id, { code: table.code, zoneId: null })
    await load()
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Failed', detail: err?.response?.data?.title ?? 'Could not remove table.', life: 3000 })
  } finally {
    removing.value = null
  }
}

// ── Edit dialog ───────────────────────────────────────────────────
const showEditDialog = ref(false)
const editName = ref('')
const editDisplayOrder = ref(0)
const editLoading = ref(false)
const editError = ref('')

const openEditDialog = () => {
  editName.value = zone.value.name
  editDisplayOrder.value = zone.value.displayOrder
  editError.value = ''
  showEditDialog.value = true
}

const handleEdit = async () => {
  if (!editName.value.trim()) {
    editError.value = 'Zone name is required.'
    return
  }
  editLoading.value = true
  editError.value = ''
  try {
    await updateZone(zoneId.value, { name: editName.value.trim(), displayOrder: editDisplayOrder.value })
    await load()
    showEditDialog.value = false
  } catch (err) {
    editError.value = err?.response?.data?.errors?.join(', ') || err?.response?.data?.title || 'Failed to update zone.'
  } finally {
    editLoading.value = false
  }
}

// ── Toggle active ─────────────────────────────────────────────────
const togglingActive = ref(false)
const toggleActive = async () => {
  togglingActive.value = true
  try {
    if (zone.value.isActive) {
      await deactivateZone(zoneId.value)
    } else {
      await activateZone(zoneId.value)
    }
    await load()
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Failed', detail: err?.response?.data?.title ?? 'Could not update zone.', life: 3000 })
  } finally {
    togglingActive.value = false
  }
}

// ── Status helpers ────────────────────────────────────────────────
const tableStatusColor = (status) => {
  if (status === 'Available') return 'tw:text-primary-400'
  if (status === 'Occupied') return 'tw:text-amber-400'
  return 'app-text-subtle'
}

onMounted(load)
</script>

<template>
  <!-- Edit dialog -->
  <prime-dialog v-model:visible="showEditDialog" header="Edit zone" :modal="true" :style="{ width: '24rem' }">
    <div class="tw:space-y-4">
      <div class="tw:space-y-1.5">
        <label class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">Zone name</label>
        <prime-input-text v-model="editName" placeholder="e.g. Tầng 1" class="app-input tw:w-full" @keyup.enter="handleEdit" />
      </div>
      <div class="tw:space-y-1.5">
        <label class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">Display order</label>
        <prime-input-number v-model="editDisplayOrder" :min="0" :show-buttons="true" button-layout="horizontal" class="app-input tw:w-full" />
        <p class="tw:text-[11px] app-text-subtle">Smaller number = shown first</p>
      </div>
      <p v-if="editError" class="tw:text-xs tw:text-red-400">{{ editError }}</p>
    </div>
    <template #footer>
      <prime-button severity="secondary" outlined @click="showEditDialog = false">Cancel</prime-button>
      <prime-button severity="primary" :loading="editLoading" @click="handleEdit">
        <iconify icon="ph:check-bold" />
        <span>Save changes</span>
      </prime-button>
    </template>
  </prime-dialog>

  <section class="tw:space-y-6">
    <!-- Header -->
    <div class="tw:flex tw:flex-wrap tw:items-center tw:gap-4">
      <prime-button :class="btnIcon" severity="secondary" text @click="router.push({ name: 'zones' })">
        <iconify icon="ph:arrow-left-bold" />
      </prime-button>
      <div class="tw:flex-1 tw:min-w-0">
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-primary-300">Operations / Zones</p>
        <div class="tw:mt-1 tw:flex tw:items-center tw:gap-3">
          <h1 v-if="zone" class="tw:text-2xl tw:font-semibold tw:truncate">{{ zone.name }}</h1>
          <div v-else-if="loading" class="tw:h-7 tw:w-40 tw:animate-pulse tw:rounded tw:bg-white/10" />
          <prime-tag
            v-if="zone"
            :value="zone.isActive ? 'Active' : 'Inactive'"
            :severity="zone.isActive ? 'success' : 'danger'"
          />
        </div>
      </div>
      <div v-if="zone" class="tw:flex tw:gap-2">
        <prime-button
          v-if="can('zone.update')"
          severity="secondary"
          outlined
          size="small"
          :class="btnIcon"
          v-tooltip.top="'Edit zone'"
          @click="openEditDialog"
        >
          <iconify icon="ph:pencil-bold" />
        </prime-button>
        <prime-button
          v-if="can('zone.update')"
          :severity="zone.isActive ? 'warn' : 'success'"
          outlined
          size="small"
          :class="btnIcon"
          :loading="togglingActive"
          v-tooltip.top="zone.isActive ? 'Deactivate' : 'Activate'"
          @click="toggleActive"
        >
          <iconify :icon="zone.isActive ? 'ph:pause-bold' : 'ph:play-bold'" />
        </prime-button>
      </div>
    </div>

    <!-- Error -->
    <prime-alert v-if="error" severity="error" variant="accent" closable @close="error = ''">{{ error }}</prime-alert>

    <!-- Loading skeleton -->
    <div v-if="loading" class="tw:grid tw:gap-6 tw:lg:grid-cols-2">
      <div v-for="n in 2" :key="n" class="tw:space-y-3">
        <div class="tw:h-5 tw:w-40 tw:animate-pulse tw:rounded tw:bg-white/10" />
        <div v-for="m in 3" :key="m" class="tw:h-16 tw:animate-pulse tw:rounded-xl tw:bg-white/5" />
      </div>
    </div>

    <div v-else-if="zone" class="tw:grid tw:gap-8 tw:lg:grid-cols-2">
      <!-- ── Tables in this zone ──────────────────────────────────── -->
      <div>
        <div class="tw:mb-3 tw:flex tw:items-center tw:justify-between">
          <h2 class="tw:text-base tw:font-semibold">
            Tables in this zone
            <span class="tw:ml-1.5 tw:text-sm tw:font-normal tw:text-muted">({{ tablesInZone.length }})</span>
          </h2>
        </div>

        <div v-if="tablesInZone.length === 0" class="tw:rounded-xl tw:border tw:border-dashed tw:border-white/10 tw:p-6 tw:text-center tw:text-muted tw:text-sm">
          No tables assigned yet.
        </div>

        <div v-else class="tw:space-y-2">
          <div
            v-for="table in tablesInZone"
            :key="table.id"
            class="tw:flex tw:items-center tw:justify-between tw:rounded-xl tw:border tw:border-white/10 tw:bg-white/5 tw:px-4 tw:py-3"
          >
            <div class="tw:flex tw:items-center tw:gap-3">
              <iconify icon="heroicons-outline:table-cells" class="tw:text-lg tw:shrink-0" :class="tableStatusColor(table.status)" />
              <div>
                <p class="tw:font-semibold">{{ table.code }}</p>
                <p class="tw:text-xs app-text-subtle">{{ table.status }}</p>
              </div>
            </div>
            <prime-button
              v-if="can('zone.update')"
              severity="secondary"
              text
              size="small"
              :class="btnIcon"
              :loading="removing === table.id"
              v-tooltip.top="'Remove from zone'"
              @click="removeTable(table)"
            >
              <iconify icon="ph:x-bold" />
            </prime-button>
          </div>
        </div>
      </div>

      <!-- ── Unzoned tables ──────────────────────────────────────── -->
      <div>
        <div class="tw:mb-3">
          <h2 class="tw:text-base tw:font-semibold">
            Unzoned tables
            <span class="tw:ml-1.5 tw:text-sm tw:font-normal tw:text-muted">({{ unzonedTables.length }})</span>
          </h2>
          <p class="tw:mt-0.5 tw:text-xs tw:text-muted">Click <iconify icon="ph:plus-bold" class="tw:inline" /> to assign a table to this zone.</p>
        </div>

        <div v-if="unzonedTables.length === 0" class="tw:rounded-xl tw:border tw:border-dashed tw:border-white/10 tw:p-6 tw:text-center tw:text-muted tw:text-sm">
          All tables have been assigned to a zone.
        </div>

        <div v-else class="tw:space-y-2">
          <div
            v-for="table in unzonedTables"
            :key="table.id"
            class="tw:flex tw:items-center tw:justify-between tw:rounded-xl tw:border tw:border-white/10 tw:bg-white/5 tw:px-4 tw:py-3"
          >
            <div class="tw:flex tw:items-center tw:gap-3">
              <iconify icon="heroicons-outline:table-cells" class="tw:text-lg tw:shrink-0" :class="tableStatusColor(table.status)" />
              <div>
                <p class="tw:font-semibold">{{ table.code }}</p>
                <p class="tw:text-xs app-text-subtle">{{ table.status }}</p>
              </div>
            </div>
            <prime-button
              v-if="can('zone.update')"
              severity="success"
              text
              size="small"
              :class="btnIcon"
              :loading="assigning === table.id"
              v-tooltip.top="'Add to this zone'"
              @click="assignTable(table)"
            >
              <iconify icon="ph:plus-bold" />
            </prime-button>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>
