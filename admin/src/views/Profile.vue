<script setup>
import { computed, onMounted, ref, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useToast } from "primevue/usetoast";
import { useAuthStore } from "@/stores/auth";
import { useThemeStore } from "@/stores/theme";
import { updateUser } from "@/services/user.service";
import { changePassword } from "@/services/auth.service";

const auth = useAuthStore();
const route = useRoute();
const router = useRouter();
const toast = useToast();

const userName = computed(() => auth.user?.fullName || "Unknown");
const userInitials = computed(() =>
  (auth.user?.fullName ?? "")
    .split(" ")
    .filter(Boolean)
    .map((w) => w[0])
    .slice(0, 2)
    .join("")
    .toUpperCase() || "?",
);
const userRole = computed(() => auth.user?.roles?.[0] || "Staff");
const userEmail = computed(() => auth.user?.email || "—");

// ── Tab routing ─────────────────────────────────────────────────────
const tabKeys = ["overview", "settings"];
const activeIndex = ref(0);

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

// ── Theme ────────────────────────────────────────────────────────────
const themeStore = useThemeStore();
const isDark = computed(() => themeStore.isDark);
const handleThemeToggle = (value) => themeStore.applyTheme(value);

// ── Shared helper ────────────────────────────────────────────────────
const extractError = (err) =>
  err?.response?.data?.errors?.map((e) => e.errorMessage ?? e).join("; ") ||
  err?.response?.data?.message ||
  "Đã có lỗi xảy ra.";

// ── Edit profile dialog ──────────────────────────────────────────────
const showEditDialog = ref(false);
const editForm = ref({ fullName: "", email: "" });
const editSaving = ref(false);
const editError = ref("");

const openEditDialog = () => {
  editForm.value = {
    fullName: auth.user?.fullName ?? "",
    email: auth.user?.email ?? "",
  };
  editError.value = "";
  showEditDialog.value = true;
};

const saveProfile = async () => {
  editError.value = "";
  editSaving.value = true;
  try {
    await updateUser(auth.user.id, {
      fullName: editForm.value.fullName.trim(),
      email: editForm.value.email.trim() || null,
    });
    await auth.doRefreshToken();
    showEditDialog.value = false;
    toast.add({
      severity: "success",
      summary: "Đã lưu",
      detail: "Thông tin cá nhân đã được cập nhật.",
      life: 3000,
    });
  } catch (err) {
    editError.value = extractError(err);
  } finally {
    editSaving.value = false;
  }
};

// ── Change password dialog ───────────────────────────────────────────
const showPasswordDialog = ref(false);
const passwordForm = ref({
  currentPassword: "",
  newPassword: "",
  confirmNewPassword: "",
});
const passwordSaving = ref(false);
const passwordError = ref("");

const openPasswordDialog = () => {
  passwordForm.value = {
    currentPassword: "",
    newPassword: "",
    confirmNewPassword: "",
  };
  passwordError.value = "";
  showPasswordDialog.value = true;
};

