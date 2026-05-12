<script setup>
import { ref, computed, onMounted, nextTick } from 'vue'
import { getProduct, updateProduct, replaceAttributeGroups } from '@/services/product.service'
import { getCategory } from '@/services/category.service'
import { uploadImage } from '@/services/upload.service'
import { getProductOptionGroups, assignOptionGroupsToProduct } from '@/services/product-option-group.service'
import CreateCategoryDrawer from '@/components/products/CreateCategoryDrawer.vue'

const { t } = useI18n()
const router = useRouter()
const route = useRoute()
const { can } = usePermission()
const toast = useToast()

const productId = Number(route.params.id)

const UNIT_HISTORY_KEY = 'product-unit-history'

const form = ref({
  name: '', price: '', costPrice: '', unit: '',
  sku: '', barcode: '', discountPrice: '',
  categoryId: null, isActive: true,
  description: '', isAccompaniment: false, estimatedPrepMinutes: null,
})

const loading = ref(true)
const saving = ref(false)
const errorMessage = ref('')
const uploading = ref(false)
const imageUrl = ref('')
const imagePreview = ref('')
const moreInfoOpen = ref(false)

// ── Option groups ─────────────────────────────────────────────────
const allOptionGroups = ref([])
const selectedGroupIds = ref([])
const optionGroupDrawerVisible = ref(false)
const draftGroupIds = ref([])

const loadOptionGroups = async () => {
  try {
    const res = await getProductOptionGroups()
    const raw = res?.data
    allOptionGroups.value = Array.isArray(raw) ? raw
      : Array.isArray(raw?.value) ? raw.value
      : []
  } catch { /* non-critical */ }
}

const openOptionGroupDrawer = () => {
  draftGroupIds.value = [...selectedGroupIds.value]
  optionGroupDrawerVisible.value = true
}

const toggleDraftGroup = (id) => {
  const idx = draftGroupIds.value.indexOf(id)
  if (idx >= 0) draftGroupIds.value.splice(idx, 1)
  else draftGroupIds.value.push(id)
}

const confirmOptionGroups = () => {
  selectedGroupIds.value = [...draftGroupIds.value]
  optionGroupDrawerVisible.value = false
}

// ── Attribute drawer ───────────────────────────────────────────────
const attrDrawerVisible = ref(false)
const attrEditIndex = ref(-1)
const attrName = ref('')
const attrValues = ref([''])
const attrNameInput = ref(null)
const attributes = ref([])

const openAttrDrawer = () => {
  attrEditIndex.value = -1
  attrName.value = ''
  attrValues.value = ['']
  attrDrawerVisible.value = true
  nextTick(() => attrNameInput.value?.focus())
}

const editAttr = (i) => {
  attrEditIndex.value = i
  attrName.value = attributes.value[i].name
  attrValues.value = [...attributes.value[i].values, '']
  attrDrawerVisible.value = true
  nextTick(() => attrNameInput.value?.focus())
}

const onAttrValueInput = (i, val) => {
  attrValues.value[i] = val
  if (i === attrValues.value.length - 1 && val !== '') attrValues.value.push('')
}

const removeAttrValue = (i) => {
  attrValues.value.splice(i, 1)
  if (attrValues.value.length === 0 || attrValues.value[attrValues.value.length - 1] !== '')
    attrValues.value.push('')
}

const isAttrValid = computed(() => attrName.value.trim().length > 0)

const saveAttr = () => {
  if (!isAttrValid.value) return
  const entry = { name: attrName.value.trim(), values: attrValues.value.filter(v => v.trim() !== '') }
  if (attrEditIndex.value >= 0) attributes.value[attrEditIndex.value] = entry
  else attributes.value.push(entry)
  attrDrawerVisible.value = false
}

const removeAttr = (i) => attributes.value.splice(i, 1)

