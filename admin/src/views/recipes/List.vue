<script setup>
import { listRecipes, deleteRecipe } from '@/services/recipe.service'

const { t } = useI18n()
const { can } = usePermission()
const router = useRouter()
const toast = useToast()
const cache = useTableCache('recipes')

// ── State ──────────────────────────────────────────────────────────
const loading      = ref(false)
const errorMessage = ref('')
const allRecipes   = ref([])
const rows         = ref(20)
const first        = ref(0)
const search       = ref('')
const typeFilter   = ref(null)
const categoryFilter = ref(null)
const filterPanel  = ref(null)
const searchTimer  = ref(null)

// ── Options ────────────────────────────────────────────────────────
const typeOptions = [
  { label: t('recipes.type.BaseIngredient'), value: 'BaseIngredient' },
  { label: t('recipes.type.Drink'),          value: 'Drink' },
]
const categoryOptions = [
  { label: t('recipes.category.BaseIngredient'), value: 'BaseIngredient' },
  { label: t('recipes.category.FruitTea'),       value: 'FruitTea' },
  { label: t('recipes.category.Coffee'),         value: 'Coffee' },
  { label: t('recipes.category.Smoothie'),       value: 'Smoothie' },
  { label: t('recipes.category.MilkTea'),        value: 'MilkTea' },
  { label: t('recipes.category.Other'),          value: 'Other' },
]

// ── Filters ────────────────────────────────────────────────────────
const activeFilterCount = computed(() => {
  let n = 0
  if (typeFilter.value)     n++
  if (categoryFilter.value) n++
  return n
})
const hasActiveFilters = computed(() => activeFilterCount.value > 0)

const clearFilters = () => {
  typeFilter.value     = null
  categoryFilter.value = null
  first.value          = 0
}

// ── Client-side filtering ──────────────────────────────────────────
const filteredRecipes = computed(() => {
  let list = allRecipes.value
  const q = search.value.trim().toLowerCase()
  if (q) list = list.filter(r => r.name.toLowerCase().includes(q) || (r.yield ?? '').toLowerCase().includes(q))
  if (typeFilter.value)     list = list.filter(r => r.type === typeFilter.value)
  if (categoryFilter.value) list = list.filter(r => r.category === categoryFilter.value)
  return list
})
const pagedRecipes   = computed(() => filteredRecipes.value.slice(first.value, first.value + rows.value))
const totalRecords   = computed(() => filteredRecipes.value.length)

// ── Load ───────────────────────────────────────────────────────────
const load = async () => {
  loading.value = true
  errorMessage.value = ''
  try {
    const res = await listRecipes()
    allRecipes.value = res.data ?? []
  } catch (err) {
    errorMessage.value = err?.response?.data?.title ?? t('recipes.error.loadFailed')
  } finally {
    loading.value = false
  }
}

// ── Delete ─────────────────────────────────────────────────────────
const handleDelete = async (row) => {
  try {
    await deleteRecipe(row.id)
    toast.add({ severity: 'success', summary: t('common.deleted'), life: 2500 })
    await load()
  } catch {
    toast.add({ severity: 'error', summary: t('recipes.error.deleteFailed'), life: 3000 })
  }
}

// ── Columns ────────────────────────────────────────────────────────
const columns = computed(() => [
  { field: 'name',     header: t('recipes.columns.name'),     width: '18rem' },
  { field: 'type',     header: t('recipes.columns.type'),     width: '9rem' },
  { field: 'category', header: t('recipes.columns.category'), width: '12rem' },
  { field: 'yield',    header: t('recipes.columns.yield'),    width: '12rem' },
  { key: 'actions',    header: t('recipes.columns.actions'),  width: '8rem', toggleable: false },
])

// ── Tag helpers ────────────────────────────────────────────────────
const typeTag = (type) => type === 'BaseIngredient'
  ? { label: t('recipes.type.BaseIngredient'), severity: 'warn' }
  : { label: t('recipes.type.Drink'),          severity: 'info' }

const categoryLabel = (cat) => t(`recipes.category.${cat}`)

// ── Mobile drawer ──────────────────────────────────────────────────
const drawerItem    = ref(null)
const drawerVisible = ref(false)
const openDrawer = (row) => { drawerItem.value = row; drawerVisible.value = true }

