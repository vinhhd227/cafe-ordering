<script setup>
import AppTable from "@/components/AppTable.vue";
import {
  getPromotions,
  createPromotion,
  updatePromotion,
  deletePromotion,
  togglePromotion,
} from "@/services/promotion.service";
import {
  PROMOTION_DISCOUNT_TYPE,
  PROMOTION_DISCOUNT_TYPE_MAP,
  PROMOTION_DISCOUNT_TYPE_OPTIONS,
} from "@/constants/promotionDiscountType";
import {
  PROMOTION_SCOPE,
  PROMOTION_SCOPE_MAP,
  PROMOTION_SCOPE_OPTIONS,
} from "@/constants/promotionScope";
import {
  STACK_POLICY,
  STACK_POLICY_MAP,
  STACK_POLICY_OPTIONS,
} from "@/constants/stackPolicy";
import { btnIcon } from "@/layout/ui";

// ── Cache ──────────────────────────────────────────────────────────
const { save: saveCache, restore: restoreCache } = useTableCache("promotions-list");

// ── PrimeVue services ─────────────────────────────────────────────
const confirm = useConfirm();
const toast   = useToast();

// ── Table state ────────────────────────────────────────────────────
const promotions   = ref([]);
const totalRecords = ref(0);
const rows         = ref(20);
const first        = ref(0);
const loading      = ref(false);
const errorMessage = ref("");

// ── Filters ────────────────────────────────────────────────────────
const isActiveFilter    = ref(null);
const scopeFilter       = ref(null);
const discountTypeFilter = ref(null);
const filterPanel        = ref(null);

// ── Restore cache ─────────────────────────────────────────────────
const _cached = restoreCache();
if (_cached) {
  if (_cached.rows !== undefined)               rows.value              = _cached.rows;
  if (_cached.first !== undefined)              first.value             = _cached.first;
  if (_cached.isActiveFilter !== undefined)     isActiveFilter.value    = _cached.isActiveFilter;
  if (_cached.scopeFilter !== undefined)        scopeFilter.value       = _cached.scopeFilter;
  if (_cached.discountTypeFilter !== undefined) discountTypeFilter.value = _cached.discountTypeFilter;
}

// ── Helpers ───────────────────────────────────────────────────────
const formatVnd = (value) =>
  new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
    maximumFractionDigits: 0,
  }).format(value ?? 0);

const formatDate = (dateStr) => {
  if (!dateStr) return "—";
  return new Intl.DateTimeFormat("vi-VN", {
    day:   "2-digit",
    month: "2-digit",
    year:  "numeric",
  }).format(new Date(dateStr));
};

const discountValueLabel = (promo) => {
  if (promo.discountType === PROMOTION_DISCOUNT_TYPE.PERCENTAGE) return `${promo.discountValue}%`;
  if (promo.discountType === PROMOTION_DISCOUNT_TYPE.FIXED)      return formatVnd(promo.discountValue);
  return `Buy ${promo.buyQuantity ?? 0} Get ${promo.getQuantity ?? 0}`;
};

// ── Filter helpers ─────────────────────────────────────────────────
const activeFilterCount = computed(() => {
  let n = 0;
  if (isActiveFilter.value !== null)    n++;
  if (scopeFilter.value !== null)       n++;
  if (discountTypeFilter.value !== null) n++;
  return n;
});

const hasActiveFilters = computed(() => activeFilterCount.value > 0);

const clearFilters = () => {
  isActiveFilter.value    = null;
  scopeFilter.value       = null;
  discountTypeFilter.value = null;
  first.value             = 0;
};

// ── Load ──────────────────────────────────────────────────────────
const saveCurrentState = () => {
  saveCache({
    rows:               rows.value,
    first:              first.value,
    isActiveFilter:     isActiveFilter.value,
    scopeFilter:        scopeFilter.value,
    discountTypeFilter: discountTypeFilter.value,
  });
};

const loadPromotions = async () => {
  loading.value = true;
  errorMessage.value = "";
  try {
    const page = Math.floor(first.value / rows.value) + 1;
    const res  = await getPromotions({
      isActive:     isActiveFilter.value ?? undefined,
      scope:        scopeFilter.value || undefined,
      discountType: discountTypeFilter.value || undefined,
      page,
      pageSize: rows.value,
    });
    const data = res?.data;
    promotions.value  = data?.items ?? [];
    totalRecords.value = data?.totalCount ?? 0;
    saveCurrentState();
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.message || "Failed to load promotions.";
  } finally {
    loading.value = false;
  }
};

