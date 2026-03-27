# List Page Rules — Vue 3 Admin

## Cấu trúc file

```
src/views/[feature]/List.vue
```

Mỗi list page là một `<script setup>` + `<template>` — không tách component trừ khi tái sử dụng nhiều nơi.

---

## Script setup — thứ tự khai báo

```js
// 1. Composables & services
const { t } = useI18n()
const cache = useTableCache('feature')
const router = useRouter()
const { can } = usePermission()

// 2. Table state
const loading = ref(false)
const errorMessage = ref('')
const items = ref([])
const rows = ref(10)
const first = ref(0)

// 3. Column definitions (nếu có column toggle)
const buildColDefs = () => [
  { field: 'id',      header: t('...'), width: '4rem',  visible: true },
  { field: 'name',    header: t('...'), width: '12rem', visible: true },
  { key: 'actions',   header: t('...'), width: '10rem', toggleable: false },
]
const colDefs = ref(buildColDefs())

// 4. Filters
const search = ref('')
const statusFilter = ref(null)
const filterPanel = ref(null)   // ref cho prime-popover

const activeFilterCount = computed(() => {
  let n = 0
  if (statusFilter.value !== null) n++
  // ... các filter khác
  return n
})
const hasActiveFilters = computed(() => activeFilterCount.value > 0)

const clearFilters = () => {
  statusFilter.value = null
  first.value = 0
}

// 5. Summary stats
const summary = ref({ total: 0, active: 0, inactive: 0 })
// hoặc computed nếu client-side

// 6. Helper functions (formatters, status helpers)
const statusTag = (isActive) =>
  isActive
    ? { label: t('...active'),   severity: 'success' }
    : { label: t('...inactive'), severity: 'danger' }

// 7. Load functions
const loadItems = async (page = 1) => { ... }

// 8. Action handlers
const handleToggleActive = async (row) => { ... }

// 9. Lifecycle & cache
onMounted(() => {
  const cached = cache.restore()
  if (cached) {
    search.value       = cached.search       ?? ''
    rows.value         = cached.rows         ?? 10
    first.value        = cached.first        ?? 0
    statusFilter.value = cached.statusFilter ?? null
    // restore colDefs nếu có column toggle
  }
  loadItems(1)
})

onBeforeRouteLeave(() => {
  cache.save({ search: search.value, rows: rows.value, first: first.value, statusFilter: statusFilter.value })
})

// 10. Watchers
watch([search], () => {
  clearTimeout(searchTimer.value)
  searchTimer.value = setTimeout(() => { first.value = 0; loadItems(1) }, 400)
})
watch([statusFilter], () => { first.value = 0; loadItems(1) })

onBeforeUnmount(() => clearTimeout(searchTimer.value))

// 11. Mobile drawer
const drawerItem = ref(null)
const drawerVisible = ref(false)
const openDrawer = (row) => { drawerItem.value = row; drawerVisible.value = true }
```

---

## Phân trang: server-side vs client-side

### Server-side (dữ liệu nhiều, có filter phức tạp)
```js
// Load theo page — gọi API mỗi khi filter/page thay đổi
const loadItems = async (page = 1) => {
  loading.value = true
  try {
    const res = await getItems({ page, pageSize: rows.value, search: search.value.trim() || undefined })
    items.value = res.data.value ?? []
    totalRecords.value = res.data.pagedInfo.totalRecords
  } catch (err) {
    errorMessage.value = err?.response?.data?.message || t('...')
  } finally {
    loading.value = false
  }
}

// AppTable event handler
// @page="(e) => loadItems(e.page + 1)"
```

### Client-side (dữ liệu nhỏ, filter đơn giản)
```js
// Load 1 lần, filter/page bằng computed
const allItems = ref([])

const filteredItems = computed(() => {
  let list = allItems.value
  if (search.value.trim()) {
    const q = search.value.trim().toLowerCase()
    list = list.filter(i => i.name.toLowerCase().includes(q))
  }
  if (statusFilter.value !== null) list = list.filter(i => i.isActive === statusFilter.value)
  return list
})

const pagedItems = computed(() =>
  filteredItems.value.slice(first.value, first.value + rows.value)
)
const totalRecords = computed(() => filteredItems.value.length)

// AppTable event handler
// @page="(e) => (first = e.first)"
```

