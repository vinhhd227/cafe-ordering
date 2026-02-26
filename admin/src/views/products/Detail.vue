<script setup>
import { onMounted, ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import { getProduct, updateProduct } from "@/services/product.service";
import { getCategory } from "@/services/category.service";
import { usePermission } from "@/composables/usePermission";

const route = useRoute();
const router = useRouter();
const productId = Number(route.params.id);
const { can } = usePermission();

// ── State ──────────────────────────────────────────────────────────
const product = ref(null);
const categories = ref([]);
const loading = ref(false);
const saving = ref(false);
const errorMessage = ref("");
const saveSuccess = ref(false);

// Edit form
const form = ref({
  categoryId: null,
  name: "",
  price: null,
  description: "",
  imageUrl: "",
  isActive: true,
  hasTemperatureOption: false,
  hasIceLevelOption: false,
  hasSugarLevelOption: false,
});

// ── Helpers ────────────────────────────────────────────────────────
const formatVnd = (v) =>
  new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
    maximumFractionDigits: 0,
  }).format(v ?? 0);

const formatDate = (d) => (d ? new Date(d).toLocaleString("vi-VN") : "—");

const extractError = (err) =>
  err?.response?.data?.errors?.map((e) => e.errorMessage ?? e).join("; ") ||
  err?.response?.data?.message ||
  "Failed to update product.";

// ── Data ───────────────────────────────────────────────────────────
const loadCategories = async () => {
  try {
    const res = await getCategory();
    const raw = res?.data;
    categories.value = Array.isArray(raw)
      ? raw
      : Array.isArray(raw?.value)
        ? raw.value
        : Array.isArray(raw?.items)
          ? raw.items
          : [];
  } catch {
    /* non-critical */
  }
};

const loadProduct = async () => {
  loading.value = true;
  errorMessage.value = "";
  try {
    const res = await getProduct(productId);
    // SendResultAsync gửi result.Value trực tiếp → res.data = ProductDto
    product.value = res?.data;
    form.value = {
      categoryId: product.value.categoryId,
      name: product.value.name,
      price: product.value.price,
      description: product.value.description ?? "",
      imageUrl: product.value.imageUrl ?? "",
      isActive: product.value.isActive,
      hasTemperatureOption: product.value.hasTemperatureOption ?? false,
      hasIceLevelOption: product.value.hasIceLevelOption ?? false,
      hasSugarLevelOption: product.value.hasSugarLevelOption ?? false,
    };
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.message || "Failed to load product.";
  } finally {
    loading.value = false;
  }
};

const save = async () => {
  saving.value = true;
  errorMessage.value = "";
  saveSuccess.value = false;
  try {
    await updateProduct(productId, {
      categoryId: form.value.categoryId,
      name: form.value.name.trim(),
      price: Number(form.value.price),
      description: form.value.description.trim() || null,
      imageUrl: form.value.imageUrl.trim() || null,
      isActive: form.value.isActive,
      hasTemperatureOption: form.value.hasTemperatureOption,
      hasIceLevelOption: form.value.hasIceLevelOption,
      hasSugarLevelOption: form.value.hasSugarLevelOption,
    });
    // Reload để lấy updatedAt mới nhất
    await loadProduct();
    saveSuccess.value = true;
    setTimeout(() => (saveSuccess.value = false), 3000);
  } catch (err) {
    errorMessage.value = extractError(err);
  } finally {
    saving.value = false;
  }
};

onMounted(() => {
  loadCategories();
  loadProduct();
});
</script>

