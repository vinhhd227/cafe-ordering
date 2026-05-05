<script setup>
const props = defineProps({
  visible: Boolean,
  product: Object,
})

const emit = defineEmits(['update:visible', 'confirm'])

const { t } = useI18n()

const pendingQuantity = ref(1)
const pendingOptions = ref({
  temperature: null,
  iceLevel: null,
  sugarLevel: null,
  isTakeaway: false,
})

watch(
  () => props.product,
  (product) => {
    if (!product) return
    pendingQuantity.value = 1
    pendingOptions.value = {
      temperature: product.hasTemperatureOption ? DRINK_TEMPERATURE.COLD : null,
      iceLevel: product.hasIceLevelOption ? ICE_LEVEL.NORMAL : null,
      sugarLevel: product.hasSugarLevelOption ? SUGAR_LEVEL.NORMAL : null,
      isTakeaway: false,
    }
  },
  { immediate: true },
)

const temperatureOptions = computed(() =>
  DRINK_TEMPERATURE_OPTIONS.map((opt) => ({
    ...opt,
    label: t(`orders.temperature.${opt.value}`),
  })),
)

const iceLevelOptions = computed(() =>
  ICE_LEVEL_OPTIONS.map((opt) => ({
    ...opt,
    label: t(`orders.iceLevel.${opt.value}`),
  })),
)

const sugarLevelOptions = computed(() =>
  SUGAR_LEVEL_OPTIONS.map((opt) => ({
    ...opt,
    label: t(`orders.sugarLevel.${opt.value}`),
  })),
)

const servingOptions = computed(() => [
  { ...SERVING_TYPE_OPTIONS[0], label: t('orders.serving.dineIn') },
  { ...SERVING_TYPE_OPTIONS[1], label: t('orders.serving.takeaway') },
])

const formatVnd = (val) =>
  new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(val ?? 0)

const makeCartKey = (productId, opts) => {
  const { temperature = '', iceLevel = '', sugarLevel = '', isTakeaway = false } = opts ?? {}
  return `${productId}|${temperature}|${iceLevel}|${sugarLevel}|${isTakeaway}`
}

const setTemperature = (opt) => {
  pendingOptions.value.temperature = opt
  if (opt === DRINK_TEMPERATURE.HOT) {
    if (props.product?.hasIceLevelOption) pendingOptions.value.iceLevel = ICE_LEVEL.LESS
    if (props.product?.hasSugarLevelOption) pendingOptions.value.sugarLevel = SUGAR_LEVEL.NORMAL
  } else if (opt === DRINK_TEMPERATURE.COLD) {
    if (props.product?.hasIceLevelOption && pendingOptions.value.iceLevel === ICE_LEVEL.LESS)
      pendingOptions.value.iceLevel = ICE_LEVEL.NORMAL
  }
}

const handleConfirm = () => {
  const key = makeCartKey(props.product.id, pendingOptions.value)
  emit('confirm', {
    _key: key,
    productId: props.product.id,
    productName: props.product.name,
    unitPrice: props.product.price,
    quantity: pendingQuantity.value,
    temperature: pendingOptions.value.temperature,
    iceLevel: pendingOptions.value.iceLevel,
    sugarLevel: pendingOptions.value.sugarLevel,
    isTakeaway: pendingOptions.value.isTakeaway,
    isAccompaniment: props.product.isAccompaniment ?? false,
  })
  emit('update:visible', false)
}
</script>

