<script setup>
const router = useRouter();
const { t, locale } = useI18n()

// ── Dynamic date subtitle ──────────────────────────────────────────
const todayLabel = computed(() => {
  return new Intl.DateTimeFormat(
    locale.value === 'vi' ? 'vi-VN' : 'en-US',
    { weekday: 'long', day: '2-digit', month: '2-digit', year: 'numeric' }
  ).format(new Date())
})

// ── Fake data ─────────────────────────────────────────────────────

const kpi = {
  revenue: { total: 4_250_000, cash: 2_400_000, bank: 1_850_000, yesterday: 5_100_000 },
  orders:  { total: 34, pending: 8, processing: 5, completed: 21, cancelled: 0, avgOrder: 125_000, takeaway: 10, dineIn: 24 },
  tables:  { total: 12, available: 5, occupied: 6, ordering: 1 },
  profit:  { amount: 2_820_000, margin: 66.4 },
};

const revenueTrend = [
  { label: "04/03", revenue: 3_800_000, expenses: 1_200_000 },
  { label: "05/03", revenue: 4_100_000, expenses:   980_000 },
  { label: "06/03", revenue: 3_500_000, expenses: 1_100_000 },
  { label: "07/03", revenue: 4_700_000, expenses: 1_350_000 },
  { label: "08/03", revenue: 3_900_000, expenses: 1_050_000 },
  { label: "09/03", revenue: 5_100_000, expenses: 1_420_000 },
  { label: "10/03", revenue: 4_250_000, expenses: 1_430_000 },
];

// Sorted by revenue — order differs from qty rank intentionally
const revenueDrivers = [
  { name: 'Bạc xỉu',       qty: 28, rev: 1_260_000, qtyRank: 1 },
  { name: 'Matcha latte',   qty: 18, rev:   990_000, qtyRank: 3 },  // upsell
  { name: 'Cà phê sữa đá', qty: 22, rev:   880_000, qtyRank: 2 },
  { name: 'Cold brew',      qty: 15, rev:   825_000, qtyRank: 4 },
  { name: 'Trà đào',        qty: 12, rev:   540_000, qtyRank: 5 },
]
const maxDriverRev = Math.max(...revenueDrivers.map(d => d.rev))

const topCategories = [
  { name: 'Cafe',   pct: 45, hex: '#fbbf24' },  // amber-400
  { name: 'Trà',    pct: 30, hex: '#34d399' },  // emerald-400
  { name: 'Đồ ăn', pct: 15, hex: '#60a5fa' },  // blue-400
  { name: 'Khác',  pct: 10, hex: '#c084fc' },  // purple-400
]

const paymentRatio = computed(() => {
  const total = kpi.revenue.cash + kpi.revenue.bank
  return {
    cash: Math.round((kpi.revenue.cash / total) * 100),
    bank: 100 - Math.round((kpi.revenue.cash / total) * 100),
  }
})

const ordersByHour = [
  { h: "7h", n: 3 }, { h: "8h", n: 7 }, { h: "9h", n: 11 },
  { h: "10h", n: 9 }, { h: "11h", n: 14 }, { h: "12h", n: 18 },
  { h: "13h", n: 15 }, { h: "14h", n: 10 }, { h: "15h", n: 7 },
  { h: "16h", n: 11 }, { h: "17h", n: 12 }, { h: "18h", n: 8 },
  { h: "19h", n: 5 },  { h: "20h", n: 2 },
];
const maxOrdersHour = Math.max(...ordersByHour.map((d) => d.n));

const revenueByHour = [
  { h: "7h",  rev:   520_000 }, { h: "8h",  rev:   680_000 },
  { h: "9h",  rev:   780_000 }, { h: "10h", rev:   650_000 },
  { h: "11h", rev:   920_000 }, { h: "12h", rev: 1_300_000 },
  { h: "13h", rev: 1_100_000 }, { h: "14h", rev:   420_000 },
  { h: "15h", rev:   280_000 }, { h: "16h", rev:   450_000 },
  { h: "17h", rev:   750_000 }, { h: "18h", rev:   950_000 },
  { h: "19h", rev:   600_000 }, { h: "20h", rev:   150_000 },
];
const maxRevenueHour = Math.max(...revenueByHour.map((d) => d.rev));
const peakHour = revenueByHour.find((d) => d.rev === maxRevenueHour);

// emerald-400 = rgb(52,211,153)
function heatColor(pct) {
  if (pct < 0.2) return "rgba(52,211,153,0.08)";
  if (pct < 0.4) return "rgba(52,211,153,0.28)";
  if (pct < 0.6) return "rgba(52,211,153,0.48)";
  if (pct < 0.8) return "rgba(52,211,153,0.68)";
  return "rgba(52,211,153,0.90)";
}
const heatLegend = [
  "rgba(52,211,153,0.08)",
  "rgba(52,211,153,0.28)",
  "rgba(52,211,153,0.48)",
  "rgba(52,211,153,0.68)",
  "rgba(52,211,153,0.90)",
];

