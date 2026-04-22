<script setup>
import { useI18n } from 'vue-i18n'
import { getMonthlyComparison } from '@/services/report.service'

const { t } = useI18n()

// ── Helpers ────────────────────────────────────────────────────────
const fmt = (value) =>
  new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value ?? 0)

const fmtCompact = (v) =>
  new Intl.NumberFormat('vi-VN', { notation: 'compact', maximumFractionDigits: 1 }).format(v ?? 0)

const delta = (current, previous) => {
  if (!previous || previous === 0) return null
  return ((current - previous) / previous) * 100
}

// ── Month state ────────────────────────────────────────────────────
const now = new Date()
const selectedYear  = ref(now.getFullYear())
const selectedMonth = ref(now.getMonth() + 1)  // 1-based

const monthLabel = computed(() => {
  const d = new Date(selectedYear.value, selectedMonth.value - 1, 1)
  return d.toLocaleDateString('vi-VN', { month: 'long', year: 'numeric' })
})

const prevMonthLabel = computed(() => {
  const d = new Date(selectedYear.value, selectedMonth.value - 2, 1)
  return d.toLocaleDateString('vi-VN', { month: 'long', year: 'numeric' })
})

const goPrev = () => {
  if (selectedMonth.value === 1) { selectedMonth.value = 12; selectedYear.value-- }
  else selectedMonth.value--
}

const goNext = () => {
  if (selectedMonth.value === 12) { selectedMonth.value = 1; selectedYear.value++ }
  else selectedMonth.value++
}

// ── Data ───────────────────────────────────────────────────────────
const loading = ref(false)
const error   = ref('')
const data    = ref(null)

const load = async () => {
  loading.value = true
  error.value   = ''
  try {
    const res = await getMonthlyComparison({
      year:  selectedYear.value,
      month: selectedMonth.value,
    })
    data.value = res.data
  } catch (e) {
    error.value = e?.response?.data?.message ?? t('report.error')
  } finally {
    loading.value = false
  }
}

watch([selectedYear, selectedMonth], load)
onMounted(load)

// ── KPI metrics config ─────────────────────────────────────────────
const metrics = computed(() => {
  if (!data.value) return []
  const { current, previous } = data.value
  return [
    {
      key: 'revenue',
      label: t('report.monthly.metrics.revenue'),
      icon: 'ph:coins-bold',
      iconColor: 'tw:text-emerald-400',
      current:  current.totalRevenue,
      previous: previous.totalRevenue,
      format:   fmt,
      delta:    delta(current.totalRevenue, previous.totalRevenue),
    },
    {
      key: 'orders',
      label: t('report.monthly.metrics.orders'),
      icon: 'ph:receipt-bold',
      iconColor: 'tw:text-blue-400',
      current:  current.totalOrders,
      previous: previous.totalOrders,
      format:   (v) => v.toLocaleString(),
      delta:    delta(current.totalOrders, previous.totalOrders),
    },
    {
      key: 'items',
      label: t('report.monthly.metrics.items'),
      icon: 'ph:coffee-bold',
      iconColor: 'tw:text-violet-400',
      current:  current.totalItemsSold,
      previous: previous.totalItemsSold,
      format:   (v) => v.toLocaleString(),
      delta:    delta(current.totalItemsSold, previous.totalItemsSold),
    },
    {
      key: 'guests',
      label: t('report.monthly.metrics.guests'),
      icon: 'ph:users-bold',
      iconColor: 'tw:text-amber-400',
      current:  current.totalGuestCount,
      previous: previous.totalGuestCount,
      format:   (v) => v.toLocaleString(),
      delta:    delta(current.totalGuestCount, previous.totalGuestCount),
    },
    {
      key: 'avgOrder',
      label: t('report.monthly.metrics.avgOrder'),
      icon: 'ph:trend-up-bold',
      iconColor: 'tw:text-rose-400',
      current:  current.avgOrderValue,
      previous: previous.avgOrderValue,
      format:   fmt,
      delta:    delta(current.avgOrderValue, previous.avgOrderValue),
    },
  ]
})

