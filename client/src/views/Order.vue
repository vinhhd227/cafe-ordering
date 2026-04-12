<script setup>
import { computed, onMounted, ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useToast } from "primevue/usetoast";
import { getMenu } from "../services/product.service.js";
import {
  getOrCreateSession,
  getSessionSummary,
} from "../services/session.service.js";
import {
  placeOrder as placeOrderApi,
  updateOrderItem,
  validatePromotion,
  getPublicPromotions,
} from "../services/order.service.js";
import { useCartStore } from "../stores/cart.js";
import { getPublicTables } from "../services/table.service.js";

const cartStore = useCartStore();
const { t } = useI18n();
const toast = useToast();

const route = useRoute();
const router = useRouter();
const tableId = computed(() => Number(route.params.tableId));
const tableCode = ref(route.query.code || null);
const qrToken = computed(() => route.query.token || null);

/* ─── Menu ─────────────────────────────────────────────── */
const menu = ref([]);
const isLoading = ref(false);
const loadError = ref("");

/* ─── Session ──────────────────────────────────────────── */
const session = ref(null);
const sessionError = ref("");

/* ─── Image fallback ────────────────────────────────────── */
const failedImages = ref(new Set())
const onImageError = (id) => { failedImages.value = new Set([...failedImages.value, id]) }

/* ─── Options dialog ───────────────────────────────────── */
const showOptionsDialog = ref(false);
const selectedProduct = ref(null);
const pendingQuantity = ref(1);
const pendingOptions = ref({
  temperature: "COLD",
  iceLevel: "NORMAL",
  sugarLevel: "NORMAL",
  isTakeaway: false,
  note: "",
});

/* ─── Mobile cart sheet ─────────────────────────────────── */
const showMobileCart = ref(false);

/* ─── Category collapse ─────────────────────────────────── */
const collapsedCategories = ref({});
const toggleCategory = (id) => {
  collapsedCategories.value = {
    ...collapsedCategories.value,
    [id]: !collapsedCategories.value[id],
  };
};

/* ─── Order ────────────────────────────────────────────── */
const isOrdering = ref(false);
const orderError = ref("");

/* ─── Summary ──────────────────────────────────────────── */
const showSummary = ref(false);
const summary = ref(null);
const isSummaryLoading = ref(false);
const itemUpdating = ref(null);

/* ─── Promo code ────────────────────────────────────────────────── */
const promoCode    = ref("");
const promoApplying = ref(false);
const promoResult  = ref(null);   // ValidatePromotionResult from API
const promoError   = ref("");

/* ─── Find promotions dialog ────────────────────────────────────── */
const findPromosVisible = ref(false);
const publicPromos = ref([]);
const publicPromosLoading = ref(false);

const formatPromotionValue = (promo) => {
  if (promo.discountType === "PERCENTAGE") return t('order.percentOff', { value: promo.discountValue });
  if (promo.discountType === "FIXED")
    return `-${new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND", maximumFractionDigits: 0 }).format(promo.discountValue)}`;
  if (promo.discountType === "BUY_X_GET_Y")
    return t('order.buyXgetY', { buy: promo.buyQuantity, get: promo.getQuantity });
  return "";
};

const promoDisableReason = (promo) => {
  const now = new Date();
  if (!promo.isActive) return t("cart.promo.inactive");
  if (new Date(promo.startDate) > now) return t("cart.promo.notStarted");
  if (promo.endDate && new Date(promo.endDate) < now) return t("cart.promo.expired");
  if (promo.maxUsage && promo.currentUsage >= promo.maxUsage) return t("cart.promo.noUsesLeft");
  if (promo.minOrderAmount && cartStore.total < promo.minOrderAmount)
    return t("cart.promo.orderTooLow");
  return null;
};

const isPromoAvailable = (promo) => promoDisableReason(promo) === null;

const openFindPromosDialog = async () => {
  findPromosVisible.value = true;
  if (publicPromos.value.length > 0) return;
  publicPromosLoading.value = true;
  try {
    const res = await getPublicPromotions();
    publicPromos.value = res.data ?? [];
  } finally {
    publicPromosLoading.value = false;
  }
};

const selectPromo = async (promo) => {
  findPromosVisible.value = false;
  promoCode.value = promo.code;
  await applyPromoCode();
};

const applyPromoCode = async () => {
  if (!promoCode.value.trim()) return;
  promoApplying.value = true;
  promoError.value = "";
  promoResult.value = null;
  try {
    const res = await validatePromotion(promoCode.value.trim(), cartStore.total);
    const data = res?.data;
    if (!data?.isApplicable) {
      promoError.value = data?.message || t('cart.promo.notApplicable');
    } else {
      promoResult.value = data;
    }
  } catch (e) {
    promoError.value = e?.response?.data?.title || t('cart.promo.invalid');
  } finally {
    promoApplying.value = false;
  }
};

const clearPromo = () => {
  promoCode.value = "";
  promoResult.value = null;
  promoError.value = "";
};

/* ─── Helpers ──────────────────────────────────────────── */
const formatPrice = (value) =>
  new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "VND",
    maximumFractionDigits: 0,
  }).format(value ?? 0);

const optionsLabel = (options) => {
  if (!options) return "";
  const parts = [];
  if (options.temperature) parts.push(t(`product.temperature.${options.temperature}`));
  if (options.iceLevel && options.iceLevel !== "NORMAL")
    parts.push(t(`product.iceLevel.${options.iceLevel}`));
  if (options.sugarLevel && options.sugarLevel !== "NORMAL")
    parts.push(t(`product.sugarLevel.${options.sugarLevel}`));
  if (options.isTakeaway) parts.push(t("product.serving.takeaway"));
  return parts.join(" · ");
};

const noteLabel = (options) => options?.note?.trim() || null;

/* ─── Session ──────────────────────────────────────────── */
const fetchSession = async () => {
  if (!tableId.value) {
    router.replace({ name: 'tableSelect' });
    return;
  }
  if (!qrToken.value) {
    sessionError.value = t('order.invalidQrCode');
    return;
  }
  if (!tableCode.value) {
    const tables = await getPublicTables().catch(() => []);
    const found = tables.find(t => t.id === tableId.value);
    if (found) tableCode.value = found.code;
  }
  try {
    session.value = await getOrCreateSession(tableId.value, qrToken.value);
  } catch (err) {
    if (err?.response?.status === 403) {
      sessionError.value = t('order.invalidQrCode');
    } else {
      sessionError.value = t('order.sessionError');
    }
  }
};

