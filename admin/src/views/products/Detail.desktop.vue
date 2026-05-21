<script setup>
import { computed, onMounted, ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import { getProduct, replaceProductVariants, updateProduct } from "@/services/product.service";
import { getCategory } from "@/services/category.service";
import { uploadImage } from "@/services/upload.service";
import { usePermission } from "@/composables/usePermission";
import { getProductOptionGroups } from "@/services/product-option-group.service";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const productId = Number(route.params.id);
const { can } = usePermission();

// â”€â”€ State â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
const product = ref(null);
const categories = ref([]);
const allOptionGroups = ref([]);
const loading = ref(false);
const saving = ref(false);
const savingGroups = ref(false);
const errorMessage = ref("");
const saveSuccess = ref(false);
const groupSaveSuccess = ref(false);
const variants = ref([]);
const savingVariants = ref(false);
const variantSaveSuccess = ref(false);

// Option groups assignment
const selectedGroupIds = ref([]);

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
  isAccompaniment: false,
  estimatedPrepMinutes: null,
});

const fileInput = ref(null);
const uploading = ref(false);

const handleFileSelect = async (event) => {
  const file = event.target.files?.[0];
  if (!file) return;
  uploading.value = true;
  try {
    const res = await uploadImage(file);
    form.value.imageUrl = res?.data?.url ?? "";
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.errors?.map((e) => e.errorMessage ?? e).join("; ") ||
      t('products.form.uploadError');
  } finally {
    uploading.value = false;
    event.target.value = "";
  }
};

// â”€â”€ Helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
const formatVnd = (v) =>
  new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
    maximumFractionDigits: 0,
  }).format(v ?? 0);

const formatDate = (d) => (d ? new Date(d).toLocaleString("vi-VN") : "â€”");

const extractError = (err) =>
  err?.response?.data?.errors?.map((e) => e.errorMessage ?? e).join("; ") ||
  err?.response?.data?.message ||
  t('products.detail.error.updateFailed');

const activeVariantGroups = computed(() =>
  (product.value?.variantGroups ?? []).filter((group) => (group.values ?? []).length > 0)
);

const variantValueLookup = computed(() => {
  const lookup = new Map();
  activeVariantGroups.value.forEach((group) => {
    (group.values ?? []).forEach((value) => {
      lookup.set(value.id, { groupName: group.name, label: value.label });
    });
  });
  return lookup;
});

const variantKey = (ids) => [...ids].sort((a, b) => a - b).join(",");

const buildVariantCombinations = (groups) =>
  groups.reduce(
    (sets, group) => sets.flatMap((set) => (group.values ?? []).map((value) => [...set, value])),
    [[]]
  );

const normalizeVariant = (variant, displayOrder) => ({
  id: variant.id ?? 0,
  valueIds: variant.valueIds ?? [],
  price: Number(variant.price ?? product.value?.price ?? 0),
  costPrice: variant.costPrice ?? null,
  sku: variant.sku ?? "",
  barcode: variant.barcode ?? "",
  isActive: variant.isActive ?? true,
  displayOrder: variant.displayOrder ?? displayOrder,
});

const rebuildVariantMatrix = () => {
  const combinations = buildVariantCombinations(activeVariantGroups.value);
  const existing = new Map(variants.value.map((variant) => [variantKey(variant.valueIds), variant]));
  variants.value = combinations.map((values, index) => {
    const valueIds = values.map((value) => value.id);
    return existing.get(variantKey(valueIds)) ?? normalizeVariant({
      valueIds,
      price: product.value?.price ?? form.value.price ?? 0,
      isActive: true,
    }, index + 1);
  });
};

const copyBasePriceToVariants = () => {
  variants.value = variants.value.map((variant) => ({
    ...variant,
    price: Number(form.value.price ?? product.value?.price ?? 0),
  }));
};

