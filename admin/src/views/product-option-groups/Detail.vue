<script setup>
import { onMounted, ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import {
  getProductOptionGroupById,
  updateProductOptionGroup,
} from "@/services/product-option-group.service";
import { usePermission } from "@/composables/usePermission";

const { t } = useI18n();
const route  = useRoute();
const router = useRouter();
const groupId = Number(route.params.id);
const { can } = usePermission();

// ── State ──────────────────────────────────────────────────────────────
const group       = ref(null);
const loading     = ref(false);
const saving      = ref(false);
const errorMessage = ref("");
const saveSuccess = ref(false);

const form = ref({
  name: "",
  isRequired: false,
  allowMultiple: false,
  allowQuantity: false,
  isActive: true,
  values: [],
});

// ── Helpers ────────────────────────────────────────────────────────────
const formatDate = (d) => (d ? new Date(d).toLocaleString("vi-VN") : "—");

const extractError = (err) => {
  const data = err?.response?.data;
  if (Array.isArray(data?.errors)) {
    return data.errors.map((e) => e.errorMessage ?? e).join("; ");
  }
  if (data?.errors && typeof data.errors === "object") {
    return Object.values(data.errors).flat().join("; ");
  }
  return data?.message || t("productOptionGroups.detail.error.updateFailed");
};

// ── Data ───────────────────────────────────────────────────────────────
const loadGroup = async () => {
  loading.value = true;
  errorMessage.value = "";
  try {
    const res = await getProductOptionGroupById(groupId);
    group.value = res?.data;
    form.value = {
      name: group.value.name,
      isRequired: group.value.isRequired,
      allowMultiple: group.value.allowMultiple,
      allowQuantity: group.value.allowQuantity,
      isActive: group.value.isActive,
      values: (group.value.values ?? []).map((v) => ({
        id: v.id,
        name: v.name,
        price: v.price,
        costPrice: v.costPrice ?? null,
      })),
    };
  } catch (err) {
    errorMessage.value = err?.response?.data?.message || t("productOptionGroups.detail.error.loadFailed");
  } finally {
    loading.value = false;
  }
};

const save = async () => {
  saving.value = true;
  errorMessage.value = "";
  saveSuccess.value = false;
  try {
    await updateProductOptionGroup(groupId, {
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
    await loadGroup();
    saveSuccess.value = true;
    setTimeout(() => (saveSuccess.value = false), 3000);
  } catch (err) {
    errorMessage.value = extractError(err);
  } finally {
    saving.value = false;
  }
};

// ── Values helpers ─────────────────────────────────────────────────────
const addValue = () => {
  form.value.values.push({ name: "", price: 0, costPrice: null });
};
const removeValue = (index) => {
  form.value.values.splice(index, 1);
};

onMounted(loadGroup);
</script>

<template>
  <section class="tw:space-y-6">
    <!-- Header -->
    <div class="tw:flex tw:flex-wrap tw:items-end tw:justify-between tw:gap-4">
      <div>
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300">
          {{ t("productOptionGroups.breadcrumb") }}
        </p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold tw:flex tw:items-center tw:gap-3">
          <span v-if="group">{{ group.name }}</span>
          <prime-skeleton v-else width="14rem" height="2rem" />
          <prime-tag
            v-if="group"
            :value="group.isActive ? t('productOptionGroups.status.active') : t('productOptionGroups.status.inactive')"
            :severity="group.isActive ? 'success' : 'danger'"
          />
        </h1>
        <p class="tw:mt-2 tw:text-sm tw:text-muted">
          {{ t("productOptionGroups.detail.id", { id: groupId }) }}
          <template v-if="group">
            · {{ t("productOptionGroups.detail.created") }} {{ formatDate(group.createdAt) }}
            <template v-if="group.updatedAt">
              · {{ t("productOptionGroups.detail.updated") }} {{ formatDate(group.updatedAt) }}
            </template>
          </template>
        </p>
      </div>
      <prime-button
        severity="secondary"
        outlined
        size="small"
        @click="router.push({ name: 'productOptionGroups' })"
      >
        <iconify icon="ph:arrow-left-bold" />
        <span>{{ t("productOptionGroups.detail.backToList") }}</span>
      </prime-button>
    </div>

    <!-- Loading skeleton -->
    <div v-if="loading" class="tw:grid tw:grid-cols-1 tw:gap-6 tw:lg:grid-cols-3">
      <prime-card class="app-card tw:rounded-2xl tw:border tw:lg:col-span-1">
        <template #content>
          <prime-skeleton height="6rem" class="tw:rounded-xl" />
          <prime-skeleton width="60%" height="1.25rem" class="tw:mt-4" />
        </template>
      </prime-card>
      <prime-card class="app-card tw:rounded-2xl tw:border tw:lg:col-span-2">
        <template #content>
          <prime-skeleton v-for="i in 4" :key="i" height="2.5rem" class="tw:mb-4" />
        </template>
      </prime-card>
    </div>

    <template v-else-if="group">
      <!-- Messages -->
      <prime-message
        v-if="errorMessage"
        severity="error"
        size="small"
        variant="simple"
        closable
        @close="errorMessage = ''"
      >{{ errorMessage }}</prime-message>

      <prime-message
        v-if="saveSuccess"
        severity="success"
        size="small"
        variant="simple"
        :closable="false"
      >{{ t("productOptionGroups.detail.updateSuccess") }}</prime-message>

      <div class="tw:grid tw:grid-cols-1 tw:gap-6 tw:lg:grid-cols-3">
        <!-- Left: settings -->
        <prime-card class="app-card tw:rounded-2xl tw:border tw:lg:col-span-1">
          <template #content>
            <div class="tw:flex tw:justify-center tw:mb-5">
              <div class="tw:h-20 tw:w-20 tw:rounded-2xl tw:bg-emerald-500/10 tw:flex tw:items-center tw:justify-center">
                <iconify icon="ph:stack-bold" class="tw:text-4xl tw:text-emerald-400" />
              </div>
            </div>

            <div class="tw:space-y-5">
              <!-- Name -->
              <div class="tw:space-y-1.5">
                <label for="group-name" class="tw:text-sm tw:font-medium">
                  {{ t("productOptionGroups.form.name") }} <span class="tw:text-red-400">*</span>
                </label>
                <prime-input-text
                  id="group-name"
                  v-model="form.name"
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

              <prime-divider />

              <!-- Active status -->
              <div class="tw:flex tw:items-start tw:justify-between tw:gap-3">
                <div>
                  <p class="tw:text-sm tw:font-semibold">{{ t("productOptionGroups.detail.activeOption.label") }}</p>
                  <p class="tw:text-xs tw:text-muted">{{ t("productOptionGroups.detail.activeOption.hint") }}</p>
                </div>
                <prime-toggle-switch v-model="form.isActive" />
              </div>
            </div>
          </template>
        </prime-card>

        <!-- Right: values editor + save -->
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
              <p>Chưa có lựa chọn nào.</p>
            </div>

            <div v-else class="tw:space-y-3">
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

            <div v-if="can('product.update')" class="tw:flex tw:justify-end tw:gap-3">
              <prime-button
                :label="t('productOptionGroups.detail.reset')"
                severity="secondary"
                outlined
                size="small"
                @click="loadGroup"
              />
              <prime-button
                severity="success"
                size="small"
                :loading="saving"
                @click="save"
              >
                <iconify icon="ph:check-bold" class="tw:-ml-1" />
                <span>{{ t("productOptionGroups.detail.saveChanges") }}</span>
              </prime-button>
            </div>
          </template>
        </prime-card>
      </div>
    </template>

    <!-- Not found -->
    <prime-card v-else class="app-card tw:rounded-2xl tw:border">
      <template #content>
        <div class="tw:flex tw:flex-col tw:items-center tw:py-10 tw:text-muted">
          <iconify icon="ph:warning-bold" class="tw:text-3xl tw:mb-2" />
          <p class="tw:text-sm">{{ t("productOptionGroups.detail.notFound") }}</p>
        </div>
      </template>
    </prime-card>
  </section>
</template>
