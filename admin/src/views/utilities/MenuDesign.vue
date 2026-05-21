<script setup>
import { toPng } from 'html-to-image'
import { jsPDF } from 'jspdf'
import html2canvas from 'html2canvas'
import { getProductTree } from '@/services/product.service'
import { listSavedMenus, createSavedMenu, deleteSavedMenu } from '@/services/savedMenu.service'

const { t } = useI18n()
const toast = useToast()
const confirm = useConfirm()

// Config
const cafeName = ref('Cafe của bạn')
const slogan = ref('')
const primaryColor = ref('#10b981')
const layout = ref('2col')

const layoutOptions = computed(() => [
  { label: t('utilities.menuDesign.layout1col'), value: '1col' },
  { label: t('utilities.menuDesign.layout2col'), value: '2col' },
  { label: t('utilities.menuDesign.layoutLandscape'), value: 'landscape' },
])

const isLandscape = computed(() => layout.value === 'landscape')

// Template
const menuTemplate = ref('default')
const templateOptions = [
  { value: 'default', label: 'Mặc định' },
  { value: 'classic', label: 'Cổ điển' },
]

// Font
const menuFont = ref('Georgia, serif')
const fontOptions = ref([
  // System fonts
  { label: 'Georgia (Serif)', value: 'Georgia, serif' },
  { label: 'Times New Roman', value: "'Times New Roman', Times, serif" },
  { label: 'Trebuchet MS', value: "'Trebuchet MS', sans-serif" },
  { label: 'Arial', value: 'Arial, Helvetica, sans-serif' },
  { label: 'System UI', value: 'system-ui, sans-serif' },
  { label: 'Courier New', value: "'Courier New', Courier, monospace" },
  // Google Fonts
  { label: 'Playfair Display', value: "'Playfair Display', serif", googleFamily: 'Playfair+Display:wght@400;700;900' },
  { label: 'Lora', value: "'Lora', serif", googleFamily: 'Lora:wght@400;700' },
  { label: 'Cormorant Garamond', value: "'Cormorant Garamond', serif", googleFamily: 'Cormorant+Garamond:wght@400;700' },
  { label: 'Montserrat', value: "'Montserrat', sans-serif", googleFamily: 'Montserrat:wght@400;700;900' },
  { label: 'Raleway', value: "'Raleway', sans-serif", googleFamily: 'Raleway:wght@400;700' },
  { label: 'Dancing Script', value: "'Dancing Script', cursive", googleFamily: 'Dancing+Script:wght@400;700' },
])

const loadedGoogleFonts = new Set()
const ensureGoogleFont = async (googleFamily) => {
  if (loadedGoogleFonts.has(googleFamily)) return
  loadedGoogleFonts.add(googleFamily)
  const link = document.createElement('link')
  link.rel = 'stylesheet'
  link.href = `https://fonts.googleapis.com/css2?family=${googleFamily}&display=swap`
  document.head.appendChild(link)
  await document.fonts.ready
}

watch(menuFont, async (val) => {
  const opt = fontOptions.value.find(f => f.value === val)
  if (opt?.googleFamily) await ensureGoogleFont(opt.googleFamily)
})

const fontFileInput = ref(null)
const handleFontUpload = async (event) => {
  const file = event.target.files?.[0]
  if (!file) return
  const fontName = `UploadedFont-${Date.now()}`
  const base64 = await new Promise(resolve => {
    const reader = new FileReader()
    reader.onload = e => resolve(e.target.result)
    reader.readAsDataURL(file)
  })
  const style = document.createElement('style')
  style.textContent = `@font-face { font-family: '${fontName}'; src: url('${base64}'); }`
  document.head.appendChild(style)
  const face = new FontFace(fontName, `url(${base64})`)
  await face.load()
  document.fonts.add(face)
  const label = file.name.replace(/\.[^.]+$/, '')
  fontOptions.value.push({ label, value: `'${fontName}', sans-serif`, isCustom: true })
  menuFont.value = `'${fontName}', sans-serif`
  event.target.value = ''
}

// Data
const loading = ref(false)
const errorMessage = ref('')
const categories = ref([]) // [{ id, name, products: [{ id, name, price, isActive }] }]
const categoryOrder = ref([]) // array of category IDs controlling display order

// Selected product ids
const selectedIds = ref(new Set())

const loadTree = async () => {
  loading.value = true
  errorMessage.value = ''
  try {
    const res = await getProductTree()
    categories.value = res.data ?? []
    categoryOrder.value = categories.value.map(c => c.id)
    // default: select all active products
    categories.value.forEach(cat => {
      cat.products?.forEach(p => {
        if (p.isActive) selectedIds.value.add(p.id)
      })
    })
  } catch {
    errorMessage.value = t('utilities.menuDesign.loadError')
  } finally {
    loading.value = false
  }
}

