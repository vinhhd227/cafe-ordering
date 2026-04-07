<script setup>
import { updatePayment } from '@/services/order.service'
import { PAYMENT_METHOD } from '@/constants/paymentMethod'
import { PAYMENT_STATUS } from '@/constants/paymentStatus'

const props = defineProps({
  order: { type: Object, default: null },
})
const emit = defineEmits(['paid', 'close'])

const { t } = useI18n()

const payMethod = ref(PAYMENT_METHOD.CASH)
const payAmountReceived = ref(null)
const payTip = ref(0)
const payLoading = ref(false)
const errorMessage = ref('')

const visible = computed({
  get: () => props.order !== null,
  set: (v) => { if (!v) emit('close') },
})

const PAYMENT_METHODS = computed(() => [
  { label: t('orders.paymentMethod.CASH'), value: PAYMENT_METHOD.CASH, icon: 'ph:money-bold' },
  { label: t('orders.paymentMethod.BANK_TRANSFER'), value: PAYMENT_METHOD.BANK_TRANSFER, icon: 'ph:bank-bold' },
])

const payChange = computed(() => {
  if (payAmountReceived.value == null || !props.order) return null
  return payAmountReceived.value - (props.order.finalAmount ?? props.order.totalAmount)
})

const payReturn = computed(() => {
  if (payChange.value === null || payChange.value <= 0) return null
  return payChange.value - (payTip.value ?? 0)
})

watch(payChange, (val) => {
  if (val !== null && val >= 0 && payTip.value > val) payTip.value = val
})

watch(() => props.order, (val) => {
  if (val) {
    payMethod.value = PAYMENT_METHOD.CASH
    payAmountReceived.value = null
    payTip.value = 0
    payLoading.value = false
    errorMessage.value = ''
  }
})

const formatVnd = (value) =>
  new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(value ?? 0)

const confirmPayment = async () => {
  if (!props.order) return
  payLoading.value = true
  errorMessage.value = ''
  try {
    await updatePayment(props.order.id, PAYMENT_STATUS.PAID, payMethod.value, payAmountReceived.value, payTip.value ?? 0)
    emit('paid', props.order, { paymentMethod: payMethod.value })
  } catch (err) {
    errorMessage.value = err?.response?.data?.errors?.join(', ') || err?.response?.data?.title || 'Failed to update payment.'
  } finally {
    payLoading.value = false
  }
}
</script>

<template>
  <prime-dialog
    v-model:visible="visible"
    :header="t('orders.pay.title')"
    :modal="true"
    class="tw:w-[22rem]"
  >
    <div class="tw:space-y-4">
      <p class="tw:text-sm app-text-muted">
        {{ t('orders.pay.order') }}
        <span class="tw:font-mono tw:font-semibold tw:text-white">{{ order?.orderNumber }}</span>
      </p>

      <!-- Error -->
      <prime-message v-if="errorMessage" severity="error" size="small" variant="simple">
        {{ errorMessage }}
      </prime-message>

      <!-- Amount received -->
      <div class="tw:space-y-1.5">
        <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">
          {{ t('orders.pay.amountReceived') }}
        </label>
        <prime-input-number
          v-model="payAmountReceived"
          :min="0"
          :use-grouping="true"
          :placeholder="String(order?.finalAmount ?? order?.totalAmount ?? '')"
          class="app-input tw:w-full"
          suffix=" ₫"
          @input="(e) => (payAmountReceived = e.value)"
        />
        <div
          v-if="payChange !== null && payChange < 0"
          class="tw:flex tw:items-center tw:justify-between tw:text-sm tw:pt-0.5"
        >
          <span class="app-text-muted">{{ t('orders.pay.short') }}</span>
          <span class="tw:text-red-400 tw:font-semibold">{{ formatVnd(Math.abs(payChange)) }}</span>
        </div>
        <template v-if="payChange !== null && payChange > 0">
          <div class="tw:flex tw:items-center tw:justify-between tw:text-sm tw:pt-0.5">
            <span class="app-text-muted">{{ t('orders.pay.change') }}</span>
            <span class="tw:font-semibold">{{ formatVnd(payChange) }}</span>
          </div>
          <div class="tw:space-y-1">
            <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">
              {{ t('orders.pay.tip') }}
            </label>
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
              <prime-button
                severity="secondary"
                outlined
                v-tooltip.top="t('orders.pay.keepAllAsTip')"
                @click="payTip = payChange"
              >
                <iconify icon="ph:heart-bold" />
              </prime-button>
            </div>
          </div>
          <div class="tw:flex tw:items-center tw:justify-between tw:text-sm">
            <span class="app-text-muted">{{ t('orders.pay.returnToCustomer') }}</span>
            <span :class="payReturn === 0 ? 'app-text-muted' : 'tw:text-emerald-400 tw:font-semibold'">
              {{ payReturn === 0 ? '—' : formatVnd(payReturn) }}
            </span>
          </div>
        </template>
      </div>

      <!-- Payment method -->
      <div class="tw:space-y-2">
        <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">
          {{ t('orders.pay.paymentMethod') }}
        </label>
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
    </div>

    <template #footer>
      <prime-button severity="secondary" outlined @click="emit('close')">
        {{ t('orders.cancel') }}
      </prime-button>
      <prime-button severity="success" :loading="payLoading" @click="confirmPayment">
        <iconify icon="ph:check-bold" />
        <span>{{ t('orders.pay.confirmPayment') }}</span>
      </prime-button>
    </template>
  </prime-dialog>
</template>
