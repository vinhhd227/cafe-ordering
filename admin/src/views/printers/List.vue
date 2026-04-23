<script setup>
import {
  listPrinters,
  createPrinter,
  updatePrinter,
  deletePrinter,
  setDefaultPrinter,
  testPrinter,
} from '@/services/printer.service'

const { t } = useI18n()
const toast = useToast()
const { can } = usePermission()

// ── Constants ──────────────────────────────────────────────────────
const ROLES = ['DRINK_LABEL', 'RECEIPT', 'KITCHEN']
const ROLE_ICON = { DRINK_LABEL: 'ph:tag-bold', RECEIPT: 'ph:receipt-bold', KITCHEN: 'ph:cooking-pot-bold' }

const FORMATTER_OPTIONS = [
  { label: 'TSPL (TSC label)',  value: 'TSPL' },
  { label: 'ESC/POS (receipt)', value: 'ESC_POS' },
  { label: 'ZPL',               value: 'ZPL' },
  { label: 'STAR PRNT',         value: 'STAR_PRNT' },
]
const TRANSPORT_OPTIONS = computed(() => [
  { label: t('printers.transport.USB_DEVICE'), value: 'USB_DEVICE' },
  { label: t('printers.transport.TCP'),        value: 'TCP' },
  { label: t('printers.transport.WEB_USB'),    value: 'WEB_USB' },
])
const ROLE_OPTIONS = computed(() => ROLES.map(r => ({ label: t(`printers.role.${r}`), value: r })))
const PAPER_OPTIONS = [{ label: '58 mm', value: 58 }, { label: '80 mm', value: 80 }]

// ── State ──────────────────────────────────────────────────────────
const loading = ref(false)
const errorMessage = ref('')
const printers = ref([])


const copyText = async (text) => {
  try {
    await navigator.clipboard.writeText(text)
  } catch {
    // fallback for http / older browsers
    const el = document.createElement('textarea')
    el.value = text
    el.style.position = 'fixed'
    el.style.opacity = '0'
    document.body.appendChild(el)
    el.select()
    document.execCommand('copy')
    document.body.removeChild(el)
  }
  toast.add({ severity: 'success', summary: t('common.copied'), life: 1500 })
}

const grouped = computed(() =>
  ROLES.map(role => ({
    role,
    label: t(`printers.role.${role}`),
    icon: ROLE_ICON[role],
    items: printers.value.filter(p => p.role === role),
  })).filter(g => g.items.length > 0)
)

// ── Load ───────────────────────────────────────────────────────────
const load = async () => {
  loading.value = true
  errorMessage.value = ''
  try {
    const res = await listPrinters()
    printers.value = res.data ?? []
  } catch (err) {
    errorMessage.value = err?.response?.data?.title ?? t('printers.error.loadFailed')
  } finally {
    loading.value = false
  }
}

onMounted(load)

// ── Form helpers ───────────────────────────────────────────────────
const emptyForm = () => ({
  name: '', role: 'DRINK_LABEL', formatterType: 'TSPL',
  transportType: 'USB_DEVICE', paperWidthMm: 58,
  devicePath: '/dev/usb/lp0', host: '', port: 9100, vendorId: '', productId: '',
})

const buildConnectionParams = (f) => {
  if (f.transportType === 'USB_DEVICE') return JSON.stringify({
    devicePath: f.devicePath || null,
  })
  if (f.transportType === 'TCP')        return JSON.stringify({ host: f.host, port: Number(f.port) })
  return JSON.stringify({ vendorId: f.vendorId, productId: f.productId })
}

const parseConnectionParams = (raw, type) => {
  try {
    const o = JSON.parse(raw)
    if (type === 'USB_DEVICE') return { devicePath: o.devicePath ?? '' }
    if (type === 'TCP')        return { host: o.host ?? '', port: o.port ?? 9100 }
    return { vendorId: o.vendorId ?? '', productId: o.productId ?? '' }
  } catch { return {} }
}

// ── Create ─────────────────────────────────────────────────────────
const showCreate = ref(false)
const cf = ref(emptyForm())
const createLoading = ref(false)
const createError = ref('')

const openCreate = () => { cf.value = emptyForm(); createError.value = ''; showCreate.value = true }

