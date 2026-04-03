<script setup>
import { computed, reactive, ref, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useAuthStore } from "@/stores/auth";
import { useThemeStore } from "@/stores/theme";
import { navGroups } from "@/layout/nav";
import { useSidebar } from "@/composables/useSidebar";

const { t } = useI18n()
const { locale, locales, setLocale } = useLocale()

const route = useRoute();
const router = useRouter();
const auth = useAuthStore();
const themeStore = useThemeStore();
const { isOpen, isCollapsed, close } = useSidebar();

const canAccess = (item) => {
  const user = auth.user
  if (!user) return false
  const isAdmin = user.roles?.includes('Admin')
  if (item.adminOnly) return isAdmin
  if (!item.requiredClaim) return true
  return isAdmin || user.permissions?.includes(item.requiredClaim)
}

const visibleNavGroups = computed(() =>
  navGroups
    .map(group => ({ ...group, items: group.items.filter(canAccess) }))
    .filter(group => group.items.length > 0)
)
const profileMenu = ref();

const isActive = (to) => route.name === to.name;

const isParentActive = (item) => route.path.startsWith('/orders');

const expandedMap = reactive({});

const isExpanded = (labelKey) => !!expandedMap[labelKey];

const toggleExpand = (labelKey) => { expandedMap[labelKey] = !expandedMap[labelKey]; };

const handleParentClick = (item) => {
  if (isCollapsed.value) {
    router.push(item.children[0].to);
    close();
  } else {
    toggleExpand(item.labelKey);
  }
};

watch(route, () => {
  visibleNavGroups.value.forEach(group => {
    group.items.forEach(item => {
      if (item.children && item.children.some(child => route.name === child.to.name)) {
        expandedMap[item.labelKey] = true;
      }
    });
  });
}, { immediate: true });

const fullName = computed(() => auth.user?.fullName || "Staff");

const roleLabel = computed(() => {
  if (Array.isArray(auth.user?.roles) && auth.user.roles.length > 0) {
    return auth.user.roles[0];
  }
  return auth.user?.role || "Staff";
});

const roleTextColor = computed(() => {
  const role = roleLabel.value.toLowerCase();
  if (role === "admin") return "tw:text-emerald-400";
  if (role === "manager") return "tw:text-sky-400";
  return "tw:text-amber-400";
});

