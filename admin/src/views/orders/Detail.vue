<script setup>
import { computed, onMounted, ref, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useToast } from "primevue/usetoast";
import { useConfirm } from "primevue/useconfirm";
import {
  getOrderById,
  getOrders,
  updateOrderStatus,
  updatePayment,
  mergeOrders,
  splitOrder,
  updateOrderItem,
} from "@/services/order.service";
import { getProducts } from "@/services/product.service";

const route = useRoute();
const router = useRouter();
const toast = useToast();
const confirm = useConfirm();
const orderId = Number(route.params.id);

// ── State ──────────────────────────────────────────────────────────
const order = ref(null);
const loading = ref(false);
const errorMessage = ref("");
const updatingId = ref(null);

// ── Helpers ────────────────────────────────────────────────────────
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

const statusTag = (status) => {
  switch (status) {
    case "Pending":
      return { severity: "warn", label: "Pending" };
    case "Processing":
      return { severity: "info", label: "Processing" };
    case "Completed":
      return { severity: "success", label: "Completed" };
    case "Cancelled":
      return { severity: "danger", label: "Cancelled" };
    default:
      return { severity: "secondary", label: status };
  }
};

const methodLabel = (method) => {
  if (method === "BankTransfer") return "Bank Transfer";
  if (method === "Cash") return "Cash";
  return "";
};

const paymentTag = (status, method) => {
  switch (status) {
    case "Paid": {
      const m = methodLabel(method);
      return { label: m ? `Paid · ${m}` : "Paid", severity: "success" };
    }
    case "Refunded":
      return { label: "Refunded", severity: "secondary" };
    case "Voided":
      return { label: "Voided", severity: "danger" };
    default:
      return { label: "Unpaid", severity: "warn" };
  }
};

const NEXT_STATUS = { Pending: "Processing", Processing: "Completed" };
const NEXT_LABEL = { Pending: "Start preparing", Processing: "Mark complete" };

const canSplit = computed(() => {
  if (!order.value || order.value.paymentStatus !== "Unpaid") return false;
  const items = order.value.items ?? [];
  return items.length > 1 || (items.length === 1 && items[0].quantity > 1);
});

// Can edit items only when Pending + Unpaid
const canEditItems = computed(
  () =>
    order.value?.status === "Pending" &&
    order.value?.paymentStatus === "Unpaid",
);

// ── Load ──────────────────────────────────────────────────────────
const loadOrder = async () => {
  loading.value = true;
  errorMessage.value = "";
  try {
    const res = await getOrderById(orderId);
    order.value = res?.data;
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.message || "Failed to load order.";
  } finally {
    loading.value = false;
  }
};

onMounted(loadOrder);

// ── Status actions ────────────────────────────────────────────────
const moveOrder = async (toStatus) => {
  updatingId.value = "status";
  errorMessage.value = "";
  try {
    await updateOrderStatus(orderId, toStatus);
    order.value.status = toStatus;
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.errors?.join(", ") || "Failed to update order.";
  } finally {
    updatingId.value = null;
  }
};

const cancelOrder = async () => {
  updatingId.value = "status";
  errorMessage.value = "";
  try {
    await updateOrderStatus(orderId, "Cancelled");
    order.value.status = "Cancelled";
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.errors?.join(", ") || "Failed to cancel order.";
  } finally {
    updatingId.value = null;
  }
};

// ── Payment (inline form) ─────────────────────────────────────────
const payMethod = ref("Cash");
const payAmountReceived = ref(null);
const payTip = ref(0);
const payLoading = ref(false);

const PAYMENT_METHODS = [
  { label: "Cash", value: "Cash", icon: "ph:money-bold" },
  { label: "Bank Transfer", value: "BankTransfer", icon: "ph:bank-bold" },
];

const payChange = computed(() => {
  if (payAmountReceived.value == null || !order.value) return null;
  return payAmountReceived.value - order.value.totalAmount;
});

const payReturn = computed(() => {
  if (payChange.value === null || payChange.value <= 0) return null;
  return payChange.value - (payTip.value ?? 0);
});

watch(payChange, (val) => {
  if (val !== null && val >= 0 && payTip.value > val) payTip.value = val;
});

