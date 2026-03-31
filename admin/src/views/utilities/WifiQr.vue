<script setup>
import QRCode from 'qrcode'
import { toPng } from 'html-to-image'
import { listWifiProfiles, createWifiProfile, deleteWifiProfile } from '@/services/wifiProfile.service'

const { t } = useI18n()
const toast = useToast()
const confirm = useConfirm()

// Form state
const ssid = ref('')
const password = ref('')
const security = ref('WPA')
const showPassword = ref(false)
const foregroundColor = ref('#000000')
const backgroundColor = ref('#ffffff')
const customText = ref('')
const logoFile = ref(null)
const logoDataUrl = ref(null)
const logoInputRef = ref(null)

// Preview refs
const qrCanvasRef = ref(null)
const previewCardRef = ref(null)
const downloading = ref(false)

// Saved profiles
const profiles = ref([])
const loadingProfiles = ref(false)
const savingProfile = ref(false)
const saveDialogVisible = ref(false)
const profileName = ref('')

const securityOptions = computed(() => [
  { label: t('utilities.wifiQr.securityWpa'), value: 'WPA' },
  { label: t('utilities.wifiQr.securityWep'), value: 'WEP' },
  { label: t('utilities.wifiQr.securityNone'), value: 'nopass' },
])

const wifiString = computed(() => {
  const s = ssid.value.trim()
  if (!s) return ''
  const p = security.value === 'nopass' ? '' : password.value
  return `WIFI:T:${security.value};S:${s};P:${p};H:false;;`
})

const renderQr = async () => {
  if (!qrCanvasRef.value || !wifiString.value) return
  try {
    await QRCode.toCanvas(qrCanvasRef.value, wifiString.value, {
      width: 256,
      margin: 2,
      color: {
        dark: foregroundColor.value,
        light: backgroundColor.value,
      },
      errorCorrectionLevel: logoDataUrl.value ? 'H' : 'M',
    })
  } catch {
    // ignore if string empty
  }
}

watch([wifiString, foregroundColor, backgroundColor], () => {
  renderQr()
})

onMounted(() => {
  renderQr()
  loadProfiles()
})

const loadProfiles = async () => {
  loadingProfiles.value = true
  try {
    const res = await listWifiProfiles()
    profiles.value = res.data ?? []
  } catch {
    // silent — profiles are optional
  } finally {
    loadingProfiles.value = false
  }
}

const applyProfile = (profile) => {
  ssid.value = profile.ssid
  password.value = profile.password
  security.value = profile.securityType
  foregroundColor.value = profile.foregroundColor
  backgroundColor.value = profile.backgroundColor
  customText.value = profile.customText ?? ''
}

const openSaveDialog = () => {
  profileName.value = ssid.value.trim() ? ssid.value.trim() : ''
  saveDialogVisible.value = true
}

const saveProfile = async () => {
  if (!profileName.value.trim()) return
  if (!ssid.value.trim()) {
    toast.add({ severity: 'warn', summary: t('utilities.wifiQr.emptySsid'), life: 3000 })
    return
  }
  savingProfile.value = true
  try {
    const res = await createWifiProfile({
      name: profileName.value.trim(),
      ssid: ssid.value.trim(),
      password: security.value === 'nopass' ? '' : password.value,
      securityType: security.value,
      foregroundColor: foregroundColor.value,
      backgroundColor: backgroundColor.value,
      customText: customText.value || null,
    })
    profiles.value.push(res.data)
    profiles.value.sort((a, b) => a.name.localeCompare(b.name))
    saveDialogVisible.value = false
    toast.add({ severity: 'success', summary: 'Đã lưu cấu hình', life: 2500 })
  } catch {
    toast.add({ severity: 'error', summary: 'Không thể lưu cấu hình', life: 3000 })
  } finally {
    savingProfile.value = false
  }
}

const confirmDelete = (profile) => {
  confirm.require({
    message: `Xóa cấu hình "${profile.name}"?`,
    header: 'Xác nhận xóa',
    icon: 'ph:trash-bold',
    acceptClass: 'p-button-danger',
    accept: () => doDelete(profile.id),
  })
}