onMounted(loadPromotions);

watch([isActiveFilter, scopeFilter, discountTypeFilter], () => {
  first.value = 0;
  loadPromotions();
});

const onPage = (e) => {
  first.value = e.first;
  rows.value  = e.rows;
  loadPromotions();
};

// ── Create / Edit dialog ──────────────────────────────────────────
const dialogVisible    = ref(false);
const editingPromotion = ref(null);
const formLoading      = ref(false);
const formError        = ref("");

// Form fields
const fName              = ref("");
const fCode              = ref("");
const fDescription       = ref("");
const fDiscountType      = ref(PROMOTION_DISCOUNT_TYPE.PERCENTAGE);
const fDiscountValue     = ref(null);
const fBuyQuantity       = ref(null);
const fGetQuantity       = ref(null);
const fScope             = ref(PROMOTION_SCOPE.ORDER);
const fApplicableProductIds  = ref("");  // comma-separated input
const fApplicableCategoryIds = ref("");  // comma-separated input
const fStackPolicy       = ref(STACK_POLICY.EXCLUSIVE);
const fMinOrderAmount    = ref(null);
const fStartDate         = ref(new Date());
const fEndDate           = ref(null);
const fMaxUsage          = ref(null);
const fIsActive          = ref(true);

const isEditMode  = computed(() => editingPromotion.value !== null);
const dialogHeader = computed(() =>
  isEditMode.value ? "Edit promotion" : "New promotion"
);

const isBuyXGetY = computed(() => fDiscountType.value === PROMOTION_DISCOUNT_TYPE.BUY_X_GET_Y);
const isProductScope  = computed(() => fScope.value === PROMOTION_SCOPE.PRODUCT);
const isCategoryScope = computed(() => fScope.value === PROMOTION_SCOPE.CATEGORY);

const resetForm = () => {
  fName.value              = "";
  fCode.value              = "";
  fDescription.value       = "";
  fDiscountType.value      = PROMOTION_DISCOUNT_TYPE.PERCENTAGE;
  fDiscountValue.value     = null;
  fBuyQuantity.value       = null;
  fGetQuantity.value       = null;
  fScope.value             = PROMOTION_SCOPE.ORDER;
  fApplicableProductIds.value  = "";
  fApplicableCategoryIds.value = "";
  fStackPolicy.value       = STACK_POLICY.EXCLUSIVE;
  fMinOrderAmount.value    = null;
  fStartDate.value         = new Date();
  fEndDate.value           = null;
  fMaxUsage.value          = null;
  fIsActive.value          = true;
  formError.value          = "";
};

const openCreateDialog = () => {
  editingPromotion.value = null;
  resetForm();
  dialogVisible.value = true;
};

const openEditDialog = (promo) => {
  editingPromotion.value = promo;
  fName.value              = promo.name;
  fCode.value              = promo.code ?? "";
  fDescription.value       = promo.description ?? "";
  fDiscountType.value      = promo.discountType;
  fDiscountValue.value     = promo.discountValue ?? null;
  fBuyQuantity.value       = promo.buyQuantity ?? null;
  fGetQuantity.value       = promo.getQuantity ?? null;
  fScope.value             = promo.scope;
  fApplicableProductIds.value  = (promo.applicableProductIds ?? []).join(", ");
  fApplicableCategoryIds.value = (promo.applicableCategoryIds ?? []).join(", ");
  fStackPolicy.value       = promo.stackPolicy;
  fMinOrderAmount.value    = promo.minOrderAmount ?? null;
  fStartDate.value         = new Date(promo.startDate);
  fEndDate.value           = promo.endDate ? new Date(promo.endDate) : null;
  fMaxUsage.value          = promo.maxUsage ?? null;
  fIsActive.value          = promo.isActive;
  formError.value          = "";
  dialogVisible.value      = true;
};

const parseIds = (str) => {
  if (!str || !str.trim()) return [];
  return str.split(",")
    .map((s) => parseInt(s.trim()))
    .filter((n) => !isNaN(n));
};

