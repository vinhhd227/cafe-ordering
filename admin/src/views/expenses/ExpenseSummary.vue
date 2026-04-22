<script setup>
const props = defineProps({
  summary: { type: Object, default: null },
  loading: { type: Boolean, default: false },
});

const { t } = useI18n();

const formatVnd = (value) =>
  new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value ?? 0);

const revenue = computed(() => props.summary?.revenue ?? { cash: 0, bank: 0, total: 0 });
const expenseBreakdown = computed(
  () => props.summary?.expenses ?? { ingredient: 0, supply: 0, equipment: 0, other: 0, total: 0, cash: 0, bank: 0 }
);
const profit = computed(() => props.summary?.profit ?? 0);
const profitMargin = computed(() => {
  const rev = revenue.value.total;
  if (!rev) return null;
  return ((profit.value / rev) * 100).toFixed(1);
});
</script>

<template>
  <!-- P&L compact bar (mobile only) -->
  <div class="tw:sm:hidden tw:rounded-xl tw:border tw:border-slate-200 tw:dark:border-white/15 tw:bg-slate-50 tw:dark:bg-white/5 tw:px-4 tw:py-3 tw:grid tw:grid-cols-3 tw:gap-2">
    <div class="tw:flex tw:flex-col tw:gap-0.5">
      <span class="tw:text-[11px] tw:uppercase tw:tracking-widest tw:text-emerald-500 tw:dark:text-emerald-400">{{ t('expenses.summary.revenue') }}</span>
      <span class="tw:text-sm tw:font-bold">{{ loading ? '…' : formatVnd(revenue.total) }}</span>
    </div>
    <div class="tw:flex tw:flex-col tw:gap-0.5 tw:border-x tw:border-slate-200 tw:dark:border-white/10 tw:px-2">
      <span class="tw:text-[11px] tw:uppercase tw:tracking-widest tw:text-red-500 tw:dark:text-red-400">{{ t('expenses.summary.expenses') }}</span>
      <span class="tw:text-sm tw:font-bold">{{ loading ? '…' : formatVnd(expenseBreakdown.total) }}</span>
    </div>
    <div class="tw:flex tw:flex-col tw:gap-0.5 tw:text-right">
      <span class="tw:text-[11px] tw:uppercase tw:tracking-widest tw:text-muted">{{ t('expenses.summary.profit') }}</span>
      <span
        class="tw:text-sm tw:font-bold"
        :class="profit >= 0 ? 'tw:text-emerald-500 tw:dark:text-emerald-400' : 'tw:text-red-500 tw:dark:text-red-400'"
      >{{ loading ? '…' : formatVnd(profit) }}</span>
    </div>
  </div>

  <!-- P&L summary cards (desktop) -->
  <div class="tw:hidden tw:sm:grid tw:grid-cols-3 tw:gap-3">
    <!-- Revenue -->
    <widget-stat
      :label="t('expenses.summary.revenue')"
      label-class="tw:text-emerald-400"
      :value="loading ? '…' : formatVnd(revenue.total)"
    >
      <template #icon>
        <iconify icon="ph:trend-up-bold" class="tw:text-emerald-400 tw:opacity-60" />
      </template>
      <template #sub>
        <div class="tw:mt-2 tw:flex tw:flex-wrap tw:gap-x-3 tw:gap-y-0.5 tw:text-xs tw:text-muted">
          <span>{{ t('expenses.summary.cash') }} <span class="tw:font-medium">{{ formatVnd(revenue.cash) }}</span></span>
          <span>{{ t('expenses.summary.bank') }} <span class="tw:font-medium">{{ formatVnd(revenue.bank) }}</span></span>
        </div>
      </template>
    </widget-stat>

    <!-- Expenses -->
    <widget-stat
      :label="t('expenses.summary.expenses')"
      label-class="tw:text-red-400"
      :value="loading ? '…' : formatVnd(expenseBreakdown.total)"
    >
      <template #icon>
        <iconify icon="ph:trend-down-bold" class="tw:text-red-400 tw:opacity-60" />
      </template>
      <template #sub>
        <div class="tw:mt-2 tw:flex tw:flex-wrap tw:gap-x-3 tw:gap-y-0.5 tw:text-xs tw:text-muted">
          <span>{{ t('expenses.summary.cash') }} <span class="tw:font-medium">{{ formatVnd(expenseBreakdown.cash) }}</span></span>
          <span>{{ t('expenses.summary.bank') }} <span class="tw:font-medium">{{ formatVnd(expenseBreakdown.bank) }}</span></span>
        </div>
      </template>
    </widget-stat>

    <!-- Profit -->
    <widget-stat
      :label="t('expenses.summary.profit')"
      :label-class="profit >= 0 ? 'tw:text-emerald-400' : 'tw:text-red-400'"
      :value="loading ? '…' : formatVnd(profit)"
    >
      <template #icon>
        <iconify
          icon="ph:chart-bar-bold"
          :class="profit >= 0 ? 'tw:text-emerald-400 tw:opacity-60' : 'tw:text-red-400 tw:opacity-60'"
        />
      </template>
      <template #sub>
        <div class="tw:mt-2 tw:text-xs tw:text-muted">
          <span v-if="profitMargin !== null">
            {{ t('expenses.summary.margin') }}
            <span :class="profit >= 0 ? 'tw:text-emerald-400 tw:font-semibold' : 'tw:text-red-400 tw:font-semibold'">{{ profitMargin }}%</span>
          </span>
          <span v-else>—</span>
        </div>
      </template>
    </widget-stat>
  </div>
</template>
