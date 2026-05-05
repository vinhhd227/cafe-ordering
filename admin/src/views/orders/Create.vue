<script setup>
import { createOrder, applyPromotionAdmin } from '@/services/order.service'
import { appCard, cardRing } from '../../layout/ui'
import OrderOptionsDialog from '@/components/orders/OrderOptionsDialog.vue'
import FindPromosDialog from '@/components/orders/FindPromosDialog.vue'

const router = useRouter()
const toast = useToast()
const { t } = useI18n()

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

// ── Local state ───────────────────────────────────────────────────
const notificationStore = useNotificationStore()
const guestCount = ref(null)
const placing = ref(false)
const errorMessage = ref('')

const canPlaceOrder = computed(
  () => !!sessionId.value && cart.value.length > 0 && !placing.value,
)

// ── Place order ───────────────────────────────────────────────────
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
        temperature: item.temperature ?? null,
        iceLevel: item.iceLevel ?? null,
        sugarLevel: item.sugarLevel ?? null,
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
  <section class="tw:space-y-4">
    <!-- Header -->
    <div class="tw:flex tw:items-center tw:gap-4">
      <prime-button :class="btnIcon" severity="secondary" text @click="router.push({ name: 'orders' })">
        <iconify icon="ph:arrow-left-bold" />
      </prime-button>
      <div>
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300">
          {{ t('orders.breadcrumb') }}
        </p>
        <h1 class="tw:text-2xl tw:font-semibold">{{ t('orders.create.title') }}</h1>
      </div>
    </div>

    <!-- Search / Table / Guest Count / Session bar -->
    <div
      :class="[appCard, cardRing, 'tw:px-4 tw:py-3 tw:rounded-md tw:flex tw:justify-between tw:items-center']"
    >
      <div class="tw:flex tw:items-center tw:gap-3">
        <div class="tw:flex tw:items-center tw:gap-1.5">
          <iconify icon="prime:search" class="tw:text-lg tw:opacity-60" />
          <prime-input-text
            v-model="searchQuery"
            :placeholder="t('orders.create.search')"
            size="small"
          />
        </div>
        <div class="tw:flex tw:items-center tw:gap-1.5">
          <iconify icon="ic:round-table-bar" class="tw:text-lg tw:opacity-60" />
          <prime-select
            v-model="selectedTableId"
            :options="tables"
            optionLabel="code"
            optionValue="id"
            :placeholder="t('orders.create.selectTable')"
            :disabled="sessionLoading"
            class="tw:w-40"
            @change="(e) => onTableSelect(e.value)"
            size="small"
          />
        </div>
        <div class="tw:flex tw:items-center tw:gap-1.5">
          <iconify icon="ph:users" class="tw:text-base tw:opacity-60" />
          <prime-input-number
            v-model="guestCount"
            :placeholder="
              defaultGuestCount > 0 ? String(defaultGuestCount) : t('orders.create.guestCountPlaceholder')
            "
            :min="1"
            :max="99"
            :useGrouping="false"
            inputClass="tw:text-sm tw:w-25"
            size="small"
          />
        </div>
      </div>
      <template v-if="sessionLoading">
        <prime-tag severity="secondary">
          <iconify icon="prime:spinner" />
          <span>{{ t('orders.create.connecting') }}</span>
        </prime-tag>
      </template>
      <template v-else-if="sessionId">
        <prime-tag v-if="sessionHadExisting" severity="info">
          <iconify icon="prime:info-circle" />
          <span>{{ t('orders.create.existingSession') }}</span>
        </prime-tag>
        <prime-tag v-else severity="success">
          <iconify icon="prime:check" />
          <span>{{ t('orders.create.sessionReady') }}</span>
        </prime-tag>
      </template>
    </div>

    <!-- Main grid -->
    <div class="tw:grid tw:gap-5 tw:lg:grid-cols-12">
      <!-- ── LEFT: Menu ──────────────────────────────────────── -->
      <section class="tw:lg:col-span-8 tw:space-y-4">
        <!-- Loading skeleton -->
        <div v-if="loadingMenu" class="tw:space-y-6">
          <div v-for="n in 2" :key="n" class="tw:space-y-3">
            <div class="tw:h-6 tw:w-32 tw:rounded-lg tw:animate-pulse" style="background: var(--app-bg-subtle)" />
            <div class="tw:grid tw:grid-cols-2 tw:gap-3 sm:tw:grid-cols-3">
              <div
                v-for="m in 3"
                :key="m"
                class="tw:animate-pulse tw:rounded-2xl app-panel tw:border tw:overflow-hidden"
              >
                <div class="tw:h-32 tw:w-full" style="background: var(--app-bg-subtle)" />
                <div class="tw:p-3 tw:space-y-2">
                  <div class="tw:h-4 tw:w-3/4 tw:rounded" style="background: var(--app-bg-subtle)" />
                  <div class="tw:h-3 tw:w-1/2 tw:rounded" style="background: var(--app-bg-subtle)" />
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Empty state -->
        <prime-panel
          v-else-if="visibleCategories.length === 0"
          class="app-panel tw:rounded-2xl tw:border tw:p-12 tw:text-center tw:text-muted"
        >
          <iconify icon="ph:magnifying-glass-bold" class="tw:text-3xl tw:mb-3 tw:block tw:mx-auto tw:opacity-40" />
          {{ t('orders.create.noProducts') }}
        </prime-panel>

        <!-- Category panels -->
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
              class="tw:group tw:flex tw:h-full tw:flex-col tw:overflow-hidden tw:rounded-xl tw:border tw:cursor-pointer tw:transition-all tw:hover:-translate-y-0.5 tw:hover:border-emerald-500/50"
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
                <div
                  v-else
                  class="tw:h-32 tw:w-full tw:flex tw:items-center tw:justify-center"
                  style="background: var(--app-bg)"
                >
                  <iconify icon="ph:coffee-bold" class="tw:text-3xl tw:text-emerald-400/20" />
                </div>
                <div
                  v-if="cartQuantity(product.id) > 0"
                  class="tw:absolute tw:top-2 tw:right-2 tw:h-5 tw:min-w-5 tw:px-1 tw:rounded-full tw:bg-emerald-500 tw:flex tw:items-center tw:justify-center tw:text-xs tw:font-bold tw:text-white tw:shadow-lg"
                >
                  {{ cartQuantity(product.id) }}
                </div>
              </div>

              <div class="tw:flex tw:flex-1 tw:flex-col tw:p-2.5">
                <h3 class="tw:text-xs tw:font-semibold tw:line-clamp-2 tw:leading-snug">
                  {{ product.name }}
                </h3>
                <p class="tw:mt-1 tw:text-xs tw:font-semibold tw:text-emerald-400">
                  {{ formatVnd(product.price) }}
                </p>
                <div
                  v-if="product.hasTemperatureOption || product.hasIceLevelOption || product.hasSugarLevelOption"
                  class="tw:mt-1.5 tw:flex tw:flex-wrap tw:gap-1"
                >
                  <span
                    v-if="product.hasTemperatureOption"
                    class="tw:rounded tw:px-1 tw:py-0.5 tw:text-xs tw:bg-orange-500/10 tw:text-orange-400"
                    >{{ t('orders.create.optionsDialog.temperature') }}</span
                  >
                  <span
                    v-if="product.hasIceLevelOption"
                    class="tw:rounded tw:px-1 tw:py-0.5 tw:text-xs tw:bg-sky-500/10 tw:text-sky-400"
                    >{{ t('orders.create.optionsDialog.iceLevel') }}</span
                  >
                  <span
                    v-if="product.hasSugarLevelOption"
                    class="tw:rounded tw:px-1 tw:py-0.5 tw:text-xs tw:bg-amber-500/10 tw:text-amber-400"
                    >{{ t('orders.create.optionsDialog.sugarLevel') }}</span
                  >
                </div>
                <div class="tw:flex-1" />
                <div class="tw:flex tw:justify-end tw:mt-2">
                  <prime-button
                    size="small"
                    rounded
                    text
                    severity="success"
                    @click.stop="handleAddToCart(product)"
                    :class="btnIcon"
                  >
                    <iconify icon="prime:plus" />
                  </prime-button>
                </div>
              </div>
            </article>
          </div>
        </prime-panel>
      </section>

      <!-- ── RIGHT: Cart ────────────────────────────────────── -->
      <aside class="tw:lg:col-span-4 tw:lg:self-start tw:lg:sticky tw:lg:top-6">
        <div :class="[appCard, cardRing, 'tw:p-5 tw:rounded-md']">
          <!-- Cart header -->
          <div class="tw:flex tw:items-center tw:justify-between tw:mb-4">
            <h2 class="tw:text-base tw:font-semibold tw:flex tw:items-center tw:gap-2">
              {{ t('orders.create.cart') }}
              <prime-badge v-if="cartItemCount > 0" :value="cartItemCount" severity="success" />
            </h2>
            <prime-button
              v-if="cart.length > 0"
              size="small"
              severity="danger"
              outlined
              @click="clearCart"
              :class="btnIcon"
            >
              <iconify icon="prime:trash" />
            </prime-button>
          </div>

          <!-- Order label -->
          <div v-if="orderLabel" class="tw:mb-3">
            <prime-tag :value="orderLabel" severity="secondary">
              <iconify icon="prime:table" />
            </prime-tag>
          </div>

          <!-- Empty state -->
          <div
            v-if="cart.length === 0"
            class="tw:py-8 tw:text-center tw:text-muted tw:text-sm tw:rounded-xl tw:border tw:border-dashed"
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
              class="tw:rounded-xl tw:border tw:p-3 tw:transition-colors"
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
                  <div class="tw:flex tw:items-center tw:gap-1.5 tw:flex-wrap">
                    <p class="tw:text-sm tw:font-medium tw:leading-snug">{{ item.productName }}</p>
                    <span
                      v-if="item.isFreeGift"
                      class="tw:inline-flex tw:items-center tw:gap-0.5 tw:text-xs tw:px-1.5 tw:py-0.5 tw:rounded tw:bg-amber-500/15 tw:text-amber-400 tw:font-medium"
                    >
                      <iconify icon="ph:gift-fill" class="tw:text-[10px]" />
                      {{ t('orders.create.freeBadge') }}
                    </span>
                    <span
                      v-else-if="isItemDiscounted(item)"
                      class="tw:inline-flex tw:items-center tw:gap-0.5 tw:text-xs tw:px-1.5 tw:py-0.5 tw:rounded tw:bg-emerald-500/15 tw:text-emerald-400 tw:font-medium"
                    >
                      <iconify icon="ph:tag-simple-fill" class="tw:text-[10px]" />
                      {{ t('orders.create.promoBadge') }}
                    </span>
                  </div>
                  <p
                    class="tw:text-xs tw:mt-0.5"
                    :class="
                      item.isFreeGift
                        ? 'tw:text-amber-400 tw:line-through tw:opacity-60'
                        : 'tw:text-emerald-400'
                    "
                  >
                    {{
                      item.isFreeGift
                        ? formatVnd(freeItemSelection?.unitPrice ?? 0)
                        : formatVnd(item.unitPrice)
                    }}
                  </p>
                  <p v-if="item.isFreeGift" class="tw:text-xs tw:text-amber-400 tw:font-semibold">
                    {{ t('orders.create.freeBadge') }}
                  </p>
                  <p
                    v-if="optionsLabel(item)"
                    class="tw:text-xs tw:text-amber-400 tw:mt-0.5 tw:leading-snug"
                  >
                    {{ optionsLabel(item) }}
                  </p>
                  <span
                    v-if="item.isTakeaway"
                    class="tw:inline-block tw:mt-1 tw:text-xs tw:px-1.5 tw:py-0.5 tw:rounded tw:bg-sky-500/10 tw:text-sky-400"
                    >{{ t('orders.create.takeaway') }}</span
                  >
                </div>
                <!-- Free gift: just remove -->
                <div v-if="item.isFreeGift" class="tw:shrink-0">
                  <prime-button
                    size="small" text rounded severity="secondary" :class="btnIcon"
                    @click="freeItemSelection = null"
                  >
                    <iconify icon="prime:times" class="tw:text-xs" />
                  </prime-button>
                </div>
                <!-- Regular item: qty controls -->
                <div v-else class="tw:flex tw:items-center tw:gap-1 tw:shrink-0">
                  <prime-button
                    size="small" text rounded severity="secondary"
                    @click="changeQty(item._key, -1)" :class="btnIcon"
                  >
                    <iconify icon="prime:minus" />
                  </prime-button>
                  <span class="tw:text-sm tw:font-bold tw:w-5 tw:text-center">{{ item.quantity }}</span>
                  <prime-button
                    size="small" text rounded severity="secondary"
                    @click="changeQty(item._key, 1)" :class="btnIcon"
                  >
                    <iconify icon="prime:plus" />
                  </prime-button>
                </div>
              </div>
              <div class="tw:flex tw:justify-end tw:mt-1.5">
                <span
                  class="tw:text-sm tw:font-semibold"
                  :class="item.isFreeGift ? 'tw:text-amber-400' : 'tw:text-emerald-400'"
                >
                  {{ item.isFreeGift ? '0 ₫' : formatVnd(item.unitPrice * item.quantity) }}
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
                <!-- Promo row -->
                <div
                  v-if="promoInfo"
                  class="tw:flex tw:items-center tw:justify-between tw:text-xs tw:mb-1.5"
                >
                  <span class="tw:flex tw:items-center tw:gap-1 tw:text-muted">
                    <iconify icon="ph:tag-bold" class="tw:text-emerald-400" />
                    {{ promoInfo.code }}
                  </span>
                  <span
                    v-if="freeItemSelection"
                    class="tw:flex tw:items-center tw:gap-1 tw:text-amber-400 tw:font-medium"
                  >
                    <iconify icon="ph:gift-bold" class="tw:text-xs" />
                    {{ t('orders.create.freeItem') }}
                  </span>
                  <span
                    v-else-if="promoInfo.estimatedDiscount"
                    class="tw:text-emerald-400 tw:font-medium"
                  >
                    -{{ formatVnd(promoInfo.estimatedDiscount) }}
                  </span>
                  <span v-else class="tw:text-muted tw:italic">{{
                    t('orders.create.willBeApplied')
                  }}</span>
                </div>
                <div class="tw:flex tw:items-center tw:justify-between tw:font-bold">
                  <span>{{ t('orders.create.total') }}</span>
                  <div class="tw:text-right">
                    <span
                      v-if="promoInfo?.estimatedDiscount && !freeItemSelection"
                      class="tw:text-muted tw:line-through tw:text-xs tw:font-normal tw:mr-1"
                      >{{ formatVnd(cartTotal) }}</span
                    >
                    <span class="tw:text-emerald-400 tw:text-base">{{ formatVnd(cartFinal) }}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Find promotions -->
          <div class="tw:flex tw:items-center tw:justify-between tw:mt-4 tw:mb-1">
            <span class="tw:flex tw:items-center tw:gap-1.5 tw:text-xs tw:text-muted">
              <iconify icon="ph:ticket-bold" class="tw:text-emerald-400" />
              {{ t('orders.create.findPromotions') }}
            </span>
            <prime-button
              size="small" text severity="secondary" class="tw:text-xs tw:h-6! tw:px-2!"
              @click="openFindPromosDialog"
            >
              <iconify icon="ph:magnifying-glass-bold" class="tw:text-xs" />
              <span>{{ t('orders.create.browse') }}</span>
            </prime-button>
          </div>

          <!-- Promo code -->
          <div v-if="cart.length > 0" class="tw:mt-3">
            <!-- Applied promo tag -->
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
              class="tw:mt-2 tw:rounded-xl tw:border tw:p-3"
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
                      : 'tw:hover:border-emerald-500/40 tw:text-muted'
                  "
                  :style="freeItemSelection?._key === item._key ? '' : 'border-color: var(--app-border)'"
                  @click="freeItemSelection = freeItemSelection?._key === item._key ? null : item"
                >
                  <span class="tw:flex tw:items-center tw:gap-1.5">
                    <iconify
                      v-if="freeItemSelection?._key === item._key"
                      icon="ph:check-circle-fill"
                      class="tw:text-emerald-400 tw:shrink-0"
                    />
                    <iconify v-else icon="ph:circle" class="tw:shrink-0" />
                    {{ item.productName }}
                  </span>
                  <span class="tw:text-xs tw:font-semibold tw:text-emerald-400 tw:shrink-0">
                    {{ formatVnd(item.unitPrice) }}
                  </span>
                </button>
              </div>
            </div>

            <!-- Promo code input -->
            <div v-if="!promoInfo">
              <div class="tw:flex tw:gap-2">
                <prime-input-text
                  v-model="promoCode"
                  :placeholder="t('orders.create.promoCode')"
                  class="tw:flex-1"
                  @keyup.enter="applyPromoCode"
                />
                <prime-button
                  severity="secondary"
                  outlined
                  :loading="promoLoading"
                  :disabled="!promoCode.trim()"
                  @click="applyPromoCode"
                  >{{ t('orders.create.apply') }}</prime-button
                >
              </div>
              <p v-if="promoError" class="tw:text-xs tw:text-red-400 tw:mt-1.5">
                {{ promoError }}
              </p>
            </div>
          </div>

          <!-- Error -->
          <prime-message
            v-if="errorMessage || sessionError"
            severity="error"
            :closable="false"
            class="tw:mt-3"
          >
            {{ errorMessage || sessionError }}
          </prime-message>

          <!-- Place order -->
          <prime-button class="tw:w-full tw:mt-4" :loading="placing" :disabled="!canPlaceOrder" @click="placeOrder">
            <iconify icon="prime:check" />
            <span>{{ t('orders.create.submit') }}</span>
          </prime-button>

          <p v-if="!sessionId && !sessionLoading" class="tw:text-xs tw:text-muted tw:text-center tw:mt-2">
            {{ t('orders.create.tableHint') }}
          </p>
        </div>
      </aside>
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