const tables = [
  { id: 1,  number: 1,  status: "OCCUPIED"  },
  { id: 2,  number: 2,  status: "OCCUPIED"  },
  { id: 3,  number: 3,  status: "AVAILABLE" },
  { id: 4,  number: 4,  status: "CLEANING"  },
  { id: 5,  number: 5,  status: "OCCUPIED"  },
  { id: 6,  number: 6,  status: "AVAILABLE" },
  { id: 7,  number: 7,  status: "OCCUPIED"  },
  { id: 8,  number: 8,  status: "AVAILABLE" },
  { id: 9,  number: 9,  status: "OCCUPIED"  },
  { id: 10, number: 10, status: "AVAILABLE" },
  { id: 11, number: 11, status: "OCCUPIED"  },
  { id: 12, number: 12, status: "AVAILABLE" },
];

const recentOrders = [
  { id: 1, number: "ORD-034", table: "Bàn 5",  amount: 185_000, status: "PROCESSING" },
  { id: 2, number: "ORD-033", table: "Bàn 9",  amount: 240_000, status: "PROCESSING" },
  { id: 3, number: "ORD-032", table: "Bàn 2",  amount:  92_000, status: "PENDING"    },
  { id: 4, number: "ORD-031", table: "Bàn 7",  amount: 310_000, status: "COMPLETED"  },
  { id: 5, number: "ORD-030", table: "Bàn 11", amount: 155_000, status: "PENDING"    },
];

// ── Helpers ───────────────────────────────────────────────────────
const fmt = (v) =>
  new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND", maximumFractionDigits: 0 }).format(v ?? 0);

// delta so với hôm qua — dương = tăng, âm = giảm
const revenueDelta = computed(() => {
  const diff = kpi.revenue.total - kpi.revenue.yesterday;
  const pct  = ((diff / kpi.revenue.yesterday) * 100).toFixed(1);
  return { diff, pct: Number(pct), up: diff >= 0 };
});

const fmtK = (v) => {
  if (v >= 1_000_000) return (v / 1_000_000).toFixed(1) + "M";
  if (v >= 1_000)     return (v / 1_000).toFixed(0) + "K";
  return String(v);
};

const tablesOccupancy = computed(() =>
  Math.round((kpi.tables.occupied / kpi.tables.total) * 100)
)

const tableStatusMeta = computed(() => ({
  AVAILABLE: { label: t('dashboard.tableStatus.available'), color: "tw:bg-primary-500/80", ring: "tw:ring-emerald-500/30", dot: "tw:bg-primary-500", icon: "ph:coffee-bold"  },
  OCCUPIED:  { label: t('dashboard.tableStatus.occupied'),  color: "tw:bg-blue-500/80",    ring: "tw:ring-blue-500/30",    dot: "tw:bg-blue-500",    icon: "ph:user-bold"    },
  CLEANING:  { label: t('dashboard.tableStatus.ordering'),  color: "tw:bg-amber-400/80",   ring: "tw:ring-amber-400/30",   dot: "tw:bg-amber-400",   icon: "ph:receipt-bold" },
}))

const orderStatusMeta = computed(() => ({
  PENDING:    { label: t('orders.status.PENDING'),    severity: "warn"    },
  PROCESSING: { label: t('orders.status.PROCESSING'), severity: "info"    },
  COMPLETED:  { label: t('orders.status.COMPLETED'),  severity: "success" },
  CANCELLED:  { label: t('orders.status.CANCELLED'),  severity: "danger"  },
}))

// ── SVG Revenue Chart ─────────────────────────────────────────────
// viewBox 560 × 120, padding left 0, right 0
const SVG_W = 560;
const SVG_H = 100;
const SVG_PAD = 8;

function buildPath(dataKey) {
  const values = revenueTrend.map((d) => d[dataKey]);
  const maxV   = Math.max(...revenueTrend.map((d) => Math.max(d.revenue, d.expenses)));
  const n      = values.length;
  const pts    = values.map((v, i) => {
    const x = SVG_PAD + (i / (n - 1)) * (SVG_W - SVG_PAD * 2);
    const y = SVG_PAD + (1 - v / maxV) * (SVG_H - SVG_PAD * 2);
    return [x, y];
  });
  return "M " + pts.map(([x, y]) => `${x.toFixed(1)},${y.toFixed(1)}`).join(" L ");
}

