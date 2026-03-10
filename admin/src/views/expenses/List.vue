<script setup>
import AppTable from "@/components/AppTable.vue";
import StatCard from "@/components/widgets/StatCard.vue";
import {
  getExpenses,
  createExpense,
  updateExpense,
  deleteExpense,
  getExpenseSummary,
} from "@/services/expense.service";
import {
  EXPENSE_CATEGORY,
  EXPENSE_CATEGORY_MAP,
  EXPENSE_CATEGORY_OPTIONS,
} from "@/constants/expenseCategory";
import {
  EXPENSE_PAYMENT_METHOD,
  EXPENSE_PAYMENT_METHOD_MAP,
  EXPENSE_PAYMENT_METHOD_OPTIONS,
} from "@/constants/expensePaymentMethod";
import { btnIcon } from "@/layout/ui";

const { can } = usePermission();

// ── Cache ──────────────────────────────────────────────────────────
const { save: saveCache, restore: restoreCache } =
  useTableCache("expenses-list");

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
  { field: "purchaseDate", header: "Date", visible: true },
  { field: "name", header: "Item", visible: true },
  { field: "category", header: "Category", visible: true },
  { field: "paymentMethod", header: "Payment", visible: true },
  { field: "qty", header: "Qty", visible: true },
  { field: "unitPrice", header: "Unit price", visible: true },
  { field: "totalAmount", header: "Total", visible: true },
  { field: "notes", header: "Notes", visible: true },
]);
const colVisible = (field) =>
  colDefs.value.find((c) => c.field === field)?.visible !== false;

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
const filterPanel = ref(null);

// ── Restore cache ─────────────────────────────────────────────────
const _cached = restoreCache();
if (_cached) {
  if (_cached.rows !== undefined) rows.value = _cached.rows;
  if (_cached.first !== undefined) first.value = _cached.first;
  if (_cached.categoryFilter !== undefined)
    categoryFilter.value = _cached.categoryFilter;
  if (_cached.dateFrom || _cached.dateTo) {
    dateRange.value = [
      _cached.dateFrom ? new Date(_cached.dateFrom) : null,
      _cached.dateTo ? new Date(_cached.dateTo) : null,
    ];
  }
  if (_cached.colDefs) colDefs.value = _cached.colDefs;
}

// ── Helpers ───────────────────────────────────────────────────────
// Normalize date về midnight local time trước khi gửi API
// (tránh lỗi khi user nhấn "Today" button lúc không phải midnight)
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
  EXPENSE_CATEGORY_MAP[cat] ?? {
    label: cat,
    icon: "ph:tag-bold",
    severity: "secondary",
  };

// ── Filter helpers ─────────────────────────────────────────────────
const activeFilterCount = computed(() => {
  let n = 0;
  if (categoryFilter.value !== null) n++;
  return n;
});

const hasActiveFilters = computed(() => activeFilterCount.value > 0);

const clearFilters = () => {
  categoryFilter.value = null;
  first.value = 0;
};

// ── Computed P&L ──────────────────────────────────────────────────
const revenue = computed(
  () => summary.value?.revenue ?? { cash: 0, bank: 0, total: 0 },
);
const expenseBreakdown = computed(
  () =>
    summary.value?.expenses ?? {
      ingredient: 0,
      supply: 0,
      equipment: 0,
      other: 0,
      total: 0,
    },
);
const profit = computed(() => summary.value?.profit ?? 0);
const profitMargin = computed(() => {
  const rev = revenue.value.total;
  if (!rev) return null;
  return ((profit.value / rev) * 100).toFixed(1);
});

// ── Load ──────────────────────────────────────────────────────────
const saveCurrentState = () => {
  saveCache({
    rows: rows.value,
    first: first.value,
    categoryFilter: categoryFilter.value,
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
    });
    summary.value = res?.data;
  } catch {
    // non-critical — table still loads
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
    errorMessage.value =
      err?.response?.data?.message || "Failed to load expenses.";
  } finally {
    loading.value = false;
  }
};

const loadAll = () => Promise.all([loadSummary(), loadExpenses()]);

onMounted(loadAll);

// Date range → reload both summary + table (wait for both dates to be selected)
watch(dateRange, (val) => {
  if (val?.[0] && val?.[1]) {
    first.value = 0;
    loadAll();
  }
});

// Category filter → reload table only (summary always shows all categories)
watch(categoryFilter, () => {
  first.value = 0;
  loadExpenses();
});

const onPage = (e) => {
  first.value = e.first;
  rows.value = e.rows;
  loadExpenses();
};

