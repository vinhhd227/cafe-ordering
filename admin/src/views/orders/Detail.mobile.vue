<script setup>
import {
  getOrderById,
  getOrders,
  updateOrderStatus,
  updatePayment,
  mergeOrders,
  splitOrder,
  updateOrderItem,
  updateOrderDate,
  deleteOrder,
} from '@/services/order.service'
import { ORDER_STATUS, ORDER_STATUS_MAP } from '@/constants/orderStatus'
import { PAYMENT_STATUS, PAYMENT_STATUS_MAP } from '@/constants/paymentStatus'
import { PAYMENT_METHOD, PAYMENT_METHOD_MAP } from '@/constants/paymentMethod'
import { getProducts } from '@/services/product.service'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const toast = useToast()
const confirm = useConfirm()
const { can } = usePermission()
const orderId = computed(() => Number(route.params.id))

// ── State ──────────────────────────────────────────────────────────
const order = ref(null)
const loading = ref(false)
const errorMessage = ref('')
const updatingId = ref(null)
const itemUpdating = ref(null)

// ── Helpers ────────────────────────────────────────────────────────
const formatVnd = (value) =>
  new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(value ?? 0)

const formatDate = (dateStr) =>
  new Intl.DateTimeFormat('vi-VN', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit',
  }).format(new Date(dateStr))

const statusTag = (status) => {
  const meta = ORDER_STATUS_MAP[status] ?? { severity: 'secondary' }
  return { ...meta, label: t(`orders.status.${status}`, status) }
}

const paymentTag = (status, method) => {
  if (status === PAYMENT_STATUS.PAID) {
    const m = t(`orders.paymentMethod.${method}`, '')
    return {
      label: m ? t('orders.pay.paidWith', { method: m }) : t('orders.paymentStatus.PAID'),
      severity: 'success',
    }
  }
  const meta = PAYMENT_STATUS_MAP[status] ?? { severity: 'warn' }
  return { ...meta, label: t(`orders.paymentStatus.${status}`, meta.label ?? status) }
}

const NEXT_STATUS = computed(() => {
  const isDelivery = order.value?.orderType === 'DELIVERY'
  return {
    [ORDER_STATUS.PENDING]:    ORDER_STATUS.PROCESSING,
    [ORDER_STATUS.PROCESSING]: isDelivery ? ORDER_STATUS.SHIPPING : ORDER_STATUS.COMPLETED,
    [ORDER_STATUS.SHIPPING]:   ORDER_STATUS.COMPLETED,
  }
})

const NEXT_LABEL = computed(() => ({
  [ORDER_STATUS.PENDING]:    t('orders.detail.actions.startPreparing'),
  [ORDER_STATUS.PROCESSING]: order.value?.orderType === 'DELIVERY' ? t('orders.kanban.markShipping') : t('orders.detail.actions.markComplete'),
  [ORDER_STATUS.SHIPPING]:   t('orders.detail.actions.markComplete'),
}))

const canSplit = computed(() => {
  if (!order.value || order.value.paymentStatus !== PAYMENT_STATUS.UNPAID) return false
  const items = order.value.items ?? []
  return items.length > 1 || (items.length === 1 && items[0].quantity > 1)
})

const canEditItems = computed(
  () =>
    order.value?.status === ORDER_STATUS.PENDING &&
    order.value?.paymentStatus === PAYMENT_STATUS.UNPAID,
)

// ── Load ───────────────────────────────────────────────────────────
const loadOrder = async () => {
  loading.value = true
  errorMessage.value = ''
  try {
    const res = await getOrderById(orderId.value)
    order.value = res?.data
  } catch (err) {
    errorMessage.value = err?.response?.data?.message || 'Failed to load order.'
  } finally {
    loading.value = false
  }
}
watch(orderId, loadOrder, { immediate: true })

// ── Status actions ─────────────────────────────────────────────────
const moveOrder = async (toStatus) => {
  updatingId.value = 'status'
  errorMessage.value = ''
  try {
    await updateOrderStatus(orderId.value, toStatus)
    order.value.status = toStatus
  } catch (err) {
    errorMessage.value = err?.response?.data?.errors?.join(', ') || 'Failed to update order.'
  } finally {
    updatingId.value = null
  }
}

const cancelOrder = async () => {
  updatingId.value = 'status'
  actionsDrawerVisible.value = false
  errorMessage.value = ''
  try {
    await updateOrderStatus(orderId.value, ORDER_STATUS.CANCELLED)
    order.value.status = ORDER_STATUS.CANCELLED
  } catch (err) {
    errorMessage.value = err?.response?.data?.errors?.join(', ') || 'Failed to cancel order.'
  } finally {
    updatingId.value = null
  }
}

// ── Payment ────────────────────────────────────────────────────────
const payMethod = ref(PAYMENT_METHOD.CASH)
const payAmountReceived = ref(null)
const payTip = ref(0)
const payLoading = ref(false)
const payDrawerVisible = ref(false)

const PAYMENT_METHODS = computed(() => [
  { label: t('orders.paymentMethod.CASH'), value: PAYMENT_METHOD.CASH, icon: 'ph:money-bold' },
  { label: t('orders.paymentMethod.BANK_TRANSFER'), value: PAYMENT_METHOD.BANK_TRANSFER, icon: 'ph:bank-bold' },
])

const payChange = computed(() => {
  if (payAmountReceived.value == null || !order.value) return null
  return payAmountReceived.value - order.value.finalAmount
})
const payReturn = computed(() => {
  if (payChange.value === null || payChange.value <= 0) return null
  return payChange.value - (payTip.value ?? 0)
})
watch(payChange, (val) => {
  if (val !== null && val >= 0 && payTip.value > val) payTip.value = val
})

const confirmPayment = async () => {
  payLoading.value = true
  errorMessage.value = ''
  try {
    await updatePayment(orderId.value, PAYMENT_STATUS.PAID, payMethod.value, payAmountReceived.value, payTip.value ?? 0)
    order.value.paymentStatus = PAYMENT_STATUS.PAID
    order.value.paymentMethod = payMethod.value
    order.value.amountReceived = payAmountReceived.value
    order.value.tipAmount = payTip.value ?? 0
    payDrawerVisible.value = false
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.errors?.join(', ') || err?.response?.data?.title || 'Failed to update payment.'
  } finally {
    payLoading.value = false
  }
}

// ── Edit order date ────────────────────────────────────────────────
const editDateDrawerVisible = ref(false)
const editDate = ref(null)
const editDateLoading = ref(false)

const openEditDateDrawer = () => {
  editDate.value = order.value ? new Date(order.value.orderDate) : new Date()
  actionsDrawerVisible.value = false
  editDateDrawerVisible.value = true
}

