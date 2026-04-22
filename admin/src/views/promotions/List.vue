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
  CODE_VISIBILITY,
  CODE_VISIBILITY_MAP,
  CODE_VISIBILITY_OPTIONS,
} from "@/constants/codeVisibility";
import { btnIcon } from "@/layout/ui";
import { getCategory } from "@/services/category.service";
import { getProductTree } from "@/services/product.service";

// ── Cache ──────────────────────────────────────────────────────────
const { t } = useI18n();
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
  return t('promotions.buyXGetY', { x: promo.buyQuantity ?? 0, y: promo.getQuantity ?? 0 });
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
      err?.response?.data?.message || t('promotions.toast.loadFailed');
  } finally {
    loading.value = false;
  }
};

onMounted(() => {
  const cached = restoreCache();
  if (cached) {
    if (cached.rows !== undefined)               rows.value               = cached.rows;
    if (cached.first !== undefined)              first.value              = cached.first;
    if (cached.isActiveFilter !== undefined)     isActiveFilter.value     = cached.isActiveFilter;
    if (cached.scopeFilter !== undefined)        scopeFilter.value        = cached.scopeFilter;
    if (cached.discountTypeFilter !== undefined) discountTypeFilter.value = cached.discountTypeFilter;
  }
  loadPromotions();
});

onBeforeRouteLeave(() => {
  saveCurrentState();
});

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

// ── Lookup data — lazy loaded by scope ────────────────────────────
// Category options (for CATEGORY scope + BUY_X_GET_Y GetFrom)
const categoryOptions   = ref([]);
const categoryLoaded    = ref(false);
const categoryLoading   = ref(false);

const loadCategoryOptions = async () => {
  if (categoryLoaded.value || categoryLoading.value) return;
  categoryLoading.value = true;
  try {
    const res = await getCategory();
    const d = res.data;
    categoryOptions.value = Array.isArray(d) ? d : (d?.items ?? []);
    categoryLoaded.value  = true;
  } catch { /* silent */ }
  finally { categoryLoading.value = false; }
};

// Product tree (for PRODUCT scope + BUY_X_GET_Y GetFrom products)
const productTreeNodes  = ref([]);
const productTreeLoaded = ref(false);
const productTreeLoading = ref(false);

const loadProductTree = async () => {
  if (productTreeLoaded.value || productTreeLoading.value) return;
  productTreeLoading.value = true;
  try {
    const res = await getProductTree();
    const categories = res.data ?? [];
    productTreeNodes.value = categories.map(cat => ({
      key:      `cat-${cat.id}`,
      label:    cat.name,
      children: cat.products.map(p => ({ key: String(p.id), label: p.name })),
    }));
    productTreeLoaded.value = true;
  } catch { /* silent */ }
  finally { productTreeLoading.value = false; }
};

// Convert array of IDs → TreeSelect selection object
const idsToTreeSel = (ids) => {
  const sel = {};
  ids.forEach(id => { sel[String(id)] = { checked: true, partialChecked: false }; });
  productTreeNodes.value.forEach(cat => {
    const total   = cat.children.length;
    const checked = cat.children.filter(c => sel[c.key]?.checked).length;
    if (checked === total && total > 0)
      sel[cat.key] = { checked: true,  partialChecked: false };
    else if (checked > 0)
      sel[cat.key] = { checked: false, partialChecked: true  };
  });
  return sel;
};

// Extract product IDs from TreeSelect selection (skip cat- keys)
const treeSelToIds = (sel) =>
  Object.entries(sel)
    .filter(([k, v]) => v.checked && !k.startsWith('cat-'))
    .map(([k]) => parseInt(k));

// TreeSelect v-models for product fields
const fApplicableProductTree = ref({});
const fGetFromProductTree     = ref({});

