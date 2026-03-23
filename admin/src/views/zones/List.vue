<script setup>
import { onMounted, ref } from "vue";
import { usePermission } from "@/composables/usePermission";
import AppTable from "@/components/AppTable.vue";
import {
  listZones,
  createZone,
  updateZone,
  deleteZone,
  activateZone,
  deactivateZone,
} from "@/services/zone.service";
import { btnIcon } from "@/layout/ui";

const { can } = usePermission();

// ── State ──────────────────────────────────────────────────────────
const loading = ref(false);
const errorMessage = ref("");
const zones = ref([]);
const rows = ref(20);
const first = ref(0);

// ── Load ───────────────────────────────────────────────────────────
const load = async () => {
  loading.value = true;
  errorMessage.value = "";
  try {
    const res = await listZones();
    zones.value = res.data ?? [];
  } catch (err) {
    errorMessage.value = err?.response?.data?.title ?? "Failed to load zones.";
  } finally {
    loading.value = false;
  }
};

// ── Actions ────────────────────────────────────────────────────────
const handleToggleActive = async (row) => {
  try {
    if (row.isActive) {
      await deactivateZone(row.id);
    } else {
      await activateZone(row.id);
    }
    await load();
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.title ?? `Failed to update zone ${row.name}.`;
  }
};

const handleDelete = async (row) => {
  try {
    await deleteZone(row.id);
    await load();
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.title ?? `Failed to delete zone ${row.name}.`;
  }
};

// ── Add dialog ─────────────────────────────────────────────────────
const showAddDialog = ref(false);
const newName = ref("");
const newDisplayOrder = ref(0);
const addError = ref("");
const addLoading = ref(false);

const openAddDialog = () => {
  newName.value = "";
  newDisplayOrder.value = 0;
  addError.value = "";
  showAddDialog.value = true;
};

const handleAddZone = async () => {
  if (!newName.value.trim()) {
    addError.value = "Zone name is required.";
    return;
  }
  addLoading.value = true;
  addError.value = "";
  try {
    await createZone({ name: newName.value.trim(), displayOrder: newDisplayOrder.value });
    await load();
    showAddDialog.value = false;
  } catch (err) {
    addError.value =
      err?.response?.data?.errors?.join(", ") ||
      err?.response?.data?.title ||
      "Failed to create zone. Name may already exist.";
  } finally {
    addLoading.value = false;
  }
};

// ── Edit dialog ────────────────────────────────────────────────────
const showEditDialog = ref(false);
const editRow = ref(null);
const editName = ref("");
const editDisplayOrder = ref(0);
const editError = ref("");
const editLoading = ref(false);

const openEditDialog = (row) => {
  editRow.value = row;
  editName.value = row.name;
  editDisplayOrder.value = row.displayOrder;
  editError.value = "";
  showEditDialog.value = true;
};

const handleEditZone = async () => {
  if (!editName.value.trim()) {
    editError.value = "Zone name is required.";
    return;
  }
  editLoading.value = true;
  editError.value = "";
  try {
    await updateZone(editRow.value.id, {
      name: editName.value.trim(),
      displayOrder: editDisplayOrder.value,
    });
    await load();
    showEditDialog.value = false;
  } catch (err) {
    editError.value =
      err?.response?.data?.errors?.join(", ") ||
      err?.response?.data?.title ||
      "Failed to update zone. Name may already exist.";
  } finally {
    editLoading.value = false;
  }
};

// ── Tag helpers ────────────────────────────────────────────────────
const activeTag = (isActive) =>
  isActive
    ? { label: "Active", severity: "success" }
    : { label: "Inactive", severity: "danger" };

// ── Table columns ──────────────────────────────────────────────────
const columns = [
  { field: "name",         header: "Name",          width: "14rem" },
  { field: "displayOrder", header: "Order",         width: "6rem" },
  { field: "tableCount",   header: "Tables",        width: "7rem" },
  { field: "isActive",     header: "Active",        width: "7rem" },
  { key: "actions",        header: "Actions",       width: "10rem", toggleable: false },
];

onMounted(() => {
  load();
});
</script>

