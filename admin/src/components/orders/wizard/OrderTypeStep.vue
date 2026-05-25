<script setup>
const { t } = useI18n()

const props = defineProps({
  modelValue: { type: String, default: 'DINE_IN' },
})
const emit = defineEmits(['update:modelValue', 'next'])

const types = [
  { value: 'DINE_IN',  icon: 'ph:fork-knife-bold',    color: 'tw:text-primary-400' },
  { value: 'TAKEAWAY', icon: 'ph:bag-bold',            color: 'tw:text-amber-400' },
  { value: 'DELIVERY', icon: 'ph:motorcycle-bold',     color: 'tw:text-sky-400' },
]

const select = (value) => {
  emit('update:modelValue', value)
  emit('next')
}
</script>

<template>
  <div class="tw:space-y-4">
   <br>

    <div class="tw:grid tw:grid-cols-1 tw:sm:grid-cols-3 tw:gap-3">
      <button
        v-for="type in types"
        :key="type.value"
        class="tw:flex tw:flex-col tw:items-center tw:gap-3 tw:rounded-2xl tw:border tw:p-6 tw:text-center tw:transition-all tw:cursor-pointer tw:hover:-translate-y-0.5"
        :class="modelValue === type.value
          ? 'tw:border-primary-500 tw:bg-primary-500/10'
          : 'tw:hover:border-primary-500/40'"
        :style="modelValue === type.value ? '' : 'border-color: var(--app-border); background: var(--app-bg-subtle)'"
        @click="select(type.value)"
      >
        <iconify :icon="type.icon" class="tw:text-4xl" :class="type.color" />
        <div>
          <p class="tw:font-semibold tw:text-sm">{{ t(`orders.create.orderType.${type.value}`) }}</p>
          <p class="tw:text-xs tw:text-muted tw:mt-0.5">{{ t(`orders.create.orderType.${type.value}_desc`) }}</p>
        </div>
        <iconify
          v-if="modelValue === type.value"
          icon="ph:check-circle-fill"
          class="tw:text-primary-400 tw:text-xl"
        />
      </button>
    </div>
  </div>
</template>
