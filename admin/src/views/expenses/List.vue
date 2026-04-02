<script setup>
import AppTable from "@/components/AppTable.vue";
import ExpenseDialog from "./ExpenseDialog.vue";
import ExpenseSummary from "./ExpenseSummary.vue";
import {
  getExpenses,
  deleteExpense,
  getExpenseSummary,
} from "@/services/expense.service";
import {
  EXPENSE_CATEGORY_MAP,
  EXPENSE_CATEGORY_OPTIONS,
} from "@/constants/expenseCategory";
import {
  EXPENSE_PAYMENT_METHOD_MAP,
  EXPENSE_PAYMENT_METHOD_OPTIONS,
} from "@/constants/expensePaymentMethod";

const { t } = useI18n();
const { can } = usePermission();

// ── Cache ──────────────────────────────────────────────────────────
const { save: saveCache, restore: restoreCache } = useTableCache("expenses-list");

// ── PrimeVue services ─────────────────────────────────────────────
const confirm = useConfirm();
const toast = useToast();

// ── P&L summary ───────────────────────────────────────────────────
const summary = ref(null);
const summaryLoading = ref(false);

// ── Table state ────────────────────────────────────────────────────
const expenses = ref([]);
const totalRecords = ref(0);
const rows = ref(20);
const first = ref(0);
const loading = ref(false);
const errorMessage = ref("");

// ── Column visibility ────────────────────────────────────────────
const colDefs = ref([
  { field: 'purchaseDate',  header: t('expenses.list.col.date'),      width: '8rem',  visible: true },
  { field: 'name',          header: t('expenses.list.col.item'),       width: '12rem', visible: true },
  { field: 'category',      header: t('expenses.list.col.category'),   width: '9rem',  visible: true },
  { field: 'paymentMethod', header: t('expenses.list.col.payment'),    width: '9rem',  visible: true },
  { key:   'qty',           header: t('expenses.list.col.qty'),        width: '7rem',  visible: true },
  { field: 'unitPrice',     header: t('expenses.list.col.unitPrice'),  width: '9rem',  visible: true },
  { field: 'totalAmount',   header: t('expenses.list.col.total'),      width: '9rem',  visible: true },
  { field: 'notes',         header: t('expenses.list.col.notes'),      width: '10rem', visible: true },
  { key:   'actions',       header: t('expenses.list.col.actions'),    width: '8rem',  toggleable: false },
]);

// ── Filters ────────────────────────────────────────────────────────
const todayMidnight = () => {
  const d = new Date();
  d.setHours(0, 0, 0, 0);
  return d;
};
const startOfMonth = () => {
  const d = new Date();
  d.setDate(1);
  d.setHours(0, 0, 0, 0);
  return d;
};

const dateRange = ref([startOfMonth(), todayMidnight()]);
const dateFrom = computed(() => dateRange.value?.[0] ?? null);
const dateTo = computed(() => dateRange.value?.[1] ?? null);
const categoryFilter = ref(null);
const paymentMethodFilter = ref(null);
const filterPanel = ref(null);

// ── Helpers ───────────────────────────────────────────────────────
const toMidnight = (d) => {
  if (!d) return undefined;
  return new Date(d.getFullYear(), d.getMonth(), d.getDate());
};

const formatVnd = (value) =>
  new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
    maximumFractionDigits: 0,
  }).format(value ?? 0);

