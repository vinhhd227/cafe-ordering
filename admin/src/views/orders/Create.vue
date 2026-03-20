<script setup>
import { getAdminMenu } from "@/services/menu.service";
import { createOrder, applyPromotionAdmin } from "@/services/order.service";
import { listTables, getOrCreateSession } from "@/services/table.service";
import { validatePromotion, getPromotions } from "@/services/promotion.service";
import { appCard } from "../../layout/ui";

const router = useRouter();
const toast = useToast();
const { t } = useI18n();

// ── Data ─────────────────────────────────────────────────────────
const tables = ref([]);
const menuCategories = ref([]);
const loadingMenu = ref(false);
const loadingTables = ref(false);

// ── Selection ────────────────────────────────────────────────────
const selectedTableId = ref(null);
const sessionId = ref(null);
const sessionHadExisting = ref(false);
const sessionLoading = ref(false);

// ── Options dialog ───────────────────────────────────────────────
const showOptionsDialog = ref(false);
const selectedProduct = ref(null);
const pendingQuantity = ref(1);
const pendingOptions = ref({
  temperature: null,
  iceLevel: null,
  sugarLevel: null,
  isTakeaway: false,
});

// ── Cart ─────────────────────────────────────────────────────────
const cart = ref([]);

// ── Filter + collapse ─────────────────────────────────────────────
const searchQuery = ref("");
const collapsedCategories = ref({});

const toggleCategory = (id) => {
  collapsedCategories.value[id] = !collapsedCategories.value[id];
};

// ── Guest count ──────────────────────────────────────────────────
const guestCount = ref(null);

// Mặc định = tổng qty đồ uống (bỏ qua free gift và đồ ăn kèm)
const defaultGuestCount = computed(() =>
  cart.value
    .filter((i) => !i.isFreeGift && !i.isAccompaniment)
    .reduce((acc, i) => acc + i.quantity, 0),
);

// ── Submit ───────────────────────────────────────────────────────
const placing = ref(false);
const errorMessage = ref("");

// ── Promotion state ──────────────────────────────────────────────
const promoCode = ref("");
const promoInfo = ref(null);
const promoLoading = ref(false);
const promoError = ref("");

// ── Find promotions dialog ────────────────────────────────────────
const findPromosVisible = ref(false);
const publicPromos = ref([]);
const publicPromosLoading = ref(false);
const lastAppliedPromo = ref(null); // stores full promo object for client-side discount estimate

// ── Free item picker (BUY_X_GET_Y partial trigger) ───────────────
const freeItemSelection = ref(null); // cart item picked as free

const freeItemPickerPool = computed(() => {
  if (!promoInfo.value) return [];

  // Use promoInfo (from validate API) as primary source; lastAppliedPromo as enrichment
  const info = promoInfo.value;
  if (info.discountType !== "BUY_X_GET_Y") return [];

  const buyQty = info.buyQuantity ?? lastAppliedPromo.value?.buyQuantity;
  const getQty = info.getQuantity ?? lastAppliedPromo.value?.getQuantity;
  if (!buyQty || !getQty) return [];

  const groupSize = buyQty + getQty;
  const scope = info.scope;
  const applicableProductIds =
    info.applicableProductIds ??
    lastAppliedPromo.value?.applicableProductIds ??
    [];
  const applicableCategoryIds =
    info.applicableCategoryIds ??
    lastAppliedPromo.value?.applicableCategoryIds ??
    [];
  const getFromProductIds =
    info.getFromProductIds ?? lastAppliedPromo.value?.getFromProductIds ?? [];
  const getFromCategoryIds =
    info.getFromCategoryIds ?? lastAppliedPromo.value?.getFromCategoryIds ?? [];

  // Exclude free gift items to avoid oscillation when gift is already in cart
  const regularItems = cart.value.filter((i) => !i.isFreeGift);

  let scopedItems;
  if (scope === "ORDER") {
    scopedItems = [...regularItems];
  } else if (scope === "PRODUCT") {
    const ids = new Set(applicableProductIds);
    scopedItems = regularItems.filter(
      (i) => ids.size === 0 || ids.has(i.productId),
    );
  } else if (scope === "CATEGORY") {
    const catIds = new Set(applicableCategoryIds);
    scopedItems = regularItems.filter(
      (i) =>
        catIds.size === 0 || catIds.has(productCategoryMap.value[i.productId]),
    );
  } else {
    scopedItems = [...regularItems];
  }

  const totalScopedQty = scopedItems.reduce((s, i) => s + i.quantity, 0);
  if (totalScopedQty < buyQty) return [];

  const isSeparate =
    getFromProductIds.length > 0 || getFromCategoryIds.length > 0;

  // Always show picker once the user has enough qualifying items (totalScopedQty >= buyQty).
  // Staff explicitly chooses which item to give free, for both SAME and SEPARATE mode.

  let freePool;
  if (getFromProductIds.length > 0) {
    // SEPARATE mode: show ALL active menu products in the getFromProductIds list
    const ids = new Set(getFromProductIds);
    freePool = menuCategories.value
      .flatMap((cat) => cat.products ?? [])
      .filter((p) => ids.has(p.id) && p.isActive)
      .map((p) => ({
        productId: p.id,
        productName: p.name,
        unitPrice: p.price,
        _key: `menu_gift_${p.id}`,
      }));
  } else if (getFromCategoryIds.length > 0) {
    // SEPARATE mode via category: show all active products in those categories
    const catIds = new Set(getFromCategoryIds);
    freePool = menuCategories.value
      .filter((cat) => catIds.has(cat.id) && cat.isActive)
      .flatMap((cat) => (cat.products ?? []).filter((p) => p.isActive))
      .map((p) => ({
        productId: p.id,
        productName: p.name,
        unitPrice: p.price,
        _key: `menu_gift_${p.id}`,
      }));
  } else {
    // SAME mode: use qualifying cart items (gift from what was already purchased)
    freePool = [...scopedItems];
  }

  return freePool;
});

watch(freeItemPickerPool, (pool) => {
  if (pool.length === 0) freeItemSelection.value = null;
});

watch(freeItemSelection, (newItem, oldItem) => {
  // Remove previous free gift item from cart
  if (oldItem) {
    const idx = cart.value.findIndex(
      (i) => i._key === oldItem._key + "_free_gift",
    );
    if (idx !== -1) cart.value.splice(idx, 1);
  }
  // Add new free gift item to cart with 0 price
  if (newItem) {
    const freeKey = newItem._key + "_free_gift";
    if (!cart.value.find((i) => i._key === freeKey)) {
      cart.value.push({
        ...newItem,
        _key: freeKey,
        unitPrice: 0,
        quantity: 1,
        isFreeGift: true,
      });
    }
  }
});

