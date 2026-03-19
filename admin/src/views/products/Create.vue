<script setup>
import { onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import { createProduct } from "@/services/product.service";
import { getCategory } from "@/services/category.service";
import { uploadImage } from "@/services/upload.service";
import { usePermission } from "@/composables/usePermission";

const { t } = useI18n();
const router = useRouter();
const { can } = usePermission();

const categories = ref([]);
const loading = ref(false);
const errorMessage = ref("");

const form = ref({
  categoryId: null,
  name: "",
  price: null,
  description: "",
  imageUrl: "",
  hasTemperatureOption: false,
  hasIceLevelOption: false,
  hasSugarLevelOption: false,
  isAccompaniment: false,
  estimatedPrepMinutes: null,
});

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
  } catch { /* non-critical */ }
};

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

const extractError = (err) =>
  err?.response?.data?.errors?.map((e) => e.errorMessage ?? e).join("; ") ||
  err?.response?.data?.message ||
  t('products.create.error');

const submit = async () => {
  loading.value = true;
  errorMessage.value = "";
  try {
    const res = await createProduct({
      categoryId: form.value.categoryId,
      name: form.value.name.trim(),
      price: Number(form.value.price),
      description: form.value.description.trim() || null,
      imageUrl: form.value.imageUrl.trim() || null,
      hasTemperatureOption: form.value.hasTemperatureOption,
      hasIceLevelOption: form.value.hasIceLevelOption,
      hasSugarLevelOption: form.value.hasSugarLevelOption,
      isAccompaniment: form.value.isAccompaniment,
      estimatedPrepMinutes: form.value.estimatedPrepMinutes || null,
    });
    const newId = res?.data?.id ?? res?.data?.Id;
    if (newId) {
      router.push({ name: "productsDetail", params: { id: newId } });
    } else {
      router.push({ name: "products" });
    }
  } catch (err) {
    errorMessage.value = extractError(err);
  } finally {
    loading.value = false;
  }
};

onMounted(loadCategories);
</script>

<template>
  <section class="tw:space-y-6">

    <!-- ── Header ───────────────────────────────────────────────── -->
    <div class="tw:flex tw:flex-wrap tw:items-end tw:justify-between tw:gap-4">
      <div>
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300">{{ t('products.breadcrumb') }}</p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">{{ t('products.create.title') }}</h1>
        <p class="tw:mt-2 tw:text-sm app-text-muted">{{ t('products.create.subtitle') }}</p>
      </div>
      <prime-button
        :label="t('products.create.backToList')"
        icon="pi pi-arrow-left"
        severity="secondary"
        outlined
        size="small"
        @click="router.push({ name: 'products' })"
      />
    </div>

    <!-- ── Error ─────────────────────────────────────────────────── -->
    <prime-message
      v-if="errorMessage"
      severity="error" size="small" variant="simple" :closable="true"
      @close="errorMessage = ''"
    >{{ errorMessage }}</prime-message>

    <!-- ── Form Card ─────────────────────────────────────────────── -->
    <prime-card class="app-card tw:rounded-2xl tw:border">
      <template #content>
        <div class="tw:grid tw:grid-cols-1 tw:gap-6 tw:lg:grid-cols-2">

          <!-- ── Left column ── -->
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
                :placeholder="t('products.form.namePlaceholder')"
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
                :placeholder="t('products.form.pricePlaceholder')"
                class="app-input tw:w-full"
              />
            </div>

            <!-- Description -->
            <div class="tw:space-y-1.5">
              <label class="tw:text-sm tw:font-medium">
                {{ t('products.form.description') }}
                <span class="app-text-muted tw:font-normal">{{ t('products.form.optional') }}</span>
              </label>
              <prime-textarea
                v-model="form.description"
                rows="3"
                :placeholder="t('products.form.descriptionPlaceholder')"
                class="app-input tw:w-full tw:resize-none"
                auto-resize
              />
            </div>

            <!-- Estimated prep time -->
            <div class="tw:space-y-1.5">
              <label class="tw:text-sm tw:font-medium">
                {{ t('products.form.estimatedPrepMinutes') }}
                <span class="app-text-muted tw:font-normal">{{ t('products.form.optional') }}</span>
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
                <span class="app-text-muted tw:font-normal">{{ t('products.form.optional') }}</span>
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
                class="tw:mt-2 tw:h-24 tw:w-24 tw:rounded-xl tw:object-cover tw:border"
              />
            </div>

          </div>

          <!-- ── Right column: Options ── -->
          <div class="tw:space-y-5">
            <div>
              <p class="tw:text-sm tw:font-medium tw:mb-1">{{ t('products.options.title') }}</p>
              <p class="tw:text-xs app-text-muted">{{ t('products.options.subtitle') }}</p>
            </div>

            <div class="tw:space-y-4">

              <!-- Temperature -->
              <div class="app-card tw:flex tw:items-start tw:justify-between tw:gap-4 tw:rounded-xl tw:border tw:p-4">
                <div>
                  <p class="tw:text-sm tw:font-medium">{{ t('products.options.temperature.label') }}</p>
                  <p class="tw:text-xs app-text-muted tw:mt-0.5">{{ t('products.options.temperature.hint') }}</p>
                </div>
                <prime-toggle-switch v-model="form.hasTemperatureOption" />
              </div>

              <!-- Ice level -->
              <div class="app-card tw:flex tw:items-start tw:justify-between tw:gap-4 tw:rounded-xl tw:border tw:p-4">
                <div>
                  <p class="tw:text-sm tw:font-medium">{{ t('products.options.iceLevel.label') }}</p>
                  <p class="tw:text-xs app-text-muted tw:mt-0.5">{{ t('products.options.iceLevel.hint') }}</p>
                </div>
                <prime-toggle-switch v-model="form.hasIceLevelOption" />
              </div>

              <!-- Sugar level -->
              <div class="app-card tw:flex tw:items-start tw:justify-between tw:gap-4 tw:rounded-xl tw:border tw:p-4">
                <div>
                  <p class="tw:text-sm tw:font-medium">{{ t('products.options.sugarLevel.label') }}</p>
                  <p class="tw:text-xs app-text-muted tw:mt-0.5">{{ t('products.options.sugarLevel.hint') }}</p>
                </div>
                <prime-toggle-switch v-model="form.hasSugarLevelOption" />
              </div>

              <!-- Accompaniment -->
              <div class="app-card tw:flex tw:items-start tw:justify-between tw:gap-4 tw:rounded-xl tw:border tw:p-4">
                <div>
                  <p class="tw:text-sm tw:font-medium">{{ t('products.options.accompaniment.label') }}</p>
                  <p class="tw:text-xs app-text-muted tw:mt-0.5">{{ t('products.options.accompaniment.hint') }}</p>
                </div>
                <prime-toggle-switch v-model="form.isAccompaniment" />
              </div>

            </div>
          </div>
        </div>
        <prime-divider/>
        <!-- ── Footer actions ── -->
        <div class="tw:flex tw:justify-end tw:gap-3">
          <prime-button
            :label="t('products.create.cancel')"
            severity="secondary"
            outlined
            size="small"
            @click="router.push({ name: 'products' })"
          />
          <prime-button
            v-if="can('product.create')"
            :label="t('products.create.submit')"
            icon="pi pi-check"
            severity="success"
            size="small"
            :loading="loading"
            @click="submit"
          />
        </div>
      </template>
    </prime-card>

  </section>
</template>