const submitForm = async () => {
  formError.value = "";
  if (!fName.value.trim()) {
    formError.value = "Promotion name is required.";
    return;
  }
  if (!isBuyXGetY.value && (fDiscountValue.value === null || fDiscountValue.value < 0)) {
    formError.value = "Discount value is required.";
    return;
  }
  if (isBuyXGetY.value && (!fBuyQuantity.value || !fGetQuantity.value)) {
    formError.value = "Buy quantity and Get quantity are required for Buy X Get Y.";
    return;
  }

  formLoading.value = true;
  try {
    const payload = {
      name:              fName.value.trim(),
      code:              fCode.value.trim() || null,
      description:       fDescription.value.trim() || null,
      discountType:      fDiscountType.value,
      discountValue:     fDiscountValue.value ?? 0,
      buyQuantity:       fBuyQuantity.value ?? null,
      getQuantity:       fGetQuantity.value ?? null,
      scope:             fScope.value,
      applicableProductIds:  parseIds(fApplicableProductIds.value),
      applicableCategoryIds: parseIds(fApplicableCategoryIds.value),
      stackPolicy:       fStackPolicy.value,
      minOrderAmount:    fMinOrderAmount.value ?? null,
      startDate:         fStartDate.value?.toISOString?.() ?? fStartDate.value,
      endDate:           fEndDate.value?.toISOString?.() ?? null,
      maxUsage:          fMaxUsage.value ?? null,
      isActive:          fIsActive.value,
    };

    if (isEditMode.value) {
      await updatePromotion(editingPromotion.value.id, payload);
      toast.add({ severity: "success", summary: "Updated", detail: "Promotion updated.", life: 3000 });
    } else {
      await createPromotion(payload);
      toast.add({ severity: "success", summary: "Created", detail: "Promotion created.", life: 3000 });
    }

    dialogVisible.value = false;
    first.value = 0;
    await loadPromotions();
  } catch (err) {
    formError.value =
      err?.response?.data?.errors?.join(", ") ||
      err?.response?.data?.title ||
      "Failed to save promotion.";
  } finally {
    formLoading.value = false;
  }
};

// ── Column definitions ────────────────────────────────────────────
const columns = [
  { field: 'name',   header: 'Name',     width: '14rem' },
  { key: 'type',     header: 'Type',     width: '10rem' },
  { key: 'scope',    header: 'Scope',    width: '10rem' },
  { key: 'discount', header: 'Discount', width: '9rem' },
  { key: 'validity', header: 'Validity', width: '12rem' },
  { key: 'usage',    header: 'Usage',    width: '7rem' },
  { key: 'stack',    header: 'Stack',    width: '8rem' },
  { key: 'isActive', header: 'Active',   width: '6rem' },
  { key: 'actions',  header: 'Actions',  width: '8rem', toggleable: false },
];

// ── Toggle active ──────────────────────────────────────────────────
const togglingId = ref(null);

const handleToggle = async (promo) => {
  togglingId.value = promo.id;
  try {
    const res = await togglePromotion(promo.id, !promo.isActive);
    const updated = res?.data;
    if (updated) {
      const idx = promotions.value.findIndex((p) => p.id === promo.id);
      if (idx !== -1) promotions.value[idx] = updated;
    }
    toast.add({
      severity: "success",
      summary: "Updated",
      detail: `Promotion ${updated?.isActive ? "activated" : "deactivated"}.`,
      life: 2500,
    });
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.title || "Failed to toggle promotion.";
  } finally {
    togglingId.value = null;
  }
};

// ── Delete ────────────────────────────────────────────────────────
const handleDelete = (promo) => {
  confirm.require({
    message: `Delete "${promo.name}"? This cannot be undone.`,
    header:  "Confirm delete",
    acceptSeverity: "danger",
    acceptLabel:    "Delete",
    rejectLabel:    "Cancel",
    accept: async () => {
      try {
        await deletePromotion(promo.id);
        toast.add({ severity: "success", summary: "Deleted", detail: "Promotion deleted.", life: 3000 });
        if (promotions.value.length === 1 && first.value > 0) {
          first.value = Math.max(0, first.value - rows.value);
        }
        await loadPromotions();
      } catch (err) {
        errorMessage.value =
          err?.response?.data?.title || "Failed to delete promotion.";
      }
    },
  });
};
</script>

