<script setup>
import { useRouter } from "vue-router";
import { getOrders, updatePayment } from "@/services/order.service";
import AppTable from "@/components/AppTable.vue";
import { btnIcon } from "@/layout/ui";
import { ORDER_STATUS, ORDER_STATUS_MAP } from "@/constants/orderStatus";
import { PAYMENT_STATUS, PAYMENT_STATUS_MAP } from "@/constants/paymentStatus";
import { PAYMENT_METHOD, PAYMENT_METHOD_MAP } from "@/constants/paymentMethod";

const router = useRouter();

// ── Table state cache ─────────────────────────────────────────────
const { save: saveCache, restore: restoreCache } = useTableCache("orders-list");

// ── Data ──────────────────────────────────────────────────────────
const orders = ref([]);             // current page only
const totalRecords = ref(0);        // from server
const cashTotal = ref(0);           // tổng tiền mặt đã thu (PAID + CASH)
const bankTransferTotal = ref(0);   // tổng chuyển khoản đã thu (PAID + BANK_TRANSFER)
const loading = ref(false);
const errorMessage = ref("");

// ── Pagination (server-side) ───────────────────────────────────────
const rows = ref(20);
const first = ref(0);

// ── Filters ───────────────────────────────────────────────────────
const todayMidnight = () => {
  const d = new Date();
  d.setHours(0, 0, 0, 0);
  return d;
};

// Server-side: trigger API reload
const dateFrom = ref(todayMidnight());  // mặc định: hôm nay
const dateTo = ref(todayMidnight());    // mặc định: hôm nay
const statusFilter = ref(null);

// Client-side: filter within current page
const searchOrder = ref("");
const paymentStatusFilter = ref(null);
const minTotal = ref(null);
const maxTotal = ref(null);
const tableCodeFilter = ref("");

// ── Restore cached state (trước khi watchers kích hoạt) ──────────
const _cached = restoreCache();
if (_cached) {
  if (_cached.rows !== undefined)              rows.value              = _cached.rows;
  if (_cached.first !== undefined)             first.value             = _cached.first;
  if (_cached.statusFilter !== undefined)      statusFilter.value      = _cached.statusFilter;
  if (_cached.paymentStatusFilter !== undefined) paymentStatusFilter.value = _cached.paymentStatusFilter;
  if (_cached.searchOrder !== undefined)       searchOrder.value       = _cached.searchOrder;
  if (_cached.tableCodeFilter !== undefined)   tableCodeFilter.value   = _cached.tableCodeFilter;
  if (_cached.minTotal !== undefined)          minTotal.value          = _cached.minTotal;
  if (_cached.maxTotal !== undefined)          maxTotal.value          = _cached.maxTotal;
  // Date được lưu dạng ISO string → khôi phục thành Date object
  if (_cached.dateFrom) dateFrom.value = new Date(_cached.dateFrom);
  if (_cached.dateTo)   dateTo.value   = new Date(_cached.dateTo);
}

const filterPanel = ref(null);

const statusOptions = Object.entries(ORDER_STATUS_MAP).map(([value, meta]) => ({
  label: meta.label,
  value,
}));

const paymentStatusOptions = Object.entries(PAYMENT_STATUS_MAP).map(([value, meta]) => ({
  label: meta.label,
  value,
}));

const activeFilterCount = computed(() => {
  let n = 0;
  if (dateFrom.value) n++;
  if (dateTo.value) n++;
  if (statusFilter.value !== null) n++;
  if (paymentStatusFilter.value !== null) n++;
  if (minTotal.value !== null) n++;
  if (maxTotal.value !== null) n++;
  if (tableCodeFilter.value.trim()) n++;
  return n;
});

const hasActiveFilters = computed(() => activeFilterCount.value > 0);

const clearFilters = () => {
  dateFrom.value = todayMidnight();
  dateTo.value = todayMidnight();
  statusFilter.value = null;
  paymentStatusFilter.value = null;
  minTotal.value = null;
  maxTotal.value = null;
  tableCodeFilter.value = "";
  searchOrder.value = "";
  first.value = 0;
  // server-side filter changes are watched and will reload + re-save cache
};

