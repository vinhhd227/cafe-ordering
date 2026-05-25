<script setup>
import { createAdminOrder } from '@/services/order.service'
import FindPromosDialog from '@/components/orders/FindPromosDialog.vue'
import OrderTypeStep from '@/components/orders/wizard/OrderTypeStep.vue'
import OrderContextStep from '@/components/orders/wizard/OrderContextStep.vue'

const router = useRouter()
const toast = useToast()
const { t } = useI18n()

// â”€â”€ Composables â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
const {
  tables, menuCategories, loadingMenu,
  selectedTableId, sessionId, sessionHadExisting, sessionLoading, sessionError,
  searchQuery, orderLabel,
  onTableSelect, loadData,
} = useMenuSession()

const {
  cart, cartTotal, cartItemCount, defaultGuestCount, productCategoryMap,
  formatVnd, optionsLabel, cartQuantity,
  changeQty, clearCart, addToCart,
} = useOrderCart(menuCategories)

const {
  promoCode, promoInfo, promoLoading, promoError,
  findPromosVisible, publicPromos, publicPromosLoading,
  freeItemSelection, freeItemPickerPool, cartFinal,
  formatPromotionValue, isPromoAvailable, promoDisableReason, isItemDiscounted,
  applyPromoCode, clearPromo, openFindPromosDialog, selectPromo,
} = useOrderPromotion(cart, cartTotal, productCategoryMap, menuCategories)

const notificationStore = useNotificationStore()

// â”€â”€ Local state â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
const setupVisible    = ref(true)
const setupStep       = ref(1)
const contextStepRef  = ref(null)
const orderType       = ref('DINE_IN')
const customerName    = ref('')
const customerPhone   = ref('')
const deliveryAddress = ref('')
const deliveryNote    = ref('')

const onTypeSelected = () => { setupStep.value = 2 }
const confirmSetup = () => {
  if (setupStep.value === 2) {
    const valid = contextStepRef.value?.validateDelivery?.() ?? true
    if (!valid) return
    if (orderType.value === 'DINE_IN' && !sessionId.value && !sessionLoading.value) return
  }
  setupVisible.value = false
}

const guestCount = ref(null)
const placing = ref(false)
const errorMessage = ref('')
const showCartDrawer = ref(false)
const selectedCategoryId = ref(null) // null = all

// â”€â”€ Options drawer â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
const showOptionsDrawer = ref(false)
const drawerProduct = ref(null)
const drawerQuantity = ref(1)
const drawerSelections = ref({})      // variantGroups: { [groupId]: valueId | valueId[] }
const drawerOptionSelections = ref({})  // { [groupId]: valueId | valueId[] } — non-quantity groups
const drawerOptionQuantities = ref({})  // { [groupId]: { [valueId]: qty } } — allowQuantity groups
const drawerTakeaway = ref(false)

const openOptionsDrawer = (product) => {
  drawerProduct.value = product
  drawerQuantity.value = 1
  drawerTakeaway.value = orderType.value !== 'DINE_IN'
  const selections = {}
  for (const group of product.variantGroups ?? []) {
    const defaultVal = group.values?.find((v) => v.isDefault)
    if (group.selectionType === 'Single') {
      selections[group.id] = defaultVal?.id ?? null
    } else {
      selections[group.id] = defaultVal ? [defaultVal.id] : []
    }
  }
  drawerSelections.value = selections
  const optionSels = {}
  const optionQtys = {}
  for (const group of product.optionGroups ?? []) {
    if (group.allowQuantity) {
      optionQtys[group.id] = {}
    } else {
      optionSels[group.id] = group.allowMultiple ? [] : null
    }
  }
  drawerOptionSelections.value = optionSels
  drawerOptionQuantities.value = optionQtys
  showOptionsDrawer.value = true
}

const drawerToggleValue = (group, valueId) => {
  if (group.selectionType === 'Single') {
    drawerSelections.value[group.id] = valueId
  } else {
    const current = drawerSelections.value[group.id] ?? []
    const idx = current.indexOf(valueId)
    drawerSelections.value[group.id] =
      idx >= 0 ? current.filter((id) => id !== valueId) : [...current, valueId]
  }
}

const drawerIsSelected = (group, valueId) => {
  const sel = drawerSelections.value[group.id]
  if (group.selectionType === 'Single') return sel === valueId
  return Array.isArray(sel) && sel.includes(valueId)
}

// ── Option groups (shared ProductOptionGroup) ──────────────────
const drawerOptionIsSelected = (group, valueId) => {
  if (group.allowQuantity)
    return (drawerOptionQuantities.value[group.id]?.[valueId] ?? 0) > 0
  const sel = drawerOptionSelections.value[group.id]
  if (!group.allowMultiple) return sel === valueId
  return Array.isArray(sel) && sel.includes(valueId)
}

const drawerOptionToggle = (group, valueId) => {
  if (group.allowQuantity) {
    const qtys = drawerOptionQuantities.value[group.id] ?? {}
    const cur = qtys[valueId] ?? 0
    drawerOptionQuantities.value[group.id] = { ...qtys, [valueId]: cur > 0 ? 0 : 1 }
    return
  }
  if (!group.allowMultiple) {
    drawerOptionSelections.value[group.id] =
      drawerOptionSelections.value[group.id] === valueId ? null : valueId
  } else {
    const current = drawerOptionSelections.value[group.id] ?? []
    const idx = current.indexOf(valueId)
    drawerOptionSelections.value[group.id] =
      idx >= 0 ? current.filter((id) => id !== valueId) : [...current, valueId]
  }
}

const drawerOptionGetQty = (group, valueId) =>
  drawerOptionQuantities.value[group.id]?.[valueId] ?? 0

const drawerOptionAdjustQty = (group, valueId, delta) => {
  const qtys = drawerOptionQuantities.value[group.id] ?? {}
  const newQty = Math.max(0, (qtys[valueId] ?? 0) + delta)
  drawerOptionQuantities.value[group.id] = { ...qtys, [valueId]: newQty }
}

const drawerOptionAdjustment = computed(() => {
  const product = drawerProduct.value
  if (!product?.optionGroups?.length) return 0
  let total = 0
  for (const group of product.optionGroups) {
    if (group.allowQuantity) {
      const qtys = drawerOptionQuantities.value[group.id] ?? {}
      for (const [idStr, qty] of Object.entries(qtys)) {
        if (qty <= 0) continue
        const val = group.values?.find((v) => v.id === Number(idStr))
        if (val) total += (val.price ?? 0) * qty
      }
    } else {
      const sel = drawerOptionSelections.value[group.id]
      const ids = group.allowMultiple ? (sel ?? []) : (sel ? [sel] : [])
      for (const id of ids) {
        const val = group.values?.find((v) => v.id === id)
        if (val) total += val.price ?? 0
      }
    }
  }
  return total
})