// Form fields
const fName              = ref("");
const fCode              = ref("");
const fCodeVisibility    = ref(CODE_VISIBILITY.PUBLIC);
const fDescription       = ref("");
const fDiscountType      = ref(PROMOTION_DISCOUNT_TYPE.PERCENTAGE);
const fDiscountValue     = ref(null);
const fMaxDiscountAmount = ref(null);
const fBuyQuantity       = ref(null);
const fGetQuantity       = ref(null);
const fScope             = ref(PROMOTION_SCOPE.ORDER);
const fApplicableProductIds  = ref([]);
const fApplicableCategoryIds = ref([]);
const fGetFromProductIds     = ref([]);
const fGetFromCategoryIds    = ref([]);
const fMinOrderAmount    = ref(null);
const fStartDate         = ref(new Date());
const fEndDate           = ref(null);
const fMaxUsage          = ref(null);
const fIsActive          = ref(true);

const isEditMode  = computed(() => editingPromotion.value !== null);
const dialogHeader = computed(() =>
  isEditMode.value ? t('promotions.dialog.editHeader') : t('promotions.dialog.createHeader')
);

const isBuyXGetY      = computed(() => fDiscountType.value === PROMOTION_DISCOUNT_TYPE.BUY_X_GET_Y);
const isPercentage    = computed(() => fDiscountType.value === PROMOTION_DISCOUNT_TYPE.PERCENTAGE);
const isProductScope  = computed(() => fScope.value === PROMOTION_SCOPE.PRODUCT);
const isCategoryScope = computed(() => fScope.value === PROMOTION_SCOPE.CATEGORY);

// Gift source options for BUY_X_GET_Y
const GIFT_SOURCE = { SAME: 'SAME', SEPARATE: 'SEPARATE' };
const GIFT_SOURCE_OPTIONS = computed(() => [
  { label: t('promotions.form.giftSource.same'),     value: GIFT_SOURCE.SAME,     icon: 'ph:repeat-bold' },
  { label: t('promotions.form.giftSource.separate'), value: GIFT_SOURCE.SEPARATE, icon: 'ph:shuffle-bold' },
]);
const fGiftSource = ref(GIFT_SOURCE.SAME);

const resetForm = () => {
  fName.value              = "";
  fCode.value              = "";
  fCodeVisibility.value    = CODE_VISIBILITY.PUBLIC;
  fDescription.value       = "";
  fDiscountType.value      = PROMOTION_DISCOUNT_TYPE.PERCENTAGE;
  fDiscountValue.value     = null;
  fMaxDiscountAmount.value = null;
  fBuyQuantity.value       = null;
  fGetQuantity.value       = null;
  fScope.value             = PROMOTION_SCOPE.ORDER;
  fApplicableProductIds.value  = [];
  fApplicableProductTree.value = {};
  fApplicableCategoryIds.value = [];
  fGetFromProductIds.value     = [];
  fGetFromProductTree.value    = {};
  fGetFromCategoryIds.value    = [];
  fGiftSource.value            = GIFT_SOURCE.SAME;
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
  // No pre-load needed — default scope is ORDER (no tree/category selects visible)
  dialogVisible.value = true;
};

