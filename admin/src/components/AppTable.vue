<script setup>
/**
 * AppTable — Reusable paginated data table wrapper
 *
 * Props:
 *   value              – array of row data
 *   loading            – show loading overlay
 *   rows / first       – pagination state (support v-model)
 *   totalRecords       – total server-side count
 *   rowsPerPageOptions – page-size choices (default [10, 20, 50])
 *   columns            – (optional) column definitions for column-visibility toggle
 *                        Array of { field: string, header: string, visible?: boolean }
 *
 * Slots:
 *   #toolbar-left    – search inputs / filter selects
 *   #toolbar-right   – action buttons (export, add, …)
 *   default          – <prime-column> definitions
 *
 * Events:
 *   @page(event)        – fired after pagination changes
 *   @update:first       – v-model:first
 *   @update:rows        – v-model:rows
 *   @update:columns     – v-model:columns (column visibility changed)
 *
 * Usage:
 *   <AppTable
 *     v-model:first="first"
 *     v-model:rows="rows"
 *     v-model:columns="colDefs"
 *     :value="items"
 *     :loading="loading"
 *     :totalRecords="total"
 *     @page="(e) => loadData(e.page + 1)"
 *   >
 *     <prime-column v-if="colDefs.find(c=>c.field==='id')?.visible" field="id" header="ID" />
 *   </AppTable>
 */

import { ref, computed, watch } from "vue";

const props = defineProps({
  value: { type: Array, default: () => [] },
  loading: { type: Boolean, default: false },
  rows: { type: Number, default: 20 },
  first: { type: Number, default: 0 },
  totalRecords: { type: Number, default: 0 },
  rowsPerPageOptions: { type: Array, default: () => [10, 20, 50] },
  columns: { type: Array, default: null },
});

const emit = defineEmits([
  "page",
  "update:first",
  "update:rows",
  "update:columns",
]);

// ── Pagination ───────────────────────────────────────────────────
const currentPage = computed(() => Math.floor(props.first / props.rows) + 1);
const totalPages = computed(() =>
  Math.max(1, Math.ceil(props.totalRecords / props.rows)),
);
const showingFrom = computed(() =>
  props.totalRecords === 0 ? 0 : props.first + 1,
);
const showingTo = computed(() =>
  Math.min(props.first + props.rows, props.totalRecords),
);

const goToPage = (page) => {
  if (page < 1 || page > totalPages.value) return;
  const newFirst = (page - 1) * props.rows;
  emit("update:first", newFirst);
  emit("page", {
    first: newFirst,
    rows: props.rows,
    page: page - 1,
    pageCount: totalPages.value,
  });
};

const onRowsChange = (newRows) => {
  const n = Number(newRows);
  emit("update:rows", n);
  emit("update:first", 0);
  emit("page", {
    first: 0,
    rows: n,
    page: 0,
    pageCount: Math.ceil(props.totalRecords / n),
  });
};

// Page number tokens: numbers or '...' for gaps
const pageTokens = computed(() => {
  const total = totalPages.value;
  const current = currentPage.value;
  if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1);
  if (current <= 4) return [1, 2, 3, 4, 5, "...", total];
  if (current >= total - 3)
    return [1, "...", total - 4, total - 3, total - 2, total - 1, total];
  return [1, "...", current - 1, current, current + 1, "...", total];
});

// ── Column Toggle ────────────────────────────────────────────────
const colDialogVisible = ref(false);

const localColumns = ref(
  props.columns
    ? props.columns.map((c) => ({ ...c, visible: c.visible !== false }))
    : [],
);

const setColVisibility = (field, visible) => {
  const col = localColumns.value.find((c) => c.field === field);
  if (col) {
    col.visible = visible;
    emit("update:columns", localColumns.value.map((c) => ({ ...c })));
  }
};

// Đồng bộ localColumns khi props.columns thay đổi từ bên ngoài (cache restore, v.v.)
watch(
  () => props.columns,
  (newVal) => {
    if (!newVal) return;
    localColumns.value = newVal.map((c) => ({ ...c, visible: c.visible !== false }));
  },
);
</script>

