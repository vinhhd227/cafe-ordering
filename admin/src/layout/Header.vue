<script setup>
import { useSidebar } from "@/composables/useSidebar";

const { isCollapsed, toggle, toggleCollapse } = useSidebar();
</script>

<template>
  <header
    class="app-header tw:flex tw:shrink-0 tw:items-center tw:justify-between tw:border-b tw:px-4 tw:h-[5.25rem] tw:backdrop-blur tw:sticky tw:top-0 tw:z-20 lg:tw:px-8"
  >
    <div class="tw:flex tw:items-center tw:gap-3">
      <!-- Mobile: open sidebar overlay -->
      <button
        type="button"
        class="tw:flex tw:h-9 tw:w-9 tw:items-center tw:justify-center tw:rounded-lg tw:transition-colors hover:tw:bg-black/5 tw:lg:hidden"
        @click="toggle"
      >
        <iconify icon="ph:list-bold" class="tw:text-lg" />
      </button>

      <!-- Desktop: collapse/expand sidebar -->
      <button
        type="button"
        class="tw:hidden tw:lg:flex tw:h-9 tw:w-9 tw:items-center tw:justify-center tw:rounded-lg tw:transition-colors hover:tw:bg-black/5"
        :title="isCollapsed ? 'Expand sidebar' : 'Collapse sidebar'"
        @click="toggleCollapse"
      >
        <iconify
          :icon="isCollapsed ? 'ph:sidebar-simple-bold' : 'ph:sidebar-simple-bold'"
          class="tw:text-lg"
        />
      </button>

      <div>
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] app-text-subtle">
          Admin workspace
        </p>
        <h2 class="tw:text-lg tw:font-semibold">Good shift, team.</h2>
      </div>
    </div>

    <prime-button label="Export" severity="secondary" outlined size="small" />
  </header>
</template>
