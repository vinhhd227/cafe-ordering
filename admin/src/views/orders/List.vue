<script setup>
import { useRouter } from "vue-router";
import { getOrders, deleteOrder } from "@/services/order.service";
import { useOrderListFilters } from "@/composables/useOrderListFilters";
import AppTable from "@/components/AppTable.vue";
import WidgetStat from "@/components/widgets/WidgetStat.vue";
import WidgetOrdersRevenue from "@/components/widgets/orders/WidgetOrdersRevenue.vue";
import WidgetOrdersSummary from "@/components/widgets/orders/WidgetOrdersSummary.vue";
import WidgetSettingsButton from "@/components/widgets/WidgetSettingsButton.vue";
import OrderFilterPanel from "./OrderFilterPanel.vue";
import OrderPaymentDialog from "./OrderPaymentDialog.vue";
import { ORDER_STATUS, ORDER_STATUS_MAP } from "@/constants/orderStatus";
import { PAYMENT_STATUS, PAYMENT_STATUS_MAP } from "@/constants/paymentStatus";

const router = useRouter();
const { t } = useI18n()
const toast = useToast()
const confirm = useConfirm()
const { can } = usePermission()

// ── Table state cache ─────────────────────────────────────────────
const { save: saveCache, restore: restoreCache } = useTableCache("orders-list");

// ── Data ──────────────────────────────────────────────────────────
const orders = ref([]);
const totalRecords = ref(0);
const cashTotal = ref(0);
const bankTransferTotal = ref(0);
const tipTotal = ref(0);
const pendingCount = ref(0);
const processingCount = ref(0);
const completedCount = ref(0);
const cancelledCount = ref(0);
const loading = ref(false);
const errorMessage = ref("");

// ── Pagination (server-side) ───────────────────────────────────────
const rows = ref(20);
const first = ref(0);

// ── Filters ───────────────────────────────────────────────────────
const {
  filters,
  statusOptions, paymentStatusOptions, paymentMethodOptions,
  activeFilterCount, hasActiveFilters,
  clearFilters, todayMidnight,
} = useOrderListFilters()

// ── Restore cached state (before watchers fire) ───────────────────
const _cached = restoreCache();
if (_cached) {
  if (_cached.rows !== undefined)                rows.value                   = _cached.rows;
  if (_cached.first !== undefined)               first.value                  = _cached.first;
  if (_cached.statusFilter !== undefined)        filters.statusFilter         = _cached.statusFilter;
  if (_cached.paymentStatusFilter !== undefined) filters.paymentStatusFilter  = _cached.paymentStatusFilter;
  if (_cached.paymentMethodFilter !== undefined) filters.paymentMethodFilter  = _cached.paymentMethodFilter;
  if (_cached.searchOrder !== undefined)         filters.searchOrder          = _cached.searchOrder;
  if (_cached.tableCodeFilter !== undefined)     filters.tableCodeFilter      = _cached.tableCodeFilter;
  if (_cached.minTotal !== undefined)            filters.minTotal             = _cached.minTotal;
  if (_cached.maxTotal !== undefined)            filters.maxTotal             = _cached.maxTotal;
  if (_cached.dateFrom) filters.dateFrom = new Date(_cached.dateFrom);
  if (_cached.dateTo)   filters.dateTo   = new Date(_cached.dateTo);
}

const filterPanel = ref(null);
const filterDrawerVisible = ref(false);

const isMobile = ref(window.innerWidth < 640);
const _onResize = () => { isMobile.value = window.innerWidth < 640; };
onMounted(() => window.addEventListener('resize', _onResize));
onUnmounted(() => window.removeEventListener('resize', _onResize));

const openFilter = (e) => {
  if (isMobile.value) filterDrawerVisible.value = true;
  else filterPanel.value.toggle(e);
};

