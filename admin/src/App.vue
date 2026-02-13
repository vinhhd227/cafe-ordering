<script setup>
import { computed, onMounted } from "vue";
import { useAuthStore } from "@/stores/auth";
import { useThemeStore } from "@/stores/theme";

const authStore = useAuthStore();
const themeStore = useThemeStore();
const isDark = computed(() => themeStore.isDark);
const isHydrating = computed(() => authStore.hydrating);

onMounted(() => {
  themeStore.init();
  authStore.hydrateFromRefresh();
});
</script>

<template>
  <div :class="isDark ? 'app-dark' : ''">
    <prime-scroll-top />
    <router-view />

    <div
      v-if="isHydrating"
      class="tw:fixed tw:inset-0 tw:z-50 tw:flex tw:items-center tw:justify-center tw:bg-black/10 tw:backdrop-blur-sm"
    >
      <div class="app-card tw:flex tw:items-center tw:gap-3 tw:rounded-2xl tw:border tw:px-4 tw:py-3">
        <span class="tw:h-5 tw:w-5 tw:animate-spin tw:rounded-full tw:border-2 tw:border-emerald-300 tw:border-t-transparent"></span>
        <span class="tw:text-sm app-text-muted">Loading...</span>
      </div>
    </div>
  </div>
</template>
