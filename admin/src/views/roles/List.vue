<script setup>
import { computed, onMounted, ref, watch } from "vue";
import { onBeforeRouteLeave } from "vue-router";
import { useToast } from "primevue/usetoast";
import {
  getRoles,
  createRole,
  updateRole,
  deleteRole,
  getRolePermissions,
  setRolePermissions,
} from "@/services/role.service";
import AppTable from "@/components/AppTable.vue";
import { useTableCache } from "@/composables/useTableCache";
import { useAuthStore } from "@/stores/auth";
import { btnIcon } from "@/layout/ui";

const cache = useTableCache("roles");
const toast = useToast();
const auth = useAuthStore();

// --- Table state ---
const roles = ref([]);
const loading = ref(false);
const errorMessage = ref("");
const rows = ref(20);
const first = ref(0);
const totalRecords = ref(0);
const searchTimer = ref(null);

// --- Filters ---
const search = ref("");

// --- Summary stats ---
const stats = ref({ total: 0 });

// --- Dialog: Add Role ---
const showAddDialog = ref(false);
const addForm = ref({ name: "", description: "" });
const addLoading = ref(false);
const addError = ref("");

// --- Dialog: Edit Role ---
const showEditDialog = ref(false);
const editForm = ref({ id: null, name: "", description: "" });
const editLoading = ref(false);
const editError = ref("");

// --- Dialog: Delete Confirm ---
const confirmDeleteRole = ref(null);
const deleteLoading = ref(false);

// --- Dialog: Permissions ---
const showPermissionsDialog = ref(false);
const permissionsRole = ref(null);
const permissions = ref([]); // List<RolePermissionDto>
const permissionsLoading = ref(false);
const permissionsSaving = ref(false);
const permissionsError = ref("");

// Group permissions by the resource prefix (e.g. "order" from "order.create")
const permissionGroups = computed(() => {
  const groups = {};
  for (const p of permissions.value) {
    const prefix = p.value.split(".")[0];
    if (!groups[prefix]) groups[prefix] = [];
    groups[prefix].push(p);
  }
  return Object.entries(groups).map(([name, items]) => ({ name, items }));
});

const selectedCount = computed(
  () => permissions.value.filter((p) => p.assigned).length,
);

// --- Helpers ---
const groupLabel = (name) => {
  const map = {
    menu: "Menu",
    order: "Orders",
    product: "Products",
    staff: "Staff",
    table: "Tables",
  };
  return map[name] ?? name.charAt(0).toUpperCase() + name.slice(1);
};

const groupIcon = (name) => {
  const map = {
    menu: "ph:fork-knife-bold",
    order: "ph:receipt-bold",
    product: "ph:package-bold",
    staff: "ph:users-bold",
    table: "ph:table-bold",
  };
  return map[name] ?? "ph:key-bold";
};

const roleIcon = (name) => {
  const n = (name ?? "").toLowerCase();
  if (n.includes("admin")) return "ph:shield-star-bold";
  if (n.includes("staff")) return "ph:users-bold";
  if (n.includes("manage")) return "ph:gear-bold";
  return "ph:shield-bold";
};

const roleColor = (name) => {
  const n = (name ?? "").toLowerCase();
  if (n.includes("admin")) return "tw:text-red-400";
  if (n.includes("staff")) return "tw:text-blue-400";
  if (n.includes("manage")) return "tw:text-amber-400";
  return "tw:text-emerald-400";
};

const roleBg = (name) => {
  const n = (name ?? "").toLowerCase();
  if (n.includes("admin")) return "tw:bg-red-500/15";
  if (n.includes("staff")) return "tw:bg-blue-500/15";
  if (n.includes("manage")) return "tw:bg-amber-500/15";
  return "tw:bg-emerald-500/15";
};

const formatDate = (dateStr) =>
  dateStr ? new Date(dateStr).toLocaleDateString("vi-VN") : "—";

const extractError = (err) =>
  err?.response?.data?.errors?.join("; ") ||
  err?.response?.data?.message ||
  "Something went wrong.";

// --- Data Loading ---
const loadRoles = async (page = 1) => {
  loading.value = true;
  errorMessage.value = "";
  try {
    const res = await getRoles({
      page,
      pageSize: rows.value,
      search: search.value.trim() || undefined,
    });
    const data = res?.data ?? {};
    roles.value = data.items ?? [];
    totalRecords.value = data.total ?? 0;
    stats.value.total = data.total ?? 0;
  } catch (err) {
    errorMessage.value = extractError(err);
  } finally {
    loading.value = false;
  }
};