const savePassword = async () => {
  passwordError.value = "";

  if (passwordForm.value.newPassword.length < 6) {
    passwordError.value = "Mật khẩu mới phải có ít nhất 6 ký tự.";
    return;
  }
  if (passwordForm.value.newPassword !== passwordForm.value.confirmNewPassword) {
    passwordError.value = "Xác nhận mật khẩu không khớp.";
    return;
  }

  passwordSaving.value = true;
  try {
    await changePassword({
      currentPassword: passwordForm.value.currentPassword,
      newPassword: passwordForm.value.newPassword,
    });
    showPasswordDialog.value = false;
    toast.add({
      severity: "success",
      summary: "Đổi mật khẩu thành công",
      detail: "Vui lòng đăng nhập lại.",
      life: 3000,
    });
    setTimeout(() => {
      auth.logout();
      router.push("/login");
    }, 1500);
  } catch (err) {
    passwordError.value = extractError(err);
  } finally {
    passwordSaving.value = false;
  }
};
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
      <!-- ── Overview tab ───────────────────────────────────────────── -->
      <prime-tab-panel header="Overview">
        <prime-card class="app-card tw:rounded-2xl tw:border">
          <template #content>
            <div class="tw:flex tw:flex-wrap tw:items-center tw:gap-6">
              <prime-avatar
                :label="userInitials"
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
              <div class="app-panel tw:rounded-xl tw:border tw:p-4">
                <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] app-text-subtle">
                  Shift
                </p>
                <p class="tw:mt-2 tw:text-lg tw:font-semibold">Morning</p>
                <p class="tw:text-xs app-text-muted">Starts 7:00 AM</p>
              </div>
              <div class="app-panel tw:rounded-xl tw:border tw:p-4">
                <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] app-text-subtle">
                  Team
                </p>
                <p class="tw:mt-2 tw:text-lg tw:font-semibold">Front Counter</p>
                <p class="tw:text-xs app-text-muted">Lead role</p>
              </div>
              <div class="app-panel tw:rounded-xl tw:border tw:p-4">
                <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] app-text-subtle">
                  Status
                </p>
                <p class="tw:mt-2 tw:text-lg tw:font-semibold">Active</p>
                <p class="tw:text-xs app-text-muted">Last login today</p>
              </div>
            </div>

            <div class="tw:mt-6 tw:flex tw:flex-wrap tw:gap-3">
              <prime-button
                label="Edit profile"
                icon="pi pi-user-edit"
                @click="openEditDialog"
              />
              <prime-button
                label="Manage password"
                icon="pi pi-lock"
                severity="secondary"
                outlined
                @click="openPasswordDialog"
              />
            </div>
          </template>
        </prime-card>
      </prime-tab-panel>

      <!-- ── Settings tab ──────────────────────────────────────────── -->
      <prime-tab-panel header="Settings">
        <prime-card class="app-card tw:rounded-2xl tw:border">
          <template #content>
            <div class="tw:flex tw:flex-wrap tw:items-center tw:justify-between tw:gap-4">
              <div>
                <p class="tw:text-base tw:font-semibold">Theme</p>
                <p class="tw:text-sm app-text-muted">
                  Toggle dark mode for the admin panel.
                </p>
              </div>
              <prime-toggle-switch
                :modelValue="isDark"
                @update:modelValue="handleThemeToggle"
              />
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

    <!-- ── Edit profile dialog ───────────────────────────────────────── -->
    <prime-dialog
      v-model:visible="showEditDialog"
      header="Edit profile"
      :style="{ width: '420px' }"
      modal
    >
      <div class="tw:space-y-4">
        <div class="tw:flex tw:flex-col tw:gap-1">
          <label class="tw:text-sm tw:font-medium">
            Full name <span class="tw:text-red-400">*</span>
          </label>
          <prime-input-text
            v-model="editForm.fullName"
            placeholder="Enter your full name"
            class="tw:w-full"
          />
        </div>

        <div class="tw:flex tw:flex-col tw:gap-1">
          <label class="tw:text-sm tw:font-medium">
            Email <span class="tw:text-xs app-text-muted">(optional)</span>
          </label>
          <prime-input-text
            v-model="editForm.email"
            placeholder="Enter your email"
            type="email"
            class="tw:w-full"
          />
        </div>

        <prime-message v-if="editError" severity="error" :closable="false">
          {{ editError }}
        </prime-message>
      </div>

      <template #footer>
        <prime-button
          label="Cancel"
          severity="secondary"
          outlined
          @click="showEditDialog = false"
        />
        <prime-button
          label="Save"
          icon="pi pi-check"
          :loading="editSaving"
          :disabled="!editForm.fullName.trim()"
          @click="saveProfile"
        />
      </template>
    </prime-dialog>

    <!-- ── Change password dialog ────────────────────────────────────── -->
    <prime-dialog
      v-model:visible="showPasswordDialog"
      header="Change password"
      :style="{ width: '420px' }"
      modal
    >
      <div class="tw:space-y-4">
        <div class="tw:flex tw:flex-col tw:gap-1">
          <label class="tw:text-sm tw:font-medium">
            Current password <span class="tw:text-red-400">*</span>
          </label>
          <prime-password
            v-model="passwordForm.currentPassword"
            placeholder="Enter current password"
            :feedback="false"
            toggleMask
            class="tw:w-full"
            input-class="tw:w-full"
          />
        </div>

        <div class="tw:flex tw:flex-col tw:gap-1">
          <label class="tw:text-sm tw:font-medium">
            New password <span class="tw:text-red-400">*</span>
          </label>
          <prime-password
            v-model="passwordForm.newPassword"
            placeholder="At least 6 characters"
            :feedback="true"
            toggleMask
            class="tw:w-full"
            input-class="tw:w-full"
          />
        </div>

        <div class="tw:flex tw:flex-col tw:gap-1">
          <label class="tw:text-sm tw:font-medium">
            Confirm new password <span class="tw:text-red-400">*</span>
          </label>
          <prime-password
            v-model="passwordForm.confirmNewPassword"
            placeholder="Repeat new password"
            :feedback="false"
            toggleMask
            class="tw:w-full"
            input-class="tw:w-full"
          />
        </div>

        <prime-message v-if="passwordError" severity="error" :closable="false">
          {{ passwordError }}
        </prime-message>
      </div>

      <template #footer>
        <prime-button
          label="Cancel"
          severity="secondary"
          outlined
          @click="showPasswordDialog = false"
        />
        <prime-button
          label="Change password"
          icon="pi pi-lock"
          :loading="passwordSaving"
          :disabled="
            !passwordForm.currentPassword ||
            !passwordForm.newPassword ||
            !passwordForm.confirmNewPassword
          "
          @click="savePassword"
        />
      </template>
    </prime-dialog>
  </section>
</template>
