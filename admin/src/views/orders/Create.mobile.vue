<script setup>
import { createOrder, applyPromotionAdmin } from '@/services/order.service'
import FindPromosDialog from '@/components/orders/FindPromosDialog.vue'

const router = useRouter()
const toast = useToast()
const { t } = useI18n()

// ── Composables ───────────────────────────────────────────────────
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

// ── Local state ───────────────────────────────────────────────────
const guestCount = ref(null)
const placing = ref(false)
const errorMessage = ref('')
const showCartDrawer = ref(false)
const selectedCategoryId = ref(null) // null = all

// ── Options drawer ────────────────────────────────────────────────
const showOptionsDrawer = ref(false)
const drawerProduct = ref(null)
const drawerQuantity = ref(1)
const drawerSelections = ref({}) // { [groupId]: valueId | valueId[] }
const drawerTakeaway = ref(false)

const openOptionsDrawer = (product) => {
  drawerProduct.value = product
  drawerQuantity.value = 1
  drawerTakeaway.value = false
  const selections = {}
  for (const group of product.optionGroups ?? []) {
    const defaultVal = group.values?.find((v) => v.isDefault)
    if (group.selectionType === 'Single') {
      selections[group.id] = defaultVal?.id ?? null
    } else {
      selections[group.id] = defaultVal ? [defaultVal.id] : []
    }
  }
  drawerSelections.value = selections
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

const drawerCanConfirm = computed(() => {
  if (!drawerProduct.value) return false
  for (const group of drawerProduct.value.optionGroups ?? []) {
    if (!group.isRequired) continue
    const sel = drawerSelections.value[group.id]
    if (group.selectionType === 'Single' && !sel) return false
    if (group.selectionType !== 'Single' && (!sel || sel.length === 0)) return false
  }
  return true
})

const confirmDrawerAdd = () => {
  const product = drawerProduct.value
  const selectedIds = []
  const selectedLabels = []
  let optionAdjustment = 0

  for (const group of product.optionGroups ?? []) {
    const sel = drawerSelections.value[group.id]
    const ids = group.selectionType === 'Single' ? (sel ? [sel] : []) : (sel ?? [])
    for (const id of ids) {
      const val = group.values?.find((v) => v.id === id)
      if (!val) continue
      selectedIds.push(id)
      selectedLabels.push(val.label)
      optionAdjustment += val.priceAdjustment ?? 0
    }
  }

  const key = `${product.id}|${[...selectedIds].sort().join(',')}|${drawerTakeaway.value ? '1' : '0'}`

  addToCart({
    _key: key,
    productId: product.id,
    productName: product.name,
    unitPrice: product.price,
    optionAdjustment,
    selectedOptionValueIds: selectedIds,
    selectedValueLabels: selectedLabels,
    quantity: drawerQuantity.value,
    isTakeaway: drawerTakeaway.value,
    isFreeGift: false,
    isAccompaniment: product.isAccompaniment ?? false,
  })
  showOptionsDrawer.value = false
}

// ── Filtered products by category ────────────────────────────────
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

// ── Place order ───────────────────────────────────────────────────
const canPlaceOrder = computed(
  () => !!sessionId.value && cart.value.length > 0 && !placing.value,
)

const placeOrder = async () => {
  if (!canPlaceOrder.value) return
  errorMessage.value = ''
  placing.value = true
  notificationStore.creatingOrder = true
  try {
    const res = await createOrder(
      sessionId.value,
      cart.value.map((item) => ({
        productId: item.productId,
        productName: item.productName,
        unitPrice: item.unitPrice,
        quantity: item.quantity,
        selectedOptionValueIds: item.selectedOptionValueIds ?? [],
        isTakeaway: item.isTakeaway ?? false,
        isFreeGift: item.isFreeGift ?? false,
      })),
      guestCount.value != null
        ? Number(guestCount.value)
        : defaultGuestCount.value || null,
    )
    const { orderId } = res.data

    if (promoCode.value.trim()) {
      try {
        await applyPromotionAdmin(orderId, promoCode.value.trim())
      } catch (promoErr) {
        const msg =
          promoErr?.response?.data?.errors?.map((e) => e.errorMessage ?? e).join('; ') ||
          promoErr?.response?.data?.message ||
          'Could not apply promotion.'
        toast.add({ severity: 'warn', summary: t('orders.create.promoBadge'), detail: msg, life: 5000 })
      }
    }

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

    <!-- ── Top bar ──────────────────────────────────────────────── -->
    <div
      class="tw:flex tw:items-center tw:gap-2 tw:px-3 tw:py-2 tw:shrink-0 tw:border-b"
      style="border-color: var(--app-border); background: var(--app-bg)"
    >
      <prime-button :class="btnIcon" severity="secondary" text @click="router.push({ name: 'orders' })">
        <iconify icon="ph:arrow-left-bold" />
      </prime-button>
      <span class="tw:font-semibold tw:text-sm tw:flex-1 tw:truncate">
        {{ t('orders.create.title') }}
      </span>

      <!-- Table select -->
      <prime-select
        v-model="selectedTableId"
        :options="tables"
        optionLabel="code"
        optionValue="id"
        :placeholder="t('orders.create.selectTable')"
        :disabled="sessionLoading"
        class="tw:w-28"
        size="small"
        @change="(e) => onTableSelect(e.value)"
      />

      <!-- Session status chip -->
      <prime-tag v-if="sessionLoading" severity="secondary" class="tw:shrink-0">
        <iconify icon="prime:spinner" class="tw:animate-spin" />
      </prime-tag>
      <prime-tag v-else-if="sessionId && sessionHadExisting" severity="info" class="tw:shrink-0">
        <iconify icon="prime:info-circle" />
      </prime-tag>
      <prime-tag v-else-if="sessionId" severity="success" class="tw:shrink-0">
        <iconify icon="prime:check" />
      </prime-tag>
    </div>

    <!-- ── Search bar ───────────────────────────────────────────── -->
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

    <!-- ── Middle: category sidebar + product grid ──────────────── -->
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
              ? 'tw:bg-emerald-500/15 tw:text-emerald-400 tw:font-semibold'
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
              ? 'tw:bg-emerald-500/15 tw:text-emerald-400 tw:font-semibold'
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
                <iconify icon="ph:coffee-bold" class="tw:text-2xl tw:text-emerald-400/20" />
              </div>
              <!-- Cart badge -->
              <div
                v-if="cartQuantity(product.id) > 0"
                class="tw:absolute tw:top-1.5 tw:right-1.5 tw:h-5 tw:min-w-5 tw:px-1 tw:rounded-full tw:bg-emerald-500 tw:flex tw:items-center tw:justify-center tw:text-xs tw:font-bold tw:text-white tw:shadow-lg"
              >
                {{ cartQuantity(product.id) }}
              </div>
            </div>
            <!-- Info -->
            <div class="tw:flex tw:flex-col tw:flex-1 tw:p-2">
              <p class="tw:text-xs tw:font-semibold tw:line-clamp-2 tw:leading-snug">{{ product.name }}</p>
              <p class="tw:text-xs tw:text-emerald-400 tw:font-semibold tw:mt-1">
                {{ formatVnd(product.price) }}
              </p>
              <div v-if="product.optionGroups?.length" class="tw:mt-1 tw:flex tw:flex-wrap tw:gap-0.5">
                <span
                  v-for="group in product.optionGroups"
                  :key="group.id"
                  class="tw:text-[10px] tw:rounded tw:px-1 tw:py-0.5 tw:bg-slate-500/10 tw:text-slate-400"
                >{{ group.name }}</span>
              </div>
            </div>
          </article>
        </div>
      </div>
    </div>

    <!-- ── Cart bar ─────────────────────────────────────────────── -->
    <button
      class="tw:flex tw:items-center tw:justify-between tw:px-4 tw:py-4 tw:shrink-0 tw:border-t tw:w-full tw:transition-colors"
      :class="
        cartItemCount > 0
          ? 'tw:bg-emerald-600 tw:hover:bg-emerald-700'
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
            class="tw:absolute tw:-top-2 tw:-right-2 tw:h-4 tw:min-w-4 tw:px-0.5 tw:rounded-full tw:bg-white tw:text-emerald-700 tw:text-[10px] tw:font-bold tw:flex tw:items-center tw:justify-center"
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

  <!-- ── Options Drawer ───────────────────────────────────────── -->
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
          class="tw:h-10 tw:w-10 tw:rounded-lg tw:object-cover tw:shrink-0"
        />
        <div class="tw:min-w-0">
          <p class="tw:font-semibold tw:text-sm tw:truncate">{{ drawerProduct?.name }}</p>
          <p class="tw:text-xs tw:text-emerald-400">{{ drawerProduct ? formatVnd(drawerProduct.price) : '' }}</p>
        </div>
      </div>
    </template>

    <div class="tw:overflow-y-auto tw:space-y-4 tw:pb-2">
      <!-- Quantity -->
      <div>
        <p class="tw:text-sm tw:font-semibold tw:mb-2">{{ t('orders.create.optionsDialog.quantity') }}</p>
        <div class="tw:flex tw:items-center tw:gap-4">
          <button
            class="tw:h-10 tw:w-10 tw:rounded-xl tw:border tw:flex tw:items-center tw:justify-center tw:text-muted tw:transition tw:active:scale-90"
            style="border-color: var(--app-border)"
            @click="drawerQuantity = Math.max(1, drawerQuantity - 1)"
          >
            <iconify icon="ph:minus-bold" />
          </button>
          <span class="tw:text-xl tw:font-bold tw:w-8 tw:text-center">{{ drawerQuantity }}</span>
          <button
            class="tw:h-10 tw:w-10 tw:rounded-xl tw:border tw:flex tw:items-center tw:justify-center tw:text-muted tw:transition tw:active:scale-90"
            style="border-color: var(--app-border)"
            @click="drawerQuantity++"
          >
            <iconify icon="ph:plus-bold" />
          </button>
        </div>
      </div>

      <!-- Dynamic option groups -->
      <div v-for="group in drawerProduct?.optionGroups ?? []" :key="group.id">
        <p class="tw:text-sm tw:font-semibold tw:mb-2">
          {{ group.name }}
          <span v-if="group.isRequired" class="tw:text-red-400 tw:text-xs tw:ml-1">*</span>
        </p>
        <div class="tw:grid tw:grid-cols-2 tw:gap-2">
          <button
            v-for="val in group.values"
            :key="val.id"
            class="tw:flex tw:items-center tw:gap-2 tw:rounded-xl tw:border tw:px-3 tw:py-2.5 tw:text-sm tw:transition-colors tw:text-left"
            :class="
              drawerIsSelected(group, val.id)
                ? 'tw:border-emerald-500 tw:bg-emerald-500/15 tw:text-emerald-300'
                : 'tw:text-muted tw:hover:border-slate-400/40'
            "
            :style="drawerIsSelected(group, val.id) ? '' : 'border-color: var(--app-border)'"
            @click="drawerToggleValue(group, val.id)"
          >
            <iconify
              :icon="drawerIsSelected(group, val.id) ? 'ph:check-circle-fill' : 'ph:circle'"
              class="tw:shrink-0"
            />
            <span class="tw:flex-1 tw:truncate">{{ val.label }}</span>
            <span v-if="val.priceAdjustment" class="tw:text-xs tw:shrink-0 tw:opacity-70">
              +{{ formatVnd(val.priceAdjustment) }}
            </span>
          </button>
        </div>
      </div>

      <!-- Serving -->
      <div>
        <p class="tw:text-sm tw:font-semibold tw:mb-2">{{ t('orders.create.optionsDialog.serving') }}</p>
        <div class="tw:grid tw:grid-cols-2 tw:gap-2">
          <button
            class="tw:flex tw:items-center tw:justify-center tw:gap-2 tw:rounded-xl tw:border tw:py-2.5 tw:text-sm tw:transition-colors"
            :class="
              !drawerTakeaway
                ? 'tw:border-emerald-500 tw:bg-emerald-500/15 tw:text-emerald-300'
                : 'tw:text-muted'
            "
            :style="!drawerTakeaway ? '' : 'border-color: var(--app-border)'"
            @click="drawerTakeaway = false"
          >
            <iconify icon="ph:coffee-bold" />
            <span>{{ t('orders.serving.dineIn') }}</span>
          </button>
          <button
            class="tw:flex tw:items-center tw:justify-center tw:gap-2 tw:rounded-xl tw:border tw:py-2.5 tw:text-sm tw:transition-colors"
            :class="
              drawerTakeaway
                ? 'tw:border-emerald-500 tw:bg-emerald-500/15 tw:text-emerald-300'
                : 'tw:text-muted'
            "
            :style="drawerTakeaway ? '' : 'border-color: var(--app-border)'"
            @click="drawerTakeaway = true"
          >
            <iconify icon="ph:bag-bold" />
            <span>{{ t('orders.serving.takeaway') }}</span>
          </button>
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
        <span>{{ t('orders.create.optionsDialog.addToCart') }}</span>
      </prime-button>
    </div>
  </prime-drawer>

  <!-- ── Cart Drawer ──────────────────────────────────────────── -->
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
          <prime-badge v-if="cartItemCount > 0" :value="cartItemCount" severity="success" />
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
                ? 'tw:border-emerald-500/60 tw:bg-emerald-500/5'
                : ''
          "
          :style="item.isFreeGift || isItemDiscounted(item) ? '' : 'border-color: var(--app-border)'"
        >
          <div class="tw:flex tw:items-start tw:gap-2">
            <div class="tw:flex-1 tw:min-w-0">
              <p class="tw:text-sm tw:font-medium tw:leading-snug">{{ item.productName }}</p>
              <p class="tw:text-xs tw:mt-0.5" :class="item.isFreeGift ? 'tw:text-amber-400' : 'tw:text-emerald-400'">
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
            <span v-else-if="isItemDiscounted(item)" class="tw:text-xs tw:text-emerald-400">
              {{ t('orders.create.promoBadge') }}
            </span>
            <span v-else />
            <span class="tw:text-sm tw:font-semibold"
              :class="item.isFreeGift ? 'tw:text-amber-400' : 'tw:text-emerald-400'">
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
            <iconify icon="ph:tag-bold" class="tw:text-emerald-400" />
            {{ promoInfo.code }}
          </span>
          <span v-if="freeItemSelection" class="tw:text-amber-400">
            {{ t('orders.create.freeItem') }}
          </span>
          <span v-else-if="promoInfo.estimatedDiscount" class="tw:text-emerald-400">
            -{{ formatVnd(promoInfo.estimatedDiscount) }}
          </span>
        </div>
        <div class="tw:flex tw:justify-between tw:font-bold tw:border-t tw:pt-2" style="border-color: var(--app-border)">
          <span>{{ t('orders.create.total') }}</span>
          <span class="tw:text-emerald-400 tw:text-base">{{ formatVnd(cartFinal) }}</span>
        </div>
      </div>

      <!-- Guest count -->
      <div class="tw:flex tw:items-center tw:gap-2">
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
            <iconify icon="ph:ticket-bold" class="tw:text-emerald-400" />
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
          class="tw:flex tw:items-center tw:justify-between tw:rounded-xl tw:border tw:border-emerald-500/30 tw:bg-emerald-500/10 tw:px-3 tw:py-2"
        >
          <div class="tw:flex tw:items-center tw:gap-2 tw:min-w-0">
            <iconify icon="ph:tag-bold" class="tw:text-emerald-400 tw:shrink-0" />
            <span class="tw:font-medium tw:text-sm tw:text-emerald-300">{{ promoInfo.code }}</span>
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
          <p class="tw:text-xs tw:font-medium tw:mb-2 tw:flex tw:items-center tw:gap-1 tw:text-emerald-400">
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
                  ? 'tw:border-emerald-500 tw:bg-emerald-500/10 tw:text-emerald-300'
                  : 'tw:text-muted'
              "
              :style="freeItemSelection?._key === item._key ? '' : 'border-color: var(--app-border)'"
              @click="freeItemSelection = freeItemSelection?._key === item._key ? null : item"
            >
              <span class="tw:flex tw:items-center tw:gap-1.5">
                <iconify :icon="freeItemSelection?._key === item._key ? 'ph:check-circle-fill' : 'ph:circle'" />
                {{ item.productName }}
              </span>
              <span class="tw:text-xs tw:font-semibold tw:text-emerald-400">{{ formatVnd(item.unitPrice) }}</span>
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

      <p v-if="!sessionId && !sessionLoading" class="tw:text-xs tw:text-muted tw:text-center">
        {{ t('orders.create.tableHint') }}
      </p>
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
