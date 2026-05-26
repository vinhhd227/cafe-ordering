<script setup>
import { computed, nextTick, ref, watch } from 'vue'

const props = defineProps({
  attributes: { type: Array, default: () => [] },
  variants: { type: Array, default: () => [] },
  product: { type: Object, default: null },
  basePrice: { type: [Number, String], default: 0 },
  saving: { type: Boolean, default: false },
})

const emit = defineEmits([
  'update:attributes',
  'update:variants',
  'save-variants',
])

const { t } = useI18n()

const btnIcon = 'tw:w-9! tw:h-9! tw:p-0! tw:inline-flex! tw:items-center! tw:justify-center!'

const attrDrawerVisible = ref(false)
const attrEditIndex = ref(-1)
const attrName = ref('')
const attrValues = ref([''])
const attrDefaultValue = ref(null)
const attrNameInput = ref(null)

const attributesModel = computed({
  get: () => props.attributes,
  set: value => emit('update:attributes', value),
})

const variantsModel = computed({
  get: () => props.variants,
  set: value => emit('update:variants', value),
})

const activeVariantGroups = computed(() =>
  attributesModel.value
    .filter(group => (group.values ?? []).length > 0)
    .map((group, groupIndex) => ({
      id: groupIndex,
      name: group.name,
      values: group.values.map(label => ({ id: label, label })),
    }))
)

const isAttrValid = computed(() => attrName.value.trim().length > 0)

const variantKey = labels => (labels ?? []).join('\u001f')

const buildVariantCombinations = groups =>
  groups.reduce(
    (sets, group) => sets.flatMap(set => (group.values ?? []).map(value => [...set, value])),
    [[]],
  )

const resolveVariantValueLabels = valueIds => {
  const savedGroups = props.product?.variantGroups ?? []
  return savedGroups.map(group => {
    const value = (group.values ?? []).find(v => valueIds.includes(v.id))
    return value?.label ?? ''
  }).filter(Boolean)
}

const normalizeVariant = (variant, displayOrder) => ({
  id: variant.id ?? 0,
  valueLabels: variant.valueLabels ?? resolveVariantValueLabels(variant.valueIds ?? []),
  price: Number(variant.price ?? props.product?.price ?? props.basePrice ?? 0),
  costPrice: variant.costPrice ?? null,
  sku: variant.sku ?? '',
  barcode: variant.barcode ?? '',
  isActive: variant.isActive ?? true,
  displayOrder: variant.displayOrder ?? displayOrder,
})

const variantLabel = (variant, group) => {
  const label = variant.valueLabels?.[group.id]
  return label && (group.values ?? []).some(value => value.label === label) ? label : '-'
}

const labelsIncludeAll = (sourceLabels, targetLabels) =>
  targetLabels.every(label => sourceLabels.includes(label))

const findReusableVariant = (valueLabels, variants) => {
  const exact = variants.find(variant => variantKey(variant.valueLabels) === variantKey(valueLabels))
  if (exact) return exact

  return [...variants]
    .filter(variant => {
      const labels = variant.valueLabels ?? []
      return labelsIncludeAll(valueLabels, labels) || labelsIncludeAll(labels, valueLabels)
    })
    .sort((a, b) => (b.valueLabels?.length ?? 0) - (a.valueLabels?.length ?? 0))[0]
}

const rebuildVariantMatrix = () => {
  if (activeVariantGroups.value.length === 0) {
    variantsModel.value = []
    return
  }

  const currentVariants = variantsModel.value.map((variant, index) => normalizeVariant(variant, index + 1))

  variantsModel.value = buildVariantCombinations(activeVariantGroups.value).map((values, index) => {
    const valueLabels = values.map(value => value.label)
    const reusable = findReusableVariant(valueLabels, currentVariants)
    if (reusable) {
      return {
        ...reusable,
        id: variantKey(reusable.valueLabels) === variantKey(valueLabels) ? reusable.id : 0,
        valueLabels,
        displayOrder: index + 1,
      }
    }

    return normalizeVariant({
      valueLabels,
      price: props.product?.price ?? props.basePrice ?? 0,
      isActive: true,
    }, index + 1)
  })
}

