<script setup>
import { getAdminMenu } from "@/services/menu.service";
import { getOrderById, editOrderItems, applyPromotionAdmin } from "@/services/order.service";
import { validatePromotion } from "@/services/promotion.service";
import { appCard } from "../../layout/ui";

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

// ── Filter + collapse ─────────────────────────────────────────────
const searchQuery = ref("");

// ── Guest count ──────────────────────────────────────────────────
const guestCount = ref(null);

const defaultGuestCount = computed(() =>
  cart.value
    .filter((i) => !i.isFreeGift && !i.isAccompaniment)
    .reduce((acc, i) => acc + i.quantity, 0),
);

// ── Submit ───────────────────────────────────────────────────────
const saving = ref(false);
const errorMessage = ref("");

// ── Promotion ────────────────────────────────────────────────────
const promoCode = ref("");
const promoInfo = ref(null);
const promoLoading = ref(false);
const promoError = ref("");

const applyPromoCode = async () => {
  const code = promoCode.value.trim();
  if (!code) return;
  promoError.value = "";
  promoInfo.value = null;
  promoLoading.value = true;
  try {
    const res = await validatePromotion(code, cartTotal.value);
    const data = res.data;
    if (!data.isApplicable) {
      promoError.value = data.message || "Promotion not applicable.";
    } else {
      promoInfo.value = data;
    }
  } catch (err) {
    promoError.value =
      err?.response?.data?.message || "Invalid promotion code.";
  } finally {
    promoLoading.value = false;
  }
};

const clearPromo = () => {
  promoCode.value = "";
  promoInfo.value = null;
  promoError.value = "";
};

// ── Helpers ──────────────────────────────────────────────────────
const formatVnd = (val) =>
  new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
    maximumFractionDigits: 0,
  }).format(val ?? 0);

const makeCartKey = (productId, opts) => {
  const {
    temperature = "",
    iceLevel = "",
    sugarLevel = "",
    isTakeaway = false,
    note = "",
  } = opts ?? {};
  return `${productId}|${temperature}|${iceLevel}|${sugarLevel}|${isTakeaway}|${note}`;
};

const optionsLabel = (item) => {
  const parts = [];
  if (item.temperature) parts.push(t(`orders.temperature.${item.temperature}`));
  if (item.iceLevel && item.iceLevel !== ICE_LEVEL.NORMAL)
    parts.push(t(`orders.iceLevel.${item.iceLevel}`));
  if (item.sugarLevel && item.sugarLevel !== SUGAR_LEVEL.NORMAL)
    parts.push(t(`orders.sugarLevel.${item.sugarLevel}`));
  return parts.join(" · ");
};

// ── i18n option arrays ────────────────────────────────────────────
const temperatureOptions = computed(() =>
  DRINK_TEMPERATURE_OPTIONS.map((opt) => ({
    ...opt,
    label: t(`orders.temperature.${opt.value}`),
  })),
);

const iceLevelOptions = computed(() =>
  ICE_LEVEL_OPTIONS.map((opt) => ({
    ...opt,
    label: t(`orders.iceLevel.${opt.value}`),
  })),
);

const sugarLevelOptions = computed(() =>
  SUGAR_LEVEL_OPTIONS.map((opt) => ({
    ...opt,
    label: t(`orders.sugarLevel.${opt.value}`),
  })),
);

const servingOptions = computed(() => [
  { ...SERVING_TYPE_OPTIONS[0], label: t("orders.serving.dineIn") },
  { ...SERVING_TYPE_OPTIONS[1], label: t("orders.serving.takeaway") },
]);

// ── Computed ─────────────────────────────────────────────────────
const visibleCategories = computed(() =>
  menuCategories.value
    .filter((c) => c.isActive)
    .map((c) => {
      let products = (c.products ?? []).filter((p) => p.isActive);
      if (searchQuery.value.trim()) {
        const q = searchQuery.value.toLowerCase();
        products = products.filter(
          (p) =>
            p.name.toLowerCase().includes(q) ||
            (p.description?.toLowerCase().includes(q) ?? false),
        );
      }
      return { ...c, filteredProducts: products };
    })
    .filter((c) => c.filteredProducts.length > 0),
);

