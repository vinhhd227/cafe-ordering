<script setup>
import { ref, computed, onMounted } from 'vue'

const props = defineProps({
  initialName: { type: String, default: '' },
  initialValues: { type: Array, default: () => [] },
  initialIsRequired: { type: Boolean, default: false },
  initialAllowMultiple: { type: Boolean, default: false },
  initialAllowQuantity: { type: Boolean, default: false },
  mode: { type: String, default: 'create' }, // 'create' | 'edit'
  submitting: { type: Boolean, default: false },
})
const emit = defineEmits(['submit', 'delete'])

const { t } = useI18n()

const NAME_MAX = 20

const formName = ref('')
const formIsRequired = ref(false)
const formAllowMultiple = ref(false)
const formAllowQuantity = ref(false)
const formValues = ref([])

const nameCount = computed(() => formName.value.length)

// Option bottom sheet state
const optionModalVisible = ref(false)
const optionName = ref('')
const optionPrice = ref(null)
const optionCostPrice = ref(null)
const optionEditIdx = ref(null)

const openAddOption = (idx = null) => {
  optionEditIdx.value = idx
  if (idx !== null) {
    const v = formValues.value[idx]
    optionName.value = v.name
    optionPrice.value = v.price
    optionCostPrice.value = v.costPrice ?? null
  } else {
    optionName.value = ''
    optionPrice.value = null
    optionCostPrice.value = null
  }
  optionModalVisible.value = true
}

const confirmOption = () => {
  if (!optionName.value.trim()) return
  const entry = {
    name: optionName.value.trim(),
    price: Number(optionPrice.value) || 0,
    costPrice: optionCostPrice.value !== null && optionCostPrice.value !== ''
      ? Number(optionCostPrice.value) : null,
  }
  if (optionEditIdx.value !== null) {
    formValues.value[optionEditIdx.value] = entry
  } else {
    formValues.value.push(entry)
  }
  optionModalVisible.value = false
}

const removeOption = (idx) => {
  formValues.value.splice(idx, 1)
}

const handleSubmit = () => {
  if (!formName.value.trim()) return
  emit('submit', {
    name: formName.value.trim(),
    isRequired: formIsRequired.value,
    allowMultiple: formAllowMultiple.value,
    allowQuantity: formAllowQuantity.value,
    values: formValues.value,
  })
}

const formatVnd = (value) =>
  new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(value ?? 0)

onMounted(() => {
  formName.value = props.initialName
  formIsRequired.value = props.initialIsRequired
  formAllowMultiple.value = props.initialAllowMultiple
  formAllowQuantity.value = props.initialAllowQuantity
  formValues.value = props.initialValues.map(v => ({ ...v }))
})
</script>

