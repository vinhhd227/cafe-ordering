<script setup>
defineProps({
  total: { type: Number, default: 0 },
  pending: { type: Number, default: 0 },
  processing: { type: Number, default: 0 },
  completed: { type: Number, default: 0 },
  cancelled: { type: Number, default: 0 },
});
</script>

<template>
  <prime-card class="app-card tw:rounded-xl tw:border">
    <template #content>
      <div class="tw:flex tw:items-start tw:justify-between">
        <p
          class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] app-text-subtle"
        >
          Total orders
        </p>
        <p class="tw:text-2xl tw:font-semibold tw:leading-none">{{ total }}</p>
      </div>

      <div class="tw:mt-3 tw:grid tw:grid-cols-2 tw:gap-x-4 tw:gap-y-1">
        <div
          class="tw:flex tw:items-center tw:justify-between tw:gap-2 tw:text-amber-400"
        >
          <span
            class="tw:flex tw:items-center tw:gap-1 tw:text-[10px] tw:uppercase tw:tracking-[0.15em]"
          >
            <iconify icon="ph:clock-bold" class="tw:text-sm tw:opacity-70" />
            Pending
          </span>
          <span class="tw:text-xs tw:font-medium">{{ pending }}</span>
        </div>
        <div
          class="tw:flex tw:items-center tw:justify-between tw:gap-2 tw:text-blue-400"
        >
          <span
            class="tw:flex tw:items-center tw:gap-1 tw:text-[10px] tw:uppercase tw:tracking-[0.15em]"
          >
            <iconify icon="ph:fire-bold" class="tw:text-sm tw:opacity-70" />
            Processing
          </span>
          <span class="tw:text-xs tw:font-medium">{{ processing }}</span>
        </div>
        <div
          class="tw:flex tw:items-center tw:justify-between tw:gap-2 tw:text-red-400"
        >
          <span
            class="tw:flex tw:items-center tw:gap-1 tw:text-[10px] tw:uppercase tw:tracking-[0.15em]"
          >
            <iconify icon="ph:x-circle-bold" class="tw:text-sm tw:opacity-70" />
            Cancelled
          </span>
          <span class="tw:text-xs tw:font-medium">{{ cancelled }}</span>
        </div>

        <div
          class="tw:flex tw:items-center tw:justify-between tw:gap-2 tw:text-emerald-400"
        >
          <span
            class="tw:flex tw:items-center tw:gap-1 tw:text-[10px] tw:uppercase tw:tracking-[0.15em]"
          >
            <iconify
              icon="ph:check-circle-bold"
              class="tw:text-sm tw:opacity-70"
            />
            Completed
          </span>
          <span class="tw:text-xs tw:font-medium">{{ completed }}</span>
        </div>
      </div>
    </template>
  </prime-card>
</template>
