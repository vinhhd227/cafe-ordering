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
  updateOrderDate,
  deleteOrder,
} from "@/services/order.service";
import { ORDER_STATUS, ORDER_STATUS_MAP } from "@/constants/orderStatus";
import { PAYMENT_STATUS, PAYMENT_STATUS_MAP } from "@/constants/paymentStatus";
import { PAYMENT_METHOD, PAYMENT_METHOD_MAP } from "@/constants/paymentMethod";
import { getProducts } from "@/services/product.service";

const { t } = useI18n();

const route = useRoute();
const router = useRouter();
const toast = useToast();
const confirm = useConfirm();
const { can } = usePermission();
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
  const meta = ORDER_STATUS_MAP[status] ?? { severity: "secondary" };
  return { ...meta, label: t(`orders.status.${status}`, status) };
};

const paymentTag = (status, method) => {
  if (status === PAYMENT_STATUS.PAID) {
    const m = t(`orders.paymentMethod.${method}`, "");
    return {
      label: m ? t("orders.pay.paidWith", { method: m }) : t("orders.paymentStatus.PAID"),
      severity: "success",
    };
  }
  const meta = PAYMENT_STATUS_MAP[status] ?? { severity: "warn" };
  return { ...meta, label: t(`orders.paymentStatus.${status}`, meta.label ?? status) };
};

const NEXT_STATUS = {
  [ORDER_STATUS.PENDING]: ORDER_STATUS.PROCESSING,
  [ORDER_STATUS.PROCESSING]: ORDER_STATUS.COMPLETED,
};

const NEXT_LABEL = computed(() => ({
  [ORDER_STATUS.PENDING]: t("orders.detail.actions.startPreparing"),
  [ORDER_STATUS.PROCESSING]: t("orders.detail.actions.markComplete"),
}));

const canSplit = computed(() => {
  if (!order.value || order.value.paymentStatus !== PAYMENT_STATUS.UNPAID) return false;
  const items = order.value.items ?? [];
  return items.length > 1 || (items.length === 1 && items[0].quantity > 1);
});

// Can edit items only when Pending + Unpaid
const canEditItems = computed(
  () =>
    order.value?.status === ORDER_STATUS.PENDING &&
    order.value?.paymentStatus === PAYMENT_STATUS.UNPAID,
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
    await updateOrderStatus(orderId, ORDER_STATUS.CANCELLED);
    order.value.status = ORDER_STATUS.CANCELLED;
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.errors?.join(", ") || "Failed to cancel order.";
  } finally {
    updatingId.value = null;
  }
};

// ── Payment (inline form) ─────────────────────────────────────────
const payMethod = ref(PAYMENT_METHOD.CASH);
const payAmountReceived = ref(null);
const payTip = ref(0);
const payLoading = ref(false);

const PAYMENT_METHODS = computed(() => [
  { label: t("orders.paymentMethod.CASH"), value: PAYMENT_METHOD.CASH, icon: "ph:money-bold" },
  { label: t("orders.paymentMethod.BANK_TRANSFER"), value: PAYMENT_METHOD.BANK_TRANSFER, icon: "ph:bank-bold" },
]);

