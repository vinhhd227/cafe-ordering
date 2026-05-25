<script setup>
import { ref } from 'vue'
import { createProductOptionGroup } from '@/services/product-option-group.service'
import AddonsGroupForm from './AddonsGroupForm.vue'
import AddonProductPicker from './AddonProductPicker.vue'

const emit = defineEmits(['back', 'created'])

const { t } = useI18n()
const toast = useToast()

const createStep = ref('1')
const submitting = ref(false)
const pendingFormData = ref(null)

const handleFormSubmit = (formData) => {
  pendingFormData.value = formData
  createStep.value = '2'
}

const handlePickerConfirm = async (selectedIds) => {
  if (!pendingFormData.value) return
  submitting.value = true
  try {
    await createProductOptionGroup({
      ...pendingFormData.value,
      productIds: selectedIds,
    })
    toast.add({ severity: 'success', summary: t('productOptionGroups.create.submit'), life: 2500 })
    emit('created')
  } catch {
    toast.add({ severity: 'error', summary: t('products.mobile.addons.createError'), life: 3000 })
  } finally {
    submitting.value = false
  }
}

const handleBack = () => {
  if (createStep.value === '2') {
    createStep.value = '1'
  } else {
    emit('back')
  }
}
</script>

<template>
  <div class="tw:fixed tw:inset-0 tw:z-30 tw:flex tw:flex-col tw:bg-slate-50 tw:dark:bg-neutral-950">

    <!-- Top bar -->
    <div class="tw:shrink-0 tw:flex tw:items-center tw:gap-3 tw:px-4 tw:py-3 tw:bg-white tw:dark:bg-neutral-900 tw:border-b tw:border-slate-100 tw:dark:border-white/10">
      <button
        type="button"
        class="tw:w-8 tw:h-8 tw:flex tw:items-center tw:justify-center tw:rounded-lg tw:bg-transparent tw:border-0 tw:cursor-pointer tw:text-muted tw:active:bg-black/5 tw:dark:active:bg-white/5"
        @click="handleBack"
      >
        <iconify icon="ph:arrow-left-bold" class="tw:text-lg" />
      </button>
      <h2 class="tw:font-semibold tw:text-slate-800 tw:dark:text-white tw:text-base">
        {{ t('products.mobile.addons.createTitle') }}
      </h2>
    </div>

    <!-- Stepper -->
    <prime-stepper
      v-model:value="createStep"
      linear
      :pt="{ root: { class: 'tw:flex-1 tw:flex tw:flex-col tw:overflow-hidden tw:min-h-0' } }"
    >
      <prime-step-list class="tw:px-5!">
        <prime-step value="1">{{ t('products.mobile.addons.step1') }}</prime-step>
        <prime-step value="2">{{ t('products.mobile.addons.step2') }}</prime-step>
      </prime-step-list>

      <prime-step-panels
        :pt="{ root: { class: 'tw:flex-1 tw:flex tw:flex-col tw:overflow-hidden tw:min-h-0' } }"
      >
        <!-- Step 1: Group info form -->
        <prime-step-panel
          value="1"
          :pt="{
            root: { class: 'tw:flex tw:flex-col tw:h-full tw:overflow-hidden' },
            content: { class: 'tw:flex! tw:flex-col! tw:h-full! tw:overflow-hidden! tw:p-0!' }
          }"
        >
          <AddonsGroupForm
            mode="create"
            :submitting="submitting"
            @submit="handleFormSubmit"
          />
        </prime-step-panel>

        <!-- Step 2: Product picker -->
        <prime-step-panel
          value="2"
          :pt="{
            root: { class: 'tw:flex tw:flex-col tw:h-full tw:overflow-hidden' },
            content: { class: 'tw:flex! tw:flex-col! tw:h-full! tw:overflow-hidden! tw:p-0!' }
          }"
        >
          <AddonProductPicker
            :submitting="submitting"
            :cancel-label="t('products.mobile.addons.pickProductsBack')"
            @confirm="handlePickerConfirm"
            @cancel="createStep = '1'"
          />
        </prime-step-panel>
      </prime-step-panels>
    </prime-stepper>

  </div>
</template>
