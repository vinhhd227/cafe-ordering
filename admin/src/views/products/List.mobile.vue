<script setup>
import { ref, computed, defineAsyncComponent, onMounted } from 'vue'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const { can } = usePermission()

// ── Tabs ─────────────────────────────────────────────────────────────
const validTabs = ['products', 'inventory', 'addons', 'categories']
const activeTab = ref('products')

onMounted(() => {
  const q = route.query.tab
  if (q && validTabs.includes(q)) activeTab.value = q
})

const tabs = [
  { key: 'products',   labelKey: 'products.mobile.tabs.products' },
  { key: 'inventory',  labelKey: 'products.mobile.tabs.inventory' },
  { key: 'addons',     labelKey: 'products.mobile.tabs.addons' },
  { key: 'categories', labelKey: 'products.mobile.tabs.categories' },
]

const tabComponents = {
  products:   defineAsyncComponent(() => import('./mobile/ProductsTab.vue')),
  inventory:  defineAsyncComponent(() => import('./mobile/InventoryTab.vue')),
  addons:     defineAsyncComponent(() => import('./mobile/AddonsTab.vue')),
  categories: defineAsyncComponent(() => import('./mobile/CategoriesTab.vue')),
}

const currentTab = computed(() => tabComponents[activeTab.value])

watch(activeTab, (val) => {
  search.value = ''
  if (route.query.tab !== val) {
    router.replace({ query: { ...route.query, tab: val } })
  }
})

// ── Search (shared, owned by shell, passed to active tab) ─────────────
const search = ref('')

const searchPlaceholders = computed(() => ({
  products:   t('products.mobile.searchPlaceholder'),
  inventory:  t('products.mobile.searchPlaceholder'),
  addons:     t('products.mobile.addonsSearchPlaceholder'),
  categories: t('products.mobile.categorySearchPlaceholder'),
}))

// ── View mode (owned here, passed to tabs that support it) ────────────
const viewMode = ref('list')
</script>

<template>
  <section class="tw:flex tw:flex-col tw:h-full">

    <!-- ── Sticky top bar ────────────────────────────────────────── -->
    <div class="tw:shrink-0 tw:border-b tw:border-slate-200 tw:dark:border-white/10" :class="bgGlass">

      <!-- Row 1: back + search + sort + view -->
      <div class="tw:flex tw:items-center tw:gap-2 tw:px-4 tw:pt-3 tw:pb-2">
        <button
          type="button"
          class="tw:shrink-0 tw:w-8 tw:h-8 tw:flex tw:items-center tw:justify-center tw:rounded-lg tw:bg-transparent tw:border-0 tw:cursor-pointer tw:text-muted tw:active:bg-black/5 tw:dark:active:bg-white/5"
          @click="router.back()"
        >
          <iconify icon="ph:arrow-left-bold" class="tw:text-lg" />
        </button>

        <div class="tw:flex-1 tw:relative">
          <iconify
            icon="ph:magnifying-glass-bold"
            class="tw:absolute tw:left-2.5 tw:top-1/2 tw:-translate-y-1/2 tw:text-muted tw:text-sm tw:pointer-events-none"
          />
          <prime-input-text
            v-model="search"
            :placeholder="searchPlaceholders[activeTab]"
            class="app-input tw:w-full tw:pl-8! tw:text-sm!"
            :class="activeTab !== 'categories' ? 'tw:pr-9!' : ''"
            size="large"
          />
          <button
            v-if="activeTab === 'products' || activeTab === 'inventory'"
            type="button"
            class="tw:absolute tw:right-1 tw:top-1/2 tw:-translate-y-1/2 tw:w-7 tw:h-7 tw:flex tw:items-center tw:justify-center tw:rounded tw:bg-transparent tw:border-0 tw:cursor-pointer tw:text-muted"
          >
            <iconify icon="ph:barcode-bold" class="tw:text-base" />
          </button>
          <button
            v-else-if="activeTab === 'categories'"
            type="button"
            class="tw:absolute tw:right-1 tw:top-1/2 tw:-translate-y-1/2 tw:w-7 tw:h-7 tw:flex tw:items-center tw:justify-center tw:rounded tw:bg-transparent tw:border-0 tw:cursor-pointer tw:text-muted"
          >
            <iconify icon="ph:eye-bold" class="tw:text-base" />
          </button>
        </div>

        <template v-if="activeTab === 'products' || activeTab === 'inventory'">
          <button
            type="button"
            class="tw:shrink-0 tw:w-8 tw:h-8 tw:flex tw:items-center tw:justify-center tw:rounded-lg tw:bg-transparent tw:border-0 tw:cursor-pointer tw:text-muted tw:active:bg-black/5 tw:dark:active:bg-white/5"
          >
            <iconify icon="ph:sort-descending-bold" class="tw:text-lg" />
          </button>
          <button
            type="button"
            class="tw:shrink-0 tw:w-8 tw:h-8 tw:flex tw:items-center tw:justify-center tw:rounded-lg tw:bg-transparent tw:border-0 tw:cursor-pointer tw:text-muted tw:active:bg-black/5 tw:dark:active:bg-white/5"
            @click="viewMode = viewMode === 'list' ? 'grid' : 'list'"
          >
            <iconify :icon="viewMode === 'grid' ? 'ph:list-bullets-bold' : 'ph:squares-four-bold'" class="tw:text-lg" />
          </button>
        </template>
      </div>

      <!-- Row 2: Tabs -->
      <prime-tabs v-model:value="activeTab">
        <prime-tab-list :pt="{ root: { class: 'tw:bg-transparent!' } }">
          <prime-tab
            v-for="tab in tabs"
            :key="tab.key"
            :value="tab.key"
            class="tw:flex-1 tw:text-lg!"
            :pt="{ root: { class: 'tw:py-2! tw:leading-7!' } }"
          >
            {{ t(tab.labelKey) }}
          </prime-tab>
        </prime-tab-list>
      </prime-tabs>
    </div>

    <!-- ── Tab content ───────────────────────────────────────────── -->
    <component
      :is="currentTab"
      :search="search"
      :view-mode="viewMode"
      class="tw:flex-1 tw:overflow-y-auto"
    />

    <!-- ── FAB ───────────────────────────────────────────────────── -->
    <router-link
      v-if="can('product.create') && activeTab === 'products'"
      :to="{ name: 'productsCreate' }"
      class="tw:fixed tw:bottom-6 tw:right-4 tw:z-20 tw:no-underline"
    >
      <div class="tw:w-14 tw:h-14 tw:rounded-full tw:bg-primary-500 tw:flex tw:items-center tw:justify-center tw:shadow-xl tw:shadow-primary-500/40 tw:transition-transform tw:active:scale-95">
        <iconify icon="ph:plus-bold" class="tw:text-2xl tw:text-white" />
      </div>
    </router-link>

  </section>
</template>
