<script setup>
import { computed, onMounted, ref } from 'vue';
import { useRoute } from 'vue-router';
import { getMenu } from '../services/product.service.js';
import { getOrCreateSession, getSessionSummary } from '../services/session.service.js';
import { placeOrder as placeOrderApi, updateOrderItem } from '../services/order.service.js';
import { useCartStore } from '../stores/cart.js';

const cartStore = useCartStore();

const route   = useRoute();
const tableId = computed(() => Number(route.params.tableId));

/* ─── Menu ─────────────────────────────────────────────── */
const menu      = ref([]);
const isLoading = ref(false);
const loadError = ref('');

/* ─── Session ──────────────────────────────────────────── */
const session      = ref(null);
const sessionError = ref('');

/* ─── Options dialog ───────────────────────────────────── */
const showOptionsDialog = ref(false);
const selectedProduct   = ref(null);
const pendingQuantity   = ref(1);
const pendingOptions    = ref({
  temperature: 'Lạnh',
  iceLevel:    'Bình thường',
  sugarLevel:  'Bình thường',
});

/* ─── Order ────────────────────────────────────────────── */
const isOrdering   = ref(false);
const orderSuccess = ref('');
const orderError   = ref('');

/* ─── Summary ──────────────────────────────────────────── */
const showSummary      = ref(false);
const summary          = ref(null);
const isSummaryLoading = ref(false);
const itemUpdating     = ref(null);   // tracks "orderId-productId" being updated

/* ─── Helpers ──────────────────────────────────────────── */
const formatPrice = (value) => `${Number(value).toLocaleString()}đ`;

const optionsLabel = (options) => {
  if (!options) return '';
  const parts = [];
  if (options.temperature) parts.push(options.temperature);
  if (options.iceLevel   && options.iceLevel   !== 'Bình thường') parts.push(`Đá: ${options.iceLevel}`);
  if (options.sugarLevel && options.sugarLevel !== 'Bình thường') parts.push(`Đường: ${options.sugarLevel}`);
  return parts.join(' · ');
};

/* ─── Session ──────────────────────────────────────────── */
const fetchSession = async () => {
  if (!tableId.value) {
    sessionError.value = 'Không tìm thấy số bàn.';
    return;
  }
  try {
    session.value = await getOrCreateSession(tableId.value);
  } catch {
    sessionError.value = 'Không kết nối được bàn. Vui lòng thử lại.';
  }
};

/* ─── Menu ─────────────────────────────────────────────── */
const fetchProducts = async () => {
  isLoading.value = true;
  loadError.value = '';
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
                id:                   p.id                   ?? p.Id,
                name:                 p.name                 ?? p.Name,
                description:          p.description          ?? p.Description,
                price:                p.price                ?? p.Price,
                imageUrl:             p.imageUrl             ?? p.ImageUrl,
                hasTemperatureOption: p.hasTemperatureOption ?? p.HasTemperatureOption ?? false,
                hasIceLevelOption:    p.hasIceLevelOption    ?? p.HasIceLevelOption    ?? false,
                hasSugarLevelOption:  p.hasSugarLevelOption  ?? p.HasSugarLevelOption  ?? false,
              }))
            : [];
          return {
            id:       category.id   ?? category.Id,
            name:     category.name ?? category.Name,
            products,
          };
        })
      : [];
  } catch (error) {
    console.error('Lỗi khi lấy menu:', error);
    loadError.value = 'Không tải được menu. Vui lòng thử lại.';
  } finally {
    isLoading.value = false;
  }
};

/* ─── Cart ─────────────────────────────────────────────── */
// Luôn mở popup để khách chọn số lượng và tùy chọn (đường/đá nếu có)
const handleAddToCart = (product) => {
  selectedProduct.value   = product;
  pendingOptions.value    = { temperature: 'Lạnh', iceLevel: 'Bình thường', sugarLevel: 'Bình thường' };
  pendingQuantity.value   = 1;
  showOptionsDialog.value = true;
};

const confirmAddToCart = () => {
  cartStore.addItem(selectedProduct.value, { ...pendingOptions.value }, pendingQuantity.value);
  showOptionsDialog.value = false;
  selectedProduct.value   = null;
};

