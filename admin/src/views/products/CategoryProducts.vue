<script setup>
import { ref, computed, watch, onMounted, onBeforeUnmount } from 'vue'
import { getCategoryById, deleteCategory, updateCategory } from '@/services/category.service'
import { uploadImage } from '@/services/upload.service'
import { getProducts, toggleProductActive } from '@/services/product.service'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const toast = useToast()
const { can } = usePermission()

const categoryId = computed(() => parseInt(route.params.id))

// ── Category ──────────────────────────────────────────────────────────
const category = ref(null)
const loadCategory = async () => {
  try {
    const res = await getCategoryById(categoryId.value)
    category.value = res?.data
  } catch {
    router.back()
  }
}

// ── Products ──────────────────────────────────────────────────────────
const loading = ref(false)
const errorMessage = ref('')
const products = ref([])
const page = ref(1)
const PAGE_SIZE = 20
const totalRecords = ref(0)
const hasMore = computed(() => products.value.length < totalRecords.value)

const search = ref('')
const showSearch = ref(false)
const searchTimer = ref(null)

const loadProducts = async (reset = true) => {
  if (reset) { page.value = 1; products.value = [] }
  loading.value = true
  try {
    const res = await getProducts({
      page: page.value,
      pageSize: PAGE_SIZE,
      searchTerm: search.value.trim() || undefined,
      categoryId: categoryId.value,
    })
    const paged = res?.data ?? {}
    const items = (paged.value ?? []).map(p => ({
      id: p.id,
      name: p.name,
      price: p.price,
      imageUrl: p.imageUrl,
      description: p.description,
      isActive: p.isActive,
    }))
    if (reset) products.value = items
    else products.value = [...products.value, ...items]
    totalRecords.value = paged.pagedInfo?.totalRecords ?? 0
  } catch (err) {
    errorMessage.value = err?.response?.data?.message || t('products.list.error')
  } finally {
    loading.value = false
  }
}

const loadMore = () => {
  if (!hasMore.value || loading.value) return
  page.value++
  loadProducts(false)
}

watch(search, () => {
  clearTimeout(searchTimer.value)
  searchTimer.value = setTimeout(() => loadProducts(true), 400)
})

onBeforeUnmount(() => clearTimeout(searchTimer.value))

// ── Helpers ───────────────────────────────────────────────────────────
const formatVnd = (value) =>
  new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(value ?? 0)

// ── Select ────────────────────────────────────────────────────────────
const selectedIds = ref(new Set())
const toggleSelect = (id) => {
  const s = new Set(selectedIds.value)
  if (s.has(id)) s.delete(id)
  else s.add(id)
  selectedIds.value = s
}

// ── Confirm dialog ────────────────────────────────────────────────────
const confirmVisible = ref(false)
const confirmTitle = ref('')
const confirmMessage = ref('')
const confirmLoading = ref(false)
const confirmCallback = ref(null)

const showConfirm = (title, message, callback) => {
  confirmTitle.value = title
  confirmMessage.value = message
  confirmCallback.value = callback
  confirmVisible.value = true
}

const execConfirm = async () => {
  confirmLoading.value = true
  try {
    await confirmCallback.value()
    confirmVisible.value = false
  } catch (err) {
    toast.add({
      severity: 'error',
      summary: err?.response?.data?.message || t('products.list.error'),
      life: 3000,
    })
  } finally {
    confirmLoading.value = false
  }
}

// ── Delete category ───────────────────────────────────────────────────
const handleDeleteCategory = () => {
  showConfirm(
    t('products.mobile.deleteCategoryTitle'),
    t('products.mobile.deleteCategoryMessage'),
    async () => {
      await deleteCategory(categoryId.value)
      router.back()
    },
  )
}

// ── Deactivate product ────────────────────────────────────────────────
const handleDeleteProduct = () => {
  if (!selectedIds.value.size) return
  showConfirm(
    t('products.mobile.deleteProductTitle'),
    t('products.mobile.deleteProductMessage'),
    async () => {
      await Promise.all([...selectedIds.value].map(id => toggleProductActive(id)))
      selectedIds.value = new Set()
      await loadProducts(true)
      toast.add({ severity: 'success', summary: t('products.detail.updateSuccess'), life: 2000 })
    },
  )
}