const handleUpdateOrderDate = async () => {
  if (!editDate.value) return
  editDateLoading.value = true
  try {
    await updateOrderDate(orderId.value, editDate.value.toISOString())
    order.value.orderDate = editDate.value.toISOString()
    editDateDrawerVisible.value = false
    toast.add({ severity: 'success', summary: t('orders.detail.info.orderDateUpdated'), life: 3000 })
  } catch (err) {
    toast.add({ severity: 'error', summary: err?.response?.data?.errors?.join(', ') || 'Failed to update date.', life: 3000 })
  } finally {
    editDateLoading.value = false
  }
}

// ── Copy ───────────────────────────────────────────────────────────
const copyOrderNumber = async (orderNumber) => {
  await navigator.clipboard.writeText(orderNumber)
  toast.add({ severity: 'success', summary: t('orders.list.copyOrderNumberSuccess'), life: 2000 })
}

// ── Delete ─────────────────────────────────────────────────────────
const handleDeleteOrder = () => {
  actionsDrawerVisible.value = false
  confirm.require({
    message: t('orders.detail.deleteOrderConfirmMsg', { orderNumber: order.value?.orderNumber }),
    header: t('orders.detail.deleteOrderConfirmHeader'),
    icon: 'ph:trash-bold',
    rejectProps: { severity: 'secondary', outlined: true, size: 'small', label: t('common.cancel') },
    acceptProps: { severity: 'danger', size: 'small', label: t('orders.detail.deleteOrder') },
    accept: async () => {
      try {
        await deleteOrder(orderId.value)
        toast.add({ severity: 'success', summary: t('orders.detail.deleteOrderSuccess', { orderNumber: order.value?.orderNumber }), life: 3000 })
        router.push({ name: 'ordersList' })
      } catch (err) {
        errorMessage.value = err?.response?.data?.errors?.join(', ') || err?.response?.data?.title || 'Failed to delete order.'
      }
    },
  })
}

// ── Item editing ───────────────────────────────────────────────────
const setItemQty = async (productId, newQty) => {
  itemUpdating.value = productId
  errorMessage.value = ''
  try {
    const res = await updateOrderItem(orderId.value, productId, newQty)
    order.value.items = res.data.items
    order.value.totalAmount = res.data.totalAmount
    order.value.totalDiscount = res.data.totalDiscount
    order.value.finalAmount = res.data.finalAmount
    order.value.promotions = res.data.promotions
    if (res.data.status === 'CANCELLED') {
      toast.add({ severity: 'info', summary: t('orders.status.CANCELLED'), detail: t('orders.detail.autoCancel'), life: 4000 })
      router.push({ name: 'orders' })
    }
  } catch (err) {
    errorMessage.value = err?.response?.data?.errors?.join(', ') || err?.response?.data?.title || 'Failed to update item.'
  } finally {
    itemUpdating.value = null
  }
}

const removeItem = (item) => {
  const regularItems = order.value?.items?.filter((i) => !i.isFreeGift) ?? []
  const isLastItem = regularItems.length === 1 && regularItems[0].productId === item.productId
  const hasFreeGift = order.value?.items?.some((i) => i.isFreeGift)
  const message = isLastItem
    ? t('orders.detail.removeItem.lastItem', { name: item.productName })
    : hasFreeGift
      ? t('orders.detail.removeItem.withFreeGift', { name: item.productName })
      : t('orders.detail.removeItem.regular', { name: item.productName })
  confirm.require({
    message,
    icon: isLastItem ? 'ph:trash-bold' : hasFreeGift ? 'ph:gift-bold' : 'ph:warning-bold',
    rejectProps: { label: t('orders.detail.removeItem.keep'), severity: 'secondary', outlined: true, size: 'small' },
    acceptProps: { label: isLastItem ? t('orders.detail.removeItem.cancelOrder') : t('orders.detail.removeItem.remove'), severity: 'danger', size: 'small' },
    accept: () => setItemQty(item.productId, 0),
  })
}

// ── Print dialogs ──────────────────────────────────────────────────
const showPrintDialog = ref(false)
const showBillDialog = ref(false)

// ── Add item drawer ────────────────────────────────────────────────
const addItemDrawerVisible = ref(false)
const addItemSearch = ref('')
const addItemProducts = ref([])
const addItemLoading = ref(false)
const addItemQty = ref(1)
const addItemSelected = ref(null)

const openAddItemDrawer = async () => {
  addItemSearch.value = ''
  addItemSelected.value = null
  addItemQty.value = 1
  actionsDrawerVisible.value = false
  addItemDrawerVisible.value = true
  addItemLoading.value = true
  try {
    const res = await getProducts({ isActive: true, pageSize: 200 })
    addItemProducts.value = res?.data?.items ?? res?.data ?? []
  } catch {
    addItemProducts.value = []
  } finally {
    addItemLoading.value = false
  }
}

const addItemFiltered = computed(() => {
  const q = addItemSearch.value.trim().toLowerCase()
  return q ? addItemProducts.value.filter((p) => p.name.toLowerCase().includes(q)) : addItemProducts.value
})

const confirmAddItem = async () => {
  if (!addItemSelected.value) return
  const existing = order.value.items.find((i) => i.productId === addItemSelected.value.id)
  const targetQty = (existing?.quantity ?? 0) + (addItemQty.value || 1)
  addItemDrawerVisible.value = false
  await setItemQty(addItemSelected.value.id, targetQty)
}

// ── Merge drawer ───────────────────────────────────────────────────
const mergeDrawerVisible = ref(false)
const mergeOrders_ = ref([])
const mergeLoading = ref(false)
const mergeFetching = ref(false)
const mergeSelected = ref([])

const openMergeDrawer = async () => {
  mergeSelected.value = []
  mergeFetching.value = true
  actionsDrawerVisible.value = false
  mergeDrawerVisible.value = true
  try {
    const res = await getOrders({ paymentStatus: PAYMENT_STATUS.UNPAID, pageSize: 100 })
    const all = res?.data?.items ?? []
    mergeOrders_.value = all.filter((o) => o.id !== orderId.value && o.status !== ORDER_STATUS.CANCELLED)
  } catch {
    mergeOrders_.value = []
  } finally {
    mergeFetching.value = false
  }
}

const confirmMerge = async () => {
  if (!mergeSelected.value.length) return
  mergeLoading.value = true
  errorMessage.value = ''
  try {
    await mergeOrders(orderId.value, mergeSelected.value)
    mergeDrawerVisible.value = false
    await loadOrder()
    toast.add({
      severity: 'success',
      summary: t('orders.detail.merge.title'),
      detail: t('orders.detail.mergeToastDetail', { n: mergeSelected.value.length, orderNumber: order.value?.orderNumber }),
      life: 4000,
    })
  } catch (err) {
    errorMessage.value = err?.response?.data?.errors?.join(', ') || err?.response?.data?.title || 'Failed to merge orders.'
  } finally {
    mergeLoading.value = false
  }
}