const getSelectedVariantValueIds = (product, selections) => {
  const selectedIds = []
  const selectedLabels = []

  for (const group of product?.variantGroups ?? []) {
    const sel = selections[group.id]
    const ids = group.selectionType === 'Single' ? (sel ? [sel] : []) : (sel ?? [])
    for (const id of ids) {
      const val = group.values?.find((v) => v.id === id)
      if (!val) continue
      selectedIds.push(id)
      selectedLabels.push(val.label)
    }
  }

  return { selectedIds, selectedLabels }
}

const resolveSelectedProductVariant = (product, selectedIds) => {
  if (!product?.variants?.length || selectedIds.length === 0) return null

  const key = [...selectedIds].sort((a, b) => a - b).join(',')
  return product.variants
    .filter((variant) => variant.isActive !== false)
    .find((variant) => [...(variant.valueIds ?? [])].sort((a, b) => a - b).join(',') === key) ?? null
}

const isDrawerValueDisabled = (group, valueId) => {
  const product = drawerProduct.value
  if (!product?.variants?.length) return false
  const activeVariants = product.variants.filter((v) => v.isActive !== false)
  const otherIds = []
  for (const g of product.variantGroups ?? []) {
    if (g.id === group.id) continue
    const sel = drawerSelections.value[g.id]
    const ids = g.selectionType === 'Single' ? (sel ? [sel] : []) : (sel ?? [])
    otherIds.push(...ids)
  }
  return !activeVariants.some((v) => {
    const vIds = v.valueIds ?? []
    return vIds.includes(valueId) && otherIds.every((id) => vIds.includes(id))
  })
}

const drawerUnitPrice = computed(() => {
  if (!drawerProduct.value) return 0
  const { selectedIds } = getSelectedVariantValueIds(drawerProduct.value, drawerSelections.value)
  const basePrice = resolveSelectedProductVariant(drawerProduct.value, selectedIds)?.price ?? drawerProduct.value.price
  const hasHardVariants = (drawerProduct.value.variants ?? []).length > 0

  let variantAdj = 0
  if (!hasHardVariants) {
    for (const group of drawerProduct.value.variantGroups ?? []) {
      const sel = drawerSelections.value[group.id]
      const ids = group.selectionType === 'Single' ? (sel ? [sel] : []) : (sel ?? [])
      for (const id of ids) {
        const val = group.values?.find((v) => v.id === id)
        if (!val) continue
        variantAdj += val.price ?? 0
      }
    }
  }
  return basePrice + variantAdj + drawerOptionAdjustment.value
})

const drawerCanConfirm = computed(() => {
  if (!drawerProduct.value) return false
  for (const group of drawerProduct.value.variantGroups ?? []) {
    if (!group.isRequired) continue
    const sel = drawerSelections.value[group.id]
    if (group.selectionType === 'Single' && !sel) return false
    if (group.selectionType !== 'Single' && (!sel || sel.length === 0)) return false
  }
  const { selectedIds } = getSelectedVariantValueIds(drawerProduct.value, drawerSelections.value)
  if ((drawerProduct.value.variants ?? []).length > 0 && !resolveSelectedProductVariant(drawerProduct.value, selectedIds)) {
    return false
  }
  for (const group of drawerProduct.value.optionGroups ?? []) {
    if (!group.isRequired) continue
    if (group.allowQuantity) {
      const hasAny = Object.values(drawerOptionQuantities.value[group.id] ?? {}).some((q) => q > 0)
      if (!hasAny) return false
    } else {
      const sel = drawerOptionSelections.value[group.id]
      if (!group.allowMultiple && !sel) return false
      if (group.allowMultiple && (!sel || sel.length === 0)) return false
    }
  }
  return true
})

const confirmDrawerAdd = () => {
  const product = drawerProduct.value
  const { selectedIds, selectedLabels } = getSelectedVariantValueIds(product, drawerSelections.value)
  const selectedVariant = resolveSelectedProductVariant(product, selectedIds)
  const hasHardVariants = (product.variants ?? []).length > 0
  let optionAdjustment = 0

  if (!hasHardVariants) {
    for (const group of product.variantGroups ?? []) {
      const sel = drawerSelections.value[group.id]
      const ids = group.selectionType === 'Single' ? (sel ? [sel] : []) : (sel ?? [])
      for (const id of ids) {
        const val = group.values?.find((v) => v.id === id)
        if (!val) continue
        optionAdjustment += val.price ?? 0
      }
    }
  }

  // Collect selected option group values → [{optionValueId, quantity}]
  const selectedOptionValues = []
  const selectedOptionLabels = []
  for (const group of product.optionGroups ?? []) {
    if (group.allowQuantity) {
      const qtys = drawerOptionQuantities.value[group.id] ?? {}
      for (const [idStr, qty] of Object.entries(qtys)) {
        if (qty <= 0) continue
        const id = Number(idStr)
        const val = group.values?.find((v) => v.id === id)
        if (!val) continue
        selectedOptionValues.push({ optionValueId: id, quantity: qty })
        selectedOptionLabels.push(`${group.name}: ${val.name}${qty > 1 ? ` x${qty}` : ''}`)
        optionAdjustment += (val.price ?? 0) * qty
      }
    } else {
      const sel = drawerOptionSelections.value[group.id]
      const ids = group.allowMultiple ? (sel ?? []) : (sel ? [sel] : [])
      for (const id of ids) {
        const val = group.values?.find((v) => v.id === id)
        if (!val) continue
        selectedOptionValues.push({ optionValueId: id, quantity: 1 })
        selectedOptionLabels.push(`${group.name}: ${val.name}`)
        optionAdjustment += val.price ?? 0
      }
    }
  }

  const optKey = selectedOptionValues.map((o) => `${o.optionValueId}x${o.quantity}`).sort().join(',')
  const key = `${product.id}|${[...selectedIds].sort().join(',')}|${optKey}|${drawerTakeaway.value ? '1' : '0'}`

  addToCart({
    _key: key,
    productId: product.id,
    productName: product.name,
    unitPrice: selectedVariant?.price ?? product.price,
    optionAdjustment,
    selectedVariantValueIds: selectedIds,
    selectedValueLabels: [...selectedLabels, ...selectedOptionLabels],
    selectedOptionValues,
    quantity: drawerQuantity.value,
    isTakeaway: drawerTakeaway.value,
    isFreeGift: false,
    isAccompaniment: product.isAccompaniment ?? false,
  })
  showOptionsDrawer.value = false
}

