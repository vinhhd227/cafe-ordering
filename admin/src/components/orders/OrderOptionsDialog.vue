<script setup>
const props = defineProps({
  visible: Boolean,
  product: Object,
})

const emit = defineEmits(['update:visible', 'confirm'])

const { t } = useI18n()

const pendingQuantity = ref(1)
const pendingSelections = ref({}) // { [groupId]: valueId (Single) | valueId[] (Multiple) }
const isTakeaway = ref(false)

watch(
  () => props.product,
  (product) => {
    if (!product) return
    pendingQuantity.value = 1
    isTakeaway.value = false
    const selections = {}
    for (const group of product.optionGroups ?? []) {
      const defaultVal = group.values?.find((v) => v.isDefault)
      if (group.selectionType === 'Single') {
        selections[group.id] = defaultVal?.id ?? null
      } else {
        selections[group.id] = defaultVal ? [defaultVal.id] : []
      }
    }
    pendingSelections.value = selections
  },
  { immediate: true },
)

const servingOptions = [
  { value: false, label: () => t('orders.serving.dineIn'), icon: 'ph:coffee-bold' },
  { value: true, label: () => t('orders.serving.takeaway'), icon: 'ph:bag-bold' },
]

const formatVnd = (val) =>
  new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(val ?? 0)

const toggleValue = (group, valueId) => {
  if (group.selectionType === 'Single') {
    pendingSelections.value[group.id] = valueId
  } else {
    const current = pendingSelections.value[group.id] ?? []
    const idx = current.indexOf(valueId)
    if (idx >= 0) {
      pendingSelections.value[group.id] = current.filter((id) => id !== valueId)
    } else {
      pendingSelections.value[group.id] = [...current, valueId]
    }
  }
}

const isSelected = (group, valueId) => {
  const sel = pendingSelections.value[group.id]
  if (group.selectionType === 'Single') return sel === valueId
  return Array.isArray(sel) && sel.includes(valueId)
}

const canConfirm = computed(() => {
  if (!props.product) return false
  for (const group of props.product.optionGroups ?? []) {
    if (!group.isRequired) continue
    const sel = pendingSelections.value[group.id]
    if (group.selectionType === 'Single' && !sel) return false
    if (group.selectionType !== 'Single' && (!sel || sel.length === 0)) return false
  }
  return true
})

const handleConfirm = () => {
  const selectedIds = []
  const selectedLabels = []
  let optionAdjustment = 0

  for (const group of props.product.optionGroups ?? []) {
    const sel = pendingSelections.value[group.id]
    const ids = group.selectionType === 'Single' ? (sel ? [sel] : []) : (sel ?? [])
    for (const id of ids) {
      const val = group.values?.find((v) => v.id === id)
      if (!val) continue
      selectedIds.push(id)
      selectedLabels.push(val.label)
      optionAdjustment += val.priceAdjustment ?? 0
    }
  }

  const key = `${props.product.id}|${[...selectedIds].sort().join(',')}|${isTakeaway.value ? '1' : '0'}`

  emit('confirm', {
    _key: key,
    productId: props.product.id,
    productName: props.product.name,
    unitPrice: props.product.price,
    optionAdjustment,
    selectedOptionValueIds: selectedIds,
    selectedValueLabels: selectedLabels,
    quantity: pendingQuantity.value,
    isTakeaway: isTakeaway.value,
    isFreeGift: false,
    isAccompaniment: props.product.isAccompaniment ?? false,
  })
  emit('update:visible', false)
}
</script>

