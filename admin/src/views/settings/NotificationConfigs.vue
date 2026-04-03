<script setup>
import {
  getNotificationConfigs,
  updateNotificationConfig,
  getNotificationSettings,
  updateNotificationSettings,
} from '@/services/notification.service'

const { t } = useI18n()
const toast = useToast()

const loading = ref(false)
const errorMessage = ref('')
const configs = ref([])

// Track dirty state per config id
const dirty = ref({})
const saving = ref({})

// Retention settings
const retentionDays = ref(30)
const retentionDirty = ref(false)
const retentionSaving = ref(false)

const AVAILABLE_ROLES = [
  { label: 'Admin', value: 'Admin' },
  { label: 'Staff', value: 'Staff' },
]

const TYPE_META = {
  ORDER_CREATED:         { icon: 'ph:receipt-bold',                labelKey: 'notificationConfigs.types.orderCreated' },
  ORDER_CANCELLED:       { icon: 'ph:x-circle-bold',               labelKey: 'notificationConfigs.types.orderCancelled' },
  ORDER_COMPLETED:       { icon: 'ph:check-circle-bold',           labelKey: 'notificationConfigs.types.orderCompleted' },
  PAYMENT_RECEIVED:      { icon: 'ph:currency-circle-dollar-bold', labelKey: 'notificationConfigs.types.paymentReceived' },
  MANUAL_ORDER_CREATED:  { icon: 'ph:pencil-bold',                 labelKey: 'notificationConfigs.types.manualOrderCreated' },
  LOW_STOCK:             { icon: 'ph:warning-bold',                labelKey: 'notificationConfigs.types.lowStock' },
  SYSTEM_ALERT:          { icon: 'ph:bell-bold',                   labelKey: 'notificationConfigs.types.systemAlert' },
}

function typeMeta(type) {
  return TYPE_META[type] ?? { icon: 'ph:bell-bold', labelKey: '' }
}

async function loadConfigs() {
  loading.value = true
  errorMessage.value = ''
  try {
    const [configsRes, settingsRes] = await Promise.all([
      getNotificationConfigs(),
      getNotificationSettings(),
    ])
    configs.value = (configsRes.data ?? []).map(c => ({ ...c, _roles: [...c.targetRoles] }))
    dirty.value = {}
    retentionDays.value = settingsRes.retentionDays ?? 30
    retentionDirty.value = false
  } catch {
    errorMessage.value = t('notificationConfigs.loadError')
  } finally {
    loading.value = false
  }
}

async function saveRetention() {
  retentionSaving.value = true
  try {
    const res = await updateNotificationSettings({ retentionDays: retentionDays.value })
    retentionDays.value = res.retentionDays
    retentionDirty.value = false
    toast.add({ severity: 'success', summary: t('notificationConfigs.retention.saved'), life: 2500 })
  } catch {
    toast.add({ severity: 'error', summary: t('notificationConfigs.retention.saveError'), life: 3000 })
  } finally {
    retentionSaving.value = false
  }
}

// Toggle enabled → auto-save immediately
async function toggleEnabled(cfg) {
  saving.value[cfg.id] = true
  try {
    await updateNotificationConfig(cfg.id, { targetRoles: cfg._roles, isEnabled: cfg.isEnabled })
    cfg.targetRoles = [...cfg._roles]
    toast.add({ severity: 'success', summary: t('notificationConfigs.saved'), life: 2500 })
  } catch {
    cfg.isEnabled = !cfg.isEnabled // revert
    toast.add({ severity: 'error', summary: t('notificationConfigs.saveError'), life: 3000 })
  } finally {
    saving.value[cfg.id] = false
  }
}

// Mark dirty when roles change
function onRolesChange(cfg) {
  const same = cfg._roles.length === cfg.targetRoles.length &&
    cfg._roles.every(r => cfg.targetRoles.includes(r))
  dirty.value[cfg.id] = !same
}

async function saveRoles(cfg) {
  saving.value[cfg.id] = true
  try {
    await updateNotificationConfig(cfg.id, { targetRoles: cfg._roles, isEnabled: cfg.isEnabled })
    cfg.targetRoles = [...cfg._roles]
    dirty.value[cfg.id] = false
    toast.add({ severity: 'success', summary: t('notificationConfigs.saved'), life: 2500 })
  } catch {
    toast.add({ severity: 'error', summary: t('notificationConfigs.saveError'), life: 3000 })
  } finally {
    saving.value[cfg.id] = false
  }
}

function discardRoles(cfg) {
  cfg._roles = [...cfg.targetRoles]
  dirty.value[cfg.id] = false
}

onMounted(loadConfigs)
</script>

