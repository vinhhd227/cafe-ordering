<script setup>
import { getAdminMenu } from "@/services/menu.service";
import { getOrderById, updateManualOrder } from "@/services/order.service";
import { appCard, cardRing, btnIcon } from "@/layout/ui";
import { ORDER_STATUS, ORDER_STATUS_OPTIONS } from "@/constants/orderStatus";
import { PAYMENT_STATUS } from "@/constants/paymentStatus";
import { PAYMENT_METHOD } from "@/constants/paymentMethod";
import { DRINK_TEMPERATURE, DRINK_TEMPERATURE_OPTIONS } from "@/constants/drinkTemperature";
import { ICE_LEVEL, ICE_LEVEL_OPTIONS } from "@/constants/iceLevel";
import { SUGAR_LEVEL, SUGAR_LEVEL_OPTIONS } from "@/constants/sugarLevel";

const router = useRouter();
const route = useRoute();
const toast = useToast();
const { t } = useI18n();

const orderId = computed(() => Number(route.params.id));

// ── Data ─────────────────────────────────────────────────────────
const order = ref(null);
const menuCategories = ref([]);
const loadingMenu = ref(false);
const loadingOrder = ref(false);

// ── Order metadata ───────────────────────────────────────────────
const orderedAt = ref(null);
const guestCount = ref(null);
const orderStatus = ref(ORDER_STATUS.PENDING);
const paymentStatus = ref(PAYMENT_STATUS.UNPAID);
const paymentMethod = ref(PAYMENT_METHOD.UNKNOWN);
const amountReceived = ref(null);
const tipAmount = ref(0);

// ── Options dialog ───────────────────────────────────────────────
const showOptionsDialog = ref(false);
const selectedProduct = ref(null);
const pendingQuantity = ref(1);
const pendingOptions = ref({
  temperature: null,
  iceLevel: null,
  sugarLevel: null,
  isTakeaway: false,
  note: "",
});

// ── Cart ─────────────────────────────────────────────────────────
const cart = ref([]);
const searchQuery = ref("");

// ── Submit ───────────────────────────────────────────────────────
const saving = ref(false);
const errorMessage = ref("");

// ── Helpers ──────────────────────────────────────────────────────
const formatVnd = (val) =>
  new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND", maximumFractionDigits: 0 }).format(val ?? 0);

const makeCartKey = (productId, opts) => {
  const { temperature = "", iceLevel = "", sugarLevel = "", isTakeaway = false, note = "" } = opts ?? {};
  return `${productId}|${temperature}|${iceLevel}|${sugarLevel}|${isTakeaway}|${note}`;
};

const optionsLabel = (item) => {
  const parts = [];
  if (item.temperature) parts.push(t(`orders.temperature.${item.temperature}`, item.temperature));
  if (item.iceLevel && item.iceLevel !== ICE_LEVEL.NORMAL) parts.push(t(`orders.iceLevel.${item.iceLevel}`, item.iceLevel));
  if (item.sugarLevel && item.sugarLevel !== SUGAR_LEVEL.NORMAL) parts.push(t(`orders.sugarLevel.${item.sugarLevel}`, item.sugarLevel));
  if (item.isTakeaway) parts.push(t("orders.serving.takeaway", "Takeaway"));
  if (item.note) parts.push(`"${item.note}"`);
  return parts.join(" · ");
};

// ── Computed ─────────────────────────────────────────────────────
const visibleCategories = computed(() =>
  menuCategories.value
    .filter((c) => c.isActive)
    .map((c) => {
      let products = (c.products ?? []).filter((p) => p.isActive);
      if (searchQuery.value.trim()) {
        const q = searchQuery.value.toLowerCase();
        products = products.filter((p) => p.name.toLowerCase().includes(q));
      }
      return { ...c, filteredProducts: products };
    })
    .filter((c) => c.filteredProducts.length > 0),
);

const cartTotal = computed(() =>
  cart.value.reduce((sum, i) => sum + i.unitPrice * i.quantity, 0),
);

const defaultGuestCount = computed(() =>
  cart.value.reduce((sum, i) => sum + i.quantity, 0),
);