const openEditDialog = (promo) => {
  editingPromotion.value = promo;
  fName.value              = promo.name;
  fCode.value              = promo.code ?? "";
  fCodeVisibility.value    = promo.codeVisibility ?? CODE_VISIBILITY.PUBLIC;
  fDescription.value       = promo.description ?? "";
  fDiscountType.value      = promo.discountType;
  fDiscountValue.value     = promo.discountValue ?? null;
  fMaxDiscountAmount.value = promo.maxDiscountAmount ?? null;
  fBuyQuantity.value       = promo.buyQuantity ?? null;
  fGetQuantity.value       = promo.getQuantity ?? null;
  fScope.value             = promo.scope;
  fApplicableProductIds.value  = promo.applicableProductIds ?? [];
  fApplicableCategoryIds.value = promo.applicableCategoryIds ?? [];
  fGetFromProductIds.value     = promo.getFromProductIds ?? [];
  fGetFromCategoryIds.value    = promo.getFromCategoryIds ?? [];
  fGiftSource.value = (fGetFromProductIds.value.length > 0 || fGetFromCategoryIds.value.length > 0)
    ? GIFT_SOURCE.SEPARATE
    : GIFT_SOURCE.SAME;
  fMinOrderAmount.value    = promo.minOrderAmount ?? null;
  fStartDate.value         = new Date(promo.startDate);
  fEndDate.value           = promo.endDate ? new Date(promo.endDate) : null;
  fMaxUsage.value          = promo.maxUsage ?? null;
  fIsActive.value          = promo.isActive;
  formError.value          = "";

  const needsTree = promo.scope === PROMOTION_SCOPE.PRODUCT
    || promo.discountType === PROMOTION_DISCOUNT_TYPE.BUY_X_GET_Y;
  const needsCategories = promo.scope === PROMOTION_SCOPE.CATEGORY
    || promo.discountType === PROMOTION_DISCOUNT_TYPE.BUY_X_GET_Y;

  if (needsTree) {
    loadProductTree().then(() => {
      fApplicableProductTree.value = idsToTreeSel(fApplicableProductIds.value);
      fGetFromProductTree.value    = idsToTreeSel(fGetFromProductIds.value);
    });
  }
  if (needsCategories) loadCategoryOptions();

  dialogVisible.value = true;
};

// Watch scope/type changes inside the open dialog to lazy-load
watch(fScope, (scope) => {
  if (!dialogVisible.value) return;
  if (scope === PROMOTION_SCOPE.PRODUCT) loadProductTree();
  if (scope === PROMOTION_SCOPE.CATEGORY) loadCategoryOptions();
});

watch(isBuyXGetY, (val) => {
  if (!dialogVisible.value || !val) return;
  // Always pre-load lookups so they're ready if user switches to SEPARATE
  loadProductTree();
  loadCategoryOptions();
});

watch(fGiftSource, (newVal) => {
  if (!dialogVisible.value || !isBuyXGetY.value) return;
  if (newVal === GIFT_SOURCE.SEPARATE) {
    loadProductTree();
    loadCategoryOptions();
  } else {
    // Clear GetFrom selections when switching back to SAME
    fGetFromProductIds.value  = [];
    fGetFromProductTree.value = {};
    fGetFromCategoryIds.value = [];
  }
});

const submitForm = async () => {
  formError.value = "";
  if (!fName.value.trim()) {
    formError.value = t('promotions.form.error.nameRequired');
    return;
  }
  if (!isBuyXGetY.value && (fDiscountValue.value === null || fDiscountValue.value < 0)) {
    formError.value = t('promotions.form.error.discountValueRequired');
    return;
  }
  if (isBuyXGetY.value && (!fBuyQuantity.value || !fGetQuantity.value)) {
    formError.value = t('promotions.form.error.buyGetQuantityRequired');
    return;
  }

  formLoading.value = true;
  try {
    const payload = {
      name:              fName.value.trim(),
      code:              fCode.value.trim() || null,
      codeVisibility:    fCodeVisibility.value,
      description:       fDescription.value.trim() || null,
      discountType:      fDiscountType.value,
      discountValue:     fDiscountValue.value ?? 0,
      maxDiscountAmount: fMaxDiscountAmount.value ?? null,
      buyQuantity:       fBuyQuantity.value ?? null,
      getQuantity:       fGetQuantity.value ?? null,
      getFromProductIds:   fGiftSource.value === GIFT_SOURCE.SEPARATE ? treeSelToIds(fGetFromProductTree.value) : null,
      getFromCategoryIds:  fGiftSource.value === GIFT_SOURCE.SEPARATE ? fGetFromCategoryIds.value : null,
      scope:             fScope.value,
      applicableProductIds:  treeSelToIds(fApplicableProductTree.value),
      applicableCategoryIds: fApplicableCategoryIds.value,
      minOrderAmount:    fMinOrderAmount.value ?? null,
      startDate:         fStartDate.value?.toISOString?.() ?? fStartDate.value,
      endDate:           fEndDate.value?.toISOString?.() ?? null,
      maxUsage:          fMaxUsage.value ?? null,
      isActive:          fIsActive.value,
    };

    if (isEditMode.value) {
      await updatePromotion(editingPromotion.value.id, payload);
      toast.add({ severity: "success", summary: t('promotions.toast.updated'), life: 3000 });
    } else {
      await createPromotion(payload);
      toast.add({ severity: "success", summary: t('promotions.toast.created'), life: 3000 });
    }

    dialogVisible.value = false;
    first.value = 0;
    await loadPromotions();
  } catch (err) {
    formError.value =
      err?.response?.data?.errors?.join(", ") ||
      err?.response?.data?.title ||
      t('promotions.form.error.saveFailed');
  } finally {
    formLoading.value = false;
  }
};