const handleCreate = async () => {
  createLoading.value = true; createError.value = ''
  try {
    await createPrinter({ ...cf.value, connectionParams: buildConnectionParams(cf.value) })
    toast.add({ severity: 'success', summary: t('printers.toast.created'), life: 3000 })
    showCreate.value = false
    await load()
  } catch (err) {
    createError.value = err?.response?.data?.errors?.join(', ') ?? err?.response?.data?.title ?? t('printers.error.createFailed')
  } finally { createLoading.value = false }
}

// ── Edit ───────────────────────────────────────────────────────────
const showEdit = ref(false)
const editRow = ref(null)
const ef = ref(emptyForm())
const editLoading = ref(false)
const editError = ref('')

const openEdit = (p) => {
  editRow.value = p
  const parsed = parseConnectionParams(p.connectionParams, p.transportType)
  ef.value = { name: p.name, role: p.role, formatterType: p.formatterType, transportType: p.transportType, paperWidthMm: p.paperWidthMm,
    devicePath: parsed.devicePath ?? '',
    host: parsed.host ?? '', port: parsed.port ?? 9100,
    vendorId: parsed.vendorId ?? '', productId: parsed.productId ?? '' }
  editError.value = ''; showEdit.value = true
}

const handleEdit = async () => {
  editLoading.value = true; editError.value = ''
  try {
    await updatePrinter(editRow.value.id, { name: ef.value.name, formatterType: ef.value.formatterType,
      transportType: ef.value.transportType, paperWidthMm: ef.value.paperWidthMm,
      connectionParams: buildConnectionParams(ef.value) })
    toast.add({ severity: 'success', summary: t('printers.toast.updated'), life: 3000 })
    showEdit.value = false
    await load()
  } catch (err) {
    editError.value = err?.response?.data?.errors?.join(', ') ?? err?.response?.data?.title ?? t('printers.error.updateFailed')
  } finally { editLoading.value = false }
}

// ── Delete ─────────────────────────────────────────────────────────
const handleDelete = async (p) => {
  try {
    await deletePrinter(p.id)
    toast.add({ severity: 'success', summary: t('printers.toast.deleted'), life: 3000 })
    await load()
  } catch (err) {
    errorMessage.value = err?.response?.data?.title ?? t('printers.error.deleteFailed')
  }
}

// ── Set default ────────────────────────────────────────────────────
const handleSetDefault = async (p) => {
  try {
    await setDefaultPrinter(p.id)
    toast.add({ severity: 'success', summary: t('printers.toast.defaultSet'), life: 3000 })
    await load()
  } catch (err) {
    errorMessage.value = err?.response?.data?.title ?? t('printers.error.setDefaultFailed')
  }
}

// ── Test ───────────────────────────────────────────────────────────
const testingId = ref(null)
const handleTest = async (p) => {
  testingId.value = p.id
  try {
    const res = await testPrinter(p.id)
    if (res.data?.success) toast.add({ severity: 'success', summary: t('printers.toast.testOk'), life: 3000 })
    else toast.add({ severity: 'error', summary: t('printers.toast.testFailed'), detail: res.data?.error, life: 5000 })
  } catch {
    toast.add({ severity: 'error', summary: t('printers.toast.testFailed'), life: 4000 })
  } finally { testingId.value = null }
}
</script>