// ── Unit history ───────────────────────────────────────────────────
const unitHistory = ref(JSON.parse(localStorage.getItem(UNIT_HISTORY_KEY) ?? '[]'))
const saveUnitHistory = (unit) => {
  if (!unit?.trim()) return
  const updated = [...new Set([unit.trim(), ...unitHistory.value])].slice(0, 15)
  unitHistory.value = updated
  localStorage.setItem(UNIT_HISTORY_KEY, JSON.stringify(updated))
}

// ── Categories ─────────────────────────────────────────────────────
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

// ── Image ──────────────────────────────────────────────────────────
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

// ── Load product ───────────────────────────────────────────────────
const loadProduct = async () => {
  loading.value = true
  errorMessage.value = ''
  try {
    const res = await getProduct(productId)
    const p = res?.data
    if (!p) { errorMessage.value = t('products.detail.notFound'); return }
    form.value = {
      name:              p.name ?? '',
      price:             p.price ?? '',
      costPrice:         p.costPrice ?? '',
      unit:              p.unit ?? '',
      sku:               p.sku ?? '',
      barcode:           p.barcode ?? '',
      discountPrice:     p.discountPrice ?? '',
      categoryId:        p.categoryId ?? null,
      isActive:          p.isActive ?? true,
      description:       p.description ?? '',
      isAccompaniment:   p.isAccompaniment ?? false,
      estimatedPrepMinutes: p.estimatedPrepMinutes ?? null,
    }
    imageUrl.value = p.imageUrl ?? ''
    selectedGroupIds.value = p.assignedOptionGroupIds ?? []
    attributes.value = (p.attributeGroups ?? []).map(g => ({
      name: g.name,
      values: (g.values ?? []).map(v => v.label),
    }))
  } catch (err) {
    errorMessage.value = err?.response?.data?.message || t('products.detail.error.loadFailed')
  } finally {
    loading.value = false
  }
}

const extractError = (err) => {
  const data = err?.response?.data
  if (Array.isArray(data?.errors))
    return data.errors.map(e => e.errorMessage ?? e).join('; ')
  return data?.message || t('products.detail.error.updateFailed')
}

const isValid = computed(() => form.value.name.trim().length > 0)

const save = async () => {
  if (saving.value) return
  saving.value = true
  errorMessage.value = ''
  try {
    await updateProduct(productId, {
      name:                 form.value.name.trim(),
      price:                Number(form.value.price) || 0,
      costPrice:            Number(form.value.costPrice) || null,
      sku:                  form.value.sku?.trim() || null,
      barcode:              form.value.barcode?.trim() || null,
      discountPrice:        Number(form.value.discountPrice) || null,
      categoryId:           form.value.categoryId,
      isActive:             form.value.isActive,
      description:          form.value.description?.trim() || null,
      isAccompaniment:      form.value.isAccompaniment,
      estimatedPrepMinutes: form.value.estimatedPrepMinutes || null,
      imageUrl:             imageUrl.value || null,
    })
    await replaceAttributeGroups(productId, {
      groups: attributes.value.map(a => ({
        name: a.name,
        isRequired: false,
        selectionType: 'Single',
        values: a.values.map(v => ({ label: v, priceAdjustment: 0, isDefault: false })),
      })),
    })
    await assignOptionGroupsToProduct(productId, selectedGroupIds.value)
    saveUnitHistory(form.value.unit)
    toast.add({ severity: 'success', summary: t('products.detail.updateSuccess'), life: 2000 })
  } catch (err) {
    errorMessage.value = extractError(err)
  } finally {
    saving.value = false
  }
}

onMounted(() => {
  loadCategories()
  loadOptionGroups()
  loadProduct()
})
</script>