<template>
  <section class="tw:space-y-8">
    <!-- Header -->
    <div>
      <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300">{{ t('notificationConfigs.breadcrumb') }}</p>
      <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">{{ t('notificationConfigs.title') }}</h1>
      <p class="tw:mt-2 tw:text-sm app-text-muted">{{ t('notificationConfigs.subtitle') }}</p>
    </div>

    <!-- Error -->
    <prime-alert v-if="errorMessage" severity="error" variant="accent" closable @close="errorMessage = ''">
      {{ errorMessage }}
    </prime-alert>

    <!-- Loading skeleton -->
    <div v-if="loading" class="tw:space-y-3">
      <prime-skeleton v-for="n in 7" :key="n" height="4rem" border-radius="0.75rem" />
    </div>

    <!-- Retention settings -->
    <div v-if="!loading" :class="appCard" class="tw:rounded-xl tw:p-5">
      <div class="tw:flex tw:items-center tw:gap-2 tw:mb-4">
        <div class="tw:flex tw:h-9 tw:w-9 tw:shrink-0 tw:items-center tw:justify-center tw:rounded-lg tw:bg-amber-500/15">
          <iconify icon="ph:trash-bold" class="tw:text-base tw:text-amber-400" />
        </div>
        <div>
          <p class="tw:text-sm tw:font-medium">{{ t('notificationConfigs.retention.title') }}</p>
          <p class="tw:text-[11px] app-text-muted">{{ t('notificationConfigs.retention.hint') }}</p>
        </div>
      </div>

      <div class="tw:flex tw:flex-wrap tw:items-end tw:gap-4">
        <div class="tw:flex-1 tw:min-w-48 tw:space-y-2">
          <label for="retention-days" class="tw:text-xs app-text-muted tw:uppercase tw:tracking-widest">
            {{ t('notificationConfigs.retention.label') }}
          </label>
          <div class="tw:flex tw:items-center tw:gap-3">
            <prime-input-number
              id="retention-days"
              v-model="retentionDays"
              :min="1"
              :max="365"
              :disabled="loading || retentionSaving"
              suffix=" ngày"
              class="tw:w-36"
              @input="retentionDirty = true"
            />
            <prime-slider
              v-model="retentionDays"
              :min="1"
              :max="365"
              :step="1"
              :disabled="loading || retentionSaving"
              class="tw:flex-1"
              @change="retentionDirty = true"
            />
          </div>
        </div>

        <prime-button
          v-if="retentionDirty"
          severity="success"
          size="small"
          :loading="retentionSaving"
          @click="saveRetention"
        >
          <iconify icon="ph:floppy-disk-bold" />
          <span>{{ t('notificationConfigs.retention.save') }}</span>
        </prime-button>
      </div>
    </div>

    <!-- Config list -->
    <div v-if="!loading && configs.length" class="tw:space-y-3">
      <div
        v-for="cfg in configs"
        :key="cfg.id"
        :class="appCard"
        class="tw:rounded-xl tw:p-4"
      >
        <div class="tw:flex tw:flex-wrap tw:items-center tw:gap-4">
          <!-- Icon + name -->
          <div class="tw:flex tw:items-center tw:gap-3 tw:min-w-52">
            <div
              class="tw:flex tw:h-9 tw:w-9 tw:shrink-0 tw:items-center tw:justify-center tw:rounded-lg"
              :class="cfg.isEnabled ? 'tw:bg-emerald-500/15' : 'tw:bg-white/5'"
            >
              <iconify
                :icon="typeMeta(cfg.type).icon"
                class="tw:text-base"
                :class="cfg.isEnabled ? 'tw:text-emerald-400' : 'app-text-muted'"
              />
            </div>
            <div>
              <p class="tw:text-sm tw:font-medium">{{ t(typeMeta(cfg.type).labelKey) }}</p>
              <p class="tw:text-[11px] app-text-muted tw:font-mono">{{ cfg.type }}</p>
            </div>
          </div>

          <!-- Roles multi-select -->
          <div class="tw:flex-1 tw:min-w-48">
            <prime-multi-select
              v-model="cfg._roles"
              :options="AVAILABLE_ROLES"
              option-label="label"
              option-value="value"
              :placeholder="t('notificationConfigs.noRoles')"
              :disabled="!cfg.isEnabled || saving[cfg.id]"
              display="chip"
              class="app-input tw:w-full"
              @change="onRolesChange(cfg)"
            />
          </div>

          <!-- Actions -->
          <div class="tw:flex tw:items-center tw:gap-2 tw:shrink-0">
            <!-- Dirty: save/discard buttons -->
            <template v-if="dirty[cfg.id]">
              <prime-button
                severity="success"
                size="small"
                :loading="saving[cfg.id]"
                @click="saveRoles(cfg)"
              >
                <iconify icon="ph:floppy-disk-bold" />
                <span>{{ t('common.save') }}</span>
              </prime-button>
              <prime-button
                severity="secondary"
                outlined
                size="small"
                :disabled="saving[cfg.id]"
                @click="discardRoles(cfg)"
              >
                <iconify icon="ph:x-bold" />
              </prime-button>
            </template>

            <!-- Enabled toggle -->
            <prime-toggle-switch
              v-model="cfg.isEnabled"
              :disabled="saving[cfg.id]"
              @change="toggleEnabled(cfg)"
            />
          </div>
        </div>

        <!-- Disabled hint -->
        <p v-if="!cfg.isEnabled" class="tw:mt-2 tw:text-xs app-text-muted tw:pl-12">
          {{ t('notificationConfigs.disabledHint') }}
        </p>
      </div>
    </div>
  </section>
</template>
