export function useOrderCart(menuCategories) {
  const { t } = useI18n()

  const cart = ref([])
  const showOptionsDialog = ref(false)
  const selectedProduct = ref(null)

  const cartTotal = computed(() =>
    cart.value.reduce((sum, i) => sum + i.unitPrice * i.quantity, 0),
  )

  const cartItemCount = computed(() =>
    cart.value.reduce((sum, i) => sum + i.quantity, 0),
  )

  const defaultGuestCount = computed(() =>
    cart.value
      .filter((i) => !i.isFreeGift && !i.isAccompaniment)
      .reduce((acc, i) => acc + i.quantity, 0),
  )

  const productCategoryMap = computed(() => {
    const map = {}
    for (const cat of menuCategories.value) {
      for (const p of cat.products ?? []) {
        map[p.id] = cat.id
      }
    }
    return map
  })

  const formatVnd = (val) =>
    new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND',
      maximumFractionDigits: 0,
    }).format(val ?? 0)

  const optionsLabel = (item) => {
    const parts = []
    if (item.temperature) parts.push(t(`orders.temperature.${item.temperature}`))
    if (item.iceLevel && item.iceLevel !== ICE_LEVEL.NORMAL)
      parts.push(t(`orders.iceLevel.${item.iceLevel}`))
    if (item.sugarLevel && item.sugarLevel !== SUGAR_LEVEL.NORMAL)
      parts.push(t(`orders.sugarLevel.${item.sugarLevel}`))
    return parts.join(' · ')
  }

  const cartQuantity = (productId) =>
    cart.value
      .filter((i) => i.productId === productId)
      .reduce((sum, i) => sum + i.quantity, 0)

  const changeQty = (key, delta) => {
    const idx = cart.value.findIndex((i) => i._key === key)
    if (idx === -1) return
    cart.value[idx].quantity += delta
    if (cart.value[idx].quantity <= 0) cart.value.splice(idx, 1)
  }

  const clearCart = () => {
    cart.value = []
  }

  const addToCart = (item) => {
    const existing = cart.value.find((i) => i._key === item._key)
    if (existing) {
      existing.quantity += item.quantity
    } else {
      cart.value.push(item)
    }
  }

  const handleAddToCart = (product) => {
    selectedProduct.value = product
    showOptionsDialog.value = true
  }

  return {
    cart,
    showOptionsDialog,
    selectedProduct,
    cartTotal,
    cartItemCount,
    defaultGuestCount,
    productCategoryMap,
    formatVnd,
    optionsLabel,
    cartQuantity,
    changeQty,
    clearCart,
    addToCart,
    handleAddToCart,
  }
}