---

## Template structure

```html
<template>
  <section class="tw:space-y-8">

    <!-- 1. Header: breadcrumb + title + CTA button -->
    <div class="tw:flex tw:flex-wrap tw:items-end tw:justify-between tw:gap-4">
      <div>
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300">{{ t('...breadcrumb') }}</p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">{{ t('...title') }}</h1>
        <p class="tw:mt-2 tw:text-sm app-text-muted">{{ t('...subtitle') }}</p>
      </div>
      <prime-button v-if="can('feature.create')" severity="success" size="small" @click="router.push({ name: '...' })">
        <iconify icon="ph:plus-bold" />
        <span>{{ t('...add') }}</span>
      </prime-button>
    </div>

    <!-- 2. Summary stats (widgets) -->
    <div class="tw:grid tw:grid-cols-3 tw:gap-3">
      <!-- prime-card hoặc widget-stat component -->
    </div>

    <!-- 3. Error message -->
    <prime-alert v-if="errorMessage" severity="error" variant="accent" closable @close="errorMessage = ''">
      {{ errorMessage }}
    </prime-alert>

    <!-- 4. AppTable -->
    <AppTable ...>
      <template #toolbar-left>
        <!-- Search input + Filter button + Filter popover -->
      </template>

      <template #mobile-card="{ data }">
        <!-- Card layout cho màn hình nhỏ -->
      </template>

      <!-- Column templates: #col-{field}="{ data }" -->
      <template #col-isActive="{ data }">
        <prime-tag :value="statusTag(data.isActive).label" :severity="statusTag(data.isActive).severity" />
      </template>

      <template #col-actions="{ data }">
        <div class="tw:flex tw:justify-end tw:gap-2">
          <!-- Action buttons với btnIcon -->
        </div>
      </template>
    </AppTable>

    <!-- 5. Mobile action drawer -->
    <prime-drawer v-model:visible="drawerVisible" position="bottom" :style="{ height: 'auto' }"
      :pt="{ root: { class: 'tw:rounded-t-2xl' } }">
      <!-- header + action buttons fluid -->
    </prime-drawer>

  </section>
</template>
```

---

## AppTable — các props/events quan trọng

```html
<AppTable
  v-model:first="first"
  v-model:rows="rows"
  v-model:columns="colDefs"         <!-- chỉ khi có column toggle -->
  :value="items"
  :loading="loading"
  :totalRecords="totalRecords"
  :rowsPerPageOptions="[5, 10, 20, 50]"
  :show-column-toggle="true"        <!-- chỉ khi có colDefs -->
  @page="(e) => loadItems(e.page + 1)"   <!-- server-side -->
  @page="(e) => (first = e.first)"       <!-- client-side -->
>
```

**Column definition:**
```js
{ field: 'name',    header: '...',   width: '12rem', visible: true }   // data column
{ key: 'product',   header: '...',   width: '14rem', visible: true }   // custom slot column
{ key: 'actions',   header: '...',   width: '10rem', toggleable: false } // không cho ẩn
```

---

## Filter pattern

```html
<!-- Filter button — đổi màu khi có active filter -->
<prime-button
  :severity="hasActiveFilters ? 'success' : 'secondary'"
  :outlined="!hasActiveFilters"
  v-tooltip.top="t('...filtersTooltip')"
  @click="filterPanel.toggle($event)"
  :class="!hasActiveFilters ? btnIcon : ''"
>
  <iconify icon="ph:funnel-bold" />
  <prime-badge v-if="activeFilterCount > 0" :value="activeFilterCount" severity="danger" class="tw:ml-1 tw:scale-90" />
</prime-button>

<!-- Filter popover -->
<prime-popover ref="filterPanel">
  <div class="tw:flex tw:flex-col tw:gap-4">
    <p class="tw:text-sm tw:font-semibold">{{ t('...filterTitle') }}</p>

    <div class="tw:space-y-1.5">
      <label class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest">{{ t('...') }}</label>
      <prime-select v-model="statusFilter" :options="statusOptions" option-label="label" option-value="value"
        :placeholder="t('...all')" show-clear class="app-input tw:w-full" />
    </div>

    <prime-button v-if="hasActiveFilters" severity="danger" outlined size="small" @click="clearFilters">
      <iconify icon="ph:x-bold" />
      <span>{{ t('...clearFilters') }}</span>
    </prime-button>
  </div>
</prime-popover>
```

