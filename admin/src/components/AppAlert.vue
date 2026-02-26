<script setup>
/**
 * AppAlert — Custom alert component inspired by PrimeBlocks alert patterns
 *
 * Props:
 *   severity    – 'success' | 'info' | 'warning' | 'error' | 'secondary'  (default: 'info')
 *   variant     – 'simple' | 'outlined' | 'accent' | 'filled'              (default: 'simple')
 *   title       – Bold heading text (optional; when set, body becomes smaller subtitle)
 *   icon        – Phosphor icon string override (e.g. 'ph:star-bold')
 *   showIcon    – Show/hide icon                                           (default: true)
 *   closable    – Show × close button                                      (default: false)
 *   visible     – v-model for dismiss state                                (default: true)
 *
 * Slots:
 *   default     – message body / description text
 *   #actions    – optional button row rendered at the bottom
 *   #icon       – fully custom icon content (overrides icon prop)
 *
 * Events:
 *   update:visible  – emitted with false when closed
 *   close           – emitted when close button is clicked
 *
 * Usage:
 *   <prime-alert severity="error" closable @close="err = ''">{{ err }}</prime-alert>
 *
 *   <prime-alert severity="success" variant="accent" title="Saved!">
 *     Your changes have been saved successfully.
 *   </prime-alert>
 *
 *   <prime-alert severity="warning" variant="outlined" title="Unsaved changes">
 *     You have unsaved changes.
 *     <template #actions>
 *       <prime-button size="small" severity="warning" label="Save now" />
 *       <prime-button size="small" severity="secondary" outlined label="Discard" />
 *     </template>
 *   </prime-alert>
 */

import { computed } from 'vue'

const props = defineProps({
  severity: {
    type: String,
    default: 'info',
    validator: (v) => ['success', 'info', 'warning', 'error', 'secondary'].includes(v),
  },
  variant: {
    type: String,
    default: 'simple',
    validator: (v) => ['simple', 'outlined', 'accent', 'filled'].includes(v),
  },
  title: {
    type: String,
    default: '',
  },
  icon: {
    type: String,
    default: '',
  },
  showIcon: {
    type: Boolean,
    default: true,
  },
  closable: {
    type: Boolean,
    default: false,
  },
  visible: {
    type: Boolean,
    default: true,
  },
})

const emit = defineEmits(['update:visible', 'close'])

// ── Severity maps ─────────────────────────────────────────────────

const DEFAULT_ICONS = {
  success:   'ph:check-circle-bold',
  info:      'ph:info-bold',
  warning:   'ph:warning-bold',
  error:     'ph:x-circle-bold',
  secondary: 'ph:bell-bold',
}

const ICON_COLOR = {
  success:   'tw:text-emerald-400',
  info:      'tw:text-sky-400',
  warning:   'tw:text-amber-400',
  error:     'tw:text-red-400',
  secondary: 'app-text-muted',
}

const TITLE_COLOR = {
  success:   'tw:text-emerald-300',
  info:      'tw:text-sky-300',
  warning:   'tw:text-amber-300',
  error:     'tw:text-red-300',
  secondary: 'app-text-muted',
}

// Background classes per variant × severity
const BG = {
  simple: {
    success:   'tw:bg-emerald-500/10',
    info:      'tw:bg-sky-500/10',
    warning:   'tw:bg-amber-500/10',
    error:     'tw:bg-red-500/10',
    secondary: 'tw:bg-white/5',
  },
  outlined: {
    success:   '',
    info:      '',
    warning:   '',
    error:     '',
    secondary: '',
  },
  accent: {
    success:   'tw:bg-emerald-500/8',
    info:      'tw:bg-sky-500/8',
    warning:   'tw:bg-amber-500/8',
    error:     'tw:bg-red-500/8',
    secondary: 'tw:bg-white/4',
  },
  filled: {
    success:   'tw:bg-emerald-500/20',
    info:      'tw:bg-sky-500/20',
    warning:   'tw:bg-amber-500/20',
    error:     'tw:bg-red-500/20',
    secondary: 'tw:bg-white/10',
  },
}

