<script setup>
import { computed, onBeforeUnmount, onMounted, ref, watch } from "vue";
import { useRouter, onBeforeRouteLeave } from "vue-router";
import { getCategory, toggleCategoryActive } from "@/services/category.service";
import AppTable from "@/components/AppTable.vue";
import { useTableCache } from "@/composables/useTableCache";
import { usePermission } from "@/composables/usePermission";
import { btnIcon } from "@/layout/ui";

const { t } = useI18n();
const cache = useTableCache("categories");

const router = useRouter();
const { can } = usePermission();

// ── Raw data from API ─────────────────────────────────────────────
const allCategories  = ref([]);
const loading        = ref(false);
const errorMessage   = ref("");
const searchTimer    = ref(null);

// ── Filters ──────────────────────────────────────────────────────
const search       = ref("");
const statusFilter = ref(null);

const statusOptions = computed(() => [
  { label: t('categories.status.active'),   value: true  },
  { label: t('categories.status.inactive'), value: false },
]);

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
    ? { label: t('categories.status.active'),   severity: "success" }
    : { label: t('categories.status.inactive'), severity: "danger" };

// ── Columns ───────────────────────────────────────────────────────
const columns = computed(() => [
  { field: 'id',          header: t('categories.list.col.id'),          width: '4rem' },
  { field: 'name',        header: t('categories.list.col.name'),        width: '12rem' },
  { field: 'description', header: t('categories.list.col.description'), width: '16rem' },
  { field: 'isActive',    header: t('categories.list.col.status'),      width: '8rem' },
  { key: 'actions',       header: t('categories.list.col.actions'),     width: '12rem', toggleable: false },
]);

// ── Actions ───────────────────────────────────────────────────────
const handleToggleActive = async (row) => {
  try {
    await toggleCategoryActive(row.id);
    await loadCategories();
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.message ?? t('categories.list.error');
  }
};

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
      err?.response?.data?.message || t('categories.list.error');
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

onBeforeUnmount(() => {
  clearTimeout(searchTimer.value);
});

// ── Mobile action drawer ───────────────────────────────────────────
const drawerCategory = ref(null);
const drawerVisible = ref(false);

const openDrawer = (row) => {
  drawerCategory.value = row;
  drawerVisible.value = true;
};
</script>

