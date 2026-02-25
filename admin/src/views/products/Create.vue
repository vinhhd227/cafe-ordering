<script setup>
import { onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import { createProduct } from "@/services/product.service";
import { getCategory } from "@/services/category.service";

const router = useRouter();

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
});

const loadCategories = async () => {
  try {
    const res = await getCategory();
    const raw = res?.data;
    // Handle plain array hoặc Ardalis-wrapped result
    categories.value = Array.isArray(raw)
      ? raw
      : Array.isArray(raw?.value)
      ? raw.value
      : Array.isArray(raw?.items)
      ? raw.items
      : [];
  } catch { /* non-critical */ }
};

const extractError = (err) =>
  err?.response?.data?.errors?.map((e) => e.errorMessage ?? e).join("; ") ||
  err?.response?.data?.message ||
  "Failed to create product.";

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
    });
    // Endpoint trả về { id } với 201
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
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300">Products</p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">Add product</h1>
        <p class="tw:mt-2 tw:text-sm app-text-muted">Fill in the details to add a new item to the menu.</p>
      </div>
      <prime-button
        label="Back to list"
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
                placeholder="e.g. Caramel Macchiato"
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
                placeholder="e.g. 45000"
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
                placeholder="Short description shown on the menu card…"
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
              <!-- Preview -->
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
              <p class="tw:text-sm tw:font-medium tw:mb-1">Customisation options</p>
              <p class="tw:text-xs app-text-muted">
                Let customers personalise their order when placing it.
              </p>
            </div>

            <div class="tw:space-y-4">

              <!-- Temperature -->
              <div class="tw:flex tw:items-start tw:justify-between tw:gap-4 tw:rounded-xl tw:border tw:p-4">
                <div>
                  <p class="tw:text-sm tw:font-medium">Temperature</p>
                  <p class="tw:text-xs app-text-muted tw:mt-0.5">
                    Hot / Iced / Blended
                  </p>
                </div>
                <prime-toggle-switch v-model="form.hasTemperatureOption" />
              </div>

              <!-- Ice level -->
              <div class="tw:flex tw:items-start tw:justify-between tw:gap-4 tw:rounded-xl tw:border tw:p-4">
                <div>
                  <p class="tw:text-sm tw:font-medium">Ice level</p>
                  <p class="tw:text-xs app-text-muted tw:mt-0.5">
                    No ice / Less / Normal / Extra
                  </p>
                </div>
                <prime-toggle-switch v-model="form.hasIceLevelOption" />
              </div>

              <!-- Sugar level -->
              <div class="tw:flex tw:items-start tw:justify-between tw:gap-4 tw:rounded-xl tw:border tw:p-4">
                <div>
                  <p class="tw:text-sm tw:font-medium">Sugar level</p>
                  <p class="tw:text-xs app-text-muted tw:mt-0.5">
                    0 % / 25 % / 50 % / 75 % / 100 %
                  </p>
                </div>
                <prime-toggle-switch v-model="form.hasSugarLevelOption" />
              </div>

            </div>
          </div>
        </div>

        <!-- ── Footer actions ── -->
        <div class="tw:flex tw:justify-end tw:gap-3 tw:mt-8 tw:pt-6 tw:border-t">
          <prime-button
            label="Cancel"
            severity="secondary"
            outlined
            size="small"
            @click="router.push({ name: 'products' })"
          />
          <prime-button
            label="Create product"
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
