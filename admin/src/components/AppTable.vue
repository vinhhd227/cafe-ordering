<script setup>
/**
 * AppTable — Reusable paginated data table wrapper
 *
 * Props:
 *   value            – array of row data
 *   loading          – show loading overlay
 *   rows / first     – pagination state (support v-model)
 *   totalRecords     – total server-side count
 *   rowsPerPageOptions – page-size choices (default [10, 20, 50])
 *
 * Slots:
 *   #toolbar-left    – search inputs / filter selects
 *   #toolbar-right   – action buttons (export, add, …)
 *   default          – <prime-column> definitions
 *
 * Events:
 *   @page(event)     – fired after pagination changes (PrimeVue page event)
 *   @update:first    – v-model:first
 *   @update:rows     – v-model:rows
 *
 * Usage:
 *   <AppTable
 *     v-model:first="first"
 *     v-model:rows="rows"
 *     :value="items"
 *     :loading="loading"
 *     :totalRecords="total"
 *     @page="(e) => loadData(e.page + 1)"
 *   >
 *     <template #toolbar-left>
 *       <prime-input-text v-model="search" placeholder="Search…" class="app-input tw:w-64" />
 *     </template>
 *     <template #toolbar-right>
 *       <prime-button label="Export" severity="secondary" outlined size="small" />
 *     </template>
 *
 *     <prime-column field="id" header="ID" />
 *     <prime-column field="name" header="Name" />
 *   </AppTable>
 */

const props = defineProps({
  value: { type: Array, default: () => [] },
  loading: { type: Boolean, default: false },
  rows: { type: Number, default: 20 },
  first: { type: Number, default: 0 },
  totalRecords: { type: Number, default: 0 },
  rowsPerPageOptions: { type: Array, default: () => [10, 20, 50] },
})

const emit = defineEmits(['page', 'update:first', 'update:rows'])

const handlePage = (event) => {
  emit('update:first', event.first)
  emit('update:rows', event.rows)
  emit('page', event)
}
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
      <!-- lazy=true: PrimeVue dùng totalRecords (không dùng value.length) để tính số trang -->
      <!-- và render toàn bộ value mà không slice theo first — cần thiết cho server-side paging -->
      <prime-data-table
        :class="{ 'tw:mt-6': $slots['toolbar-left'] || $slots['toolbar-right'] }"
        :value="value"
        :loading="loading"
        :lazy="true"
        :paginator="true"
        :rows="rows"
        :first="first"
        :totalRecords="totalRecords"
        :rowsPerPageOptions="rowsPerPageOptions"
        responsiveLayout="scroll"
        @page="handlePage"
      >
        <slot />
      </prime-data-table>

    </template>
  </prime-card>
</template>
