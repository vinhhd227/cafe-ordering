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
import { useI18n } from "vue-i18n";

const { t } = useI18n();

const props = defineProps({
  value: { type: Array, default: () => [] },
  loading: { type: Boolean, default: false },
  rows: { type: Number, default: 20 },
  first: { type: Number, default: 0 },
  totalRecords: { type: Number, default: 0 },
  rowsPerPageOptions: { type: Array, default: () => [10, 20, 50] },
  columns: { type: Array, default: () => [] },
  emptyMessage: { type: String, default: null },
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
  props.columns.map((c) => ({ ...c, visible: c.visible !== false })),
);

const visibleColumns = computed(() =>
  localColumns.value.filter((c) => c.visible !== false),
);

const toggleableColumns = computed(() =>
  localColumns.value.filter((c) => c.toggleable !== false),
);

const setColVisibility = (id, visible) => {
  const col = localColumns.value.find((c) => (c.key ?? c.field) === id);
  if (col) {
    col.visible = visible;
    emit(
      "update:columns",
      localColumns.value.map((c) => ({ ...c })),
    );
  }
};

// Đồng bộ localColumns khi props.columns thay đổi từ bên ngoài (cache restore, v.v.)
watch(
  () => props.columns,
  (newVal) => {
    if (!newVal?.length) return;
    localColumns.value = newVal.map((c) => ({
      ...c,
      visible: c.visible !== false,
    }));
  },
);
</script>

<template>
  <prime-card
    :pt="{
      root: { class: `${appCard} ${cardRing} tw:p-4` },
      header: { class: 'tw:flex tw:justify-between' },
      body: { class: 'tw:p-0!' },
      content: { class: 'tw:space-y-1 tw:mt-3' },
    }"
  >
    <template #header>
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
    </template>
    <template #content>
      <!-- ── Skeleton Loading ───────────────────────────────────── -->
      <div v-if="loading" class="tw:space-y-1 tw:py-1">
        <prime-skeleton height="2.75rem" border-radius="0" class="tw:opacity-70" />
        <prime-skeleton
          v-for="n in Math.min(rows, 8)"
          :key="n"
          height="2.75rem"
          border-radius="0"
        />
      </div>

      <template v-else>
        <!-- ── Mobile card grid (khi có slot #mobile-card) ────────── -->
        <div
          v-if="$slots['mobile-card']"
          class="tw:sm:hidden tw:py-2"
        >
          <div v-if="value.length === 0" class="tw:py-14 tw:flex tw:flex-col tw:items-center tw:gap-3 tw:text-muted">
            <iconify icon="ph:tray-bold" class="tw:text-5xl" />
            <span class="tw:text-sm">{{ emptyMessage ?? t('common.table.emptyMessage') }}</span>
          </div>
          <div v-else class="tw:grid tw:grid-cols-2 tw:gap-3">
            <slot v-for="row in value" name="mobile-card" :data="row" />
          </div>
        </div>

        <!-- ── Data Table ─────────────────────────────────────────── -->
        <div :class="$slots['mobile-card'] ? 'tw:hidden tw:sm:block' : ''">
        <prime-data-table
          :pt="{
          bodyRow: { class: 'tw:bg-transparent!' },
          emptyMessage: { class: 'tw:bg-transparent!' },

        }"
        :value="value"
        :lazy="true"
        :paginator="false"
        responsiveLayout="scroll"
      >
        <template #empty>
          <div class="tw:py-14 tw:flex tw:flex-col tw:items-center tw:gap-3 tw:text-muted">
            <iconify icon="ph:tray-bold" class="tw:text-5xl" />
            <span class="tw:text-sm">{{ emptyMessage ?? t('common.table.emptyMessage') }}</span>
          </div>
        </template>
        <prime-column
          :pt="{
            headerCell: { class: 'tw:bg-transparent!' },
            bodyCell: { class: 'tw:bg-transparent!' },
          }"
          v-for="col in visibleColumns"
          :key="col.key ?? col.field"
          :field="col.field"
          :header="col.header"
          :style="col.width ? `min-width: ${col.width}` : undefined"
          :sortable="col.sortable ?? false"
        >
          <template
            v-if="$slots[`col-${col.key ?? col.field}`]"
            #body="slotProps"
          >
            <slot :name="`col-${col.key ?? col.field}`" v-bind="slotProps" />
          </template>
        </prime-column>
      </prime-data-table>
        </div>
      </template>
    </template>
    <template #footer>
      <!-- ── Pagination Bar ──────────────────────────────────────── -->
      <div
        class="tw:mt-4 tw:flex tw:flex-col tw:sm:flex-row tw:items-center tw:justify-between tw:gap-y-3 tw:gap-x-4"
      >
        <!-- Left: showing info -->
        <span
          class="tw:text-sm tw:text-muted tw:w-full tw:sm:w-auto tw:sm:min-w-[14rem]"
        >
          {{ t('common.table.showing', { from: showingFrom, to: showingTo, total: totalRecords }) }}
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
            :class="[btnIcon, 'tw:hidden tw:sm:flex']"
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
              class="tw:px-1 tw:text-sm tw:text-muted tw:select-none tw:leading-none"
              >…</span
            >
            <prime-button
              v-else
              :severity="token === currentPage ? 'primary' : 'secondary'"
              :text="token !== currentPage"
              size="small"
              @click="goToPage(token)"
              :class="[btnIcon, 'tw:mx-0.5', idx !== 0 && idx !== pageTokens.length - 1 && token !== currentPage ? 'tw:hidden tw:sm:flex' : '']"
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
            :class="[btnIcon, 'tw:hidden tw:sm:flex']"
          >
            <iconify icon="ph:caret-double-right-bold" />
          </prime-button>
        </div>

        <!-- Right: items per page + column toggle -->
        <div
          class="tw:flex tw:items-center tw:gap-2 tw:w-full tw:sm:w-auto tw:sm:min-w-[14rem] tw:justify-between tw:sm:justify-end"
        >
          <span class="tw:text-sm tw:text-muted tw:whitespace-nowrap tw:hidden tw:sm:inline">{{ t('common.table.itemsPerPage') }}</span>
          <div class="tw:flex tw:items-center tw:gap-2">
            <prime-select
              :model-value="rows"
              :options="rowsPerPageOptions"
              class="app-input"
              size="small"
              @update:model-value="onRowsChange"
            />
            <prime-button
              v-if="toggleableColumns.length > 0"
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
      </div>
      <!-- ── Column toggle dialog ───────────────────────────────── -->
      <prime-dialog
        v-if="toggleableColumns.length > 0"
        v-model:visible="colDialogVisible"
        header="Columns"
        :modal="true"
        :style="{ width: '18rem' }"
      >
        <div class="tw:flex tw:flex-col tw:gap-3">
          <div
            v-for="col in toggleableColumns"
            :key="col.key ?? col.field"
            class="tw:flex tw:items-center tw:gap-2"
          >
            <prime-checkbox
              :inputId="`col-toggle-${col.key ?? col.field}`"
              :model-value="col.visible"
              binary
              @update:model-value="
                (val) => setColVisibility(col.key ?? col.field, val)
              "
            />
            <label
              :for="`col-toggle-${col.key ?? col.field}`"
              class="tw:text-sm tw:cursor-pointer tw:select-none"
              >{{ col.header }}</label
            >
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
