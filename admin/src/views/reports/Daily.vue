<script setup>
import { useI18n } from 'vue-i18n'
import { getDailyReport, getDailyOrders } from '@/services/report.service'

const { t } = useI18n()
const toast = useToast()

// ── Helpers ──────────────────────────────────────────────────────────
const fmt = (v) =>
  new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(v ?? 0)

const fmtCompact = (v) =>
  new Intl.NumberFormat('vi-VN', { notation: 'compact', maximumFractionDigits: 1 }).format(v ?? 0).replace(/\u00a0?N$/, 'K')

const toApiDate = (d) => {
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

const fmtTime = (iso) => {
  if (!iso) return ''
  const d = new Date(iso)
  return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`
}

const todayMidnight = () => {
  const d = new Date()
  d.setHours(0, 0, 0, 0)
  return d
}


// ── Date ─────────────────────────────────────────────────────────────
const selectedDate = ref(todayMidnight())

const setToday = () => { selectedDate.value = todayMidnight() }
const setYesterday = () => {
  const d = new Date()
  d.setDate(d.getDate() - 1)
  d.setHours(0, 0, 0, 0)
  selectedDate.value = d
}

// ── Data ─────────────────────────────────────────────────────────────
const loading = ref(false)
const reportData = ref(null)
const orders = ref([])

const progressReady = ref(false)

const load = async () => {
  loading.value = true
  progressReady.value = false
  const dateStr = toApiDate(selectedDate.value)
  try {
    const [rRes, oRes] = await Promise.all([
      getDailyReport({ date: dateStr }),
      getDailyOrders({ date: dateStr }),
    ])
    reportData.value = rRes.data
    orders.value = oRes.data.orders ?? []
    await nextTick()
    setTimeout(() => { progressReady.value = true }, 50)
  } catch {
    toast.add({ severity: 'error', summary: t('report.daily.error'), life: 5000 })
  } finally {
    loading.value = false
  }
}

onMounted(() => load())
watch(selectedDate, () => load())

// ── Delta % ───────────────────────────────────────────────────────────
const deltaPct = (current, base) => {
  if (!base) return null
  return ((current - base) / Math.abs(base)) * 100
}

// ── Payment progress ──────────────────────────────────────────────────
const cashPct = computed(() => {
  const total = reportData.value?.revenue?.total ?? 0
  if (!total) return 0
  return Math.round(((reportData.value?.revenue?.cash ?? 0) / total) * 100)
})
const transferPct = computed(() => {
  const total = reportData.value?.revenue?.total ?? 0
  if (!total) return 0
  return 100 - cashPct.value
})

// ── Hourly chart ──────────────────────────────────────────────────────
const CHART_HOURS = Array.from({ length: 15 }, (_, i) => `${String(i + 8).padStart(2, '0')}:00`)

const hourlyRevFilled = computed(() => {
  const map = Object.fromEntries((reportData.value?.hourlyRevenue ?? []).map(h => [h.hour, h.revenue]))
  return CHART_HOURS.map(h => map[h] ?? 0)
})

const hourlyAvgFilled = computed(() => {
  const map = Object.fromEntries((reportData.value?.hourlyAvg7Days ?? []).map(h => [h.hour, h.revenue]))
  return CHART_HOURS.map(h => map[h] ?? 0)
})

const peakHour = computed(() => reportData.value?.peakHour?.hour ?? null)

const hourlyChartData = computed(() => ({
  labels: CHART_HOURS,
  datasets: [
    {
      type: 'bar',
      label: t('report.daily.hourlyChart.today'),
      data: hourlyRevFilled.value,
      backgroundColor: CHART_HOURS.map(h => h === peakHour.value ? '#BA7517' : '#EF9F27'),
      borderRadius: 4,
      order: 1,
    },
    {
      type: 'line',
      label: t('report.daily.hourlyChart.avg7Days'),
      data: hourlyAvgFilled.value,
      borderColor: '#888780',
      borderWidth: 1.5,
      borderDash: [4, 4],
      pointRadius: 0,
      fill: false,
      tension: 0,
      order: 0,
    },
  ],
}))

const hourlyChartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  interaction: { mode: 'index' },
  plugins: {
    legend: { display: false },
    tooltip: {
      callbacks: {
        label: (ctx) => ` ${ctx.dataset.label}: ${fmt(ctx.parsed.y)}`,
      },
    },
  },
  scales: {
    x: {
      ticks: { color: 'rgb(156,163,175)', font: { size: 10 }, maxRotation: 0 },
      grid: { color: 'rgba(255,255,255,0.05)' },
    },
    y: {
      ticks: {
        color: 'rgb(156,163,175)',
        font: { size: 11 },
        callback: (v) => {
          if (v >= 1_000_000) return (v / 1_000_000).toFixed(1).replace(/\.0$/, '') + 'M'
          if (v >= 1_000) return Math.round(v / 1_000) + 'K'
          return String(v)
        },
      },
      grid: { color: 'rgba(255,255,255,0.05)' },
    },
  },
}))

// ── Orders table ──────────────────────────────────────────────────────
const paymentFilter = ref(null)

const paymentFilterOptions = computed(() => [
  { label: t('report.daily.ordersTable.filterAll'), value: null },
  { label: t('report.daily.ordersTable.filterCash'), value: 'cash' },
  { label: t('report.daily.ordersTable.filterTransfer'), value: 'transfer' },
])

const filteredOrders = computed(() => {
  let list = orders.value.filter(o => o.status !== 'cancelled' && o.paymentStatus !== 'unpaid')
  if (paymentFilter.value) list = list.filter(o => o.paymentMethod === paymentFilter.value)
  return list
})

const filteredTotal = computed(() => filteredOrders.value.reduce((s, o) => s + (o.totalAmount ?? 0), 0))

// ── Order detail drawer ───────────────────────────────────────────────
const drawerVisible = ref(false)
const drawerOrder = ref(null)
const openDrawer = (order) => { drawerOrder.value = order; drawerVisible.value = true }

// ── Status / payment badge helpers ────────────────────────────────────
const statusBadge = (status) => {
  const map = {
    completed:  { label: t('report.daily.ordersTable.status.completed'),  severity: 'success' },
    pending:    { label: t('report.daily.ordersTable.status.pending'),    severity: 'warn' },
    processing: { label: t('report.daily.ordersTable.status.processing'), severity: 'info' },
    cancelled: { label: t('report.daily.ordersTable.status.cancelled'), severity: 'danger' },
  }
  return map[status] ?? { label: status, severity: 'secondary' }
}

const paymentBadge = (method) => {
  const map = {
    cash:     { label: t('report.daily.ordersTable.payment.cash'),     severity: 'success' },
    transfer: { label: t('report.daily.ordersTable.payment.transfer'), severity: 'info' },
  }
  return map[method] ?? { label: method, severity: 'secondary' }
}

// ── Top products ──────────────────────────────────────────────────────
const maxProductRevenue = computed(() => {
  const products = reportData.value?.topProducts ?? []
  return products.length ? Math.max(...products.map(p => p.revenue)) : 1
})

// ── Category colors ───────────────────────────────────────────────────
const CATEGORY_COLORS = ['#F59E0B', '#14B8A6', '#60A5FA', '#A78BFA', '#F472B6', '#34D399']
const categoryColor = (idx) => CATEGORY_COLORS[idx % CATEGORY_COLORS.length]


// ── Export actions ────────────────────────────────────────────────────
const exportCsv = () => {
  const dateStr = toApiDate(selectedDate.value)
  const rows = filteredOrders.value

  const headers = [
    'ID',
    t('report.daily.ordersTable.colTime'),
    t('report.daily.ordersTable.colTable'),
    t('report.daily.ordersTable.colItems'),
    t('report.daily.ordersTable.colAmount'),
    t('report.daily.ordersTable.colPayment'),
    t('report.daily.ordersTable.colStatus'),
  ]

  const csvRows = rows.map(o => [
    o.orderId,
    fmtTime(o.createdAt),
    o.tableNumber != null ? t('report.daily.tableNum', { n: o.tableNumber }) : '',
    o.items.map(i => `${i.name} x${i.quantity}`).join('; '),
    o.totalAmount,
    paymentBadge(o.paymentMethod).label,
    statusBadge(o.status).label,
  ])

  const csvContent = [headers, ...csvRows]
    .map(row => row.map(cell => `"${String(cell ?? '').replace(/"/g, '""')}"`).join(','))
    .join('\n')

  const blob = new Blob(['\uFEFF' + csvContent], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `bao-cao-ngay-${dateStr}.csv`
  a.click()
  URL.revokeObjectURL(url)
}

