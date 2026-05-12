<script setup>
import { computed, onBeforeUnmount, onMounted, ref, watch } from "vue";
import { useRouter, onBeforeRouteLeave } from "vue-router";
import {
  getProductOptionGroups,
  toggleProductOptionGroupActive,
  deleteProductOptionGroup,
} from "@/services/product-option-group.service";
import AppTable from "@/components/AppTable.vue";
import { useTableCache } from "@/composables/useTableCache";
import { usePermission } from "@/composables/usePermission";
import { btnIcon } from "@/layout/ui";

const { t } = useI18n();
const cache = useTableCache("product-option-groups");
const router = useRouter();
const { can } = usePermission();
const toast = useToast();
const confirm = useConfirm();

// ── Raw data ─────────────────────────────────────────────────────────
const allGroups   = ref([]);
const loading     = ref(false);
const errorMessage = ref("");
const searchTimer = ref(null);

// ── Filters ──────────────────────────────────────────────────────────
const search       = ref("");
const statusFilter = ref(null);
const filterPanel  = ref(null);

const statusOptions = computed(() => [
  { label: t("productOptionGroups.status.active"),   value: true  },
  { label: t("productOptionGroups.status.inactive"), value: false },
]);

const activeFilterCount = computed(() => (statusFilter.value !== null ? 1 : 0));
const hasActiveFilters  = computed(() => activeFilterCount.value > 0);

const clearFilters = () => {
  statusFilter.value = null;
  first.value = 0;
};

// ── Pagination (client-side) ──────────────────────────────────────────
const rows  = ref(10);
const first = ref(0);

const filteredGroups = computed(() => {
  let list = allGroups.value;
  if (search.value.trim()) {
    const q = search.value.trim().toLowerCase();
    list = list.filter((g) => g.name.toLowerCase().includes(q));
  }
  if (statusFilter.value !== null) {
    list = list.filter((g) => g.isActive === statusFilter.value);
  }
  return list;
});

const pagedGroups  = computed(() => filteredGroups.value.slice(first.value, first.value + rows.value));
const totalRecords = computed(() => filteredGroups.value.length);

// ── Summary ───────────────────────────────────────────────────────────
const summary = computed(() => {
  const all = allGroups.value;
  return {
    total:    all.length,
    active:   all.filter((g) => g.isActive).length,
    inactive: all.filter((g) => !g.isActive).length,
  };
});

// ── Columns ───────────────────────────────────────────────────────────
const columns = computed(() => [
  { field: "id",       header: t("productOptionGroups.list.col.id"),     width: "4rem" },
  { field: "name",     header: t("productOptionGroups.list.col.name"),   width: "14rem" },
  { key: "flags",      header: t("productOptionGroups.list.col.flags"),  width: "12rem" },
  { key: "values",     header: t("productOptionGroups.list.col.values"), width: "8rem" },
  { field: "isActive", header: t("productOptionGroups.list.col.status"), width: "8rem" },
  { key: "actions",    header: t("productOptionGroups.list.col.actions"),width: "10rem", toggleable: false },
]);

// ── Helpers ───────────────────────────────────────────────────────────
const statusTag = (isActive) =>
  isActive
    ? { label: t("productOptionGroups.status.active"),   severity: "success" }
    : { label: t("productOptionGroups.status.inactive"), severity: "danger" };

// ── Load ──────────────────────────────────────────────────────────────
const loadGroups = async () => {
  loading.value = true;
  errorMessage.value = "";
  try {
    const res = await getProductOptionGroups();
    const raw = res?.data;
    allGroups.value = Array.isArray(raw)
      ? raw
      : Array.isArray(raw?.value)
      ? raw.value
      : Array.isArray(raw?.items)
      ? raw.items
      : [];
  } catch (err) {
    errorMessage.value = err?.response?.data?.message || t("productOptionGroups.list.error");
  } finally {
    loading.value = false;
  }
};