// ── Create / Edit dialog ──────────────────────────────────────────
const dialogVisible = ref(false);
const editingExpense = ref(null);
const formLoading = ref(false);
const formError = ref("");

// Form fields
const fName = ref("");
const fCategory = ref(EXPENSE_CATEGORY.INGREDIENT);
const fPaymentMethod = ref(EXPENSE_PAYMENT_METHOD.CASH);
const fQuantity = ref(null);
const fUnit = ref("");
const fUnitPrice = ref(null);
const fPurchaseDate = ref(todayMidnight());
const fNotes = ref("");

const fTotalAmount = computed(
  () => (fQuantity.value ?? 0) * (fUnitPrice.value ?? 0),
);
const isEditMode = computed(() => editingExpense.value !== null);
const dialogHeader = computed(() =>
  isEditMode.value ? "Edit expense" : "New expense",
);

const openCreateDialog = () => {
  editingExpense.value = null;
  fName.value = "";
  fCategory.value = EXPENSE_CATEGORY.INGREDIENT;
  fPaymentMethod.value = EXPENSE_PAYMENT_METHOD.CASH;
  fQuantity.value = null;
  fUnit.value = "";
  fUnitPrice.value = null;
  fPurchaseDate.value = todayMidnight();
  fNotes.value = "";
  formError.value = "";
  dialogVisible.value = true;
};

const openEditDialog = (expense) => {
  editingExpense.value = expense;
  fName.value = expense.name;
  fCategory.value = expense.category;
  fPaymentMethod.value = expense.paymentMethod ?? EXPENSE_PAYMENT_METHOD.CASH;
  fQuantity.value = expense.quantity;
  fUnit.value = expense.unit ?? "";
  fUnitPrice.value = expense.unitPrice;
  fPurchaseDate.value = new Date(expense.purchaseDate);
  fNotes.value = expense.notes ?? "";
  formError.value = "";
  dialogVisible.value = true;
};

const submitForm = async () => {
  formError.value = "";
  if (!fName.value.trim()) {
    formError.value = "Item name is required.";
    return;
  }
  if (!fQuantity.value || fQuantity.value <= 0) {
    formError.value = "Quantity must be greater than 0.";
    return;
  }
  if (fUnitPrice.value === null || fUnitPrice.value < 0) {
    formError.value = "Unit price must be 0 or greater.";
    return;
  }

  formLoading.value = true;
  try {
    const payload = {
      name: fName.value.trim(),
      category: fCategory.value,
      paymentMethod: fPaymentMethod.value,
      quantity: fQuantity.value,
      unit: fUnit.value.trim() || null,
      unitPrice: fUnitPrice.value,
      purchaseDate: fPurchaseDate.value?.toISOString?.() ?? fPurchaseDate.value,
      notes: fNotes.value.trim() || null,
    };

    if (isEditMode.value) {
      await updateExpense(editingExpense.value.id, payload);
      toast.add({
        severity: "success",
        summary: "Updated",
        detail: "Expense updated.",
        life: 3000,
      });
    } else {
      await createExpense(payload);
      toast.add({
        severity: "success",
        summary: "Created",
        detail: "Expense recorded.",
        life: 3000,
      });
    }

    dialogVisible.value = false;
    first.value = 0;
    await loadAll();
  } catch (err) {
    formError.value =
      err?.response?.data?.errors?.join(", ") ||
      err?.response?.data?.title ||
      "Failed to save expense.";
  } finally {
    formLoading.value = false;
  }
};

