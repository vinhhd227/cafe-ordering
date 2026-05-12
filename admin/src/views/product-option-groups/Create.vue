<script setup>
import { ref } from "vue";
import { useRouter } from "vue-router";
import { createProductOptionGroup } from "@/services/product-option-group.service";
import { usePermission } from "@/composables/usePermission";

const { t } = useI18n();
const router = useRouter();
const { can } = usePermission();

const saving = ref(false);
const errorMessage = ref("");

const form = ref({
  name: "",
  isRequired: false,
  allowMultiple: false,
  allowQuantity: false,
  values: [],
});

const addValue = () => {
  form.value.values.push({ name: "", price: 0, costPrice: null });
};

const removeValue = (index) => {
  form.value.values.splice(index, 1);
};

const extractError = (err) => {
  const data = err?.response?.data;
  if (Array.isArray(data?.errors)) {
    return data.errors.map((e) => e.errorMessage ?? e).join("; ");
  }
  if (data?.errors && typeof data.errors === "object") {
    return Object.values(data.errors).flat().join("; ");
  }
  return data?.message || t("productOptionGroups.create.error");
};

const submit = async () => {
  if (!form.value.name.trim()) return;
  saving.value = true;
  errorMessage.value = "";
  try {
    await createProductOptionGroup({
      name: form.value.name.trim(),
      isRequired: form.value.isRequired,
      allowMultiple: form.value.allowMultiple,
      allowQuantity: form.value.allowQuantity,
      values: form.value.values.map((v) => ({
        name: v.name.trim(),
        price: Number(v.price) || 0,
        costPrice: v.costPrice !== null && v.costPrice !== "" ? Number(v.costPrice) : null,
      })),
    });
    router.push({ name: "productOptionGroups" });
  } catch (err) {
    errorMessage.value = extractError(err);
  } finally {
    saving.value = false;
  }
};
</script>

