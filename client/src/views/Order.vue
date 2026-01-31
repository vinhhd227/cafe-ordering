<script setup>
import { computed, onMounted, ref } from 'vue';
import { getMenu } from '../services/product.service.js';

const menu = ref([]);
const cart = ref([]);
const isLoading = ref(false);
const loadError = ref('');

const total = computed(() =>
  cart.value.reduce((acc, item) => acc + item.price * item.quantity, 0)
);

const formatPrice = (value) => `${Number(value).toLocaleString()}đ`;

const addToCart = (item) => {
  const existingItem = cart.value.find((cartItem) => cartItem.id === item.id);
  if (existingItem) {
    existingItem.quantity += 1;
    return;
  }
  cart.value.push({ ...item, quantity: 1 });
};

const removeFromCart = (item) => {
  const existingItem = cart.value.find((cartItem) => cartItem.id === item.id);
  if (!existingItem) return;
  existingItem.quantity -= 1;
  if (existingItem.quantity === 0) {
    cart.value = cart.value.filter((cartItem) => cartItem.id !== item.id);
  }
};

const placeOrder = () => {
  if (!cart.value.length) return;
  alert('Đặt hàng thành công!');
  cart.value = [];
};

const fetchProducts = async () => {
  isLoading.value = true;
  loadError.value = '';
  try {
    const data = await getMenu();
    const categories =
      data?.categories ??
      data?.Categories ??
      data?.data?.categories ??
      data?.data?.Categories ??
      [];
    const normalized = Array.isArray(categories)
      ? categories.map((category) => {
          const rawProducts = category.products ?? category.Products ?? [];
          const products = Array.isArray(rawProducts)
            ? rawProducts.map((product) => ({
                id: product.id ?? product.Id,
                name: product.name ?? product.Name,
                description: product.description ?? product.Description,
                price: product.price ?? product.Price,
                imageUrl: product.imageUrl ?? product.ImageUrl,
                hasTemperatureOption: product.hasTemperatureOption ?? product.HasTemperatureOption,
                hasIceLevelOption: product.hasIceLevelOption ?? product.HasIceLevelOption,
                hasSugarLevelOption: product.hasSugarLevelOption ?? product.HasSugarLevelOption,
              }))
            : [];
          return {
            id: category.id ?? category.Id,
            name: category.name ?? category.Name,
            products,
          };
        })
      : [];
    menu.value = normalized;
  } catch (error) {
    console.error('Lỗi khi lấy menu:', error);
    loadError.value = 'Không tải được menu. Vui lòng thử lại.';
  } finally {
    isLoading.value = false;
  }
};

onMounted(fetchProducts);
</script>

