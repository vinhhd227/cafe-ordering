<script setup>
import { computed, nextTick, onMounted, ref, watch } from "vue";
import { onBeforeRouteLeave } from "vue-router";
import { usePermission } from "@/composables/usePermission";
import { useTableCache } from "@/composables/useTableCache";
import AppTable from "@/components/AppTable.vue";
import WidgetSettingsButton from "@/components/widgets/WidgetSettingsButton.vue";
import {
  listTables,
  createTable,
  updateTable,
  toggleActive,
  deleteTable,
  markAvailable,
  closeSession,
} from "@/services/table.service";
import { listZones } from "@/services/zone.service";
import QRCode from "qrcode";
import { btnIcon } from "@/layout/ui";

const cache = useTableCache("tables");
const { can } = usePermission();

// ── Table state ────────────────────────────────────────────────────
const loading = ref(false);
const errorMessage = ref("");
const tables = ref([]);
const rows = ref(20);
const first = ref(0);

// ── Zones ───────────────────────────────────────────────────────────
const zones = ref([]);
const loadZones = async () => {
  try {
    const res = await listZones();
    zones.value = res.data ?? [];
  } catch {
    // non-critical, ignore
  }
};
const zoneOptions = computed(() =>
  zones.value.map((z) => ({ label: z.name, value: z.id }))
);

// ── Filters ────────────────────────────────────────────────────────
const statusFilter = ref(null);
const activeFilter = ref(null);
const zoneFilter = ref(null);

const statusOptions = [
  { label: "Available", value: "Available" },
  { label: "Occupied", value: "Occupied" },
  { label: "Cleaning", value: "Cleaning" },
];

const activeOptions = [
  { label: "Active", value: true },
  { label: "Inactive", value: false },
];

// ── Summary ────────────────────────────────────────────────────────
const summary = computed(() => {
  const all = tables.value;
  return {
    total: all.length,
    available: all.filter((t) => t.status === "Available").length,
    occupied: all.filter((t) => t.status === "Occupied").length,
    cleaning: all.filter((t) => t.status === "Cleaning").length,
  };
});

// ── Filtered rows (client-side) ─────────────────────────────────
const filtered = computed(() => {
  return tables.value.filter((t) => {
    if (statusFilter.value !== null && t.status !== statusFilter.value)
      return false;
    if (activeFilter.value !== null && t.isActive !== activeFilter.value)
      return false;
    if (zoneFilter.value !== null && t.zoneId !== zoneFilter.value)
      return false;
    return true;
  });
});

const pagedRows = computed(() => {
  return filtered.value.slice(first.value, first.value + rows.value);
});

// ── Load ───────────────────────────────────────────────────────────
const load = async () => {
  loading.value = true;
  errorMessage.value = "";
  try {
    const res = await listTables();
    tables.value = res.data ?? [];
  } catch (err) {
    errorMessage.value = err?.response?.data?.title ?? "Failed to load tables.";
  } finally {
    loading.value = false;
  }
};

// ── Actions ────────────────────────────────────────────────────────
const handleToggleActive = async (row) => {
  try {
    await toggleActive(row.id);
    await load();
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.title ?? `Failed to update table ${row.code}.`;
  }
};

const handleMarkAvailable = async (row) => {
  try {
    await markAvailable(row.id);
    await load();
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.title ??
      `Failed to mark table ${row.code} available.`;
  }
};

const handleCloseSession = async (row) => {
  if (!row.activeSessionId) return;
  try {
    await closeSession(row.activeSessionId);
    await load();
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.title ??
      `Failed to close session on table ${row.code}.`;
  }
};

const handleDelete = async (row) => {
  try {
    await deleteTable(row.id);
    await load();
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.title ?? `Failed to delete table ${row.code}.`;
  }
};

// ── Add table dialog ───────────────────────────────────────────────
const showAddDialog = ref(false);
const newCode = ref("");
const newZoneId = ref(null);
const addError = ref("");
const addLoading = ref(false);

const openAddDialog = () => {
  newCode.value = "";
  newZoneId.value = null;
  addError.value = "";
  showAddDialog.value = true;
};

const handleAddTable = async () => {
  if (!newCode.value.trim()) {
    addError.value = "Table code is required.";
    return;
  }
  addLoading.value = true;
  addError.value = "";
  try {
    await createTable(newCode.value.trim(), newZoneId.value);
    await load();
    showAddDialog.value = false;
    newCode.value = "";
  } catch (err) {
    addError.value =
      err?.response?.data?.errors?.join(", ") ||
      err?.response?.data?.title ||
      "Failed to create table. Code may already exist.";
  } finally {
    addLoading.value = false;
  }
};