const reportSection = ref(null)
const exporting = ref(false)

const exportPdf = async () => {
  if (!reportSection.value) return
  exporting.value = true
  try {
    const { toPng } = await import('html-to-image')
    const { jsPDF } = await import('jspdf')
    const el = reportSection.value
    const bg = getComputedStyle(document.documentElement).getPropertyValue('--app-bg').trim() || '#ffffff'
    const pad = 24
    const dataUrl = await toPng(el, {
      pixelRatio: 2,
      backgroundColor: bg,
      width: el.offsetWidth + pad * 2,
      height: el.offsetHeight + pad * 2,
      style: { padding: `${pad}px` },
    })
    const img = new Image()
    img.src = dataUrl
    await new Promise(r => { img.onload = r })
    const pw = img.naturalWidth / 2
    const ph = img.naturalHeight / 2
    const pdf = new jsPDF({
      orientation: pw > ph ? 'landscape' : 'portrait',
      unit: 'px',
      format: [pw, ph],
    })
    pdf.addImage(dataUrl, 'PNG', 0, 0, pw, ph)
    pdf.save(`bao-cao-ngay-${toApiDate(selectedDate.value)}.pdf`)
  } finally {
    exporting.value = false
  }
}

const exportItems = computed(() => [
  { label: t('report.daily.actions.exportCsv'), command: exportCsv },
  { label: t('report.daily.actions.exportPdf'), command: exportPdf },
])
</script>