const formatPromotionValue = (promo) => {
  if (promo.discountType === "PERCENTAGE") return `${promo.discountValue}% off`;
  if (promo.discountType === "FIXED")
    return `-${new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND", maximumFractionDigits: 0 }).format(promo.discountValue)}`;
  if (promo.discountType === "BUY_X_GET_Y")
    return `Buy ${promo.buyQuantity} get ${promo.getQuantity}`;
  return "";
};

// Build productId → categoryId lookup from loaded menu
const productCategoryMap = computed(() => {
  const map = {};
  for (const cat of menuCategories.value) {
    for (const p of cat.products ?? []) {
      map[p.id] = cat.id;
    }
  }
  return map;
});

const promoDisableReason = (promo) => {
  const now = new Date();
  if (!promo.isActive) return "Inactive";
  if (new Date(promo.startDate) > now) return "Not started yet";
  if (promo.endDate && new Date(promo.endDate) < now) return "Expired";
  if (promo.maxUsage && promo.currentUsage >= promo.maxUsage)
    return "No uses left";
  if (promo.minOrderAmount && cartTotal.value < promo.minOrderAmount)
    return "Order total too low";

  const cartProductIds = new Set(cart.value.map((i) => i.productId));

  if (promo.scope === "PRODUCT") {
    const applicable = promo.applicableProductIds ?? [];
    if (
      applicable.length > 0 &&
      !applicable.some((id) => cartProductIds.has(id))
    )
      return "No matching products in cart";
  } else if (promo.scope === "CATEGORY") {
    const applicable = promo.applicableCategoryIds ?? [];
    if (applicable.length > 0) {
      const cartCatIds = new Set(
        cart.value
          .map((i) => productCategoryMap.value[i.productId])
          .filter(Boolean),
      );
      if (!applicable.some((id) => cartCatIds.has(id)))
        return "No matching category in cart";
    }
  }
  return null;
};

const isPromoAvailable = (promo) => promoDisableReason(promo) === null;

const openFindPromosDialog = async () => {
  findPromosVisible.value = true;
  if (publicPromos.value.length > 0) return;
  publicPromosLoading.value = true;
  try {
    const res = await getPromotions({ pageSize: 200 });
    publicPromos.value = (res.data?.items ?? []).filter(
      (p) => p.codeVisibility === "PUBLIC",
    );
  } catch {
    publicPromos.value = [];
  } finally {
    publicPromosLoading.value = false;
  }
};

// Estimate discount client-side for PRODUCT/CATEGORY scope
// (backend validate only calculates ORDER scope without item details)
const estimateClientDiscount = (promo) => {
  const type = promo.discountType;
  const scope = promo.scope;

  let eligibleSubtotal = 0;

  if (scope === "ORDER") {
    eligibleSubtotal = cartTotal.value;
  } else if (scope === "PRODUCT") {
    const ids = new Set(promo.applicableProductIds ?? []);
    eligibleSubtotal = cart.value
      .filter((i) => ids.size === 0 || ids.has(i.productId))
      .reduce((s, i) => s + i.unitPrice * i.quantity, 0);
  } else if (scope === "CATEGORY") {
    const catIds = new Set(promo.applicableCategoryIds ?? []);
    eligibleSubtotal = cart.value
      .filter(
        (i) =>
          catIds.size === 0 ||
          catIds.has(productCategoryMap.value[i.productId]),
      )
      .reduce((s, i) => s + i.unitPrice * i.quantity, 0);
  }

  if (eligibleSubtotal <= 0) return null;

  if (type === "PERCENTAGE") {
    const disc = Math.round((eligibleSubtotal * promo.discountValue) / 100);
    return promo.maxDiscountAmount
      ? Math.min(disc, promo.maxDiscountAmount)
      : disc;
  }
  if (type === "FIXED") {
    return Math.min(promo.discountValue, eligibleSubtotal);
  }

  if (type === "BUY_X_GET_Y") {
    const buyQty = promo.buyQuantity;
    const getQty = promo.getQuantity;
    if (!buyQty || !getQty) return null;

    const groupSize = buyQty + getQty;

    // Scoped items (buy side)
    let scopedItems;
    if (scope === "ORDER") {
      scopedItems = [...cart.value];
    } else if (scope === "PRODUCT") {
      const ids = new Set(promo.applicableProductIds ?? []);
      scopedItems = cart.value.filter(
        (i) => ids.size === 0 || ids.has(i.productId),
      );
    } else if (scope === "CATEGORY") {
      const catIds = new Set(promo.applicableCategoryIds ?? []);
      scopedItems = cart.value.filter(
        (i) =>
          catIds.size === 0 ||
          catIds.has(productCategoryMap.value[i.productId]),
      );
    } else {
      scopedItems = [...cart.value];
    }

    const totalScopedQty = scopedItems.reduce((s, i) => s + i.quantity, 0);
    if (totalScopedQty < buyQty) return null;

    const groups = Math.floor(totalScopedQty / groupSize);
    if (groups === 0) return null;

    let freeUnitsRemaining = groups * getQty;

    // Free pool
    let freePool;
    if (promo.getFromProductIds?.length > 0) {
      const ids = new Set(promo.getFromProductIds);
      freePool = cart.value.filter((i) => ids.has(i.productId));
    } else if (promo.getFromCategoryIds?.length > 0) {
      const catIds = new Set(promo.getFromCategoryIds);
      freePool = cart.value.filter((i) =>
        catIds.has(productCategoryMap.value[i.productId]),
      );
    } else {
      freePool = [...scopedItems];
    }

    if (freePool.length === 0) return null;

    // Cheapest items free
    const sortedPool = [...freePool].sort((a, b) => a.unitPrice - b.unitPrice);
    let discount = 0;
    for (const item of sortedPool) {
      if (freeUnitsRemaining <= 0) break;
      const freeFromItem = Math.min(item.quantity, freeUnitsRemaining);
      discount += freeFromItem * item.unitPrice;
      freeUnitsRemaining -= freeFromItem;
    }
    return discount > 0 ? discount : null;
  }

  return null;
};