const openAttrDrawer = () => {
  attrEditIndex.value = -1
  attrName.value = ''
  attrValues.value = ['']
  attrDefaultValue.value = null
  attrDrawerVisible.value = true
  nextTick(() => attrNameInput.value?.focus())
}

const editAttr = index => {
  attrEditIndex.value = index
  attrName.value = attributesModel.value[index].name
  attrValues.value = [...attributesModel.value[index].values, '']
  attrDefaultValue.value = attributesModel.value[index].defaultValue ?? null
  attrDrawerVisible.value = true
  nextTick(() => attrNameInput.value?.focus())
}

const onAttrValueInput = (index, value) => {
  const old = attrValues.value[index]
  if (attrDefaultValue.value === old.trim()) attrDefaultValue.value = value.trim() || null
  attrValues.value[index] = value
  if (index === attrValues.value.length - 1 && value !== '') {
    attrValues.value.push('')
  }
}

const removeAttrValue = index => {
  const removed = attrValues.value[index]
  if (attrDefaultValue.value === removed.trim()) attrDefaultValue.value = null
  attrValues.value.splice(index, 1)
  if (attrValues.value.length === 0 || attrValues.value[attrValues.value.length - 1] !== '') {
    attrValues.value.push('')
  }
}

const saveAttr = () => {
  if (!isAttrValid.value) return

  const values = attrValues.value.map(v => v.trim()).filter(Boolean)
  const entry = {
    name: attrName.value.trim(),
    values,
    defaultValue: values.includes(attrDefaultValue.value) ? attrDefaultValue.value : null,
  }
  const nextAttributes = [...attributesModel.value]
  if (attrEditIndex.value >= 0) nextAttributes[attrEditIndex.value] = entry
  else nextAttributes.push(entry)

  attributesModel.value = nextAttributes
  attrDrawerVisible.value = false
  nextTick(rebuildVariantMatrix)
}

const removeAttr = index => {
  attributesModel.value = attributesModel.value.filter((_, currentIndex) => currentIndex !== index)
  nextTick(rebuildVariantMatrix)
}

const removeEditingAttr = () => {
  if (attrEditIndex.value < 0) return
  removeAttr(attrEditIndex.value)
  attrDrawerVisible.value = false
}

const copyBasePriceToVariants = () => {
  variantsModel.value = variantsModel.value.map(variant => ({
    ...variant,
    price: Number(props.basePrice ?? props.product?.price ?? 0),
  }))
}

watch(
  () => props.product,
  product => {
    if (!product) return

    attributesModel.value = (product.variantGroups ?? []).map(group => ({
      name: group.name,
      values: (group.values ?? []).map(value => value.label),
      defaultValue: group.values?.find(v => v.isDefault)?.label ?? null,
    }))

    variantsModel.value = (product.variants ?? []).map(normalizeVariant)
    nextTick(rebuildVariantMatrix)
  },
  { immediate: true },
)
</script>

