<script setup>
const props = defineProps({
  visible: Boolean,
  product: Object,
})

const emit = defineEmits(['update:visible', 'confirm'])

const { t } = useI18n()

const pendingQuantity = ref(1)
const pendingSelections = ref({})        // variantGroups
const pendingOptionSelections = ref({})  // optionGroups — non-quantity
const pendingOptionQuantities = ref({})  // optionGroups — allowQuantity
const isTakeaway = ref(false)

watch(
  () => props.product,
  (product) => {
    if (!product) return
    pendingQuantity.value = 1
    isTakeaway.value = false
    const selections = {}
    for (const group of product.variantGroups ?? []) {
      const defaultVal = group.values?.find((v) => v.isDefault)
      if (group.selectionType === 'Single') {
        selections[group.id] = defaultVal?.id ?? null
      } else {
        selections[group.id] = defaultVal ? [defaultVal.id] : []
      }
    }
    pendingSelections.value = selections
    const optionSels = {}
    const optionQtys = {}
    for (const group of product.optionGroups ?? []) {
      if (group.allowQuantity) {
        optionQtys[group.id] = {}
      } else {
        optionSels[group.id] = group.allowMultiple ? [] : null
      }
    }
    pendingOptionSelections.value = optionSels
    pendingOptionQuantities.value = optionQtys
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

const isOptionSelected = (group, valueId) => {
  if (group.allowQuantity)
    return (pendingOptionQuantities.value[group.id]?.[valueId] ?? 0) > 0
  const sel = pendingOptionSelections.value[group.id]
  if (!group.allowMultiple) return sel === valueId
  return Array.isArray(sel) && sel.includes(valueId)
}

const toggleOptionValue = (group, valueId) => {
  if (group.allowQuantity) {
    const qtys = pendingOptionQuantities.value[group.id] ?? {}
    const cur = qtys[valueId] ?? 0
    pendingOptionQuantities.value[group.id] = { ...qtys, [valueId]: cur > 0 ? 0 : 1 }
    return
  }
  if (!group.allowMultiple) {
    pendingOptionSelections.value[group.id] =
      pendingOptionSelections.value[group.id] === valueId ? null : valueId
  } else {
    const current = pendingOptionSelections.value[group.id] ?? []
    const idx = current.indexOf(valueId)
    pendingOptionSelections.value[group.id] =
      idx >= 0 ? current.filter((id) => id !== valueId) : [...current, valueId]
  }
}

const getOptionQty = (group, valueId) =>
  pendingOptionQuantities.value[group.id]?.[valueId] ?? 0

const adjustOptionQty = (group, valueId, delta) => {
  const qtys = pendingOptionQuantities.value[group.id] ?? {}
  const newQty = Math.max(0, (qtys[valueId] ?? 0) + delta)
  pendingOptionQuantities.value[group.id] = { ...qtys, [valueId]: newQty }
}

const isValueDisabled = (group, valueId) => {
  if (!props.product?.variants?.length) return false
  const activeVariants = props.product.variants.filter((v) => v.isActive !== false)
  // collect selected ids from OTHER groups
  const otherIds = []
  for (const g of props.product.variantGroups ?? []) {
    if (g.id === group.id) continue
    const sel = pendingSelections.value[g.id]
    const ids = g.selectionType === 'Single' ? (sel ? [sel] : []) : (sel ?? [])
    otherIds.push(...ids)
  }
  return !activeVariants.some((v) => {
    const vIds = v.valueIds ?? []
    return vIds.includes(valueId) && otherIds.every((id) => vIds.includes(id))
  })
}

const canConfirm = computed(() => {
  if (!props.product) return false
  for (const group of props.product.variantGroups ?? []) {
    if (!group.isRequired) continue
    const sel = pendingSelections.value[group.id]
    if (group.selectionType === 'Single' && !sel) return false
    if (group.selectionType !== 'Single' && (!sel || sel.length === 0)) return false
  }
  for (const group of props.product.optionGroups ?? []) {
    if (!group.isRequired) continue
    if (group.allowQuantity) {
      const hasAny = Object.values(pendingOptionQuantities.value[group.id] ?? {}).some((q) => q > 0)
      if (!hasAny) return false
    } else {
      const sel = pendingOptionSelections.value[group.id]
      if (!group.allowMultiple && !sel) return false
      if (group.allowMultiple && (!sel || sel.length === 0)) return false
    }
  }
  return true
})

const handleConfirm = () => {
  const selectedIds = []
  const selectedLabels = []
  let optionAdjustment = 0

  for (const group of props.product.variantGroups ?? []) {
    const sel = pendingSelections.value[group.id]
    const ids = group.selectionType === 'Single' ? (sel ? [sel] : []) : (sel ?? [])
    for (const id of ids) {
      const val = group.values?.find((v) => v.id === id)
      if (!val) continue
      selectedIds.push(id)
      selectedLabels.push(val.label)
      optionAdjustment += val.price ?? 0
    }
  }

  const selectedOptionValues = []
  for (const group of props.product.optionGroups ?? []) {
    if (group.allowQuantity) {
      const qtys = pendingOptionQuantities.value[group.id] ?? {}
      for (const [idStr, qty] of Object.entries(qtys)) {
        if (qty <= 0) continue
        const id = Number(idStr)
        const val = group.values?.find((v) => v.id === id)
        if (!val) continue
        selectedOptionValues.push({ optionValueId: id, quantity: qty })
        selectedLabels.push(`${group.name}: ${val.name}${qty > 1 ? ` x${qty}` : ''}`)
        optionAdjustment += (val.price ?? 0) * qty
      }
    } else {
      const sel = pendingOptionSelections.value[group.id]
      const ids = group.allowMultiple ? (sel ?? []) : (sel ? [sel] : [])
      for (const id of ids) {
        const val = group.values?.find((v) => v.id === id)
        if (!val) continue
        selectedOptionValues.push({ optionValueId: id, quantity: 1 })
        selectedLabels.push(`${group.name}: ${val.name}`)
        optionAdjustment += val.price ?? 0
      }
    }
  }

  const optKey = selectedOptionValues.map((o) => `${o.optionValueId}x${o.quantity}`).sort().join(',')
  const key = `${props.product.id}|${[...selectedIds].sort().join(',')}|${optKey}|${isTakeaway.value ? '1' : '0'}`

  emit('confirm', {
    _key: key,
    productId: props.product.id,
    productName: props.product.name,
    unitPrice: props.product.price,
    optionAdjustment,
    selectedVariantValueIds: selectedIds,
    selectedOptionValues,
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
            <iconify icon="ph:coffee-bold" class="tw:text-6xl tw:text-primary-400/20" />
          </div>
        </div>

        <div class="tw:flex tw:flex-1 tw:flex-col tw:gap-2 tw:p-4">
          <h3 class="tw:text-lg tw:font-bold tw:leading-snug">{{ product?.name }}</h3>
          <p class="tw:text-xl tw:font-semibold tw:text-primary-500">
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
              <prime-button
                severity="secondary"
                variant="outlined"
                :class="btnIcon"
                @click="pendingQuantity = Math.max(1, pendingQuantity - 1)"
              >
                <iconify icon="ph:minus-bold" />
              </prime-button>
              <span class="tw:min-w-10 tw:text-center tw:text-xl tw:font-bold">{{
                pendingQuantity
              }}</span>
              <prime-button
                severity="secondary"
                variant="outlined"
                :class="btnIcon"
                @click="pendingQuantity++"
              >
                <iconify icon="ph:plus-bold" />
              </prime-button>
            </div>
          </div>

          <!-- Dynamic option groups (variantGroups) -->
          <div v-for="group in product?.variantGroups ?? []" :key="group.id">
            <p class="tw:mb-2 tw:text-sm tw:font-semibold">
              {{ group.name }}
              <span v-if="group.isRequired" class="tw:text-red-400 tw:text-xs tw:ml-1">*</span>
            </p>
            <!-- Single → RadioButtonGroup -->
            <prime-radio-button-group
              v-if="group.selectionType === 'Single'"
              v-model="pendingSelections[group.id]"
              class="tw:grid tw:grid-cols-2 tw:gap-2"
            >
              <label
                v-for="val in group.values"
                :key="val.id"
                :for="`vg-${group.id}-${val.id}`"
                class="tw:flex tw:items-center tw:gap-2 tw:rounded-xl tw:border tw:px-3 tw:py-2.5 tw:text-sm tw:cursor-pointer tw:transition-colors"
                :class="pendingSelections[group.id] === val.id ? 'tw:border-primary-500/60 tw:bg-primary-500/10' : ''"
                :style="pendingSelections[group.id] === val.id ? '' : 'border-color: var(--app-border)'"
              >
                <prime-radio-button
                  :input-id="`vg-${group.id}-${val.id}`"
                  :value="val.id"
                  :disabled="isValueDisabled(group, val.id)"
                />
                <span class="tw:flex-1 tw:truncate">{{ val.label }}</span>
                <span v-if="val.price && val.price !== 0" class="tw:text-xs tw:opacity-70 tw:shrink-0">
                  +{{ formatVnd(val.price) }}
                </span>
              </label>
            </prime-radio-button-group>
            <!-- Multiple → Checkbox -->
            <div v-else class="tw:grid tw:grid-cols-2 tw:gap-2">
              <label
                v-for="val in group.values"
                :key="val.id"
                :for="`vg-${group.id}-${val.id}`"
                class="tw:flex tw:items-center tw:gap-2 tw:rounded-xl tw:border tw:px-3 tw:py-2.5 tw:text-sm tw:cursor-pointer tw:transition-colors"
                :class="(pendingSelections[group.id] ?? []).includes(val.id) ? 'tw:border-primary-500/60 tw:bg-primary-500/10' : ''"
                :style="(pendingSelections[group.id] ?? []).includes(val.id) ? '' : 'border-color: var(--app-border)'"
              >
                <prime-checkbox
                  :input-id="`vg-${group.id}-${val.id}`"
                  v-model="pendingSelections[group.id]"
                  :value="val.id"
                  :disabled="isValueDisabled(group, val.id)"
                />
                <span class="tw:flex-1 tw:truncate">{{ val.label }}</span>
                <span v-if="val.price && val.price !== 0" class="tw:text-xs tw:opacity-70 tw:shrink-0">
                  +{{ formatVnd(val.price) }}
                </span>
              </label>
            </div>
          </div>

          <!-- Shared option groups (ProductOptionGroup) -->
          <div v-for="group in product?.optionGroups ?? []" :key="`og-${group.id}`">
            <p class="tw:mb-2 tw:text-sm tw:font-semibold">
              {{ group.name }}
              <span v-if="group.isRequired" class="tw:text-red-400 tw:text-xs tw:ml-1">*</span>
              <span v-if="group.allowMultiple && !group.allowQuantity" class="tw:text-muted tw:text-xs tw:ml-1 tw:font-normal">({{ t('orders.create.optionsDialog.multiSelect') }})</span>
            </p>

            <!-- allowQuantity: full-width list with inline stepper -->
            <div v-if="group.allowQuantity" class="tw:space-y-2">
              <div
                v-for="val in group.values"
                :key="val.id"
                class="tw:flex tw:items-center tw:gap-3 tw:rounded-xl tw:border tw:px-3 tw:py-2 tw:text-sm tw:transition-colors"
                :class="isOptionSelected(group, val.id) ? 'tw:border-primary-400/50 tw:bg-primary-500/5' : ''"
                :style="isOptionSelected(group, val.id) ? '' : 'border-color: var(--app-border)'"
              >
                <prime-checkbox
                  :model-value="isOptionSelected(group, val.id)"
                  :binary="true"
                  @change="toggleOptionValue(group, val.id)"
                />
                <span class="tw:flex-1">{{ val.name }}</span>
                <span v-if="val.price" class="tw:text-xs tw:text-muted tw:shrink-0">+{{ formatVnd(val.price) }}</span>
                <div v-if="isOptionSelected(group, val.id)" class="tw:flex tw:items-center tw:gap-1.5 tw:shrink-0">
                  <prime-button size="small" severity="secondary" variant="outlined" :class="btnIcon" @click="adjustOptionQty(group, val.id, -1)">
                    <iconify icon="ph:minus-bold" />
                  </prime-button>
                  <span class="tw:w-5 tw:text-center tw:font-semibold tw:text-primary-400">{{ getOptionQty(group, val.id) }}</span>
                  <prime-button size="small" severity="secondary" variant="outlined" :class="btnIcon" @click="adjustOptionQty(group, val.id, 1)">
                    <iconify icon="ph:plus-bold" />
                  </prime-button>
                </div>
              </div>
            </div>

            <!-- single / multi select (no qty) -->
            <template v-else>
              <!-- Single → RadioButtonGroup -->
              <prime-radio-button-group
                v-if="!group.allowMultiple"
                v-model="pendingOptionSelections[group.id]"
                class="tw:grid tw:grid-cols-2 tw:gap-2"
              >
                <label
                  v-for="val in group.values"
                  :key="val.id"
                  :for="`og-${group.id}-${val.id}`"
                  class="tw:flex tw:items-center tw:gap-2 tw:rounded-xl tw:border tw:px-3 tw:py-2.5 tw:text-sm tw:cursor-pointer tw:transition-colors"
                  :class="pendingOptionSelections[group.id] === val.id ? 'tw:border-primary-500/60 tw:bg-primary-500/10' : ''"
                  :style="pendingOptionSelections[group.id] === val.id ? '' : 'border-color: var(--app-border)'"
                >
                  <prime-radio-button :input-id="`og-${group.id}-${val.id}`" :value="val.id" />
                  <span class="tw:flex-1 tw:truncate">{{ val.name }}</span>
                  <span v-if="val.price" class="tw:text-xs tw:opacity-70 tw:shrink-0">+{{ formatVnd(val.price) }}</span>
                </label>
              </prime-radio-button-group>
              <!-- Multiple → Checkbox -->
              <div v-else class="tw:grid tw:grid-cols-2 tw:gap-2">
                <label
                  v-for="val in group.values"
                  :key="val.id"
                  :for="`og-${group.id}-${val.id}`"
                  class="tw:flex tw:items-center tw:gap-2 tw:rounded-xl tw:border tw:px-3 tw:py-2.5 tw:text-sm tw:cursor-pointer tw:transition-colors"
                  :class="(pendingOptionSelections[group.id] ?? []).includes(val.id) ? 'tw:border-primary-500/60 tw:bg-primary-500/10' : ''"
                  :style="(pendingOptionSelections[group.id] ?? []).includes(val.id) ? '' : 'border-color: var(--app-border)'"
                >
                  <prime-checkbox
                    :input-id="`og-${group.id}-${val.id}`"
                    v-model="pendingOptionSelections[group.id]"
                    :value="val.id"
                  />
                  <span class="tw:flex-1 tw:truncate">{{ val.name }}</span>
                  <span v-if="val.price" class="tw:text-xs tw:opacity-70 tw:shrink-0">+{{ formatVnd(val.price) }}</span>
                </label>
              </div>
            </template>
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