<template>
  <section class="tw:space-y-6">
    <!-- ── Header ───────────────────────────────────────────────── -->
    <div class="tw:flex tw:flex-wrap tw:items-end tw:justify-between tw:gap-4">
      <div>
        <p
          class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300"
        >
          Products
        </p>
        <h1
          class="tw:mt-2 tw:text-3xl tw:font-semibold tw:flex tw:items-center tw:gap-3"
        >
          <span v-if="product">{{ product.name }}</span>
          <prime-skeleton v-else width="16rem" height="2rem" />
          <prime-tag
            v-if="product"
            :value="product.isActive ? 'Active' : 'Inactive'"
            :severity="product.isActive ? 'success' : 'danger'"
          />
        </h1>
        <p class="tw:mt-2 tw:text-sm app-text-muted">
          Product #{{ productId }}
          <template v-if="product">
            · Created {{ formatDate(product.createdAt) }}
            <template v-if="product.updatedAt">
              · Updated {{ formatDate(product.updatedAt) }}
            </template>
          </template>
        </p>
      </div>
      <prime-button
        severity="secondary"
        outlined
        size="small"
        @click="router.push({ name: 'products' })"
      >
        <iconify icon="ph:arrow-left-bold" />
        <span>Back to list</span>
      </prime-button>
    </div>

    <!-- ── Loading skeleton ──────────────────────────────────────── -->
    <div
      v-if="loading"
      class="tw:grid tw:grid-cols-1 tw:gap-6 tw:lg:grid-cols-3"
    >
      <prime-card class="app-card tw:rounded-2xl tw:border tw:lg:col-span-1">
        <template #content>
          <prime-skeleton height="18rem" class="tw:rounded-xl" />
          <prime-skeleton width="60%" height="1.25rem" class="tw:mt-4" />
          <prime-skeleton width="40%" height="1rem" class="tw:mt-2" />
        </template>
      </prime-card>
      <prime-card class="app-card tw:rounded-2xl tw:border tw:lg:col-span-2">
        <template #content>
          <prime-skeleton
            v-for="i in 4"
            :key="i"
            height="2.5rem"
            class="tw:mb-4"
          />
        </template>
      </prime-card>
    </div>

    <template v-else-if="product">
      <!-- ── Error ──────────────────────────────────────────────── -->
      <prime-alert
        v-if="errorMessage"
        severity="error"
        variant="accent"
        closable
        @close="errorMessage = ''"
        >{{ errorMessage }}</prime-alert
      >
      <prime-alert
        v-if="saveSuccess"
        severity="success"
        variant="accent"
        closable  
        @close="errorMessage = ''"
        >Product updated successfully.</prime-alert
      >
      <div class="tw:grid tw:grid-cols-1 tw:gap-6 tw:lg:grid-cols-3">
        <!-- ── Left: readonly info ─────────────────────────────── -->
        <prime-card class="app-card tw:rounded-2xl tw:border tw:lg:col-span-1">
          <template #content>
            <!-- Image -->
            <div class="tw:flex tw:justify-center">
              <img
                v-if="product.imageUrl"
                :src="product.imageUrl"
                :alt="product.name"
                class="tw:h-100 tw:w-full tw:rounded-xl tw:object-cover"
              />
              <div
                v-else
                class="tw:h-60 tw:w-full tw:rounded-xl tw:bg-white/5 tw:flex tw:items-center tw:justify-center tw:border"
              >
                <iconify
                  icon="ph:image-bold"
                  class="tw:text-4xl app-text-muted"
                />
              </div>
            </div>

            <!-- Info rows -->
            <div class="tw:mt-5 tw:space-y-3">
              <div class="tw:flex tw:justify-between tw:text-sm">
                <span class="app-text-muted">Category</span>
                <span class="tw:font-medium">
                  {{
                    categories.find((c) => c.id === form.categoryId)?.name ||
                    product.categoryName ||
                    "—"
                  }}
                </span>
              </div>
              <div class="tw:flex tw:justify-between tw:text-sm">
                <span class="app-text-muted">Price</span>
                <span class="tw:font-semibold tw:text-emerald-300">{{
                  formatVnd(product.price)
                }}</span>
              </div>
              <div class="tw:flex tw:justify-between tw:text-sm">
                <span class="app-text-muted">Status</span>
                <prime-tag
                  :value="product.isActive ? 'Active' : 'Inactive'"
                  :severity="product.isActive ? 'success' : 'danger'"
                />
              </div>
            </div>

            <!-- Options -->
            <div class="tw:mt-5">
              <p
                class="tw:text-xs tw:uppercase tw:tracking-widest app-text-subtle tw:mb-3"
              >
                Customisation options
              </p>
              <div
                v-if="
                  product.hasTemperatureOption ||
                  product.hasIceLevelOption ||
                  product.hasSugarLevelOption
                "
                class="tw:flex tw:flex-wrap tw:gap-2"
              >
                <prime-tag
                  v-if="product.hasTemperatureOption"
                  value="Temperature"
                  severity="info"
                />
                <prime-tag
                  v-if="product.hasIceLevelOption"
                  value="Ice level"
                  severity="info"
                />
                <prime-tag
                  v-if="product.hasSugarLevelOption"
                  value="Sugar level"
                  severity="info"
                />
              </div>
              <p v-else class="tw:text-xs app-text-muted">None</p>
            </div>

            <!-- Description -->
            <div v-if="product.description" class="tw:mt-5">
              <p
                class="tw:text-xs tw:uppercase tw:tracking-widest app-text-subtle tw:mb-1"
              >
                Description
              </p>
              <p class="tw:text-sm app-text-muted tw:leading-relaxed">
                {{ product.description }}
              </p>
            </div>
          </template>
        </prime-card>

        <!-- ── Right: edit form ────────────────────────────────── -->
        <prime-card class="app-card tw:rounded-2xl tw:border tw:lg:col-span-2">
          <template #content>
            <p class="tw:text-sm tw:font-semibold tw:mb-5">Edit details</p>

            <div class="tw:space-y-5">
              <!-- Category -->
              <div class="tw:space-y-1.5">
                <label class="tw:text-sm tw:font-medium">
                  Category <span class="tw:text-red-400">*</span>
                </label>
                <prime-select
                  v-model="form.categoryId"
                  :options="categories"
                  option-label="name"
                  option-value="id"
                  placeholder="Select a category"
                  class="app-input tw:w-full"
                />
              </div>

              <!-- Name -->
              <div class="tw:space-y-1.5">
                <label class="tw:text-sm tw:font-medium">
                  Name <span class="tw:text-red-400">*</span>
                </label>
                <prime-input-text
                  v-model="form.name"
                  class="app-input tw:w-full"
                />
              </div>

              <!-- Price -->
              <div class="tw:space-y-1.5">
                <label class="tw:text-sm tw:font-medium">
                  Price (VND) <span class="tw:text-red-400">*</span>
                </label>
                <prime-input-number
                  v-model="form.price"
                  :min="0"
                  :use-grouping="true"
                  class="app-input tw:w-full"
                />
              </div>

              <!-- Description -->
              <div class="tw:space-y-1.5">
                <label class="tw:text-sm tw:font-medium">
                  Description
                  <span class="app-text-muted tw:font-normal">(optional)</span>
                </label>
                <prime-textarea
                  v-model="form.description"
                  rows="3"
                  class="app-input tw:w-full tw:resize-none"
                  auto-resize
                />
              </div>

              <!-- Image URL -->
              <div class="tw:space-y-1.5">
                <label class="tw:text-sm tw:font-medium">
                  Image URL
                  <span class="app-text-muted tw:font-normal">(optional)</span>
                </label>
                <prime-input-text
                  v-model="form.imageUrl"
                  placeholder="https://…"
                  class="app-input tw:w-full"
                />
                <img
                  v-if="form.imageUrl"
                  :src="form.imageUrl"
                  alt="preview"
                  class="tw:mt-2 tw:h-20 tw:w-20 tw:rounded-lg tw:object-cover tw:border"
                />
              </div>
            </div>

            <prime-divider class="tw:my-5" />
            <!-- Customisation options -->
            <div class="tw:space-y-4">
              <p class="tw:text-sm tw:font-semibold">Customisation options</p>

              <!-- Temperature -->
              <div class="tw:flex tw:items-center tw:justify-between">
                <div>
                  <p class="tw:text-sm tw:font-medium">Temperature option</p>
                  <p class="tw:text-xs app-text-muted">
                    Cho phép chọn nóng / lạnh
                  </p>
                </div>
                <prime-toggle-switch v-model="form.hasTemperatureOption" />
              </div>

              <!-- Ice level -->
              <div class="tw:flex tw:items-center tw:justify-between">
                <div>
                  <p class="tw:text-sm tw:font-medium">Ice level option</p>
                  <p class="tw:text-xs app-text-muted">
                    Cho phép chọn lượng đá
                  </p>
                </div>
                <prime-toggle-switch v-model="form.hasIceLevelOption" />
              </div>

              <!-- Sugar level -->
              <div class="tw:flex tw:items-center tw:justify-between">
                <div>
                  <p class="tw:text-sm tw:font-medium">Sugar level option</p>
                  <p class="tw:text-xs app-text-muted">
                    Cho phép chọn lượng đường
                  </p>
                </div>
                <prime-toggle-switch v-model="form.hasSugarLevelOption" />
              </div>
            </div>
            <prime-divider class="tw:my-5" />
            <!-- Active status -->
            <div>
              <div class="tw:flex tw:items-center tw:justify-between">
                <div>
                  <p class="tw:text-sm tw:font-semibold">Active</p>
                  <p class="tw:text-xs app-text-muted">
                    Hiện sản phẩm trên menu
                  </p>
                </div>
                <prime-toggle-switch v-model="form.isActive" />
              </div>
            </div>
            <prime-divider class="tw:my-5" />
            <!-- Actions -->
            <div
              v-if="can('product.update')"
              class="tw:flex tw:justify-end tw:gap-3"
            >
              <prime-button
                label="Reset"
                severity="secondary"
                outlined
                size="small"
                @click="loadProduct"
              />
              <prime-button
                severity="success"
                size="small"
                :loading="saving"
                @click="save"
              >
                <iconify icon="ph:check-bold" class="tw:-ml-1" />
                <span>Save changes</span>
              </prime-button>
            </div>
          </template>
        </prime-card>
      </div>
    </template>

    <!-- ── Not found ──────────────────────────────────────────────── -->
    <prime-card v-else class="app-card tw:rounded-2xl tw:border">
      <template #content>
        <div
          class="tw:flex tw:flex-col tw:items-center tw:py-10 app-text-muted"
        >
          <iconify icon="ph:warning-bold" class="tw:text-3xl tw:mb-2" />
          <p class="tw:text-sm">Product not found.</p>
        </div>
      </template>
    </prime-card>
  </section>
</template>