const canSave = computed(() => cart.value.length > 0 && !saving.value);

const paymentStatusOptions = computed(() => [
  { label: "Chưa thanh toán", value: PAYMENT_STATUS.UNPAID },
  { label: "Đã thanh toán", value: PAYMENT_STATUS.PAID },
  { label: "Hoàn tiền", value: PAYMENT_STATUS.REFUNDED },
  { label: "Huỷ", value: PAYMENT_STATUS.VOIDED },
]);

const paymentMethodOptions = computed(() => [
  { label: "Không xác định", value: PAYMENT_METHOD.UNKNOWN },
  { label: "Tiền mặt", value: PAYMENT_METHOD.CASH },
  { label: "Chuyển khoản", value: PAYMENT_METHOD.BANK_TRANSFER },
]);

const statusOptions = computed(() =>
  ORDER_STATUS_OPTIONS.map((o) => ({ ...o, label: t(`orders.status.${o.value}`, o.label) })),
);

const temperatureOptions = computed(() =>
  DRINK_TEMPERATURE_OPTIONS.map((o) => ({ ...o, label: t(`orders.temperature.${o.value}`, o.label) })),
);
const iceLevelOptions = computed(() =>
  ICE_LEVEL_OPTIONS.map((o) => ({ ...o, label: t(`orders.iceLevel.${o.value}`, o.label) })),
);
const sugarLevelOptions = computed(() =>
  SUGAR_LEVEL_OPTIONS.map((o) => ({ ...o, label: t(`orders.sugarLevel.${o.value}`, o.label) })),
);
const servingOptions = computed(() => [
  { value: false, label: t("orders.serving.dineIn", "Dine-in") },
  { value: true,  label: t("orders.serving.takeaway", "Takeaway") },
]);

// ── Cart helpers ──────────────────────────────────────────────────
const changeQty = (key, delta) => {
  const idx = cart.value.findIndex((i) => i._key === key);
  if (idx === -1) return;
  cart.value[idx].quantity += delta;
  if (cart.value[idx].quantity <= 0) cart.value.splice(idx, 1);
};

const handleAddToCart = (product) => {
  selectedProduct.value = product;
  pendingOptions.value = {
    temperature: product.hasTemperatureOption ? DRINK_TEMPERATURE.COLD : null,
    iceLevel: product.hasIceLevelOption ? ICE_LEVEL.NORMAL : null,
    sugarLevel: product.hasSugarLevelOption ? SUGAR_LEVEL.NORMAL : null,
    isTakeaway: false,
    note: "",
  };
  pendingQuantity.value = 1;
  showOptionsDialog.value = true;
};

const setTemperature = (opt) => {
  pendingOptions.value.temperature = opt;
  if (opt === DRINK_TEMPERATURE.HOT && selectedProduct.value?.hasIceLevelOption)
    pendingOptions.value.iceLevel = ICE_LEVEL.LESS;
  else if (opt === DRINK_TEMPERATURE.COLD && pendingOptions.value.iceLevel === ICE_LEVEL.LESS)
    pendingOptions.value.iceLevel = ICE_LEVEL.NORMAL;
};

const confirmAddToCart = () => {
  const key = makeCartKey(selectedProduct.value.id, pendingOptions.value);
  const existing = cart.value.find((i) => i._key === key);
  if (existing) {
    existing.quantity += pendingQuantity.value;
  } else {
    cart.value.push({
      _key: key,
      productId: selectedProduct.value.id,
      productName: selectedProduct.value.name,
      unitPrice: selectedProduct.value.price,
      quantity: pendingQuantity.value,
      temperature: pendingOptions.value.temperature,
      iceLevel: pendingOptions.value.iceLevel,
      sugarLevel: pendingOptions.value.sugarLevel,
      isTakeaway: pendingOptions.value.isTakeaway,
      note: pendingOptions.value.note?.trim() || null,
    });
  }
  showOptionsDialog.value = false;
};