<template>
  <!-- Confirm dialog (delete) -->
  <prime-confirm-dialog />

  <!-- Create / Edit dialog -->
  <prime-dialog
    v-model:visible="dialogVisible"
    :header="dialogHeader"
    :modal="true"
    :style="{ width: '38rem' }"
  >
    <div class="tw:space-y-4">
      <!-- Name -->
      <div class="tw:space-y-1.5">
        <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">
          Name <span class="tw:text-red-400">*</span>
        </label>
        <prime-input-text
          v-model="fName"
          placeholder="e.g. Summer sale 20%…"
          class="app-input tw:w-full"
        />
      </div>

      <!-- Code + Description -->
      <div class="tw:grid tw:grid-cols-2 tw:gap-3">
        <div class="tw:space-y-1.5">
          <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">
            Promo code
            <span class="tw:normal-case tw:opacity-60">(leave blank for auto-apply)</span>
          </label>
          <prime-input-text
            v-model="fCode"
            placeholder="SUMMER20"
            class="app-input tw:w-full tw:uppercase"
          />
        </div>
        <div class="tw:space-y-1.5">
          <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">
            Stack policy
          </label>
          <prime-select
            v-model="fStackPolicy"
            :options="STACK_POLICY_OPTIONS"
            option-label="label"
            option-value="value"
            class="app-input tw:w-full"
          >
            <template #option="{ option }">
              <div class="tw:flex tw:items-center tw:gap-2">
                <iconify :icon="option.icon" />
                <span>{{ option.label }}</span>
              </div>
            </template>
            <template #value="{ value }">
              <div v-if="value" class="tw:flex tw:items-center tw:gap-2">
                <iconify :icon="STACK_POLICY_MAP[value]?.icon" />
                <span>{{ STACK_POLICY_MAP[value]?.label }}</span>
              </div>
            </template>
          </prime-select>
        </div>
      </div>

      <!-- Discount type + Scope -->
      <div class="tw:grid tw:grid-cols-2 tw:gap-3">
        <div class="tw:space-y-1.5">
          <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">
            Discount type
          </label>
          <prime-select
            v-model="fDiscountType"
            :options="PROMOTION_DISCOUNT_TYPE_OPTIONS"
            option-label="label"
            option-value="value"
            class="app-input tw:w-full"
          >
            <template #option="{ option }">
              <div class="tw:flex tw:items-center tw:gap-2">
                <iconify :icon="option.icon" />
                <span>{{ option.label }}</span>
              </div>
            </template>
            <template #value="{ value }">
              <div v-if="value" class="tw:flex tw:items-center tw:gap-2">
                <iconify :icon="PROMOTION_DISCOUNT_TYPE_MAP[value]?.icon" />
                <span>{{ PROMOTION_DISCOUNT_TYPE_MAP[value]?.label }}</span>
              </div>
            </template>
          </prime-select>
        </div>
        <div class="tw:space-y-1.5">
          <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">
            Scope
          </label>
          <prime-select
            v-model="fScope"
            :options="PROMOTION_SCOPE_OPTIONS"
            option-label="label"
            option-value="value"
            class="app-input tw:w-full"
          >
            <template #option="{ option }">
              <div class="tw:flex tw:items-center tw:gap-2">
                <iconify :icon="option.icon" />
                <span>{{ option.label }}</span>
              </div>
            </template>
            <template #value="{ value }">
              <div v-if="value" class="tw:flex tw:items-center tw:gap-2">
                <iconify :icon="PROMOTION_SCOPE_MAP[value]?.icon" />
                <span>{{ PROMOTION_SCOPE_MAP[value]?.label }}</span>
              </div>
            </template>
          </prime-select>
        </div>
      </div>

      <!-- Discount value (conditional) -->
      <div v-if="!isBuyXGetY" class="tw:space-y-1.5">
        <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">
          Discount value <span class="tw:text-red-400">*</span>
          <span class="tw:normal-case tw:opacity-60">
            {{ fDiscountType === 'PERCENTAGE' ? '(%)' : '(₫)' }}
          </span>
        </label>
        <prime-input-number
          v-model="fDiscountValue"
          :min="0"
          :max="fDiscountType === 'PERCENTAGE' ? 100 : undefined"
          :max-fraction-digits="fDiscountType === 'PERCENTAGE' ? 2 : 0"
          :use-grouping="fDiscountType !== 'PERCENTAGE'"
          :suffix="fDiscountType === 'PERCENTAGE' ? '%' : ' ₫'"
          placeholder="0"
          class="app-input tw:w-full"
          @input="(e) => (fDiscountValue = e.value)"
        />
      </div>

      <!-- Buy qty + Get qty (only for BUY_X_GET_Y) -->
      <div v-if="isBuyXGetY" class="tw:grid tw:grid-cols-2 tw:gap-3">
        <div class="tw:space-y-1.5">
          <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">
            Buy quantity <span class="tw:text-red-400">*</span>
          </label>
          <prime-input-number
            v-model="fBuyQuantity"
            :min="1"
            placeholder="2"
            class="app-input tw:w-full"
            @input="(e) => (fBuyQuantity = e.value)"
          />
        </div>
        <div class="tw:space-y-1.5">
          <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">
            Get quantity <span class="tw:text-red-400">*</span>
          </label>
          <prime-input-number
            v-model="fGetQuantity"
            :min="1"
            placeholder="1"
            class="app-input tw:w-full"
            @input="(e) => (fGetQuantity = e.value)"
          />
        </div>
      </div>

      <!-- Applicable product IDs (only for PRODUCT scope) -->
      <div v-if="isProductScope" class="tw:space-y-1.5">
        <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">
          Applicable product IDs
          <span class="tw:normal-case tw:opacity-60">(comma-separated)</span>
        </label>
        <prime-input-text
          v-model="fApplicableProductIds"
          placeholder="1, 2, 5, 12"
          class="app-input tw:w-full"
        />
      </div>

      <!-- Applicable category IDs (only for CATEGORY scope) -->
      <div v-if="isCategoryScope" class="tw:space-y-1.5">
        <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">
          Applicable category IDs
          <span class="tw:normal-case tw:opacity-60">(comma-separated)</span>
        </label>
        <prime-input-text
          v-model="fApplicableCategoryIds"
          placeholder="3, 7"
          class="app-input tw:w-full"
        />
      </div>

      <!-- Min order amount + Max usage -->
      <div class="tw:grid tw:grid-cols-2 tw:gap-3">
        <div class="tw:space-y-1.5">
          <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">
            Min order amount
          </label>
          <prime-input-number
            v-model="fMinOrderAmount"
            :min="0"
            :use-grouping="true"
            suffix=" ₫"
            placeholder="No minimum"
            class="app-input tw:w-full"
            @input="(e) => (fMinOrderAmount = e.value)"
          />
        </div>
        <div class="tw:space-y-1.5">
          <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">
            Max usage
            <span class="tw:normal-case tw:opacity-60">(blank = unlimited)</span>
          </label>
          <prime-input-number
            v-model="fMaxUsage"
            :min="1"
            placeholder="Unlimited"
            class="app-input tw:w-full"
            @input="(e) => (fMaxUsage = e.value)"
          />
        </div>
      </div>

      <!-- Start date + End date -->
      <div class="tw:grid tw:grid-cols-2 tw:gap-3">
        <div class="tw:space-y-1.5">
          <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">
            Start date <span class="tw:text-red-400">*</span>
          </label>
          <prime-date-picker
            v-model="fStartDate"
            date-format="dd/mm/yy"
            show-button-bar
            class="app-input tw:w-full"
          />
        </div>
        <div class="tw:space-y-1.5">
          <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">
            End date
            <span class="tw:normal-case tw:opacity-60">(blank = no expiry)</span>
          </label>
          <prime-date-picker
            v-model="fEndDate"
            date-format="dd/mm/yy"
            show-button-bar
            class="app-input tw:w-full"
          />
        </div>
      </div>

      <!-- Description -->
      <div class="tw:space-y-1.5">
        <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">
          Description
        </label>
        <prime-textarea
          v-model="fDescription"
          :rows="2"
          placeholder="Optional description…"
          class="app-input tw:w-full"
          auto-resize
        />
      </div>

      <!-- Active toggle (edit only) -->
      <div v-if="isEditMode" class="tw:flex tw:items-center tw:justify-between">
        <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">
          Active
        </label>
        <prime-toggle-switch v-model="fIsActive" />
      </div>

      <p v-if="formError" class="tw:text-xs tw:text-red-400">{{ formError }}</p>
    </div>

    <template #footer>
      <prime-button severity="secondary" outlined @click="dialogVisible = false">
        Cancel
      </prime-button>
      <prime-button
        :severity="isEditMode ? 'primary' : 'success'"
        :loading="formLoading"
        @click="submitForm"
      >
        <iconify :icon="isEditMode ? 'ph:check-bold' : 'ph:plus-bold'" />
        <span>{{ isEditMode ? "Save changes" : "Add promotion" }}</span>
      </prime-button>
    </template>
  </prime-dialog>

  <section class="tw:space-y-8">
    <!-- Header -->
    <div class="tw:flex tw:flex-wrap tw:items-end tw:justify-between tw:gap-4">
      <div>
        <p class="tw:text-[11px] tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300">
          Operations
        </p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">Promotions</h1>
        <p class="tw:mt-2 tw:text-sm app-text-muted">
          Manage discount codes and promotional campaigns.
        </p>
      </div>
      <div class="tw:flex tw:items-center tw:gap-2">
        <prime-button
          severity="secondary"
          outlined
          size="small"
          :loading="loading"
          @click="loadPromotions"
        >
          <iconify icon="ph:arrows-clockwise-bold" class="tw:mr-1" />
          <span>Refresh</span>
        </prime-button>
        <prime-button severity="success" size="small" @click="openCreateDialog">
          <iconify icon="ph:plus-bold" class="tw:mr-1" />
          <span>New promotion</span>
        </prime-button>
      </div>
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

    <!-- Table -->
    <AppTable
      lazy
      v-model:first="first"
      v-model:rows="rows"
      :value="promotions"
      :loading="loading"
      :totalRecords="totalRecords"
      :rowsPerPageOptions="[10, 20, 50]"
      :columns="columns"
      @page="onPage"
    >
      <template #toolbar-left>
        <div class="tw:flex tw:items-center tw:gap-2">
          <!-- Filter toggle -->
          <prime-button
            :severity="hasActiveFilters ? 'success' : 'secondary'"
            :outlined="!hasActiveFilters"
            v-tooltip.top="'Filters'"
            @click="filterPanel.toggle($event)"
            :class="!hasActiveFilters ? btnIcon : ''"
          >
            <iconify icon="ph:funnel-bold" />
            <span>Filters</span>
            <prime-badge
              v-if="activeFilterCount > 0"
              :value="activeFilterCount"
              severity="danger"
              class="tw:ml-1 tw:scale-90"
            />
          </prime-button>

          <!-- Filter popover -->
          <prime-popover ref="filterPanel">
            <div class="tw:flex tw:flex-col tw:gap-4 tw:w-60">
              <p class="tw:text-sm tw:font-semibold">Filter promotions</p>

              <!-- Status -->
              <div class="tw:space-y-1.5">
                <label class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest">Status</label>
                <prime-select
                  v-model="isActiveFilter"
                  :options="[{ label: 'Active', value: true }, { label: 'Inactive', value: false }]"
                  option-label="label"
                  option-value="value"
                  placeholder="All"
                  show-clear
                  class="app-input tw:w-full"
                />
              </div>

              <!-- Discount type -->
              <div class="tw:space-y-1.5">
                <label class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest">Discount type</label>
                <prime-select
                  v-model="discountTypeFilter"
                  :options="PROMOTION_DISCOUNT_TYPE_OPTIONS"
                  option-label="label"
                  option-value="value"
                  placeholder="All types"
                  show-clear
                  class="app-input tw:w-full"
                >
                  <template #option="{ option }">
                    <div class="tw:flex tw:items-center tw:gap-2">
                      <iconify :icon="option.icon" />
                      <span>{{ option.label }}</span>
                    </div>
                  </template>
                </prime-select>
              </div>

              <!-- Scope -->
              <div class="tw:space-y-1.5">
                <label class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest">Scope</label>
                <prime-select
                  v-model="scopeFilter"
                  :options="PROMOTION_SCOPE_OPTIONS"
                  option-label="label"
                  option-value="value"
                  placeholder="All scopes"
                  show-clear
                  class="app-input tw:w-full"
                >
                  <template #option="{ option }">
                    <div class="tw:flex tw:items-center tw:gap-2">
                      <iconify :icon="option.icon" />
                      <span>{{ option.label }}</span>
                    </div>
                  </template>
                </prime-select>
              </div>

              <prime-button
                v-if="hasActiveFilters"
                severity="danger"
                outlined
                size="small"
                @click="clearFilters"
              >
                <iconify icon="ph:x-bold" />
                <span>Clear filters</span>
              </prime-button>
            </div>
          </prime-popover>
        </div>
      </template>

      <template #col-name="{ data }">
        <div>
          <p class="tw:font-semibold tw:text-sm">{{ data.name }}</p>
          <p v-if="data.code" class="tw:text-xs tw:font-mono app-text-muted">{{ data.code }}</p>
          <p v-else class="tw:text-xs app-text-subtle tw:italic">auto-apply</p>
        </div>
      </template>

      <template #col-type="{ data }">
        <div class="tw:flex tw:items-center tw:gap-1.5">
          <iconify
            :icon="PROMOTION_DISCOUNT_TYPE_MAP[data.discountType]?.icon ?? 'ph:tag-bold'"
            class="tw:text-sm app-text-muted"
          />
          <prime-tag
            :value="PROMOTION_DISCOUNT_TYPE_MAP[data.discountType]?.label ?? data.discountType"
            :severity="PROMOTION_DISCOUNT_TYPE_MAP[data.discountType]?.severity ?? 'secondary'"
          />
        </div>
      </template>

      <template #col-scope="{ data }">
        <div class="tw:flex tw:items-center tw:gap-1.5">
          <iconify
            :icon="PROMOTION_SCOPE_MAP[data.scope]?.icon ?? 'ph:tag-bold'"
            class="tw:text-sm app-text-muted"
          />
          <prime-tag
            :value="PROMOTION_SCOPE_MAP[data.scope]?.label ?? data.scope"
            :severity="PROMOTION_SCOPE_MAP[data.scope]?.severity ?? 'secondary'"
          />
        </div>
      </template>

      <template #col-discount="{ data }">
        <span class="tw:font-semibold tw:text-sm">{{ discountValueLabel(data) }}</span>
      </template>

      <template #col-validity="{ data }">
        <div class="tw:text-sm">
          <span class="app-text-muted">{{ formatDate(data.startDate) }}</span>
          <span class="app-text-muted tw:mx-1">–</span>
          <span class="app-text-muted">{{ data.endDate ? formatDate(data.endDate) : '∞' }}</span>
        </div>
      </template>

      <template #col-usage="{ data }">
        <span class="tw:text-sm">
          {{ data.currentUsage }}
          <span class="app-text-muted">/ {{ data.maxUsage ?? '∞' }}</span>
        </span>
      </template>

      <template #col-stack="{ data }">
        <prime-tag
          :value="STACK_POLICY_MAP[data.stackPolicy]?.label ?? data.stackPolicy"
          :severity="STACK_POLICY_MAP[data.stackPolicy]?.severity ?? 'secondary'"
        />
      </template>

      <template #col-isActive="{ data }">
        <prime-toggle-switch
          :model-value="data.isActive"
          :disabled="togglingId === data.id"
          @change="handleToggle(data)"
        />
      </template>

      <template #col-actions="{ data }">
        <div class="tw:flex tw:justify-end tw:items-center tw:gap-2">
          <prime-button
            severity="secondary"
            outlined
            size="small"
            v-tooltip.top="'Edit'"
            :class="btnIcon"
            @click="openEditDialog(data)"
          >
            <iconify icon="ph:pencil-bold" />
          </prime-button>
          <prime-button
            severity="danger"
            outlined
            size="small"
            v-tooltip.top="'Delete'"
            :class="btnIcon"
            @click="handleDelete(data)"
          >
            <iconify icon="ph:trash-bold" />
          </prime-button>
        </div>
      </template>
    </AppTable>
  </section>
</template>
