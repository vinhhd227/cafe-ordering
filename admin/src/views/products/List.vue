<script setup>
import { computed, onMounted, ref, watch } from "vue";
import { useRouter, onBeforeRouteLeave } from "vue-router";
import { usePermission } from "@/composables/usePermission";
import { getProducts } from "@/services/product.service";
import { getCategory } from "@/services/category.service";
import AppTable from "@/components/AppTable.vue";
import { useTableCache } from "@/composables/useTableCache";

const cache = useTableCache("products");

const router = useRouter();
const { can } = usePermission();

// ── Table state ─────────────────────────────────────────────────────
const loading      = ref(false);
const errorMessage = ref("");
const products     = ref([]);
const rows         = ref(10);
const first        = ref(0);

// ── Column visibility ────────────────────────────────────────────────
const colDefs = ref([
  { field: "id",       header: "ID",       visible: true },
  { field: "product",  header: "Product",  visible: true },
  { field: "category", header: "Category", visible: true },
  { field: "price",    header: "Price",    visible: true },
  { field: "status",   header: "Status",   visible: true },
]);
const colVisible = (field) => colDefs.value.find((c) => c.field === field)?.visible !== false;
const totalRecords = ref(0);
const summary      = ref({ total: 0, active: 0, low: 0, inactive: 0 });
const searchTimer  = ref(null);

// ── Filters ─────────────────────────────────────────────────────────
const search         = ref("");
const categoryFilter = ref(null);
const statusFilter   = ref(null);
const minPrice       = ref(null);
const maxPrice       = ref(null);
const categories     = ref([]);

const statusOptions = [
  { label: "Active",   value: true  },
  { label: "Inactive", value: false },
];

const filterPanel = ref(null);

const activeFilterCount = computed(() => {
  let n = 0;
  if (categoryFilter.value !== null) n++;
  if (statusFilter.value   !== null) n++;
  if (minPrice.value       !== null) n++;
  if (maxPrice.value       !== null) n++;
  return n;
});

const hasActiveFilters = computed(() => activeFilterCount.value > 0);

const clearFilters = () => {
  categoryFilter.value = null;
  statusFilter.value   = null;
  minPrice.value       = null;
  maxPrice.value       = null;
  first.value          = 0;
};

const statusTag = (status) => {
  switch (status) {
    case "active":
      return { label: "Active", severity: "success" };
    case "inactive":
      return { label: "Inactive", severity: "danger" };
    default:
      return { label: "Unknown", severity: "info" };
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
  } catch { /* non-critical */ }
};

