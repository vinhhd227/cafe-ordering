<script setup>
import { useI18n } from 'vue-i18n'
import { getOrdersSummary } from '@/services/report.service'
import WidgetOrdersRevenue from '@/components/widgets/orders/WidgetOrdersRevenue.vue'
import WidgetOrdersSummary from '@/components/widgets/orders/WidgetOrdersSummary.vue'
import WidgetStat from '@/components/widgets/WidgetStat.vue'

const { t } = useI18n()

// ── Helpers ────────────────────────────────────────────────────────
const toMidnight = (d) => {
  if (!d) return undefined
  return new Date(d.getFullYear(), d.getMonth(), d.getDate())
}

const fmt = (value) =>
  new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value ?? 0)

const fmtDate = (dateStr) => {
  const [, m, d] = dateStr.split('-')
  return `${d}/${m}`
}

// ── Default date range: first day of current month → today ─────────
const firstOfMonth = () => {
  const d = new Date()
  d.setDate(1)
  d.setHours(0, 0, 0, 0)
  return d
}
const todayMidnight = () => {
  const d = new Date()
  d.setHours(0, 0, 0, 0)
  return d
}

const dateRange = ref([firstOfMonth(), todayMidnight()])
const dateFrom  = computed(() => dateRange.value?.[0] ?? null)
const dateTo    = computed(() => dateRange.value?.[1] ?? null)

// ── Quick presets (computed so labels react to locale changes) ──────
const presets = computed(() => [
  {
    label: t('report.presets.today'),
    apply() {
      dateRange.value = [todayMidnight(), todayMidnight()]
    },
  },
  {
    label: t('report.presets.yesterday'),
    apply() {
      const d = new Date()
      d.setDate(d.getDate() - 1)
      d.setHours(0, 0, 0, 0)
      dateRange.value = [d, new Date(d)]
    },
  },
  {
    label: t('report.presets.last7days'),
    apply() {
      const d = new Date()
      d.setDate(d.getDate() - 6)
      d.setHours(0, 0, 0, 0)
      dateRange.value = [d, todayMidnight()]
    },
  },
  {
    label: t('report.presets.thisMonth'),
    apply() {
      dateRange.value = [firstOfMonth(), todayMidnight()]
    },
  },
  {
    label: t('report.presets.lastMonth'),
    apply() {
      const now   = new Date()
      const first = new Date(now.getFullYear(), now.getMonth() - 1, 1)
      const last  = new Date(now.getFullYear(), now.getMonth(), 0)
      last.setHours(0, 0, 0, 0)
      dateRange.value = [first, last]
    },
  },
])

// ── Data ───────────────────────────────────────────────────────────
const loading = ref(false)
const error   = ref('')
const data    = ref(null)

const avgOrderValue = computed(() => {
  if (!data.value || data.value.completedOrders === 0) return 0
  return data.value.totalRevenue / data.value.completedOrders
})

const avgDrinksPerDay = computed(() => {
  const days = data.value?.dailyRevenue?.length
  if (!days) return 0
  return data.value.totalItemsSold / days
})

const avgRevPerDay = computed(() => {
  const days = data.value?.dailyRevenue?.length
  if (!days) return 0
  return data.value.totalRevenue / days
})

const avgGuestsPerDay = computed(() => {
  const days = data.value?.dailyRevenue?.length
  const guests = data.value?.totalGuestCount ?? 0
  if (!days) return 0
  return guests / days
})

const avgOrdersPerDay = computed(() => {
  const days = data.value?.dailyRevenue?.length
  if (!days) return 0
  return (data.value?.totalOrders ?? 0) / days
})

// ── Chart ──────────────────────────────────────────────────────────
const chartData = computed(() => {
  const daily = data.value?.dailyRevenue ?? []
  return {
    labels: daily.map((d) => fmtDate(d.date)),
    datasets: [
      {
        label: t('report.chart.cash'),
        data: daily.map((d) => d.cashRevenue),
        backgroundColor: 'rgba(52, 211, 153, 0.75)',
        borderColor: 'rgb(52, 211, 153)',
        borderWidth: 1,
        borderRadius: 4,
        stack: 'revenue',
      },
      {
        label: t('report.chart.bank'),
        data: daily.map((d) => d.bankRevenue),
        backgroundColor: 'rgba(96, 165, 250, 0.75)',
        borderColor: 'rgb(96, 165, 250)',
        borderWidth: 1,
        borderRadius: 4,
        stack: 'revenue',
      },
    ],
  }
})

const chartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: {
      position: 'top',
      labels: {
        color: 'rgb(156, 163, 175)',
        padding: 16,
        font: { size: 11 },
      },
    },
    tooltip: {
      callbacks: {
        label: (ctx) => ` ${ctx.dataset.label}: ${fmt(ctx.parsed.y)}`,
        footer: (items) => {
          const total = items.reduce((s, i) => s + i.parsed.y, 0)
          return `${t('report.chart.total')}: ${fmt(total)}`
        },
      },
    },
  },
  scales: {
    x: {
      stacked: true,
      ticks: { color: 'rgb(156, 163, 175)', font: { size: 11 } },
      grid: { color: 'rgba(255,255,255,0.05)' },
    },
    y: {
      stacked: true,
      ticks: {
        color: 'rgb(156, 163, 175)',
        font: { size: 11 },
        callback: (v) =>
          new Intl.NumberFormat('vi-VN', { notation: 'compact', maximumFractionDigits: 1 }).format(v),
      },
      grid: { color: 'rgba(255,255,255,0.05)' },
    },
  },
}))

// ── Items chart ────────────────────────────────────────────────────
const itemsChartData = computed(() => {
  const daily = data.value?.dailyRevenue ?? []
  return {
    labels: daily.map((d) => fmtDate(d.date)),
    datasets: [
      {
        label: t('report.itemsChart.label'),
        data: daily.map((d) => d.itemsSold),
        backgroundColor: 'rgba(167, 139, 250, 0.75)',  // purple-400
        borderColor: 'rgb(167, 139, 250)',
        borderWidth: 1,
        borderRadius: 4,
      },
    ],
  }
})

const itemsChartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: {
      position: 'top',
      labels: { color: 'rgb(156, 163, 175)', padding: 16, font: { size: 11 } },
    },
    tooltip: {
      callbacks: {
        label: (ctx) => ` ${ctx.dataset.label}: ${ctx.parsed.y}`,
      },
    },
  },
  scales: {
    x: {
      ticks: { color: 'rgb(156, 163, 175)', font: { size: 11 } },
      grid: { color: 'rgba(255,255,255,0.05)' },
    },
    y: {
      ticks: {
        color: 'rgb(156, 163, 175)',
        font: { size: 11 },
        callback: (v) => Math.round(v),
        stepSize: 1,
      },
      grid: { color: 'rgba(255,255,255,0.05)' },
    },
  },
}))

// ── Export ─────────────────────────────────────────────────────────
const reportContent = ref(null)
const exporting     = ref(false)
const exportMenu    = ref(null)

const exportMenuItems = computed(() => [
  { label: t('report.exportPng'), command: downloadPng },
  { label: t('report.exportPdf'), command: downloadPdf },
])

const fileStem = computed(() => {
  const f = dateFrom.value
  const t2 = dateTo.value
  if (!f || !t2) return new Date().toISOString().slice(0, 10)
  const iso = (d) => d.toISOString().slice(0, 10)
  return f.toDateString() === t2.toDateString() ? iso(f) : `${iso(f)}_${iso(t2)}`
})

const downloadPng = async () => {
  if (!data.value) return
  exporting.value = true
  try {
    const { toPng } = await import('html-to-image')
    const dataUrl = await toPng(reportContent.value, { pixelRatio: 2 })
    const a = document.createElement('a')
    a.download = `report-${fileStem.value}.png`
    a.href = dataUrl
    a.click()
  } finally {
    exporting.value = false
  }
}

