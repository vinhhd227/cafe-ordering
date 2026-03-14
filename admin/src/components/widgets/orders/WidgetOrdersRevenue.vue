<script setup>
import { useI18n } from 'vue-i18n'

defineProps({
  total: { type: Number, default: 0 },
  cash: { type: Number, default: 0 },
  bank: { type: Number, default: 0 },
  tip: { type: Number, default: 0 },
})

const { t } = useI18n()

const fmt = (value) =>
  new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value ?? 0)
</script>

<template>
  <prime-card
    :pt="{
      root: { class: `${appCard} ${cardRing} tw:p-4` },
      header: { class: 'tw:flex tw:justify-between tw:gap-3 tw:min-w-0' },
      body: { class: 'tw:p-0!' },
      content: { class: 'tw:space-y-1 tw:mt-3' },
    }"
  >
    <template #header>
      <p class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] app-text-subtle tw:min-w-0 tw:truncate">
        {{ t('widget.revenue.label') }}
      </p>
      <p class="tw:text-2xl tw:font-semibold tw:shrink-0">{{ fmt(total) }}</p>
    </template>
    <template #content>
      <div class="tw:flex tw:items-center tw:justify-between tw:gap-2 tw:text-emerald-400 tw:min-w-0">
        <span class="tw:flex tw:min-w-0 tw:flex-1 tw:items-center tw:gap-1 tw:text-[10px] tw:uppercase tw:tracking-[0.15em]">
          <iconify icon="ph:money-bold" class="tw:text-sm tw:opacity-70 tw:shrink-0" />
          <span class="tw:min-w-0 tw:truncate">{{ t('widget.revenue.cash') }}</span>
        </span>
        <span class="tw:text-xs tw:font-medium tw:shrink-0">{{ fmt(cash) }}</span>
      </div>
      <div class="tw:flex tw:items-center tw:justify-between tw:gap-2 tw:text-blue-400 tw:min-w-0">
        <span class="tw:flex tw:min-w-0 tw:flex-1 tw:items-center tw:gap-1 tw:text-[10px] tw:uppercase tw:tracking-[0.15em]">
          <iconify icon="ph:bank-bold" class="tw:text-sm tw:opacity-70 tw:shrink-0" />
          <span class="tw:min-w-0 tw:truncate">{{ t('widget.revenue.bank') }}</span>
        </span>
        <span class="tw:text-xs tw:font-medium tw:shrink-0">{{ fmt(bank) }}</span>
      </div>
      <div v-if="tip > 0" class="tw:flex tw:items-center tw:justify-between tw:gap-2 tw:text-amber-400 tw:min-w-0">
        <span class="tw:flex tw:min-w-0 tw:flex-1 tw:items-center tw:gap-1 tw:text-[10px] tw:uppercase tw:tracking-[0.15em]">
          <iconify icon="ph:heart-bold" class="tw:text-sm tw:opacity-70 tw:shrink-0" />
          <span class="tw:min-w-0 tw:truncate">{{ t('widget.revenue.tip') }}</span>
        </span>
        <span class="tw:text-xs tw:font-medium tw:shrink-0">{{ fmt(tip) }}</span>
      </div>
    </template>
  </prime-card>
</template>
