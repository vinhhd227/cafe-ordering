<script setup>
import { computed, onBeforeUnmount, onMounted, ref, watch } from "vue";
import { useRouter, onBeforeRouteLeave } from "vue-router";
import { usePermission } from "@/composables/usePermission";
import { getProducts, toggleProductActive } from "@/services/product.service";
import { getCategory } from "@/services/category.service";
import AppTable from "@/components/AppTable.vue";
import WidgetSettingsButton from "@/components/widgets/WidgetSettingsButton.vue";
import { useTableCache } from "@/composables/useTableCache";
import { btnIcon } from "@/layout/ui";

const { t } = useI18n();
const cache = useTableCache("products");

const router = useRouter();
const { can } = usePermission();

// ── Table state ─────────────────────────────────────────────────────
const loading = ref(false);
const errorMessage = ref("");
const products = ref([]);
const rows = ref(10);
const first = ref(0);

// ── Column visibility ────────────────────────────────────────────────
const buildColDefs = () => [
  { field: 'id',       header: t('products.list.col.id'),       width: '4rem',  visible: true },
  { key: 'product',    header: t('products.list.col.product'),  width: '14rem', visible: true },
  { field: 'category', header: t('products.list.col.category'),                 visible: true },
  { field: 'price',    header: t('products.list.col.price'),                    visible: true },
  { field: 'status',   header: t('products.list.col.status'),                   visible: true },
  { key: 'actions',    header: t('products.list.col.actions'),  width: '12rem', toggleable: false },
];
const colDefs = ref(buildColDefs());
const totalRecords = ref(0);
const summary = ref({ total: 0, active: 0, low: 0, inactive: 0 });
const searchTimer = ref(null);

// ── Filters ─────────────────────────────────────────────────────────
const search = ref("");
const categoryFilter = ref(null);
const statusFilter = ref(null);
const minPrice = ref(null);
const maxPrice = ref(null);
const categories = ref([]);

const statusOptions = computed(() => [
  { label: t('products.status.active'),   value: true },
  { label: t('products.status.inactive'), value: false },
]);

const filterPanel = ref(null);

const activeFilterCount = computed(() => {
  let n = 0;
  if (categoryFilter.value !== null) n++;
  if (statusFilter.value !== null) n++;
  if (minPrice.value !== null) n++;
  if (maxPrice.value !== null) n++;
  return n;
});

const hasActiveFilters = computed(() => activeFilterCount.value > 0);

const clearFilters = () => {
  categoryFilter.value = null;
  statusFilter.value = null;
  minPrice.value = null;
  maxPrice.value = null;
  first.value = 0;
};

const statusTag = (status) => {
  switch (status) {
    case "active":
      return { label: t('products.status.active'),   severity: "success" };
    case "inactive":
      return { label: t('products.status.inactive'), severity: "danger" };
    default:
      return { label: status, severity: "info" };
  }
};

const formatVnd = (value) =>
  new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
    maximumFractionDigits: 0,
  }).format(value ?? 0);

const loadStats = async () => {
  try {
    const term = search.value.trim() || undefined;
    const [activeRes, inactiveRes] = await Promise.all([
      getProducts({ page: 1, pageSize: 1, searchTerm: term, isActive: true }),
      getProducts({ page: 1, pageSize: 1, searchTerm: term, isActive: false }),
    ]);
    summary.value = {
      total: summary.value.total,
      active: activeRes?.data?.pagedInfo?.totalRecords ?? 0,
      low: 0,
      inactive: inactiveRes?.data?.pagedInfo?.totalRecords ?? 0,
    };
  } catch {
    /* non-critical */
  }
};

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

const loadProducts = async (page = 1) => {
  loading.value = true;
  errorMessage.value = "";
  try {
    const res = await getProducts({
      page,
      pageSize: rows.value,
      searchTerm: search.value.trim() || undefined,
      isActive: statusFilter.value ?? undefined,
      categoryId: categoryFilter.value ?? undefined,
      minPrice: minPrice.value ?? undefined,
      maxPrice: maxPrice.value ?? undefined,
    });
    const paged = res?.data ?? {};
    products.value =
      paged.value?.map((p) => ({
        id: p.id,
        name: p.name,
        price: p.price,
        imageUrl: p.imageUrl,
        status: p.isActive ? "active" : "inactive",
        category: p.categoryName,
      })) ?? [];
    const total = paged.pagedInfo?.totalRecords ?? 0;
    totalRecords.value = total;
    summary.value = { ...summary.value, total };
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.message || t('products.list.error');
  } finally {
    loading.value = false;
  }
};

const handleToggleActive = async (row) => {
  try {
    await toggleProductActive(row.id);
    const page = rows.value > 0 ? Math.floor(first.value / rows.value) + 1 : 1;
    await loadProducts(page);
    loadStats();
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.title ?? t('products.list.error');
  }
};

