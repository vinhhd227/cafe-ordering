<script setup>
import { onMounted, ref, computed } from 'vue'
import { VueDraggable } from 'vue-draggable-plus'
import { getCategory, reorderCategories } from '@/services/category.service'
import CreateCategoryDrawer from '@/components/products/CreateCategoryDrawer.vue'

const props = defineProps({
  search: { type: String, default: '' },
  viewMode: { type: String, default: 'list' },
})

const { t } = useI18n()
const router = useRouter()
const { can } = usePermission()
const toast = useToast()

// ── State ─────────────────────────────────────────────────────────────
const loading = ref(false)
const errorMessage = ref('')
const allCategories = ref([])

const filteredCategories = computed(() => {
  const q = props.search.trim().toLowerCase()
  if (!q) return allCategories.value
  return allCategories.value.filter(c => c.name.toLowerCase().includes(q))
})

// ── Load ──────────────────────────────────────────────────────────────
const loadCategories = async () => {
  loading.value = true
  errorMessage.value = ''
  try {
    const res = await getCategory()
    const raw = res?.data
    allCategories.value = Array.isArray(raw) ? raw
      : Array.isArray(raw?.value) ? raw.value
      : Array.isArray(raw?.items) ? raw.items
      : []
  } catch (err) {
    errorMessage.value = err?.response?.data?.message || t('products.list.error')
  } finally {
    loading.value = false
  }
}

onMounted(loadCategories)

// ── Drag-to-reorder ───────────────────────────────────────────────────
const onDragEnd = async () => {
  try {
    await reorderCategories(allCategories.value.map(c => c.id))
  } catch {
    toast.add({ severity: 'error', summary: t('products.mobile.reorderError'), life: 3000 })
    await loadCategories()
  }
}

// ── Create drawer ─────────────────────────────────────────────────────
const drawerVisible = ref(false)
const openCreateDrawer = () => { drawerVisible.value = true }
</script>

<template>
  <div class="tw:flex tw:flex-col">

    <!-- ── Error ──────────────────────────────────────────────────── -->
    <prime-alert
      v-if="errorMessage"
      severity="error"
      variant="accent"
      closable
      class="tw:mx-4 tw:mt-2"
      @close="errorMessage = ''"
    >
      {{ errorMessage }}
    </prime-alert>

    <!-- ── Skeleton ───────────────────────────────────────────────── -->
    <template v-if="loading">
      <div
        v-for="n in 5"
        :key="n"
        class="tw:flex tw:items-center tw:gap-3 tw:px-4 tw:py-3 tw:bg-white tw:dark:bg-neutral-900 tw:border-b tw:border-slate-100 tw:dark:border-white/5"
      >
        <prime-skeleton width="1.5rem" height="1.5rem" class="tw:shrink-0" />
        <prime-skeleton width="3.5rem" height="3.5rem" border-radius="10px" class="tw:shrink-0" />
        <div class="tw:flex-1 tw:space-y-2">
          <prime-skeleton width="50%" height="0.9rem" />
          <prime-skeleton width="30%" height="0.75rem" />
        </div>
        <prime-skeleton width="2.75rem" height="2.75rem" border-radius="10px" class="tw:shrink-0" />
      </div>
    </template>

    <!-- ── Category list ─────────────────────────────────────────── -->
    <template v-else>
      <VueDraggable
        v-model="allCategories"
        handle=".drag-handle"
        :animation="150"
        :disabled="!!props.search"
        class="tw:space-y-1 tw:px-4 tw:pt-2"
        @end="onDragEnd"
      >
        <div
          v-for="cat in allCategories"
          v-show="!props.search || cat.name.toLowerCase().includes(props.search.trim().toLowerCase())"
          :key="cat.id"
          class="tw:flex tw:items-center tw:gap-3 tw:px-4 tw:py-3 tw:rounded-xl tw:select-none tw:cursor-pointer tw:active:bg-slate-50 tw:dark:active:bg-white/5"
          :class="[bgGlass, borderGlass]"
          @click="router.push({ name: 'categoryProducts', params: { id: cat.id } })"
        >
          <!-- Drag handle -->
          <div
            class="drag-handle tw:shrink-0 tw:touch-none tw:text-xl"
            :class="props.search
              ? 'tw:text-slate-200 tw:dark:text-white/10 tw:cursor-default'
              : 'tw:text-slate-300 tw:dark:text-white/20 tw:cursor-grab tw:active:cursor-grabbing'"
            @click.stop
          >
            <iconify icon="ph:arrows-out-cardinal-bold" />
          </div>

          <!-- Thumbnail -->
          <div class="tw:shrink-0 tw:w-14 tw:h-14 tw:rounded-xl tw:overflow-hidden tw:bg-primary-50 tw:dark:bg-primary/10">
            <img
              v-if="cat.imageUrl"
              :src="cat.imageUrl"
              :alt="cat.name"
              class="tw:w-full tw:h-full tw:object-cover"
            />
            <div v-else class="tw:w-full tw:h-full tw:flex tw:items-center tw:justify-center">
              <iconify icon="ph:tag-bold" class="tw:text-xl tw:text-primary-400" />
            </div>
          </div>

          <!-- Info -->
          <div class="tw:flex-1 tw:min-w-0">
            <p class="tw:font-semibold tw:text-lg tw:leading-tight tw:text-slate-800 tw:dark:text-white">
              {{ cat.name }}
            </p>
            <p class="tw:text-xs tw:text-primary-500 tw:dark:text-primary-400 tw:mt-0.5">
              {{ cat.productCount != null
                ? `${cat.productCount} ${t('products.mobile.productCount')}`
                : cat.description || '' }}
            </p>
          </div>
        </div>
      </VueDraggable>

      <!-- Empty -->
      <div
        v-if="filteredCategories.length === 0"
        class="tw:flex tw:flex-col tw:items-center tw:justify-center tw:py-20 tw:gap-3"
      >
        <iconify icon="ph:tag-bold" class="tw:text-5xl tw:text-slate-300 tw:dark:text-white/20" />
        <p class="tw:text-sm tw:text-slate-400 tw:dark:text-slate-500">{{ t('products.mobile.empty') }}</p>
      </div>
    </template>

    <!-- Spacer cho nút cố định bên dưới -->
    <div class="tw:h-24" />

    <!-- ── Nút tạo danh mục (fixed bottom) ───────────────────────── -->
    <div
      v-if="can('product.create')"
      class="tw:fixed tw:bottom-6 tw:left-4 tw:right-4 tw:z-20"
    >
      <prime-button
        class="tw:w-full tw:py-3.5!"
        rounded
        severity="primary"
        @click="openCreateDrawer"
      >
        <iconify icon="ph:plus-bold" class="tw:text-base" />
        <span class="tw:font-semibold">{{ t('products.mobile.createCategory') }}</span>
      </prime-button>
    </div>

    <!-- ── Create drawer ─────────────────────────────────────────── -->
    <CreateCategoryDrawer v-model:visible="drawerVisible" @created="loadCategories" />

  </div>
</template>