// ── Split drawer ───────────────────────────────────────────────────
const splitDrawerVisible = ref(false)
const splitItems = ref([])
const splitLoading = ref(false)

const openSplitDrawer = () => {
  splitItems.value = (order.value?.items ?? []).map((item) => ({
    productId: item.productId,
    productName: item.productName,
    unitPrice: item.unitPrice,
    quantity: item.quantity,
    splitQty: 0,
  }))
  actionsDrawerVisible.value = false
  splitDrawerVisible.value = true
}

const splitPreview = computed(() => {
  const toNew = splitItems.value.filter((i) => i.splitQty > 0)
  const toNewQty = toNew.reduce((s, i) => s + i.splitQty, 0)
  const keepQty = splitItems.value.reduce((s, i) => s + (i.quantity - i.splitQty), 0)
  return { toNew, toNewQty, keepQty }
})

const splitValid = computed(() => {
  const { toNewQty, keepQty } = splitPreview.value
  return toNewQty > 0 && keepQty > 0
})

const confirmSplit = async () => {
  const items = splitItems.value.filter((i) => i.splitQty > 0).map((i) => ({ productId: i.productId, quantity: i.splitQty }))
  splitLoading.value = true
  errorMessage.value = ''
  try {
    const res = await splitOrder(orderId.value, items)
    const result = res?.data
    splitDrawerVisible.value = false
    await loadOrder()
    toast.add({
      severity: 'success',
      summary: t('orders.detail.split.title'),
      detail: t('orders.detail.splitToastDetail', { orderNumber: result?.newOrderNumber }),
      life: 6000,
      group: 'split-result',
      data: { newOrderId: result?.newOrderId },
    })
  } catch (err) {
    errorMessage.value = err?.response?.data?.errors?.join(', ') || err?.response?.data?.title || 'Failed to split order.'
  } finally {
    splitLoading.value = false
  }
}

// ── Actions drawer ─────────────────────────────────────────────────
const actionsDrawerVisible = ref(false)
</script>

