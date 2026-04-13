<script setup>
import QRCode from 'qrcode'
import { toPng } from 'html-to-image'

const { t } = useI18n()
const toast = useToast()

// ── Form state ─────────────────────────────────────────────────────
const qrContent   = ref('')
const qrColor     = ref('#1a5e38')
const line1       = ref('')
const line2       = ref('')
const line3       = ref('')
const logoDataUrl = ref(null)

// ── Customizer state ───────────────────────────────────────────────
const qrStyle      = ref('square')
const markerBorder = ref('square')
const markerCenter = ref('square')
const errorLevel   = ref('M')

// ── Preview refs ───────────────────────────────────────────────────
const qrCanvasRef    = ref(null)
const previewCardRef = ref(null)
const downloading    = ref(false)

// ── Helpers ────────────────────────────────────────────────────────
const rrect = (ctx, x, y, w, h, r) => {
  ctx.beginPath()
  ctx.moveTo(x + r, y)
  ctx.lineTo(x + w - r, y)
  ctx.quadraticCurveTo(x + w, y, x + w, y + r)
  ctx.lineTo(x + w, y + h - r)
  ctx.quadraticCurveTo(x + w, y + h, x + w - r, y + h)
  ctx.lineTo(x + r, y + h)
  ctx.quadraticCurveTo(x, y + h, x, y + h - r)
  ctx.lineTo(x, y + r)
  ctx.quadraticCurveTo(x, y, x + r, y)
  ctx.closePath()
}

// ── QR render ─────────────────────────────────────────────────────
const renderQr = () => {
  const canvas = qrCanvasRef.value
  if (!canvas) return
  const SIZE = 220, S = 2
  canvas.width  = SIZE * S
  canvas.height = SIZE * S
  canvas.style.width  = `${SIZE}px`
  canvas.style.height = `${SIZE}px`
  const ctx = canvas.getContext('2d')
  ctx.setTransform(1, 0, 0, 1, 0, 0)
  ctx.clearRect(0, 0, canvas.width, canvas.height)
  ctx.scale(S, S)

  if (!qrContent.value.trim()) return

  try {
    const errLevel = logoDataUrl.value ? 'H' : errorLevel.value
    const qrData = QRCode.create(qrContent.value.trim(), { errorCorrectionLevel: errLevel })
    const mods = qrData.modules
    const sz   = mods.size
    const cell = SIZE / sz

    // White bg
    ctx.fillStyle = '#ffffff'
    ctx.fillRect(0, 0, SIZE, SIZE)

    const isFinderZone = (r, c) =>
      (r < 7 && c < 7) || (r < 7 && c >= sz - 7) || (r >= sz - 7 && c < 7)

    const drawFinder = (startR, startC) => {
      const fx = startC * cell, fy = startR * cell
      // Outer border
      ctx.fillStyle = qrColor.value
      if (markerBorder.value === 'circle') {
        ctx.beginPath(); ctx.arc(fx + 3.5*cell, fy + 3.5*cell, 3.5*cell, 0, Math.PI*2); ctx.fill()
      } else if (markerBorder.value === 'rounded') {
        rrect(ctx, fx, fy, 7*cell, 7*cell, cell*1.1); ctx.fill()
      } else {
        ctx.fillRect(fx, fy, 7*cell, 7*cell)
      }
      // White inner
      ctx.fillStyle = '#ffffff'
      if (markerBorder.value === 'circle') {
        ctx.beginPath(); ctx.arc(fx + 3.5*cell, fy + 3.5*cell, 2.5*cell, 0, Math.PI*2); ctx.fill()
      } else if (markerBorder.value === 'rounded') {
        rrect(ctx, fx + cell, fy + cell, 5*cell, 5*cell, cell*0.65); ctx.fill()
      } else {
        ctx.fillRect(fx + cell, fy + cell, 5*cell, 5*cell)
      }
      // Center
      ctx.fillStyle = qrColor.value
      if (markerCenter.value === 'dot') {
        ctx.beginPath(); ctx.arc(fx + 3.5*cell, fy + 3.5*cell, 1.5*cell, 0, Math.PI*2); ctx.fill()
      } else if (markerCenter.value === 'rounded') {
        rrect(ctx, fx + 2*cell, fy + 2*cell, 3*cell, 3*cell, cell*0.5); ctx.fill()
      } else {
        ctx.fillRect(fx + 2*cell, fy + 2*cell, 3*cell, 3*cell)
      }
    }

    drawFinder(0, 0)
    drawFinder(0, sz - 7)
    drawFinder(sz - 7, 0)

    // Data modules
    ctx.fillStyle = qrColor.value
    for (let r = 0; r < sz; r++) {
      for (let c = 0; c < sz; c++) {
        if (isFinderZone(r, c)) continue
        if (!mods.data[r * sz + c]) continue
        const mx = c * cell, my = r * cell
        if (qrStyle.value === 'dots') {
          ctx.beginPath(); ctx.arc(mx + cell/2, my + cell/2, cell*0.42, 0, Math.PI*2); ctx.fill()
        } else if (qrStyle.value === 'rounded') {
          rrect(ctx, mx + cell*0.08, my + cell*0.08, cell*0.84, cell*0.84, cell*0.28); ctx.fill()
        } else {
          ctx.fillRect(mx, my, cell, cell)
        }
      }
    }
  } catch {
    // ignore
  }
}