/* ─── Order ────────────────────────────────────────────── */
const submitOrder = async () => {
  if (!cartStore.items.length || !session.value) return;
  isOrdering.value   = true;
  orderSuccess.value = '';
  orderError.value   = '';
  try {
    const sessionId = session.value.sessionId ?? session.value.id;
    const payload   = {
      sessionId,
      items: cartStore.items.map((item) => ({
        productId:   item.id,
        productName: item.name,
        unitPrice:   item.price,
        quantity:    item.quantity,
      })),
    };
    const result = await placeOrderApi(payload);
    cartStore.clear();
    orderSuccess.value = `Đặt hàng thành công! Mã đơn: ${result.orderNumber ?? result.OrderNumber}`;
  } catch (e) {
    orderError.value = e?.detail ?? e?.message ?? 'Đặt hàng thất bại. Vui lòng thử lại.';
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
    summary.value      = await getSessionSummary(sessionId);
    showSummary.value  = true;
  } catch {
    orderError.value = 'Không tải được hóa đơn. Vui lòng thử lại.';
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
    orderError.value = e?.detail ?? e?.message ?? 'Cập nhật thất bại.';
  } finally {
    itemUpdating.value = null;
  }
};

const decrementClientItem = (orderId, productId, currentQty) => {
  if (currentQty <= 1) {
    if (!confirm('Xoá món này khỏi đơn?')) return;
    setClientItemQty(orderId, productId, 0);
  } else {
    setClientItemQty(orderId, productId, currentQty - 1);
  }
};

const removeClientItem = (orderId, productId) => {
  if (!confirm('Xoá món này khỏi đơn?')) return;
  setClientItemQty(orderId, productId, 0);
};

onMounted(async () => {
  await fetchSession();
  await fetchProducts();
});
</script>