const doDelete = async (id) => {
  try {
    await deleteWifiProfile(id)
    profiles.value = profiles.value.filter(p => p.id !== id)
    toast.add({ severity: 'success', summary: 'Đã xóa cấu hình', life: 2500 })
  } catch {
    toast.add({ severity: 'error', summary: 'Không thể xóa cấu hình', life: 3000 })
  }
}

const handleLogoUpload = (e) => {
  const file = e.target.files?.[0]
  if (!file) return
  const reader = new FileReader()
  reader.onload = (ev) => {
    logoDataUrl.value = ev.target.result
    renderQr()
  }
  reader.readAsDataURL(file)
}

const removeLogo = () => {
  logoDataUrl.value = null
  logoFile.value = null
  if (logoInputRef.value) logoInputRef.value.value = ''
  renderQr()
}

const downloadPng = async () => {
  if (!ssid.value.trim()) {
    toast.add({ severity: 'warn', summary: t('utilities.wifiQr.emptySsid'), life: 3000 })
    return
  }
  downloading.value = true
  try {
    const dataUrl = await toPng(previewCardRef.value, { pixelRatio: 2 })
    const link = document.createElement('a')
    link.download = `wifi-qr-${ssid.value.trim()}.png`
    link.href = dataUrl
    link.click()
  } finally {
    downloading.value = false
  }
}
</script>

