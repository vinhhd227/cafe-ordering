const STORAGE_KEY = 'admin_widget_settings'

function loadFromStorage() {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    return raw ? JSON.parse(raw) : {}
  } catch {
    return {}
  }
}

/**
 * useWidgetSettings — lưu trạng thái hiển thị widget theo từng trang vào localStorage.
 *
 * @param {string} pageKey - khóa định danh trang (vd: 'orders-list', 'products')
 * @param {Array<{id: string, label: string}>} widgetDefs - danh sách widget của trang
 */
export function useWidgetSettings(pageKey, widgetDefs) {
  const allSettings = ref(loadFromStorage())

  // hidden[widgetId] = true nghĩa là widget đó đang ẩn
  const hidden = computed(() => allSettings.value[pageKey] ?? {})

  const isVisible = (id) => !hidden.value[id]

  const toggle = (id) => {
    if (!allSettings.value[pageKey]) allSettings.value[pageKey] = {}
    if (allSettings.value[pageKey][id]) {
      delete allSettings.value[pageKey][id]
    } else {
      allSettings.value[pageKey][id] = true
    }
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(allSettings.value))
    } catch {
      // localStorage full — ignore
    }
  }

  const hiddenCount = computed(() => Object.keys(hidden.value).length)

  const widgets = computed(() =>
    widgetDefs.map((d) => ({
      ...d,
      visible: isVisible(d.id),
    })),
  )

  return { isVisible, toggle, hiddenCount, widgets }
}
