<script setup>
import { ref, onMounted } from 'vue'
import {
  getProductOptionGroupById,
  updateProductOptionGroup,
  linkGroupToProducts,
  unlinkGroupFromProduct,
} from '@/services/product-option-group.service'
import AddonsGroupForm from './AddonsGroupForm.vue'
import AddonProductPicker from './AddonProductPicker.vue'

const props = defineProps({
  group: { type: Object, required: true },
})
const emit = defineEmits(['back', 'submitted', 'delete'])

const { t } = useI18n()
const toast = useToast()

const editTab = ref('info') // 'info' | 'links' | 'link-picker'
const loading = ref(false)
const submitting = ref(false)
const fullGroup = ref(null)
const linkedProducts = ref([])
const unlinkingId = ref(null)

const loadFullGroup = async () => {
  loading.value = true
  try {
    const res = await getProductOptionGroupById(props.group.id)
    fullGroup.value = res?.data
    linkedProducts.value = fullGroup.value?.linkedProducts ?? []
  } catch {
    toast.add({ severity: 'error', summary: t('products.mobile.addons.loadError'), life: 3000 })
  } finally {
    loading.value = false
  }
}

const handleFormSubmit = async (formData) => {
  submitting.value = true
  try {
    await updateProductOptionGroup(props.group.id, formData)
    toast.add({ severity: 'success', summary: t('productOptionGroups.detail.updateSuccess'), life: 2500 })
    emit('submitted')
    emit('back')
  } catch {
    toast.add({ severity: 'error', summary: t('products.mobile.addons.updateError'), life: 3000 })
  } finally {
    submitting.value = false
  }
}

const handleUnlink = async (productId) => {
  if (unlinkingId.value === productId) return
  unlinkingId.value = productId
  try {
    await unlinkGroupFromProduct(props.group.id, productId)
    linkedProducts.value = linkedProducts.value.filter(p => p.id !== productId)
    emit('submitted') // refresh parent list count
  } catch {
    toast.add({ severity: 'error', summary: t('products.mobile.addons.unlinkError'), life: 3000 })
  } finally {
    unlinkingId.value = null
  }
}

const handleLinkPickerConfirm = async (selectedIds) => {
  submitting.value = true
  try {
    const selectedSet = new Set(selectedIds)
    const currentIds = new Set(linkedProducts.value.map(p => p.id))
    const toAdd = selectedIds.filter(id => !currentIds.has(id))
    const toRemove = [...currentIds].filter(id => !selectedSet.has(id))

    if (toAdd.length > 0)
      await linkGroupToProducts(props.group.id, toAdd)
    for (const productId of toRemove)
      await unlinkGroupFromProduct(props.group.id, productId)

    // Reload to get fresh linked products list
    const res = await getProductOptionGroupById(props.group.id)
    linkedProducts.value = res?.data?.linkedProducts ?? []
    emit('submitted') // refresh parent list count

    editTab.value = 'links'
  } catch {
    toast.add({ severity: 'error', summary: t('products.mobile.addons.linkError'), life: 3000 })
  } finally {
    submitting.value = false
  }
}

const handleBack = () => {
  if (editTab.value === 'link-picker') {
    editTab.value = 'links'
  } else {
    emit('back')
  }
}

onMounted(loadFullGroup)
</script>