const saveVariants = async () => {
  savingVariants.value = true;
  errorMessage.value = "";
  variantSaveSuccess.value = false;
  try {
    const res = await replaceProductVariants(productId, {
      variants: variants.value.map((variant) => ({
        valueIds: variant.valueIds,
        price: Number(variant.price) || 0,
        costPrice: variant.costPrice === "" || variant.costPrice == null ? null : Number(variant.costPrice),
        sku: variant.sku?.trim() || null,
        barcode: variant.barcode?.trim() || null,
        isActive: variant.isActive ?? true,
      })),
    });
    const raw = res?.data;
    const saved = Array.isArray(raw) ? raw : Array.isArray(raw?.value) ? raw.value : [];
    variants.value = saved.map(normalizeVariant);
    variantSaveSuccess.value = true;
    setTimeout(() => (variantSaveSuccess.value = false), 3000);
  } catch (err) {
    errorMessage.value = extractError(err);
  } finally {
    savingVariants.value = false;
  }
};

// â”€â”€ Data â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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

const loadOptionGroups = async () => {
  try {
    const res = await getProductOptionGroups();
    const raw = res?.data;
    allOptionGroups.value = Array.isArray(raw)
      ? raw
      : Array.isArray(raw?.value)
        ? raw.value
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
    product.value = res?.data;
    variants.value = (product.value.variants ?? []).map(normalizeVariant);
    selectedGroupIds.value = product.value.assignedOptionGroupIds ?? [];
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
      isAccompaniment: product.value.isAccompaniment ?? false,
      estimatedPrepMinutes: product.value.estimatedPrepMinutes ?? null,
    };
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.message || t('products.detail.error.loadFailed');
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
      isAccompaniment: form.value.isAccompaniment,
      estimatedPrepMinutes: form.value.estimatedPrepMinutes || null,
      assignedOptionGroupIds: selectedGroupIds.value,
    });
    await loadProduct();
    saveSuccess.value = true;
    setTimeout(() => (saveSuccess.value = false), 3000);
  } catch (err) {
    errorMessage.value = extractError(err);
  } finally {
    saving.value = false;
  }
};

const saveOptionGroups = async () => {
  savingGroups.value = true;
  errorMessage.value = "";
  groupSaveSuccess.value = false;
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
      isAccompaniment: form.value.isAccompaniment,
      estimatedPrepMinutes: form.value.estimatedPrepMinutes || null,
      assignedOptionGroupIds: selectedGroupIds.value,
    });
    await loadProduct();
    groupSaveSuccess.value = true;
    setTimeout(() => (groupSaveSuccess.value = false), 3000);
  } catch (err) {
    errorMessage.value = extractError(err);
  } finally {
    savingGroups.value = false;
  }
};

onMounted(() => {
  loadCategories();
  loadOptionGroups();
  loadProduct();
});
</script>

