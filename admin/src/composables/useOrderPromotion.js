import { validatePromotion, getPromotions } from '@/services/promotion.service'

export function useOrderPromotion(cart, cartTotal, productCategoryMap, menuCategories) {
  const promoCode = ref('')
  const promoInfo = ref(null)
  const promoLoading = ref(false)
  const promoError = ref('')

  const findPromosVisible = ref(false)
  const publicPromos = ref([])
  const publicPromosLoading = ref(false)
  const lastAppliedPromo = ref(null)
  const freeItemSelection = ref(null)

  // ── Computed ────────────────────────────────────────────────────

  const freeItemPickerPool = computed(() => {
    if (!promoInfo.value) return []
    const info = promoInfo.value
    if (info.discountType !== 'BUY_X_GET_Y') return []

    const buyQty = info.buyQuantity ?? lastAppliedPromo.value?.buyQuantity
    const getQty = info.getQuantity ?? lastAppliedPromo.value?.getQuantity
    if (!buyQty || !getQty) return []

    const scope = info.scope
    const applicableProductIds =
      info.applicableProductIds ?? lastAppliedPromo.value?.applicableProductIds ?? []
    const applicableCategoryIds =
      info.applicableCategoryIds ?? lastAppliedPromo.value?.applicableCategoryIds ?? []
    const getFromProductIds =
      info.getFromProductIds ?? lastAppliedPromo.value?.getFromProductIds ?? []
    const getFromCategoryIds =
      info.getFromCategoryIds ?? lastAppliedPromo.value?.getFromCategoryIds ?? []

    const regularItems = cart.value.filter((i) => !i.isFreeGift)

    let scopedItems
    if (scope === 'ORDER') {
      scopedItems = [...regularItems]
    } else if (scope === 'PRODUCT') {
      const ids = new Set(applicableProductIds)
      scopedItems = regularItems.filter((i) => ids.size === 0 || ids.has(i.productId))
    } else if (scope === 'CATEGORY') {
      const catIds = new Set(applicableCategoryIds)
      scopedItems = regularItems.filter(
        (i) => catIds.size === 0 || catIds.has(productCategoryMap.value[i.productId]),
      )
    } else {
      scopedItems = [...regularItems]
    }

    const totalScopedQty = scopedItems.reduce((s, i) => s + i.quantity, 0)
    if (totalScopedQty < buyQty) return []

    if (getFromProductIds.length > 0) {
      const ids = new Set(getFromProductIds)
      return menuCategories.value
        .flatMap((cat) => cat.products ?? [])
        .filter((p) => ids.has(p.id) && p.isActive)
        .map((p) => ({ productId: p.id, productName: p.name, unitPrice: p.price, _key: `menu_gift_${p.id}` }))
    } else if (getFromCategoryIds.length > 0) {
      const catIds = new Set(getFromCategoryIds)
      return menuCategories.value
        .filter((cat) => catIds.has(cat.id) && cat.isActive)
        .flatMap((cat) => (cat.products ?? []).filter((p) => p.isActive))
        .map((p) => ({ productId: p.id, productName: p.name, unitPrice: p.price, _key: `menu_gift_${p.id}` }))
    } else {
      return [...scopedItems]
    }
  })

  const cartDiscount = computed(() =>
    freeItemSelection.value ? 0 : (promoInfo.value?.estimatedDiscount ?? 0),
  )

  const cartFinal = computed(() => cartTotal.value - cartDiscount.value)

  // ── Watchers ────────────────────────────────────────────────────

  watch(freeItemPickerPool, (pool) => {
    if (pool.length === 0) freeItemSelection.value = null
  })

  watch(freeItemSelection, (newItem, oldItem) => {
    if (oldItem) {
      const idx = cart.value.findIndex((i) => i._key === oldItem._key + '_free_gift')
      if (idx !== -1) cart.value.splice(idx, 1)
    }
    if (newItem) {
      const freeKey = newItem._key + '_free_gift'
      if (!cart.value.find((i) => i._key === freeKey)) {
        cart.value.push({
          ...newItem,
          _key: freeKey,
          unitPrice: 0,
          quantity: 1,
          isFreeGift: true,
        })
      }
    }
  })

  watch(cartTotal, async (newVal) => {
    if (!promoInfo.value) return
    try {
      const res = await validatePromotion(promoCode.value.trim(), newVal)
      if (res.data.isApplicable) {
        promoInfo.value = res.data
        if (freeItemSelection.value) {
          const idx = cart.value.findIndex(
            (i) => i._key === freeItemSelection.value._key + '_free_gift',
          )
          if (idx !== -1) cart.value.splice(idx, 1)
          freeItemSelection.value = null
        }
        if (promoInfo.value.estimatedDiscount == null && lastAppliedPromo.value) {
          const est = estimateClientDiscount(lastAppliedPromo.value)
          if (est != null) promoInfo.value = { ...promoInfo.value, estimatedDiscount: est }
        }
      } else {
        clearPromo()
      }
    } catch {
      clearPromo()
    }
  })

  // ── Helpers ────────────────────────────────────────────────────

  const formatVnd = (val) =>
    new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND',
      maximumFractionDigits: 0,
    }).format(val ?? 0)

  const formatPromotionValue = (promo) => {
    if (promo.discountType === 'PERCENTAGE') return `${promo.discountValue}% off`
    if (promo.discountType === 'FIXED') return `-${formatVnd(promo.discountValue)}`
    if (promo.discountType === 'BUY_X_GET_Y')
      return `Buy ${promo.buyQuantity} get ${promo.getQuantity}`
    return ''
  }

  const promoDisableReason = (promo) => {
    const now = new Date()
    if (!promo.isActive) return 'Inactive'
    if (new Date(promo.startDate) > now) return 'Not started yet'
    if (promo.endDate && new Date(promo.endDate) < now) return 'Expired'
    if (promo.maxUsage && promo.currentUsage >= promo.maxUsage) return 'No uses left'
    if (promo.minOrderAmount && cartTotal.value < promo.minOrderAmount)
      return 'Order total too low'

    const cartProductIds = new Set(cart.value.map((i) => i.productId))
    if (promo.scope === 'PRODUCT') {
      const applicable = promo.applicableProductIds ?? []
      if (applicable.length > 0 && !applicable.some((id) => cartProductIds.has(id)))
        return 'No matching products in cart'
    } else if (promo.scope === 'CATEGORY') {
      const applicable = promo.applicableCategoryIds ?? []
      if (applicable.length > 0) {
        const cartCatIds = new Set(
          cart.value.map((i) => productCategoryMap.value[i.productId]).filter(Boolean),
        )
        if (!applicable.some((id) => cartCatIds.has(id)))
          return 'No matching category in cart'
      }
    }
    return null
  }

  const isPromoAvailable = (promo) => promoDisableReason(promo) === null

  const estimateClientDiscount = (promo) => {
    const type = promo.discountType
    const scope = promo.scope
    let eligibleSubtotal = 0

    if (scope === 'ORDER') {
      eligibleSubtotal = cartTotal.value
    } else if (scope === 'PRODUCT') {
      const ids = new Set(promo.applicableProductIds ?? [])
      eligibleSubtotal = cart.value
        .filter((i) => ids.size === 0 || ids.has(i.productId))
        .reduce((s, i) => s + i.unitPrice * i.quantity, 0)
    } else if (scope === 'CATEGORY') {
      const catIds = new Set(promo.applicableCategoryIds ?? [])
      eligibleSubtotal = cart.value
        .filter((i) => catIds.size === 0 || catIds.has(productCategoryMap.value[i.productId]))
        .reduce((s, i) => s + i.unitPrice * i.quantity, 0)
    }

    if (eligibleSubtotal <= 0) return null

    if (type === 'PERCENTAGE') {
      const disc = Math.round((eligibleSubtotal * promo.discountValue) / 100)
      return promo.maxDiscountAmount ? Math.min(disc, promo.maxDiscountAmount) : disc
    }
    if (type === 'FIXED') return Math.min(promo.discountValue, eligibleSubtotal)

    if (type === 'BUY_X_GET_Y') {
      const buyQty = promo.buyQuantity
      const getQty = promo.getQuantity
      if (!buyQty || !getQty) return null
      const groupSize = buyQty + getQty

      let scopedItems
      if (scope === 'ORDER') {
        scopedItems = [...cart.value]
      } else if (scope === 'PRODUCT') {
        const ids = new Set(promo.applicableProductIds ?? [])
        scopedItems = cart.value.filter((i) => ids.size === 0 || ids.has(i.productId))
      } else if (scope === 'CATEGORY') {
        const catIds = new Set(promo.applicableCategoryIds ?? [])
        scopedItems = cart.value.filter(
          (i) => catIds.size === 0 || catIds.has(productCategoryMap.value[i.productId]),
        )
      } else {
        scopedItems = [...cart.value]
      }

      const totalScopedQty = scopedItems.reduce((s, i) => s + i.quantity, 0)
      if (totalScopedQty < buyQty) return null
      const groups = Math.floor(totalScopedQty / groupSize)
      if (groups === 0) return null

      let freeUnitsRemaining = groups * getQty
      let freePool
      if (promo.getFromProductIds?.length > 0) {
        const ids = new Set(promo.getFromProductIds)
        freePool = cart.value.filter((i) => ids.has(i.productId))
      } else if (promo.getFromCategoryIds?.length > 0) {
        const catIds = new Set(promo.getFromCategoryIds)
        freePool = cart.value.filter((i) => catIds.has(productCategoryMap.value[i.productId]))
      } else {
        freePool = [...scopedItems]
      }

      if (freePool.length === 0) return null
      const sortedPool = [...freePool].sort((a, b) => a.unitPrice - b.unitPrice)
      let discount = 0
      for (const item of sortedPool) {
        if (freeUnitsRemaining <= 0) break
        const freeFromItem = Math.min(item.quantity, freeUnitsRemaining)
        discount += freeFromItem * item.unitPrice
        freeUnitsRemaining -= freeFromItem
      }
      return discount > 0 ? discount : null
    }

    return null
  }

  const isItemDiscounted = (item) => {
    if (item.isFreeGift) return false
    if (!promoInfo.value) return false
    const scope = promoInfo.value.scope ?? lastAppliedPromo.value?.scope
    if (!scope) return false
    if (scope === 'ORDER') return true
    if (scope === 'PRODUCT') {
      const ids =
        promoInfo.value.applicableProductIds ??
        lastAppliedPromo.value?.applicableProductIds ??
        []
      return ids.length === 0 || ids.includes(item.productId)
    }
    if (scope === 'CATEGORY') {
      const catIds = new Set(
        promoInfo.value.applicableCategoryIds ??
          lastAppliedPromo.value?.applicableCategoryIds ??
          [],
      )
      return catIds.size === 0 || catIds.has(productCategoryMap.value[item.productId])
    }
    return false
  }

  // ── Actions ────────────────────────────────────────────────────

  const applyPromoCode = async () => {
    const code = promoCode.value.trim()
    if (!code) return
    promoError.value = ''
    promoInfo.value = null
    promoLoading.value = true
    try {
      const res = await validatePromotion(code, cartTotal.value)
      const data = res.data
      if (!data.isApplicable) {
        promoError.value = data.message || 'Promotion not applicable.'
      } else {
        promoInfo.value = data
      }
    } catch (err) {
      promoError.value = err?.response?.data?.message || 'Invalid promotion code.'
    } finally {
      promoLoading.value = false
    }
  }

  const clearPromo = () => {
    if (freeItemSelection.value) {
      const idx = cart.value.findIndex(
        (i) => i._key === freeItemSelection.value._key + '_free_gift',
      )
      if (idx !== -1) cart.value.splice(idx, 1)
    }
    cart.value = cart.value.filter((i) => !i.isFreeGift)
    promoCode.value = ''
    promoInfo.value = null
    promoError.value = ''
    lastAppliedPromo.value = null
    freeItemSelection.value = null
  }

  const openFindPromosDialog = async () => {
    findPromosVisible.value = true
    if (publicPromos.value.length > 0) return
    publicPromosLoading.value = true
    try {
      const res = await getPromotions({ pageSize: 200 })
      publicPromos.value = (res.data?.items ?? []).filter((p) => p.codeVisibility === 'PUBLIC')
    } catch {
      publicPromos.value = []
    } finally {
      publicPromosLoading.value = false
    }
  }

  const selectPromo = async (promo) => {
    findPromosVisible.value = false
    lastAppliedPromo.value = promo
    promoCode.value = promo.code
    await applyPromoCode()
    if (promoInfo.value && promoInfo.value.estimatedDiscount == null) {
      const est = estimateClientDiscount(promo)
      if (est != null) promoInfo.value = { ...promoInfo.value, estimatedDiscount: est }
    }
  }

  return {
    promoCode,
    promoInfo,
    promoLoading,
    promoError,
    findPromosVisible,
    publicPromos,
    publicPromosLoading,
    freeItemSelection,
    freeItemPickerPool,
    cartDiscount,
    cartFinal,
    formatVnd,
    formatPromotionValue,
    promoDisableReason,
    isPromoAvailable,
    isItemDiscounted,
    applyPromoCode,
    clearPromo,
    openFindPromosDialog,
    selectPromo,
  }
}