// â”€â”€ Filtered products by category â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
const allProducts = computed(() => {
  const products = []
  for (const cat of menuCategories.value) {
    for (const p of cat.products ?? []) {
      products.push(p)
    }
  }
  return products
})

const displayProducts = computed(() => {
  const q = searchQuery.value.trim().toLowerCase()
  let list = selectedCategoryId.value === null
    ? allProducts.value
    : (menuCategories.value.find((c) => c.id === selectedCategoryId.value)?.products ?? [])
  if (q) list = list.filter((p) => p.name.toLowerCase().includes(q))
  return list
})

const productPriceText = (product) => {
  const variantPrices = (product.variants ?? [])
    .filter((variant) => variant.isActive !== false)
    .map((variant) => Number(variant.price))
    .filter((price) => Number.isFinite(price))

  if (variantPrices.length === 0) {
    return formatVnd(product.price)
  }

  const minPrice = Math.min(...variantPrices)
  const maxPrice = Math.max(...variantPrices)
  return minPrice === maxPrice
    ? formatVnd(minPrice)
    : `${formatVnd(minPrice)} - ${formatVnd(maxPrice)}`
}

// â”€â”€ Place order â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
const canPlaceOrder = computed(
  () => (orderType.value !== 'DINE_IN' || !!sessionId.value) && cart.value.length > 0 && !placing.value,
)

const placeOrder = async () => {
  if (!canPlaceOrder.value) return
  errorMessage.value = ''
  placing.value = true
  notificationStore.creatingOrder = true
  try {
    const res = await createAdminOrder({
      orderType: orderType.value,
      sessionId: orderType.value === 'DINE_IN' ? sessionId.value : null,
      items: cart.value.map((item) => ({
        productId: item.productId,
        productName: item.productName,
        unitPrice: item.unitPrice,
        quantity: item.quantity,
        selectedVariantValueIds: item.selectedVariantValueIds ?? [],
        selectedOptionValues: item.selectedOptionValues ?? [],
        isTakeaway: item.isTakeaway ?? false,
        isFreeGift: item.isFreeGift ?? false,
      })),
      guestCount: guestCount.value != null
        ? Number(guestCount.value)
        : defaultGuestCount.value || null,
      promoCode: promoCode.value.trim() || undefined,
      customerName: customerName.value || undefined,
      customerPhone: customerPhone.value || undefined,
      deliveryAddress: deliveryAddress.value || undefined,
      deliveryNote: deliveryNote.value || undefined,
    })
    const { orderId } = res.data
    toast.add({
      severity: 'success',
      summary: t('orders.create.submit'),
      detail: `#${res.data.orderNumber}`,
      life: 3000,
    })
    router.push({ name: 'ordersDetail', params: { id: orderId } })
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.errors?.map((e) => e.errorMessage ?? e).join('; ') ||
      err?.response?.data?.message ||
      'Failed to place order.'
  } finally {
    placing.value = false
    notificationStore.creatingOrder = false
  }
}

onMounted(() => loadData())
</script>

