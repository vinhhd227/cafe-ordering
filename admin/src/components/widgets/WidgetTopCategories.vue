<script setup>
const PALETTE = ['#fbbf24', '#34d399', '#60a5fa', '#c084fc', '#f87171', '#a78bfa']

const props = defineProps({
  title:    { type: String, default: 'Top categories' },
  subtitle: { type: String, default: '' },
  items: {
    type: Array,
    // [{ name: String, pct: Number, hex?: String }]
    default: () => [],
  },
})

const itemsWithColor = computed(() =>
  props.items.map((cat, i) => ({
    ...cat,
    hex: cat.hex ?? PALETTE[i % PALETTE.length],
  })),
)
</script>

<template>
  <prime-card :class="[appCard, cardRing]">
    <template #content>
      <div class="tw:flex tw:items-center tw:justify-between tw:mb-4">
        <div>
          <p class="tw:text-sm tw:font-semibold">{{ title }}</p>
          <p v-if="subtitle" class="tw:text-xs app-text-muted">{{ subtitle }}</p>
        </div>
        <iconify icon="ph:tag-bold" class="tw:text-amber-400 tw:opacity-60 tw:text-lg" />
      </div>

      <div class="tw:space-y-3">
        <div v-for="cat in itemsWithColor" :key="cat.name" class="tw:flex tw:items-center tw:gap-3">
          <span class="tw:size-2 tw:rounded-full tw:flex-shrink-0" :style="{ backgroundColor: cat.hex }" />
          <span class="tw:text-sm tw:w-20 tw:flex-shrink-0 tw:truncate">{{ cat.name }}</span>
          <div class="tw:flex-1 tw:h-2 tw:rounded-full tw:bg-white/8">
            <div
              class="tw:h-full tw:rounded-full tw:transition-all"
              :style="{ width: cat.pct + '%', backgroundColor: cat.hex, opacity: '0.75' }"
            />
          </div>
          <span class="tw:text-xs tw:font-semibold tw:w-8 tw:text-right app-text-muted">{{ cat.pct }}%</span>
        </div>
      </div>
    </template>
  </prime-card>
</template>
