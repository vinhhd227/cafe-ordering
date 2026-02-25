<script setup>
import { computed, onMounted, ref, watch } from "vue";
import { getProducts } from "@/services/product.service";
import { useAuthStore } from "@/stores/auth";
import AppTable from "@/components/AppTable.vue";

const search = ref("");
const loading = ref(false);
const errorMessage = ref("");
const products = ref([]);
const rows = ref(10);
const first = ref(0);
const totalRecords = ref(0);
const summary = ref({ total: 0, active: 0, low: 0, inactive: 0 });
const searchTimer = ref(null);
const authStore = useAuthStore();
const canManageProducts = computed(() => {
  const roles = authStore.user?.roles ?? [];
  return roles.includes("Admin") || roles.includes("Manager");
});

const statusTag = (status) => {
  switch (status) {
    case "active":
      return { label: "Active", severity: "success" };
    case "low":
      return { label: "Low stock", severity: "warning" };
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

// Đếm active / inactive toàn bộ (không phụ thuộc trang hiện tại)
const loadStats = async () => {
  try {
    const term = search.value.trim() || undefined;
    const [activeRes, inactiveRes] = await Promise.all([
      getProducts({ page: 1, pageSize: 1, searchTerm: term, isActive: true }),
      getProducts({ page: 1, pageSize: 1, searchTerm: term, isActive: false }),
    ]);
    summary.value = {
      total: summary.value.total, // giữ lại total đã set từ loadProducts
      active: activeRes?.data?.pagedInfo?.totalRecords ?? 0,
      low: 0,
      inactive: inactiveRes?.data?.pagedInfo?.totalRecords ?? 0,
    };
  } catch {
    // stats non-critical, không hiển thị lỗi
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
    });
    // API trả về Ardalis PagedResult:
    // res.data = { pagedInfo: { pageNumber, pageSize, totalPages, totalRecords }, value: [...] }
    const paged = res?.data ?? {};
    products.value =
      paged.value?.map((product) => ({
        id: product.id,
        name: product.name,
        price: product.price,
        imageUrl: product.imageUrl,
        status: product.isActive ? "active" : "inactive",
        category: product.categoryName,
      })) ?? [];
    const total = paged.pagedInfo?.totalRecords ?? 0;
    totalRecords.value = total;
    summary.value = { total, active: 0, low: 0, inactive: 0 };
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.message || "Failed to load products.";
  } finally {
    loading.value = false;
  }
};

onMounted(() => {
  loadProducts();
  loadStats();
});

watch(search, () => {
  clearTimeout(searchTimer.value);
  searchTimer.value = setTimeout(() => {
    first.value = 0;
    loadProducts(1);
    loadStats();
  }, 400);
});
</script>

<template>
  <section class="tw:space-y-8">

    <!-- ── Header ───────────────────────────────────────────────── -->
    <div class="tw:flex tw:flex-wrap tw:items-end tw:justify-between tw:gap-4">
      <div>
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300">
          Products
        </p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">Catalog overview</h1>
        <p class="tw:mt-2 tw:text-sm app-text-muted">
          Track pricing, stock, and status across the menu.
        </p>
      </div>
      <div class="tw:flex tw:items-center tw:gap-3">
        <prime-button label="Import" severity="secondary" outlined size="small" />
        <prime-button
          v-if="canManageProducts"
          label="Add product"
          severity="success"
          size="small"
        />
      </div>
    </div>

    <!-- ── Summary Stats ─────────────────────────────────────────── -->
    <div class="tw:grid tw:grid-cols-1 tw:gap-3 tw:md:grid-cols-2 tw:lg:grid-cols-4">
      <prime-card class="app-card tw:rounded-xl tw:border">
        <template #content>
          <p class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] app-text-subtle">Total items</p>
          <p class="tw:mt-2 tw:text-2xl tw:font-semibold">{{ summary.total }}</p>
        </template>
      </prime-card>
      <prime-card class="app-card tw:rounded-xl tw:border">
        <template #content>
          <p class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] app-text-subtle">Active</p>
          <p class="tw:mt-2 tw:text-2xl tw:font-semibold">{{ summary.active }}</p>
        </template>
      </prime-card>
      <prime-card class="app-card tw:rounded-xl tw:border">
        <template #content>
          <p class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] app-text-subtle">Low stock</p>
          <p class="tw:mt-2 tw:text-2xl tw:font-semibold">{{ summary.low }}</p>
        </template>
      </prime-card>
      <prime-card class="app-card tw:rounded-xl tw:border">
        <template #content>
          <p class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] app-text-subtle">Inactive</p>
          <p class="tw:mt-2 tw:text-2xl tw:font-semibold">{{ summary.inactive }}</p>
        </template>
      </prime-card>
    </div>

    <!-- ── Error ─────────────────────────────────────────────────── -->
    <prime-message
      v-if="errorMessage"
      severity="error"
      size="small"
      variant="simple"
      :closable="true"
      @close="errorMessage = ''"
    >
      {{ errorMessage }}
    </prime-message>

    <!-- ── Table ──────────────────────────────────────────────────── -->
    <AppTable
      v-model:first="first"
      v-model:rows="rows"
      :value="products"
      :loading="loading"
      :totalRecords="totalRecords"
      :rowsPerPageOptions="[5, 10, 20, 50]"
      @page="(e) => loadProducts(e.page + 1)"
    >
      <template #toolbar-left>
        <prime-input-text
          v-model="search"
          placeholder="Search products"
          class="app-input tw:w-64"
        />
      </template>
      <template #toolbar-right>
        <prime-button label="Export" severity="secondary" outlined size="small" />
      </template>

      <prime-column field="id" header="ID" style="min-width: 4rem" />
      <prime-column field="name" header="Product" style="min-width: 12rem" />
      <prime-column field="category" header="Category" />
      <prime-column field="price" header="Price">
        <template #body="{ data }">{{ formatVnd(data.price) }}</template>
      </prime-column>
      <prime-column field="status" header="Status">
        <template #body="{ data }">
          <prime-tag
            :value="statusTag(data.status).label"
            :severity="statusTag(data.status).severity"
          />
        </template>
      </prime-column>
      <prime-column header="Actions" style="min-width: 8rem">
        <template #body>
          <prime-button
            v-if="canManageProducts"
            label="Edit"
            severity="secondary"
            outlined
            size="small"
          />
        </template>
      </prime-column>
    </AppTable>

  </section>
</template>