// ── Column definitions ────────────────────────────────────────────
const columns = computed(() => [
  { field: 'name',     header: t('promotions.col.name'),       width: '14rem' },
  { key: 'type',       header: t('promotions.col.type'),       width: '10rem' },
  { key: 'scope',      header: t('promotions.col.scope'),      width: '10rem' },
  { key: 'discount',   header: t('promotions.col.discount'),   width: '9rem' },
  { key: 'validity',   header: t('promotions.col.validity'),   width: '12rem' },
  { key: 'usage',      header: t('promotions.col.usage'),      width: '7rem' },
  { key: 'visibility', header: t('promotions.col.visibility'), width: '8rem' },
  { key: 'isActive',   header: t('promotions.col.active'),     width: '6rem' },
  { key: 'actions',    header: t('promotions.col.actions'),    width: '8rem', toggleable: false },
]);

// ── Mobile drawer ──────────────────────────────────────────────────
const drawerPromo = ref(null);
const drawerVisible = ref(false);
const openDrawer = (row) => { drawerPromo.value = row; drawerVisible.value = true; };

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
      summary: updated?.isActive ? t('promotions.toast.activated') : t('promotions.toast.deactivated'),
      life: 2500,
    });
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.title || t('promotions.toast.toggleFailed');
  } finally {
    togglingId.value = null;
  }
};

