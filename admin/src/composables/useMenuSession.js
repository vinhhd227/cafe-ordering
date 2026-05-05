import { getAdminMenu } from '@/services/menu.service'
import { listTables, getOrCreateSession } from '@/services/table.service'

export function useMenuSession() {
  const tables = ref([])
  const menuCategories = ref([])
  const loadingMenu = ref(false)
  const loadingTables = ref(false)

  const selectedTableId = ref(null)
  const sessionId = ref(null)
  const sessionHadExisting = ref(false)
  const sessionLoading = ref(false)
  const sessionError = ref('')

  const searchQuery = ref('')
  const collapsedCategories = ref({})

  const toggleCategory = (id) => {
    collapsedCategories.value[id] = !collapsedCategories.value[id]
  }

  const visibleCategories = computed(() =>
    menuCategories.value
      .filter((c) => c.isActive)
      .map((c) => {
        let products = (c.products ?? []).filter((p) => p.isActive)
        if (searchQuery.value.trim()) {
          const q = searchQuery.value.toLowerCase()
          products = products.filter(
            (p) =>
              p.name.toLowerCase().includes(q) ||
              (p.description?.toLowerCase().includes(q) ?? false),
          )
        }
        return { ...c, filteredProducts: products }
      })
      .filter((c) => c.filteredProducts.length > 0),
  )

  const orderLabel = computed(() => {
    const table = tables.value.find((t) => t.id === selectedTableId.value)
    return table ? `Table ${table.code}` : ''
  })

  const onTableSelect = async (tableId) => {
    if (!tableId) {
      sessionId.value = null
      sessionHadExisting.value = false
      return
    }
    sessionLoading.value = true
    sessionId.value = null
    sessionError.value = ''
    try {
      const res = await getOrCreateSession(tableId)
      const existed = !!tables.value.find((t) => t.id === tableId)?.activeSessionId
      sessionId.value = res.data.sessionId
      sessionHadExisting.value = existed
    } catch {
      sessionError.value = 'Could not get session for table.'
    } finally {
      sessionLoading.value = false
    }
  }

  const loadData = async () => {
    loadingTables.value = true
    loadingMenu.value = true
    try {
      const [tablesRes, menuRes] = await Promise.all([listTables(), getAdminMenu()])
      tables.value = (tablesRes.data ?? []).filter((t) => t.isActive)
      menuCategories.value = menuRes.data ?? []
    } finally {
      loadingTables.value = false
      loadingMenu.value = false
    }
  }

  return {
    tables,
    menuCategories,
    loadingMenu,
    loadingTables,
    selectedTableId,
    sessionId,
    sessionHadExisting,
    sessionLoading,
    sessionError,
    searchQuery,
    collapsedCategories,
    visibleCategories,
    orderLabel,
    toggleCategory,
    onTableSelect,
    loadData,
  }
}