<template>
  <!-- ── Create dialog ──────────────────────────────────────────────── -->
  <prime-dialog v-model:visible="showCreate" :header="t('printers.create.title')" :modal="true" class="tw:w-[28rem]">
    <div class="tw:space-y-4">
      <div class="tw:space-y-1.5">
        <label for="cf-name" class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">{{ t('printers.form.name') }}</label>
        <prime-input-text id="cf-name" v-model="cf.name" size="small" class="app-input tw:w-full" :placeholder="t('printers.form.namePlaceholder')" />
      </div>
      <div class="tw:space-y-1.5">
        <label for="cf-role" class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">{{ t('printers.form.role') }}</label>
        <prime-select id="cf-role" v-model="cf.role" :options="ROLE_OPTIONS" option-label="label" option-value="value" size="small" class="app-input tw:w-full" />
      </div>
      <div class="tw:grid tw:grid-cols-2 tw:gap-3">
        <div class="tw:space-y-1.5">
          <label for="cf-fmt" class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">{{ t('printers.form.formatter') }}</label>
          <prime-select id="cf-fmt" v-model="cf.formatterType" :options="FORMATTER_OPTIONS" option-label="label" option-value="value" size="small" class="app-input tw:w-full" />
        </div>
        <div class="tw:space-y-1.5">
          <label for="cf-paper" class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">{{ t('printers.form.paper') }}</label>
          <prime-select id="cf-paper" v-model="cf.paperWidthMm" :options="PAPER_OPTIONS" option-label="label" option-value="value" size="small" class="app-input tw:w-full" />
        </div>
      </div>
      <div class="tw:space-y-1.5">
        <label for="cf-transport" class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">{{ t('printers.form.transport') }}</label>
        <prime-select id="cf-transport" v-model="cf.transportType" :options="TRANSPORT_OPTIONS" option-label="label" option-value="value" size="small" class="app-input tw:w-full" />
      </div>
      <!-- Dynamic connection params -->
      <template v-if="cf.transportType === 'USB_DEVICE'">
        <div class="tw:space-y-1.5">
          <label for="cf-path" class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">{{ t('printers.form.devicePath') }}</label>
          <prime-input-text id="cf-path" v-model="cf.devicePath" size="small" class="app-input tw:w-full tw:font-mono" placeholder="/dev/usb/lp0" />
          <p class="tw:text-xs tw:text-muted">{{ t('printers.form.devicePathHint') }}</p>
        </div>
      </template>
      <template v-else-if="cf.transportType === 'TCP'">
        <div class="tw:grid tw:grid-cols-3 tw:gap-3">
          <div class="tw:col-span-2 tw:space-y-1.5">
            <label for="cf-host" class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">{{ t('printers.form.host') }}</label>
            <prime-input-text id="cf-host" v-model="cf.host" size="small" class="app-input tw:w-full tw:font-mono" placeholder="192.168.1.50" />
          </div>
          <div class="tw:space-y-1.5">
            <label for="cf-port" class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">{{ t('printers.form.port') }}</label>
            <prime-input-number id="cf-port" v-model="cf.port" :use-grouping="false" size="small" class="app-input tw:w-full" />
          </div>
        </div>
      </template>
      <template v-else-if="cf.transportType === 'WEB_USB'">
        <div class="tw:grid tw:grid-cols-2 tw:gap-3">
          <div class="tw:space-y-1.5">
            <label for="cf-vid" class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">{{ t('printers.form.vendorId') }}</label>
            <prime-input-text id="cf-vid" v-model="cf.vendorId" size="small" class="app-input tw:w-full tw:font-mono" placeholder="0x04B8" />
          </div>
          <div class="tw:space-y-1.5">
            <label for="cf-pid" class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">{{ t('printers.form.productId') }}</label>
            <prime-input-text id="cf-pid" v-model="cf.productId" size="small" class="app-input tw:w-full tw:font-mono" placeholder="0x0E27" />
          </div>
        </div>
      </template>
      <p v-if="createError" class="tw:text-xs tw:text-red-400">{{ createError }}</p>
    </div>
    <template #footer>
      <prime-button size="small" severity="secondary" outlined @click="showCreate = false">{{ t('common.cancel') }}</prime-button>
      <prime-button size="small" severity="success" :loading="createLoading" @click="handleCreate">
        <iconify icon="ph:plus-bold" />
        <span>{{ t('printers.create.submit') }}</span>
      </prime-button>
    </template>
  </prime-dialog>

  <!-- ── Edit dialog ────────────────────────────────────────────────── -->
  <prime-dialog v-model:visible="showEdit" :header="t('printers.edit.title')" :modal="true" class="tw:w-[28rem]">
    <div class="tw:space-y-4">
      <div class="tw:space-y-1.5">
        <label for="ef-name" class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">{{ t('printers.form.name') }}</label>
        <prime-input-text id="ef-name" v-model="ef.name" size="small" class="app-input tw:w-full" />
      </div>
      <div class="tw:space-y-1.5">
        <label class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">{{ t('printers.form.role') }}</label>
        <p class="tw:text-sm tw:font-medium">{{ t(`printers.role.${ef.role}`) }}</p>
        <p class="tw:text-[11px] tw:text-muted">{{ t('printers.form.roleReadonly') }}</p>
      </div>
      <div class="tw:grid tw:grid-cols-2 tw:gap-3">
        <div class="tw:space-y-1.5">
          <label for="ef-fmt" class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">{{ t('printers.form.formatter') }}</label>
          <prime-select id="ef-fmt" v-model="ef.formatterType" :options="FORMATTER_OPTIONS" option-label="label" option-value="value" size="small" class="app-input tw:w-full" />
        </div>
        <div class="tw:space-y-1.5">
          <label for="ef-paper" class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">{{ t('printers.form.paper') }}</label>
          <prime-select id="ef-paper" v-model="ef.paperWidthMm" :options="PAPER_OPTIONS" option-label="label" option-value="value" size="small" class="app-input tw:w-full" />
        </div>
      </div>
      <div class="tw:space-y-1.5">
        <label for="ef-transport" class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">{{ t('printers.form.transport') }}</label>
        <prime-select id="ef-transport" v-model="ef.transportType" :options="TRANSPORT_OPTIONS" option-label="label" option-value="value" size="small" class="app-input tw:w-full" />
      </div>
      <template v-if="ef.transportType === 'USB_DEVICE'">
        <div class="tw:space-y-1.5">
          <label for="ef-path" class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">{{ t('printers.form.devicePath') }}</label>
          <prime-input-text id="ef-path" v-model="ef.devicePath" size="small" class="app-input tw:w-full tw:font-mono" placeholder="/dev/usb/lp0" />
          <p class="tw:text-xs tw:text-muted">{{ t('printers.form.devicePathHint') }}</p>
        </div>
      </template>
      <template v-else-if="ef.transportType === 'TCP'">
        <div class="tw:grid tw:grid-cols-3 tw:gap-3">
          <div class="tw:col-span-2 tw:space-y-1.5">
            <label for="ef-host" class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">{{ t('printers.form.host') }}</label>
            <prime-input-text id="ef-host" v-model="ef.host" size="small" class="app-input tw:w-full tw:font-mono" />
          </div>
          <div class="tw:space-y-1.5">
            <label for="ef-port" class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">{{ t('printers.form.port') }}</label>
            <prime-input-number id="ef-port" v-model="ef.port" :use-grouping="false" size="small" class="app-input tw:w-full" />
          </div>
        </div>
      </template>
      <template v-else-if="ef.transportType === 'WEB_USB'">
        <div class="tw:grid tw:grid-cols-2 tw:gap-3">
          <div class="tw:space-y-1.5">
            <label for="ef-vid" class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">{{ t('printers.form.vendorId') }}</label>
            <prime-input-text id="ef-vid" v-model="ef.vendorId" size="small" class="app-input tw:w-full tw:font-mono" />
          </div>
          <div class="tw:space-y-1.5">
            <label for="ef-pid" class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">{{ t('printers.form.productId') }}</label>
            <prime-input-text id="ef-pid" v-model="ef.productId" size="small" class="app-input tw:w-full tw:font-mono" />
          </div>
        </div>
      </template>
      <p v-if="editError" class="tw:text-xs tw:text-red-400">{{ editError }}</p>
    </div>
    <template #footer>
      <prime-button size="small" severity="secondary" outlined @click="showEdit = false">{{ t('common.cancel') }}</prime-button>
      <prime-button size="small" severity="primary" :loading="editLoading" @click="handleEdit">
        <iconify icon="ph:check-bold" />
        <span>{{ t('common.save') }}</span>
      </prime-button>
    </template>
  </prime-dialog>

  <section class="tw:space-y-8">
    <!-- ── Header ─────────────────────────────────────────────────── -->
    <div class="tw:flex tw:flex-wrap tw:items-end tw:justify-between tw:gap-4">
      <div>
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300">{{ t('printers.groupLabel') }}</p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">{{ t('printers.title') }}</h1>
        <p class="tw:mt-2 tw:text-sm tw:text-muted">{{ t('printers.subtitle') }}</p>
      </div>
      <prime-button v-if="can('printer.create')" severity="success" size="small" @click="openCreate">
        <iconify icon="ph:plus-bold" />
        <span>{{ t('printers.addPrinter') }}</span>
      </prime-button>
    </div>

    <!-- ── Error ───────────────────────────────────────────────────── -->
    <prime-alert v-if="errorMessage" severity="error" variant="accent" closable @close="errorMessage = ''">
      {{ errorMessage }}
    </prime-alert>

    <!-- ── Loading ─────────────────────────────────────────────────── -->
    <div v-if="loading" class="tw:space-y-3">
      <prime-skeleton v-for="i in 2" :key="i" height="5rem" class="tw:rounded-xl" />
    </div>

    <!-- ── Empty ───────────────────────────────────────────────────── -->
    <div v-else-if="printers.length === 0"
      class="tw:rounded-2xl tw:border tw:border-dashed tw:border-slate-300 tw:dark:border-white/20 tw:p-12 tw:text-center tw:space-y-3">
      <iconify icon="ph:printer-bold" class="tw:text-4xl tw:text-muted tw:mx-auto" />
      <p class="tw:text-muted tw:text-sm">{{ t('printers.empty') }}</p>
      <prime-button v-if="can('printer.create')" severity="success" size="small" @click="openCreate">
        <iconify icon="ph:plus-bold" />
        <span>{{ t('printers.addPrinter') }}</span>
      </prime-button>
    </div>

    <!-- ── Grouped ─────────────────────────────────────────────────── -->
    <div v-else class="tw:space-y-8">
      <div v-for="group in grouped" :key="group.role" class="tw:space-y-3">
        <div class="tw:flex tw:items-center tw:gap-2">
          <iconify :icon="group.icon" class="tw:text-lg tw:text-muted" />
          <h2 class="tw:text-sm tw:font-semibold tw:uppercase tw:tracking-widest tw:text-muted">{{ group.label }}</h2>
        </div>

        <div class="tw:grid tw:grid-cols-1 tw:sm:grid-cols-2 tw:lg:grid-cols-3 tw:gap-3">
          <div v-for="p in group.items" :key="p.id"
            :class="['tw:rounded-xl tw:border tw:p-4 tw:space-y-3 tw:transition-all',
              p.isDefault
                ? 'tw:border-emerald-400 tw:dark:border-emerald-500/60 tw:bg-emerald-50 tw:dark:bg-emerald-500/5'
                : 'tw:border-slate-200 tw:dark:border-white/10 tw:bg-white tw:dark:bg-white/3',
              !p.isActive && 'tw:opacity-60']">

            <div class="tw:flex tw:items-start tw:justify-between tw:gap-2">
              <div class="tw:space-y-1 tw:min-w-0">
                <p class="tw:font-semibold tw:text-sm tw:truncate">{{ p.name }}</p>
                <div class="tw:flex tw:gap-1.5 tw:flex-wrap">
                  <prime-tag v-if="p.isDefault" :value="t('printers.badge.default')" severity="success" class="tw:text-[10px]! tw:px-1.5! tw:py-0.5!" />
                  <prime-tag v-if="!p.isActive" :value="t('printers.badge.inactive')" severity="danger" class="tw:text-[10px]! tw:px-1.5! tw:py-0.5!" />
                </div>
              </div>
              <div class="tw:flex tw:gap-1 tw:shrink-0">
                <prime-button :class="btnIcon" severity="secondary" outlined size="small"
                  v-tooltip.top="t('printers.actions.testTooltip')"
                  :loading="testingId === p.id" @click="handleTest(p)">
                  <iconify icon="ph:plugs-connected-bold" />
                </prime-button>
                <prime-button v-if="can('printer.update')" :class="btnIcon" severity="secondary" outlined size="small"
                  v-tooltip.top="t('printers.actions.editTooltip')" @click="openEdit(p)">
                  <iconify icon="ph:pencil-bold" />
                </prime-button>
                <prime-button v-if="can('printer.delete')" :class="btnIcon" severity="danger" outlined size="small"
                  v-tooltip.top="t('printers.actions.deleteTooltip')" @click="handleDelete(p)">
                  <iconify icon="ph:trash-bold" />
                </prime-button>
              </div>
            </div>

            <div class="tw:space-y-1 tw:text-xs tw:text-muted">
              <div class="tw:flex tw:items-center tw:gap-1.5">
                <iconify icon="ph:code-bold" class="tw:shrink-0" />
                <span>{{ p.formatterType }}</span>
                <span class="tw:opacity-40">·</span>
                <span>{{ p.paperWidthMm }}mm</span>
              </div>
              <div class="tw:flex tw:items-center tw:gap-1.5">
                <iconify icon="ph:usb-bold" class="tw:shrink-0" />
                <span>{{ t(`printers.transport.${p.transportType}`) }}</span>
              </div>
            </div>

            <prime-button v-if="!p.isDefault && can('printer.update')" severity="secondary" outlined size="small" fluid
              @click="handleSetDefault(p)">
              <iconify icon="ph:star-bold" />
              <span>{{ t('printers.actions.setDefault') }}</span>
            </prime-button>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>