---

## Cache pattern — bắt buộc

Mọi list page **phải** dùng `useTableCache` để lưu/khôi phục trạng thái khi quay lại trang.

```js
const cache = useTableCache('feature-name') // key unique per page

onMounted(() => {
  const cached = cache.restore()
  if (cached) {
    search.value       = cached.search       ?? ''
    rows.value         = cached.rows         ?? 10
    first.value        = cached.first        ?? 0
    statusFilter.value = cached.statusFilter ?? null
    // restore colDefs nếu có
    if (cached.colDefs) {
      const cachedMap = Object.fromEntries(cached.colDefs.map(c => [c.key ?? c.field, c]))
      colDefs.value = colDefs.value.map(col => {
        if (col.toggleable === false) return col
        const id = col.key ?? col.field
        const c = cachedMap[id]
        return c ? { ...col, visible: c.visible } : col
      })
    }
  }
  loadItems(/* page từ first nếu server-side */)
})

onBeforeRouteLeave(() => {
  cache.save({
    search: search.value,
    rows: rows.value,
    first: first.value,
    statusFilter: statusFilter.value,
    // thêm filter khác nếu có
  })
})
```

---

## Mobile drawer — bắt buộc

Mọi list page có action column **phải** có mobile drawer thay thế action buttons trên màn nhỏ.

```html
<!-- Trong mobile-card slot: nút full-width để mở drawer -->
<prime-button severity="secondary" outlined size="small" fluid @click="openDrawer(data)">
  <iconify icon="ph:dots-three-bold" />
  <span>{{ t('common.moreActions') }}</span>
</prime-button>

<!-- Drawer -->
<prime-drawer v-model:visible="drawerVisible" position="bottom" :style="{ height: 'auto' }"
  :pt="{ root: { class: 'tw:rounded-t-2xl' } }">
  <template #header>
    <div class="tw:flex tw:items-center tw:gap-2">
      <span class="tw:font-medium">{{ drawerItem?.name }}</span>
      <prime-tag v-if="drawerItem" :value="statusTag(drawerItem.isActive).label"
        :severity="statusTag(drawerItem.isActive).severity" class="tw:text-[11px]! tw:px-1.5! tw:py-0.5!" />
    </div>
  </template>
  <div v-if="drawerItem" class="tw:flex tw:flex-col tw:gap-2 tw:pb-4">
    <!-- Buttons dùng fluid -->
    <prime-button label="..." severity="..." outlined fluid @click="...; drawerVisible = false">
      <template #icon><iconify icon="..." /></template>
    </prime-button>
  </div>
</prime-drawer>
```

---

## Debounce — text input vs select

```js
const searchTimer = ref(null)

// Text/number input → debounce 400ms
watch([search, minPrice, maxPrice], () => {
  clearTimeout(searchTimer.value)
  searchTimer.value = setTimeout(() => { first.value = 0; loadItems(1) }, 400)
})

// Select/checkbox → thay đổi ngay lập tức
watch([statusFilter, categoryFilter], () => {
  first.value = 0
  loadItems(1)
})

onBeforeUnmount(() => clearTimeout(searchTimer.value))
```

---

## Quy tắc tổng hợp

- **Luôn dùng `useTableCache`** — không để user mất filter khi back
- **Luôn có `mobile-card` slot + drawer** cho action trên mobile
- **`btnIcon`** cho tất cả icon-only action buttons trong table
- **`v-if="can('...')`** bảo vệ CTA button và action buttons theo permission
- **`app-input`** class cho tất cả input trong filter/toolbar
- **`app-text-muted` / `app-text-subtle`** cho text phụ — không hardcode màu
- **`tw:space-y-8`** trên `<section>` root để căn khoảng cách giữa các block
- Error dùng `<prime-alert severity="error" variant="accent" closable>` — không dùng toast cho load error
- Toast chỉ dùng cho action success/error (toggle, delete, ...)