watch([qrContent, qrColor, qrStyle, markerBorder, markerCenter, errorLevel], () => renderQr())
watch(logoDataUrl, () => renderQr())
onMounted(() => renderQr())

// ── Download ───────────────────────────────────────────────────────
const downloadPng = async () => {
  if (!qrContent.value.trim()) {
    toast.add({ severity: 'warn', summary: t('utilities.paymentQr.emptyContent'), life: 3000 })
    return
  }
  downloading.value = true
  try {
    const dataUrl = await toPng(previewCardRef.value, { pixelRatio: 3 })
    const link = document.createElement('a')
    link.download = `payment-qr.png`
    link.href = dataUrl
    link.click()
  } finally {
    downloading.value = false
  }
}

const hasPreview = computed(() => !!qrContent.value.trim())
</script>

<template>
  <section class="tw:space-y-8">
    <!-- Header -->
    <div>
      <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300">{{ t('nav.groups.utilities') }}</p>
      <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">{{ t('utilities.paymentQr.title') }}</h1>
      <p class="tw:mt-2 tw:text-sm app-text-muted">{{ t('utilities.paymentQr.subtitle') }}</p>
    </div>

    <div class="tw:grid tw:grid-cols-1 tw:lg:grid-cols-2 tw:gap-8 tw:items-start">

      <!-- ── Left: Form ─────────────────────────────────────────── -->
      <div :class="appCard" class="tw:rounded-2xl tw:p-6 tw:space-y-5">

        <!-- QR content -->
        <div class="tw:space-y-1.5">
          <label for="qr-content" class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted">
            {{ t('utilities.paymentQr.content') }}
          </label>
          <prime-textarea
            id="qr-content"
            v-model="qrContent"
            :placeholder="t('utilities.paymentQr.contentPlaceholder')"
            class="app-input tw:w-full tw:font-mono tw:text-sm"
            :rows="3"
            auto-resize
          />
          <p class="tw:text-[11px] app-text-subtle">{{ t('utilities.paymentQr.contentHint') }}</p>
        </div>

        <prime-divider />

        <!-- Text lines -->
        <p class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted tw:-mb-1">{{ t('utilities.paymentQr.textLines') }}</p>

        <div class="tw:space-y-1.5">
          <label for="line1" class="tw:text-xs app-text-muted">{{ t('utilities.paymentQr.line1') }}</label>
          <prime-input-text
            id="line1"
            v-model="line1"
            :placeholder="t('utilities.paymentQr.line1Placeholder')"
            class="app-input tw:w-full tw:font-semibold"
          />
        </div>

        <div class="tw:space-y-1.5">
          <label for="line2" class="tw:text-xs app-text-muted">{{ t('utilities.paymentQr.line2') }}</label>
          <prime-input-text
            id="line2"
            v-model="line2"
            :placeholder="t('utilities.paymentQr.line2Placeholder')"
            class="app-input tw:w-full tw:font-mono"
          />
        </div>

        <div class="tw:space-y-1.5">
          <label for="line3" class="tw:text-xs app-text-muted">{{ t('utilities.paymentQr.line3') }}</label>
          <prime-input-text
            id="line3"
            v-model="line3"
            :placeholder="t('utilities.paymentQr.line3Placeholder')"
            class="app-input tw:w-full"
          />
        </div>

        <div class="tw:flex tw:gap-2">
          <PaymentQrCustomizer
            v-model:color="qrColor"
            v-model:style="qrStyle"
            v-model:marker-border="markerBorder"
            v-model:marker-center="markerCenter"
            v-model:error-level="errorLevel"
            v-model:logo="logoDataUrl"
          />
          <prime-button
            severity="primary"
            class="tw:flex-1"
            :loading="downloading"
            :disabled="!hasPreview"
            @click="downloadPng"
            size="small"
          >
            <iconify icon="ph:download-bold" />
            <span>{{ t('utilities.paymentQr.downloadPng') }}</span>
          </prime-button>
        </div>
      </div>

      <!-- ── Right: Preview ─────────────────────────────────────── -->
      <div class="tw:flex tw:flex-col tw:items-center tw:gap-4">
        <p class="tw:text-xs tw:uppercase tw:tracking-widest app-text-muted tw:self-start">
          {{ t('utilities.paymentQr.preview') }}
        </p>

        <!-- Preview card (this gets captured for download) -->
        <div
          ref="previewCardRef"
          class="tw:bg-white tw:rounded-3xl tw:shadow-xl tw:p-6 tw:flex tw:flex-col tw:items-center tw:gap-0 tw:w-80"
        >
          <!-- QR + logo overlay + VietQR/Napas inside border -->
          <div class="tw:rounded-2xl tw:border tw:border-slate-200 tw:overflow-hidden tw:flex tw:flex-col tw:p-3">
            <div class="tw:relative tw:inline-flex">
              <canvas
                ref="qrCanvasRef"
                class="tw:block"
                :class="hasPreview ? '' : 'tw:opacity-20'"
              />
              <!-- Logo center overlay -->
              <div
                v-if="logoDataUrl && hasPreview"
                class="tw:absolute tw:inset-0 tw:flex tw:items-center tw:justify-center tw:pointer-events-none"
              >
                <div class="tw:bg-white tw:rounded-xl tw:p-1.5 tw:shadow-sm">
                  <img :src="logoDataUrl" class="tw:w-14 tw:h-14 tw:rounded-lg tw:object-cover" />
                </div>
              </div>
              <!-- Placeholder when empty -->
              <div
                v-if="!hasPreview"
                class="tw:absolute tw:inset-0 tw:flex tw:flex-col tw:items-center tw:justify-center tw:gap-2"
              >
                <iconify icon="ph:qr-code-bold" class="tw:text-3xl tw:text-slate-400" />
                <p class="tw:text-xs tw:text-slate-400 tw:text-center tw:px-4">{{ t('utilities.paymentQr.emptyPreview') }}</p>
              </div>
            </div>
            <!-- VietQR + Napas logos inside border -->
            <div class="tw:flex tw:items-center tw:justify-between tw:px-3 tw:py-2 tw:bg-white">
              <img src="/vietqr.svg" alt="VietQR" class="tw:h-6 tw:object-contain" />
              <img src="/napas.png" alt="Napas" class="tw:h-6 tw:object-contain" />
            </div>
          </div>

          <!-- Text lines (only when filled) -->
          <template v-if="line1 || line2 || line3">
            <div class="tw:w-full tw:pt-4 tw:text-center tw:space-y-0.5">
              <p v-if="line1" class="tw:text-sm tw:font-bold tw:uppercase tw:tracking-wide tw:text-slate-800">{{ line1 }}</p>
              <p v-if="line2" class="tw:text-xl tw:font-bold tw:text-slate-900 tw:tracking-widest">{{ line2 }}</p>
              <p v-if="line3" class="tw:text-sm tw:text-slate-500 tw:mt-1">{{ line3 }}</p>
            </div>
          </template>
        </div>

        <p v-if="!hasPreview" class="tw:text-xs app-text-subtle tw:text-center">
          {{ t('utilities.paymentQr.emptyPreview') }}
        </p>
      </div>

    </div>
  </section>
</template>
