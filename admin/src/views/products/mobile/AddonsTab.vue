<script setup>
import { onMounted, onBeforeUnmount, ref, computed, watch } from 'vue'
import {
  getProductOptionGroups,
  getProductOptionGroupById,
  createProductOptionGroup,
  updateProductOptionGroup,
  deleteProductOptionGroup,
  toggleOptionValueStock,
  linkGroupToProducts,
  unlinkGroupFromProduct,
} from '@/services/product-option-group.service'
import { getProducts } from '@/services/product.service'
import { getCategory } from '@/services/category.service'

const props = defineProps({
  search: { type: String, default: '' },
  viewMode: { type: String, default: 'list' },
})

const { t } = useI18n()
const toast = useToast()

// ── State ─────────────────────────────────────────────────────────────
const loading = ref(false)
const groups = ref([])
const mode = ref('list') // 'list' | 'create' | 'edit'
const editingGroup = ref(null)

const filteredGroups = computed(() => {
  const q = props.search.trim().toLowerCase()
  if (!q) return groups.value
  return groups.value.filter(g => g.name.toLowerCase().includes(q))
})

// ── Form state ────────────────────────────────────────────────────────
const NAME_MAX = 20
const formName = ref('')
const formIsRequired = ref(false)
const formAllowMultiple = ref(false)
const formAllowQuantity = ref(false)
const formValues = ref([])
const submitting = ref(false)

const nameCount = computed(() => formName.value.length)

const resetForm = () => {
  formName.value = ''
  formIsRequired.value = false
  formAllowMultiple.value = false
  formAllowQuantity.value = false
  formValues.value = []
}

// ── Add-option bottom sheet ───────────────────────────────────────────
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
    costPrice: optionCostPrice.value !== null && optionCostPrice.value !== '' ? Number(optionCostPrice.value) : null,
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

// ── Navigation ────────────────────────────────────────────────────────
const createStep = ref('1')

const openCreate = () => {
  resetForm()
  editingGroup.value = null
  createStep.value = '1'
  pickerSelected.value = new Set()
  pickerSearch.value = ''
  pickerCategoryFilter.value = null
  mode.value = 'create'
}

const openEdit = async (group) => {
  try {
    const res = await getProductOptionGroupById(group.id)
    const full = res?.data
    editingGroup.value = full
    formName.value = full.name
    formIsRequired.value = full.isRequired
    formAllowMultiple.value = full.allowMultiple
    formAllowQuantity.value = full.allowQuantity
    formValues.value = (full.values ?? []).map(v => ({
      name: v.name,
      price: v.price,
      costPrice: v.costPrice ?? null,
    }))
    linkedProducts.value = full.linkedProducts ?? []
    editTab.value = 'info'
    mode.value = 'edit'
  } catch {
    toast.add({ severity: 'error', summary: t('products.mobile.addons.loadError'), life: 3000 })
  }
}

const backToList = () => {
  mode.value = 'list'
  editingGroup.value = null
}

// ── Load ──────────────────────────────────────────────────────────────
const loadGroups = async () => {
  loading.value = true
  try {
    const res = await getProductOptionGroups()
    const raw = res?.data
    groups.value = Array.isArray(raw) ? raw
      : Array.isArray(raw?.value) ? raw.value
      : Array.isArray(raw?.items) ? raw.items
      : []
  } catch {
    toast.add({ severity: 'error', summary: t('products.mobile.addons.loadError'), life: 3000 })
  } finally {
    loading.value = false
  }
}

onMounted(loadGroups)

// ── Submit ────────────────────────────────────────────────────────────
const handleSubmit = async () => {
  if (!formName.value.trim()) return
  if (mode.value === 'create') {
    createStep.value = '2'
    loadPickerProducts(true)
    return
  }
  submitting.value = true
  try {
    const payload = {
      name: formName.value.trim(),
      isRequired: formIsRequired.value,
      allowMultiple: formAllowMultiple.value,
      allowQuantity: formAllowQuantity.value,
      values: formValues.value,
    }
    await updateProductOptionGroup(editingGroup.value.id, payload)
    toast.add({ severity: 'success', summary: t('productOptionGroups.detail.updateSuccess'), life: 2500 })
    backToList()
    await loadGroups()
  } catch {
    toast.add({ severity: 'error', summary: t('products.mobile.addons.updateError'), life: 3000 })
  } finally {
    submitting.value = false
  }
}

// ── Product picker ────────────────────────────────────────────────────
const pickerProducts = ref([])
const pickerLoading = ref(false)
const pickerPage = ref(1)
const PICKER_PAGE_SIZE = 20
const pickerTotal = ref(0)
const pickerSearch = ref('')
const pickerCategoryFilter = ref(null)
const pickerCategories = ref([])
const pickerSelected = ref(new Set())
const pickerSearchTimer = ref(null)
const pickerHasMore = computed(() => pickerProducts.value.length < pickerTotal.value)

const loadPickerProducts = async (reset = true) => {
  if (reset) { pickerPage.value = 1; pickerProducts.value = [] }
  pickerLoading.value = true
  try {
    const res = await getProducts({
      page: pickerPage.value,
      pageSize: PICKER_PAGE_SIZE,
      searchTerm: pickerSearch.value.trim() || undefined,
      categoryId: pickerCategoryFilter.value ?? undefined,
    })
    const paged = res?.data ?? {}
    const items = paged.value?.map(p => ({
      id: p.id, name: p.name, price: p.price, imageUrl: p.imageUrl,
    })) ?? []
    pickerProducts.value = reset ? items : [...pickerProducts.value, ...items]
    pickerTotal.value = paged.pagedInfo?.totalRecords ?? 0
  } catch { /* non-critical */ }
  finally { pickerLoading.value = false }
}