<template>
  <prime-card class="app-card tw:rounded-2xl tw:border">
    <template #content>
      <!-- ── Toolbar ───────────────────────────────────────────── -->
      <div
        v-if="$slots['toolbar-left'] || $slots['toolbar-right']"
        class="tw:flex tw:flex-wrap tw:items-center tw:justify-between tw:gap-4"
      >
        <div class="tw:flex tw:flex-wrap tw:items-center tw:gap-3">
          <slot name="toolbar-left" />
        </div>
        <div class="tw:flex tw:items-center tw:gap-3">
          <slot name="toolbar-right" />
        </div>
      </div>

      <!-- ── Data Table ─────────────────────────────────────────── -->
      <prime-data-table
        :class="{
          'tw:mt-6': $slots['toolbar-left'] || $slots['toolbar-right'],
        }"
        :value="value"
        :loading="loading"
        :lazy="true"
        :paginator="false"
        responsiveLayout="scroll"
      >
        <slot />
      </prime-data-table>

      <!-- ── Pagination Bar ──────────────────────────────────────── -->
      <div
        class="tw:mt-4 tw:flex tw:flex-wrap tw:items-center tw:justify-between tw:gap-y-3 tw:gap-x-4"
      >
        <!-- Left: showing info -->
        <span
          class="tw:text-sm app-text-muted tw:whitespace-nowrap tw:min-w-[14rem]"
        >
          Showing {{ showingFrom }} to {{ showingTo }} of
          {{ totalRecords }} items
        </span>

        <!-- Center: page navigation -->
        <div class="tw:flex tw:items-center tw:gap-0.5">
          <prime-button
            severity="secondary"
            text
            size="small"
            :disabled="currentPage <= 1"
            v-tooltip.top="'First page'"
            @click="goToPage(1)"
            :class="btnIcon"
          >
            <iconify icon="ph:caret-double-left-bold" />
          </prime-button>

          <prime-button
            severity="secondary"
            text
            size="small"
            :disabled="currentPage <= 1"
            v-tooltip.top="'Previous page'"
            @click="goToPage(currentPage - 1)"
            :class="btnIcon"
          >
            <iconify icon="ph:caret-left-bold" />
          </prime-button>

          <template v-for="(token, idx) in pageTokens" :key="idx">
            <span
              v-if="token === '...'"
              class="tw:px-1 tw:text-sm app-text-muted tw:select-none tw:leading-none"
              >…</span
            >
            <prime-button
              v-else
              :severity="token === currentPage ? 'primary' : 'secondary'"
              :text="token !== currentPage"
              size="small"
              @click="goToPage(token)"
               :class="[btnIcon, 'tw:mx-1']"
              >{{ token }}</prime-button
             
            >
          </template>

          <prime-button
            severity="secondary"
            text
            size="small"
            :disabled="currentPage >= totalPages"
            v-tooltip.top="'Next page'"
            @click="goToPage(currentPage + 1)"
            :class="btnIcon"
          >
            <iconify icon="ph:caret-right-bold" />
          </prime-button>

          <prime-button
            severity="secondary"
            text
            size="small"
            :disabled="currentPage >= totalPages"
            v-tooltip.top="'Last page'"
            @click="goToPage(totalPages)"
            :class="btnIcon"
          >
            <iconify icon="ph:caret-double-right-bold" />
          </prime-button>
        </div>

        <!-- Right: items per page + column toggle -->
        <div
          class="tw:flex tw:items-center tw:gap-2 tw:min-w-[14rem] tw:justify-end"
        >
          <span class="tw:text-sm app-text-muted tw:whitespace-nowrap"
            >Items per page</span
          >
          <prime-select
            :model-value="rows"
            :options="rowsPerPageOptions"
            class="app-input"
            @update:model-value="onRowsChange"
          />
          <prime-button
            v-if="columns"
            severity="secondary"
            outlined
            v-tooltip.top="'Toggle columns'"
            @click="colDialogVisible = true"
            :class="btnIcon"
          >
            <iconify icon="ph:list-dashes-bold" />
          </prime-button>
        </div>
      </div>

      <!-- ── Column toggle dialog ───────────────────────────────── -->
      <prime-dialog
        v-if="columns"
        v-model:visible="colDialogVisible"
        header="Columns"
        :modal="true"
        :style="{ width: '18rem' }"
      >
        <div class="tw:flex tw:flex-col tw:gap-3">
          <div
            v-for="col in localColumns"
            :key="col.field"
            class="tw:flex tw:items-center tw:gap-2"
          >
            <prime-checkbox
              :inputId="`col-toggle-${col.field}`"
              :model-value="col.visible"
              binary
              @update:model-value="(val) => setColVisibility(col.field, val)"
            />
            <label
              :for="`col-toggle-${col.field}`"
              class="tw:text-sm tw:cursor-pointer tw:select-none"
            >{{ col.header }}</label>
          </div>
        </div>
      </prime-dialog>
    </template>
  </prime-card>
</template>

<style scoped>
/* Right-align the header label of the last column (Actions) */
:deep(th:last-child .p-datatable-column-header-content) {
  justify-content: flex-end;
}
</style>
