<script setup>
import {
  getOrders,
  updateOrderStatus,
  updatePayment,
} from "@/services/order.service";
import WidgetOrdersRevenue from "@/components/widgets/orders/WidgetOrdersRevenue.vue";
import WidgetOrdersSummary from "@/components/widgets/orders/WidgetOrdersSummary.vue";
import WidgetSettingsButton from "@/components/widgets/WidgetSettingsButton.vue";

const router = useRouter();
const { t } = useI18n();

const orders = ref([]);
const cashTotal = ref(0);
const bankTransferTotal = ref(0);
const loading = ref(false);
const errorMessage = ref("");
const updatingId = ref(null);
const draggingOrder = ref(null);
const dragOverCol = ref(null);
const pendingMoves = ref(new Map()); // orderId → { timeoutId, originalStatus }
const toast = useToast();
const confirm = useConfirm();

// ── SSE: real-time order updates ──────────────────────────────────
const isToday = (dateStr) => {
  const d = new Date(dateStr);
  const now = new Date();
  return (
    d.getFullYear() === now.getFullYear() &&
    d.getMonth() === now.getMonth() &&
    d.getDate() === now.getDate()
  );
};

const { connected: sseConnected } = useOrderSse({
  onOrderCreated(order) {
    // Only add if the order belongs to today and is not already in the list
    if (!isToday(order.orderDate)) return;
    const exists = orders.value.some((o) => o.id === order.id);
    if (!exists) orders.value.unshift(order);
  },
  onOrderUpdated(order) {
    const idx = orders.value.findIndex((o) => o.id === order.id);
    if (idx !== -1) {
      // Preserve pending drag-and-drop if an undo is in progress
      if (pendingMoves.value.has(order.id)) return;
      orders.value[idx] = order;
    }
  },
  onError(msg) {
    errorMessage.value = msg;
  },
});

// ── Payment dialog ────────────────────────────────────────────────
const payDialog = ref(false);
const payOrder = ref(null); // order being paid
const payMethod = ref("Cash"); // selected PaymentMethod
const payAmountReceived = ref(null); // actual amount received from customer
const payLoading = ref(false);

const payChange = computed(() => {
  if (payAmountReceived.value == null || !payOrder.value) return null;
  return payAmountReceived.value - payOrder.value.totalAmount;
});

const payTip = ref(0);
const payReturn = computed(() => {
  if (payChange.value === null || payChange.value <= 0) return null;
  return payChange.value - (payTip.value ?? 0);
});

// Clamp tip if change drops below the current tip value
watch(payChange, (val) => {
  if (val !== null && val >= 0 && payTip.value > val) {
    payTip.value = val;
  }
});

// Exclude UNKNOWN from payment dialog options
const PAYMENT_METHODS = computed(() =>
  PAYMENT_METHOD_OPTIONS.filter((o) => o.value !== PAYMENT_METHOD.UNKNOWN).map(
    (o) => ({
      ...o,
      label: t(`orders.paymentMethod.${o.value}`, o.label),
    }),
  ),
);

const paymentTag = (status, method) => {
  if (status === PAYMENT_STATUS.PAID) {
    const m = t(`orders.paymentMethod.${method}`, "");
    return {
      label: m
        ? t("orders.pay.paidWith", { method: m })
        : t("orders.paymentStatus.PAID"),
      severity: PAYMENT_STATUS_MAP[PAYMENT_STATUS.PAID].severity,
    };
  }
  const meta =
    PAYMENT_STATUS_MAP[status] ?? PAYMENT_STATUS_MAP[PAYMENT_STATUS.UNPAID];
  return {
    ...meta,
    label: t(`orders.paymentStatus.${status}`, meta.label ?? status),
  };
};

const openPayDialog = (order) => {
  payOrder.value = order;
  payMethod.value = PAYMENT_METHOD.CASH;
  payAmountReceived.value = null;
  payTip.value = 0;
  payDialog.value = true;
};

const confirmPayment = async () => {
  if (!payOrder.value) return;
  payLoading.value = true;
  try {
    await updatePayment(
      payOrder.value.id,
      PAYMENT_STATUS.PAID,
      payMethod.value,
      payAmountReceived.value,
      payTip.value ?? 0,
    );
    payOrder.value.paymentStatus = PAYMENT_STATUS.PAID;
    payOrder.value.paymentMethod = payMethod.value;
    payDialog.value = false;
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.errors?.join(", ") ||
      err?.response?.data?.title ||
      "Failed to update payment.";
  } finally {
    payLoading.value = false;
  }
};