<template>
  <prime-confirm-dialog />

  <PrintDrinkLabelsDialog
    v-model:visible="showPrintDialog"
    :order-id="orderId"
    :order-number="order?.orderNumber ?? ''"
    :items="order?.items ?? []"
  />
  <PrintBillDialog
    v-model:visible="showBillDialog"
    :order-id="orderId"
    :order-number="order?.orderNumber ?? ''"
  />

  <!-- Split result toast -->
  <prime-toast group="split-result" position="bottom-center">
    <template #message="{ message }">
      <div class="tw:flex tw:items-center tw:gap-3 tw:w-full">
        <iconify icon="ph:scissors-bold" class="tw:text-lg tw:shrink-0 tw:text-primary-400" />
        <div class="tw:flex-1 tw:min-w-0">
          <p class="tw:font-medium tw:text-sm">{{ message.summary }}</p>
          <p class="tw:text-xs tw:text-muted tw:mt-0.5">{{ message.detail }}</p>
        </div>
        <prime-button
          v-if="message.data?.newOrderId"
          severity="secondary" outlined size="small"
          @click="router.push({ name: 'ordersDetail', params: { id: message.data.newOrderId } })"
        >
          {{ t('orders.detail.split.viewNew') }}
        </prime-button>
      </div>
    </template>
  </prime-toast>

  <!-- Full-height layout -->
  <div class="tw:flex tw:flex-col tw:h-dvh ">

    <!-- ── Top bar ──────────────────────────────────────────────── -->
    <div :class="[bgGlass, borderGlass, 'tw:flex tw:items-center tw:gap-2 tw:px-3 tw:py-2 tw:border-b tw:shrink-0 tw:sticky tw:top-0 tw:z-10']">
      <prime-button :class="btnIcon" severity="secondary" text @click="router.push({ name: 'ordersList' })">
        <iconify icon="ph:arrow-left-bold" />
      </prime-button>

      <div class="tw:flex-1 tw:min-w-0">
        <template v-if="order">
          <div class="tw:flex tw:items-center tw:gap-1.5 tw:flex-wrap">
            <span class="tw:font-mono tw:font-semibold tw:text-xl tw:truncate">{{ order.orderNumber }}</span>
            <prime-tag :value="statusTag(order.status).label" :severity="statusTag(order.status).severity" class="tw:text-[10px]! tw:px-1.5!" />
            <prime-tag :value="paymentTag(order.paymentStatus, order.paymentMethod).label" :severity="paymentTag(order.paymentStatus, order.paymentMethod).severity" class="tw:text-[10px]! tw:px-1.5!" />
            <prime-tag v-if="order.isManual" value="Thủ công" severity="secondary" class="tw:text-[10px]! tw:px-1.5!" />
          </div>
          <p class="tw:text-[11px] tw:text-muted tw:mt-0.5">{{ formatDate(order.orderDate) }}</p>
        </template>
        <prime-skeleton v-else width="10rem" height="1.25rem" />
      </div>

      <!-- ⋮ menu button -->
      <prime-button v-if="order" :class="btnIcon" severity="secondary" text @click="actionsDrawerVisible = true">
        <iconify icon="ph:dots-three-vertical-bold" />
      </prime-button>
    </div>

    <!-- ── Scrollable content ───────────────────────────────────── -->
    <div class="tw:flex-1 tw:overflow-y-auto tw:p-4 tw:space-y-4">

      <!-- Error -->
      <prime-message v-if="errorMessage" severity="error" size="small" variant="simple" closable @close="errorMessage = ''">
        {{ errorMessage }}
      </prime-message>

      <!-- Loading skeleton -->
      <div v-if="loading" class="tw:space-y-3">
        <prime-skeleton v-for="i in 5" :key="i" height="2.5rem" class="tw:rounded-xl" />
      </div>

      <template v-else-if="order">

        <!-- ── Info section ───────────────────────────────────── -->
        <div :class="[appCard, 'tw:rounded-2xl tw:p-4 tw:space-y-3']">
          <!-- Order type badge -->
          <div class="tw:flex tw:items-center tw:justify-between">
            <span class="tw:text-xs tw:text-muted tw:font-medium tw:uppercase tw:tracking-widest">
              {{ t('orders.detail.info.orderType') }}
            </span>
            <prime-tag
              :severity="order.orderType === 'DINE_IN' ? 'secondary' : order.orderType === 'TAKEAWAY' ? 'warn' : 'info'"
              class="tw:text-xs!"
            >
              <iconify
                :icon="order.orderType === 'DINE_IN' ? 'ph:fork-knife-bold' : order.orderType === 'TAKEAWAY' ? 'ph:bag-bold' : 'ph:motorcycle-bold'"
                class="tw:mr-1"
              />
              {{ t(`orders.create.orderType.${order.orderType ?? 'DINE_IN'}`) }}
            </prime-tag>
          </div>

          <!-- Table (DineIn) -->
          <div v-if="order.orderType === 'DINE_IN' || order.tableCode" class="tw:flex tw:items-center tw:justify-between tw:text-sm">
            <span class="tw:text-muted">{{ t('orders.detail.info.table') }}</span>
            <span v-if="order.tableCode" class="tw:font-semibold tw:font-mono">{{ order.tableCode }}</span>
            <span v-else class="tw:text-muted tw:italic tw:text-xs">—</span>
          </div>

          <!-- Customer info -->
          <div v-if="order.customerName || order.customerPhone" class="tw:flex tw:items-center tw:justify-between tw:text-sm">
            <span class="tw:text-muted tw:flex tw:items-center tw:gap-1">
              <iconify icon="ph:user-bold" class="tw:text-sm" />
              {{ t('orders.detail.info.customer') }}
            </span>
            <span class="tw:font-medium tw:text-right">
              <span v-if="order.customerName">{{ order.customerName }}</span>
              <span v-if="order.customerName && order.customerPhone"> · </span>
              <span v-if="order.customerPhone" class="tw:font-mono">{{ order.customerPhone }}</span>
            </span>
          </div>

          <!-- Delivery address -->
          <div v-if="order.deliveryAddress" class="tw:flex tw:items-start tw:justify-between tw:text-sm tw:gap-4">
            <span class="tw:text-muted tw:flex tw:items-center tw:gap-1 tw:shrink-0">
              <iconify icon="ph:map-pin-bold" class="tw:text-sm" />
              {{ t('orders.detail.info.deliveryAddress') }}
            </span>
            <span class="tw:font-medium tw:text-right tw:break-words">{{ order.deliveryAddress }}</span>
          </div>

          <!-- Delivery note -->
          <div v-if="order.deliveryNote" class="tw:flex tw:items-start tw:justify-between tw:text-sm tw:gap-4">
            <span class="tw:text-muted tw:shrink-0">{{ t('orders.detail.info.deliveryNote') }}</span>
            <span class="tw:text-right tw:text-muted tw:italic">{{ order.deliveryNote }}</span>
          </div>

          <!-- Guest count -->
          <div v-if="order.guestCount" class="tw:flex tw:items-center tw:justify-between tw:text-sm">
            <span class="tw:text-muted tw:flex tw:items-center tw:gap-1">
              <iconify icon="ph:users" class="tw:text-sm" />
              {{ t('orders.detail.info.guestCount') }}
            </span>
            <span class="tw:font-semibold">{{ order.guestCount }}</span>
          </div>

          <!-- Completed at -->
          <div v-if="order.completedAt" class="tw:flex tw:items-center tw:justify-between tw:text-sm">
            <span class="tw:text-muted tw:flex tw:items-center tw:gap-1">
              <iconify icon="ph:check-circle" class="tw:text-sm tw:text-green-500" />
              {{ t('orders.detail.info.completedAt') }}
            </span>
            <span class="tw:text-xs tw:font-mono">{{ formatDate(order.completedAt) }}</span>
          </div>

          <!-- Paid at -->
          <div v-if="order.paidAt" class="tw:flex tw:items-center tw:justify-between tw:text-sm">
            <span class="tw:text-muted tw:flex tw:items-center tw:gap-1">
              <iconify icon="ph:currency-circle-dollar" class="tw:text-sm tw:text-blue-400" />
              {{ t('orders.detail.info.paidAt') }}
            </span>
            <span class="tw:text-xs tw:font-mono">{{ formatDate(order.paidAt) }}</span>
          </div>

          <prime-divider class="tw:my-1!" />

          <!-- Discount -->
          <div v-if="order.totalDiscount > 0" class="tw:flex tw:justify-between tw:text-xs">
            <span class="tw:flex tw:items-center tw:gap-1 tw:text-muted">
              <iconify icon="ph:tag-bold" class="tw:text-primary-400" />
              {{ t('orders.detail.info.discount') }}
            </span>
            <span class="tw:text-primary-400">-{{ formatVnd(order.totalDiscount) }}</span>
          </div>

          <!-- Total -->
          <div class="tw:flex tw:justify-between tw:items-center tw:text-sm">
            <span class="tw:font-semibold">{{ t('orders.detail.info.total') }}</span>
            <div class="tw:text-right">
              <span v-if="order.totalDiscount > 0" class="tw:text-muted tw:line-through tw:text-xs tw:mr-1">
                {{ formatVnd(order.totalAmount) }}
              </span>
              <span class="tw:font-bold tw:text-base" :class="order.totalDiscount > 0 ? 'tw:text-primary-400' : ''">
                {{ formatVnd(order.finalAmount) }}
              </span>
            </div>
          </div>
        </div>

        <!-- ── Items section ──────────────────────────────────── -->
        <div :class="[appCard, 'tw:rounded-2xl tw:p-4']">
          <div class="tw:flex tw:items-center tw:justify-between tw:mb-3">
            <p class="tw:text-sm tw:font-semibold">
              {{ t('orders.detail.items') }}
              <span class="tw:text-muted tw:font-normal">({{ order.items?.length }})</span>
            </p>
            <prime-button
              v-if="canEditItems"
              severity="secondary" outlined size="small"
              @click="openAddItemDrawer"
            >
              <iconify icon="ph:plus-bold" />
              <span>{{ t('orders.detail.addItem.title') }}</span>
            </prime-button>
          </div>

          <div class="tw:space-y-3">
            <div
              v-for="(item, index) in order.items"
              :key="item.productId"
              :class="itemUpdating === item.productId ? 'tw:opacity-50' : ''"
            >
              <div class="tw:flex tw:items-start tw:gap-3">
                <!-- Qty controls or static badge -->
                <template v-if="canEditItems && !item.isFreeGift">
                  <div class="tw:flex tw:items-center tw:gap-1 tw:shrink-0 tw:mt-0.5">
                    <prime-button
                      severity="secondary" size="small" text
                      class="tw:w-7 tw:h-7 tw:p-0!"
                      :disabled="!!itemUpdating"
                      @click="item.quantity > 1 ? setItemQty(item.productId, item.quantity - 1) : removeItem(item)"
                    >
                      <iconify icon="ph:minus-bold" class="tw:text-xs" />
                    </prime-button>
                    <span class="tw:w-6 tw:text-center tw:font-semibold tw:tabular-nums tw:text-sm">{{ item.quantity }}</span>
                    <prime-button
                      severity="secondary" size="small" text
                      class="tw:w-7 tw:h-7 tw:p-0!"
                      :disabled="!!itemUpdating"
                      @click="setItemQty(item.productId, item.quantity + 1)"
                    >
                      <iconify icon="ph:plus-bold" class="tw:text-xs" />
                    </prime-button>
                  </div>
                </template>
                <span
                  v-else
                  class="tw:w-6 tw:h-6 tw:rounded-full tw:flex tw:items-center tw:justify-center tw:text-xs tw:font-semibold tw:shrink-0 tw:mt-0.5"
                  :class="item.isFreeGift ? 'tw:bg-amber-500/20 tw:text-amber-400' : 'tw:bg-white/10'"
                >{{ item.quantity }}</span>

                <div class="tw:flex-1 tw:min-w-0">
                  <div class="tw:flex tw:items-start tw:justify-between tw:gap-2">
                    <span class="tw:font-medium tw:text-sm tw:leading-snug">{{ item.productName }}</span>
                    <div class="tw:text-right tw:shrink-0">
                      <span class="tw:font-semibold tw:text-sm">{{ formatVnd(item.totalPrice) }}</span>
                      <p class="tw:text-[11px] tw:text-muted">{{ formatVnd(item.unitPrice) }}/cái</p>
                    </div>
                  </div>
                  <!-- Item tags -->
                  <div
                    v-if="item.temperature || item.iceLevel || item.sugarLevel || item.isTakeaway || item.isFreeGift"
                    class="tw:flex tw:flex-wrap tw:gap-1 tw:mt-1.5"
                  >
                    <span v-if="item.temperature"
                      class="tw:inline-flex tw:items-center tw:gap-0.5 tw:rounded tw:px-1.5 tw:py-0.5 tw:text-[10px] tw:font-medium"
                      :class="item.temperature === 'HOT' ? 'tw:bg-orange-500/15 tw:text-orange-300' : 'tw:bg-sky-500/15 tw:text-sky-300'"
                    >
                      <iconify :icon="item.temperature === 'HOT' ? 'ph:flame-bold' : 'ph:snowflake-bold'" class="tw:text-[9px]" />
                      {{ t(`orders.temperature.${item.temperature}`, item.temperature) }}
                    </span>
                    <span v-if="item.iceLevel && item.iceLevel !== 'NORMAL'"
                      class="tw:inline-flex tw:items-center tw:gap-0.5 tw:rounded tw:px-1.5 tw:py-0.5 tw:text-[10px] tw:font-medium tw:bg-sky-500/10 tw:text-sky-400"
                    >
                      <iconify icon="game-icons:ice-cube" class="tw:text-[9px]" />
                      {{ t(`orders.iceLevel.${item.iceLevel}`, item.iceLevel) }}
                    </span>
                    <span v-if="item.sugarLevel && item.sugarLevel !== 'NORMAL'"
                      class="tw:inline-flex tw:items-center tw:gap-0.5 tw:rounded tw:px-1.5 tw:py-0.5 tw:text-[10px] tw:font-medium tw:bg-amber-500/10 tw:text-amber-400"
                    >
                      <iconify icon="ph:cube-bold" class="tw:text-[9px]" />
                      {{ t(`orders.sugarLevel.${item.sugarLevel}`, item.sugarLevel) }}
                    </span>
                    <span v-if="item.isTakeaway"
                      class="tw:inline-flex tw:items-center tw:gap-0.5 tw:rounded tw:px-1.5 tw:py-0.5 tw:text-[10px] tw:font-medium tw:bg-purple-500/10 tw:text-purple-400"
                    >
                      <iconify icon="ph:bag-bold" class="tw:text-[9px]" />
                      {{ t('orders.serving.takeaway') }}
                    </span>
                    <span v-if="item.isFreeGift"
                      class="tw:inline-flex tw:items-center tw:gap-0.5 tw:rounded tw:px-1.5 tw:py-0.5 tw:text-[10px] tw:font-medium tw:bg-amber-500/15 tw:text-amber-400"
                    >
                      <iconify icon="ph:gift-bold" class="tw:text-[9px]" />
                      {{ t('orders.create.freeBadge') }}
                    </span>
                  </div>
                </div>

                <!-- Remove button -->
                <prime-button
                  v-if="canEditItems && !item.isFreeGift"
                  severity="danger" size="small" text
                  class="tw:shrink-0 tw:w-7 tw:h-7 tw:p-0! tw:mt-0.5"
                  :disabled="!!itemUpdating"
                  @click="removeItem(item)"
                >
                  <iconify icon="ph:trash-bold" class="tw:text-xs" />
                </prime-button>
              </div>
              <prime-divider v-if="index < order.items.length - 1" type="dashed" class="tw:my-2!" />
            </div>
          </div>

          <!-- Promotions -->
          <template v-if="order.totalDiscount > 0">
            <prime-divider class="tw:my-2!" />
            <div class="tw:space-y-1.5">
              <div v-for="promo in order.promotions" :key="promo.promotionId" class="tw:flex tw:justify-between tw:items-center tw:text-xs">
                <span class="tw:flex tw:items-center tw:gap-1 tw:text-muted">
                  <iconify icon="ph:tag-bold" class="tw:text-primary-400" />
                  {{ promo.promoCode }}
                </span>
                <span class="tw:text-primary-400 tw:font-medium">-{{ formatVnd(promo.discountAmount) }}</span>
              </div>
            </div>
          </template>
        </div>

        <!-- ── Payment status card (when paid) ────────────────── -->
        <div v-if="order.paymentStatus === PAYMENT_STATUS.PAID" :class="[appCard, 'tw:rounded-2xl tw:p-4 tw:space-y-3']">
          <div class="tw:flex tw:items-center tw:justify-between tw:mb-1">
            <p class="tw:text-sm tw:font-semibold">{{ t('orders.pay.title') }}</p>
            <prime-tag
              :value="paymentTag(order.paymentStatus, order.paymentMethod).label"
              :severity="paymentTag(order.paymentStatus, order.paymentMethod).severity"
            />
          </div>
          <div class="tw:flex tw:justify-between tw:text-sm">
            <span class="tw:text-muted">{{ t('orders.pay.paymentMethod') }}</span>
            <span class="tw:font-medium">{{ t(`orders.paymentMethod.${order.paymentMethod}`, PAYMENT_METHOD_MAP[order.paymentMethod]?.label ?? order.paymentMethod) }}</span>
          </div>
          <div v-if="order.amountReceived != null" class="tw:flex tw:justify-between tw:text-sm">
            <span class="tw:text-muted">{{ t('orders.pay.amountReceived') }}</span>
            <span class="tw:font-medium">{{ formatVnd(order.amountReceived) }}</span>
          </div>
          <div v-if="order.tipAmount" class="tw:flex tw:justify-between tw:text-sm">
            <span class="tw:text-muted">{{ t('orders.pay.tip') }}</span>
            <span class="tw:font-medium tw:text-primary-400">{{ formatVnd(order.tipAmount) }}</span>
          </div>
        </div>

        <!-- Not found would not reach here since we check order != null above -->
      </template>

      <!-- Not found -->
      <div v-else-if="!loading" :class="[appCard, 'tw:rounded-2xl tw:p-10 tw:flex tw:flex-col tw:items-center tw:text-muted']">
        <iconify icon="ph:receipt-x-bold" class="tw:text-3xl tw:mb-2" />
        <p class="tw:text-sm">{{ t('orders.detail.orderNotFound') }}</p>
      </div>
    </div>

    <!-- ── Sticky bottom bar ────────────────────────────────────── -->
    <div
      v-if="order && order.status !== ORDER_STATUS.CANCELLED && (NEXT_STATUS[order.status] || order.paymentStatus === PAYMENT_STATUS.UNPAID)"
      class="tw:px-4 tw:py-3 tw:border-t tw:border-(--app-border) tw:shrink-0 tw:flex tw:gap-2"
    >
      <!-- Advance status button -->
      <prime-button
        v-if="NEXT_STATUS[order.status]"
        severity="success"
        :loading="updatingId === 'status'"
        fluid
        @click="moveOrder(NEXT_STATUS[order.status])"
      >
        <iconify icon="ph:arrow-right-bold" />
        <span>{{ NEXT_LABEL[order.status] }}</span>
      </prime-button>

      <!-- Pay button -->
      <prime-button
        v-if="order.paymentStatus === PAYMENT_STATUS.UNPAID && order.status !== ORDER_STATUS.CANCELLED"
        severity="warn"
        :outlined="!!NEXT_STATUS[order.status]"
        fluid
        @click="payDrawerVisible = true"
      >
        <iconify icon="ph:currency-circle-dollar-bold" />
        <span>{{ t('orders.list.markPaid') }}</span>
      </prime-button>
    </div>
  </div>

  <!-- ── Actions drawer ────────────────────────────────────────────── -->
  <prime-drawer
    v-model:visible="actionsDrawerVisible"
    position="bottom"
    :style="{ height: 'auto' }"
    :pt="{ root: { class: 'tw:rounded-t-2xl' } }"
  >
    <template #header>
      <span class="tw:font-medium tw:text-sm">{{ order?.orderNumber }}</span>
    </template>
    <div class="tw:flex tw:flex-col tw:gap-2 tw:pb-4">
      <!-- Edit order (Pending + Unpaid) -->
      <prime-button
        v-if="canEditItems"
        severity="secondary" outlined fluid
        @click="router.push({ name: 'ordersEdit', params: { id: orderId } }); actionsDrawerVisible = false"
      >
        <iconify icon="ph:pencil-simple-bold" />
        <span>{{ t('orders.detail.editOrder') }}</span>
      </prime-button>

      <!-- Add item -->
      <prime-button v-if="canEditItems" severity="secondary" outlined fluid @click="openAddItemDrawer">
        <iconify icon="ph:plus-bold" />
        <span>{{ t('orders.detail.addItem.title') }}</span>
      </prime-button>

      <!-- Split -->
      <prime-button v-if="canSplit" severity="secondary" outlined fluid @click="openSplitDrawer">
        <iconify icon="ph:scissors-bold" />
        <span>{{ t('orders.detail.split.title') }}</span>
      </prime-button>

      <!-- Merge -->
      <prime-button
        v-if="order?.paymentStatus === PAYMENT_STATUS.UNPAID"
        severity="secondary" outlined fluid
        @click="openMergeDrawer"
      >
        <iconify icon="ph:git-merge-bold" />
        <span>{{ t('orders.detail.mergeWith') }}</span>
      </prime-button>

      <!-- Edit date -->
      <prime-button severity="secondary" outlined fluid @click="openEditDateDrawer">
        <iconify icon="ph:calendar-bold" />
        <span>{{ t('orders.detail.info.editOrderDate') }}</span>
      </prime-button>

      <!-- Print labels -->
      <prime-button v-if="can('order.print')" severity="secondary" outlined fluid @click="showPrintDialog = true; actionsDrawerVisible = false">
        <iconify icon="ph:printer-bold" />
        <span>{{ t('printers.print.title') }}</span>
      </prime-button>

      <!-- Print bill -->
      <prime-button v-if="can('order.print')" severity="secondary" outlined fluid @click="showBillDialog = true; actionsDrawerVisible = false">
        <iconify icon="ph:receipt-bold" />
        <span>{{ t('printers.printBill.title') }}</span>
      </prime-button>

      <!-- Manual edit -->
      <prime-button
        v-if="order?.isManual"
        severity="secondary" outlined fluid
        @click="router.push({ name: 'ordersEditManual', params: { id: orderId } }); actionsDrawerVisible = false"
      >
        <iconify icon="ph:pencil-line-bold" />
        <span>Sửa thủ công</span>
      </prime-button>

      <!-- Copy order number -->
      <prime-button severity="secondary" outlined fluid @click="copyOrderNumber(order?.orderNumber); actionsDrawerVisible = false">
        <iconify icon="ph:copy-bold" />
        <span>{{ t('orders.list.copyOrderNumber') }}</span>
      </prime-button>

      <prime-divider class="tw:my-1!" />

      <!-- Cancel order -->
      <prime-button
        v-if="order?.status !== ORDER_STATUS.COMPLETED && order?.status !== ORDER_STATUS.CANCELLED"
        severity="danger" outlined fluid
        :loading="updatingId === 'status'"
        @click="confirm.require({
          message: t('orders.kanban.cancelTitle', { orderNumber: order?.orderNumber }),
          icon: 'ph:warning-bold',
          rejectProps: { label: t('orders.kanban.keepOrder'), severity: 'secondary', outlined: true, size: 'small' },
          acceptProps: { label: t('orders.kanban.confirmCancel'), severity: 'danger', size: 'small' },
          accept: cancelOrder,
        }); actionsDrawerVisible = false"
      >
        <iconify icon="ph:x-circle-bold" />
        <span>{{ t('orders.detail.actions.cancel') }}</span>
      </prime-button>

      <!-- Delete (cancelled only) -->
      <prime-button
        v-if="order?.status === ORDER_STATUS.CANCELLED && can('order.delete')"
        severity="danger" outlined fluid
        @click="handleDeleteOrder"
      >
        <iconify icon="ph:trash-bold" />
        <span>{{ t('orders.detail.deleteOrder') }}</span>
      </prime-button>
    </div>
  </prime-drawer>

  <!-- ── Payment drawer ────────────────────────────────────────────── -->
  <prime-drawer
    v-model:visible="payDrawerVisible"
    position="bottom"
    :style="{ height: 'auto', maxHeight: '90dvh' }"
    :pt="{ root: { class: 'tw:rounded-t-2xl' } }"
  >
    <template #header>
      <div>
        <p class="tw:font-semibold tw:text-sm">{{ t('orders.pay.title') }}</p>
        <p class="tw:text-xs tw:text-muted tw:mt-0.5">{{ order?.orderNumber }} · {{ formatVnd(order?.finalAmount) }}</p>
      </div>
    </template>
    <div class="tw:pb-4 tw:space-y-4">
      <!-- Amount received -->
      <div class="tw:space-y-1.5">
        <label for="mob-amount" class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
          {{ t('orders.pay.amountReceived') }}
        </label>
        <prime-input-number
          id="mob-amount"
          v-model="payAmountReceived"
          :min="0"
          :use-grouping="true"
          :placeholder="String(order?.finalAmount ?? 0)"
          class="app-input tw:w-full"
          suffix=" ₫"
          @input="(e) => (payAmountReceived = e.value)"
        />
        <!-- Short -->
        <div v-if="payChange !== null && payChange < 0" class="tw:flex tw:items-center tw:justify-between tw:text-sm">
          <span class="tw:text-muted">{{ t('orders.pay.short') }}</span>
          <span class="tw:text-red-400 tw:font-semibold">{{ formatVnd(Math.abs(payChange)) }}</span>
        </div>
        <!-- Change + tip -->
        <template v-if="payChange !== null && payChange > 0">
          <div class="tw:flex tw:items-center tw:justify-between tw:text-sm">
            <span class="tw:text-muted">{{ t('orders.pay.change') }}</span>
            <span class="tw:font-semibold">{{ formatVnd(payChange) }}</span>
          </div>
          <div class="tw:space-y-1">
            <label class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">{{ t('orders.pay.tip') }}</label>
            <div class="tw:flex tw:gap-2">
              <prime-input-number
                v-model="payTip"
                :min="0"
                :max="payChange"
                :use-grouping="true"
                class="app-input tw:flex-1"
                suffix=" ₫"
                @input="(e) => (payTip = e.value ?? 0)"
              />
              <prime-button severity="secondary" outlined v-tooltip.top="t('orders.pay.keepAllAsTip')" @click="payTip = payChange">
                <iconify icon="ph:heart-bold" />
              </prime-button>
            </div>
          </div>
          <div class="tw:flex tw:items-center tw:justify-between tw:text-sm">
            <span class="tw:text-muted">{{ t('orders.pay.returnToCustomer') }}</span>
            <span :class="payReturn === 0 ? 'tw:text-muted' : 'tw:text-primary-400 tw:font-semibold'">
              {{ payReturn === 0 ? '—' : formatVnd(payReturn) }}
            </span>
          </div>
        </template>
      </div>

      <!-- Payment method -->
      <div class="tw:space-y-1.5">
        <label class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">{{ t('orders.pay.paymentMethod') }}</label>
        <prime-select-button
          v-model="payMethod"
          :options="PAYMENT_METHODS"
          option-label="label"
          option-value="value"
          :pt="{
            root: { class: 'tw:flex tw:w-full' },
            button: { class: 'tw:flex-1 tw:justify-center' },
          }"
        >
          <template #option="{ option }">
            <iconify :icon="option.icon" class="tw:mr-1.5" />
            <span>{{ option.label }}</span>
          </template>
        </prime-select-button>
      </div>

      <prime-button severity="success" fluid :loading="payLoading" @click="confirmPayment">
        <iconify icon="ph:check-bold" />
        <span>{{ t('orders.pay.confirmPayment') }}</span>
      </prime-button>
    </div>
  </prime-drawer>

  <!-- ── Edit date drawer ───────────────────────────────────────────── -->
  <prime-drawer
    v-model:visible="editDateDrawerVisible"
    position="bottom"
    :style="{ height: 'auto' }"
    :pt="{ root: { class: 'tw:rounded-t-2xl' } }"
  >
    <template #header>
      <p class="tw:font-semibold tw:text-sm">{{ t('orders.detail.info.editOrderDate') }}</p>
    </template>
    <div class="tw:pb-4 tw:space-y-4">
      <prime-date-picker v-model="editDate" show-time hour-format="24" inline date-format="dd/mm/yy" class="tw:w-full" />
      <prime-button severity="success" fluid :loading="editDateLoading" @click="handleUpdateOrderDate">
        <iconify icon="ph:floppy-disk-bold" />
        <span>{{ t('common.save') }}</span>
      </prime-button>
    </div>
  </prime-drawer>

  <!-- ── Add item drawer ────────────────────────────────────────────── -->
  <prime-drawer
    v-model:visible="addItemDrawerVisible"
    position="bottom"
    :style="{ height: '70dvh' }"
    :pt="{ root: { class: 'tw:rounded-t-2xl' } }"
  >
    <template #header>
      <p class="tw:font-semibold tw:text-sm">{{ t('orders.detail.addItem.title') }}</p>
    </template>
    <div class="tw:flex tw:flex-col tw:h-full tw:pb-4 tw:gap-3">
      <prime-input-text
        v-model="addItemSearch"
        :placeholder="t('orders.detail.addItem.searchPlaceholder')"
        class="app-input tw:w-full"
      />
      <div class="tw:flex-1 tw:overflow-y-auto tw:space-y-1">
        <div v-if="addItemLoading" class="tw:flex tw:justify-center tw:py-8">
          <iconify icon="prime:spinner" class="tw:animate-spin tw:text-2xl tw:text-muted" />
        </div>
        <p v-else-if="!addItemFiltered.length" class="tw:text-sm tw:text-muted tw:text-center tw:py-6">
          {{ t('orders.detail.addItem.noProducts') }}
        </p>
        <button
          v-for="product in addItemFiltered"
          :key="product.id"
          class="tw:w-full tw:flex tw:items-center tw:justify-between tw:gap-3 tw:rounded-xl tw:px-3 tw:py-2.5 tw:text-left tw:transition-colors tw:cursor-pointer"
          :class="addItemSelected?.id === product.id
            ? 'tw:bg-primary-500/15 tw:text-primary-300'
            : 'tw:hover:bg-white/5'"
          :style="addItemSelected?.id === product.id ? '' : 'background: var(--app-bg-subtle)'"
          @click="addItemSelected = product"
        >
          <span class="tw:font-medium tw:text-sm">{{ product.name }}</span>
          <span class="tw:text-sm tw:text-muted tw:shrink-0">{{ formatVnd(product.price) }}</span>
        </button>
      </div>
      <div v-if="addItemSelected" class="tw:flex tw:items-center tw:gap-3">
        <div class="tw:flex tw:items-center tw:gap-2 tw:shrink-0">
          <prime-button severity="secondary" text class="tw:w-8 tw:h-8 tw:p-0!" @click="addItemQty = Math.max(1, addItemQty - 1)">
            <iconify icon="ph:minus-bold" />
          </prime-button>
          <span class="tw:w-8 tw:text-center tw:font-semibold tw:tabular-nums">{{ addItemQty }}</span>
          <prime-button severity="secondary" text class="tw:w-8 tw:h-8 tw:p-0!" @click="addItemQty++">
            <iconify icon="ph:plus-bold" />
          </prime-button>
        </div>
        <prime-button severity="success" fluid @click="confirmAddItem">
          <iconify icon="ph:plus-bold" />
          <span>{{ t('orders.detail.addItem.confirm') }}</span>
        </prime-button>
      </div>
    </div>
  </prime-drawer>

  <!-- ── Merge drawer ───────────────────────────────────────────────── -->
  <prime-drawer
    v-model:visible="mergeDrawerVisible"
    position="bottom"
    :style="{ height: '70dvh' }"
    :pt="{ root: { class: 'tw:rounded-t-2xl' } }"
  >
    <template #header>
      <p class="tw:font-semibold tw:text-sm">{{ t('orders.detail.merge.title') }}</p>
    </template>
    <div class="tw:flex tw:flex-col tw:h-full tw:pb-4 tw:gap-3">
      <p class="tw:text-xs tw:text-muted">
        {{ t('orders.detail.merge.instruction', { orderNumber: order?.orderNumber }) }}
      </p>
      <div class="tw:flex-1 tw:overflow-y-auto tw:space-y-2">
        <div v-if="mergeFetching" class="tw:flex tw:justify-center tw:py-8">
          <iconify icon="prime:spinner" class="tw:animate-spin tw:text-2xl tw:text-muted" />
        </div>
        <p v-else-if="!mergeOrders_.length" class="tw:text-sm tw:text-muted tw:text-center tw:py-6">
          {{ t('orders.detail.merge.empty') }}
        </p>
        <button
          v-for="o in mergeOrders_"
          :key="o.id"
          class="tw:w-full tw:flex tw:items-center tw:justify-between tw:gap-3 tw:rounded-xl tw:px-3 tw:py-2.5 tw:text-left tw:transition-colors tw:cursor-pointer"
          :class="mergeSelected.includes(o.id) ? 'tw:bg-primary-500/15 tw:text-primary-300' : 'tw:hover:bg-white/5'"
          :style="mergeSelected.includes(o.id) ? '' : 'background: var(--app-bg-subtle)'"
          @click="mergeSelected.includes(o.id) ? (mergeSelected = mergeSelected.filter(id => id !== o.id)) : mergeSelected.push(o.id)"
        >
          <div>
            <span class="tw:font-mono tw:font-medium tw:text-sm">{{ o.orderNumber }}</span>
            <p class="tw:text-[11px] tw:text-muted tw:mt-0.5">{{ o.tableCode ?? t('orders.detail.noTable') }}</p>
          </div>
          <div class="tw:text-right">
            <span class="tw:text-sm tw:font-semibold">{{ formatVnd(o.finalAmount) }}</span>
            <p class="tw:text-[11px] tw:text-muted tw:mt-0.5">{{ o.itemCount }} {{ t('orders.detail.items') }}</p>
          </div>
        </button>
      </div>
      <prime-button
        severity="success" fluid
        :disabled="!mergeSelected.length"
        :loading="mergeLoading"
        @click="confirmMerge"
      >
        {{ t('orders.detail.merge.confirm', { n: mergeSelected.length }) }}
      </prime-button>
    </div>
  </prime-drawer>

  <!-- ── Split drawer ───────────────────────────────────────────────── -->
  <prime-drawer
    v-model:visible="splitDrawerVisible"
    position="bottom"
    :style="{ height: '80dvh' }"
    :pt="{ root: { class: 'tw:rounded-t-2xl' } }"
  >
    <template #header>
      <p class="tw:font-semibold tw:text-sm">{{ t('orders.detail.split.title') }}</p>
    </template>
    <div class="tw:flex tw:flex-col tw:h-full tw:pb-4 tw:gap-3">
      <p class="tw:text-xs tw:text-muted">{{ t('orders.detail.split.instruction') }}</p>

      <div class="tw:flex-1 tw:overflow-y-auto tw:space-y-3">
        <div v-for="item in splitItems" :key="item.productId" :class="[appCard, 'tw:rounded-xl tw:p-3']">
          <div class="tw:flex tw:items-start tw:justify-between tw:gap-3 tw:mb-2">
            <div>
              <p class="tw:font-medium tw:text-sm">{{ item.productName }}</p>
              <p class="tw:text-[11px] tw:text-muted tw:mt-0.5">{{ t('orders.detail.split.itemDetail', { n: item.quantity, price: formatVnd(item.unitPrice) }) }}</p>
            </div>
          </div>
          <div class="tw:flex tw:items-center tw:gap-3">
            <span class="tw:text-xs tw:text-muted tw:shrink-0">{{ t('orders.detail.split.toNew') }}</span>
            <div class="tw:flex tw:items-center tw:gap-2">
              <prime-button
                severity="secondary" text
                class="tw:w-8 tw:h-8 tw:p-0!"
                :disabled="item.splitQty <= 0"
                @click="item.splitQty = Math.max(0, item.splitQty - 1)"
              >
                <iconify icon="ph:minus-bold" class="tw:text-xs" />
              </prime-button>
              <span class="tw:w-8 tw:text-center tw:font-semibold tw:tabular-nums tw:text-sm">{{ item.splitQty }}</span>
              <prime-button
                severity="secondary" text
                class="tw:w-8 tw:h-8 tw:p-0!"
                :disabled="item.splitQty >= item.quantity"
                @click="item.splitQty = Math.min(item.quantity, item.splitQty + 1)"
              >
                <iconify icon="ph:plus-bold" class="tw:text-xs" />
              </prime-button>
            </div>
            <span class="tw:text-xs tw:text-muted">/ {{ item.quantity }}</span>
          </div>
        </div>
      </div>

      <!-- Preview -->
      <div class="tw:flex tw:gap-3 tw:text-center tw:text-xs">
        <div :class="[appCard, 'tw:rounded-xl tw:p-3 tw:flex-1']">
          <p class="tw:text-muted tw:mb-1">{{ t('orders.detail.split.thisOrderKeeps') }}</p>
          <p class="tw:font-semibold">{{ t('orders.detail.split.itemCount', { n: splitPreview.keepQty }) }}</p>
        </div>
        <div :class="[appCard, 'tw:rounded-xl tw:p-3 tw:flex-1']">
          <p class="tw:text-muted tw:mb-1">{{ t('orders.detail.split.newOrderGets') }}</p>
          <p class="tw:font-semibold" :class="splitPreview.toNewQty > 0 ? 'tw:text-primary-400' : ''">
            {{ t('orders.detail.split.itemCount', { n: splitPreview.toNewQty }) }}
          </p>
        </div>
      </div>

      <p v-if="splitPreview.toNewQty > 0 && splitPreview.keepQty === 0" class="tw:text-xs tw:text-red-400 tw:text-center">
        {{ t('orders.detail.split.error') }}
      </p>

      <prime-button
        severity="success" fluid
        :disabled="!splitValid"
        :loading="splitLoading"
        @click="confirmSplit"
      >
        <iconify icon="ph:scissors-bold" />
        <span>{{ t('orders.detail.split.confirm') }}</span>
      </prime-button>
    </div>
  </prime-drawer>
</template>
