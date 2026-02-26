<script setup>
import { computed, onMounted, ref, watch } from "vue";
import { useRouter, onBeforeRouteLeave } from "vue-router";
import { getCategory } from "@/services/category.service";
import AppTable from "@/components/AppTable.vue";
import { useTableCache } from "@/composables/useTableCache";

const cache = useTableCache("categories");

const router = useRouter();

// ── Raw data from API ─────────────────────────────────────────────
const allCategories  = ref([]);
const loading        = ref(false);
const errorMessage   = ref("");
const searchTimer    = ref(null);

// ── Filters ──────────────────────────────────────────────────────
const search       = ref("");
const statusFilter = ref(null);

const statusOptions = [
  { label: "Active",   value: true  },
  { label: "Inactive", value: false },
];

const filterPanel = ref(null);

const activeFilterCount = computed(() => statusFilter.value !== null ? 1 : 0);
const hasActiveFilters  = computed(() => activeFilterCount.value > 0);

const clearFilters = () => {
  statusFilter.value = null;
  first.value = 0;
};

// ── Table pagination (client-side) ────────────────────────────────
const rows  = ref(10);
const first = ref(0);

const filteredCategories = computed(() => {
  let list = allCategories.value;

  if (search.value.trim()) {
    const q = search.value.trim().toLowerCase();
    list = list.filter(
      (c) =>
        c.name.toLowerCase().includes(q) ||
        (c.description ?? "").toLowerCase().includes(q)
    );
  }

  if (statusFilter.value !== null) {
    list = list.filter((c) => c.isActive === statusFilter.value);
  }

  return list;
});

const pagedCategories = computed(() => {
  return filteredCategories.value.slice(first.value, first.value + rows.value);
});

const totalRecords = computed(() => filteredCategories.value.length);

// ── Summary stats ─────────────────────────────────────────────────
const summary = computed(() => {
  const all      = allCategories.value;
  const active   = all.filter((c) => c.isActive).length;
  const inactive = all.filter((c) => !c.isActive).length;
  return { total: all.length, active, inactive };
});

// ── Status helpers ────────────────────────────────────────────────
const statusTag = (isActive) =>
  isActive
    ? { label: "Active", severity: "success" }
    : { label: "Inactive", severity: "danger" };

// ── Load data ─────────────────────────────────────────────────────
const loadCategories = async () => {
  loading.value = true;
  errorMessage.value = "";
  try {
    const res = await getCategory();
    const raw = res?.data;
    allCategories.value = Array.isArray(raw)
      ? raw
      : Array.isArray(raw?.value)
      ? raw.value
      : Array.isArray(raw?.items)
      ? raw.items
      : [];
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.message || "Failed to load categories.";
  } finally {
    loading.value = false;
  }
};

onMounted(() => {
  const cached = cache.restore();
  if (cached) {
    search.value       = cached.search       ?? "";
    rows.value         = cached.rows         ?? 10;
    first.value        = cached.first        ?? 0;
    statusFilter.value = cached.statusFilter ?? null;
  }
  loadCategories();
});

onBeforeRouteLeave(() => {
  cache.save({
    search:       search.value,
    rows:         rows.value,
    first:        first.value,
    statusFilter: statusFilter.value,
  });
});

// Reset pagination when filters/search change
watch([search, statusFilter], () => {
  clearTimeout(searchTimer.value);
  searchTimer.value = setTimeout(() => {
    first.value = 0;
  }, 300);
});
</script>

<template>
  <section class="tw:space-y-8">

    <!-- ── Header ───────────────────────────────────────────────── -->
    <div class="tw:flex tw:flex-wrap tw:items-end tw:justify-between tw:gap-4">
      <div>
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300">
          Catalog
        </p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">Categories</h1>
        <p class="tw:mt-2 tw:text-sm app-text-muted">
          Manage the categories used to organise your menu.
        </p>
      </div>
      <prime-button
        severity="success"
        size="small"
        @click="router.push({ name: 'categoriesCreate' })"
      >
        <iconify icon="ph:plus-bold" />
        <span>Add category</span>
      </prime-button>
    </div>

    <!-- ── Summary Stats ─────────────────────────────────────────── -->
    <div class="tw:grid tw:grid-cols-3 tw:gap-3">
      <prime-card class="app-card tw:rounded-xl tw:border">
        <template #content>
          <p class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] app-text-subtle">
            Total
          </p>
          <p class="tw:mt-2 tw:text-2xl tw:font-semibold">{{ summary.total }}</p>
        </template>
      </prime-card>
      <prime-card class="app-card tw:rounded-xl tw:border">
        <template #content>
          <p class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] tw:text-emerald-400">
            Active
          </p>
          <p class="tw:mt-2 tw:text-2xl tw:font-semibold">{{ summary.active }}</p>
        </template>
      </prime-card>
      <prime-card class="app-card tw:rounded-xl tw:border">
        <template #content>
          <p class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] tw:text-red-400">
            Inactive
          </p>
          <p class="tw:mt-2 tw:text-2xl tw:font-semibold">{{ summary.inactive }}</p>
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
      :value="pagedCategories"
      :loading="loading"
      :totalRecords="totalRecords"
      :rowsPerPageOptions="[5, 10, 20, 50]"
      @page="(e) => (first = e.first)"
    >
      <template #toolbar-left>
        <div class="tw:flex tw:items-center tw:gap-2">
          <!-- Search -->
          <prime-input-text
            v-model="search"
            placeholder="Search by name…"
            class="app-input tw:w-56"
          />

          <!-- Filter toggle -->
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
            <div class="tw:flex tw:flex-col tw:gap-4">
              <p class="tw:text-sm tw:font-semibold">Filter categories</p>

              <div class="tw:space-y-1.5">
                <label class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest">Status</label>
                <prime-select
                  v-model="statusFilter"
                  :options="statusOptions"
                  option-label="label"
                  option-value="value"
                  placeholder="All statuses"
                  show-clear
                  class="app-input tw:w-full"
                />
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

      <prime-column field="id" header="ID" style="min-width: 4rem" />
      <prime-column field="name" header="Name" style="min-width: 12rem" />
      <prime-column header="Description" style="min-width: 16rem">
        <template #body="{ data }">
          <span
            v-if="data.description"
            class="tw:text-sm app-text-muted tw:line-clamp-1"
          >{{ data.description }}</span>
          <span v-else class="tw:text-sm app-text-subtle">—</span>
        </template>
      </prime-column>
      <prime-column field="isActive" header="Status" style="min-width: 8rem">
        <template #body="{ data }">
          <prime-tag
            :value="statusTag(data.isActive).label"
            :severity="statusTag(data.isActive).severity"
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
            @click="router.push({ name: 'categoriesDetail', params: { id: data.id } })"
          >
            <iconify icon="ph:arrow-right-bold" />
            <span>View</span>
          </prime-button>
        </template>
      </prime-column>
    </AppTable>

  </section>
</template>
