<script setup>
import { listPrinters, printDrinkLabels } from '@/services/printer.service'

const props = defineProps({
  visible: { type: Boolean, default: false },
  orderId: { type: Number, required: true },
  orderNumber: { type: String, default: '' },
  items: { type: Array, default: () => [] },
})

const emit = defineEmits(['update:visible'])

const { t } = useI18n()
const toast = useToast()

// ── State ──────────────────────────────────────────────────────────
const printers = ref([])
const selectedItemIds = ref([])
const selectedPrinterId = ref(null)
const copies = ref(1)
const printing = ref(false)
const printError = ref('')

const dialogVisible = computed({
  get: () => props.visible,
  set: (v) => emit('update:visible', v),
})

// ── Printer options ────────────────────────────────────────────────
const drinkLabelPrinters = computed(() =>
  printers.value.filter(p => p.role === 'DRINK_LABEL' && p.isActive)
)
const printerOptions = computed(() => [
  { label: t('printers.print.defaultPrinter'), value: null },
  ...drinkLabelPrinters.value.map(p => ({
    label: p.isDefault ? `${p.name} (${t('printers.badge.default')})` : p.name,
    value: p.id,
  })),
])

// ── Helpers ────────────────────────────────────────────────────────
const formatOption = (key, value) => {
  if (!value) return null
  const labels = {
    HOT: t('printers.print.temp.HOT'), COLD: t('printers.print.temp.COLD'),
    LESS: t('printers.print.level.LESS'), NORMAL: t('printers.print.level.NORMAL'), MORE: t('printers.print.level.MORE'),
  }
  return labels[value] ?? value
}

const itemOptions = (item) => {
  const parts = []
  if (item.temperature) parts.push(formatOption('temp', item.temperature))
  if (item.iceLevel)    parts.push(`${t('printers.print.ice')}: ${formatOption('level', item.iceLevel)}`)
  if (item.sugarLevel)  parts.push(`${t('printers.print.sugar')}: ${formatOption('level', item.sugarLevel)}`)
  return parts.filter(Boolean).join(' · ')
}

// ── Init on open ───────────────────────────────────────────────────
watch(dialogVisible, async (open) => {
  if (!open) return
  printError.value = ''
  selectedItemIds.value = props.items.map(i => i.id)
  copies.value = 1
  selectedPrinterId.value = null
  try {
    const res = await listPrinters()
    printers.value = res.data ?? []
  } catch {
    printers.value = []
  }
})

