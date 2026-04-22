<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { getPublicTables } from '../services/table.service.js'

const { t } = useI18n()
const router = useRouter()

const tables = ref([])
const isLoading = ref(false)
const loadError = ref('')

const fetchTables = async () => {
  isLoading.value = true
  loadError.value = ''
  try {
    tables.value = await getPublicTables() ?? []
  } catch {
    loadError.value = t('table.empty')
  } finally {
    isLoading.value = false
  }
}

// Group tables by zone; tables without zone go to a null-key group
const tableGroups = computed(() => {
  const groups = new Map()
  for (const table of tables.value) {
    const key = table.zoneId ?? null
    if (!groups.has(key)) {
      groups.set(key, { zoneName: table.zoneName ?? null, tables: [] })
    }
    groups.get(key).tables.push(table)
  }
  // Sort: named zones first (by zoneName), then null zone last
  return [...groups.values()].sort((a, b) => {
    if (a.zoneName === null) return 1
    if (b.zoneName === null) return -1
    return a.zoneName.localeCompare(b.zoneName)
  })
})

const hasZones = computed(() =>
  tables.value.some((t) => t.zoneId !== null && t.zoneId !== undefined)
)

const selectTable = (table) => {
  if (table.status === 'Inactive') return
  router.push({ name: 'order', params: { tableId: table.id }, query: { code: table.code } })
}

const statusColor = (status) => {
  if (status === 'Available') return 'tw:text-emerald-400'
  if (status === 'Occupied') return 'tw:text-amber-400'
  return 'app-text-subtle'
}

const statusLabel = (status) => {
  if (status === 'Available') return t('table.available')
  if (status === 'Occupied') return t('table.occupied')
  return t('table.inactive')
}

onMounted(fetchTables)
</script>

<template>
  <div>
    <div class="tw:mb-6">
      <h1 class="tw:text-2xl tw:font-semibold">{{ t('table.selectTitle') }}</h1>
      <p class="tw:mt-1 tw:text-muted">{{ t('table.selectSubtitle') }}</p>
    </div>

    <!-- Loading -->
    <div v-if="isLoading" class="tw:grid tw:gap-4 tw:grid-cols-2 tw:sm:grid-cols-3 tw:lg:grid-cols-4 tw:xl:grid-cols-5">
      <div
        v-for="n in 8"
        :key="n"
        class="tw:animate-pulse tw:rounded-2xl tw:border tw:border-white/10 tw:bg-white/5 tw:p-6 tw:h-32"
      />
    </div>

    <!-- Error -->
    <div
      v-else-if="loadError"
      class="tw:rounded-2xl tw:border tw:border-rose-500/30 tw:bg-rose-500/10 tw:p-6 tw:text-center tw:text-rose-400"
    >
      {{ loadError }}
    </div>

    <!-- Empty -->
    <div
      v-else-if="tables.length === 0"
      class="tw:rounded-2xl tw:border tw:border-white/10 tw:bg-white/5 tw:p-12 tw:text-center tw:text-muted"
    >
      <iconify icon="ph:chair-bold" class="tw:mb-3 tw:text-4xl app-text-subtle" />
      <p>{{ t('table.empty') }}</p>
    </div>

    <!-- Table grid — grouped by zone (if zones exist) -->
    <div v-else class="tw:space-y-8">
      <div v-for="group in tableGroups" :key="group.zoneName ?? '__no_zone__'">
        <!-- Zone header (only show if any table has a zone) -->
        <div v-if="hasZones" class="tw:mb-3 tw:flex tw:items-center tw:gap-3">
          <iconify
            icon="ph:map-pin-bold"
            class="tw:text-base tw:text-emerald-400 tw:shrink-0"
          />
          <span class="tw:text-sm tw:font-semibold tw:uppercase tw:tracking-widest tw:text-emerald-300">
            {{ group.zoneName ?? t('table.noZone') }}
          </span>
          <div class="tw:flex-1 tw:border-t tw:border-white/10" />
        </div>

        <div class="tw:grid tw:gap-4 tw:grid-cols-2 tw:sm:grid-cols-3 tw:lg:grid-cols-4 tw:xl:grid-cols-5">
          <button
            v-for="table in group.tables"
            :key="table.id"
            type="button"
            class="app-panel tw:flex tw:flex-col tw:items-center tw:justify-center tw:gap-2 tw:rounded-2xl tw:border tw:p-6 tw:text-center tw:transition-all tw:duration-150"
            :class="
              table.status === 'Available'
                ? 'tw:border-emerald-500/30 tw:hover:border-emerald-400 tw:hover:bg-emerald-500/10 tw:cursor-pointer'
                : table.status === 'Occupied'
                ? 'tw:border-amber-500/20 tw:hover:border-amber-400 tw:hover:bg-amber-500/10 tw:cursor-pointer'
                : 'tw:border-white/10 tw:opacity-40 tw:cursor-not-allowed'
            "
            :disabled="table.status === 'Inactive'"
            @click="selectTable(table)"
          >
            <iconify
              icon="ic:round-table-bar"
              class="tw:text-3xl"
              :class="statusColor(table.status)"
            />
            <span class="tw:text-lg tw:font-bold">{{ table.code }}</span>
            <span class="tw:text-xs tw:font-medium" :class="statusColor(table.status)">
              {{ statusLabel(table.status) }}
            </span>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
