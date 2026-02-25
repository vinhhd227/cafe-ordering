<script setup>
import { computed, ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useAuthStore } from "@/stores/auth";
import { useThemeStore } from "@/stores/theme";
import { navGroups } from "@/layout/nav";

const route = useRoute();
const router = useRouter();
const auth = useAuthStore();
const themeStore = useThemeStore();
const profileMenu = ref();

const isActive = (to) => route.name === to.name;

const fullName = computed(() => {
  const firstName = auth.user?.firstName || "";
  const lastName = auth.user?.lastName || "";
  const name = `${firstName} ${lastName}`.trim();
  return name || "Staff";
});

const roleLabel = computed(() => {
  if (auth.user?.role) return auth.user.role;
  if (Array.isArray(auth.user?.roles) && auth.user.roles.length > 0) {
    return auth.user.roles[0];
  }
  return "staff";
});

const avatarLabel = computed(() => {
  const parts = fullName.value.split(" ").filter(Boolean);
  if (parts.length === 0) return "ST";
  const initials = parts
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase())
    .join("");
  return initials || "ST";
});

const avatarImage = computed(() => {
  return auth.user?.avatarUrl || auth.user?.avatar || null;
});

const profileItems = computed(() => [
  {
    label: "View Profile",
    command: () => router.push({ name: "profile", query: { tab: "overview" } }),
  },
  {
    label: "Settings",
    command: () => router.push({ name: "profile", query: { tab: "settings" } }),
  },
  { separator: true },
  {
    label: themeStore.isDark ? "Switch to Light" : "Switch to Dark",
    command: () => themeStore.toggleTheme(),
  },
  {
    label: "Help/Support",
    command: () => window.open("mailto:support@cafe.com", "_blank"),
  },
  { separator: true },
  {
    label: "Logout",
    command: () => {
      auth.logout();
      window.location.replace("/login");
    },
  },
]);

const toggleProfileMenu = (event) => {
  profileMenu.value?.toggle(event);
};
</script>

<template>
  <aside class="app-sidebar tw:flex tw:h-screen tw:w-64 tw:flex-col tw:border-r tw:backdrop-blur">

    <!-- Brand -->
    <div class="tw:shrink-0 tw:border-b tw:px-6 tw:py-5" style="border-color: var(--app-border)">
      <p class="tw:text-[10px] tw:font-semibold tw:uppercase tw:tracking-[0.4em] tw:text-emerald-400">
        Cafe Ordering
      </p>
      <h1 class="tw:mt-1 tw:text-base tw:font-semibold">Admin Panel</h1>
    </div>

    <!-- Navigation groups -->
    <nav class="tw:flex-1 tw:overflow-y-auto tw:px-3 tw:py-4">
      <div
        v-for="group in navGroups"
        :key="group.label"
        class="tw:mb-5 last:tw:mb-0"
      >
        <!-- Section label -->
        <p class="tw:mb-1.5 tw:px-3 tw:text-[10px] tw:font-semibold tw:uppercase tw:tracking-[0.15em] app-text-subtle">
          {{ group.label }}
        </p>

        <!-- Nav items -->
        <div class="tw:space-y-0.5">
          <router-link
            v-for="item in group.items"
            :key="item.to.name"
            :to="item.to"
            class="tw:flex tw:items-center tw:gap-3 tw:rounded-lg tw:px-3 tw:py-2.5 tw:text-sm tw:font-medium tw:transition-all tw:duration-150 tw:no-underline"
            :class="
              isActive(item.to)
                ? 'tw:bg-emerald-500/10 tw:text-emerald-400'
                : 'app-text-muted hover:tw:bg-white/5 hover:tw:text-white'
            "
          >
            <iconify :icon="item.icon" class="tw:shrink-0 tw:text-base" />
            <span>{{ item.label }}</span>
          </router-link>
        </div>
      </div>
    </nav>

    <!-- User profile footer -->
    <div class="tw:shrink-0 tw:border-t tw:p-3" style="border-color: var(--app-border)">
      <button
        type="button"
        class="tw:flex tw:w-full tw:items-center tw:gap-3 tw:rounded-xl tw:px-3 tw:py-2.5 tw:transition-all tw:duration-150 hover:tw:bg-white/5"
        @click="toggleProfileMenu"
      >
        <prime-avatar
          v-if="avatarImage"
          :image="avatarImage"
          shape="circle"
          size="normal"
          class="tw:shrink-0"
        />
        <prime-avatar
          v-else
          :label="avatarLabel"
          shape="circle"
          size="normal"
          class="tw:shrink-0 tw:bg-emerald-500/20 tw:text-emerald-300"
        />
        <div class="tw:min-w-0 tw:flex-1 tw:text-left">
          <p class="tw:truncate tw:text-sm tw:font-semibold">{{ fullName }}</p>
          <p class="tw:truncate tw:text-xs app-text-subtle tw:capitalize">{{ roleLabel }}</p>
        </div>
        <iconify icon="ph:dots-three-bold" class="tw:shrink-0 tw:text-base app-text-subtle" />
      </button>
      <prime-menu ref="profileMenu" :model="profileItems" popup />
    </div>

  </aside>
</template>