// ── Peak hours chart ───────────────────────────────────────────────
const peakHoursChartData = computed(() => {
  const hours = data.value?.peakHours ?? []
  return {
    labels: hours.map((h) => `${String(h.hour).padStart(2, '0')}:00`),
    datasets: [
      {
        label: t('report.monthly.metrics.orders'),
        data: hours.map((h) => h.orderCount),
        backgroundColor: 'rgba(96, 165, 250, 0.75)',
        borderColor: 'rgb(96, 165, 250)',
        borderWidth: 1,
        borderRadius: 3,
        yAxisID: 'yOrders',
      },
      {
        label: t('report.monthly.metrics.revenue'),
        data: hours.map((h) => h.revenue),
        backgroundColor: 'rgba(52, 211, 153, 0.6)',
        borderColor: 'rgb(52, 211, 153)',
        borderWidth: 1,
        borderRadius: 3,
        type: 'line',
        yAxisID: 'yRevenue',
        tension: 0.3,
        pointRadius: 2,
      },
    ],
  }
})

const peakHoursChartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  interaction: { mode: 'index' },
  plugins: {
    legend: {
      position: 'top',
      labels: { color: 'rgb(156, 163, 175)', padding: 12, font: { size: 11 } },
    },
    tooltip: {
      callbacks: {
        label: (ctx) =>
          ctx.dataset.yAxisID === 'yRevenue'
            ? ` ${ctx.dataset.label}: ${fmt(ctx.parsed.y)}`
            : ` ${ctx.dataset.label}: ${ctx.parsed.y}`,
      },
    },
  },
  scales: {
    x: {
      ticks: { color: 'rgb(156, 163, 175)', font: { size: 10 }, maxRotation: 45 },
      grid: { color: 'rgba(255,255,255,0.05)' },
    },
    yOrders: {
      type: 'linear',
      position: 'left',
      ticks: { color: 'rgb(96, 165, 250)', font: { size: 10 }, stepSize: 1 },
      grid: { color: 'rgba(255,255,255,0.05)' },
    },
    yRevenue: {
      type: 'linear',
      position: 'right',
      ticks: {
        color: 'rgb(52, 211, 153)',
        font: { size: 10 },
        callback: (v) => fmtCompact(v),
      },
      grid: { drawOnChartArea: false },
    },
  },
}))

// ── Weekday chart ──────────────────────────────────────────────────
const weekdayLabels = computed(() => t('report.monthly.charts.weekdays'))

const weekdayChartData = computed(() => {
  const days = data.value?.weekdayPattern ?? []
  return {
    labels: weekdayLabels.value,
    datasets: [
      {
        label: t('report.monthly.metrics.orders'),
        data: days.map((d) => d.orderCount),
        backgroundColor: 'rgba(167, 139, 250, 0.75)',
        borderColor: 'rgb(167, 139, 250)',
        borderWidth: 1,
        borderRadius: 4,
        yAxisID: 'yOrders',
      },
      {
        label: t('report.monthly.metrics.revenue'),
        data: days.map((d) => d.revenue),
        backgroundColor: 'rgba(251, 191, 36, 0.6)',
        borderColor: 'rgb(251, 191, 36)',
        borderWidth: 1,
        borderRadius: 4,
        type: 'line',
        yAxisID: 'yRevenue',
        tension: 0.3,
        pointRadius: 3,
      },
    ],
  }
})

const weekdayChartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  interaction: { mode: 'index' },
  plugins: {
    legend: {
      position: 'top',
      labels: { color: 'rgb(156, 163, 175)', padding: 12, font: { size: 11 } },
    },
    tooltip: {
      callbacks: {
        label: (ctx) =>
          ctx.dataset.yAxisID === 'yRevenue'
            ? ` ${ctx.dataset.label}: ${fmt(ctx.parsed.y)}`
            : ` ${ctx.dataset.label}: ${ctx.parsed.y}`,
      },
    },
  },
  scales: {
    x: {
      ticks: { color: 'rgb(156, 163, 175)', font: { size: 11 } },
      grid: { color: 'rgba(255,255,255,0.05)' },
    },
    yOrders: {
      type: 'linear',
      position: 'left',
      ticks: { color: 'rgb(167, 139, 250)', font: { size: 10 }, stepSize: 1 },
      grid: { color: 'rgba(255,255,255,0.05)' },
    },
    yRevenue: {
      type: 'linear',
      position: 'right',
      ticks: {
        color: 'rgb(251, 191, 36)',
        font: { size: 10 },
        callback: (v) => fmtCompact(v),
      },
      grid: { drawOnChartArea: false },
    },
  },
}))