<template>
  <section class="tw:space-y-6">
    <!-- â”€â”€ Header â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ -->
    <div class="tw:flex tw:flex-wrap tw:items-end tw:justify-between tw:gap-4">
      <div>
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-primary-300">
          {{ t('products.breadcrumb') }}
        </p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold tw:flex tw:items-center tw:gap-3">
          <span v-if="product">{{ product.name }}</span>
          <prime-skeleton v-else width="16rem" height="2rem" />
          <prime-tag
            v-if="product"
            :value="product.isActive ? t('products.status.active') : t('products.status.inactive')"
            :severity="product.isActive ? 'success' : 'danger'"
          />
        </h1>
        <p class="tw:mt-2 tw:text-sm tw:text-muted">
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
        <span>{{ t('products.detail.backToList') }}</span>
      </prime-button>
    </div>

    <!-- â”€â”€ Loading skeleton â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ -->
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
      <!-- â”€â”€ Error â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ -->
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
        @close="saveSuccess = false"
        >{{ t('products.detail.updateSuccess') }}</prime-alert
      >
      <div class="tw:grid tw:grid-cols-1 tw:gap-6 tw:lg:grid-cols-3">
        <!-- â”€â”€ Left: readonly info â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ -->
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
                  class="tw:text-4xl tw:text-muted"
                />
              </div>
            </div>

            <!-- Info rows -->
            <div class="tw:mt-5 tw:space-y-3">
              <div class="tw:flex tw:justify-between tw:text-sm">
                <span class="tw:text-muted">{{ t('products.detail.info.category') }}</span>
                <span class="tw:font-medium">
                  {{
                    categories.find((c) => c.id === form.categoryId)?.name ||
                    product.categoryName ||
                    "â€”"
                  }}
                </span>
              </div>
              <div class="tw:flex tw:justify-between tw:text-sm">
                <span class="tw:text-muted">{{ t('products.detail.info.price') }}</span>
                <span class="tw:font-semibold tw:text-primary-300">{{
                  formatVnd(product.price)
                }}</span>
              </div>
              <div class="tw:flex tw:justify-between tw:text-sm">
                <span class="tw:text-muted">{{ t('products.detail.info.status') }}</span>
                <prime-tag
                  :value="product.isActive ? t('products.status.active') : t('products.status.inactive')"
                  :severity="product.isActive ? 'success' : 'danger'"
                />
              </div>
              <div v-if="product.estimatedPrepMinutes" class="tw:flex tw:justify-between tw:text-sm">
                <span class="tw:text-muted">{{ t('products.detail.info.estimatedPrepMinutes') }}</span>
                <span>{{ product.estimatedPrepMinutes }} {{ t('products.detail.info.minutes') }}</span>
              </div>
            </div>

            <!-- Options -->
            <div class="tw:mt-5">
              <p class="tw:text-xs tw:uppercase tw:tracking-widest app-text-subtle tw:mb-3">
                {{ t('products.detail.info.options') }}
              </p>
              <div
                v-if="
                  product.hasTemperatureOption ||
                  product.hasIceLevelOption ||
                  product.hasSugarLevelOption ||
                  product.isAccompaniment
                "
                class="tw:flex tw:flex-wrap tw:gap-2"
              >
                <prime-tag
                  v-if="product.hasTemperatureOption"
                  :value="t('products.options.temperature.label')"
                  severity="info"
                />
                <prime-tag
                  v-if="product.hasIceLevelOption"
                  :value="t('products.options.iceLevel.label')"
                  severity="info"
                />
                <prime-tag
                  v-if="product.hasSugarLevelOption"
                  :value="t('products.options.sugarLevel.label')"
                  severity="info"
                />
                <prime-tag
                  v-if="product.isAccompaniment"
                  :value="t('products.options.accompaniment.label')"
                  severity="warn"
                />
              </div>
              <p v-else class="tw:text-xs tw:text-muted">{{ t('products.detail.info.noOptions') }}</p>
            </div>

            <!-- Description -->
            <div v-if="product.description" class="tw:mt-5">
              <p class="tw:text-xs tw:uppercase tw:tracking-widest app-text-subtle tw:mb-1">
                {{ t('products.detail.info.description') }}
              </p>
              <p class="tw:text-sm tw:text-muted tw:leading-relaxed">
                {{ product.description }}
              </p>
            </div>
          </template>
        </prime-card>

        <!-- â”€â”€ Right: edit form â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ -->
        <prime-card class="app-card tw:rounded-2xl tw:border tw:lg:col-span-2">
          <template #content>
            <p class="tw:text-sm tw:font-semibold tw:mb-5">{{ t('products.detail.editTitle') }}</p>

            <div class="tw:space-y-5">
              <!-- Category -->
              <div class="tw:space-y-1.5">
                <label class="tw:text-sm tw:font-medium">
                  {{ t('products.form.category') }} <span class="tw:text-red-400">*</span>
                </label>
                <prime-select
                  v-model="form.categoryId"
                  :options="categories"
                  option-label="name"
                  option-value="id"
                  :placeholder="t('products.form.selectCategory')"
                  class="app-input tw:w-full"
                />
              </div>

              <!-- Name -->
              <div class="tw:space-y-1.5">
                <label class="tw:text-sm tw:font-medium">
                  {{ t('products.form.name') }} <span class="tw:text-red-400">*</span>
                </label>
                <prime-input-text
                  v-model="form.name"
                  class="app-input tw:w-full"
                />
              </div>

              <!-- Price -->
              <div class="tw:space-y-1.5">
                <label class="tw:text-sm tw:font-medium">
                  {{ t('products.form.price') }} <span class="tw:text-red-400">*</span>
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
                  {{ t('products.form.description') }}
                  <span class="tw:text-muted tw:font-normal">{{ t('products.form.optional') }}</span>
                </label>
                <prime-textarea
                  v-model="form.description"
                  rows="3"
                  class="app-input tw:w-full tw:resize-none"
                  auto-resize
                />
              </div>

              <!-- Estimated prep time -->
              <div class="tw:space-y-1.5">
                <label class="tw:text-sm tw:font-medium">
                  {{ t('products.form.estimatedPrepMinutes') }}
                  <span class="tw:text-muted tw:font-normal">{{ t('products.form.optional') }}</span>
                </label>
                <prime-input-number
                  v-model="form.estimatedPrepMinutes"
                  :min="1"
                  :max="120"
                  :placeholder="t('products.form.estimatedPrepMinutesPlaceholder')"
                  class="app-input tw:w-full"
                />
              </div>

              <!-- Image -->
              <div class="tw:space-y-1.5">
                <label class="tw:text-sm tw:font-medium">
                  {{ t('products.form.image') }}
                  <span class="tw:text-muted tw:font-normal">{{ t('products.form.optional') }}</span>
                </label>
                <div class="tw:flex tw:gap-2">
                  <input
                    ref="fileInput"
                    type="file"
                    accept="image/*"
                    class="tw:hidden"
                    @change="handleFileSelect"
                  />
                  <prime-button
                    severity="secondary"
                    outlined
                    size="small"
                    :loading="uploading"
                    v-tooltip.top="t('products.form.uploadImage')"
                    @click="fileInput.click()"
                  >
                    <iconify icon="ph:upload-simple-bold" />
                    <span>{{ t('products.form.uploadImage') }}</span>
                  </prime-button>
                  <prime-input-text
                    v-model="form.imageUrl"
                    :placeholder="t('products.form.pasteUrl')"
                    class="app-input tw:flex-1"
                  />
                </div>
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
              <p class="tw:text-sm tw:font-semibold">{{ t('products.options.title') }}</p>

              <!-- Temperature -->
              <div class="tw:flex tw:items-center tw:justify-between">
                <div>
                  <p class="tw:text-sm tw:font-medium">{{ t('products.options.temperature.label') }}</p>
                  <p class="tw:text-xs tw:text-muted">{{ t('products.options.temperature.hint') }}</p>
                </div>
                <prime-toggle-switch v-model="form.hasTemperatureOption" />
              </div>

              <!-- Ice level -->
              <div class="tw:flex tw:items-center tw:justify-between">
                <div>
                  <p class="tw:text-sm tw:font-medium">{{ t('products.options.iceLevel.label') }}</p>
                  <p class="tw:text-xs tw:text-muted">{{ t('products.options.iceLevel.hint') }}</p>
                </div>
                <prime-toggle-switch v-model="form.hasIceLevelOption" />
              </div>

              <!-- Sugar level -->
              <div class="tw:flex tw:items-center tw:justify-between">
                <div>
                  <p class="tw:text-sm tw:font-medium">{{ t('products.options.sugarLevel.label') }}</p>
                  <p class="tw:text-xs tw:text-muted">{{ t('products.options.sugarLevel.hint') }}</p>
                </div>
                <prime-toggle-switch v-model="form.hasSugarLevelOption" />
              </div>

              <!-- Accompaniment -->
              <div class="tw:flex tw:items-center tw:justify-between">
                <div>
                  <p class="tw:text-sm tw:font-medium">{{ t('products.options.accompaniment.label') }}</p>
                  <p class="tw:text-xs tw:text-muted">{{ t('products.options.accompaniment.hint') }}</p>
                </div>
                <prime-toggle-switch v-model="form.isAccompaniment" />
              </div>
            </div>
            <prime-divider class="tw:my-5" />
            <!-- Active status -->
            <div>
              <div class="tw:flex tw:items-center tw:justify-between">
                <div>
                  <p class="tw:text-sm tw:font-semibold">{{ t('products.detail.activeOption.label') }}</p>
                  <p class="tw:text-xs tw:text-muted">{{ t('products.detail.activeOption.hint') }}</p>
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
                :label="t('products.detail.reset')"
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
                <span>{{ t('products.detail.saveChanges') }}</span>
              </prime-button>
            </div>
          </template>
        </prime-card>
      </div>
      <!-- â”€â”€ Option Groups Assignment â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ -->
      <prime-card class="app-card tw:rounded-2xl tw:border">
        <template #content>
          <div class="tw:flex tw:flex-wrap tw:items-center tw:justify-between tw:gap-3 tw:mb-5">
            <div>
              <p class="tw:text-sm tw:font-semibold">Bảng giá biến thể</p>
              <p class="tw:text-xs tw:text-muted">
                Giá ở đây là giá bán cuối cùng cho từng tổ hợp, không cộng dồn từ từng giá trị.
              </p>
            </div>
            <div v-if="can('product.update')" class="tw:flex tw:flex-wrap tw:gap-2">
              <prime-button severity="secondary" outlined size="small" :disabled="activeVariantGroups.length === 0" @click="rebuildVariantMatrix">
                <iconify icon="ph:grid-four-bold" />
                <span>Sinh tổ hợp</span>
              </prime-button>
              <prime-button severity="secondary" outlined size="small" :disabled="variants.length === 0" @click="copyBasePriceToVariants">
                <iconify icon="ph:copy-bold" />
                <span>Copy giá gốc</span>
              </prime-button>
              <prime-button severity="success" size="small" :disabled="variants.length === 0" :loading="savingVariants" @click="saveVariants">
                <iconify icon="ph:check-bold" class="tw:-ml-1" />
                <span>Lưu bảng giá</span>
              </prime-button>
            </div>
          </div>

          <prime-message
            v-if="variantSaveSuccess"
            severity="success"
            size="small"
            variant="simple"
            :closable="false"
            class="tw:mb-4"
          >Đã lưu bảng giá biến thể.</prime-message>

          <div v-if="activeVariantGroups.length === 0" class="tw:py-8 tw:text-center tw:text-muted tw:text-sm">
            <iconify icon="ph:git-branch-bold" class="tw:text-3xl tw:mb-2 tw:opacity-30" />
            <p>Thêm nhóm biến thể trước, sau đó sinh tổ hợp giá.</p>
          </div>

          <div v-else-if="variants.length === 0" class="tw:py-8 tw:text-center tw:text-muted tw:text-sm">
            <iconify icon="ph:grid-four-bold" class="tw:text-3xl tw:mb-2 tw:opacity-30" />
            <p>Chưa có bảng giá. Bấm "Sinh tổ hợp" để tạo {{ buildVariantCombinations(activeVariantGroups).length }} dòng giá.</p>
          </div>

          <div v-else class="tw:overflow-x-auto">
            <table class="tw:min-w-full tw:text-sm">
              <thead>
                <tr class="tw:border-b tw:border-slate-200 tw:dark:border-white/10">
                  <th
                    v-for="group in activeVariantGroups"
                    :key="group.id"
                    class="tw:px-3 tw:py-2 tw:text-left tw:font-medium tw:text-muted"
                  >
                    {{ group.name }}
                  </th>
                  <th class="tw:px-3 tw:py-2 tw:text-left tw:font-medium tw:text-muted tw:w-44">Giá bán</th>
                  <th class="tw:px-3 tw:py-2 tw:text-left tw:font-medium tw:text-muted tw:w-44">Giá vốn</th>
                  <th class="tw:px-3 tw:py-2 tw:text-left tw:font-medium tw:text-muted tw:w-28">Đang bán</th>
                </tr>
              </thead>
              <tbody>
                <tr
                  v-for="variant in variants"
                  :key="variantKey(variant.valueIds)"
                  class="tw:border-b tw:border-slate-100 tw:dark:border-white/5"
                >
                  <td
                    v-for="group in activeVariantGroups"
                    :key="`${variantKey(variant.valueIds)}-${group.id}`"
                    class="tw:px-3 tw:py-3"
                  >
                    <span class="tw:inline-flex tw:rounded-lg tw:border tw:px-2.5 tw:py-1 tw:text-xs">
                      {{
                        variantValueLookup.get(
                          variant.valueIds.find((id) => (group.values ?? []).some((value) => value.id === id))
                        )?.label ?? "-"
                      }}
                    </span>
                  </td>
                  <td class="tw:px-3 tw:py-3">
                    <prime-input-number v-model="variant.price" :min="0" :use-grouping="true" class="app-input tw:w-full" />
                  </td>
                  <td class="tw:px-3 tw:py-3">
                    <prime-input-number v-model="variant.costPrice" :min="0" :use-grouping="true" class="app-input tw:w-full" />
                  </td>
                  <td class="tw:px-3 tw:py-3">
                    <prime-toggle-switch v-model="variant.isActive" />
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </template>
      </prime-card>

      <prime-card class="app-card tw:rounded-2xl tw:border">
        <template #content>
          <div class="tw:flex tw:items-center tw:justify-between tw:mb-5">
            <div>
              <p class="tw:text-sm tw:font-semibold">{{ t('products.detail.optionGroups.title') }}</p>
              <p class="tw:text-xs tw:text-muted">{{ t('products.detail.optionGroups.hint') }}</p>
            </div>
            <prime-button
              v-if="can('product.update')"
              severity="success"
              size="small"
              :loading="savingGroups"
              @click="saveOptionGroups"
            >
              <iconify icon="ph:check-bold" class="tw:-ml-1" />
              <span>{{ t('products.detail.optionGroups.save') }}</span>
            </prime-button>
          </div>

          <prime-message
            v-if="groupSaveSuccess"
            severity="success"
            size="small"
            variant="simple"
            :closable="false"
            class="tw:mb-4"
          >{{ t('products.detail.optionGroups.saveSuccess') }}</prime-message>

          <div v-if="allOptionGroups.length === 0" class="tw:py-6 tw:text-center tw:text-muted tw:text-sm">
            <iconify icon="ph:stack-bold" class="tw:text-3xl tw:mb-2 tw:opacity-30" />
            <p>{{ t('products.detail.optionGroups.noGroups') }}</p>
            <prime-button
              severity="secondary"
              text
              size="small"
              class="tw:mt-2"
              @click="$router.push({ name: 'productOptionGroupsCreate' })"
            >
              {{ t('products.detail.optionGroups.createFirst') }}
            </prime-button>
          </div>

          <div v-else class="tw:flex tw:flex-wrap tw:gap-3">
            <div
              v-for="group in allOptionGroups"
              :key="group.id"
              class="tw:flex tw:items-center tw:gap-2 tw:rounded-xl tw:border tw:px-3 tw:py-2 tw:cursor-pointer tw:transition-colors"
              :class="selectedGroupIds.includes(group.id)
                ? 'tw:border-primary-400 tw:bg-primary-500/10'
                : 'tw:border-slate-200 tw:dark:border-white/10 tw:hover:border-slate-400 tw:dark:hover:border-white/30'"
              @click="selectedGroupIds.includes(group.id)
                ? selectedGroupIds.splice(selectedGroupIds.indexOf(group.id), 1)
                : selectedGroupIds.push(group.id)"
            >
              <iconify
                :icon="selectedGroupIds.includes(group.id) ? 'ph:check-circle-bold' : 'ph:circle'"
                class="tw:text-lg"
                :class="selectedGroupIds.includes(group.id) ? 'tw:text-primary-400' : 'tw:text-muted'"
              />
              <div>
                <p class="tw:text-sm tw:font-medium tw:leading-none">{{ group.name }}</p>
                <p class="tw:text-xs tw:text-muted tw:mt-0.5">{{ group.values?.length ?? 0 }} lựa chọn</p>
              </div>
              <prime-tag
                v-if="!group.isActive"
                severity="danger"
                value="Tắt"
                class="tw:text-[10px]! tw:px-1! tw:py-0!"
              />
            </div>
          </div>
        </template>
      </prime-card>
    </template>

    <!-- â”€â”€ Not found â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ -->
    <prime-card v-else class="app-card tw:rounded-2xl tw:border">
      <template #content>
        <div class="tw:flex tw:flex-col tw:items-center tw:py-10 tw:text-muted">
          <iconify icon="ph:warning-bold" class="tw:text-3xl tw:mb-2" />
          <p class="tw:text-sm">{{ t('products.detail.notFound') }}</p>
        </div>
      </template>
    </prime-card>
  </section>
</template>
