<script setup>
import { onMounted, ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useToast } from "primevue/usetoast";
import {
  getUserById,
  updateUser,
  changeUserRole,
  activateUser,
  deactivateUser,
} from "@/services/user.service";
import { getRoles } from "@/services/role.service";
import { useAuthStore } from "@/stores/auth";
import { usePermission } from "@/composables/usePermission";

const route  = useRoute();
const router = useRouter();
const userId = route.params.id;
const toast  = useToast();
const auth   = useAuthStore();
const { can } = usePermission();

// ── State ──────────────────────────────────────────────────────────
const user         = ref(null);
const loading      = ref(false);
const errorMessage = ref("");

// Profile form
const profileForm = ref({ fullName: "", email: "" });
const saving      = ref(false);

// Role form
const selectedRole = ref("");
const roleLoading  = ref(false);
const roleOptions  = ref([]);

// Shared feedback
const successMessage = ref("");

// Activate / Deactivate
const toggleLoading          = ref(false);
const showDeactivateConfirm  = ref(false);

// ── Helpers ────────────────────────────────────────────────────────
const initials = (fullName) =>
  (fullName ?? "")
    .split(" ")
    .filter(Boolean)
    .map((w) => w[0])
    .slice(0, 2)
    .join("")
    .toUpperCase();

const formatDate = (d) =>
  d ? new Date(d).toLocaleDateString("vi-VN") : "—";

const roleSeverity = (role) =>
  role === "Admin" ? "danger" : role === "Staff" ? "info" : "secondary";

const extractError = (err) =>
  err?.response?.data?.errors?.map((e) => e.errorMessage ?? e).join("; ") ||
  err?.response?.data?.message ||
  "Something went wrong.";

// ── Load ──────────────────────────────────────────────────────────
const loadUser = async () => {
  loading.value      = true;
  errorMessage.value = "";
  try {
    const res    = await getUserById(userId);
    user.value   = res?.data;
    profileForm.value = {
      fullName: user.value.fullName  ?? "",
      email:    user.value.email     ?? "",
    };
    selectedRole.value = user.value.roles?.[0] ?? "Staff";
  } catch (err) {
    errorMessage.value = err?.response?.data?.message || "Failed to load user.";
  } finally {
    loading.value = false;
  }
};

const loadRoleOptions = async () => {
  try {
    const res = await getRoles({ page: 1, pageSize: 100 });
    roleOptions.value = (res.data.items ?? []).map((r) => ({
      label: r.name,
      value: r.name,
    }));
  } catch {
    // fallback: giữ rỗng, dropdown vẫn hiển thị được
  }
};

onMounted(() => {
  loadUser();
  loadRoleOptions();
});

// ── Save profile ──────────────────────────────────────────────────
const saveProfile = async () => {
  saving.value         = true;
  errorMessage.value   = "";
  successMessage.value = "";
  try {
    await updateUser(userId, {
      fullName: profileForm.value.fullName.trim(),
      email:    profileForm.value.email.trim() || null,
    });
    await loadUser();
    successMessage.value = "Profile updated successfully.";
    setTimeout(() => (successMessage.value = ""), 3000);
  } catch (err) {
    errorMessage.value = extractError(err);
  } finally {
    saving.value = false;
  }
};

// ── Change role ───────────────────────────────────────────────────
const saveRole = async () => {
  roleLoading.value    = true;
  errorMessage.value   = "";
  successMessage.value = "";
  try {
    await changeUserRole(userId, selectedRole.value);
    await loadUser();
    successMessage.value = "Role updated successfully.";
    setTimeout(() => (successMessage.value = ""), 3000);
    if (userId === auth.user?.id) {
      toast.add({
        severity: 'warn',
        summary: 'Role updated',
        detail: 'Your role was changed. Please log in again for it to take effect.',
        life: 8000,
      });
    }
  } catch (err) {
    errorMessage.value = extractError(err);
  } finally {
    roleLoading.value = false;
  }
};

// ── Toggle active ─────────────────────────────────────────────────
const doActivate = async () => {
  toggleLoading.value = true;
  errorMessage.value  = "";
  try {
    await activateUser(userId);
    await loadUser();
  } catch (err) {
    errorMessage.value = extractError(err);
  } finally {
    toggleLoading.value = false;
  }
};

const confirmDeactivate = async () => {
  toggleLoading.value      = true;
  showDeactivateConfirm.value = false;
  errorMessage.value       = "";
  try {
    await deactivateUser(userId);
    await loadUser();
  } catch (err) {
    errorMessage.value = extractError(err);
  } finally {
    toggleLoading.value = false;
  }
};
</script>