const orderedCategories = computed(() =>
  categoryOrder.value
    .map(id => categories.value.find(c => c.id === id))
    .filter(Boolean)
)

const moveCategoryUp = (id) => {
  const idx = categoryOrder.value.indexOf(id)
  if (idx <= 0) return
  const arr = [...categoryOrder.value]
  ;[arr[idx - 1], arr[idx]] = [arr[idx], arr[idx - 1]]
  categoryOrder.value = arr
}

const moveCategoryDown = (id) => {
  const idx = categoryOrder.value.indexOf(id)
  if (idx === -1 || idx >= categoryOrder.value.length - 1) return
  const arr = [...categoryOrder.value]
  ;[arr[idx], arr[idx + 1]] = [arr[idx + 1], arr[idx]]
  categoryOrder.value = arr
}

onMounted(() => {
  loadTree()
  loadSavedMenusList()
})

// Helpers
const isCategorySelected = (cat) => cat.products?.every(p => selectedIds.value.has(p.id))
const isCategoryPartial = (cat) => cat.products?.some(p => selectedIds.value.has(p.id)) && !isCategorySelected(cat)

const toggleCategory = (cat) => {
  if (isCategorySelected(cat)) {
    cat.products?.forEach(p => selectedIds.value.delete(p.id))
  } else {
    cat.products?.forEach(p => selectedIds.value.add(p.id))
  }
}

const toggleProduct = (id) => {
  if (selectedIds.value.has(id)) selectedIds.value.delete(id)
  else selectedIds.value.add(id)
}

const selectAll = () => {
  categories.value.forEach(cat => cat.products?.forEach(p => selectedIds.value.add(p.id)))
}

const deselectAll = () => {
  selectedIds.value.clear()
}

const selectedCount = computed(() => selectedIds.value.size)

// Preview data — only selected products, in user-defined order
const previewCategories = computed(() =>
  orderedCategories.value
    .map(cat => ({
      ...cat,
      products: (cat.products ?? []).filter(p => selectedIds.value.has(p.id)),
    }))
    .filter(cat => cat.products.length > 0)
)

const formatPrice = (price) =>
  new Intl.NumberFormat('vi-VN').format(price) + 'đ'

// Split preview categories into 3 columns left→right (sequential, not interleaved)
const classicColumns = computed(() => {
  const cats = previewCategories.value
  const total = cats.length
  const col1Size = Math.ceil(total / 3)
  const col2Size = Math.ceil((total - col1Size) / 2)
  return [
    cats.slice(0, col1Size),
    cats.slice(col1Size, col1Size + col2Size),
    cats.slice(col1Size + col2Size),
  ]
})

// Export
const previewRef = ref(null)
const exporting = ref(false)

const downloadPng = async () => {
  if (!previewRef.value) return
  exporting.value = true
  try {
    await document.fonts.ready
    const dataUrl = await toPng(previewRef.value, { pixelRatio: 2, cacheBust: true })
    const link = document.createElement('a')
    link.download = `menu-${cafeName.value.trim() || 'cafe'}.png`
    link.href = dataUrl
    link.click()
  } catch {
    toast.add({ severity: 'error', summary: 'Không thể xuất ảnh', life: 3000 })
  } finally {
    exporting.value = false
  }
}

const downloadPdf = async () => {
  if (!previewRef.value) return
  exporting.value = true
  try {
    await document.fonts.ready
    const canvas = await html2canvas(previewRef.value, { scale: 2, useCORS: true })
    const imgData = canvas.toDataURL('image/png')
    const pdf = new jsPDF({ orientation: isLandscape.value ? 'landscape' : 'portrait', unit: 'px', format: 'a4' })
    const pdfWidth = pdf.internal.pageSize.getWidth()
    const pdfHeight = (canvas.height * pdfWidth) / canvas.width
    pdf.addImage(imgData, 'PNG', 0, 0, pdfWidth, pdfHeight)
    pdf.save(`menu-${cafeName.value.trim() || 'cafe'}.pdf`)
  } catch {
    toast.add({ severity: 'error', summary: 'Không thể xuất PDF', life: 3000 })
  } finally {
    exporting.value = false
  }
}

// Saved menus
const savedMenus = ref([])
const showSaveDialog = ref(false)
const saveMenuName = ref('')
const saving = ref(false)

const loadSavedMenusList = async () => {
  try {
    const res = await listSavedMenus()
    savedMenus.value = res.data ?? []
  } catch {
    // non-critical
  }
}

const openSaveDialog = () => {
  saveMenuName.value = cafeName.value || ''
  showSaveDialog.value = true
}

