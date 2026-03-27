<script setup>
defineProps({
  title:    { type: String, default: 'Top products' },
  subtitle: { type: String, default: '' },
  unit:     { type: String, default: 'cups' },
  items: {
    type: Array,
    // [{ name: String, qty: Number, pct: Number }]
    default: () => [],
  },
})
</script>

<template>
  <prime-card :class="[appCard, cardRing]">
    <template #content>
      <div class="tw:flex tw:items-center tw:justify-between tw:mb-3">
        <div>
          <p class="tw:text-sm tw:font-semibold">{{ title }}</p>
          <p v-if="subtitle" class="tw:text-xs app-text-muted">{{ subtitle }}</p>
        </div>
        <iconify icon="ph:ranking-bold" class="tw:text-emerald-400 tw:opacity-60 tw:text-lg" />
      </div>

      <div class="tw:space-y-2">
        <div
          v-for="(p, i) in items"
          :key="i"
          class="tw:flex tw:items-center tw:gap-2"
        >
          <span class="tw:text-[11px] tw:font-bold tw:w-3 tw:text-center app-text-subtle tw:flex-shrink-0">{{ i + 1 }}</span>
          <div class="tw:flex-1 tw:min-w-0">
            <div class="tw:flex tw:items-center tw:justify-between tw:mb-0.5">
              <span class="tw:text-xs tw:font-medium tw:truncate">{{ p.name }}</span>
              <span class="tw:text-[11px] app-text-muted tw:ml-2 tw:flex-shrink-0">{{ p.qty }} {{ unit }}</span>
            </div>
            <div class="tw:h-1 tw:w-full tw:rounded-full tw:bg-white/8">
              <div class="tw:h-full tw:rounded-full tw:bg-emerald-400/70" :style="{ width: p.pct + '%' }" />
            </div>
          </div>
        </div>
      </div>
    </template>
  </prime-card>
</template>