// ── Delete ────────────────────────────────────────────────────────
const handleDelete = (promo) => {
  confirm.require({
    message: t('promotions.confirm.deleteMessage', { name: promo.name }),
    header:  t('promotions.confirm.deleteHeader'),
    acceptSeverity: "danger",
    acceptLabel:    t('promotions.confirm.deleteAccept'),
    rejectLabel:    t('promotions.confirm.deleteReject'),
    accept: async () => {
      try {
        await deletePromotion(promo.id);
        toast.add({ severity: "success", summary: t('promotions.toast.deleted'), life: 3000 });
        if (promotions.value.length === 1 && first.value > 0) {
          first.value = Math.max(0, first.value - rows.value);
        }
        await loadPromotions();
      } catch (err) {
        errorMessage.value =
          err?.response?.data?.title || t('promotions.toast.deleteFailed');
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
        <label class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
          {{ t('promotions.form.name') }} <span class="tw:text-red-400">*</span>
        </label>
        <prime-input-text
          v-model="fName"
          :placeholder="t('promotions.form.namePlaceholder')"
          class="app-input tw:w-full"
        />
      </div>

      <!-- Code + Visibility -->
      <div class="tw:grid tw:grid-cols-2 tw:gap-3">
        <div class="tw:space-y-1.5">
          <label class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
            {{ t('promotions.form.code') }}
            <span class="tw:normal-case tw:opacity-60">{{ t('promotions.form.codeHint') }}</span>
          </label>
          <prime-input-text
            v-model="fCode"
            :placeholder="t('promotions.form.codePlaceholder')"
            class="app-input tw:w-full tw:uppercase"
          />
        </div>
        <div class="tw:space-y-1.5">
          <label class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
            {{ t('promotions.form.visibility') }}
          </label>
          <prime-select
            v-model="fCodeVisibility"
            :options="CODE_VISIBILITY_OPTIONS"
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
                <iconify :icon="CODE_VISIBILITY_MAP[value]?.icon" />
                <span>{{ CODE_VISIBILITY_MAP[value]?.label }}</span>
              </div>
            </template>
          </prime-select>
        </div>
      </div>

      <!-- Discount type + Scope -->
      <div class="tw:grid tw:grid-cols-2 tw:gap-3">
        <div class="tw:space-y-1.5">
          <label class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
            {{ t('promotions.form.discountType') }}
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
          <label class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
            {{ t('promotions.form.scope') }}
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
        <label class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
          {{ t('promotions.form.discountValue') }} <span class="tw:text-red-400">*</span>
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

      <!-- Max discount amount (only for PERCENTAGE) -->
      <div v-if="isPercentage" class="tw:space-y-1.5">
        <label class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
          {{ t('promotions.form.maxDiscountAmount') }}
          <span class="tw:normal-case tw:opacity-60">{{ t('promotions.form.maxDiscountAmountHint') }}</span>
        </label>
        <prime-input-number
          v-model="fMaxDiscountAmount"
          :min="0"
          :use-grouping="true"
          suffix=" ₫"
          :placeholder="t('promotions.form.noCap')"
          class="app-input tw:w-full"
          @input="(e) => (fMaxDiscountAmount = e.value)"
        />
      </div>

      <!-- Buy qty + Get qty (only for BUY_X_GET_Y) -->
      <div v-if="isBuyXGetY" class="tw:grid tw:grid-cols-2 tw:gap-3">
        <div class="tw:space-y-1.5">
          <label class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
            {{ t('promotions.form.buyQuantity') }} <span class="tw:text-red-400">*</span>
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
          <label class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
            {{ t('promotions.form.getQuantity') }} <span class="tw:text-red-400">*</span>
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

      <!-- Applicable products (only for PRODUCT scope) -->
      <div v-show="isProductScope" class="tw:space-y-1.5">
        <label class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
          {{ t('promotions.form.applicableProducts') }}
        </label>
        <prime-tree-select
          v-model="fApplicableProductTree"
          :options="productTreeNodes"
          selection-mode="checkbox"
          :placeholder="t('promotions.form.selectProducts')"
          class="app-input tw:w-full"
        />
      </div>

      <!-- Applicable categories (only for CATEGORY scope) -->
      <div v-if="isCategoryScope" class="tw:space-y-1.5">
        <label class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
          {{ t('promotions.form.applicableCategories') }}
        </label>
        <prime-multi-select
          v-model="fApplicableCategoryIds"
          :options="categoryOptions"
          option-label="name"
          option-value="id"
          :placeholder="t('promotions.form.selectCategories')"
          filter
          display="chip"
          class="app-input tw:w-full"
        />
      </div>

      <!-- Gift source classification (only for BUY_X_GET_Y) -->
      <div v-if="isBuyXGetY" class="tw:space-y-3">
        <div class="tw:space-y-1.5">
          <label class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
            {{ t('promotions.form.freeItem') }}
          </label>
          <prime-select-button
            v-model="fGiftSource"
            :options="GIFT_SOURCE_OPTIONS"
            option-label="label"
            option-value="value"
            class="tw:w-full"
          >
            <template #option="{ option }">
              <div class="tw:flex tw:items-center tw:gap-1.5">
                <iconify :icon="option.icon" class="tw:text-base" />
                <span class="tw:text-xs">{{ option.label }}</span>
              </div>
            </template>
          </prime-select-button>
        </div>

        <!-- GetFrom selectors — only shown when SEPARATE -->
        <template v-if="fGiftSource === 'SEPARATE'">
          <div class="tw:space-y-1.5">
            <label class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
              {{ t('promotions.form.freeItemFromProducts') }}
            </label>
            <prime-tree-select
              v-model="fGetFromProductTree"
              :options="productTreeNodes"
              selection-mode="checkbox"
              :placeholder="t('promotions.form.selectProducts')"
              class="app-input tw:w-full"
            />
          </div>
          <div class="tw:space-y-1.5">
            <label class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
              {{ t('promotions.form.freeItemFromCategories') }}
            </label>
            <prime-multi-select
              v-model="fGetFromCategoryIds"
              :options="categoryOptions"
              option-label="name"
              option-value="id"
              :placeholder="t('promotions.form.selectCategories')"
              filter
              display="chip"
              class="app-input tw:w-full"
            />
          </div>
        </template>
      </div>

      <!-- Min order amount + Max usage -->
      <div class="tw:grid tw:grid-cols-2 tw:gap-3">
        <div class="tw:space-y-1.5">
          <label class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
            {{ t('promotions.form.minOrderAmount') }}
          </label>
          <prime-input-number
            v-model="fMinOrderAmount"
            :min="0"
            :use-grouping="true"
            suffix=" ₫"
            :placeholder="t('promotions.form.noMinimum')"
            class="app-input tw:w-full"
            @input="(e) => (fMinOrderAmount = e.value)"
          />
        </div>
        <div class="tw:space-y-1.5">
          <label class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
            {{ t('promotions.form.maxUsage') }}
            <span class="tw:normal-case tw:opacity-60">{{ t('promotions.form.maxUsageHint') }}</span>
          </label>
          <prime-input-number
            v-model="fMaxUsage"
            :min="1"
            :placeholder="t('promotions.form.unlimited')"
            class="app-input tw:w-full"
            @input="(e) => (fMaxUsage = e.value)"
          />
        </div>
      </div>

      <!-- Start date + End date -->
      <div class="tw:grid tw:grid-cols-2 tw:gap-3">
        <div class="tw:space-y-1.5">
          <label class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
            {{ t('promotions.form.startDate') }} <span class="tw:text-red-400">*</span>
          </label>
          <prime-date-picker
            v-model="fStartDate"
            date-format="dd/mm/yy"
            show-button-bar
            class="app-input tw:w-full"
          />
        </div>
        <div class="tw:space-y-1.5">
          <label class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
            {{ t('promotions.form.endDate') }}
            <span class="tw:normal-case tw:opacity-60">{{ t('promotions.form.endDateHint') }}</span>
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
        <label class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
          {{ t('promotions.form.description') }}
        </label>
        <prime-textarea
          v-model="fDescription"
          :rows="2"
          :placeholder="t('promotions.form.descriptionPlaceholder')"
          class="app-input tw:w-full"
          auto-resize
        />
      </div>

      <!-- Active toggle (edit only) -->
      <div v-if="isEditMode" class="tw:flex tw:items-center tw:justify-between">
        <label class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
          {{ t('promotions.form.activeLabel') }}
        </label>
        <prime-toggle-switch v-model="fIsActive" />
      </div>

      <p v-if="formError" class="tw:text-xs tw:text-red-400">{{ formError }}</p>
    </div>

    <template #footer>
      <prime-button severity="secondary" outlined @click="dialogVisible = false">
        {{ t('common.cancel') }}
      </prime-button>
      <prime-button
        :severity="isEditMode ? 'primary' : 'success'"
        :loading="formLoading"
        @click="submitForm"
      >
        <iconify :icon="isEditMode ? 'ph:check-bold' : 'ph:plus-bold'" />
        <span>{{ isEditMode ? t('promotions.form.saveChanges') : t('promotions.form.addPromotion') }}</span>
      </prime-button>
    </template>
  </prime-dialog>

  <section class="tw:space-y-8">
    <!-- Header -->
    <div class="tw:flex tw:flex-wrap tw:items-end tw:justify-between tw:gap-4">
      <div>
        <p class="tw:text-[11px] tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300">
          {{ t('promotions.breadcrumb') }}
        </p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">{{ t('promotions.title') }}</h1>
        <p class="tw:mt-2 tw:text-sm tw:text-muted">{{ t('promotions.subtitle') }}</p>
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
          <span>{{ t('promotions.refresh') }}</span>
        </prime-button>
        <prime-button severity="success" size="small" @click="openCreateDialog">
          <iconify icon="ph:plus-bold" class="tw:mr-1" />
          <span>{{ t('promotions.newPromotion') }}</span>
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
            v-tooltip.top="t('promotions.filter.filtersTooltip')"
            @click="filterPanel.toggle($event)"
            :class="btnIcon"
          >
            <span class="tw:relative tw:inline-flex">
              <iconify icon="ph:funnel-bold" />
              <prime-badge
                v-if="activeFilterCount > 0"
                :value="activeFilterCount"
                severity="danger"
                class="tw:absolute! tw:-top-2.5! tw:-right-2.5! tw:scale-75! tw:origin-top-right!"
              />
            </span>
          </prime-button>

          <!-- Filter popover -->
          <prime-popover ref="filterPanel">
            <div class="tw:flex tw:flex-col tw:gap-4 tw:w-60">
              <p class="tw:text-sm tw:font-semibold">{{ t('promotions.filter.title') }}</p>

              <!-- Status -->
              <div class="tw:space-y-1.5">
                <label class="tw:text-xs tw:text-muted tw:uppercase tw:tracking-widest">{{ t('promotions.filter.status') }}</label>
                <prime-select
                  v-model="isActiveFilter"
                  :options="[{ label: t('promotions.tag.active'), value: true }, { label: t('promotions.tag.inactive'), value: false }]"
                  option-label="label"
                  option-value="value"
                  :placeholder="t('promotions.filter.allStatuses')"
                  show-clear
                  class="app-input tw:w-full"
                />
              </div>

              <!-- Discount type -->
              <div class="tw:space-y-1.5">
                <label class="tw:text-xs tw:text-muted tw:uppercase tw:tracking-widest">{{ t('promotions.filter.discountType') }}</label>
                <prime-select
                  v-model="discountTypeFilter"
                  :options="PROMOTION_DISCOUNT_TYPE_OPTIONS"
                  option-label="label"
                  option-value="value"
                  :placeholder="t('promotions.filter.allTypes')"
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
                <label class="tw:text-xs tw:text-muted tw:uppercase tw:tracking-widest">{{ t('promotions.filter.scope') }}</label>
                <prime-select
                  v-model="scopeFilter"
                  :options="PROMOTION_SCOPE_OPTIONS"
                  option-label="label"
                  option-value="value"
                  :placeholder="t('promotions.filter.allScopes')"
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
                <span>{{ t('promotions.filter.clearFilters') }}</span>
              </prime-button>
            </div>
          </prime-popover>
        </div>
      </template>

      <template #mobile-card="{ data }">
        <div class="tw:rounded-xl tw:border tw:border-slate-200 tw:dark:border-white/10 tw:bg-white tw:dark:bg-white/5 tw:p-3 tw:flex tw:flex-col tw:gap-2">
          <div class="tw:flex tw:items-start tw:justify-between tw:gap-1">
            <span class="tw:font-semibold tw:text-sm tw:leading-snug">{{ data.name }}</span>
            <prime-tag
              :value="data.isActive ? t('promotions.tag.active') : t('promotions.tag.inactive')"
              :severity="data.isActive ? 'success' : 'danger'"
              class="tw:text-[11px]! tw:px-1.5! tw:py-0.5! tw:flex-shrink-0"
            />
          </div>
          <p v-if="data.code" class="tw:text-xs tw:font-mono tw:text-muted">{{ data.code }}</p>
          <p class="tw:text-xs tw:text-muted">{{ discountValueLabel(data) }}</p>
          <div class="tw:border-t tw:border-slate-200 tw:dark:border-white/10 tw:pt-2">
            <prime-button severity="secondary" outlined size="small" fluid @click="openDrawer(data)">
              <iconify icon="ph:dots-three-bold" />
              <span>{{ t('common.moreActions') }}</span>
            </prime-button>
          </div>
        </div>
      </template>

      <template #col-name="{ data }">
        <div>
          <p class="tw:font-semibold tw:text-sm">{{ data.name }}</p>
          <p class="tw:text-xs tw:font-mono tw:text-muted">{{ data.code }}</p>
        </div>
      </template>

      <template #col-type="{ data }">
        <div class="tw:flex tw:items-center tw:gap-1.5">
          <iconify
            :icon="PROMOTION_DISCOUNT_TYPE_MAP[data.discountType]?.icon ?? 'ph:tag-bold'"
            class="tw:text-sm tw:text-muted"
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
            class="tw:text-sm tw:text-muted"
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
          <span class="tw:text-muted">{{ formatDate(data.startDate) }}</span>
          <span class="tw:text-muted tw:mx-1">–</span>
          <span class="tw:text-muted">{{ data.endDate ? formatDate(data.endDate) : '∞' }}</span>
        </div>
      </template>

      <template #col-usage="{ data }">
        <span class="tw:text-sm">
          {{ data.currentUsage }}
          <span class="tw:text-muted">/ {{ data.maxUsage ?? '∞' }}</span>
        </span>
      </template>

      <template #col-visibility="{ data }">
        <div class="tw:flex tw:items-center tw:gap-1.5">
          <iconify
            :icon="CODE_VISIBILITY_MAP[data.codeVisibility]?.icon ?? 'ph:eye-bold'"
            class="tw:text-sm tw:text-muted"
          />
          <prime-tag
            :value="CODE_VISIBILITY_MAP[data.codeVisibility]?.label ?? data.codeVisibility"
            :severity="CODE_VISIBILITY_MAP[data.codeVisibility]?.severity ?? 'secondary'"
          />
        </div>
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
            v-tooltip.top="t('common.edit')"
            :class="btnIcon"
            @click="openEditDialog(data)"
          >
            <iconify icon="ph:pencil-bold" />
          </prime-button>
          <prime-button
            severity="danger"
            outlined
            size="small"
            v-tooltip.top="t('common.delete')"
            :class="btnIcon"
            @click="handleDelete(data)"
          >
            <iconify icon="ph:trash-bold" />
          </prime-button>
        </div>
      </template>
    </AppTable>

    <!-- ── Mobile action drawer ───────────────────────────────────── -->
    <prime-drawer
      v-model:visible="drawerVisible"
      position="bottom"
      :style="{ height: 'auto' }"
      :pt="{ root: { class: 'tw:rounded-t-2xl' } }"
    >
      <template #header>
        <div class="tw:flex tw:items-center tw:gap-2">
          <span class="tw:font-medium">{{ drawerPromo?.name }}</span>
          <prime-tag
            v-if="drawerPromo"
            :value="drawerPromo.isActive ? t('promotions.tag.active') : t('promotions.tag.inactive')"
            :severity="drawerPromo.isActive ? 'success' : 'danger'"
            class="tw:text-[11px]! tw:px-1.5! tw:py-0.5!"
          />
        </div>
      </template>
      <div v-if="drawerPromo" class="tw:flex tw:flex-col tw:gap-2 tw:pb-4">
        <prime-button
          :label="drawerPromo.isActive ? t('promotions.drawer.deactivate') : t('promotions.drawer.activate')"
          :severity="drawerPromo.isActive ? 'danger' : 'success'"
          outlined fluid
          :loading="togglingId === drawerPromo.id"
          @click="handleToggle(drawerPromo); drawerVisible = false"
        >
          <template #icon>
            <iconify :icon="drawerPromo.isActive ? 'ph:toggle-left-bold' : 'ph:toggle-right-bold'" />
          </template>
        </prime-button>
        <prime-button :label="t('common.edit')" severity="secondary" outlined fluid @click="openEditDialog(drawerPromo); drawerVisible = false">
          <template #icon><iconify icon="ph:pencil-bold" /></template>
        </prime-button>
        <prime-button :label="t('common.delete')" severity="danger" outlined fluid @click="handleDelete(drawerPromo); drawerVisible = false">
          <template #icon><iconify icon="ph:trash-bold" /></template>
        </prime-button>
      </div>
    </prime-drawer>
  </section>
</template>