// Border classes per variant × severity
const BORDER = {
  simple: {
    success:   'tw:border tw:border-emerald-500/30',
    info:      'tw:border tw:border-sky-500/30',
    warning:   'tw:border tw:border-amber-500/30',
    error:     'tw:border tw:border-red-500/30',
    secondary: 'tw:border tw:border-white/15',
  },
  outlined: {
    success:   'tw:border tw:border-emerald-500/50',
    info:      'tw:border tw:border-sky-500/50',
    warning:   'tw:border tw:border-amber-500/50',
    error:     'tw:border tw:border-red-500/50',
    secondary: 'tw:border tw:border-white/25',
  },
  accent: {
    // Only left border — implemented via inline style for the accent bar
    success:   '',
    info:      '',
    warning:   '',
    error:     '',
    secondary: '',
  },
  filled: {
    success:   'tw:border tw:border-emerald-500/20',
    info:      'tw:border tw:border-sky-500/20',
    warning:   'tw:border tw:border-amber-500/20',
    error:     'tw:border tw:border-red-500/20',
    secondary: 'tw:border tw:border-white/10',
  },
}

// Left accent bar color (for variant="accent")
const ACCENT_BAR = {
  success:   'tw:bg-emerald-400',
  info:      'tw:bg-sky-400',
  warning:   'tw:bg-amber-400',
  error:     'tw:bg-red-400',
  secondary: 'tw:bg-slate-400',
}

// ── Computed ──────────────────────────────────────────────────────

const resolvedIcon = computed(() =>
  props.icon || DEFAULT_ICONS[props.severity]
)

const iconColor = computed(() => ICON_COLOR[props.severity])
const titleColor = computed(() => TITLE_COLOR[props.severity])
const accentBarColor = computed(() => ACCENT_BAR[props.severity])

const containerClass = computed(() => [
  'tw:relative tw:flex tw:items-start tw:gap-3 tw:rounded-xl tw:px-4 tw:py-3.5 tw:text-sm',
  BG[props.variant][props.severity],
  BORDER[props.variant][props.severity],
  // accent variant adds padding-left for the bar
  props.variant === 'accent' ? 'tw:pl-4 tw:overflow-hidden' : '',
])

// ── Handlers ─────────────────────────────────────────────────────

const dismiss = () => {
  emit('update:visible', false)
  emit('close')
}
</script>

<template>
  <Transition
    enter-active-class="tw:transition-all tw:duration-200 tw:ease-out"
    enter-from-class="tw:opacity-0 tw:translate-y-1"
    enter-to-class="tw:opacity-100 tw:translate-y-0"
    leave-active-class="tw:transition-all tw:duration-150 tw:ease-in"
    leave-from-class="tw:opacity-100 tw:scale-100"
    leave-to-class="tw:opacity-0 tw:scale-95"
  >
    <div v-if="visible" :class="containerClass" role="alert">

      <!-- ── Accent left bar (variant="accent" only) ─────────── -->
      <div
        v-if="variant === 'accent'"
        :class="['tw:absolute tw:inset-y-0 tw:left-0 tw:w-[3px] tw:rounded-l-xl', accentBarColor]"
        aria-hidden="true"
      />

      <!-- ── Icon ────────────────────────────────────────────── -->
      <div
        v-if="showIcon"
        class="tw:mt-0.5 tw:shrink-0"
        :class="iconColor"
      >
        <!-- Custom icon slot -->
        <slot name="icon">
          <iconify :icon="resolvedIcon" class="tw:text-[1.15rem]" />
        </slot>
      </div>

      <!-- ── Content ─────────────────────────────────────────── -->
      <div class="tw:min-w-0 tw:flex-1">

        <!-- Title (when provided) -->
        <p
          v-if="title"
          class="tw:font-semibold tw:leading-snug"
          :class="titleColor"
        >{{ title }}</p>

        <!-- Body text -->
        <div
          class="tw:leading-relaxed"
          :class="[title ? 'tw:mt-0.5 tw:text-xs app-text-muted' : '']"
        >
          <slot />
        </div>

        <!-- Actions slot -->
        <div
          v-if="$slots.actions"
          class="tw:mt-3 tw:flex tw:flex-wrap tw:items-center tw:gap-2"
        >
          <slot name="actions" />
        </div>

      </div>

      <!-- ── Close button ─────────────────────────────────────── -->
      <button
        v-if="closable"
        type="button"
        class="tw:ml-auto tw:shrink-0 tw:mt-0.5 tw:-mr-1 tw:flex tw:h-6 tw:w-6 tw:items-center tw:justify-center tw:rounded-md tw:opacity-60 tw:transition-opacity hover:tw:opacity-100 tw:cursor-pointer"
        :class="iconColor"
        @click="dismiss"
        aria-label="Dismiss"
      >
        <iconify icon="ph:x-bold" class="tw:text-sm" />
      </button>

    </div>
  </Transition>
</template>