<template>
  <div class="tw:min-h-screen tw:bg-linear-to-b tw:from-amber-50 tw:via-white tw:to-orange-50">
    <div class="tw:mx-auto tw:max-w-6xl tw:px-4 tw:py-8">

      <!-- Session banner -->
      <div
        v-if="tableId"
        class="tw:mb-4 tw:flex tw:items-center tw:justify-between tw:rounded-2xl tw:bg-white/70 tw:px-6 tw:py-3 tw:shadow-sm tw:backdrop-blur"
      >
        <div class="tw:flex tw:items-center tw:gap-3">
          <iconify icon="heroicons-outline:table-cells" class="tw:text-xl tw:text-orange-400" />
          <span class="tw:text-sm tw:font-medium tw:text-slate-700">Bàn {{ tableId }}</span>
          <span
            v-if="session"
            class="tw:rounded-full tw:bg-green-100 tw:px-2 tw:py-0.5 tw:text-xs tw:font-semibold tw:text-green-700"
          >Active</span>
        </div>
        <prime-button
          v-if="session"
          label="Xem hóa đơn"
          icon="pi pi-file"
          severity="secondary"
          text
          :loading="isSummaryLoading"
          @click="fetchSummary"
        />
      </div>
      <div
        v-if="sessionError"
        class="tw:mb-4 tw:rounded-2xl tw:border tw:border-rose-200 tw:bg-rose-50 tw:px-6 tw:py-3 tw:text-sm tw:text-rose-700"
      >
        {{ sessionError }}
      </div>

      <!-- Header -->
      <div class="tw:mb-8 tw:rounded-2xl tw:bg-white/70 tw:p-6 tw:shadow-sm tw:backdrop-blur">
        <p class="tw:text-sm tw:uppercase tw:tracking-[0.3em] tw:text-orange-400">Cafe Ordering</p>
        <h1 class="tw:mt-3 tw:text-3xl tw:font-semibold tw:text-slate-900 tw:sm:text-4xl">
          Chọn món yêu thích, đặt nhanh trong một chạm
        </h1>
        <p class="tw:mt-3 tw:max-w-2xl tw:text-slate-600">
          Menu được cập nhật theo thời gian thực từ API. Thêm món vào giỏ và theo dõi tổng
          tiền ngay bên cạnh.
        </p>
      </div>

      <!-- Order notifications -->
      <div
        v-if="orderSuccess"
        class="tw:mb-4 tw:rounded-2xl tw:border tw:border-green-200 tw:bg-green-50 tw:px-6 tw:py-3 tw:text-sm tw:text-green-700"
      >
        {{ orderSuccess }}
      </div>
      <div
        v-if="orderError"
        class="tw:mb-4 tw:rounded-2xl tw:border tw:border-rose-200 tw:bg-rose-50 tw:px-6 tw:py-3 tw:text-sm tw:text-rose-700"
      >
        {{ orderError }}
      </div>

      <div class="tw:grid tw:gap-6 tw:lg:grid-cols-12">

        <!-- ── Menu section ───────────────────────────────── -->
        <section class="tw:lg:col-span-8">
          <div class="tw:mb-4 tw:flex tw:items-center tw:justify-between">
            <h2 class="tw:text-xl tw:font-semibold tw:text-slate-900">Menu hôm nay</h2>
            <prime-button label="Tải lại" icon="pi pi-refresh" severity="secondary" text @click="fetchProducts" />
          </div>

          <!-- Loading skeleton -->
          <div v-if="isLoading" class="tw:grid tw:gap-4 tw:sm:grid-cols-2 tw:lg:grid-cols-3">
            <div v-for="skeleton in 6" :key="skeleton" class="tw:animate-pulse tw:rounded-2xl tw:border tw:border-orange-100 tw:bg-white tw:p-4">
              <div class="tw:h-32 tw:rounded-xl tw:bg-orange-100"></div>
              <div class="tw:mt-4 tw:h-4 tw:w-3/4 tw:rounded tw:bg-slate-200"></div>
              <div class="tw:mt-2 tw:h-4 tw:w-1/2 tw:rounded tw:bg-slate-200"></div>
              <div class="tw:mt-4 tw:h-10 tw:rounded tw:bg-slate-100"></div>
            </div>
          </div>

          <!-- Error -->
          <div v-else-if="loadError" class="tw:rounded-2xl tw:border tw:border-rose-200 tw:bg-rose-50 tw:p-4 tw:text-rose-700">
            {{ loadError }}
          </div>

          <!-- Menu list -->
          <div v-else class="tw:space-y-8">
            <div v-for="category in menu" :key="category.id">
              <div class="tw:mb-3 tw:flex tw:items-center tw:justify-between">
                <h3 class="tw:text-lg tw:font-semibold tw:text-slate-900">{{ category.name }}</h3>
                <span class="tw:text-xs tw:text-slate-400">{{ category.products?.length || 0 }} món</span>
              </div>
              <div class="tw:grid tw:gap-4 tw:sm:grid-cols-2 tw:lg:grid-cols-3">
                <article
                  v-for="product in category.products || []"
                  :key="product.id"
                  class="tw:group tw:flex tw:h-full tw:flex-col tw:overflow-hidden tw:rounded-2xl tw:border tw:border-orange-100 tw:bg-white tw:shadow-sm tw:transition hover:tw:-translate-y-1 hover:tw:shadow-md"
                >
                  <div class="tw:relative">
                    <img :src="product.imageUrl" :alt="product.name" class="tw:h-36 tw:w-full tw:object-cover">
                  </div>
                  <div class="tw:flex tw:flex-1 tw:flex-col tw:p-4">
                    <h4 class="tw:text-lg tw:font-semibold tw:text-slate-900">{{ product.name }}</h4>
                    <p class="tw:mt-1 tw:text-sm tw:font-semibold tw:text-orange-600">
                      {{ formatPrice(product.price) }}
                    </p>
                    <p class="tw:mt-1 tw:flex-1 tw:text-sm tw:text-slate-500">
                      {{ product.description || 'Đậm vị, giao nhanh, phục vụ nóng.' }}
                    </p>
                    <!-- Option badges -->
                    <div
                      v-if="product.hasTemperatureOption || product.hasIceLevelOption || product.hasSugarLevelOption"
                      class="tw:mt-2 tw:flex tw:flex-wrap tw:gap-1"
                    >
                      <span v-if="product.hasTemperatureOption" class="tw:rounded tw:bg-orange-50 tw:px-1.5 tw:py-0.5 tw:text-xs tw:text-orange-500">Nóng/Lạnh</span>
                      <span v-if="product.hasIceLevelOption"    class="tw:rounded tw:bg-blue-50   tw:px-1.5 tw:py-0.5 tw:text-xs tw:text-blue-500">Mức đá</span>
                      <span v-if="product.hasSugarLevelOption"  class="tw:rounded tw:bg-amber-50  tw:px-1.5 tw:py-0.5 tw:text-xs tw:text-amber-500">Mức đường</span>
                    </div>
                    <prime-button
                      label="Thêm vào giỏ"
                      icon="pi pi-shopping-cart"
                      class="tw:mt-4 tw:w-full"
                      @click="handleAddToCart(product)"
                    />
                  </div>
                </article>
                <div
                  v-if="!category.products || category.products.length === 0"
                  class="tw:col-span-full tw:rounded-2xl tw:border tw:border-dashed tw:border-orange-200 tw:bg-white tw:p-6 tw:text-center tw:text-slate-500"
                >
                  Danh mục này chưa có sản phẩm.
                </div>
              </div>
            </div>
          </div>

          <div v-if="!isLoading && !loadError && menu.length === 0" class="tw:mt-6 tw:rounded-2xl tw:border tw:border-orange-100 tw:bg-white tw:p-6 tw:text-center tw:text-slate-500">
            Menu đang trống, vui lòng thử lại sau.
          </div>
        </section>

        <!-- ── Cart aside ──────────────────────────────────── -->
        <aside class="tw:lg:col-span-4">
          <div class="tw:sticky tw:top-6 tw:rounded-2xl tw:border tw:border-orange-100 tw:bg-white tw:p-5 tw:shadow-sm">
            <div class="tw:flex tw:items-center tw:justify-between">
              <h2 class="tw:text-xl tw:font-semibold tw:text-slate-900">Giỏ hàng</h2>
              <span class="tw:rounded-full tw:bg-orange-50 tw:px-3 tw:py-1 tw:text-xs tw:font-semibold tw:text-orange-600">
                {{ cartStore.count }} món
              </span>
            </div>

            <div v-if="cartStore.count === 0" class="tw:mt-6 tw:rounded-xl tw:border tw:border-dashed tw:border-orange-200 tw:p-4 tw:text-center tw:text-slate-500">
              Chưa có món nào trong giỏ. Hãy chọn một món bên trái nhé!
            </div>

            <div v-else class="tw:mt-4 tw:space-y-4">
              <div
                v-for="(item, idx) in cartStore.items"
                :key="idx"
                class="tw:flex tw:items-start tw:justify-between tw:gap-3"
              >
                <div class="tw:min-w-0 tw:flex-1">
                  <h3 class="tw:text-sm tw:font-semibold tw:text-slate-900">{{ item.name }}</h3>
                  <p class="tw:text-xs tw:text-slate-500">{{ formatPrice(item.price) }} × {{ item.quantity }}</p>
                  <p v-if="optionsLabel(item.options)" class="tw:mt-0.5 tw:text-xs tw:text-orange-500">
                    {{ optionsLabel(item.options) }}
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

              <div class="tw:rounded-xl tw:bg-orange-50 tw:p-4">
                <div class="tw:flex tw:items-center tw:justify-between tw:text-sm tw:text-slate-600">
                  <span>Tạm tính</span>
                  <span class="tw:font-semibold tw:text-slate-900">{{ formatPrice(cartStore.total) }}</span>
                </div>
                <div class="tw:mt-2 tw:flex tw:items-center tw:justify-between tw:text-xs tw:text-slate-500">
                  <span>Phí phục vụ</span>
                  <span>Miễn phí</span>
                </div>
                <div class="tw:mt-4 tw:flex tw:items-center tw:justify-between tw:text-lg tw:font-semibold tw:text-slate-900">
                  <span>Tổng cộng</span>
                  <span>{{ formatPrice(cartStore.total) }}</span>
                </div>
              </div>

              <prime-button
                label="Đặt hàng"
                icon="pi pi-check"
                class="tw:w-full"
                :disabled="cartStore.count === 0 || !session"
                :loading="isOrdering"
                @click="submitOrder"
              />
              <p v-if="!session && cartStore.count > 0" class="tw:text-center tw:text-xs tw:text-rose-500">
                Chưa kết nối bàn — không thể đặt hàng.
              </p>
            </div>
          </div>
        </aside>
      </div>
    </div>
  </div>

  <!-- ── Options Dialog ─────────────────────────────────── -->
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
        <p class="tw:mb-2 tw:text-sm tw:font-medium tw:text-slate-700">Số lượng</p>
        <div class="tw:flex tw:items-center tw:gap-3">
          <button
            class="tw:flex tw:h-9 tw:w-9 tw:items-center tw:justify-center tw:rounded-xl tw:border tw:border-slate-200 tw:text-slate-600 tw:transition hover:tw:border-orange-300"
            @click="pendingQuantity = Math.max(1, pendingQuantity - 1)"
          >
            <iconify icon="heroicons-outline:minus" class="tw:h-4 tw:w-4" />
          </button>
          <span class="tw:min-w-8 tw:text-center tw:text-lg tw:font-semibold tw:text-slate-900">
            {{ pendingQuantity }}
          </span>
          <button
            class="tw:flex tw:h-9 tw:w-9 tw:items-center tw:justify-center tw:rounded-xl tw:border tw:border-slate-200 tw:text-slate-600 tw:transition hover:tw:border-orange-300"
            @click="pendingQuantity++"
          >
            <iconify icon="heroicons-outline:plus" class="tw:h-4 tw:w-4" />
          </button>
        </div>
      </div>

      <!-- Temperature -->
      <div v-if="selectedProduct?.hasTemperatureOption">
        <p class="tw:mb-2 tw:text-sm tw:font-medium tw:text-slate-700">Nhiệt độ</p>
        <div class="tw:flex tw:gap-2">
          <button
            v-for="opt in ['Nóng', 'Lạnh']"
            :key="opt"
            class="tw:flex-1 tw:rounded-xl tw:border tw:px-4 tw:py-2 tw:text-sm tw:font-medium tw:transition"
            :class="pendingOptions.temperature === opt
              ? 'tw:border-orange-400 tw:bg-orange-50 tw:text-orange-700'
              : 'tw:border-slate-200 tw:text-slate-600 hover:tw:border-orange-300'"
            @click="pendingOptions.temperature = opt; if (opt === 'Nóng') { pendingOptions.iceLevel = 'Không đá'; pendingOptions.sugarLevel = 'Bình thường' }"
          >{{ opt }}</button>
        </div>
      </div>

      <!-- Ice level — chỉ hiện khi chọn Lạnh -->
      <div v-if="selectedProduct?.hasIceLevelOption && pendingOptions.temperature !== 'Nóng'">
        <p class="tw:mb-2 tw:text-sm tw:font-medium tw:text-slate-700">Mức đá</p>
        <div class="tw:grid tw:grid-cols-2 tw:gap-2">
          <button
            v-for="opt in ['Không đá', 'Ít đá', 'Bình thường', 'Nhiều đá']"
            :key="opt"
            class="tw:rounded-xl tw:border tw:px-3 tw:py-2 tw:text-sm tw:font-medium tw:transition"
            :class="pendingOptions.iceLevel === opt
              ? 'tw:border-blue-400 tw:bg-blue-50 tw:text-blue-700'
              : 'tw:border-slate-200 tw:text-slate-600 hover:tw:border-blue-300'"
            @click="pendingOptions.iceLevel = opt"
          >{{ opt }}</button>
        </div>
      </div>

      <!-- Sugar level — chỉ hiện khi chọn Lạnh -->
      <div v-if="selectedProduct?.hasSugarLevelOption && pendingOptions.temperature !== 'Nóng'">
        <p class="tw:mb-2 tw:text-sm tw:font-medium tw:text-slate-700">Mức đường</p>
        <div class="tw:grid tw:grid-cols-2 tw:gap-2">
          <button
            v-for="opt in ['Không đường', 'Ít đường', 'Bình thường', 'Nhiều đường']"
            :key="opt"
            class="tw:rounded-xl tw:border tw:px-3 tw:py-2 tw:text-sm tw:font-medium tw:transition"
            :class="pendingOptions.sugarLevel === opt
              ? 'tw:border-amber-400 tw:bg-amber-50 tw:text-amber-700'
              : 'tw:border-slate-200 tw:text-slate-600 hover:tw:border-amber-300'"
            @click="pendingOptions.sugarLevel = opt"
          >{{ opt }}</button>
        </div>
      </div>
    </div>

    <template #footer>
      <prime-button label="Huỷ" severity="secondary" text @click="showOptionsDialog = false" />
      <prime-button label="Thêm vào giỏ" icon="pi pi-shopping-cart" @click="confirmAddToCart" />
    </template>
  </prime-dialog>

  <!-- ── Summary Dialog ─────────────────────────────────── -->
  <prime-dialog
    v-model:visible="showSummary"
    header="Hóa đơn bàn"
    modal
    :style="{ width: '36rem' }"
  >
    <div v-if="summary">
      <div
        v-for="order in summary.orders ?? summary.Orders ?? []"
        :key="order.orderId ?? order.OrderId"
        class="tw:mb-4 tw:rounded-xl tw:border tw:border-orange-100 tw:p-4"
      >
        <div class="tw:flex tw:items-center tw:justify-between">
          <div class="tw:flex tw:items-center tw:gap-2">
            <span class="tw:text-sm tw:font-semibold tw:text-slate-700">
              {{ order.orderNumber ?? order.OrderNumber }}
            </span>
            <span
              v-if="(order.status ?? order.Status) === 'Pending'"
              class="tw:rounded-full tw:bg-amber-100 tw:px-2 tw:py-0.5 tw:text-xs tw:font-semibold tw:text-amber-700"
            >Đang chờ</span>
          </div>
          <span class="tw:text-sm tw:font-semibold tw:text-orange-600">
            {{ formatPrice(order.totalAmount ?? order.TotalAmount) }}
          </span>
        </div>
        <ul class="tw:mt-2 tw:space-y-1.5">
          <li
            v-for="item in order.items ?? order.Items ?? []"
            :key="(item.productId ?? item.ProductId) + String(item.productName ?? item.ProductName)"
            class="tw:flex tw:items-center tw:justify-between tw:gap-2 tw:text-sm tw:text-slate-600"
            :class="{ 'tw:opacity-50 tw:pointer-events-none': itemUpdating === `${order.orderId ?? order.OrderId}-${item.productId ?? item.ProductId}` }"
          >
            <span class="tw:flex-1">{{ item.productName ?? item.ProductName }}</span>
            <!-- Edit controls — Pending orders only -->
            <div v-if="(order.status ?? order.Status) === 'Pending'" class="tw:flex tw:items-center tw:gap-1">
              <button
                class="tw:flex tw:h-6 tw:w-6 tw:items-center tw:justify-center tw:rounded tw:border tw:border-slate-200 tw:text-slate-500 tw:transition hover:tw:border-orange-300 hover:tw:text-orange-600"
                @click="decrementClientItem(order.orderId ?? order.OrderId, item.productId ?? item.ProductId, item.quantity ?? item.Quantity)"
              ><iconify icon="heroicons-outline:minus" class="tw:h-3 tw:w-3" /></button>
              <span class="tw:min-w-5 tw:text-center tw:font-semibold">{{ item.quantity ?? item.Quantity }}</span>
              <button
                class="tw:flex tw:h-6 tw:w-6 tw:items-center tw:justify-center tw:rounded tw:border tw:border-slate-200 tw:text-slate-500 tw:transition hover:tw:border-orange-300 hover:tw:text-orange-600"
                @click="setClientItemQty(order.orderId ?? order.OrderId, item.productId ?? item.ProductId, (item.quantity ?? item.Quantity) + 1)"
              ><iconify icon="heroicons-outline:plus" class="tw:h-3 tw:w-3" /></button>
              <button
                class="tw:ml-1 tw:flex tw:h-6 tw:w-6 tw:items-center tw:justify-center tw:rounded tw:border tw:border-rose-200 tw:text-rose-400 tw:transition hover:tw:border-rose-400 hover:tw:text-rose-600"
                @click="removeClientItem(order.orderId ?? order.OrderId, item.productId ?? item.ProductId)"
              ><iconify icon="heroicons-outline:trash" class="tw:h-3 tw:w-3" /></button>
            </div>
            <span v-else class="tw:font-medium">× {{ item.quantity ?? item.Quantity }}</span>
            <span class="tw:shrink-0">{{ formatPrice((item.unitPrice ?? item.UnitPrice) * (item.quantity ?? item.Quantity)) }}</span>
          </li>
        </ul>
      </div>

      <div class="tw:rounded-xl tw:bg-orange-50 tw:p-4">
        <div class="tw:flex tw:items-center tw:justify-between tw:text-lg tw:font-semibold tw:text-slate-900">
          <span>Tổng cộng</span>
          <span class="tw:text-orange-600">
            {{ formatPrice(summary.grandTotal ?? summary.GrandTotal ?? 0) }}
          </span>
        </div>
      </div>
    </div>
    <div v-else class="tw:py-8 tw:text-center tw:text-slate-500">
      Chưa có đơn hàng nào.
    </div>

    <template #footer>
      <prime-button label="Đóng" severity="secondary" @click="showSummary = false" />
    </template>
  </prime-dialog>
</template>

<style scoped>
</style>
