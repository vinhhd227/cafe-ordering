<script setup>
import { computed, onMounted, ref, watch } from "vue";
import { useRouter } from "vue-router";
import { useToast } from "primevue/usetoast";
import { getAdminMenu } from "@/services/menu.service";
import { createOrder } from "@/services/order.service";
import { listTables, getOrCreateSession, createCounterSession } from "@/services/table.service";

const router = useRouter();
const toast = useToast();

// ── Data ─────────────────────────────────────────────────────────
const tables = ref([]);
const menuCategories = ref([]);
const loadingMenu = ref(false);
const loadingTables = ref(false);

// ── Selection ────────────────────────────────────────────────────
const isTakeaway = ref(false);
const selectedTableId = ref(null);
const sessionId = ref(null);
const sessionHadExisting = ref(false);
const sessionLoading = ref(false);

// ── Options dialog ───────────────────────────────────────────────
const showOptionsDialog = ref(false);
const selectedProduct = ref(null);
const pendingQuantity = ref(1);
const pendingOptions = ref({ temperature: null, iceLevel: null, sugarLevel: null });

// ── Cart ─────────────────────────────────────────────────────────
const cart = ref([]);

// ── Filter ───────────────────────────────────────────────────────
const searchQuery = ref("");
const activeCategoryId = ref(null);

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
  const { temperature = "", iceLevel = "", sugarLevel = "" } = opts ?? {};
  return `${productId}|${temperature}|${iceLevel}|${sugarLevel}`;
};

const optionsLabel = (item) => {
  const parts = [];
  if (item.temperature) parts.push(item.temperature);
  if (item.iceLevel && item.iceLevel !== "Bình thường") parts.push(`Đá: ${item.iceLevel}`);
  if (item.sugarLevel && item.sugarLevel !== "Bình thường") parts.push(`Đường: ${item.sugarLevel}`);
  return parts.join(" · ");
};

// ── Computed ─────────────────────────────────────────────────────
const activeCategories = computed(() =>
  menuCategories.value.filter((c) => c.isActive && c.products?.some((p) => p.isActive))
);

const visibleCategories = computed(() => {
  let cats = menuCategories.value.filter((c) => c.isActive);
  if (activeCategoryId.value) cats = cats.filter((c) => c.id === activeCategoryId.value);
  return cats
    .map((c) => {
      let products = (c.products ?? []).filter((p) => p.isActive);
      if (searchQuery.value.trim()) {
        const q = searchQuery.value.toLowerCase();
        products = products.filter(
          (p) =>
            p.name.toLowerCase().includes(q) ||
            (p.description?.toLowerCase().includes(q) ?? false)
        );
      }
      return { ...c, filteredProducts: products };
    })
    .filter((c) => c.filteredProducts.length > 0);
});

const cartTotal = computed(() =>
  cart.value.reduce((sum, i) => sum + i.unitPrice * i.quantity, 0)
);

const cartItemCount = computed(() =>
  cart.value.reduce((sum, i) => sum + i.quantity, 0)
);

const canPlaceOrder = computed(
  () => !!sessionId.value && cart.value.length > 0 && !placing.value
);

const orderLabel = computed(() => {
  if (isTakeaway.value) return "Takeaway";
  const t = tables.value.find((t) => t.id === selectedTableId.value);
  return t ? `Bàn ${t.code}` : "";
});

// ── Cart helpers ──────────────────────────────────────────────────
const cartQuantity = (productId) =>
  cart.value.filter((i) => i.productId === productId).reduce((sum, i) => sum + i.quantity, 0);

const changeQty = (key, delta) => {
  const idx = cart.value.findIndex((i) => i._key === key);
  if (idx === -1) return;
  cart.value[idx].quantity += delta;
  if (cart.value[idx].quantity <= 0) cart.value.splice(idx, 1);
};

const clearCart = () => { cart.value = []; };

// ── Options dialog ────────────────────────────────────────────────
const handleAddToCart = (product) => {
  selectedProduct.value = product;
  pendingOptions.value = {
    temperature: product.hasTemperatureOption ? "Lạnh" : null,
    iceLevel: product.hasIceLevelOption ? "Bình thường" : null,
    sugarLevel: product.hasSugarLevelOption ? "Bình thường" : null,
  };
  pendingQuantity.value = 1;
  showOptionsDialog.value = true;
};

