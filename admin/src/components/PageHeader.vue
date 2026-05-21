<script setup>
const route = useRoute()
const { t } = useI18n()

const props = defineProps({
  /** Override the section label (raw string — not an i18n key) */
  section:  { type: String, default: null },
  /** Override the page title (raw string — not an i18n key) */
  title:    { type: String, default: null },
  /** Static subtitle string */
  subtitle: { type: String, default: null },
})

/**
 * Section label — resolved from prop (raw string) or route.meta.section (i18n key).
 */
const resolvedSection = computed(() => {
  if (props.section !== null) return props.section
  const key = route.meta?.section
  return key ? t(key) : ''
})

/**
 * Page title — resolved from prop (raw string) or route.meta.pageTitle (i18n key).
 */
const resolvedTitle = computed(() => {
  if (props.title !== null) return props.title
  const key = route.meta?.pageTitle
  return key ? t(key) : ''
})
</script>

<template>
  <div class="tw:flex tw:flex-wrap tw:items-end tw:justify-between tw:gap-4">
    <!-- Left: section label + title + subtitle -->
    <div>
      <p
        v-if="resolvedSection"
        class="tw:text-[11px] tw:uppercase tw:tracking-[0.3em] tw:text-primary-300"
      >
        {{ resolvedSection }}
      </p>
      <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">{{ resolvedTitle }}</h1>

      <!-- #subtitle slot: rendered bare (slot provides its own wrapper) -->
      <div v-if="$slots.subtitle" class="tw:mt-1">
        <slot name="subtitle" />
      </div>
      <!-- subtitle prop: rendered inside a standard <p> -->
      <p v-else-if="subtitle" class="tw:mt-1 tw:text-sm tw:text-muted">{{ subtitle }}</p>
    </div>

    <!-- Right: action buttons etc. (default slot) -->
    <div v-if="$slots.default" class="tw:flex tw:items-center tw:gap-2">
      <slot />
    </div>
  </div>
</template>