const confirmSave = async () => {
  if (!saveMenuName.value.trim()) return
  saving.value = true
  try {
    await createSavedMenu({
      name: saveMenuName.value.trim(),
      cafeName: cafeName.value,
      slogan: slogan.value,
      primaryColor: primaryColor.value,
      layout: layout.value,
      menuTemplate: menuTemplate.value,
      menuFont: menuFont.value,
      categoryOrderJson: JSON.stringify(categoryOrder.value),
      selectedProductIdsJson: JSON.stringify([...selectedIds.value]),
    })
    toast.add({ severity: 'success', summary: 'Đã lưu mẫu menu', life: 2500 })
    showSaveDialog.value = false
    await loadSavedMenusList()
  } catch {
    toast.add({ severity: 'error', summary: 'Lưu thất bại', life: 3000 })
  } finally {
    saving.value = false
  }
}

const applyMenu = (menu) => {
  cafeName.value = menu.cafeName
  slogan.value = menu.slogan
  primaryColor.value = menu.primaryColor
  layout.value = menu.layout
  menuTemplate.value = menu.menuTemplate
  const restoredFont = menu.menuFont || 'Georgia, serif'
  // Uploaded fonts không persist qua reload — fallback về Georgia nếu không còn trong danh sách
  menuFont.value = fontOptions.value.some(f => f.value === restoredFont) || !restoredFont.includes('UploadedFont-')
    ? restoredFont
    : 'Georgia, serif'

  const order = JSON.parse(menu.categoryOrderJson ?? '[]')
  const ids = JSON.parse(menu.selectedProductIdsJson ?? '[]')

  if (order.length > 0) {
    // keep any new categories at the end
    const extra = categoryOrder.value.filter(id => !order.includes(id))
    categoryOrder.value = [...order.filter(id => categoryOrder.value.includes(id)), ...extra]
  }

  selectedIds.value = new Set(ids)
  toast.add({ severity: 'success', summary: `Đã tải "${menu.name}"`, life: 2500 })
}

const confirmDeleteMenu = (menu) => {
  confirm.require({
    message: `Xóa mẫu "${menu.name}"?`,
    header: 'Xác nhận xóa',
    icon: 'ph:trash-bold',
    acceptSeverity: 'danger',
    acceptLabel: 'Xóa',
    rejectLabel: 'Hủy',
    accept: async () => {
      try {
        await deleteSavedMenu(menu.id)
        savedMenus.value = savedMenus.value.filter(m => m.id !== menu.id)
        toast.add({ severity: 'success', summary: 'Đã xóa', life: 2000 })
      } catch {
        toast.add({ severity: 'error', summary: 'Xóa thất bại', life: 3000 })
      }
    },
  })
}
</script>