// ── Edit drawer ───────────────────────────────────────────────────────
const editDrawerVisible = ref(false)
const editName = ref('')
const editDescription = ref('')
const editImageUrl = ref('')
const editLoading = ref(false)
const editImageUploading = ref(false)
const editFileInputRef = ref(null)

const openEditDrawer = () => {
  editName.value = category.value?.name ?? ''
  editDescription.value = category.value?.description ?? ''
  editImageUrl.value = category.value?.imageUrl ?? ''
  editDrawerVisible.value = true
}

const handleEditImageSelect = async (e) => {
  const file = e.target.files?.[0]
  if (!file) return
  editImageUploading.value = true
  try {
    const res = await uploadImage(file)
    editImageUrl.value = res?.data?.url ?? ''
  } catch {
    // ignore
  } finally {
    editImageUploading.value = false
    e.target.value = ''
  }
}

const submitEdit = async () => {
  if (!editName.value.trim() || editLoading.value) return
  editLoading.value = true
  try {
    await updateCategory(categoryId.value, {
      name: editName.value.trim(),
      description: editDescription.value.trim() || null,
      imageUrl: editImageUrl.value || null,
    })
    editDrawerVisible.value = false
    await loadCategory()
  } catch (err) {
    toast.add({
      severity: 'error',
      summary: err?.response?.data?.message || t('categories.detail.error.updateFailed'),
      life: 3000,
    })
  } finally {
    editLoading.value = false
  }
}

onMounted(() => Promise.all([loadCategory(), loadProducts(true)]))
</script>

