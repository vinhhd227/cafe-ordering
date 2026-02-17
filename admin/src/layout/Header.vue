<script setup>
import { computed, ref } from "vue";
import { useRouter } from "vue-router";
import { useAuthStore } from "@/stores/auth";
import { useThemeStore } from "@/stores/theme";

const router = useRouter();
const auth = useAuthStore();
const themeStore = useThemeStore();
const profileMenu = ref();

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
    command: () =>
      router.push({ name: "profile", query: { tab: "overview" } }),
  },
  {
    label: "Settings",
    command: () =>
      router.push({ name: "profile", query: { tab: "settings" } }),
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
      router.push({ name: "login" });
    },
  },
]);

const toggleProfileMenu = (event) => {
  profileMenu.value?.toggle(event);
};
</script>

<template>
  <header
    class="app-header tw:flex tw:items-center tw:justify-between tw:border-b tw:px-8 tw:py-5 tw:backdrop-blur"
  >
    <div>
      <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] app-text-subtle">
        Admin workspace
      </p>
      <h2 class="tw:text-lg tw:font-semibold">Good shift, team.</h2>
    </div>
    <div class="tw:flex tw:items-center tw:gap-4">
      <prime-button label="Export" severity="secondary" outlined size="small" />
      <div class="tw:flex tw:items-center tw:gap-3">
        <button
          type="button"
          class="tw:flex tw:items-center tw:gap-3 tw:rounded-2xl tw:border app-border tw:bg-white/5 tw:px-3 tw:py-2 tw:transition hover:tw:bg-white/10"
          @click="toggleProfileMenu"
        >
          <prime-avatar
            v-if="avatarImage"
            :image="avatarImage"
            shape="circle"
            size="normal"
          />
          <prime-avatar
            v-else
            :label="avatarLabel"
            shape="circle"
            size="normal"
            class="tw:bg-emerald-500/20 tw:text-emerald-200"
          />
          <div class="tw:text-left tw:text-sm">
            <p class="tw:font-semibold">{{ fullName }}</p>
            <p class="tw:text-xs app-text-subtle">{{ roleLabel }}</p>
          </div>
        </button>
        <prime-menu ref="profileMenu" :model="profileItems" popup />
      </div>
    </div>
  </header>
</template>
