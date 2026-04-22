<script setup>
import { ref } from "vue";
import { useSidebar } from "@/composables/useSidebar";
import { useThemeStore } from "@/stores/theme";
import NotificationBell from "@/components/NotificationBell.vue";
import ProfileMenu from "@/layout/ProfileMenu.vue";

const { t } = useI18n();
const { locale, locales, setLocale } = useLocale();
const { isCollapsed, toggle, toggleCollapse } = useSidebar();
const themeStore = useThemeStore();

const langMenu = ref();
const langMenuItems = computed(() =>
  (locales ?? []).map((l) => ({
    label: l.name,
    command: () => setLocale(l.code),
    class: locale.value === l.code ? "p-menuitem-active-locale" : "",
  })),
);
</script>

<template>
  <header
    class="app-header tw:flex tw:shrink-0 tw:items-center tw:justify-between tw:border-b tw:px-4 tw:h-21 tw:backdrop-blur tw:sticky tw:top-0 tw:z-20 lg:tw:px-8"
  >
    <div class="tw:flex tw:items-center tw:gap-3">
      <!-- Mobile: open sidebar -->
      <prime-button
        variant="text"
        class="tw:flex! tw:lg:hidden! tw:items-center tw:justify-center tw:rounded-lg tw:transition-colors tw:hover:bg-black/5"
        @click="toggle"
      >
        <iconify icon="ph:list-bold" class="tw:text-lg tw:text-muted" />
      </prime-button>

      <!-- Desktop: collapse/expand sidebar -->
      <prime-button
        variant="text"
        class="tw:hidden! tw:lg:flex! tw:items-center tw:justify-center tw:rounded-lg tw:transition-colors tw:hover:bg-black/5"
        :title="
          isCollapsed ? t('header.expandSidebar') : t('header.collapseSidebar')
        "
        @click="toggleCollapse"
      >
        <iconify
          :icon="isCollapsed ? 'ph:sidebar-simple-bold' : 'ph:sidebar-duotone'"
          class="tw:text-lg tw:text-muted"
        />
      </prime-button>

      <div class="tw:hidden tw:lg:block">
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] app-text-subtle">
          {{ t("header.workspace") }}
        </p>
        <h2 class="tw:text-lg tw:font-semibold">{{ t("header.greeting") }}</h2>
      </div>
    </div>

    <div class="tw:flex tw:items-center tw:gap-2">
      <!-- Theme toggle -->

      <!-- Language toggle -->
      <div class="tw:relative">
        <prime-button
          variant="text"
          class="tw:flex tw:h-9! tw:w-9! tw:p-0! tw:items-center tw:justify-center tw:rounded-full tw:transition-colors tw:hover:ring-2 tw:hover:ring-white/20 tw:text-muted!"
          :title="t('header.language')"
          @click="langMenu.toggle($event)"
        >
          <iconify icon="flowbite:language-outline" class="tw:text-lg" />
        </prime-button>
        <prime-menu ref="langMenu" :model="langMenuItems" popup />
      </div>
      <prime-button
        variant="text"
        class="tw:flex tw:h-9! tw:w-9! tw:p-0! tw:items-center tw:justify-center tw:rounded-full tw:transition-colors tw:hover:ring-2 tw:hover:ring-white/20 tw:text-muted!"
        :title="
          themeStore.isDark ? t('auth.switchToLight') : t('auth.switchToDark')
        "
        @click="themeStore.toggleTheme()"
      >
        <iconify
          :icon="themeStore.isDark ? 'ph:sun-bold' : 'ph:moon-bold'"
          class="tw:text-lg"
        />
      </prime-button>
      <notification-bell /> 

      <profile-menu />
    </div>
  </header>
</template>
