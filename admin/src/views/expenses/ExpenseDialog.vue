<script setup>
import {
  createExpense,
  updateExpense,
  getExpenseItemNames,
} from "@/services/expense.service";
import { EXPENSE_UNITS } from "@/constants/expenseUnit";
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

const props = defineProps({
  visible: { type: Boolean, required: true },
  expense: { type: Object, default: null },
});
const emit = defineEmits(['update:visible', 'saved']);

const { t } = useI18n();
const toast = useToast();

const todayMidnight = () => {
  const d = new Date();
  d.setHours(0, 0, 0, 0);
  return d;
};

const formLoading = ref(false);
const formError = ref('');

const unitSuggestions = ref([]);
const fetchUnitSuggestions = ({ query }) => {
  const q = query.trim().toLowerCase();
  unitSuggestions.value = q
    ? EXPENSE_UNITS.filter(u => u.toLowerCase().includes(q))
    : [...EXPENSE_UNITS];
};

const nameSuggestions = ref([]);
const fetchNameSuggestions = async ({ query }) => {
  try {
    const res = await getExpenseItemNames(query);
    nameSuggestions.value = res.data ?? [];
  } catch {
    nameSuggestions.value = [];
  }
};

const fName = ref('');
const fCategory = ref(EXPENSE_CATEGORY.INGREDIENT);
const fPaymentMethod = ref(EXPENSE_PAYMENT_METHOD.CASH);
const fQuantity = ref(null);
const fUnit = ref('');
const fUnitPrice = ref(null);
const fTotalAmount = ref(null);
// null = cả 2 đều trống, 'unitPrice' = user nhập đơn giá, 'total' = user nhập tổng
const lastEdited = ref(null);
const fPurchaseDate = ref(todayMidnight());
const fNotes = ref('');

const unitPriceDisabled = computed(() => lastEdited.value === 'total');
const totalAmountDisabled = computed(() => lastEdited.value === 'unitPrice');

const onUnitPriceInput = (e) => {
  fUnitPrice.value = e.value;
  if (!e.value) {
    lastEdited.value = null;
    fTotalAmount.value = null;
  } else {
    lastEdited.value = 'unitPrice';
    fTotalAmount.value = Math.round(e.value * (fQuantity.value ?? 0));
  }
};

const onTotalAmountInput = (e) => {
  fTotalAmount.value = e.value;
  if (!e.value) {
    lastEdited.value = null;
    fUnitPrice.value = null;
  } else {
    lastEdited.value = 'total';
    const qty = fQuantity.value ?? 0;
    fUnitPrice.value = qty > 0 ? Math.round(e.value / qty) : 0;
  }
};

// quantity thay đổi → recompute field bị lock
watch(fQuantity, (qty) => {
  if (lastEdited.value === 'unitPrice' && fUnitPrice.value) {
    fTotalAmount.value = Math.round(fUnitPrice.value * (qty ?? 0));
  } else if (lastEdited.value === 'total' && fTotalAmount.value) {
    fUnitPrice.value = (qty ?? 0) > 0 ? Math.round(fTotalAmount.value / qty) : 0;
  }
});
const isEditMode = computed(() => props.expense !== null);
const dialogHeader = computed(() =>
  isEditMode.value ? t('expenses.dialog.titleEdit') : t('expenses.dialog.titleCreate')
);


watch(() => props.visible, (val) => {
  if (!val) return;
  formError.value = '';
  if (props.expense) {
    fName.value = props.expense.name;
    fCategory.value = props.expense.category;
    fPaymentMethod.value = props.expense.paymentMethod ?? EXPENSE_PAYMENT_METHOD.CASH;
    fQuantity.value = props.expense.quantity;
    fUnit.value = props.expense.unit ?? '';
    fUnitPrice.value = props.expense.unitPrice;
    fTotalAmount.value = props.expense.totalAmount;
    fPurchaseDate.value = new Date(props.expense.purchaseDate);
    fNotes.value = props.expense.notes ?? '';
    // Detect: nếu total không khớp qty × unitPrice → lần trước nhập theo total
    const expectedTotal = Math.round(props.expense.quantity * props.expense.unitPrice);
    lastEdited.value = Math.abs(expectedTotal - props.expense.totalAmount) > 0.01 ? 'total' : 'unitPrice';
  } else {
    fName.value = '';
    fCategory.value = EXPENSE_CATEGORY.INGREDIENT;
    fPaymentMethod.value = EXPENSE_PAYMENT_METHOD.CASH;
    fQuantity.value = null;
    fUnit.value = '';
    fUnitPrice.value = null;
    fTotalAmount.value = null;
    lastEdited.value = null;
    fPurchaseDate.value = todayMidnight();
    fNotes.value = '';
  }
});