// ── Widget visibility ──────────────────────────────────────────────
const { isVisible: wVisible, toggle: wToggle, hiddenCount: wHidden, widgets: wDefs, colsPerRow: wCols, setColsPerRow: wSetCols } =
  useWidgetSettings('orders-list', [
    {
      id: 'total',
      label: t('orders.widgets.totalOrders'),
      description: t('orders.widgets.totalOrdersListDesc'),
      previewComponent: WidgetOrdersSummary,
      previewProps: { total: 128, pending: 18, processing: 8, completed: 98, cancelled: 4 },
    },
    {
      id: 'revenue',
      label: t('orders.widgets.totalRevenue'),
      description: t('orders.widgets.totalRevenueListDesc'),
      previewComponent: WidgetOrdersRevenue,
      previewProps: { total: 4250000, cash: 2400000, bank: 1650000, tip: 200000 },
    },
    {
      id: 'cash',
      label: t('orders.widgets.cashCollected'),
      description: t('orders.widgets.cashCollected'),
      previewComponent: WidgetStat,
      previewProps: { label: 'Cash collected', value: '2,400,000 ₫', labelClass: 'tw:text-emerald-400' },
    },
    {
      id: 'bank',
      label: t('orders.widgets.bankTransfer'),
      description: t('orders.widgets.bankTransfer'),
      previewComponent: WidgetStat,
      previewProps: { label: 'Bank transfer', value: '1,850,000 ₫', labelClass: 'tw:text-blue-400' },
    },
  ], { defaultCols: 4 })
const W_COLS_CLASS = { 1: 'tw:grid-cols-1', 2: 'tw:grid-cols-2', 3: 'tw:grid-cols-3', 4: 'tw:grid-cols-4' }
const wColsClass = computed(() => W_COLS_CLASS[wCols.value] ?? 'tw:grid-cols-2')

const summary = computed(() => ({
  total: totalRecords.value,
  cash: cashTotal.value,
  bank: bankTransferTotal.value,
  tip: tipTotal.value,
  revenue: cashTotal.value + bankTransferTotal.value,
  pending: pendingCount.value,
  processing: processingCount.value,
  completed: completedCount.value,
  cancelled: cancelledCount.value,
}));

// ── Helpers ───────────────────────────────────────────────────────
const toMidnight = (d) => {
  if (!d) return undefined;
  return new Date(d.getFullYear(), d.getMonth(), d.getDate());
};

const shortOrderNum = (num) => num ? `ORD-${num.slice(-6)}` : num

const formatVnd = (value) =>
  new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND", maximumFractionDigits: 0 }).format(value ?? 0);

const formatDate = (dateStr) =>
  new Intl.DateTimeFormat("vi-VN", {
    day: "2-digit", month: "2-digit", year: "numeric",
    hour: "2-digit", minute: "2-digit",
  }).format(new Date(dateStr));

const statusTag = (status) => {
  const meta = ORDER_STATUS_MAP[status] ?? { severity: 'secondary' }
  return { ...meta, label: t(`orders.status.${status}`, status) }
}

const paymentTag = (status, method) => {
  if (status === PAYMENT_STATUS.PAID) {
    const m = t(`orders.paymentMethod.${method}`, '')
    return { label: m ? t('orders.pay.paidWith', { method: m }) : t('orders.paymentStatus.PAID'), severity: 'success' }
  }
  const meta = PAYMENT_STATUS_MAP[status] ?? { severity: 'warn' }
  return { ...meta, label: t(`orders.paymentStatus.${status}`, meta.label ?? status) }
}

// ── Load ──────────────────────────────────────────────────────────
const saveCurrentState = () => {
  saveCache({
    rows: rows.value,
    first: first.value,
    statusFilter: filters.statusFilter,
    paymentStatusFilter: filters.paymentStatusFilter,
    paymentMethodFilter: filters.paymentMethodFilter,
    searchOrder: filters.searchOrder,
    tableCodeFilter: filters.tableCodeFilter,
    minTotal: filters.minTotal,
    maxTotal: filters.maxTotal,
    dateFrom: filters.dateFrom?.toISOString?.() ?? filters.dateFrom,
    dateTo: filters.dateTo?.toISOString?.() ?? filters.dateTo,
    colDefs: columns.value.map(c => ({ key: c.key ?? c.field, visible: c.visible })),
  });
};