<template>
  <template v-if="attributesModel.length">
    <div class="tw:-mx-4 tw:mt-2">
      <div class="tw:bg-slate-50 tw:dark:bg-neutral-800 tw:px-4 tw:py-3">
        <p class="tw:text-xs tw:font-semibold tw:uppercase tw:tracking-widest tw:text-slate-500 tw:dark:text-slate-400">
          {{ t('products.create.mobile.attrSectionHeader') }}
        </p>
      </div>
      <div v-for="(attr, index) in attributesModel" :key="index" class="tw:px-4 tw:pt-3 tw:pb-4">
        <div class="tw:flex tw:items-center tw:mb-2">
          <span class="tw:flex-1 tw:text-sm tw:font-medium tw:text-slate-800 tw:dark:text-white">
            {{ attr.name }}
            <span class="tw:text-slate-400 tw:font-normal tw:ml-1">({{ attr.values.length }})</span>
          </span>
          <div class="tw:flex tw:items-center tw:gap-2">
            <prime-button severity="info" text size="small" class="tw:p-0! tw:h-auto! tw:font-medium!" @click="editAttr(index)">
              {{ t('products.create.mobile.attrEdit') }}
            </prime-button>
            <prime-button severity="danger" text :class="btnIcon" @click="removeAttr(index)">
              <iconify icon="ph:trash-bold" />
            </prime-button>
          </div>
        </div>
        <div class="tw:flex tw:flex-wrap tw:gap-2">
          <span
            v-for="value in attr.values"
            :key="value"
            class="tw:inline-flex tw:items-center tw:gap-1 tw:px-3 tw:py-1 tw:rounded-lg tw:border tw:border-slate-200 tw:dark:border-white/10 tw:text-sm tw:text-slate-700 tw:dark:text-white/80"
          >
            <iconify v-if="value === attr.defaultValue" icon="ph:star-fill" class="tw:text-amber-400 tw:text-xs tw:shrink-0" />
            {{ value }}
          </span>
        </div>
      </div>
    </div>
  </template>

  <div v-if="activeVariantGroups.length" class="tw:-mx-4 tw:mt-2">
    <div class="tw:bg-slate-50 tw:dark:bg-neutral-800 tw:px-4 tw:py-3">
      <div class="tw:flex tw:items-center tw:justify-between tw:gap-2">
        <div class="tw:min-w-0">
          <p class="tw:text-xs tw:font-semibold tw:uppercase tw:tracking-widest tw:text-slate-500 tw:dark:text-slate-400">
            BẢNG GIÁ BIẾN THỂ
          </p>
          <p class="tw:text-xs tw:text-slate-400 tw:mt-1">
            Giá cố định cho từng tổ hợp tuỳ chọn.
          </p>
        </div>
        <prime-button severity="secondary" text :class="btnIcon" @click="rebuildVariantMatrix">
          <iconify icon="ph:grid-four-bold" />
        </prime-button>
      </div>
    </div>

    <div class="tw:px-4 tw:py-3 tw:space-y-3">
      <div class="tw:flex tw:gap-2">
        <prime-button severity="secondary" outlined size="small" class="tw:flex-1" @click="copyBasePriceToVariants">
          Copy giá gốc
        </prime-button>
        <prime-button severity="success" size="small" class="tw:flex-1" :loading="saving" :disabled="variantsModel.length === 0" @click="emit('save-variants')">
          Lưu bảng giá
        </prime-button>
      </div>

      <div v-if="variantsModel.length === 0" class="tw:rounded-xl tw:border tw:border-dashed tw:border-slate-200 tw:dark:border-white/10 tw:p-4 tw:text-center tw:text-sm tw:text-slate-400">
        Bấm nút lưới để sinh {{ buildVariantCombinations(activeVariantGroups).length }} dòng giá.
      </div>

      <div
        v-for="variant in variantsModel"
        v-else
        :key="variantKey(variant.valueLabels)"
        class="tw:rounded-xl tw:border tw:border-slate-200 tw:dark:border-white/10 tw:p-3 tw:space-y-3"
      >
        <div class="tw:flex tw:flex-wrap tw:gap-2">
          <span
            v-for="group in activeVariantGroups"
            :key="`${variantKey(variant.valueLabels)}-${group.id}`"
            class="tw:inline-flex tw:items-center tw:rounded-lg tw:bg-slate-100 tw:dark:bg-white/5 tw:px-2.5 tw:py-1 tw:text-xs tw:text-slate-700 tw:dark:text-white/80"
          >
            {{ group.name }}: {{ variantLabel(variant, group) }}
          </span>
        </div>
        <div class="tw:flex tw:items-center tw:gap-3">
          <input
            v-model="variant.price"
            type="number"
            inputmode="numeric"
            class="tw:flex-1 tw:min-w-0 tw:bg-transparent tw:border tw:border-slate-200 tw:dark:border-white/10 tw:rounded-lg tw:px-3 tw:py-2 tw:outline-none"
          />
          <prime-toggle-switch v-model="variant.isActive" />
        </div>
      </div>
    </div>
  </div>

  <div
    class="tw:flex tw:items-center tw:gap-2 tw:py-4 tw:border-b tw:border-slate-100 tw:dark:border-white/5 tw:cursor-pointer tw:active:bg-slate-50 tw:dark:active:bg-white/3 tw:-mx-4 tw:px-4"
    @click="openAttrDrawer"
  >
    <iconify icon="ph:plus-circle-bold" class="tw:text-lg tw:text-primary-500 tw:shrink-0" />
    <span class="tw:text-primary-500 tw:font-medium tw:text-sm">{{ t('products.create.mobile.addAttribute') }}</span>
    <span class="tw:text-sm tw:text-slate-400">{{ t('products.create.mobile.addAttributeHint') }}</span>
  </div>

  <prime-drawer
    v-model:visible="attrDrawerVisible"
    position="bottom"
    :style="{ height: 'auto' }"
    :pt="{ root: { class: 'tw:rounded-t-2xl' } }"
  >
    <template #header>
      <span class="tw:font-semibold tw:text-lg">
        {{ attrEditIndex >= 0 ? t('products.create.mobile.attrDrawerEditTitle') : t('products.create.mobile.attrDrawerTitle') }}
      </span>
    </template>

    <div class="tw:flex tw:flex-col tw:gap-0 tw:pb-4">
      <div class="tw:pb-4 tw:border-slate-100 tw:dark:border-white/5">
        <p class="tw:block tw:text-slate-500 tw:dark:text-slate-400 tw:mb-2">
          {{ t('products.create.mobile.attrNameLabel') }}<span class="tw:text-red-400 tw:ml-0.5">*</span>
        </p>
        <input
        id="attributeName"
          ref="attrNameInput"
          v-model="attrName"
          type="text"
          class="tw:w-full tw:bg-transparent tw:border-0 tw:border-b-2 tw:outline-none tw:text-base tw:text-slate-800 tw:dark:text-white tw:placeholder-slate-300 tw:dark:placeholder-white/20 tw:pb-1 tw:transition-colors"
          :class="attrName ? 'tw:border-primary-500' : 'tw:border-slate-200 tw:dark:border-white/10'"
          @keydown.enter.prevent
        />
      </div>

      <p class=" tw:text-xs tw:font-semibold tw:uppercase tw:tracking-widest tw:text-slate-400 tw:dark:text-slate-500 tw:mt-4 tw:mb-2">
        {{ t('products.create.mobile.attrValuesHeader') }}
      </p>

      <div
        v-for="(value, index) in attrValues"
        :key="index"
        class="tw:flex tw:items-center tw:gap-2 tw:py-3 tw:border-b tw:border-slate-100 tw:dark:border-white/5"
      >
        <input
          :value="value"
          type="text"
          :placeholder="index === attrValues.length - 1 ? t('products.create.mobile.attrValuePlaceholder') : ''"
          class="tw:flex-1 tw:bg-transparent tw:border-0 tw:outline-none tw:text-base tw:text-slate-800 tw:dark:text-white tw:placeholder-slate-300 tw:dark:placeholder-white/20"
          @input="onAttrValueInput(index, $event.target.value)"
        />
        <template v-if="value !== ''">
          <prime-button
            :severity="attrDefaultValue === value.trim() ? 'warning' : 'secondary'"
            text
            :class="btnIcon"
            v-tooltip.top="t('products.create.mobile.attrSetDefault')"
            @click="attrDefaultValue = attrDefaultValue === value.trim() ? null : value.trim()"
          >
            <iconify :icon="attrDefaultValue === value.trim() ? 'ph:star-fill' : 'ph:star'" />
          </prime-button>
          <prime-button severity="danger" text :class="btnIcon" @click="removeAttrValue(index)">
            <iconify icon="ph:trash-bold" />
          </prime-button>
        </template>
      </div>

      <prime-button
        :severity="isAttrValid ? 'success' : 'secondary'"
        :disabled="!isAttrValid"
        fluid
        class="tw:mt-4"
        @click="saveAttr"
      >
        {{ t('products.create.mobile.save') }}
      </prime-button>

      <prime-button
        v-if="attrEditIndex >= 0"
        severity="danger"
        outlined
        fluid
        class="tw:mt-2"
        @click="removeEditingAttr"
      >
        <iconify icon="ph:trash-bold" />
        <span>Xóa biến thể</span>
      </prime-button>
    </div>
  </prime-drawer>
</template>