// ── Actions ───────────────────────────────────────────────────────────
const handleToggleActive = async (row) => {
  try {
    await toggleProductOptionGroupActive(row.id);
    await loadGroups();
    toast.add({
      severity: "success",
      summary: row.isActive
        ? t("productOptionGroups.list.tooltip.deactivate")
        : t("productOptionGroups.list.tooltip.activate"),
      life: 2500,
    });
  } catch (err) {
    toast.add({ severity: "error", summary: err?.response?.data?.message ?? "Lỗi", life: 3000 });
  }
};

const handleDelete = (row) => {
  confirm.require({
    message: t("productOptionGroups.list.deleteConfirm", { name: row.name }),
    header: t("productOptionGroups.list.tooltip.delete"),
    icon: "ph:trash-bold",
    acceptSeverity: "danger",
    accept: async () => {
      try {
        await deleteProductOptionGroup(row.id);
        await loadGroups();
        toast.add({ severity: "success", summary: "Đã xoá", life: 2500 });
      } catch (err) {
        toast.add({ severity: "error", summary: err?.response?.data?.message ?? "Lỗi", life: 3000 });
      }
    },
  });
};

// ── Lifecycle ─────────────────────────────────────────────────────────
onMounted(() => {
  const cached = cache.restore();
  if (cached) {
    search.value       = cached.search       ?? "";
    rows.value         = cached.rows         ?? 10;
    first.value        = cached.first        ?? 0;
    statusFilter.value = cached.statusFilter ?? null;
  }
  loadGroups();
});

onBeforeRouteLeave(() => {
  cache.save({ search: search.value, rows: rows.value, first: first.value, statusFilter: statusFilter.value });
});

watch([search], () => {
  clearTimeout(searchTimer.value);
  searchTimer.value = setTimeout(() => { first.value = 0; }, 300);
});
watch([statusFilter], () => { first.value = 0; });

onBeforeUnmount(() => clearTimeout(searchTimer.value));

// ── Mobile drawer ─────────────────────────────────────────────────────
const drawerGroup   = ref(null);
const drawerVisible = ref(false);
const openDrawer    = (row) => { drawerGroup.value = row; drawerVisible.value = true; };
</script>

