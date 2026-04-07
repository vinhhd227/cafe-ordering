import { ORDER_STATUS_MAP } from '@/constants/orderStatus'
import { PAYMENT_STATUS_MAP } from '@/constants/paymentStatus'
import { PAYMENT_METHOD } from '@/constants/paymentMethod'

export function useOrderListFilters() {
  const { t } = useI18n()

  const todayMidnight = () => {
    const d = new Date()
    d.setHours(0, 0, 0, 0)
    return d
  }

  const filters = reactive({
    dateFrom: todayMidnight(),
    dateTo: todayMidnight(),
    statusFilter: null,
    searchOrder: '',
    paymentStatusFilter: null,
    paymentMethodFilter: null,
    minTotal: null,
    maxTotal: null,
    tableCodeFilter: '',
  })

  const statusOptions = computed(() =>
    Object.entries(ORDER_STATUS_MAP).map(([value]) => ({
      label: t(`orders.status.${value}`),
      value,
    }))
  )

  const paymentStatusOptions = computed(() =>
    Object.entries(PAYMENT_STATUS_MAP).map(([value]) => ({
      label: t(`orders.paymentStatus.${value}`),
      value,
    }))
  )

  const paymentMethodOptions = computed(() => [
    { label: t('orders.paymentMethod.CASH'), value: PAYMENT_METHOD.CASH },
    { label: t('orders.paymentMethod.BANK_TRANSFER'), value: PAYMENT_METHOD.BANK_TRANSFER },
  ])

  const activeFilterCount = computed(() => {
    let n = 0
    if (filters.dateFrom) n++
    if (filters.dateTo) n++
    if (filters.statusFilter !== null) n++
    if (filters.paymentStatusFilter !== null) n++
    if (filters.paymentMethodFilter !== null) n++
    if (filters.minTotal !== null) n++
    if (filters.maxTotal !== null) n++
    if (filters.tableCodeFilter.trim()) n++
    return n
  })

  const hasActiveFilters = computed(() => activeFilterCount.value > 0)

  const clearFilters = () => {
    filters.dateFrom = todayMidnight()
    filters.dateTo = todayMidnight()
    filters.statusFilter = null
    filters.paymentStatusFilter = null
    filters.paymentMethodFilter = null
    filters.minTotal = null
    filters.maxTotal = null
    filters.tableCodeFilter = ''
    filters.searchOrder = ''
  }

  return {
    filters,
    statusOptions,
    paymentStatusOptions,
    paymentMethodOptions,
    activeFilterCount,
    hasActiveFilters,
    clearFilters,
    todayMidnight,
  }
}
