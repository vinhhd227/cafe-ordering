<script setup>
import { getAdminMenu } from "@/services/menu.service";
import { createOrder } from "@/services/order.service";
import { listTables, getOrCreateSession } from "@/services/table.service";

const router = useRouter();
const toast = useToast();

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

// ── Submit ───────────────────────────────────────────────────────
const placing = ref(false);
const errorMessage = ref("");

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
  } = opts ?? {};
  return `${productId}|${temperature}|${iceLevel}|${sugarLevel}|${isTakeaway}`;
};

const optionsLabel = (item) => {
  const parts = [];
  if (item.temperature)
    parts.push(DRINK_TEMPERATURE_MAP[item.temperature]?.label ?? item.temperature);
  if (item.iceLevel && item.iceLevel !== ICE_LEVEL.NORMAL)
    parts.push(ICE_LEVEL_MAP[item.iceLevel]?.label ?? item.iceLevel);
  if (item.sugarLevel && item.sugarLevel !== SUGAR_LEVEL.NORMAL)
    parts.push(SUGAR_LEVEL_MAP[item.sugarLevel]?.label ?? item.sugarLevel);
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
const placeOrder = async () => {
  if (!canPlaceOrder.value) return;
  errorMessage.value = "";
  placing.value = true;
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
      })),
    );
    const { orderId } = res.data;
    toast.add({
      severity: "success",
      summary: "Order placed",
      detail: `Order #${res.data.orderNumber} created.`,
      life: 3000,
    });
    router.push({ name: "ordersDetail", params: { id: orderId } });
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.errors?.map((e) => e.errorMessage ?? e).join("; ") ||
      err?.response?.data?.message ||
      "Đặt hàng thất bại.";
  } finally {
    placing.value = false;
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
        icon="pi pi-arrow-left"
        severity="secondary"
        text
        @click="router.push({ name: 'orders' })"
      />
      <div>
        <p
          class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300"
        >
          Orders
        </p>
        <h1 class="tw:text-2xl tw:font-semibold">Create order</h1>
      </div>
    </div>

    <!-- Table / Session bar -->
    <div
      class="app-panel tw:rounded-2xl tw:border tw:px-4 tw:py-3 tw:flex tw:flex-wrap tw:items-center tw:gap-3"
    >
      <prime-select
        v-model="selectedTableId"
        :options="tables"
        optionLabel="code"
        optionValue="id"
        placeholder="Select a table"
        :disabled="sessionLoading"
        class="tw:w-40"
        @change="(e) => onTableSelect(e.value)"
      />
      <template v-if="sessionLoading">
        <prime-tag severity="secondary">
          <iconify icon="prime:spinner" />
          <span>Connecting...</span>
        </prime-tag>
      </template>
      <template v-else-if="sessionId">
        <prime-tag v-if="sessionHadExisting" severity="info">
          <iconify icon="prime:info-circle" />
          <span>Table has an existing session. Order will be added to it.</span>
        </prime-tag>
        <prime-tag v-else severity="success">
          <iconify icon="prime:check" />
          <span>Session ready</span>
        </prime-tag>
      </template>
    </div>

    <!-- Main grid -->
    <div class="tw:grid tw:gap-6 tw:lg:grid-cols-12">
      <!-- ── LEFT: Menu ──────────────────────────────────────── -->
      <section class="tw:lg:col-span-8 tw:space-y-4">
        <!-- Search -->
        <div class="tw:relative">
          <i
            class="pi pi-search tw:absolute tw:left-3 tw:top-1/2 tw:-translate-y-1/2 tw:text-sm app-text-subtle tw:pointer-events-none"
          />
          <prime-input-text
            v-model="searchQuery"
            placeholder="Search..."
            class="tw:w-full tw:pl-9"
          />
        </div>

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
        <div
          v-else-if="!loadingMenu && visibleCategories.length === 0"
          class="app-panel tw:rounded-2xl tw:border tw:p-12 tw:text-center app-text-muted"
        >
          <iconify
            icon="ph:magnifying-glass-bold"
            class="tw:text-3xl tw:mb-3 tw:block tw:mx-auto tw:opacity-40"
          />
          No products found.
        </div>

        <!-- Categories (collapsible) -->
        <div v-else class="tw:space-y-4">
          <div
            v-for="category in visibleCategories"
            :key="category.id"
            class="app-panel tw:rounded-2xl tw:border tw:overflow-hidden"
          >
            <!-- Category header — click to collapse -->
            <button
              class="tw:w-full tw:flex tw:items-center tw:justify-between tw:px-4 tw:py-3 tw:transition hover:tw:bg-white/5"
              @click="toggleCategory(category.id)"
            >
              <div class="tw:flex tw:items-center tw:gap-2">
                <h2 class="tw:text-sm tw:font-semibold">{{ category.name }}</h2>
                <span class="tw:text-xs app-text-muted"
                  >{{ category.filteredProducts.length }} items</span
                >
              </div>
              <iconify
                icon="ph:caret-down-bold"
                class="tw:text-sm app-text-muted tw:transition-transform tw:duration-200"
                :class="collapsedCategories[category.id] ? 'tw:-rotate-90' : ''"
              />
            </button>

            <!-- Products grid -->
            <div
              v-show="!collapsedCategories[category.id]"
              class="tw:grid tw:grid-cols-2 tw:gap-3 sm:tw:grid-cols-3 tw:p-3 tw:pt-0"
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
                      >Temp</span
                    >
                    <span
                      v-if="product.hasIceLevelOption"
                      class="tw:rounded tw:px-1 tw:py-0.5 tw:text-xs tw:bg-sky-500/10 tw:text-sky-400"
                      >Ice</span
                    >
                    <span
                      v-if="product.hasSugarLevelOption"
                      class="tw:rounded tw:px-1 tw:py-0.5 tw:text-xs tw:bg-amber-500/10 tw:text-amber-400"
                      >Sugar</span
                    >
                  </div>

                  <div class="tw:flex-1" />

                  <!-- Add button -->
                  <div class="tw:flex tw:justify-end tw:mt-2">
                    <prime-button
                      icon="pi pi-plus"
                      size="small"
                      rounded
                      text
                      severity="success"
                      @click.stop="handleAddToCart(product)"
                    />
                  </div>
                </div>
              </article>
            </div>
          </div>
        </div>
      </section>

      <!-- ── RIGHT: Cart ────────────────────────────────────── -->
      <aside class="tw:lg:col-span-4 tw:lg:self-start tw:lg:sticky tw:lg:top-6">
        <div class="app-panel tw:rounded-2xl tw:border tw:p-5">
          <!-- Cart header -->
          <div class="tw:flex tw:items-center tw:justify-between tw:mb-4">
            <h2
              class="tw:text-base tw:font-semibold tw:flex tw:items-center tw:gap-2"
            >
              Cart
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
            Cart is empty<br />
            <span class="tw:text-xs tw:opacity-60">
              Choose a product to add it to your cart.
            </span>
          </div>

          <!-- Cart items -->
          <div v-else class="tw:space-y-2">
            <div
              v-for="item in cart"
              :key="item._key"
              class="tw:rounded-xl tw:border tw:p-3"
              style="border-color: var(--app-border)"
            >
              <div class="tw:flex tw:items-start tw:gap-2">
                <div class="tw:flex-1 tw:min-w-0">
                  <p class="tw:text-sm tw:font-medium tw:leading-snug">
                    {{ item.productName }}
                  </p>
                  <p class="tw:text-xs tw:text-emerald-400 tw:mt-0.5">
                    {{ formatVnd(item.unitPrice) }}
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
                    >Takeaway</span
                  >
                </div>
                <div class="tw:flex tw:items-center tw:gap-1 tw:shrink-0">
                  <prime-button
                    icon="pi pi-minus"
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
                <span class="tw:text-sm tw:font-semibold tw:text-emerald-400">
                  {{ formatVnd(item.unitPrice * item.quantity) }}
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
                <span class="app-text-muted">Subtotal</span>
                <span class="tw:font-medium">{{ formatVnd(cartTotal) }}</span>
              </div>
              <div
                class="tw:flex tw:items-center tw:justify-between tw:text-xs tw:mb-2"
              >
                <span class="app-text-muted">Service Charge</span>
                <span class="app-text-muted">Free</span>
              </div>
              <div
                class="tw:border-t tw:pt-2"
                style="border-color: var(--app-border)"
              >
                <div
                  class="tw:flex tw:items-center tw:justify-between tw:font-bold"
                >
                  <span>Total</span>
                  <span class="tw:text-emerald-400 tw:text-base">{{
                    formatVnd(cartTotal)
                  }}</span>
                </div>
              </div>
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
            <span>Create order</span>
          </prime-button>

          <p
            v-if="!sessionId && !sessionLoading"
            class="tw:text-xs app-text-muted tw:text-center tw:mt-2"
          >
            Select a table ( use BAR for orders at the bar )
          </p>
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
        <p class="tw:mb-2 tw:text-sm tw:font-semibold">Quantity</p>
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
        <p class="tw:mb-2 tw:text-sm tw:font-semibold">Temperature</p>
        <div class="tw:flex tw:gap-2">
           <prime-button
           v-for="opt in DRINK_TEMPERATURE_OPTIONS"
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

      <!-- Ice level — only when Cold -->
      <div
        v-if="
          selectedProduct?.hasIceLevelOption &&
          pendingOptions.temperature !== DRINK_TEMPERATURE.HOT
        "
      >
        <p class="tw:mb-2 tw:text-sm tw:font-semibold">Ice level</p>
        <div class="tw:grid tw:grid-cols-2 tw:gap-2">
          <prime-button
            v-for="opt in ICE_LEVEL_OPTIONS"
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

      <!-- Sugar level — only when Cold -->
      <div
        v-if="
          selectedProduct?.hasSugarLevelOption &&
          pendingOptions.temperature !== DRINK_TEMPERATURE.HOT
        "
      >
        <p class="tw:mb-2 tw:text-sm tw:font-semibold">Sugar level</p>
        <div class="tw:grid tw:grid-cols-3 tw:gap-2">
          <prime-button
            v-for="opt in SUGAR_LEVEL_OPTIONS"
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
        <p class="tw:mb-2 tw:text-sm tw:font-semibold">Serving</p>
        <div class="tw:flex tw:gap-2">
          <prime-button
            v-for="servingType in SERVING_TYPE_OPTIONS"
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
    </div>

    <template #footer>
      <prime-button
        severity="secondary"
        text
        @click="showOptionsDialog = false"
      >
        <span class="tw:ml-2">Cancel</span>
      </prime-button>
      <prime-button @click="confirmAddToCart">
        <iconify icon="prime:shopping-cart" />
        <span class="tw:ml-2">Add to cart</span>
      </prime-button>
    </template>
  </prime-dialog>
</template>