// ── Takeaway donut ─────────────────────────────────────────────────
const takeawayChartData = computed(() => {
  const ratio = data.value?.takeawayRatio ?? 0
  return {
    labels: [t('report.monthly.takeaway.takeaway'), t('report.monthly.takeaway.dineIn')],
    datasets: [
      {
        data: [ratio, Math.max(0, 100 - ratio)],
        backgroundColor: ['rgba(96, 165, 250, 0.85)', 'rgba(52, 211, 153, 0.85)'],
        borderColor: ['rgb(96, 165, 250)', 'rgb(52, 211, 153)'],
        borderWidth: 1,
      },
    ],
  }
})

const takeawayChartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: {
      position: 'bottom',
      labels: { color: 'rgb(156, 163, 175)', font: { size: 11 }, padding: 12 },
    },
    tooltip: {
      callbacks: { label: (ctx) => ` ${ctx.label}: ${ctx.parsed.toFixed(1)}%` },
    },
  },
}
</script>

<template>
  <div class="tw:space-y-6">
    <!-- Month navigator -->
    <div class="tw:flex tw:items-center tw:gap-3">
      <prime-button
        size="small"
        severity="secondary"
        outlined
        :pt="{ root: { class: btnIcon } }"
        @click="goPrev"
      >
        <iconify icon="ph:caret-left-bold" />
      </prime-button>

      <span class="tw:text-sm tw:font-semibold tw:min-w-40 tw:text-center tw:capitalize">
        {{ monthLabel }}
      </span>

      <prime-button
        size="small"
        severity="secondary"
        outlined
        :pt="{ root: { class: btnIcon } }"
        @click="goNext"
      >
        <iconify icon="ph:caret-right-bold" />
      </prime-button>

      <span class="tw:text-xs tw:text-muted tw:ml-1">
        {{ t('report.monthly.vs') }} {{ prevMonthLabel }}
      </span>

      <prime-button
        class="tw:ml-auto"
        severity="secondary"
        outlined
        size="small"
        :loading="loading"
        @click="load"
      >
        <iconify icon="ph:arrows-clockwise-bold" class="tw:mr-1.5" />
        <span>{{ t('report.refresh') }}</span>
      </prime-button>
    </div>

    <!-- Error -->
    <prime-message
      v-if="error"
      severity="error"
      size="small"
      variant="simple"
      :closable="true"
      @close="error = ''"
    >{{ error }}</prime-message>

    <!-- Loading skeleton -->
    <template v-if="!data && loading">
      <div class="tw:grid tw:gap-3 tw:grid-cols-2 md:tw:grid-cols-5">
        <prime-skeleton v-for="n in 5" :key="n" height="7rem" class="tw:rounded-xl" />
      </div>
      <div class="tw:grid tw:grid-cols-2 tw:gap-4">
        <prime-skeleton height="16rem" class="tw:rounded-xl" />
        <prime-skeleton height="16rem" class="tw:rounded-xl" />
      </div>
      <prime-skeleton height="14rem" class="tw:rounded-xl" />
    </template>

    <!-- No data -->
    <div
      v-else-if="data && data.current.totalOrders === 0"
      class="tw:text-sm tw:text-muted tw:text-center tw:py-10"
    >
      {{ t('report.monthly.noData') }}
    </div>

    <!-- Content -->
    <template v-else-if="data">
      <!-- KPI comparison cards -->
      <div class="tw:grid tw:gap-3 tw:grid-cols-2 md:tw:grid-cols-5">
        <prime-card
          v-for="m in metrics"
          :key="m.key"
          :pt="{
            root: { class: `${appCard} ${cardRing} tw:p-4` },
            body: { class: 'tw:p-0! tw:h-full' },
            content: { class: 'tw:p-0! tw:flex tw:flex-col tw:gap-1 tw:mt-2' },
          }"
        >
          <template #header>
            <div class="tw:flex tw:items-center tw:justify-between">
              <p class="tw:text-[11px] tw:uppercase tw:tracking-[0.2em] tw:truncate app-text-subtle">
                {{ m.label }}
              </p>
              <iconify :icon="m.icon" :class="[m.iconColor, 'tw:opacity-70 tw:shrink-0']" />
            </div>
          </template>
          <template #content>
            <!-- Current value -->
            <p class="tw:text-base tw:font-bold tw:truncate">{{ m.format(m.current) }}</p>
            <!-- Previous value -->
            <p class="tw:text-xs tw:text-muted tw:truncate">{{ m.format(m.previous) }}</p>
            <!-- Delta -->
            <div
              v-if="m.delta !== null"
              class="tw:flex tw:items-center tw:gap-1 tw:text-xs tw:font-semibold"
              :class="m.delta >= 0 ? 'tw:text-emerald-400' : 'tw:text-rose-400'"
            >
              <iconify :icon="m.delta >= 0 ? 'ph:trend-up-bold' : 'ph:trend-down-bold'" />
              <span>{{ m.delta >= 0 ? '+' : '' }}{{ m.delta.toFixed(1) }}%</span>
            </div>
            <div v-else class="tw:text-xs tw:text-muted">—</div>
          </template>
        </prime-card>
      </div>

      <!-- Charts row: peak hours + weekday -->
      <div class="tw:grid tw:grid-cols-1 md:tw:grid-cols-2 tw:gap-4">
        <!-- Peak hours -->
        <prime-card
          :pt="{
            root: { class: `${appCard} ${cardRing} tw:p-4` },
            body: { class: 'tw:p-0!' },
            header: { class: 'tw:flex tw:flex-col tw:gap-0.5 tw:mb-4' },
            content: { class: 'tw:h-56' },
          }"
        >
          <template #header>
            <div class="tw:flex tw:items-center tw:gap-2">
              <iconify icon="ph:clock-bold" class="app-text-subtle" />
              <span class="tw:text-sm tw:font-medium">{{ t('report.monthly.charts.peakHours') }}</span>
            </div>
            <p class="tw:text-xs tw:text-muted">{{ t('report.monthly.charts.peakHoursSubtitle') }}</p>
          </template>
          <template #content>
            <prime-chart type="bar" :data="peakHoursChartData" :options="peakHoursChartOptions" class="tw:h-full" />
          </template>
        </prime-card>

        <!-- Weekday pattern -->
        <prime-card
          :pt="{
            root: { class: `${appCard} ${cardRing} tw:p-4` },
            body: { class: 'tw:p-0!' },
            header: { class: 'tw:flex tw:flex-col tw:gap-0.5 tw:mb-4' },
            content: { class: 'tw:h-56' },
          }"
        >
          <template #header>
            <div class="tw:flex tw:items-center tw:gap-2">
              <iconify icon="ph:calendar-bold" class="app-text-subtle" />
              <span class="tw:text-sm tw:font-medium">{{ t('report.monthly.charts.weekday') }}</span>
            </div>
            <p class="tw:text-xs tw:text-muted">{{ t('report.monthly.charts.weekdaySubtitle') }}</p>
          </template>
          <template #content>
            <prime-chart type="bar" :data="weekdayChartData" :options="weekdayChartOptions" class="tw:h-full" />
          </template>
        </prime-card>
      </div>

      <!-- Top products table -->
      <prime-card
        v-if="data.topProducts?.length"
        :pt="{
          root: { class: `${appCard} ${cardRing}` },
          body: { class: 'tw:p-0!' },
          header: { class: 'tw:flex tw:flex-col tw:gap-0.5 tw:px-4 tw:pt-4 tw:pb-2' },
          content: { class: 'tw:p-0!' },
        }"
      >
        <template #header>
          <div class="tw:flex tw:items-center tw:gap-2">
            <iconify icon="ph:trophy-bold" class="tw:text-amber-400" />
            <span class="tw:text-sm tw:font-medium">{{ t('report.monthly.topProducts.title') }}</span>
          </div>
          <p class="tw:text-xs tw:text-muted">{{ t('report.monthly.topProducts.subtitle') }}</p>
        </template>
        <template #content>
          <prime-data-table
            :value="data.topProducts"
            size="small"
            :pt="{ root: { class: 'tw:text-sm' } }"
          >
            <prime-column :header="t('report.monthly.topProducts.rank')" style="width:50px">
              <template #body="{ data: row }">
                <span class="tw:font-mono tw:font-bold tw:text-xs">{{ row.rank }}</span>
              </template>
            </prime-column>

            <prime-column :header="t('report.monthly.topProducts.product')">
              <template #body="{ data: row }">
                <span class="tw:font-medium">{{ row.name }}</span>
              </template>
            </prime-column>

            <prime-column :header="t('report.monthly.topProducts.qty')" style="width:80px">
              <template #body="{ data: row }">
                <span>{{ row.qty }}</span>
              </template>
            </prime-column>

            <prime-column :header="t('report.monthly.topProducts.revenue')" style="width:140px">
              <template #body="{ data: row }">
                <span class="tw:font-medium tw:text-emerald-400">{{ fmt(row.revenue) }}</span>
              </template>
            </prime-column>

            <prime-column :header="t('report.monthly.topProducts.change')" style="width:100px">
              <template #body="{ data: row }">
                <!-- New product (no previous rank) -->
                <prime-tag
                  v-if="row.prevRank === null || row.prevRank === undefined"
                  :value="t('report.monthly.topProducts.new')"
                  severity="info"
                  size="small"
                />
                <!-- Rank improved -->
                <div
                  v-else-if="row.rankChange > 0"
                  class="tw:flex tw:items-center tw:gap-1 tw:text-emerald-400 tw:text-xs tw:font-semibold"
                >
                  <iconify icon="ph:arrow-up-bold" />
                  <span>{{ row.rankChange }}</span>
                </div>
                <!-- Rank dropped -->
                <div
                  v-else-if="row.rankChange < 0"
                  class="tw:flex tw:items-center tw:gap-1 tw:text-rose-400 tw:text-xs tw:font-semibold"
                >
                  <iconify icon="ph:arrow-down-bold" />
                  <span>{{ Math.abs(row.rankChange) }}</span>
                </div>
                <!-- No change -->
                <span v-else class="tw:text-muted tw:text-xs">—</span>
              </template>
            </prime-column>
          </prime-data-table>
        </template>
      </prime-card>

      <!-- Worst sellers + Takeaway -->
      <div class="tw:grid tw:grid-cols-1 md:tw:grid-cols-2 tw:gap-4">
        <!-- Worst sellers -->
        <prime-card
          v-if="data.worstSellers?.length"
          :pt="{
            root: { class: `${appCard} ${cardRing} tw:p-4` },
            body: { class: 'tw:p-0!' },
            header: { class: 'tw:flex tw:flex-col tw:gap-0.5 tw:mb-3' },
            content: { class: 'tw:p-0! tw:space-y-2' },
          }"
        >
          <template #header>
            <div class="tw:flex tw:items-center tw:gap-2">
              <iconify icon="ph:arrow-down-bold" class="tw:text-rose-400" />
              <span class="tw:text-sm tw:font-medium">{{ t('report.monthly.worstSellers.title') }}</span>
            </div>
            <p class="tw:text-xs tw:text-muted">{{ t('report.monthly.worstSellers.subtitle') }}</p>
          </template>
          <template #content>
            <div
              v-for="(item, idx) in data.worstSellers"
              :key="item.name"
              class="tw:flex tw:items-center tw:justify-between tw:py-1.5 tw:border-b tw:border-white/5 last:tw:border-0"
            >
              <div class="tw:flex tw:items-center tw:gap-2">
                <span class="tw:text-xs tw:text-muted tw:w-5 tw:text-right">{{ idx + 1 }}.</span>
                <span class="tw:text-sm">{{ item.name }}</span>
              </div>
              <div class="tw:flex tw:items-center tw:gap-3">
                <span class="tw:text-xs tw:text-muted">{{ item.qty }} {{ t('report.topProducts.unit') }}</span>
                <span class="tw:text-xs tw:font-medium tw:text-emerald-400">{{ fmt(item.revenue) }}</span>
              </div>
            </div>
          </template>
        </prime-card>

        <!-- Takeaway ratio -->
        <prime-card
          :pt="{
            root: { class: `${appCard} ${cardRing} tw:p-4` },
            body: { class: 'tw:p-0!' },
            header: { class: 'tw:flex tw:items-center tw:gap-2 tw:mb-3' },
            content: { class: 'tw:p-0! tw:h-52' },
          }"
        >
          <template #header>
            <iconify icon="ph:bag-bold" class="tw:text-blue-400" />
            <span class="tw:text-sm tw:font-medium">{{ t('report.monthly.takeaway.title') }}</span>
            <span class="tw:ml-auto tw:text-xs tw:text-muted">
              {{ data.takeawayRatio.toFixed(1) }}% {{ t('report.monthly.takeaway.takeaway') }}
            </span>
          </template>
          <template #content>
            <prime-chart
              v-if="data.current.totalOrders > 0"
              type="doughnut"
              :data="takeawayChartData"
              :options="takeawayChartOptions"
              class="tw:h-full"
            />
            <div v-else class="tw:flex tw:items-center tw:justify-center tw:h-full tw:text-xs tw:text-muted">
              {{ t('report.empty') }}
            </div>
          </template>
        </prime-card>
      </div>
    </template>
  </div>
</template>