// ── Edit dialog ────────────────────────────────────────────────────
const showEditDialog = ref(false);
const editRow = ref(null);
const editCode = ref("");
const editZoneId = ref(null);
const editError = ref("");
const editLoading = ref(false);

const openEditDialog = (row) => {
  editRow.value = row;
  editCode.value = row.code;
  editZoneId.value = row.zoneId ?? null;
  editError.value = "";
  showEditDialog.value = true;
};

const handleEditTable = async () => {
  if (!editCode.value.trim()) {
    editError.value = "Table code is required.";
    return;
  }
  editLoading.value = true;
  editError.value = "";
  try {
    await updateTable(editRow.value.id, {
      code: editCode.value.trim(),
      zoneId: editZoneId.value,
    });
    await load();
    showEditDialog.value = false;
  } catch (err) {
    editError.value =
      err?.response?.data?.errors?.join(", ") ||
      err?.response?.data?.title ||
      "Failed to update table. Code may already exist.";
  } finally {
    editLoading.value = false;
  }
};

// ── QR dialog ──────────────────────────────────────────────────────
const showQrDialog = ref(false);
const qrRow = ref(null);
const qrCanvas = ref(null);
const qrUrl = ref("");

const openQrDialog = async (row) => {
  qrRow.value = row;
  qrUrl.value = `${import.meta.env.VITE_ORDERING_BASE_URL ?? ""}/table/${row.code}`;
  showQrDialog.value = true;
  await nextTick();
  if (qrCanvas.value) {
    await QRCode.toCanvas(qrCanvas.value, qrUrl.value, {
      width: 240,
      margin: 2,
      color: { dark: "#000000", light: "#ffffff" },
    });
  }
};

const downloadQr = () => {
  if (!qrCanvas.value) return;
  const link = document.createElement("a");
  link.download = `table-${qrRow.value.code}.png`;
  link.href = qrCanvas.value.toDataURL("image/png");
  link.click();
};

// ── Tag helpers ────────────────────────────────────────────────────
const statusTag = (status) => {
  switch (status) {
    case "Available":
      return { label: "Available", severity: "success" };
    case "Occupied":
      return { label: "Occupied", severity: "info" };
    case "Cleaning":
      return { label: "Cleaning", severity: "warn" };
    default:
      return { label: status, severity: "secondary" };
  }
};

const activeTag = (isActive) =>
  isActive
    ? { label: "Active", severity: "success" }
    : { label: "Inactive", severity: "danger" };

// ── Filter helpers ─────────────────────────────────────────────────
const activeFilterCount = computed(() => {
  let n = 0;
  if (statusFilter.value !== null) n++;
  if (activeFilter.value !== null) n++;
  if (zoneFilter.value !== null) n++;
  return n;
});

const clearFilters = () => {
  statusFilter.value = null;
  activeFilter.value = null;
  zoneFilter.value = null;
  first.value = 0;
};

const filterPanel = ref(null);

// ── Cache ──────────────────────────────────────────────────────────
onMounted(() => {
  const cached = cache.restore();
  if (cached) {
    rows.value = cached.rows ?? 20;
    first.value = cached.first ?? 0;
    statusFilter.value = cached.statusFilter ?? null;
    activeFilter.value = cached.activeFilter ?? null;
    zoneFilter.value = cached.zoneFilter ?? null;
  }
  load();
  loadZones();
});

onBeforeRouteLeave(() => {
  cache.save({
    rows: rows.value,
    first: first.value,
    statusFilter: statusFilter.value,
    activeFilter: activeFilter.value,
    zoneFilter: zoneFilter.value,
  });
});

watch([statusFilter, activeFilter, zoneFilter], () => {
  first.value = 0;
});

// ── Widget visibility ──────────────────────────────────────────────
const { isVisible: wVisible, toggle: wToggle, hiddenCount: wHidden, widgets: wDefs, colsPerRow: wCols, setColsPerRow: wSetCols } =
  useWidgetSettings('tables', [
    { id: 'total',     label: 'Total',     preview: '20', description: 'Tổng số bàn có trong nhà hàng.' },
    { id: 'available', label: 'Available', preview: '12', description: 'Bàn trống, sẵn sàng đón khách mới.', labelClass: 'tw:text-emerald-400' },
    { id: 'occupied',  label: 'Occupied',  preview: '6',  description: 'Bàn đang có khách, đang trong phiên đặt món.', labelClass: 'tw:text-blue-400' },
    { id: 'cleaning',  label: 'Cleaning',  preview: '2',  description: 'Bàn đang dọn dẹp sau khi khách rời đi.', labelClass: 'tw:text-yellow-400' },
  ], { defaultCols: 4 })