<template>
  <div class="tw:min-h-screen tw:bg-gradient-to-b tw:from-amber-50 tw:via-white tw:to-orange-50">
    <div class="tw:mx-auto tw:max-w-6xl tw:px-4 tw:py-8">
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

      <div class="tw:grid tw:gap-6 tw:lg:grid-cols-12">
        <section class="tw:lg:col-span-8">
          <div class="tw:mb-4 tw:flex tw:items-center tw:justify-between">
            <h2 class="tw:text-xl tw:font-semibold tw:text-slate-900">Menu hôm nay</h2>
            <prime-button label="Tải lại" icon="pi pi-refresh" severity="secondary" text @click="fetchProducts" />
          </div>

          <div v-if="isLoading" class="tw:grid tw:gap-4 tw:sm:grid-cols-2 tw:lg:grid-cols-3">
            <div v-for="skeleton in 6" :key="skeleton" class="tw:animate-pulse tw:rounded-2xl tw:border tw:border-orange-100 tw:bg-white tw:p-4">
              <div class="tw:h-32 tw:rounded-xl tw:bg-orange-100"></div>
              <div class="tw:mt-4 tw:h-4 tw:w-3/4 tw:rounded tw:bg-slate-200"></div>
              <div class="tw:mt-2 tw:h-4 tw:w-1/2 tw:rounded tw:bg-slate-200"></div>
              <div class="tw:mt-4 tw:h-10 tw:rounded tw:bg-slate-100"></div>
            </div>
          </div>

          <div v-else-if="loadError" class="tw:rounded-2xl tw:border tw:border-rose-200 tw:bg-rose-50 tw:p-4 tw:text-rose-700">
            {{ loadError }}
          </div>

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
                    <p class="tw:mt-1 tw:text-sm tw:text-slate-500">
                      {{ product.description || 'Đậm vị, giao nhanh, phục vụ nóng.' }}
                    </p>
                    <prime-button
                      label="Thêm vào giỏ"
                      icon="pi pi-shopping-cart"
                      class="tw:mt-4 tw:w-full"
                      @click="addToCart(product)"
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

        <aside class="tw:lg:col-span-4">
          <div class="tw:sticky tw:top-6 tw:rounded-2xl tw:border tw:border-orange-100 tw:bg-white tw:p-5 tw:shadow-sm">
            <div class="tw:flex tw:items-center tw:justify-between">
              <h2 class="tw:text-xl tw:font-semibold tw:text-slate-900">Giỏ hàng</h2>
              <span class="tw:rounded-full tw:bg-orange-50 tw:px-3 tw:py-1 tw:text-xs tw:font-semibold tw:text-orange-600">
                {{ cart.length }} món
              </span>
            </div>

            <div v-if="cart.length === 0" class="tw:mt-6 tw:rounded-xl tw:border tw:border-dashed tw:border-orange-200 tw:p-4 tw:text-center tw:text-slate-500">
              Chưa có món nào trong giỏ. Hãy chọn một món bên trái nhé!
            </div>

            <div v-else class="tw:mt-4 tw:space-y-4">
              <div v-for="item in cart" :key="item.id" class="tw:flex tw:items-center tw:justify-between tw:gap-3">
                <div>
                  <h3 class="tw:text-sm tw:font-semibold tw:text-slate-900">{{ item.name }}</h3>
                  <p class="tw:text-xs tw:text-slate-500">{{ formatPrice(item.price) }} x {{ item.quantity }}</p>
                </div>
                <div class="tw:flex tw:items-center tw:gap-2">
                  <prime-button icon="pi pi-minus" class="p-button-sm p-button-rounded"  @click="removeFromCart(item)" >
                    <iconify icon="heroicons-outline:minus"></iconify>
                  </prime-button>
                  <span class="tw:min-w-6 tw:text-center tw:text-sm tw:font-semibold">{{ item.quantity }}</span>
                  <prime-button icon="pi pi-minus" class="p-button-sm p-button-rounded tw:p-0" @click="addToCart(item)">
                    <iconify icon="heroicons-outline:plus"></iconify>
                  </prime-button>
                </div>
              </div>

              <div class="tw:rounded-xl tw:bg-orange-50 tw:p-4">
                <div class="tw:flex tw:items-center tw:justify-between tw:text-sm tw:text-slate-600">
                  <span>Tạm tính</span>
                  <span class="tw:font-semibold tw:text-slate-900">{{ formatPrice(total) }}</span>
                </div>
                <div class="tw:mt-2 tw:flex tw:items-center tw:justify-between tw:text-xs tw:text-slate-500">
                  <span>Phí giao hàng</span>
                  <span>Miễn phí</span>
                </div>
                <div class="tw:mt-4 tw:flex tw:items-center tw:justify-between tw:text-lg tw:font-semibold tw:text-slate-900">
                  <span>Tổng cộng</span>
                  <span>{{ formatPrice(total) }}</span>
                </div>
              </div>

              <prime-button label="Đặt hàng" class="tw:w-full" :disabled="cart.length === 0" @click="placeOrder" />
            </div>
          </div>
        </aside>
      </div>
    </div>
  </div>
</template>

<style scoped>

</style>