const loadOrders = async () => {
  loading.value = true;
  errorMessage.value = "";
  try {
    const page = Math.floor(first.value / rows.value) + 1;
    const res = await getOrders({
      status: filters.statusFilter || undefined,
      paymentStatus: filters.paymentStatusFilter || undefined,
      paymentMethod: filters.paymentMethodFilter || undefined,
      orderNumber: filters.searchOrder.trim() || undefined,
      minAmount: filters.minTotal ?? undefined,
      maxAmount: filters.maxTotal ?? undefined,
      tableCode: filters.tableCodeFilter.trim() || undefined,
      page,
      pageSize: rows.value,
      dateFrom: toMidnight(filters.dateFrom),
      dateTo: toMidnight(filters.dateTo),
    });
    const data = res?.data;
    orders.value = data?.items ?? [];
    totalRecords.value = data?.totalCount ?? 0;
    cashTotal.value = data?.cashTotal ?? 0;
    bankTransferTotal.value = data?.bankTransferTotal ?? 0;
    tipTotal.value = data?.tipTotal ?? 0;
    pendingCount.value = data?.pendingCount ?? 0;
    processingCount.value = data?.processingCount ?? 0;
    completedCount.value = data?.completedCount ?? 0;
    cancelledCount.value = data?.cancelledCount ?? 0;
    saveCurrentState();
  } catch (err) {
    errorMessage.value = err?.response?.data?.message || "Failed to load orders.";
  } finally {
    loading.value = false;
  }
};

onMounted(loadOrders);
onBeforeRouteLeave(() => { saveCurrentState(); });

watch(
  () => [
    filters.statusFilter, filters.paymentStatusFilter, filters.paymentMethodFilter,
    filters.dateFrom, filters.dateTo,
    filters.searchOrder, filters.tableCodeFilter,
    filters.minTotal, filters.maxTotal,
  ],
  () => { first.value = 0; loadOrders(); },
);


const onPage = (e) => {
  first.value = e.first;
  rows.value = e.rows;
  loadOrders();
};

const buildColDefs = () => [
  { field: 'orderNumber',   header: t('orders.list.col.orderNumber'), width: '9rem',  visible: true },
  { field: 'orderDate',     header: t('orders.list.col.date'),        width: '10rem', visible: true },
  { field: 'status',        header: t('orders.list.col.status'),      width: '8rem',  visible: true },
  { field: 'paymentStatus', header: t('orders.list.col.payment'),     width: '10rem', visible: true },
  { field: 'guestCount',    header: t('orders.list.col.guests'),      width: '6rem',  visible: true },
  { key: 'items',           header: t('orders.list.col.items'),       width: '11rem', visible: true },
  { key: 'promos',          header: t('orders.list.col.discount'),    width: '11rem', visible: true },
  { field: 'totalAmount',   header: t('orders.list.col.total'),       width: '7rem',  visible: true },
  { key: 'actions',         header: t('orders.list.col.actions'),     width: '12rem', toggleable: false },
]
const columns = ref(buildColDefs())
if (_cached?.colDefs) {
  const cachedMap = Object.fromEntries(_cached.colDefs.map(c => [c.key, c]))
  columns.value = columns.value.map(col => {
    if (col.toggleable === false) return col
    const id = col.key ?? col.field
    const c = cachedMap[id]
    return c ? { ...col, visible: c.visible } : col
  })
}

// ── Payment dialog ────────────────────────────────────────────────
const payOrder = ref(null);