const setTemperature = (opt) => {
  pendingOptions.value.temperature = opt;
  if (opt === "Nóng") {
    if (selectedProduct.value?.hasIceLevelOption) pendingOptions.value.iceLevel = "Không đá";
    if (selectedProduct.value?.hasSugarLevelOption) pendingOptions.value.sugarLevel = "Bình thường";
  } else if (opt === "Lạnh") {
    if (selectedProduct.value?.hasIceLevelOption && pendingOptions.value.iceLevel === "Không đá")
      pendingOptions.value.iceLevel = "Bình thường";
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
      // Counter session mặc định mang về; bàn mặc định tại chỗ
      isTakeaway: isTakeaway.value,
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
  isTakeaway.value = false;
  sessionLoading.value = true;
  sessionId.value = null;
  try {
    const res = await getOrCreateSession(tableId);
    const existed = !!tables.value.find((t) => t.id === tableId)?.activeSessionId;
    sessionId.value = res.data.sessionId;
    sessionHadExisting.value = existed;
  } catch {
    errorMessage.value = "Không thể lấy session cho bàn này.";
  } finally {
    sessionLoading.value = false;
  }
};

const onTakeawayToggle = async () => {
  if (!isTakeaway.value) {
    sessionId.value = null;
    sessionHadExisting.value = false;
    return;
  }
  selectedTableId.value = null;
  sessionId.value = null;
  sessionHadExisting.value = false;
  sessionLoading.value = true;
  try {
    const res = await createCounterSession();
    sessionId.value = res.data.sessionId;
  } catch {
    errorMessage.value = "Không thể tạo counter session.";
    isTakeaway.value = false;
  } finally {
    sessionLoading.value = false;
  }
};

watch(isTakeaway, (val, old) => {
  if (val !== old) onTakeawayToggle();
});

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
      }))
    );
    const { orderId } = res.data;
    toast.add({
      severity: "success",
      summary: "Đặt hàng thành công",
      detail: `Order #${res.data.orderNumber} đã tạo.`,
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
    const [tablesRes, menuRes] = await Promise.all([listTables(), getAdminMenu()]);
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
      <prime-button icon="pi pi-arrow-left" severity="secondary" text @click="router.push({ name: 'orders' })" />
      <div>
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300">Orders</p>
        <h1 class="tw:text-2xl tw:font-semibold">Tạo đơn mới</h1>
      </div>
    </div>

    <!-- Table / Takeaway / Session bar -->
    <div class="app-panel tw:rounded-2xl tw:border tw:px-4 tw:py-3 tw:flex tw:flex-wrap tw:items-center tw:gap-3">
      <prime-select
        v-model="selectedTableId"
        :options="tables"
        optionLabel="code"
        optionValue="id"
        placeholder="Chọn bàn"
        :disabled="isTakeaway || sessionLoading"
        class="tw:w-40"
        @change="(e) => onTableSelect(e.value)"
      />
      <div class="tw:flex tw:items-center tw:gap-2">
        <prime-toggle-switch v-model="isTakeaway" :disabled="sessionLoading" />
        <span class="tw:text-sm tw:font-medium">Takeaway</span>
      </div>
      <template v-if="sessionLoading">
        <prime-tag icon="pi pi-spin pi-spinner" value="Đang kết nối..." severity="secondary" />
      </template>
      <template v-else-if="sessionId">
        <prime-tag
          v-if="sessionHadExisting && !isTakeaway"
          icon="pi pi-info-circle"
          value="Bàn đang có session — order sẽ được thêm vào"
          severity="info"
        />
        <prime-tag v-else icon="pi pi-check" value="Session sẵn sàng" severity="success" />
      </template>
    </div>

    <!-- Main grid -->
    <div class="tw:grid tw:gap-6 tw:lg:grid-cols-12">

      <!-- ── LEFT: Menu ──────────────────────────────────────── -->
      <section class="tw:lg:col-span-8 tw:space-y-4">

        <!-- Search + category tabs -->
        <div class="app-panel tw:rounded-2xl tw:border tw:p-4 tw:space-y-3">
          <div class="tw:relative">
            <i class="pi pi-search tw:absolute tw:left-3 tw:top-1/2 tw:-translate-y-1/2 tw:text-sm app-text-subtle tw:pointer-events-none" />
            <prime-input-text
              v-model="searchQuery"
              placeholder="Tìm sản phẩm..."
              class="tw:w-full tw:pl-9"
            />
          </div>
          <div class="tw:flex tw:flex-wrap tw:gap-2">
            <prime-button
              label="Tất cả"
              size="small"
              :severity="activeCategoryId === null ? 'success' : 'secondary'"
              :outlined="activeCategoryId !== null"
              @click="activeCategoryId = null"
            />
            <prime-button
              v-for="cat in activeCategories"
              :key="cat.id"
              :label="cat.name"
              size="small"
              :severity="activeCategoryId === cat.id ? 'success' : 'secondary'"
              :outlined="activeCategoryId !== cat.id"
              @click="activeCategoryId = cat.id"
            />
          </div>
        </div>

        <!-- Loading skeleton -->
        <div v-if="loadingMenu" class="tw:grid tw:grid-cols-2 tw:gap-4 sm:tw:grid-cols-3">
          <div v-for="n in 6" :key="n" class="tw:animate-pulse tw:rounded-2xl app-panel tw:border tw:overflow-hidden">
            <div class="tw:h-36 tw:w-full" style="background: var(--app-bg-subtle)" />
            <div class="tw:p-3 tw:space-y-2">
              <div class="tw:h-4 tw:w-3/4 tw:rounded" style="background: var(--app-bg-subtle)" />
              <div class="tw:h-3 tw:w-1/2 tw:rounded" style="background: var(--app-bg-subtle)" />
            </div>
          </div>
        </div>

        <!-- Empty state -->
        <div
          v-else-if="!loadingMenu && visibleCategories.length === 0"
          class="app-panel tw:rounded-2xl tw:border tw:p-12 tw:text-center app-text-muted"
        >
          <iconify icon="ph:magnifying-glass-bold" class="tw:text-3xl tw:mb-3 tw:block tw:mx-auto tw:opacity-40" />
          Không tìm thấy sản phẩm nào.
        </div>

        <!-- Products by category -->
        <div v-else class="tw:space-y-6">
          <div v-for="category in visibleCategories" :key="category.id">

            <!-- Category header -->
            <div class="tw:flex tw:items-center tw:justify-between tw:mb-3">
              <h2 class="tw:text-base tw:font-semibold">{{ category.name }}</h2>
              <span class="tw:text-xs app-text-muted">{{ category.filteredProducts.length }} món</span>
            </div>

            <!-- Product grid -->
            <div class="tw:grid tw:grid-cols-2 tw:gap-4 sm:tw:grid-cols-3">
              <article
                v-for="product in category.filteredProducts"
                :key="product.id"
                class="app-panel tw:group tw:flex tw:h-full tw:flex-col tw:overflow-hidden tw:rounded-2xl tw:border tw:cursor-pointer tw:transition-all hover:tw:-translate-y-0.5 hover:tw:border-emerald-500/50"
                @click="handleAddToCart(product)"
              >
                <!-- Product image / placeholder -->
                <div class="tw:relative tw:overflow-hidden tw:shrink-0">
                  <img
                    v-if="product.imageUrl"
                    :src="product.imageUrl"
                    :alt="product.name"
                    class="tw:h-36 tw:w-full tw:object-cover"
                  />
                  <div
                    v-else
                    class="tw:h-36 tw:w-full tw:flex tw:items-center tw:justify-center"
                    style="background: var(--app-bg-subtle)"
                  >
                    <iconify icon="ph:coffee-bold" class="tw:text-4xl tw:text-emerald-400/30" />
                  </div>
                  <!-- Cart qty badge overlay -->
                  <div
                    v-if="cartQuantity(product.id) > 0"
                    class="tw:absolute tw:top-2 tw:right-2 tw:h-6 tw:min-w-6 tw:px-1.5 tw:rounded-full tw:bg-emerald-500 tw:flex tw:items-center tw:justify-center tw:text-xs tw:font-bold tw:text-white tw:shadow-lg"
                  >
                    {{ cartQuantity(product.id) }}
                  </div>
                </div>

                <!-- Product info -->
                <div class="tw:flex tw:flex-1 tw:flex-col tw:p-3">
                  <h3 class="tw:text-sm tw:font-semibold tw:line-clamp-2 tw:leading-snug">{{ product.name }}</h3>
                  <p class="tw:mt-1 tw:text-xs tw:font-semibold tw:text-emerald-400">
                    {{ formatVnd(product.price) }}
                  </p>
                  <p v-if="product.description" class="tw:mt-1 tw:text-xs app-text-muted tw:line-clamp-2 tw:flex-1">
                    {{ product.description }}
                  </p>
                  <div v-else class="tw:flex-1" />

                  <!-- Option badges -->
                  <div
                    v-if="product.hasTemperatureOption || product.hasIceLevelOption || product.hasSugarLevelOption"
                    class="tw:mt-2 tw:flex tw:flex-wrap tw:gap-1"
                  >
                    <span
                      v-if="product.hasTemperatureOption"
                      class="tw:rounded tw:px-1.5 tw:py-0.5 tw:text-xs tw:font-medium tw:bg-orange-500/10 tw:text-orange-400"
                    >Nóng/Lạnh</span>
                    <span
                      v-if="product.hasIceLevelOption"
                      class="tw:rounded tw:px-1.5 tw:py-0.5 tw:text-xs tw:font-medium tw:bg-sky-500/10 tw:text-sky-400"
                    >Mức đá</span>
                    <span
                      v-if="product.hasSugarLevelOption"
                      class="tw:rounded tw:px-1.5 tw:py-0.5 tw:text-xs tw:font-medium tw:bg-amber-500/10 tw:text-amber-400"
                    >Mức đường</span>
                  </div>

                  <!-- Add button -->
                  <div class="tw:flex tw:justify-end tw:mt-3">
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
            <h2 class="tw:text-base tw:font-semibold tw:flex tw:items-center tw:gap-2">
              Giỏ hàng
              <prime-badge v-if="cartItemCount > 0" :value="cartItemCount" severity="success" />
            </h2>
            <prime-button
              v-if="cart.length > 0"
              icon="pi pi-trash"
              size="small"
              severity="danger"
              text
              @click="clearCart"
            />
          </div>

          <!-- Order label -->
          <div v-if="orderLabel" class="tw:mb-3">
            <prime-tag
              :value="orderLabel"
              :icon="isTakeaway ? 'pi pi-shopping-bag' : 'pi pi-table'"
              severity="secondary"
            />
          </div>

          <!-- Empty state -->
          <div
            v-if="cart.length === 0"
            class="tw:py-8 tw:text-center app-text-muted tw:text-sm tw:rounded-xl tw:border tw:border-dashed"
            style="border-color: var(--app-border)"
          >
            Chưa có món nào.<br />
            <span class="tw:text-xs tw:opacity-60">Chọn món từ menu bên trái.</span>
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
                  <p class="tw:text-sm tw:font-medium tw:leading-snug">{{ item.productName }}</p>
                  <p class="tw:text-xs tw:text-emerald-400 tw:mt-0.5">{{ formatVnd(item.unitPrice) }}</p>
                  <p v-if="optionsLabel(item)" class="tw:text-xs tw:text-amber-400 tw:mt-0.5 tw:leading-snug">
                    {{ optionsLabel(item) }}
                  </p>
                  <!-- Toggle tại chỗ / mang về -->
                  <div class="tw:flex tw:gap-1 tw:mt-1.5">
                    <button
                      class="tw:text-xs tw:px-2 tw:py-0.5 tw:rounded-lg tw:border tw:transition"
                      :class="!item.isTakeaway
                        ? 'tw:border-emerald-400 tw:bg-emerald-500/10 tw:text-emerald-400'
                        : 'tw:border-white/10 app-text-muted hover:tw:border-white/30'"
                      @click.stop="item.isTakeaway = false"
                    >Tại chỗ</button>
                    <button
                      class="tw:text-xs tw:px-2 tw:py-0.5 tw:rounded-lg tw:border tw:transition"
                      :class="item.isTakeaway
                        ? 'tw:border-sky-400 tw:bg-sky-500/10 tw:text-sky-400'
                        : 'tw:border-white/10 app-text-muted hover:tw:border-white/30'"
                      @click.stop="item.isTakeaway = true"
                    >Mang về</button>
                  </div>
                </div>
                <div class="tw:flex tw:items-center tw:gap-1 tw:shrink-0">
                  <prime-button
                    icon="pi pi-minus"
                    size="small"
                    text
                    rounded
                    severity="secondary"
                    @click="changeQty(item._key, -1)"
                  />
                  <span class="tw:text-sm tw:font-bold tw:w-5 tw:text-center">{{ item.quantity }}</span>
                  <prime-button
                    icon="pi pi-plus"
                    size="small"
                    text
                    rounded
                    severity="secondary"
                    @click="changeQty(item._key, 1)"
                  />
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
                <span class="app-text-muted">Tạm tính</span>
                <span class="tw:font-medium">{{ formatVnd(cartTotal) }}</span>
              </div>
              <div class="tw:flex tw:items-center tw:justify-between tw:text-xs tw:mb-2">
                <span class="app-text-muted">Phí phục vụ</span>
                <span class="app-text-muted">Miễn phí</span>
              </div>
              <div class="tw:border-t tw:pt-2" style="border-color: var(--app-border)">
                <div class="tw:flex tw:items-center tw:justify-between tw:font-bold">
                  <span>Tổng cộng</span>
                  <span class="tw:text-emerald-400 tw:text-base">{{ formatVnd(cartTotal) }}</span>
                </div>
              </div>
            </div>
          </div>

          <!-- Error -->
          <prime-message v-if="errorMessage" severity="error" :closable="false" class="tw:mt-3">
            {{ errorMessage }}
          </prime-message>

          <!-- Place order -->
          <prime-button
            label="Đặt order"
            icon="pi pi-check"
            class="tw:w-full tw:mt-4"
            :loading="placing"
            :disabled="!canPlaceOrder"
            @click="placeOrder"
          />

          <p v-if="!sessionId && !sessionLoading" class="tw:text-xs app-text-muted tw:text-center tw:mt-2">
            Chọn bàn hoặc bật Takeaway trước
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

      <!-- Số lượng -->
      <div>
        <p class="tw:mb-2 tw:text-sm tw:font-semibold">Số lượng</p>
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

      <!-- Nhiệt độ -->
      <div v-if="selectedProduct?.hasTemperatureOption">
        <p class="tw:mb-2 tw:text-sm tw:font-semibold">Nhiệt độ</p>
        <div class="tw:flex tw:gap-2">
          <button
            v-for="opt in ['Nóng', 'Lạnh']"
            :key="opt"
            class="tw:flex-1 tw:rounded-xl tw:border tw:px-4 tw:py-2 tw:text-sm tw:font-medium tw:transition"
            :class="pendingOptions.temperature === opt
              ? (opt === 'Nóng'
                  ? 'tw:border-orange-400 tw:bg-orange-500/10 tw:text-orange-400'
                  : 'tw:border-sky-400 tw:bg-sky-500/10 tw:text-sky-400')
              : 'tw:border-white/10 app-text-muted hover:tw:border-white/30'"
            @click="setTemperature(opt)"
          >{{ opt }}</button>
        </div>
      </div>

      <!-- Mức đá — chỉ hiện khi chọn Lạnh (hoặc không có tuỳ chọn nhiệt độ) -->
      <div v-if="selectedProduct?.hasIceLevelOption && pendingOptions.temperature !== 'Nóng'">
        <p class="tw:mb-2 tw:text-sm tw:font-semibold">Mức đá</p>
        <div class="tw:grid tw:grid-cols-2 tw:gap-2">
          <button
            v-for="opt in ['Không đá', 'Ít đá', 'Bình thường', 'Nhiều đá']"
            :key="opt"
            class="tw:rounded-xl tw:border tw:px-3 tw:py-2 tw:text-sm tw:font-medium tw:transition"
            :class="pendingOptions.iceLevel === opt
              ? 'tw:border-sky-400 tw:bg-sky-500/10 tw:text-sky-400'
              : 'tw:border-white/10 app-text-muted hover:tw:border-sky-400/50'"
            @click="pendingOptions.iceLevel = opt"
          >{{ opt }}</button>
        </div>
      </div>

      <!-- Mức đường — chỉ hiện khi chọn Lạnh (hoặc không có tuỳ chọn nhiệt độ) -->
      <div v-if="selectedProduct?.hasSugarLevelOption && pendingOptions.temperature !== 'Nóng'">
        <p class="tw:mb-2 tw:text-sm tw:font-semibold">Mức đường</p>
        <div class="tw:grid tw:grid-cols-2 tw:gap-2">
          <button
            v-for="opt in ['Không đường', 'Ít đường', 'Bình thường', 'Nhiều đường']"
            :key="opt"
            class="tw:rounded-xl tw:border tw:px-3 tw:py-2 tw:text-sm tw:font-medium tw:transition"
            :class="pendingOptions.sugarLevel === opt
              ? 'tw:border-amber-400 tw:bg-amber-500/10 tw:text-amber-400'
              : 'tw:border-white/10 app-text-muted hover:tw:border-amber-400/50'"
            @click="pendingOptions.sugarLevel = opt"
          >{{ opt }}</button>
        </div>
      </div>

    </div>

    <template #footer>
      <prime-button label="Huỷ" severity="secondary" text @click="showOptionsDialog = false" />
      <prime-button
        label="Thêm vào giỏ"
        icon="pi pi-shopping-cart"
        @click="confirmAddToCart"
      />
    </template>
  </prime-dialog>
</template>
