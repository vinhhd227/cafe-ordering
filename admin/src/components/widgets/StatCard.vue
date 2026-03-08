<script setup>
defineProps({
  label: String,
  value: { type: [String, Number], default: '-' },
  labelClass: { type: String, default: 'app-text-subtle' },
})
</script>

<template>
  <prime-card class="app-card tw:rounded-xl tw:border">
    <template #content>
      <div class="tw:flex tw:items-center tw:justify-between">
        <p
          class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em]"
          :class="labelClass"
        >
          {{ label }}
        </p>
        <slot name="icon" />
      </div>
      <p class="tw:mt-2 tw:text-2xl tw:font-semibold">{{ value }}</p>
      <slot name="sub" />
    </template>
  </prime-card>
</template>
