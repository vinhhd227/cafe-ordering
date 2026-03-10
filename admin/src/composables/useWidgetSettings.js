const STORAGE_KEY = 'admin_widget_settings'

function loadFromStorage() {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    return raw ? JSON.parse(raw) : {}
  } catch {
    return {}
  }
}

function persist(allSettings) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(allSettings))
  } catch {
    // localStorage full — ignore
  }
}

/**
 * useWidgetSettings — lưu trạng thái hiển thị widget và số cột trên một hàng theo từng trang vào localStorage.
 *
 * @param {string} pageKey - khóa định danh trang (vd: 'orders-list', 'products')
 * @param {Array<{id: string, label: string}>} widgetDefs - danh sách widget của trang
 * @param {Object} [options]
 * @param {number} [options.defaultCols=2] - số cột mặc định nếu chưa có giá trị lưu
 */
export function useWidgetSettings(pageKey, widgetDefs, { defaultCols = 2 } = {}) {
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
    persist(allSettings.value)
  }

  const hiddenCount = computed(() => Object.keys(hidden.value).length)

  const widgets = computed(() =>
    widgetDefs.map((d) => ({
      ...d,
      visible: isVisible(d.id),
    })),
  )

  // ── Columns per row ──────────────────────────────────────────────
  // Lưu riêng dưới key `pageKey:cols` để không conflict với widgetId
  const colsKey = pageKey + ':cols'
  const colsPerRow = ref(
    typeof allSettings.value[colsKey] === 'number'
      ? allSettings.value[colsKey]
      : defaultCols,
  )

  const setColsPerRow = (n) => {
    colsPerRow.value = n
    allSettings.value[colsKey] = n
    persist(allSettings.value)
  }

  return { isVisible, toggle, hiddenCount, widgets, colsPerRow, setColsPerRow }
}