onMounted(() => {
  const cached = cache.restore();
  if (cached) {
    search.value = cached.search ?? "";
    rows.value = cached.rows ?? 20;
    first.value = cached.first ?? 0;
    const page = rows.value > 0 ? Math.floor(first.value / rows.value) + 1 : 1;
    loadRoles(page);
  } else {
    loadRoles();
  }
});

onBeforeRouteLeave(() => {
  cache.save({
    search: search.value,
    rows: rows.value,
    first: first.value,
  });
});

watch([search], () => {
  clearTimeout(searchTimer.value);
  searchTimer.value = setTimeout(() => {
    first.value = 0;
    loadRoles(1);
  }, 400);
});

// --- Add Role ---
const openAddDialog = () => {
  addForm.value = { name: "", description: "" };
  addError.value = "";
  showAddDialog.value = true;
};

const submitAddRole = async () => {
  if (!addForm.value.name.trim()) {
    addError.value = "Role name is required.";
    return;
  }
  addLoading.value = true;
  addError.value = "";
  try {
    await createRole(addForm.value);
    showAddDialog.value = false;
    loadRoles(1);
  } catch (err) {
    addError.value = extractError(err);
  } finally {
    addLoading.value = false;
  }
};

// --- Edit Role ---
const openEditDialog = (role) => {
  editForm.value = {
    id: role.id,
    name: role.name,
    description: role.description ?? "",
  };
  editError.value = "";
  showEditDialog.value = true;
};

const submitEditRole = async () => {
  if (!editForm.value.name.trim()) {
    editError.value = "Role name is required.";
    return;
  }
  editLoading.value = true;
  editError.value = "";
  try {
    await updateRole(editForm.value.id, {
      name: editForm.value.name,
      description: editForm.value.description,
    });
    showEditDialog.value = false;
    loadRoles(Math.floor(first.value / rows.value) + 1);
  } catch (err) {
    editError.value = extractError(err);
  } finally {
    editLoading.value = false;
  }
};

// --- Delete Role ---
const confirmAndDelete = async () => {
  if (!confirmDeleteRole.value) return;
  deleteLoading.value = true;
  try {
    await deleteRole(confirmDeleteRole.value.id);
    confirmDeleteRole.value = null;
    loadRoles(Math.floor(first.value / rows.value) + 1);
  } catch (err) {
    errorMessage.value = extractError(err);
    confirmDeleteRole.value = null;
  } finally {
    deleteLoading.value = false;
  }
};

// --- Permissions ---
const openPermissionsDialog = async (role) => {
  permissionsRole.value = role;
  permissionsError.value = "";
  permissions.value = [];
  showPermissionsDialog.value = true;
  permissionsLoading.value = true;
  try {
    const res = await getRolePermissions(role.id);
    permissions.value = res.data ?? [];
  } catch (err) {
    permissionsError.value = extractError(err);
  } finally {
    permissionsLoading.value = false;
  }
};

const togglePermission = (perm) => {
  perm.assigned = !perm.assigned;
};

const toggleGroup = (items) => {
  const allChecked = items.every((p) => p.assigned);
  items.forEach((p) => {
    p.assigned = !allChecked;
  });
};

const isGroupAllChecked = (items) => items.every((p) => p.assigned);
const isGroupPartChecked = (items) =>
  items.some((p) => p.assigned) && !items.every((p) => p.assigned);

const savePermissions = async () => {
  permissionsSaving.value = true;
  permissionsError.value = "";
  try {
    const selected = permissions.value
      .filter((p) => p.assigned)
      .map((p) => p.value);
    const roleName = permissionsRole.value.name;
    await setRolePermissions(permissionsRole.value.id, selected);
    showPermissionsDialog.value = false;

    const affectsCurrentUser = auth.user?.roles?.includes(roleName);
    if (affectsCurrentUser) {
      toast.add({
        severity: "warn",
        summary: "Permissions updated",
        detail: `Your role "${roleName}" was modified. Please log in again for changes to take effect.`,
        life: 8000,
      });
    } else {
      toast.add({
        severity: "success",
        summary: "Permissions updated",
        detail: `Permissions for "${roleName}" saved successfully.`,
        life: 4000,
      });
    }
  } catch (err) {
    permissionsError.value = extractError(err);
  } finally {
    permissionsSaving.value = false;
  }
};
</script>