const confirmPayment = async () => {
  payLoading.value = true;
  errorMessage.value = "";
  try {
    await updatePayment(
      orderId,
      "Paid",
      payMethod.value,
      payAmountReceived.value,
      payTip.value ?? 0,
    );
    order.value.paymentStatus = "Paid";
    order.value.paymentMethod = payMethod.value;
    order.value.amountReceived = payAmountReceived.value;
    order.value.tipAmount = payTip.value ?? 0;
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.errors?.join(", ") ||
      err?.response?.data?.title ||
      "Failed to update payment.";
  } finally {
    payLoading.value = false;
  }
};

// ── Item editing ──────────────────────────────────────────────────
const itemUpdating = ref(null); // productId currently being updated

const setItemQty = async (productId, newQty) => {
  itemUpdating.value = productId;
  errorMessage.value = "";
  try {
    const res = await updateOrderItem(orderId, productId, newQty);
    // Update local order with returned items + total
    order.value.items = res.data.items;
    order.value.totalAmount = res.data.totalAmount;
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.errors?.join(", ") ||
      err?.response?.data?.title ||
      "Failed to update item.";
  } finally {
    itemUpdating.value = null;
  }
};

const removeItem = (item) => {
  confirm.require({
    message: `Remove "${item.productName}" from this order?`,
    icon: "ph:warning-bold",
    rejectProps: { label: "Keep", severity: "secondary", outlined: true, size: "small" },
    acceptProps: { label: "Remove", severity: "danger", size: "small" },
    accept: () => setItemQty(item.productId, 0),
  });
};

// ── Add item dialog ────────────────────────────────────────────────
const addItemDialog = ref(false);
const addItemSearch = ref("");
const addItemProducts = ref([]);
const addItemLoading = ref(false);
const addItemQty = ref(1);
const addItemSelected = ref(null);

const openAddItemDialog = async () => {
  addItemSearch.value = "";
  addItemSelected.value = null;
  addItemQty.value = 1;
  addItemDialog.value = true;
  addItemLoading.value = true;
  try {
    const res = await getProducts({ isActive: true, pageSize: 200 });
    addItemProducts.value = res?.data?.items ?? res?.data ?? [];
  } catch {
    addItemProducts.value = [];
  } finally {
    addItemLoading.value = false;
  }
};

const addItemFiltered = computed(() => {
  const q = addItemSearch.value.trim().toLowerCase();
  return q
    ? addItemProducts.value.filter((p) =>
        p.name.toLowerCase().includes(q),
      )
    : addItemProducts.value;
});

const confirmAddItem = async () => {
  if (!addItemSelected.value) return;
  // If product already in order, add to existing quantity
  const existing = order.value.items.find(
    (i) => i.productId === addItemSelected.value.id,
  );
  const targetQty = (existing?.quantity ?? 0) + (addItemQty.value || 1);
  addItemDialog.value = false;
  await setItemQty(addItemSelected.value.id, targetQty);
};

// ── Merge dialog ──────────────────────────────────────────────────
const mergeDialog = ref(false);
const mergeOrders_ = ref([]); // candidate orders
const mergeLoading = ref(false);
const mergeFetching = ref(false);
const mergeSelected = ref([]); // selected secondary order IDs

const openMergeDialog = async () => {
  mergeSelected.value = [];
  mergeFetching.value = true;
  mergeDialog.value = true;
  try {
    const res = await getOrders();
    const all = res?.data ?? [];
    mergeOrders_.value = all.filter(
      (o) => o.paymentStatus === "Unpaid" && o.id !== orderId,
    );
  } catch {
    mergeOrders_.value = [];
  } finally {
    mergeFetching.value = false;
  }
};

const confirmMerge = async () => {
  if (!mergeSelected.value.length) return;
  mergeLoading.value = true;
  errorMessage.value = "";
  try {
    await mergeOrders(orderId, mergeSelected.value);
    mergeDialog.value = false;
    await loadOrder();
    toast.add({
      severity: "success",
      summary: "Orders merged",
      detail: `${mergeSelected.value.length} order(s) merged into ${order.value?.orderNumber}.`,
      life: 4000,
    });
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.errors?.join(", ") ||
      err?.response?.data?.title ||
      "Failed to merge orders.";
  } finally {
    mergeLoading.value = false;
  }
};

// ── Split dialog ──────────────────────────────────────────────────
const splitDialog = ref(false);
const splitItems = ref([]); // [{ productId, productName, unitPrice, quantity, splitQty }]
const splitLoading = ref(false);

const openSplitDialog = () => {
  splitItems.value = (order.value?.items ?? []).map((item) => ({
    productId: item.productId,
    productName: item.productName,
    unitPrice: item.unitPrice,
    quantity: item.quantity,
    splitQty: 0,
  }));
  splitDialog.value = true;
};

