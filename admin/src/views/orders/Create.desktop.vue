<script setup>
import { createAdminOrder, applyPromotionAdmin } from '@/services/order.service'
import { appCard, cardRing } from '../../layout/ui'
import OrderOptionsDialog from '@/components/orders/OrderOptionsDialog.vue'
import FindPromosDialog from '@/components/orders/FindPromosDialog.vue'
import OrderTypeStep from '@/components/orders/wizard/OrderTypeStep.vue'
import OrderContextStep from '@/components/orders/wizard/OrderContextStep.vue'

const router = useRouter()
const toast = useToast()
const { t } = useI18n()

// ── Wizard state ──────────────────────────────────────────────────
const step = ref(1) // 1=type 2=context 3=menu 4=confirm (desktop: 3+4 merged)
const contextStepRef = ref(null)

// ── Composables ───────────────────────────────────────────────────
const {
  tables, menuCategories, loadingMenu, loadingTables,
  selectedTableId, sessionId, sessionHadExisting, sessionLoading, sessionError,
  searchQuery, collapsedCategories, visibleCategories, orderLabel,
  onTableSelect, toggleCategory, loadData,
} = useMenuSession()

const {
  cart, showOptionsDialog, selectedProduct,
  cartTotal, cartItemCount, defaultGuestCount, productCategoryMap,
  formatVnd, optionsLabel, cartQuantity,
  changeQty, clearCart, addToCart, handleAddToCart,
} = useOrderCart(menuCategories)

const {
  promoCode, promoInfo, promoLoading, promoError,
  findPromosVisible, publicPromos, publicPromosLoading,
  freeItemSelection, freeItemPickerPool, cartDiscount, cartFinal,
  formatPromotionValue, isPromoAvailable, promoDisableReason, isItemDiscounted,
  applyPromoCode, clearPromo, openFindPromosDialog, selectPromo,
} = useOrderPromotion(cart, cartTotal, productCategoryMap, menuCategories)

// ── Order type + customer context ─────────────────────────────────
const orderType    = ref('DINE_IN')
const customerName = ref('')
const customerPhone = ref('')
const deliveryAddress = ref('')
const deliveryNote  = ref('')

// ── Local state ───────────────────────────────────────────────────
const notificationStore = useNotificationStore()
const guestCount = ref(null)
const placing    = ref(false)
const errorMessage = ref('')

const canPlaceOrder = computed(() => {
  if (orderType.value === 'DINE_IN') return !!sessionId.value && cart.value.length > 0 && !placing.value
  return cart.value.length > 0 && !placing.value
})

// ── Wizard navigation ─────────────────────────────────────────────
const goNext = () => {
  if (step.value === 2) {
    const valid = contextStepRef.value?.validateDelivery?.() ?? true
    if (!valid) return
    // DineIn requires a session
    if (orderType.value === 'DINE_IN' && !sessionId.value && !sessionLoading.value) return
  }
  step.value++
}

const goBack = () => { step.value-- }

const onTypeSelected = () => { step.value = 2 }

