<script setup>
import { ref } from "vue";
import { useRouter } from "vue-router";
import { createCategory } from "@/services/category.service";

const router = useRouter();

const loading      = ref(false);
const errorMessage = ref("");

const form = ref({
  name:        "",
  description: "",
});

const extractError = (err) =>
  err?.response?.data?.errors?.map((e) => e.errorMessage ?? e).join("; ") ||
  err?.response?.data?.message ||
  "Failed to create category.";

const submit = async () => {
  loading.value = true;
  errorMessage.value = "";
  try {
    const res = await createCategory({
      name:        form.value.name.trim(),
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
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300">Categories</p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">Add category</h1>
        <p class="tw:mt-2 tw:text-sm app-text-muted">Create a new category to group your menu items.</p>
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

    <!-- ── Error ─────────────────────────────────────────────────── -->
    <prime-message
      v-if="errorMessage"
      severity="error"
      size="small"
      variant="simple"
      :closable="true"
      @close="errorMessage = ''"
    >{{ errorMessage }}</prime-message>

    <!-- ── Form ──────────────────────────────────────────────────── -->
    <prime-card class="app-card tw:rounded-2xl tw:border">
      <template #content>
        <div class="tw:max-w-lg tw:space-y-5">

          <!-- Name -->
          <div class="tw:space-y-1.5">
            <label class="tw:text-sm tw:font-medium">
              Name <span class="tw:text-red-400">*</span>
            </label>
            <prime-input-text
              v-model="form.name"
              placeholder="e.g. Hot Beverages"
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
              placeholder="Short description of this category…"
              class="app-input tw:w-full tw:resize-none"
              auto-resize
            />
          </div>

        </div>

        <!-- ── Footer actions ── -->
        <div class="tw:flex tw:justify-end tw:gap-3 tw:mt-8 tw:pt-6 tw:border-t">
          <prime-button
            label="Cancel"
            severity="secondary"
            outlined
            size="small"
            @click="router.push({ name: 'categories' })"
          />
          <prime-button
            severity="success"
            size="small"
            :loading="loading"
            @click="submit"
          >
            <iconify icon="ph:check-bold" />
            <span>Create category</span>
          </prime-button>
        </div>
      </template>
    </prime-card>

  </section>
</template>
