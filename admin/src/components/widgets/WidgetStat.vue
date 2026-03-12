<script setup>
defineProps({
  label: String,
  value: { type: [String, Number], default: '-' },
  labelClass: { type: String, default: 'app-text-subtle' },
})
</script>

<template>
  <prime-card :pt="{
      root: { class: `${appCard} ${cardRing} tw:p-4` },
      header: { class: 'tw:flex tw:justify-between tw:items-center tw:gap-3 tw:min-w-0' },
      body: { class: 'tw:p-0!' },
      content: { class: 'tw:space-y-1' },
    }">
    <template #header>
      <p
          class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] tw:min-w-0 tw:flex-1 tw:truncate"
          :class="labelClass"
        >
          {{ label }}
        </p>
         <div class="tw:shrink-0">
          <slot name="icon" />
         </div>
      </template>
    <template #content>
      <p class="tw:mt-2 tw:text-2xl tw:font-semibold">{{ value }}</p>
      <slot name="sub" />
    </template>
  </prime-card>
</template>