// ── Submit ────────────────────────────────────────────────────────
const submit = async () => {
  if (!canSave.value) return;
  errorMessage.value = "";
  saving.value = true;
  try {
    await updateManualOrder(orderId.value, {
      items: cart.value.map((i) => ({
        productId: i.productId,
        quantity: i.quantity,
        temperature: i.temperature ?? null,
        iceLevel: i.iceLevel ?? null,
        sugarLevel: i.sugarLevel ?? null,
        isTakeaway: i.isTakeaway ?? false,
        note: i.note ?? null,
      })),
      orderedAt: orderedAt.value ? orderedAt.value.toISOString() : null,
      guestCount: guestCount.value ? Number(guestCount.value) : (defaultGuestCount.value || null),
      status: orderStatus.value,
      paymentStatus: paymentStatus.value,
      paymentMethod: paymentMethod.value,
      amountReceived: amountReceived.value,
      tipAmount: tipAmount.value ?? 0,
    });
    toast.add({ severity: "success", summary: "Cập nhật thành công", detail: order.value?.orderNumber, life: 3000 });
    router.push({ name: "ordersDetail", params: { id: orderId.value } });
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.errors?.map((e) => e.errorMessage ?? e).join("; ") ||
      err?.response?.data?.message ||
      "Cập nhật thất bại.";
  } finally {
    saving.value = false;
  }
};

// ── Init ─────────────────────────────────────────────────────────
onMounted(async () => {
  loadingOrder.value = true;
  loadingMenu.value = true;
  try {
    const [orderRes, menuRes] = await Promise.all([
      getOrderById(orderId.value),
      getAdminMenu(),
    ]);
    order.value = orderRes.data;
    menuCategories.value = menuRes.data ?? [];

    // Pre-populate metadata
    orderStatus.value = order.value.status ?? ORDER_STATUS.PENDING;
    paymentStatus.value = order.value.paymentStatus ?? PAYMENT_STATUS.UNPAID;
    paymentMethod.value = order.value.paymentMethod ?? PAYMENT_METHOD.UNKNOWN;
    amountReceived.value = order.value.amountReceived ?? null;
    tipAmount.value = order.value.tipAmount ?? 0;
    guestCount.value = order.value.guestCount ?? null;
    orderedAt.value = order.value.orderDate ? new Date(order.value.orderDate) : null;

    // Pre-populate cart from existing items (skip free gifts)
    cart.value = (order.value.items ?? [])
      .filter((i) => !i.isFreeGift)
      .map((i) => ({
        _key: makeCartKey(i.productId, {
          temperature: i.temperature,
          iceLevel: i.iceLevel,
          sugarLevel: i.sugarLevel,
          isTakeaway: i.isTakeaway,
          note: i.note ?? "",
        }),
        productId: i.productId,
        productName: i.productName,
        unitPrice: i.unitPrice,
        quantity: i.quantity,
        temperature: i.temperature ?? null,
        iceLevel: i.iceLevel ?? null,
        sugarLevel: i.sugarLevel ?? null,
        isTakeaway: i.isTakeaway ?? false,
        note: i.note ?? null,
      }));
  } finally {
    loadingOrder.value = false;
    loadingMenu.value = false;
  }
});
</script>

