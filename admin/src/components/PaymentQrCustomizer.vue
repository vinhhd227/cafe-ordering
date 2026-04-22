<script setup>
const { t } = useI18n()

const color         = defineModel('color',        { default: '#1a5e38' })
const style         = defineModel('style',        { default: 'square' })
const markerBorder  = defineModel('markerBorder', { default: 'square' })
const markerCenter  = defineModel('markerCenter', { default: 'square' })
const errorLevel    = defineModel('errorLevel',   { default: 'M' })
const logo          = defineModel('logo',         { default: null })

const qrStyleOptions = [
  { value: 'square',  label: '■' },
  { value: 'rounded', label: '▣' },
  { value: 'dots',    label: '●' },
]
const markerBorderOptions = [
  { value: 'square',  label: '□' },
  { value: 'rounded', label: '▢' },
  { value: 'circle',  label: '○' },
]
const markerCenterOptions = [
  { value: 'square',  label: '■' },
  { value: 'rounded', label: '▣' },
  { value: 'dot',     label: '●' },
]
const errorLevelOptions = ['L', 'M', 'Q', 'H']

const customizerRef = ref(null)
const logoInputRef  = ref(null)

const handleLogoUpload = (e) => {
  const file = e.target.files?.[0]
  if (!file) return
  const reader = new FileReader()
  reader.onload = (ev) => { logo.value = ev.target.result }
  reader.readAsDataURL(file)
}

const removeLogo = () => {
  logo.value = null
  if (logoInputRef.value) logoInputRef.value.value = ''
}
</script>