const loadProducts = async (page = 1) => {
  loading.value = true;
  errorMessage.value = "";
  try {
    const res = await getProducts({
      page,
      pageSize:   rows.value,
      searchTerm: search.value.trim()  || undefined,
      isActive:   statusFilter.value   ?? undefined,
      categoryId: categoryFilter.value ?? undefined,
      minPrice:   minPrice.value       ?? undefined,
      maxPrice:   maxPrice.value       ?? undefined,
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
    // Chỉ cập nhật total — active/inactive do loadStats() quản lý riêng,
    // không reset về 0 khi đổi trang
    summary.value = { ...summary.value, total };
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.message || "Failed to load products.";
  } finally {
    loading.value = false;
  }
};

onMounted(() => {
  loadCategories();
  const cached = cache.restore();
  if (cached) {
    search.value         = cached.search         ?? "";
    rows.value           = cached.rows           ?? 10;
    first.value          = cached.first          ?? 0;
    categoryFilter.value = cached.categoryFilter ?? null;
    statusFilter.value   = cached.statusFilter   ?? null;
    minPrice.value       = cached.minPrice       ?? null;
    maxPrice.value       = cached.maxPrice       ?? null;
    if (cached.colDefs) colDefs.value = cached.colDefs;
    const page = rows.value > 0 ? Math.floor(first.value / rows.value) + 1 : 1;
    loadProducts(page);
  } else {
    loadProducts(1);
  }
  loadStats();
});

onBeforeRouteLeave(() => {
  cache.save({
    search:         search.value,
    rows:           rows.value,
    first:          first.value,
    categoryFilter: categoryFilter.value,
    statusFilter:   statusFilter.value,
    minPrice:       minPrice.value,
    maxPrice:       maxPrice.value,
    colDefs:        colDefs.value,
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

watch([categoryFilter, statusFilter], () => {
  first.value = 0;
  loadProducts(1);
});
</script>

<template>
  <section class="tw:space-y-8">
    <!-- ── Header ───────────────────────────────────────────────── -->
    <div class="tw:flex tw:flex-wrap tw:items-end tw:justify-between tw:gap-4">
      <div>
        <p
          class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300"
        >
          Products
        </p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">Catalog overview</h1>
        <p class="tw:mt-2 tw:text-sm app-text-muted">
          Track pricing, stock, and status across the menu.
        </p>
      </div>
      <prime-button
        v-if="can('product.create')"
        severity="success"
        size="small"
        @click="router.push({ name: 'productsCreate' })"
      >
        <iconify icon="ph:plus-bold" />
        <span>Add product</span>
      </prime-button>
    </div>

    <!-- ── Summary Stats ─────────────────────────────────────────── -->
    <div class="tw:grid tw:grid-cols-2 tw:gap-3 tw:lg:grid-cols-4">
      <prime-card class="app-card tw:rounded-xl tw:border">
        <template #content>
          <p
            class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] app-text-subtle"
          >
            Total items
          </p>
          <p class="tw:mt-2 tw:text-2xl tw:font-semibold">
            {{ summary.total }}
          </p>
        </template>
      </prime-card>
      <prime-card class="app-card tw:rounded-xl tw:border">
        <template #content>
          <p
            class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] tw:text-emerald-400"
          >
            Active
          </p>
          <p class="tw:mt-2 tw:text-2xl tw:font-semibold">
            {{ summary.active }}
          </p>
        </template>
      </prime-card>
      <prime-card class="app-card tw:rounded-xl tw:border">
        <template #content>
          <p
            class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] app-text-subtle"
          >
            Low stock
          </p>
          <p class="tw:mt-2 tw:text-2xl tw:font-semibold">{{ summary.low }}</p>
        </template>
      </prime-card>
      <prime-card class="app-card tw:rounded-xl tw:border">
        <template #content>
          <p
            class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] tw:text-red-400"
          >
            Inactive
          </p>
          <p class="tw:mt-2 tw:text-2xl tw:font-semibold">
            {{ summary.inactive }}
          </p>
        </template>
      </prime-card>
    </div>

    <!-- ── Error ─────────────────────────────────────────────────── -->
    <prime-alert
      v-if="errorMessage"
      severity="error"
      variant="accent"
      closable
      @close="errorMessage = ''"
    >{{ errorMessage }}</prime-alert>

    <!-- ── Table ──────────────────────────────────────────────────── -->
    <AppTable
      v-model:first="first"
      v-model:rows="rows"
      v-model:columns="colDefs"
      :value="products"
      :loading="loading"
      :totalRecords="totalRecords"
      :rowsPerPageOptions="[5, 10, 20, 50]"
      @page="(e) => loadProducts(e.page + 1)"
    >
      <template #toolbar-left>
        <div class="tw:flex tw:items-center tw:gap-2">
          <!-- Search by name -->
          <prime-input-text
            v-model="search"
            placeholder="Search by name…"
            class="app-input tw:w-56"
          />

          <!-- Filter toggle button -->
          <prime-button
            :severity="hasActiveFilters ? 'success' : 'secondary'"
            :outlined="!hasActiveFilters"
            v-tooltip.top="'Filters'"
            @click="filterPanel.toggle($event)"
          >
            <iconify icon="ph:funnel-bold" />
            <prime-badge
              v-if="activeFilterCount > 0"
              :value="activeFilterCount"
              severity="danger"
              class="tw:ml-1 tw:scale-90"
            />
          </prime-button>

          <!-- Filter popover -->
          <prime-popover ref="filterPanel">
            <div class="tw:flex tw:flex-col tw:gap-4 tw:w-full">
              <p class="tw:text-sm tw:font-semibold">Filter products</p>

              <!-- Category -->
              <div class="tw:space-y-1.5">
                <label for="filter-category" class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest">Category</label>
                <prime-select
                  v-model="categoryFilter"
                  input-id="filter-category"
                  :options="categories"
                  option-label="name"
                  option-value="id"
                  placeholder="All categories"
                  show-clear
                  class="app-input tw:w-full"
                />
              </div>

              <!-- Status -->
              <div class="tw:space-y-1.5">
                <label for="filter-status" class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest">Status</label>
                <prime-select
                  v-model="statusFilter"
                  input-id="filter-status"
                  :options="statusOptions"
                  option-label="label"
                  option-value="value"
                  placeholder="All statuses"
                  show-clear
                  class="app-input tw:w-full"
                />
              </div>

              <!-- Price range -->
              <div class="tw:space-y-1.5">
                <label class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest">Price range (VND)</label>
                <div class="tw:flex tw:items-center tw:gap-2">
                  <prime-input-number
                    v-model="minPrice"
                    input-id="filter-min-price"
                    placeholder="Min"
                    :min="0"
                    :use-grouping="true"
                    class="app-input tw:flex-1"
                  />
                  <span class="app-text-muted tw:text-sm">–</span>
                  <prime-input-number
                    v-model="maxPrice"
                    input-id="filter-max-price"
                    placeholder="Max"
                    :min="0"
                    :use-grouping="true"
                    class="app-input tw:flex-1"
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
              >
                <iconify icon="ph:x-bold" />
                <span>Clear filters</span>
              </prime-button>
            </div>
          </prime-popover>
        </div>
      </template>

      <prime-column v-if="colVisible('id')" field="id" header="ID" style="min-width: 4rem" />
      <prime-column v-if="colVisible('product')" header="Product" style="min-width: 14rem">
        <template #body="{ data }">
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
              <iconify icon="ph:coffee-bold" class="tw:text-sm app-text-muted" />
            </div>
            <span class="tw:font-medium tw:text-sm">{{ data.name }}</span>
          </div>
        </template>
      </prime-column>
      <prime-column v-if="colVisible('category')" field="category" header="Category" />
      <prime-column v-if="colVisible('price')" field="price" header="Price">
        <template #body="{ data }">{{ formatVnd(data.price) }}</template>
      </prime-column>
      <prime-column v-if="colVisible('status')" field="status" header="Status">
        <template #body="{ data }">
          <prime-tag
            :value="statusTag(data.status).label"
            :severity="statusTag(data.status).severity"
          />
        </template>
      </prime-column>
      <prime-column header="Actions" style="min-width: 8rem">
        <template #body="{ data }">
          <prime-button
            iconPos="right"
            severity="secondary"
            outlined
            size="small"
            @click="
              router.push({ name: 'productsDetail', params: { id: data.id } })
            "
          >
            <iconify icon="ph:arrow-right-bold" />
            <span>View</span>
          </prime-button>
        </template>
      </prime-column>
    </AppTable>
  </section>
</template>