const cartTotal = computed(() =>
  cart.value.reduce((sum, i) => sum + i.unitPrice * i.quantity, 0),
);

const cartItemCount = computed(() =>
  cart.value.reduce((sum, i) => sum + i.quantity, 0),
);

const canSave = computed(() => cart.value.length > 0 && !saving.value);

const hasPromotions = computed(
  () => order.value?.promotions && order.value.promotions.length > 0,
);

// ── Cart helpers ──────────────────────────────────────────────────
const cartQuantity = (productId) =>
  cart.value
    .filter((i) => i.productId === productId)
    .reduce((sum, i) => sum + i.quantity, 0);

const changeQty = (key, delta) => {
  const idx = cart.value.findIndex((i) => i._key === key);
  if (idx === -1) return;
  cart.value[idx].quantity += delta;
  if (cart.value[idx].quantity <= 0) cart.value.splice(idx, 1);
};

// ── Options dialog ────────────────────────────────────────────────
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
  if (opt === DRINK_TEMPERATURE.HOT) {
    if (selectedProduct.value?.hasIceLevelOption)
      pendingOptions.value.iceLevel = ICE_LEVEL.LESS;
    if (selectedProduct.value?.hasSugarLevelOption)
      pendingOptions.value.sugarLevel = SUGAR_LEVEL.NORMAL;
  } else if (opt === DRINK_TEMPERATURE.COLD) {
    if (
      selectedProduct.value?.hasIceLevelOption &&
      pendingOptions.value.iceLevel === ICE_LEVEL.LESS
    )
      pendingOptions.value.iceLevel = ICE_LEVEL.NORMAL;
  }
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
      isAccompaniment: selectedProduct.value.isAccompaniment ?? false,
      isFreeGift: false,
    });
  }
  showOptionsDialog.value = false;
  selectedProduct.value = null;
};

// ── Save ─────────────────────────────────────────────────────────
const saveChanges = async () => {
  if (!canSave.value) return;
  errorMessage.value = "";
  saving.value = true;
  try {
    await editOrderItems(
      orderId.value,
      cart.value.map((item) => ({
        productId: item.productId,
        quantity: item.quantity,
        temperature: item.temperature ?? null,
        iceLevel: item.iceLevel ?? null,
        sugarLevel: item.sugarLevel ?? null,
        isTakeaway: item.isTakeaway ?? false,
        note: item.note ?? null,
      })),
      guestCount.value != null
        ? Number(guestCount.value)
        : (defaultGuestCount.value || null),
    );
    // Apply promo code if entered
    if (promoCode.value.trim()) {
      try {
        await applyPromotionAdmin(orderId.value, promoCode.value.trim());
      } catch (promoErr) {
        const msg =
          promoErr?.response?.data?.errors?.map((e) => e.errorMessage ?? e).join("; ") ||
          promoErr?.response?.data?.message ||
          "Could not apply promotion.";
        toast.add({
          severity: "warn",
          summary: t("orders.create.promoBadge"),
          detail: msg,
          life: 5000,
        });
      }
    }

    toast.add({
      severity: "success",
      summary: t("orders.edit.saveChanges"),
      detail: t("orders.edit.successToast", { orderNumber: order.value?.orderNumber }),
      life: 3000,
    });
    router.push({ name: "ordersDetail", params: { id: orderId.value } });
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.errors?.map((e) => e.errorMessage ?? e).join("; ") ||
      err?.response?.data?.message ||
      "Failed to save changes.";
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

    // Pre-populate cart from existing order items (skip free gifts)
    cart.value = (orderRes.data?.items ?? [])
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
        isAccompaniment: false,
        isFreeGift: false,
      }));

    guestCount.value = orderRes.data?.guestCount ?? null;
  } finally {
    loadingOrder.value = false;
    loadingMenu.value = false;
  }
});
</script>