<template>
  <div class="tw:flex tw:flex-col tw:flex-1 tw:min-h-0 tw:bg-white tw:dark:bg-neutral-900">

    <!-- ── Top bar ──────────────────────────────────────────────── -->
    <div class="tw:flex tw:items-center tw:px-4 tw:py-3 tw:border-b tw:border-slate-100 tw:dark:border-white/5 tw:shrink-0">
      <prime-button severity="secondary" text :class="btnIcon" @click="router.back()">
        <iconify icon="ph:arrow-left-bold" />
      </prime-button>
      <h1 class="tw:flex-1 tw:text-center tw:font-semibold tw:text-base tw:text-slate-800 tw:dark:text-white tw:line-clamp-1 tw:px-2">
        {{ loading ? '...' : (form.name || t('products.detail.editTitle')) }}
      </h1>
      <div class="tw:w-9 tw:shrink-0" />
    </div>

    <!-- ── Loading skeleton ───────────────────────────────────── -->
    <div v-if="loading" class="tw:flex-1 tw:overflow-y-auto tw:pb-24">
      <div class="tw:bg-slate-50 tw:dark:bg-neutral-800 tw:px-4 tw:py-4">
        <prime-skeleton width="6rem" height="6rem" border-radius="12px" />
      </div>
      <div class="tw:px-4 tw:space-y-6 tw:pt-4">
        <prime-skeleton height="1.5rem" width="70%" />
        <prime-skeleton height="1.5rem" width="50%" />
        <prime-skeleton height="1.5rem" width="40%" />
      </div>
    </div>

    <!-- ── Scrollable content ───────────────────────────────────── -->
    <div v-else class="tw:flex-1 tw:overflow-y-auto tw:pb-24">

      <!-- Hidden file inputs -->
      <input ref="galleryInput" type="file" accept="image/*" class="tw:hidden" @change="handleImage" />
      <input ref="cameraInput" type="file" accept="image/*" capture="environment" class="tw:hidden" @change="handleImage" />

      <!-- ── Image section ──────────────────────────────────────── -->
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

      <!-- ── Error ──────────────────────────────────────────────── -->
      <prime-alert v-if="errorMessage" severity="error" variant="accent" closable class="tw:mx-4 tw:mt-3" @close="errorMessage = ''">
        {{ errorMessage }}
      </prime-alert>

      <!-- ── Form fields ─────────────────────────────────────────── -->
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

        <!-- ── More info content ───────────────────────────────── -->
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
              <prime-button severity="secondary" text :class="btnIcon" class="tw:shrink-0" @click="openCatDrawer">
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

          <!-- Option groups -->
          <div
            class="tw:flex tw:items-center tw:py-4 tw:border-b tw:border-slate-100 tw:dark:border-white/5 tw:cursor-pointer tw:active:bg-slate-50 tw:dark:active:bg-white/3 tw:-mx-4 tw:px-4"
            @click="openOptionGroupDrawer"
          >
            <div class="tw:flex-1">
              <p class="tw:text-base tw:text-slate-800 tw:dark:text-white">{{ t('products.detail.optionGroups.title') }}</p>
              <p v-if="selectedGroupIds.length" class="tw:text-xs tw:text-emerald-500 tw:mt-0.5">
                {{ selectedGroupIds.length }} {{ t('products.detail.optionGroups.selected') }}
              </p>
              <p v-else class="tw:text-xs tw:text-slate-400 tw:mt-0.5">{{ t('products.detail.optionGroups.noneSelected') }}</p>
            </div>
            <iconify icon="ph:caret-right-bold" class="tw:text-slate-400 tw:text-sm" />
          </div>

          <!-- Active status -->
          <div class="tw:-mx-4 tw:mt-2">
            <div class="tw:bg-slate-50 tw:dark:bg-neutral-800 tw:flex tw:items-center tw:px-4 tw:py-3.5">
              <p class="tw:text-xs tw:font-semibold tw:uppercase tw:tracking-widest tw:text-slate-500 tw:dark:text-slate-400 tw:flex-1">
                {{ t('products.detail.mobile.statusLabel') }}
              </p>
              <div class="tw:flex tw:rounded-lg tw:overflow-hidden tw:border tw:border-slate-200 tw:dark:border-white/10">
                <prime-button
                  :severity="form.isActive ? 'success' : 'secondary'"
                  :text="!form.isActive"
                  :outlined="!form.isActive"
                  size="small"
                  class="tw:rounded-none! tw:border-0!"
                  @click="form.isActive = true"
                >
                  {{ t('products.status.active') }}
                </prime-button>
                <div class="tw:w-px tw:bg-slate-200 tw:dark:bg-white/10" />
                <prime-button
                  :severity="!form.isActive ? 'danger' : 'secondary'"
                  :text="form.isActive"
                  :outlined="form.isActive"
                  size="small"
                  class="tw:rounded-none! tw:border-0!"
                  @click="form.isActive = false"
                >
                  {{ t('products.status.inactive') }}
                </prime-button>
              </div>
            </div>
          </div>

          <!-- Attributes section -->
          <template v-if="attributes.length">
            <div class="tw:-mx-4 tw:mt-2">
              <div class="tw:bg-slate-50 tw:dark:bg-neutral-800 tw:px-4 tw:py-3">
                <p class="tw:text-xs tw:font-semibold tw:uppercase tw:tracking-widest tw:text-slate-500 tw:dark:text-slate-400">
                  {{ t('products.create.mobile.attrSectionHeader') }}
                </p>
              </div>
              <div v-for="(attr, i) in attributes" :key="i" class="tw:px-4 tw:pt-3 tw:pb-4">
                <div class="tw:flex tw:items-center tw:mb-2">
                  <span class="tw:flex-1 tw:text-sm tw:font-medium tw:text-slate-800 tw:dark:text-white">
                    {{ attr.name }}
                    <span class="tw:text-slate-400 tw:font-normal tw:ml-1">({{ attr.values.length }})</span>
                  </span>
                  <prime-button severity="info" text size="small" class="tw:p-0! tw:h-auto! tw:font-medium!" @click="editAttr(i)">
                    {{ t('products.create.mobile.attrEdit') }}
                  </prime-button>
                </div>
                <div class="tw:flex tw:flex-wrap tw:gap-2">
                  <span
                    v-for="val in attr.values"
                    :key="val"
                    class="tw:inline-flex tw:items-center tw:px-3 tw:py-1 tw:rounded-lg tw:border tw:border-slate-200 tw:dark:border-white/10 tw:text-sm tw:text-slate-700 tw:dark:text-white/80"
                  >
                    {{ val }}
                  </span>
                </div>
              </div>
            </div>
          </template>

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

      <!-- ── Display settings section ───────────────────────────── -->
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

    <!-- ── Category picker drawer ───────────────────────────────── -->
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
                <img v-if="cat.imageUrl" :src="cat.imageUrl" :alt="cat.name" class="tw:w-full tw:h-full tw:object-cover" />
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

    <!-- ── Create category drawer ───────────────────────────────── -->
    <CreateCategoryDrawer v-model:visible="createCatVisible" @created="onCategoryCreated" />

    <!-- ── Option group picker drawer ──────────────────────────── -->
    <prime-drawer
      v-model:visible="optionGroupDrawerVisible"
      position="bottom"
      :style="{ height: '70dvh' }"
      :pt="{ root: { class: 'tw:rounded-t-2xl' } }"
    >
      <template #header>
        <span class="tw:font-semibold tw:text-base">{{ t('products.detail.optionGroups.title') }}</span>
      </template>

      <div class="tw:flex tw:flex-col tw:h-full">
        <div v-if="allOptionGroups.length === 0" class="tw:flex-1 tw:flex tw:flex-col tw:items-center tw:justify-center tw:text-muted tw:text-sm tw:gap-2">
          <iconify icon="ph:stack-bold" class="tw:text-3xl tw:opacity-30" />
          <p>{{ t('products.detail.optionGroups.noGroups') }}</p>
        </div>
        <div v-else class="tw:flex-1 tw:overflow-y-auto tw:space-y-2 tw:pb-2">
          <div
            v-for="group in allOptionGroups"
            :key="group.id"
            class="tw:flex tw:items-center tw:gap-3 tw:p-3 tw:rounded-xl tw:cursor-pointer tw:transition-colors"
            :class="draftGroupIds.includes(group.id)
              ? 'tw:bg-emerald-500/10 tw:border tw:border-emerald-400'
              : 'tw:border tw:border-slate-200 tw:dark:border-white/10'"
            @click="toggleDraftGroup(group.id)"
          >
            <iconify
              :icon="draftGroupIds.includes(group.id) ? 'ph:check-circle-bold' : 'ph:circle'"
              class="tw:text-xl tw:shrink-0"
              :class="draftGroupIds.includes(group.id) ? 'tw:text-emerald-400' : 'tw:text-slate-300 tw:dark:text-white/20'"
            />
            <div class="tw:flex-1 tw:min-w-0">
              <p class="tw:text-sm tw:font-medium tw:leading-none">{{ group.name }}</p>
              <p class="tw:text-xs tw:text-slate-400 tw:mt-1">{{ group.values?.length ?? 0 }} lựa chọn</p>
            </div>
            <prime-tag v-if="!group.isActive" severity="danger" value="Tắt" class="tw:text-[10px]! tw:px-1! tw:py-0! tw:shrink-0" />
          </div>
        </div>
      </div>

      <template #footer>
        <div class="tw:flex tw:gap-3 tw:pt-2">
          <prime-button severity="secondary" outlined fluid @click="optionGroupDrawerVisible = false">
            {{ t('common.cancel') }}
          </prime-button>
          <prime-button severity="success" fluid @click="confirmOptionGroups">
            {{ t('common.confirm') }}
          </prime-button>
        </div>
      </template>
    </prime-drawer>

    <!-- ── Add attribute drawer ──────────────────────────────────── -->
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
        <div class="tw:pb-4 tw:border-b tw:border-slate-100 tw:dark:border-white/5">
          <label class="tw:block tw:text-xs tw:text-slate-500 tw:dark:text-slate-400 tw:mb-2">
            {{ t('products.create.mobile.attrNameLabel') }}<span class="tw:text-red-400 tw:ml-0.5">*</span>
          </label>
          <input
            ref="attrNameInput"
            v-model="attrName"
            type="text"
            class="tw:w-full tw:bg-transparent tw:border-0 tw:border-b-2 tw:outline-none tw:text-base tw:text-slate-800 tw:dark:text-white tw:placeholder-slate-300 tw:dark:placeholder-white/20 tw:pb-1 tw:transition-colors"
            :class="attrName ? 'tw:border-emerald-500' : 'tw:border-slate-200 tw:dark:border-white/10'"
            @keydown.enter.prevent
          />
        </div>

        <p class="tw:text-[10px] tw:font-semibold tw:uppercase tw:tracking-widest tw:text-slate-400 tw:dark:text-slate-500 tw:mt-4 tw:mb-2">
          {{ t('products.create.mobile.attrValuesHeader') }}
        </p>

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
          <prime-button v-if="val !== ''" severity="danger" text :class="btnIcon" @click="removeAttrValue(i)">
            <iconify icon="ph:trash-bold" />
          </prime-button>
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
      </div>
    </prime-drawer>

    <!-- ── Bottom action bar ────────────────────────────────────── -->
    <div class="tw:flex tw:gap-3 tw:px-4 tw:py-3 tw:border-t tw:border-slate-100 tw:dark:border-white/5 tw:bg-white tw:dark:bg-neutral-900 tw:shrink-0">
      <prime-button
        v-if="can('product.update')"
        :severity="isValid ? 'success' : 'secondary'"
        fluid
        :disabled="!isValid || loading"
        :loading="saving"
        @click="save"
      >
        {{ t('products.detail.saveChanges') }}
      </prime-button>
    </div>

  </div>
</template>