const downloadPdf = async () => {
  if (!data.value) return
  exporting.value = true
  try {
    const { toPng } = await import('html-to-image')
    const { jsPDF }  = await import('jspdf')
    const dataUrl = await toPng(reportContent.value, { pixelRatio: 2 })
    const img = new Image()
    img.src = dataUrl
    await new Promise(r => { img.onload = r })
    const pw = img.naturalWidth  / 2
    const ph = img.naturalHeight / 2
    const pdf = new jsPDF({
      orientation: pw > ph ? 'landscape' : 'portrait',
      unit: 'px',
      format: [pw, ph],
    })
    pdf.addImage(dataUrl, 'PNG', 0, 0, pw, ph)
    pdf.save(`report-${fileStem.value}.pdf`)
  } finally {
    exporting.value = false
  }
}

// ── Load ───────────────────────────────────────────────────────────
const load = async () => {
  loading.value = true
  error.value   = ''
  try {
    const res  = await getOrdersSummary({
      dateFrom: toMidnight(dateFrom.value),
      dateTo:   toMidnight(dateTo.value),
    })
    data.value = res.data
  } catch (e) {
    error.value = e?.response?.data?.message ?? t('report.error')
  } finally {
    loading.value = false
  }
}

watch(dateRange, (val) => {
  if (val?.[0] && val?.[1]) load()
})
onMounted(load)
</script>