const STATUSES = [
  {
    key: ORDER_STATUS.PENDING,
    icon: "ph:clock-bold",
    color: "tw:text-amber-400",
    bg: "tw:bg-amber-400/10 tw:border-amber-400/20",
    dot: "tw:bg-amber-400",
  },
  {
    key: ORDER_STATUS.PROCESSING,
    icon: "ph:fire-bold",
    color: "tw:text-blue-400",
    bg: "tw:bg-blue-400/10 tw:border-blue-400/20",
    dot: "tw:bg-blue-400",
  },
  {
    key: ORDER_STATUS.COMPLETED,
    icon: "ph:check-circle-bold",
    color: "tw:text-emerald-400",
    bg: "tw:bg-emerald-400/10 tw:border-emerald-400/20",
    dot: "tw:bg-emerald-400",
  },
  {
    key: ORDER_STATUS.CANCELLED,
    icon: "ph:x-circle-bold",
    color: "tw:text-red-400",
    bg: "tw:bg-red-400/10 tw:border-red-400/20",
    dot: "tw:bg-red-400",
  },
];

const NEXT_STATUS = {
  [ORDER_STATUS.PENDING]: ORDER_STATUS.PROCESSING,
  [ORDER_STATUS.PROCESSING]: ORDER_STATUS.COMPLETED,
};

const NEXT_LABEL = computed(() => ({
  [ORDER_STATUS.PENDING]: t("orders.kanban.startPreparing"),
  [ORDER_STATUS.PROCESSING]: t("orders.kanban.markComplete"),
}));

const activeTab = ref(ORDER_STATUS.PENDING);

const ordersByStatus = computed(() => {
  const map = {};
  for (const s of STATUSES) map[s.key] = [];
  for (const o of orders.value) {
    if (map[o.status]) map[o.status].push(o);
  }
  return map;
});

const summary = computed(() => ({
  total: orders.value.length,
  pending: ordersByStatus.value[ORDER_STATUS.PENDING].length,
  processing: ordersByStatus.value[ORDER_STATUS.PROCESSING].length,
  completed: ordersByStatus.value[ORDER_STATUS.COMPLETED].length,
  cancelled: ordersByStatus.value[ORDER_STATUS.CANCELLED].length,
  cash: cashTotal.value,
  bank: bankTransferTotal.value,
  revenue: cashTotal.value + bankTransferTotal.value,
}));

// ── Widget visibility ──────────────────────────────────────────────
const {
  isVisible: wVisible,
  toggle: wToggle,
  hiddenCount: wHidden,
  widgets: wDefs,
  colsPerRow: wCols,
  setColsPerRow: wSetCols,
} = useWidgetSettings(
  "orders-kanban",
  [
    {
      id: "summary",
      label: t("orders.widgets.totalOrders"),
      description: t("orders.widgets.totalOrdersKanbanDesc"),
      previewComponent: WidgetOrdersSummary,
      previewProps: {
        total: 34,
        pending: 8,
        processing: 5,
        completed: 21,
        cancelled: 0,
      },
    },
    {
      id: "revenue",
      label: t("orders.widgets.totalRevenue"),
      description: t("orders.widgets.totalRevenueKanbanDesc"),
      previewComponent: WidgetOrdersRevenue,
      previewProps: { total: 1750000, cash: 1000000, bank: 750000 },
    },
  ],
  { defaultCols: 2 },
);
const W_COLS_CLASS = {
  1: "tw:grid-cols-1",
  2: "tw:grid-cols-2",
  3: "tw:grid-cols-3",
  4: "tw:grid-cols-4",
};
const wColsClass = computed(
  () => W_COLS_CLASS[wCols.value] ?? "tw:grid-cols-2",
);

const formatVnd = (value) =>
  new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
    maximumFractionDigits: 0,
  }).format(value ?? 0);

const timeAgo = (dateStr) => {
  const diff = Date.now() - new Date(dateStr).getTime();
  const mins = Math.floor(diff / 60000);
  if (mins < 1) return "Just now";
  if (mins < 60) return `${mins}m ago`;
  const hrs = Math.floor(mins / 60);
  if (hrs < 24) return `${hrs}h ago`;
  return `${Math.floor(hrs / 24)}d ago`;
};

