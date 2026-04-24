<script setup>
import { listPrinters, printBill } from '@/services/printer.service'

const props = defineProps({
  visible: { type: Boolean, default: false },
  orderId: { type: Number, required: true },
  orderNumber: { type: String, default: '' },
})

const emit = defineEmits(['update:visible'])

const { t } = useI18n()
const toast = useToast()

const printers = ref([])
const selectedPrinterId = ref(null)
const printing = ref(false)
const printError = ref('')

const dialogVisible = computed({
  get: () => props.visible,
  set: (v) => emit('update:visible', v),
})

const receiptPrinters = computed(() =>
  printers.value.filter(p => p.role === 'RECEIPT' && p.isActive)
)
const printerOptions = computed(() => [
  { label: t('printers.print.defaultPrinter'), value: null },
  ...receiptPrinters.value.map(p => ({
    label: p.isDefault ? `${p.name} (${t('printers.badge.default')})` : p.name,
    value: p.id,
  })),
])

watch(dialogVisible, async (open) => {
  if (!open) return
  printError.value = ''
  selectedPrinterId.value = null
  try {
    const res = await listPrinters()
    printers.value = res.data ?? []
  } catch {
    printers.value = []
  }
})

const sendWebUsb = async (base64Bytes) => {
  const bytes = Uint8Array.from(atob(base64Bytes), c => c.charCodeAt(0))
  const device = await navigator.usb.requestDevice({ filters: [] })
  await device.open()
  await device.selectConfiguration(1)
  await device.claimInterface(0)
  await device.transferOut(1, bytes)
  await device.close()
}

const handlePrint = async () => {
  printing.value = true
  printError.value = ''
  try {
    const res = await printBill({
      orderId: props.orderId,
      printerId: selectedPrinterId.value,
    })
    const data = res.data
    if (data.requiresClientPrint) {
      await sendWebUsb(data.bytes)
    }
    if (data.success || data.requiresClientPrint) {
      toast.add({ severity: 'success', summary: t('printers.print.success'), life: 3000 })
      dialogVisible.value = false
    } else {
      printError.value = data.error ?? t('printers.error.printFailed')
    }
  } catch (err) {
    printError.value = err?.response?.data?.errors?.join(', ') ?? err?.response?.data?.title ?? t('printers.error.printFailed')
  } finally {
    printing.value = false
  }
}
</script>

<template>
  <prime-dialog
    v-model:visible="dialogVisible"
    :header="t('printers.printBill.title')"
    :modal="true"
    class="tw:w-[26rem]"
  >
    <div class="tw:space-y-5">
      <p class="tw:text-sm tw:text-muted">
        {{ t('printers.print.order') }}: <span class="tw:font-semibold tw:text-base">{{ orderNumber }}</span>
      </p>

      <div class="tw:space-y-1.5">
        <label for="bill-printer" class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
          {{ t('printers.print.printer') }}
        </label>
        <prime-select
          id="bill-printer"
          v-model="selectedPrinterId"
          :options="printerOptions"
          option-label="label"
          option-value="value"
          class="app-input tw:w-full"
        />
      </div>

      <p v-if="printError" class="tw:text-xs tw:text-red-400">{{ printError }}</p>
    </div>

    <template #footer>
      <prime-button severity="secondary" outlined @click="dialogVisible = false">
        {{ t('common.cancel') }}
      </prime-button>
      <prime-button severity="success" :loading="printing" @click="handlePrint">
        <iconify icon="ph:receipt-bold" />
        <span>{{ t('printers.printBill.printBtn') }}</span>
      </prime-button>
    </template>
  </prime-dialog>
</template>