<template>
  <prime-dialog
    :visible="visible"
    modal
    :style="{ width: '52rem', maxWidth: '95vw' }"
    :pt="{ content: { class: 'tw:p-0! tw:overflow-hidden' } }"
    @update:visible="$emit('update:visible', $event)"
    @hide="$emit('update:visible', false)"
  >
    <div class="tw:flex tw:min-h-[28rem] tw:flex-col tw:sm:flex-row">
      <!-- LEFT: Product info -->
      <div
        class="tw:flex tw:flex-col tw:border-b tw:border-slate-200 tw:sm:w-5/12 tw:sm:border-b-0 tw:sm:border-r"
        style="border-color: var(--app-border); background: var(--app-bg-subtle)"
      >
        <div
          class="tw:relative tw:h-48 tw:flex-none tw:overflow-hidden tw:sm:h-56"
          style="background: var(--app-bg)"
        >
          <img
            v-if="product?.imageUrl"
            :src="product.imageUrl"
            :alt="product?.name"
            class="tw:h-full tw:w-full tw:object-cover"
          />
          <div
            v-else
            class="tw:flex tw:h-full tw:w-full tw:items-center tw:justify-center"
          >
            <iconify icon="ph:coffee-bold" class="tw:text-6xl tw:text-emerald-400/20" />
          </div>
        </div>

        <div class="tw:flex tw:flex-1 tw:flex-col tw:gap-2 tw:p-4">
          <h3 class="tw:text-lg tw:font-bold tw:leading-snug">{{ product?.name }}</h3>
          <p class="tw:text-xl tw:font-semibold tw:text-emerald-500">
            {{ product ? formatVnd(product.price) : '' }}
          </p>
          <p v-if="product?.description" class="tw:text-sm tw:leading-relaxed tw:text-muted">
            {{ product.description }}
          </p>
          <div
            v-if="product?.estimatedPrepMinutes"
            class="tw:mt-auto tw:flex tw:items-center tw:gap-1 tw:pt-2 tw:text-xs tw:text-muted"
          >
            <iconify icon="ph:clock-bold" class="tw:h-4 tw:w-4" />
            <span>{{
              t('orders.create.optionsDialog.prepTime', { min: product.estimatedPrepMinutes })
            }}</span>
          </div>
        </div>
      </div>

      <!-- RIGHT: Options + actions -->
      <div class="tw:flex tw:flex-1 tw:flex-col tw:p-5">
        <div class="tw:flex-1 tw:space-y-5 tw:overflow-y-auto">
          <!-- Quantity -->
          <div>
            <p class="tw:mb-2 tw:text-sm tw:font-semibold">
              {{ t('orders.create.optionsDialog.quantity') }}
            </p>
            <div class="tw:flex tw:items-center tw:gap-3">
              <button
                class="tw:flex tw:h-9 tw:w-9 tw:items-center tw:justify-center tw:rounded-xl tw:border tw:transition tw:hover:border-emerald-400 tw:text-muted"
                style="border-color: var(--app-border)"
                @click="pendingQuantity = Math.max(1, pendingQuantity - 1)"
              >
                <iconify icon="ph:minus-bold" class="tw:h-4 tw:w-4" />
              </button>
              <span class="tw:min-w-10 tw:text-center tw:text-xl tw:font-bold">{{
                pendingQuantity
              }}</span>
              <button
                class="tw:flex tw:h-9 tw:w-9 tw:items-center tw:justify-center tw:rounded-xl tw:border tw:transition tw:hover:border-emerald-400 tw:text-muted"
                style="border-color: var(--app-border)"
                @click="pendingQuantity++"
              >
                <iconify icon="ph:plus-bold" class="tw:h-4 tw:w-4" />
              </button>
            </div>
          </div>

          <!-- Dynamic option groups -->
          <div v-for="group in product?.optionGroups ?? []" :key="group.id">
            <p class="tw:mb-2 tw:text-sm tw:font-semibold">
              {{ group.name }}
              <span v-if="group.isRequired" class="tw:text-red-400 tw:text-xs tw:ml-1">*</span>
            </p>
            <div class="tw:grid tw:grid-cols-2 tw:gap-2">
              <prime-button
                v-for="val in group.values"
                :key="val.id"
                variant="outlined"
                class="tw:w-full tw:justify-start"
                :severity="isSelected(group, val.id) ? 'primary' : 'secondary'"
                @click="toggleValue(group, val.id)"
              >
                <iconify
                  :icon="isSelected(group, val.id) ? 'ph:check-circle-fill' : 'ph:circle'"
                  class="tw:shrink-0"
                />
                <span class="tw:flex-1 tw:truncate">{{ val.label }}</span>
                <span
                  v-if="val.priceAdjustment && val.priceAdjustment !== 0"
                  class="tw:text-xs tw:opacity-70 tw:shrink-0"
                >
                  +{{ formatVnd(val.priceAdjustment) }}
                </span>
              </prime-button>
            </div>
          </div>

          <!-- Serving -->
          <div>
            <p class="tw:mb-2 tw:text-sm tw:font-semibold">
              {{ t('orders.create.optionsDialog.serving') }}
            </p>
            <div class="tw:flex tw:gap-2">
              <prime-button
                v-for="opt in servingOptions"
                :key="String(opt.value)"
                variant="outlined"
                class="tw:w-full"
                :severity="isTakeaway === opt.value ? 'primary' : 'secondary'"
                @click="isTakeaway = opt.value"
              >
                <iconify :icon="opt.icon" />
                <span>{{ opt.label() }}</span>
              </prime-button>
            </div>
          </div>
        </div>

        <!-- Actions -->
        <div
          class="tw:mt-5 tw:flex tw:justify-end tw:gap-2 tw:border-t tw:pt-4"
          style="border-color: var(--app-border)"
        >
          <prime-button severity="secondary" text @click="$emit('update:visible', false)">
            <span>{{ t('orders.cancel') }}</span>
          </prime-button>
          <prime-button :disabled="!canConfirm" @click="handleConfirm">
            <iconify icon="prime:shopping-cart" />
            <span class="tw:ml-2">{{ t('orders.create.optionsDialog.addToCart') }}</span>
          </prime-button>
        </div>
      </div>
    </div>
  </prime-dialog>
</template>