const onPaid = (order, { paymentMethod }) => {
  order.paymentStatus = PAYMENT_STATUS.PAID;
  order.paymentMethod = paymentMethod;
  payOrder.value = null;
};

// ── Copy ──────────────────────────────────────────────────────────
const copyOrderNumber = async (orderNumber) => {
  try {
    await navigator.clipboard.writeText(orderNumber)
  } catch {
    const el = document.createElement('textarea')
    el.value = orderNumber
    el.style.position = 'fixed'
    el.style.opacity = '0'
    document.body.appendChild(el)
    el.select()
    document.execCommand('copy')
    document.body.removeChild(el)
  }
  toast.add({ severity: 'success', summary: t('orders.list.copyOrderNumberSuccess'), life: 2000 })
}

// ── Delete ────────────────────────────────────────────────────────
const handleDeleteOrder = (order) => {
  confirm.require({
    message: t('orders.detail.deleteOrderConfirmMsg', { orderNumber: order.orderNumber }),
    header: t('orders.detail.deleteOrderConfirmHeader'),
    icon: 'ph:trash-bold',
    rejectProps: { severity: 'secondary', outlined: true, size: 'small', label: t('common.cancel') },
    acceptProps: { severity: 'danger', size: 'small', label: t('orders.detail.deleteOrder') },
    accept: async () => {
      try {
        await deleteOrder(order.id)
        toast.add({ severity: 'success', summary: t('orders.detail.deleteOrderSuccess', { orderNumber: order.orderNumber }), life: 3000 })
        await loadOrders()
      } catch (err) {
        errorMessage.value = err?.response?.data?.errors?.join(', ') || err?.response?.data?.title || 'Failed to delete order.'
      }
    },
  })
}
</script>