onMounted(() => {
  loadCategories();
  const cached = cache.restore();
  if (cached) {
    search.value = cached.search ?? "";
    rows.value = cached.rows ?? 10;
    first.value = cached.first ?? 0;
    categoryFilter.value = cached.categoryFilter ?? null;
    statusFilter.value = cached.statusFilter ?? null;
    minPrice.value = cached.minPrice ?? null;
    maxPrice.value = cached.maxPrice ?? null;
    if (cached.colDefs) {
      const cachedMap = Object.fromEntries(
        cached.colDefs.map(c => [c.key ?? c.field, c])
      );
      colDefs.value = colDefs.value.map(col => {
        if (col.toggleable === false) return col;
        const id = col.key ?? col.field;
        const cached = cachedMap[id];
        return cached ? { ...col, visible: cached.visible } : col;
      });
    }
    const page = rows.value > 0 ? Math.floor(first.value / rows.value) + 1 : 1;
    loadProducts(page);
  } else {
    loadProducts(1);
  }
  loadStats();
});

onBeforeRouteLeave(() => {
  cache.save({
    search: search.value,
    rows: rows.value,
    first: first.value,
    categoryFilter: categoryFilter.value,
    statusFilter: statusFilter.value,
    minPrice: minPrice.value,
    maxPrice: maxPrice.value,
    colDefs: colDefs.value,
  });
});

// Debounce cho text/number inputs; select thay đổi ngay lập tức
watch([search, minPrice, maxPrice], () => {
  clearTimeout(searchTimer.value);
  searchTimer.value = setTimeout(() => {
    first.value = 0;
    loadProducts(1);
  }, 400);
});

onBeforeUnmount(() => {
  clearTimeout(searchTimer.value);
});

watch([categoryFilter, statusFilter], () => {
  first.value = 0;
  loadProducts(1);
});

// ── Mobile action drawer ───────────────────────────────────────────
const drawerProduct = ref(null);
const drawerVisible = ref(false);

const openDrawer = (row) => {
  drawerProduct.value = row;
  drawerVisible.value = true;
};

// ── Widget visibility ──────────────────────────────────────────────
const { isVisible: wVisible, toggle: wToggle, hiddenCount: wHidden, widgets: wDefs, colsPerRow: wCols, setColsPerRow: wSetCols } =
  useWidgetSettings('products', [
    { id: 'total',    label: t('products.widgets.total.label'),    preview: '48', description: t('products.widgets.total.description') },
    { id: 'active',   label: t('products.widgets.active.label'),   preview: '36', description: t('products.widgets.active.description'),   labelClass: 'tw:text-primary-400' },
    { id: 'low',      label: t('products.widgets.low.label'),      preview: '4',  description: t('products.widgets.low.description') },
    { id: 'inactive', label: t('products.widgets.inactive.label'), preview: '8',  description: t('products.widgets.inactive.description'), labelClass: 'tw:text-red-400' },
  ], { defaultCols: 4 })
const W_COLS_CLASS = { 1: 'tw:grid-cols-1', 2: 'tw:grid-cols-2', 3: 'tw:grid-cols-3', 4: 'tw:grid-cols-4' }
const wColsClass = computed(() => W_COLS_CLASS[wCols.value] ?? 'tw:grid-cols-2')
</script>