// ── Lifecycle ──────────────────────────────────────────────────────
onMounted(() => {
  const cached = cache.restore()
  if (cached) {
    search.value         = cached.search         ?? ''
    rows.value           = cached.rows           ?? 20
    first.value          = cached.first          ?? 0
    typeFilter.value     = cached.typeFilter     ?? null
    categoryFilter.value = cached.categoryFilter ?? null
  }
  load()
})

onBeforeRouteLeave(() => {
  cache.save({ search: search.value, rows: rows.value, first: first.value, typeFilter: typeFilter.value, categoryFilter: categoryFilter.value })
})

watch([search], () => {
  clearTimeout(searchTimer.value)
  searchTimer.value = setTimeout(() => { first.value = 0 }, 300)
})

watch([typeFilter, categoryFilter], () => { first.value = 0 })
onBeforeUnmount(() => clearTimeout(searchTimer.value))
</script>

<template>
  <section class="tw:space-y-8">

    <!-- Header -->
    <div class="tw:flex tw:flex-wrap tw:items-end tw:justify-between tw:gap-4">
      <div>
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300">{{ t('nav.groups.operations') }}</p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">{{ t('recipes.title') }}</h1>
        <p class="tw:mt-2 tw:text-sm tw:text-muted">{{ t('recipes.subtitle') }}</p>
      </div>
      <prime-button
        v-if="can('recipe.create')"
        severity="success"
        size="small"
        @click="router.push({ name: 'recipeNew' })"
      >
        <iconify icon="ph:plus-bold" />
        <span>{{ t('recipes.addRecipe') }}</span>
      </prime-button>
    </div>

    <!-- Error -->
    <prime-alert v-if="errorMessage" severity="error" variant="accent" closable @close="errorMessage = ''">
      {{ errorMessage }}
    </prime-alert>

    <!-- Table -->
    <AppTable
      v-model:first="first"
      v-model:rows="rows"
      :value="pagedRecipes"
      :loading="loading"
      :totalRecords="totalRecords"
      :rowsPerPageOptions="[10, 20, 50]"
      :columns="columns"
      @page="(e) => (first = e.first)"
    >
      <template #toolbar-left>
        <!-- Search -->
        <prime-icon-field>
          <prime-input-icon>
            <iconify icon="ph:magnifying-glass-bold" />
          </prime-input-icon>
          <prime-input-text
            v-model="search"
            :placeholder="t('common.search')"
            class="app-input tw:w-56"
          />
        </prime-icon-field>

        <!-- Filter button -->
        <prime-button
          :severity="hasActiveFilters ? 'success' : 'secondary'"
          :outlined="!hasActiveFilters"
          v-tooltip.top="t('recipes.filter.filtersTooltip')"
          @click="filterPanel.toggle($event)"
          :class="btnIcon"
        >
          <span class="tw:relative tw:inline-flex">
            <iconify icon="ph:funnel-bold" />
            <prime-badge
              v-if="activeFilterCount > 0"
              :value="activeFilterCount"
              severity="danger"
              class="tw:absolute! tw:-top-2.5! tw:-right-2.5! tw:scale-75! tw:origin-top-right!"
            />
          </span>
        </prime-button>

        <!-- Filter popover -->
        <prime-popover ref="filterPanel">
          <div class="tw:flex tw:flex-col tw:gap-4" style="min-width: 220px">
            <p class="tw:text-sm tw:font-semibold">{{ t('recipes.filter.title') }}</p>

            <div class="tw:space-y-1.5">
              <label class="tw:text-xs tw:text-muted tw:uppercase tw:tracking-widest">{{ t('recipes.filter.type') }}</label>
              <prime-select
                v-model="typeFilter"
                :options="typeOptions"
                option-label="label"
                option-value="value"
                :placeholder="t('recipes.filter.allTypes')"
                show-clear
                class="app-input tw:w-full"
              />
            </div>

            <div class="tw:space-y-1.5">
              <label class="tw:text-xs tw:text-muted tw:uppercase tw:tracking-widest">{{ t('recipes.filter.category') }}</label>
              <prime-select
                v-model="categoryFilter"
                :options="categoryOptions"
                option-label="label"
                option-value="value"
                :placeholder="t('recipes.filter.allCategories')"
                show-clear
                class="app-input tw:w-full"
              />
            </div>

            <prime-button v-if="hasActiveFilters" severity="danger" outlined size="small" @click="clearFilters">
              <iconify icon="ph:x-bold" />
              <span>{{ t('recipes.filter.clearFilters') }}</span>
            </prime-button>
          </div>
        </prime-popover>
      </template>

      <!-- Mobile card -->
      <template #mobile-card="{ data }">
        <div class="tw:rounded-xl tw:border tw:border-slate-200 tw:dark:border-white/10 tw:bg-white tw:dark:bg-white/5 tw:p-3 tw:flex tw:flex-col tw:gap-2">
          <div class="tw:flex tw:items-start tw:justify-between tw:gap-2">
            <span class="tw:font-semibold tw:text-sm tw:leading-snug">{{ data.name }}</span>
            <prime-tag :value="typeTag(data.type).label" :severity="typeTag(data.type).severity"
              class="tw:text-[11px]! tw:px-1.5! tw:py-0.5! tw:shrink-0" />
          </div>
          <p class="tw:text-xs tw:text-muted">{{ categoryLabel(data.category) }}</p>
          <p v-if="data.yield" class="tw:text-xs app-text-subtle tw:truncate">{{ data.yield }}</p>
          <div class="tw:border-t tw:border-slate-200 tw:dark:border-white/10 tw:pt-2">
            <prime-button severity="secondary" outlined size="small" fluid @click="openDrawer(data)">
              <iconify icon="ph:dots-three-bold" />
              <span>{{ t('common.moreActions') }}</span>
            </prime-button>
          </div>
        </div>
      </template>

      <!-- Columns -->
      <template #col-name="{ data }">
        <button
          class="tw:text-left tw:font-medium tw:text-sm tw:hover:text-primary-500 tw:transition-colors tw:cursor-pointer tw:bg-transparent tw:border-0 tw:p-0"
          @click="router.push({ name: 'recipeDetail', params: { id: data.id } })"
        >{{ data.name }}</button>
      </template>

      <template #col-type="{ data }">
        <prime-tag :value="typeTag(data.type).label" :severity="typeTag(data.type).severity" />
      </template>

      <template #col-category="{ data }">
        <span class="tw:text-sm tw:text-muted">{{ categoryLabel(data.category) }}</span>
      </template>

      <template #col-yield="{ data }">
        <span class="tw:text-sm tw:text-muted tw:truncate tw:block tw:max-w-48">{{ data.yield ?? '—' }}</span>
      </template>

      <template #col-actions="{ data }">
        <div class="tw:flex tw:justify-end tw:gap-2">
          <prime-button
            v-if="can('recipe.update')"
            severity="secondary" outlined size="small"
            v-tooltip.top="t('recipes.actions.editTooltip')"
            :class="btnIcon"
            @click="router.push({ name: 'recipeEdit', params: { id: data.id } })"
          >
            <iconify icon="ph:pencil-bold" />
          </prime-button>
          <prime-button
            v-if="can('recipe.delete')"
            severity="danger" outlined size="small"
            v-tooltip.top="t('recipes.actions.deleteTooltip')"
            :class="btnIcon"
            @click="handleDelete(data)"
          >
            <iconify icon="ph:trash-bold" />
          </prime-button>
        </div>
      </template>
    </AppTable>

    <!-- Mobile drawer -->
    <prime-drawer
      v-model:visible="drawerVisible"
      position="bottom"
      :style="{ height: 'auto' }"
      :pt="{ root: { class: 'tw:rounded-t-2xl' } }"
    >
      <template #header>
        <div class="tw:flex tw:items-center tw:gap-2">
          <span class="tw:font-medium">{{ drawerItem?.name }}</span>
          <prime-tag v-if="drawerItem"
            :value="typeTag(drawerItem.type).label"
            :severity="typeTag(drawerItem.type).severity"
            class="tw:text-[11px]! tw:px-1.5! tw:py-0.5!" />
        </div>
      </template>
      <div v-if="drawerItem" class="tw:flex tw:flex-col tw:gap-2 tw:pb-4">
        <prime-button
          v-if="can('recipe.update')"
          :label="t('recipes.actions.edit')" severity="secondary" outlined fluid
          @click="router.push({ name: 'recipeEdit', params: { id: drawerItem.id } }); drawerVisible = false"
        >
          <template #icon><iconify icon="ph:pencil-bold" /></template>
        </prime-button>
        <prime-button
          v-if="can('recipe.delete')"
          :label="t('recipes.actions.delete')" severity="danger" outlined fluid
          @click="handleDelete(drawerItem); drawerVisible = false"
        >
          <template #icon><iconify icon="ph:trash-bold" /></template>
        </prime-button>
      </div>
    </prime-drawer>

  </section>
</template>
