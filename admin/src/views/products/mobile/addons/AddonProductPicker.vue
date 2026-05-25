<script setup>
import { ref, computed, watch, onMounted, onBeforeUnmount } from 'vue'
import { getProducts } from '@/services/product.service'
import { getCategory } from '@/services/category.service'

const props = defineProps({
  submitting: { type: Boolean, default: false },
  initialSelectedIds: { type: Array, default: () => [] },
  cancelLabel: { type: String, default: null },
})
const emit = defineEmits(['confirm', 'cancel'])

const { t } = useI18n()

const PICKER_PAGE_SIZE = 20

const pickerSearch = ref('')
const pickerCategoryFilter = ref(null)
const pickerProducts = ref([])
const pickerCategories = ref([])
const pickerPage = ref(1)
const pickerTotal = ref(0)
const pickerLoading = ref(false)
const pickerSelected = ref(new Set(props.initialSelectedIds))
const searchTimer = ref(null)

const pickerHasMore = computed(() => pickerProducts.value.length < pickerTotal.value)

const formatVnd = (value) =>
  new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(value ?? 0)

const loadProducts = async (reset = true) => {
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

const loadCategories = async () => {
  try {
    const res = await getCategory()
    const raw = res?.data
    pickerCategories.value = Array.isArray(raw) ? raw
      : Array.isArray(raw?.value) ? raw.value
      : Array.isArray(raw?.items) ? raw.items
      : []
  } catch { /* non-critical */ }
}

const toggleProduct = (id) => {
  const s = new Set(pickerSelected.value)
  s.has(id) ? s.delete(id) : s.add(id)
  pickerSelected.value = s
}

const loadMore = () => {
  if (!pickerHasMore.value || pickerLoading.value) return
  pickerPage.value++
  loadProducts(false)
}

watch(pickerSearch, () => {
  clearTimeout(searchTimer.value)
  searchTimer.value = setTimeout(() => loadProducts(true), 400)
})
watch(pickerCategoryFilter, () => loadProducts(true))

onMounted(() => {
  loadCategories()
  loadProducts(true)
})
onBeforeUnmount(() => clearTimeout(searchTimer.value))
</script>

<template>
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
        @click="toggleProduct(product.id)"
      >
        <div
          class="tw:shrink-0 tw:w-5 tw:h-5 tw:rounded tw:border-2 tw:flex tw:items-center tw:justify-center tw:transition-colors"
          :class="pickerSelected.has(product.id)
            ? 'tw:bg-primary-500 tw:border-primary-500'
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
        <prime-button severity="secondary" text size="small" :loading="pickerLoading" @click="loadMore">
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
      <prime-button severity="secondary" outlined class="tw:flex-1" @click="emit('cancel')">
        {{ cancelLabel ?? t('common.cancel') }}
      </prime-button>
      <prime-button severity="success" class="tw:flex-1" :loading="submitting" @click="emit('confirm', [...pickerSelected])">
        {{ t('products.mobile.addons.pickProductsSubmit') }}
        <span v-if="pickerSelected.size > 0" class="tw:ml-1 tw:text-primary-100">({{ pickerSelected.size }})</span>
      </prime-button>
    </div>

  </div>
</template>
