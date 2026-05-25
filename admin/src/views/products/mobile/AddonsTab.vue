<script setup>
import { ref, computed, onMounted } from 'vue'
import {
  getProductOptionGroups,
  deleteProductOptionGroup,
} from '@/services/product-option-group.service'
import AddonsListView from './addons/AddonsListView.vue'
import AddonsCreateWizard from './addons/AddonsCreateWizard.vue'
import AddonsEditView from './addons/AddonsEditView.vue'

const props = defineProps({
  search: { type: String, default: '' },
  viewMode: { type: String, default: 'list' },
})

const { t } = useI18n()
const toast = useToast()

// ── Groups list ───────────────────────────────────────────────────────
const loading = ref(false)
const groups = ref([])
const mode = ref('list') // 'list' | 'create' | 'edit'
const editingGroup = ref(null)

const filteredGroups = computed(() => {
  const q = props.search.trim().toLowerCase()
  if (!q) return groups.value
  return groups.value.filter(g => g.name.toLowerCase().includes(q))
})

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

// ── Navigation ────────────────────────────────────────────────────────
const openEdit = (group) => {
  editingGroup.value = group
  mode.value = 'edit'
}

const backToList = () => {
  mode.value = 'list'
  editingGroup.value = null
}

// ── Delete (shared between list + edit) ───────────────────────────────
const deleteTarget = ref(null)
const deleteDrawerVisible = ref(false)
const deleting = ref(false)

const handleDeleteRequest = (group) => {
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

onMounted(loadGroups)
</script>

<template>
  <div class="tw:flex tw:flex-col tw:min-h-full">

    <!-- Delete confirmation drawer -->
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

    <!-- List mode -->
    <AddonsListView
      v-if="mode === 'list'"
      :groups="filteredGroups"
      :loading="loading"
      @create="mode = 'create'"
      @edit="openEdit"
      @delete="handleDeleteRequest"
    />

    <!-- Create mode -->
    <AddonsCreateWizard
      v-if="mode === 'create'"
      @back="backToList"
      @created="() => { backToList(); loadGroups() }"
    />

    <!-- Edit mode -->
    <AddonsEditView
      v-if="mode === 'edit' && editingGroup"
      :group="editingGroup"
      @back="backToList"
      @submitted="loadGroups"
      @delete="handleDeleteRequest"
    />

  </div>
</template>
