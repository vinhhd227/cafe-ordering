<script setup>
const PALETTE = ['#fbbf24', '#34d399', '#60a5fa', '#c084fc', '#f87171', '#a78bfa']

const props = defineProps({
  title:    { type: String, default: 'Top categories' },
  subtitle: { type: String, default: '' },
  items: {
    type: Array,
    // [{ name: String, pct: Number, revenue?: Number, hex?: String }]
    default: () => [],
  },
})

const itemsWithColor = computed(() =>
  props.items.map((cat, i) => ({
    ...cat,
    hex: cat.hex ?? PALETTE[i % PALETTE.length],
  })),
)

const fmtCompact = (val) => {
  if (!val) return '0'
  if (val >= 1_000_000) return (val / 1_000_000).toFixed(1).replace(/\.0$/, '') + 'M'
  if (val >= 1_000) return Math.round(val / 1_000) + 'K'
  return String(val)
}
</script>

<template>
  <prime-card :class="[appCard, cardRing, 'tw:h-full']">
    <template #content>
      <div class="tw:flex tw:items-center tw:justify-between tw:mb-4">
        <div>
          <p class="tw:text-sm tw:font-semibold">{{ title }}</p>
          <p v-if="subtitle" class="tw:text-xs app-text-muted">{{ subtitle }}</p>
        </div>
        <iconify icon="ph:tag-bold" class="tw:text-amber-400 tw:opacity-60 tw:text-lg" />
      </div>

      <div class="tw:space-y-3">
        <div v-for="cat in itemsWithColor" :key="cat.name" class="tw:space-y-1">
          <div class="tw:flex tw:items-center tw:justify-between tw:gap-2">
            <div class="tw:flex tw:items-center tw:gap-2 tw:min-w-0">
              <span class="tw:size-2 tw:rounded-full tw:shrink-0" :style="{ backgroundColor: cat.hex }" />
              <span class="tw:text-sm tw:truncate">{{ cat.name }}</span>
              <span class="tw:text-[11px] app-text-muted tw:shrink-0">{{ cat.pct }}%</span>
            </div>
            <span v-if="cat.revenue != null" class="tw:text-xs tw:font-semibold tw:shrink-0">{{ fmtCompact(cat.revenue) }}</span>
          </div>
          <div class="tw:h-2 tw:rounded-full tw:bg-white/8">
            <div
              class="tw:h-full tw:rounded-full tw:transition-all"
              :style="{ width: cat.pct + '%', backgroundColor: cat.hex, opacity: '0.75' }"
            />
          </div>
        </div>
      </div>
    </template>
  </prime-card>
</template>