<template>
  <section class="tw:space-y-4">
    <!-- Header -->
    <div class="tw:flex tw:items-center tw:justify-between">
      <div class="tw:flex tw:items-center tw:gap-4">
        <prime-button
          :class="btnIcon"
          severity="secondary"
          text
          @click="router.push({ name: 'ordersDetail', params: { id: orderId } })"
        >
          <iconify icon="ph:arrow-left-bold" />
        </prime-button>
        <div>
          <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300">
            {{ t("orders.breadcrumb") }}
          </p>
          <h1 class="tw:text-2xl tw:font-semibold">
            {{ t("orders.edit.title") }}
            <span v-if="order" class="tw:text-emerald-400 tw:font-mono tw:text-xl">
              #{{ order.orderNumber }}
            </span>
          </h1>
        </div>
      </div>
    </div>

    <!-- Top bar: search + guest count -->
    <div
      :class="[appCard, cardRing, 'tw:px-4 tw:py-3', 'tw:rounded-md', 'tw:flex tw:justify-between tw:items-center tw:flex-wrap tw:gap-3']"
    >
      <div class="tw:flex tw:items-center tw:gap-3 tw:flex-wrap">
        <div class="tw:flex tw:items-center tw:gap-1.5">
          <iconify icon="prime:search" class="tw:text-lg tw:opacity-60" />
          <prime-input-text
            v-model="searchQuery"
            :placeholder="t('orders.create.search')"
            size="small"
          />
        </div>
        <div class="tw:flex tw:items-center tw:gap-1.5">
          <iconify icon="ph:users" class="tw:text-base tw:opacity-60" />
          <prime-input-number
            v-model="guestCount"
            :placeholder="defaultGuestCount > 0 ? String(defaultGuestCount) : t('orders.create.guestCountPlaceholder')"
            :min="1"
            :max="99"
            :useGrouping="false"
            inputClass="tw:text-sm tw:w-25"
            size="small"
          />
        </div>
      </div>
      <!-- Table tag -->
      <prime-tag v-if="order?.tableCode" severity="secondary">
        <iconify icon="ic:round-table-bar" class="tw:mr-1" />
        {{ order.tableCode }}
      </prime-tag>
    </div>

    <!-- Promotion warning -->
    <div
      v-if="hasPromotions"
      class="tw:flex tw:items-center tw:gap-2 tw:rounded-xl tw:border tw:border-amber-500/30 tw:bg-amber-500/10 tw:px-4 tw:py-3 tw:text-sm tw:text-amber-300"
    >
      <iconify icon="ph:warning-bold" class="tw:shrink-0" />
      <span>{{ t("orders.edit.promotionWarning") }}</span>
    </div>

    <!-- Main grid -->
    <div class="tw:grid tw:gap-5 tw:lg:grid-cols-12">
      <!-- ── LEFT: Menu ──────────────────────────────────────── -->
      <section class="tw:lg:col-span-8 tw:space-y-4">
        <!-- Loading skeleton -->
        <div v-if="loadingMenu" class="tw:space-y-6">
          <div v-for="n in 2" :key="n" class="tw:space-y-3">
            <div
              class="tw:h-6 tw:w-32 tw:rounded-lg tw:animate-pulse"
              style="background: var(--app-bg-subtle)"
            />
            <div class="tw:grid tw:grid-cols-2 tw:gap-3 sm:tw:grid-cols-3">
              <div
                v-for="m in 3"
                :key="m"
                class="tw:animate-pulse tw:rounded-2xl app-panel tw:border tw:overflow-hidden"
              >
                <div class="tw:h-32 tw:w-full" style="background: var(--app-bg-subtle)" />
                <div class="tw:p-3 tw:space-y-2">
                  <div class="tw:h-4 tw:w-3/4 tw:rounded" style="background: var(--app-bg-subtle)" />
                  <div class="tw:h-3 tw:w-1/2 tw:rounded" style="background: var(--app-bg-subtle)" />
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Empty state -->
        <prime-panel
          v-else-if="!loadingMenu && visibleCategories.length === 0"
          class="app-panel tw:rounded-2xl tw:border tw:p-12 tw:text-center app-text-muted"
        >
          <iconify icon="ph:magnifying-glass-bold" class="tw:text-3xl tw:mb-3 tw:block tw:mx-auto tw:opacity-40" />
          {{ t("orders.create.noProducts") }}
        </prime-panel>

        <prime-panel
          v-else
          v-for="category in visibleCategories"
          :key="category.id"
          :header="category.name"
          toggleable
          :pt="{ root: { class: `${appCard} ${cardRing}` } }"
        >
          <div
            class="tw:grid tw:grid-cols-2 tw:md:grid-cols-3 tw:xl:grid-cols-4 tw:gap-3 sm:tw:grid-cols-3 tw:p-3 tw:pt-0"
          >
            <article
              v-for="product in category.filteredProducts"
              :key="product.id"
              class="tw:group tw:flex tw:h-full tw:flex-col tw:overflow-hidden tw:rounded-xl tw:border tw:cursor-pointer tw:transition-all hover:tw:-translate-y-0.5 hover:tw:border-emerald-500/50"
              style="border-color: var(--app-border); background: var(--app-bg-subtle)"
              @click="handleAddToCart(product)"
            >
              <!-- Product image / placeholder -->
              <div class="tw:relative tw:overflow-hidden tw:shrink-0">
                <img
                  v-if="product.imageUrl"
                  :src="product.imageUrl"
                  :alt="product.name"
                  class="tw:h-32 tw:w-full tw:object-cover tw:transition group-hover:tw:scale-105"
                />
                <div
                  v-else
                  class="tw:flex tw:h-32 tw:w-full tw:items-center tw:justify-center"
                  style="background: var(--app-bg-muted)"
                >
                  <iconify icon="ph:coffee-bold" class="tw:text-3xl tw:opacity-30" />
                </div>
                <!-- Cart badge -->
                <div
                  v-if="cartQuantity(product.id) > 0"
                  class="tw:absolute tw:top-2 tw:right-2 tw:h-6 tw:w-6 tw:rounded-full tw:bg-emerald-500 tw:text-white tw:text-xs tw:font-bold tw:flex tw:items-center tw:justify-center tw:shadow"
                >
                  {{ cartQuantity(product.id) }}
                </div>
              </div>
              <div class="tw:flex tw:flex-1 tw:flex-col tw:gap-0.5 tw:p-3">
                <p class="tw:text-sm tw:font-semibold tw:leading-snug">{{ product.name }}</p>
                <p v-if="product.description" class="tw:text-xs app-text-muted tw:leading-snug tw:line-clamp-2">
                  {{ product.description }}
                </p>
                <p class="tw:mt-auto tw:pt-1 tw:text-sm tw:font-bold tw:text-emerald-400">
                  {{ formatVnd(product.price) }}
                </p>
              </div>
            </article>
          </div>
        </prime-panel>
      </section>

      <!-- ── RIGHT: Cart ────────────────────────────────────── -->
      <aside class="tw:lg:col-span-4 tw:lg:self-start tw:lg:sticky tw:lg:top-6">
        <div :class="[appCard, cardRing, 'tw:p-5', 'tw:rounded-md']">
          <!-- Cart header -->
          <div class="tw:flex tw:items-center tw:justify-between tw:mb-4">
            <h2 class="tw:text-base tw:font-semibold tw:flex tw:items-center tw:gap-2">
              {{ t("orders.create.cart") }}
              <prime-badge v-if="cartItemCount > 0" :value="cartItemCount" severity="success" />
            </h2>
          </div>

          <!-- Empty state -->
          <div
            v-if="cart.length === 0"
            class="tw:py-8 tw:text-center app-text-muted tw:text-sm tw:rounded-xl tw:border tw:border-dashed"
            style="border-color: var(--app-border)"
          >
            {{ t("orders.create.cartEmpty") }}<br />
            <span class="tw:text-xs tw:opacity-60">{{ t("orders.create.cartEmptyHint") }}</span>
          </div>

          <!-- Cart items -->
          <div v-else class="tw:space-y-2">
            <div
              v-for="item in cart"
              :key="item._key"
              class="tw:rounded-xl tw:border tw:p-3 tw:transition-colors"
              style="border-color: var(--app-border)"
            >
              <div class="tw:flex tw:items-start tw:gap-2">
                <div class="tw:flex-1 tw:min-w-0">
                  <p class="tw:text-sm tw:font-medium tw:leading-snug">{{ item.productName }}</p>
                  <p class="tw:text-xs tw:mt-0.5 tw:text-emerald-400">{{ formatVnd(item.unitPrice) }}</p>
                  <p v-if="optionsLabel(item)" class="tw:text-xs tw:text-amber-400 tw:mt-0.5 tw:leading-snug">
                    {{ optionsLabel(item) }}
                  </p>
                  <span
                    v-if="item.isTakeaway"
                    class="tw:inline-block tw:mt-1 tw:text-xs tw:px-1.5 tw:py-0.5 tw:rounded tw:bg-sky-500/10 tw:text-sky-400"
                  >{{ t("orders.create.takeaway") }}</span>
                  <p v-if="item.note" class="tw:text-xs tw:mt-1 tw:italic app-text-muted">{{ item.note }}</p>
                </div>
                <div class="tw:flex tw:items-center tw:gap-1 tw:shrink-0">
                  <prime-button
                    size="small" text rounded severity="secondary"
                    @click="changeQty(item._key, -1)"
                    :class="btnIcon"
                  >
                    <iconify icon="prime:minus" />
                  </prime-button>
                  <span class="tw:text-sm tw:font-bold tw:w-5 tw:text-center">{{ item.quantity }}</span>
                  <prime-button
                    size="small" text rounded severity="secondary"
                    @click="changeQty(item._key, 1)"
                    :class="btnIcon"
                  >
                    <iconify icon="prime:plus" />
                  </prime-button>
                </div>
              </div>
              <div class="tw:flex tw:justify-end tw:mt-1.5">
                <span class="tw:text-sm tw:font-semibold tw:text-emerald-400">
                  {{ formatVnd(item.unitPrice * item.quantity) }}
                </span>
              </div>
            </div>

            <!-- Total summary -->
            <div class="tw:rounded-xl tw:p-3 tw:mt-1" style="background: var(--app-bg-subtle)">
              <div class="tw:flex tw:items-center tw:justify-between tw:text-sm tw:mb-1">
                <span class="app-text-muted">{{ t("orders.create.subtotal") }}</span>
                <span class="tw:font-medium">{{ formatVnd(cartTotal) }}</span>
              </div>
              <div class="tw:flex tw:items-center tw:justify-between tw:text-xs tw:mb-2">
                <span class="app-text-muted">{{ t("orders.create.serviceCharge") }}</span>
                <span class="app-text-muted">{{ t("orders.create.free") }}</span>
              </div>
              <div class="tw:border-t tw:pt-2" style="border-color: var(--app-border)">
                <div class="tw:flex tw:items-center tw:justify-between tw:font-bold">
                  <span>{{ t("orders.create.total") }}</span>
                  <span class="tw:text-emerald-400 tw:text-base">{{ formatVnd(cartTotal) }}</span>
                </div>
              </div>
            </div>
          </div>

          <!-- Promo code -->
          <div class="tw:mt-4">
            <!-- Applied -->
            <div
              v-if="promoInfo"
              class="tw:flex tw:items-center tw:justify-between tw:rounded-xl tw:border tw:border-emerald-500/30 tw:bg-emerald-500/10 tw:px-3 tw:py-2"
            >
              <div class="tw:flex tw:items-center tw:gap-2 tw:min-w-0">
                <iconify icon="ph:tag-bold" class="tw:text-emerald-400 tw:shrink-0" />
                <span class="tw:font-medium tw:text-sm tw:text-emerald-300">{{ promoInfo.code }}</span>
                <span class="tw:text-xs app-text-muted tw:truncate">{{ promoInfo.name }}</span>
              </div>
              <div class="tw:flex tw:items-center tw:gap-2 tw:shrink-0">
                <span v-if="promoInfo.estimatedDiscount" class="tw:text-emerald-400 tw:text-sm tw:font-medium">
                  -{{ new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(promoInfo.estimatedDiscount) }}
                </span>
                <prime-button size="small" text severity="secondary" :class="btnIcon" @click="clearPromo">
                  <iconify icon="prime:times" class="tw:text-xs" />
                </prime-button>
              </div>
            </div>

            <!-- Input -->
            <div v-else class="tw:flex tw:gap-2">
              <prime-input-text
                v-model="promoCode"
                :placeholder="t('orders.create.promoPlaceholder')"
                size="small"
                class="tw:flex-1 tw:font-mono"
                @keyup.enter="applyPromoCode"
              />
              <prime-button
                size="small"
                severity="secondary"
                outlined
                :loading="promoLoading"
                :disabled="!promoCode.trim()"
                @click="applyPromoCode"
              >
                <iconify icon="ph:tag-bold" />
              </prime-button>
            </div>
            <p v-if="promoError" class="tw:text-xs tw:text-red-400 tw:mt-1">{{ promoError }}</p>
          </div>

          <!-- Error -->
          <p v-if="errorMessage" class="tw:text-xs tw:text-red-400 tw:mt-3">{{ errorMessage }}</p>

          <!-- Save button -->
          <prime-button
            class="tw:w-full tw:mt-4"
            :loading="saving"
            :disabled="!canSave"
            @click="saveChanges"
          >
            <iconify icon="prime:check" />
            <span>{{ saving ? t("orders.edit.saving") : t("orders.edit.saveChanges") }}</span>
          </prime-button>
        </div>
      </aside>
    </div>
  </section>

  <!-- ── Options Dialog ─────────────────────────────────────────── -->
  <prime-dialog
    v-model:visible="showOptionsDialog"
    :header="selectedProduct?.name"
    modal
    :style="{ width: '22rem' }"
    @hide="selectedProduct = null"
  >
    <div class="tw:space-y-5">
      <!-- Quantity -->
      <div>
        <p class="tw:mb-2 tw:text-sm tw:font-semibold">{{ t("orders.create.optionsDialog.quantity") }}</p>
        <div class="tw:flex tw:items-center tw:gap-3">
          <button
            class="tw:flex tw:h-9 tw:w-9 tw:items-center tw:justify-center tw:rounded-xl tw:border tw:transition hover:tw:border-emerald-400 app-text-muted"
            style="border-color: var(--app-border)"
            @click="pendingQuantity = Math.max(1, pendingQuantity - 1)"
          >
            <iconify icon="ph:minus-bold" class="tw:h-4 tw:w-4" />
          </button>
          <span class="tw:min-w-10 tw:text-center tw:text-xl tw:font-bold">{{ pendingQuantity }}</span>
          <button
            class="tw:flex tw:h-9 tw:w-9 tw:items-center tw:justify-center tw:rounded-xl tw:border tw:transition hover:tw:border-emerald-400 app-text-muted"
            style="border-color: var(--app-border)"
            @click="pendingQuantity++"
          >
            <iconify icon="ph:plus-bold" class="tw:h-4 tw:w-4" />
          </button>
        </div>
      </div>

      <!-- Temperature -->
      <div v-if="selectedProduct?.hasTemperatureOption">
        <p class="tw:mb-2 tw:text-sm tw:font-semibold">{{ t("orders.create.optionsDialog.temperature") }}</p>
        <div class="tw:flex tw:gap-2">
          <prime-button
            v-for="opt in temperatureOptions"
            variant="outlined"
            class="tw:w-full"
            :severity="pendingOptions.temperature === opt.value ? 'primary' : 'secondary'"
            @click="setTemperature(opt.value)"
          >
            <iconify :icon="opt.icon" />
            <span>{{ opt.label }}</span>
          </prime-button>
        </div>
      </div>

      <!-- Ice level -->
      <div v-if="selectedProduct?.hasIceLevelOption && pendingOptions.temperature !== DRINK_TEMPERATURE.HOT">
        <p class="tw:mb-2 tw:text-sm tw:font-semibold">{{ t("orders.create.optionsDialog.iceLevel") }}</p>
        <div class="tw:grid tw:grid-cols-2 tw:gap-2">
          <prime-button
            v-for="opt in iceLevelOptions"
            :key="opt.value"
            variant="outlined"
            class="tw:w-full"
            :severity="pendingOptions.iceLevel === opt.value ? 'primary' : 'secondary'"
            @click="pendingOptions.iceLevel = opt.value"
          >
            <iconify :icon="opt.icon" />
            <span>{{ opt.label }}</span>
          </prime-button>
        </div>
      </div>

      <!-- Sugar level -->
      <div v-if="selectedProduct?.hasSugarLevelOption && pendingOptions.temperature !== DRINK_TEMPERATURE.HOT">
        <p class="tw:mb-2 tw:text-sm tw:font-semibold">{{ t("orders.create.optionsDialog.sugarLevel") }}</p>
        <div class="tw:grid tw:grid-cols-2 tw:gap-2">
          <prime-button
            v-for="opt in sugarLevelOptions"
            :key="opt.value"
            variant="outlined"
            class="tw:w-full"
            :severity="pendingOptions.sugarLevel === opt.value ? 'primary' : 'secondary'"
            @click="pendingOptions.sugarLevel = opt.value"
          >
            <iconify v-if="opt.icon" :icon="opt.icon" />
            <span>{{ opt.label }}</span>
          </prime-button>
        </div>
      </div>

      <!-- Serving -->
      <div>
        <p class="tw:mb-2 tw:text-sm tw:font-semibold">{{ t("orders.create.optionsDialog.serving") }}</p>
        <div class="tw:flex tw:gap-2">
          <prime-button
            v-for="servingType in servingOptions"
            variant="outlined"
            class="tw:w-full"
            :severity="pendingOptions.isTakeaway === servingType.value ? 'primary' : 'secondary'"
            @click="pendingOptions.isTakeaway = servingType.value"
          >
            <iconify :icon="servingType.icon" />
            <span>{{ servingType.label }}</span>
          </prime-button>
        </div>
      </div>

      <!-- Note -->
      <div class="tw:mt-4">
        <p class="tw:mb-2 tw:text-sm tw:font-semibold">{{ t("orders.create.optionsDialog.note") }}</p>
        <prime-textarea
          v-model="pendingOptions.note"
          :placeholder="t('orders.create.optionsDialog.notePlaceholder')"
          rows="2"
          class="tw:w-full"
          auto-resize
        />
      </div>
    </div>

    <template #footer>
      <prime-button severity="secondary" text @click="showOptionsDialog = false">
        <span>{{ t("orders.cancel") }}</span>
      </prime-button>
      <prime-button @click="confirmAddToCart">
        <iconify icon="prime:shopping-cart" />
        <span class="tw:ml-2">{{ t("orders.create.optionsDialog.addToCart") }}</span>
      </prime-button>
    </template>
  </prime-dialog>
</template>