// Returns true if a cart item is within the discount scope of the applied promo
const isItemDiscounted = (item) => {
  if (item.isFreeGift) return false;
  if (!promoInfo.value) return false;
  const scope = promoInfo.value.scope ?? lastAppliedPromo.value?.scope;
  if (!scope) return false;
  if (scope === "ORDER") return true;
  if (scope === "PRODUCT") {
    const ids =
      promoInfo.value.applicableProductIds ??
      lastAppliedPromo.value?.applicableProductIds ??
      [];
    return ids.length === 0 || ids.includes(item.productId);
  }
  if (scope === "CATEGORY") {
    const catIds = new Set(
      promoInfo.value.applicableCategoryIds ??
        lastAppliedPromo.value?.applicableCategoryIds ??
        [],
    );
    return (
      catIds.size === 0 || catIds.has(productCategoryMap.value[item.productId])
    );
  }
  return false;
};

const selectPromo = async (promo) => {
  findPromosVisible.value = false;
  lastAppliedPromo.value = promo;
  promoCode.value = promo.code;
  await applyPromoCode();
  // Patch estimatedDiscount for non-ORDER scope (backend returns null without item info)
  if (promoInfo.value && promoInfo.value.estimatedDiscount == null) {
    const est = estimateClientDiscount(promo);
    if (est != null)
      promoInfo.value = { ...promoInfo.value, estimatedDiscount: est };
  }
};

// ── Helpers ──────────────────────────────────────────────────────
const formatVnd = (val) =>
  new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
    maximumFractionDigits: 0,
  }).format(val ?? 0);

// ── i18n option arrays (computed so labels re-evaluate on locale change) ──
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

const makeCartKey = (productId, opts) => {
  const {
    temperature = "",
    iceLevel = "",
    sugarLevel = "",
    isTakeaway = false,
  } = opts ?? {};
  return `${productId}|${temperature}|${iceLevel}|${sugarLevel}|${isTakeaway}`;
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

const canPlaceOrder = computed(
  () => !!sessionId.value && cart.value.length > 0 && !placing.value,
);

const orderLabel = computed(() => {
  const t = tables.value.find((t) => t.id === selectedTableId.value);
  return t ? `Table ${t.code}` : "";
});

// ── Promotion computeds + logic ───────────────────────────────────
// BUY_X_GET_Y: free item đã được thêm vào cart ở unitPrice=0 → không trừ thêm lần nữa
const cartDiscount = computed(() =>
  freeItemSelection.value ? 0 : (promoInfo.value?.estimatedDiscount ?? 0),
);
const cartFinal = computed(() => cartTotal.value - cartDiscount.value);

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
  // Remove free gift item from cart before clearing state
  if (freeItemSelection.value) {
    const idx = cart.value.findIndex(
      (i) => i._key === freeItemSelection.value._key + "_free_gift",
    );
    if (idx !== -1) cart.value.splice(idx, 1);
  }
  // Also remove any orphaned free gift items
  cart.value = cart.value.filter((i) => !i.isFreeGift);
  promoCode.value = "";
  promoInfo.value = null;
  promoError.value = "";
  lastAppliedPromo.value = null;
  freeItemSelection.value = null;
};

watch(cartTotal, async (newVal) => {
  if (!promoInfo.value) return;
  // Don't re-validate when only a free gift item was added/removed (price = 0)
  try {
    const res = await validatePromotion(promoCode.value.trim(), newVal);
    if (res.data.isApplicable) {
      promoInfo.value = res.data;
      // Remove free gift and reset picker so user re-selects with new cart state
      if (freeItemSelection.value) {
        const idx = cart.value.findIndex(
          (i) => i._key === freeItemSelection.value._key + "_free_gift",
        );
        if (idx !== -1) cart.value.splice(idx, 1);
        freeItemSelection.value = null;
      }
      if (promoInfo.value.estimatedDiscount == null && lastAppliedPromo.value) {
        const est = estimateClientDiscount(lastAppliedPromo.value);
        if (est != null)
          promoInfo.value = { ...promoInfo.value, estimatedDiscount: est };
      }
    } else clearPromo();
  } catch {
    clearPromo();
  }
});

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

const clearCart = () => {
  cart.value = [];
};

// ── Options dialog ────────────────────────────────────────────────
const handleAddToCart = (product) => {
  selectedProduct.value = product;
  pendingOptions.value = {
    temperature: product.hasTemperatureOption ? DRINK_TEMPERATURE.COLD : null,
    iceLevel: product.hasIceLevelOption ? ICE_LEVEL.NORMAL : null,
    sugarLevel: product.hasSugarLevelOption ? SUGAR_LEVEL.NORMAL : null,
    isTakeaway: false,
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
      isAccompaniment: selectedProduct.value.isAccompaniment ?? false,
    });
  }
  showOptionsDialog.value = false;
  selectedProduct.value = null;
};

// ── Session ───────────────────────────────────────────────────────
const onTableSelect = async (tableId) => {
  if (!tableId) {
    sessionId.value = null;
    sessionHadExisting.value = false;
    return;
  }
  sessionLoading.value = true;
  sessionId.value = null;
  try {
    const res = await getOrCreateSession(tableId);
    const existed = !!tables.value.find((t) => t.id === tableId)
      ?.activeSessionId;
    sessionId.value = res.data.sessionId;
    sessionHadExisting.value = existed;
  } catch {
    errorMessage.value = "Could not get session for table.";
  } finally {
    sessionLoading.value = false;
  }
};

// ── Place order ────────────────────────────────────────────────────
const notificationStore = useNotificationStore();
const placeOrder = async () => {
  if (!canPlaceOrder.value) return;
  errorMessage.value = "";
  placing.value = true;
  notificationStore.creatingOrder = true;
  try {
    const res = await createOrder(
      sessionId.value,
      cart.value.map((item) => ({
        productId: item.productId,
        productName: item.productName,
        unitPrice: item.unitPrice,
        quantity: item.quantity,
        temperature: item.temperature ?? null,
        iceLevel: item.iceLevel ?? null,
        sugarLevel: item.sugarLevel ?? null,
        isTakeaway: item.isTakeaway ?? false,
        isFreeGift: item.isFreeGift ?? false,
      })),
      guestCount.value != null ? Number(guestCount.value) : (defaultGuestCount.value || null),
    );
    const { orderId } = res.data;

    // Apply promo code if entered
    if (promoCode.value.trim()) {
      try {
        await applyPromotionAdmin(orderId, promoCode.value.trim());
      } catch (promoErr) {
        const msg =
          promoErr?.response?.data?.errors
            ?.map((e) => e.errorMessage ?? e)
            .join("; ") ||
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
      summary: t("orders.create.submit"),
      detail: `#${res.data.orderNumber}`,
      life: 3000,
    });
    router.push({ name: "ordersDetail", params: { id: orderId } });
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.errors?.map((e) => e.errorMessage ?? e).join("; ") ||
      err?.response?.data?.message ||
      "Failed to place order.";
  } finally {
    placing.value = false;
    notificationStore.creatingOrder = false;
  }
};