<template>
  <section class="tw:space-y-8">
    <!-- Header -->
    <div>
      <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-primary-300">{{ t('nav.groups.utilities') }}</p>
      <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">{{ t('utilities.menuDesign.title') }}</h1>
      <p class="tw:mt-2 tw:text-sm tw:text-muted">{{ t('utilities.menuDesign.subtitle') }}</p>
    </div>

    <prime-alert v-if="errorMessage" severity="error" variant="accent" closable @close="errorMessage = ''">
      {{ errorMessage }}
    </prime-alert>

    <div class="tw:grid tw:grid-cols-1 tw:xl:grid-cols-[380px_1fr] tw:gap-8 tw:items-start">
      <!-- Left: Settings + product selection -->
      <div class="tw:space-y-6">
        <!-- Design settings -->
        <div :class="appCard" class="tw:rounded-2xl tw:p-5 tw:space-y-5">
          <p class="tw:text-sm tw:font-semibold">Thông tin & giao diện</p>

          <!-- Template selector -->
          <div class="tw:space-y-1.5">
            <p class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">Template</p>
            <div class="tw:grid tw:grid-cols-2 tw:gap-2">
              <prime-button
                v-for="tpl in templateOptions"
                :key="tpl.value"
                text
                class="tw:rounded-xl! tw:border-2! tw:p-3! tw:text-left! tw:transition-all! tw:h-auto! tw:w-full! tw:flex! tw:flex-col! tw:items-start! tw:justify-start!"
                :class="menuTemplate === tpl.value
                  ? 'tw:border-primary-500! tw:bg-primary-50! tw:dark:bg-primary-900/20!'
                  : 'tw:border-slate-200! tw:dark:border-white/10! tw:hover:border-slate-300!'"
                @click="menuTemplate = tpl.value"
              >
                <!-- Mini preview thumbnail -->
                <div
                  class="tw:w-full tw:h-12 tw:rounded-lg tw:mb-2 tw:overflow-hidden tw:flex tw:flex-col"
                  :class="tpl.value === 'classic' ? 'tw:bg-amber-50' : 'tw:bg-white tw:border tw:border-slate-100'"
                >
                  <div v-if="tpl.value === 'default'" class="tw:h-3 tw:w-full" :style="{ backgroundColor: primaryColor }"></div>
                  <div v-if="tpl.value === 'default'" class="tw:flex-1 tw:p-1 tw:space-y-0.5">
                    <div class="tw:h-1 tw:w-10 tw:rounded" :style="{ backgroundColor: primaryColor + '80' }"></div>
                    <div class="tw:h-px tw:w-full tw:bg-slate-200"></div>
                    <div class="tw:h-px tw:w-full tw:bg-slate-200"></div>
                  </div>
                  <div v-if="tpl.value === 'classic'" class="tw:flex-1 tw:flex tw:items-center tw:justify-center tw:gap-1 tw:px-1" style="background: #d8d3c9;">
                    <div style="flex: 1; background: #f5f1ea; height: 100%; padding: 3px 4px; display: flex; flex-direction: column; gap: 2px;">
                      <div style="height: 2px; background: #1a1a1a; width: 80%;"></div>
                      <div style="height: 1px; background: #888; width: 100%;"></div>
                      <div style="height: 1px; background: #888; width: 70%;"></div>
                    </div>
                    <div style="flex: 1; display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 2px;">
                      <div style="height: 3px; background: #1a1a1a; width: 60%;"></div>
                      <div style="height: 1px; background: #888; width: 80%;"></div>
                    </div>
                    <div style="flex: 1; background: #f5f1ea; height: 100%; padding: 3px 4px; display: flex; flex-direction: column; gap: 2px;">
                      <div style="height: 2px; background: #1a1a1a; width: 80%;"></div>
                      <div style="height: 1px; background: #888; width: 100%;"></div>
                      <div style="height: 1px; background: #888; width: 70%;"></div>
                    </div>
                  </div>
                </div>
                <span class="tw:text-xs tw:font-medium">{{ tpl.label }}</span>
              </prime-button>
            </div>
          </div>

          <div class="tw:space-y-1.5">
            <label for="cafe-name" class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">{{ t('utilities.menuDesign.cafeName') }}</label>
            <prime-input-text id="cafe-name" v-model="cafeName" :placeholder="t('utilities.menuDesign.cafeNamePlaceholder')" class="app-input tw:w-full" />
          </div>

          <div class="tw:space-y-1.5">
            <label for="slogan" class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">{{ t('utilities.menuDesign.slogan') }}</label>
            <prime-input-text id="slogan" v-model="slogan" :placeholder="t('utilities.menuDesign.sloganPlaceholder')" class="app-input tw:w-full" />
          </div>

          <div class="tw:grid tw:grid-cols-2 tw:gap-4">
            <div class="tw:space-y-1.5">
              <label for="primary-color" class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">{{ t('utilities.menuDesign.primaryColor') }}</label>
              <div class="tw:flex tw:items-center tw:gap-2">
                <input id="primary-color" type="color" v-model="primaryColor" class="tw:w-10 tw:h-10 tw:rounded-lg tw:border tw:border-slate-300 tw:dark:border-white/20 tw:cursor-pointer tw:bg-transparent" />
                <span class="tw:text-sm tw:font-mono tw:text-muted">{{ primaryColor }}</span>
              </div>
            </div>
            <div class="tw:space-y-1.5">
              <p class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">{{ t('utilities.menuDesign.layout') }}</p>
              <prime-select v-model="layout" :options="layoutOptions" option-label="label" option-value="value" class="app-input tw:w-full" />
            </div>
          </div>

          <div class="tw:space-y-1.5">
            <p class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">Font chữ</p>
            <div class="tw:flex tw:gap-2">
              <prime-select v-model="menuFont" :options="fontOptions" option-label="label" option-value="value" class="app-input tw:flex-1">
                <template #option="{ option }">
                  <span :style="{ fontFamily: option.value }">{{ option.label }}</span>
                </template>
                <template #value="{ value }">
                  <span :style="{ fontFamily: value }">{{ fontOptions.find(f => f.value === value)?.label ?? value }}</span>
                </template>
              </prime-select>
              <prime-button
                severity="secondary"
                outlined
                v-tooltip.top="'Tải font từ máy (.ttf, .otf, .woff)'"
                :class="btnIcon"
                @click="fontFileInput.click()"
              >
                <iconify icon="ph:upload-simple-bold" />
              </prime-button>
            </div>
            <input
              ref="fontFileInput"
              type="file"
              accept=".ttf,.otf,.woff,.woff2"
              class="tw:hidden"
              @change="handleFontUpload"
            />
          </div>
        </div>

        <!-- Product selection -->
        <div :class="appCard" class="tw:rounded-2xl tw:p-5 tw:space-y-4">
          <div class="tw:flex tw:items-center tw:justify-between">
            <p class="tw:text-sm tw:font-semibold">{{ t('utilities.menuDesign.selectProducts') }}</p>
            <div class="tw:flex tw:gap-2">
              <prime-button size="small" severity="secondary" outlined @click="selectAll">
                <iconify icon="ph:check-square-bold" />
                <span class="tw:text-xs">{{ t('utilities.menuDesign.selectAll') }}</span>
              </prime-button>
              <prime-button size="small" severity="secondary" outlined @click="deselectAll">
                <iconify icon="ph:square-bold" />
                <span class="tw:text-xs">{{ t('utilities.menuDesign.deselectAll') }}</span>
              </prime-button>
            </div>
          </div>

          <p class="tw:text-xs tw:text-muted">
            {{ t('utilities.menuDesign.selectedCount', { n: selectedCount }) }}
          </p>

          <div v-if="loading" class="tw:flex tw:justify-center tw:py-8">
            <iconify icon="ph:spinner-bold" class="tw:text-2xl tw:animate-spin tw:text-muted" />
          </div>

          <div v-else class="tw:space-y-3 tw:max-h-[400px] tw:overflow-y-auto tw:pr-1">
            <div v-for="(cat, idx) in orderedCategories" :key="cat.id" class="tw:space-y-1">
              <!-- Category row -->
              <div
                class="tw:flex tw:items-center tw:gap-2 tw:select-none tw:py-1 tw:px-2 tw:rounded-lg tw:hover:bg-slate-100 tw:dark:hover:bg-white/5"
              >
                <!-- Reorder buttons -->
                <div class="tw:flex tw:flex-col tw:gap-0.5 tw:flex-shrink-0">
                  <prime-button
                    text
                    class="tw:w-4! tw:h-4! tw:p-0! tw:min-w-0! tw:text-slate-400! tw:hover:text-slate-700! tw:dark:hover:text-white! tw:disabled:opacity-20!"
                    :disabled="idx === 0"
                    @click.stop="moveCategoryUp(cat.id)"
                  >
                    <iconify icon="ph:caret-up-bold" class="tw:text-xs" />
                  </prime-button>
                  <prime-button
                    text
                    class="tw:w-4! tw:h-4! tw:p-0! tw:min-w-0! tw:text-slate-400! tw:hover:text-slate-700! tw:dark:hover:text-white! tw:disabled:opacity-20!"
                    :disabled="idx === orderedCategories.length - 1"
                    @click.stop="moveCategoryDown(cat.id)"
                  >
                    <iconify icon="ph:caret-down-bold" class="tw:text-xs" />
                  </prime-button>
                </div>
                <div class="tw:flex tw:items-center tw:gap-2 tw:flex-1 tw:cursor-pointer" @click="toggleCategory(cat)">
                  <iconify
                    :icon="isCategorySelected(cat) ? 'ph:check-square-fill' : isCategoryPartial(cat) ? 'ph:minus-square-fill' : 'ph:square-bold'"
                    class="tw:text-lg tw:flex-shrink-0"
                    :style="{ color: isCategorySelected(cat) || isCategoryPartial(cat) ? primaryColor : undefined }"
                  />
                  <span class="tw:text-sm tw:font-semibold">{{ cat.name }}</span>
                  <span class="tw:text-xs tw:text-muted tw:ml-auto">{{ cat.products?.length }} sp</span>
                </div>
              </div>
              <!-- Product rows -->
              <div class="tw:ml-6 tw:space-y-0.5">
                <div
                  v-for="product in cat.products"
                  :key="product.id"
                  class="tw:flex tw:items-center tw:gap-2 tw:cursor-pointer tw:select-none tw:py-1 tw:px-2 tw:rounded-lg tw:hover:bg-slate-100 tw:dark:hover:bg-white/5"
                  @click="toggleProduct(product.id)"
                >
                  <iconify
                    :icon="selectedIds.has(product.id) ? 'ph:check-square-fill' : 'ph:square-bold'"
                    class="tw:text-base tw:flex-shrink-0"
                    :style="{ color: selectedIds.has(product.id) ? primaryColor : undefined }"
                  />
                  <span class="tw:text-sm tw:flex-1">{{ product.name }}</span>
                  <span class="tw:text-xs tw:text-muted">{{ formatPrice(product.price) }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Export + Save buttons -->
        <div class="tw:flex tw:gap-3">
          <prime-button
            severity="secondary"
            outlined
            class="tw:flex-1"
            :disabled="exporting || selectedCount === 0"
            @click="downloadPng"
          >
            <iconify :icon="exporting ? 'ph:spinner-bold' : 'ph:image-bold'" :class="{ 'tw:animate-spin': exporting }" />
            <span>{{ t('utilities.menuDesign.downloadPng') }}</span>
          </prime-button>
          <prime-button
            severity="success"
            class="tw:flex-1"
            :disabled="exporting || selectedCount === 0"
            @click="downloadPdf"
          >
            <iconify :icon="exporting ? 'ph:spinner-bold' : 'ph:file-pdf-bold'" :class="{ 'tw:animate-spin': exporting }" />
            <span>{{ t('utilities.menuDesign.downloadPdf') }}</span>
          </prime-button>
        </div>

        <prime-button severity="secondary" outlined fluid @click="openSaveDialog">
          <iconify icon="ph:floppy-disk-bold" />
          <span>{{ t('utilities.menuDesign.saveMenu') }}</span>
        </prime-button>

        <!-- Saved menus list -->
        <div v-if="savedMenus.length > 0" :class="appCard" class="tw:rounded-2xl tw:p-5 tw:space-y-3">
          <p class="tw:text-sm tw:font-semibold">{{ t('utilities.menuDesign.savedMenus') }}</p>
          <div class="tw:space-y-2 tw:max-h-[260px] tw:overflow-y-auto tw:pr-1">
            <div
              v-for="menu in savedMenus"
              :key="menu.id"
              class="tw:flex tw:items-center tw:gap-3 tw:rounded-xl tw:border tw:border-slate-200 tw:dark:border-white/10 tw:px-3 tw:py-2.5 tw:hover:bg-slate-50 tw:dark:hover:bg-white/5 tw:transition-colors"
            >
              <div class="tw:flex-1 tw:min-w-0">
                <p class="tw:text-sm tw:font-medium tw:truncate">{{ menu.name }}</p>
                <p class="tw:text-xs tw:text-muted tw:truncate">{{ menu.cafeName }} · {{ menu.layout }} · {{ menu.menuTemplate }}</p>
              </div>
              <prime-button :class="btnIcon" severity="secondary" outlined v-tooltip.top="t('utilities.menuDesign.loadMenu')" @click="applyMenu(menu)">
                <iconify icon="ph:arrow-counter-clockwise-bold" />
              </prime-button>
              <prime-button :class="btnIcon" severity="danger" outlined v-tooltip.top="t('common.delete')" @click="confirmDeleteMenu(menu)">
                <iconify icon="ph:trash-bold" />
              </prime-button>
            </div>
          </div>
        </div>
      </div>

      <!-- Right: A4 Preview -->
      <div class="tw:flex tw:flex-col tw:items-center tw:gap-4">
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-muted tw:self-start">{{ t('utilities.menuDesign.preview') }}</p>

        <!-- A4 wrapper: scale to fit, preserve ratio -->
        <div class="tw:w-full tw:overflow-auto">
          <div class="tw:flex tw:justify-center">

            <!-- ===== TEMPLATE: DEFAULT ===== -->
            <div
              v-if="menuTemplate === 'default'"
              ref="previewRef"
              class="tw:bg-white tw:shadow-2xl"
              :style="isLandscape
                ? `width: 842px; min-height: 595px; font-family: ${menuFont};`
                : `width: 595px; min-height: 842px; font-family: ${menuFont};`"
            >
              <div
                :class="isLandscape ? 'tw:px-10 tw:py-5 tw:text-center' : 'tw:px-10 tw:py-8 tw:text-center'"
                :style="{ backgroundColor: primaryColor }"
              >
                <h1 :class="isLandscape ? 'tw:text-2xl tw:font-bold tw:text-white tw:tracking-wide' : 'tw:text-3xl tw:font-bold tw:text-white tw:tracking-wide'">{{ cafeName }}</h1>
                <p v-if="slogan" class="tw:mt-1 tw:text-sm tw:text-white/80">{{ slogan }}</p>
              </div>
              <div v-if="previewCategories.length === 0" class="tw:flex tw:flex-col tw:items-center tw:justify-center tw:py-20 tw:gap-3">
                <iconify icon="ph:fork-knife-bold" class="tw:text-4xl tw:text-slate-300" />
                <p class="tw:text-sm tw:text-slate-400">{{ t('utilities.menuDesign.noProductsSelected') }}</p>
              </div>
              <div class="tw:px-8 tw:py-6 tw:space-y-8">
                <div v-for="cat in previewCategories" :key="cat.id" class="tw:space-y-3">
                  <div class="tw:flex tw:items-center tw:gap-3">
                    <h2 class="tw:text-base tw:font-bold tw:uppercase tw:tracking-widest" :style="{ color: primaryColor }">{{ cat.name }}</h2>
                    <div class="tw:flex-1 tw:h-px" :style="{ backgroundColor: primaryColor + '40' }"></div>
                  </div>
                  <div :class="layout === '2col' ? 'tw:grid tw:grid-cols-2 tw:gap-x-6 tw:gap-y-2'
                              : layout === 'landscape' ? 'tw:grid tw:grid-cols-3 tw:gap-x-4 tw:gap-y-1'
                              : 'tw:space-y-2'">
                    <div v-for="product in cat.products" :key="product.id"
                      class="tw:flex tw:items-baseline tw:justify-between tw:gap-2 tw:py-1.5 tw:border-b tw:border-slate-100">
                      <span class="tw:text-sm tw:text-slate-800">{{ product.name }}</span>
                      <span class="tw:text-sm tw:font-semibold tw:whitespace-nowrap tw:flex-shrink-0" :style="{ color: primaryColor }">{{ formatPrice(product.price) }}</span>
                    </div>
                  </div>
                </div>
              </div>
              <div class="tw:px-10 tw:py-4 tw:text-center tw:mt-4" :style="{ backgroundColor: primaryColor + '15' }">
                <p class="tw:text-xs tw:text-slate-400">{{ cafeName }}</p>
              </div>
            </div>

            <!-- ===== TEMPLATE: CLASSIC ===== -->
            <div
              v-else-if="menuTemplate === 'classic'"
              ref="previewRef"
              class="tw:shadow-2xl"
              :style="isLandscape
                ? `width: 842px; min-height: 595px; background: #d8d3c9; font-family: ${menuFont}; color: #1a1a1a;`
                : `width: 595px; min-height: 842px; background: #d8d3c9; font-family: ${menuFont}; color: #1a1a1a;`"
            >
              <!-- Classic landscape: 3-column newspaper style -->
              <div v-if="isLandscape" style="display: flex; min-height: 595px; gap: 0;">

                <!-- Left column -->
                <div style="flex: 1; display: flex; flex-direction: column; gap: 10px; padding: 16px 14px; background: #e8e3da;">
                  <div
                    v-for="(cat, j) in classicColumns[0]" :key="cat.id"
                    :style="j % 2 === 0 ? 'background: #ffffff; padding: 16px 18px;' : 'background: transparent; padding: 16px 18px;'"
                  >
                    <h2 style="font-size: 13px; font-weight: 900; text-transform: uppercase; letter-spacing: 0.06em; margin: 0 0 10px;">{{ cat.name }}</h2>
                    <div v-for="product in cat.products" :key="product.id" style="margin-bottom: 8px;">
                      <div style="display: flex; justify-content: space-between; align-items: baseline; gap: 6px;">
                        <span style="font-size: 10px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.04em;">{{ product.name }}</span>
                        <span style="font-size: 10px; font-weight: 700; white-space: nowrap; flex-shrink: 0;">{{ formatPrice(product.price) }}</span>
                      </div>
                    </div>
                  </div>
                </div>

                <!-- Center column: full white background -->
                <div style="flex: 1; display: flex; flex-direction: column; gap: 10px; padding: 16px 14px; background: #ffffff;">
                  <div style="text-align: center; padding: 12px 8px 16px;">
                    <p style="font-size: 8px; text-transform: uppercase; letter-spacing: 0.25em; color: #7a7570; margin: 0 0 8px;">{{ slogan || 'thực đơn' }}</p>
                    <div style="border-top: 1px solid #9a958d; margin-bottom: 8px;"></div>
                    <h1 style="font-size: 40px; font-weight: 900; text-transform: uppercase; letter-spacing: 0.05em; margin: 0 0 4px;">MENU</h1>
                    <div style="border-bottom: 1px solid #9a958d; margin-bottom: 8px;"></div>
                    <p style="font-size: 12px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.1em; margin: 0;">{{ cafeName }}</p>
                    <p v-if="slogan" style="font-size: 8px; color: #7a7570; margin: 4px 0 0;">{{ slogan }}</p>
                  </div>
                  <div
                    v-for="cat in classicColumns[1]" :key="cat.id"
                    style="background: transparent; padding: 16px 18px;"
                  >
                    <h2 style="font-size: 13px; font-weight: 900; text-transform: uppercase; letter-spacing: 0.06em; margin: 0 0 10px;">{{ cat.name }}</h2>
                    <div v-for="product in cat.products" :key="product.id" style="margin-bottom: 8px;">
                      <div style="display: flex; justify-content: space-between; align-items: baseline; gap: 6px;">
                        <span style="font-size: 10px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.04em;">{{ product.name }}</span>
                        <span style="font-size: 10px; font-weight: 700; white-space: nowrap; flex-shrink: 0;">{{ formatPrice(product.price) }}</span>
                      </div>
                    </div>
                  </div>
                </div>

                <!-- Right column -->
                <div style="flex: 1; display: flex; flex-direction: column; gap: 10px; padding: 16px 14px; background: #e8e3da;">
                  <div
                    v-for="(cat, j) in classicColumns[2]" :key="cat.id"
                    :style="j % 2 === 0 ? 'background: #ffffff; padding: 16px 18px;' : 'background: transparent; padding: 16px 18px;'"
                  >
                    <h2 style="font-size: 13px; font-weight: 900; text-transform: uppercase; letter-spacing: 0.06em; margin: 0 0 10px;">{{ cat.name }}</h2>
                    <div v-for="product in cat.products" :key="product.id" style="margin-bottom: 8px;">
                      <div style="display: flex; justify-content: space-between; align-items: baseline; gap: 6px;">
                        <span style="font-size: 10px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.04em;">{{ product.name }}</span>
                        <span style="font-size: 10px; font-weight: 700; white-space: nowrap; flex-shrink: 0;">{{ formatPrice(product.price) }}</span>
                      </div>
                    </div>
                  </div>
                </div>

              </div>

              <!-- Classic portrait: title at top, category boxes stacked -->
              <div v-else style="padding: 24px 20px;">
                <!-- Title block -->
                <div style="text-align: center; padding: 16px 12px 20px;">
                  <p style="font-size: 9px; text-transform: uppercase; letter-spacing: 0.25em; color: #6b6560; margin: 0 0 8px;">{{ slogan || 'thực đơn' }}</p>
                  <div style="border-top: 1px solid #8a857d; margin-bottom: 8px;"></div>
                  <h1 style="font-size: 48px; font-weight: 900; text-transform: uppercase; letter-spacing: 0.06em; margin: 0 0 4px;">MENU</h1>
                  <div style="border-bottom: 1px solid #8a857d; margin-bottom: 8px;"></div>
                  <p style="font-size: 14px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.12em; margin: 0;">{{ cafeName }}</p>
                </div>

                <div v-if="previewCategories.length === 0" style="text-align: center; padding: 40px; color: #9e9991;">
                  {{ t('utilities.menuDesign.noProductsSelected') }}
                </div>

                <!-- Portrait: 2-column grid of category boxes -->
                <div :style="layout === '2col'
                  ? 'display: grid; grid-template-columns: 1fr 1fr; gap: 14px;'
                  : 'display: flex; flex-direction: column; gap: 14px;'">
                  <div v-for="cat in previewCategories" :key="cat.id" style="background: #f5f1ea; padding: 18px 20px;">
                    <h2 style="font-size: 14px; font-weight: 900; text-transform: uppercase; letter-spacing: 0.06em; margin: 0 0 12px;">{{ cat.name }}</h2>
                    <div v-for="product in cat.products" :key="product.id" style="margin-bottom: 10px;">
                      <div style="display: flex; justify-content: space-between; align-items: baseline; gap: 8px;">
                        <span style="font-size: 11px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.04em;">{{ product.name }}</span>
                        <span style="font-size: 11px; font-weight: 700; white-space: nowrap; flex-shrink: 0;">{{ formatPrice(product.price) }}</span>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>

          </div>
        </div>
      </div>
    </div>

    <!-- Save dialog -->
    <prime-dialog
      v-model:visible="showSaveDialog"
      :header="t('utilities.menuDesign.saveMenu')"
      modal
      class="tw:w-[360px]"
    >
      <div class="tw:space-y-4 tw:py-2">
        <div class="tw:space-y-1.5">
          <label for="save-menu-name" class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">{{ t('utilities.menuDesign.menuName') }}</label>
          <prime-input-text
            id="save-menu-name"
            v-model="saveMenuName"
            :placeholder="t('utilities.menuDesign.menuNamePlaceholder')"
            class="app-input tw:w-full"
            autofocus
            @keyup.enter="confirmSave"
          />
        </div>
      </div>
      <template #footer>
        <prime-button severity="secondary" outlined @click="showSaveDialog = false">{{ t('common.cancel') }}</prime-button>
        <prime-button severity="success" :loading="saving" :disabled="!saveMenuName.trim()" @click="confirmSave">
          <iconify icon="ph:floppy-disk-bold" />
          <span>{{ t('common.save') }}</span>
        </prime-button>
      </template>
    </prime-dialog>

    <prime-confirm-dialog />
  </section>
</template>