const roleDotColor = computed(() => {
  const role = roleLabel.value.toLowerCase();
  if (role === "admin") return "tw:bg-emerald-400";
  if (role === "manager") return "tw:bg-sky-400";
  return "tw:bg-amber-400";
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
    label: t('auth.profile'),
    command: () => router.push({ name: "profile", query: { tab: "overview" } }),
  },
  {
    label: t('auth.settings'),
    command: () => router.push({ name: "profile", query: { tab: "settings" } }),
  },
  { separator: true },
  {
    label: themeStore.isDark ? t('auth.switchToLight') : t('auth.switchToDark'),
    command: () => themeStore.toggleTheme(),
  },
  {
    label: t('auth.helpSupport'),
    command: () => window.open("mailto:support@cafe.com", "_blank"),
  },
  { separator: true },
  {
    label: t('auth.logout'),
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
  <aside
    class="app-sidebar tw:fixed tw:inset-y-0 tw:left-0 tw:z-40 tw:flex tw:flex-col tw:border-r tw:backdrop-blur tw:transition-all tw:duration-300 tw:overflow-x-hidden"
    :class="[
      isCollapsed ? 'tw:w-16' : 'tw:w-64',
      isOpen ? 'tw:translate-x-0' : 'tw:-translate-x-full tw:lg:translate-x-0'
    ]"
  >

    <!-- Brand -->
    <div
      class="tw:shrink-0 tw:flex tw:border-b tw:transition-all tw:duration-300 tw:h-[5.25rem]"
      :class="isCollapsed ? 'tw:items-center tw:justify-center tw:px-2' : 'tw:flex-col tw:justify-center tw:px-6'"
      style="border-color: var(--app-border)"
    >
      <template v-if="!isCollapsed">
        <p class="tw:text-[10px] tw:font-semibold tw:uppercase tw:tracking-[0.4em] tw:text-emerald-400">
          {{ t('sidebar.brand') }}
        </p>
        <h1 class="tw:mt-1 tw:text-base tw:font-semibold">{{ t('sidebar.title') }}</h1>
      </template>
      <iconify v-else icon="ph:coffee-bold" class="tw:text-xl tw:text-emerald-400" />
    </div>

    <!-- Navigation groups -->
    <nav
      class="tw:flex-1 tw:overflow-y-auto tw:py-4 tw:transition-all tw:duration-300"
      :class="isCollapsed ? 'tw:px-2' : 'tw:px-3'"
    >
      <div
        v-for="group in visibleNavGroups"
        :key="group.labelKey"
        class="tw:mb-5 last:tw:mb-0"
      >
        <!-- Section label -->
        <template v-if="!isCollapsed">
          <p class="tw:mb-1.5 tw:px-3 tw:text-[10px] tw:font-semibold tw:uppercase tw:tracking-[0.15em] app-text-subtle">
            {{ t(group.labelKey) }}
          </p>
        </template>
        <div v-else class="tw:mb-2 tw:mx-1 tw:h-px" style="background: var(--app-border)" />

        <!-- Nav items -->
        <div class="tw:space-y-0.5">
          <template v-for="item in group.items" :key="item.labelKey">
            <!-- Item with children (expandable) -->
            <template v-if="item.children">
              <button
                type="button"
                class="tw:w-full tw:flex tw:items-center tw:rounded-lg tw:py-2.5 tw:text-sm tw:font-medium tw:transition-all tw:duration-150"
                :class="[
                  isCollapsed ? 'tw:justify-center tw:px-0' : 'tw:gap-3 tw:px-3',
                  isParentActive(item)
                    ? 'tw:bg-emerald-500/10 tw:text-emerald-400'
                    : 'app-text-muted hover:tw:bg-white/5 hover:tw:text-white'
                ]"
                :title="isCollapsed ? t(item.labelKey) : undefined"
                @click="handleParentClick(item)"
              >
                <iconify :icon="item.icon" class="tw:shrink-0 tw:text-base" />
                <span v-if="!isCollapsed" class="tw:flex-1 tw:text-left">{{ t(item.labelKey) }}</span>
                <iconify
                  v-if="!isCollapsed"
                  icon="ph:caret-down-bold"
                  class="tw:shrink-0 tw:text-xs tw:transition-transform tw:duration-200"
                  :class="isExpanded(item.labelKey) ? 'tw:rotate-180' : ''"
                />
              </button>
              <!-- Children -->
              <div v-if="!isCollapsed && isExpanded(item.labelKey)" class="tw:ml-4 tw:mt-0.5 tw:space-y-0.5">
                <router-link
                  v-for="child in item.children"
                  :key="child.to.name"
                  :to="child.to"
                  class="tw:flex tw:items-center tw:rounded-lg tw:py-2 tw:px-3 tw:text-sm tw:font-medium tw:transition-all tw:duration-150 tw:no-underline tw:gap-3"
                  :class="isActive(child.to)
                    ? 'tw:bg-emerald-500/10 tw:text-emerald-400'
                    : 'app-text-muted hover:tw:bg-white/5 hover:tw:text-white'"
                  @click="close"
                >
                  <iconify :icon="child.icon" class="tw:shrink-0 tw:text-sm" />
                  <span>{{ t(child.labelKey) }}</span>
                </router-link>
              </div>
            </template>

            <!-- Regular item -->
            <router-link
              v-else
              :to="item.to"
              class="tw:flex tw:items-center tw:rounded-lg tw:py-2.5 tw:text-sm tw:font-medium tw:transition-all tw:duration-150 tw:no-underline"
              :class="[
                isCollapsed ? 'tw:justify-center tw:px-0' : 'tw:gap-3 tw:px-3',
                isActive(item.to)
                  ? 'tw:bg-emerald-500/10 tw:text-emerald-400'
                  : 'app-text-muted hover:tw:bg-white/5 hover:tw:text-white'
              ]"
              :title="isCollapsed ? t(item.labelKey) : undefined"
              @click="close"
            >
              <iconify :icon="item.icon" class="tw:shrink-0 tw:text-base" />
              <span v-if="!isCollapsed">{{ t(item.labelKey) }}</span>
            </router-link>
          </template>
        </div>
      </div>
    </nav>

    <!-- Language toggle -->
    <div
      v-if="!isCollapsed"
      class="tw:shrink-0 tw:border-t tw:px-4 tw:py-2 tw:flex tw:gap-1"
      style="border-color: var(--app-border)"
    >
      <button
        v-for="l in locales"
        :key="l.code"
        type="button"
        class="tw:flex-1 tw:rounded-lg tw:py-1 tw:text-xs tw:font-semibold tw:transition-all tw:duration-150"
        :class="locale === l.code
          ? 'tw:bg-emerald-500/15 tw:text-emerald-400'
          : 'app-text-subtle hover:tw:bg-white/5 hover:tw:text-white'"
        @click="setLocale(l.code)"
      >
        {{ l.label }}
      </button>
    </div>

    <!-- User profile footer -->
    <div class="tw:shrink-0 tw:border-t tw:p-3" style="border-color: var(--app-border)">
      <button
        type="button"
        class="tw:flex tw:w-full tw:items-center tw:rounded-xl tw:py-2.5 tw:transition-all tw:duration-150 hover:tw:bg-white/5"
        :class="isCollapsed ? 'tw:justify-center tw:px-0' : 'tw:gap-3 tw:px-3'"
        :title="isCollapsed ? fullName : undefined"
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
        <template v-if="!isCollapsed">
          <div class="tw:min-w-0 tw:flex-1 tw:text-left">
            <p class="tw:truncate tw:text-sm tw:font-semibold">{{ fullName }}</p>
            <p class="tw:flex tw:items-center tw:gap-1 tw:mt-0.5">
              <span class="tw:inline-block tw:h-1.5 tw:w-1.5 tw:rounded-full tw:shrink-0" :class="roleDotColor" style="opacity: 0.85" />
              <span class="tw:truncate tw:text-xs tw:font-medium tw:capitalize" :class="roleTextColor">{{ roleLabel }}</span>
            </p>
          </div>
          <iconify icon="ph:dots-three-bold" class="tw:shrink-0 tw:text-base app-text-subtle" />
        </template>
      </button>
      <prime-menu ref="profileMenu" :model="profileItems" popup />
    </div>

  </aside>
</template>
