<script setup>
defineProps({
  widgets: Array,  // [{ id, label, description, visible, previewComponent, previewProps }]
  hiddenCount: { type: Number, default: 0 },
})

const emit = defineEmits(['toggle'])

const visible = ref(false)
</script>

<template>
  <div class="tw:relative tw:inline-flex">
    <prime-button
      v-tooltip.bottom="'Widget settings'"
      :class="btnIcon"
      severity="secondary"
      outlined
      @click="visible = true"
    >
      <iconify icon="ph:squares-four-bold" />
    </prime-button>

    <!-- Hidden count badge -->
    <span
      v-if="hiddenCount > 0"
      class="tw:pointer-events-none tw:absolute tw:-right-1.5 tw:-top-1.5 tw:flex tw:size-4 tw:items-center tw:justify-center tw:rounded-full tw:bg-amber-500 tw:text-[9px] tw:font-bold tw:text-white"
    >
      {{ hiddenCount }}
    </span>

    <prime-dialog
      v-model:visible="visible"
      modal
      :draggable="false"
      header="Widget settings"
    >
      <div class="tw:grid tw:grid-cols-2 tw:gap-x-3 tw:gap-y-4 sm:tw:grid-cols-3">
        <div v-for="w in widgets" :key="w.id" class="tw:flex tw:flex-col tw:gap-2">
          <!-- Actual component rendered with preview props -->
          <div
            class="tw:cursor-pointer tw:transition-all tw:duration-200"
            :class="w.visible ? '' : 'tw:opacity-40 tw:grayscale'"
            @click="emit('toggle', w.id)"
          >
            <component
              :is="w.previewComponent"
              v-bind="w.previewProps"
              class="tw:pointer-events-none"
            />
          </div>

          <!-- Toggle -->
          <div class="tw:flex tw:justify-end tw:px-1" @click.stop>
            <prime-toggle-switch
              :model-value="w.visible"
              class="tw:scale-75 tw:origin-right"
              @change="emit('toggle', w.id)"
            />
          </div>

          <!-- Description (outside the card) -->
          <p class="tw:text-[11px] tw:leading-snug app-text-muted tw:px-1">
            {{ w.description }}
          </p>
        </div>
      </div>
    </prime-dialog>
  </div>
</template>