/* ─── Menu ─────────────────────────────────────────────── */
const fetchProducts = async () => {
  isLoading.value = true;
  loadError.value = "";
  try {
    const data = await getMenu();
    const categories =
      (Array.isArray(data) ? data : null) ??
      data?.categories ??
      data?.Categories ??
      data?.data?.categories ??
      data?.data?.Categories ??
      [];
    menu.value = Array.isArray(categories)
      ? categories.map((category) => {
          const rawProducts = category.products ?? category.Products ?? [];
          const products = Array.isArray(rawProducts)
            ? rawProducts.map((p) => ({
                id: p.id ?? p.Id,
                name: p.name ?? p.Name,
                description: p.description ?? p.Description,
                price: p.price ?? p.Price,
                imageUrl: p.imageUrl ?? p.ImageUrl,
                hasTemperatureOption:
                  p.hasTemperatureOption ?? p.HasTemperatureOption ?? false,
                hasIceLevelOption:
                  p.hasIceLevelOption ?? p.HasIceLevelOption ?? false,
                hasSugarLevelOption:
                  p.hasSugarLevelOption ?? p.HasSugarLevelOption ?? false,
              }))
            : [];
          return {
            id: category.id ?? category.Id,
            name: category.name ?? category.Name,
            products,
          };
        })
      : [];
  } catch (error) {
    console.error("Failed to load menu:", error);
    loadError.value = t('order.menuLoadError');
  } finally {
    isLoading.value = false;
  }
};

/* ─── Cart ─────────────────────────────────────────────── */
const handleAddToCart = (product) => {
  selectedProduct.value = product;
  pendingOptions.value = {
    temperature: "COLD",
    iceLevel: "NORMAL",
    sugarLevel: "NORMAL",
    isTakeaway: false,
    note: "",
  };
  pendingQuantity.value = 1;
  showOptionsDialog.value = true;
};

const confirmAddToCart = () => {
  cartStore.addItem(
    selectedProduct.value,
    { ...pendingOptions.value },
    pendingQuantity.value,
  );
  showOptionsDialog.value = false;
  selectedProduct.value = null;
};

/* ─── Order ────────────────────────────────────────────── */
const submitOrder = async () => {
  if (!cartStore.items.length || !session.value) return;
  isOrdering.value = true;
  orderError.value = "";
  try {
    const sessionId = session.value.sessionId ?? session.value.id;
    const payload = {
      sessionId,
      promoCode: promoResult.value ? promoCode.value.trim() : null,
      items: cartStore.items.map((item) => ({
        productId: item.id,
        productName: item.name,
        unitPrice: item.price,
        quantity: item.quantity,
        temperature: item.options?.temperature ?? null,
        iceLevel: item.options?.iceLevel ?? null,
        sugarLevel: item.options?.sugarLevel ?? null,
        isTakeaway: item.options?.isTakeaway ?? false,
        note: item.options?.note?.trim() || null,
      })),
    };
    const result = await placeOrderApi(payload);
    cartStore.clear();
    clearPromo();
    showMobileCart.value = false;
    toast.add({
      severity: 'success',
      summary: t('order.orderPlacedTitle'),
      detail: t('order.orderPlaced', { number: result.orderNumber ?? result.OrderNumber }),
      life: 5000,
    });
  } catch (e) {
    orderError.value =
      e?.detail ?? e?.message ?? t('order.orderError');
  } finally {
    isOrdering.value = false;
  }
};

/* ─── Summary ──────────────────────────────────────────── */
const fetchSummary = async () => {
  if (!session.value) return;
  isSummaryLoading.value = true;
  try {
    const sessionId = session.value.sessionId ?? session.value.id;
    summary.value = await getSessionSummary(sessionId);
    showSummary.value = true;
  } catch {
    orderError.value = t('order.billLoadError');
  } finally {
    isSummaryLoading.value = false;
  }
};

/* ─── Item editing (summary) ────────────────────────────── */
const setClientItemQty = async (orderId, productId, newQty) => {
  if (!session.value) return;
  itemUpdating.value = `${orderId}-${productId}`;
  try {
    const sessionId = session.value.sessionId ?? session.value.id;
    await updateOrderItem(orderId, productId, newQty, sessionId);
    summary.value = await getSessionSummary(sessionId);
  } catch (e) {
    orderError.value = e?.detail ?? e?.message ?? t('order.updateFailed');
  } finally {
    itemUpdating.value = null;
  }
};

const decrementClientItem = (orderId, productId, currentQty) => {
  if (currentQty <= 1) {
    if (!confirm(t('order.removeItemConfirm'))) return;
    setClientItemQty(orderId, productId, 0);
  } else {
    setClientItemQty(orderId, productId, currentQty - 1);
  }
};

const removeClientItem = (orderId, productId) => {
  if (!confirm(t('order.removeItemConfirm'))) return;
  setClientItemQty(orderId, productId, 0);
};

onMounted(async () => {
  await fetchSession();
  await fetchProducts();
});
</script>