<template>
  <section class="tw:space-y-8">
    <!-- Header -->
    <div>
      <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300">{{ t('nav.groups.utilities') }}</p>
      <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">{{ t('utilities.wifiQr.title') }}</h1>
      <p class="tw:mt-2 tw:text-sm app-text-muted">{{ t('utilities.wifiQr.subtitle') }}</p>
    </div>

    <div class="tw:grid tw:grid-cols-1 tw:lg:grid-cols-2 tw:gap-8 tw:items-start">
      <!-- Left: Form + Saved profiles -->
      <div class="tw:space-y-6">
        <!-- Form -->
        <div :class="appCard" class="tw:rounded-2xl tw:p-6 tw:space-y-5">
          <!-- SSID -->
          <div class="tw:space-y-1.5">
            <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">{{ t('utilities.wifiQr.ssid') }}</label>
            <prime-input-text
              v-model="ssid"
              :placeholder="t('utilities.wifiQr.ssidPlaceholder')"
              class="app-input tw:w-full"
            />
          </div>

          <!-- Security type -->
          <div class="tw:space-y-1.5">
            <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">{{ t('utilities.wifiQr.security') }}</label>
            <prime-select
              v-model="security"
              :options="securityOptions"
              option-label="label"
              option-value="value"
              class="app-input tw:w-full"
            />
          </div>

          <!-- Password -->
          <div v-if="security !== 'nopass'" class="tw:space-y-1.5">
            <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">{{ t('utilities.wifiQr.password') }}</label>
            <prime-input-group>
              <prime-input-text
                v-model="password"
                :type="showPassword ? 'text' : 'password'"
                :placeholder="t('utilities.wifiQr.passwordPlaceholder')"
                class="app-input tw:w-full"
              />
              <prime-button severity="secondary" @click="showPassword = !showPassword">
                <iconify :icon="showPassword ? 'ph:eye-slash-bold' : 'ph:eye-bold'" />
              </prime-button>
            </prime-input-group>
          </div>

          <!-- Colors -->
          <div class="tw:grid tw:grid-cols-2 tw:gap-4">
            <div class="tw:space-y-1.5">
              <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">{{ t('utilities.wifiQr.foregroundColor') }}</label>
              <div class="tw:flex tw:items-center tw:gap-2">
                <input type="color" v-model="foregroundColor" class="tw:w-10 tw:h-10 tw:rounded-lg tw:border tw:border-slate-300 tw:dark:border-white/20 tw:cursor-pointer tw:bg-transparent" />
                <span class="tw:text-sm tw:font-mono app-text-muted">{{ foregroundColor }}</span>
              </div>
            </div>
            <div class="tw:space-y-1.5">
              <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">{{ t('utilities.wifiQr.backgroundColor') }}</label>
              <div class="tw:flex tw:items-center tw:gap-2">
                <input type="color" v-model="backgroundColor" class="tw:w-10 tw:h-10 tw:rounded-lg tw:border tw:border-slate-300 tw:dark:border-white/20 tw:cursor-pointer tw:bg-transparent" />
                <span class="tw:text-sm tw:font-mono app-text-muted">{{ backgroundColor }}</span>
              </div>
            </div>
          </div>

          <!-- Custom text -->
          <div class="tw:space-y-1.5">
            <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">{{ t('utilities.wifiQr.customText') }}</label>
            <prime-input-text
              v-model="customText"
              :placeholder="t('utilities.wifiQr.customTextPlaceholder')"
              class="app-input tw:w-full"
            />
          </div>

          <!-- Logo upload -->
          <div class="tw:space-y-1.5">
            <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">{{ t('utilities.wifiQr.logoUpload') }}</label>
            <div class="tw:flex tw:items-center tw:gap-3">
              <prime-button severity="secondary" outlined size="small" @click="logoInputRef.click()">
                <iconify icon="ph:upload-simple-bold" />
                <span>{{ t('utilities.wifiQr.logoUpload') }}</span>
              </prime-button>
              <prime-button v-if="logoDataUrl" :class="btnIcon" severity="danger" outlined @click="removeLogo" v-tooltip.top="'Xóa logo'">
                <iconify icon="ph:x-bold" />
              </prime-button>
              <span v-if="!logoDataUrl" class="tw:text-xs app-text-muted">{{ t('utilities.wifiQr.logoHint') }}</span>
            </div>
            <input ref="logoInputRef" type="file" accept="image/*" class="tw:hidden" @change="handleLogoUpload" />
          </div>

          <!-- Actions -->
          <div class="tw:flex tw:gap-3 tw:pt-2">
            <prime-button
              severity="secondary"
              outlined
              class="tw:flex-1"
              :disabled="!ssid.trim()"
              @click="openSaveDialog"
            >
              <iconify icon="ph:floppy-disk-bold" />
              <span>Lưu cấu hình</span>
            </prime-button>
            <prime-button
              severity="success"
              class="tw:flex-1"
              :disabled="!ssid.trim() || downloading"
              @click="downloadPng"
            >
              <iconify :icon="downloading ? 'ph:spinner-bold' : 'ph:download-simple-bold'" :class="{ 'tw:animate-spin': downloading }" />
              <span>{{ t('utilities.wifiQr.downloadPng') }}</span>
            </prime-button>
          </div>
        </div>

        <!-- Saved profiles -->
        <div :class="appCard" class="tw:rounded-2xl tw:p-5">
          <div class="tw:flex tw:items-center tw:justify-between tw:mb-4">
            <p class="tw:text-sm tw:font-semibold">Cấu hình đã lưu</p>
            <iconify v-if="loadingProfiles" icon="ph:spinner-bold" class="tw:animate-spin app-text-muted" />
          </div>

          <div v-if="!loadingProfiles && profiles.length === 0" class="tw:text-sm app-text-muted tw:text-center tw:py-6">
            Chưa có cấu hình nào được lưu
          </div>

          <div v-else class="tw:space-y-2">
            <div
              v-for="profile in profiles"
              :key="profile.id"
              class="tw:flex tw:items-center tw:justify-between tw:gap-3 tw:p-3 tw:rounded-xl tw:border tw:border-slate-200 tw:dark:border-white/10 tw:hover:bg-slate-50 tw:dark:hover:bg-white/5 tw:transition-colors"
            >
              <div class="tw:flex tw:items-center tw:gap-3 tw:min-w-0">
                <!-- Color preview -->
                <div
                  class="tw:w-8 tw:h-8 tw:rounded-lg tw:flex-shrink-0 tw:border tw:border-white/20"
                  :style="{ backgroundColor: profile.foregroundColor }"
                />
                <div class="tw:min-w-0">
                  <p class="tw:text-sm tw:font-medium tw:truncate">{{ profile.name }}</p>
                  <p class="tw:text-xs app-text-muted tw:truncate">{{ profile.ssid }} · {{ profile.securityType }}</p>
                </div>
              </div>
              <div class="tw:flex tw:gap-1.5 tw:flex-shrink-0">
                <prime-button
                  :class="btnIcon"
                  severity="secondary"
                  outlined
                  v-tooltip.top="'Tải cấu hình này'"
                  @click="applyProfile(profile)"
                >
                  <iconify icon="ph:arrow-square-in-bold" />
                </prime-button>
                <prime-button
                  :class="btnIcon"
                  severity="danger"
                  outlined
                  v-tooltip.top="'Xóa'"
                  @click="confirmDelete(profile)"
                >
                  <iconify icon="ph:trash-bold" />
                </prime-button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Right: Preview -->
      <div class="tw:flex tw:flex-col tw:items-center tw:gap-6">
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] app-text-muted tw:self-start">{{ t('utilities.wifiQr.preview') }}</p>

        <!-- QR card (this is what gets captured) -->
        <div
          ref="previewCardRef"
          class="tw:rounded-2xl tw:p-8 tw:flex tw:flex-col tw:items-center tw:gap-4 tw:shadow-xl"
          :style="{ backgroundColor: backgroundColor }"
        >
          <!-- Logo overlay -->
          <div class="tw:relative tw:inline-block">
            <canvas ref="qrCanvasRef" class="tw:rounded-xl tw:block" />
            <img
              v-if="logoDataUrl"
              :src="logoDataUrl"
              class="tw:absolute tw:inset-0 tw:m-auto tw:w-12 tw:h-12 tw:object-contain tw:rounded-lg tw:bg-white tw:p-1 tw:shadow"
              style="top: 50%; left: 50%; transform: translate(-50%, -50%)"
            />
          </div>

          <!-- Custom text below QR -->
          <p
            v-if="customText"
            class="tw:text-sm tw:font-medium tw:text-center tw:max-w-[260px]"
            :style="{ color: foregroundColor }"
          >
            {{ customText }}
          </p>

          <!-- Wifi name hint -->
          <div v-if="ssid" class="tw:flex tw:items-center tw:gap-1.5">
            <iconify icon="ph:wifi-high-bold" class="tw:text-base" :style="{ color: foregroundColor }" />
            <span class="tw:text-sm tw:font-semibold" :style="{ color: foregroundColor }">{{ ssid }}</span>
          </div>
          <p v-else class="tw:text-sm app-text-muted tw:italic">Nhập tên wifi để xem QR</p>
        </div>
      </div>
    </div>

    <!-- Save profile dialog -->
    <prime-dialog
      v-model:visible="saveDialogVisible"
      header="Lưu cấu hình WiFi"
      :style="{ width: '380px' }"
      :modal="true"
    >
      <div class="tw:space-y-4 tw:py-2">
        <div class="tw:space-y-1.5">
          <label class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">Tên cấu hình</label>
          <prime-input-text
            v-model="profileName"
            placeholder="VD: WiFi Tầng 1"
            class="app-input tw:w-full"
            autofocus
            @keyup.enter="saveProfile"
          />
        </div>
        <div class="tw:text-xs app-text-muted">
          <span class="tw:font-medium">SSID:</span> {{ ssid }} ·
          <span class="tw:font-medium">Loại:</span> {{ security }}
        </div>
      </div>
      <template #footer>
        <prime-button severity="secondary" outlined @click="saveDialogVisible = false">Hủy</prime-button>
        <prime-button
          severity="success"
          :loading="savingProfile"
          :disabled="!profileName.trim()"
          @click="saveProfile"
        >
          <iconify icon="ph:floppy-disk-bold" />
          <span>Lưu</span>
        </prime-button>
      </template>
    </prime-dialog>

    <prime-confirm-dialog />
  </section>
</template>