<template>
  <!-- ── Add zone dialog ────────────────────────────────────────── -->
  <prime-dialog
    v-model:visible="showAddDialog"
    header="Add zone"
    :modal="true"
    :style="{ width: '24rem' }"
  >
    <div class="tw:space-y-4">
      <div class="tw:space-y-1.5">
        <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">
          Zone name
        </label>
        <prime-input-text
          v-model="newName"
          placeholder="e.g. Tầng 1, Tầng Hầm"
          class="app-input tw:w-full"
          @keyup.enter="handleAddZone"
        />
      </div>
      <div class="tw:space-y-1.5">
        <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">
          Display order
        </label>
        <prime-input-number
          v-model="newDisplayOrder"
          :min="0"
          :show-buttons="true"
          button-layout="horizontal"
          class="app-input tw:w-full"
        />
        <p class="tw:text-[11px] app-text-subtle">Smaller number = shown first</p>
      </div>
      <p v-if="addError" class="tw:text-xs tw:text-red-400">{{ addError }}</p>
    </div>
    <template #footer>
      <prime-button severity="secondary" outlined @click="showAddDialog = false">
        Cancel
      </prime-button>
      <prime-button severity="success" :loading="addLoading" @click="handleAddZone">
        <iconify icon="ph:plus-bold" />
        <span>Add zone</span>
      </prime-button>
    </template>
  </prime-dialog>

  <!-- ── Edit zone dialog ───────────────────────────────────────── -->
  <prime-dialog
    v-model:visible="showEditDialog"
    header="Edit zone"
    :modal="true"
    :style="{ width: '24rem' }"
  >
    <div class="tw:space-y-4">
      <div class="tw:space-y-1.5">
        <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">
          Zone name
        </label>
        <prime-input-text
          v-model="editName"
          placeholder="e.g. Tầng 1"
          class="app-input tw:w-full"
          @keyup.enter="handleEditZone"
        />
      </div>
      <div class="tw:space-y-1.5">
        <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">
          Display order
        </label>
        <prime-input-number
          v-model="editDisplayOrder"
          :min="0"
          :show-buttons="true"
          button-layout="horizontal"
          class="app-input tw:w-full"
        />
      </div>
      <p v-if="editError" class="tw:text-xs tw:text-red-400">{{ editError }}</p>
    </div>
    <template #footer>
      <prime-button severity="secondary" outlined @click="showEditDialog = false">
        Cancel
      </prime-button>
      <prime-button severity="primary" :loading="editLoading" @click="handleEditZone">
        <iconify icon="ph:check-bold" />
        <span>Save changes</span>
      </prime-button>
    </template>
  </prime-dialog>

  <section class="tw:space-y-8">
    <!-- ── Header ────────────────────────────────────────────────── -->
    <div class="tw:flex tw:flex-wrap tw:items-end tw:justify-between tw:gap-4">
      <div>
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300">
          Operations
        </p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">Zones</h1>
        <p class="tw:mt-2 tw:text-sm app-text-muted">
          Manage dining areas such as floors, terraces, or private rooms.
        </p>
      </div>
      <prime-button
        v-if="can('zone.create')"
        severity="success"
        size="small"
        @click="openAddDialog"
      >
        <iconify icon="ph:plus-bold" />
        <span>Add zone</span>
      </prime-button>
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
      :value="zones.slice(first, first + rows)"
      :loading="loading"
      :totalRecords="zones.length"
      :rowsPerPageOptions="[10, 20, 50]"
      :columns="columns"
      @page="(e) => (first = e.first)"
    >
      <template #col-tableCount="{ data }">
        <span class="tw:text-sm">{{ data.tableCount }}</span>
      </template>

      <template #col-isActive="{ data }">
        <prime-tag
          :value="activeTag(data.isActive).label"
          :severity="activeTag(data.isActive).severity"
        />
      </template>

      <template #col-actions="{ data }">
        <div class="tw:flex tw:justify-end tw:gap-2">
          <prime-button
            severity="secondary"
            outlined
            size="small"
            v-tooltip.top="'Manage tables'"
            @click="$router.push({ name: 'zoneDetail', params: { id: data.id } })"
            :class="btnIcon"
          >
            <iconify icon="ph:arrow-square-out-bold" />
          </prime-button>

          <prime-button
            v-if="can('zone.update')"
            :severity="data.isActive ? 'danger' : 'success'"
            outlined
            size="small"
            v-tooltip.top="data.isActive ? 'Deactivate' : 'Activate'"
            @click="handleToggleActive(data)"
            :class="btnIcon"
          >
            <iconify :icon="data.isActive ? 'ph:toggle-left-bold' : 'ph:toggle-right-bold'" />
          </prime-button>

          <prime-button
            v-if="can('zone.delete')"
            :disabled="data.tableCount > 0"
            :severity="data.tableCount === 0 ? 'danger' : 'secondary'"
            outlined
            size="small"
            v-tooltip.top="data.tableCount > 0 ? `Cannot delete — ${data.tableCount} table(s) assigned` : 'Delete zone'"
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
