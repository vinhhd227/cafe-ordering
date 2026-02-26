<script setup>
import { onMounted, ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import { getCategoryById, updateCategory } from "@/services/category.service";

const route  = useRoute();
const router = useRouter();
const categoryId = Number(route.params.id);

// ── State ──────────────────────────────────────────────────────────
const category    = ref(null);
const loading     = ref(false);
const saving      = ref(false);
const errorMessage = ref("");
const saveSuccess  = ref(false);

// Edit form
const form = ref({
  name:        "",
  description: "",
  isActive:    true,
});

// ── Helpers ────────────────────────────────────────────────────────
const formatDate = (d) =>
  d ? new Date(d).toLocaleString("vi-VN") : "—";

const extractError = (err) =>
  err?.response?.data?.errors?.map((e) => e.errorMessage ?? e).join("; ") ||
  err?.response?.data?.message ||
  "Failed to update category.";

// ── Data ───────────────────────────────────────────────────────────
const loadCategory = async () => {
  loading.value = true;
  errorMessage.value = "";
  try {
    const res = await getCategoryById(categoryId);
    category.value = res?.data;
    form.value = {
      name:        category.value.name,
      description: category.value.description ?? "",
      isActive:    category.value.isActive,
    };
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.message || "Failed to load category.";
  } finally {
    loading.value = false;
  }
};

const save = async () => {
  saving.value = true;
  errorMessage.value = "";
  saveSuccess.value = false;
  try {
    await updateCategory(categoryId, {
      name:        form.value.name.trim(),
      description: form.value.description.trim() || null,
      isActive:    form.value.isActive,
    });
    await loadCategory();
    saveSuccess.value = true;
    setTimeout(() => (saveSuccess.value = false), 3000);
  } catch (err) {
    errorMessage.value = extractError(err);
  } finally {
    saving.value = false;
  }
};

onMounted(loadCategory);
</script>

<template>
  <section class="tw:space-y-6">

    <!-- ── Header ───────────────────────────────────────────────── -->
    <div class="tw:flex tw:flex-wrap tw:items-end tw:justify-between tw:gap-4">
      <div>
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300">Categories</p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold tw:flex tw:items-center tw:gap-3">
          <span v-if="category">{{ category.name }}</span>
          <prime-skeleton v-else width="14rem" height="2rem" />
          <prime-tag
            v-if="category"
            :value="category.isActive ? 'Active' : 'Inactive'"
            :severity="category.isActive ? 'success' : 'danger'"
          />
        </h1>
        <p class="tw:mt-2 tw:text-sm app-text-muted">
          Category #{{ categoryId }}
          <template v-if="category">
            · Created {{ formatDate(category.createdAt) }}
            <template v-if="category.updatedAt">
              · Updated {{ formatDate(category.updatedAt) }}
            </template>
          </template>
        </p>
      </div>
      <prime-button
        severity="secondary"
        outlined
        size="small"
        @click="router.push({ name: 'categories' })"
      >
        <iconify icon="ph:arrow-left-bold" />
        <span>Back to list</span>
      </prime-button>
    </div>

    <!-- ── Loading skeleton ──────────────────────────────────────── -->
    <div v-if="loading" class="tw:grid tw:grid-cols-1 tw:gap-6 tw:lg:grid-cols-3">
      <prime-card class="app-card tw:rounded-2xl tw:border tw:lg:col-span-1">
        <template #content>
          <prime-skeleton height="6rem" class="tw:rounded-xl" />
          <prime-skeleton width="60%" height="1.25rem" class="tw:mt-4" />
          <prime-skeleton width="40%" height="1rem" class="tw:mt-2" />
        </template>
      </prime-card>
      <prime-card class="app-card tw:rounded-2xl tw:border tw:lg:col-span-2">
        <template #content>
          <prime-skeleton v-for="i in 3" :key="i" height="2.5rem" class="tw:mb-4" />
        </template>
      </prime-card>
    </div>

    <template v-else-if="category">

      <!-- ── Messages ───────────────────────────────────────────── -->
      <prime-message
        v-if="errorMessage"
        severity="error"
        size="small"
        variant="simple"
        :closable="true"
        @close="errorMessage = ''"
      >{{ errorMessage }}</prime-message>

      <prime-message
        v-if="saveSuccess"
        severity="success"
        size="small"
        variant="simple"
        :closable="false"
      >Category updated successfully.</prime-message>

      <div class="tw:grid tw:grid-cols-1 tw:gap-6 tw:lg:grid-cols-3">

        <!-- ── Left: readonly info ─────────────────────────────── -->
        <prime-card class="app-card tw:rounded-2xl tw:border tw:lg:col-span-1">
          <template #content>

            <!-- Icon placeholder -->
            <div class="tw:flex tw:justify-center tw:mb-5">
              <div class="tw:h-24 tw:w-24 tw:rounded-2xl tw:bg-emerald-500/10 tw:flex tw:items-center tw:justify-center">
                <iconify icon="ph:tag-bold" class="tw:text-4xl tw:text-emerald-400" />
              </div>
            </div>

            <!-- Info rows -->
            <div class="tw:space-y-3">
              <div class="tw:flex tw:justify-between tw:text-sm">
                <span class="app-text-muted">Status</span>
                <prime-tag
                  :value="category.isActive ? 'Active' : 'Inactive'"
                  :severity="category.isActive ? 'success' : 'danger'"
                />
              </div>
              <div class="tw:flex tw:justify-between tw:text-sm">
                <span class="app-text-muted">Created</span>
                <span class="tw:font-medium tw:text-right">{{ formatDate(category.createdAt) }}</span>
              </div>
              <div v-if="category.updatedAt" class="tw:flex tw:justify-between tw:text-sm">
                <span class="app-text-muted">Updated</span>
                <span class="tw:font-medium tw:text-right">{{ formatDate(category.updatedAt) }}</span>
              </div>
            </div>

            <!-- Description preview -->
            <div v-if="category.description" class="tw:mt-5">
              <p class="tw:text-xs tw:uppercase tw:tracking-widest app-text-subtle tw:mb-1">
                Description
              </p>
              <p class="tw:text-sm app-text-muted tw:leading-relaxed">
                {{ category.description }}
              </p>
            </div>

          </template>
        </prime-card>

        <!-- ── Right: edit form ────────────────────────────────── -->
        <prime-card class="app-card tw:rounded-2xl tw:border tw:lg:col-span-2">
          <template #content>
            <p class="tw:text-sm tw:font-semibold tw:mb-5">Edit details</p>

            <div class="tw:space-y-5">

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

            </div>

            <!-- Active status -->
            <div class="tw:mt-6 tw:pt-5 tw:border-t">
              <div class="tw:flex tw:items-center tw:justify-between">
                <div>
                  <p class="tw:text-sm tw:font-semibold">Active</p>
                  <p class="tw:text-xs app-text-muted">Hiển thị category trên menu</p>
                </div>
                <prime-toggle-switch v-model="form.isActive" />
              </div>
            </div>

            <!-- Actions -->
            <div class="tw:flex tw:justify-end tw:gap-3 tw:mt-6 tw:pt-6 tw:border-t">
              <prime-button
                label="Reset"
                severity="secondary"
                outlined
                size="small"
                @click="loadCategory"
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
        <div class="tw:flex tw:flex-col tw:items-center tw:py-10 app-text-muted">
          <iconify icon="ph:warning-bold" class="tw:text-3xl tw:mb-2" />
          <p class="tw:text-sm">Category not found.</p>
        </div>
      </template>
    </prime-card>

  </section>
</template>
