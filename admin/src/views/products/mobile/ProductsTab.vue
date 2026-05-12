<script setup>
import { onBeforeUnmount, onMounted, ref, watch, computed } from 'vue'
import { onBeforeRouteLeave } from 'vue-router'
import { getProducts } from '@/services/product.service'
import { getCategory } from '@/services/category.service'

const props = defineProps({
  search: { type: String, default: '' },
  viewMode: { type: String, default: 'list' },
})

const { t } = useI18n()
const router = useRouter()
const cache = useTableCache('products-mobile')

// ── State ────────────────────────────────────────────────────────────
const loading = ref(false)
const errorMessage = ref('')
const products = ref([])
const page = ref(1)
const PAGE_SIZE = 20
const totalRecords = ref(0)
const hasMore = computed(() => products.value.length < totalRecords.value)

// ── Category filter ───────────────────────────────────────────────────
const categoryFilter = ref(null)
const categories = ref([])
const searchTimer = ref(null)

// ── Drag-to-scroll (pills) ────────────────────────────────────────────
const pillsRef = ref(null)
let dragState = null

const onPillsPointerDown = (e) => {
  if (e.button !== 0) return
  dragState = { startX: e.clientX, scrollLeft: pillsRef.value.scrollLeft, moved: false }
}
const onPillsPointerMove = (e) => {
  if (!dragState) return
  const dx = e.clientX - dragState.startX
  if (Math.abs(dx) > 4) dragState.moved = true
  pillsRef.value.scrollLeft = dragState.scrollLeft - dx
}
const onPillsPointerUp = () => { dragState = null }
const onPillsClick = (e) => { if (dragState?.moved) e.stopPropagation() }

// ── Helpers ───────────────────────────────────────────────────────────
const formatVnd = (value) =>
  new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(value ?? 0)

// ── Load ──────────────────────────────────────────────────────────────
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