<template>
  <section ref="reportSection" class="tw:space-y-6">

    <!-- ── Header ──────────────────────────────────────────────────── -->
    <div>
      <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] app-text-muted">
        {{ t('report.daily.breadcrumb') }}
      </p>
      <h1 class="tw:mt-1 tw:text-3xl tw:font-semibold">{{ t('report.daily.title') }}</h1>
      <p class="tw:mt-1 tw:text-sm app-text-muted">{{ t('report.daily.subtitle') }}</p>
    </div>

    <!-- Toolbar: date + actions -->
    <div class="tw:flex tw:flex-col tw:gap-2 tw:sm:flex-row tw:sm:flex-wrap tw:sm:items-center">
      <div class="tw:flex tw:items-center tw:flex-wrap tw:gap-2">
        <prime-date-picker
          v-model="selectedDate"
          date-format="dd/mm/yy"
          show-icon
          icon-display="input"
          size="small"
          class="tw:w-36"
        />
        <prime-button
          :label="t('report.daily.shortcuts.today')"
          severity="secondary"
          outlined
          size="small"
          @click="setToday"
        />
        <prime-button
          :label="t('report.daily.shortcuts.yesterday')"
          severity="secondary"
          outlined
          size="small"
          @click="setYesterday"
        />
      </div>
      <div class="tw:flex tw:items-center tw:gap-2 tw:sm:ml-auto">
        <prime-button
          :class="[btnIcon, 'tw:w-8! tw:h-8!']"
          size="small"
          severity="secondary"
          outlined
          :loading="loading"
          v-tooltip.top="t('report.daily.actions.refresh')"
          @click="load"
        >
          <iconify icon="ph:arrows-clockwise-bold" />
        </prime-button>
        <prime-split-button
          class="tw:h-8!"
          :label="t('report.daily.actions.export')"
          :model="exportItems"
          :loading="exporting"
          severity="secondary"
          outlined
          size="small"
        >
          <template #icon>
            <iconify icon="ph:export-bold" />
          </template>
        </prime-split-button>
      </div>
    </div>

    <!-- ── 4 Metric Cards ──────────────────────────────────────────── -->
    <div class="tw:grid tw:grid-cols-2 tw:md:grid-cols-4 tw:gap-4">

      <!-- Card 1: Tổng doanh thu -->
      <div :class="[appCard, 'tw:rounded-xl! tw:p-4!']">
        <template v-if="loading">
          <prime-skeleton height="1.5rem" class="tw:mb-3" />
          <prime-skeleton height="2rem" width="70%" class="tw:mb-2" />
          <prime-skeleton height="1rem" width="60%" class="tw:mb-1" />
          <prime-skeleton height="1rem" width="50%" />
        </template>
        <template v-else>
          <p class="tw:text-xs tw:font-medium app-text-muted tw:mb-2">
            {{ t('report.daily.metrics.revenue') }}
          </p>
          <p class="tw:text-2xl tw:font-bold tw:tabular-nums">
            {{ fmt(reportData?.revenue?.total) }}
          </p>
          <div class="tw:mt-2 tw:flex tw:items-center tw:gap-1.5 tw:text-xs">
            <template v-if="deltaPct(reportData?.revenue?.total, reportData?.revenue?.yesterday) !== null">
              <span
                :class="deltaPct(reportData?.revenue?.total, reportData?.revenue?.yesterday) >= 0
                  ? 'tw:text-green-600 tw:dark:text-green-400'
                  : 'tw:text-red-500 tw:dark:text-red-400'"
                class="tw:font-semibold"
              >
                {{ deltaPct(reportData?.revenue?.total, reportData?.revenue?.yesterday) >= 0 ? '▲' : '▼' }}
                {{ Math.abs(deltaPct(reportData?.revenue?.total, reportData?.revenue?.yesterday)).toFixed(1) }}%
              </span>
              <span class="app-text-muted">{{ t('report.daily.metrics.vsYesterday') }}</span>
            </template>
          </div>
          <p class="tw:mt-1 tw:text-xs app-text-muted">
            {{ t('report.daily.metrics.vsMonthAvg') }}:
            {{ fmtCompact(reportData?.revenue?.avg30Days) }}
          </p>
        </template>
      </div>

      <!-- Card 2: Đơn hàng -->
      <div :class="[appCard, 'tw:rounded-xl! tw:p-4!']">
        <template v-if="loading">
          <prime-skeleton height="1.5rem" class="tw:mb-3" />
          <prime-skeleton height="2rem" width="40%" class="tw:mb-2" />
          <prime-skeleton height="1rem" width="65%" class="tw:mb-1" />
          <prime-skeleton height="1rem" width="55%" />
        </template>
        <template v-else>
          <p class="tw:text-xs tw:font-medium app-text-muted tw:mb-2">
            {{ t('report.daily.metrics.orders') }}
          </p>
          <p class="tw:text-2xl tw:font-bold tw:tabular-nums">
            {{ reportData?.orders?.total ?? 0 }}
          </p>
          <div class="tw:mt-2 tw:flex tw:items-center tw:gap-1.5 tw:text-xs">
            <prime-tag
              :value="`${t('report.daily.metrics.completed')} ${reportData?.orders?.completed ?? 0}/${reportData?.orders?.total ?? 0}`"
              severity="success"
              class="tw:text-[11px]! tw:px-1.5! tw:py-0.5!"
            />
          </div>
          <p class="tw:mt-1 tw:text-xs app-text-muted">
            {{ t('report.daily.metrics.cancelled') }}: {{ reportData?.orders?.cancelled ?? 0 }}
            &nbsp;·&nbsp;
            {{ t('report.daily.metrics.pending') }}: {{ reportData?.orders?.pending ?? 0 }}
          </p>
        </template>
      </div>

      <!-- Card 3: TB / Đơn -->
      <div :class="[appCard, 'tw:rounded-xl! tw:p-4!']">
        <template v-if="loading">
          <prime-skeleton height="1.5rem" class="tw:mb-3" />
          <prime-skeleton height="2rem" width="70%" class="tw:mb-2" />
          <prime-skeleton height="1rem" width="50%" class="tw:mb-1" />
          <prime-skeleton height="1rem" width="55%" />
        </template>
        <template v-else>
          <p class="tw:text-xs tw:font-medium app-text-muted tw:mb-2">
            {{ t('report.daily.metrics.avgPerOrder') }}
          </p>
          <p class="tw:text-2xl tw:font-bold tw:tabular-nums">
            {{ fmt(reportData?.averages?.revenuePerOrder) }}
          </p>
          <div class="tw:mt-2 tw:flex tw:items-center tw:gap-1.5 tw:text-xs">
            <template v-if="deltaPct(reportData?.averages?.revenuePerOrder, reportData?.averages?.revenuePerOrderMonthAvg) !== null">
              <span
                :class="deltaPct(reportData?.averages?.revenuePerOrder, reportData?.averages?.revenuePerOrderMonthAvg) >= 0
                  ? 'tw:text-green-600 tw:dark:text-green-400'
                  : 'tw:text-red-500 tw:dark:text-red-400'"
                class="tw:font-semibold"
              >
                {{ deltaPct(reportData?.averages?.revenuePerOrder, reportData?.averages?.revenuePerOrderMonthAvg) >= 0 ? '▲' : '▼' }}
                {{ Math.abs(deltaPct(reportData?.averages?.revenuePerOrder, reportData?.averages?.revenuePerOrderMonthAvg)).toFixed(0) }}%
              </span>
              <span class="app-text-muted">{{ t('report.daily.metrics.vsMonthAvg') }}</span>
            </template>
          </div>
          <p class="tw:mt-1 tw:text-xs app-text-muted">
            {{ t('report.daily.metrics.vsMonthAvg') }}:
            {{ fmt(reportData?.averages?.revenuePerOrderMonthAvg) }}
          </p>
        </template>
      </div>

      <!-- Card 4: Ly / Đơn -->
      <div :class="[appCard, 'tw:rounded-xl! tw:p-4!']">
        <template v-if="loading">
          <prime-skeleton height="1.5rem" class="tw:mb-3" />
          <prime-skeleton height="2rem" width="30%" class="tw:mb-2" />
          <prime-skeleton height="1rem" width="50%" class="tw:mb-1" />
          <prime-skeleton height="1rem" width="55%" />
        </template>
        <template v-else>
          <p class="tw:text-xs tw:font-medium app-text-muted tw:mb-2">
            {{ t('report.daily.metrics.itemsPerOrder') }}
          </p>
          <p class="tw:text-2xl tw:font-bold tw:tabular-nums">
            {{ reportData?.averages?.itemsPerOrder?.toFixed(1) ?? '0.0' }}
          </p>
          <div class="tw:mt-2 tw:flex tw:items-center tw:gap-1.5 tw:text-xs">
            <prime-tag
              :value="`TB: ${reportData?.averages?.itemsPerOrderMonthAvg?.toFixed(1) ?? '0.0'}`"
              severity="secondary"
              class="tw:text-[11px]! tw:px-1.5! tw:py-0.5!"
            />
          </div>
          <p class="tw:mt-1 tw:text-xs app-text-muted">
            {{ t('report.daily.metrics.totalItems') }}:
            {{ orders.reduce((s, o) => s + (o.items?.length ?? 0), 0) }}
          </p>
        </template>
      </div>
    </div>

    <!-- ── Middle: Payment/Stats + Products/Categories ─────────────── -->
    <div class="tw:grid tw:grid-cols-1 tw:md:grid-cols-2 tw:gap-4">

      <!-- Left: Thanh toán & Thống kê -->
      <div :class="appCard" class="tw:rounded-xl tw:p-5 tw:space-y-5">
        <template v-if="loading">
          <prime-skeleton height="1rem" width="40%" class="tw:mb-4" />
          <prime-skeleton height="2.5rem" class="tw:mb-2" />
          <prime-skeleton height="2.5rem" class="tw:mb-4" />
          <prime-skeleton height="1px" class="tw:mb-3" />
          <prime-skeleton height="1rem" class="tw:mb-2" />
          <prime-skeleton height="1rem" class="tw:mb-2" />
          <prime-skeleton height="1rem" />
        </template>
        <template v-else>
          <!-- Payment section -->
          <div>
            <p class="tw:text-xs tw:font-semibold tw:uppercase tw:tracking-widest app-text-muted tw:mb-3">
              {{ t('report.daily.payment.title') }}
            </p>
            <!-- Cash row -->
            <div class="tw:flex tw:items-center tw:gap-3 tw:mb-3">
              <span class="tw:w-2 tw:h-2 tw:rounded-full tw:shrink-0" style="background: #14B8A6" />
              <span class="tw:text-sm tw:w-25 tw:shrink-0">{{ t('report.daily.payment.cash') }}</span>
              <prime-progress-bar
                :value="progressReady ? cashPct : 0"
                :show-value="false"
                class="pb-animate tw:flex-1 tw:h-1.5!"
                style="--pb-color: #14B8A6; --pb-delay: 0ms"
              />
              <span class="tw:text-xs tw:w-8 tw:text-right app-text-muted">{{ cashPct }}%</span>
              <span class="tw:text-sm tw:font-medium tw:w-15 tw:text-right tw:tabular-nums">
                {{ fmt(reportData?.revenue?.cash) }}
              </span>
            </div>
            <!-- Transfer row -->
            <div class="tw:flex tw:items-center tw:gap-3">
              <span class="tw:w-2 tw:h-2 tw:rounded-full tw:shrink-0" style="background: #60A5FA" />
              <span class="tw:text-sm tw:w-25 tw:shrink-0">{{ t('report.daily.payment.transfer') }}</span>
              <prime-progress-bar
                :value="progressReady ? transferPct : 0"
                :show-value="false"
                class="pb-animate tw:flex-1 tw:h-1.5!"
                style="--pb-color: #60A5FA; --pb-delay: 100ms"
              />
              <span class="tw:text-xs tw:w-8 tw:text-right app-text-muted">{{ transferPct }}%</span>
              <span class="tw:text-sm tw:font-medium tw:w-15 tw:text-right tw:tabular-nums">
                {{ fmt(reportData?.revenue?.transfer) }}
              </span>
            </div>
          </div>

          <!-- Stats section -->
          <div class="tw:border-t tw:border-slate-200 tw:dark:border-white/10 tw:pt-4 tw:space-y-2.5">
            <div class="tw:flex tw:justify-between tw:text-sm">
              <span class="app-text-muted">{{ t('report.daily.stats.peakHour') }}</span>
              <span v-if="reportData?.peakHour" class="tw:font-medium tw:text-right">
                {{ reportData.peakHour.hour }} ·
                {{ fmtCompact(reportData.peakHour.revenue) }} ·
                {{ reportData.peakHour.percentage }}% {{ t('report.daily.metrics.revenue').toLowerCase() }}
              </span>
              <span v-else class="app-text-muted">—</span>
            </div>
            <div class="tw:flex tw:justify-between tw:text-sm">
              <span class="app-text-muted">{{ t('report.daily.stats.estimatedGuests') }}</span>
              <span class="tw:font-medium">
                ~{{ orders.reduce((s, o) => s + (o.items?.reduce((a, i) => a + (i.quantity ?? 1), 0) ?? 0), 0) }}
              </span>
            </div>
          </div>

          <div class="tw:border-t tw:border-slate-200 tw:dark:border-white/10 tw:pt-4 tw:space-y-2.5">
            <div class="tw:flex tw:justify-between tw:text-sm">
              <span class="app-text-muted">{{ t('report.daily.stats.yesterday') }}</span>
              <span class="tw:font-medium tw:tabular-nums">{{ fmt(reportData?.revenue?.yesterday) }}</span>
            </div>
            <div class="tw:flex tw:justify-between tw:text-sm">
              <span class="app-text-muted">{{ t('report.daily.stats.avg7Days') }}</span>
              <span class="tw:font-medium tw:tabular-nums">{{ fmt(reportData?.revenue?.avg7Days) }}</span>
            </div>
            <div class="tw:flex tw:justify-between tw:text-sm">
              <span class="app-text-muted">{{ t('report.daily.stats.avg30Days') }}</span>
              <span class="tw:font-medium tw:tabular-nums">{{ fmt(reportData?.revenue?.avg30Days) }}</span>
            </div>
          </div>
        </template>
      </div>

      <!-- Right: Top Products & Categories -->
      <div :class="appCard" class="tw:rounded-xl tw:p-5 tw:space-y-5">
        <template v-if="loading">
          <prime-skeleton height="1rem" width="50%" class="tw:mb-4" />
          <prime-skeleton v-for="n in 5" :key="n" height="2rem" class="tw:mb-2" />
          <prime-skeleton height="1px" class="tw:my-4" />
          <prime-skeleton height="1rem" width="50%" class="tw:mb-3" />
          <prime-skeleton v-for="n in 3" :key="n + 10" height="1.5rem" class="tw:mb-2" />
        </template>
        <template v-else>
          <!-- Top products -->
          <div>
            <p class="tw:text-xs tw:font-semibold tw:uppercase tw:tracking-widest app-text-muted tw:mb-3">
              {{ t('report.daily.topProducts.title') }}
            </p>
            <div class="tw:space-y-3">
              <div
                v-for="(product, idx) in (reportData?.topProducts ?? [])"
                :key="product.productId"
                class="tw:space-y-1"
              >
                <div class="tw:flex tw:items-center tw:gap-2">
                  <span class="tw:text-xs tw:font-bold tw:w-4 tw:text-center app-text-muted tw:shrink-0">{{ idx + 1 }}</span>
                  <span class="tw:text-sm tw:w-24 tw:truncate tw:shrink-0">{{ product.name }}</span>
                  <prime-progress-bar
                    :value="progressReady ? Math.round((product.revenue / maxProductRevenue) * 100) : 0"
                    :show-value="false"
                    class="pb-animate tw:flex-1 tw:h-1!"
                    :style="{ '--pb-color': '#F59E0B', '--pb-delay': `${idx * 120}ms` }"
                  />
                  <span class="tw:text-xs app-text-muted tw:w-8 tw:text-right tw:shrink-0">{{ product.quantity }} ly</span>
                  <span class="tw:text-xs tw:font-medium tw:tabular-nums tw:w-10 tw:text-right tw:shrink-0">{{ fmtCompact(product.revenue) }}</span>
                </div>
              </div>
              <p v-if="!reportData?.topProducts?.length" class="tw:text-sm app-text-muted tw:text-center tw:py-2">
                —
              </p>
            </div>
          </div>

          <!-- Top categories -->
          <div class="tw:border-t tw:border-slate-200 tw:dark:border-white/10 tw:pt-4">
            <p class="tw:text-xs tw:font-semibold tw:uppercase tw:tracking-widest app-text-muted tw:mb-3">
              {{ t('report.daily.topCategories.title') }}
            </p>
            <div class="tw:space-y-2.5">
              <div
                v-for="(cat, idx) in (reportData?.topCategories ?? [])"
                :key="cat.categoryId"
                class="tw:flex tw:items-center tw:gap-2"
              >
                <span
                  class="tw:w-2 tw:h-2 tw:rounded-full tw:shrink-0"
                  :style="{ background: categoryColor(idx) }"
                />
                <span class="tw:text-sm tw:w-24 tw:truncate tw:shrink-0">{{ cat.name }}</span>
                <prime-progress-bar
                  :value="progressReady ? cat.percentage : 0"
                  :show-value="false"
                  class="pb-animate tw:flex-1 tw:h-1.5!"
                  :style="{ '--pb-color': categoryColor(idx), '--pb-delay': `${idx * 120}ms` }"
                />
                <span class="tw:text-xs app-text-muted tw:w-8 tw:text-right tw:shrink-0">{{ cat.percentage }}%</span>
                <span class="tw:text-xs tw:font-medium tw:tabular-nums tw:w-10 tw:text-right tw:shrink-0">{{ fmtCompact(cat.revenue) }}</span>
              </div>
              <p v-if="!reportData?.topCategories?.length" class="tw:text-sm app-text-muted tw:text-center tw:py-2">
                —
              </p>
            </div>
          </div>
        </template>
      </div>
    </div>

    <!-- ── Hourly Chart ─────────────────────────────────────────────── -->
    <div :class="appCard" class="tw:rounded-xl tw:p-5">
      <div class="tw:flex tw:flex-wrap tw:items-start tw:justify-between tw:gap-3 tw:mb-4">
        <div>
          <p class="tw:text-sm tw:font-semibold">{{ t('report.daily.hourlyChart.title') }}</p>
          <p class="tw:text-xs app-text-muted">{{ t('report.daily.hourlyChart.subtitle') }}</p>
        </div>
        <div class="tw:flex tw:items-center tw:gap-4">
          <prime-chip
            v-if="peakHour"
            :label="`${t('report.daily.hourlyChart.peakLabel')}: ${peakHour}`"
            class="tw:text-xs!"
          />
          <!-- Custom legend -->
          <div class="tw:flex tw:items-center tw:gap-3 tw:text-xs app-text-muted">
            <span class="tw:flex tw:items-center tw:gap-1.5">
              <span class="tw:inline-block tw:w-3 tw:h-3 tw:rounded-sm" style="background: #EF9F27" />
              {{ t('report.daily.hourlyChart.today') }}
            </span>
            <span class="tw:flex tw:items-center tw:gap-1.5">
              <span
                class="tw:inline-block tw:w-6 tw:border-b-2 tw:border-dashed"
                style="border-color: #888780"
              />
              {{ t('report.daily.hourlyChart.avg7Days') }}
            </span>
          </div>
        </div>
      </div>

      <template v-if="loading">
        <prime-skeleton height="220px" />
      </template>
      <div v-else style="height: 220px; position: relative">
        <prime-chart type="bar" :data="hourlyChartData" :options="hourlyChartOptions" class="tw:h-full" />
      </div>
    </div>

    <!-- ── Orders Table ────────────────────────────────────────────── -->
    <div :class="appCard" class="tw:rounded-xl tw:p-5">
      <div class="tw:flex tw:flex-wrap tw:items-center tw:justify-between tw:gap-3 tw:mb-4">
        <div class="tw:flex tw:items-center tw:gap-3">
          <p class="tw:text-sm tw:font-semibold">{{ t('report.daily.ordersTable.title') }}</p>
          <span class="tw:flex tw:items-center tw:gap-1 tw:text-[11px] app-text-muted">
            <span class="tw:w-2 tw:h-2 tw:rounded-full tw:bg-emerald-500 tw:shrink-0" />
            {{ t('report.daily.payment.cash') }}
          </span>
          <span class="tw:flex tw:items-center tw:gap-1 tw:text-[11px] app-text-muted">
            <span class="tw:w-2 tw:h-2 tw:rounded-full tw:bg-blue-400 tw:shrink-0" />
            {{ t('report.daily.payment.transfer') }}
          </span>
        </div>
        <!-- Payment filter -->
        <prime-select-button
          v-model="paymentFilter"
          :options="paymentFilterOptions"
          option-label="label"
          option-value="value"
          :allow-empty="false"
          size="small"
        />
      </div>

      <prime-data-table
        :value="filteredOrders"
        :loading="loading"
        sort-field="createdAt"
        :sort-order="1"
        size="small"
        class="tw:text-sm"
        :pt="{
          root: { class: 'tw:bg-transparent!' },
          header: { class: 'tw:bg-transparent!' },
          thead: { class: 'tw:bg-transparent!' },
          headerRow: { class: 'tw:bg-transparent!' },
          headerCell: { class: 'tw:bg-transparent!' },
          bodyRow: { class: 'tw:bg-transparent!' },
          footer: { class: 'tw:bg-transparent!' },
        }"
      >
        <prime-column
          field="createdAt"
          :header="t('report.daily.ordersTable.colTime')"
          sortable
          style="width: 100px"
        >
          <template #body="{ data }">
            {{ fmtTime(data.createdAt) }}
          </template>
        </prime-column>

        <prime-column
          field="tableNumber"
          :header="t('report.daily.ordersTable.colTable')"
          sortable
          style="width: 60px"
          :pt="{ headerCell: { class: 'tw:hidden tw:sm:table-cell!' }, bodyCell: { class: 'tw:hidden tw:sm:table-cell!' } }"
        >
          <template #body="{ data }">
            {{ t('report.daily.tableNum', { n: data.tableNumber }) }}
          </template>
        </prime-column>

        <prime-column
          field="items"
          :header="t('report.daily.ordersTable.colItems')"
          :pt="{ headerCell: { class: 'tw:hidden tw:sm:table-cell!' }, bodyCell: { class: 'tw:hidden tw:sm:table-cell!' } }"
        >
          <template #body="{ data }">
            <div class="tw:space-y-0.5">
              <p
                v-for="(item, i) in data.items"
                :key="i"
                class="tw:text-xs tw:leading-snug"
              >
                {{ item.name }} x{{ item.quantity }}
              </p>
            </div>
          </template>
        </prime-column>

        <prime-column
          field="totalAmount"
          :header="t('report.daily.ordersTable.colAmount')"
          sortable
          style="width: 90px"
          header-class="tw:text-right!"
        >
          <template #body="{ data }">
            <p
              class="tw:text-right tw:tabular-nums tw:font-medium"
              :class="{
                'tw:text-emerald-500': data.paymentMethod === 'cash',
                'tw:text-blue-400': data.paymentMethod === 'transfer',
              }"
            >{{ fmt(data.totalAmount) }}</p>
          </template>
        </prime-column>

        <prime-column
          field="status"
          :header="t('report.daily.ordersTable.colStatus')"
          style="width: 90px"
        >
          <template #body="{ data }">
            <prime-tag
              :value="statusBadge(data.status).label"
              :severity="statusBadge(data.status).severity"
              class="tw:text-[11px]! tw:px-1.5! tw:py-0.5!"
            />
          </template>
        </prime-column>

        <prime-column
          style="width: 40px"
          :pt="{ headerCell: { class: 'tw:sm:hidden!' }, bodyCell: { class: 'tw:sm:hidden!' } }"
        >
          <template #body="{ data }">
            <prime-button
              :class="btnIcon"
              size="small"
              severity="secondary"
              text
              @click="openDrawer(data)"
            >
              <iconify icon="ph:eye-bold" />
            </prime-button>
          </template>
        </prime-column>

        <template #empty>
          <p class="tw:text-center tw:py-6 app-text-muted tw:text-sm">
            {{ t('report.daily.ordersTable.noData') }}
          </p>
        </template>

        <template #footer>
          <div class="tw:flex tw:justify-between tw:items-center tw:text-xs app-text-muted tw:pt-1">
            <span>
              {{ t('report.daily.ordersTable.showing', { shown: filteredOrders.length, total: orders.filter(o => o.status !== 'cancelled' && o.paymentStatus !== 'unpaid').length }) }}
            </span>
            <span class="tw:font-semibold tw:text-sm tw:tabular-nums">
              {{ t('report.daily.ordersTable.total') }}: {{ fmt(filteredTotal) }}
            </span>
          </div>
        </template>
      </prime-data-table>
    </div>

  </section>

  <!-- Order detail drawer -->
  <prime-drawer
    v-model:visible="drawerVisible"
    position="bottom"
    :style="{ height: 'auto' }"
    :pt="{ root: { class: 'tw:rounded-t-2xl' } }"
  >
    <template #header>
      <div class="tw:flex tw:items-center tw:gap-3">
        <span class="tw:font-semibold">{{ t('report.daily.ordersTable.drawerTitle') }}</span>
        <prime-tag
          v-if="drawerOrder"
          :value="statusBadge(drawerOrder.status).label"
          :severity="statusBadge(drawerOrder.status).severity"
          class="tw:text-[11px]! tw:px-1.5! tw:py-0.5!"
        />
      </div>
    </template>
    <div v-if="drawerOrder" class="tw:space-y-4 tw:pb-4">
      <!-- Meta -->
      <div class="tw:grid tw:grid-cols-2 tw:gap-2 tw:text-sm">
        <div>
          <p class="tw:text-xs app-text-muted tw:mb-0.5">{{ t('report.daily.ordersTable.colTime') }}</p>
          <p class="tw:font-medium">{{ fmtTime(drawerOrder.createdAt) }}</p>
        </div>
        <div>
          <p class="tw:text-xs app-text-muted tw:mb-0.5">{{ t('report.daily.ordersTable.colTable') }}</p>
          <p class="tw:font-medium">{{ t('report.daily.tableNum', { n: drawerOrder.tableNumber }) }}</p>
        </div>
        <div>
          <p class="tw:text-xs app-text-muted tw:mb-0.5">{{ t('report.daily.ordersTable.colPayment') }}</p>
          <prime-tag
            :value="paymentBadge(drawerOrder.paymentMethod).label"
            :severity="paymentBadge(drawerOrder.paymentMethod).severity"
            class="tw:text-[11px]! tw:px-1.5! tw:py-0.5!"
          />
        </div>
        <div>
          <p class="tw:text-xs app-text-muted tw:mb-0.5">{{ t('report.daily.ordersTable.colAmount') }}</p>
          <p
            class="tw:font-semibold tw:text-base tw:tabular-nums"
            :class="{
              'tw:text-emerald-500': drawerOrder.paymentMethod === 'cash',
              'tw:text-blue-400': drawerOrder.paymentMethod === 'transfer',
            }"
          >{{ fmt(drawerOrder.totalAmount) }}</p>
        </div>
      </div>
      <!-- Items -->
      <div class="tw:border-t tw:border-slate-200 tw:dark:border-white/10 tw:pt-3">
        <p class="tw:text-xs app-text-muted tw:mb-2">{{ t('report.daily.ordersTable.colItems') }}</p>
        <div class="tw:space-y-1.5">
          <div
            v-for="(item, i) in drawerOrder.items"
            :key="i"
            class="tw:flex tw:justify-between tw:text-sm"
          >
            <span>{{ item.name }}</span>
            <span class="app-text-muted">x{{ item.quantity }}</span>
          </div>
        </div>
      </div>
    </div>
  </prime-drawer>
</template>

<style scoped>
.pb-animate :deep(.p-progressbar-value) {
  background: var(--pb-color, #F59E0B) !important;
  transition: width 0.8s cubic-bezier(0.4, 0, 0.2, 1) var(--pb-delay, 0ms) !important;
}

:deep(.p-datatable-header-cell) {
  background: transparent !important;
}
</style>