<template>
  <div class="tw:fixed tw:inset-0 tw:z-30 tw:flex tw:flex-col tw:bg-slate-50 tw:dark:bg-neutral-950">

    <!-- Top bar -->
    <div class="tw:shrink-0 tw:flex tw:items-center tw:gap-3 tw:px-4 tw:py-3 tw:bg-white tw:dark:bg-neutral-900 tw:border-b tw:border-slate-100 tw:dark:border-white/10">
      <button
        type="button"
        class="tw:w-8 tw:h-8 tw:flex tw:items-center tw:justify-center tw:rounded-lg tw:bg-transparent tw:border-0 tw:cursor-pointer tw:text-muted tw:active:bg-black/5 tw:dark:active:bg-white/5"
        @click="handleBack"
      >
        <iconify icon="ph:arrow-left-bold" class="tw:text-lg" />
      </button>
      <h2 class="tw:font-semibold tw:text-slate-800 tw:dark:text-white tw:text-base tw:flex-1">
        {{ editTab === 'link-picker' ? t('products.mobile.addons.pickProducts') : t('products.mobile.addons.editTitle') }}
      </h2>
    </div>

    <!-- Tab bar (hidden in link-picker sub-view) -->
    <div
      v-if="editTab !== 'link-picker'"
      class="tw:shrink-0 tw:flex tw:bg-white tw:dark:bg-neutral-900 tw:border-b tw:border-slate-100 tw:dark:border-white/10"
    >
      <button
        type="button"
        class="tw:flex-1 tw:py-2.5 tw:text-lg tw:font-medium tw:border-0 tw:bg-transparent tw:cursor-pointer tw:transition-colors"
        :class="editTab === 'info'
          ? 'tw:text-primary-600 tw:dark:text-primary-400 tw:border-b-2 tw:border-primary-500'
          : 'tw:text-slate-500 tw:dark:text-slate-400'"
        @click="editTab = 'info'"
      >
        {{ t('products.mobile.addons.tabInfo') }}
      </button>
      <button
        type="button"
        class="tw:flex-1 tw:py-2.5 tw:text-lg tw:font-medium tw:border-0 tw:bg-transparent tw:cursor-pointer tw:transition-colors"
        :class="editTab === 'links'
          ? 'tw:text-primary-600 tw:dark:text-primary-400 tw:border-b-2 tw:border-primary-500'
          : 'tw:text-slate-500 tw:dark:text-slate-400'"
        @click="editTab = 'links'"
      >
        {{ t('products.mobile.addons.tabLinks') }}
        <span class="tw:ml-1">({{ linkedProducts.length }})</span>
      </button>
    </div>

    <!-- Loading state -->
    <template v-if="loading">
      <div class="tw:flex-1 tw:flex tw:flex-col tw:gap-3 tw:p-4">
        <prime-skeleton width="100%" height="5rem" border-radius="12px" />
        <prime-skeleton width="100%" height="8rem" border-radius="12px" />
        <prime-skeleton width="100%" height="6rem" border-radius="12px" />
      </div>
    </template>

    <!-- TAB: Info -->
    <template v-else-if="editTab === 'info'">
      <AddonsGroupForm
        v-if="fullGroup"
        mode="edit"
        :initial-name="fullGroup.name"
        :initial-values="(fullGroup.values ?? []).map(v => ({ name: v.name, price: v.price, costPrice: v.costPrice ?? null }))"
        :initial-is-required="fullGroup.isRequired"
        :initial-allow-multiple="fullGroup.allowMultiple"
        :initial-allow-quantity="fullGroup.allowQuantity"
        :submitting="submitting"
        @submit="handleFormSubmit"
        @delete="emit('delete', group)"
      />
    </template>

    <!-- TAB: Links -->
    <template v-else-if="editTab === 'links'">
      <div class="tw:flex-1 tw:overflow-y-auto">

        <!-- Add product button -->
        <div class="tw:px-4 tw:py-3">
          <button
            type="button"
            class="tw:w-full tw:flex tw:items-center tw:justify-center tw:gap-2 tw:py-3 tw:rounded-xl tw:border tw:border-dashed tw:border-primary-400 tw:dark:border-primary-600 tw:bg-transparent tw:cursor-pointer tw:text-primary-600 tw:dark:text-primary-400 tw:text-sm tw:font-medium tw:active:bg-primary-50 tw:dark:active:bg-primary-900/20"
            @click="editTab = 'link-picker'"
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

    <!-- SUB-VIEW: Link picker -->
    <template v-else-if="editTab === 'link-picker'">
      <AddonProductPicker
        :submitting="submitting"
        :initial-selected-ids="linkedProducts.map(p => p.id)"
        @confirm="handleLinkPickerConfirm"
        @cancel="editTab = 'links'"
      />
    </template>

  </div>
</template>
