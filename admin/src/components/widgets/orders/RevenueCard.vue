<script setup>
defineProps({
  total: { type: Number, default: 0 },
  cash: { type: Number, default: 0 },
  bank: { type: Number, default: 0 },
});

const fmt = (value) =>
  new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
    maximumFractionDigits: 0,
  }).format(value ?? 0);
</script>

<template>
  <prime-card class="app-card tw:rounded-xl tw:border">
    <template #content>
      <div class="tw:flex tw:justify-between">
        <p
        class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] app-text-subtle"
      >
        Total revenue
      </p>
      <p class="tw:text-2xl tw:font-semibold">{{ fmt(total) }}</p>
      </div>
      <div class="tw:mt-3 tw:space-y-1">
        <div class="tw:flex tw:items-center tw:justify-between tw:gap-2 tw:text-emerald-400">
          <span class="tw:flex tw:items-center tw:gap-1 tw:text-[10px] tw:uppercase tw:tracking-[0.15em]">
            <iconify icon="ph:money-bold" class="tw:text-sm tw:opacity-70" />
            Cash
          </span>
          <span class="tw:text-xs tw:font-medium">{{ fmt(cash) }}</span>
        </div>
        <div class="tw:flex tw:items-center tw:justify-between tw:gap-2 tw:text-blue-400">
          <span class="tw:flex tw:items-center tw:gap-1 tw:text-[10px] tw:uppercase tw:tracking-[0.15em]">
            <iconify icon="ph:bank-bold" class="tw:text-sm tw:opacity-70" />
            Bank
          </span>
          <span class="tw:text-xs tw:font-medium">{{ fmt(bank) }}</span>
        </div>
      </div>
    </template>
  </prime-card>
</template>
