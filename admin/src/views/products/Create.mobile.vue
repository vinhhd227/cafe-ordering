<script setup>
import { ref, computed, onMounted, nextTick } from 'vue'
import { createProduct } from '@/services/product.service'
import { getCategory } from '@/services/category.service'
import { uploadImage } from '@/services/upload.service'
import { getProductOptionGroups, assignOptionGroupsToProduct } from '@/services/product-option-group.service'
import CreateCategoryDrawer from '@/components/products/CreateCategoryDrawer.vue'

const { t } = useI18n()
const router = useRouter()
const { can } = usePermission()

const UNIT_HISTORY_KEY = 'product-unit-history'

const emptyForm = () => ({
  name: '', price: '', costPrice: '', unit: '',
  sku: '', barcode: '', discountPrice: '',
  categoryId: null, trackInventory: false, inStock: true,
  attributes: [],
})
const form = ref(emptyForm())

const loading = ref(false)
const errorMessage = ref('')
const uploading = ref(false)
const imageUrl = ref('')
const imagePreview = ref('')
const moreInfoOpen = ref(false)

// Attribute drawer
const attrDrawerVisible = ref(false)
const attrEditIndex = ref(-1)
const attrName = ref('')
const attrValues = ref([''])
const attrDefaultValue = ref(null)
const attrNameInput = ref(null)
const variants = ref([])

const variantKey = (labels) => labels.join('\u001f')

const buildVariantCombinations = () =>
  form.value.attributes.reduce(
    (sets, attr) => sets.flatMap(set => attr.values.map(value => [...set, value])),
    [[]],
  )

const rebuildVariantMatrix = () => {
  if (form.value.attributes.length === 0) {
    variants.value = []
    return
  }

  const existing = new Map(variants.value.map(variant => [variantKey(variant.valueLabels), variant]))
  variants.value = buildVariantCombinations().map(labels => (
    existing.get(variantKey(labels)) ?? {
      valueLabels: labels,
      price: Number(form.value.price) || 0,
      costPrice: null,
      sku: '',
      barcode: '',
      isActive: true,
    }
  ))
}

const copyBasePriceToVariants = () => {
  variants.value = variants.value.map(variant => ({
    ...variant,
    price: Number(form.value.price) || 0,
  }))
}

const openAttrDrawer = () => {
  attrEditIndex.value = -1
  attrName.value = ''
  attrValues.value = ['']
  attrDefaultValue.value = null
  attrDrawerVisible.value = true
  nextTick(() => attrNameInput.value?.focus())
}

const editAttr = (i) => {
  attrEditIndex.value = i
  attrName.value = form.value.attributes[i].name
  attrValues.value = [...form.value.attributes[i].values, '']
  attrDefaultValue.value = form.value.attributes[i].defaultValue ?? null
  attrDrawerVisible.value = true
  nextTick(() => attrNameInput.value?.focus())
}

const onAttrValueInput = (i, val) => {
  const old = attrValues.value[i]
  if (attrDefaultValue.value === old.trim()) attrDefaultValue.value = val.trim() || null
  attrValues.value[i] = val
  if (i === attrValues.value.length - 1 && val !== '') {
    attrValues.value.push('')
  }
}

const removeAttrValue = (i) => {
  const removed = attrValues.value[i]
  if (attrDefaultValue.value === removed.trim()) attrDefaultValue.value = null
  attrValues.value.splice(i, 1)
  if (attrValues.value.length === 0 || attrValues.value[attrValues.value.length - 1] !== '') {
    attrValues.value.push('')
  }
}

const isAttrValid = computed(() => attrName.value.trim().length > 0)

const saveAttr = () => {
  if (!isAttrValid.value) return
  const values = attrValues.value.map(v => v.trim()).filter(Boolean)
  const entry = {
    name: attrName.value.trim(),
    values,
    defaultValue: values.includes(attrDefaultValue.value) ? attrDefaultValue.value : null,
  }
  if (attrEditIndex.value >= 0) {
    form.value.attributes[attrEditIndex.value] = entry
  } else {
    form.value.attributes.push(entry)
  }
  rebuildVariantMatrix()
  attrDrawerVisible.value = false
}

const removeAttr = (i) => {
  form.value.attributes.splice(i, 1)
  rebuildVariantMatrix()
}

const removeEditingAttr = () => {
  if (attrEditIndex.value < 0) return
  removeAttr(attrEditIndex.value)
  attrDrawerVisible.value = false
}

const isValid = computed(() => form.value.name.trim().length > 0)

// Unit history â€” localStorage
const unitHistory = ref(JSON.parse(localStorage.getItem(UNIT_HISTORY_KEY) ?? '[]'))
const saveUnitHistory = (unit) => {
  if (!unit?.trim()) return
  const updated = [...new Set([unit.trim(), ...unitHistory.value])].slice(0, 15)
  unitHistory.value = updated
  localStorage.setItem(UNIT_HISTORY_KEY, JSON.stringify(updated))
}

// Option groups
const allGroups = ref([])
const selectedGroupIds = ref(new Set())
const addonDrawerVisible = ref(false)
const addonSearch = ref('')
const addonDraftIds = ref(new Set())

