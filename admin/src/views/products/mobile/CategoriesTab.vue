<script setup>
import { onMounted, ref, computed } from 'vue'
import { VueDraggable } from 'vue-draggable-plus'
import { getCategory, createCategory, reorderCategories } from '@/services/category.service'
import { uploadImage } from '@/services/upload.service'

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
const createName = ref('')
const createDescription = ref('')
const createImageUrl = ref('')
const createLoading = ref(false)
const imageUploading = ref(false)
const fileInputRef = ref(null)

const openCreateDrawer = () => {
  createName.value = ''
  createDescription.value = ''
  createImageUrl.value = ''
  drawerVisible.value = true
}

const handleImageSelect = async (e) => {
  const file = e.target.files?.[0]
  if (!file) return
  imageUploading.value = true
  try {
    const res = await uploadImage(file)
    createImageUrl.value = res?.data?.url ?? ''
  } catch {
    // ignore — category can be created without image
  } finally {
    imageUploading.value = false
    e.target.value = ''
  }
}

const submitCreate = async () => {
  if (!createName.value.trim() || createLoading.value) return
  createLoading.value = true
  try {
    await createCategory({
      name: createName.value.trim(),
      description: createDescription.value.trim() || null,
      imageUrl: createImageUrl.value || null,
    })
    drawerVisible.value = false
    await loadCategories()
  } catch (err) {
    toast.add({
      severity: 'error',
      summary: err?.response?.data?.message || t('categories.create.error'),
      life: 3000,
    })
  } finally {
    createLoading.value = false
  }
}
</script>

<template>
  <div class="tw:flex tw:flex-col">

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
        class="tw:space-y-1 tw:px-3 tw:pt-2"
        @end="onDragEnd"
      >
        <div
          v-for="cat in allCategories"
          v-show="!props.search || cat.name.toLowerCase().includes(props.search.trim().toLowerCase())"
          :key="cat.id"
          class="tw:flex tw:items-center tw:gap-3 tw:px-3 tw:py-3 tw:bg-white tw:dark:bg-neutral-900 tw:rounded-xl tw:select-none tw:cursor-pointer tw:active:bg-slate-50 tw:dark:active:bg-white/5"
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
          <div class="tw:shrink-0 tw:w-14 tw:h-14 tw:rounded-xl tw:overflow-hidden tw:bg-slate-100 tw:dark:bg-white/10">
            <img
              v-if="cat.imageUrl"
              :src="cat.imageUrl"
              :alt="cat.name"
              class="tw:w-full tw:h-full tw:object-cover"
            />
            <div v-else class="tw:w-full tw:h-full tw:flex tw:items-center tw:justify-center">
              <iconify icon="ph:tag-bold" class="tw:text-xl tw:text-slate-400" />
            </div>
          </div>

          <!-- Info -->
          <div class="tw:flex-1 tw:min-w-0">
            <p class="tw:font-semibold tw:text-sm tw:leading-tight tw:text-slate-800 tw:dark:text-white">
              {{ cat.name }}
            </p>
            <p class="tw:text-xs tw:text-slate-500 tw:dark:text-slate-400 tw:mt-0.5">
              {{ cat.productCount != null
                ? `${cat.productCount} ${t('products.mobile.productCount')}`
                : cat.description || '' }}
            </p>
          </div>

          <!-- Edit button -->
          <button
            type="button"
            class="tw:shrink-0 tw:w-11 tw:h-11 tw:rounded-xl tw:border tw:border-slate-300 tw:dark:border-white/20 tw:bg-transparent tw:flex tw:items-center tw:justify-center tw:cursor-pointer tw:transition-colors tw:active:bg-slate-100 tw:dark:active:bg-white/10"
            @click.stop="router.push({ name: 'categoriesDetail', params: { id: cat.id } })"
          >
            <iconify icon="ph:pencil-bold" class="tw:text-lg tw:text-slate-500 tw:dark:text-slate-400" />
          </button>
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
        class="tw:w-full tw:rounded-full! tw:py-3.5!"
        severity="primary"
        @click="openCreateDrawer"
      >
        <iconify icon="ph:plus-bold" class="tw:text-base" />
        <span class="tw:font-semibold">{{ t('products.mobile.createCategory') }}</span>
      </prime-button>
    </div>

    <!-- ── Create drawer ─────────────────────────────────────────── -->
    <prime-drawer
      v-model:visible="drawerVisible"
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
              {{ t('products.mobile.createCategory') }}
            </h3>
            <button
              type="button"
              class="tw:w-8 tw:h-8 tw:flex tw:items-center tw:justify-center tw:rounded-full tw:bg-transparent tw:border-0 tw:cursor-pointer tw:text-slate-400 tw:active:bg-slate-100 tw:dark:active:bg-white/10"
              @click="drawerVisible = false"
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
              v-if="createImageUrl"
              :src="createImageUrl"
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
            @click="fileInputRef?.click()"
          >
            <iconify
              v-if="!imageUploading"
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
            ref="fileInputRef"
            type="file"
            accept="image/*"
            class="tw:hidden"
            @change="handleImageSelect"
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
            v-model="createName"
            type="text"
            :placeholder="t('products.mobile.categoryNamePlaceholder')"
            class="tw:w-full tw:bg-transparent tw:border-0 tw:border-b-2 tw:border-emerald-500 tw:py-2 tw:pr-6 tw:text-sm tw:outline-none tw:text-slate-800 tw:dark:text-white tw:placeholder-slate-400 tw:dark:placeholder-white/30"
            @keyup.enter="submitCreate"
          />
          <button
            v-if="createName"
            type="button"
            class="tw:absolute tw:right-0 tw:top-1/2 tw:-translate-y-1/2 tw:text-slate-400 tw:hover:text-slate-600 tw:dark:hover:text-white tw:border-0 tw:bg-transparent tw:cursor-pointer tw:p-0.5"
            @click="createName = ''"
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
            v-model="createDescription"
            type="text"
            :placeholder="t('products.mobile.categoryDescriptionPlaceholder')"
            class="tw:w-full tw:bg-transparent tw:border-0 tw:border-b tw:border-slate-300 tw:dark:border-white/20 tw:py-2 tw:pr-6 tw:text-sm tw:outline-none tw:text-slate-800 tw:dark:text-white tw:placeholder-slate-400 tw:dark:placeholder-white/30"
          />
          <button
            v-if="createDescription"
            type="button"
            class="tw:absolute tw:right-0 tw:top-1/2 tw:-translate-y-1/2 tw:text-slate-400 tw:hover:text-slate-600 tw:dark:hover:text-white tw:border-0 tw:bg-transparent tw:cursor-pointer tw:p-0.5"
            @click="createDescription = ''"
          >
            <iconify icon="ph:x-circle-fill" class="tw:text-base" />
          </button>
        </div>
      </div>

      <!-- Submit button -->
      <button
        type="button"
        class="tw:w-full tw:rounded-2xl tw:py-4 tw:text-sm tw:font-semibold tw:transition-colors tw:border-0 tw:cursor-pointer"
        :class="createName.trim()
          ? 'tw:bg-emerald-500 tw:text-white'
          : 'tw:bg-slate-100 tw:dark:bg-white/5 tw:text-slate-400 tw:dark:text-white/30 tw:cursor-not-allowed'"
        :disabled="!createName.trim() || createLoading"
        @click="submitCreate"
      >
        <iconify
          v-if="createLoading"
          icon="ph:spinner-bold"
          class="tw:animate-spin tw:text-base"
        />
        <span v-else>{{ t('products.mobile.createSubmit') }}</span>
      </button>
    </prime-drawer>

  </div>
</template>