const loadPickerCategories = async () => {
  try {
    const res = await getCategory()
    const raw = res?.data
    pickerCategories.value = Array.isArray(raw) ? raw
      : Array.isArray(raw?.value) ? raw.value
      : Array.isArray(raw?.items) ? raw.items
      : []
  } catch { /* non-critical */ }
}

const togglePickerProduct = (id) => {
  const s = new Set(pickerSelected.value)
  s.has(id) ? s.delete(id) : s.add(id)
  pickerSelected.value = s
}

const loadMorePicker = () => {
  if (!pickerHasMore.value || pickerLoading.value) return
  pickerPage.value++
  loadPickerProducts(false)
}

const backFromPicker = () => {
  createStep.value = '1'
}

const confirmPicker = async () => {
  submitting.value = true
  try {
    const payload = {
      name: formName.value.trim(),
      isRequired: formIsRequired.value,
      allowMultiple: formAllowMultiple.value,
      allowQuantity: formAllowQuantity.value,
      values: formValues.value,
      productIds: pickerSelected.value.size > 0 ? [...pickerSelected.value] : [],
    }
    await createProductOptionGroup(payload)

    toast.add({ severity: 'success', summary: t('productOptionGroups.create.submit'), life: 2500 })
    pickerSelected.value = new Set()
    pickerSearch.value = ''
    pickerCategoryFilter.value = null
    backToList()
    await loadGroups()
  } catch {
    toast.add({ severity: 'error', summary: t('products.mobile.addons.createError'), life: 3000 })
  } finally {
    submitting.value = false
  }
}

watch(pickerSearch, () => {
  clearTimeout(pickerSearchTimer.value)
  pickerSearchTimer.value = setTimeout(() => loadPickerProducts(true), 400)
})
watch(pickerCategoryFilter, () => loadPickerProducts(true))
onMounted(loadPickerCategories)
onBeforeUnmount(() => clearTimeout(pickerSearchTimer.value))

// ── Delete ────────────────────────────────────────────────────────────
const deleteTarget = ref(null)
const deleteDrawerVisible = ref(false)
const deleting = ref(false)

const handleDelete = (group) => {
  deleteTarget.value = group
  deleteDrawerVisible.value = true
}

const confirmDelete = async () => {
  if (!deleteTarget.value) return
  deleting.value = true
  try {
    await deleteProductOptionGroup(deleteTarget.value.id)
    deleteDrawerVisible.value = false
    if (mode.value === 'edit') backToList()
    await loadGroups()
  } catch {
    toast.add({ severity: 'error', summary: t('products.mobile.addons.deleteError'), life: 3000 })
  } finally {
    deleting.value = false
  }
}

// ── Toggle value stock ────────────────────────────────────────────────
const togglingValueId = ref(null)

const handleToggleStock = async (group, value) => {
  if (togglingValueId.value === value.id) return
  togglingValueId.value = value.id
  const prev = value.isInStock
  value.isInStock = !prev
  try {
    await toggleOptionValueStock(group.id, value.id)
  } catch {
    value.isInStock = prev
    toast.add({ severity: 'error', summary: t('products.mobile.addons.toggleStockError'), life: 3000 })
  } finally {
    togglingValueId.value = null
  }
}

// ── Edit tabs ────────────────────────────────────────────────────────
const editTab = ref('info') // 'info' | 'links'
const linkedProducts = ref([])
const unlinkingId = ref(null)

const handleUnlink = async (productId) => {
  if (!editingGroup.value || unlinkingId.value === productId) return
  unlinkingId.value = productId
  try {
    await unlinkGroupFromProduct(editingGroup.value.id, productId)
    linkedProducts.value = linkedProducts.value.filter(p => p.id !== productId)
    const cached = groups.value.find(g => g.id === editingGroup.value.id)
    if (cached) cached.linkedProductCount = Math.max(0, (cached.linkedProductCount ?? 1) - 1)
  } catch {
    toast.add({ severity: 'error', summary: t('products.mobile.addons.unlinkError'), life: 3000 })
  } finally {
    unlinkingId.value = null
  }
}

// Product picker for link tab (reuses existing picker state)
const openLinkPicker = () => {
  pickerSelected.value = new Set(linkedProducts.value.map(p => p.id))
  pickerSearch.value = ''
  pickerCategoryFilter.value = null
  loadPickerProducts(true)
  editTab.value = 'link-picker'
}

const confirmLinkPicker = async () => {
  submitting.value = true
  try {
    const currentIds = new Set(linkedProducts.value.map(p => p.id))
    const toAdd = [...pickerSelected.value].filter(id => !currentIds.has(id))
    const toRemove = [...currentIds].filter(id => !pickerSelected.value.has(id))

    if (toAdd.length > 0)
      await linkGroupToProducts(editingGroup.value.id, toAdd)

    for (const productId of toRemove)
      await unlinkGroupFromProduct(editingGroup.value.id, productId)

    // Reload group to get fresh linked products
    const res = await getProductOptionGroupById(editingGroup.value.id)
    linkedProducts.value = res?.data?.linkedProducts ?? []
    const cached = groups.value.find(g => g.id === editingGroup.value.id)
    if (cached) cached.linkedProductCount = linkedProducts.value.length

    editTab.value = 'links'
  } catch {
    toast.add({ severity: 'error', summary: t('products.mobile.addons.linkError'), life: 3000 })
  } finally {
    submitting.value = false
  }
}

// ── Helpers ───────────────────────────────────────────────────────────
const formatVnd = (value) =>
  new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(value ?? 0)
</script>