<template>
  <!-- Full-screen POS layout -->
  <div class="tw:flex tw:flex-col tw:flex-1 tw:min-h-0 tw:overflow-hidden">

    <!-- â”€â”€ Top bar â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ -->
    <div
      class="tw:flex tw:items-center tw:gap-2 tw:px-3 tw:py-2 tw:shrink-0 tw:border-b"
      style="border-color: var(--app-border); background: var(--app-bg)"
    >
      <prime-button :class="btnIcon" severity="secondary" text @click="router.push({ name: 'orders' })">
        <iconify icon="ph:arrow-left-bold" />
      </prime-button>
      <span class="tw:font-semibold tw:flex-1 tw:truncate">
        {{ t('orders.create.title') }}
      </span>

      <!-- Order type / table badge -->
      <prime-tag
        :severity="orderType === 'DINE_IN' ? 'secondary' : orderType === 'TAKEAWAY' ? 'warn' : 'info'"
        class="tw:shrink-0 tw:cursor-pointer"
        @click="setupVisible = true"
      >
        <iconify
          :icon="orderType === 'DINE_IN' ? 'ph:fork-knife-bold' : orderType === 'TAKEAWAY' ? 'ph:bag-bold' : 'ph:motorcycle-bold'"
          class="tw:mr-1"
        />
        <span>{{ orderType === 'DINE_IN' && selectedTableId
          ? tables.find(tb => tb.id === selectedTableId)?.code
          : t(`orders.create.orderType.${orderType}`) }}</span>
      </prime-tag>

      <!-- Session status (DineIn only) -->
      <template v-if="orderType === 'DINE_IN'">
        <prime-tag v-if="sessionLoading" severity="secondary" class="tw:shrink-0">
          <iconify icon="prime:spinner" class="tw:animate-spin" />
        </prime-tag>
        <prime-tag v-else-if="sessionId && sessionHadExisting" severity="info" class="tw:shrink-0">
          <iconify icon="prime:info-circle" />
        </prime-tag>
        <prime-tag v-else-if="sessionId" severity="success" class="tw:shrink-0">
          <iconify icon="prime:check" />
        </prime-tag>
      </template>
    </div>

    <!-- â”€â”€ Search bar â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ -->
    <div
      class="tw:px-3 tw:py-2 tw:shrink-0 tw:border-b"
      style="border-color: var(--app-border); background: var(--app-bg)"
    >
      <div class="tw:flex tw:items-center tw:gap-2 tw:rounded-lg tw:border tw:px-2 tw:py-1.5"
        style="border-color: var(--app-border)">
        <iconify icon="prime:search" class="tw:text-base tw:opacity-50 tw:shrink-0" />
        <input
          v-model="searchQuery"
          :placeholder="t('orders.create.search')"
          class="tw:flex-1 tw:bg-transparent tw:text-sm tw:outline-none"
        />
        <button v-if="searchQuery" @click="searchQuery = ''">
          <iconify icon="ph:x-bold" class="tw:text-sm tw:opacity-40" />
        </button>
      </div>
    </div>

    <!-- â”€â”€ Middle: category sidebar + product grid â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ -->
    <div class="tw:flex tw:flex-1 tw:overflow-hidden">

      <!-- Category sidebar -->
      <nav
        class="tw:w-20 tw:shrink-0 tw:overflow-y-auto tw:border-r tw:flex tw:flex-col tw:gap-1 tw:py-2 tw:px-1 no-scrollbar"
        style="border-color: var(--app-border); background: var(--app-bg-subtle)"
      >
        <!-- All -->
        <button
          class="tw:flex tw:flex-col tw:items-center tw:gap-1 tw:rounded-lg tw:px-1 tw:py-2 tw:text-center tw:text-xs tw:transition-colors tw:w-full"
          :class="
            selectedCategoryId === null
              ? 'tw:bg-primary-500/15 tw:text-primary-400 tw:font-semibold'
              : 'tw:text-muted tw:hover:bg-white/5'
          "
          @click="selectedCategoryId = null"
        >
          <iconify icon="ph:squares-four-bold" class="tw:text-lg tw:shrink-0" />
          <span class="tw:leading-tight tw:line-clamp-2">{{ t('orders.create.allCategories') }}</span>
        </button>

        <!-- Category buttons -->
        <button
          v-for="cat in menuCategories"
          :key="cat.id"
          class="tw:flex tw:flex-col tw:items-center tw:gap-1 tw:rounded-lg tw:px-1 tw:py-2 tw:text-center tw:text-xs tw:transition-colors tw:w-full"
          :class="
            selectedCategoryId === cat.id
              ? 'tw:bg-primary-500/15 tw:text-primary-400 tw:font-semibold'
              : 'tw:text-muted tw:hover:bg-white/5'
          "
          @click="selectedCategoryId = cat.id"
        >
          <iconify icon="ph:coffee-bold" class="tw:text-lg tw:shrink-0" />
          <span class="tw:leading-tight tw:line-clamp-2">{{ cat.name }}</span>
        </button>
      </nav>

      <!-- Product grid -->
      <div class="tw:flex-1 tw:overflow-y-auto no-scrollbar">
        <!-- Loading -->
        <div v-if="loadingMenu" class="tw:grid tw:grid-cols-2 tw:gap-2 tw:p-2">
          <div
            v-for="n in 6"
            :key="n"
            class="tw:rounded-xl tw:border tw:overflow-hidden tw:animate-pulse"
            style="border-color: var(--app-border)"
          >
            <div class="tw:h-24 tw:w-full" style="background: var(--app-bg-subtle)" />
            <div class="tw:p-2 tw:space-y-1.5">
              <div class="tw:h-3 tw:w-3/4 tw:rounded" style="background: var(--app-bg-subtle)" />
              <div class="tw:h-3 tw:w-1/2 tw:rounded" style="background: var(--app-bg-subtle)" />
            </div>
          </div>
        </div>

        <!-- Empty -->
        <div
          v-else-if="displayProducts.length === 0"
          class="tw:flex tw:flex-col tw:items-center tw:justify-center tw:h-full tw:gap-2 tw:text-muted tw:text-sm tw:p-8 tw:text-center"
        >
          <iconify icon="ph:magnifying-glass-bold" class="tw:text-3xl tw:opacity-40" />
          {{ t('orders.create.noProducts') }}
        </div>

        <!-- Grid -->
        <div v-else class="tw:grid tw:grid-cols-2 tw:gap-2 tw:p-2">
          <article
            v-for="product in displayProducts"
            :key="product.id"
            class="tw:relative tw:flex tw:flex-col tw:overflow-hidden tw:rounded-xl tw:border tw:cursor-pointer tw:active:scale-95 tw:transition-transform"
            style="border-color: var(--app-border); background: var(--app-bg-subtle)"
            @click="openOptionsDrawer(product)"
          >
            <!-- Image -->
            <div class="tw:relative tw:shrink-0">
              <img
                v-if="product.imageUrl"
                :src="product.imageUrl"
                :alt="product.name"
                class="tw:h-24 tw:w-full tw:object-cover"
              />
              <div
                v-else
                class="tw:h-24 tw:w-full tw:flex tw:items-center tw:justify-center"
                style="background: var(--app-bg)"
              >
                <iconify icon="ph:coffee-bold" class="tw:text-2xl tw:text-primary-400/20" />
              </div>
              <!-- Cart badge -->
              <div
                v-if="cartQuantity(product.id) > 0"
                class="tw:absolute tw:top-1.5 tw:right-1.5 tw:h-5 tw:min-w-5 tw:px-1 tw:rounded-full tw:bg-primary-500 tw:flex tw:items-center tw:justify-center tw:text-xs tw:font-bold tw:text-white tw:shadow-lg"
              >
                {{ cartQuantity(product.id) }}
              </div>
            </div>
            <!-- Info -->
            <div class="tw:flex tw:flex-col tw:flex-1 tw:p-2">
              <p class="tw:text-xs tw:font-semibold tw:line-clamp-2 tw:leading-snug">{{ product.name }}</p>
              <p class="tw:text-xs tw:text-primary-400 tw:font-semibold tw:mt-1">
                {{ productPriceText(product) }}
              </p>
              <div v-if="product.variantGroups?.length" class="tw:mt-1 tw:flex tw:flex-wrap tw:gap-0.5">
                <span
                  v-for="group in product.variantGroups"
                  :key="group.id"
                  class="tw:text-[10px] tw:rounded tw:px-1 tw:py-0.5 tw:bg-slate-500/10 tw:text-slate-400"
                >{{ group.name }}</span>
              </div>
            </div>
          </article>
        </div>
      </div>
    </div>

    <!-- â”€â”€ Cart bar â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ -->
    <button
      class="tw:flex tw:items-center tw:justify-between tw:px-4 tw:py-4 tw:shrink-0 tw:border-t tw:w-full tw:transition-colors"
      :class="
        cartItemCount > 0
          ? 'tw:bg-primary-600 tw:hover:bg-primary-700'
          : 'tw:cursor-default'
      "
      style="border-color: var(--app-border)"
      :style="cartItemCount === 0 ? 'background: var(--app-bg-subtle)' : ''"
      @click="cartItemCount > 0 && (showCartDrawer = true)"
    >
      <div class="tw:flex tw:items-center tw:gap-2">
        <div class="tw:relative">
          <iconify
            icon="ph:shopping-cart-bold"
            class="tw:text-xl"
            :class="cartItemCount > 0 ? 'tw:text-white' : 'tw:text-muted'"
          />
          <span
            v-if="cartItemCount > 0"
            class="tw:absolute tw:-top-2 tw:-right-2 tw:h-4 tw:min-w-4 tw:px-0.5 tw:rounded-full tw:bg-white tw:text-primary-700 tw:text-[10px] tw:font-bold tw:flex tw:items-center tw:justify-center"
          >{{ cartItemCount }}</span>
        </div>
        <span
          class="tw:text-sm tw:font-medium"
          :class="cartItemCount > 0 ? 'tw:text-white' : 'tw:text-muted'"
        >
          {{ cartItemCount > 0 ? `${cartItemCount} món` : t('orders.create.cartEmpty') }}
        </span>
      </div>
      <span
        v-if="cartItemCount > 0"
        class="tw:text-sm tw:font-bold tw:text-white"
      >{{ formatVnd(cartFinal) }}</span>
    </button>
  </div>

  <!-- â”€â”€ Options Drawer â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ -->
  <prime-drawer
    v-model:visible="showOptionsDrawer"
    position="bottom"
    :style="{ height: 'auto', maxHeight: '90dvh' }"
    :pt="{ root: { class: 'tw:rounded-t-2xl' } }"
  >
    <template #header>
      <div class="tw:flex tw:items-center tw:gap-3 tw:min-w-0">
        <img
          v-if="drawerProduct?.imageUrl"
          :src="drawerProduct.imageUrl"
          class="tw:h-12 tw:w-12 tw:rounded-xl tw:object-cover tw:shrink-0"
        />
        <div class="tw:min-w-0">
          <p class="tw:font-bold tw:text-base tw:truncate tw:text-xl">{{ drawerProduct?.name }}</p>
          <p class="tw:text-lg tw:font-semibold tw:text-primary-400">{{ drawerProduct ? formatVnd(drawerUnitPrice) : '' }}</p>
        </div>
      </div>
    </template>

    <div class="tw:overflow-y-auto tw:space-y-4 tw:pb-2">
      <!-- Quantity -->
      <div>
        <p class="tw:text-md tw:font-semibold tw:mb-2">{{ t('orders.create.optionsDialog.quantity') }}</p>
        <div class="tw:flex tw:items-center tw:gap-4">
          <prime-button
            severity="secondary"
            variant="outlined"
            :class="btnIcon"
            @click="drawerQuantity = Math.max(1, drawerQuantity - 1)"
          >
            <iconify icon="ph:minus-bold" />
          </prime-button>
          <span class="tw:text-xl tw:font-bold tw:w-8 tw:text-center">{{ drawerQuantity }}</span>
          <prime-button
            severity="secondary"
            variant="outlined"
            :class="btnIcon"
            @click="drawerQuantity++"
          >
            <iconify icon="ph:plus-bold" />
          </prime-button>
        </div>
      </div>

      <!-- Dynamic option groups (variantGroups) -->
      <div v-for="group in drawerProduct?.variantGroups ?? []" :key="group.id">
        <p class="tw:text-sm tw:font-semibold tw:mb-2">
          {{ group.name }}
          <span v-if="group.isRequired" class="tw:text-red-400 tw:text-xs tw:ml-1">*</span>
        </p>
        <!-- Single → RadioButtonGroup -->
        <prime-radio-button-group
          v-if="group.selectionType === 'Single'"
          v-model="drawerSelections[group.id]"
          class="tw:grid tw:grid-cols-2 tw:gap-2"
        >
          <label
            v-for="val in group.values"
            :key="val.id"
            :for="`vg-${group.id}-${val.id}`"
            class="tw:flex tw:items-center tw:gap-2 tw:rounded-xl tw:border tw:px-3 tw:py-2.5 tw:text-sm tw:cursor-pointer tw:transition-colors"
            :class="drawerSelections[group.id] === val.id ? 'tw:border-primary-500/60 tw:bg-primary-500/10' : ''"
            :style="drawerSelections[group.id] === val.id ? '' : 'border-color: var(--app-border)'"
          >
            <prime-radio-button
              :input-id="`vg-${group.id}-${val.id}`"
              :value="val.id"
              :disabled="isDrawerValueDisabled(group, val.id)"
            />
            <span class="tw:flex-1 tw:truncate">{{ val.label }}</span>
            <span v-if="isDrawerValueDisabled(group, val.id)" class="tw:text-xs tw:shrink-0 tw:text-red-400/70">
              {{ t('orders.create.optionsDialog.unavailable') }}
            </span>
            <span v-else-if="val.price && !(drawerProduct?.variants?.length)" class="tw:text-xs tw:shrink-0 tw:opacity-70">
              +{{ formatVnd(val.price) }}
            </span>
          </label>
        </prime-radio-button-group>
        <!-- Multiple → Checkbox -->
        <div v-else class="tw:grid tw:grid-cols-2 tw:gap-2">
          <label
            v-for="val in group.values"
            :key="val.id"
            :for="`vg-${group.id}-${val.id}`"
            class="tw:flex tw:items-center tw:gap-2 tw:rounded-xl tw:border tw:px-3 tw:py-2.5 tw:text-sm tw:cursor-pointer tw:transition-colors"
            :class="(drawerSelections[group.id] ?? []).includes(val.id) ? 'tw:border-primary-500/60 tw:bg-primary-500/10' : ''"
            :style="(drawerSelections[group.id] ?? []).includes(val.id) ? '' : 'border-color: var(--app-border)'"
          >
            <prime-checkbox
              :input-id="`vg-${group.id}-${val.id}`"
              v-model="drawerSelections[group.id]"
              :value="val.id"
              :disabled="isDrawerValueDisabled(group, val.id)"
            />
            <span class="tw:flex-1 tw:truncate">{{ val.label }}</span>
            <span v-if="isDrawerValueDisabled(group, val.id)" class="tw:text-xs tw:shrink-0 tw:text-red-400/70">
              {{ t('orders.create.optionsDialog.unavailable') }}
            </span>
            <span v-else-if="val.price && !(drawerProduct?.variants?.length)" class="tw:text-xs tw:shrink-0 tw:opacity-70">
              +{{ formatVnd(val.price) }}
            </span>
          </label>
        </div>
      </div>

      <!-- Shared option groups (ProductOptionGroup) -->
      <div v-for="group in drawerProduct?.optionGroups ?? []" :key="`og-${group.id}`">
        <p class="tw:text-md tw:font-semibold tw:mb-2">
          {{ group.name }}
          <span v-if="group.isRequired" class="tw:text-red-400 tw:text-xs tw:ml-1">*</span>
          <span v-if="group.allowMultiple && !group.allowQuantity" class="tw:text-muted tw:text-xs tw:ml-1 tw:font-normal">{{ t('orders.create.optionsDialog.multiSelect') }}</span>
        </p>

        <!-- allowQuantity: full-width list with stepper -->
        <div v-if="group.allowQuantity" class="tw:space-y-2">
          <div
            v-for="val in group.values"
            :key="val.id"
            class="tw:flex tw:items-center tw:gap-3 tw:rounded-xl tw:border tw:px-3 tw:py-2.5 tw:text-sm tw:transition-colors"
            :class="drawerOptionIsSelected(group, val.id)
              ? 'tw:border-primary-400/50 tw:bg-primary-500/5'
              : ''"
            :style="drawerOptionIsSelected(group, val.id) ? '' : 'border-color: var(--app-border)'"
          >
            <prime-checkbox
              :model-value="drawerOptionIsSelected(group, val.id)"
              :binary="true"
              @change="drawerOptionToggle(group, val.id)"
            />
            <span class="tw:flex-1 tw:truncate tw:text-lg">{{ val.name }}</span>
            <span v-if="val.price" class="tw:text-md tw:text-muted tw:shrink-0">+{{ formatVnd(val.price) }}</span>
            <!-- qty stepper — visible only when selected -->
            <div v-if="drawerOptionIsSelected(group, val.id)" class="tw:flex tw:items-center tw:gap-1.5 tw:shrink-0">
              <prime-button size="small" severity="secondary" variant="outlined" :class="btnIcon" @click="drawerOptionAdjustQty(group, val.id, -1)">
                <iconify icon="ph:minus-bold" />
              </prime-button>
              <span class="tw:w-5 tw:text-center tw:font-semibold tw:text-primary-400">{{ drawerOptionGetQty(group, val.id) }}</span>
              <prime-button size="small" severity="secondary" variant="outlined" :class="btnIcon" @click="drawerOptionAdjustQty(group, val.id, 1)">
                <iconify icon="ph:plus-bold" />
              </prime-button>
            </div>
          </div>
        </div>

        <!-- single/multi select (no qty) -->
        <template v-else>
          <!-- Single → RadioButtonGroup -->
          <prime-radio-button-group
            v-if="!group.allowMultiple"
            v-model="drawerOptionSelections[group.id]"
            class="tw:grid tw:grid-cols-2 tw:gap-2"
          >
            <label
              v-for="val in group.values"
              :key="val.id"
              :for="`og-${group.id}-${val.id}`"
              class="tw:flex tw:items-center tw:gap-2 tw:rounded-xl tw:border tw:px-3 tw:py-2.5 tw:text-sm tw:cursor-pointer tw:transition-colors"
              :class="drawerOptionSelections[group.id] === val.id ? 'tw:border-primary-500/60 tw:bg-primary-500/10' : ''"
              :style="drawerOptionSelections[group.id] === val.id ? '' : 'border-color: var(--app-border)'"
            >
              <prime-radio-button :input-id="`og-${group.id}-${val.id}`" :value="val.id" />
              <span class="tw:flex-1 tw:truncate">{{ val.name }}</span>
              <span v-if="val.price" class="tw:text-xs tw:shrink-0 tw:opacity-70">+{{ formatVnd(val.price) }}</span>
            </label>
          </prime-radio-button-group>
          <!-- Multiple → Checkbox -->
          <div v-else class="tw:grid tw:grid-cols-2 tw:gap-2">
            <label
              v-for="val in group.values"
              :key="val.id"
              :for="`og-${group.id}-${val.id}`"
              class="tw:flex tw:items-center tw:gap-2 tw:rounded-xl tw:border tw:px-3 tw:py-2.5 tw:text-sm tw:cursor-pointer tw:transition-colors"
              :class="(drawerOptionSelections[group.id] ?? []).includes(val.id) ? 'tw:border-primary-500/60 tw:bg-primary-500/10' : ''"
              :style="(drawerOptionSelections[group.id] ?? []).includes(val.id) ? '' : 'border-color: var(--app-border)'"
            >
              <prime-checkbox
                :input-id="`og-${group.id}-${val.id}`"
                v-model="drawerOptionSelections[group.id]"
                :value="val.id"
              />
              <span class="tw:flex-1 tw:truncate">{{ val.name }}</span>
              <span v-if="val.price" class="tw:text-xs tw:shrink-0 tw:opacity-70">+{{ formatVnd(val.price) }}</span>
            </label>
          </div>
        </template>
      </div>

      <!-- Serving (only for DINE_IN — per-item serving choice) -->
      <div v-if="orderType === 'DINE_IN'">
        <p class="tw:text-md tw:font-semibold tw:mb-2">{{ t('orders.create.optionsDialog.serving') }}</p>
        <div class="tw:flex tw:gap-2">
          <prime-button
            variant="outlined"
            class="tw:flex-1"
            :severity="!drawerTakeaway ? 'primary' : 'secondary'"
            @click="drawerTakeaway = false"
          >
            <iconify icon="ph:coffee-bold" />
            <span class="tw:text-lg!">{{ t('orders.serving.dineIn') }}</span>
          </prime-button>
          <prime-button
            variant="outlined"
            class="tw:flex-1"
            :severity="drawerTakeaway ? 'primary' : 'secondary'"
            @click="drawerTakeaway = true"
          >
            <iconify icon="ph:bag-bold" />
            <span class="tw:text-lg!">{{ t('orders.serving.takeaway') }}</span>
          </prime-button>
        </div>
      </div>

      <!-- Add to cart button -->
      <prime-button
        :disabled="!drawerCanConfirm"
        fluid
        class="tw:mt-2"
        @click="confirmDrawerAdd"
      >
        <iconify icon="ph:shopping-cart-bold" />
        <span>{{ t('orders.create.optionsDialog.addToCart') }} - {{ formatVnd(drawerUnitPrice) }}</span>
      </prime-button>
    </div>
  </prime-drawer>

  <!-- â”€â”€ Cart Drawer â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ -->
  <prime-drawer
    v-model:visible="showCartDrawer"
    position="bottom"
    :style="{ height: 'auto', maxHeight: '92dvh' }"
    :pt="{ root: { class: 'tw:rounded-t-2xl' } }"
  >
    <template #header>
      <div class="tw:flex tw:items-center tw:justify-between tw:w-full">
        <h2 class="tw:font-semibold tw:flex tw:items-center tw:gap-2">
          {{ t('orders.create.cart') }}
          <prime-badge v-if="cartItemCount > 0" :value="cartItemCount" severity="primary" />
        </h2>
        <prime-button
          v-if="cart.length > 0"
          size="small"
          severity="danger"
          outlined
          :class="btnIcon"
          @click="clearCart"
        >
          <iconify icon="prime:trash" />
        </prime-button>
      </div>
    </template>

    <div class="tw:overflow-y-auto tw:space-y-3 tw:pb-4">
      <!-- Order label (table) -->
      <div v-if="orderLabel">
        <prime-tag :value="orderLabel" severity="secondary">
          <iconify icon="prime:table" />
        </prime-tag>
      </div>

      <!-- Session error -->
      <prime-message v-if="sessionError" severity="error" :closable="false">
        {{ sessionError }}
      </prime-message>

      <!-- Empty -->
      <div
        v-if="cart.length === 0"
        class="tw:py-10 tw:text-center tw:text-muted tw:text-sm tw:rounded-xl tw:border tw:border-dashed"
        style="border-color: var(--app-border)"
      >
        {{ t('orders.create.cartEmpty') }}<br />
        <span class="tw:text-xs tw:opacity-60">{{ t('orders.create.cartEmptyHint') }}</span>
      </div>

      <!-- Cart items -->
      <div v-else class="tw:space-y-2">
        <div
          v-for="item in cart"
          :key="item._key"
          class="tw:rounded-xl tw:border tw:p-3"
          :class="
            item.isFreeGift
              ? 'tw:border-amber-500/40 tw:bg-amber-500/5'
              : isItemDiscounted(item)
                ? 'tw:border-primary-500/60 tw:bg-primary-500/5'
                : ''
          "
          :style="item.isFreeGift || isItemDiscounted(item) ? '' : 'border-color: var(--app-border)'"
        >
          <div class="tw:flex tw:items-start tw:gap-2">
            <div class="tw:flex-1 tw:min-w-0">
              <p class="tw:text-sm tw:font-medium tw:leading-snug">{{ item.productName }}</p>
              <p class="tw:text-xs tw:mt-0.5" :class="item.isFreeGift ? 'tw:text-amber-400' : 'tw:text-primary-400'">
                {{ item.isFreeGift ? '0 ₫' : formatVnd(item.unitPrice + (item.optionAdjustment ?? 0)) }}
              </p>
              <p v-if="optionsLabel(item)" class="tw:text-xs tw:text-amber-400 tw:mt-0.5">
                {{ optionsLabel(item) }}
              </p>
              <span
                v-if="item.isTakeaway"
                class="tw:inline-block tw:mt-1 tw:text-xs tw:px-1.5 tw:py-0.5 tw:rounded tw:bg-sky-500/10 tw:text-sky-400"
              >{{ t('orders.create.takeaway') }}</span>
            </div>
            <div v-if="item.isFreeGift" class="tw:shrink-0">
              <prime-button size="small" text rounded severity="secondary" :class="btnIcon"
                @click="freeItemSelection = null">
                <iconify icon="prime:times" class="tw:text-xs" />
              </prime-button>
            </div>
            <div v-else class="tw:flex tw:items-center tw:gap-1 tw:shrink-0">
              <prime-button size="small" text rounded severity="secondary" :class="btnIcon"
                @click="changeQty(item._key, -1)">
                <iconify icon="prime:minus" />
              </prime-button>
              <span class="tw:text-sm tw:font-bold tw:w-5 tw:text-center">{{ item.quantity }}</span>
              <prime-button size="small" text rounded severity="secondary" :class="btnIcon"
                @click="changeQty(item._key, 1)">
                <iconify icon="prime:plus" />
              </prime-button>
            </div>
          </div>
          <div class="tw:flex tw:justify-between tw:items-center tw:mt-1.5">
            <span v-if="item.isFreeGift" class="tw:text-xs tw:text-amber-400 tw:font-semibold">
              {{ t('orders.create.freeBadge') }}
            </span>
            <span v-else-if="isItemDiscounted(item)" class="tw:text-xs tw:text-primary-400">
              {{ t('orders.create.promoBadge') }}
            </span>
            <span v-else />
            <span class="tw:text-sm tw:font-semibold"
              :class="item.isFreeGift ? 'tw:text-amber-400' : 'tw:text-primary-400'">
              {{ item.isFreeGift ? '0 ₫' : formatVnd((item.unitPrice + (item.optionAdjustment ?? 0)) * item.quantity) }}
            </span>
          </div>
        </div>
      </div>

      <!-- Summary -->
      <div v-if="cart.length > 0" class="tw:rounded-xl tw:p-3" style="background: var(--app-bg-subtle)">
        <div class="tw:flex tw:justify-between tw:text-sm tw:mb-1">
          <span class="tw:text-muted">{{ t('orders.create.subtotal') }}</span>
          <span>{{ formatVnd(cartTotal) }}</span>
        </div>
        <div v-if="promoInfo" class="tw:flex tw:justify-between tw:text-xs tw:mb-1">
          <span class="tw:flex tw:items-center tw:gap-1 tw:text-muted">
            <iconify icon="ph:tag-bold" class="tw:text-primary-400" />
            {{ promoInfo.code }}
          </span>
          <span v-if="freeItemSelection" class="tw:text-amber-400">
            {{ t('orders.create.freeItem') }}
          </span>
          <span v-else-if="promoInfo.estimatedDiscount" class="tw:text-primary-400">
            -{{ formatVnd(promoInfo.estimatedDiscount) }}
          </span>
        </div>
        <div class="tw:flex tw:justify-between tw:font-bold tw:border-t tw:pt-2" style="border-color: var(--app-border)">
          <span>{{ t('orders.create.total') }}</span>
          <span class="tw:text-primary-400 tw:text-base">{{ formatVnd(cartFinal) }}</span>
        </div>
      </div>

      <!-- Guest count (DineIn only) -->
      <div v-if="orderType === 'DINE_IN'" class="tw:flex tw:items-center tw:gap-2">
        <iconify icon="ph:users" class="tw:opacity-60" />
        <prime-input-number
          v-model="guestCount"
          :placeholder="defaultGuestCount > 0 ? String(defaultGuestCount) : t('orders.create.guestCountPlaceholder')"
          :min="1" :max="99" :useGrouping="false"
          inputClass="tw:text-sm tw:w-24"
          size="small"
        />
      </div>

      <!-- Promo section -->
      <div class="tw:space-y-2">
        <div class="tw:flex tw:items-center tw:justify-between">
          <span class="tw:flex tw:items-center tw:gap-1.5 tw:text-xs tw:text-muted">
            <iconify icon="ph:ticket-bold" class="tw:text-primary-400" />
            {{ t('orders.create.findPromotions') }}
          </span>
          <prime-button size="small" text severity="secondary" class="tw:text-xs tw:h-6! tw:px-2!"
            @click="openFindPromosDialog">
            <iconify icon="ph:magnifying-glass-bold" class="tw:text-xs" />
            <span>{{ t('orders.create.browse') }}</span>
          </prime-button>
        </div>

        <!-- Applied promo -->
        <div
          v-if="promoInfo"
          class="tw:flex tw:items-center tw:justify-between tw:rounded-xl tw:border tw:border-primary-500/30 tw:bg-primary-500/10 tw:px-3 tw:py-2"
        >
          <div class="tw:flex tw:items-center tw:gap-2 tw:min-w-0">
            <iconify icon="ph:tag-bold" class="tw:text-primary-400 tw:shrink-0" />
            <span class="tw:font-medium tw:text-sm tw:text-primary-300">{{ promoInfo.code }}</span>
            <span class="tw:text-xs tw:text-muted tw:truncate">{{ promoInfo.name }}</span>
          </div>
          <prime-button size="small" text severity="secondary" :class="btnIcon" @click="clearPromo">
            <iconify icon="prime:times" class="tw:text-xs" />
          </prime-button>
        </div>

        <!-- Free item picker -->
        <div
          v-if="freeItemPickerPool.length > 0"
          class="tw:rounded-xl tw:border tw:p-3"
          style="border-color: var(--app-border)"
        >
          <p class="tw:text-xs tw:font-medium tw:mb-2 tw:flex tw:items-center tw:gap-1 tw:text-primary-400">
            <iconify icon="ph:gift-bold" />
            {{ t('orders.create.selectFreeGift') }}
          </p>
          <div class="tw:flex tw:flex-col tw:gap-1.5">
            <button
              v-for="item in freeItemPickerPool"
              :key="item._key"
              class="tw:flex tw:items-center tw:justify-between tw:rounded-lg tw:border tw:px-3 tw:py-2 tw:text-left tw:text-sm tw:transition-colors tw:w-full"
              :class="
                freeItemSelection?._key === item._key
                  ? 'tw:border-primary-500 tw:bg-primary-500/10 tw:text-primary-300'
                  : 'tw:text-muted'
              "
              :style="freeItemSelection?._key === item._key ? '' : 'border-color: var(--app-border)'"
              @click="freeItemSelection = freeItemSelection?._key === item._key ? null : item"
            >
              <span class="tw:flex tw:items-center tw:gap-1.5">
                <iconify :icon="freeItemSelection?._key === item._key ? 'ph:check-circle-fill' : 'ph:circle'" />
                {{ item.productName }}
              </span>
              <span class="tw:text-xs tw:font-semibold tw:text-primary-400">{{ formatVnd(item.unitPrice) }}</span>
            </button>
          </div>
        </div>

        <!-- Promo code input -->
        <div v-if="!promoInfo && cart.length > 0">
          <div class="tw:flex tw:gap-2">
            <prime-input-text
              v-model="promoCode"
              :placeholder="t('orders.create.promoCode')"
              class="tw:flex-1"
              @keyup.enter="applyPromoCode"
            />
            <prime-button
              severity="secondary" outlined :loading="promoLoading"
              :disabled="!promoCode.trim()" @click="applyPromoCode"
            >{{ t('orders.create.apply') }}</prime-button>
          </div>
          <p v-if="promoError" class="tw:text-xs tw:text-red-400 tw:mt-1.5">{{ promoError }}</p>
        </div>
      </div>

      <!-- Error -->
      <prime-message v-if="errorMessage" severity="error" :closable="false">
        {{ errorMessage }}
      </prime-message>

      <!-- Place order -->
      <prime-button
        fluid
        :loading="placing"
        :disabled="!canPlaceOrder"
        @click="placeOrder"
      >
        <iconify icon="prime:check" />
        <span>{{ t('orders.create.submit') }}</span>
      </prime-button>

      <p v-if="orderType === 'DINE_IN' && !sessionId && !sessionLoading" class="tw:text-xs tw:text-muted tw:text-center">
        {{ t('orders.create.tableHint') }}
      </p>
    </div>
  </prime-drawer>

  <!-- Setup Drawer (wizard: step 1 type, step 2 context) -->
  <prime-drawer
    v-model:visible="setupVisible"
    position="bottom"
    :closable="false"
    :style="{ height: 'auto', maxHeight: '90dvh' }"
    :pt="{ root: { class: 'tw:rounded-t-2xl' } }"
  >
    <template #header>
      <div class="tw:flex tw:items-center tw:justify-between tw:w-full">
        <h2 class="tw:font-semibold tw:text-lg">{{ t(`orders.create.wizard.step${setupStep}`) }}</h2>
        <div class="tw:flex tw:gap-1.5 tw:items-center">
          <span
            v-for="s in 2" :key="s"
            class="tw:h-2 tw:rounded-full tw:transition-all"
            :class="s === setupStep ? 'tw:w-6 tw:bg-primary-400' : 'tw:w-2 tw:bg-white/20'"
          />
        </div>
      </div>
    </template>

    <div class="tw:pb-4 tw:space-y-4">
      <!-- Step 1: order type -->
      <OrderTypeStep
        v-if="setupStep === 1"
        v-model="orderType"
        @next="onTypeSelected"
      />

      <!-- Step 2: context -->
      <template v-else-if="setupStep === 2">
        <OrderContextStep
          ref="contextStepRef"
          :orderType="orderType"
          :tables="tables"
          :selectedTableId="selectedTableId"
          :sessionId="sessionId"
          :sessionLoading="sessionLoading"
          :sessionError="sessionError"
          :sessionHadExisting="sessionHadExisting"
          :guestCount="guestCount"
          :customerName="customerName"
          :customerPhone="customerPhone"
          :deliveryAddress="deliveryAddress"
          :deliveryNote="deliveryNote"
          @update:selectedTableId="v => { selectedTableId = v }"
          @update:guestCount="v => guestCount = v"
          @update:customerName="v => customerName = v"
          @update:customerPhone="v => customerPhone = v"
          @update:deliveryAddress="v => deliveryAddress = v"
          @update:deliveryNote="v => deliveryNote = v"
          @tableSelect="onTableSelect"
        />

        <div class="tw:flex tw:gap-2">
          <prime-button severity="secondary" outlined fluid @click="setupStep = 1">
            <iconify icon="ph:arrow-left-bold" />
            <span>{{ t('orders.create.wizard.back') }}</span>
          </prime-button>
          <prime-button fluid @click="confirmSetup">
            <span>{{ t('orders.create.wizard.confirm') }}</span>
            <iconify icon="ph:check-bold" />
          </prime-button>
        </div>
      </template>
    </div>
  </prime-drawer>

  <FindPromosDialog
    v-model:visible="findPromosVisible"
    :promos="publicPromos"
    :loading="publicPromosLoading"
    :promoInfo="promoInfo"
    :isPromoAvailable="isPromoAvailable"
    :promoDisableReason="promoDisableReason"
    :formatPromotionValue="formatPromotionValue"
    :formatVnd="formatVnd"
    @select="selectPromo"
  />
</template>

<style scoped>
.no-scrollbar {
  -webkit-overflow-scrolling: touch;
  scrollbar-width: none;
}
.no-scrollbar::-webkit-scrollbar {
  display: none;
}
</style>