const formatDate = (dateStr) =>
  new Intl.DateTimeFormat("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  }).format(new Date(dateStr));

const categoryMeta = (cat) =>
  EXPENSE_CATEGORY_MAP[cat] ?? { label: cat, icon: "ph:tag-bold", severity: "secondary" };

// ── Filter helpers ─────────────────────────────────────────────────
const activeFilterCount = computed(() => {
  let n = 0;
  if (categoryFilter.value !== null) n++;
  if (paymentMethodFilter.value !== null) n++;
  return n;
});
const hasActiveFilters = computed(() => activeFilterCount.value > 0);
const clearFilters = () => {
  categoryFilter.value = null;
  paymentMethodFilter.value = null;
  first.value = 0;
};

// ── Load ──────────────────────────────────────────────────────────
const saveCurrentState = () => {
  saveCache({
    rows: rows.value,
    first: first.value,
    categoryFilter: categoryFilter.value,
    paymentMethodFilter: paymentMethodFilter.value,
    dateFrom: dateFrom.value?.toISOString?.() ?? dateFrom.value,
    dateTo: dateTo.value?.toISOString?.() ?? dateTo.value,
    colDefs: colDefs.value,
  });
};

const loadSummary = async () => {
  summaryLoading.value = true;
  try {
    const res = await getExpenseSummary({
      dateFrom: toMidnight(dateFrom.value),
      dateTo: toMidnight(dateTo.value),
      category: categoryFilter.value || undefined,
      paymentMethod: paymentMethodFilter.value || undefined,
    });
    summary.value = res?.data;
  } catch {
    // non-critical
  } finally {
    summaryLoading.value = false;
  }
};

const loadExpenses = async () => {
  loading.value = true;
  errorMessage.value = "";
  try {
    const page = Math.floor(first.value / rows.value) + 1;
    const res = await getExpenses({
      category: categoryFilter.value || undefined,
      paymentMethod: paymentMethodFilter.value || undefined,
      dateFrom: toMidnight(dateFrom.value),
      dateTo: toMidnight(dateTo.value),
      page,
      pageSize: rows.value,
    });
    const data = res?.data;
    expenses.value = data?.items ?? [];
    totalRecords.value = data?.totalCount ?? 0;
    saveCurrentState();
  } catch (err) {
    errorMessage.value = err?.response?.data?.message || t('expenses.error.loadFailed');
  } finally {
    loading.value = false;
  }
};

const loadAll = () => Promise.all([loadSummary(), loadExpenses()]);

// ── Create / Edit dialog ──────────────────────────────────────────
const dialogVisible = ref(false);
const dialogExpense = ref(null);
const openCreateDialog = () => { dialogExpense.value = null; dialogVisible.value = true; };
const openEditDialog = (expense) => { dialogExpense.value = expense; dialogVisible.value = true; };
const onExpenseSaved = () => { first.value = 0; loadAll(); };

// ── Mobile action drawer ───────────────────────────────────────────
const drawerExpense = ref(null);
const drawerVisible = ref(false);
const openDrawer = (row) => { drawerExpense.value = row; drawerVisible.value = true; };

// ── Delete ────────────────────────────────────────────────────────
const handleDelete = (expense) => {
  confirm.require({
    message: t('expenses.confirm.deleteMessage', { name: expense.name }),
    header: t('expenses.confirm.deleteHeader'),
    acceptSeverity: "danger",
    acceptLabel: t('expenses.confirm.deleteAccept'),
    rejectLabel: t('expenses.confirm.deleteReject'),
    accept: async () => {
      try {
        await deleteExpense(expense.id);
        toast.add({
          severity: "success",
          summary: t('expenses.toast.deletedTitle'),
          detail: t('expenses.toast.deletedDetail'),
          life: 3000,
        });
        if (expenses.value.length === 1 && first.value > 0) {
          first.value = Math.max(0, first.value - rows.value);
        }
        await loadAll();
      } catch (err) {
        errorMessage.value = err?.response?.data?.title || t('expenses.error.deleteFailed');
      }
    },
  });
};

// ── Lifecycle & watchers ──────────────────────────────────────────
onMounted(() => {
  const cached = restoreCache();
  if (cached) {
    if (cached.rows !== undefined) rows.value = cached.rows;
    if (cached.first !== undefined) first.value = cached.first;
    if (cached.categoryFilter !== undefined) categoryFilter.value = cached.categoryFilter;
    if (cached.paymentMethodFilter !== undefined) paymentMethodFilter.value = cached.paymentMethodFilter;
    if (cached.dateFrom || cached.dateTo) {
      dateRange.value = [
        cached.dateFrom ? new Date(cached.dateFrom) : null,
        cached.dateTo ? new Date(cached.dateTo) : null,
      ];
    }
    if (cached.colDefs) {
      const cachedVis = {};
      for (const c of cached.colDefs) cachedVis[c.key ?? c.field] = c.visible !== false;
      colDefs.value = colDefs.value.map((c) => {
        const id = c.key ?? c.field;
        return id in cachedVis ? { ...c, visible: cachedVis[id] } : c;
      });
    }
  }
  loadAll();
});

onBeforeRouteLeave(() => {
  saveCache({
    rows: rows.value,
    first: first.value,
    categoryFilter: categoryFilter.value,
    paymentMethodFilter: paymentMethodFilter.value,
    dateFrom: dateFrom.value?.toISOString?.() ?? dateFrom.value,
    dateTo: dateTo.value?.toISOString?.() ?? dateTo.value,
    colDefs: colDefs.value,
  });
});

watch(dateRange, (val) => {
  if (val?.[0] && val?.[1]) {
    first.value = 0;
    loadAll();
  }
});

watch([categoryFilter, paymentMethodFilter], () => {
  first.value = 0;
  loadAll();
});

const onPage = (e) => {
  first.value = e.first;
  rows.value = e.rows;
  loadExpenses();
};
</script>

<template>
  <prime-confirm-dialog />
  <ExpenseDialog
    v-model:visible="dialogVisible"
    :expense="dialogExpense"
    @saved="onExpenseSaved"
  />

  <section class="tw:space-y-8">
    <!-- Header -->
    <div class="tw:flex tw:flex-wrap tw:items-end tw:justify-between tw:gap-4">
      <div>
        <p class="tw:text-[11px] tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300">{{ t('expenses.groupLabel') }}</p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">{{ t('expenses.title') }}</h1>
        <p class="tw:mt-2 tw:text-sm app-text-muted">{{ t('expenses.subtitle') }}</p>
      </div>
      <div class="tw:flex tw:items-center tw:gap-2">
        <prime-button severity="secondary" outlined size="small" :loading="loading || summaryLoading" @click="loadAll">
          <iconify icon="ph:arrows-clockwise-bold" class="tw:mr-1" />
          <span>{{ t('expenses.refresh') }}</span>
        </prime-button>
        <prime-button v-if="can('expense.create')" severity="success" size="small" @click="openCreateDialog">
          <iconify icon="ph:plus-bold" class="tw:mr-1" />
          <span>{{ t('expenses.newExpense') }}</span>
        </prime-button>
      </div>
    </div>

    <!-- P&L Summary -->
    <ExpenseSummary :summary="summary" :loading="summaryLoading" />

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
      v-model:columns="colDefs"
      :value="expenses"
      :loading="loading"
      :totalRecords="totalRecords"
      :rowsPerPageOptions="[10, 20, 50]"
      :show-column-toggle="true"
      @page="onPage"
    >
      <template #toolbar-left>
        <div class="tw:flex tw:items-center tw:gap-2">
          <!-- Filter toggle -->
          <prime-button
            :severity="hasActiveFilters ? 'success' : 'secondary'"
            :outlined="!hasActiveFilters"
            v-tooltip.top="t('expenses.filter.filtersTooltip')"
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
            <div class="tw:flex tw:flex-col tw:gap-4 tw:w-72">
              <p class="tw:text-sm tw:font-semibold">{{ t('expenses.filter.title') }}</p>

              <!-- Date range -->
              <div class="tw:space-y-1.5">
                <label class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest">
                  {{ t('expenses.filter.dateRange') }}
                </label>
                <prime-date-picker
                  v-model="dateRange"
                  selection-mode="range"
                  :number-of-months="1"
                  date-format="dd/mm/yy"
                  show-button-bar
                  class="app-input tw:w-full"
                />
              </div>

              <!-- Payment method -->
              <div class="tw:space-y-1.5">
                <label class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest">
                  {{ t('expenses.filter.payment') }}
                </label>
                <prime-select
                  v-model="paymentMethodFilter"
                  :options="EXPENSE_PAYMENT_METHOD_OPTIONS"
                  option-label="label"
                  option-value="value"
                  :placeholder="t('expenses.filter.allPayments')"
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

              <!-- Category -->
              <div class="tw:space-y-1.5">
                <label class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest">
                  {{ t('expenses.filter.category') }}
                </label>
                <prime-select
                  v-model="categoryFilter"
                  :options="EXPENSE_CATEGORY_OPTIONS"
                  option-label="label"
                  option-value="value"
                  :placeholder="t('expenses.filter.allCategories')"
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

              <prime-button v-if="hasActiveFilters" severity="danger" outlined size="small" @click="clearFilters">
                <iconify icon="ph:x-bold" />
                <span>{{ t('expenses.filter.clearFilters') }}</span>
              </prime-button>
            </div>
          </prime-popover>
        </div>
      </template>

      <template #col-purchaseDate="{ data }">
        <span class="tw:text-sm app-text-muted">{{ formatDate(data.purchaseDate) }}</span>
      </template>

      <template #col-name="{ data }">
        <span class="tw:font-semibold tw:text-sm">{{ data.name }}</span>
      </template>

      <template #col-category="{ data }">
        <div class="tw:flex tw:items-center tw:gap-1.5">
          <prime-tag :severity="categoryMeta(data.category).severity">
            <iconify :icon="categoryMeta(data.category).icon" class="tw:text-sm app-text-muted" />
            <span>{{ categoryMeta(data.category).label }}</span>
          </prime-tag>
        </div>
      </template>

      <template #col-paymentMethod="{ data }">
        <div class="tw:flex tw:items-center tw:gap-1.5">
          <prime-tag :severity="EXPENSE_PAYMENT_METHOD_MAP[data.paymentMethod]?.severity ?? 'secondary'">
            <iconify :icon="EXPENSE_PAYMENT_METHOD_MAP[data.paymentMethod]?.icon ?? 'ph:money-bold'" class="tw:text-sm app-text-muted" />
            <span>{{ EXPENSE_PAYMENT_METHOD_MAP[data.paymentMethod]?.label ?? data.paymentMethod }}</span>
          </prime-tag>
        </div>
      </template>

      <template #col-qty="{ data }">
        <span class="tw:text-sm">
          {{ data.quantity }}
          <span v-if="data.unit" class="app-text-muted tw:ml-0.5">{{ data.unit }}</span>
        </span>
      </template>

      <template #col-unitPrice="{ data }">
        <span class="tw:text-sm app-text-muted">{{ formatVnd(data.unitPrice) }}</span>
      </template>

      <template #col-totalAmount="{ data }">
        <span class="tw:font-semibold tw:text-sm">{{ formatVnd(data.totalAmount) }}</span>
      </template>

      <template #col-notes="{ data }">
        <span v-if="data.notes" class="tw:text-sm app-text-muted tw:line-clamp-1">{{ data.notes }}</span>
        <span v-else class="app-text-subtle">—</span>
      </template>

      <template #col-actions="{ data }">
        <div class="tw:flex tw:justify-end tw:items-center tw:gap-2">
          <prime-button
            v-if="can('expense.update')"
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
            v-if="can('expense.delete')"
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

      <template #mobile-card="{ data }">
        <div class="tw:rounded-xl tw:border tw:border-slate-200 tw:dark:border-white/15 tw:p-3 tw:flex tw:flex-col tw:gap-2">
          <div class="tw:flex tw:items-center tw:justify-between tw:gap-1">
            <span class="tw:text-xs app-text-muted">{{ formatDate(data.purchaseDate) }}</span>
            <span class="tw:font-semibold tw:text-sm">{{ formatVnd(data.totalAmount) }}</span>
          </div>
          <span class="tw:font-medium tw:text-sm tw:leading-snug">{{ data.name }}</span>
          <div class="tw:flex tw:flex-wrap tw:items-center tw:gap-1.5">
            <prime-tag :severity="categoryMeta(data.category).severity" class="tw:text-[11px]! tw:px-1.5! tw:py-0.5!">
              <iconify :icon="categoryMeta(data.category).icon" class="tw:text-xs" />
              <span>{{ categoryMeta(data.category).label }}</span>
            </prime-tag>
            <prime-tag :severity="EXPENSE_PAYMENT_METHOD_MAP[data.paymentMethod]?.severity ?? 'secondary'" class="tw:text-[11px]! tw:px-1.5! tw:py-0.5!">
              <iconify :icon="EXPENSE_PAYMENT_METHOD_MAP[data.paymentMethod]?.icon ?? 'ph:money-bold'" class="tw:text-xs" />
              <span>{{ EXPENSE_PAYMENT_METHOD_MAP[data.paymentMethod]?.label ?? data.paymentMethod }}</span>
            </prime-tag>
          </div>
          <p class="tw:text-xs app-text-muted">
            {{ data.quantity }}<span v-if="data.unit"> {{ data.unit }}</span> × {{ formatVnd(data.unitPrice) }}
          </p>
          <div class="tw:border-t tw:border-slate-200 tw:dark:border-white/10 tw:pt-2">
            <prime-button severity="secondary" outlined size="small" fluid @click="openDrawer(data)">
              <iconify icon="ph:dots-three-bold" />
              <span>{{ t('common.moreActions') }}</span>
            </prime-button>
          </div>
        </div>
      </template>
    </AppTable>

    <!-- Mobile action drawer -->
    <prime-drawer
      v-model:visible="drawerVisible"
      position="bottom"
      :style="{ height: 'auto' }"
      :pt="{ root: { class: 'tw:rounded-t-2xl' } }"
    >
      <template #header>
        <div class="tw:flex tw:flex-col tw:gap-0.5">
          <span class="tw:font-medium">{{ drawerExpense?.name }}</span>
          <span v-if="drawerExpense" class="tw:text-xs app-text-muted">
            {{ formatDate(drawerExpense.purchaseDate) }} · {{ formatVnd(drawerExpense.totalAmount) }}
          </span>
        </div>
      </template>
      <div v-if="drawerExpense" class="tw:flex tw:flex-col tw:gap-2 tw:pb-4">
        <prime-button
          v-if="can('expense.update')"
          :label="t('common.edit')"
          severity="secondary"
          outlined
          fluid
          @click="openEditDialog(drawerExpense); drawerVisible = false"
        >
          <template #icon><iconify icon="ph:pencil-bold" /></template>
        </prime-button>
        <prime-button
          v-if="can('expense.delete')"
          :label="t('common.delete')"
          severity="danger"
          outlined
          fluid
          @click="handleDelete(drawerExpense); drawerVisible = false"
        >
          <template #icon><iconify icon="ph:trash-bold" /></template>
        </prime-button>
      </div>
    </prime-drawer>
  </section>
</template>