<template>
  <!-- Trigger button -->
  <prime-button
    severity="secondary"
    outlined
    size="small"
    :class="btnIcon"
    v-tooltip.top="t('tables.qr.customizeTooltip')"
    @click="customizerRef.toggle($event)"
  >
    <iconify icon="ph:sliders-bold" />
  </prime-button>

  <!-- Popover: all customization options -->
  <prime-popover ref="customizerRef">
    <div class="tw:flex tw:flex-col tw:gap-4" style="min-width: 230px">

      <!-- Color -->
      <div class="tw:flex tw:items-center tw:gap-2">
        <span class="tw:text-xs tw:text-muted tw:w-28 tw:shrink-0">{{ t('utilities.paymentQr.qrColor') }}</span>
        <div class="tw:relative">
          <div
            class="tw:w-6 tw:h-6 tw:rounded tw:cursor-pointer tw:border tw:border-slate-200 tw:dark:border-white/15"
            :style="{ backgroundColor: color }"
            @click="$refs.colorPickerRef.click()"
          />
          <input
            ref="colorPickerRef"
            type="color"
            v-model="color"
            class="tw:absolute tw:inset-0 tw:opacity-0 tw:w-full tw:h-full tw:cursor-pointer"
          />
        </div>
        <input
          type="text"
          :value="color"
          @change="e => { if (/^#[0-9a-fA-F]{6}$/.test(e.target.value)) color = e.target.value }"
          class="app-input tw:w-24 tw:font-mono tw:text-sm tw:px-2 tw:py-1"
          maxlength="7"
          spellcheck="false"
        />
        <button
          class="tw:text-xs tw:text-muted tw:underline tw:cursor-pointer tw:bg-transparent tw:border-0 tw:p-0"
          @click="color = '#1a5e38'"
        >{{ t('utilities.paymentQr.resetColor') }}</button>
      </div>

      <prime-divider class="tw:my-0!" />

      <!-- Pattern -->
      <div class="tw:flex tw:items-center tw:gap-2">
        <span class="tw:text-xs tw:text-muted tw:w-28 tw:shrink-0">{{ t('tables.qr.customizer.pattern') }}</span>
        <div class="tw:flex tw:gap-1">
          <button
            v-for="opt in qrStyleOptions" :key="opt.value"
            @click="style = opt.value"
            :class="['tw:text-sm tw:w-8 tw:h-7 tw:rounded tw:border tw:cursor-pointer tw:transition-colors',
              style === opt.value ? 'tw:bg-primary-500 tw:text-white tw:border-primary-500' : 'tw:text-muted tw:border-surface-400 tw:bg-transparent']"
          >{{ opt.label }}</button>
        </div>
      </div>

      <!-- Marker border -->
      <div class="tw:flex tw:items-center tw:gap-2">
        <span class="tw:text-xs tw:text-muted tw:w-28 tw:shrink-0">{{ t('tables.qr.customizer.markerBorder') }}</span>
        <div class="tw:flex tw:gap-1">
          <button
            v-for="opt in markerBorderOptions" :key="opt.value"
            @click="markerBorder = opt.value"
            :class="['tw:text-sm tw:w-8 tw:h-7 tw:rounded tw:border tw:cursor-pointer tw:transition-colors',
              markerBorder === opt.value ? 'tw:bg-primary-500 tw:text-white tw:border-primary-500' : 'tw:text-muted tw:border-surface-400 tw:bg-transparent']"
          >{{ opt.label }}</button>
        </div>
      </div>

      <!-- Marker center -->
      <div class="tw:flex tw:items-center tw:gap-2">
        <span class="tw:text-xs tw:text-muted tw:w-28 tw:shrink-0">{{ t('tables.qr.customizer.markerCenter') }}</span>
        <div class="tw:flex tw:gap-1">
          <button
            v-for="opt in markerCenterOptions" :key="opt.value"
            @click="markerCenter = opt.value"
            :class="['tw:text-sm tw:w-8 tw:h-7 tw:rounded tw:border tw:cursor-pointer tw:transition-colors',
              markerCenter === opt.value ? 'tw:bg-primary-500 tw:text-white tw:border-primary-500' : 'tw:text-muted tw:border-surface-400 tw:bg-transparent']"
          >{{ opt.label }}</button>
        </div>
      </div>

      <!-- Precision -->
      <div class="tw:flex tw:items-center tw:gap-2">
        <span class="tw:text-xs tw:text-muted tw:w-28 tw:shrink-0">{{ t('tables.qr.customizer.precision') }}</span>
        <div class="tw:flex tw:gap-1">
          <button
            v-for="lvl in errorLevelOptions" :key="lvl"
            @click="errorLevel = lvl"
            :class="['tw:text-xs tw:font-mono tw:w-8 tw:h-7 tw:rounded tw:border tw:cursor-pointer tw:transition-colors',
              errorLevel === lvl ? 'tw:bg-primary-500 tw:text-white tw:border-primary-500' : 'tw:text-muted tw:border-surface-400 tw:bg-transparent']"
          >{{ lvl }}</button>
        </div>
      </div>

      <prime-divider class="tw:my-0!" />

      <!-- Logo -->
      <div class="tw:flex tw:items-center tw:gap-2">
        <span class="tw:text-xs tw:text-muted tw:w-28 tw:shrink-0">{{ t('utilities.paymentQr.centerLogo') }}</span>
        <div v-if="!logo" class="tw:flex tw:items-center tw:gap-2">
          <prime-button severity="secondary" outlined size="small" @click="logoInputRef.click()">
            <iconify icon="ph:upload-simple-bold" />
            <span>{{ t('utilities.paymentQr.uploadLogo') }}</span>
          </prime-button>
          <input ref="logoInputRef" type="file" accept="image/*" class="tw:hidden" @change="handleLogoUpload" />
        </div>
        <div v-else class="tw:flex tw:items-center tw:gap-2">
          <img :src="logo" class="tw:w-8 tw:h-8 tw:rounded tw:object-cover tw:border tw:border-slate-200 tw:dark:border-white/15" />
          <prime-button severity="danger" text size="small" @click="removeLogo">
            <iconify icon="ph:x-bold" />
            <span>{{ t('utilities.paymentQr.removeLogo') }}</span>
          </prime-button>
        </div>
      </div>

    </div>
  </prime-popover>
</template>