<template>
  <section class="tw:space-y-6">

    <!-- ── Header ─────────────────────────────────────────────────── -->
    <div class="tw:flex tw:flex-wrap tw:items-end tw:justify-between tw:gap-4">
      <div>
        <p class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300">Users</p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold tw:flex tw:items-center tw:gap-3">
          <span v-if="user">{{ user.username }}</span>
          <prime-skeleton v-else width="12rem" height="2rem" />
          <prime-tag
            v-if="user"
            :value="user.isActive ? 'Active' : 'Inactive'"
            :severity="user.isActive ? 'success' : 'danger'"
          />
        </h1>
        <p class="tw:mt-2 tw:text-sm app-text-muted">
          <template v-if="user">
            {{ user.fullName }} · Joined {{ formatDate(user.createdAt) }}
          </template>
          <prime-skeleton v-else width="16rem" height="1rem" />
        </p>
      </div>
      <prime-button
        severity="secondary"
        outlined
        size="small"
        @click="router.push({ name: 'user' })"
      >
        <iconify icon="ph:arrow-left-bold" />
        <span>Back to list</span>
      </prime-button>
    </div>

    <!-- ── Feedback ──────────────────────────────────────────────── -->
    <prime-alert
      v-if="successMessage"
      severity="success"
      variant="accent"
      :closable="false"
    >{{ successMessage }}</prime-alert>
    <prime-alert
      v-if="errorMessage"
      severity="error"
      variant="accent"
      closable
      @close="errorMessage = ''"
    >{{ errorMessage }}</prime-alert>

    <!-- ── Loading skeleton ───────────────────────────────────────── -->
    <div v-if="loading" class="tw:grid tw:grid-cols-1 tw:gap-6 tw:lg:grid-cols-3">
      <prime-card class="app-card tw:rounded-2xl tw:border">
        <template #content>
          <div class="tw:flex tw:flex-col tw:items-center tw:gap-3">
            <prime-skeleton shape="circle" size="5rem" />
            <prime-skeleton width="8rem" height="1.25rem" />
          </div>
          <prime-skeleton v-for="i in 3" :key="i" height="1rem" class="tw:mt-3" />
        </template>
      </prime-card>
      <prime-card class="app-card tw:rounded-2xl tw:border tw:lg:col-span-2">
        <template #content>
          <prime-skeleton v-for="i in 4" :key="i" height="2.5rem" class="tw:mb-4" />
        </template>
      </prime-card>
    </div>

    <template v-else-if="user">
      <div class="tw:grid tw:grid-cols-1 tw:gap-6 tw:lg:grid-cols-3">

        <!-- ── Left card: overview + status toggle ─────────────── -->
        <prime-card class="app-card tw:rounded-2xl tw:border tw:lg:col-span-1">
          <template #content>

            <!-- Avatar -->
            <div class="tw:flex tw:flex-col tw:items-center tw:gap-3 tw:mb-6">
              <div
                class="tw:h-20 tw:w-20 tw:rounded-full tw:flex tw:items-center tw:justify-center
                       tw:bg-emerald-500/20 tw:text-emerald-300 tw:text-2xl tw:font-bold"
              >
                {{ initials(user.fullName) }}
              </div>
              <div class="tw:text-center">
                <p class="tw:font-semibold tw:text-base">{{ user.username }}</p>
                <p class="tw:text-sm app-text-muted">{{ user.fullName }}</p>
              </div>
            </div>

            <!-- Info rows -->
            <div class="tw:space-y-3">
              <!-- Roles -->
              <div class="tw:flex tw:justify-between tw:items-center tw:text-sm tw:gap-2">
                <span class="app-text-muted tw:shrink-0">Role</span>
                <div class="tw:flex tw:gap-1 tw:flex-wrap tw:justify-end">
                  <prime-tag
                    v-for="role in user.roles"
                    :key="role"
                    :value="role"
                    :severity="roleSeverity(role)"
                  />
                  <span v-if="!user.roles?.length" class="app-text-muted">—</span>
                </div>
              </div>

              <!-- Status -->
              <div class="tw:flex tw:justify-between tw:items-center tw:text-sm">
                <span class="app-text-muted">Status</span>
                <prime-tag
                  :value="user.isActive ? 'Active' : 'Inactive'"
                  :severity="user.isActive ? 'success' : 'danger'"
                />
              </div>

              <!-- Email -->
              <div class="tw:flex tw:justify-between tw:text-sm tw:gap-2">
                <span class="app-text-muted tw:shrink-0">Email</span>
                <span class="tw:font-medium tw:text-right tw:truncate">
                  {{ user.email || '—' }}
                </span>
              </div>

              <!-- Created -->
              <div class="tw:flex tw:justify-between tw:text-sm">
                <span class="app-text-muted">Joined</span>
                <span class="tw:font-medium">{{ formatDate(user.createdAt) }}</span>
              </div>
            </div>

            <!-- Activate / Deactivate -->
            <div v-if="can('user.deactivate')" class="tw:mt-6 tw:pt-5 tw:border-t">
              <prime-button
                v-if="user.isActive"
                severity="danger"
                outlined
                size="small"
                class="tw:w-full"
                :loading="toggleLoading"
                @click="showDeactivateConfirm = true"
              >
                <iconify icon="ph:prohibit-bold" />
                <span>Deactivate account</span>
              </prime-button>
              <prime-button
                v-else
                severity="success"
                outlined
                size="small"
                class="tw:w-full"
                :loading="toggleLoading"
                @click="doActivate"
              >
                <iconify icon="ph:check-circle-bold" />
                <span>Activate account</span>
              </prime-button>
            </div>

          </template>
        </prime-card>

        <!-- ── Right card: edit form ───────────────────────────── -->
        <prime-card class="app-card tw:rounded-2xl tw:border tw:lg:col-span-2">
          <template #content>

            <!-- ── Profile section ───────────────────────────── -->
            <p class="tw:text-sm tw:font-semibold tw:mb-5">Profile</p>

            <div class="tw:space-y-5">
              <!-- Username (readonly) -->
              <div class="tw:space-y-1.5">
                <label class="tw:text-sm tw:font-medium">Username</label>
                <prime-input-text
                  :model-value="user.username"
                  class="app-input tw:w-full tw:opacity-50 tw:cursor-not-allowed"
                  readonly
                />
              </div>

              <!-- Full name -->
              <div class="tw:space-y-1.5">
                <label class="tw:text-sm tw:font-medium">
                  Full name <span class="tw:text-red-400">*</span>
                </label>
                <prime-input-text
                  v-model="profileForm.fullName"
                  class="app-input tw:w-full"
                  placeholder="e.g. Nguyen Van A"
                />
              </div>

              <!-- Email -->
              <div class="tw:space-y-1.5">
                <label class="tw:text-sm tw:font-medium">
                  Email
                  <span class="app-text-muted tw:font-normal">(optional)</span>
                </label>
                <prime-input-text
                  v-model="profileForm.email"
                  class="app-input tw:w-full"
                  placeholder="user@example.com"
                />
              </div>
            </div>

            <div
              v-if="can('user.update')"
              class="tw:flex tw:justify-end tw:gap-3 tw:mt-5 tw:pt-5 tw:border-t"
            >
              <prime-button
                severity="secondary"
                outlined
                size="small"
                @click="loadUser"
              >
                <iconify icon="ph:arrow-counter-clockwise-bold" />
                <span>Reset</span>
              </prime-button>
              <prime-button
                severity="success"
                size="small"
                :loading="saving"
                @click="saveProfile"
              >
                <iconify icon="ph:check-bold" />
                <span>Save changes</span>
              </prime-button>
            </div>

            <!-- ── Role section ───────────────────────────────── -->
            <div class="tw:mt-8 tw:pt-6 tw:border-t">
              <p class="tw:text-sm tw:font-semibold tw:mb-4">Role</p>

              <div class="tw:flex tw:items-end tw:gap-3">
                <div class="tw:space-y-1.5 tw:flex-1">
                  <label class="tw:text-sm tw:font-medium">Assign role</label>
                  <prime-select
                    v-model="selectedRole"
                    :options="roleOptions"
                    optionLabel="label"
                    optionValue="value"
                    class="app-input tw:w-full"
                  />
                </div>
                <prime-button
                  v-if="can('user.update')"
                  severity="warning"
                  size="small"
                  :loading="roleLoading"
                  @click="saveRole"
                >
                  <iconify icon="ph:shield-check-bold" />
                  <span>Change role</span>
                </prime-button>
              </div>
              <p class="tw:text-xs app-text-muted tw:mt-2">
                Changing the role will take effect on the user's next login.
              </p>
            </div>

          </template>
        </prime-card>

      </div>
    </template>

    <!-- ── Not found ──────────────────────────────────────────────── -->
    <prime-card v-else class="app-card tw:rounded-2xl tw:border">
      <template #content>
        <div class="tw:flex tw:flex-col tw:items-center tw:py-10 app-text-muted">
          <iconify icon="ph:user-x-bold" class="tw:text-3xl tw:mb-2" />
          <p class="tw:text-sm">User not found.</p>
        </div>
      </template>
    </prime-card>

    <!-- ── Deactivate Confirm Dialog ──────────────────────────────── -->
    <prime-dialog
      v-model:visible="showDeactivateConfirm"
      header="Deactivate account"
      :modal="true"
      style="width: 24rem"
    >
      <div class="tw:pt-2">
        <p class="tw:text-sm app-text-muted">
          Deactivate <strong class="tw:font-semibold">{{ user?.username }}</strong>?
          They will be immediately logged out and cannot log in until reactivated.
        </p>
      </div>
      <template #footer>
        <prime-button
          severity="secondary"
          outlined
          size="small"
          @click="showDeactivateConfirm = false"
        >
          <iconify icon="ph:x-bold" />
          <span>Cancel</span>
        </prime-button>
        <prime-button
          severity="danger"
          size="small"
          :loading="toggleLoading"
          @click="confirmDeactivate"
        >
          <iconify icon="ph:prohibit-bold" />
          <span>Deactivate</span>
        </prime-button>
      </template>
    </prime-dialog>

  </section>
</template>