const payChange = computed(() => {
  if (payAmountReceived.value == null || !order.value) return null;
  return payAmountReceived.value - order.value.finalAmount;
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
      PAYMENT_STATUS.PAID,
      payMethod.value,
      payAmountReceived.value,
      payTip.value ?? 0,
    );
    order.value.paymentStatus = PAYMENT_STATUS.PAID;
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

// ── Edit order date ───────────────────────────────────────────────
const editDatePanel = ref(null);
const editDate = ref(null);
const editDateLoading = ref(false);

const openEditDatePanel = (event) => {
  editDate.value = order.value ? new Date(order.value.orderDate) : new Date();
  editDatePanel.value.toggle(event);
};

const handleUpdateOrderDate = async () => {
  if (!editDate.value) return;
  editDateLoading.value = true;
  try {
    await updateOrderDate(orderId, editDate.value.toISOString());
    order.value.orderDate = editDate.value.toISOString();
    editDatePanel.value.hide();
    toast.add({ severity: "success", summary: t("orders.detail.info.orderDateUpdated"), life: 3000 });
  } catch (err) {
    toast.add({
      severity: "error",
      summary: err?.response?.data?.errors?.join(", ") || "Failed to update order date.",
      life: 3000,
    });
  } finally {
    editDateLoading.value = false;
  }
};

// ── Copy ──────────────────────────────────────────────────────────
const copyOrderNumber = async (orderNumber) => {
  await navigator.clipboard.writeText(orderNumber)
  toast.add({ severity: 'success', summary: t('orders.list.copyOrderNumberSuccess'), life: 2000 })
}

// ── Delete order ──────────────────────────────────────────────────
const handleDeleteOrder = () => {
  confirm.require({
    message: t('orders.detail.deleteOrderConfirmMsg', { orderNumber: order.value?.orderNumber }),
    header: t('orders.detail.deleteOrderConfirmHeader'),
    icon: 'ph:trash-bold',
    rejectProps: { severity: 'secondary', outlined: true, size: 'small', label: t('common.cancel') },
    acceptProps: { severity: 'danger', size: 'small', label: t('orders.detail.deleteOrder') },
    accept: async () => {
      try {
        await deleteOrder(orderId)
        toast.add({ severity: 'success', summary: t('orders.detail.deleteOrderSuccess', { orderNumber: order.value?.orderNumber }), life: 3000 })
        router.push({ name: 'ordersList' })
      } catch (err) {
        errorMessage.value = err?.response?.data?.errors?.join(', ') || err?.response?.data?.title || 'Failed to delete order.'
      }
    },
  })
}

// ── Item editing ──────────────────────────────────────────────────
const itemUpdating = ref(null); // productId currently being updated

const setItemQty = async (productId, newQty) => {
  itemUpdating.value = productId;
  errorMessage.value = "";
  try {
    const res = await updateOrderItem(orderId, productId, newQty);
    // Backend automatically removes free gift items and cancels empty orders.
    order.value.items = res.data.items;
    order.value.totalAmount = res.data.totalAmount;
    order.value.totalDiscount = res.data.totalDiscount;
    order.value.finalAmount = res.data.finalAmount;
    order.value.promotions = res.data.promotions;

    // If order was auto-cancelled (no items left), redirect to orders list.
    if (res.data.status === "CANCELLED") {
      toast.add({
        severity: "info",
        summary: t("orders.status.CANCELLED"),
        detail: t("orders.detail.autoCancel"),
        life: 4000,
      });
      router.push({ name: "orders" });
    }
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
  const regularItems = order.value?.items?.filter((i) => !i.isFreeGift) ?? [];
  const isLastItem = regularItems.length === 1 && regularItems[0].productId === item.productId;
  const hasFreeGift = order.value?.items?.some((i) => i.isFreeGift);

  const message = isLastItem
    ? t("orders.detail.removeItem.lastItem", { name: item.productName })
    : hasFreeGift
      ? t("orders.detail.removeItem.withFreeGift", { name: item.productName })
      : t("orders.detail.removeItem.regular", { name: item.productName });

  confirm.require({
    message,
    icon: isLastItem ? "ph:trash-bold" : hasFreeGift ? "ph:gift-bold" : "ph:warning-bold",
    rejectProps: { label: t("orders.detail.removeItem.keep"), severity: "secondary", outlined: true, size: "small" },
    acceptProps: { label: isLastItem ? t("orders.detail.removeItem.cancelOrder") : t("orders.detail.removeItem.remove"), severity: "danger", size: "small" },
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
    const res = await getOrders({ paymentStatus: PAYMENT_STATUS.UNPAID, pageSize: 100 });
    const all = res?.data?.items ?? [];
    mergeOrders_.value = all.filter(
      (o) => o.id !== orderId && o.status !== ORDER_STATUS.CANCELLED,
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
      summary: t("orders.detail.merge.title"),
      detail: t("orders.detail.mergeToastDetail", { n: mergeSelected.value.length, orderNumber: order.value?.orderNumber }),
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
      summary: t("orders.detail.split.title"),
      detail: t("orders.detail.splitToastDetail", { orderNumber: result?.newOrderNumber }),
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
  <prime-confirm-dialog />

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
          :label="t('orders.detail.split.viewNew')"
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
    :header="t('orders.detail.merge.title')"
    :modal="true"
    :style="{ width: '36rem' }"
  >
    <div class="tw:space-y-3">
      <p class="tw:text-sm app-text-muted">
        {{ t('orders.detail.merge.instruction', { orderNumber: order?.orderNumber }) }}
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
        {{ t('orders.detail.merge.empty') }}
      </div>

      <!-- Order list -->
      <div v-else class="tw:space-y-2 tw:max-h-80 tw:overflow-y-auto tw:pr-1">
        <label
          v-for="o in mergeOrders_"
          :key="o.id"
          class="tw:flex tw:items-start tw:gap-3 tw:rounded-xl tw:border tw:px-4 tw:py-3 tw:cursor-pointer tw:transition-colors app-card"
          :class="
            mergeSelected.includes(o.id)
              ? 'tw:border-emerald-500/50 tw:bg-emerald-500/10'
              : ''
          "
        >
          <input
            type="checkbox"
            class="tw:accent-emerald-500 tw:mt-0.5"
            :value="o.id"
            v-model="mergeSelected"
          />
          <div class="tw:flex-1 tw:min-w-0">
            <p class="tw:font-mono tw:font-semibold tw:text-sm tw:flex tw:items-center tw:gap-1.5">
              {{ o.orderNumber }}
              <span v-if="o.tableCode" class="tw:text-xs tw:font-normal app-text-muted">
                · <iconify icon="ph:table-bold" class="tw:inline tw:text-[10px]" />
                {{ o.tableCode }}
              </span>
              <span v-else class="tw:text-xs tw:font-normal tw:italic app-text-muted">· {{ t('orders.detail.noTable') }}</span>
            </p>
            <ul class="tw:mt-0.5 tw:space-y-0.5">
              <li
                v-for="item in o.items"
                :key="item.productId"
                class="tw:text-xs app-text-muted tw:flex tw:items-center tw:gap-1"
              >
                <iconify icon="ph:circle-fill" class="tw:text-[5px] tw:shrink-0" />
                {{ item.productName }}
                <span class="tw:font-semibold tw:text-foreground">×{{ item.quantity }}</span>
              </li>
            </ul>
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
        >{{ t('orders.cancel') }}</prime-button
      >
      <prime-button
        severity="success"
        :loading="mergeLoading"
        :disabled="!mergeSelected.length"
        @click="confirmMerge"
      >
        <iconify icon="ph:git-merge-bold" />
        <span>{{ t('orders.detail.merge.confirm', { n: mergeSelected.length }) }}</span>
      </prime-button>
    </template>
  </prime-dialog>

  <!-- ── Split dialog ───────────────────────────────────────────── -->
  <prime-dialog
    v-model:visible="splitDialog"
    :header="t('orders.detail.split.title')"
    :modal="true"
    :style="{ width: '32rem' }"
  >
    <div class="tw:space-y-4">
      <p class="tw:text-sm app-text-muted">
        {{ t('orders.detail.split.instruction') }}
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
              {{ t('orders.detail.split.itemDetail', { n: item.quantity, price: formatVnd(item.unitPrice) }) }}
            </p>
          </div>
          <div class="tw:flex tw:items-center tw:gap-2 tw:shrink-0">
            <span class="tw:text-xs app-text-muted tw:mr-1">{{ t('orders.detail.split.toNew') }}</span>
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
          <span class="app-text-muted">{{ t('orders.detail.split.thisOrderKeeps') }}</span>
          <span class="tw:font-medium">{{ t('orders.detail.split.itemCount', { n: splitPreview.keepQty }) }}</span>
        </div>
        <div class="tw:flex tw:justify-between">
          <span class="app-text-muted">{{ t('orders.detail.split.newOrderGets') }}</span>
          <span class="tw:font-medium tw:text-blue-400"
            >{{ t('orders.detail.split.itemCount', { n: splitPreview.toNewQty }) }}</span
          >
        </div>
      </div>

      <p
        v-if="splitPreview.toNewQty > 0 && splitPreview.keepQty === 0"
        class="tw:text-xs tw:text-red-400"
      >
        {{ t('orders.detail.split.error') }}
      </p>
    </div>

    <template #footer>
      <prime-button severity="secondary" outlined @click="splitDialog = false"
        >{{ t('orders.cancel') }}</prime-button
      >
      <prime-button
        severity="info"
        :loading="splitLoading"
        :disabled="!splitValid"
        @click="confirmSplit"
      >
        <iconify icon="ph:scissors-bold" />
        <span>{{ t('orders.detail.split.confirm') }}</span>
      </prime-button>
    </template>
  </prime-dialog>

  <!-- ── Add item dialog ───────────────────────────────────────────── -->
  <prime-dialog
    v-model:visible="addItemDialog"
    :header="t('orders.detail.addItem.title')"
    :modal="true"
    :style="{ width: '28rem' }"
  >
    <div class="tw:space-y-3">
      <prime-input-text
        v-model="addItemSearch"
        :placeholder="t('orders.detail.addItem.searchPlaceholder')"
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
          {{ t('orders.detail.addItem.noProducts') }}
        </p>
      </div>

      <div v-if="addItemSelected" class="tw:flex tw:items-center tw:gap-3">
        <label class="tw:text-xs app-text-muted tw:shrink-0">{{ t('orders.detail.addItem.quantity') }}</label>
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
      <prime-button severity="secondary" outlined @click="addItemDialog = false">{{ t('orders.cancel') }}</prime-button>
      <prime-button
        severity="success"
        :disabled="!addItemSelected"
        :loading="!!itemUpdating"
        @click="confirmAddItem"
      >
        <iconify icon="ph:plus-bold" />
        <span>{{ t('orders.detail.addItem.confirm') }}</span>
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
          {{ t('orders.breadcrumb') }}
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
            <prime-tag v-if="order.isManual" value="Thủ công" severity="secondary" />
          </template>
        </h1>
        <p v-if="order" class="tw:mt-2 tw:text-sm app-text-muted">
          {{ formatDate(order.orderDate) }}
        </p>
        <prime-skeleton v-else width="14rem" height="1rem" class="tw:mt-2" />
      </div>
      <div class="tw:flex tw:gap-2">
        <prime-button
          v-if="order?.isManual"
          severity="secondary"
          outlined
          size="small"
          @click="router.push({ name: 'ordersEditManual', params: { id: orderId } })"
        >
          <iconify icon="ph:pencil-line-bold" />
          <span>Sửa thủ công</span>
        </prime-button>
        <prime-button
          severity="secondary"
          outlined
          size="small"
          @click="router.push({ name: 'ordersList' })"
        >
          <iconify icon="ph:arrow-left-bold" />
          <span>{{ t('orders.detail.backToList') }}</span>
        </prime-button>
      </div>
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
              <p class="tw:text-sm tw:font-semibold tw:mb-4">{{ t('orders.detail.info.title') }}</p>
              <div class="tw:space-y-3">
                <div class="tw:flex tw:justify-between tw:text-sm tw:items-center">
                  <span class="app-text-muted">{{ t('orders.detail.info.orderNumber') }}</span>
                  <div class="tw:flex tw:items-center tw:gap-1">
                    <span class="tw:font-mono tw:font-semibold">{{ order.orderNumber }}</span>
                    <prime-button
                      :class="btnIcon"
                      severity="secondary"
                      text
                      v-tooltip.top="t('orders.list.copyOrderNumber')"
                      @click="copyOrderNumber(order.orderNumber)"
                    >
                      <iconify icon="ph:copy" class="tw:text-xs" />
                    </prime-button>
                  </div>
                </div>
                <div class="tw:flex tw:justify-between tw:text-sm tw:items-center">
                  <span class="app-text-muted">{{ t('orders.detail.info.date') }}</span>
                  <div class="tw:flex tw:items-center tw:gap-1">
                    <span class="tw:font-medium">{{ formatDate(order.orderDate) }}</span>
                    <prime-button
                      :class="btnIcon"
                      severity="secondary"
                      text
                      v-tooltip.top="t('orders.detail.info.editOrderDateTooltip')"
                      @click="openEditDatePanel($event)"
                    >
                      <iconify icon="ph:pencil-simple-bold" />
                    </prime-button>
                  </div>
                </div>
                <prime-popover ref="editDatePanel">
                  <div class="tw:flex tw:flex-col tw:gap-3 tw:min-w-56">
                    <p class="tw:text-sm tw:font-semibold">{{ t('orders.detail.info.editOrderDate') }}</p>
                    <prime-date-picker
                      v-model="editDate"
                      show-time
                      hour-format="24"
                      inline
                      date-format="dd/mm/yy"
                    />
                    <prime-button
                      severity="success"
                      size="small"
                      :loading="editDateLoading"
                      @click="handleUpdateOrderDate"
                    >
                      <iconify icon="ph:floppy-disk-bold" />
                      <span>{{ t('common.save') }}</span>
                    </prime-button>
                  </div>
                </prime-popover>
                <div class="tw:flex tw:justify-between tw:text-sm">
                  <span class="app-text-muted">{{ t('orders.detail.info.status') }}</span>
                  <prime-tag
                    :value="statusTag(order.status).label"
                    :severity="statusTag(order.status).severity"
                  />
                </div>
                <div class="tw:flex tw:justify-between tw:text-sm tw:items-center">
                  <span class="app-text-muted">{{ t('orders.detail.info.table') }}</span>
                  <span v-if="order.tableCode" class="tw:font-semibold tw:font-mono">
                    {{ order.tableCode }}
                  </span>
                  <span v-else class="app-text-muted tw:italic tw:text-xs">—</span>
                </div>
                <div
                  class="tw:flex tw:justify-between tw:text-sm tw:items-center"
                >
                  <span class="app-text-muted">{{ t('orders.detail.info.session') }}</span>
                  <span
                    class="tw:font-mono tw:text-xs app-text-muted tw:max-w-28 tw:truncate"
                    :title="String(order.sessionId)"
                    >{{ String(order.sessionId).slice(0, 8) }}…</span
                  >
                </div>
                <div v-if="order.guestCount" class="tw:flex tw:justify-between tw:text-sm tw:items-center">
                  <span class="app-text-muted tw:flex tw:items-center tw:gap-1">
                    <iconify icon="ph:users" class="tw:text-sm" />
                    {{ t('orders.detail.info.guestCount') }}
                  </span>
                  <span class="tw:font-semibold">{{ order.guestCount }}</span>
                </div>
                <div v-if="order.completedAt" class="tw:flex tw:justify-between tw:text-sm tw:items-center">
                  <span class="app-text-muted tw:flex tw:items-center tw:gap-1">
                    <iconify icon="ph:check-circle" class="tw:text-sm tw:text-green-500" />
                    {{ t('orders.detail.info.completedAt') }}
                  </span>
                  <span class="tw:text-xs tw:font-mono">{{ formatDate(order.completedAt) }}</span>
                </div>
                <div v-if="order.paidAt" class="tw:flex tw:justify-between tw:text-sm tw:items-center">
                  <span class="app-text-muted tw:flex tw:items-center tw:gap-1">
                    <iconify icon="ph:currency-circle-dollar" class="tw:text-sm tw:text-blue-400" />
                    {{ t('orders.detail.info.paidAt') }}
                  </span>
                  <span class="tw:text-xs tw:font-mono">{{ formatDate(order.paidAt) }}</span>
                </div>
                <prime-divider />
                <div v-if="order.totalDiscount > 0" class="tw:flex tw:justify-between tw:text-xs tw:mb-1">
                  <span class="tw:flex tw:items-center tw:gap-1 app-text-muted">
                    <iconify icon="ph:tag-bold" class="tw:text-emerald-400" />
                    {{ t('orders.detail.info.discount') }}
                  </span>
                  <span class="tw:text-emerald-400">-{{ formatVnd(order.totalDiscount) }}</span>
                </div>
                <div class="tw:flex tw:justify-between tw:text-sm">
                  <span class="tw:font-medium">{{ t('orders.detail.info.total') }}</span>
                  <div class="tw:text-right">
                    <span
                      v-if="order.totalDiscount > 0"
                      class="app-text-muted tw:line-through tw:text-xs tw:mr-1"
                    >{{ formatVnd(order.totalAmount) }}</span>
                    <span class="tw:font-semibold tw:text-base" :class="order.totalDiscount > 0 ? 'tw:text-emerald-400' : ''">
                      {{ formatVnd(order.finalAmount) }}
                    </span>
                  </div>
                </div>
              </div>
            </template>
          </prime-card>

          <!-- Status action card -->
          <prime-card
            v-if="order.status !== ORDER_STATUS.COMPLETED && order.status !== ORDER_STATUS.CANCELLED"
            class="app-card tw:rounded-2xl tw:border"
          >
            <template #content>
              <p class="tw:text-sm tw:font-semibold tw:mb-4">{{ t('orders.detail.actions.title') }}</p>
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
                        message: t('orders.kanban.cancelTitle', { orderNumber: order.orderNumber }),
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
                        accept: cancelOrder,
                      })
                  "
                >
                  <iconify icon="ph:x-circle-bold" />
                  <span>{{ t('orders.detail.actions.cancel') }}</span>
                </prime-button>

                <!-- Merge button -->
                <prime-button
                  v-if="order.paymentStatus === PAYMENT_STATUS.UNPAID"
                  severity="secondary"
                  outlined
                  size="small"
                  class="tw:w-full"
                  @click="openMergeDialog"
                >
                  <iconify icon="ph:git-merge-bold" />
                  <span>{{ t('orders.detail.mergeWith') }}</span>
                </prime-button>
              </div>
            </template>
          </prime-card>

          <!-- Merge button for Completed+Unpaid (no status action card) -->
          <prime-card
            v-else-if="
              order.status === ORDER_STATUS.COMPLETED && order.paymentStatus === PAYMENT_STATUS.UNPAID
            "
            class="app-card tw:rounded-2xl tw:border"
          >
            <template #content>
              <p class="tw:text-sm tw:font-semibold tw:mb-4">{{ t('orders.detail.actions.title') }}</p>
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

          <!-- Delete card (CANCELLED orders only) -->
          <prime-card
            v-if="order.status === ORDER_STATUS.CANCELLED && can('order.delete')"
            class="app-card tw:rounded-2xl tw:border"
          >
            <template #content>
              <prime-button
                severity="danger"
                outlined
                size="small"
                class="tw:w-full"
                @click="handleDeleteOrder"
              >
                <iconify icon="ph:trash-bold" />
                <span>{{ t('orders.detail.deleteOrder') }}</span>
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
                  {{ t('orders.detail.items') }}
                  <span class="app-text-muted tw:font-normal"
                    >({{ order.items?.length }})</span
                  >
                </p>
                <div class="tw:flex tw:gap-2">
                  <!-- Edit order button (Pending + Unpaid only) -->
                  <prime-button
                    v-if="canEditItems"
                    severity="secondary"
                    outlined
                    size="small"
                    @click="router.push({ name: 'ordersEdit', params: { id: orderId } })"
                  >
                    <iconify icon="ph:pencil-simple-bold" />
                    <span>{{ t('orders.detail.editOrder') }}</span>
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
                    <span>{{ t('orders.detail.split.title') }}</span>
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
                    <template v-if="canEditItems && !item.isFreeGift">
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
                      class="tw:w-6 tw:h-6 tw:rounded-full tw:flex tw:items-center tw:justify-center tw:text-xs tw:font-semibold tw:shrink-0"
                      :class="item.isFreeGift ? 'tw:bg-amber-500/20 tw:text-amber-400' : 'tw:bg-white/10'"
                      >{{ item.quantity }}</span
                    >
                    <div class="tw:min-w-0">
                      <span class="tw:font-medium tw:truncate tw:block">{{ item.productName }}</span>
                      <!-- Drink options -->
                      <div
                        v-if="item.temperature || item.iceLevel || item.sugarLevel || item.isTakeaway || item.isFreeGift"
                        class="tw:flex tw:flex-wrap tw:gap-1 tw:mt-1"
                      >
                        <span v-if="item.temperature"
                          class="tw:inline-flex tw:items-center tw:gap-0.5 tw:rounded tw:px-1.5 tw:py-0.5 tw:text-[10px] tw:font-medium"
                          :class="item.temperature === 'HOT'
                            ? 'tw:bg-orange-500/15 tw:text-orange-300'
                            : 'tw:bg-sky-500/15 tw:text-sky-300'"
                        >
                          <iconify :icon="item.temperature === 'HOT' ? 'ph:flame-bold' : 'ph:snowflake-bold'" class="tw:text-[9px]" />
                          {{ t(`orders.temperature.${item.temperature}`, item.temperature) }}
                        </span>
                        <span v-if="item.iceLevel && item.iceLevel !== 'NORMAL'"
                          class="tw:inline-flex tw:items-center tw:gap-0.5 tw:rounded tw:px-1.5 tw:py-0.5 tw:text-[10px] tw:font-medium tw:bg-sky-500/10 tw:text-sky-400"
                        >
                          <iconify icon="game-icons:ice-cube" class="tw:text-[9px]" />
                            {{ t(`orders.iceLevel.${item.iceLevel}`, item.iceLevel) }}
                        </span>
                        <span v-if="item.sugarLevel && item.sugarLevel !== 'NORMAL'"
                          class="tw:inline-flex tw:items-center tw:gap-0.5 tw:rounded tw:px-1.5 tw:py-0.5 tw:text-[10px] tw:font-medium tw:bg-amber-500/10 tw:text-amber-400"
                        >
                          <iconify icon="ph:cube-bold" class="tw:text-[9px]" />
                          {{ t(`orders.sugarLevel.${item.sugarLevel}`, item.sugarLevel) }}
                        </span>
                        <span v-if="item.isTakeaway"
                          class="tw:inline-flex tw:items-center tw:gap-0.5 tw:rounded tw:px-1.5 tw:py-0.5 tw:text-[10px] tw:font-medium tw:bg-purple-500/10 tw:text-purple-400"
                        >
                          <iconify icon="ph:bag-bold" class="tw:text-[9px]" />
                          {{ t('orders.serving.takeaway') }}
                        </span>
                        <span v-if="item.isFreeGift"
                          class="tw:inline-flex tw:items-center tw:gap-0.5 tw:rounded tw:px-1.5 tw:py-0.5 tw:text-[10px] tw:font-medium tw:bg-amber-500/15 tw:text-amber-400"
                        >
                          <iconify icon="ph:gift-bold" class="tw:text-[9px]" />
                          {{ t('orders.create.freeBadge') }}
                        </span>
                      </div>
                    </div>
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
                      v-if="canEditItems && !item.isFreeGift"
                      severity="danger" size="small" text
                      class="tw:shrink-0"
                      :disabled="!!itemUpdating"
                      v-tooltip.top="t('orders.detail.removeTooltip')"
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
              <!-- Promotions applied -->
              <div v-if="order.totalDiscount > 0" class="tw:space-y-1.5 tw:mb-2">
                <div
                  v-for="promo in order.promotions"
                  :key="promo.promotionId"
                  class="tw:flex tw:justify-between tw:items-center tw:text-xs"
                >
                  <span class="tw:flex tw:items-center tw:gap-1 app-text-muted">
                    <iconify icon="ph:tag-bold" class="tw:text-emerald-400" />
                    {{ promo.promoCode }}
                  </span>
                  <span class="tw:text-emerald-400 tw:font-medium">-{{ formatVnd(promo.discountAmount) }}</span>
                </div>
              </div>
              <!-- Total row -->
              <div class="tw:flex tw:justify-between tw:items-center tw:text-sm">
                <span class="tw:font-medium">{{ t('orders.detail.info.total') }}</span>
                <div class="tw:text-right">
                  <span
                    v-if="order.totalDiscount > 0"
                    class="app-text-muted tw:line-through tw:text-xs tw:mr-1"
                  >{{ formatVnd(order.totalAmount) }}</span>
                  <span class="tw:font-semibold tw:text-base" :class="order.totalDiscount > 0 ? 'tw:text-emerald-400' : ''">
                    {{ formatVnd(order.finalAmount) }}
                  </span>
                </div>
              </div>
            </template>
          </prime-card>

          <!-- Payment card -->
          <prime-card class="app-card tw:rounded-2xl tw:border">
            <template #content>
              <div class="tw:flex tw:items-center tw:justify-between tw:mb-4">
                <p class="tw:text-sm tw:font-semibold">{{ t('orders.pay.title') }}</p>
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
              <template v-if="order.paymentStatus === PAYMENT_STATUS.PAID">
                <div class="tw:space-y-3">
                  <div class="tw:flex tw:justify-between tw:text-sm">
                    <span class="app-text-muted">{{ t('orders.pay.paymentMethod') }}</span>
                    <span class="tw:font-medium">{{
                      t(`orders.paymentMethod.${order.paymentMethod}`, PAYMENT_METHOD_MAP[order.paymentMethod]?.label ?? order.paymentMethod)
                    }}</span>
                  </div>
                  <div
                    v-if="order.amountReceived != null"
                    class="tw:flex tw:justify-between tw:text-sm"
                  >
                    <span class="app-text-muted">{{ t('orders.pay.amountReceived') }}</span>
                    <span class="tw:font-medium">{{
                      formatVnd(order.amountReceived)
                    }}</span>
                  </div>
                  <div
                    v-if="order.tipAmount"
                    class="tw:flex tw:justify-between tw:text-sm"
                  >
                    <span class="app-text-muted">{{ t('orders.pay.tip') }}</span>
                    <span class="tw:font-medium tw:text-emerald-400">{{
                      formatVnd(order.tipAmount)
                    }}</span>
                  </div>
                  <prime-divider />
                  <div v-if="order.totalDiscount > 0" class="tw:flex tw:justify-between tw:text-xs tw:mb-1">
                    <span class="tw:flex tw:items-center tw:gap-1 app-text-muted">
                      <iconify icon="ph:tag-bold" class="tw:text-emerald-400" />
                      {{ t('orders.detail.info.discount') }}
                    </span>
                    <span class="tw:text-emerald-400">-{{ formatVnd(order.totalDiscount) }}</span>
                  </div>
                  <div class="tw:flex tw:justify-between tw:text-sm">
                    <span class="tw:font-medium">{{ t('orders.pay.order') }}</span>
                    <span class="tw:font-semibold">{{
                      formatVnd(order.finalAmount)
                    }}</span>
                  </div>
                </div>
              </template>

              <!-- Unpaid & not Cancelled: inline payment form -->
              <template
                v-else-if="
                  order.paymentStatus === PAYMENT_STATUS.UNPAID &&
                  order.status !== ORDER_STATUS.CANCELLED
                "
              >
                <div class="tw:space-y-4">
                  <div class="tw:space-y-1.5">
                    <label
                      for="amountReceived"
                      class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted"
                    >
                      {{ t('orders.pay.amountReceived') }}
                    </label>
                    <prime-input-number
                      id="amountReceived"
                      v-model="payAmountReceived"
                      :min="0"
                      :use-grouping="true"
                      :placeholder="String(order.finalAmount)"
                      class="app-input tw:w-full"
                      suffix=" ₫"
                      @input="(e) => (payAmountReceived = e.value)"
                    />
                    <div
                      v-if="payChange !== null && payChange < 0"
                      class="tw:flex tw:items-center tw:justify-between tw:text-sm"
                    >
                      <span class="app-text-muted">{{ t('orders.pay.short') }}</span>
                      <span class="tw:text-red-400 tw:font-semibold">{{
                        formatVnd(Math.abs(payChange))
                      }}</span>
                    </div>
                    <template v-if="payChange !== null && payChange > 0">
                      <div
                        class="tw:flex tw:items-center tw:justify-between tw:text-sm"
                      >
                        <span class="app-text-muted">{{ t('orders.pay.change') }}</span>
                        <span class="tw:font-semibold">{{
                          formatVnd(payChange)
                        }}</span>
                      </div>
                      <div class="tw:space-y-1">
                        <label
                          class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted"
                          >{{ t('orders.pay.tip') }}</label
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
                            v-tooltip.top="t('orders.pay.keepAllAsTip')"
                            @click="payTip = payChange"
                          >
                            <iconify icon="ph:heart-bold" />
                          </prime-button>
                        </div>
                      </div>
                      <div
                        class="tw:flex tw:items-center tw:justify-between tw:text-sm"
                      >
                        <span class="app-text-muted">{{ t('orders.pay.returnToCustomer') }}</span>
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
                      >{{ t('orders.pay.paymentMethod') }}</label
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
                    <span>{{ t('orders.pay.confirmPayment') }}</span>
                  </prime-button>
                </div>
              </template>

              <!-- Other statuses -->
              <template v-else>
                <p class="tw:text-sm app-text-muted">
                  {{ paymentTag(order.paymentStatus, order.paymentMethod).label }}
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
          <p class="tw:text-sm">{{ t('orders.detail.orderNotFound') }}</p>
        </div>
      </template>
    </prime-card>
  </section>
</template>