// ── Place order ───────────────────────────────────────────────────
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
      guestCount: guestCount.value != null ? Number(guestCount.value) : defaultGuestCount.value || null,
      promoCode: promoCode.value.trim() || null,
      customerName: customerName.value || null,
      customerPhone: customerPhone.value || null,
      deliveryAddress: deliveryAddress.value || null,
      deliveryNote: deliveryNote.value || null,
    })

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

    toast.add({ severity: 'success', summary: t('orders.create.submit'), detail: `#${res.data.orderNumber}`, life: 3000 })
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
  <section class="tw:space-y-4">
    <!-- Header -->
    <div class="tw:flex tw:items-center tw:gap-4">
      <prime-button :class="btnIcon" severity="secondary" text @click="router.push({ name: 'orders' })">
        <iconify icon="ph:arrow-left-bold" />
      </prime-button>
      <div>
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-primary-300">
          {{ t('orders.breadcrumb') }}
        </p>
        <h1 class="tw:text-2xl tw:font-semibold">{{ t('orders.create.title') }}</h1>
      </div>
    </div>

    <!-- Step indicator -->
    <div :class="[appCard, cardRing, 'tw:px-4 tw:py-3 tw:rounded-md']">
      <div class="tw:flex tw:items-center tw:gap-0">
        <div
          v-for="(label, i) in [
            t('orders.create.wizard.step1'),
            t('orders.create.wizard.step2'),
            t('orders.create.wizard.step3'),
          ]"
          :key="i"
          class="tw:flex tw:items-center"
        >
          <div class="tw:flex tw:items-center tw:gap-2">
            <div
              class="tw:h-6 tw:w-6 tw:rounded-full tw:flex tw:items-center tw:justify-center tw:text-xs tw:font-bold tw:shrink-0"
              :class="step > i + 1
                ? 'tw:bg-primary-500 tw:text-white'
                : step === i + 1
                  ? 'tw:bg-primary-500/20 tw:text-primary-400 tw:ring-1 tw:ring-primary-500'
                  : 'tw:bg-white/5 tw:text-muted'"
            >
              <iconify v-if="step > i + 1" icon="ph:check-bold" class="tw:text-[11px]" />
              <span v-else>{{ i + 1 }}</span>
            </div>
            <span
              class="tw:text-sm tw:hidden tw:sm:block"
              :class="step === i + 1 ? 'tw:font-medium' : 'tw:text-muted'"
            >{{ label }}</span>
          </div>
          <div v-if="i < 2" class="tw:h-px tw:w-6 tw:mx-2 tw:bg-white/10" />
        </div>
      </div>
    </div>

    <!-- Step 1: Order type -->
    <div v-if="step === 1" :class="[appCard, cardRing, 'tw:p-5 tw:rounded-md']">
      <OrderTypeStep v-model="orderType" @next="onTypeSelected" />
    </div>

    <!-- Step 2: Context -->
    <div v-else-if="step === 2" :class="[appCard, cardRing, 'tw:p-5 tw:rounded-md tw:space-y-4']">
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
        v-model:customerName="customerName"
        v-model:customerPhone="customerPhone"
        v-model:deliveryAddress="deliveryAddress"
        v-model:deliveryNote="deliveryNote"
        @update:selectedTableId="selectedTableId = $event"
        @update:guestCount="guestCount = $event"
        @tableSelect="onTableSelect"
      />

      <div class="tw:flex tw:justify-between tw:pt-2">
        <prime-button severity="secondary" outlined size="small" @click="goBack">
          <iconify icon="ph:arrow-left-bold" />
          <span>{{ t('orders.create.wizard.back') }}</span>
        </prime-button>
        <prime-button
          size="small"
          :disabled="orderType === 'DINE_IN' && !sessionId && !sessionLoading"
          @click="goNext"
        >
          <span>{{ t('orders.create.wizard.next') }}</span>
          <iconify icon="ph:arrow-right-bold" />
        </prime-button>
      </div>
    </div>

    <!-- Step 3: Menu + Cart -->
    <div v-else-if="step === 3">
      <!-- Search bar -->
      <div :class="[appCard, cardRing, 'tw:px-4 tw:py-3 tw:rounded-md tw:flex tw:items-center tw:gap-3 tw:mb-4']">
        <prime-button severity="secondary" outlined size="small" @click="goBack">
          <iconify icon="ph:arrow-left-bold" />
          <span>{{ t('orders.create.wizard.back') }}</span>
        </prime-button>
        <div class="tw:flex tw:items-center tw:gap-1.5">
          <iconify icon="prime:search" class="tw:text-lg tw:opacity-60" />
          <prime-input-text
            v-model="searchQuery"
            :placeholder="t('orders.create.search')"
            size="small"
          />
        </div>
        <!-- Order type badge -->
        <prime-tag :severity="orderType === 'DINE_IN' ? 'secondary' : orderType === 'TAKEAWAY' ? 'warn' : 'info'">
          <iconify :icon="orderType === 'DINE_IN' ? 'ph:fork-knife-bold' : orderType === 'TAKEAWAY' ? 'ph:bag-bold' : 'ph:motorcycle-bold'" class="tw:mr-1" />
          {{ t(`orders.create.orderType.${orderType}`) }}
          <span v-if="orderType === 'DINE_IN' && orderLabel"> · {{ orderLabel }}</span>
          <span v-else-if="customerName"> · {{ customerName }}</span>
        </prime-tag>
      </div>

      <!-- Main grid -->
      <div class="tw:grid tw:gap-5 tw:lg:grid-cols-12">
        <!-- LEFT: Menu -->
        <section class="tw:lg:col-span-8 tw:space-y-4">
          <div v-if="loadingMenu" class="tw:space-y-6">
            <div v-for="n in 2" :key="n" class="tw:space-y-3">
              <div class="tw:h-6 tw:w-32 tw:rounded-lg tw:animate-pulse" style="background: var(--app-bg-subtle)" />
              <div class="tw:grid tw:grid-cols-2 tw:gap-3 sm:tw:grid-cols-3">
                <div v-for="m in 3" :key="m" class="tw:animate-pulse tw:rounded-2xl app-panel tw:border tw:overflow-hidden">
                  <div class="tw:h-32 tw:w-full" style="background: var(--app-bg-subtle)" />
                  <div class="tw:p-3 tw:space-y-2">
                    <div class="tw:h-4 tw:w-3/4 tw:rounded" style="background: var(--app-bg-subtle)" />
                    <div class="tw:h-3 tw:w-1/2 tw:rounded" style="background: var(--app-bg-subtle)" />
                  </div>
                </div>
              </div>
            </div>
          </div>

          <prime-panel
            v-else-if="visibleCategories.length === 0"
            class="app-panel tw:rounded-2xl tw:border tw:p-12 tw:text-center tw:text-muted"
          >
            <iconify icon="ph:magnifying-glass-bold" class="tw:text-3xl tw:mb-3 tw:block tw:mx-auto tw:opacity-40" />
            {{ t('orders.create.noProducts') }}
          </prime-panel>

          <prime-panel
            v-else
            v-for="category in visibleCategories"
            :key="category.id"
            :header="category.name"
            toggleable
            :pt="{ root: { class: `${appCard} ${cardRing}` } }"
          >
            <div
              v-show="!collapsedCategories[category.id]"
              class="tw:grid tw:grid-cols-2 tw:md:grid-cols-3 tw:xl:grid-cols-4 tw:2xl:grid-cols-5 tw:gap-3 sm:tw:grid-cols-3 tw:p-3 tw:pt-0"
            >
              <article
                v-for="product in category.filteredProducts"
                :key="product.id"
                class="tw:group tw:flex tw:h-full tw:flex-col tw:overflow-hidden tw:rounded-xl tw:border tw:cursor-pointer tw:transition-all tw:hover:-translate-y-0.5 tw:hover:border-primary-500/50"
                style="border-color: var(--app-border); background: var(--app-bg-subtle)"
                @click="handleAddToCart(product)"
              >
                <div class="tw:relative tw:overflow-hidden tw:shrink-0">
                  <img
                    v-if="product.imageUrl"
                    :src="product.imageUrl"
                    :alt="product.name"
                    class="tw:h-32 tw:w-full tw:object-cover"
                  />
                  <div v-else class="tw:h-32 tw:w-full tw:flex tw:items-center tw:justify-center" style="background: var(--app-bg)">
                    <iconify icon="ph:coffee-bold" class="tw:text-3xl tw:text-primary-400/20" />
                  </div>
                  <div
                    v-if="cartQuantity(product.id) > 0"
                    class="tw:absolute tw:top-2 tw:right-2 tw:h-5 tw:min-w-5 tw:px-1 tw:rounded-full tw:bg-primary-500 tw:flex tw:items-center tw:justify-center tw:text-xs tw:font-bold tw:text-white tw:shadow-lg"
                  >
                    {{ cartQuantity(product.id) }}
                  </div>
                </div>
                <div class="tw:flex tw:flex-1 tw:flex-col tw:p-2.5">
                  <h3 class="tw:text-xs tw:font-semibold tw:line-clamp-2 tw:leading-snug">{{ product.name }}</h3>
                  <p class="tw:mt-1 tw:text-xs tw:font-semibold tw:text-primary-400">{{ formatVnd(product.price) }}</p>
                  <div v-if="product.variantGroups?.length" class="tw:mt-1.5 tw:flex tw:flex-wrap tw:gap-1">
                    <span
                      v-for="group in product.variantGroups"
                      :key="group.id"
                      class="tw:rounded tw:px-1 tw:py-0.5 tw:text-xs tw:bg-slate-500/10 tw:text-slate-400"
                    >{{ group.name }}</span>
                  </div>
                  <div class="tw:flex-1" />
                  <div class="tw:flex tw:justify-end tw:mt-2">
                    <prime-button size="small" rounded text severity="success" @click.stop="handleAddToCart(product)" :class="btnIcon">
                      <iconify icon="prime:plus" />
                    </prime-button>
                  </div>
                </div>
              </article>
            </div>
          </prime-panel>
        </section>

        <!-- RIGHT: Cart -->
        <aside class="tw:lg:col-span-4 tw:lg:self-start tw:lg:sticky tw:lg:top-6">
          <div :class="[appCard, cardRing, 'tw:p-5 tw:rounded-md']">
            <!-- Cart header -->
            <div class="tw:flex tw:items-center tw:justify-between tw:mb-4">
              <h2 class="tw:text-base tw:font-semibold tw:flex tw:items-center tw:gap-2">
                {{ t('orders.create.cart') }}
                <prime-badge v-if="cartItemCount > 0" :value="cartItemCount" severity="success" />
              </h2>
              <prime-button v-if="cart.length > 0" size="small" severity="danger" outlined @click="clearCart" :class="btnIcon">
                <iconify icon="prime:trash" />
              </prime-button>
            </div>

            <!-- Order label -->
            <div v-if="orderType === 'DINE_IN' && orderLabel" class="tw:mb-3">
              <prime-tag :value="orderLabel" severity="secondary">
                <iconify icon="prime:table" />
              </prime-tag>
            </div>
            <div v-else-if="orderType !== 'DINE_IN' && (customerName || customerPhone)" class="tw:mb-3">
              <prime-tag severity="secondary">
                <iconify :icon="orderType === 'TAKEAWAY' ? 'ph:bag-bold' : 'ph:motorcycle-bold'" class="tw:mr-1" />
                {{ customerName || customerPhone }}
              </prime-tag>
            </div>

            <!-- Empty state -->
            <div v-if="cart.length === 0" class="tw:py-8 tw:text-center tw:text-muted tw:text-sm tw:rounded-xl tw:border tw:border-dashed" style="border-color: var(--app-border)">
              {{ t('orders.create.cartEmpty') }}<br />
              <span class="tw:text-xs tw:opacity-60">{{ t('orders.create.cartEmptyHint') }}</span>
            </div>

            <!-- Cart items -->
            <div v-else class="tw:space-y-2">
              <div
                v-for="item in cart"
                :key="item._key"
                class="tw:rounded-xl tw:border tw:p-3 tw:transition-colors"
                :class="item.isFreeGift ? 'tw:border-amber-500/40 tw:bg-amber-500/5' : isItemDiscounted(item) ? 'tw:border-primary-500/60 tw:bg-primary-500/5' : ''"
                :style="item.isFreeGift || isItemDiscounted(item) ? '' : 'border-color: var(--app-border)'"
              >
                <div class="tw:flex tw:items-start tw:gap-2">
                  <div class="tw:flex-1 tw:min-w-0">
                    <div class="tw:flex tw:items-center tw:gap-1.5 tw:flex-wrap">
                      <p class="tw:text-sm tw:font-medium tw:leading-snug">{{ item.productName }}</p>
                      <span v-if="item.isFreeGift" class="tw:inline-flex tw:items-center tw:gap-0.5 tw:text-xs tw:px-1.5 tw:py-0.5 tw:rounded tw:bg-amber-500/15 tw:text-amber-400 tw:font-medium">
                        <iconify icon="ph:gift-fill" class="tw:text-[10px]" />
                        {{ t('orders.create.freeBadge') }}
                      </span>
                      <span v-else-if="isItemDiscounted(item)" class="tw:inline-flex tw:items-center tw:gap-0.5 tw:text-xs tw:px-1.5 tw:py-0.5 tw:rounded tw:bg-primary-500/15 tw:text-primary-400 tw:font-medium">
                        <iconify icon="ph:tag-simple-fill" class="tw:text-[10px]" />
                        {{ t('orders.create.promoBadge') }}
                      </span>
                    </div>
                    <p class="tw:text-xs tw:mt-0.5" :class="item.isFreeGift ? 'tw:text-amber-400 tw:line-through tw:opacity-60' : 'tw:text-primary-400'">
                      {{ item.isFreeGift ? formatVnd(freeItemSelection?.unitPrice ?? 0) : formatVnd(item.unitPrice + (item.optionAdjustment ?? 0)) }}
                    </p>
                    <p v-if="item.isFreeGift" class="tw:text-xs tw:text-amber-400 tw:font-semibold">{{ t('orders.create.freeBadge') }}</p>
                    <p v-if="optionsLabel(item)" class="tw:text-xs tw:text-amber-400 tw:mt-0.5 tw:leading-snug">{{ optionsLabel(item) }}</p>
                    <span v-if="item.isTakeaway" class="tw:inline-block tw:mt-1 tw:text-xs tw:px-1.5 tw:py-0.5 tw:rounded tw:bg-sky-500/10 tw:text-sky-400">{{ t('orders.create.takeaway') }}</span>
                  </div>
                  <div v-if="item.isFreeGift" class="tw:shrink-0">
                    <prime-button size="small" text rounded severity="secondary" :class="btnIcon" @click="freeItemSelection = null">
                      <iconify icon="prime:times" class="tw:text-xs" />
                    </prime-button>
                  </div>
                  <div v-else class="tw:flex tw:items-center tw:gap-1 tw:shrink-0">
                    <prime-button size="small" text rounded severity="secondary" @click="changeQty(item._key, -1)" :class="btnIcon">
                      <iconify icon="prime:minus" />
                    </prime-button>
                    <span class="tw:text-sm tw:font-bold tw:w-5 tw:text-center">{{ item.quantity }}</span>
                    <prime-button size="small" text rounded severity="secondary" @click="changeQty(item._key, 1)" :class="btnIcon">
                      <iconify icon="prime:plus" />
                    </prime-button>
                  </div>
                </div>
                <div class="tw:flex tw:justify-end tw:mt-1.5">
                  <span class="tw:text-sm tw:font-semibold" :class="item.isFreeGift ? 'tw:text-amber-400' : 'tw:text-primary-400'">
                    {{ item.isFreeGift ? '0 ₫' : formatVnd((item.unitPrice + (item.optionAdjustment ?? 0)) * item.quantity) }}
                  </span>
                </div>
              </div>

              <!-- Total summary -->
              <div class="tw:rounded-xl tw:p-3 tw:mt-1" style="background: var(--app-bg-subtle)">
                <div class="tw:flex tw:items-center tw:justify-between tw:text-sm tw:mb-1">
                  <span class="tw:text-muted">{{ t('orders.create.subtotal') }}</span>
                  <span class="tw:font-medium">{{ formatVnd(cartTotal) }}</span>
                </div>
                <div class="tw:flex tw:items-center tw:justify-between tw:text-xs tw:mb-2">
                  <span class="tw:text-muted">{{ t('orders.create.serviceCharge') }}</span>
                  <span class="tw:text-muted">{{ t('orders.create.free') }}</span>
                </div>
                <div class="tw:border-t tw:pt-2" style="border-color: var(--app-border)">
                  <div v-if="promoInfo" class="tw:flex tw:items-center tw:justify-between tw:text-xs tw:mb-1.5">
                    <span class="tw:flex tw:items-center tw:gap-1 tw:text-muted">
                      <iconify icon="ph:tag-bold" class="tw:text-primary-400" />
                      {{ promoInfo.code }}
                    </span>
                    <span v-if="freeItemSelection" class="tw:flex tw:items-center tw:gap-1 tw:text-amber-400 tw:font-medium">
                      <iconify icon="ph:gift-bold" class="tw:text-xs" />
                      {{ t('orders.create.freeItem') }}
                    </span>
                    <span v-else-if="promoInfo.estimatedDiscount" class="tw:text-primary-400 tw:font-medium">-{{ formatVnd(promoInfo.estimatedDiscount) }}</span>
                    <span v-else class="tw:text-muted tw:italic">{{ t('orders.create.willBeApplied') }}</span>
                  </div>
                  <div class="tw:flex tw:items-center tw:justify-between tw:font-bold">
                    <span>{{ t('orders.create.total') }}</span>
                    <div class="tw:text-right">
                      <span v-if="promoInfo?.estimatedDiscount && !freeItemSelection" class="tw:text-muted tw:line-through tw:text-xs tw:font-normal tw:mr-1">{{ formatVnd(cartTotal) }}</span>
                      <span class="tw:text-primary-400 tw:text-base">{{ formatVnd(cartFinal) }}</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <!-- Find promotions -->
            <div class="tw:flex tw:items-center tw:justify-between tw:mt-4 tw:mb-1">
              <span class="tw:flex tw:items-center tw:gap-1.5 tw:text-xs tw:text-muted">
                <iconify icon="ph:ticket-bold" class="tw:text-primary-400" />
                {{ t('orders.create.findPromotions') }}
              </span>
              <prime-button size="small" text severity="secondary" class="tw:text-xs tw:h-6! tw:px-2!" @click="openFindPromosDialog">
                <iconify icon="ph:magnifying-glass-bold" class="tw:text-xs" />
                <span>{{ t('orders.create.browse') }}</span>
              </prime-button>
            </div>

            <!-- Promo code -->
            <div v-if="cart.length > 0" class="tw:mt-3">
              <div v-if="promoInfo" class="tw:flex tw:items-center tw:justify-between tw:rounded-xl tw:border tw:border-primary-500/30 tw:bg-primary-500/10 tw:px-3 tw:py-2">
                <div class="tw:flex tw:items-center tw:gap-2 tw:min-w-0">
                  <iconify icon="ph:tag-bold" class="tw:text-primary-400 tw:shrink-0" />
                  <span class="tw:font-medium tw:text-sm tw:text-primary-300">{{ promoInfo.code }}</span>
                  <span class="tw:text-xs tw:text-muted tw:truncate">{{ promoInfo.name }}</span>
                </div>
                <prime-button size="small" text severity="secondary" :class="btnIcon" @click="clearPromo">
                  <iconify icon="prime:times" class="tw:text-xs" />
                </prime-button>
              </div>

              <div v-if="freeItemPickerPool.length > 0" class="tw:mt-2 tw:rounded-xl tw:border tw:p-3" style="border-color: var(--app-border)">
                <p class="tw:text-xs tw:font-medium tw:mb-2 tw:flex tw:items-center tw:gap-1 tw:text-primary-400">
                  <iconify icon="ph:gift-bold" />
                  {{ t('orders.create.selectFreeGift') }}
                </p>
                <div class="tw:flex tw:flex-col tw:gap-1.5">
                  <button
                    v-for="item in freeItemPickerPool"
                    :key="item._key"
                    class="tw:flex tw:items-center tw:justify-between tw:rounded-lg tw:border tw:px-3 tw:py-2 tw:text-left tw:text-sm tw:transition-colors tw:w-full"
                    :class="freeItemSelection?._key === item._key ? 'tw:border-primary-500 tw:bg-primary-500/10 tw:text-primary-300' : 'tw:hover:border-primary-500/40 tw:text-muted'"
                    :style="freeItemSelection?._key === item._key ? '' : 'border-color: var(--app-border)'"
                    @click="freeItemSelection = freeItemSelection?._key === item._key ? null : item"
                  >
                    <span class="tw:flex tw:items-center tw:gap-1.5">
                      <iconify v-if="freeItemSelection?._key === item._key" icon="ph:check-circle-fill" class="tw:text-primary-400 tw:shrink-0" />
                      <iconify v-else icon="ph:circle" class="tw:shrink-0" />
                      {{ item.productName }}
                    </span>
                    <span class="tw:text-xs tw:font-semibold tw:text-primary-400 tw:shrink-0">{{ formatVnd(item.unitPrice) }}</span>
                  </button>
                </div>
              </div>

              <div v-if="!promoInfo">
                <div class="tw:flex tw:gap-2">
                  <prime-input-text v-model="promoCode" :placeholder="t('orders.create.promoCode')" class="tw:flex-1" @keyup.enter="applyPromoCode" />
                  <prime-button severity="secondary" outlined :loading="promoLoading" :disabled="!promoCode.trim()" @click="applyPromoCode">{{ t('orders.create.apply') }}</prime-button>
                </div>
                <p v-if="promoError" class="tw:text-xs tw:text-red-400 tw:mt-1.5">{{ promoError }}</p>
              </div>
            </div>

            <!-- Error -->
            <prime-message v-if="errorMessage" severity="error" :closable="false" class="tw:mt-3">{{ errorMessage }}</prime-message>

            <!-- Place order -->
            <prime-button class="tw:w-full tw:mt-4" :loading="placing" :disabled="!canPlaceOrder" @click="placeOrder">
              <iconify icon="prime:check" />
              <span>{{ t('orders.create.wizard.confirm') }}</span>
            </prime-button>

            <p v-if="orderType === 'DINE_IN' && !sessionId && !sessionLoading" class="tw:text-xs tw:text-muted tw:text-center tw:mt-2">
              {{ t('orders.create.tableHint') }}
            </p>
          </div>
        </aside>
      </div>
    </div>
  </section>

  <OrderOptionsDialog
    v-model:visible="showOptionsDialog"
    :product="selectedProduct"
    @confirm="addToCart"
  />

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