<template>
  <section class="tw:space-y-8">
    <!-- ── Header ───────────────────────────────────────────────── -->
    <div class="tw:flex tw:flex-wrap tw:items-end tw:justify-between tw:gap-4">
      <div>
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-primary-300">
          {{ t('products.breadcrumb') }}
        </p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">{{ t('products.list.title') }}</h1>
        <p class="tw:mt-2 tw:text-sm tw:text-muted">{{ t('products.list.subtitle') }}</p>
      </div>
      <div class="tw:flex tw:items-center tw:gap-2">
        <widget-settings-button
          :widgets="wDefs"
          :hidden-count="wHidden"
          :cols-per-row="wCols"
          @toggle="wToggle"
          @update:cols-per-row="wSetCols"
        />
        <prime-button
          v-if="can('product.create')"
          severity="success"
          size="small"
          @click="router.push({ name: 'productsCreate' })"
        >
          <iconify icon="ph:plus-bold" />
          <span>{{ t('products.list.addProduct') }}</span>
        </prime-button>
      </div>
    </div>

    <!-- ── Summary Stats ─────────────────────────────────────────── -->
    <div :class="['tw:grid tw:gap-3', wColsClass]">
      <widget-stat
        v-if="wVisible('total')"
        :label="t('products.widgets.total.label')"
        :value="summary.total"
      />
      <widget-stat
        v-if="wVisible('active')"
        :label="t('products.widgets.active.label')"
        label-class="tw:text-primary-400"
        :value="summary.active"
      />
      <widget-stat
        v-if="wVisible('low')"
        :label="t('products.widgets.low.label')"
        :value="summary.low"
      />
      <widget-stat
        v-if="wVisible('inactive')"
        :label="t('products.widgets.inactive.label')"
        label-class="tw:text-red-400"
        :value="summary.inactive"
      />
    </div>

    <!-- ── Error ─────────────────────────────────────────────────── -->
    <prime-alert
      v-if="errorMessage"
      severity="error"
      variant="accent"
      closable
      @close="errorMessage = ''"
      >{{ errorMessage }}</prime-alert
    >

    <!-- ── Table ──────────────────────────────────────────────────── -->
    <AppTable
      v-model:first="first"
      v-model:rows="rows"
      v-model:columns="colDefs"
      :value="products"
      :loading="loading"
      :totalRecords="totalRecords"
      :rowsPerPageOptions="[5, 10, 20, 50]"
      :show-column-toggle="true"
      @page="(e) => loadProducts(e.page + 1)"
    >
      <template #toolbar-left>
        <div class="tw:flex tw:items-center tw:gap-2">
          <!-- Search by name -->
          <prime-input-text
            v-model="search"
            :placeholder="t('products.list.searchPlaceholder')"
            class="app-input tw:w-56"
            size="small"
          />

          <!-- Filter toggle button -->
          <prime-button
            :severity="hasActiveFilters ? 'success' : 'secondary'"
            :outlined="!hasActiveFilters"
            v-tooltip.top="t('products.list.filtersTooltip')"
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
            <div class="tw:flex tw:flex-col tw:gap-2 tw:w-full">
              <p class="tw:text-sm tw:font-semibold">{{ t('products.list.filterTitle') }}</p>

              <!-- Category -->
              <div class="tw:space-y-1.5">
                <label
                  for="filter-category"
                  class="tw:text-xs tw:text-muted tw:uppercase tw:tracking-widest"
                  >{{ t('products.form.category') }}</label
                >
                <prime-select
                  v-model="categoryFilter"
                  input-id="filter-category"
                  :options="categories"
                  option-label="name"
                  option-value="id"
                  :placeholder="t('products.list.allCategories')"
                  show-clear
                  class="app-input tw:w-full"
                  size="small"
                />
              </div>

              <!-- Status -->
              <div class="tw:space-y-1.5">
                <label
                  for="filter-status"
                  class="tw:text-xs tw:text-muted tw:uppercase tw:tracking-widest"
                  >{{ t('common.status') }}</label
                >
                <prime-select
                  v-model="statusFilter"
                  input-id="filter-status"
                  :options="statusOptions"
                  option-label="label"
                  option-value="value"
                  :placeholder="t('products.list.allStatuses')"
                  show-clear
                  class="app-input tw:w-full"
                  size="small"
                />
              </div>

              <!-- Price range -->
              <div class="tw:space-y-1.5">
                <label
                  class="tw:text-xs tw:text-muted tw:uppercase tw:tracking-widest"
                  >{{ t('products.list.priceRange') }}</label
                >
                <div class="tw:flex tw:items-center tw:gap-2">
                  <prime-input-number
                    v-model="minPrice"
                    input-id="filter-min-price"
                    placeholder="Min"
                    :min="0"
                    :use-grouping="true"
                    class="app-input tw:flex-1"
                    size="small"
                  />
                  <span class="tw:text-muted tw:text-sm">–</span>
                  <prime-input-number
                    v-model="maxPrice"
                    input-id="filter-max-price"
                    placeholder="Max"
                    :min="0"
                    :use-grouping="true"
                    class="app-input tw:flex-1"
                    size="small"
                  />
                </div>
              </div>

              <!-- Clear -->
              <prime-button
                v-if="hasActiveFilters"
                severity="danger"
                outlined
                size="small"
                @click="clearFilters"
                class="tw:mt-1"
              >
                <iconify icon="ph:x-bold" />
                <span>{{ t('products.list.clearFilters') }}</span>
              </prime-button>
            </div>
          </prime-popover>
        </div>
      </template>

      <template #mobile-card="{ data }">
        <div class="tw:rounded-xl tw:border tw:border-slate-200 tw:dark:border-white/10 tw:bg-white tw:dark:bg-white/5 tw:p-3 tw:flex tw:flex-col tw:gap-2">
          <!-- Image + Name -->
          <div class="tw:flex tw:items-center tw:gap-2">
            <img
              v-if="data.imageUrl"
              :src="data.imageUrl"
              :alt="data.name"
              class="tw:h-10 tw:w-10 tw:rounded-lg tw:object-cover tw:flex-shrink-0"
            />
            <div
              v-else
              class="tw:h-10 tw:w-10 tw:rounded-lg tw:bg-white/10 tw:flex-shrink-0 tw:flex tw:items-center tw:justify-center"
            >
              <iconify icon="ph:coffee-bold" class="tw:text-sm tw:text-muted" />
            </div>
            <span class="tw:font-medium tw:text-sm tw:line-clamp-2 tw:leading-tight">{{ data.name }}</span>
          </div>
          <!-- Price -->
          <p class="tw:font-semibold tw:text-sm">{{ formatVnd(data.price) }}</p>
          <!-- Category + Status -->
          <div class="tw:flex tw:items-center tw:gap-1.5 tw:flex-wrap">
            <span v-if="data.category" class="tw:text-xs tw:text-muted">{{ data.category }}</span>
            <prime-tag
              :value="statusTag(data.status).label"
              :severity="statusTag(data.status).severity"
              class="tw:text-[11px]! tw:px-1.5! tw:py-0.5!"
            />
          </div>
          <!-- Actions -->
          <div class="tw:border-t tw:border-slate-200 tw:dark:border-white/10 tw:pt-2">
            <prime-button severity="secondary" outlined size="small" fluid @click="openDrawer(data)">
              <iconify icon="ph:dots-three-bold" />
              <span>{{ t('common.moreActions') }}</span>
            </prime-button>
          </div>
        </div>
      </template>

      <template #col-product="{ data }">
        <div class="tw:flex tw:items-center tw:gap-3">
          <img
            v-if="data.imageUrl"
            :src="data.imageUrl"
            :alt="data.name"
            class="tw:h-9 tw:w-9 tw:rounded-lg tw:object-cover tw:flex-shrink-0"
          />
          <div
            v-else
            class="tw:h-9 tw:w-9 tw:rounded-lg tw:bg-white/10 tw:flex-shrink-0 tw:flex tw:items-center tw:justify-center"
          >
            <iconify icon="ph:coffee-bold" class="tw:text-sm tw:text-muted" />
          </div>
          <span class="tw:font-medium tw:text-sm">{{ data.name }}</span>
        </div>
      </template>

      <template #col-price="{ data }">{{ formatVnd(data.price) }}</template>

      <template #col-status="{ data }">
        <prime-tag
          :value="statusTag(data.status).label"
          :severity="statusTag(data.status).severity"
        />
      </template>

      <template #col-actions="{ data }">
        <div class="tw:flex tw:justify-end tw:gap-2">
          <prime-button
            v-if="can('product.update')"
            :severity="data.status === 'active' ? 'danger' : 'success'"
            outlined
            size="small"
            :class="btnIcon"
            v-tooltip.top="data.status === 'active' ? t('products.list.tooltip.deactivate') : t('products.list.tooltip.activate')"
            @click="handleToggleActive(data)"
          >
            <iconify :icon="data.status === 'active' ? 'ph:toggle-left-bold' : 'ph:toggle-right-bold'" />
          </prime-button>
          <prime-button
            v-if="can('product.view')"
            severity="secondary"
            outlined
            size="small"
            v-tooltip.top="t('products.list.tooltip.viewDetail')"
            @click="router.push({ name: 'productsDetail', params: { id: data.id } })"
            :class="btnIcon"
          >
            <iconify icon="ph:arrow-right-bold" />
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
          <span class="tw:font-medium">{{ drawerProduct?.name }}</span>
          <prime-tag
            v-if="drawerProduct"
            :value="statusTag(drawerProduct.status).label"
            :severity="statusTag(drawerProduct.status).severity"
            class="tw:text-[11px]! tw:px-1.5! tw:py-0.5!"
          />
        </div>
      </template>

      <div v-if="drawerProduct" class="tw:flex tw:flex-col tw:gap-2 tw:pb-4">
        <prime-button
          v-if="can('product.update')"
          :label="drawerProduct.status === 'active' ? t('products.list.tooltip.deactivate') : t('products.list.tooltip.activate')"
          :severity="drawerProduct.status === 'active' ? 'danger' : 'success'"
          outlined fluid
          @click="handleToggleActive(drawerProduct); drawerVisible = false"
        >
          <template #icon>
            <iconify :icon="drawerProduct.status === 'active' ? 'ph:toggle-left-bold' : 'ph:toggle-right-bold'" />
          </template>
        </prime-button>

        <prime-button
          v-if="can('product.view')"
          :label="t('products.list.tooltip.viewDetail')"
          severity="secondary" outlined fluid
          @click="router.push({ name: 'productsDetail', params: { id: drawerProduct.id } }); drawerVisible = false"
        >
          <template #icon><iconify icon="ph:arrow-right-bold" /></template>
        </prime-button>
      </div>
    </prime-drawer>
  </section>
</template>