const splitPreview = computed(() => {
  const toNew = splitItems.value.filter((i) => i.splitQty > 0);
  const toNewQty = toNew.reduce((s, i) => s + i.splitQty, 0);
  const keepQty = splitItems.value.reduce(
    (s, i) => s + (i.quantity - i.splitQty),
    0,
  );
  return { toNew, toNewQty, keepQty };
});

const splitValid = computed(() => {
  const { toNewQty, keepQty } = splitPreview.value;
  return toNewQty > 0 && keepQty > 0;
});

const confirmSplit = async () => {
  const items = splitItems.value
    .filter((i) => i.splitQty > 0)
    .map((i) => ({ productId: i.productId, quantity: i.splitQty }));

  splitLoading.value = true;
  errorMessage.value = "";
  try {
    const res = await splitOrder(orderId, items);
    const result = res?.data;
    splitDialog.value = false;
    await loadOrder();
    toast.add({
      severity: "success",
      summary: "Order split",
      detail: `New order ${result?.newOrderNumber} created.`,
      life: 6000,
      group: "split-result",
      data: { newOrderId: result?.newOrderId },
    });
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.errors?.join(", ") ||
      err?.response?.data?.title ||
      "Failed to split order.";
  } finally {
    splitLoading.value = false;
  }
};
</script>