const submitForm = async () => {
  formError.value = '';
  if (!fName.value.trim()) {
    formError.value = t('expenses.validation.nameRequired');
    return;
  }
  if (!fQuantity.value || fQuantity.value <= 0) {
    formError.value = t('expenses.validation.quantityRequired');
    return;
  }
  if (lastEdited.value !== 'total' && (fUnitPrice.value === null || fUnitPrice.value < 0)) {
    formError.value = t('expenses.validation.unitPriceRequired');
    return;
  }
  if (lastEdited.value === 'total' && (fTotalAmount.value === null || fTotalAmount.value < 0)) {
    formError.value = t('expenses.validation.totalAmountRequired');
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
      unitPrice: fUnitPrice.value ?? 0,
      totalAmount: lastEdited.value === 'total' ? fTotalAmount.value : null,
      purchaseDate: fPurchaseDate.value?.toISOString?.() ?? fPurchaseDate.value,
      notes: fNotes.value.trim() || null,
    };

    if (isEditMode.value) {
      await updateExpense(props.expense.id, payload);
      toast.add({ severity: 'success', summary: t('expenses.toast.updatedTitle'), detail: t('expenses.toast.updatedDetail'), life: 3000 });
    } else {
      await createExpense(payload);
      toast.add({ severity: 'success', summary: t('expenses.toast.createdTitle'), detail: t('expenses.toast.createdDetail'), life: 3000 });
    }

    emit('update:visible', false);
    emit('saved');
  } catch (err) {
    formError.value =
      err?.response?.data?.errors?.join(', ') ||
      err?.response?.data?.title ||
      t('expenses.error.saveFailed');
  } finally {
    formLoading.value = false;
  }
};
</script>

<template>
  <prime-dialog
    :visible="visible"
    @update:visible="emit('update:visible', $event)"
    :header="dialogHeader"
    :modal="true"
    :style="{ width: '32rem' }"
  >
    <div class="tw:space-y-4">
      <!-- Purchase date -->
      <div class="tw:space-y-1.5">
        <label class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
          {{ t('expenses.dialog.purchaseDate') }}
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
        <label class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
          {{ t('expenses.dialog.itemName') }} <span class="tw:text-red-400">*</span>
        </label>
        <prime-auto-complete
          v-model="fName"
          :suggestions="nameSuggestions"
          @complete="fetchNameSuggestions"
          :placeholder="t('expenses.dialog.itemNamePlaceholder')"
          class="app-input tw:w-full"
          fluid
        />
      </div>

      <!-- Category + Payment method -->
      <div class="tw:grid tw:grid-cols-2 tw:gap-3">
        <div class="tw:space-y-1.5">
          <label class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
            {{ t('expenses.dialog.category') }}
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
          <label class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
            {{ t('expenses.dialog.payment') }}
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
          <label class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
            {{ t('expenses.dialog.quantity') }} <span class="tw:text-red-400">*</span>
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
          <label class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
            {{ t('expenses.dialog.unit') }}
          </label>
          <prime-auto-complete
            v-model="fUnit"
            :suggestions="unitSuggestions"
            @complete="fetchUnitSuggestions"
            :placeholder="t('expenses.dialog.unitPlaceholder')"
            class="app-input tw:w-full"
            fluid
          />
        </div>
      </div>

      <!-- Unit price + Total -->
      <div class="tw:grid tw:grid-cols-2 tw:gap-3">
        <div class="tw:space-y-1.5">
          <label for="expense-unit-price" class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
            {{ t('expenses.dialog.unitPrice') }}
            <span v-if="lastEdited !== 'total'" class="tw:text-red-400">*</span>
          </label>
          <prime-input-number
            id="expense-unit-price"
            v-model="fUnitPrice"
            :disabled="unitPriceDisabled"
            :min="0"
            :max-fraction-digits="0"
            :use-grouping="true"
            suffix=" ₫"
            placeholder="0 ₫"
            class="app-input tw:w-full"
            @input="onUnitPriceInput"
          />
        </div>
        <div class="tw:space-y-1.5">
          <label for="expense-total" class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
            {{ t('expenses.dialog.totalAuto') }}
            <span v-if="lastEdited === 'total'" class="tw:text-red-400">*</span>
          </label>
          <prime-input-number
            id="expense-total"
            v-model="fTotalAmount"
            :disabled="totalAmountDisabled"
            :min="0"
            :max-fraction-digits="0"
            :use-grouping="true"
            suffix=" ₫"
            placeholder="0 ₫"
            class="app-input tw:w-full"
            @input="onTotalAmountInput"
          />
        </div>
      </div>

      <!-- Notes -->
      <div class="tw:space-y-1.5">
        <label class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
          {{ t('expenses.dialog.notes') }}
        </label>
        <prime-textarea
          v-model="fNotes"
          :rows="2"
          :placeholder="t('expenses.dialog.notesPlaceholder')"
          class="app-input tw:w-full"
          auto-resize
        />
      </div>

      <p v-if="formError" class="tw:text-xs tw:text-red-400">{{ formError }}</p>
    </div>

    <template #footer>
      <prime-button severity="secondary" outlined @click="emit('update:visible', false)">
        {{ t('expenses.dialog.cancel') }}
      </prime-button>
      <prime-button
        :severity="isEditMode ? 'primary' : 'success'"
        :loading="formLoading"
        @click="submitForm"
      >
        <iconify :icon="isEditMode ? 'ph:check-bold' : 'ph:plus-bold'" />
        <span>{{ isEditMode ? t('expenses.dialog.saveChanges') : t('expenses.dialog.addExpense') }}</span>
      </prime-button>
    </template>
  </prime-dialog>
</template>