<template>
  <!-- Options dialog -->
  <prime-dialog v-model:visible="showOptionsDialog" :header="selectedProduct?.name" :modal="true" class="tw:w-[360px]">
    <div class="tw:space-y-4">
      <div v-if="selectedProduct?.hasTemperatureOption" class="tw:space-y-2">
        <p class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest">{{ t("orders.temperature.label", "Nhiệt độ") }}</p>
        <prime-select-button
          v-model="pendingOptions.temperature"
          :options="temperatureOptions"
          option-label="label"
          option-value="value"
          @change="(e) => setTemperature(e.value)"
          :pt="{ root: { class: 'tw:flex tw:w-full' }, button: { class: 'tw:flex-1 tw:justify-center' } }"
        />
      </div>
      <div v-if="selectedProduct?.hasIceLevelOption && pendingOptions.temperature !== DRINK_TEMPERATURE.HOT" class="tw:space-y-2">
        <p class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest">{{ t("orders.iceLevel.label", "Đá") }}</p>
        <prime-select-button
          v-model="pendingOptions.iceLevel"
          :options="iceLevelOptions"
          option-label="label"
          option-value="value"
          :pt="{ root: { class: 'tw:flex tw:w-full' }, button: { class: 'tw:flex-1 tw:justify-center' } }"
        />
      </div>
      <div v-if="selectedProduct?.hasSugarLevelOption" class="tw:space-y-2">
        <p class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest">{{ t("orders.sugarLevel.label", "Đường") }}</p>
        <prime-select-button
          v-model="pendingOptions.sugarLevel"
          :options="sugarLevelOptions"
          option-label="label"
          option-value="value"
          :pt="{ root: { class: 'tw:flex tw:w-full' }, button: { class: 'tw:flex-1 tw:justify-center' } }"
        />
      </div>
      <div class="tw:space-y-2">
        <p class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest">{{ t("orders.serving.label", "Phục vụ") }}</p>
        <prime-select-button
          v-model="pendingOptions.isTakeaway"
          :options="servingOptions"
          option-label="label"
          option-value="value"
          :pt="{ root: { class: 'tw:flex tw:w-full' }, button: { class: 'tw:flex-1 tw:justify-center' } }"
        />
      </div>
      <div class="tw:space-y-1.5">
        <label for="edit-manual-note" class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest">{{ t("orders.create.note", "Ghi chú") }}</label>
        <prime-input-text id="edit-manual-note" v-model="pendingOptions.note" :placeholder="t('orders.create.notePlaceholder', 'Yêu cầu đặc biệt...')" class="app-input tw:w-full" />
      </div>
      <div class="tw:flex tw:items-center tw:justify-between">
        <span class="tw:text-sm app-text-muted">{{ t("orders.create.quantity", "Số lượng") }}</span>
        <div class="tw:flex tw:items-center tw:gap-3">
          <prime-button :class="btnIcon" severity="secondary" outlined :disabled="pendingQuantity <= 1" @click="pendingQuantity--">
            <iconify icon="ph:minus-bold" />
          </prime-button>
          <span class="tw:w-8 tw:text-center tw:font-semibold tw:text-lg tw:tabular-nums">{{ pendingQuantity }}</span>
          <prime-button :class="btnIcon" severity="secondary" outlined @click="pendingQuantity++">
            <iconify icon="ph:plus-bold" />
          </prime-button>
        </div>
      </div>
    </div>
    <template #footer>
      <prime-button severity="secondary" outlined @click="showOptionsDialog = false">{{ t("orders.cancel", "Huỷ") }}</prime-button>
      <prime-button severity="success" @click="confirmAddToCart">
        <iconify icon="ph:plus-bold" />
        <span>{{ t("orders.create.addToCart", "Thêm vào giỏ") }}</span>
      </prime-button>
    </template>
  </prime-dialog>

  <section class="tw:space-y-4">
    <!-- Header -->
    <div class="tw:flex tw:items-center tw:justify-between">
      <div class="tw:flex tw:items-center tw:gap-4">
        <prime-button :class="btnIcon" severity="secondary" text @click="router.push({ name: 'ordersDetail', params: { id: orderId } })">
          <iconify icon="ph:arrow-left-bold" />
        </prime-button>
        <div>
          <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300">{{ t("orders.breadcrumb") }}</p>
          <h1 class="tw:text-2xl tw:font-semibold tw:flex tw:items-center tw:gap-2">
            Sửa order thủ công
            <span v-if="order" class="tw:text-emerald-400 tw:font-mono tw:text-xl">#{{ order.orderNumber }}</span>
            <prime-tag value="Thủ công" severity="secondary" />
          </h1>
        </div>
      </div>
    </div>

    <!-- Error -->
    <prime-message v-if="errorMessage" severity="error" size="small" variant="simple" closable @close="errorMessage = ''">{{ errorMessage }}</prime-message>

    <!-- Loading -->
    <div v-if="loadingOrder || loadingMenu" class="tw:flex tw:justify-center tw:py-12">
      <prime-progress-spinner style="width: 2.5rem; height: 2.5rem" />
    </div>

    <div v-else class="tw:grid tw:grid-cols-1 tw:lg:grid-cols-5 tw:gap-4">
      <!-- ── Left: product catalog ─────────────────────────────── -->
      <div :class="[appCard, cardRing, 'tw:lg:col-span-3 tw:rounded-xl tw:p-4 tw:flex tw:flex-col tw:gap-3']">
        <div class="tw:flex tw:items-center tw:gap-2">
          <iconify icon="prime:search" class="tw:opacity-60" />
          <prime-input-text v-model="searchQuery" :placeholder="t('orders.create.search', 'Tìm sản phẩm...')" size="small" class="tw:flex-1" />
        </div>

        <div class="tw:space-y-4 tw:overflow-y-auto tw:max-h-[60dvh]">
          <div v-for="cat in visibleCategories" :key="cat.id">
            <p class="tw:text-xs tw:font-semibold tw:uppercase tw:tracking-widest app-text-muted tw:mb-2">{{ cat.name }}</p>
            <div class="tw:grid tw:grid-cols-2 tw:sm:grid-cols-3 tw:gap-2">
              <button
                v-for="product in cat.filteredProducts"
                :key="product.id"
                class="tw:text-left tw:rounded-xl tw:border tw:border-white/10 tw:bg-white/5 tw:p-3 tw:transition-colors hover:tw:bg-white/10 hover:tw:border-emerald-500/40"
                @click="handleAddToCart(product)"
              >
                <p class="tw:text-sm tw:font-medium tw:leading-tight">{{ product.name }}</p>
                <p class="tw:text-xs tw:text-emerald-400 tw:mt-1">{{ formatVnd(product.price) }}</p>
              </button>
            </div>
          </div>
          <p v-if="!visibleCategories.length" class="tw:text-sm app-text-muted tw:text-center tw:py-6">Không có sản phẩm</p>
        </div>
      </div>

      <!-- ── Right: cart + metadata ────────────────────────────── -->
      <div class="tw:lg:col-span-2 tw:flex tw:flex-col tw:gap-4">

        <!-- Order metadata -->
        <div :class="[appCard, cardRing, 'tw:rounded-xl tw:p-4 tw:space-y-4']">
          <div class="tw:flex tw:items-center tw:justify-between">
            <p class="tw:text-sm tw:font-semibold">Thông tin order</p>
            <prime-tag v-if="order?.tableCode" severity="secondary">
              <iconify icon="ic:round-table-bar" class="tw:mr-1" />
              {{ order.tableCode }}
            </prime-tag>
          </div>

          <!-- Ordered at + Guest count -->
          <div class="tw:grid tw:grid-cols-2 tw:gap-3">
            <div class="tw:space-y-1.5">
              <label for="edit-manual-date" class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest">Thời gian</label>
              <prime-date-picker
                id="edit-manual-date"
                v-model="orderedAt"
                :show-time="true"
                :hour-format="'24'"
                date-format="dd/mm/yy"
                show-button-bar
                class="app-input tw:w-full"
              />
            </div>
            <div class="tw:space-y-1.5">
              <label for="edit-manual-guests" class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest">Số khách</label>
              <prime-input-number
                id="edit-manual-guests"
                v-model="guestCount"
                :placeholder="defaultGuestCount > 0 ? String(defaultGuestCount) : '—'"
                :min="1"
                :max="99"
                :use-grouping="false"
                class="app-input tw:w-full"
              />
            </div>
          </div>

          <!-- Status -->
          <div class="tw:space-y-1.5">
            <label for="edit-manual-status" class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest">Trạng thái</label>
            <prime-select
              id="edit-manual-status"
              v-model="orderStatus"
              :options="statusOptions"
              option-label="label"
              option-value="value"
              class="app-input tw:w-full"
            />
          </div>

          <!-- Payment status + method -->
          <div class="tw:grid tw:grid-cols-2 tw:gap-3">
            <div class="tw:space-y-1.5">
              <label for="edit-manual-pay-status" class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest">TT thanh toán</label>
              <prime-select
                id="edit-manual-pay-status"
                v-model="paymentStatus"
                :options="paymentStatusOptions"
                option-label="label"
                option-value="value"
                class="app-input tw:w-full"
              />
            </div>
            <div class="tw:space-y-1.5">
              <label for="edit-manual-pay-method" class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest">Phương thức</label>
              <prime-select
                id="edit-manual-pay-method"
                v-model="paymentMethod"
                :options="paymentMethodOptions"
                option-label="label"
                option-value="value"
                class="app-input tw:w-full"
              />
            </div>
          </div>

          <!-- Amount received + tip (only when PAID) -->
          <div v-if="paymentStatus === PAYMENT_STATUS.PAID" class="tw:grid tw:grid-cols-2 tw:gap-3">
            <div class="tw:space-y-1.5">
              <label for="edit-manual-amount" class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest">Số tiền nhận</label>
              <prime-input-number
                id="edit-manual-amount"
                v-model="amountReceived"
                :min="0"
                :use-grouping="true"
                suffix=" ₫"
                class="app-input tw:w-full"
                @input="(e) => (amountReceived = e.value)"
              />
            </div>
            <div class="tw:space-y-1.5">
              <label for="edit-manual-tip" class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest">Tiền tip</label>
              <prime-input-number
                id="edit-manual-tip"
                v-model="tipAmount"
                :min="0"
                :use-grouping="true"
                suffix=" ₫"
                class="app-input tw:w-full"
                @input="(e) => (tipAmount = e.value ?? 0)"
              />
            </div>
          </div>
        </div>

        <!-- Cart -->
        <div :class="[appCard, cardRing, 'tw:rounded-xl tw:p-4 tw:flex tw:flex-col tw:gap-3']">
          <div class="tw:flex tw:items-center tw:justify-between">
            <p class="tw:text-sm tw:font-semibold">Giỏ hàng</p>
            <span class="tw:text-xs app-text-muted">{{ cart.length }} sản phẩm</span>
          </div>

          <div v-if="!cart.length" class="tw:text-sm app-text-muted tw:text-center tw:py-4">
            Chưa có sản phẩm nào
          </div>

          <div v-else class="tw:space-y-2 tw:max-h-60 tw:overflow-y-auto tw:pr-1">
            <div
              v-for="item in cart"
              :key="item._key"
              class="tw:flex tw:items-start tw:justify-between tw:gap-2 tw:rounded-xl tw:border tw:border-white/10 tw:bg-white/5 tw:px-3 tw:py-2"
            >
              <div class="tw:flex-1 tw:min-w-0">
                <p class="tw:text-sm tw:font-medium tw:truncate">{{ item.productName }}</p>
                <p v-if="optionsLabel(item)" class="tw:text-[11px] app-text-muted tw:mt-0.5">{{ optionsLabel(item) }}</p>
                <p class="tw:text-xs tw:text-emerald-400 tw:mt-0.5">{{ formatVnd(item.unitPrice) }}</p>
              </div>
              <div class="tw:flex tw:items-center tw:gap-1.5 tw:shrink-0">
                <prime-button :class="btnIcon" severity="secondary" size="small" outlined @click="changeQty(item._key, -1)">
                  <iconify icon="ph:minus-bold" />
                </prime-button>
                <span class="tw:w-6 tw:text-center tw:text-sm tw:font-semibold tw:tabular-nums">{{ item.quantity }}</span>
                <prime-button :class="btnIcon" severity="secondary" size="small" outlined @click="changeQty(item._key, 1)">
                  <iconify icon="ph:plus-bold" />
                </prime-button>
              </div>
            </div>
          </div>

          <div v-if="cart.length" class="tw:border-t tw:border-white/10 tw:pt-3 tw:flex tw:justify-between tw:items-center">
            <span class="tw:text-sm app-text-muted">Tạm tính</span>
            <span class="tw:font-semibold">{{ formatVnd(cartTotal) }}</span>
          </div>

          <prime-button
            severity="success"
            :disabled="!canSave"
            :loading="saving"
            class="tw:w-full"
            @click="submit"
          >
            <iconify icon="ph:floppy-disk-bold" />
            <span>Lưu thay đổi</span>
          </prime-button>
        </div>

      </div>
    </div>
  </section>
</template>