// ── Init ──────────────────────────────────────────────────────────
onMounted(async () => {
  loadingTables.value = true;
  loadingMenu.value = true;
  try {
    const [tablesRes, menuRes] = await Promise.all([
      listTables(),
      getAdminMenu(),
    ]);
    tables.value = (tablesRes.data ?? []).filter((t) => t.isActive);
    menuCategories.value = menuRes.data ?? [];
  } finally {
    loadingTables.value = false;
    loadingMenu.value = false;
  }
});
</script>

<template>
  <section class="tw:space-y-4">
    <!-- Header -->
    <div class="tw:flex tw:items-center tw:gap-4">
      <prime-button
        :class="btnIcon"
        severity="secondary"
        text
        @click="router.push({ name: 'orders' })"
      >
        <iconify icon="ph:arrow-left-bold" />
      </prime-button>
      <div>
        <p
          class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300"
        >
          {{ t("orders.breadcrumb") }}
        </p>
        <h1 class="tw:text-2xl tw:font-semibold">
          {{ t("orders.create.title") }}
        </h1>
      </div>
    </div>

    <!-- Search / Table / Guest Count / Session bar -->
    <div
    :class="[appCard, cardRing, 'tw:px-4 tw:py-3', 'tw:rounded-md','tw:flex tw:justify-between tw:items-center']"
    >
      <div class="tw:flex tw:items-center tw:gap-3">
        <div class="tw:flex tw:items-center tw:gap-1.5">
          <iconify icon="prime:search" class="tw:text-lg tw:opacity-60" />
          <prime-input-text
            v-model="searchQuery"
            :placeholder="t('orders.create.search')"
            size="small"
          />
        </div>
        <div class="tw:flex tw:items-center tw:gap-1.5">
          <iconify icon="ic:round-table-bar" class="tw:text-lg tw:opacity-60" />
          <prime-select
            v-model="selectedTableId"
            :options="tables"
            optionLabel="code"
            optionValue="id"
            :placeholder="t('orders.create.selectTable')"
            :disabled="sessionLoading"
            class="tw:w-40"
            @change="(e) => onTableSelect(e.value)"
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
      <template v-if="sessionLoading">
        <prime-tag severity="secondary">
          <iconify icon="prime:spinner" />
          <span>{{ t("orders.create.connecting") }}</span>
        </prime-tag>
      </template>
      <template v-else-if="sessionId">
        <prime-tag v-if="sessionHadExisting" severity="info">
          <iconify icon="prime:info-circle" />
          <span>{{ t("orders.create.existingSession") }}</span>
        </prime-tag>
        <prime-tag v-else severity="success">
          <iconify icon="prime:check" />
          <span>{{ t("orders.create.sessionReady") }}</span>
        </prime-tag>
      </template>
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
                <div
                  class="tw:h-32 tw:w-full"
                  style="background: var(--app-bg-subtle)"
                />
                <div class="tw:p-3 tw:space-y-2">
                  <div
                    class="tw:h-4 tw:w-3/4 tw:rounded"
                    style="background: var(--app-bg-subtle)"
                  />
                  <div
                    class="tw:h-3 tw:w-1/2 tw:rounded"
                    style="background: var(--app-bg-subtle)"
                  />
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
          <iconify
            icon="ph:magnifying-glass-bold"
            class="tw:text-3xl tw:mb-3 tw:block tw:mx-auto tw:opacity-40"
          />
          {{ t("orders.create.noProducts") }}
        </prime-panel>

        <prime-panel
          v-else
          v-for="category in visibleCategories"
          :key="category.id"
          :header="category.name"
          toggleable
          :pt="{
            root: { class: `${appCard} ${cardRing}` },
          }"
        >
          <!-- Products grid -->
          <div
            v-show="!collapsedCategories[category.id]"
            class="tw:grid tw:grid-cols-2 tw:md:grid-cols-3 tw:xl:grid-cols-4 tw:2xl:grid-cols-5 tw:gap-3 sm:tw:grid-cols-3 tw:p-3 tw:pt-0"
          >
            <article
              v-for="product in category.filteredProducts"
              :key="product.id"
              class="tw:group tw:flex tw:h-full tw:flex-col tw:overflow-hidden tw:rounded-xl tw:border tw:cursor-pointer tw:transition-all hover:tw:-translate-y-0.5 hover:tw:border-emerald-500/50"
              style="
                border-color: var(--app-border);
                background: var(--app-bg-subtle);
              "
              @click="handleAddToCart(product)"
            >
              <!-- Product image / placeholder -->
              <div class="tw:relative tw:overflow-hidden tw:shrink-0">
                <img
                  v-if="product.imageUrl"
                  :src="product.imageUrl"
                  :alt="product.name"
                  class="tw:h-32 tw:w-full tw:object-cover"
                />
                <div
                  v-else
                  class="tw:h-32 tw:w-full tw:flex tw:items-center tw:justify-center"
                  style="background: var(--app-bg)"
                >
                  <iconify
                    icon="ph:coffee-bold"
                    class="tw:text-3xl tw:text-emerald-400/20"
                  />
                </div>
                <!-- Cart qty badge -->
                <div
                  v-if="cartQuantity(product.id) > 0"
                  class="tw:absolute tw:top-2 tw:right-2 tw:h-5 tw:min-w-5 tw:px-1 tw:rounded-full tw:bg-emerald-500 tw:flex tw:items-center tw:justify-center tw:text-xs tw:font-bold tw:text-white tw:shadow-lg"
                >
                  {{ cartQuantity(product.id) }}
                </div>
              </div>

              <!-- Product info -->
              <div class="tw:flex tw:flex-1 tw:flex-col tw:p-2.5">
                <h3
                  class="tw:text-xs tw:font-semibold tw:line-clamp-2 tw:leading-snug"
                >
                  {{ product.name }}
                </h3>
                <p
                  class="tw:mt-1 tw:text-xs tw:font-semibold tw:text-emerald-400"
                >
                  {{ formatVnd(product.price) }}
                </p>

                <!-- Option badges -->
                <div
                  v-if="
                    product.hasTemperatureOption ||
                    product.hasIceLevelOption ||
                    product.hasSugarLevelOption
                  "
                  class="tw:mt-1.5 tw:flex tw:flex-wrap tw:gap-1"
                >
                  <span
                    v-if="product.hasTemperatureOption"
                    class="tw:rounded tw:px-1 tw:py-0.5 tw:text-xs tw:bg-orange-500/10 tw:text-orange-400"
                    >{{ t("orders.create.optionsDialog.temperature") }}</span
                  >
                  <span
                    v-if="product.hasIceLevelOption"
                    class="tw:rounded tw:px-1 tw:py-0.5 tw:text-xs tw:bg-sky-500/10 tw:text-sky-400"
                    >{{ t("orders.create.optionsDialog.iceLevel") }}</span
                  >
                  <span
                    v-if="product.hasSugarLevelOption"
                    class="tw:rounded tw:px-1 tw:py-0.5 tw:text-xs tw:bg-amber-500/10 tw:text-amber-400"
                    >{{ t("orders.create.optionsDialog.sugarLevel") }}</span
                  >
                </div>

                <div class="tw:flex-1" />

                <!-- Add button -->
                <div class="tw:flex tw:justify-end tw:mt-2">
                  <prime-button
                    size="small"
                    rounded
                    text
                    severity="success"
                    @click.stop="handleAddToCart(product)"
                    :class="btnIcon"
                  >
                    <iconify icon="prime:plus" />
                  </prime-button>
                </div>
              </div>
            </article>
          </div>
        </prime-panel>
        <!-- Categories (collapsible) -->
      </section>

      <!-- ── RIGHT: Cart ────────────────────────────────────── -->
      <aside class="tw:lg:col-span-4 tw:lg:self-start tw:lg:sticky tw:lg:top-6">
        <div 
        :class="[appCard, cardRing, 'tw:p-5', 'tw:rounded-md']"
       >
          <!-- Cart header -->
          <div class="tw:flex tw:items-center tw:justify-between tw:mb-4">
            <h2
              class="tw:text-base tw:font-semibold tw:flex tw:items-center tw:gap-2"
            >
              {{ t("orders.create.cart") }}
              <prime-badge
                v-if="cartItemCount > 0"
                :value="cartItemCount"
                severity="success"
              />
            </h2>
            <prime-button
              v-if="cart.length > 0"
              size="small"
              severity="danger"
              outlined
              @click="clearCart"
              :class="btnIcon"
            >
              <iconify icon="prime:trash" />
            </prime-button>
          </div>

          <!-- Order label -->
          <div v-if="orderLabel" class="tw:mb-3">
            <prime-tag :value="orderLabel" severity="secondary">
              <iconify icon="prime:table" />
            </prime-tag>
          </div>

          <!-- Empty state -->
          <div
            v-if="cart.length === 0"
            class="tw:py-8 tw:text-center app-text-muted tw:text-sm tw:rounded-xl tw:border tw:border-dashed"
            style="border-color: var(--app-border)"
          >
            {{ t("orders.create.cartEmpty") }}<br />
            <span class="tw:text-xs tw:opacity-60">
              {{ t("orders.create.cartEmptyHint") }}
            </span>
          </div>

          <!-- Cart items -->
          <div v-else class="tw:space-y-2">
            <div
              v-for="item in cart"
              :key="item._key"
              class="tw:rounded-xl tw:border tw:p-3 tw:transition-colors"
              :class="
                item.isFreeGift
                  ? 'tw:border-amber-500/40 tw:bg-amber-500/5'
                  : isItemDiscounted(item)
                    ? 'tw:border-emerald-500/60 tw:bg-emerald-500/5'
                    : ''
              "
              :style="
                item.isFreeGift || isItemDiscounted(item)
                  ? ''
                  : 'border-color: var(--app-border)'
              "
            >
              <div class="tw:flex tw:items-start tw:gap-2">
                <div class="tw:flex-1 tw:min-w-0">
                  <div class="tw:flex tw:items-center tw:gap-1.5 tw:flex-wrap">
                    <p class="tw:text-sm tw:font-medium tw:leading-snug">
                      {{ item.productName }}
                    </p>
                    <!-- Free gift badge -->
                    <span
                      v-if="item.isFreeGift"
                      class="tw:inline-flex tw:items-center tw:gap-0.5 tw:text-xs tw:px-1.5 tw:py-0.5 tw:rounded tw:bg-amber-500/15 tw:text-amber-400 tw:font-medium"
                    >
                      <iconify icon="ph:gift-fill" class="tw:text-[10px]" />
                      {{ t("orders.create.freeBadge") }}
                    </span>
                    <!-- Promotion badge for regular scoped items -->
                    <span
                      v-else-if="isItemDiscounted(item)"
                      class="tw:inline-flex tw:items-center tw:gap-0.5 tw:text-xs tw:px-1.5 tw:py-0.5 tw:rounded tw:bg-emerald-500/15 tw:text-emerald-400 tw:font-medium"
                    >
                      <iconify
                        icon="ph:tag-simple-fill"
                        class="tw:text-[10px]"
                      />
                      {{ t("orders.create.promoBadge") }}
                    </span>
                  </div>
                  <p
                    class="tw:text-xs tw:mt-0.5"
                    :class="
                      item.isFreeGift
                        ? 'tw:text-amber-400 tw:line-through tw:opacity-60'
                        : 'tw:text-emerald-400'
                    "
                  >
                    {{
                      item.isFreeGift
                        ? formatVnd(freeItemSelection?.unitPrice ?? 0)
                        : formatVnd(item.unitPrice)
                    }}
                  </p>
                  <p
                    v-if="item.isFreeGift"
                    class="tw:text-xs tw:text-amber-400 tw:font-semibold"
                  >
                    {{ t("orders.create.freeBadge") }}
                  </p>
                  <p
                    v-if="optionsLabel(item)"
                    class="tw:text-xs tw:text-amber-400 tw:mt-0.5 tw:leading-snug"
                  >
                    {{ optionsLabel(item) }}
                  </p>
                  <!-- Takeaway badge -->
                  <span
                    v-if="item.isTakeaway"
                    class="tw:inline-block tw:mt-1 tw:text-xs tw:px-1.5 tw:py-0.5 tw:rounded tw:bg-sky-500/10 tw:text-sky-400"
                    >{{ t("orders.create.takeaway") }}</span
                  >
                </div>
                <!-- Free gift: no qty controls, just remove -->
                <div v-if="item.isFreeGift" class="tw:shrink-0">
                  <prime-button
                    size="small"
                    text
                    rounded
                    severity="secondary"
                    :class="btnIcon"
                    @click="freeItemSelection = null"
                  >
                    <iconify icon="prime:times" class="tw:text-xs" />
                  </prime-button>
                </div>
                <!-- Regular item: qty controls -->
                <div
                  v-else
                  class="tw:flex tw:items-center tw:gap-1 tw:shrink-0"
                >
                  <prime-button
                    size="small"
                    text
                    rounded
                    severity="secondary"
                    @click="changeQty(item._key, -1)"
                    :class="btnIcon"
                  >
                    <iconify icon="prime:minus" />
                  </prime-button>
                  <span class="tw:text-sm tw:font-bold tw:w-5 tw:text-center">{{
                    item.quantity
                  }}</span>
                  <prime-button
                    size="small"
                    text
                    rounded
                    severity="secondary"
                    @click="changeQty(item._key, 1)"
                    :class="btnIcon"
                  >
                    <iconify icon="prime:plus" />
                  </prime-button>
                </div>
              </div>
              <div class="tw:flex tw:justify-end tw:mt-1.5">
                <span
                  class="tw:text-sm tw:font-semibold"
                  :class="
                    item.isFreeGift
                      ? 'tw:text-amber-400'
                      : 'tw:text-emerald-400'
                  "
                >
                  {{
                    item.isFreeGift
                      ? "0 ₫"
                      : formatVnd(item.unitPrice * item.quantity)
                  }}
                </span>
              </div>
            </div>

            <!-- Total summary -->
            <div
              class="tw:rounded-xl tw:p-3 tw:mt-1"
              style="background: var(--app-bg-subtle)"
            >
              <div
                class="tw:flex tw:items-center tw:justify-between tw:text-sm tw:mb-1"
              >
                <span class="app-text-muted">{{
                  t("orders.create.subtotal")
                }}</span>
                <span class="tw:font-medium">{{ formatVnd(cartTotal) }}</span>
              </div>
              <div
                class="tw:flex tw:items-center tw:justify-between tw:text-xs tw:mb-2"
              >
                <span class="app-text-muted">{{
                  t("orders.create.serviceCharge")
                }}</span>
                <span class="app-text-muted">{{
                  t("orders.create.free")
                }}</span>
              </div>
              <div
                class="tw:border-t tw:pt-2"
                style="border-color: var(--app-border)"
              >
                <!-- Promo row -->
                <div
                  v-if="promoInfo"
                  class="tw:flex tw:items-center tw:justify-between tw:text-xs tw:mb-1.5"
                >
                  <span class="tw:flex tw:items-center tw:gap-1 app-text-muted">
                    <iconify icon="ph:tag-bold" class="tw:text-emerald-400" />
                    {{ promoInfo.code }}
                  </span>
                  <!-- Free gift selected: show gift label -->
                  <span
                    v-if="freeItemSelection"
                    class="tw:flex tw:items-center tw:gap-1 tw:text-amber-400 tw:font-medium"
                  >
                    <iconify icon="ph:gift-bold" class="tw:text-xs" />
                    {{ t("orders.create.freeItem") }}
                  </span>
                  <!-- Numeric discount (PERCENTAGE / FIXED) -->
                  <span
                    v-else-if="promoInfo.estimatedDiscount"
                    class="tw:text-emerald-400 tw:font-medium"
                  >
                    -{{ formatVnd(promoInfo.estimatedDiscount) }}
                  </span>
                  <span v-else class="app-text-muted tw:italic">{{
                    t("orders.create.willBeApplied")
                  }}</span>
                </div>
                <div
                  class="tw:flex tw:items-center tw:justify-between tw:font-bold"
                >
                  <span>{{ t("orders.create.total") }}</span>
                  <div class="tw:text-right">
                    <span
                      v-if="promoInfo?.estimatedDiscount && !freeItemSelection"
                      class="app-text-muted tw:line-through tw:text-xs tw:font-normal tw:mr-1"
                      >{{ formatVnd(cartTotal) }}</span
                    >
                    <span class="tw:text-emerald-400 tw:text-base">{{
                      formatVnd(cartFinal)
                    }}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Find promotions -->
          <div
            class="tw:flex tw:items-center tw:justify-between tw:mt-4 tw:mb-1"
          >
            <span
              class="tw:flex tw:items-center tw:gap-1.5 tw:text-xs app-text-muted"
            >
              <iconify icon="ph:ticket-bold" class="tw:text-emerald-400" />
              {{ t("orders.create.findPromotions") }}
            </span>
            <prime-button
              size="small"
              text
              severity="secondary"
              class="tw:text-xs tw:h-6! tw:px-2!"
              @click="openFindPromosDialog"
            >
              <iconify icon="ph:magnifying-glass-bold" class="tw:text-xs" />
              <span>{{ t("orders.create.browse") }}</span>
            </prime-button>
          </div>

          <!-- Promo code -->
          <div v-if="cart.length > 0" class="tw:mt-3">
            <!-- Applied: show tag with remove -->
            <div
              v-if="promoInfo"
              class="tw:flex tw:items-center tw:justify-between tw:rounded-xl tw:border tw:border-emerald-500/30 tw:bg-emerald-500/10 tw:px-3 tw:py-2"
            >
              <div class="tw:flex tw:items-center tw:gap-2 tw:min-w-0">
                <iconify
                  icon="ph:tag-bold"
                  class="tw:text-emerald-400 tw:shrink-0"
                />
                <span class="tw:font-medium tw:text-sm tw:text-emerald-300">{{
                  promoInfo.code
                }}</span>
                <span class="tw:text-xs app-text-muted tw:truncate">{{
                  promoInfo.name
                }}</span>
              </div>
              <prime-button
                size="small"
                text
                severity="secondary"
                :class="btnIcon"
                @click="clearPromo"
              >
                <iconify icon="prime:times" class="tw:text-xs" />
              </prime-button>
            </div>

            <!-- Free item picker: BUY_X_GET_Y with partial trigger (groups=0) -->
            <div
              v-if="freeItemPickerPool.length > 0"
              class="tw:mt-2 tw:rounded-xl tw:border tw:p-3"
              style="border-color: var(--app-border)"
            >
              <p
                class="tw:text-xs tw:font-medium tw:mb-2 tw:flex tw:items-center tw:gap-1 tw:text-emerald-400"
              >
                <iconify icon="ph:gift-bold" />
                {{ t("orders.create.selectFreeGift") }}
              </p>
              <div class="tw:flex tw:flex-col tw:gap-1.5">
                <button
                  v-for="item in freeItemPickerPool"
                  :key="item._key"
                  class="tw:flex tw:items-center tw:justify-between tw:rounded-lg tw:border tw:px-3 tw:py-2 tw:text-left tw:text-sm tw:transition-colors tw:w-full"
                  :class="
                    freeItemSelection?._key === item._key
                      ? 'tw:border-emerald-500 tw:bg-emerald-500/10 tw:text-emerald-300'
                      : 'hover:tw:border-emerald-500/40 app-text-muted'
                  "
                  :style="
                    freeItemSelection?._key === item._key
                      ? ''
                      : 'border-color: var(--app-border)'
                  "
                  @click="
                    freeItemSelection =
                      freeItemSelection?._key === item._key ? null : item
                  "
                >
                  <span class="tw:flex tw:items-center tw:gap-1.5">
                    <iconify
                      v-if="freeItemSelection?._key === item._key"
                      icon="ph:check-circle-fill"
                      class="tw:text-emerald-400 tw:shrink-0"
                    />
                    <iconify v-else icon="ph:circle" class="tw:shrink-0" />
                    {{ item.productName }}
                  </span>
                  <span
                    class="tw:text-xs tw:font-semibold tw:text-emerald-400 tw:shrink-0"
                  >
                    {{ formatVnd(item.unitPrice) }}
                  </span>
                </button>
              </div>
            </div>

            <!-- Not applied: input + button -->
            <div v-if="!promoInfo">
              <div class="tw:flex tw:gap-2">
                <prime-input-text
                  v-model="promoCode"
                  :placeholder="t('orders.create.promoCode')"
                  class="tw:flex-1"
                  @keyup.enter="applyPromoCode"
                />
                <prime-button
                  severity="secondary"
                  outlined
                  :loading="promoLoading"
                  :disabled="!promoCode.trim()"
                  @click="applyPromoCode"
                  >{{ t("orders.create.apply") }}</prime-button
                >
              </div>
              <p v-if="promoError" class="tw:text-xs tw:text-red-400 tw:mt-1.5">
                {{ promoError }}
              </p>
            </div>
          </div>

          <!-- Error -->
          <prime-message
            v-if="errorMessage"
            severity="error"
            :closable="false"
            class="tw:mt-3"
          >
            {{ errorMessage }}
          </prime-message>

          <!-- Place order -->
          <prime-button
            class="tw:w-full tw:mt-4"
            :loading="placing"
            :disabled="!canPlaceOrder"
            @click="placeOrder"
          >
            <iconify icon="prime:check" />
            <span>{{ t("orders.create.submit") }}</span>
          </prime-button>

          <p
            v-if="!sessionId && !sessionLoading"
            class="tw:text-xs app-text-muted tw:text-center tw:mt-2"
          >
            {{ t("orders.create.tableHint") }}
          </p>
        </div>
      </aside>
    </div>
  </section>

  <!-- ── Find Promotions Dialog ────────────────────────────────── -->
  <prime-dialog
    v-model:visible="findPromosVisible"
    :header="t('orders.create.promoDialog.title')"
    modal
    :style="{ width: '30rem' }"
  >
    <div class="tw:space-y-2">
      <div
        v-if="publicPromosLoading"
        class="tw:flex tw:items-center tw:justify-center tw:gap-2 tw:py-8 app-text-muted"
      >
        <iconify icon="prime:spinner" class="tw:animate-spin" />
        <span class="tw:text-sm">{{
          t("orders.create.promoDialog.loading")
        }}</span>
      </div>
      <div
        v-else-if="publicPromos.length === 0"
        class="tw:py-8 tw:text-center tw:text-sm app-text-muted"
      >
        {{ t("orders.create.promoDialog.empty") }}
      </div>
      <div
        v-else
        v-for="promo in publicPromos"
        :key="promo.id"
        class="tw:rounded-xl tw:border tw:p-3 tw:transition-colors"
        :class="
          isPromoAvailable(promo)
            ? 'tw:cursor-pointer hover:tw:border-emerald-500/50 hover:tw:bg-emerald-500/5'
            : 'tw:opacity-40 tw:cursor-not-allowed'
        "
        style="border-color: var(--app-border)"
        @click="isPromoAvailable(promo) && selectPromo(promo)"
      >
        <div class="tw:flex tw:items-start tw:justify-between tw:gap-3">
          <div class="tw:min-w-0">
            <p class="tw:text-sm tw:font-semibold tw:leading-snug">
              {{ promo.name }}
            </p>
            <p class="tw:text-xs tw:font-mono app-text-muted tw:mt-0.5">
              {{ promo.code }}
            </p>
            <div class="tw:flex tw:flex-wrap tw:gap-x-3 tw:gap-y-0.5 tw:mt-1.5">
              <span
                v-if="promo.minOrderAmount"
                class="tw:text-xs app-text-muted"
              >
                {{
                  t("orders.create.promoDialog.minAmount", {
                    amount: formatVnd(promo.minOrderAmount),
                  })
                }}
              </span>
              <span v-if="promo.endDate" class="tw:text-xs app-text-muted">
                {{
                  t("orders.create.promoDialog.until", {
                    date: new Date(promo.endDate).toLocaleDateString("vi-VN"),
                  })
                }}
              </span>
              <span v-if="promo.maxUsage" class="tw:text-xs app-text-muted">
                {{
                  t("orders.create.promoDialog.usesLeft", {
                    n: promo.maxUsage - promo.currentUsage,
                  })
                }}
              </span>
              <span
                v-if="!isPromoAvailable(promo)"
                class="tw:text-xs tw:text-red-400"
              >
                {{ promoDisableReason(promo) }}
              </span>
            </div>
          </div>
          <span
            class="tw:shrink-0 tw:text-emerald-400 tw:font-semibold tw:text-sm"
          >
            {{ formatPromotionValue(promo) }}
          </span>
        </div>
      </div>
    </div>
  </prime-dialog>

  <!-- ── Options Dialog ─────────────────────────────────────────── -->
  <prime-dialog
    v-model:visible="showOptionsDialog"
    modal
    :style="{ width: '52rem', maxWidth: '95vw' }"
    :pt="{ content: { class: 'tw:p-0! tw:overflow-hidden' } }"
    @hide="selectedProduct = null"
  >
    <div class="tw:flex tw:min-h-[28rem] tw:flex-col tw:sm:flex-row">
      <!-- LEFT: Product info -->
      <div
        class="tw:flex tw:flex-col tw:border-b tw:border-slate-200 tw:sm:w-5/12 tw:sm:border-b-0 tw:sm:border-r"
        style="border-color: var(--app-border); background: var(--app-bg-subtle)"
      >
        <!-- Image -->
        <div class="tw:relative tw:h-48 tw:flex-none tw:overflow-hidden tw:sm:h-56" style="background: var(--app-bg)">
          <img
            v-if="selectedProduct?.imageUrl"
            :src="selectedProduct.imageUrl"
            :alt="selectedProduct?.name"
            class="tw:h-full tw:w-full tw:object-cover"
          />
          <div
            v-else
            class="tw:flex tw:h-full tw:w-full tw:items-center tw:justify-center"
          >
            <iconify icon="ph:coffee-bold" class="tw:text-6xl tw:text-emerald-400/20" />
          </div>
        </div>

        <!-- Info -->
        <div class="tw:flex tw:flex-1 tw:flex-col tw:gap-2 tw:p-4">
          <h3 class="tw:text-lg tw:font-bold tw:leading-snug">
            {{ selectedProduct?.name }}
          </h3>
          <p class="tw:text-xl tw:font-semibold tw:text-emerald-500">
            {{ selectedProduct ? formatVnd(selectedProduct.price) : '' }}
          </p>
          <p
            v-if="selectedProduct?.description"
            class="tw:text-sm tw:leading-relaxed app-text-muted"
          >
            {{ selectedProduct.description }}
          </p>
          <div
            v-if="selectedProduct?.estimatedPrepMinutes"
            class="tw:mt-auto tw:flex tw:items-center tw:gap-1 tw:pt-2 tw:text-xs app-text-muted"
          >
            <iconify icon="ph:clock-bold" class="tw:h-4 tw:w-4" />
            <span>{{ t("orders.create.optionsDialog.prepTime", { min: selectedProduct.estimatedPrepMinutes }) }}</span>
          </div>
        </div>
      </div>

      <!-- RIGHT: Options + actions -->
      <div class="tw:flex tw:flex-1 tw:flex-col tw:p-5">
        <div class="tw:flex-1 tw:space-y-5">
          <!-- Quantity -->
          <div>
            <p class="tw:mb-2 tw:text-sm tw:font-semibold">
              {{ t("orders.create.optionsDialog.quantity") }}
            </p>
            <div class="tw:flex tw:items-center tw:gap-3">
              <button
                class="tw:flex tw:h-9 tw:w-9 tw:items-center tw:justify-center tw:rounded-xl tw:border tw:transition hover:tw:border-emerald-400 app-text-muted"
                style="border-color: var(--app-border)"
                @click="pendingQuantity = Math.max(1, pendingQuantity - 1)"
              >
                <iconify icon="ph:minus-bold" class="tw:h-4 tw:w-4" />
              </button>
              <span class="tw:min-w-10 tw:text-center tw:text-xl tw:font-bold">{{
                pendingQuantity
              }}</span>
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
            <p class="tw:mb-2 tw:text-sm tw:font-semibold">
              {{ t("orders.create.optionsDialog.temperature") }}
            </p>
            <div class="tw:flex tw:gap-2">
              <prime-button
                v-for="opt in temperatureOptions"
                variant="outlined"
                class="tw:w-full"
                :severity="
                  pendingOptions.temperature === opt.value ? 'primary' : 'secondary'
                "
                @click="setTemperature(opt.value)"
              >
                <iconify :icon="opt.icon" />
                <span>{{ opt.label }}</span>
              </prime-button>
            </div>
          </div>

          <!-- Ice level — only when Cold -->
          <div
            v-if="
              selectedProduct?.hasIceLevelOption &&
              pendingOptions.temperature !== DRINK_TEMPERATURE.HOT
            "
          >
            <p class="tw:mb-2 tw:text-sm tw:font-semibold">
              {{ t("orders.create.optionsDialog.iceLevel") }}
            </p>
            <div class="tw:grid tw:grid-cols-2 tw:gap-2">
              <prime-button
                v-for="opt in iceLevelOptions"
                :key="opt.value"
                variant="outlined"
                class="tw:w-full"
                :severity="
                  pendingOptions.iceLevel === opt.value ? 'primary' : 'secondary'
                "
                @click="pendingOptions.iceLevel = opt.value"
              >
                <iconify :icon="opt.icon" />
                <span>{{ opt.label }}</span>
              </prime-button>
            </div>
          </div>

          <!-- Sugar level — only when Cold -->
          <div
            v-if="
              selectedProduct?.hasSugarLevelOption &&
              pendingOptions.temperature !== DRINK_TEMPERATURE.HOT
            "
          >
            <p class="tw:mb-2 tw:text-sm tw:font-semibold">
              {{ t("orders.create.optionsDialog.sugarLevel") }}
            </p>
            <div class="tw:grid tw:grid-cols-2 tw:gap-2">
              <prime-button
                v-for="opt in sugarLevelOptions"
                :key="opt.value"
                variant="outlined"
                class="tw:w-full"
                :severity="
                  pendingOptions.sugarLevel === opt.value ? 'primary' : 'secondary'
                "
                @click="pendingOptions.sugarLevel = opt.value"
              >
                <iconify v-if="opt.icon" :icon="opt.icon" />
                <span>{{ opt.label }}</span>
              </prime-button>
            </div>
          </div>

          <!-- Serving -->
          <div>
            <p class="tw:mb-2 tw:text-sm tw:font-semibold">
              {{ t("orders.create.optionsDialog.serving") }}
            </p>
            <div class="tw:flex tw:gap-2">
              <prime-button
                v-for="servingType in servingOptions"
                variant="outlined"
                class="tw:w-full"
                :severity="
                  pendingOptions.isTakeaway === servingType.value
                    ? 'primary'
                    : 'secondary'
                "
                @click="pendingOptions.isTakeaway = servingType.value"
              >
                <iconify :icon="servingType.icon" />
                <span>{{ servingType.label }}</span>
              </prime-button>
            </div>
          </div>
        </div>

        <!-- Actions -->
        <div
          class="tw:mt-5 tw:flex tw:justify-end tw:gap-2 tw:border-t tw:pt-4"
          style="border-color: var(--app-border)"
        >
          <prime-button
            severity="secondary"
            text
            @click="showOptionsDialog = false"
          >
            <span>{{ t("orders.cancel") }}</span>
          </prime-button>
          <prime-button @click="confirmAddToCart">
            <iconify icon="prime:shopping-cart" />
            <span class="tw:ml-2">{{
              t("orders.create.optionsDialog.addToCart")
            }}</span>
          </prime-button>
        </div>
      </div>
    </div>
  </prime-dialog>
</template>