const summary = computed(() => ({
  total: totalRecords.value,
  cash: cashTotal.value,
  bank: bankTransferTotal.value,
}));

// ── Helpers ───────────────────────────────────────────────────────
const formatVnd = (value) =>
  new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
    maximumFractionDigits: 0,
  }).format(value ?? 0);

const formatDate = (dateStr) =>
  new Intl.DateTimeFormat("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  }).format(new Date(dateStr));

const statusTag = (status) =>
  ORDER_STATUS_MAP[status] ?? { severity: "secondary", label: status };

const paymentTag = (status, method) => {
  if (status === PAYMENT_STATUS.PAID) {
    const m = PAYMENT_METHOD_MAP[method]?.label ?? "";
    return { label: m ? `Paid · ${m}` : "Paid", severity: "success" };
  }
  return PAYMENT_STATUS_MAP[status] ?? { label: "Unpaid", severity: "warn" };
};

// ── Load ──────────────────────────────────────────────────────────
const saveCurrentState = () => {
  saveCache({
    rows: rows.value,
    first: first.value,
    statusFilter: statusFilter.value,
    paymentStatusFilter: paymentStatusFilter.value,
    searchOrder: searchOrder.value,
    tableCodeFilter: tableCodeFilter.value,
    minTotal: minTotal.value,
    maxTotal: maxTotal.value,
    dateFrom: dateFrom.value?.toISOString?.() ?? dateFrom.value,
    dateTo: dateTo.value?.toISOString?.() ?? dateTo.value,
  });
};

const loadOrders = async () => {
  loading.value = true;
  errorMessage.value = "";
  try {
    const page = Math.floor(first.value / rows.value) + 1;
    const res = await getOrders({
      status: statusFilter.value || undefined,
      paymentStatus: paymentStatusFilter.value || undefined,
      orderNumber: searchOrder.value.trim() || undefined,
      minAmount: minTotal.value ?? undefined,
      maxAmount: maxTotal.value ?? undefined,
      tableCode: tableCodeFilter.value.trim() || undefined,
      page,
      pageSize: rows.value,
      dateFrom: dateFrom.value ?? undefined,
      dateTo: dateTo.value ?? undefined,
    });
    const data = res?.data;
    orders.value = data?.items ?? [];
    totalRecords.value = data?.totalCount ?? 0;
    cashTotal.value = data?.cashTotal ?? 0;
    bankTransferTotal.value = data?.bankTransferTotal ?? 0;
    saveCurrentState();
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.message || "Failed to load orders.";
  } finally {
    loading.value = false;
  }
};

onMounted(loadOrders);

// Re-fetch khi bất kỳ filter nào thay đổi — reset về trang 1
watch(
  [statusFilter, paymentStatusFilter, dateFrom, dateTo, searchOrder, tableCodeFilter, minTotal, maxTotal],
  () => {
    first.value = 0;
    loadOrders();
  },
);

// Handle server-side page change from AppTable
const onPage = (e) => {
  first.value = e.first;
  rows.value = e.rows;
  loadOrders();
};

// ── Payment dialog ────────────────────────────────────────────────
const payDialog = ref(false);
const payOrder = ref(null);
const payMethod = ref(PAYMENT_METHOD.CASH);
const payAmountReceived = ref(null);
const payLoading = ref(false);

const PAYMENT_METHODS = [
  { label: "Cash", value: PAYMENT_METHOD.CASH, icon: "ph:money-bold" },
  { label: "Bank Transfer", value: PAYMENT_METHOD.BANK_TRANSFER, icon: "ph:bank-bold" },
];

const payChange = computed(() => {
  if (payAmountReceived.value == null || !payOrder.value) return null;
  return payAmountReceived.value - payOrder.value.totalAmount;
});

const payTip = ref(0);
const payReturn = computed(() => {
  if (payChange.value === null || payChange.value <= 0) return null;
  return payChange.value - (payTip.value ?? 0);
});

watch(payChange, (val) => {
  if (val !== null && val >= 0 && payTip.value > val) payTip.value = val;
});

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
</script>

<template>
  <!-- Payment dialog -->
  <prime-dialog
    v-model:visible="payDialog"
    header="Mark as Paid"
    :modal="true"
    :style="{ width: '22rem' }"
  >
    <div class="tw:space-y-4">
      <p class="tw:text-sm app-text-muted">
        Order
        <span class="tw:font-mono tw:font-semibold tw:text-white">{{
          payOrder?.orderNumber
        }}</span>
      </p>
      <!-- Amount received -->
      <div class="tw:space-y-1.5">
        <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted"
          >Amount received</label
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
        <div
          v-if="payChange !== null && payChange < 0"
          class="tw:flex tw:items-center tw:justify-between tw:text-sm tw:pt-0.5"
        >
          <span class="app-text-muted">Short</span>
          <span class="tw:text-red-400 tw:font-semibold">{{
            formatVnd(Math.abs(payChange))
          }}</span>
        </div>
        <template v-if="payChange !== null && payChange > 0">
          <div
            class="tw:flex tw:items-center tw:justify-between tw:text-sm tw:pt-0.5"
          >
            <span class="app-text-muted">Change</span>
            <span class="tw:font-semibold">{{ formatVnd(payChange) }}</span>
          </div>
          <div class="tw:space-y-1">
            <label
              class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted"
              >Tip</label
            >
            <div class="tw:flex tw:gap-2">
              <prime-input-number
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
                v-tooltip.top="'Keep all as tip'"
                @click="payTip = payChange"
              >
                <iconify icon="ph:heart-bold" />
              </prime-button>
            </div>
          </div>
          <div class="tw:flex tw:items-center tw:justify-between tw:text-sm">
            <span class="app-text-muted">Return to customer</span>
            <span
              :class="
                payReturn === 0
                  ? 'app-text-muted'
                  : 'tw:text-emerald-400 tw:font-semibold'
              "
            >
              {{ payReturn === 0 ? "—" : formatVnd(payReturn) }}
            </span>
          </div>
        </template>
      </div>
      <div class="tw:space-y-2">
        <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted"
          >Payment method</label
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
      <prime-button severity="secondary" outlined @click="payDialog = false"
        >Cancel</prime-button
      >
      <prime-button
        severity="success"
        :loading="payLoading"
        @click="confirmPayment"
      >
        <iconify icon="ph:check-bold" />
        <span>Confirm payment</span>
      </prime-button>
    </template>
  </prime-dialog>

  <section class="tw:space-y-8">
    <!-- Header -->
    <div class="tw:flex tw:flex-wrap tw:items-end tw:justify-between tw:gap-4">
      <div>
        <p
          class="tw:text-[11px] tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300"
        >
          Orders
        </p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">Order management</h1>
        <p class="tw:mt-2 tw:text-sm app-text-muted">
          Full order history with payment status.
        </p>
      </div>
      <div class="tw:flex tw:items-center tw:gap-2">
        <!-- New Order -->
        <prime-button
          severity="success"
          size="small"
          @click="router.push({ name: 'ordersCreate' })"
        >
          <iconify icon="ph:plus-bold" class="tw:mr-1" />
          <span>New Order</span>
        </prime-button>
        <!-- View toggle -->
        <div
          class="tw:flex tw:items-center tw:rounded-lg tw:border tw:border-white/10 tw:p-1 tw:gap-1"
        >
          <prime-button
            severity="secondary"
            text
            size="small"
            v-tooltip.top="'Kanban'"
            :class="btnIcon"
            @click="router.push({ name: 'orders' })"
          >
            <iconify icon="ph:kanban-bold" />
          </prime-button>
          <prime-button severity="primary" size="small" v-tooltip.top="'List'" :class="btnIcon">
            <iconify icon="ph:list-bold" />
          </prime-button>
        </div>
        <!-- Refresh -->
        <prime-button
          severity="secondary"
          outlined
          size="small"
          :loading="loading"
          @click="loadOrders"
        >
          <iconify icon="ph:arrows-clockwise-bold" class="tw:mr-1" />
          <span>Refresh</span>
        </prime-button>
      </div>
    </div>

    <!-- Summary stats -->
    <div class="tw:grid tw:grid-cols-1 tw:gap-3 tw:sm:grid-cols-3">
      <prime-card class="app-card tw:rounded-xl tw:border">
        <template #content>
          <p class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] app-text-subtle">
            Total orders
          </p>
          <p class="tw:mt-2 tw:text-2xl tw:font-semibold">
            {{ summary.total }}
          </p>
        </template>
      </prime-card>
      <prime-card class="app-card tw:rounded-xl tw:border">
        <template #content>
          <div class="tw:flex tw:items-center tw:justify-between">
            <p class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] tw:text-emerald-400">
              Cash collected
            </p>
            <iconify icon="ph:money-bold" class="tw:text-emerald-400 tw:opacity-60" />
          </div>
          <p class="tw:mt-2 tw:text-2xl tw:font-semibold">
            {{ formatVnd(summary.cash) }}
          </p>
        </template>
      </prime-card>
      <prime-card class="app-card tw:rounded-xl tw:border">
        <template #content>
          <div class="tw:flex tw:items-center tw:justify-between">
            <p class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] tw:text-blue-400">
              Bank transfer
            </p>
            <iconify icon="ph:bank-bold" class="tw:text-blue-400 tw:opacity-60" />
          </div>
          <p class="tw:mt-2 tw:text-2xl tw:font-semibold">
            {{ formatVnd(summary.bank) }}
          </p>
        </template>
      </prime-card>
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

    <!-- Table -->
    <AppTable
      lazy
      v-model:first="first"
      v-model:rows="rows"
      :value="orders"
      :loading="loading"
      :totalRecords="totalRecords"
      :rowsPerPageOptions="[10, 20, 50]"
      @page="onPage"
    >
      <template #toolbar-left>
        <div class="tw:flex tw:items-center tw:gap-2">
          <!-- Search by order number (server-side) -->
          <prime-input-text
            v-model="searchOrder"
            placeholder="Search order #…"
            class="app-input tw:w-48"
          />

          <!-- Filter toggle button -->
          <prime-button
            :severity="hasActiveFilters ? 'success' : 'secondary'"
            :outlined="!hasActiveFilters"
            v-tooltip.top="'Filters'"
            @click="filterPanel.toggle($event)"
            :class="!hasActiveFilters ? btnIcon : ''"
          >
            <iconify icon="ph:funnel-bold" />
            <span>Filters</span>
            <prime-badge
              v-if="activeFilterCount > 0"
              :value="activeFilterCount"
              severity="danger"
              class="tw:ml-1 tw:scale-90"
            />
          </prime-button>

          <!-- Filter popover -->
          <prime-popover ref="filterPanel">
            <div class="tw:flex tw:flex-col tw:gap-4 tw:w-full">
              <p class="tw:text-sm tw:font-semibold">Filter orders</p>

              <!-- Date range (server-side) -->
              <div class="tw:space-y-1.5">
                <label
                  for="dateFrom"
                  class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest"
                  >Date range</label
                >
                <div class="tw:flex tw:items-center tw:gap-2">
                  <prime-date-picker
                    id="dateFrom"
                    v-model="dateFrom"
                    placeholder="From"
                    date-format="dd/mm/yy"
                    show-button-bar
                    class="app-input tw:flex-1"
                  />
                  <span class="app-text-muted tw:text-sm">–</span>
                  <prime-date-picker
                    v-model="dateTo"
                    placeholder="To"
                    date-format="dd/mm/yy"
                    show-button-bar
                    class="app-input tw:flex-1"
                  />
                </div>
              </div>

              <!-- Order status (server-side) -->
              <div class="tw:space-y-1.5">
                <label
                  class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest"
                  >Order status</label
                >
                <prime-select
                  v-model="statusFilter"
                  :options="statusOptions"
                  option-label="label"
                  option-value="value"
                  placeholder="All statuses"
                  show-clear
                  class="app-input tw:w-full"
                />
              </div>

              <!-- Payment status (server-side) -->
              <div class="tw:space-y-1.5">
                <label
                  class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest"
                  >Payment status</label
                >
                <prime-select
                  v-model="paymentStatusFilter"
                  :options="paymentStatusOptions"
                  option-label="label"
                  option-value="value"
                  placeholder="All payments"
                  show-clear
                  class="app-input tw:w-full"
                />
              </div>

              <!-- Total price range (server-side) -->
              <div class="tw:space-y-1.5">
                <label
                  class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest"
                  >Total (VND)</label
                >
                <div class="tw:flex tw:items-center tw:gap-2">
                  <prime-input-number
                    v-model="minTotal"
                    placeholder="Min"
                    :min="0"
                    :use-grouping="true"
                    class="app-input tw:flex-1"
                  />
                  <span class="app-text-muted tw:text-sm">–</span>
                  <prime-input-number
                    v-model="maxTotal"
                    placeholder="Max"
                    :min="0"
                    :use-grouping="true"
                    class="app-input tw:flex-1"
                  />
                </div>
              </div>

              <!-- Table code (server-side) -->
              <div class="tw:space-y-1.5">
                <label
                  class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest"
                  >Table</label
                >
                <prime-input-text
                  v-model="tableCodeFilter"
                  placeholder="Table code…"
                  class="app-input tw:w-full"
                />
              </div>

              <!-- Clear -->
              <prime-button
                v-if="hasActiveFilters"
                severity="danger"
                outlined
                size="small"
                @click="clearFilters"
              >
                <iconify icon="ph:x-bold" />
                <span>Clear filters</span>
              </prime-button>
            </div>
          </prime-popover>
        </div>
      </template>

      <prime-column
        field="orderNumber"
        header="Order #"
        style="min-width: 9rem"
      >
        <template #body="{ data }">
          <span class="tw:font-mono tw:text-sm tw:font-semibold">{{
            data.orderNumber
          }}</span>
        </template>
      </prime-column>

      <prime-column field="orderDate" header="Date" style="min-width: 10rem">
        <template #body="{ data }">
          <span class="tw:text-sm app-text-muted">{{
            formatDate(data.orderDate)
          }}</span>
        </template>
      </prime-column>

      <prime-column field="status" header="Status" style="min-width: 8rem">
        <template #body="{ data }">
          <prime-tag
            :value="statusTag(data.status).label"
            :severity="statusTag(data.status).severity"
          />
        </template>
      </prime-column>

      <prime-column
        field="paymentStatus"
        header="Payment"
        style="min-width: 10rem"
      >
        <template #body="{ data }">
          <prime-tag
            :value="paymentTag(data.paymentStatus, data.paymentMethod).label"
            :severity="
              paymentTag(data.paymentStatus, data.paymentMethod).severity
            "
          />
        </template>
      </prime-column>

      <prime-column header="Items" style="min-width: 5rem">
        <template #body="{ data }">
          <span class="tw:text-sm app-text-muted"
            >{{ data.items?.length ?? 0 }} item(s)</span
          >
        </template>
      </prime-column>

      <prime-column field="totalAmount" header="Total" style="min-width: 8rem">
        <template #body="{ data }">
          <span class="tw:font-semibold tw:text-sm">{{
            formatVnd(data.totalAmount)
          }}</span>
        </template>
      </prime-column>

      <prime-column header="Actions" style="min-width: 12rem">
        <template #body="{ data }">
          <div class="tw:flex tw:justify-end tw:items-center tw:gap-2">
            <prime-button
              v-if="
                data.paymentStatus === PAYMENT_STATUS.UNPAID && data.status !== ORDER_STATUS.CANCELLED
              "
              severity="warn"
              size="small"
              outlined
              @click="openPayDialog(data)"
            >
              <iconify icon="ph:money-bold" />
              <span>Mark paid</span>
            </prime-button>
            <prime-button
              severity="secondary"
              outlined
              size="small"
              @click="
                router.push({ name: 'ordersDetail', params: { id: data.id } })
              "
              :class="btnIcon"
            >
              <iconify icon="ph:arrow-right-bold" />
            </prime-button>
          </div>
        </template>
      </prime-column>
    </AppTable>
  </section>
</template>