<template>
  <section class="tw:space-y-8">

    <!-- Header -->
    <div class="tw:flex tw:flex-wrap tw:items-end tw:justify-between tw:gap-4">
      <div>
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300">
          {{ t("productOptionGroups.breadcrumb") }}
        </p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">{{ t("productOptionGroups.list.title") }}</h1>
        <p class="tw:mt-2 tw:text-sm tw:text-muted">{{ t("productOptionGroups.list.subtitle") }}</p>
      </div>
      <prime-button
        v-if="can('product.create')"
        severity="success"
        size="small"
        @click="router.push({ name: 'productOptionGroupsCreate' })"
      >
        <iconify icon="ph:plus-bold" />
        <span>{{ t("productOptionGroups.list.addGroup") }}</span>
      </prime-button>
    </div>

    <!-- Summary -->
    <div class="tw:grid tw:grid-cols-1 tw:sm:grid-cols-3 tw:gap-3">
      <prime-card class="app-card tw:rounded-xl tw:border">
        <template #content>
          <p class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] app-text-subtle">
            {{ t("productOptionGroups.widgets.total.label") }}
          </p>
          <p class="tw:mt-2 tw:text-2xl tw:font-semibold">{{ summary.total }}</p>
        </template>
      </prime-card>
      <prime-card class="app-card tw:rounded-xl tw:border">
        <template #content>
          <p class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] tw:text-emerald-400">
            {{ t("productOptionGroups.widgets.active.label") }}
          </p>
          <p class="tw:mt-2 tw:text-2xl tw:font-semibold">{{ summary.active }}</p>
        </template>
      </prime-card>
      <prime-card class="app-card tw:rounded-xl tw:border">
        <template #content>
          <p class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] tw:text-red-400">
            {{ t("productOptionGroups.widgets.inactive.label") }}
          </p>
          <p class="tw:mt-2 tw:text-2xl tw:font-semibold">{{ summary.inactive }}</p>
        </template>
      </prime-card>
    </div>

    <!-- Error -->
    <prime-alert
      v-if="errorMessage"
      severity="error"
      variant="accent"
      closable
      @close="errorMessage = ''"
    >{{ errorMessage }}</prime-alert>

    <!-- Table -->
    <AppTable
      v-model:first="first"
      v-model:rows="rows"
      :value="pagedGroups"
      :loading="loading"
      :totalRecords="totalRecords"
      :rowsPerPageOptions="[5, 10, 20, 50]"
      :columns="columns"
      @page="(e) => (first = e.first)"
    >
      <template #toolbar-left>
        <div class="tw:flex tw:items-center tw:gap-2">
          <prime-input-text
            v-model="search"
            :placeholder="t('productOptionGroups.list.searchPlaceholder')"
            class="app-input tw:w-56"
          />

          <prime-button
            :severity="hasActiveFilters ? 'success' : 'secondary'"
            :outlined="!hasActiveFilters"
            v-tooltip.top="t('productOptionGroups.list.filtersTooltip')"
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

          <prime-popover ref="filterPanel">
            <div class="tw:flex tw:flex-col tw:gap-4">
              <p class="tw:text-sm tw:font-semibold">{{ t("productOptionGroups.list.filterTitle") }}</p>
              <div class="tw:space-y-1.5">
                <label class="tw:text-xs tw:text-muted tw:uppercase tw:tracking-widest">
                  {{ t("productOptionGroups.list.col.status") }}
                </label>
                <prime-select
                  v-model="statusFilter"
                  :options="statusOptions"
                  option-label="label"
                  option-value="value"
                  :placeholder="t('productOptionGroups.list.allStatuses')"
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
                <span>{{ t("productOptionGroups.list.clearFilters") }}</span>
              </prime-button>
            </div>
          </prime-popover>
        </div>
      </template>

      <!-- Mobile card -->
      <template #mobile-card="{ data }">
        <div class="tw:rounded-xl tw:border tw:border-slate-200 tw:dark:border-white/10 tw:bg-white tw:dark:bg-white/5 tw:p-3 tw:flex tw:flex-col tw:gap-2">
          <div class="tw:flex tw:items-start tw:justify-between tw:gap-1">
            <span class="tw:font-semibold tw:text-sm tw:leading-snug">{{ data.name }}</span>
            <prime-tag
              :value="statusTag(data.isActive).label"
              :severity="statusTag(data.isActive).severity"
              class="tw:text-[11px]! tw:px-1.5! tw:py-0.5! tw:shrink-0"
            />
          </div>
          <div class="tw:flex tw:flex-wrap tw:gap-1">
            <prime-tag v-if="data.isRequired"    severity="info"    :value="t('productOptionGroups.list.flags.required')"    class="tw:text-[11px]!" />
            <prime-tag v-if="data.allowMultiple" severity="warn"    :value="t('productOptionGroups.list.flags.allowMultiple')" class="tw:text-[11px]!" />
            <prime-tag v-if="data.allowQuantity" severity="secondary" :value="t('productOptionGroups.list.flags.allowQuantity')" class="tw:text-[11px]!" />
          </div>
          <p class="tw:text-xs tw:text-muted">{{ data.values?.length ?? 0 }} lựa chọn</p>
          <div class="tw:border-t tw:border-slate-200 tw:dark:border-white/10 tw:pt-2">
            <prime-button severity="secondary" outlined size="small" fluid @click="openDrawer(data)">
              <iconify icon="ph:dots-three-bold" />
              <span>{{ t("common.moreActions") }}</span>
            </prime-button>
          </div>
        </div>
      </template>

      <!-- Flags column -->
      <template #col-flags="{ data }">
        <div class="tw:flex tw:flex-wrap tw:gap-1">
          <prime-tag v-if="data.isRequired"    severity="info"      :value="t('productOptionGroups.list.flags.required')"     class="tw:text-[11px]!" />
          <prime-tag v-if="data.allowMultiple" severity="warn"      :value="t('productOptionGroups.list.flags.allowMultiple')" class="tw:text-[11px]!" />
          <prime-tag v-if="data.allowQuantity" severity="secondary" :value="t('productOptionGroups.list.flags.allowQuantity')" class="tw:text-[11px]!" />
          <span v-if="!data.isRequired && !data.allowMultiple && !data.allowQuantity" class="tw:text-sm app-text-subtle">—</span>
        </div>
      </template>

      <!-- Values count column -->
      <template #col-values="{ data }">
        <span class="tw:text-sm">{{ data.values?.length ?? 0 }}</span>
      </template>

      <!-- Status column -->
      <template #col-isActive="{ data }">
        <prime-tag :value="statusTag(data.isActive).label" :severity="statusTag(data.isActive).severity" />
      </template>

      <!-- Actions column -->
      <template #col-actions="{ data }">
        <div class="tw:flex tw:justify-end tw:gap-2">
          <prime-button
            :severity="data.isActive ? 'danger' : 'success'"
            outlined
            size="small"
            :class="btnIcon"
            v-tooltip.top="data.isActive ? t('productOptionGroups.list.tooltip.deactivate') : t('productOptionGroups.list.tooltip.activate')"
            @click="handleToggleActive(data)"
          >
            <iconify :icon="data.isActive ? 'ph:toggle-left-bold' : 'ph:toggle-right-bold'" />
          </prime-button>
          <prime-button
            severity="secondary"
            outlined
            size="small"
            :class="btnIcon"
            v-tooltip.top="t('productOptionGroups.list.tooltip.edit')"
            @click="router.push({ name: 'productOptionGroupsDetail', params: { id: data.id } })"
          >
            <iconify icon="ph:pencil-simple-bold" />
          </prime-button>
          <prime-button
            v-if="can('product.delete')"
            severity="danger"
            outlined
            size="small"
            :class="btnIcon"
            v-tooltip.top="t('productOptionGroups.list.tooltip.delete')"
            @click="handleDelete(data)"
          >
            <iconify icon="ph:trash-bold" />
          </prime-button>
        </div>
      </template>
    </AppTable>

    <!-- Mobile drawer -->
    <prime-drawer
      v-model:visible="drawerVisible"
      position="bottom"
      :style="{ height: 'auto' }"
      :pt="{ root: { class: 'tw:rounded-t-2xl' } }"
    >
      <template #header>
        <div class="tw:flex tw:items-center tw:gap-2">
          <span class="tw:font-medium">{{ drawerGroup?.name }}</span>
          <prime-tag
            v-if="drawerGroup"
            :value="statusTag(drawerGroup.isActive).label"
            :severity="statusTag(drawerGroup.isActive).severity"
            class="tw:text-[11px]! tw:px-1.5! tw:py-0.5!"
          />
        </div>
      </template>
      <div v-if="drawerGroup" class="tw:flex tw:flex-col tw:gap-2 tw:pb-4">
        <prime-button
          :label="drawerGroup.isActive ? t('productOptionGroups.list.tooltip.deactivate') : t('productOptionGroups.list.tooltip.activate')"
          :severity="drawerGroup.isActive ? 'danger' : 'success'"
          outlined fluid
          @click="handleToggleActive(drawerGroup); drawerVisible = false"
        >
          <template #icon>
            <iconify :icon="drawerGroup.isActive ? 'ph:toggle-left-bold' : 'ph:toggle-right-bold'" />
          </template>
        </prime-button>
        <prime-button
          :label="t('productOptionGroups.list.tooltip.edit')"
          severity="secondary" outlined fluid
          @click="router.push({ name: 'productOptionGroupsDetail', params: { id: drawerGroup.id } }); drawerVisible = false"
        >
          <template #icon><iconify icon="ph:pencil-simple-bold" /></template>
        </prime-button>
        <prime-button
          v-if="can('product.delete')"
          :label="t('productOptionGroups.list.tooltip.delete')"
          severity="danger" outlined fluid
          @click="handleDelete(drawerGroup); drawerVisible = false"
        >
          <template #icon><iconify icon="ph:trash-bold" /></template>
        </prime-button>
      </div>
    </prime-drawer>

  </section>
</template>