const W_COLS_CLASS = { 1: 'tw:grid-cols-1', 2: 'tw:grid-cols-2', 3: 'tw:grid-cols-3', 4: 'tw:grid-cols-4' }
const wColsClass = computed(() => W_COLS_CLASS[wCols.value] ?? 'tw:grid-cols-2')

const columns = [
  { field: 'code',            header: 'Code',    width: '8rem' },
  { field: 'zoneName',        header: 'Zone',    width: '9rem' },
  { field: 'status',          header: 'Status',  width: '8rem' },
  { field: 'isActive',        header: 'Active',  width: '7rem' },
  { field: 'activeSessionId', header: 'Session', width: '8rem' },
  { key: 'actions',           header: 'Actions', width: '18rem', toggleable: false },
]
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
        <label
          for="tableCode"
          class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted"
        >
          Table code
        </label>
        <prime-input-text
          id="tableCode"
          v-model="newCode"
          placeholder="e.g. F1-01, BM-02"
          class="app-input tw:w-full tw:font-mono"
          @keyup.enter="handleAddTable"
        />
        <p class="tw:text-[11px] app-text-subtle">
          Format tự do — ví dụ: F1-01 (tầng 1 bàn 1)
        </p>
      </div>
      <div class="tw:space-y-1.5">
        <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">
          Zone <span class="app-text-subtle">(optional)</span>
        </label>
        <prime-select
          v-model="newZoneId"
          :options="zoneOptions"
          option-label="label"
          option-value="value"
          placeholder="No zone"
          show-clear
          class="app-input tw:w-full"
        />
      </div>
      <p v-if="addError" class="tw:text-xs tw:text-red-400">{{ addError }}</p>
    </div>
    <template #footer>
      <prime-button severity="secondary" outlined @click="showAddDialog = false"
        >Cancel</prime-button
      >
      <prime-button
        severity="success"
        :loading="addLoading"
        @click="handleAddTable"
      >
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
        <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted"
          >Table code</label
        >
        <prime-input-text
          v-model="editCode"
          placeholder="e.g. F1-01"
          class="app-input tw:w-full tw:font-mono"
          @keyup.enter="handleEditTable"
        />
      </div>
      <div class="tw:space-y-1.5">
        <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">
          Zone <span class="app-text-subtle">(optional)</span>
        </label>
        <prime-select
          v-model="editZoneId"
          :options="zoneOptions"
          option-label="label"
          option-value="value"
          placeholder="No zone"
          show-clear
          class="app-input tw:w-full"
        />
      </div>
      <p v-if="editError" class="tw:text-xs tw:text-red-400">{{ editError }}</p>
    </div>
    <template #footer>
      <prime-button
        severity="secondary"
        outlined
        @click="showEditDialog = false"
        >Cancel</prime-button
      >
      <prime-button
        severity="primary"
        :loading="editLoading"
        @click="handleEditTable"
      >
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
      <p
        class="tw:text-xs tw:font-mono app-text-muted tw:text-center tw:break-all tw:px-2"
      >
        {{ qrUrl }}
      </p>
    </div>
    <template #footer>
      <prime-button severity="secondary" outlined @click="showQrDialog = false"
        >Close</prime-button
      >
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
        <p
          class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300"
        >
          Operations
        </p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">Tables</h1>
        <p class="tw:mt-2 tw:text-sm app-text-muted">
          Manage dining tables, sessions, and availability.
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
          v-if="can('table.create')"
          severity="success"
          size="small"
          @click="openAddDialog"
        >
          <iconify icon="ph:plus-bold" />
          <span>Add table</span>
        </prime-button>
      </div>
    </div>

    <!-- ── Summary stats ─────────────────────────────────────────── -->
    <div :class="['tw:grid tw:gap-3', wColsClass]">
      <widget-stat v-if="wVisible('total')"     label="Total"     :value="summary.total" />
      <widget-stat v-if="wVisible('available')" label="Available" :value="summary.available" label-class="tw:text-emerald-400" />
      <widget-stat v-if="wVisible('occupied')"  label="Occupied"  :value="summary.occupied"  label-class="tw:text-blue-400" />
      <widget-stat v-if="wVisible('cleaning')"  label="Cleaning"  :value="summary.cleaning"  label-class="tw:text-yellow-400" />
    </div>

    <!-- ── Error ──────────────────────────────────────────────────── -->
    <prime-alert
      v-if="errorMessage"
      severity="error"
      variant="accent"
      closable
      @close="errorMessage = ''"
      >{{ errorMessage }}</prime-alert
    >

    <!-- ── Table ──────────────────────────────────────────────────── -->
    <AppTable
      v-model:first="first"
      v-model:rows="rows"
      :value="pagedRows"
      :loading="loading"
      :totalRecords="filtered.length"
      :rowsPerPageOptions="[10, 20, 50]"
      :columns="columns"
      @page="(e) => (first = e.first)"
    >
      <template #toolbar-left>
        <div class="tw:flex tw:items-center tw:gap-2">
          <prime-button
            :severity="activeFilterCount > 0 ? 'success' : 'secondary'"
            :outlined="activeFilterCount === 0"
            v-tooltip.top="'Filters'"
            @click="filterPanel.toggle($event)"
            :class="!activeFilterCount ? btnIcon : ''"
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
                <label
                  for="statusFilter"
                  class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest"
                >
                  Status
                </label>
                <prime-select
                  id="statusFilter"
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
                <label
                  class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest"
                  >Active</label
                >
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

              <div v-if="zoneOptions.length > 0" class="tw:space-y-1.5">
                <label
                  class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest"
                  >Zone</label
                >
                <prime-select
                  v-model="zoneFilter"
                  :options="zoneOptions"
                  option-label="label"
                  option-value="value"
                  placeholder="All zones"
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

      <template #col-code="{ data }">
        <span class="tw:font-mono tw:font-semibold tw:text-sm">{{ data.code }}</span>
      </template>

      <template #col-zoneName="{ data }">
        <span v-if="data.zoneName" class="tw:text-sm">{{ data.zoneName }}</span>
        <span v-else class="app-text-subtle">—</span>
      </template>

      <template #col-status="{ data }">
        <prime-tag
          :value="statusTag(data.status).label"
          :severity="statusTag(data.status).severity"
        />
      </template>

      <template #col-isActive="{ data }">
        <prime-tag
          :value="activeTag(data.isActive).label"
          :severity="activeTag(data.isActive).severity"
        />
      </template>

      <template #col-activeSessionId="{ data }">
        <span v-if="data.activeSessionId" class="tw:text-xs app-text-muted tw:font-mono">
          {{ data.activeSessionId.slice(0, 8) }}…
        </span>
        <span v-else class="app-text-subtle">—</span>
      </template>

      <template #col-actions="{ data }">
        <div class="tw:flex tw:justify-end tw:flex-wrap tw:gap-2">
          <prime-button
            v-show="data.status === 'Occupied' && data.activeSessionId"
            severity="info"
            outlined
            size="small"
            v-tooltip.top="'Close session'"
            @click="handleCloseSession(data)"
            :class="btnIcon"
          >
            <iconify icon="ph:x-circle-bold" />
          </prime-button>

          <prime-button
            v-show="data.status === 'Cleaning'"
            severity="success"
            outlined
            size="small"
            v-tooltip.top="'Mark as available'"
            @click="handleMarkAvailable(data)"
          >
            <iconify icon="ph:check-circle-bold" />
          </prime-button>

          <prime-button
            v-if="can('table.create')"
            severity="secondary"
            outlined
            size="small"
            v-tooltip.top="'Edit table'"
            @click="openEditDialog(data)"
            :class="btnIcon"
          >
            <iconify icon="ph:pencil-bold" />
          </prime-button>

          <prime-button
            severity="warn"
            outlined
            size="small"
            v-tooltip.top="'Show QR code'"
            @click="openQrDialog(data)"
            :class="btnIcon"
          >
            <iconify icon="ph:qr-code-bold" />
          </prime-button>

          <prime-button
            :disabled="data.status == 'Occupied'"
            :severity="data.status == 'Occupied' ? 'secondary' : data.isActive ? 'danger' : 'success'"
            outlined
            size="small"
            @click="handleToggleActive(data)"
            :class="btnIcon"
          >
            <iconify :icon="data.isActive ? 'ph:toggle-left-bold' : 'ph:toggle-right-bold'" />
          </prime-button>

          <prime-button
            :disabled="data.status == 'Occupied' || !can('table.delete')"
            :severity="data.status !== 'Occupied' ? 'danger' : 'secondary'"
            outlined
            size="small"
            v-tooltip.top="'Delete table'"
            @click="handleDelete(data)"
            :class="btnIcon"
          >
            <iconify icon="ph:trash-bold" />
          </prime-button>
        </div>
      </template>
    </AppTable>
  </section>
</template>
