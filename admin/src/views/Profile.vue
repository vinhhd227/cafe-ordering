<script setup>
import { computed, onMounted, ref, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useAuthStore } from "@/stores/auth";
import { useThemeStore } from "@/stores/theme";

const auth = useAuthStore();
const route = useRoute();
const router = useRouter();

const userName = computed(() => {
  const firstName = auth.user?.firstName || "";
  const lastName = auth.user?.lastName || "";
  const fullName = `${firstName} ${lastName}`.trim();
  return fullName || "Shift Lead";
});
const userRole = computed(() => auth.user?.roles?.[0] || "admin");
const userEmail = computed(() => auth.user?.email || "staff@cafe.com");

const tabKeys = ["overview", "settings"];
const activeIndex = ref(0);

const themeStore = useThemeStore();
const isDark = computed(() => themeStore.isDark);

const handleThemeToggle = (value) => themeStore.applyTheme(value);

onMounted(() => {
  const tab = route.query.tab;
  if (typeof tab === "string") {
    const nextIndex = tabKeys.indexOf(tab);
    if (nextIndex >= 0) activeIndex.value = nextIndex;
  }
});

watch(
  () => route.query.tab,
  (tab) => {
    if (typeof tab !== "string") return;
    const nextIndex = tabKeys.indexOf(tab);
    if (nextIndex >= 0 && nextIndex !== activeIndex.value) {
      activeIndex.value = nextIndex;
    }
  },
);

watch(activeIndex, (value) => {
  const tab = tabKeys[value] ?? "overview";
  if (route.query.tab !== tab) {
    router.replace({ query: { ...route.query, tab } });
  }
});
</script>

<template>
  <section class="tw:space-y-6">
    <div>
      <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300">
        Profile
      </p>
      <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">Account</h1>
      <p class="tw:mt-2 tw:text-sm app-text-muted">
        Overview and settings for your staff account.
      </p>
    </div>

    <prime-tab-view v-model:activeIndex="activeIndex">
      <prime-tab-panel header="Overview">
        <prime-card
          class="app-card tw:rounded-2xl tw:border"
        >
          <template #content>
            <div class="tw:flex tw:flex-wrap tw:items-center tw:gap-6">
              <prime-avatar
                label="AO"
                shape="circle"
                size="xlarge"
                class="tw:bg-emerald-500/20 tw:text-emerald-200"
              />
              <div class="tw:space-y-2">
                <div class="tw:flex tw:items-center tw:gap-3">
                  <h2 class="tw:text-xl tw:font-semibold">{{ userName }}</h2>
                  <prime-tag :value="userRole" severity="success" />
                </div>
                <p class="tw:text-sm app-text-muted">{{ userEmail }}</p>
              </div>
            </div>

            <prime-divider class="tw:my-6" />

            <div class="tw:grid tw:grid-cols-1 tw:gap-4 lg:tw:grid-cols-3">
              <div
                class="app-panel tw:rounded-xl tw:border tw:p-4"
              >
                <p
                  class="tw:text-xs tw:uppercase tw:tracking-[0.3em] app-text-subtle"
                >
                  Shift
                </p>
                <p class="tw:mt-2 tw:text-lg tw:font-semibold">Morning</p>
                <p class="tw:text-xs app-text-muted">Starts 7:00 AM</p>
              </div>
              <div
                class="app-panel tw:rounded-xl tw:border tw:p-4"
              >
                <p
                  class="tw:text-xs tw:uppercase tw:tracking-[0.3em] app-text-subtle"
                >
                  Team
                </p>
                <p class="tw:mt-2 tw:text-lg tw:font-semibold">Front Counter</p>
                <p class="tw:text-xs app-text-muted">Lead role</p>
              </div>
              <div
                class="app-panel tw:rounded-xl tw:border tw:p-4"
              >
                <p
                  class="tw:text-xs tw:uppercase tw:tracking-[0.3em] app-text-subtle"
                >
                  Status
                </p>
                <p class="tw:mt-2 tw:text-lg tw:font-semibold">Active</p>
                <p class="tw:text-xs app-text-muted">Last login today</p>
              </div>
            </div>

            <div class="tw:mt-6 tw:flex tw:flex-wrap tw:gap-3">
              <prime-button label="Edit profile" icon="pi pi-user-edit" />
              <prime-button
                label="Manage password"
                icon="pi pi-lock"
                severity="secondary"
                outlined
              />
            </div>
          </template>
        </prime-card>
      </prime-tab-panel>

      <prime-tab-panel header="Settings">
        <prime-card
          class="app-card tw:rounded-2xl tw:border"
        >
          <template #content>
            <div class="tw:flex tw:flex-wrap tw:items-center tw:justify-between tw:gap-4">
              <div>
                <p class="tw:text-base tw:font-semibold">Theme</p>
                <p class="tw:text-sm app-text-muted">
                  Toggle dark mode for the admin panel.
                </p>
              </div>
              <prime-toggle-switch :modelValue="isDark" @update:modelValue="handleThemeToggle" />
            </div>

            <prime-divider class="tw:my-6" />

            <div class="tw:flex tw:flex-wrap tw:items-center tw:justify-between tw:gap-4">
              <div>
                <p class="tw:text-base tw:font-semibold">Notifications</p>
                <p class="tw:text-sm app-text-muted">
                  Manage order and shift alerts.
                </p>
              </div>
              <prime-button label="Configure" severity="secondary" outlined />
            </div>
          </template>
        </prime-card>
      </prime-tab-panel>
    </prime-tab-view>
  </section>
</template>