const todayRange = () => {
  const now = new Date();
  const dateFrom = new Date(
    now.getFullYear(),
    now.getMonth(),
    now.getDate(),
    0,
    0,
    0,
  ).toISOString();
  const dateTo = new Date(
    now.getFullYear(),
    now.getMonth(),
    now.getDate(),
    23,
    59,
    59,
  ).toISOString();
  return { dateFrom, dateTo };
};

const loadOrders = async () => {
  if (loading.value) return;
  loading.value = true;
  errorMessage.value = "";
  try {
    const res = await getOrders({ ...todayRange(), pageSize: 200 });
    orders.value = res?.data?.items ?? [];
    cashTotal.value = res?.data?.cashTotal ?? 0;
    bankTransferTotal.value = res?.data?.bankTransferTotal ?? 0;
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.message || "Failed to load orders.";
  } finally {
    loading.value = false;
  }
};

const moveOrder = async (order, toStatus) => {
  updatingId.value = order.id;
  try {
    await updateOrderStatus(order.id, toStatus);
    order.status = toStatus;
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.errors?.join(", ") || "Failed to update order.";
  } finally {
    updatingId.value = null;
  }
};

const cancelOrder = async (order) => {
  updatingId.value = order.id;
  try {
    await updateOrderStatus(order.id, ORDER_STATUS.CANCELLED);
    order.status = ORDER_STATUS.CANCELLED;
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.errors?.join(", ") || "Failed to cancel order.";
  } finally {
    updatingId.value = null;
  }
};

const isValidDrop = (fromStatus, toStatus) => {
  if (!fromStatus || fromStatus === toStatus) return false;
  if (NEXT_STATUS[fromStatus] === toStatus) return true;
  if (
    toStatus === ORDER_STATUS.CANCELLED &&
    (fromStatus === ORDER_STATUS.PENDING ||
      fromStatus === ORDER_STATUS.PROCESSING)
  )
    return true;
  return false;
};

const handleDragStart = (order) => {
  draggingOrder.value = order;
};
const handleDragEnd = () => {
  draggingOrder.value = null;
  dragOverCol.value = null;
};

const handleDragOver = (e, colKey) => {
  if (!isValidDrop(draggingOrder.value?.status, colKey)) return;
  e.preventDefault();
  dragOverCol.value = colKey;
};

const handleDragleave = () => {
  dragOverCol.value = null;
};

const commitMove = async (order, toStatus, originalStatus) => {
  pendingMoves.value.delete(order.id);
  updatingId.value = order.id;
  try {
    await updateOrderStatus(order.id, toStatus);
  } catch (err) {
    order.status = originalStatus;
    errorMessage.value =
      err?.response?.data?.errors?.join(", ") || "Failed to update order.";
  } finally {
    updatingId.value = null;
  }
};

const undoMove = (orderId) => {
  const pending = pendingMoves.value.get(orderId);
  if (!pending) return;
  clearTimeout(pending.timeoutId);
  pendingMoves.value.delete(orderId);
  const order = orders.value.find((o) => o.id === orderId);
  if (order) order.status = pending.originalStatus;
  toast.removeGroup("order-move");
};

const handleDrop = (colKey) => {
  const order = draggingOrder.value;
  dragOverCol.value = null;
  draggingOrder.value = null;
  if (!order || !isValidDrop(order.status, colKey)) return;

  if (pendingMoves.value.has(order.id)) {
    clearTimeout(pendingMoves.value.get(order.id).timeoutId);
  }

  const originalStatus = order.status;
  order.status = colKey;

  const timeoutId = setTimeout(
    () => commitMove(order, colKey, originalStatus),
    5000,
  );
  pendingMoves.value.set(order.id, { timeoutId, originalStatus });

  toast.add({
    severity: "info",
    summary: `Moved to ${colKey}`,
    detail: order.orderNumber,
    life: 5000,
    group: "order-move",
    data: { orderId: order.id },
  });
};

onMounted(() => {
  loadOrders();
  // SSE connection is managed by useOrderSse (onMounted/onUnmounted inside the composable)
});

onUnmounted(() => {
  // Clear all pending drag-drop timers to avoid API calls after unmount
  pendingMoves.value.forEach(({ timeoutId }) => clearTimeout(timeoutId));
});
</script>

