<script setup>
import { useI18n } from 'vue-i18n'

defineProps({
  total: { type: Number, default: 0 },
  pending: { type: Number, default: 0 },
  processing: { type: Number, default: 0 },
  completed: { type: Number, default: 0 },
  cancelled: { type: Number, default: 0 },
})

const { t } = useI18n()
</script>

<template>
  <prime-card :pt="{
    root: { class: `${appCard} ${cardRing} tw:p-4` },
    header: { class: 'tw:flex tw:justify-between tw:gap-3 tw:min-w-0' },
    body: { class: 'tw:p-0!' },
    content: { class: 'tw:mt-5 tw:space-y-1 tw:grid tw:grid-cols-2 tw:gap-x-4' },
  }">
    <template #header>
      <p class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] app-text-subtle tw:min-w-0 tw:truncate">
        {{ t('widget.orders.label') }}
      </p>
      <p class="tw:text-2xl tw:font-semibold tw:leading-none tw:shrink-0">{{ total }}</p>
    </template>
    <template #content>
      <div class="tw:flex tw:items-center tw:justify-between tw:gap-2 tw:text-amber-400 tw:min-w-0">
        <span class="tw:flex tw:min-w-0 tw:flex-1 tw:items-center tw:gap-1 tw:text-[10px] tw:uppercase tw:tracking-[0.15em]">
          <iconify icon="ph:clock-bold" class="tw:text-sm tw:opacity-70 tw:shrink-0" />
          <span class="tw:min-w-0 tw:truncate">{{ t('widget.orders.pending') }}</span>
        </span>
        <span class="tw:text-xs tw:font-medium tw:shrink-0">{{ pending }}</span>
      </div>
      <div class="tw:flex tw:items-center tw:justify-between tw:gap-2 tw:text-blue-400 tw:min-w-0">
        <span class="tw:flex tw:min-w-0 tw:flex-1 tw:items-center tw:gap-1 tw:text-[10px] tw:uppercase tw:tracking-[0.15em]">
          <iconify icon="ph:fire-bold" class="tw:text-sm tw:opacity-70 tw:shrink-0" />
          <span class="tw:min-w-0 tw:truncate">{{ t('widget.orders.processing') }}</span>
        </span>
        <span class="tw:text-xs tw:font-medium tw:shrink-0">{{ processing }}</span>
      </div>
      <div class="tw:flex tw:items-center tw:justify-between tw:gap-2 tw:text-red-400 tw:min-w-0">
        <span class="tw:flex tw:min-w-0 tw:flex-1 tw:items-center tw:gap-1 tw:text-[10px] tw:uppercase tw:tracking-[0.15em]">
          <iconify icon="ph:x-circle-bold" class="tw:text-sm tw:opacity-70 tw:shrink-0" />
          <span class="tw:min-w-0 tw:truncate">{{ t('widget.orders.cancelled') }}</span>
        </span>
        <span class="tw:text-xs tw:font-medium tw:shrink-0">{{ cancelled }}</span>
      </div>
      <div class="tw:flex tw:items-center tw:justify-between tw:gap-2 tw:text-emerald-400 tw:min-w-0">
        <span class="tw:flex tw:min-w-0 tw:flex-1 tw:items-center tw:gap-1 tw:text-[10px] tw:uppercase tw:tracking-[0.15em]">
          <iconify icon="ph:check-circle-bold" class="tw:text-sm tw:opacity-70 tw:shrink-0" />
          <span class="tw:min-w-0 tw:truncate">{{ t('widget.orders.completed') }}</span>
        </span>
        <span class="tw:text-xs tw:font-medium tw:shrink-0">{{ completed }}</span>
      </div>
    </template>
  </prime-card>
</template>
