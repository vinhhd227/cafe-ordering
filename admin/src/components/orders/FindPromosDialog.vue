<script setup>
defineProps({
  visible: Boolean,
  promos: Array,
  loading: Boolean,
  promoInfo: Object,
  isPromoAvailable: Function,
  promoDisableReason: Function,
  formatPromotionValue: Function,
  formatVnd: Function,
})

const emit = defineEmits(['update:visible', 'select'])

const { t } = useI18n()
</script>

<template>
  <prime-dialog
    :visible="visible"
    :header="t('orders.create.promoDialog.title')"
    modal
    :style="{ width: '30rem' }"
    @update:visible="$emit('update:visible', $event)"
  >
    <div class="tw:space-y-2">
      <div
        v-if="loading"
        class="tw:flex tw:items-center tw:justify-center tw:gap-2 tw:py-8 tw:text-muted"
      >
        <iconify icon="prime:spinner" class="tw:animate-spin" />
        <span class="tw:text-sm">{{ t('orders.create.promoDialog.loading') }}</span>
      </div>
      <div
        v-else-if="promos.length === 0"
        class="tw:py-8 tw:text-center tw:text-sm tw:text-muted"
      >
        {{ t('orders.create.promoDialog.empty') }}
      </div>
      <div
        v-else
        v-for="promo in promos"
        :key="promo.id"
        class="tw:rounded-xl tw:border tw:p-3 tw:transition-colors"
        :class="
          isPromoAvailable(promo)
            ? 'tw:cursor-pointer tw:hover:border-primary-500/50 tw:hover:bg-primary-500/5'
            : 'tw:opacity-40 tw:cursor-not-allowed'
        "
        style="border-color: var(--app-border)"
        @click="isPromoAvailable(promo) && $emit('select', promo)"
      >
        <div class="tw:flex tw:items-start tw:justify-between tw:gap-3">
          <div class="tw:min-w-0">
            <p class="tw:text-sm tw:font-semibold tw:leading-snug">{{ promo.name }}</p>
            <p class="tw:text-xs tw:font-mono tw:text-muted tw:mt-0.5">{{ promo.code }}</p>
            <div class="tw:flex tw:flex-wrap tw:gap-x-3 tw:gap-y-0.5 tw:mt-1.5">
              <span v-if="promo.minOrderAmount" class="tw:text-xs tw:text-muted">
                {{
                  t('orders.create.promoDialog.minAmount', {
                    amount: formatVnd(promo.minOrderAmount),
                  })
                }}
              </span>
              <span v-if="promo.endDate" class="tw:text-xs tw:text-muted">
                {{
                  t('orders.create.promoDialog.until', {
                    date: new Date(promo.endDate).toLocaleDateString('vi-VN'),
                  })
                }}
              </span>
              <span v-if="promo.maxUsage" class="tw:text-xs tw:text-muted">
                {{
                  t('orders.create.promoDialog.usesLeft', {
                    n: promo.maxUsage - promo.currentUsage,
                  })
                }}
              </span>
              <span v-if="!isPromoAvailable(promo)" class="tw:text-xs tw:text-red-400">
                {{ promoDisableReason(promo) }}
              </span>
            </div>
          </div>
          <span class="tw:shrink-0 tw:text-primary-400 tw:font-semibold tw:text-sm">
            {{ formatPromotionValue(promo) }}
          </span>
        </div>
      </div>
    </div>
  </prime-dialog>
</template>
