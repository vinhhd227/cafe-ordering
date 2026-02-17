<script setup>
import { computed, onMounted, ref, watch } from "vue";
import { getProducts } from "@/services/product.service";
import { useAuthStore } from "@/stores/auth";

const search = ref("");
const statusFilter = ref("all");
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

const statusOptions = [
  { label: "All", value: "all" },
  { label: "Active", value: "active" },
  { label: "Inactive", value: "inactive" },
];


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

const loadProducts = async (page = 1) => {
  loading.value = true;
  errorMessage.value = "";
  try {
    const res = await getProducts({
      page,
      pageSize: rows.value,
      search: search.value.trim() || undefined,
      status: statusFilter.value !== "all" ? statusFilter.value : undefined,
    });
    const data = res?.data ?? {};
    products.value =
      data.items?.map((product) => ({
        id: product.id,
        name: product.name,
        description: product.description,
        price: product.price,
        imageUrl: product.imageUrl,
        status: product.isActive ? "active" : "inactive",
        category: product.categoryName,
        options: {
          temperature: product.hasTemperatureOption,
          ice: product.hasIceLevelOption,
          sugar: product.hasSugarLevelOption,
        },
      })) ?? [];
    totalRecords.value = data.total ?? 0;
    summary.value = {
      total: data.total ?? 0,
      active: data.activeCount ?? 0,
      low: 0,
      inactive: data.inactiveCount ?? 0,
    };
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.message || "Failed to load products.";
  } finally {
    loading.value = false;
  }
};

onMounted(loadProducts);

watch([search, statusFilter], () => {
  clearTimeout(searchTimer.value);
  searchTimer.value = setTimeout(() => {
    first.value = 0;
    loadProducts(1);
  }, 400);
});
</script>

<template>
  <section class="tw:space-y-8">
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

    <div class="tw:grid tw:grid-cols-1 tw:gap-3 tw:md:grid-cols-2 tw:lg:grid-cols-4">
      <prime-card class="app-card tw:rounded-xl tw:border">
        <template #content>
          <p class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] app-text-subtle">
            Total items
          </p>
          <p class="tw:mt-2 tw:text-2xl tw:font-semibold">{{ summary.total }}</p>
        </template>
      </prime-card>
      <prime-card class="app-card tw:rounded-xl tw:border">
        <template #content>
          <p class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] app-text-subtle">
            Active
          </p>
          <p class="tw:mt-2 tw:text-2xl tw:font-semibold">{{ summary.active }}</p>
        </template>
      </prime-card>
      <prime-card class="app-card tw:rounded-xl tw:border">
        <template #content>
          <p class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] app-text-subtle">
            Low stock
          </p>
          <p class="tw:mt-2 tw:text-2xl tw:font-semibold">{{ summary.low }}</p>
        </template>
      </prime-card>
      <prime-card class="app-card tw:rounded-xl tw:border">
        <template #content>
          <p class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] app-text-subtle">
            Inactive
          </p>
          <p class="tw:mt-2 tw:text-2xl tw:font-semibold">{{ summary.inactive }}</p>
        </template>
      </prime-card>
    </div>

    <prime-card class="app-card tw:rounded-2xl tw:border">
      <template #content>
        <div class="tw:flex tw:flex-wrap tw:items-center tw:justify-between tw:gap-4">
          <div class="tw:flex tw:flex-wrap tw:items-center tw:gap-3">
            <prime-input-text
              v-model="search"
              placeholder="Search products"
              class="app-input tw:w-64"
            />
            <prime-select
              v-model="statusFilter"
              :options="statusOptions"
              optionLabel="label"
              optionValue="value"
              class="app-input tw:w-40"
            />
          </div>
          <prime-button label="Export" severity="secondary" outlined size="small" />
        </div>

        <prime-message
          v-if="errorMessage"
          severity="error"
          size="small"
          variant="simple"
          :closable="false"
          class="tw:mt-4"
        >
          {{ errorMessage }}
        </prime-message>

        <prime-data-table
          class="tw:mt-6"
          :value="products"
          :loading="loading"
          :paginator="true"
          :rows="rows"
          :first="first"
          :totalRecords="totalRecords"
          :rowsPerPageOptions="[5, 10, 20, 50]"
          @page="(event) => { first = event.first; rows = event.rows; loadProducts(event.page + 1); }"
          responsiveLayout="scroll"
        >
          <prime-column field="id" header="ID"></prime-column>
          <prime-column field="name" header="Product"></prime-column>
          <prime-column field="category" header="Category"></prime-column>
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
          <prime-column header="Options">
            <template #body="{ data }">
              <div class="tw:flex tw:flex-wrap tw:gap-2">
                <prime-tag
                  v-if="data.options.temperature"
                  value="Temp"
                  severity="info"
                />
                <prime-tag
                  v-if="data.options.ice"
                  value="Ice"
                  severity="info"
                />
                <prime-tag
                  v-if="data.options.sugar"
                  value="Sugar"
                  severity="info"
                />
                <span v-if="!data.options.temperature && !data.options.ice && !data.options.sugar" class="app-text-muted">
                  --
                </span>
              </div>
            </template>
          </prime-column>
          <prime-column header="Actions">
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
        </prime-data-table>
      </template>
    </prime-card>
  </section>
</template>