<template>
  <section class="tw:space-y-6">
    <!-- ── Page Header ───────────────────────────────────────────── -->
    <div class="tw:flex tw:flex-wrap tw:items-end tw:justify-between tw:gap-4">
      <div>
        <p
          class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300"
        >
          Access control
        </p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">Role management</h1>
        <p class="tw:mt-2 tw:text-sm app-text-muted">
          Define roles and control what users can access.
        </p>
      </div>
      <prime-button severity="success" size="small" @click="openAddDialog">
        <iconify icon="ph:shield-plus-bold" />
        <span>Add role</span>
      </prime-button>
    </div>

    <!-- ── Summary Stats ─────────────────────────────────────────── -->
    <div class="tw:grid tw:grid-cols-2 tw:gap-3 tw:md:grid-cols-4">
      <prime-card class="app-card tw:rounded-xl tw:border">
        <template #content>
          <p
            class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] app-text-subtle"
          >
            Total roles
          </p>
          <p class="tw:mt-2 tw:text-2xl tw:font-semibold">{{ stats.total }}</p>
        </template>
      </prime-card>
    </div>

    <!-- ── Error Banner ──────────────────────────────────────────── -->
    <prime-alert
      v-if="errorMessage"
      severity="error"
      variant="accent"
      closable
      @close="errorMessage = ''"
      >{{ errorMessage }}</prime-alert
    >

    <!-- ── Table ──────────────────────────────────────────────────── -->
    <AppTable
      v-model:first="first"
      v-model:rows="rows"
      :value="roles"
      :loading="loading"
      :totalRecords="totalRecords"
      :rowsPerPageOptions="[10, 20, 50]"
      @page="(e) => loadRoles(e.page + 1)"
    >
      <template #toolbar-left>
        <prime-input-text
          v-model="search"
          placeholder="Search roles…"
          class="app-input tw:w-64"
        />
      </template>

      <!-- Role name + icon -->
      <prime-column header="Role" style="min-width: 14rem">
        <template #body="{ data }">
          <div class="tw:flex tw:items-center tw:gap-3">
            <div
              :class="[
                'tw:h-9 tw:w-9 tw:rounded-full tw:flex tw:items-center tw:justify-center tw:flex-shrink-0',
                roleBg(data.name),
                roleColor(data.name),
              ]"
            >
              <iconify :icon="roleIcon(data.name)" class="tw:text-base" />
            </div>
            <div>
              <p class="tw:text-sm tw:font-medium">{{ data.name }}</p>
              <p class="tw:text-xs app-text-muted">
                {{ data.description || "—" }}
              </p>
            </div>
          </div>
        </template>
      </prime-column>

      <!-- User count -->
      <prime-column header="Users" style="min-width: 7rem">
        <template #body="{ data }">
          <span class="tw:text-sm">{{ data.userCount ?? "—" }}</span>
        </template>
      </prime-column>

      <!-- Created date -->
      <prime-column header="Created">
        <template #body="{ data }">
          <span class="tw:text-xs app-text-muted">{{
            formatDate(data.createdAt)
          }}</span>
        </template>
      </prime-column>

      <!-- Actions -->
      <prime-column header="Actions" style="min-width: 13rem">
        <template #body="{ data }">
          <div class="tw:flex tw:gap-2">
            <prime-button
              severity="secondary"
              outlined
              size="small"
              v-tooltip.top="'Permissions'"
              @click="openPermissionsDialog(data)"
              :class="btnIcon"
            >
              <iconify icon="ph:key-bold" />
            </prime-button>

            <prime-button
              severity="secondary"
              outlined
              size="small"
              v-tooltip.top="'Edit'"
              @click="openEditDialog(data)"
              :class="btnIcon"
            >
              <iconify icon="ph:pencil-bold" />
            </prime-button>

            <prime-button
              severity="danger"
              outlined
              size="small"
              v-tooltip.top="'Delete'"
              @click="confirmDeleteRole = data"
              :class="btnIcon"
            >
              <iconify icon="ph:trash-bold" />
            </prime-button>
          </div>
        </template>
      </prime-column>
    </AppTable>

    <!-- ===== Add Role Dialog ===== -->
    <prime-dialog
      v-model:visible="showAddDialog"
      header="Add role"
      :modal="true"
      style="width: 26rem"
      :breakpoints="{ '640px': '95vw' }"
    >
      <div class="tw:space-y-4 tw:pt-2">
        <prime-alert
          v-if="addError"
          severity="error"
          variant="accent"
          :closable="false"
          >{{ addError }}</prime-alert
        >

        <div class="tw:space-y-1">
          <label class="tw:text-sm tw:font-medium"
            >Role name <span class="tw:text-red-400">*</span></label
          >
          <prime-input-text
            v-model="addForm.name"
            class="app-input tw:w-full"
            placeholder="e.g. Manager"
          />
        </div>
        <div class="tw:space-y-1">
          <label class="tw:text-sm tw:font-medium">Description</label>
          <prime-textarea
            v-model="addForm.description"
            class="app-input tw:w-full"
            placeholder="Describe what this role can do…"
            :rows="3"
            auto-resize
          />
        </div>
      </div>

      <template #footer>
        <prime-button
          severity="secondary"
          outlined
          size="small"
          @click="showAddDialog = false"
        >
          <iconify icon="ph:x-bold" />
          <span>Cancel</span>
        </prime-button>
        <prime-button
          severity="success"
          size="small"
          :loading="addLoading"
          @click="submitAddRole"
        >
          <iconify icon="ph:shield-plus-bold" />
          <span>Create</span>
        </prime-button>
      </template>
    </prime-dialog>

    <!-- ===== Edit Role Dialog ===== -->
    <prime-dialog
      v-model:visible="showEditDialog"
      header="Edit role"
      :modal="true"
      style="width: 26rem"
      :breakpoints="{ '640px': '95vw' }"
    >
      <div class="tw:space-y-4 tw:pt-2">
        <prime-alert
          v-if="editError"
          severity="error"
          variant="accent"
          :closable="false"
          >{{ editError }}</prime-alert
        >

        <div class="tw:space-y-1">
          <label class="tw:text-sm tw:font-medium"
            >Role name <span class="tw:text-red-400">*</span></label
          >
          <prime-input-text
            v-model="editForm.name"
            class="app-input tw:w-full"
          />
        </div>
        <div class="tw:space-y-1">
          <label class="tw:text-sm tw:font-medium">Description</label>
          <prime-textarea
            v-model="editForm.description"
            class="app-input tw:w-full"
            placeholder="Describe what this role can do…"
            :rows="3"
            auto-resize
          />
        </div>
      </div>

      <template #footer>
        <prime-button
          severity="secondary"
          outlined
          size="small"
          @click="showEditDialog = false"
        >
          <iconify icon="ph:x-bold" />
          <span>Cancel</span>
        </prime-button>
        <prime-button
          severity="success"
          size="small"
          :loading="editLoading"
          @click="submitEditRole"
        >
          <iconify icon="ph:floppy-disk-bold" />
          <span>Save</span>
        </prime-button>
      </template>
    </prime-dialog>

    <!-- ===== Delete Confirm Dialog ===== -->
    <prime-dialog
      :visible="!!confirmDeleteRole"
      header="Delete role"
      :modal="true"
      style="width: 24rem"
      :breakpoints="{ '640px': '95vw' }"
      @update:visible="
        (v) => {
          if (!v) confirmDeleteRole = null;
        }
      "
    >
      <div class="tw:pt-2">
        <p class="tw:text-sm app-text-muted">
          Delete role
          <strong class="tw:font-semibold">{{ confirmDeleteRole?.name }}</strong
          >? This action cannot be undone. Users assigned this role will lose
          its permissions.
        </p>
      </div>

      <template #footer>
        <prime-button
          severity="secondary"
          outlined
          size="small"
          @click="confirmDeleteRole = null"
        >
          <iconify icon="ph:x-bold" />
          <span>Cancel</span>
        </prime-button>
        <prime-button
          severity="danger"
          size="small"
          :loading="deleteLoading"
          @click="confirmAndDelete"
        >
          <iconify icon="ph:trash-bold" />
          <span>Delete</span>
        </prime-button>
      </template>
    </prime-dialog>

    <!-- ===== Permissions Dialog ===== -->
    <prime-dialog
      v-model:visible="showPermissionsDialog"
      :header="`Permissions — ${permissionsRole?.name ?? ''}`"
      :modal="true"
      style="width: 60rem"
      :breakpoints="{ '1280px': '90vw', '960px': '90vw', '640px': '95vw' }"
    >
      <div class="tw:pt-2 tw:space-y-4">
        <prime-alert
          v-if="permissionsError"
          severity="error"
          variant="accent"
          :closable="false"
          >{{ permissionsError }}</prime-alert
        >

        <!-- Loading skeleton -->
        <template v-if="permissionsLoading">
          <div v-for="i in 3" :key="i" class="tw:space-y-2">
            <prime-skeleton height="1.5rem" width="6rem" />
            <div class="tw:grid tw:grid-cols-2 tw:gap-2">
              <prime-skeleton v-for="j in 4" :key="j" height="2.25rem" />
            </div>
          </div>
        </template>

        <!-- Permission groups -->
        <template v-else>
          <div
            v-for="group in permissionGroups"
            :key="group.name"
            class="tw:rounded-xl tw:border app-card tw:p-4 tw:space-y-3"
          >
            <!-- Group header with select-all toggle -->
            <div class="tw:flex tw:items-center tw:justify-between">
              <div class="tw:flex tw:items-center tw:gap-2">
                <iconify
                  :icon="groupIcon(group.name)"
                  :class="['tw:text-base tw:text-emerald-400']"
                />
                <span class="tw:text-sm tw:font-semibold">{{
                  groupLabel(group.name)
                }}</span>
                <prime-badge
                  :value="`${group.items.filter((p) => p.assigned).length}/${group.items.length}`"
                  severity="secondary"
                  class="tw:scale-90"
                />
              </div>
              <prime-checkbox
                :model-value="isGroupAllChecked(group.items)"
                :indeterminate="isGroupPartChecked(group.items)"
                :binary="true"
                @change="toggleGroup(group.items)"
              />
            </div>

            <!-- Individual permissions -->
            <div
              class="tw:grid tw:grid-cols-1 tw:sm:grid-cols-2 tw:md:grid-cols-3 tw:lg:grid-cols-4 tw:gap-2"
            >
              <div
                v-for="perm in group.items"
                :key="perm.value"
                :class="[
                  'tw:flex tw:items-start tw:gap-3 tw:rounded-lg tw:p-2.5 tw:cursor-pointer tw:transition-colors',
                  perm.assigned
                    ? 'tw:bg-emerald-500/10 tw:border tw:border-emerald-500/30'
                    : 'tw:bg-transparent tw:border tw:border-transparent hover:tw:bg-white/5',
                ]"
                @click="togglePermission(perm)"
              >
                <prime-checkbox
                  :model-value="perm.assigned"
                  :binary="true"
                  class="tw:mt-0.5 tw:pointer-events-none"
                />
                <div class="tw:min-w-0">
                  <p class="tw:text-sm tw:font-medium tw:leading-tight">
                    {{ perm.description }}
                  </p>
                  <p
                    class="tw:text-[11px] app-text-subtle tw:font-mono tw:mt-0.5"
                  >
                    {{ perm.value }}
                  </p>
                </div>
              </div>
            </div>
          </div>

          <!-- Summary bar -->
          <div
            class="tw:flex tw:items-center tw:justify-between tw:text-sm app-text-muted"
          >
            <span>
              <strong class="tw:text-emerald-400">{{ selectedCount }}</strong>
              of {{ permissions.length }} permissions selected
            </span>
            <prime-button
              v-if="selectedCount > 0"
              severity="secondary"
              text
              size="small"
              @click="
                permissions.forEach((p) => {
                  p.assigned = false;
                })
              "
            >
              Clear all
            </prime-button>
          </div>
        </template>
      </div>

      <template #footer>
        <prime-button
          severity="secondary"
          outlined
          size="small"
          @click="showPermissionsDialog = false"
        >
          <iconify icon="ph:x-bold" />
          <span>Cancel</span>
        </prime-button>
        <prime-button
          severity="success"
          size="small"
          :loading="permissionsSaving"
          :disabled="permissionsLoading"
          @click="savePermissions"
        >
          <iconify icon="ph:floppy-disk-bold" />
          <span>Save permissions</span>
        </prime-button>
      </template>
    </prime-dialog>
  </section>
</template>