<template>
  <section class="tw:space-y-8">
    <!-- Header -->
    <page-header :subtitle="t('report.subtitle')">
      <prime-button
        severity="secondary"
        outlined
        size="small"
        :loading="loading"
        @click="load"
      >
        <iconify icon="ph:arrows-clockwise-bold" class="tw:mr-1.5" />
        <span>{{ t('report.refresh') }}</span>
      </prime-button>
      <prime-button
        severity="secondary"
        outlined
        size="small"
        :loading="exporting"
        :disabled="!data"
        @click="exportMenu.toggle($event)"
      >
        <iconify icon="ph:download-simple-bold" class="tw:mr-1.5" />
        <span>{{ t('report.export') }}</span>
        <iconify icon="ph:caret-down-bold" class="tw:ml-1 tw:text-[10px]" />
      </prime-button>
      <prime-menu ref="exportMenu" :model="exportMenuItems" popup />
    </page-header>

    <!-- Date range filter -->
    <div class="tw:flex tw:flex-wrap tw:items-center tw:gap-2">
      <prime-date-picker
        v-model="dateRange"
        selection-mode="range"
        :number-of-months="2"
        date-format="dd/mm/yy"
        show-button-bar
        class="app-input tw:w-55"
      />
      <prime-button
        v-for="p in presets"
        :key="p.label"
        size="small"
        severity="secondary"
        outlined
        class="tw:text-xs"
        @click="p.apply()"
      >
        {{ p.label }}
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
      <div class="tw:grid tw:gap-3 tw:grid-cols-2 md:tw:grid-cols-4">
        <prime-skeleton v-for="n in 7" :key="n" height="5.5rem" class="tw:rounded-xl" />
      </div>
      <prime-skeleton height="18rem" class="tw:rounded-xl" />
      <prime-skeleton height="14rem" class="tw:rounded-xl" />
    </template>

    <!-- Content -->
    <div v-if="data" ref="reportContent" class="tw:space-y-8">
      <!-- Export header: date range label (visible in capture) -->
      <p class="tw:text-sm app-text-muted">
        {{ fileStem.replace('_', ' → ') }}
      </p>
      <!-- Summary widgets -->
      <div class="tw:grid tw:gap-3 tw:grid-cols-3 md:tw:grid-cols-3">
        <widget-orders-revenue
          :total="data.totalRevenue"
          :cash="data.cashRevenue"
          :bank="data.bankRevenue"
        />
        <widget-orders-summary
          :total="data.totalOrders"
          :pending="data.pendingOrders"
          :processing="data.processingOrders"
          :completed="data.completedOrders"
          :cancelled="data.cancelledOrders"
        />

        <!-- Avg per day combined widget -->
        <prime-card
          :pt="{
            root: { class: `${appCard} ${cardRing} tw:p-4` },
            body: { class: 'tw:p-0! tw:h-full' },
            content: { class: 'tw:h-full tw:flex tw:flex-col tw:justify-between' },
          }"
        >
          <template #header>
            <p class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] tw:truncate app-text-subtle">
              {{ t('report.widgets.avgPerDay.title') }}
            </p>
            <iconify icon="ph:trend-up-bold" class="tw:text-violet-400 tw:opacity-70 tw:shrink-0" />
          </template>
          <template #content>
            <div class="tw:grid tw:grid-cols-2 tw:gap-x-4 tw:mt-1">
              <!-- Revenue / day -->
              <div class="tw:flex tw:items-center tw:justify-between tw:py-1.5">
                <div class="tw:flex tw:items-center tw:gap-1.5">
                  <iconify icon="ph:coins-bold" class="tw:text-rose-400 tw:text-sm tw:opacity-80" />
                  <span class="tw:text-xs app-text-muted">{{ t('report.widgets.avgPerDay.rev') }}</span>
                </div>
                <span class="tw:text-xs tw:font-semibold">{{ fmt(avgRevPerDay) }}</span>
              </div>
              <!-- Drinks / day -->
              <div class="tw:flex tw:items-center tw:justify-between tw:py-1.5">
                <div class="tw:flex tw:items-center tw:gap-1.5">
                  <iconify icon="ph:coffee-bold" class="tw:text-cyan-400 tw:text-sm tw:opacity-80" />
                  <span class="tw:text-xs app-text-muted">{{ t('report.widgets.avgPerDay.drinks') }}</span>
                </div>
                <span class="tw:text-xs tw:font-semibold">
                  {{ avgDrinksPerDay % 1 === 0 ? avgDrinksPerDay : avgDrinksPerDay.toFixed(1) }}
                </span>
              </div>
              <!-- Orders / day -->
              <div class="tw:flex tw:items-center tw:justify-between tw:py-1.5">
                <div class="tw:flex tw:items-center tw:gap-1.5">
                  <iconify icon="ph:receipt-bold" class="tw:text-green-400 tw:text-sm tw:opacity-80" />
                  <span class="tw:text-xs app-text-muted">{{ t('report.widgets.avgPerDay.orders') }}</span>
                </div>
                <span class="tw:text-xs tw:font-semibold">
                  {{ avgOrdersPerDay % 1 === 0 ? avgOrdersPerDay : avgOrdersPerDay.toFixed(1) }}
                </span>
              </div>
              <!-- Guests / day -->
              <div class="tw:flex tw:items-center tw:justify-between tw:py-1.5">
                <div class="tw:flex tw:items-center tw:gap-1.5">
                  <iconify icon="ph:users-bold" class="tw:text-amber-400 tw:text-sm tw:opacity-80" />
                  <span class="tw:text-xs app-text-muted">{{ t('report.widgets.avgPerDay.guests') }}</span>
                </div>
                <span class="tw:text-xs tw:font-semibold">
                  {{ avgGuestsPerDay % 1 === 0 ? avgGuestsPerDay : avgGuestsPerDay.toFixed(1) }}
                </span>
              </div>
            </div>
          </template>
        </prime-card>
      </div>
 <!-- Top products & Top categories -->
      <div
        v-if="data.topProducts?.length || data.topCategories?.length"
        class="tw:grid tw:grid-cols-2 tw:gap-4 md:tw:grid-cols-2"
      >
        <widget-top-products
          v-if="data.topProducts?.length"
          :title="t('report.topProducts.title')"
          :subtitle="t('report.topProducts.subtitle')"
          :unit="t('report.topProducts.unit')"
          :items="data.topProducts"
        />
        <widget-top-categories
          v-if="data.topCategories?.length"
          :title="t('report.topCategories.title')"
          :subtitle="t('report.topCategories.subtitle')"
          :items="data.topCategories"
        />
      </div>
      <!-- Chart -->
      <prime-card
        v-if="data.dailyRevenue.length > 0"
        :pt="{
          root: { class: `${appCard} ${cardRing} tw:p-4` },
          body: { class: 'tw:p-0!' },
          header: { class: 'tw:flex tw:items-center tw:gap-2 tw:mb-4' },
          content: { class: 'tw:h-64' },
        }"
      >
        <template #header>
          <iconify icon="ph:chart-bar-bold" class="app-text-subtle" />
          <span class="tw:text-sm tw:font-medium">{{ t('report.chart.title') }}</span>
        </template>
        <template #content>
          <prime-chart type="bar" :data="chartData" :options="chartOptions" class="tw:h-full" />
        </template>
      </prime-card>

      <!-- Items sold chart -->
      <prime-card
        v-if="data.dailyRevenue.length > 0"
        :pt="{
          root: { class: `${appCard} ${cardRing} tw:p-4` },
          body: { class: 'tw:p-0!' },
          header: { class: 'tw:flex tw:items-center tw:gap-2 tw:mb-4' },
          content: { class: 'tw:h-48' },
        }"
      >
        <template #header>
          <iconify icon="ph:coffee-bold" class="app-text-subtle" />
          <span class="tw:text-sm tw:font-medium">{{ t('report.itemsChart.title') }}</span>
        </template>
        <template #content>
          <prime-chart type="bar" :data="itemsChartData" :options="itemsChartOptions" class="tw:h-full" />
        </template>
      </prime-card>

      <!-- Daily revenue table -->
      <prime-card
        v-if="data.dailyRevenue.length > 0"
        :pt="{
          root: { class: `${appCard} ${cardRing}` },
          body: { class: 'tw:p-0!' },
          header: { class: 'tw:flex tw:items-center tw:gap-2 tw:px-4 tw:pt-4 tw:pb-2' },
          content: { class: 'tw:p-0!' },
        }"
      >
        <template #header>
          <iconify icon="ph:table-bold" class="app-text-subtle" />
          <span class="tw:text-sm tw:font-medium">{{ t('report.table.title') }}</span>
        </template>
        <template #content>
          <prime-data-table
            :value="data.dailyRevenue"
            size="small"
            :pt="{ root: { class: 'tw:text-sm' } }"
          >
            <prime-column field="date" :header="t('report.table.date')" :sortable="true">
              <template #body="{ data: row }">
                <span class="tw:font-mono tw:text-xs">
                  {{ row.date.split('-').reverse().join('/') }}
                </span>
              </template>
            </prime-column>

            <prime-column field="revenue" :header="t('report.table.revenue')" :sortable="true">
              <template #body="{ data: row }">
                <span class="tw:font-medium">{{ fmt(row.revenue) }}</span>
              </template>
            </prime-column>

            <prime-column field="cashRevenue" :header="t('report.table.cash')" :sortable="true">
              <template #body="{ data: row }">
                <span class="tw:text-emerald-400">{{ fmt(row.cashRevenue) }}</span>
              </template>
            </prime-column>

            <prime-column field="bankRevenue" :header="t('report.table.bank')" :sortable="true">
              <template #body="{ data: row }">
                <span class="tw:text-blue-400">{{ fmt(row.bankRevenue) }}</span>
              </template>
            </prime-column>

            <prime-column field="orderCount" :header="t('report.table.orders')" :sortable="true" style="width:90px">
              <template #body="{ data: row }">
                <prime-tag :value="String(row.orderCount)" severity="secondary" />
              </template>
            </prime-column>

            <prime-column field="completedCount" :header="t('report.table.completed')" :sortable="true" style="width:110px">
              <template #body="{ data: row }">
                <prime-tag
                  :value="String(row.completedCount)"
                  :severity="row.completedCount > 0 ? 'success' : 'secondary'"
                />
              </template>
            </prime-column>

            <prime-column field="itemsSold" :header="t('report.table.items')" :sortable="true" style="width:90px">
              <template #body="{ data: row }">
                <prime-tag
                  :value="String(row.itemsSold)"
                  :severity="row.itemsSold > 0 ? 'warn' : 'secondary'"
                />
              </template>
            </prime-column>
          </prime-data-table>
        </template>
      </prime-card>

     

      <!-- Empty state -->
      <div
        v-if="data.dailyRevenue.length === 0"
        class="tw:flex tw:flex-col tw:items-center tw:justify-center tw:py-16 app-text-subtle"
      >
        <iconify icon="ph:chart-bar-bold" class="tw:text-4xl tw:mb-2 tw:opacity-30" />
        <p class="tw:text-sm">{{ t('report.empty') }}</p>
      </div>
    </div>
  </section>
</template>