<template>
  <div class="tw:flex tw:flex-col tw:min-h-full tw:bg-slate-50 tw:dark:bg-neutral-950">

    <!-- ── Delete confirmation drawer ───────────────────────────────── -->
    <prime-drawer
      v-model:visible="deleteDrawerVisible"
      position="bottom"
      :style="{ height: 'auto' }"
      :pt="{ root: { class: 'tw:rounded-t-2xl' } }"
    >
      <template #header>
        <p class="tw:font-semibold tw:text-slate-800 tw:dark:text-white">{{ deleteTarget?.name }}</p>
      </template>
      <div class="tw:pb-4 tw:space-y-4">
        <p class="tw:text-sm tw:text-slate-500 tw:dark:text-slate-400">
          {{ t('products.mobile.addons.deleteConfirm', { name: deleteTarget?.name }) }}
        </p>
        <div class="tw:flex tw:gap-3">
          <prime-button severity="secondary" outlined fluid @click="deleteDrawerVisible = false">
            {{ t('common.cancel') }}
          </prime-button>
          <prime-button severity="danger" fluid :loading="deleting" @click="confirmDelete">
            {{ t('common.delete') }}
          </prime-button>
        </div>
      </div>
    </prime-drawer>

    <!-- ════════════════════════════════════════════════════════════════ -->
    <!-- LIST MODE                                                        -->
    <!-- ════════════════════════════════════════════════════════════════ -->
    <template v-if="mode === 'list'">

      <template v-if="loading">
        <div
          v-for="n in 3"
          :key="n"
          class="tw:mx-4 tw:mt-3 tw:rounded-xl tw:bg-white tw:dark:bg-neutral-900 tw:p-4 tw:space-y-3"
        >
          <prime-skeleton width="60%" height="1rem" />
          <prime-skeleton width="100%" height="0.75rem" />
          <prime-skeleton width="80%" height="0.75rem" />
        </div>
      </template>

      <div
        v-else-if="filteredGroups.length === 0"
        class="tw:flex tw:flex-col tw:items-center tw:justify-center tw:gap-5 tw:py-24 tw:px-8 tw:text-center"
      >
        <div class="tw:w-20 tw:h-20 tw:rounded-full tw:bg-emerald-50 tw:dark:bg-emerald-900/20 tw:flex tw:items-center tw:justify-center">
          <iconify icon="ph:plus-square-bold" class="tw:text-4xl tw:text-emerald-400" />
        </div>
        <div>
          <p class="tw:font-semibold tw:text-slate-700 tw:dark:text-white">{{ t('products.mobile.addons.empty') }}</p>
        </div>
        <prime-button severity="success" @click="openCreate">
          <iconify icon="ph:plus-bold" />
          <span>{{ t('products.mobile.addons.createGroup') }}</span>
        </prime-button>
      </div>

      <template v-else>
        <div class="tw:pb-32 tw:px-4 tw:pt-3 tw:space-y-2.5">
          <div
            v-for="group in filteredGroups"
            :key="group.id"
            class="tw:rounded-xl tw:overflow-hidden tw:shadow-sm tw:bg-white tw:dark:bg-neutral-900"
          >
            <!-- Main info -->
            <div class="tw:px-4 tw:py-3.5">
              <p class="tw:text-xl tw:font-semibold tw:text-slate-800 tw:dark:text-white tw:leading-tight">{{ group.name }}</p>
              <p
                v-if="group.valueNames && group.valueNames.length > 0"
                class="tw:text tw:text-slate-400 tw:dark:text-slate-500 tw:mt-0.5 tw:line-clamp-1"
              >{{ group.valueNames.join('; ') }}</p>
              <p
                v-else
                class="tw:text-xs tw:text-slate-300 tw:dark:text-slate-600 tw:mt-0.5 tw:italic"
              >{{ t('products.mobile.addons.noOptions') }}</p>
            </div>

            <!-- Bottom row: stats + action buttons -->
            <div class="tw:border-t tw:border-slate-50 tw:dark:border-white/5 tw:flex tw:items-stretch">
              <!-- Stats -->
              <div class="tw:flex-1 tw:flex tw:divide-x tw:divide-slate-100 tw:dark:divide-white/5">
                <div class="tw:flex-1 tw:flex tw:items-center tw:gap-1 tw:px-3 tw:py-2">
                  <span class="tw:text tw:text-blue-500 tw:dark:text-blue-400 tw:font-medium">
                    {{ t('products.mobile.addons.valueCount', { n: group.valueCount }) }}
                  </span>
                </div>
                <div class="tw:flex-1 tw:flex tw:items-center tw:gap-1 tw:px-3 tw:py-2">
                  <span class="tw:text tw:text-blue-500 tw:dark:text-blue-400 tw:font-medium">
                    {{ t('products.mobile.addons.linkedProductCount', { n: group.linkedProductCount ?? 0 }) }}
                  </span>
                </div>
              </div>

              <!-- Edit / Delete buttons -->
              <div class="tw:flex tw:divide-x tw:divide-slate-100 tw:dark:divide-white/5 tw:border-l tw:border-slate-100 tw:dark:border-white/5">
                <button
                  type="button"
                  class="tw:w-11 tw:flex tw:items-center tw:justify-center tw:bg-transparent tw:border-0 tw:cursor-pointer tw:text-blue-500 tw:dark:text-blue-400 tw:active:bg-blue-50 tw:dark:active:bg-blue-900/20"
                  @click="openEdit(group)"
                >
                  <iconify icon="ph:pencil-simple-bold" class="tw:text-base" />
                </button>
                <button
                  type="button"
                  class="tw:w-11 tw:flex tw:items-center tw:justify-center tw:bg-transparent tw:border-0 tw:cursor-pointer tw:text-red-400 tw:active:bg-red-50 tw:dark:active:bg-red-900/20"
                  @click="handleDelete(group)"
                >
                  <iconify icon="ph:trash-bold" class="tw:text-base" />
                </button>
              </div>
            </div>
          </div>
        </div>
      </template>

      <!-- Fixed bottom FAB -->
      <div v-if="!loading" class="tw:fixed tw:bottom-6 tw:left-4 tw:right-4 tw:z-20">
        <prime-button rounded class="tw:w-full tw:py-3.5!" @click="openCreate">
          <iconify icon="ph:plus-bold" class="tw:text-base" />
          <span class="tw:font-semibold tw:text-xl">{{ t('products.mobile.addons.createGroup') }}</span>
        </prime-button>
      </div>

    </template>

    <!-- ════════════════════════════════════════════════════════════════ -->
    <!-- CREATE MODE — 2-step stepper                                     -->
    <!-- ════════════════════════════════════════════════════════════════ -->
    <div
      v-if="mode === 'create'"
      class="tw:fixed tw:inset-0 tw:z-30 tw:flex tw:flex-col tw:bg-slate-50 tw:dark:bg-neutral-950"
    >

      <!-- Top bar -->
      <div class="tw:shrink-0 tw:flex tw:items-center tw:gap-3 tw:px-4 tw:py-3 tw:bg-white tw:dark:bg-neutral-900 tw:border-b tw:border-slate-100 tw:dark:border-white/10">
        <button
          type="button"
          class="tw:w-8 tw:h-8 tw:flex tw:items-center tw:justify-center tw:rounded-lg tw:bg-transparent tw:border-0 tw:cursor-pointer tw:text-muted tw:active:bg-black/5 tw:dark:active:bg-white/5"
          @click="createStep === '2' ? backFromPicker() : backToList()"
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
        <prime-step-list>
          <prime-step value="1">{{ t('products.mobile.addons.step1') }}</prime-step>
          <prime-step value="2">{{ t('products.mobile.addons.step2') }}</prime-step>
        </prime-step-list>

        <prime-step-panels
          :pt="{ root: { class: 'tw:flex-1 tw:flex tw:flex-col tw:overflow-hidden tw:min-h-0' } }"
        >

          <!-- Step 1: Group info -->
          <prime-step-panel
            value="1"
            :pt="{
              root: { class: 'tw:flex tw:flex-col tw:h-full tw:overflow-hidden' },
              content: { class: 'tw:flex! tw:flex-col! tw:h-full! tw:overflow-hidden! tw:p-0!' }
            }"
          >
            <div class="tw:flex tw:flex-col tw:h-full tw:overflow-hidden tw:bg-transparent">
              <div class="tw:flex-1 tw:overflow-y-auto tw:pb-4 tw:bg-transparent">

                <!-- Name field -->
                <div class="tw:bg-white tw:dark:bg-neutral-900 tw:mt-3 tw:mx-4 tw:rounded-xl tw:p-4 tw:space-y-1">
                  <div class="tw:flex tw:items-center tw:justify-between">
                    <label for="addon-name" class="tw:text-sm tw:font-medium tw:text-slate-700 tw:dark:text-slate-200">
                      {{ t('products.mobile.addons.nameLabel') }}<span class="tw:text-red-500 tw:ml-0.5">*</span>
                    </label>
                    <span class="tw:text-xs tw:text-slate-400">{{ nameCount }}/{{ NAME_MAX }}</span>
                  </div>
                  <prime-input-text
                    id="addon-name"
                    v-model="formName"
                    :maxlength="NAME_MAX"
                    :placeholder="t('products.mobile.addons.namePlaceholder')"
                    class="app-input tw:w-full"
                  />
                </div>

                <!-- Options section -->
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
                    class="tw:w-full tw:flex tw:items-center tw:gap-2 tw:px-4 tw:py-3.5 tw:bg-transparent tw:border-0 tw:cursor-pointer tw:text-emerald-600 tw:dark:text-emerald-400 tw:text-sm tw:font-medium tw:active:bg-slate-50 tw:dark:active:bg-white/5"
                    @click="openAddOption()"
                  >
                    <iconify icon="ph:plus-circle-bold" class="tw:text-lg" />
                    {{ t('products.mobile.addons.addOption') }}
                  </button>
                </div>

                <!-- Settings -->
                <div class="tw:bg-white tw:dark:bg-neutral-900 tw:mt-3 tw:mx-4 tw:rounded-xl tw:p-4 tw:space-y-3">
                  <p class="tw:text-xs tw:font-semibold tw:uppercase tw:tracking-widest tw:text-slate-400 tw:dark:text-slate-500">
                    {{ t('products.mobile.addons.settings') }}
                  </p>
                  <label class="tw:flex tw:items-start tw:gap-3 tw:cursor-pointer">
                    <prime-checkbox v-model="formIsRequired" :binary="true" class="tw:mt-0.5" />
                    <div>
                      <p class="tw:text-sm tw:font-medium tw:text-slate-700 tw:dark:text-slate-200">{{ t('products.mobile.addons.isRequired') }}</p>
                      <p class="tw:text-xs tw:text-slate-400 tw:mt-0.5">{{ t('products.mobile.addons.isRequiredHint') }}</p>
                    </div>
                  </label>
                  <label class="tw:flex tw:items-start tw:gap-3 tw:cursor-pointer">
                    <prime-checkbox v-model="formAllowMultiple" :binary="true" class="tw:mt-0.5" />
                    <div>
                      <p class="tw:text-sm tw:font-medium tw:text-slate-700 tw:dark:text-slate-200">{{ t('products.mobile.addons.allowMultiple') }}</p>
                      <p class="tw:text-xs tw:text-slate-400 tw:mt-0.5">{{ t('products.mobile.addons.allowMultipleHint') }}</p>
                    </div>
                  </label>
                  <label class="tw:flex tw:items-start tw:gap-3 tw:cursor-pointer">
                    <prime-checkbox v-model="formAllowQuantity" :binary="true" class="tw:mt-0.5" />
                    <div>
                      <p class="tw:text-sm tw:font-medium tw:text-slate-700 tw:dark:text-slate-200">{{ t('products.mobile.addons.allowQuantity') }}</p>
                      <p class="tw:text-xs tw:text-slate-400 tw:mt-0.5">{{ t('products.mobile.addons.allowQuantityHint') }}</p>
                    </div>
                  </label>
                </div>

              </div>

              <!-- Bottom bar -->
              <div class="tw:shrink-0 tw:bg-white tw:dark:bg-neutral-900 tw:border-t tw:border-slate-100 tw:dark:border-white/10 tw:px-4 tw:py-3">
                <prime-button
                  severity="success"
                  fluid
                  :disabled="!formName.trim()"
                  @click="handleSubmit"
                >
                  {{ t('products.mobile.addons.submit') }}
                </prime-button>
              </div>
            </div>
          </prime-step-panel>

          <!-- Step 2: Product picker -->
          <prime-step-panel
            value="2"
            :pt="{
              root: { class: 'tw:flex tw:flex-col tw:h-full tw:overflow-hidden' },
              content: { class: 'tw:flex! tw:flex-col! tw:h-full! tw:overflow-hidden! tw:p-0!' }
            }"
          >
            <div class="tw:flex tw:flex-col tw:h-full tw:overflow-hidden">

              <!-- Search + categories -->
              <div class="tw:shrink-0 tw:bg-white tw:dark:bg-neutral-900 tw:border-b tw:border-slate-100 tw:dark:border-white/10 tw:px-4 tw:pt-2 tw:pb-2 tw:space-y-2">
                <div class="tw:relative">
                  <iconify icon="ph:magnifying-glass-bold" class="tw:absolute tw:left-2.5 tw:top-1/2 tw:-translate-y-1/2 tw:text-muted tw:text-sm tw:pointer-events-none" />
                  <prime-input-text
                    v-model="pickerSearch"
                    :placeholder="t('products.mobile.searchProductsPlaceholder')"
                    class="app-input tw:w-full tw:pl-8! tw:text-sm!"
                  />
                </div>
                <div class="tw:flex tw:gap-2 tw:overflow-x-auto" style="scrollbar-width: none;">
                  <prime-button
                    size="small" rounded outlined
                    :severity="pickerCategoryFilter === null ? 'info' : 'secondary'"
                    class="tw:shrink-0 tw:whitespace-nowrap"
                    @click="pickerCategoryFilter = null"
                  >
                    {{ t('products.mobile.addons.allCategories') }}
                  </prime-button>
                  <prime-button
                    v-for="cat in pickerCategories"
                    :key="cat.id"
                    size="small" rounded outlined
                    :severity="pickerCategoryFilter === cat.id ? 'success' : 'secondary'"
                    class="tw:shrink-0 tw:whitespace-nowrap"
                    @click="pickerCategoryFilter = cat.id"
                  >
                    {{ cat.name }}
                  </prime-button>
                </div>
              </div>

              <!-- Product list -->
              <div class="tw:flex-1 tw:overflow-y-auto">
                <template v-if="pickerLoading && pickerProducts.length === 0">
                  <div
                    v-for="n in 5" :key="n"
                    class="tw:flex tw:items-center tw:gap-3 tw:px-4 tw:py-3 tw:bg-white tw:dark:bg-neutral-900 tw:border-b tw:border-slate-100 tw:dark:border-white/5"
                  >
                    <prime-skeleton width="1.25rem" height="1.25rem" border-radius="4px" class="tw:shrink-0" />
                    <prime-skeleton width="3rem" height="3rem" border-radius="10px" class="tw:shrink-0" />
                    <div class="tw:flex-1 tw:space-y-2">
                      <prime-skeleton width="60%" height="0.85rem" />
                      <prime-skeleton width="30%" height="0.75rem" />
                    </div>
                  </div>
                </template>

                <div
                  v-for="product in pickerProducts"
                  :key="product.id"
                  class="tw:flex tw:items-center tw:gap-3 tw:px-4 tw:py-3 tw:bg-white tw:dark:bg-neutral-900 tw:border-b tw:border-slate-100 tw:dark:border-white/5 tw:cursor-pointer tw:active:bg-slate-50 tw:dark:active:bg-white/5"
                  @click="togglePickerProduct(product.id)"
                >
                  <div
                    class="tw:shrink-0 tw:w-5 tw:h-5 tw:rounded tw:border-2 tw:flex tw:items-center tw:justify-center tw:transition-colors"
                    :class="pickerSelected.has(product.id)
                      ? 'tw:bg-emerald-500 tw:border-emerald-500'
                      : 'tw:border-slate-300 tw:dark:border-white/30'"
                  >
                    <iconify v-if="pickerSelected.has(product.id)" icon="ph:check-bold" class="tw:text-white tw:text-xs" />
                  </div>
                  <div class="tw:shrink-0 tw:w-12 tw:h-12 tw:rounded-xl tw:overflow-hidden tw:bg-slate-100 tw:dark:bg-white/10">
                    <img v-if="product.imageUrl" :src="product.imageUrl" :alt="product.name" class="tw:w-full tw:h-full tw:object-cover" />
                    <div v-else class="tw:w-full tw:h-full tw:flex tw:items-center tw:justify-center">
                      <iconify icon="ph:coffee-bold" class="tw:text-lg tw:text-slate-400" />
                    </div>
                  </div>
                  <div class="tw:flex-1 tw:min-w-0">
                    <p class="tw:text-sm tw:font-semibold tw:text-slate-800 tw:dark:text-white tw:leading-tight tw:line-clamp-1">{{ product.name }}</p>
                    <p class="tw:text-xs tw:text-amber-500 tw:font-semibold tw:mt-0.5">{{ formatVnd(product.price) }}</p>
                  </div>
                </div>

                <div v-if="pickerHasMore" class="tw:flex tw:justify-center tw:py-5 tw:bg-white tw:dark:bg-neutral-900">
                  <prime-button severity="secondary" text size="small" :loading="pickerLoading" @click="loadMorePicker">
                    {{ t('products.mobile.loadMore') }}
                  </prime-button>
                </div>

                <div v-if="!pickerLoading && pickerProducts.length === 0" class="tw:flex tw:flex-col tw:items-center tw:justify-center tw:py-20 tw:gap-3">
                  <iconify icon="ph:coffee-bold" class="tw:text-4xl tw:text-slate-300 tw:dark:text-white/20" />
                  <p class="tw:text-sm tw:text-slate-400">{{ t('products.mobile.empty') }}</p>
                </div>
              </div>

              <!-- Action bar -->
              <div class="tw:shrink-0 tw:bg-white tw:dark:bg-neutral-900 tw:border-t tw:border-slate-100 tw:dark:border-white/10 tw:px-4 tw:py-3 tw:flex tw:gap-3">
                <prime-button severity="secondary" outlined class="tw:flex-1" @click="backFromPicker">
                  {{ t('products.mobile.addons.pickProductsBack') }}
                </prime-button>
                <prime-button severity="success" class="tw:flex-1" :loading="submitting" @click="confirmPicker">
                  {{ t('products.mobile.addons.pickProductsSubmit') }}
                  <span v-if="pickerSelected.size > 0" class="tw:ml-1 tw:text-emerald-100">({{ pickerSelected.size }})</span>
                </prime-button>
              </div>

            </div>
          </prime-step-panel>

        </prime-step-panels>
      </prime-stepper>
    </div>

    <!-- ════════════════════════════════════════════════════════════════ -->
    <!-- EDIT MODE — 2 tabs: Thông tin / Liên kết                        -->
    <!-- ════════════════════════════════════════════════════════════════ -->
    <div
      v-if="mode === 'edit'"
      class="tw:fixed tw:inset-0 tw:z-30 tw:flex tw:flex-col tw:bg-slate-50 tw:dark:bg-neutral-950"
    >

      <!-- Top bar -->
      <div class="tw:shrink-0 tw:flex tw:items-center tw:gap-3 tw:px-4 tw:py-3 tw:bg-white tw:dark:bg-neutral-900 tw:border-b tw:border-slate-100 tw:dark:border-white/10">
        <button
          type="button"
          class="tw:w-8 tw:h-8 tw:flex tw:items-center tw:justify-center tw:rounded-lg tw:bg-transparent tw:border-0 tw:cursor-pointer tw:text-muted tw:active:bg-black/5 tw:dark:active:bg-white/5"
          @click="editTab === 'link-picker' ? editTab = 'links' : backToList()"
        >
          <iconify icon="ph:arrow-left-bold" class="tw:text-lg" />
        </button>
        <h2 class="tw:font-semibold tw:text-slate-800 tw:dark:text-white tw:text-base tw:flex-1">
          {{ editTab === 'link-picker' ? t('products.mobile.addons.pickProducts') : t('products.mobile.addons.editTitle') }}
        </h2>
      </div>

      <!-- Tab bar (hidden when in link-picker sub-view) -->
      <div
        v-if="editTab !== 'link-picker'"
        class="tw:shrink-0 tw:flex tw:bg-white tw:dark:bg-neutral-900 tw:border-b tw:border-slate-100 tw:dark:border-white/10"
      >
        <button
          type="button"
          class="tw:flex-1 tw:py-2.5 tw:text-lg tw:font-medium tw:border-0 tw:bg-transparent tw:cursor-pointer tw:transition-colors"
          :class="editTab === 'info'
            ? 'tw:text-emerald-600 tw:dark:text-emerald-400 tw:border-b-2 tw:border-emerald-500'
            : 'tw:text-slate-500 tw:dark:text-slate-400'"
          @click="editTab = 'info'"
        >
          {{ t('products.mobile.addons.tabInfo') }}
        </button>
        <button
          type="button"
          class="tw:flex-1 tw:py-2.5 tw:text-lg tw:font-medium tw:border-0 tw:bg-transparent tw:cursor-pointer tw:transition-colors"
          :class="editTab === 'links'
            ? 'tw:text-emerald-600 tw:dark:text-emerald-400 tw:border-b-2 tw:border-emerald-500'
            : 'tw:text-slate-500 tw:dark:text-slate-400'"
          @click="editTab = 'links'"
        >
          {{ t('products.mobile.addons.tabLinks') }}
          <span class="tw:ml-1">({{ linkedProducts.length }})</span>
        </button>
      </div>

      <!-- ── TAB 1: Thông tin ── -->
      <template v-if="editTab === 'info'">
        <div class="tw:flex-1 tw:overflow-y-auto tw:pb-4">

          <!-- Name field -->
          <div class="tw:bg-white tw:dark:bg-neutral-900 tw:mt-3 tw:mx-4 tw:rounded-xl tw:p-4 tw:space-y-1">
            <div class="tw:flex tw:items-center tw:justify-between">
              <label for="edit-addon-name" class="tw:text-sm tw:font-medium tw:text-slate-700 tw:dark:text-slate-200">
                {{ t('products.mobile.addons.nameLabel') }}<span class="tw:text-red-500 tw:ml-0.5">*</span>
              </label>
              <span class="tw:text-xs tw:text-slate-400">{{ nameCount }}/{{ NAME_MAX }}</span>
            </div>
            <prime-input-text
              id="edit-addon-name"
              v-model="formName"
              :maxlength="NAME_MAX"
              :placeholder="t('products.mobile.addons.namePlaceholder')"
              class="app-input tw:w-full"
            />
          </div>

          <!-- Options section -->
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
              class="tw:w-full tw:flex tw:items-center tw:gap-2 tw:px-4 tw:py-3.5 tw:bg-transparent tw:border-0 tw:cursor-pointer tw:text-emerald-600 tw:dark:text-emerald-400 tw:text-sm tw:font-medium tw:active:bg-slate-50 tw:dark:active:bg-white/5"
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

        <!-- Bottom bar: Xóa + Cập nhật -->
        <div class="tw:shrink-0 tw:bg-white tw:dark:bg-neutral-900 tw:border-t tw:border-slate-100 tw:dark:border-white/10 tw:px-4 tw:py-3 tw:flex tw:gap-3">
          <prime-button
            severity="danger"
            outlined
            class="tw:flex-1"
            @click="handleDelete(editingGroup)"
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
      </template>

      <!-- ── TAB 2: Liên kết ── -->
      <template v-else-if="editTab === 'links'">
        <div class="tw:flex-1 tw:overflow-y-auto">

          <!-- Add product button -->
          <div class="tw:px-4 tw:py-3">
            <button
              type="button"
              class="tw:w-full tw:flex tw:items-center tw:justify-center tw:gap-2 tw:py-3 tw:rounded-xl tw:border tw:border-dashed tw:border-emerald-400 tw:dark:border-emerald-600 tw:bg-transparent tw:cursor-pointer tw:text-emerald-600 tw:dark:text-emerald-400 tw:text-sm tw:font-medium tw:active:bg-emerald-50 tw:dark:active:bg-emerald-900/20"
              @click="openLinkPicker"
            >
              <iconify icon="ph:plus-bold" class="tw:text-base" />
              {{ t('products.mobile.addons.addLinkedProduct') }}
            </button>
          </div>

          <!-- Linked products list -->
          <div v-if="linkedProducts.length > 0" class="tw:mx-4 tw:rounded-xl tw:overflow-hidden tw:bg-white tw:dark:bg-neutral-900">
            <div
              v-for="product in linkedProducts"
              :key="product.id"
              class="tw:flex tw:items-center tw:gap-3 tw:px-4 tw:py-3 tw:border-b tw:border-slate-50 tw:dark:border-white/5 last:tw:border-0"
            >
              <div class="tw:shrink-0 tw:w-12 tw:h-12 tw:rounded-xl tw:overflow-hidden tw:bg-slate-100 tw:dark:bg-white/10">
                <img v-if="product.imageUrl" :src="product.imageUrl" :alt="product.name" class="tw:w-full tw:h-full tw:object-cover" />
                <div v-else class="tw:w-full tw:h-full tw:flex tw:items-center tw:justify-center">
                  <iconify icon="ph:coffee-bold" class="tw:text-lg tw:text-slate-400" />
                </div>
              </div>
              <div class="tw:flex-1 tw:min-w-0">
                <p class="tw:text-sm tw:font-semibold tw:text-slate-800 tw:dark:text-white tw:leading-tight tw:line-clamp-1">{{ product.name }}</p>
                <p class="tw:text-xs tw:text-amber-500 tw:font-semibold tw:mt-0.5">{{ formatVnd(product.price) }}</p>
              </div>
              <button
                type="button"
                class="tw:shrink-0 tw:px-3 tw:py-1.5 tw:rounded-lg tw:bg-transparent tw:border tw:border-red-200 tw:dark:border-red-800 tw:text-red-500 tw:dark:text-red-400 tw:text-xs tw:font-medium tw:cursor-pointer tw:active:bg-red-50 tw:dark:active:bg-red-900/20 tw:flex tw:items-center tw:gap-1"
                :disabled="unlinkingId === product.id"
                @click="handleUnlink(product.id)"
              >
                <prime-progress-spinner v-if="unlinkingId === product.id" style="width:12px;height:12px" />
                <span v-else>{{ t('products.mobile.addons.unlink') }}</span>
              </button>
            </div>
          </div>

          <div v-else class="tw:flex tw:flex-col tw:items-center tw:justify-center tw:py-16 tw:gap-3">
            <iconify icon="ph:link-simple-break-bold" class="tw:text-4xl tw:text-slate-300 tw:dark:text-white/20" />
            <p class="tw:text-sm tw:text-slate-400">{{ t('products.mobile.addons.noLinkedProducts') }}</p>
          </div>

        </div>
      </template>

      <!-- ── TAB 2 sub-view: Product picker ── -->
      <template v-else-if="editTab === 'link-picker'">
        <div class="tw:flex tw:flex-col tw:flex-1 tw:overflow-hidden">

          <!-- Search + categories -->
          <div class="tw:shrink-0 tw:bg-white tw:dark:bg-neutral-900 tw:border-b tw:border-slate-100 tw:dark:border-white/10 tw:px-4 tw:pt-2 tw:pb-2 tw:space-y-2">
            <div class="tw:relative">
              <iconify icon="ph:magnifying-glass-bold" class="tw:absolute tw:left-2.5 tw:top-1/2 tw:-translate-y-1/2 tw:text-muted tw:text-sm tw:pointer-events-none" />
              <prime-input-text
                v-model="pickerSearch"
                :placeholder="t('products.mobile.searchProductsPlaceholder')"
                class="app-input tw:w-full tw:pl-8! tw:text-sm!"
              />
            </div>
            <div class="tw:flex tw:gap-2 tw:overflow-x-auto" style="scrollbar-width: none;">
              <prime-button size="small" rounded outlined :severity="pickerCategoryFilter === null ? 'info' : 'secondary'"
                class="tw:shrink-0 tw:whitespace-nowrap" @click="pickerCategoryFilter = null">
                {{ t('products.mobile.addons.allCategories') }}
              </prime-button>
              <prime-button
                v-for="cat in pickerCategories" :key="cat.id"
                size="small" rounded outlined
                :severity="pickerCategoryFilter === cat.id ? 'success' : 'secondary'"
                class="tw:shrink-0 tw:whitespace-nowrap"
                @click="pickerCategoryFilter = cat.id"
              >
                {{ cat.name }}
              </prime-button>
            </div>
          </div>

          <!-- Product list -->
          <div class="tw:flex-1 tw:overflow-y-auto">
            <template v-if="pickerLoading && pickerProducts.length === 0">
              <div v-for="n in 5" :key="n"
                class="tw:flex tw:items-center tw:gap-3 tw:px-4 tw:py-3 tw:bg-white tw:dark:bg-neutral-900 tw:border-b tw:border-slate-100 tw:dark:border-white/5">
                <prime-skeleton width="1.25rem" height="1.25rem" border-radius="4px" class="tw:shrink-0" />
                <prime-skeleton width="3rem" height="3rem" border-radius="10px" class="tw:shrink-0" />
                <div class="tw:flex-1 tw:space-y-2">
                  <prime-skeleton width="60%" height="0.85rem" />
                  <prime-skeleton width="30%" height="0.75rem" />
                </div>
              </div>
            </template>

            <div
              v-for="product in pickerProducts" :key="product.id"
              class="tw:flex tw:items-center tw:gap-3 tw:px-4 tw:py-3 tw:bg-white tw:dark:bg-neutral-900 tw:border-b tw:border-slate-100 tw:dark:border-white/5 tw:cursor-pointer tw:active:bg-slate-50 tw:dark:active:bg-white/5"
              @click="togglePickerProduct(product.id)"
            >
              <div
                class="tw:shrink-0 tw:w-5 tw:h-5 tw:rounded tw:border-2 tw:flex tw:items-center tw:justify-center tw:transition-colors"
                :class="pickerSelected.has(product.id) ? 'tw:bg-emerald-500 tw:border-emerald-500' : 'tw:border-slate-300 tw:dark:border-white/30'"
              >
                <iconify v-if="pickerSelected.has(product.id)" icon="ph:check-bold" class="tw:text-white tw:text-xs" />
              </div>
              <div class="tw:shrink-0 tw:w-12 tw:h-12 tw:rounded-xl tw:overflow-hidden tw:bg-slate-100 tw:dark:bg-white/10">
                <img v-if="product.imageUrl" :src="product.imageUrl" :alt="product.name" class="tw:w-full tw:h-full tw:object-cover" />
                <div v-else class="tw:w-full tw:h-full tw:flex tw:items-center tw:justify-center">
                  <iconify icon="ph:coffee-bold" class="tw:text-lg tw:text-slate-400" />
                </div>
              </div>
              <div class="tw:flex-1 tw:min-w-0">
                <p class="tw:text-sm tw:font-semibold tw:text-slate-800 tw:dark:text-white tw:leading-tight tw:line-clamp-1">{{ product.name }}</p>
                <p class="tw:text-xs tw:text-amber-500 tw:font-semibold tw:mt-0.5">{{ formatVnd(product.price) }}</p>
              </div>
            </div>

            <div v-if="pickerHasMore" class="tw:flex tw:justify-center tw:py-5 tw:bg-white tw:dark:bg-neutral-900">
              <prime-button severity="secondary" text size="small" :loading="pickerLoading" @click="loadMorePicker">
                {{ t('products.mobile.loadMore') }}
              </prime-button>
            </div>

            <div v-if="!pickerLoading && pickerProducts.length === 0" class="tw:flex tw:flex-col tw:items-center tw:justify-center tw:py-20 tw:gap-3">
              <iconify icon="ph:coffee-bold" class="tw:text-4xl tw:text-slate-300 tw:dark:text-white/20" />
              <p class="tw:text-sm tw:text-slate-400">{{ t('products.mobile.empty') }}</p>
            </div>
          </div>

          <!-- Action bar -->
          <div class="tw:shrink-0 tw:bg-white tw:dark:bg-neutral-900 tw:border-t tw:border-slate-100 tw:dark:border-white/10 tw:px-4 tw:py-3 tw:flex tw:gap-3">
            <prime-button severity="secondary" outlined class="tw:flex-1" @click="editTab = 'links'">
              {{ t('common.cancel') }}
            </prime-button>
            <prime-button severity="success" class="tw:flex-1" :loading="submitting" @click="confirmLinkPicker">
              {{ t('products.mobile.addons.pickProductsSubmit') }}
              <span v-if="pickerSelected.size > 0" class="tw:ml-1 tw:text-emerald-100">({{ pickerSelected.size }})</span>
            </prime-button>
          </div>

        </div>
      </template>

    </div>

    <!-- ════════════════════════════════════════════════════════════════ -->
    <!-- ADD OPTION BOTTOM SHEET                                         -->
    <!-- ════════════════════════════════════════════════════════════════ -->
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

  </div>
</template>