// ── Print ──────────────────────────────────────────────────────────
const handlePrint = async () => {
  if (selectedItemIds.value.length === 0) {
    printError.value = t('printers.print.noItemsSelected')
    return
  }
  printing.value = true
  printError.value = ''
  try {
    const res = await printDrinkLabels({
      orderId: props.orderId,
      itemIds: selectedItemIds.value,
      printerId: selectedPrinterId.value,
      copiesPerItem: copies.value,
    })
    const data = res.data
    if (data.requiresClientPrint) {
      // WebUSB: send bytes to printer via Web USB API
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

const sendWebUsb = async (base64Bytes) => {
  const bytes = Uint8Array.from(atob(base64Bytes), c => c.charCodeAt(0))
  const device = await navigator.usb.requestDevice({ filters: [] })
  await device.open()
  await device.selectConfiguration(1)
  await device.claimInterface(0)
  await device.transferOut(1, bytes)
  await device.close()
}

const toggleAll = () => {
  if (selectedItemIds.value.length === props.items.length) {
    selectedItemIds.value = []
  } else {
    selectedItemIds.value = props.items.map(i => i.id)
  }
}
</script>

<template>
  <prime-dialog
    v-model:visible="dialogVisible"
    :header="t('printers.print.title')"
    :modal="true"
    class="tw:w-[32rem]"
  >
    <div class="tw:space-y-5">
      <!-- Order number -->
      <p class="tw:text-sm tw:text-muted">
        {{ t('printers.print.order') }}: <span class="tw:font-semibold tw:text-base">{{ orderNumber }}</span>
      </p>

      <!-- Item selection -->
      <div class="tw:space-y-2">
        <div class="tw:flex tw:items-center tw:justify-between">
          <p class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">{{ t('printers.print.selectItems') }}</p>
          <prime-button severity="secondary" text size="small" @click="toggleAll" class="tw:text-xs">
            {{ selectedItemIds.length === items.length ? t('printers.print.deselectAll') : t('printers.print.selectAll') }}
          </prime-button>
        </div>
        <div class="tw:space-y-1.5 tw:max-h-48 tw:overflow-y-auto">
          <label v-for="item in items" :key="item.id"
            :class="['tw:flex tw:items-start tw:gap-3 tw:rounded-lg tw:border tw:px-3 tw:py-2.5 tw:cursor-pointer tw:transition-colors',
              selectedItemIds.includes(item.id)
                ? 'tw:border-primary-400 tw:bg-primary-50 tw:dark:bg-primary-500/10'
                : 'tw:border-slate-200 tw:dark:border-white/10']">
            <prime-checkbox
              v-model="selectedItemIds"
              :value="item.id"
              class="tw:mt-0.5 tw:shrink-0"
            />
            <div class="tw:min-w-0">
              <p class="tw:text-sm tw:font-medium tw:truncate">{{ item.productName }}</p>
              <p v-if="itemOptions(item)" class="tw:text-xs tw:text-muted tw:truncate">{{ itemOptions(item) }}</p>
              <p v-if="item.note" class="tw:text-xs tw:text-muted tw:italic tw:truncate">{{ item.note }}</p>
              <div class="tw:flex tw:items-center tw:gap-1.5 tw:mt-0.5">
                <prime-tag v-if="item.isTakeaway" :value="t('printers.print.takeaway')" severity="warn" class="tw:text-[10px]! tw:px-1.5! tw:py-0!" />
                <span class="tw:text-xs tw:text-muted">×{{ item.quantity }}</span>
              </div>
            </div>
          </label>
        </div>
      </div>

      <!-- Printer + Copies -->
      <div class="tw:grid tw:grid-cols-2 tw:gap-3">
        <div class="tw:space-y-1.5">
          <label for="pd-printer" class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">{{ t('printers.print.printer') }}</label>
          <prime-select id="pd-printer" v-model="selectedPrinterId"
            :options="printerOptions" option-label="label" option-value="value"
            class="app-input tw:w-full" />
        </div>
        <div class="tw:space-y-1.5">
          <label for="pd-copies" class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">{{ t('printers.print.copies') }}</label>
          <prime-input-number id="pd-copies" v-model="copies" :min="1" :max="5" :use-grouping="false" class="app-input tw:w-full" />
        </div>
      </div>

      <!-- Label preview (CSS simulation) -->
      <div class="tw:space-y-1.5" v-if="selectedItemIds.length > 0">
        <p class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">{{ t('printers.print.preview') }}</p>
        <div class="tw:bg-white tw:dark:bg-gray-900 tw:rounded-lg tw:border tw:border-slate-200 tw:dark:border-white/10 tw:p-3 tw:font-mono tw:text-xs tw:leading-relaxed tw:max-h-40 tw:overflow-y-auto">
          <template v-for="item in items.filter(i => selectedItemIds.includes(i.id))" :key="item.id">
            <div class="tw:border-b tw:border-dashed tw:border-slate-300 tw:dark:border-white/20 tw:pb-2 tw:mb-2 tw:last:border-0 tw:last:pb-0 tw:last:mb-0">
              <div class="tw:flex tw:justify-between tw:text-[11px] tw:text-muted">
                <span>{{ t('printers.print.previewTable') }}</span>
                <span>{{ orderNumber }}</span>
              </div>
              <p class="tw:font-bold tw:text-sm tw:text-center tw:my-1">{{ item.productName }}</p>
              <p v-if="itemOptions(item)" class="tw:text-[11px]">{{ itemOptions(item) }}</p>
              <p v-if="item.note" class="tw:text-[11px] tw:text-muted">{{ t('printers.print.previewNote') }}: {{ item.note }}</p>
              <div class="tw:flex tw:justify-between tw:text-[11px] tw:text-muted tw:mt-1">
                <span>{{ new Date().toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' }) }}</span>
                <span>×{{ item.quantity }}</span>
              </div>
            </div>
          </template>
        </div>
      </div>

      <p v-if="printError" class="tw:text-xs tw:text-red-400">{{ printError }}</p>
    </div>

    <template #footer>
      <prime-button severity="secondary" outlined @click="dialogVisible = false">{{ t('common.cancel') }}</prime-button>
      <prime-button severity="success" :loading="printing" :disabled="selectedItemIds.length === 0" @click="handlePrint">
        <iconify icon="ph:printer-bold" />
        <span>{{ t('printers.print.printBtn') }}</span>
      </prime-button>
    </template>
  </prime-dialog>
</template>