<template>
  <div class="tw:flex tw:flex-col tw:h-full tw:overflow-hidden">

    <!-- Option bottom sheet -->
    <prime-drawer
      v-model:visible="optionModalVisible"
      position="bottom"
      :style="{ height: 'auto' }"
      :pt="{ root: { class: 'tw:rounded-t-2xl' } }"
    >
      <template #header>
        <p class="tw:font-semibold tw:text-slate-800 tw:dark:text-white">
          {{ t('products.mobile.addons.addOptionTitle') }}
        </p>
      </template>
      <div class="tw:flex tw:flex-col tw:gap-4 tw:pb-4">
        <div class="tw:space-y-1.5">
          <label for="opt-name" class="tw:text-sm tw:font-medium tw:text-slate-700 tw:dark:text-slate-200">
            {{ t('products.mobile.addons.optionName') }}<span class="tw:text-red-500 tw:ml-0.5">*</span>
          </label>
          <prime-input-text
            id="opt-name"
            v-model="optionName"
            :placeholder="t('products.mobile.addons.optionNamePlaceholder')"
            class="app-input tw:w-full"
          />
        </div>
        <div class="tw:space-y-1.5">
          <label for="opt-price" class="tw:text-sm tw:font-medium tw:text-slate-700 tw:dark:text-slate-200">
            {{ t('products.mobile.addons.optionPrice') }}<span class="tw:text-red-500 tw:ml-0.5">*</span>
          </label>
          <prime-input-number
            id="opt-price"
            v-model="optionPrice"
            :min="0"
            :use-grouping="true"
            suffix=" ₫"
            class="app-input tw:w-full"
            input-class="tw:w-full"
          />
        </div>
        <div class="tw:space-y-1.5">
          <label for="opt-cost" class="tw:text-sm tw:font-medium tw:text-slate-700 tw:dark:text-slate-200">
            {{ t('products.mobile.addons.optionCostPrice') }}
            <span class="tw:text-slate-400 tw:font-normal tw:text-xs tw:ml-1">({{ t('products.form.optional') }})</span>
          </label>
          <prime-input-number
            id="opt-cost"
            v-model="optionCostPrice"
            :min="0"
            :use-grouping="true"
            suffix=" ₫"
            class="app-input tw:w-full"
            input-class="tw:w-full"
          />
        </div>
        <prime-button
          severity="success"
          fluid
          :disabled="!optionName.trim()"
          @click="confirmOption"
        >
          {{ t('products.mobile.addons.confirm') }}
        </prime-button>
      </div>
    </prime-drawer>

    <!-- Scrollable content -->
    <div class="tw:flex-1 tw:overflow-y-auto tw:pb-4">

      <!-- Name field -->
      <div class="tw:bg-white tw:dark:bg-neutral-900 tw:mt-3 tw:mx-4 tw:rounded-xl tw:p-4 tw:space-y-1">
        <div class="tw:flex tw:items-center tw:justify-between">
          <label :for="mode === 'edit' ? 'edit-addon-name' : 'addon-name'" class="tw:text-sm tw:font-medium tw:text-slate-700 tw:dark:text-slate-200">
            {{ t('products.mobile.addons.nameLabel') }}<span class="tw:text-red-500 tw:ml-0.5">*</span>
          </label>
          <span class="tw:text-xs tw:text-slate-400">{{ nameCount }}/{{ NAME_MAX }}</span>
        </div>
        <prime-input-text
          :id="mode === 'edit' ? 'edit-addon-name' : 'addon-name'"
          v-model="formName"
          :maxlength="NAME_MAX"
          :placeholder="t('products.mobile.addons.namePlaceholder')"
          class="app-input tw:w-full"
        />
      </div>

      <!-- Options list -->
      <div class="tw:bg-white tw:dark:bg-neutral-900 tw:mt-3 tw:mx-4 tw:rounded-xl tw:overflow-hidden">
        <div
          v-for="(val, idx) in formValues"
          :key="idx"
          class="tw:flex tw:items-center tw:gap-3 tw:px-4 tw:py-3 tw:border-b tw:border-slate-50 tw:dark:border-white/5"
        >
          <div class="tw:flex-1 tw:min-w-0" @click="openAddOption(idx)">
            <p class="tw:text-sm tw:font-medium tw:text-slate-700 tw:dark:text-slate-200 tw:leading-tight">{{ val.name }}</p>
            <div class="tw:flex tw:gap-3 tw:mt-0.5">
              <span class="tw:text-xs tw:text-amber-500 tw:font-semibold">{{ formatVnd(val.price) }}</span>
              <span v-if="val.costPrice !== null && val.costPrice !== ''" class="tw:text-xs tw:text-slate-400">vốn {{ formatVnd(val.costPrice) }}</span>
            </div>
          </div>
          <button
            type="button"
            class="tw:w-7 tw:h-7 tw:flex tw:items-center tw:justify-center tw:rounded-lg tw:bg-transparent tw:border-0 tw:cursor-pointer tw:text-red-400 tw:active:bg-red-50 tw:dark:active:bg-red-900/20"
            @click="removeOption(idx)"
          >
            <iconify icon="ph:x-bold" class="tw:text-sm" />
          </button>
        </div>
        <button
          type="button"
          class="tw:w-full tw:flex tw:items-center tw:gap-2 tw:px-4 tw:py-3.5 tw:bg-transparent tw:border-0 tw:cursor-pointer tw:text-primary-600 tw:dark:text-primary-400 tw:text-sm tw:font-medium tw:active:bg-slate-50 tw:dark:active:bg-white/5"
          @click="openAddOption()"
        >
          <iconify icon="ph:plus-circle-bold" class="tw:text-lg" />
          {{ t('products.mobile.addons.addOption') }}
        </button>
      </div>

      <!-- Settings -->
      <div class="tw:bg-white tw:dark:bg-neutral-900 tw:mt-3 tw:mx-4 tw:rounded-xl tw:divide-y tw:divide-slate-50 tw:dark:divide-white/5">
        <label class="tw:flex tw:items-center tw:justify-between tw:px-4 tw:py-3.5 tw:cursor-pointer">
          <div>
            <p class="tw:text-sm tw:font-medium tw:text-slate-700 tw:dark:text-slate-200">{{ t('products.mobile.addons.isRequired') }}</p>
            <p class="tw:text-xs tw:text-slate-400 tw:mt-0.5">{{ t('products.mobile.addons.isRequiredHint') }}</p>
          </div>
          <prime-checkbox v-model="formIsRequired" :binary="true" />
        </label>
        <label class="tw:flex tw:items-center tw:justify-between tw:px-4 tw:py-3.5 tw:cursor-pointer">
          <div>
            <p class="tw:text-sm tw:font-medium tw:text-slate-700 tw:dark:text-slate-200">{{ t('products.mobile.addons.allowMultiple') }}</p>
            <p class="tw:text-xs tw:text-slate-400 tw:mt-0.5">{{ t('products.mobile.addons.allowMultipleHint') }}</p>
          </div>
          <prime-checkbox v-model="formAllowMultiple" :binary="true" />
        </label>
        <label class="tw:flex tw:items-center tw:justify-between tw:px-4 tw:py-3.5 tw:cursor-pointer">
          <div>
            <p class="tw:text-sm tw:font-medium tw:text-slate-700 tw:dark:text-slate-200">{{ t('products.mobile.addons.allowQuantity') }}</p>
            <p class="tw:text-xs tw:text-slate-400 tw:mt-0.5">{{ t('products.mobile.addons.allowQuantityHint') }}</p>
          </div>
          <prime-checkbox v-model="formAllowQuantity" :binary="true" />
        </label>
      </div>

    </div>

    <!-- Bottom bar -->
    <div class="tw:shrink-0 tw:bg-white tw:dark:bg-neutral-900 tw:border-t tw:border-slate-100 tw:dark:border-white/10 tw:px-4 tw:py-3">
      <!-- Create mode: single submit button -->
      <prime-button
        v-if="mode === 'create'"
        severity="success"
        fluid
        :disabled="!formName.trim()"
        @click="handleSubmit"
      >
        {{ t('products.mobile.addons.submit') }}
      </prime-button>

      <!-- Edit mode: delete + update -->
      <div v-else class="tw:flex tw:gap-3">
        <prime-button
          severity="danger"
          outlined
          class="tw:flex-1"
          @click="emit('delete')"
        >
          {{ t('products.mobile.addons.deleteGroup') }}
        </prime-button>
        <prime-button
          severity="success"
          class="tw:flex-1"
          :loading="submitting"
          :disabled="!formName.trim()"
          @click="handleSubmit"
        >
          {{ t('products.mobile.addons.update') }}
        </prime-button>
      </div>
    </div>

  </div>
</template>