<template>
  <section class="tw:space-y-6">
    <!-- Header -->
    <div class="tw:flex tw:flex-wrap tw:items-end tw:justify-between tw:gap-4">
      <div>
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300">
          {{ t("productOptionGroups.breadcrumb") }}
        </p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">{{ t("productOptionGroups.create.title") }}</h1>
        <p class="tw:mt-2 tw:text-sm tw:text-muted">{{ t("productOptionGroups.create.subtitle") }}</p>
      </div>
      <prime-button
        severity="secondary"
        outlined
        size="small"
        @click="router.push({ name: 'productOptionGroups' })"
      >
        <iconify icon="ph:arrow-left-bold" />
        <span>{{ t("productOptionGroups.create.backToList") }}</span>
      </prime-button>
    </div>

    <!-- Error -->
    <prime-message
      v-if="errorMessage"
      severity="error"
      size="small"
      variant="simple"
      :closable="true"
      @close="errorMessage = ''"
    >{{ errorMessage }}</prime-message>

    <div class="tw:grid tw:grid-cols-1 tw:gap-6 tw:lg:grid-cols-3">
      <!-- Left: settings -->
      <prime-card class="app-card tw:rounded-2xl tw:border tw:lg:col-span-1">
        <template #content>
          <p class="tw:text-sm tw:font-semibold tw:mb-5">Cài đặt nhóm</p>

          <div class="tw:space-y-5">
            <!-- Name -->
            <div class="tw:space-y-1.5">
              <label for="group-name" class="tw:text-sm tw:font-medium">
                {{ t("productOptionGroups.form.name") }} <span class="tw:text-red-400">*</span>
              </label>
              <prime-input-text
                id="group-name"
                v-model="form.name"
                :placeholder="t('productOptionGroups.form.namePlaceholder')"
                class="app-input tw:w-full"
              />
            </div>

            <prime-divider />

            <!-- Flags -->
            <div class="tw:space-y-4">
              <div class="tw:flex tw:items-start tw:justify-between tw:gap-3">
                <div>
                  <p class="tw:text-sm tw:font-medium">{{ t("productOptionGroups.form.isRequired") }}</p>
                  <p class="tw:text-xs tw:text-muted">{{ t("productOptionGroups.form.isRequiredHint") }}</p>
                </div>
                <prime-toggle-switch v-model="form.isRequired" />
              </div>
              <div class="tw:flex tw:items-start tw:justify-between tw:gap-3">
                <div>
                  <p class="tw:text-sm tw:font-medium">{{ t("productOptionGroups.form.allowMultiple") }}</p>
                  <p class="tw:text-xs tw:text-muted">{{ t("productOptionGroups.form.allowMultipleHint") }}</p>
                </div>
                <prime-toggle-switch v-model="form.allowMultiple" />
              </div>
              <div class="tw:flex tw:items-start tw:justify-between tw:gap-3">
                <div>
                  <p class="tw:text-sm tw:font-medium">{{ t("productOptionGroups.form.allowQuantity") }}</p>
                  <p class="tw:text-xs tw:text-muted">{{ t("productOptionGroups.form.allowQuantityHint") }}</p>
                </div>
                <prime-toggle-switch v-model="form.allowQuantity" />
              </div>
            </div>
          </div>
        </template>
      </prime-card>

      <!-- Right: values -->
      <prime-card class="app-card tw:rounded-2xl tw:border tw:lg:col-span-2">
        <template #content>
          <div class="tw:flex tw:items-center tw:justify-between tw:mb-5">
            <p class="tw:text-sm tw:font-semibold">{{ t("productOptionGroups.form.values") }}</p>
            <prime-button severity="success" outlined size="small" @click="addValue">
              <iconify icon="ph:plus-bold" />
              <span>{{ t("productOptionGroups.form.addValue") }}</span>
            </prime-button>
          </div>

          <div v-if="form.values.length === 0" class="tw:py-8 tw:text-center tw:text-muted tw:text-sm">
            <iconify icon="ph:list-bullets-bold" class="tw:text-3xl tw:mb-2 tw:opacity-30" />
            <p>Chưa có lựa chọn nào. Nhấn "Thêm lựa chọn" để bắt đầu.</p>
          </div>

          <div v-else class="tw:space-y-3">
            <!-- Header row -->
            <div class="tw:grid tw:grid-cols-12 tw:gap-2 tw:text-xs tw:text-muted tw:uppercase tw:tracking-wider tw:px-1">
              <div class="tw:col-span-5">{{ t("productOptionGroups.form.valueName") }}</div>
              <div class="tw:col-span-3">{{ t("productOptionGroups.form.valuePrice") }}</div>
              <div class="tw:col-span-3">{{ t("productOptionGroups.form.valueCostPrice") }}</div>
              <div class="tw:col-span-1"></div>
            </div>

            <div
              v-for="(val, idx) in form.values"
              :key="idx"
              class="tw:grid tw:grid-cols-12 tw:gap-2 tw:items-center"
            >
              <div class="tw:col-span-5">
                <prime-input-text
                  v-model="val.name"
                  :placeholder="t('productOptionGroups.form.valueNamePlaceholder')"
                  class="app-input tw:w-full tw:text-sm"
                  size="small"
                />
              </div>
              <div class="tw:col-span-3">
                <prime-input-number
                  v-model="val.price"
                  :min="0"
                  :max-fraction-digits="0"
                  class="app-input tw:w-full"
                  input-class="tw:text-sm tw:text-right"
                  size="small"
                />
              </div>
              <div class="tw:col-span-3">
                <prime-input-number
                  v-model="val.costPrice"
                  :min="0"
                  :max-fraction-digits="0"
                  class="app-input tw:w-full"
                  input-class="tw:text-sm tw:text-right"
                  size="small"
                />
              </div>
              <div class="tw:col-span-1 tw:flex tw:justify-center">
                <prime-button
                  severity="danger"
                  text
                  size="small"
                  v-tooltip.top="t('productOptionGroups.form.removeValue')"
                  @click="removeValue(idx)"
                >
                  <iconify icon="ph:x-bold" class="tw:text-xs" />
                </prime-button>
              </div>
            </div>
          </div>

          <prime-divider />

          <div class="tw:flex tw:justify-end tw:gap-3">
            <prime-button
              :label="t('productOptionGroups.create.backToList')"
              severity="secondary"
              outlined
              size="small"
              @click="router.push({ name: 'productOptionGroups' })"
            />
            <prime-button
              severity="success"
              size="small"
              :loading="saving"
              :disabled="!form.name.trim()"
              @click="submit"
            >
              <iconify icon="ph:check-bold" class="tw:-ml-1" />
              <span>{{ t("productOptionGroups.create.submit") }}</span>
            </prime-button>
          </div>
        </template>
      </prime-card>
    </div>
  </section>
</template>