const handleDelete = (expense) => {
  confirm.require({
    message: `Delete "${expense.name}"? This cannot be undone.`,
    header: "Confirm delete",
    acceptSeverity: "danger",
    acceptLabel: "Delete",
    rejectLabel: "Cancel",
    accept: async () => {
      try {
        await deleteExpense(expense.id);
        toast.add({
          severity: "success",
          summary: "Deleted",
          detail: "Expense deleted.",
          life: 3000,
        });
        if (expenses.value.length === 1 && first.value > 0) {
          first.value = Math.max(0, first.value - rows.value);
        }
        await loadAll();
      } catch (err) {
        errorMessage.value =
          err?.response?.data?.title || "Failed to delete expense.";
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
    :style="{ width: '32rem' }"
  >
    <div class="tw:space-y-4">
      <!-- Purchase date -->
      <div class="tw:space-y-1.5">
        <label
          class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted"
        >
          Purchase date
        </label>
        <prime-date-picker
          v-model="fPurchaseDate"
          date-format="dd/mm/yy"
          show-button-bar
          class="app-input tw:w-full"
        />
      </div>

      <!-- Item name -->
      <div class="tw:space-y-1.5">
        <label
          class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted"
        >
          Item name <span class="tw:text-red-400">*</span>
        </label>
        <prime-input-text
          v-model="fName"
          placeholder="e.g. Fresh milk, Trash bags…"
          class="app-input tw:w-full"
        />
      </div>

      <!-- Category + Payment method -->
      <div class="tw:grid tw:grid-cols-2 tw:gap-3">
        <div class="tw:space-y-1.5">
          <label
            class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted"
          >
            Category
          </label>
          <prime-select
            v-model="fCategory"
            :options="EXPENSE_CATEGORY_OPTIONS"
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
                <iconify :icon="EXPENSE_CATEGORY_MAP[value]?.icon" />
                <span>{{ EXPENSE_CATEGORY_MAP[value]?.label }}</span>
              </div>
            </template>
          </prime-select>
        </div>

        <div class="tw:space-y-1.5">
          <label
            class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted"
          >
            Payment
          </label>
          <prime-select
            v-model="fPaymentMethod"
            :options="EXPENSE_PAYMENT_METHOD_OPTIONS"
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
                <iconify :icon="EXPENSE_PAYMENT_METHOD_MAP[value]?.icon" />
                <span>{{ EXPENSE_PAYMENT_METHOD_MAP[value]?.label }}</span>
              </div>
            </template>
          </prime-select>
        </div>
      </div>

      <!-- Qty + Unit -->
      <div class="tw:grid tw:grid-cols-2 tw:gap-3">
        <div class="tw:space-y-1.5">
          <label
            class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted"
          >
            Quantity <span class="tw:text-red-400">*</span>
          </label>
          <prime-input-number
            v-model="fQuantity"
            :min="0"
            :max-fraction-digits="3"
            :use-grouping="true"
            placeholder="0"
            class="app-input tw:w-full"
            @input="(e) => (fQuantity = e.value)"
          />
        </div>
        <div class="tw:space-y-1.5">
          <label
            class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted"
          >
            Unit
          </label>
          <prime-input-text
            v-model="fUnit"
            placeholder="box, kg, bottle…"
            class="app-input tw:w-full"
          />
        </div>
      </div>

      <!-- Unit price + Total (computed) -->
      <div class="tw:grid tw:grid-cols-2 tw:gap-3">
        <div class="tw:space-y-1.5">
          <label
            class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted"
          >
            Unit price <span class="tw:text-red-400">*</span>
          </label>
          <prime-input-number
            v-model="fUnitPrice"
            :min="0"
            :use-grouping="true"
            suffix=" ₫"
            placeholder="0 ₫"
            class="app-input tw:w-full"
            @input="(e) => (fUnitPrice = e.value)"
          />
        </div>
        <div class="tw:space-y-1.5">
          <label
            class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted"
          >
            Total (auto)
          </label>
          <div
            class="tw:flex tw:items-center tw:h-10 tw:px-3 tw:rounded-lg tw:border tw:border-white/10 tw:bg-white/5 tw:text-sm tw:font-semibold"
          >
            {{ formatVnd(fTotalAmount) }}
          </div>
        </div>
      </div>

      <!-- Notes -->
      <div class="tw:space-y-1.5">
        <label
          class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted"
        >
          Notes
        </label>
        <prime-textarea
          v-model="fNotes"
          :rows="2"
          placeholder="Additional notes…"
          class="app-input tw:w-full"
          auto-resize
        />
      </div>

      <p v-if="formError" class="tw:text-xs tw:text-red-400">{{ formError }}</p>
    </div>

    <template #footer>
      <prime-button
        severity="secondary"
        outlined
        @click="dialogVisible = false"
      >
        Cancel
      </prime-button>
      <prime-button
        :severity="isEditMode ? 'primary' : 'success'"
        :loading="formLoading"
        @click="submitForm"
      >
        <iconify :icon="isEditMode ? 'ph:check-bold' : 'ph:plus-bold'" />
        <span>{{ isEditMode ? "Save changes" : "Add expense" }}</span>
      </prime-button>
    </template>
  </prime-dialog>

  <section class="tw:space-y-8">
    <!-- Header -->
    <div class="tw:flex tw:flex-wrap tw:items-end tw:justify-between tw:gap-4">
      <div>
        <p
          class="tw:text-[11px] tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300"
        >
          Operations
        </p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">Expenses</h1>
        <p class="tw:mt-2 tw:text-sm app-text-muted">
          Track purchases and compare with revenue (P&amp;L).
        </p>
      </div>
      <div class="tw:flex tw:items-center tw:gap-2">
        <prime-button
          severity="secondary"
          outlined
          size="small"
          :loading="loading || summaryLoading"
          @click="loadAll"
        >
          <iconify icon="ph:arrows-clockwise-bold" class="tw:mr-1" />
          <span>Refresh</span>
        </prime-button>
        <prime-button
          v-if="can('expense.create')"
          severity="success"
          size="small"
          @click="openCreateDialog"
        >
          <iconify icon="ph:plus-bold" class="tw:mr-1" />
          <span>New expense</span>
        </prime-button>
      </div>
    </div>

    <!-- Date range (applies to both summary + table) -->
    <prime-date-picker
      v-model="dateRange"
      selection-mode="range"
      :number-of-months="2"
      date-format="dd/mm/yy"
      show-button-bar
      class="app-input tw:w-55"
    />

    <!-- P&L summary cards -->
    <div class="tw:grid tw:grid-cols-3 tw:gap-3 sm:tw:grid-cols-3">
      <!-- Revenue -->
      <stat-card
        label="Revenue"
        label-class="tw:text-emerald-400"
        :value="summaryLoading ? '…' : formatVnd(revenue.total)"
      >
        <template #icon>
          <iconify
            icon="ph:trend-up-bold"
            class="tw:text-emerald-400 tw:opacity-60"
          />
        </template>
        <template #sub>
          <div
            class="tw:mt-2 tw:flex tw:flex-wrap tw:gap-x-3 tw:gap-y-0.5 tw:text-xs app-text-muted"
          >
            <span
              >Cash
              <span class="tw:text-white/70">{{
                formatVnd(revenue.cash)
              }}</span></span
            >
            <span
              >Bank
              <span class="tw:text-white/70">{{
                formatVnd(revenue.bank)
              }}</span></span
            >
          </div>
        </template>
      </stat-card>

      <!-- Expenses -->
      <stat-card
        label="Expenses"
        label-class="tw:text-red-400"
        :value="summaryLoading ? '…' : formatVnd(expenseBreakdown.total)"
      >
        <template #icon>
          <iconify
            icon="ph:trend-down-bold"
            class="tw:text-red-400 tw:opacity-60"
          />
        </template>
        <template #sub>
          <div
            class="tw:mt-2 tw:flex tw:flex-wrap tw:gap-x-3 tw:gap-y-0.5 tw:text-xs app-text-muted"
          >
            <span
              >Cash
              <span class="tw:text-white/70">{{
                formatVnd(expenseBreakdown.cash)
              }}</span></span
            >
            <span
              >Bank
              <span class="tw:text-white/70">{{
                formatVnd(expenseBreakdown.bank)
              }}</span></span
            >
          </div>
        </template>
      </stat-card>

      <!-- Profit -->
      <stat-card
        label="Profit"
        :label-class="profit >= 0 ? 'tw:text-emerald-400' : 'tw:text-red-400'"
        :value="summaryLoading ? '…' : formatVnd(profit)"
      >
        <template #icon>
          <iconify
            icon="ph:chart-bar-bold"
            :class="
              profit >= 0
                ? 'tw:text-emerald-400 tw:opacity-60'
                : 'tw:text-red-400 tw:opacity-60'
            "
          />
        </template>
        <template #sub>
          <div class="tw:mt-2 tw:text-xs app-text-muted">
            <span v-if="profitMargin !== null">
              Margin
              <span
                :class="
                  profit >= 0
                    ? 'tw:text-emerald-400 tw:font-semibold'
                    : 'tw:text-red-400 tw:font-semibold'
                "
                >{{ profitMargin }}%</span
              >
            </span>
            <span v-else>—</span>
          </div>
        </template>
      </stat-card>
    </div>

    <!-- Error -->
    <prime-message
      v-if="errorMessage"
      severity="error"
      size="small"
      variant="simple"
      :closable="true"
      @close="errorMessage = ''"
      >{{ errorMessage }}</prime-message
    >

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
            <prime-badge
              v-if="activeFilterCount > 0"
              :value="activeFilterCount"
              severity="danger"
              class="tw:ml-1 tw:scale-90"
            />
          </prime-button>

          <!-- Filter popover -->
          <prime-popover ref="filterPanel">
            <div class="tw:flex tw:flex-col tw:gap-4 tw:w-56">
              <p class="tw:text-sm tw:font-semibold">Filter expenses</p>

              <!-- Category -->
              <div class="tw:space-y-1.5">
                <label
                  class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest"
                >
                  Category
                </label>
                <prime-select
                  v-model="categoryFilter"
                  :options="EXPENSE_CATEGORY_OPTIONS"
                  option-label="label"
                  option-value="value"
                  placeholder="All categories"
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

      <!-- Date -->
      <prime-column
        v-if="colVisible('purchaseDate')"
        field="purchaseDate"
        header="Date"
        style="min-width: 8rem"
      >
        <template #body="{ data }">
          <span class="tw:text-sm app-text-muted">{{
            formatDate(data.purchaseDate)
          }}</span>
        </template>
      </prime-column>

      <!-- Item name -->
      <prime-column
        v-if="colVisible('name')"
        field="name"
        header="Item"
        style="min-width: 12rem"
      >
        <template #body="{ data }">
          <span class="tw:font-semibold tw:text-sm">{{ data.name }}</span>
        </template>
      </prime-column>

      <!-- Category -->
      <prime-column
        v-if="colVisible('category')"
        field="category"
        header="Category"
        style="min-width: 9rem"
      >
        <template #body="{ data }">
          <div class="tw:flex tw:items-center tw:gap-1.5">
            <prime-tag :severity="categoryMeta(data.category).severity">
              <iconify
                :icon="categoryMeta(data.category).icon"
                class="tw:text-sm app-text-muted"
              />
              <span>
                {{ categoryMeta(data.category).label }}
              </span>
            </prime-tag>
          </div>
        </template>
      </prime-column>

      <!-- Payment method -->
      <prime-column
        v-if="colVisible('paymentMethod')"
        field="paymentMethod"
        header="Payment"
        class="tw:min-w-36"
      >
        <template #body="{ data }">
          <div class="tw:flex tw:items-center tw:gap-1.5">
            <prime-tag
              :severity="
                EXPENSE_PAYMENT_METHOD_MAP[data.paymentMethod]?.severity ??
                'secondary'
              "
            >
              <iconify
                :icon="
                  EXPENSE_PAYMENT_METHOD_MAP[data.paymentMethod]?.icon ??
                  'ph:money-bold'
                "
                class="tw:text-sm app-text-muted"
              />
              <span>
                {{
                  EXPENSE_PAYMENT_METHOD_MAP[data.paymentMethod]?.label ??
                  data.paymentMethod
                }}
              </span>
            </prime-tag>
          </div>
        </template>
      </prime-column>

      <!-- Qty + Unit -->
      <prime-column
        v-if="colVisible('qty')"
        header="Qty"
        style="min-width: 7rem"
      >
        <template #body="{ data }">
          <span class="tw:text-sm">
            {{ data.quantity }}
            <span v-if="data.unit" class="app-text-muted tw:ml-0.5">{{
              data.unit
            }}</span>
          </span>
        </template>
      </prime-column>

      <!-- Unit price -->
      <prime-column
        v-if="colVisible('unitPrice')"
        field="unitPrice"
        header="Unit price"
        style="min-width: 9rem"
      >
        <template #body="{ data }">
          <span class="tw:text-sm app-text-muted">{{
            formatVnd(data.unitPrice)
          }}</span>
        </template>
      </prime-column>

      <!-- Total -->
      <prime-column
        v-if="colVisible('totalAmount')"
        field="totalAmount"
        header="Total"
        style="min-width: 9rem"
      >
        <template #body="{ data }">
          <span class="tw:font-semibold tw:text-sm">{{
            formatVnd(data.totalAmount)
          }}</span>
        </template>
      </prime-column>

      <!-- Notes -->
      <prime-column
        v-if="colVisible('notes')"
        field="notes"
        header="Notes"
        style="min-width: 10rem"
      >
        <template #body="{ data }">
          <span
            v-if="data.notes"
            class="tw:text-sm app-text-muted tw:line-clamp-1"
            >{{ data.notes }}</span
          >
          <span v-else class="app-text-subtle">—</span>
        </template>
      </prime-column>

      <!-- Actions -->
      <prime-column header="Actions" class="tw:min-w-32">
        <template #body="{ data }">
          <div class="tw:flex tw:justify-end tw:items-center tw:gap-2">
            <prime-button
              v-if="can('expense.update')"
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
              v-if="can('expense.delete')"
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
      </prime-column>
    </AppTable>
  </section>
</template>