const filteredGroups = computed(() => {
  if (!addonSearch.value.trim()) return allGroups.value
  const q = addonSearch.value.toLowerCase()
  return allGroups.value.filter(g => g.name.toLowerCase().includes(q))
})

const openAddonDrawer = () => {
  addonDraftIds.value = new Set(selectedGroupIds.value)
  addonSearch.value = ''
  addonDrawerVisible.value = true
}

const confirmAddonGroups = () => {
  selectedGroupIds.value = new Set(addonDraftIds.value)
  addonDrawerVisible.value = false
}

const toggleAddonGroup = (id) => {
  const s = new Set(addonDraftIds.value)
  s.has(id) ? s.delete(id) : s.add(id)
  addonDraftIds.value = s
}

const loadOptionGroups = async () => {
  try {
    const res = await getProductOptionGroups()
    const raw = res?.data
    allGroups.value = Array.isArray(raw) ? raw
      : Array.isArray(raw?.value) ? raw.value
      : Array.isArray(raw?.items) ? raw.items
      : []
  } catch { /* non-critical */ }
}

// Categories
const categories = ref([])
const loadCategories = async () => {
  try {
    const res = await getCategory()
    const raw = res?.data
    categories.value = Array.isArray(raw) ? raw
      : Array.isArray(raw?.value) ? raw.value
      : Array.isArray(raw?.items) ? raw.items
      : []
  } catch { /* non-critical */ }
}

// Category picker drawer
const catDrawerVisible = ref(false)
const catDraftId = ref(null)
const catSearch = ref('')
const createCatVisible = ref(false)

const onCategoryCreated = async (newCat) => {
  await loadCategories()
  if (newCat?.id) catDraftId.value = newCat.id
}

const filteredCategories = computed(() => {
  if (!catSearch.value.trim()) return categories.value
  const q = catSearch.value.toLowerCase()
  return categories.value.filter(c => c.name.toLowerCase().includes(q))
})

const sortedCategories = computed(() => {
  if (!form.value.categoryId) return categories.value
  return [
    ...categories.value.filter(c => c.id === form.value.categoryId),
    ...categories.value.filter(c => c.id !== form.value.categoryId),
  ]
})

const openCatDrawer = () => {
  catDraftId.value = form.value.categoryId
  catSearch.value = ''
  catDrawerVisible.value = true
}

const confirmCategory = () => {
  form.value.categoryId = catDraftId.value
  catDrawerVisible.value = false
}

// Image
const galleryInput = ref(null)
const cameraInput = ref(null)

const handleImage = async (event) => {
  const file = event.target.files?.[0]
  if (!file) return
  imagePreview.value = URL.createObjectURL(file)
  uploading.value = true
  try {
    const res = await uploadImage(file)
    imageUrl.value = res?.data?.url ?? ''
  } catch (err) {
    errorMessage.value = err?.response?.data?.errors?.map(e => e.errorMessage ?? e).join('; ') || t('products.form.uploadError')
    imagePreview.value = ''
  } finally {
    uploading.value = false
    event.target.value = ''
  }
}

const clearImage = () => {
  imagePreview.value = ''
  imageUrl.value = ''
}

const extractError = (err) => {
  const data = err?.response?.data
  if (Array.isArray(data?.errors))
    return data.errors.map(e => e.errorMessage ?? e).join('; ')
  return data?.message || t('products.create.error')
}

const submit = async (addNew = false) => {
  if (loading.value) return
  loading.value = true
  errorMessage.value = ''
  try {
    const res = await createProduct({
      name: form.value.name.trim(),
      price: Number(form.value.price) || 0,
      costPrice: Number(form.value.costPrice) || null,
      unit: form.value.unit.trim() || null,
      sku: form.value.sku.trim() || null,
      barcode: form.value.barcode.trim() || null,
      discountPrice: Number(form.value.discountPrice) || null,
      categoryId: form.value.categoryId,
      imageUrl: imageUrl.value || null,
      variantGroups: form.value.attributes.length > 0
        ? form.value.attributes.map(a => ({
            name: a.name,
            isRequired: false,
            selectionType: 'Single',
            values: a.values.map(v => ({ label: v, price: 0, isDefault: v === a.defaultValue })),
          }))
        : null,
      variants: form.value.attributes.length > 0 && variants.value.length > 0
        ? variants.value.map(variant => ({
            valueLabels: variant.valueLabels,
            price: Number(variant.price) || 0,
            costPrice: variant.costPrice === '' || variant.costPrice == null ? null : Number(variant.costPrice),
            sku: variant.sku?.trim() || null,
            barcode: variant.barcode?.trim() || null,
            isActive: variant.isActive ?? true,
          }))
        : null,
    })
    const newId = res?.data?.id ?? res?.data
    saveUnitHistory(form.value.unit)
    if (newId && selectedGroupIds.value.size > 0) {
      await assignOptionGroupsToProduct(newId, [...selectedGroupIds.value])
    }
    if (addNew) {
      form.value = emptyForm()
      imageUrl.value = ''
      imagePreview.value = ''
      errorMessage.value = ''
      selectedGroupIds.value = new Set()
      variants.value = []
    } else {
      router.push(newId ? { name: 'productsDetail', params: { id: newId } } : { name: 'products' })
    }
  } catch (err) {
    errorMessage.value = extractError(err)
  } finally {
    loading.value = false
  }
}