function buildArea(dataKey) {
  const values = revenueTrend.map((d) => d[dataKey]);
  const maxV   = Math.max(...revenueTrend.map((d) => Math.max(d.revenue, d.expenses)));
  const n      = values.length;
  const pts    = values.map((v, i) => {
    const x = SVG_PAD + (i / (n - 1)) * (SVG_W - SVG_PAD * 2);
    const y = SVG_PAD + (1 - v / maxV) * (SVG_H - SVG_PAD * 2);
    return [x, y];
  });
  const first = pts[0];
  const last  = pts[pts.length - 1];
  return (
    "M " + pts.map(([x, y]) => `${x.toFixed(1)},${y.toFixed(1)}`).join(" L ") +
    ` L ${last[0].toFixed(1)},${(SVG_H + 2).toFixed(1)} L ${first[0].toFixed(1)},${(SVG_H + 2).toFixed(1)} Z`
  );
}

const revenuePath  = buildPath("revenue");
const expensePath  = buildPath("expenses");
const revenueArea  = buildArea("revenue");
const expensesArea = buildArea("expenses");
</script>

<template>
  <section class="tw:space-y-6">

    <!-- ── Header ──────────────────────────────────────────────────── -->
    <page-header>
      <template #subtitle>
        <p class="tw:text-sm tw:text-muted">{{ todayLabel }} &mdash; {{ t('dashboard.snapshotToday') }}</p>
      </template>
      <prime-button severity="secondary" outlined size="small" @click="router.push({ name: 'orders' })">
        <iconify icon="ph:receipt-bold" class="tw:mr-1" />
        <span>{{ t('dashboard.viewOrders') }}</span>
      </prime-button>
      <prime-button severity="primary" size="small" @click="router.push({ name: 'ordersCreate' })">
        <iconify icon="ph:plus-bold" class="tw:mr-1" />
        <span>{{ t('dashboard.newOrder') }}</span>
      </prime-button>
    </page-header>

    <!-- ── KPI Row ──────────────────────────────────────────────────── -->
    <div class="tw:grid tw:grid-cols-2 tw:gap-4 tw:lg:grid-cols-4">

      <!-- Revenue -->
      <prime-card class="app-card tw:rounded-2xl tw:border">
        <template #content>
          <div class="tw:flex tw:items-start tw:justify-between">
            <p class="tw:text-[11px] tw:uppercase tw:tracking-widest tw:text-muted">{{ t('dashboard.kpi.revenue') }}</p>
            <iconify icon="ph:trend-up-bold" class="tw:text-primary-400 tw:opacity-60 tw:text-lg" />
          </div>
          <p class="tw:mt-3 tw:text-2xl tw:font-bold tw:tracking-tight">{{ fmt(kpi.revenue.total) }}</p>
          <div class="tw:mt-2 tw:flex tw:items-center tw:gap-2">
            <span
              class="tw:inline-flex tw:items-center tw:gap-0.5 tw:text-xs tw:font-semibold tw:px-1.5 tw:py-0.5 tw:rounded-md"
              :class="revenueDelta.up
                ? 'tw:bg-primary-500/15 tw:text-primary-400'
                : 'tw:bg-red-500/15 tw:text-red-400'"
            >
              <iconify :icon="revenueDelta.up ? 'ph:trend-up-bold' : 'ph:trend-down-bold'" />
              {{ revenueDelta.up ? '+' : '' }}{{ revenueDelta.pct }}%
            </span>
            <span class="tw:text-xs app-text-subtle">{{ t('dashboard.kpi.vsYesterday') }}</span>
          </div>
          <div class="tw:mt-2 tw:flex tw:gap-x-3 tw:text-xs tw:text-muted tw:flex-wrap">
            <span>{{ t('dashboard.kpi.cash') }} <span class="tw:font-medium">{{ fmtK(kpi.revenue.cash) }}₫</span></span>
            <span>{{ t('dashboard.kpi.bank') }} <span class="tw:font-medium">{{ fmtK(kpi.revenue.bank) }}₫</span></span>
          </div>
        </template>
      </prime-card>

      <!-- Orders -->
      <prime-card class="app-card tw:rounded-2xl tw:border">
        <template #content>
          <div class="tw:flex tw:items-start tw:justify-between">
            <p class="tw:text-[11px] tw:uppercase tw:tracking-widest tw:text-muted">{{ t('dashboard.kpi.orders') }}</p>
            <iconify icon="ph:receipt-bold" class="tw:text-blue-400 tw:opacity-60 tw:text-lg" />
          </div>
          <p class="tw:mt-3 tw:text-2xl tw:font-bold tw:tracking-tight">{{ kpi.orders.total }}</p>
          <div class="tw:mt-3 tw:flex tw:gap-x-2 tw:flex-wrap">
            <prime-tag v-if="kpi.orders.pending"    severity="warn"    class="tw:text-[10px]! tw:inline-flex tw:items-center tw:gap-1">
              <iconify icon="ph:hourglass-bold" class="tw:text-[11px]" /> {{ t('orders.status.PENDING') }} {{ kpi.orders.pending }}
            </prime-tag>
            <prime-tag v-if="kpi.orders.processing" severity="info"    class="tw:text-[10px]! tw:inline-flex tw:items-center tw:gap-1">
              <iconify icon="ph:arrows-clockwise-bold" class="tw:text-[11px]" /> {{ t('orders.status.PROCESSING') }} {{ kpi.orders.processing }}
            </prime-tag>
            <prime-tag v-if="kpi.orders.completed"  severity="success" class="tw:text-[10px]! tw:inline-flex tw:items-center tw:gap-1">
              <iconify icon="ph:check-circle-bold" class="tw:text-[11px]" /> {{ t('orders.status.COMPLETED') }} {{ kpi.orders.completed }}
            </prime-tag>
          </div>
          <div class="tw:mt-2 tw:flex tw:gap-x-3 tw:text-xs tw:text-muted tw:flex-wrap">
            <span class="tw:inline-flex tw:items-center tw:gap-1">
              <iconify icon="ph:calculator-bold" />
              {{ t('dashboard.kpi.avg') }} <span class="tw:font-medium">{{ fmtK(kpi.orders.avgOrder) }}₫</span>
            </span>
            <span class="tw:inline-flex tw:items-center tw:gap-1">
              <iconify icon="ph:bag-bold" />
              {{ t('orders.serving.takeaway') }} <span class="tw:font-medium">{{ kpi.orders.takeaway }}</span>
            </span>
            <span class="tw:inline-flex tw:items-center tw:gap-1">
              <iconify icon="ph:fork-knife-bold" />
              {{ t('orders.serving.dineIn') }} <span class="tw:font-medium">{{ kpi.orders.dineIn }}</span>
            </span>
          </div>
        </template>
      </prime-card>

      <!-- Tables -->
      <prime-card class="app-card tw:rounded-2xl tw:border">
        <template #content>
          <div class="tw:flex tw:items-start tw:justify-between">
            <p class="tw:text-[11px] tw:uppercase tw:tracking-widest tw:text-muted">{{ t('dashboard.kpi.tables') }}</p>
            <iconify icon="ph:table-bold" class="tw:text-purple-400 tw:opacity-60 tw:text-lg" />
          </div>
          <!-- Occupancy rate -->
          <div class="tw:mt-3 tw:flex tw:items-end tw:justify-between">
            <p class="tw:text-2xl tw:font-bold tw:tracking-tight">{{ tablesOccupancy }}%</p>
            <p class="tw:text-xs tw:text-muted tw:mb-0.5">{{ t('dashboard.kpi.tablesCount', { n: kpi.tables.total }) }}</p>
          </div>
          <!-- Progress bar -->
          <div class="tw:mt-2 tw:h-1.5 tw:rounded-full tw:bg-white/10 tw:overflow-hidden">
            <div
              class="tw:h-full tw:rounded-full tw:bg-purple-400 tw:transition-all"
              :style="{ width: tablesOccupancy + '%' }"
            />
          </div>
          <p class="tw:mt-1 tw:text-[10px] app-text-subtle">{{ t('dashboard.kpi.occupancyRate') }}</p>
          <!-- Breakdown -->
          <div class="tw:mt-3 tw:flex tw:gap-x-3 tw:text-xs tw:text-muted">
            <span class="tw:inline-flex tw:items-center tw:gap-1">
              <iconify icon="ph:user-bold" class="tw:text-blue-400" />
              {{ t('dashboard.kpi.occupied') }} <span class="tw:font-medium">{{ kpi.tables.occupied }}</span>
            </span>
            <span class="tw:inline-flex tw:items-center tw:gap-1">
              <iconify icon="ph:check-bold" class="tw:text-primary-400" />
              {{ t('dashboard.kpi.free') }} <span class="tw:font-medium">{{ kpi.tables.available }}</span>
            </span>
            <span class="tw:inline-flex tw:items-center tw:gap-1">
              <iconify icon="ph:clock-bold" class="tw:text-amber-400" />
              {{ t('dashboard.kpi.ordering') }} <span class="tw:font-medium">{{ kpi.tables.ordering }}</span>
            </span>
          </div>
        </template>
      </prime-card>

      <!-- Est. Profit -->
      <prime-card class="app-card tw:rounded-2xl tw:border">
        <template #content>
          <div class="tw:flex tw:items-start tw:justify-between">
            <div class="tw:flex tw:items-center tw:gap-1.5">
              <p class="tw:text-[11px] tw:uppercase tw:tracking-widest tw:text-muted">{{ t('dashboard.kpi.profit') }}</p>
              <span
                v-tooltip.bottom="t('dashboard.kpi.profitTooltip')"
                class="tw:inline-flex tw:items-center tw:justify-center tw:size-3.5 tw:rounded-full tw:bg-white/10 tw:cursor-help"
              >
                <iconify icon="ph:question-bold" class="tw:text-[8px] tw:text-muted" />
              </span>
            </div>
            <iconify icon="ph:chart-bar-bold" class="tw:text-primary-400 tw:opacity-60 tw:text-lg" />
          </div>
          <p class="tw:mt-3 tw:text-2xl tw:font-bold tw:tracking-tight tw:text-primary-400">
            {{ fmt(kpi.profit.amount) }}
          </p>
          <div class="tw:mt-2 tw:text-xs tw:text-muted">
            {{ t('dashboard.kpi.margin') }}
            <span class="tw:text-primary-400 tw:font-semibold tw:ml-1">{{ kpi.profit.margin }}%</span>
          </div>
          <p class="tw:mt-2 tw:text-[10px] app-text-subtle tw:italic">{{ t('dashboard.kpi.profitNote') }}</p>
        </template>
      </prime-card>
    </div>

    <!-- ── Charts Row ───────────────────────────────────────────────── -->
    <div class="tw:grid tw:grid-cols-1 tw:gap-4 tw:lg:grid-cols-3">

      <!-- Revenue trend (1/3) -->
      <prime-card class="app-card tw:rounded-2xl tw:border">
        <template #content>
          <div class="tw:flex tw:items-center tw:justify-between tw:mb-4">
            <div>
              <p class="tw:text-sm tw:font-semibold">{{ t('dashboard.charts.revExpenses') }}</p>
              <p class="tw:text-xs tw:text-muted">{{ t('dashboard.charts.last7days') }}</p>
            </div>
            <div class="tw:flex tw:items-center tw:gap-4 tw:text-xs tw:text-muted">
              <span class="tw:flex tw:items-center tw:gap-1.5">
                <span class="tw:inline-block tw:size-2 tw:rounded-full tw:bg-primary-400"></span>
                {{ t('dashboard.charts.revenue') }}
              </span>
              <span class="tw:flex tw:items-center tw:gap-1.5">
                <span class="tw:inline-block tw:size-2 tw:rounded-full tw:bg-red-400"></span>
                {{ t('dashboard.charts.expenses') }}
              </span>
            </div>
          </div>

          <!-- SVG Chart -->
          <svg
            :viewBox="`0 0 ${SVG_W} ${SVG_H}`"
            class="tw:w-full tw:overflow-visible"
            :height="SVG_H"
            preserveAspectRatio="none"
          >
            <defs>
              <linearGradient id="grad-revenue" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%"   stop-color="#34d399" stop-opacity="0.25" />
                <stop offset="100%" stop-color="#34d399" stop-opacity="0.02" />
              </linearGradient>
              <linearGradient id="grad-expenses" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%"   stop-color="#f87171" stop-opacity="0.18" />
                <stop offset="100%" stop-color="#f87171" stop-opacity="0.02" />
              </linearGradient>
            </defs>

            <!-- Area fills -->
            <path :d="revenueArea"  fill="url(#grad-revenue)"  />
            <path :d="expensesArea" fill="url(#grad-expenses)" />

            <!-- Lines -->
            <path :d="revenuePath"  fill="none" stroke="#34d399" stroke-width="2" stroke-linejoin="round" />
            <path :d="expensePath"  fill="none" stroke="#f87171" stroke-width="1.5" stroke-linejoin="round" stroke-dasharray="4 3" />

            <!-- Data points (revenue) -->
            <circle
              v-for="(d, i) in revenueTrend"
              :key="i"
              :cx="SVG_PAD + (i / (revenueTrend.length - 1)) * (SVG_W - SVG_PAD * 2)"
              :cy="SVG_PAD + (1 - d.revenue / Math.max(...revenueTrend.map(x => Math.max(x.revenue, x.expenses)))) * (SVG_H - SVG_PAD * 2)"
              r="3"
              fill="#34d399"
              stroke="#1a1a2e"
              stroke-width="1.5"
            />
          </svg>

          <!-- X-axis labels -->
          <div class="tw:flex tw:justify-between tw:mt-2 tw:px-1">
            <span
              v-for="d in revenueTrend"
              :key="d.label"
              class="tw:text-[10px] app-text-subtle"
            >{{ d.label }}</span>
          </div>
        </template>
      </prime-card>

      <!-- Orders by hour (1/3) -->
      <prime-card class="app-card tw:rounded-2xl tw:border">
        <template #content>
          <div class="tw:mb-4">
            <p class="tw:text-sm tw:font-semibold">{{ t('dashboard.charts.ordersByHour') }}</p>
            <p class="tw:text-xs tw:text-muted">{{ t('dashboard.charts.todayPeakOrders', { h: '12h', n: maxOrdersHour }) }}</p>
          </div>

          <div class="tw:flex tw:items-end tw:gap-1 tw:h-24">
            <div
              v-for="h in ordersByHour"
              :key="h.h"
              class="tw:flex tw:flex-col tw:items-center tw:gap-1 tw:flex-1 tw:min-w-0"
            >
              <div
                class="tw:w-full tw:rounded-sm tw:transition-all"
                :class="h.n === maxOrdersHour ? 'tw:bg-primary-400' : 'tw:bg-white/15'"
                :style="{ height: `${Math.max(4, (h.n / maxOrdersHour) * 80)}px` }"
                v-tooltip.top="h.n + ' đơn'"
              />
            </div>
          </div>

          <div class="tw:flex tw:gap-1 tw:mt-2">
            <div
              v-for="h in ordersByHour"
              :key="h.h"
              class="tw:flex-1 tw:min-w-0 tw:text-center"
            >
              <span class="tw:text-[8px] app-text-subtle tw:leading-none tw:block tw:truncate">
                {{ h.h }}
              </span>
            </div>
          </div>
        </template>
      </prime-card>

      <!-- Peak Revenue Hours — Heatmap (1/3) -->
      <prime-card class="app-card tw:rounded-2xl tw:border">
        <template #content>
          <div class="tw:mb-3">
            <p class="tw:text-sm tw:font-semibold tw:flex tw:items-center tw:gap-1.5">
              <iconify icon="ph:fire-bold" class="tw:text-primary-400" />
              {{ t('dashboard.charts.heatmap') }}
            </p>
            <p class="tw:text-xs tw:text-muted">
              {{ t('dashboard.charts.todayPeakRev', { h: peakHour.h, rev: fmtK(peakHour.rev) + '₫' }) }}
            </p>
          </div>

          <!-- Heatmap grid: 2 rows × 7 cols -->
          <div class="tw:grid tw:grid-cols-7 tw:gap-1">
            <div
              v-for="h in revenueByHour"
              :key="h.h"
              class="tw:rounded-lg tw:py-1.5 tw:flex tw:flex-col tw:items-center tw:gap-0.5 tw:cursor-default tw:transition-transform tw:hover:scale-105"
              :style="{ backgroundColor: heatColor(h.rev / maxRevenueHour) }"
              v-tooltip.top="`${h.h}: ${fmtK(h.rev)}₫`"
            >
              <span class="tw:text-[9px] tw:font-bold tw:text-white/80 tw:leading-none">{{ h.h }}</span>
              <span class="tw:text-[8px] tw:text-white/55 tw:leading-none tw:mt-0.5">{{ fmtK(h.rev) }}</span>
            </div>
          </div>

          <!-- Intensity legend -->
          <div class="tw:mt-2.5 tw:flex tw:items-center tw:gap-1.5 tw:text-[10px] app-text-subtle">
            <span>{{ t('dashboard.charts.low') }}</span>
            <div class="tw:flex tw:gap-0.5 tw:flex-1">
              <div
                v-for="c in heatLegend" :key="c"
                class="tw:h-2 tw:flex-1 tw:rounded-sm"
                :style="{ backgroundColor: c }"
              />
            </div>
            <span>{{ t('dashboard.charts.high') }}</span>
          </div>

          <!-- Peak insight -->
          <div class="tw:mt-3 tw:pt-3 tw:border-t tw:border-white/10">
            <p class="tw:text-xs tw:text-primary-400 tw:font-semibold tw:flex tw:items-center tw:gap-1">
              <iconify icon="ph:lightning-bold" />
              {{ t('dashboard.charts.peakLabel', { h: peakHour.h, rev: fmtK(peakHour.rev) + '₫' }) }}
            </p>
            <p class="tw:mt-1 tw:text-[11px] tw:text-muted tw:leading-relaxed">
              {{ t('dashboard.charts.prepareBefore') }}
              <span class="tw:font-medium">11h45</span>
            </p>
          </div>
        </template>
      </prime-card>
    </div>

    <!-- ── Live Row ─────────────────────────────────────────────────── -->
    <div class="tw:grid tw:grid-cols-1 tw:gap-4 tw:lg:grid-cols-2">

      <!-- Table status grid -->
      <prime-card class="app-card tw:rounded-2xl tw:border">
        <template #content>
          <div class="tw:flex tw:items-center tw:justify-between tw:mb-4">
            <div>
              <p class="tw:text-sm tw:font-semibold">{{ t('dashboard.tableStatus.title') }}</p>
              <p class="tw:text-xs tw:text-muted">{{ t('dashboard.tableStatus.occupiedCount', { n: kpi.tables.occupied }) }}</p>
            </div>
          </div>

          <div class="tw:grid tw:grid-cols-6 tw:gap-2">
            <button
              v-for="tbl in tables"
              :key="tbl.id"
              class="tw:aspect-square tw:rounded-xl tw:flex tw:flex-col tw:items-center tw:justify-center tw:gap-0.5 tw:ring-1 tw:transition-transform tw:hover:scale-105 tw:text-white"
              :class="[tableStatusMeta[tbl.status].color, tableStatusMeta[tbl.status].ring]"
              v-tooltip.top="tableStatusMeta[tbl.status].label"
              @click="router.push({ name: 'tables' })"
            >
              <iconify :icon="tableStatusMeta[tbl.status].icon" class="tw:text-sm tw:opacity-90" />
              <span class="tw:text-[9px] tw:font-semibold tw:opacity-90">{{ tbl.number }}</span>
            </button>
          </div>

          <!-- Legend -->
          <div class="tw:mt-4 tw:pt-3 tw:border-t tw:border-white/10 tw:flex tw:items-center tw:gap-5">
            <template v-for="(meta, key) in tableStatusMeta" :key="key">
              <span class="tw:flex tw:items-center tw:gap-1.5 tw:text-xs tw:text-muted">
                <span class="tw:size-2.5 tw:rounded-full tw:flex-shrink-0" :class="meta.dot" />
                {{ meta.label }}
              </span>
            </template>
          </div>
        </template>
      </prime-card>

      <!-- Recent orders -->
      <prime-card class="app-card tw:rounded-2xl tw:border">
        <template #content>
          <div class="tw:flex tw:items-center tw:justify-between tw:mb-4">
            <div>
              <p class="tw:text-sm tw:font-semibold">{{ t('dashboard.recentOrders.title') }}</p>
              <p class="tw:text-xs tw:text-muted">{{ t('dashboard.recentOrders.subtitle') }}</p>
            </div>
            <prime-button
              severity="secondary"
              outlined
              size="small"
              @click="router.push({ name: 'orders-kanban' })"
            >
              <iconify icon="ph:arrow-right-bold" class="tw:mr-1 tw:text-xs" />
              <span>{{ t('dashboard.recentOrders.viewKanban') }}</span>
            </prime-button>
          </div>

          <div class="tw:space-y-2">
            <div
              v-for="o in recentOrders"
              :key="o.id"
              class="app-panel tw:flex tw:items-center tw:justify-between tw:rounded-xl tw:border tw:px-3 tw:py-2.5 tw:cursor-pointer tw:hover:border-white/20 tw:transition-colors"
              @click="router.push({ name: 'orders' })"
            >
              <div class="tw:flex tw:items-center tw:gap-3 tw:min-w-0">
                <div class="tw:flex-shrink-0">
                  <span class="tw:text-xs tw:font-mono tw:font-semibold">{{ o.number }}</span>
                </div>
                <div class="tw:min-w-0">
                  <p class="tw:text-xs tw:text-muted tw:truncate">{{ o.table }}</p>
                </div>
              </div>
              <div class="tw:flex tw:items-center tw:gap-2 tw:flex-shrink-0">
                <span class="tw:text-xs tw:font-semibold">{{ fmt(o.amount) }}</span>
                <prime-tag
                  :value="orderStatusMeta[o.status].label"
                  :severity="orderStatusMeta[o.status].severity"
                  class="tw:text-[10px]!"
                />
              </div>
            </div>
          </div>
        </template>
      </prime-card>
    </div>

    <!-- ── Bottom Row: Top products + Revenue drivers + Top category + Payment ratio ─── -->
    <div class="tw:grid tw:grid-cols-1 tw:gap-4 tw:lg:grid-cols-2 tw:xl:grid-cols-4">

      <!-- Top products (compact) -->
      <widget-top-products
        :title="t('dashboard.topProducts.title')"
        :subtitle="t('dashboard.topProducts.subtitle')"
        :unit="t('dashboard.topProducts.unit')"
        :items="[
          { name: 'Bạc xỉu',       qty: 28, pct: 100 },
          { name: 'Cà phê sữa đá', qty: 22, pct: 79  },
          { name: 'Matcha latte',   qty: 18, pct: 64  },
          { name: 'Cold brew',      qty: 15, pct: 54  },
          { name: 'Trà đào',        qty: 12, pct: 43  },
        ]"
      />

      <!-- Revenue Drivers -->
      <prime-card class="app-card tw:rounded-2xl tw:border">
        <template #content>
          <div class="tw:flex tw:items-center tw:justify-between tw:mb-3">
            <div>
              <p class="tw:text-sm tw:font-semibold tw:flex tw:items-center tw:gap-1.5">
                <iconify icon="ph:currency-circle-dollar-bold" class="tw:text-primary-400" />
                {{ t('dashboard.revenueDrivers.title') }}
              </p>
              <p class="tw:text-xs tw:text-muted">{{ t('dashboard.revenueDrivers.subtitle') }}</p>
            </div>
          </div>

          <div class="tw:space-y-2">
            <div
              v-for="(p, i) in revenueDrivers"
              :key="i"
              class="tw:flex tw:items-center tw:gap-2"
            >
              <!-- Rank -->
              <span class="tw:text-[11px] tw:font-bold tw:w-3 tw:text-center app-text-subtle tw:flex-shrink-0">{{ i + 1 }}</span>
              <div class="tw:flex-1 tw:min-w-0">
                <div class="tw:flex tw:items-center tw:justify-between tw:mb-0.5">
                  <div class="tw:flex tw:items-center tw:gap-1 tw:min-w-0">
                    <span class="tw:text-xs tw:font-medium tw:truncate">{{ p.name }}</span>
                    <!-- Upsell badge: revenue rank cao hơn qty rank -->
                    <span
                      v-if="i + 1 < p.qtyRank"
                      class="tw:flex-shrink-0 tw:text-[9px] tw:font-bold tw:px-1 tw:py-0.5 tw:rounded tw:bg-primary-500/20 tw:text-primary-400"
                    >{{ t('dashboard.revenueDrivers.upsell') }}</span>
                  </div>
                  <span class="tw:text-[11px] tw:font-semibold tw:text-white/80 tw:ml-1 tw:flex-shrink-0">
                    {{ fmtK(p.rev) }}₫
                  </span>
                </div>
                <!-- Revenue bar -->
                <div class="tw:h-1 tw:w-full tw:rounded-full tw:bg-white/8">
                  <div
                    class="tw:h-full tw:rounded-full tw:bg-primary-400/70"
                    :style="{ width: (p.rev / maxDriverRev * 100) + '%' }"
                  />
                </div>
              </div>
            </div>
          </div>

          <!-- Insight footer -->
          <div class="tw:mt-3 tw:pt-3 tw:border-t tw:border-white/10 tw:text-[11px] tw:text-muted tw:leading-relaxed">
            <iconify icon="ph:lightbulb-bold" class="tw:text-amber-400 tw:mr-1" />
            {{ t('dashboard.revenueDrivers.insightPre') }} <span class="tw:font-medium">{{ t('dashboard.revenueDrivers.upsell') }}</span>
            {{ t('dashboard.revenueDrivers.insightPost') }}
          </div>
        </template>
      </prime-card>

      <!-- Top category -->
      <widget-top-categories
        :title="t('dashboard.topCategories.title')"
        :subtitle="t('dashboard.topCategories.subtitle')"
        :items="topCategories"
      />

      <!-- Payment ratio -->
      <prime-card class="app-card tw:rounded-2xl tw:border">
        <template #content>
          <div class="tw:flex tw:items-center tw:justify-between tw:mb-4">
            <div>
              <p class="tw:text-sm tw:font-semibold">{{ t('dashboard.paymentRatio.title') }}</p>
              <p class="tw:text-xs tw:text-muted">{{ t('dashboard.paymentRatio.today') }}</p>
            </div>
            <iconify icon="ph:credit-card-bold" class="tw:text-blue-400 tw:opacity-60 tw:text-lg" />
          </div>

          <!-- Split bar -->
          <div class="tw:flex tw:h-6 tw:w-full tw:overflow-hidden tw:rounded-xl tw:gap-0.5">
            <div
              class="tw:flex tw:items-center tw:justify-center tw:text-[10px] tw:font-bold tw:text-white tw:bg-primary-500/80 tw:transition-all"
              :style="{ width: paymentRatio.cash + '%' }"
            >
              {{ paymentRatio.cash }}%
            </div>
            <div
              class="tw:flex tw:items-center tw:justify-center tw:text-[10px] tw:font-bold tw:text-white tw:bg-blue-500/80 tw:transition-all tw:flex-1"
            >
              {{ paymentRatio.bank }}%
            </div>
          </div>

          <!-- Detail rows -->
          <div class="tw:mt-4 tw:space-y-2.5">
            <div class="tw:flex tw:items-center tw:justify-between">
              <span class="tw:flex tw:items-center tw:gap-2 tw:text-sm">
                <span class="tw:size-2.5 tw:rounded-full tw:bg-primary-500 tw:flex-shrink-0" />
                <iconify icon="ph:money-bold" class="tw:text-muted" />
                {{ t('dashboard.paymentRatio.cash') }}
              </span>
              <div class="tw:text-right">
                <span class="tw:text-sm tw:font-semibold">{{ fmtK(kpi.revenue.cash) }}₫</span>
                <span class="tw:text-xs tw:text-muted tw:ml-2">{{ paymentRatio.cash }}%</span>
              </div>
            </div>
            <div class="tw:flex tw:items-center tw:justify-between">
              <span class="tw:flex tw:items-center tw:gap-2 tw:text-sm">
                <span class="tw:size-2.5 tw:rounded-full tw:bg-blue-500 tw:flex-shrink-0" />
                <iconify icon="ph:bank-bold" class="tw:text-muted" />
                {{ t('dashboard.paymentRatio.bank') }}
              </span>
              <div class="tw:text-right">
                <span class="tw:text-sm tw:font-semibold">{{ fmtK(kpi.revenue.bank) }}₫</span>
                <span class="tw:text-xs tw:text-muted tw:ml-2">{{ paymentRatio.bank }}%</span>
              </div>
            </div>
          </div>
        </template>
      </prime-card>

    </div>

  </section>
</template>