<template>
  <prime-confirm-popup />

  <!-- Payment dialog -->
  <prime-dialog
    v-model:visible="payDialog"
    :header="t('orders.pay.title')"
    :modal="true"
    :style="{ width: '22rem' }"
  >
    <div class="tw:space-y-4">
      <p class="tw:text-sm tw:text-muted">
        {{ t("orders.pay.order") }}
        <span class="tw:font-mono tw:font-semibold tw:text-white">{{
          payOrder?.orderNumber
        }}</span>
      </p>
      <!-- Amount received -->
      <div class="tw:space-y-1.5">
        <label
          class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted"
          >{{ t("orders.pay.amountReceived") }}</label
        >
        <prime-input-number
          v-model="payAmountReceived"
          :min="0"
          :use-grouping="true"
          :placeholder="String(payOrder?.totalAmount ?? '')"
          class="app-input tw:w-full"
          suffix=" ₫"
          @input="(e) => (payAmountReceived = e.value)"
        />
        <!-- Short -->
        <div
          v-if="payChange !== null && payChange < 0"
          class="tw:flex tw:items-center tw:justify-between tw:text-sm tw:pt-0.5"
        >
          <span class="tw:text-muted">{{ t("orders.pay.short") }}</span>
          <span class="tw:text-red-400 tw:font-semibold">{{
            formatVnd(Math.abs(payChange))
          }}</span>
        </div>

        <!-- Change → tip + return -->
        <template v-if="payChange !== null && payChange > 0">
          <div
            class="tw:flex tw:items-center tw:justify-between tw:text-sm tw:pt-0.5"
          >
            <span class="tw:text-muted">{{ t("orders.pay.change") }}</span>
            <span class="tw:font-semibold">{{ formatVnd(payChange) }}</span>
          </div>
          <div class="tw:space-y-1">
            <label
              for="tip"
              class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted"
              >{{ t("orders.pay.tip") }}</label
            >
            <div class="tw:flex tw:gap-2">
              <prime-input-number
                id="tip"
                v-model="payTip"
                :min="0"
                :max="payChange"
                :use-grouping="true"
                class="app-input tw:flex-1"
                suffix=" ₫"
                @input="(e) => (payTip = e.value ?? 0)"
              />
              <prime-button
                severity="secondary"
                outlined
                v-tooltip.top="t('orders.pay.keepAllAsTip')"
                @click="payTip = payChange"
              >
                <iconify icon="ph:heart-bold" />
              </prime-button>
            </div>
          </div>
          <div class="tw:flex tw:items-center tw:justify-between tw:text-sm">
            <span class="tw:text-muted">{{
              t("orders.pay.returnToCustomer")
            }}</span>
            <span
              :class="
                payReturn === 0
                  ? 'tw:text-muted'
                  : 'tw:text-emerald-400 tw:font-semibold'
              "
            >
              {{ payReturn === 0 ? "—" : formatVnd(payReturn) }}
            </span>
          </div>
        </template>
      </div>

      <div class="tw:space-y-2">
        <label
          class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted"
          >{{ t("orders.pay.paymentMethod") }}</label
        >
        <prime-select-button
          v-model="payMethod"
          :options="PAYMENT_METHODS"
          option-label="label"
          option-value="value"
          :pt="{
            root: { class: 'tw:flex tw:w-full' },
            button: { class: 'tw:flex-1 tw:justify-center' },
          }"
        >
          <template #option="{ option }">
            <iconify :icon="option.icon" class="tw:mr-1.5" />
            <span>{{ option.label }}</span>
          </template>
        </prime-select-button>
      </div>
    </div>
    <template #footer>
      <prime-button severity="secondary" outlined @click="payDialog = false">{{
        t("orders.cancel")
      }}</prime-button>
      <prime-button
        severity="success"
        :loading="payLoading"
        @click="confirmPayment"
      >
        <iconify icon="ph:check-bold" />
        <span>{{ t("orders.pay.confirmPayment") }}</span>
      </prime-button>
    </template>
  </prime-dialog>

  <!-- Undo toast -->
  <prime-toast group="order-move" position="bottom-right">
    <template #message="{ message }">
      <div class="tw:flex tw:items-center tw:gap-3 tw:w-full">
        <iconify
          icon="ph:arrows-left-right-bold"
          class="tw:text-lg tw:shrink-0 tw:text-blue-400"
        />
        <div class="tw:flex-1 tw:min-w-0">
          <p class="tw:text-sm tw:font-semibold">{{ message.summary }}</p>
          <p class="tw:text-xs tw:text-muted">{{ message.detail }}</p>
        </div>
        <prime-button
          :label="t('orders.kanban.undo')"
          size="small"
          severity="secondary"
          outlined
          class="tw:shrink-0"
          @click="undoMove(message.data.orderId)"
        />
      </div>
    </template>
  </prime-toast>

  <section class="tw:space-y-6">
    <!-- Header -->
    <page-header>
      <template #subtitle>
        <p class="tw:text-sm tw:text-muted tw:flex tw:items-center tw:gap-1.5">
          <span
            class="tw:inline-block tw:h-2 tw:w-2 tw:rounded-full tw:shrink-0 tw:transition-colors"
            :class="
              sseConnected
                ? 'tw:bg-emerald-400'
                : 'tw:bg-amber-400 tw:animate-pulse'
            "
          />
          {{
            sseConnected
              ? t("orders.kanban.live")
              : t("orders.kanban.connecting")
          }}
        </p>
      </template>
      <template #default>
        <!-- New Order -->
        <prime-button
          severity="success"
          size="small"
          class="tw:h-8!"
          @click="router.push({ name: 'ordersCreate' })"
        >
          <iconify icon="ph:plus-bold" class="tw:mr-1" />
          <span>{{ t("orders.newOrder") }}</span>
        </prime-button>
        <!-- Widget settings -->
        <widget-settings-button
          :widgets="wDefs"
          :hidden-count="wHidden"
          :cols-per-row="wCols"
          @toggle="wToggle"
          @update:cols-per-row="wSetCols"
        />
        <!-- Refresh -->
        <prime-button
          severity="secondary"
          outlined
          size="small"
          :loading="loading"
          @click="loadOrders"
          class="tw:h-8!"
        >
          <iconify icon="ph:arrows-clockwise-bold" class="tw:mr-1" />
          <span>{{ t("orders.refresh") }}</span>
        </prime-button>
      </template>
    </page-header>

    <!-- Mobile compact stats (replaces widgets on mobile) -->
    <div class="tw:flex tw:md:hidden tw:items-center tw:gap-3 tw:rounded-xl tw:border tw:border-white/10 tw:bg-white/5 tw:px-4 tw:py-3">
      <div class="tw:shrink-0">
        <span class="tw:text-xl tw:font-bold">{{ summary.total }}</span>
        <span class="tw:text-muted tw:text-xs tw:ml-1.5">{{ t('orders.breadcrumb').toLowerCase() }}</span>
      </div>
      <div class="tw:h-4 tw:w-px tw:bg-white/10 tw:shrink-0" />
      <div class="tw:flex tw:flex-wrap tw:gap-x-3 tw:gap-y-0.5">
        <span v-if="summary.pending > 0"    class="tw:text-amber-400  tw:text-xs tw:font-medium">{{ summary.pending }}  {{ t('orders.status.PENDING').toLowerCase() }}</span>
        <span v-if="summary.processing > 0" class="tw:text-blue-400   tw:text-xs tw:font-medium">{{ summary.processing }} {{ t('orders.status.PROCESSING').toLowerCase() }}</span>
        <span v-if="summary.completed > 0"  class="tw:text-emerald-400 tw:text-xs tw:font-medium">{{ summary.completed }} {{ t('orders.status.COMPLETED').toLowerCase() }}</span>
        <span v-if="summary.cancelled > 0"  class="tw:text-red-400    tw:text-xs tw:font-medium">{{ summary.cancelled }} {{ t('orders.status.CANCELLED').toLowerCase() }}</span>
      </div>
      <span class="tw:ml-auto tw:font-semibold tw:text-sm tw:shrink-0">{{ formatVnd(summary.revenue) }}</span>
    </div>

    <!-- Summary stats (desktop only) -->
    <div :class="['tw:hidden tw:md:grid tw:gap-3', wColsClass]">
      <widget-orders-summary
        v-if="wVisible('summary')"
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
      />
    </div>

    <!-- Error -->
    <prime-message
      v-if="errorMessage"
      severity="error"
      size="small"
      variant="simple"
      :closable="true"
      @close="errorMessage = ''"
      >{{ errorMessage }}</prime-message
    >

    <!-- Mobile tab strip -->
    <div class="tw:md:hidden tw:flex tw:gap-1.5 tw:overflow-x-auto tw:pb-0.5" style="scrollbar-width:none">
      <button
        v-for="col in STATUSES"
        :key="col.key"
        @click="activeTab = col.key"
        :class="[
          'tw:flex tw:items-center tw:gap-1.5 tw:rounded-full tw:px-3 tw:py-1.5 tw:text-xs tw:font-semibold tw:whitespace-nowrap tw:shrink-0 tw:border tw:transition-colors tw:cursor-pointer tw:bg-transparent',
          activeTab === col.key ? col.bg + ' ' + col.color : 'tw:border-white/15 tw:text-muted'
        ]"
      >
        <span
          class="tw:inline-block tw:h-1.5 tw:w-1.5 tw:rounded-full tw:shrink-0"
          :class="activeTab === col.key ? col.dot : 'tw:bg-white/25'"
        />
        {{ t(`orders.status.${col.key}`) }}
        <span
          v-if="ordersByStatus[col.key].length > 0"
          class="tw:rounded-full tw:px-1.5 tw:text-[10px] tw:font-bold"
          :class="activeTab === col.key ? 'tw:bg-white/20' : 'tw:bg-white/10'"
        >{{ ordersByStatus[col.key].length }}</span>
      </button>
    </div>

    <!-- Kanban board -->
    <div
      class="tw:flex tw:gap-4 tw:overflow-x-auto tw:pb-2"
    >
      <div
        v-for="col in STATUSES"
        :key="col.key"
        class="tw:flex tw:flex-col tw:gap-3 tw:min-w-72 tw:flex-1"
      >
        <!-- Column header -->
        <div
          class="tw:flex tw:items-center tw:gap-2 tw:rounded-xl tw:border tw:px-4 tw:py-3"
          :class="col.bg"
        >
          <span
            class="tw:inline-block tw:h-2 tw:w-2 tw:rounded-full tw:shrink-0"
            :class="col.dot"
          />
          <span class="tw:font-semibold tw:text-sm" :class="col.color">{{
            t(`orders.status.${col.key}`, col.key)
          }}</span>
          <span
            class="tw:ml-auto tw:rounded-full tw:px-2 tw:py-0.5 tw:text-xs tw:font-medium"
            :class="col.bg + ' ' + col.color"
            >{{ ordersByStatus[col.key].length }}</span
          >
        </div>

        <!-- Cards -->
        <div
          class="tw:flex tw:flex-col tw:gap-3 tw:min-h-80 tw:rounded-xl tw:transition-colors tw:p-3"
          :class="dragOverCol === col.key ? col.bg : ''"
          @dragover="handleDragOver($event, col.key)"
          @dragleave="handleDragleave"
          @drop.prevent="handleDrop(col.key)"
        >
          <!-- Loading skeleton -->
          <template v-if="loading && orders.length === 0">
            <prime-card
              v-for="i in 2"
              :key="i"
              :pt="{
                root: {
                  class: `${appCard} ${cardRing} tw:p-4 tw:animate-pulse tw:space-y-3`,
                },
              }"
            >
              <template #content>
                <prime-skeleton
                  v-for="i in 4"
                  class="tw:h-3 tw:rounded tw:bg-white/10 tw:w-3/4 tw:mb-2"
                />
              </template>
            </prime-card>
          </template>

          <!-- Empty state -->
          <prime-card
            v-else-if="!loading && ordersByStatus[col.key].length === 0"
            :pt="{
              root: { class: `${appCard} tw:border-dashed! tw:py-10 ` },
            }"
          >
            <template #content>
              <div
                class="tw:flex tw:flex-col tw:items-center tw:justify-center tw:text-muted tw:text-sm tw:text-center tw:opacity-50"
              >
                <iconify icon="ph:tray-bold" class="tw:text-2xl tw:mb-2" />
                <span>{{ t("orders.kanban.noOrders") }}</span>
              </div>
            </template>
          </prime-card>

          <!-- Order cards -->
          <prime-card
            v-for="order in ordersByStatus[col.key]"
            :key="order.id"
            :class="[
              updatingId === order.id ? 'tw:opacity-50' : '',
              draggingOrder?.id === order.id ? 'tw:opacity-40 tw:scale-95' : '',
              col.key === ORDER_STATUS.PENDING ||
              col.key === ORDER_STATUS.PROCESSING
                ? 'tw:cursor-grab active:tw:cursor-grabbing'
                : '',
            ]"
            :draggable="
              col.key === ORDER_STATUS.PENDING ||
              col.key === ORDER_STATUS.PROCESSING
            "
            @dragstart="handleDragStart(order)"
            @dragend="handleDragEnd"
            :pt="{
              root: {
                class: `${appCard} ${cardRing} tw:p-4 tw:space-y-3 tw:transition-all`,
              },
              body: { class: 'tw:p-0!' },
            }"
          >
            <template #header>
              <div class="tw:flex tw:items-start tw:justify-between tw:gap-2">
                <div>
                  <p
                    class="tw:text-xs tw:font-mono tw:font-semibold"
                    :class="col.color"
                  >
                    {{ order.orderNumber }}
                  </p>
                  <p class="tw:text-xs tw:text-muted tw:mt-0.5">
                    {{ timeAgo(order.orderDate) }}
                  </p>
                </div>
                <div class="tw:flex tw:flex-col tw:items-end tw:gap-1">
                  <p class="tw:text-sm tw:font-semibold tw:whitespace-nowrap">
                    {{ formatVnd(order.totalAmount) }}
                  </p>
                  <prime-tag
                    :value="
                      paymentTag(order.paymentStatus, order.paymentMethod).label
                    "
                    :severity="
                      paymentTag(order.paymentStatus, order.paymentMethod)
                        .severity
                    "
                    class="tw:text-[10px]"
                  />
                </div>
              </div>
            </template>
            <template #content>
              <!-- Order header -->

              <!-- Items -->
              <ul class="tw:space-y-1">
                <li
                  v-for="item in order.items"
                  :key="item.productId"
                  class="tw:flex tw:items-center tw:justify-between tw:text-xs tw:text-muted"
                >
                  <span class="tw:truncate tw:max-w-30">{{
                    item.productName
                  }}</span>
                  <span class="tw:font-medium tw:shrink-0"
                    >×{{ item.quantity }}</span
                  >
                </li>
              </ul>

              <!-- Actions -->
              <div class="tw:flex tw:flex-col tw:gap-2 tw:pt-1">
                <div class="tw:flex tw:gap-2">
                  <prime-button
                    v-if="NEXT_STATUS[col.key]"
                    :label="NEXT_LABEL[col.key]"
                    severity="success"
                    size="small"
                    class="tw:flex-1"
                    :loading="updatingId === order.id"
                    @click="moveOrder(order, NEXT_STATUS[col.key])"
                  />
                  <prime-button
                    v-if="
                      col.key === ORDER_STATUS.PENDING ||
                      col.key === ORDER_STATUS.PROCESSING
                    "
                    severity="danger"
                    size="small"
                    outlined
                    :class="NEXT_STATUS[col.key] ? btnIcon : 'tw:flex-1'"
                    v-tooltip.top="NEXT_STATUS[col.key] ? t('orders.cancel') : ''"
                    :loading="updatingId === order.id"
                    @click="
                      (e) =>
                        confirm.require({
                          target: e.currentTarget,
                          message: t('orders.kanban.cancelTitle', {
                            orderNumber: order.orderNumber,
                          }),
                          icon: 'ph:warning-bold',
                          rejectProps: {
                            label: t('orders.kanban.keepOrder'),
                            severity: 'secondary',
                            outlined: true,
                            size: 'small',
                          },
                          acceptProps: {
                            label: t('orders.kanban.confirmCancel'),
                            severity: 'danger',
                            size: 'small',
                          },
                          accept: () => cancelOrder(order),
                        })
                    "
                  >
                    <iconify icon="ph:x-circle" />
                    <span v-if="!NEXT_STATUS[col.key]">{{ t("orders.cancel") }}</span>
                  </prime-button>
                </div>
                <prime-button
                  v-if="
                    order.paymentStatus === PAYMENT_STATUS.UNPAID &&
                    col.key !== ORDER_STATUS.CANCELLED
                  "
                  severity="warn"
                  size="small"
                  outlined
                  class="tw:w-full"
                  @click="openPayDialog(order)"
                >
                  <iconify icon="ph:money-bold" />
                  <span>{{ t("orders.kanban.markPaid") }}</span>
                </prime-button>
                <prime-button
                  severity="secondary"
                  size="small"
                  class="tw:w-full"
                  @click="
                    router.push({
                      name: 'ordersDetail',
                      params: { id: order.id },
                    })
                  "
                >
                  <iconify icon="ph:arrow-square-out-bold" />
                  <span>{{ t("orders.kanban.viewDetail") }}</span>
                </prime-button>
              </div>
            </template>
          </prime-card>
        </div>
      </div>
    </div>
  </section>
</template>