<template>
  <section class="tw:flex tw:flex-col tw:-mx-4 tw:-mt-5 tw:min-h-[calc(100dvh-3.5rem)] tw:bg-slate-50 tw:dark:bg-neutral-950">

    <!-- ── Sticky header ────────────────────────────────────────── -->
    <div class="tw:sticky tw:top-0 tw:z-10 tw:bg-white tw:dark:bg-neutral-900 tw:border-b tw:border-slate-200 tw:dark:border-white/10">

      <!-- Row 1: back + category info + actions -->
      <div class="tw:flex tw:items-center tw:gap-2 tw:px-3 tw:pt-3 tw:pb-2.5">

        <!-- Back -->
        <button
          type="button"
          class="tw:shrink-0 tw:w-8 tw:h-8 tw:flex tw:items-center tw:justify-center tw:rounded-lg tw:bg-transparent tw:border-0 tw:cursor-pointer tw:text-slate-600 tw:dark:text-white tw:active:bg-black/5"
          @click="router.push({ name: 'products', query: { tab: 'categories' } })"
        >
          <iconify icon="ph:arrow-left-bold" class="tw:text-lg" />
        </button>

        <!-- Category info -->
        <div class="tw:flex tw:items-center tw:gap-2.5 tw:flex-1 tw:min-w-0">
          <div class="tw:w-9 tw:h-9 tw:rounded-xl tw:overflow-hidden tw:bg-slate-100 tw:dark:bg-white/10 tw:shrink-0">
            <img
              v-if="category?.imageUrl"
              :src="category.imageUrl"
              :alt="category.name"
              class="tw:w-full tw:h-full tw:object-cover"
            />
            <div v-else class="tw:w-full tw:h-full tw:flex tw:items-center tw:justify-center">
              <iconify icon="ph:tag-bold" class="tw:text-base tw:text-slate-400" />
            </div>
          </div>
          <div class="tw:min-w-0">
            <p class="tw:font-semibold tw:text-sm tw:truncate tw:leading-tight tw:text-slate-800 tw:dark:text-white">
              {{ category?.name ?? '…' }}
            </p>
            <p class="tw:text-xs tw:text-slate-500 tw:dark:text-slate-400">
              {{ totalRecords }} {{ t('products.mobile.productCount') }}
            </p>
          </div>
        </div>

        <!-- Search toggle -->
        <button
          type="button"
          class="tw:shrink-0 tw:w-8 tw:h-8 tw:flex tw:items-center tw:justify-center tw:rounded-lg tw:bg-transparent tw:border-0 tw:cursor-pointer tw:active:bg-black/5 tw:dark:active:bg-white/5"
          :class="showSearch ? 'tw:text-emerald-500' : 'tw:text-slate-500 tw:dark:text-slate-400'"
          @click="showSearch = !showSearch; if (!showSearch) search = ''"
        >
          <iconify icon="ph:magnifying-glass-bold" class="tw:text-lg" />
        </button>

        <!-- Edit -->
        <button
          type="button"
          class="tw:shrink-0 tw:w-8 tw:h-8 tw:flex tw:items-center tw:justify-center tw:rounded-lg tw:bg-transparent tw:border-0 tw:cursor-pointer tw:text-slate-500 tw:dark:text-slate-400 tw:active:bg-black/5 tw:dark:active:bg-white/5"
          @click="openEditDrawer"
        >
          <iconify icon="ph:pencil-bold" class="tw:text-lg" />
        </button>

        <!-- Delete category -->
        <button
          v-if="can('product.delete')"
          type="button"
          class="tw:shrink-0 tw:w-8 tw:h-8 tw:flex tw:items-center tw:justify-center tw:rounded-lg tw:bg-transparent tw:border-0 tw:cursor-pointer tw:text-red-400 tw:active:bg-red-50 tw:dark:active:bg-red-500/10"
          @click="handleDeleteCategory"
        >
          <iconify icon="ph:trash-bold" class="tw:text-lg" />
        </button>
      </div>

      <!-- Row 2: search input (conditional) -->
      <div v-if="showSearch" class="tw:px-3 tw:pb-2.5">
        <div class="tw:relative">
          <iconify
            icon="ph:magnifying-glass-bold"
            class="tw:absolute tw:left-2.5 tw:top-1/2 tw:-translate-y-1/2 tw:text-slate-400 tw:text-sm tw:pointer-events-none"
          />
          <prime-input-text
            v-model="search"
            :placeholder="t('products.mobile.searchProductsPlaceholder')"
            class="app-input tw:w-full tw:pl-8!"
            autofocus
          />
        </div>
      </div>
    </div>

    <!-- ── Error ──────────────────────────────────────────────────── -->
    <prime-alert
      v-if="errorMessage"
      severity="error"
      variant="accent"
      closable
      class="tw:mx-3 tw:mt-2"
      @close="errorMessage = ''"
    >
      {{ errorMessage }}
    </prime-alert>

    <!-- ── Product list ───────────────────────────────────────────── -->
    <div class="tw:flex tw:flex-col tw:gap-1 tw:px-3 tw:pt-2">

      <!-- Skeleton -->
      <template v-if="loading && products.length === 0">
        <div
          v-for="n in 6"
          :key="n"
          class="tw:flex tw:items-center tw:gap-3 tw:px-3 tw:py-3 tw:bg-white tw:dark:bg-neutral-900 tw:rounded-xl"
        >
          <prime-skeleton width="3.5rem" height="3.5rem" border-radius="12px" class="tw:shrink-0" />
          <div class="tw:flex-1 tw:space-y-2">
            <prime-skeleton width="60%" height="0.85rem" />
            <prime-skeleton width="35%" height="0.75rem" />
          </div>
          <prime-skeleton width="4rem" height="0.85rem" />
        </div>
      </template>

      <!-- Cards -->
      <template v-else>
        <div
          v-for="product in products"
          :key="product.id"
          class="tw:flex tw:items-center tw:gap-3 tw:px-3 tw:py-3 tw:bg-white tw:dark:bg-neutral-900 tw:rounded-xl tw:cursor-pointer tw:transition-all tw:select-none"
          :class="selectedIds.has(product.id)
            ? 'tw:ring-2 tw:ring-blue-500 tw:ring-inset'
            : 'tw:active:bg-slate-50 tw:dark:active:bg-white/5'"
          @click="toggleSelect(product.id)"
        >
          <!-- Checkbox indicator -->
          <prime-checkbox
            :model-value="selectedIds.has(product.id)"
            :binary="true"
            class="tw:shrink-0 tw:pointer-events-none"
          />

          <!-- Thumbnail -->
          <div class="tw:shrink-0 tw:w-14 tw:h-14 tw:rounded-xl tw:overflow-hidden tw:bg-slate-100 tw:dark:bg-white/10">
            <img
              v-if="product.imageUrl"
              :src="product.imageUrl"
              :alt="product.name"
              class="tw:w-full tw:h-full tw:object-cover"
            />
            <div v-else class="tw:w-full tw:h-full tw:flex tw:items-center tw:justify-center">
              <iconify icon="ph:coffee-bold" class="tw:text-xl tw:text-slate-400" />
            </div>
          </div>

          <!-- Info -->
          <div class="tw:flex-1 tw:min-w-0">
            <p class="tw:font-semibold tw:text-sm tw:truncate tw:text-slate-800 tw:dark:text-white tw:uppercase tw:tracking-wide tw:leading-tight">
              {{ product.name }}
            </p>
            <p class="tw:text-xs tw:text-slate-500 tw:dark:text-slate-400 tw:mt-0.5 tw:truncate">
              {{ product.description || '—' }}
            </p>
            <p class="tw:text-sm tw:font-semibold tw:text-amber-600 tw:dark:text-amber-400 tw:mt-1">
              {{ formatVnd(product.price) }}
            </p>
          </div>

          <!-- Detail button -->
          <button
            type="button"
            class="tw:shrink-0 tw:w-9 tw:h-9 tw:rounded-xl tw:border tw:border-blue-500 tw:bg-transparent tw:flex tw:items-center tw:justify-center tw:cursor-pointer tw:active:bg-blue-50 tw:dark:active:bg-blue-500/10"
            @click.stop="router.push({ name: 'productsDetail', params: { id: product.id } })"
          >
            <iconify icon="ph:arrow-right-bold" class="tw:text-sm tw:text-blue-500" />
          </button>
        </div>

        <!-- Load more -->
        <button
          v-if="hasMore"
          type="button"
          class="tw:w-full tw:py-3 tw:text-sm tw:text-slate-500 tw:bg-transparent tw:border-0 tw:cursor-pointer tw:active:bg-black/5"
          :disabled="loading"
          @click="loadMore"
        >
          <iconify v-if="loading" icon="ph:spinner-bold" class="tw:animate-spin tw:mr-1" />
          {{ t('products.mobile.loadMore') }}
        </button>

        <!-- Empty -->
        <div
          v-if="products.length === 0 && !loading"
          class="tw:flex tw:flex-col tw:items-center tw:justify-center tw:py-20 tw:gap-3"
        >
          <iconify icon="ph:coffee-bold" class="tw:text-5xl tw:text-slate-300 tw:dark:text-white/20" />
          <p class="tw:text-sm tw:text-slate-400 tw:dark:text-slate-500">{{ t('products.mobile.empty') }}</p>
        </div>
      </template>
    </div>

    <!-- Spacer -->
    <div class="tw:h-28" />

    <!-- ── Fixed bottom actions ───────────────────────────────────── -->
    <div class="tw:fixed tw:bottom-6 tw:inset-x-4 tw:flex tw:gap-3 tw:z-20">
      <!-- Xóa sản phẩm -->
      <button
        type="button"
        class="tw:flex-1 tw:py-3.5 tw:rounded-2xl tw:border tw:text-sm tw:font-semibold tw:transition-colors tw:cursor-pointer"
        :class="selectedIds.size
          ? 'tw:border-red-400 tw:text-red-500 tw:bg-white tw:dark:bg-neutral-900 tw:active:bg-red-50'
          : 'tw:border-slate-200 tw:text-slate-300 tw:bg-white tw:dark:bg-neutral-900 tw:cursor-not-allowed'"
        :disabled="!selectedIds.size"
        @click="handleDeleteProduct"
      >
        {{ t('products.mobile.deleteProduct') }}
      </button>

      <!-- Thêm sản phẩm -->
      <button
        type="button"
        class="tw:flex-1 tw:py-3.5 tw:rounded-2xl tw:bg-emerald-500 tw:text-white tw:text-sm tw:font-semibold tw:border-0 tw:cursor-pointer tw:active:bg-emerald-600"
        @click="router.push({ name: 'productsCreate', query: { categoryId } })"
      >
        {{ t('products.mobile.addProduct') }}
      </button>
    </div>

    <!-- ── Edit drawer ──────────────────────────────────────────────── -->
    <prime-drawer
      v-model:visible="editDrawerVisible"
      position="bottom"
      :style="{ height: 'auto' }"
      :show-close-icon="false"
      :pt="{
        root: { class: 'tw:rounded-t-2xl' },
        header: { class: 'tw:pt-3 tw:pb-0 tw:px-5' },
        content: { class: 'tw:px-5 tw:pb-8' },
      }"
    >
      <template #header>
        <div class="tw:flex tw:flex-col tw:w-full">
          <!-- Handle -->
          <div class="tw:flex tw:justify-center tw:pb-3">
            <div class="tw:w-10 tw:h-1 tw:rounded-full tw:bg-slate-300 tw:dark:bg-white/20" />
          </div>
          <!-- Title row -->
          <div class="tw:flex tw:items-center tw:justify-between">
            <h3 class="tw:text-base tw:font-bold tw:text-slate-800 tw:dark:text-white">
              {{ t('products.mobile.editCategory') }}
            </h3>
            <button
              type="button"
              class="tw:w-8 tw:h-8 tw:flex tw:items-center tw:justify-center tw:rounded-full tw:bg-transparent tw:border-0 tw:cursor-pointer tw:text-slate-400 tw:active:bg-slate-100 tw:dark:active:bg-white/10"
              @click="editDrawerVisible = false"
            >
              <iconify icon="ph:x-bold" class="tw:text-base" />
            </button>
          </div>
        </div>
      </template>

      <!-- Image picker -->
      <div class="tw:flex tw:justify-center tw:mt-5 tw:mb-7">
        <div class="tw:relative tw:w-36 tw:h-36">
          <div class="tw:w-full tw:h-full tw:rounded-2xl tw:bg-slate-100 tw:dark:bg-white/10 tw:overflow-hidden tw:flex tw:items-center tw:justify-center">
            <img
              v-if="editImageUrl"
              :src="editImageUrl"
              class="tw:w-full tw:h-full tw:object-cover"
            />
            <iconify
              v-else
              icon="ph:image-square-bold"
              class="tw:text-5xl tw:text-slate-300 tw:dark:text-white/20"
            />
          </div>
          <button
            type="button"
            class="tw:absolute tw:-bottom-2 tw:-right-2 tw:w-9 tw:h-9 tw:rounded-full tw:bg-emerald-500 tw:flex tw:items-center tw:justify-center tw:border-2 tw:border-white tw:dark:border-neutral-900 tw:cursor-pointer tw:shadow-md"
            @click="editFileInputRef?.click()"
          >
            <iconify
              v-if="!editImageUploading"
              icon="ph:camera-bold"
              class="tw:text-sm tw:text-white"
            />
            <iconify
              v-else
              icon="ph:spinner-bold"
              class="tw:text-sm tw:text-white tw:animate-spin"
            />
          </button>
          <input
            ref="editFileInputRef"
            type="file"
            accept="image/*"
            class="tw:hidden"
            @change="handleEditImageSelect"
          />
        </div>
      </div>

      <!-- Name input -->
      <div class="tw:mb-5">
        <label class="tw:block tw:text-sm tw:font-semibold tw:text-emerald-600 tw:dark:text-emerald-400 tw:mb-2">
          {{ t('products.mobile.categoryNameLabel') }}
          <span class="tw:text-red-500">*</span>
        </label>
        <div class="tw:relative">
          <input
            v-model="editName"
            type="text"
            :placeholder="t('products.mobile.categoryNamePlaceholder')"
            class="tw:w-full tw:bg-transparent tw:border-0 tw:border-b-2 tw:border-emerald-500 tw:py-2 tw:pr-6 tw:text-sm tw:outline-none tw:text-slate-800 tw:dark:text-white tw:placeholder-slate-400 tw:dark:placeholder-white/30"
            @keyup.enter="submitEdit"
          />
          <button
            v-if="editName"
            type="button"
            class="tw:absolute tw:right-0 tw:top-1/2 tw:-translate-y-1/2 tw:text-slate-400 tw:hover:text-slate-600 tw:dark:hover:text-white tw:border-0 tw:bg-transparent tw:cursor-pointer tw:p-0.5"
            @click="editName = ''"
          >
            <iconify icon="ph:x-circle-fill" class="tw:text-base" />
          </button>
        </div>
      </div>

      <!-- Description input -->
      <div class="tw:mb-7">
        <label class="tw:block tw:text-sm tw:font-semibold tw:text-slate-500 tw:dark:text-slate-400 tw:mb-2">
          {{ t('products.mobile.categoryDescriptionLabel') }}
        </label>
        <div class="tw:relative">
          <input
            v-model="editDescription"
            type="text"
            :placeholder="t('products.mobile.categoryDescriptionPlaceholder')"
            class="tw:w-full tw:bg-transparent tw:border-0 tw:border-b tw:border-slate-300 tw:dark:border-white/20 tw:py-2 tw:pr-6 tw:text-sm tw:outline-none tw:text-slate-800 tw:dark:text-white tw:placeholder-slate-400 tw:dark:placeholder-white/30"
          />
          <button
            v-if="editDescription"
            type="button"
            class="tw:absolute tw:right-0 tw:top-1/2 tw:-translate-y-1/2 tw:text-slate-400 tw:hover:text-slate-600 tw:dark:hover:text-white tw:border-0 tw:bg-transparent tw:cursor-pointer tw:p-0.5"
            @click="editDescription = ''"
          >
            <iconify icon="ph:x-circle-fill" class="tw:text-base" />
          </button>
        </div>
      </div>

      <!-- Submit button -->
      <button
        type="button"
        class="tw:w-full tw:rounded-2xl tw:py-4 tw:text-sm tw:font-semibold tw:transition-colors tw:border-0 tw:cursor-pointer"
        :class="editName.trim()
          ? 'tw:bg-emerald-500 tw:text-white'
          : 'tw:bg-slate-100 tw:dark:bg-white/5 tw:text-slate-400 tw:dark:text-white/30 tw:cursor-not-allowed'"
        :disabled="!editName.trim() || editLoading"
        @click="submitEdit"
      >
        <iconify
          v-if="editLoading"
          icon="ph:spinner-bold"
          class="tw:animate-spin tw:text-base"
        />
        <span v-else>{{ t('products.mobile.saveChanges') }}</span>
      </button>
    </prime-drawer>

    <!-- ── Confirm dialog ─────────────────────────────────────────── -->
    <prime-dialog
      v-model:visible="confirmVisible"
      :header="confirmTitle"
      modal
      :style="{ width: '90vw', maxWidth: '360px' }"
      :pt="{ root: { class: 'tw:rounded-2xl!' } }"
    >
      <p class="tw:text-sm tw:text-slate-600 tw:dark:text-slate-300">{{ confirmMessage }}</p>
      <template #footer>
        <div class="tw:flex tw:gap-2 tw:justify-end">
          <prime-button
            severity="secondary"
            outlined
            size="small"
            :label="t('categories.create.cancel')"
            @click="confirmVisible = false"
          />
          <prime-button
            severity="danger"
            size="small"
            :loading="confirmLoading"
            @click="execConfirm"
          >
            <iconify icon="ph:trash-bold" />
            <span>{{ t('common.delete') }}</span>
          </prime-button>
        </div>
      </template>
    </prime-dialog>

  </section>
</template>