<template>
  <section class="tw:space-y-8">

    <!-- ── Header ───────────────────────────────────────────────── -->
    <div class="tw:flex tw:flex-wrap tw:items-end tw:justify-between tw:gap-4">
      <div>
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300">
          {{ t('categories.breadcrumb') }}
        </p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">{{ t('categories.list.title') }}</h1>
        <p class="tw:mt-2 tw:text-sm app-text-muted">
          {{ t('categories.list.subtitle') }}
        </p>
      </div>
      <prime-button
        v-if="can('product.create')"
        severity="success"
        size="small"
        @click="router.push({ name: 'categoriesCreate' })"
      >
        <iconify icon="ph:plus-bold" />
        <span>{{ t('categories.list.addCategory') }}</span>
      </prime-button>
    </div>

    <!-- ── Summary Stats ─────────────────────────────────────────── -->
    <div class="tw:grid tw:grid-cols-3 tw:gap-3">
      <prime-card class="app-card tw:rounded-xl tw:border">
        <template #content>
          <p class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] app-text-subtle">
            {{ t('categories.widgets.total.label') }}
          </p>
          <p class="tw:mt-2 tw:text-2xl tw:font-semibold">{{ summary.total }}</p>
        </template>
      </prime-card>
      <prime-card class="app-card tw:rounded-xl tw:border">
        <template #content>
          <p class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] tw:text-emerald-400">
            {{ t('categories.widgets.active.label') }}
          </p>
          <p class="tw:mt-2 tw:text-2xl tw:font-semibold">{{ summary.active }}</p>
        </template>
      </prime-card>
      <prime-card class="app-card tw:rounded-xl tw:border">
        <template #content>
          <p class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] tw:text-red-400">
            {{ t('categories.widgets.inactive.label') }}
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
      :columns="columns"
      @page="(e) => (first = e.first)"
    >
      <template #toolbar-left>
        <div class="tw:flex tw:items-center tw:gap-2">
          <!-- Search -->
          <prime-input-text
            v-model="search"
            :placeholder="t('categories.list.searchPlaceholder')"
            class="app-input tw:w-56"
          />

          <!-- Filter toggle -->
          <prime-button
            :severity="hasActiveFilters ? 'success' : 'secondary'"
            :outlined="!hasActiveFilters"
            v-tooltip.top="t('categories.list.filtersTooltip')"
            @click="filterPanel.toggle($event)"
            :class="!hasActiveFilters ? btnIcon : ''"
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
              <p class="tw:text-sm tw:font-semibold">{{ t('categories.list.filterTitle') }}</p>

              <div class="tw:space-y-1.5">
                <label class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest">{{ t('categories.list.col.status') }}</label>
                <prime-select
                  v-model="statusFilter"
                  :options="statusOptions"
                  option-label="label"
                  option-value="value"
                  :placeholder="t('categories.list.allStatuses')"
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
                <span>{{ t('categories.list.clearFilters') }}</span>
              </prime-button>
            </div>
          </prime-popover>
        </div>
      </template>

      <template #mobile-card="{ data }">
        <div class="tw:rounded-xl tw:border tw:border-slate-200 tw:dark:border-white/10 tw:bg-white tw:dark:bg-white/5 tw:p-3 tw:flex tw:flex-col tw:gap-2">
          <!-- Name + Status -->
          <div class="tw:flex tw:items-start tw:justify-between tw:gap-1">
            <span class="tw:font-semibold tw:text-sm tw:leading-snug">{{ data.name }}</span>
            <prime-tag
              :value="statusTag(data.isActive).label"
              :severity="statusTag(data.isActive).severity"
              class="tw:text-[11px]! tw:px-1.5! tw:py-0.5! tw:flex-shrink-0"
            />
          </div>
          <!-- Description -->
          <p v-if="data.description" class="tw:text-xs app-text-muted tw:line-clamp-2">{{ data.description }}</p>
          <p v-else class="tw:text-xs app-text-subtle">—</p>
          <!-- Actions -->
          <div class="tw:border-t tw:border-slate-200 tw:dark:border-white/10 tw:pt-2 tw:flex tw:justify-end">
            <prime-button
              severity="secondary" outlined size="small" :class="btnIcon"
              @click="openDrawer(data)"
            ><iconify icon="ph:dots-three-bold" /></prime-button>
          </div>
        </div>
      </template>

      <template #col-description="{ data }">
        <span v-if="data.description" class="tw:text-sm app-text-muted tw:line-clamp-1">{{ data.description }}</span>
        <span v-else class="tw:text-sm app-text-subtle">—</span>
      </template>

      <template #col-isActive="{ data }">
        <prime-tag
          :value="statusTag(data.isActive).label"
          :severity="statusTag(data.isActive).severity"
        />
      </template>

      <template #col-actions="{ data }">
        <div class="tw:flex tw:justify-end tw:gap-2">
          <prime-button
            :severity="data.isActive ? 'danger' : 'success'"
            outlined
            size="small"
            :class="btnIcon"
            v-tooltip.top="data.isActive ? t('categories.list.tooltip.deactivate') : t('categories.list.tooltip.activate')"
            @click="handleToggleActive(data)"
          >
            <iconify :icon="data.isActive ? 'ph:toggle-left-bold' : 'ph:toggle-right-bold'" />
          </prime-button>
          <prime-button
            severity="secondary"
            outlined
            size="small"
            v-tooltip.top="t('categories.list.tooltip.viewDetail')"
            @click="router.push({ name: 'categoriesDetail', params: { id: data.id } })"
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
          <span class="tw:font-medium">{{ drawerCategory?.name }}</span>
          <prime-tag
            v-if="drawerCategory"
            :value="statusTag(drawerCategory.isActive).label"
            :severity="statusTag(drawerCategory.isActive).severity"
            class="tw:text-[11px]! tw:px-1.5! tw:py-0.5!"
          />
        </div>
      </template>

      <div v-if="drawerCategory" class="tw:flex tw:flex-col tw:gap-2 tw:pb-4">
        <prime-button
          :label="drawerCategory.isActive ? t('categories.list.tooltip.deactivate') : t('categories.list.tooltip.activate')"
          :severity="drawerCategory.isActive ? 'danger' : 'success'"
          outlined fluid
          @click="handleToggleActive(drawerCategory); drawerVisible = false"
        >
          <template #icon>
            <iconify :icon="drawerCategory.isActive ? 'ph:toggle-left-bold' : 'ph:toggle-right-bold'" />
          </template>
        </prime-button>

        <prime-button
          :label="t('categories.list.tooltip.viewDetail')"
          severity="secondary" outlined fluid
          @click="router.push({ name: 'categoriesDetail', params: { id: drawerCategory.id } }); drawerVisible = false"
        >
          <template #icon><iconify icon="ph:arrow-right-bold" /></template>
        </prime-button>
      </div>
    </prime-drawer>

  </section>
</template>