<template>
  <prime-dialog
    :visible="visible"
    modal
    :style="{ width: '52rem', maxWidth: '95vw' }"
    :pt="{ content: { class: 'tw:p-0! tw:overflow-hidden' } }"
    @update:visible="$emit('update:visible', $event)"
    @hide="$emit('update:visible', false)"
  >
    <div class="tw:flex tw:min-h-[28rem] tw:flex-col tw:sm:flex-row">
      <!-- LEFT: Product info -->
      <div
        class="tw:flex tw:flex-col tw:border-b tw:border-slate-200 tw:sm:w-5/12 tw:sm:border-b-0 tw:sm:border-r"
        style="border-color: var(--app-border); background: var(--app-bg-subtle)"
      >
        <div
          class="tw:relative tw:h-48 tw:flex-none tw:overflow-hidden tw:sm:h-56"
          style="background: var(--app-bg)"
        >
          <img
            v-if="product?.imageUrl"
            :src="product.imageUrl"
            :alt="product?.name"
            class="tw:h-full tw:w-full tw:object-cover"
          />
          <div
            v-else
            class="tw:flex tw:h-full tw:w-full tw:items-center tw:justify-center"
          >
            <iconify icon="ph:coffee-bold" class="tw:text-6xl tw:text-emerald-400/20" />
          </div>
        </div>

        <div class="tw:flex tw:flex-1 tw:flex-col tw:gap-2 tw:p-4">
          <h3 class="tw:text-lg tw:font-bold tw:leading-snug">{{ product?.name }}</h3>
          <p class="tw:text-xl tw:font-semibold tw:text-emerald-500">
            {{ product ? formatVnd(product.price) : '' }}
          </p>
          <p v-if="product?.description" class="tw:text-sm tw:leading-relaxed tw:text-muted">
            {{ product.description }}
          </p>
          <div
            v-if="product?.estimatedPrepMinutes"
            class="tw:mt-auto tw:flex tw:items-center tw:gap-1 tw:pt-2 tw:text-xs tw:text-muted"
          >
            <iconify icon="ph:clock-bold" class="tw:h-4 tw:w-4" />
            <span>{{
              t('orders.create.optionsDialog.prepTime', { min: product.estimatedPrepMinutes })
            }}</span>
          </div>
        </div>
      </div>

      <!-- RIGHT: Options + actions -->
      <div class="tw:flex tw:flex-1 tw:flex-col tw:p-5">
        <div class="tw:flex-1 tw:space-y-5">
          <!-- Quantity -->
          <div>
            <p class="tw:mb-2 tw:text-sm tw:font-semibold">
              {{ t('orders.create.optionsDialog.quantity') }}
            </p>
            <div class="tw:flex tw:items-center tw:gap-3">
              <button
                class="tw:flex tw:h-9 tw:w-9 tw:items-center tw:justify-center tw:rounded-xl tw:border tw:transition tw:hover:border-emerald-400 tw:text-muted"
                style="border-color: var(--app-border)"
                @click="pendingQuantity = Math.max(1, pendingQuantity - 1)"
              >
                <iconify icon="ph:minus-bold" class="tw:h-4 tw:w-4" />
              </button>
              <span class="tw:min-w-10 tw:text-center tw:text-xl tw:font-bold">{{
                pendingQuantity
              }}</span>
              <button
                class="tw:flex tw:h-9 tw:w-9 tw:items-center tw:justify-center tw:rounded-xl tw:border tw:transition tw:hover:border-emerald-400 tw:text-muted"
                style="border-color: var(--app-border)"
                @click="pendingQuantity++"
              >
                <iconify icon="ph:plus-bold" class="tw:h-4 tw:w-4" />
              </button>
            </div>
          </div>

          <!-- Temperature -->
          <div v-if="product?.hasTemperatureOption">
            <p class="tw:mb-2 tw:text-sm tw:font-semibold">
              {{ t('orders.create.optionsDialog.temperature') }}
            </p>
            <div class="tw:flex tw:gap-2">
              <prime-button
                v-for="opt in temperatureOptions"
                :key="opt.value"
                variant="outlined"
                class="tw:w-full"
                :severity="pendingOptions.temperature === opt.value ? 'primary' : 'secondary'"
                @click="setTemperature(opt.value)"
              >
                <iconify :icon="opt.icon" />
                <span>{{ opt.label }}</span>
              </prime-button>
            </div>
          </div>

          <!-- Ice level — only when not Hot -->
          <div
            v-if="
              product?.hasIceLevelOption && pendingOptions.temperature !== DRINK_TEMPERATURE.HOT
            "
          >
            <p class="tw:mb-2 tw:text-sm tw:font-semibold">
              {{ t('orders.create.optionsDialog.iceLevel') }}
            </p>
            <div class="tw:grid tw:grid-cols-2 tw:gap-2">
              <prime-button
                v-for="opt in iceLevelOptions"
                :key="opt.value"
                variant="outlined"
                class="tw:w-full"
                :severity="pendingOptions.iceLevel === opt.value ? 'primary' : 'secondary'"
                @click="pendingOptions.iceLevel = opt.value"
              >
                <iconify :icon="opt.icon" />
                <span>{{ opt.label }}</span>
              </prime-button>
            </div>
          </div>

          <!-- Sugar level — only when not Hot -->
          <div
            v-if="
              product?.hasSugarLevelOption && pendingOptions.temperature !== DRINK_TEMPERATURE.HOT
            "
          >
            <p class="tw:mb-2 tw:text-sm tw:font-semibold">
              {{ t('orders.create.optionsDialog.sugarLevel') }}
            </p>
            <div class="tw:grid tw:grid-cols-2 tw:gap-2">
              <prime-button
                v-for="opt in sugarLevelOptions"
                :key="opt.value"
                variant="outlined"
                class="tw:w-full"
                :severity="pendingOptions.sugarLevel === opt.value ? 'primary' : 'secondary'"
                @click="pendingOptions.sugarLevel = opt.value"
              >
                <iconify v-if="opt.icon" :icon="opt.icon" />
                <span>{{ opt.label }}</span>
              </prime-button>
            </div>
          </div>

          <!-- Serving -->
          <div>
            <p class="tw:mb-2 tw:text-sm tw:font-semibold">
              {{ t('orders.create.optionsDialog.serving') }}
            </p>
            <div class="tw:flex tw:gap-2">
              <prime-button
                v-for="servingType in servingOptions"
                :key="String(servingType.value)"
                variant="outlined"
                class="tw:w-full"
                :severity="
                  pendingOptions.isTakeaway === servingType.value ? 'primary' : 'secondary'
                "
                @click="pendingOptions.isTakeaway = servingType.value"
              >
                <iconify :icon="servingType.icon" />
                <span>{{ servingType.label }}</span>
              </prime-button>
            </div>
          </div>
        </div>

        <!-- Actions -->
        <div
          class="tw:mt-5 tw:flex tw:justify-end tw:gap-2 tw:border-t tw:pt-4"
          style="border-color: var(--app-border)"
        >
          <prime-button severity="secondary" text @click="$emit('update:visible', false)">
            <span>{{ t('orders.cancel') }}</span>
          </prime-button>
          <prime-button @click="handleConfirm">
            <iconify icon="prime:shopping-cart" />
            <span class="tw:ml-2">{{ t('orders.create.optionsDialog.addToCart') }}</span>
          </prime-button>
        </div>
      </div>
    </div>
  </prime-dialog>
</template>