<template>
  <div :class="{ 'tw:pb-24': cartStore.count > 0 }">
    <!-- Session banner -->
    <div
      v-if="tableId"
      class="app-panel tw:mb-4 tw:flex tw:items-center tw:justify-between tw:rounded-2xl tw:border tw:px-6 tw:py-3"
    >
      <div class="tw:flex tw:items-center tw:gap-3">
        <iconify icon="ic:round-table-bar" class="tw:text-xl tw:text-emerald-600 tw:dark:text-emerald-400" />
        <span class="tw:text-sm tw:font-medium">{{ tableCode || `Table ${tableId}` }}</span>
        <span
          v-if="session"
          class="tw:rounded-full tw:bg-green-500/20 tw:px-2 tw:py-0.5 tw:text-xs tw:font-semibold tw:text-green-400"
          >{{ t('order.active') }}</span
        >
      </div>
      <prime-button v-if="session" severity="secondary" text :loading="isSummaryLoading" @click="fetchSummary">
        <iconify icon="prime:file" />
        <span>{{ t('order.viewBill') }}</span>
      </prime-button>
    </div>
    <div
      v-if="sessionError"
      class="tw:mb-4 tw:rounded-2xl tw:border tw:border-rose-500/30 tw:bg-rose-500/10 tw:px-6 tw:py-3 tw:text-sm tw:text-rose-400 tw:flex tw:items-center tw:justify-between tw:gap-4"
    >
      <span>{{ sessionError }}</span>
      <button type="button" class="tw:shrink-0 tw:text-emerald-600 tw:dark:text-emerald-400 tw:underline hover:tw:text-emerald-700 hover:tw:dark:text-emerald-300 tw:text-xs" @click="router.push({ name: 'tableSelect' })">
        {{ t('order.selectTable') }}
      </button>
    </div>

    <!-- Order error -->
    <div
      v-if="orderError"
      class="tw:mb-4 tw:rounded-2xl tw:border tw:border-rose-500/30 tw:bg-rose-500/10 tw:px-6 tw:py-3 tw:text-sm tw:text-rose-400"
    >
      {{ orderError }}
    </div>

    <div class="tw:grid tw:gap-6 tw:lg:grid-cols-12">
      <!-- ── Menu section ───────────────────────────────── -->
      <section class="tw:lg:col-span-8">
        <div class="tw:mb-4 tw:flex tw:items-center tw:justify-between">
          <h2 class="tw:text-xl tw:font-semibold">{{ t('order.todaysMenu') }}</h2>
          <prime-button severity="secondary" text @click="fetchProducts">
            <iconify icon="prime:refresh" />
            <span>{{ t('common.refresh') }}</span>
          </prime-button>
        </div>

        <!-- Loading skeleton -->
        <div v-if="isLoading" class="tw:space-y-3">
          <div
            v-for="skeleton in 5"
            :key="skeleton"
            class="tw:flex tw:animate-pulse tw:overflow-hidden tw:rounded-2xl tw:border tw:border-white/10 tw:bg-white/5"
          >
            <div class="tw:h-20 tw:w-24 tw:shrink-0 tw:bg-white/10"></div>
            <div class="tw:flex tw:flex-1 tw:items-center tw:gap-3 tw:px-3 tw:py-3">
              <div class="tw:flex-1 tw:space-y-2">
                <div class="tw:h-3.5 tw:w-2/3 tw:rounded tw:bg-white/15"></div>
                <div class="tw:h-3 tw:w-1/3 tw:rounded tw:bg-white/10"></div>
                <div class="tw:flex tw:gap-1">
                  <div class="tw:h-4 tw:w-14 tw:rounded-md tw:bg-white/10"></div>
                  <div class="tw:h-4 tw:w-12 tw:rounded-md tw:bg-white/10"></div>
                </div>
              </div>
              <div class="tw:h-10 tw:w-10 tw:shrink-0 tw:rounded-full tw:bg-white/10"></div>
            </div>
          </div>
        </div>

        <!-- Error -->
        <div
          v-else-if="loadError"
          class="tw:rounded-2xl tw:border tw:border-rose-500/30 tw:bg-rose-500/10 tw:p-4 tw:text-rose-400"
        >
          {{ loadError }}
        </div>

        <!-- Menu list -->
        <div v-else class="tw:space-y-6">
          <div v-for="category in menu" :key="category.id">
            <!-- Category header -->
            <button
              class="tw:mb-2 tw:flex tw:w-full tw:items-center tw:justify-between tw:gap-2 tw:rounded-xl tw:px-1 tw:py-1.5 tw:transition hover:tw:bg-white/5"
              @click="toggleCategory(category.id)"
            >
              <div class="tw:flex tw:items-center tw:gap-2">
                <h3 class="tw:text-lg tw:font-semibold">{{ category.name }}</h3>
                <span class="tw:text-xs app-text-subtle">{{ t('order.itemCount', { count: category.products?.length || 0 }) }}</span>
              </div>
              <iconify
                icon="heroicons-outline:chevron-down"
                class="tw:h-5 tw:w-5 tw:shrink-0 app-text-subtle tw:transition-transform tw:duration-200"
                :class="{ 'tw:-rotate-90': collapsedCategories[category.id] }"
              />
            </button>

            <!-- Products (collapsible) -->
            <div
              v-show="!collapsedCategories[category.id]"
              class="tw:grid tw:gap-3 tw:sm:grid-cols-2 tw:lg:grid-cols-3"
            >
              <article
                v-for="product in category.products || []"
                :key="product.id"
                class="app-panel tw:flex tw:flex-row tw:overflow-hidden tw:rounded-2xl tw:border tw:border-white/10 tw:shadow-sm tw:transition hover:tw:bg-white/5 tw:lg:flex-col"
              >
                <div class="tw:w-24 tw:shrink-0 tw:self-stretch tw:overflow-hidden tw:lg:h-44 tw:lg:w-full tw:lg:shrink-0">
                  <img
                    v-if="product.imageUrl && !failedImages.has(product.id)"
                    :src="product.imageUrl"
                    :alt="product.name"
                    class="tw:h-full tw:w-full tw:object-cover"
                    @error="onImageError(product.id)"
                  />
                  <div v-else class="tw:flex tw:h-full tw:w-full tw:items-center tw:justify-center tw:bg-white/5">
                    <iconify icon="ph:coffee-bold" class="tw:h-8 tw:w-8 tw:opacity-30 tw:lg:h-12 tw:lg:w-12" />
                  </div>
                </div>
                <div
                  class="tw:flex tw:flex-1 tw:items-center tw:gap-3 tw:px-3 tw:py-3 tw:lg:flex-col tw:lg:items-start tw:lg:gap-0 tw:lg:p-4"
                >
                  <div class="tw:min-w-0 tw:flex-1 tw:lg:w-full tw:lg:flex-none">
                    <h4 class="tw:line-clamp-2 tw:text-sm tw:font-semibold tw:leading-snug tw:lg:text-base">
                      {{ product.name }}
                    </h4>
                    <p class="tw:mt-1 tw:hidden tw:text-sm app-text-muted tw:lg:line-clamp-2 tw:lg:block">
                      {{ product.description || t('order.defaultDescription') }}
                    </p>
                    <p class="tw:mt-0.5 tw:text-xs tw:font-bold tw:text-emerald-600 tw:dark:text-emerald-400 tw:lg:mt-1.5 tw:lg:text-sm">
                      {{ formatPrice(product.price) }}
                    </p>
                    <div
                      v-if="product.hasTemperatureOption || product.hasIceLevelOption || product.hasSugarLevelOption"
                      class="tw:mt-1.5 tw:flex tw:flex-wrap tw:gap-1"
                    >
                      <span
                        v-if="product.hasTemperatureOption"
                        class="tw:rounded-md tw:bg-emerald-500/15 tw:px-1.5 tw:py-0.5 tw:text-[10px] tw:font-medium tw:text-emerald-600 tw:dark:text-emerald-400"
                        >{{ t('product.temperature.HOT') }}/{{ t('product.temperature.COLD') }}</span
                      >
                      <span
                        v-if="product.hasIceLevelOption"
                        class="tw:rounded-md tw:bg-sky-500/15 tw:px-1.5 tw:py-0.5 tw:text-[10px] tw:font-medium tw:text-sky-400"
                        >{{ t('product.optionsDialog.iceLevel') }}</span
                      >
                      <span
                        v-if="product.hasSugarLevelOption"
                        class="tw:rounded-md tw:bg-amber-500/15 tw:px-1.5 tw:py-0.5 tw:text-[10px] tw:font-medium tw:text-amber-400"
                        >{{ t('product.optionsDialog.sugarLevel') }}</span
                      >
                    </div>
                  </div>

                  <!-- Mobile: round + button -->
                  <div class="tw:shrink-0 tw:lg:hidden">
                    <button
                      class="tw:flex tw:h-10 tw:w-10 tw:items-center tw:justify-center tw:rounded-full tw:bg-emerald-500 tw:text-white tw:shadow-sm tw:transition active:tw:scale-95 hover:tw:bg-emerald-600"
                      @click="handleAddToCart(product)"
                    >
                      <iconify icon="heroicons-outline:plus" class="tw:h-5 tw:w-5" />
                    </button>
                  </div>

                  <!-- lg+: full-width button -->
                  <div class="tw:hidden tw:w-full tw:lg:mt-auto tw:lg:block tw:lg:pt-2">
                    <prime-button class="tw:w-full" @click="handleAddToCart(product)">
                      <iconify icon="prime:shopping-cart" />
                      <span>{{ t('product.optionsDialog.addToCart') }}</span>
                    </prime-button>
                  </div>
                </div>
              </article>

              <div
                v-if="!category.products || category.products.length === 0"
                class="tw:col-span-full tw:rounded-2xl tw:border tw:border-dashed tw:border-white/10 tw:p-6 tw:text-center app-text-muted"
              >
                {{ t('order.noCategoryItems') }}
              </div>
            </div>
          </div>
        </div>

        <div
          v-if="!isLoading && !loadError && menu.length === 0"
          class="tw:mt-6 tw:rounded-2xl tw:border tw:border-white/10 tw:bg-white/5 tw:p-6 tw:text-center app-text-muted"
        >
          {{ t('order.menuEmpty') }}
        </div>
      </section>

      <!-- ── Cart aside — desktop only ─────────────────────── -->
      <aside class="tw:hidden tw:lg:col-span-4 tw:lg:block tw:lg:self-start tw:lg:sticky tw:lg:top-[calc(5.25rem+1.5rem)]">
        <div class="app-panel tw:rounded-2xl tw:border tw:border-white/10 tw:p-5">
          <div class="tw:flex tw:items-center tw:justify-between">
            <h2 class="tw:text-xl tw:font-semibold">{{ t('order.cart') }}</h2>
            <span class="tw:rounded-full tw:bg-emerald-500/15 tw:px-3 tw:py-1 tw:text-xs tw:font-semibold tw:text-emerald-600 tw:dark:text-emerald-400">
              {{ t('order.cartItems', { count: cartStore.count }) }}
            </span>
          </div>

          <div
            v-if="cartStore.count === 0"
            class="tw:mt-6 tw:rounded-xl tw:border tw:border-dashed tw:border-white/10 tw:p-4 tw:text-center app-text-muted"
          >
            {{ t('order.cartEmpty') }}
          </div>

          <div v-else class="tw:mt-4 tw:space-y-4">
            <div
              v-for="(item, idx) in cartStore.items"
              :key="idx"
              class="tw:flex tw:items-start tw:justify-between tw:gap-3"
            >
              <div class="tw:min-w-0 tw:flex-1">
                <h3 class="tw:text-sm tw:font-semibold">{{ item.name }}</h3>
                <p class="tw:text-xs app-text-muted">{{ formatPrice(item.price) }} × {{ item.quantity }}</p>
                <p v-if="optionsLabel(item.options)" class="tw:mt-0.5 tw:text-xs tw:text-emerald-600 tw:dark:text-emerald-400">
                  {{ optionsLabel(item.options) }}
                </p>
                <p v-if="noteLabel(item.options)" class="tw:mt-0.5 tw:text-xs app-text-muted tw:italic">
                  {{ noteLabel(item.options) }}
                </p>
              </div>
              <div class="tw:flex tw:shrink-0 tw:items-center tw:gap-2">
                <prime-button class="p-button-sm p-button-rounded" @click="cartStore.removeItem(item)">
                  <iconify icon="heroicons-outline:minus"></iconify>
                </prime-button>
                <span class="tw:min-w-6 tw:text-center tw:text-sm tw:font-semibold">{{ item.quantity }}</span>
                <prime-button class="p-button-sm p-button-rounded" @click="cartStore.increaseItem(item)">
                  <iconify icon="heroicons-outline:plus"></iconify>
                </prime-button>
              </div>
            </div>

            <!-- Find promotions bar -->
            <div class="tw:flex tw:items-center tw:justify-between">
              <span class="tw:flex tw:items-center tw:gap-1.5 tw:text-xs app-text-subtle">
                <iconify icon="ph:ticket-bold" class="tw:text-green-500" />
                {{ t('cart.promo.findPromotions') }}
              </span>
              <button
                class="tw:flex tw:items-center tw:gap-1 tw:rounded-lg tw:px-2 tw:py-1 tw:text-xs app-text-muted tw:transition hover:tw:bg-white/5 hover:tw:text-emerald-600 hover:tw:dark:text-emerald-400"
                @click="openFindPromosDialog"
              >
                <iconify icon="ph:magnifying-glass-bold" />
                {{ t('cart.promo.browse') }}
              </button>
            </div>

            <!-- Promo code input -->
            <div>
              <!-- Applied state -->
              <div
                v-if="promoResult"
                class="tw:flex tw:items-center tw:justify-between tw:rounded-xl tw:border tw:border-green-500/30 tw:bg-green-500/10 tw:px-3 tw:py-2"
              >
                <div class="tw:flex tw:items-center tw:gap-2 tw:min-w-0">
                  <iconify icon="ph:tag-bold" class="tw:shrink-0 tw:text-green-400" />
                  <span class="tw:font-medium tw:text-sm tw:text-green-400">{{ promoCode }}</span>
                  <span class="tw:text-xs app-text-muted tw:truncate">{{ promoResult.name }}</span>
                </div>
                <button
                  class="tw:ml-2 tw:shrink-0 tw:rounded-lg tw:p-1 app-text-subtle tw:transition hover:tw:bg-red-500/10 hover:tw:text-red-400"
                  @click="clearPromo"
                >
                  <iconify icon="ph:x-bold" />
                </button>
              </div>
              <!-- Input state -->
              <div v-else class="tw:space-y-1">
                <div class="tw:flex tw:gap-2">
                  <input
                    v-model="promoCode"
                    type="text"
                    :placeholder="t('cart.promo.placeholder')"
                    class="app-input tw:flex-1 tw:rounded-xl tw:border tw:px-3 tw:py-2 tw:text-sm tw:uppercase tw:outline-none focus:tw:border-emerald-400"
                    @keyup.enter="applyPromoCode"
                  />
                  <button
                    class="tw:rounded-xl tw:bg-emerald-500 tw:px-3 tw:py-2 tw:text-sm tw:font-medium tw:text-white tw:transition hover:tw:bg-emerald-600 disabled:tw:opacity-50"
                    :disabled="promoApplying || !promoCode.trim()"
                    @click="applyPromoCode"
                  >
                    {{ t('cart.promo.apply') }}
                  </button>
                </div>
                <p v-if="promoError" class="tw:text-xs tw:text-rose-400">{{ promoError }}</p>
              </div>
            </div>

            <div class="tw:rounded-xl tw:bg-white/5 tw:p-4">
              <div class="tw:flex tw:items-center tw:justify-between tw:text-sm app-text-muted">
                <span>{{ t('order.subtotal') }}</span>
                <span class="tw:font-semibold tw:text-white">{{ formatPrice(cartStore.total) }}</span>
              </div>
              <div
                v-if="promoResult?.estimatedDiscount"
                class="tw:mt-1 tw:flex tw:items-center tw:justify-between tw:text-sm tw:text-green-400"
              >
                <span>{{ t('order.discount', { code: promoCode }) }}</span>
                <span class="tw:font-semibold">−{{ formatPrice(promoResult.estimatedDiscount) }}</span>
              </div>
              <div class="tw:mt-2 tw:flex tw:items-center tw:justify-between tw:text-xs app-text-subtle">
                <span>{{ t('order.serviceFee') }}</span>
                <span>{{ t('order.free') }}</span>
              </div>
              <div class="tw:mt-4 tw:flex tw:items-center tw:justify-between tw:text-lg tw:font-semibold">
                <span>{{ t('order.total') }}</span>
                <span>{{ formatPrice(promoResult?.estimatedDiscount ? Math.max(0, cartStore.total - promoResult.estimatedDiscount) : cartStore.total) }}</span>
              </div>
            </div>

            <prime-button class="tw:w-full" :disabled="cartStore.count === 0 || !session" :loading="isOrdering" @click="submitOrder">
              <iconify icon="prime:check" />
              <span>{{ t('order.placeOrder') }}</span>
            </prime-button>
            <div v-if="!session && cartStore.count > 0" class="tw:text-center tw:text-xs tw:text-rose-400 tw:space-y-1">
              <p>{{ t('order.tableNotConnected') }}</p>
              <button type="button" class="tw:text-emerald-600 tw:dark:text-emerald-400 tw:underline hover:tw:text-emerald-700 hover:tw:dark:text-emerald-300 tw:text-xs" @click="router.push({ name: 'tableSelect' })">
                {{ t('order.selectTable') }}
              </button>
            </div>
          </div>
        </div>
      </aside>
    </div>
  </div>

  <!-- ── Mobile sticky cart bar ── -->
  <div
    v-if="cartStore.count > 0"
    class="tw:fixed tw:inset-x-0 tw:bottom-0 tw:z-40 tw:border-t tw:border-white/10 tw:bg-black/80 tw:px-4 tw:py-3 tw:shadow-lg tw:backdrop-blur tw:lg:hidden"
  >
    <div class="tw:mx-auto tw:flex tw:max-w-xl tw:items-center tw:gap-3">
      <div class="tw:relative tw:shrink-0">
        <iconify icon="heroicons-outline:shopping-cart" class="tw:h-6 tw:w-6 tw:text-white" />
        <span
          class="tw:absolute tw:-right-2 tw:-top-2 tw:flex tw:h-4 tw:w-4 tw:items-center tw:justify-center tw:rounded-full tw:bg-emerald-500 tw:text-[10px] tw:font-bold tw:text-white"
        >
          {{ cartStore.count }}
        </span>
      </div>
      <span class="tw:flex-1 tw:text-sm tw:font-semibold tw:text-white">{{ formatPrice(cartStore.total) }}</span>
      <prime-button size="small" @click="showMobileCart = true">
        <iconify icon="prime:shopping-cart" />
        <span>{{ t('order.viewCart') }}</span>
      </prime-button>
    </div>
  </div>

  <!-- ── Mobile cart dialog ─────────────────────────────────── -->
  <prime-dialog
    v-model:visible="showMobileCart"
    :header="t('order.cart')"
    modal
    position="bottom"
    :style="{ width: '100%', maxWidth: '30rem', marginBottom: '0' }"
  >
    <div v-if="cartStore.count === 0" class="tw:py-6 tw:text-center app-text-muted">
      {{ t('order.cartEmptyShort') }}
    </div>
    <div v-else class="tw:space-y-4">
      <div
        v-for="(item, idx) in cartStore.items"
        :key="idx"
        class="tw:flex tw:items-start tw:justify-between tw:gap-3"
      >
        <div class="tw:min-w-0 tw:flex-1">
          <h3 class="tw:text-sm tw:font-semibold">{{ item.name }}</h3>
          <p class="tw:text-xs app-text-muted">{{ formatPrice(item.price) }} × {{ item.quantity }}</p>
          <p v-if="optionsLabel(item.options)" class="tw:mt-0.5 tw:text-xs tw:text-emerald-600 tw:dark:text-emerald-400">
            {{ optionsLabel(item.options) }}
          </p>
          <p v-if="noteLabel(item.options)" class="tw:mt-0.5 tw:text-xs app-text-muted tw:italic">
            {{ noteLabel(item.options) }}
          </p>
        </div>
        <div class="tw:flex tw:shrink-0 tw:items-center tw:gap-2">
          <prime-button class="p-button-sm p-button-rounded" @click="cartStore.removeItem(item)">
            <iconify icon="heroicons-outline:minus" />
          </prime-button>
          <span class="tw:min-w-6 tw:text-center tw:text-sm tw:font-semibold">{{ item.quantity }}</span>
          <prime-button class="p-button-sm p-button-rounded" @click="cartStore.increaseItem(item)">
            <iconify icon="heroicons-outline:plus" />
          </prime-button>
        </div>
      </div>

      <!-- Promo code (mobile) -->
      <div class="tw:flex tw:items-center tw:justify-between">
        <span class="tw:flex tw:items-center tw:gap-1.5 tw:text-xs app-text-subtle">
          <iconify icon="ph:ticket-bold" class="tw:text-green-500" />
          {{ t('cart.promo.findPromotions') }}
        </span>
        <button
          class="tw:flex tw:items-center tw:gap-1 tw:rounded-lg tw:px-2 tw:py-1 tw:text-xs app-text-muted tw:transition hover:tw:bg-white/5 hover:tw:text-emerald-600 hover:tw:dark:text-emerald-400"
          @click="openFindPromosDialog"
        >
          <iconify icon="ph:magnifying-glass-bold" />
          {{ t('cart.promo.browse') }}
        </button>
      </div>
      <div>
        <!-- Applied state -->
        <div
          v-if="promoResult"
          class="tw:flex tw:items-center tw:justify-between tw:rounded-xl tw:border tw:border-green-500/30 tw:bg-green-500/10 tw:px-3 tw:py-2"
        >
          <div class="tw:flex tw:items-center tw:gap-2 tw:min-w-0">
            <iconify icon="ph:tag-bold" class="tw:shrink-0 tw:text-green-400" />
            <span class="tw:font-medium tw:text-sm tw:text-green-400">{{ promoCode }}</span>
            <span class="tw:text-xs app-text-muted tw:truncate">{{ promoResult.name }}</span>
          </div>
          <button
            class="tw:ml-2 tw:shrink-0 tw:rounded-lg tw:p-1 app-text-subtle tw:transition hover:tw:bg-red-500/10 hover:tw:text-red-400"
            @click="clearPromo"
          >
            <iconify icon="ph:x-bold" />
          </button>
        </div>
        <!-- Input state -->
        <div v-else class="tw:space-y-1">
          <div class="tw:flex tw:gap-2">
            <input
              v-model="promoCode"
              type="text"
              :placeholder="t('cart.promo.placeholder')"
              class="app-input tw:flex-1 tw:rounded-xl tw:border tw:px-3 tw:py-2 tw:text-sm tw:uppercase tw:outline-none focus:tw:border-emerald-400"
              @keyup.enter="applyPromoCode"
            />
            <button
              class="tw:rounded-xl tw:bg-emerald-500 tw:px-3 tw:py-2 tw:text-sm tw:font-medium tw:text-white tw:transition hover:tw:bg-emerald-600 disabled:tw:opacity-50"
              :disabled="promoApplying || !promoCode.trim()"
              @click="applyPromoCode"
            >
              {{ t('cart.promo.apply') }}
            </button>
          </div>
          <p v-if="promoError" class="tw:text-xs tw:text-rose-400">{{ promoError }}</p>
        </div>
      </div>

      <div class="tw:rounded-xl tw:bg-white/5 tw:p-4">
        <div class="tw:flex tw:items-center tw:justify-between tw:text-sm app-text-muted">
          <span>{{ t('order.subtotal') }}</span>
          <span class="tw:font-semibold tw:text-white">{{ formatPrice(cartStore.total) }}</span>
        </div>
        <div
          v-if="promoResult?.estimatedDiscount"
          class="tw:mt-1 tw:flex tw:items-center tw:justify-between tw:text-sm tw:text-green-400"
        >
          <span>{{ t('order.discount', { code: promoCode }) }}</span>
          <span class="tw:font-semibold">−{{ formatPrice(promoResult.estimatedDiscount) }}</span>
        </div>
        <div class="tw:mt-2 tw:flex tw:items-center tw:justify-between tw:text-xs app-text-subtle">
          <span>{{ t('order.serviceFee') }}</span>
          <span>{{ t('order.free') }}</span>
        </div>
        <div class="tw:mt-4 tw:flex tw:items-center tw:justify-between tw:text-lg tw:font-semibold">
          <span>{{ t('order.total') }}</span>
          <span class="tw:text-emerald-600 tw:dark:text-emerald-400">{{ formatPrice(promoResult?.estimatedDiscount ? Math.max(0, cartStore.total - promoResult.estimatedDiscount) : cartStore.total) }}</span>
        </div>
      </div>

      <div v-if="!session" class="tw:text-center tw:text-xs tw:text-rose-400 tw:space-y-1">
        <p>{{ t('order.tableNotConnected') }}</p>
        <button type="button" class="tw:text-emerald-600 tw:dark:text-emerald-400 tw:underline hover:tw:text-emerald-700 hover:tw:dark:text-emerald-300 tw:text-xs" @click="router.push({ name: 'tableSelect' })">
          {{ t('order.selectTable') }}
        </button>
      </div>
    </div>

    <template #footer>
      <prime-button :label="t('order.keepBrowsing')" severity="secondary" text @click="showMobileCart = false" />
      <prime-button :disabled="cartStore.count === 0 || !session" :loading="isOrdering" @click="submitOrder">
        <iconify icon="prime:check" />
        <span>{{ t('order.placeOrder') }}</span>
      </prime-button>
    </template>
  </prime-dialog>

  <!-- ── Options Dialog ─────────────────────────────────── -->
  <prime-dialog
    v-model:visible="showOptionsDialog"
    modal
    :style="{ width: '52rem', maxWidth: '95vw' }"
    :pt="{ header: { class: 'tw:justify-end! tw:p-2!' }, content: { class: 'tw:p-0! tw:overflow-hidden' } }"
    @hide="selectedProduct = null"
  >
    <div class="tw:flex tw:min-h-[28rem] tw:flex-col tw:sm:flex-row">
      <!-- LEFT: Image + Info side by side -->
      <div
        class="tw:flex tw:flex-row tw:border-b tw:border-white/10 tw:sm:w-5/12 tw:sm:border-b-0 tw:sm:border-r"
      >
        <!-- Image 50% -->
        <div class="tw:relative tw:w-1/2 tw:shrink-0 tw:overflow-hidden tw:bg-white/10">
          <img
            v-if="selectedProduct?.imageUrl && !failedImages.has(selectedProduct.id)"
            :src="selectedProduct.imageUrl"
            :alt="selectedProduct?.name"
            class="tw:h-full tw:w-full tw:object-contain"
            @error="onImageError(selectedProduct.id)"
          />
          <div v-else class="tw:flex tw:h-full tw:w-full tw:items-center tw:justify-center app-text-subtle">
            <iconify icon="ph:coffee-bold" class="tw:h-20 tw:w-20" />
          </div>
        </div>

        <!-- Info 50% -->
        <div class="tw:flex tw:w-1/2 tw:flex-col tw:gap-2 tw:p-4 tw:bg-white/5">
          <h3 class="tw:text-lg tw:font-bold tw:leading-snug">{{ selectedProduct?.name }}</h3>
          <p class="tw:text-xl tw:font-semibold tw:text-emerald-600 tw:dark:text-emerald-400">
            {{ selectedProduct ? formatPrice(selectedProduct.price) : '' }}
          </p>
          <p v-if="selectedProduct?.description" class="tw:text-sm tw:leading-relaxed app-text-muted">
            {{ selectedProduct.description }}
          </p>
          <div
            v-if="selectedProduct?.estimatedPrepMinutes"
            class="tw:mt-auto tw:flex tw:items-center tw:gap-1 tw:pt-2 tw:text-xs app-text-subtle"
          >
            <iconify icon="heroicons-outline:clock" class="tw:h-4 tw:w-4" />
            <span>{{ t('order.prepTime', { minutes: selectedProduct.estimatedPrepMinutes }) }}</span>
          </div>
        </div>
      </div>

      <!-- RIGHT: Options + actions -->
      <div class="tw:flex tw:flex-1 tw:flex-col tw:p-5">
        <div class="tw:flex-1 tw:space-y-5">
          <!-- Temperature -->
          <div v-if="selectedProduct?.hasTemperatureOption">
            <p class="tw:mb-2 tw:text-sm tw:font-semibold">{{ t('product.optionsDialog.temperature') }}</p>
            <div class="tw:flex tw:gap-2">
              <prime-button
                v-for="opt in [
                  { value: 'HOT',  label: t('product.temperature.HOT'),  icon: 'ph:thermometer-hot-light' },
                  { value: 'COLD', label: t('product.temperature.COLD'), icon: 'ph:thermometer-cold-light' },
                ]"
                :key="opt.value"
                variant="outlined"
                class="tw:w-full"
                :severity="pendingOptions.temperature === opt.value ? 'primary' : 'secondary'"
                @click="
                  pendingOptions.temperature = opt.value;
                  if (opt.value === 'HOT') {
                    pendingOptions.iceLevel = 'LESS';
                    pendingOptions.sugarLevel = 'NORMAL';
                  }
                "
                size="small"
              >
                <iconify :icon="opt.icon" />
                <span>{{ opt.label }}</span>
              </prime-button>
            </div>
          </div>

          <!-- Ice level — only shown when Cold -->
          <div v-if="selectedProduct?.hasIceLevelOption && pendingOptions.temperature !== 'HOT'">
            <p class="tw:mb-2 tw:text-sm tw:font-semibold">{{ t('product.optionsDialog.iceLevel') }}</p>
            <div class="tw:grid tw:grid-cols-3 tw:gap-2">
              <prime-button
                v-for="opt in [
                  { value: 'LESS',   label: t('product.iceLevel.LESS'),   icon: 'game-icons:ice-cube' },
                  { value: 'NORMAL', label: t('product.iceLevel.NORMAL'), icon: 'flowbite:cubes-stacked-solid' },
                  { value: 'MORE',   label: t('product.iceLevel.MORE'),   icon: 'fa6-solid:cubes-stacked' },
                ]"
                :key="opt.value"
                variant="outlined"
                class="tw:w-full"
                :severity="pendingOptions.iceLevel === opt.value ? 'primary' : 'secondary'"
                @click="pendingOptions.iceLevel = opt.value"
                size="small"
              >
                <iconify :icon="opt.icon" />
                <span>{{ opt.label }}</span>
              </prime-button>
            </div>
          </div>

          <!-- Sugar level — only shown when Cold -->
          <div v-if="selectedProduct?.hasSugarLevelOption && pendingOptions.temperature !== 'HOT'">
            <p class="tw:mb-2 tw:text-sm tw:font-semibold">{{ t('product.optionsDialog.sugarLevel') }}</p>
            <div class="tw:grid tw:grid-cols-3 tw:gap-2">
              <prime-button
                v-for="opt in [
                  { value: 'LESS',   label: t('product.sugarLevel.LESS'),   icon: 'ph:drop-bold' },
                  { value: 'NORMAL', label: t('product.sugarLevel.NORMAL'), icon: 'ph:drop-half-bold' },
                  { value: 'MORE',   label: t('product.sugarLevel.MORE'),   icon: 'ph:drop-fill' },
                ]"
                :key="opt.value"
                variant="outlined"
                class="tw:w-full"
                :severity="pendingOptions.sugarLevel === opt.value ? 'primary' : 'secondary'"
                @click="pendingOptions.sugarLevel = opt.value"
                size="small"
              >
                <iconify :icon="opt.icon" />
                <span>{{ opt.label }}</span>
              </prime-button>
            </div>
          </div>

          <!-- Dine-in / Takeaway -->
          <div>
            <p class="tw:mb-2 tw:text-sm tw:font-semibold">{{ t('product.optionsDialog.serving') }}</p>
            <div class="tw:flex tw:gap-2">
              <prime-button
                v-for="opt in [
                  { value: false, label: t('product.serving.dineIn'),   icon: 'tdesign:drink-filled' },
                  { value: true,  label: t('product.serving.takeaway'), icon: 'streamline:coffee-takeaway-cup-remix' },
                ]"
                :key="String(opt.value)"
                variant="outlined"
                class="tw:w-full"
                :severity="pendingOptions.isTakeaway === opt.value ? 'primary' : 'secondary'"
                @click="pendingOptions.isTakeaway = opt.value"
                size="small"
              >
                <iconify :icon="opt.icon" />
                <span>{{ opt.label }}</span>
              </prime-button>
            </div>
          </div>

          <!-- Note -->
          <div>
            <p class="tw:mb-2 tw:text-sm tw:font-semibold">{{ t('product.optionsDialog.note') }}</p>
            <textarea
              v-model="pendingOptions.note"
              :placeholder="t('product.optionsDialog.notePlaceholder')"
              rows="2"
              class="tw:w-full tw:rounded-xl tw:border tw:border-white/15 tw:bg-white/5 tw:px-3 tw:py-2 tw:text-sm tw:resize-none tw:outline-none focus:tw:border-emerald-400/50 placeholder:tw:text-white/30"
            />
          </div>

          <!-- Quantity + Add -->
          <div class="tw:border-t tw:border-white/10 tw:pt-4">
            <p class="tw:mb-2 tw:text-sm tw:font-semibold">{{ t('product.optionsDialog.quantity') }}</p>
            <div class="tw:flex tw:items-center tw:justify-between tw:gap-3">
            <div class="tw:flex tw:items-center tw:gap-3">
              <button
                class="tw:flex tw:h-9 tw:w-9 tw:items-center tw:justify-center tw:rounded-xl tw:border tw:border-white/15 app-text-muted tw:transition hover:tw:border-emerald-400/50"
                @click="pendingQuantity = Math.max(1, pendingQuantity - 1)"
              >
                <iconify icon="ph:minus-bold" class="tw:h-4 tw:w-4" />
              </button>
              <span class="tw:min-w-8 tw:text-center tw:text-xl tw:font-bold">{{ pendingQuantity }}</span>
              <button
                class="tw:flex tw:h-9 tw:w-9 tw:items-center tw:justify-center tw:rounded-xl tw:border tw:border-white/15 app-text-muted tw:transition hover:tw:border-emerald-400/50"
                @click="pendingQuantity++"
              >
                <iconify icon="ph:plus-bold" class="tw:h-4 tw:w-4" />
              </button>
            </div>
            <prime-button @click="confirmAddToCart">
              <iconify icon="prime:shopping-cart" />
              <span class="tw:ml-2">{{ t('product.optionsDialog.addToCart') }}</span>
            </prime-button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </prime-dialog>

  <!-- ── Bill / Summary Dialog ─────────────────────────────────── -->
  <prime-dialog v-model:visible="showSummary" :header="t('order.tableBill')" modal :style="{ width: '36rem' }">
    <div v-if="summary">
      <div
        v-for="order in summary.orders ?? summary.Orders ?? []"
        :key="order.orderId ?? order.OrderId"
        class="tw:mb-4 tw:rounded-xl tw:border tw:border-white/10 tw:p-4"
      >
        <div class="tw:flex tw:items-center tw:justify-between">
          <div class="tw:flex tw:items-center tw:gap-2">
            <span class="tw:text-sm tw:font-semibold">{{ order.orderNumber ?? order.OrderNumber }}</span>
            <span
              v-if="(order.status ?? order.Status) === 'Pending'"
              class="tw:rounded-full tw:bg-amber-500/20 tw:px-2 tw:py-0.5 tw:text-xs tw:font-semibold tw:text-amber-400"
              >{{ t('order.pending') }}</span
            >
          </div>
          <span class="tw:text-sm tw:font-semibold tw:text-emerald-600 tw:dark:text-emerald-400">
            {{ formatPrice(order.totalAmount ?? order.TotalAmount) }}
          </span>
        </div>
        <ul class="tw:mt-2 tw:space-y-1.5">
          <li
            v-for="item in order.items ?? order.Items ?? []"
            :key="(item.productId ?? item.ProductId) + String(item.productName ?? item.ProductName)"
            class="tw:flex tw:items-center tw:justify-between tw:gap-2 tw:text-sm app-text-muted"
            :class="{
              'tw:opacity-50 tw:pointer-events-none':
                itemUpdating === `${order.orderId ?? order.OrderId}-${item.productId ?? item.ProductId}`,
            }"
          >
            <span class="tw:flex-1">{{ item.productName ?? item.ProductName }}</span>
            <!-- Edit controls — Pending orders only -->
            <div v-if="(order.status ?? order.Status) === 'Pending'" class="tw:flex tw:items-center tw:gap-1">
              <button
                class="tw:flex tw:h-6 tw:w-6 tw:items-center tw:justify-center tw:rounded tw:border tw:border-white/15 app-text-subtle tw:transition hover:tw:border-emerald-400/50 hover:tw:text-emerald-600 hover:tw:dark:text-emerald-400"
                @click="decrementClientItem(order.orderId ?? order.OrderId, item.productId ?? item.ProductId, item.quantity ?? item.Quantity)"
              >
                <iconify icon="heroicons-outline:minus" class="tw:h-3 tw:w-3" />
              </button>
              <span class="tw:min-w-5 tw:text-center tw:font-semibold">{{ item.quantity ?? item.Quantity }}</span>
              <button
                class="tw:flex tw:h-6 tw:w-6 tw:items-center tw:justify-center tw:rounded tw:border tw:border-white/15 app-text-subtle tw:transition hover:tw:border-emerald-400/50 hover:tw:text-emerald-600 hover:tw:dark:text-emerald-400"
                @click="setClientItemQty(order.orderId ?? order.OrderId, item.productId ?? item.ProductId, (item.quantity ?? item.Quantity) + 1)"
              >
                <iconify icon="heroicons-outline:plus" class="tw:h-3 tw:w-3" />
              </button>
              <button
                class="tw:ml-1 tw:flex tw:h-6 tw:w-6 tw:items-center tw:justify-center tw:rounded tw:border tw:border-rose-500/30 tw:text-rose-400 tw:transition hover:tw:border-rose-500 hover:tw:text-rose-500"
                @click="removeClientItem(order.orderId ?? order.OrderId, item.productId ?? item.ProductId)"
              >
                <iconify icon="heroicons-outline:trash" class="tw:h-3 tw:w-3" />
              </button>
            </div>
            <span v-else class="tw:font-medium">× {{ item.quantity ?? item.Quantity }}</span>
            <span class="tw:shrink-0">{{ formatPrice((item.unitPrice ?? item.UnitPrice) * (item.quantity ?? item.Quantity)) }}</span>
          </li>
        </ul>
      </div>

      <div class="tw:rounded-xl tw:bg-white/5 tw:p-4">
        <div class="tw:flex tw:items-center tw:justify-between tw:text-lg tw:font-semibold">
          <span>{{ t('order.grandTotal') }}</span>
          <span class="tw:text-emerald-600 tw:dark:text-emerald-400">{{ formatPrice(summary.grandTotal ?? summary.GrandTotal ?? 0) }}</span>
        </div>
      </div>
    </div>
    <div v-else class="tw:py-8 tw:text-center app-text-muted">{{ t('order.noOrders') }}</div>

    <template #footer>
      <prime-button :label="t('common.close')" severity="secondary" @click="showSummary = false" />
    </template>
  </prime-dialog>

  <!-- Find Promotions Dialog -->
  <prime-dialog
    v-model:visible="findPromosVisible"
    :header="t('cart.promo.dialog.title')"
    modal
    :style="{ width: '26rem' }"
  >
    <div v-if="publicPromosLoading" class="tw:flex tw:items-center tw:justify-center tw:gap-2 tw:py-8 tw:text-sm app-text-subtle">
      <iconify icon="prime:spinner" class="tw:animate-spin" />
      {{ t('cart.promo.dialog.loading') }}
    </div>
    <div v-else-if="publicPromos.length === 0" class="tw:py-8 tw:text-center tw:text-sm app-text-subtle">
      {{ t('cart.promo.dialog.empty') }}
    </div>
    <div v-else class="tw:space-y-2">
      <div
        v-for="promo in publicPromos"
        :key="promo.id"
        class="tw:rounded-xl tw:border tw:p-3 tw:transition-colors"
        :class="
          isPromoAvailable(promo)
            ? 'tw:cursor-pointer tw:border-white/10 hover:tw:border-green-500/50 hover:tw:bg-green-500/10'
            : 'tw:cursor-not-allowed tw:border-white/5 tw:opacity-40'
        "
        @click="isPromoAvailable(promo) && selectPromo(promo)"
      >
        <div class="tw:flex tw:items-start tw:justify-between tw:gap-3">
          <div class="tw:min-w-0">
            <p class="tw:text-sm tw:font-semibold tw:leading-snug">{{ promo.name }}</p>
            <p class="tw:mt-0.5 tw:font-mono tw:text-xs app-text-subtle">{{ promo.code }}</p>
            <div class="tw:mt-1.5 tw:flex tw:flex-wrap tw:gap-x-3 tw:gap-y-0.5">
              <span v-if="promo.minOrderAmount" class="tw:text-xs app-text-subtle">
                {{ t('cart.promo.dialog.minAmount', { amount: formatPrice(promo.minOrderAmount) }) }}
              </span>
              <span v-if="promo.endDate" class="tw:text-xs app-text-subtle">
                {{ t('cart.promo.dialog.until', { date: new Date(promo.endDate).toLocaleDateString('vi-VN') }) }}
              </span>
              <span v-if="!isPromoAvailable(promo)" class="tw:text-xs tw:text-red-400">
                {{ promoDisableReason(promo) }}
              </span>
            </div>
          </div>
          <span class="tw:shrink-0 tw:text-sm tw:font-semibold tw:text-green-400">
            {{ formatPromotionValue(promo) }}
          </span>
        </div>
      </div>
    </div>
  </prime-dialog>
</template>

<style scoped></style>