<template>
  <prime-confirm-dialog />
  <OrderPaymentDialog :order="payOrder" @paid="onPaid" @close="payOrder = null" />

  <section class="tw:space-y-8">
    <!-- Header -->
    <page-header :subtitle="t('orders.list.subtitle')">
      <widget-settings-button
        :widgets="wDefs"
        :hidden-count="wHidden"
        :cols-per-row="wCols"
        @toggle="wToggle"
        @update:cols-per-row="wSetCols"
      />
      <prime-button
        v-if="can('order.createManual')"
        severity="secondary"
        outlined
        size="small"
        class="tw:h-8!"
        @click="router.push({ name: 'ordersCreateManual' })"
      >
        <iconify icon="ph:pencil-line-bold" class="tw:mr-1" />
        <span>{{ t('orders.newManualOrder') }}</span>
      </prime-button>
      <prime-button
        severity="secondary"
        outlined
        size="small"
        class="tw:h-8!"
        :loading="loading"
        @click="loadOrders"
      >
        <iconify icon="ph:arrows-clockwise-bold" class="tw:mr-1" />
        <span>{{ t('orders.refresh') }}</span>
      </prime-button>
    </page-header>

    <!-- Mobile compact stats -->
    <div class="tw:flex tw:sm:hidden tw:items-center tw:gap-3 tw:rounded-xl tw:border tw:border-white/10 tw:bg-white/5 tw:px-4 tw:py-3">
      <div class="tw:shrink-0">
        <span class="tw:text-xl tw:font-bold">{{ summary.total }}</span>
        <span class="tw:text-muted tw:text-xs tw:ml-1.5">{{ t('orders.breadcrumb').toLowerCase() }}</span>
      </div>
      <div class="tw:h-4 tw:w-px tw:bg-white/10 tw:shrink-0" />
      <div class="tw:flex tw:flex-wrap tw:gap-x-3 tw:gap-y-0.5">
        <span v-if="summary.pending > 0"    class="tw:text-amber-400   tw:text-xs tw:font-medium">{{ summary.pending }} {{ t('orders.status.PENDING').toLowerCase() }}</span>
        <span v-if="summary.processing > 0" class="tw:text-blue-400    tw:text-xs tw:font-medium">{{ summary.processing }} {{ t('orders.status.PROCESSING').toLowerCase() }}</span>
        <span v-if="summary.completed > 0"  class="tw:text-emerald-400 tw:text-xs tw:font-medium">{{ summary.completed }} {{ t('orders.status.COMPLETED').toLowerCase() }}</span>
        <span v-if="summary.cancelled > 0"  class="tw:text-red-400     tw:text-xs tw:font-medium">{{ summary.cancelled }} {{ t('orders.status.CANCELLED').toLowerCase() }}</span>
      </div>
      <span class="tw:ml-auto tw:font-semibold tw:text-sm tw:shrink-0">{{ formatVnd(summary.revenue) }}</span>
    </div>

    <!-- Summary stats (desktop only) -->
    <div :class="['tw:hidden tw:sm:grid tw:gap-3', wColsClass]">
      <widget-orders-summary
        v-if="wVisible('total')"
        :total="summary.total"
        :pending="summary.pending"
        :processing="summary.processing"
        :completed="summary.completed"
        :cancelled="summary.cancelled"
      />
      <widget-orders-revenue
        v-if="wVisible('revenue')"
        :total="summary.revenue"
        :cash="summary.cash"
        :bank="summary.bank"
        :tip="summary.tip"
      />
      <widget-stat
        v-if="wVisible('cash')"
        :label="t('orders.widgets.cashCollected')"
        label-class="tw:text-emerald-400"
        :value="formatVnd(summary.cash)"
      >
        <template #icon>
          <iconify icon="ph:money-bold" class="tw:text-emerald-400 tw:opacity-60" />
        </template>
      </widget-stat>
      <widget-stat
        v-if="wVisible('bank')"
        :label="t('orders.widgets.bankTransfer')"
        label-class="tw:text-blue-400"
        :value="formatVnd(summary.bank)"
      >
        <template #icon>
          <iconify icon="ph:bank-bold" class="tw:text-blue-400 tw:opacity-60" />
        </template>
      </widget-stat>
    </div>

    <!-- Error -->
    <prime-message
      v-if="errorMessage"
      severity="error"
      size="small"
      variant="simple"
      :closable="true"
      @close="errorMessage = ''"
    >{{ errorMessage }}</prime-message>

    <!-- Filter drawer (mobile) -->
    <prime-drawer
      v-model:visible="filterDrawerVisible"
      position="bottom"
      :style="{ height: 'auto', maxHeight: '90dvh' }"
      :pt="{ root: { class: 'tw:rounded-t-2xl' }, content: { class: 'tw:overflow-y-auto' } }"
    >
      <template #header>
        <div class="tw:flex tw:items-center tw:justify-between tw:w-full tw:pr-2">
          <span class="tw:font-semibold">{{ t('orders.filter.title') }}</span>
          <prime-button
            v-if="hasActiveFilters"
            severity="danger"
            outlined
            size="small"
            @click="clearFilters"
          >
            <iconify icon="ph:x-bold" />
            <span>{{ t('orders.filter.clearFilters') }}</span>
          </prime-button>
        </div>
      </template>
      <div class="tw:pb-6">
        <OrderFilterPanel
          :filters="filters"
          :status-options="statusOptions"
          :payment-status-options="paymentStatusOptions"
          :payment-method-options="paymentMethodOptions"
          show-search
        />
      </div>
    </prime-drawer>

    <!-- Table -->
    <AppTable
      lazy
      size="small"
      v-model:first="first"
      v-model:rows="rows"
      :value="orders"
      :loading="loading"
      :totalRecords="totalRecords"
      :rowsPerPageOptions="[10, 20, 50]"
      :columns="columns"
      @update:columns="(cols) => saveCache({ ...restoreCache(), colDefs: cols.map(c => ({ key: c.key ?? c.field, visible: c.visible })) })"
      @page="onPage"
    >
      <template #toolbar-left>
        <div class="tw:flex tw:items-center tw:gap-2">
          <!-- Search input — desktop only -->
          <prime-input-text
            v-model="filters.searchOrder"
            :placeholder="t('orders.filter.searchOrder')"
            class="app-input tw:w-48 tw:hidden tw:sm:block"
          />

          <!-- Filter toggle button -->
          <prime-button
            :severity="hasActiveFilters ? 'success' : 'secondary'"
            :outlined="!hasActiveFilters"
            v-tooltip.top="t('orders.filter.filters')"
            @click="openFilter($event)"
            :class="btnIcon"
          >
            <span class="tw:relative tw:inline-flex">
              <iconify icon="ph:funnel-bold" />
              <prime-badge
                v-if="activeFilterCount > 0"
                :value="activeFilterCount"
                severity="danger"
                class="tw:absolute! tw:-top-2.5! tw:-right-2.5! tw:scale-75! tw:origin-top-right!"
              />
            </span>
          </prime-button>

          <!-- Filter popover (desktop only) -->
          <prime-popover ref="filterPanel">
            <div class="tw:flex tw:flex-col tw:gap-4 tw:w-full">
              <p class="tw:text-sm tw:font-semibold">{{ t('orders.filter.title') }}</p>
              <OrderFilterPanel
                :filters="filters"
                :status-options="statusOptions"
                :payment-status-options="paymentStatusOptions"
                :payment-method-options="paymentMethodOptions"
              />
              <prime-button
                v-if="hasActiveFilters"
                severity="danger"
                outlined
                size="small"
                @click="clearFilters"
              >
                <iconify icon="ph:x-bold" />
                <span>{{ t('orders.filter.clearFilters') }}</span>
              </prime-button>
            </div>
          </prime-popover>
        </div>
      </template>

      <!-- ── Mobile card ──────────────────────────────────────────── -->
      <template #mobile-card="{ data }">
        <div
          class="tw:col-span-2 tw:rounded-xl tw:border tw:border-white/10 tw:bg-white/5 tw:p-3 tw:flex tw:flex-col tw:gap-2"
          :class="data.status === ORDER_STATUS.CANCELLED ? 'tw:opacity-60' : ''"
        >
          <!-- Order # + amount -->
          <div class="tw:flex tw:items-start tw:justify-between tw:gap-2">
            <div>
              <div class="tw:flex tw:items-center tw:gap-1.5">
                <span class="tw:font-mono tw:font-bold tw:text-sm" v-tooltip.top="data.orderNumber">{{ shortOrderNum(data.orderNumber) }}</span>
                <prime-tag v-if="data.isManual" value="Thủ công" severity="secondary" class="tw:text-[10px]! tw:px-1.5! tw:py-0!" />
              </div>
              <p class="tw:text-[11px] tw:text-muted tw:mt-0.5">{{ formatDate(data.orderDate) }}</p>
            </div>
            <div class="tw:text-right tw:shrink-0">
              <p class="tw:font-semibold tw:text-sm">
                <span v-if="data.totalDiscount > 0" class="tw:text-xs tw:text-muted tw:line-through tw:mr-1">
                  {{ formatVnd(data.totalAmount) }}
                </span>
                {{ formatVnd(data.totalDiscount > 0 ? data.finalAmount : data.totalAmount) }}
              </p>
              <div v-if="data.tipAmount > 0" class="tw:flex tw:items-center tw:justify-end tw:gap-1 tw:text-[10px] tw:text-amber-400">
                <iconify icon="ph:heart-fill" /><span>+{{ formatVnd(data.tipAmount) }}</span>
              </div>
            </div>
          </div>

          <!-- Status + payment tags + guests -->
          <div class="tw:flex tw:flex-wrap tw:items-center tw:gap-1.5">
            <prime-tag :value="statusTag(data.status).label" :severity="statusTag(data.status).severity" class="tw:text-[10px]! tw:px-1.5! tw:py-0.5!" />
            <prime-tag :value="paymentTag(data.paymentStatus, data.paymentMethod).label" :severity="paymentTag(data.paymentStatus, data.paymentMethod).severity" class="tw:text-[10px]! tw:px-1.5! tw:py-0.5!" />
            <span v-if="data.guestCount" class="tw:flex tw:items-center tw:gap-0.5 tw:text-[10px] tw:text-muted">
              <iconify icon="ph:users" />{{ data.guestCount }}
            </span>
          </div>

          <!-- Items summary -->
          <div class="tw:flex tw:flex-wrap tw:gap-x-3 tw:gap-y-0.5">
            <span v-for="(item, idx) in (data.items ?? []).slice(0, 3)" :key="idx" class="tw:text-[11px] tw:text-muted">
              <span class="tw:text-emerald-400 tw:font-semibold">{{ item.quantity }}×</span>
              {{ item.productName }}
            </span>
            <span v-if="(data.items?.length ?? 0) > 3" class="tw:text-[10px] app-text-subtle tw:italic">
              {{ t('orders.list.moreItems', { n: data.items.length - 3 }) }}
            </span>
          </div>

          <!-- Actions -->
          <div class="tw:flex tw:items-center tw:justify-end tw:gap-2 tw:pt-1.5 tw:border-t tw:border-white/10">
            <prime-button
              v-if="data.paymentStatus === PAYMENT_STATUS.UNPAID && data.status !== ORDER_STATUS.CANCELLED"
              severity="warn"
              size="small"
              outlined
              @click="payOrder = data"
            >
              <iconify icon="ph:money-bold" />
              <span>{{ t('orders.list.markPaid') }}</span>
            </prime-button>
            <prime-button
              v-if="can('order.delete') && data.status === ORDER_STATUS.CANCELLED"
              severity="danger"
              outlined
              :class="btnIcon"
              v-tooltip.top="t('orders.detail.deleteOrderTooltip')"
              @click="handleDeleteOrder(data)"
            >
              <iconify icon="ph:trash-bold" />
            </prime-button>
            <prime-button
              severity="secondary"
              outlined
              size="small"
              :class="btnIcon"
              @click="router.push({ name: 'ordersDetail', params: { id: data.id } })"
            >
              <iconify icon="ph:arrow-right-bold" />
            </prime-button>
          </div>
        </div>
      </template>

      <template #col-orderNumber="{ data }">
        <div class="tw:flex tw:flex-col tw:gap-0.5">
          <div class="tw:flex tw:items-center tw:gap-1">
            <span class="tw:font-mono tw:text-sm tw:font-semibold tw:whitespace-nowrap" v-tooltip.top="data.orderNumber">{{ shortOrderNum(data.orderNumber) }}</span>
            <prime-button
              :class="btnIcon"
              size="small"
              severity="secondary"
              text
              v-tooltip.top="t('orders.list.copyOrderNumber')"
              @click.stop="copyOrderNumber(data.orderNumber)"
            >
              <iconify icon="ph:copy" class="tw:text-xs" />
            </prime-button>
          </div>
          <prime-tag
            v-if="data.isManual"
            value="Thủ công"
            severity="secondary"
            class="tw:text-[10px]! tw:px-1.5! tw:py-0! tw:self-start"
          />
        </div>
      </template>

      <template #col-orderDate="{ data }">
        <span class="tw:text-sm tw:text-muted">{{ formatDate(data.orderDate) }}</span>
      </template>

      <template #col-status="{ data }">
        <prime-tag :value="statusTag(data.status).label" :severity="statusTag(data.status).severity" />
      </template>

      <template #col-paymentStatus="{ data }">
        <prime-tag
          :value="paymentTag(data.paymentStatus, data.paymentMethod).label"
          :severity="paymentTag(data.paymentStatus, data.paymentMethod).severity"
        />
      </template>

      <template #col-guestCount="{ data }">
        <span v-if="data.guestCount" class="tw:flex tw:items-center tw:gap-1 tw:text-sm">
          <iconify icon="ph:users" class="tw:text-sm tw:opacity-60" />
          {{ data.guestCount }}
        </span>
        <span v-else class="tw:text-xs app-text-subtle">—</span>
      </template>

      <template #col-items="{ data }">
        <div class="tw:space-y-0.5">
          <div
            v-for="(item, idx) in (data.items ?? []).slice(0, 3)"
            :key="idx"
            class="tw:flex tw:items-center tw:gap-1.5 tw:text-xs"
          >
            <span class="tw:shrink-0 tw:font-semibold tw:text-emerald-400 tw:w-4 tw:text-right">{{ item.quantity }}×</span>
            <span class="tw:truncate tw:text-muted" style="max-width: 9rem">{{ item.productName }}</span>
          </div>
          <span v-if="(data.items?.length ?? 0) > 3" class="tw:text-[10px] app-text-subtle tw:italic">
            {{ t('orders.list.moreItems', { n: data.items.length - 3 }) }}
          </span>
        </div>
      </template>

      <template #col-promos="{ data }">
        <div v-if="data.promotions?.length" class="tw:space-y-0.5">
          <div v-for="p in data.promotions" :key="p.promotionId" class="tw:flex tw:items-center tw:gap-1.5">
            <prime-tag :value="p.promoCode" severity="success" class="tw:text-[10px]! tw:shrink-0" />
            <span class="tw:text-xs tw:text-emerald-400 tw:font-medium tw:shrink-0">
              –{{ formatVnd(p.discountAmount) }}
            </span>
          </div>
        </div>
        <span v-else class="tw:text-xs app-text-subtle">—</span>
      </template>

      <template #col-totalAmount="{ data }">
        <div class="tw:space-y-0.5">
          <div class="tw:flex tw:items-baseline tw:gap-1.5 tw:flex-wrap">
            <span v-if="data.totalDiscount > 0" class="tw:text-xs tw:text-muted tw:line-through">
              {{ formatVnd(data.totalAmount) }}
            </span>
            <span class="tw:font-semibold tw:text-sm">
              {{ formatVnd(data.totalDiscount > 0 ? data.finalAmount : data.totalAmount) }}
            </span>
          </div>
          <div v-if="data.tipAmount > 0" class="tw:flex tw:items-center tw:gap-1 tw:text-xs tw:text-amber-400">
            <iconify icon="ph:heart-fill" class="tw:text-[10px]" />
            <span>{{ formatVnd(data.tipAmount) }}</span>
          </div>
        </div>
      </template>

      <template #col-actions="{ data }">
        <div class="tw:flex tw:justify-end tw:items-center tw:gap-2">
          <prime-button
            v-if="data.paymentStatus === PAYMENT_STATUS.UNPAID && data.status !== ORDER_STATUS.CANCELLED"
            severity="warn"
            size="small"
            outlined
            @click="payOrder = data"
          >
            <iconify icon="ph:money-bold" />
            <span>{{ t('orders.list.markPaid') }}</span>
          </prime-button>
          <prime-button
            v-if="can('order.delete') && data.status === ORDER_STATUS.CANCELLED"
            severity="danger"
            outlined
            :class="btnIcon"
            v-tooltip.top="t('orders.detail.deleteOrderTooltip')"
            @click="handleDeleteOrder(data)"
          >
            <iconify icon="ph:trash-bold" />
          </prime-button>
          <prime-button
            severity="secondary"
            outlined
            size="small"
            :class="btnIcon"
            @click="router.push({ name: 'ordersDetail', params: { id: data.id } })"
          >
            <iconify icon="ph:arrow-right-bold" />
          </prime-button>
        </div>
      </template>
    </AppTable>
  </section>
</template>

