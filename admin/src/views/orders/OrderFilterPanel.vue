<script setup>
const props = defineProps({
  filters: { type: Object, required: true },
  statusOptions: { type: Array, default: () => [] },
  paymentStatusOptions: { type: Array, default: () => [] },
  paymentMethodOptions: { type: Array, default: () => [] },
  showSearch: { type: Boolean, default: false },
})

const { t } = useI18n()
</script>

<template>
  <div class="tw:flex tw:flex-col tw:gap-4">
    <!-- Search (mobile drawer only) -->
    <div v-if="showSearch" class="tw:space-y-1.5">
      <label class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest">
        {{ t('orders.filter.searchOrder').replace('…', '').trim() }}
      </label>
      <prime-input-text
        v-model="filters.searchOrder"
        :placeholder="t('orders.filter.searchOrder')"
        class="app-input tw:w-full"
      />
    </div>

    <!-- Date range -->
    <div class="tw:space-y-1.5">
      <label class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest">
        {{ t('orders.filter.dateRange') }}
      </label>
      <div class="tw:flex tw:items-center tw:gap-2">
        <prime-date-picker
          v-model="filters.dateFrom"
          :placeholder="t('orders.filter.dateFrom')"
          date-format="dd/mm/yy"
          show-button-bar
          class="app-input tw:flex-1"
        />
        <span class="app-text-muted tw:text-sm">–</span>
        <prime-date-picker
          v-model="filters.dateTo"
          :placeholder="t('orders.filter.dateTo')"
          date-format="dd/mm/yy"
          show-button-bar
          class="app-input tw:flex-1"
        />
      </div>
    </div>

    <!-- Order status -->
    <div class="tw:space-y-1.5">
      <label class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest">
        {{ t('orders.filter.orderStatus') }}
      </label>
      <prime-select
        v-model="filters.statusFilter"
        :options="statusOptions"
        option-label="label"
        option-value="value"
        :placeholder="t('orders.filter.allStatuses')"
        show-clear
        class="app-input tw:w-full"
      />
    </div>

    <!-- Payment status -->
    <div class="tw:space-y-1.5">
      <label class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest">
        {{ t('orders.filter.paymentStatus') }}
      </label>
      <prime-select
        v-model="filters.paymentStatusFilter"
        :options="paymentStatusOptions"
        option-label="label"
        option-value="value"
        :placeholder="t('orders.filter.allPayments')"
        show-clear
        class="app-input tw:w-full"
      />
    </div>

    <!-- Payment method -->
    <div class="tw:space-y-1.5">
      <label class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest">
        {{ t('orders.filter.paymentMethod') }}
      </label>
      <prime-select
        v-model="filters.paymentMethodFilter"
        :options="paymentMethodOptions"
        option-label="label"
        option-value="value"
        :placeholder="t('orders.filter.allMethods')"
        show-clear
        class="app-input tw:w-full"
      />
    </div>

    <!-- Total range -->
    <div class="tw:space-y-1.5">
      <label class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest">
        {{ t('orders.filter.total') }}
      </label>
      <div class="tw:flex tw:items-center tw:gap-2">
        <prime-input-number
          v-model="filters.minTotal"
          :placeholder="t('orders.filter.min')"
          :min="0"
          :use-grouping="true"
          class="app-input tw:flex-1"
        />
        <span class="app-text-muted tw:text-sm">–</span>
        <prime-input-number
          v-model="filters.maxTotal"
          :placeholder="t('orders.filter.max')"
          :min="0"
          :use-grouping="true"
          class="app-input tw:flex-1"
        />
      </div>
    </div>

    <!-- Table code -->
    <div class="tw:space-y-1.5">
      <label class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest">
        {{ t('orders.filter.table') }}
      </label>
      <prime-input-text
        v-model="filters.tableCodeFilter"
        :placeholder="t('orders.filter.tableCode')"
        class="app-input tw:w-full"
      />
    </div>
  </div>
</template>