onMounted(() => { loadCategories(); loadOptionGroups() })
</script>

<template>
  <div class="tw:flex tw:flex-col tw:flex-1 tw:min-h-0 tw:bg-white tw:dark:bg-neutral-900">

    <!-- â”€â”€ Top bar â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ -->
    <div class="tw:flex tw:items-center tw:px-4 tw:py-3 tw:border-b tw:border-slate-100 tw:dark:border-white/5 tw:shrink-0">
      <prime-button severity="secondary" text :class="btnIcon" @click="router.back()">
        <iconify icon="ph:arrow-left-bold" />
      </prime-button>
      <h1 class="tw:flex-1 tw:text-center tw:font-semibold tw:text-base tw:text-slate-800 tw:dark:text-white">
        {{ t('products.create.mobile.title') }}
      </h1>
      <div class="tw:w-9 tw:shrink-0" />
    </div>

    <!-- â”€â”€ Scrollable content â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ -->
    <div class="tw:flex-1 tw:overflow-y-auto tw:pb-24">

      <!-- Hidden file inputs -->
      <input ref="galleryInput" type="file" accept="image/*" class="tw:hidden" @change="handleImage" />
      <input ref="cameraInput" type="file" accept="image/*" capture="environment" class="tw:hidden" @change="handleImage" />

      <!-- â”€â”€ Image section â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ -->
      <div class="tw:bg-slate-50 tw:dark:bg-neutral-800 tw:px-4 tw:py-4">
        <div v-if="imagePreview || imageUrl" class="tw:flex tw:items-center tw:gap-3">
          <div class="tw:relative tw:shrink-0">
            <img
              :src="imagePreview || imageUrl"
              alt="preview"
              class="tw:w-24 tw:h-24 tw:rounded-xl tw:object-cover tw:border tw:border-slate-200 tw:dark:border-white/10"
            />
            <button
              type="button"
              class="tw:absolute tw:-top-2 tw:-right-2 tw:w-6 tw:h-6 tw:rounded-full tw:bg-slate-700 tw:text-white tw:flex tw:items-center tw:justify-center tw:border-0 tw:cursor-pointer"
              @click="clearImage"
            >
              <iconify icon="ph:x-bold" class="tw:text-xs" />
            </button>
          </div>
          <prime-button severity="secondary" outlined size="small" :loading="uploading" @click="galleryInput.click()">
            <iconify icon="ph:pencil-simple-bold" />
            <span>{{ t('products.create.mobile.changePhoto') }}</span>
          </prime-button>
        </div>
        <div v-else class="tw:flex tw:gap-3">
          <button
            type="button"
            class="tw:flex-1 tw:h-24 tw:border-2 tw:border-dashed tw:border-slate-300 tw:dark:border-white/20 tw:rounded-xl tw:flex tw:flex-col tw:items-center tw:justify-center tw:gap-1.5 tw:bg-transparent tw:cursor-pointer tw:active:bg-slate-100 tw:dark:active:bg-white/5 tw:transition-colors"
            @click="galleryInput.click()"
          >
            <iconify icon="ph:image-bold" class="tw:text-2xl tw:text-blue-500" />
            <span class="tw:text-xs tw:text-slate-500 tw:dark:text-slate-400">{{ t('products.create.mobile.addPhoto') }}</span>
          </button>
          <button
            type="button"
            class="tw:flex-1 tw:h-24 tw:border-2 tw:border-dashed tw:border-slate-300 tw:dark:border-white/20 tw:rounded-xl tw:flex tw:flex-col tw:items-center tw:justify-center tw:gap-1.5 tw:bg-transparent tw:cursor-pointer tw:active:bg-slate-100 tw:dark:active:bg-white/5 tw:transition-colors"
            @click="cameraInput.click()"
          >
            <iconify icon="ph:camera-bold" class="tw:text-2xl tw:text-blue-500" />
            <span class="tw:text-xs tw:text-slate-500 tw:dark:text-slate-400">{{ t('products.create.mobile.takePhoto') }}</span>
          </button>
        </div>
      </div>

      <!-- â”€â”€ Error â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ -->
      <prime-alert v-if="errorMessage" severity="error" variant="accent" closable class="tw:mx-4 tw:mt-3" @close="errorMessage = ''">
        {{ errorMessage }}
      </prime-alert>

      <!-- â”€â”€ Form fields â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ -->
      <div class="tw:px-4">

        <!-- Name -->
        <div class="tw:py-4 tw:border-b tw:border-slate-100 tw:dark:border-white/5">
          <label class="tw:block tw:text-xs tw:text-slate-500 tw:dark:text-slate-400 tw:mb-2">
            {{ t('products.create.mobile.productName') }}<span class="tw:text-red-400 tw:ml-0.5">*</span>
          </label>
          <input
            v-model="form.name"
            type="text"
            :placeholder="t('products.form.namePlaceholder')"
            class="tw:w-full tw:bg-transparent tw:border-0 tw:outline-none tw:text-base tw:text-slate-800 tw:dark:text-white tw:placeholder-slate-300 tw:dark:placeholder-white/20"
          />
        </div>

        <!-- Price row -->
        <div class="tw:flex tw:py-4 tw:border-b tw:border-slate-100 tw:dark:border-white/5">
          <div class="tw:flex-1 tw:min-w-0">
            <label class="tw:block tw:text-xs tw:text-slate-500 tw:dark:text-slate-400 tw:mb-2">{{ t('products.create.mobile.sellingPrice') }}</label>
            <input
              v-model="form.price"
              type="number"
              inputmode="numeric"
              :placeholder="t('products.form.pricePlaceholder')"
              class="tw:w-full tw:bg-transparent tw:border-0 tw:outline-none tw:text-base tw:text-slate-800 tw:dark:text-white tw:placeholder-slate-300 tw:dark:placeholder-white/20"
            />
          </div>
          <div class="tw:w-px tw:bg-slate-100 tw:dark:bg-white/5 tw:mx-4 tw:my-1 tw:shrink-0" />
          <div class="tw:flex-1 tw:min-w-0">
            <label class="tw:block tw:text-xs tw:text-slate-500 tw:dark:text-slate-400 tw:mb-2">{{ t('products.create.mobile.costPrice') }}</label>
            <input
              v-model="form.costPrice"
              type="number"
              inputmode="numeric"
              :placeholder="t('products.form.pricePlaceholder')"
              class="tw:w-full tw:bg-transparent tw:border-0 tw:outline-none tw:text-base tw:text-slate-800 tw:dark:text-white tw:placeholder-slate-300 tw:dark:placeholder-white/20"
            />
          </div>
        </div>

        <!-- Unit -->
        <div class="tw:py-4 tw:border-b tw:border-slate-100 tw:dark:border-white/5">
          <label class="tw:block tw:text-xs tw:text-slate-500 tw:dark:text-slate-400 tw:mb-2">{{ t('products.create.mobile.unit') }}</label>
          <input
            v-model="form.unit"
            type="text"
            :placeholder="t('products.create.mobile.unitPlaceholder')"
            class="tw:w-full tw:bg-transparent tw:border-0 tw:outline-none tw:text-base tw:text-slate-800 tw:dark:text-white tw:placeholder-slate-300 tw:dark:placeholder-white/20"
          />
          <div v-if="unitHistory.length" class="tw:flex tw:flex-wrap tw:gap-2 tw:mt-3">
            <prime-button
              v-for="unit in unitHistory"
              :key="unit"
              :severity="form.unit === unit ? 'info' : 'secondary'"
              :outlined="form.unit !== unit"
              size="small"
              @click="form.unit = unit"
            >
              {{ unit }}
            </prime-button>
          </div>
        </div>

        <!-- More info toggle -->
        <prime-button
          severity="info"
          text
          fluid
          size="small"
          class="tw:justify-start! tw:py-4! tw:px-0!"
          @click="moreInfoOpen = !moreInfoOpen"
        >
          {{ moreInfoOpen ? t('products.create.mobile.hideInfo') : t('products.create.mobile.moreInfo') }}
          <iconify :icon="moreInfoOpen ? 'ph:caret-up-bold' : 'ph:caret-down-bold'" class="tw:ml-1" />
        </prime-button>

        <!-- â”€â”€ More info content â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ -->
        <template v-if="moreInfoOpen">

          <!-- SKU + Barcode -->
          <div class="tw:flex tw:pb-4 tw:border-b tw:border-slate-100 tw:dark:border-white/5">
            <div class="tw:flex-1 tw:min-w-0">
              <label class="tw:block tw:text-xs tw:text-slate-500 tw:dark:text-slate-400 tw:mb-2">{{ t('products.create.mobile.sku') }}</label>
              <input
                v-model="form.sku"
                type="text"
                class="tw:w-full tw:bg-transparent tw:border-0 tw:outline-none tw:text-base tw:text-slate-800 tw:dark:text-white tw:placeholder-slate-300 tw:dark:placeholder-white/20"
              />
            </div>
            <div class="tw:w-px tw:bg-slate-100 tw:dark:bg-white/5 tw:mx-4 tw:my-1 tw:shrink-0" />
            <div class="tw:flex-1 tw:min-w-0">
              <label class="tw:block tw:text-xs tw:text-slate-500 tw:dark:text-slate-400 tw:mb-2">{{ t('products.create.mobile.barcode') }}</label>
              <div class="tw:flex tw:items-center tw:gap-2">
                <input
                  v-model="form.barcode"
                  type="text"
                  class="tw:flex-1 tw:min-w-0 tw:bg-transparent tw:border-0 tw:outline-none tw:text-base tw:text-slate-800 tw:dark:text-white tw:placeholder-slate-300 tw:dark:placeholder-white/20"
                />
                <prime-button severity="secondary" text :class="btnIcon">
                  <iconify icon="ph:barcode-bold" class="tw:text-slate-400" />
                </prime-button>
              </div>
            </div>
          </div>

          <!-- Discount price -->
          <div class="tw:py-4 tw:border-b tw:border-slate-100 tw:dark:border-white/5">
            <label class="tw:block tw:text-xs tw:text-slate-500 tw:dark:text-slate-400 tw:mb-2">{{ t('products.create.mobile.discountPrice') }}</label>
            <input
              v-model="form.discountPrice"
              type="number"
              inputmode="numeric"
              :placeholder="t('products.form.pricePlaceholder')"
              class="tw:w-full tw:bg-transparent tw:border-0 tw:outline-none tw:text-base tw:text-slate-800 tw:dark:text-white tw:placeholder-slate-300 tw:dark:placeholder-white/20"
            />
          </div>

          <!-- Category chips -->
          <div class="tw:py-4 tw:border-b tw:border-slate-100 tw:dark:border-white/5">
            <label class="tw:block tw:text-xs tw:text-slate-500 tw:dark:text-slate-400 tw:mb-3">{{ t('products.create.mobile.category') }}</label>
            <div class="tw:flex tw:items-center tw:gap-2 tw:overflow-x-auto tw:pb-1" style="scrollbar-width: none;">
              <prime-button
                severity="secondary"
                text
                :class="btnIcon"
                class="tw:shrink-0"
                @click="openCatDrawer"
              >
                <iconify icon="ph:list-bold" />
              </prime-button>
              <prime-button
                v-for="cat in sortedCategories"
                :key="cat.id"
                :severity="form.categoryId === cat.id ? 'success' : 'secondary'"
                :outlined="form.categoryId !== cat.id"
                size="small"
                class="tw:shrink-0 tw:whitespace-nowrap"
                @click="form.categoryId = form.categoryId === cat.id ? null : cat.id"
              >
                {{ cat.name }}
              </prime-button>
            </div>
          </div>

          <!-- Addon groups -->
          <div
            class="tw:flex tw:items-center tw:py-4 tw:border-b tw:border-slate-100 tw:dark:border-white/5 tw:cursor-pointer tw:active:bg-slate-50 tw:dark:active:bg-white/3 tw:-mx-4 tw:px-4"
            @click="openAddonDrawer"
          >
            <span class="tw:flex-1 tw:text-base"
              :class="selectedGroupIds.size > 0 ? 'tw:text-slate-800 tw:dark:text-white' : 'tw:text-slate-400 tw:dark:text-slate-500'">
              {{ t('products.create.mobile.addonGroup') }}
            </span>
            <span v-if="selectedGroupIds.size > 0" class="tw:text-sm tw:text-primary-600 tw:dark:text-primary-400 tw:font-medium tw:mr-2">
              {{ selectedGroupIds.size }}
            </span>
            <iconify icon="ph:caret-right-bold" class="tw:text-slate-400 tw:text-sm" />
          </div>

          <!-- Track inventory (gray section) -->
          <div class="tw:-mx-4 tw:mt-2">
            <div class="tw:bg-slate-50 tw:dark:bg-neutral-800 tw:flex tw:items-center tw:px-4 tw:py-3.5">
              <p class="tw:text-xs tw:font-semibold tw:uppercase tw:tracking-widest tw:text-slate-500 tw:dark:text-slate-400 tw:flex-1">
                {{ t('products.create.mobile.trackInventory') }}
              </p>
              <prime-toggle-switch v-model="form.trackInventory" />
            </div>
            <div class="tw:flex tw:items-center tw:px-4 tw:py-3.5 tw:border-b tw:border-slate-100 tw:dark:border-white/5">
              <span class="tw:flex-1 tw:text-base tw:text-slate-800 tw:dark:text-white">{{ t('products.create.mobile.stockStatus') }}</span>
              <div class="tw:flex tw:rounded-lg tw:overflow-hidden tw:border tw:border-slate-200 tw:dark:border-white/10">
                <prime-button
                  :severity="form.inStock ? 'success' : 'secondary'"
                  :outlined="!form.inStock"
                  :text="!form.inStock"
                  size="small"
                  class="tw:rounded-none! tw:border-0!"
                  @click="form.inStock = true"
                >
                  {{ t('products.create.mobile.inStock') }}
                </prime-button>
                <div class="tw:w-px tw:bg-slate-200 tw:dark:bg-white/10" />
                <prime-button
                  :severity="!form.inStock ? 'danger' : 'secondary'"
                  :outlined="form.inStock"
                  :text="form.inStock"
                  size="small"
                  class="tw:rounded-none! tw:border-0!"
                  @click="form.inStock = false"
                >
                  {{ t('products.create.mobile.outOfStock') }}
                </prime-button>
              </div>
            </div>
          </div>

          <!-- Attributes section -->
          <template v-if="form.attributes.length">
            <div class="tw:-mx-4 tw:mt-2">
              <div class="tw:bg-slate-50 tw:dark:bg-neutral-800 tw:px-4 tw:py-3">
                <p class="tw:text-xs tw:font-semibold tw:uppercase tw:tracking-widest tw:text-slate-500 tw:dark:text-slate-400">
                  {{ t('products.create.mobile.attrSectionHeader') }}
                </p>
              </div>
              <div
                v-for="(attr, i) in form.attributes"
                :key="i"
                class="tw:px-4 tw:pt-3 tw:pb-4"
              >
                <div class="tw:flex tw:items-center tw:mb-2">
                  <span class="tw:flex-1 tw:text-sm tw:font-medium tw:text-slate-800 tw:dark:text-white">
                    {{ attr.name }}
                    <span class="tw:text-slate-400 tw:font-normal tw:ml-1">({{ attr.values.length }})</span>
                  </span>
                  <div class="tw:flex tw:items-center tw:gap-2">
                    <prime-button severity="info" text size="small" class="tw:p-0! tw:h-auto! tw:font-medium!" @click="editAttr(i)">
                      {{ t('products.create.mobile.attrEdit') }}
                    </prime-button>
                    <prime-button severity="danger" text :class="btnIcon" @click="removeAttr(i)">
                      <iconify icon="ph:trash-bold" />
                    </prime-button>
                  </div>
                </div>
                <div class="tw:flex tw:flex-wrap tw:gap-2">
                  <span
                    v-for="val in attr.values"
                    :key="val"
                    class="tw:inline-flex tw:items-center tw:gap-1 tw:px-3 tw:py-1 tw:rounded-lg tw:border tw:border-slate-200 tw:dark:border-white/10 tw:text-sm tw:text-slate-700 tw:dark:text-white/80"
                  >
                    <iconify v-if="val === attr.defaultValue" icon="ph:star-fill" class="tw:text-amber-400 tw:text-xs tw:shrink-0" />
                    {{ val }}
                  </span>
                </div>
              </div>
            </div>
          </template>

          <div v-if="form.attributes.length" class="tw:-mx-4 tw:mt-2">
            <div class="tw:bg-slate-50 tw:dark:bg-neutral-800 tw:px-4 tw:py-3">
              <div class="tw:flex tw:items-center tw:justify-between tw:gap-2">
                <div class="tw:min-w-0">
                  <p class="tw:text-xs tw:font-semibold tw:uppercase tw:tracking-widest tw:text-slate-500 tw:dark:text-slate-400">
                    BẢNG GIÁ BIẾN THỂ
                  </p>
                  <p class="tw:text-xs tw:text-slate-400 tw:mt-1">
                    Giá bán cuối cùng cho từng tổ hợp tuỳ chọn.
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
              </div>

              <div
                v-for="variant in variants"
                :key="variantKey(variant.valueLabels)"
                class="tw:rounded-xl tw:border tw:border-slate-200 tw:dark:border-white/10 tw:p-3 tw:space-y-3"
              >
                <div class="tw:flex tw:flex-wrap tw:gap-2">
                  <span
                    v-for="(label, index) in variant.valueLabels"
                    :key="`${variantKey(variant.valueLabels)}-${index}`"
                    class="tw:inline-flex tw:items-center tw:rounded-lg tw:bg-slate-100 tw:dark:bg-white/5 tw:px-2.5 tw:py-1 tw:text-xs tw:text-slate-700 tw:dark:text-white/80"
                  >
                    {{ form.attributes[index]?.name }}: {{ label }}
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

          <!-- Add attribute row -->
          <div
            class="tw:flex tw:items-center tw:gap-2 tw:py-4 tw:border-b tw:border-slate-100 tw:dark:border-white/5 tw:cursor-pointer tw:active:bg-slate-50 tw:dark:active:bg-white/3 tw:-mx-4 tw:px-4"
            @click="openAttrDrawer"
          >
            <iconify icon="ph:plus-circle-bold" class="tw:text-lg tw:text-blue-500 tw:shrink-0" />
            <span class="tw:text-blue-500 tw:font-medium tw:text-sm">{{ t('products.create.mobile.addAttribute') }}</span>
            <span class="tw:text-sm tw:text-slate-400">{{ t('products.create.mobile.addAttributeHint') }}</span>
          </div>
        </template>

      </div>

      <!-- â”€â”€ Display settings section â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ -->
      <div class="tw:mx-4 tw:mt-4 tw:bg-slate-50 tw:dark:bg-neutral-800 tw:rounded-xl tw:p-4">
        <p class="tw:text-[10px] tw:font-semibold tw:uppercase tw:tracking-widest tw:text-slate-400 tw:dark:text-slate-500 tw:mb-3">
          {{ t('products.create.mobile.displaySettingsLabel') }}
        </p>
        <div class="tw:flex tw:items-center tw:gap-3">
          <iconify icon="ph:gear-bold" class="tw:text-xl tw:text-slate-300 tw:dark:text-white/20 tw:shrink-0" />
          <p class="tw:text-sm tw:text-slate-400 tw:dark:text-slate-500">{{ t('products.create.mobile.displaySettingsHint') }}</p>
        </div>
      </div>

    </div>

    <!-- â”€â”€ Category picker drawer â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ -->
    <prime-drawer
      v-model:visible="catDrawerVisible"
      position="bottom"
      :style="{ height: '80dvh' }"
      :pt="{ root: { class: 'tw:rounded-t-2xl' } }"
    >
      <template #header>
        <span class="tw:font-semibold tw:text-base">{{ t('products.create.mobile.catDrawerTitle') }}</span>
      </template>

      <div class="tw:flex tw:flex-col tw:h-full tw:gap-3">

        <!-- Search + add -->
        <div class="tw:flex tw:items-center tw:gap-2 tw:shrink-0">
          <div class="tw:flex tw:flex-1 tw:items-center tw:gap-2 tw:bg-slate-100 tw:dark:bg-white/5 tw:rounded-xl tw:px-3 tw:py-2.5">
            <iconify icon="ph:magnifying-glass-bold" class="tw:text-slate-400 tw:shrink-0" />
            <input
              v-model="catSearch"
              type="text"
              :placeholder="t('products.create.mobile.catSearchPlaceholder')"
              class="tw:flex-1 tw:bg-transparent tw:border-0 tw:outline-none tw:text-sm tw:text-slate-800 tw:dark:text-white tw:placeholder-slate-400"
            />
          </div>
          <prime-button severity="success" :class="btnIcon" @click="createCatVisible = true">
            <iconify icon="ph:plus-bold" />
          </prime-button>
        </div>

        <!-- Category grid -->
        <div class="tw:flex-1 tw:overflow-y-auto">
          <div class="tw:grid tw:grid-cols-4 tw:gap-3">
            <div
              v-for="cat in filteredCategories"
              :key="cat.id"
              class="tw:flex tw:flex-col tw:items-center tw:gap-1.5 tw:cursor-pointer"
              @click="catDraftId = catDraftId === cat.id ? null : cat.id"
            >
              <div
                class="tw:w-full tw:aspect-square tw:rounded-xl tw:overflow-hidden tw:bg-slate-100 tw:dark:bg-white/5 tw:flex tw:items-center tw:justify-center tw:transition-all"
                :class="catDraftId === cat.id ? 'tw:ring-2 tw:ring-blue-500' : ''"
              >
                <img
                  v-if="cat.imageUrl"
                  :src="cat.imageUrl"
                  :alt="cat.name"
                  class="tw:w-full tw:h-full tw:object-cover"
                />
                <iconify v-else icon="ph:tag-bold" class="tw:text-xl tw:text-slate-300 tw:dark:text-white/20" />
              </div>
              <p
                class="tw:text-xs tw:text-center tw:leading-tight tw:line-clamp-2"
                :class="catDraftId === cat.id ? 'tw:text-blue-500 tw:font-medium' : 'tw:text-slate-600 tw:dark:text-slate-300'"
              >
                {{ cat.name }}
              </p>
            </div>
          </div>
        </div>

      </div>

      <template #footer>
        <div class="tw:flex tw:gap-3 tw:pt-2">
          <prime-button severity="secondary" outlined fluid @click="catDrawerVisible = false">
            {{ t('products.create.mobile.catBack') }}
          </prime-button>
          <prime-button severity="success" fluid @click="confirmCategory">
            {{ t('products.create.mobile.catUpdate') }}
          </prime-button>
        </div>
      </template>
    </prime-drawer>

    <!-- â”€â”€ Create category drawer â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ -->
    <CreateCategoryDrawer v-model:visible="createCatVisible" @created="onCategoryCreated" />

    <!-- â”€â”€ Addon groups drawer â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ -->
    <prime-drawer
      v-model:visible="addonDrawerVisible"
      position="bottom"
      :style="{ height: '80dvh' }"
      :pt="{ root: { class: 'tw:rounded-t-2xl' } }"
    >
      <template #header>
        <span class="tw:font-semibold tw:text-base">{{ t('products.create.mobile.addonGroup') }}</span>
      </template>

      <div class="tw:flex tw:flex-col tw:h-full tw:gap-3">
        <!-- Search -->
        <div class="tw:flex tw:items-center tw:gap-2 tw:bg-slate-100 tw:dark:bg-white/5 tw:rounded-xl tw:px-3 tw:py-2.5 tw:shrink-0">
          <iconify icon="ph:magnifying-glass-bold" class="tw:text-slate-400 tw:shrink-0" />
          <input
            v-model="addonSearch"
            type="text"
            :placeholder="t('products.create.mobile.addonSearchPlaceholder')"
            class="tw:flex-1 tw:bg-transparent tw:border-0 tw:outline-none tw:text-sm tw:text-slate-800 tw:dark:text-white tw:placeholder-slate-400"
          />
        </div>

        <!-- Groups list -->
        <div class="tw:flex-1 tw:overflow-y-auto tw:-mx-4">
          <div
            v-for="group in filteredGroups"
            :key="group.id"
            class="tw:flex tw:items-center tw:gap-3 tw:px-4 tw:py-3.5 tw:border-b tw:border-slate-100 tw:dark:border-white/5 tw:cursor-pointer tw:active:bg-slate-50 tw:dark:active:bg-white/5"
            @click="toggleAddonGroup(group.id)"
          >
            <div
              class="tw:shrink-0 tw:w-5 tw:h-5 tw:rounded tw:border-2 tw:flex tw:items-center tw:justify-center tw:transition-colors"
              :class="addonDraftIds.has(group.id)
                ? 'tw:bg-primary-500 tw:border-primary-500'
                : 'tw:border-slate-300 tw:dark:border-white/30'"
            >
              <iconify v-if="addonDraftIds.has(group.id)" icon="ph:check-bold" class="tw:text-white tw:text-xs" />
            </div>
            <div class="tw:flex-1 tw:min-w-0">
              <p class="tw:text-sm tw:font-medium tw:text-slate-800 tw:dark:text-white tw:leading-tight">{{ group.name }}</p>
              <p v-if="group.valueNames?.length" class="tw:text-xs tw:text-slate-400 tw:dark:text-slate-500 tw:mt-0.5 tw:line-clamp-1">
                {{ group.valueNames.join('; ') }}
              </p>
            </div>
            <span class="tw:shrink-0 tw:text-xs tw:text-slate-400">{{ group.valueCount }} tuỳ chọn</span>
          </div>

          <div v-if="filteredGroups.length === 0" class="tw:flex tw:flex-col tw:items-center tw:justify-center tw:py-16 tw:gap-2">
            <iconify icon="ph:magnifying-glass-bold" class="tw:text-3xl tw:text-slate-300 tw:dark:text-white/20" />
            <p class="tw:text-sm tw:text-slate-400">{{ t('products.create.mobile.addonNoResults') }}</p>
          </div>
        </div>
      </div>

      <template #footer>
        <div class="tw:flex tw:gap-3 tw:pt-2">
          <prime-button severity="secondary" outlined fluid @click="addonDrawerVisible = false">
            {{ t('products.create.mobile.catBack') }}
          </prime-button>
          <prime-button severity="success" fluid @click="confirmAddonGroups">
            {{ t('products.create.mobile.catUpdate') }}
            <span v-if="addonDraftIds.size > 0" class="tw:ml-1 tw:opacity-70">({{ addonDraftIds.size }})</span>
          </prime-button>
        </div>
      </template>
    </prime-drawer>

    <!-- â”€â”€ Add attribute drawer â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ -->
    <prime-drawer
      v-model:visible="attrDrawerVisible"
      position="bottom"
      :style="{ height: 'auto' }"
      :pt="{ root: { class: 'tw:rounded-t-2xl' } }"
    >
      <template #header>
        <span class="tw:font-semibold tw:text-base">{{ attrEditIndex >= 0 ? t('products.create.mobile.attrDrawerEditTitle') : t('products.create.mobile.attrDrawerTitle') }}</span>
      </template>

      <div class="tw:flex tw:flex-col tw:gap-0 tw:pb-4">

        <!-- Attribute name -->
        <div class="tw:pb-4 tw:border-b tw:border-slate-100 tw:dark:border-white/5">
          <label class="tw:block tw:text-xs tw:text-slate-500 tw:dark:text-slate-400 tw:mb-2">
            {{ t('products.create.mobile.attrNameLabel') }}<span class="tw:text-red-400 tw:ml-0.5">*</span>
          </label>
          <input
            ref="attrNameInput"
            v-model="attrName"
            type="text"
            class="tw:w-full tw:bg-transparent tw:border-0 tw:border-b-2 tw:outline-none tw:text-base tw:text-slate-800 tw:dark:text-white tw:placeholder-slate-300 tw:dark:placeholder-white/20 tw:pb-1 tw:transition-colors"
            :class="attrName ? 'tw:border-primary-500' : 'tw:border-slate-200 tw:dark:border-white/10'"
            @keydown.enter.prevent
          />
        </div>

        <!-- Values section header -->
        <p class="tw:text-[10px] tw:font-semibold tw:uppercase tw:tracking-widest tw:text-slate-400 tw:dark:text-slate-500 tw:mt-4 tw:mb-2">
          {{ t('products.create.mobile.attrValuesHeader') }}
        </p>

        <!-- Value list (last item is always the empty "add" input) -->
        <div
          v-for="(val, i) in attrValues"
          :key="i"
          class="tw:flex tw:items-center tw:gap-2 tw:py-3 tw:border-b tw:border-slate-100 tw:dark:border-white/5"
        >
          <input
            :value="val"
            type="text"
            :placeholder="i === attrValues.length - 1 ? t('products.create.mobile.attrValuePlaceholder') : ''"
            class="tw:flex-1 tw:bg-transparent tw:border-0 tw:outline-none tw:text-base tw:text-slate-800 tw:dark:text-white tw:placeholder-slate-300 tw:dark:placeholder-white/20"
            @input="onAttrValueInput(i, $event.target.value)"
          />
          <template v-if="val !== ''">
            <prime-button
              :severity="attrDefaultValue === val.trim() ? 'warning' : 'secondary'"
              text
              :class="btnIcon"
              v-tooltip.top="t('products.create.mobile.attrSetDefault')"
              @click="attrDefaultValue = attrDefaultValue === val.trim() ? null : val.trim()"
            >
              <iconify :icon="attrDefaultValue === val.trim() ? 'ph:star-fill' : 'ph:star'" />
            </prime-button>
            <prime-button
              severity="danger"
              text
              :class="btnIcon"
              @click="removeAttrValue(i)"
            >
              <iconify icon="ph:trash-bold" />
            </prime-button>
          </template>
        </div>

        <!-- Save button -->
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

    <!-- â”€â”€ Bottom action bar â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ -->
    <div class="tw:flex tw:gap-3 tw:px-4 tw:py-3 tw:border-t tw:border-slate-100 tw:dark:border-white/5 tw:bg-white tw:dark:bg-neutral-900 tw:shrink-0">
      <prime-button
        :severity="isValid ? 'success' : 'secondary'"
        outlined
        fluid
        :disabled="!isValid"
        :loading="loading"
        @click="submit(true)"
      >
        {{ t('products.create.mobile.saveAndNew') }}
      </prime-button>
      <prime-button
        v-if="can('product.create')"
        :severity="isValid ? 'success' : 'secondary'"
        fluid
        :disabled="!isValid"
        :loading="loading"
        @click="submit(false)"
      >
        {{ t('products.create.mobile.save') }}
      </prime-button>
    </div>

  </div>
</template>