const loadProducts = async (reset = true) => {
  if (reset) { page.value = 1; products.value = [] }
  loading.value = true
  errorMessage.value = ''
  try {
    const res = await getProducts({
      page: page.value,
      pageSize: PAGE_SIZE,
      searchTerm: props.search.trim() || undefined,
      categoryId: categoryFilter.value ?? undefined,
    })
    const paged = res?.data ?? {}
    const items = paged.value?.map(p => ({
      id: p.id,
      name: p.name,
      price: p.price,
      imageUrl: p.imageUrl,
      category: p.categoryName,
      isActive: p.isActive,
    })) ?? []
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

onMounted(() => {
  loadCategories()
  const cached = cache.restore()
  if (cached) categoryFilter.value = cached.categoryFilter ?? null
  loadProducts(true)
})

onBeforeRouteLeave(() => {
  cache.save({ categoryFilter: categoryFilter.value })
})

// search prop thay đổi từ parent → debounce
watch(() => props.search, () => {
  clearTimeout(searchTimer.value)
  searchTimer.value = setTimeout(() => loadProducts(true), 400)
})

watch(categoryFilter, () => loadProducts(true))

onBeforeUnmount(() => clearTimeout(searchTimer.value))
</script>

<template>
  <div class="tw:flex tw:flex-col">

    <!-- ── Category pills ─────────────────────────────────────────── -->
    <div class="tw:flex tw:items-center tw:gap-2 tw:px-5 tw:py-2.5 tw:bg-white tw:dark:bg-neutral-900 tw:border-b tw:border-slate-100 tw:dark:border-white/5">
      <div
        ref="pillsRef"
        class="tw:flex tw:gap-2 tw:overflow-x-auto tw:flex-1 tw:select-none tw:cursor-grab"
        style="scrollbar-width: none; -ms-overflow-style: none;"
        @pointerdown="onPillsPointerDown"
        @pointermove="onPillsPointerMove"
        @pointerup="onPillsPointerUp"
        @pointercancel="onPillsPointerUp"
        @click.capture="onPillsClick"
      >
        <prime-button
          size="small"
          rounded
          outlined
          :severity="categoryFilter === null ? 'info' : 'secondary'"
          class="tw:shrink-0 tw:whitespace-nowrap"
          @click="categoryFilter = null"
        >
          {{ t('products.mobile.allCategories') }}
        </prime-button>
        <prime-button
          v-for="cat in categories"
          :key="cat.id"
          size="small"
          rounded
          outlined
          :severity="categoryFilter === cat.id ? 'success' : 'secondary'"
          class="tw:shrink-0 tw:whitespace-nowrap"
          @click="categoryFilter = cat.id"
        >
          {{ cat.name }}
        </prime-button>
      </div>
      <prime-button severity="secondary" text :class="btnIcon">
        <iconify icon="ph:grid-four-bold" class="tw:text-blue-500" />
      </prime-button>
    </div>

    <!-- ── Error ──────────────────────────────────────────────────── -->
    <prime-alert
      v-if="errorMessage"
      severity="error"
      variant="accent"
      closable
      class="tw:mx-5 tw:mt-2"
      @close="errorMessage = ''"
    >
      {{ errorMessage }}
    </prime-alert>

    <!-- ── Content ────────────────────────────────────────────────── -->
    <div class="tw:bg-slate-50 tw:dark:bg-neutral-950 tw:pb-24">

      <!-- Skeleton -->
      <template v-if="loading && products.length === 0">
        <div
          v-for="n in 6"
          :key="n"
          class="tw:flex tw:items-center tw:gap-3 tw:px-5 tw:py-3 tw:bg-white tw:dark:bg-neutral-900 tw:border-b tw:border-slate-100 tw:dark:border-white/5"
        >
          <prime-skeleton width="3.5rem" height="3.5rem" border-radius="12px" class="tw:shrink-0" />
          <div class="tw:flex-1 tw:space-y-2">
            <prime-skeleton width="65%" height="0.85rem" />
            <prime-skeleton width="35%" height="0.75rem" />
            <prime-skeleton width="28%" height="0.85rem" />
          </div>
          <prime-skeleton width="2.5rem" height="2.5rem" border-radius="10px" class="tw:shrink-0" />
        </div>
      </template>

      <!-- List view -->
      <template v-else-if="viewMode === 'list'">
        <div
          v-for="product in products"
          :key="product.id"
          class="tw:flex tw:items-center tw:gap-3 tw:px-5 tw:py-3 tw:bg-white tw:dark:bg-neutral-900 tw:border-b tw:border-slate-100 tw:dark:border-white/5"
        >
          <div class="tw:shrink-0 tw:w-14 tw:h-14 tw:rounded-xl tw:overflow-hidden tw:bg-slate-100 tw:dark:bg-white/10">
            <img v-if="product.imageUrl" :src="product.imageUrl" :alt="product.name" class="tw:w-full tw:h-full tw:object-cover" />
            <div v-else class="tw:w-full tw:h-full tw:flex tw:items-center tw:justify-center">
              <iconify icon="ph:coffee-bold" class="tw:text-xl tw:text-slate-400" />
            </div>
          </div>
          <div class="tw:flex-1 tw:min-w-0">
            <p class="tw:font-semibold tw:text-sm tw:uppercase tw:tracking-wide tw:leading-tight tw:line-clamp-1 tw:text-slate-800 tw:dark:text-white">{{ product.name }}</p>
            <p class="tw:text-xs tw:text-slate-500 tw:dark:text-slate-400 tw:mt-0.5">{{ product.category }}</p>
            <p class="tw:text-sm tw:font-semibold tw:text-amber-500 tw:mt-1">{{ formatVnd(product.price) }}</p>
          </div>
          <prime-button
            severity="info"
            outlined
            :class="btnIcon"
            @click="router.push({ name: 'productsDetail', params: { id: product.id } })"
          >
            <iconify icon="ph:arrow-square-out-bold" />
          </prime-button>
        </div>

        <div v-if="hasMore" class="tw:flex tw:justify-center tw:py-5 tw:bg-white tw:dark:bg-neutral-900">
          <prime-button severity="secondary" text size="small" :loading="loading" @click="loadMore">
            {{ t('products.mobile.loadMore') }}
          </prime-button>
        </div>
      </template>

      <!-- Grid view -->
      <div v-else class="tw:grid tw:grid-cols-2 tw:gap-px tw:bg-slate-200 tw:dark:bg-white/10">
        <div
          v-for="product in products"
          :key="product.id"
          class="tw:bg-white tw:dark:bg-neutral-900 tw:cursor-pointer tw:active:bg-slate-50 tw:dark:active:bg-white/5"
          @click="router.push({ name: 'productsDetail', params: { id: product.id } })"
        >
          <div class="tw:aspect-square tw:bg-slate-100 tw:dark:bg-white/10">
            <img v-if="product.imageUrl" :src="product.imageUrl" :alt="product.name" class="tw:w-full tw:h-full tw:object-cover" />
            <div v-else class="tw:w-full tw:h-full tw:flex tw:items-center tw:justify-center">
              <iconify icon="ph:coffee-bold" class="tw:text-3xl tw:text-slate-400" />
            </div>
          </div>
          <div class="tw:p-3">
            <p class="tw:font-semibold tw:text-xs tw:uppercase tw:tracking-wide tw:line-clamp-2 tw:leading-tight tw:text-slate-800 tw:dark:text-white">{{ product.name }}</p>
            <p class="tw:text-xs tw:text-slate-500 tw:dark:text-slate-400 tw:mt-0.5 tw:truncate">{{ product.category }}</p>
            <p class="tw:text-sm tw:font-semibold tw:text-amber-500 tw:mt-1">{{ formatVnd(product.price) }}</p>
          </div>
        </div>
        <div v-if="hasMore" class="tw:col-span-2 tw:flex tw:justify-center tw:py-5 tw:bg-white tw:dark:bg-neutral-900">
          <prime-button severity="secondary" text size="small" :loading="loading" @click="loadMore">
            {{ t('products.mobile.loadMore') }}
          </prime-button>
        </div>
      </div>

      <!-- Empty -->
      <div v-if="!loading && products.length === 0" class="tw:flex tw:flex-col tw:items-center tw:justify-center tw:py-20 tw:gap-3">
        <iconify icon="ph:coffee-bold" class="tw:text-5xl tw:text-slate-300 tw:dark:text-white/20" />
        <p class="tw:text-sm tw:text-slate-400 tw:dark:text-slate-500">{{ t('products.mobile.empty') }}</p>
      </div>
    </div>

  </div>
</template>