<template>
  <prime-confirm-popup />

  <!-- Split result toast with navigate link -->
  <prime-toast group="split-result" position="bottom-right">
    <template #message="{ message }">
      <div class="tw:flex tw:items-center tw:gap-3 tw:w-full">
        <iconify
          icon="ph:scissors-bold"
          class="tw:text-lg tw:shrink-0 tw:text-emerald-400"
        />
        <div class="tw:flex-1 tw:min-w-0">
          <p class="tw:text-sm tw:font-semibold">{{ message.summary }}</p>
          <p class="tw:text-xs app-text-muted">{{ message.detail }}</p>
        </div>
        <prime-button
          label="View"
          size="small"
          severity="secondary"
          outlined
          class="tw:shrink-0"
          @click="
            router.push({
              name: 'ordersDetail',
              params: { id: message.data.newOrderId },
            })
          "
        />
      </div>
    </template>
  </prime-toast>

  <!-- ── Merge dialog ───────────────────────────────────────────── -->
  <prime-dialog
    v-model:visible="mergeDialog"
    header="Merge orders"
    :modal="true"
    :style="{ width: '36rem' }"
  >
    <div class="tw:space-y-3">
      <p class="tw:text-sm app-text-muted">
        Select orders to merge into
        <span class="tw:font-mono tw:font-semibold tw:text-white">{{
          order?.orderNumber
        }}</span
        >. Their items will be added here and they will be cancelled.
      </p>

      <!-- Loading -->
      <div v-if="mergeFetching" class="tw:flex tw:justify-center tw:py-6">
        <prime-progress-spinner style="width: 2rem; height: 2rem" />
      </div>

      <!-- Empty state -->
      <div
        v-else-if="!mergeOrders_.length"
        class="tw:flex tw:flex-col tw:items-center tw:py-8 app-text-muted tw:text-sm"
      >
        <iconify icon="ph:tray-bold" class="tw:text-2xl tw:mb-2" />
        No other Unpaid orders available.
      </div>

      <!-- Order list -->
      <div v-else class="tw:space-y-2 tw:max-h-80 tw:overflow-y-auto tw:pr-1">
        <label
          v-for="o in mergeOrders_"
          :key="o.id"
          class="tw:flex tw:items-center tw:gap-3 tw:rounded-xl tw:border tw:px-4 tw:py-3 tw:cursor-pointer tw:transition-colors app-card"
          :class="
            mergeSelected.includes(o.id)
              ? 'tw:border-emerald-500/50 tw:bg-emerald-500/10'
              : ''
          "
        >
          <input
            type="checkbox"
            class="tw:accent-emerald-500"
            :value="o.id"
            v-model="mergeSelected"
          />
          <div class="tw:flex-1 tw:min-w-0">
            <p class="tw:font-mono tw:font-semibold tw:text-sm">
              {{ o.orderNumber }}
            </p>
            <p class="tw:text-xs app-text-muted">
              {{ o.items?.length ?? 0 }} item(s)
            </p>
          </div>
          <div class="tw:text-right tw:shrink-0">
            <prime-tag
              :value="statusTag(o.status).label"
              :severity="statusTag(o.status).severity"
              class="tw:text-[10px] tw:mb-1"
            />
            <p class="tw:text-sm tw:font-semibold">
              {{ formatVnd(o.totalAmount) }}
            </p>
          </div>
        </label>
      </div>
    </div>

    <template #footer>
      <prime-button severity="secondary" outlined @click="mergeDialog = false"
        >Cancel</prime-button
      >
      <prime-button
        severity="success"
        :loading="mergeLoading"
        :disabled="!mergeSelected.length"
        @click="confirmMerge"
      >
        <iconify icon="ph:git-merge-bold" />
        <span
          >Merge
          {{
            mergeSelected.length > 0 ? `(${mergeSelected.length})` : ""
          }}</span
        >
      </prime-button>
    </template>
  </prime-dialog>

  <!-- ── Split dialog ───────────────────────────────────────────── -->
  <prime-dialog
    v-model:visible="splitDialog"
    header="Split order"
    :modal="true"
    :style="{ width: '32rem' }"
  >
    <div class="tw:space-y-4">
      <p class="tw:text-sm app-text-muted">
        Set how many of each item to move to the new order. The current order
        will keep the rest.
      </p>

      <!-- Item rows -->
      <div class="tw:space-y-3">
        <div
          v-for="item in splitItems"
          :key="item.productId"
          class="tw:flex tw:items-center tw:gap-3 tw:rounded-xl tw:border tw:px-4 tw:py-3 app-card"
          :class="
            item.splitQty > 0 ? 'tw:border-blue-500/40 tw:bg-blue-500/5' : ''
          "
        >
          <div class="tw:flex-1 tw:min-w-0">
            <p class="tw:text-sm tw:font-medium tw:truncate">
              {{ item.productName }}
            </p>
            <p class="tw:text-xs app-text-muted">
              {{ item.quantity }} in order · {{ formatVnd(item.unitPrice) }} ea.
            </p>
          </div>
          <div class="tw:flex tw:items-center tw:gap-2 tw:shrink-0">
            <span class="tw:text-xs app-text-muted tw:mr-1">To new:</span>
            <prime-button
              severity="secondary"
              size="small"
              outlined
              :disabled="item.splitQty <= 0"
              @click="item.splitQty = Math.max(0, item.splitQty - 1)"
            >
              <iconify icon="ph:minus-bold" />
            </prime-button>
            <span
              class="tw:w-7 tw:text-center tw:text-sm tw:font-semibold tw:tabular-nums"
            >
              {{ item.splitQty }}
            </span>
            <prime-button
              severity="secondary"
              size="small"
              outlined
              :disabled="item.splitQty >= item.quantity"
              @click="
                item.splitQty = Math.min(item.quantity, item.splitQty + 1)
              "
            >
              <iconify icon="ph:plus-bold" />
            </prime-button>
          </div>
        </div>
      </div>

      <!-- Preview -->
      <div
        v-if="splitPreview.toNewQty > 0"
        class="tw:rounded-xl tw:border tw:border-blue-500/30 tw:bg-blue-500/5 tw:px-4 tw:py-3 tw:text-sm tw:space-y-1"
      >
        <div class="tw:flex tw:justify-between">
          <span class="app-text-muted">This order keeps</span>
          <span class="tw:font-medium">{{ splitPreview.keepQty }} item(s)</span>
        </div>
        <div class="tw:flex tw:justify-between">
          <span class="app-text-muted">New order gets</span>
          <span class="tw:font-medium tw:text-blue-400"
            >{{ splitPreview.toNewQty }} item(s)</span
          >
        </div>
      </div>

      <p
        v-if="splitPreview.toNewQty > 0 && splitPreview.keepQty === 0"
        class="tw:text-xs tw:text-red-400"
      >
        Cannot split all items — at least one item must remain in this order.
      </p>
    </div>

    <template #footer>
      <prime-button severity="secondary" outlined @click="splitDialog = false"
        >Cancel</prime-button
      >
      <prime-button
        severity="info"
        :loading="splitLoading"
        :disabled="!splitValid"
        @click="confirmSplit"
      >
        <iconify icon="ph:scissors-bold" />
        <span>Split order</span>
      </prime-button>
    </template>
  </prime-dialog>

  <!-- ── Add item dialog ───────────────────────────────────────────── -->
  <prime-dialog
    v-model:visible="addItemDialog"
    header="Add item"
    :modal="true"
    :style="{ width: '28rem' }"
  >
    <div class="tw:space-y-3">
      <prime-input-text
        v-model="addItemSearch"
        placeholder="Search product…"
        class="app-input tw:w-full"
        autofocus
      />

      <div v-if="addItemLoading" class="tw:flex tw:justify-center tw:py-4">
        <prime-progress-spinner style="width: 1.5rem; height: 1.5rem" />
      </div>

      <div v-else class="tw:space-y-1 tw:max-h-64 tw:overflow-y-auto tw:pr-1">
        <div
          v-for="p in addItemFiltered"
          :key="p.id"
          class="tw:flex tw:items-center tw:justify-between tw:rounded-xl tw:border tw:px-3 tw:py-2.5 tw:cursor-pointer tw:transition-colors app-card"
          :class="
            addItemSelected?.id === p.id
              ? 'tw:border-emerald-500/50 tw:bg-emerald-500/10'
              : 'hover:tw:bg-white/5'
          "
          @click="addItemSelected = p"
        >
          <div>
            <p class="tw:text-sm tw:font-medium">{{ p.name }}</p>
            <p class="tw:text-xs app-text-muted">{{ formatVnd(p.price) }}</p>
          </div>
          <iconify
            v-if="addItemSelected?.id === p.id"
            icon="ph:check-circle-fill"
            class="tw:text-emerald-400 tw:text-lg"
          />
        </div>
        <p
          v-if="!addItemFiltered.length"
          class="tw:text-sm app-text-muted tw:text-center tw:py-4"
        >
          No products found.
        </p>
      </div>

      <div v-if="addItemSelected" class="tw:flex tw:items-center tw:gap-3">
        <label class="tw:text-xs app-text-muted tw:shrink-0">Quantity</label>
        <div class="tw:flex tw:items-center tw:gap-2">
          <prime-button
            severity="secondary" size="small" outlined
            :disabled="addItemQty <= 1"
            @click="addItemQty = Math.max(1, addItemQty - 1)"
          >
            <iconify icon="ph:minus-bold" />
          </prime-button>
          <span class="tw:w-8 tw:text-center tw:font-semibold tw:tabular-nums">{{ addItemQty }}</span>
          <prime-button
            severity="secondary" size="small" outlined
            @click="addItemQty++"
          >
            <iconify icon="ph:plus-bold" />
          </prime-button>
        </div>
      </div>
    </div>

    <template #footer>
      <prime-button severity="secondary" outlined @click="addItemDialog = false">Cancel</prime-button>
      <prime-button
        severity="success"
        :disabled="!addItemSelected"
        :loading="!!itemUpdating"
        @click="confirmAddItem"
      >
        <iconify icon="ph:plus-bold" />
        <span>Add to order</span>
      </prime-button>
    </template>
  </prime-dialog>

  <section class="tw:space-y-6">
    <!-- Header -->
    <div class="tw:flex tw:flex-wrap tw:items-end tw:justify-between tw:gap-4">
      <div>
        <p
          class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300"
        >
          Orders
        </p>
        <h1
          class="tw:mt-2 tw:text-3xl tw:font-semibold tw:flex tw:items-center tw:gap-3"
        >
          <span v-if="order" class="tw:font-mono">{{ order.orderNumber }}</span>
          <prime-skeleton v-else width="12rem" height="2rem" />
          <template v-if="order">
            <prime-tag
              :value="statusTag(order.status).label"
              :severity="statusTag(order.status).severity"
            />
            <prime-tag
              :value="
                paymentTag(order.paymentStatus, order.paymentMethod).label
              "
              :severity="
                paymentTag(order.paymentStatus, order.paymentMethod).severity
              "
            />
          </template>
        </h1>
        <p v-if="order" class="tw:mt-2 tw:text-sm app-text-muted">
          {{ formatDate(order.orderDate) }}
        </p>
        <prime-skeleton v-else width="14rem" height="1rem" class="tw:mt-2" />
      </div>
      <prime-button
        severity="secondary"
        outlined
        size="small"
        @click="router.push({ name: 'ordersList' })"
      >
        <iconify icon="ph:arrow-left-bold" />
        <span>Back to list</span>
      </prime-button>
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

    <!-- Loading skeleton -->
    <div
      v-if="loading"
      class="tw:grid tw:grid-cols-1 tw:gap-6 tw:lg:grid-cols-3"
    >
      <prime-card class="app-card tw:rounded-2xl tw:border">
        <template #content>
          <prime-skeleton
            v-for="i in 4"
            :key="i"
            height="1.25rem"
            class="tw:mb-4"
          />
        </template>
      </prime-card>
      <prime-card class="app-card tw:rounded-2xl tw:border tw:lg:col-span-2">
        <template #content>
          <prime-skeleton
            v-for="i in 5"
            :key="i"
            height="2rem"
            class="tw:mb-3"
          />
        </template>
      </prime-card>
    </div>

    <template v-else-if="order">
      <div class="tw:grid tw:grid-cols-1 tw:gap-6 tw:lg:grid-cols-3">
        <!-- Left: Info + Status actions -->
        <div class="tw:space-y-4">
          <!-- Order info card -->
          <prime-card class="app-card tw:rounded-2xl tw:border">
            <template #content>
              <p class="tw:text-sm tw:font-semibold tw:mb-4">Order info</p>
              <div class="tw:space-y-3">
                <div class="tw:flex tw:justify-between tw:text-sm">
                  <span class="app-text-muted">Order #</span>
                  <span class="tw:font-mono tw:font-semibold">{{
                    order.orderNumber
                  }}</span>
                </div>
                <div class="tw:flex tw:justify-between tw:text-sm">
                  <span class="app-text-muted">Date</span>
                  <span class="tw:font-medium">{{
                    formatDate(order.orderDate)
                  }}</span>
                </div>
                <div class="tw:flex tw:justify-between tw:text-sm">
                  <span class="app-text-muted">Status</span>
                  <prime-tag
                    :value="statusTag(order.status).label"
                    :severity="statusTag(order.status).severity"
                  />
                </div>
                <div
                  class="tw:flex tw:justify-between tw:text-sm tw:items-center"
                >
                  <span class="app-text-muted">Session</span>
                  <span
                    class="tw:font-mono tw:text-xs app-text-muted tw:max-w-28 tw:truncate"
                    :title="String(order.sessionId)"
                    >{{ String(order.sessionId).slice(0, 8) }}…</span
                  >
                </div>
                <prime-divider />
                <div class="tw:flex tw:justify-between tw:text-sm">
                  <span class="tw:font-medium">Total</span>
                  <span class="tw:font-semibold tw:text-base">{{
                    formatVnd(order.totalAmount)
                  }}</span>
                </div>
              </div>
            </template>
          </prime-card>

          <!-- Status action card -->
          <prime-card
            v-if="order.status !== 'Completed' && order.status !== 'Cancelled'"
            class="app-card tw:rounded-2xl tw:border"
          >
            <template #content>
              <p class="tw:text-sm tw:font-semibold tw:mb-4">Actions</p>
              <div class="tw:flex tw:flex-col tw:gap-2">
                <prime-button
                  v-if="NEXT_STATUS[order.status]"
                  :label="NEXT_LABEL[order.status]"
                  severity="success"
                  size="small"
                  class="tw:w-full"
                  :loading="updatingId === 'status'"
                  @click="moveOrder(NEXT_STATUS[order.status])"
                />
                <prime-button
                  severity="danger"
                  size="small"
                  outlined
                  class="tw:w-full"
                  :loading="updatingId === 'status'"
                  @click="
                    (e) =>
                      confirm.require({
                        target: e.currentTarget,
                        message: `Cancel order ${order.orderNumber}?`,
                        icon: 'ph:warning-bold',
                        rejectProps: {
                          label: 'Keep',
                          severity: 'secondary',
                          outlined: true,
                          size: 'small',
                        },
                        acceptProps: {
                          label: 'Yes, cancel',
                          severity: 'danger',
                          size: 'small',
                        },
                        accept: cancelOrder,
                      })
                  "
                >
                  <iconify icon="ph:x-circle-bold" />
                  <span>Cancel order</span>
                </prime-button>

                <!-- Merge button -->
                <prime-button
                  v-if="order.paymentStatus === 'Unpaid'"
                  severity="secondary"
                  outlined
                  size="small"
                  class="tw:w-full"
                  @click="openMergeDialog"
                >
                  <iconify icon="ph:git-merge-bold" />
                  <span>Merge with another</span>
                </prime-button>
              </div>
            </template>
          </prime-card>

          <!-- Merge button for Completed+Unpaid (no status action card) -->
          <prime-card
            v-else-if="
              order.status === 'Completed' && order.paymentStatus === 'Unpaid'
            "
            class="app-card tw:rounded-2xl tw:border"
          >
            <template #content>
              <p class="tw:text-sm tw:font-semibold tw:mb-4">Actions</p>
              <prime-button
                severity="secondary"
                outlined
                size="small"
                class="tw:w-full"
                @click="openMergeDialog"
              >
                <iconify icon="ph:git-merge-bold" />
                <span>Merge with another</span>
              </prime-button>
            </template>
          </prime-card>
        </div>

        <!-- Right: Items + Payment -->
        <div class="tw:lg:col-span-2 tw:space-y-4">
          <!-- Items card -->
          <prime-card class="app-card tw:rounded-2xl tw:border">
            <template #content>
              <div class="tw:flex tw:items-center tw:justify-between tw:mb-4">
                <p class="tw:text-sm tw:font-semibold">
                  Items
                  <span class="app-text-muted tw:font-normal"
                    >({{ order.items?.length }})</span
                  >
                </p>
                <div class="tw:flex tw:gap-2">
                  <!-- Add item button (Pending + Unpaid only) -->
                  <prime-button
                    v-if="canEditItems"
                    severity="secondary"
                    outlined
                    size="small"
                    @click="openAddItemDialog"
                  >
                    <iconify icon="ph:plus-bold" />
                    <span>Add item</span>
                  </prime-button>
                  <!-- Split button -->
                  <prime-button
                    v-if="canSplit"
                    severity="secondary"
                    outlined
                    size="small"
                    @click="openSplitDialog"
                  >
                    <iconify icon="ph:scissors-bold" />
                    <span>Split order</span>
                  </prime-button>
                </div>
              </div>
              <div
                class="tw:space-y-2"
                v-for="(item, index) in order.items"
                :key="item.productId"
              >
                <div
                  class="tw:flex tw:items-center tw:justify-between tw:text-sm"
                  :class="itemUpdating === item.productId ? 'tw:opacity-50' : ''"
                >
                  <div class="tw:flex tw:items-center tw:gap-3 tw:flex-1 tw:min-w-0">
                    <!-- Qty controls when editable, static badge otherwise -->
                    <template v-if="canEditItems">
                      <prime-button
                        severity="secondary" size="small" text
                        class="tw:shrink-0 tw:w-6 tw:h-6 tw:p-0!"
                        :disabled="!!itemUpdating"
                        @click="
                          item.quantity > 1
                            ? setItemQty(item.productId, item.quantity - 1)
                            : removeItem(item)
                        "
                      >
                        <iconify icon="ph:minus-bold" class="tw:text-xs" />
                      </prime-button>
                      <span class="tw:w-6 tw:text-center tw:font-semibold tw:tabular-nums tw:shrink-0">
                        {{ item.quantity }}
                      </span>
                      <prime-button
                        severity="secondary" size="small" text
                        class="tw:shrink-0 tw:w-6 tw:h-6 tw:p-0!"
                        :disabled="!!itemUpdating"
                        @click="setItemQty(item.productId, item.quantity + 1)"
                      >
                        <iconify icon="ph:plus-bold" class="tw:text-xs" />
                      </prime-button>
                    </template>
                    <span
                      v-else
                      class="tw:w-6 tw:h-6 tw:rounded-full tw:bg-white/10 tw:flex tw:items-center tw:justify-center tw:text-xs tw:font-semibold tw:shrink-0"
                      >{{ item.quantity }}</span
                    >
                    <span class="tw:font-medium tw:truncate">{{ item.productName }}</span>
                  </div>
                  <div class="tw:flex tw:items-center tw:gap-2 tw:shrink-0">
                    <div class="tw:text-right">
                      <span class="tw:font-semibold">{{
                        formatVnd(item.totalPrice)
                      }}</span>
                      <span class="app-text-muted tw:text-xs tw:ml-1">
                        ({{ formatVnd(item.unitPrice) }} ea.)
                      </span>
                    </div>
                    <!-- Remove button -->
                    <prime-button
                      v-if="canEditItems"
                      severity="danger" size="small" text
                      class="tw:shrink-0"
                      :disabled="!!itemUpdating"
                      v-tooltip.top="'Remove'"
                      @click="removeItem(item)"
                    >
                      <iconify icon="ph:trash-bold" class="tw:text-xs" />
                    </prime-button>
                  </div>
                </div>
                <prime-divider
                  v-if="index < order.items.length - 1"
                  type="dashed"
                />
              </div>
              <prime-divider />
              <!-- Subtotal row -->
              <div
                class="tw:flex tw:justify-between tw:items-center tw:text-sm"
              >
                <span class="tw:font-medium">Total</span>
                <span class="tw:font-semibold tw:text-base">{{
                  formatVnd(order.totalAmount)
                }}</span>
              </div>
            </template>
          </prime-card>

          <!-- Payment card -->
          <prime-card class="app-card tw:rounded-2xl tw:border">
            <template #content>
              <div class="tw:flex tw:items-center tw:justify-between tw:mb-4">
                <p class="tw:text-sm tw:font-semibold">Payment</p>
                <prime-tag
                  :value="
                    paymentTag(order.paymentStatus, order.paymentMethod).label
                  "
                  :severity="
                    paymentTag(order.paymentStatus, order.paymentMethod)
                      .severity
                  "
                />
              </div>

              <!-- Paid: show recorded info -->
              <template v-if="order.paymentStatus === 'Paid'">
                <div class="tw:space-y-3">
                  <div class="tw:flex tw:justify-between tw:text-sm">
                    <span class="app-text-muted">Method</span>
                    <span class="tw:font-medium">{{
                      methodLabel(order.paymentMethod)
                    }}</span>
                  </div>
                  <div
                    v-if="order.amountReceived != null"
                    class="tw:flex tw:justify-between tw:text-sm"
                  >
                    <span class="app-text-muted">Amount received</span>
                    <span class="tw:font-medium">{{
                      formatVnd(order.amountReceived)
                    }}</span>
                  </div>
                  <div
                    v-if="order.tipAmount"
                    class="tw:flex tw:justify-between tw:text-sm"
                  >
                    <span class="app-text-muted">Tip</span>
                    <span class="tw:font-medium tw:text-emerald-400">{{
                      formatVnd(order.tipAmount)
                    }}</span>
                  </div>
                  <prime-divider />
                  <div class="tw:flex tw:justify-between tw:text-sm">
                    <span class="tw:font-medium">Order total</span>
                    <span class="tw:font-semibold">{{
                      formatVnd(order.totalAmount)
                    }}</span>
                  </div>
                </div>
              </template>

              <!-- Unpaid & not Cancelled: inline payment form -->
              <template
                v-else-if="
                  order.paymentStatus === 'Unpaid' &&
                  order.status !== 'Cancelled'
                "
              >
                <div class="tw:space-y-4">
                  <div class="tw:space-y-1.5">
                    <label
                      for="amountReceived"
                      class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted"
                    >
                      mount received
                    </label>
                    <prime-input-number
                      id="amountReceived"
                      v-model="payAmountReceived"
                      :min="0"
                      :use-grouping="true"
                      :placeholder="String(order.totalAmount)"
                      class="app-input tw:w-full"
                      suffix=" ₫"
                      @input="(e) => (payAmountReceived = e.value)"
                    />
                    <div
                      v-if="payChange !== null && payChange < 0"
                      class="tw:flex tw:items-center tw:justify-between tw:text-sm"
                    >
                      <span class="app-text-muted">Short</span>
                      <span class="tw:text-red-400 tw:font-semibold">{{
                        formatVnd(Math.abs(payChange))
                      }}</span>
                    </div>
                    <template v-if="payChange !== null && payChange > 0">
                      <div
                        class="tw:flex tw:items-center tw:justify-between tw:text-sm"
                      >
                        <span class="app-text-muted">Change</span>
                        <span class="tw:font-semibold">{{
                          formatVnd(payChange)
                        }}</span>
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
                      <div
                        class="tw:flex tw:items-center tw:justify-between tw:text-sm"
                      >
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
                  <div class="tw:space-y-1.5">
                    <label
                      class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted"
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
                  <prime-button
                    severity="success"
                    class="tw:w-full"
                    :loading="payLoading"
                    @click="confirmPayment"
                  >
                    <iconify icon="ph:check-bold" />
                    <span>Confirm payment</span>
                  </prime-button>
                </div>
              </template>

              <!-- Other statuses -->
              <template v-else>
                <p class="tw:text-sm app-text-muted">
                  Payment status: {{ order.paymentStatus }}
                </p>
              </template>
            </template>
          </prime-card>
        </div>
      </div>
    </template>

    <!-- Not found -->
    <prime-card v-else-if="!loading" class="app-card tw:rounded-2xl tw:border">
      <template #content>
        <div
          class="tw:flex tw:flex-col tw:items-center tw:py-10 app-text-muted"
        >
          <iconify icon="ph:receipt-x-bold" class="tw:text-3xl tw:mb-2" />
          <p class="tw:text-sm">Order not found.</p>
        </div>
      </template>
    </prime-card>
  </section>
</template>
