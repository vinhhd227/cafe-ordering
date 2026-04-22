<script setup>
defineProps({
  title:    { type: String, default: 'Top products' },
  subtitle: { type: String, default: '' },
  unit:     { type: String, default: 'cups' },
  items: {
    type: Array,
    // [{ name: String, qty: Number, pct: Number, revenue?: Number }]
    default: () => [],
  },
})

const fmtRevenue = (val) => {
  if (!val) return null
  if (val >= 1_000_000) return (val / 1_000_000).toFixed(1).replace(/\.0$/, '') + 'M'
  if (val >= 1_000) return Math.round(val / 1_000) + 'K'
  return String(Math.round(val))
}
</script>

<template>
  <prime-card :class="[appCard, cardRing, 'tw:h-full']">
    <template #content>
      <div class="tw:flex tw:items-center tw:justify-between tw:mb-3">
        <div>
          <p class="tw:text-sm tw:font-semibold">{{ title }}</p>
          <p v-if="subtitle" class="tw:text-xs tw:text-muted">{{ subtitle }}</p>
        </div>
        <iconify icon="ph:ranking-bold" class="tw:text-emerald-400 tw:opacity-60 tw:text-lg" />
      </div>

      <div class="tw:space-y-3">
        <div v-for="(p, i) in items" :key="i" class="tw:flex tw:items-start tw:gap-1.5">
          <span class="tw:text-[11px] tw:font-bold app-text-subtle tw:shrink-0 tw:w-3 tw:text-center tw:pt-px">{{ i + 1 }}</span>
          <div class="tw:flex-1 tw:min-w-0">
            <div class="tw:flex tw:items-center tw:justify-between tw:mb-[3.75px] tw:h-[17.5px]">
              <span class="tw:text-sm tw:font-medium tw:truncate">{{ p.name }}</span>
              <span class="tw:text-[11px] tw:ml-2 tw:shrink-0 tw:flex tw:items-center tw:gap-1.5">
                <span class="tw:text-muted">{{ p.qty }} {{ unit }}</span>
                <span v-if="fmtRevenue(p.revenue)" class="tw:text-emerald-400/80">{{ fmtRevenue(p.revenue) }}</span>
              </span>
            </div>
            <div class="tw:h-2 tw:w-full tw:rounded-full tw:bg-white/8">
              <div class="tw:h-full tw:rounded-full tw:bg-emerald-400/70" :style="{ width: p.pct + '%' }" />
            </div>
          </div>
        </div>
      </div>
    </template>
  </prime-card>
</template>
