<script setup>
import { ref } from "vue";
import { useRouter } from "vue-router";
import { createCategory } from "@/services/category.service";
import { usePermission } from "@/composables/usePermission";

const { t } = useI18n();
const router = useRouter();
const { can } = usePermission();

const loading = ref(false);
const errorMessage = ref("");

const form = ref({
  name: "",
  description: "",
});

const extractError = (err) =>
  err?.response?.data?.errors?.map((e) => e.errorMessage ?? e).join("; ") ||
  err?.response?.data?.message ||
  t('categories.create.error');

const submit = async () => {
  loading.value = true;
  errorMessage.value = "";
  try {
    const res = await createCategory({
      name: form.value.name.trim(),
      description: form.value.description.trim() || null,
    });
    const newId = res?.data?.id ?? res?.data?.Id;
    if (newId) {
      router.push({ name: "categoriesDetail", params: { id: newId } });
    } else {
      router.push({ name: "categories" });
    }
  } catch (err) {
    errorMessage.value = extractError(err);
  } finally {
    loading.value = false;
  }
};
</script>

<template>
  <section class="tw:space-y-6">
    <!-- ── Header ───────────────────────────────────────────────── -->
    <div class="tw:flex tw:flex-wrap tw:items-end tw:justify-between tw:gap-4">
      <div>
        <p
          class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-primary-300"
        >
          {{ t('categories.breadcrumb') }}
        </p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">{{ t('categories.create.title') }}</h1>
        <p class="tw:mt-2 tw:text-sm tw:text-muted">
          {{ t('categories.create.subtitle') }}
        </p>
      </div>
      <prime-button
        severity="secondary"
        outlined
        size="small"
        @click="router.push({ name: 'categories' })"
      >
        <iconify icon="ph:arrow-left-bold" />
        <span>{{ t('categories.create.backToList') }}</span>
      </prime-button>
    </div>

    <!-- ── Error ─────────────────────────────────────────────────── -->
    <prime-message
      v-if="errorMessage"
      severity="error"
      size="small"
      variant="simple"
      :closable="true"
      @close="errorMessage = ''"
      >{{ errorMessage }}</prime-message
    >

    <!-- ── Form ──────────────────────────────────────────────────── -->
    <prime-card class="app-card tw:rounded-2xl tw:border">
      <template #content>
        <div class="tw:max-w-lg tw:space-y-5">
          <!-- Name -->
          <div class="tw:space-y-1.5">
            <label for="name" class="tw:text-sm tw:font-medium">
              {{ t('categories.form.name') }} <span class="tw:text-red-400">*</span>
            </label>
            <prime-input-text
              id="name"
              v-model="form.name"
              :placeholder="t('categories.form.namePlaceholder')"
              class="app-input tw:w-full"
            />
          </div>

          <!-- Description -->
          <div class="tw:space-y-1.5">
            <label for="description" class="tw:text-sm tw:font-medium">
              {{ t('categories.form.description') }}
              <span class="tw:text-muted tw:font-normal">{{ t('categories.form.optional') }}</span>
            </label>
            <prime-textarea
              id="description"
              v-model="form.description"
              rows="3"
              :placeholder="t('categories.form.descriptionPlaceholder')"
              class="app-input tw:w-full tw:resize-none"
              auto-resize
            />
          </div>
        </div>
        <prime-divider />
        <!-- ── Footer actions ── -->
        <div class="tw:flex tw:justify-end tw:gap-3">
          <prime-button
            :label="t('categories.create.cancel')"
            severity="secondary"
            outlined
            size="small"
            @click="router.push({ name: 'categories' })"
          />
          <prime-button
            v-if="can('product.create')"
            severity="success"
            size="small"
            :loading="loading"
            @click="submit"
          >
            <iconify icon="ph:check-bold" />
            <span>{{ t('categories.create.submit') }}</span>
          </prime-button>
        </div>
      </template>
    </prime-card>
  </section>
</template>
