<script setup>
import {
  getRoles,
  createRole,
  updateRole,
  deleteRole,
  getRolePermissions,
  setRolePermissions,
} from "@/services/role.service";

const { t } = useI18n();
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

// --- Column definitions ---
const columns = computed(() => [
  { key: 'role',    header: t('roles.table.colRole'),    width: '14rem' },
  { key: 'users',   header: t('roles.table.colUsers'),   width: '7rem' },
  { key: 'created', header: t('roles.table.colCreated') },
  { key: 'actions', header: t('roles.table.colActions'), width: '13rem', toggleable: false },
]);

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
  const key = `roles.permissionGroups.${name}`
  const translated = t(key)
  return translated !== key ? translated : name.charAt(0).toUpperCase() + name.slice(1)
};

const groupIcon = (name) => {
  const map = {
    menu: "ph:fork-knife-bold",
    order: "ph:receipt-bold",
    product: "ph:package-bold",
    staff: "ph:users-bold",
    table: "ic:round-table-bar",
    user: "ph:user-bold",
    role: "ph:shield-bold",
    permission: "ph:key-bold",
    expense: "ph:shopping-cart-bold",
    category: "ph:tag-bold",
    promotion: "ph:gift-bold",
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
  t('roles.errors.generic');

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

onBeforeUnmount(() => {
  clearTimeout(searchTimer.value);
});

// --- Add Role ---
const openAddDialog = () => {
  addForm.value = { name: "", description: "" };
  addError.value = "";
  showAddDialog.value = true;
};

const submitAddRole = async () => {
  if (!addForm.value.name.trim()) {
    addError.value = t('roles.errors.roleNameRequired');
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
    editError.value = t('roles.errors.roleNameRequired');
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

// ── Mobile drawer ──────────────────────────────────────────────────
const drawerRole = ref(null);
const drawerVisible = ref(false);
const openDrawer = (row) => { drawerRole.value = row; drawerVisible.value = true; };

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
        summary: t('roles.toast.permissionsUpdated'),
        detail: t('roles.toast.permissionsUpdatedDetailAffectsSelf', { name: roleName }),
        life: 8000,
      });
    } else {
      toast.add({
        severity: "success",
        summary: t('roles.toast.permissionsUpdated'),
        detail: t('roles.toast.permissionsUpdatedDetail', { name: roleName }),
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
          {{ t('roles.breadcrumb') }}
        </p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">{{ t('roles.title') }}</h1>
        <p class="tw:mt-2 tw:text-sm app-text-muted">
          {{ t('roles.subtitle') }}
        </p>
      </div>
      <prime-button severity="success" size="small" @click="openAddDialog">
        <iconify icon="ph:shield-plus-bold" />
        <span>{{ t('roles.addRole') }}</span>
      </prime-button>
    </div>

    <!-- ── Summary Stats ─────────────────────────────────────────── -->
    <div class="tw:grid tw:grid-cols-2 tw:gap-3 tw:md:grid-cols-4">
      <prime-card class="app-card tw:rounded-xl tw:border">
        <template #content>
          <p
            class="tw:text-[11px] tw:uppercase tw:tracking-[0.25em] app-text-subtle"
          >
            {{ t('roles.stats.totalRoles') }}
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
      :columns="columns"
      @page="(e) => loadRoles(e.page + 1)"
    >
      <template #toolbar-left>
        <prime-input-text
          v-model="search"
          :placeholder="t('roles.table.searchPlaceholder')"
          class="app-input tw:w-64"
        />
      </template>

      <template #mobile-card="{ data }">
        <div class="tw:rounded-xl tw:border tw:border-slate-200 tw:dark:border-white/10 tw:bg-white tw:dark:bg-white/5 tw:p-3 tw:flex tw:flex-col tw:gap-2">
          <div class="tw:flex tw:items-start tw:justify-between tw:gap-1">
            <div class="tw:flex tw:items-center tw:gap-2">
              <div :class="['tw:h-8 tw:w-8 tw:rounded-full tw:flex tw:items-center tw:justify-center tw:flex-shrink-0', roleBg(data.name), roleColor(data.name)]">
                <iconify :icon="roleIcon(data.name)" class="tw:text-sm" />
              </div>
              <span class="tw:font-semibold tw:text-sm">{{ data.name }}</span>
            </div>
          </div>
          <p v-if="data.description" class="tw:text-xs app-text-muted tw:line-clamp-2">{{ data.description }}</p>
          <div class="tw:border-t tw:border-slate-200 tw:dark:border-white/10 tw:pt-2">
            <prime-button severity="secondary" outlined size="small" fluid @click="openDrawer(data)">
              <iconify icon="ph:dots-three-bold" />
              <span>{{ t('common.moreActions') }}</span>
            </prime-button>
          </div>
        </div>
      </template>

      <template #col-role="{ data }">
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
            <p class="tw:text-xs app-text-muted">{{ data.description || "—" }}</p>
          </div>
        </div>
      </template>

      <template #col-users="{ data }">
        <span class="tw:text-sm">{{ data.userCount ?? "—" }}</span>
      </template>

      <template #col-created="{ data }">
        <span class="tw:text-xs app-text-muted">{{ formatDate(data.createdAt) }}</span>
      </template>

      <template #col-actions="{ data }">
        <div class="tw:flex tw:justify-end tw:gap-2">
          <prime-button
            severity="secondary"
            outlined
            size="small"
            v-tooltip.top="t('roles.tooltips.permissions')"
            @click="openPermissionsDialog(data)"
            :class="btnIcon"
          >
            <iconify icon="ph:key-bold" />
          </prime-button>
          <prime-button
            severity="secondary"
            outlined
            size="small"
            v-tooltip.top="t('roles.tooltips.edit')"
            @click="openEditDialog(data)"
            :class="btnIcon"
          >
            <iconify icon="ph:pencil-bold" />
          </prime-button>
          <prime-button
            severity="danger"
            outlined
            size="small"
            v-tooltip.top="t('roles.tooltips.delete')"
            @click="confirmDeleteRole = data"
            :class="btnIcon"
          >
            <iconify icon="ph:trash-bold" />
          </prime-button>
        </div>
      </template>
    </AppTable>

    <!-- ── Mobile action drawer ───────────────────────────────────── -->
    <prime-drawer
      v-model:visible="drawerVisible"
      position="bottom"
      :style="{ height: 'auto' }"
      :pt="{ root: { class: 'tw:rounded-t-2xl' } }"
    >
      <template #header>
        <div class="tw:flex tw:items-center tw:gap-2">
          <span class="tw:font-medium">{{ drawerRole?.name }}</span>
        </div>
      </template>
      <div v-if="drawerRole" class="tw:flex tw:flex-col tw:gap-2 tw:pb-4">
        <prime-button :label="t('roles.tooltips.permissions')" severity="secondary" outlined fluid @click="openPermissionsDialog(drawerRole); drawerVisible = false">
          <template #icon><iconify icon="ph:key-bold" /></template>
        </prime-button>
        <prime-button :label="t('roles.tooltips.edit')" severity="secondary" outlined fluid @click="openEditDialog(drawerRole); drawerVisible = false">
          <template #icon><iconify icon="ph:pencil-bold" /></template>
        </prime-button>
        <prime-button :label="t('roles.tooltips.delete')" severity="danger" outlined fluid @click="confirmDeleteRole = drawerRole; drawerVisible = false">
          <template #icon><iconify icon="ph:trash-bold" /></template>
        </prime-button>
      </div>
    </prime-drawer>

    <!-- ===== Add Role Dialog ===== -->
    <prime-dialog
      v-model:visible="showAddDialog"
      :header="t('roles.addDialog.header')"
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
            >{{ t('roles.addDialog.roleName') }} <span class="tw:text-red-400">*</span></label
          >
          <prime-input-text
            v-model="addForm.name"
            class="app-input tw:w-full"
            :placeholder="t('roles.addDialog.roleNamePlaceholder')"
          />
        </div>
        <div class="tw:space-y-1">
          <label class="tw:text-sm tw:font-medium">{{ t('roles.addDialog.description') }}</label>
          <prime-textarea
            v-model="addForm.description"
            class="app-input tw:w-full"
            :placeholder="t('roles.addDialog.descriptionPlaceholder')"
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
          <span>{{ t('common.cancel') }}</span>
        </prime-button>
        <prime-button
          severity="success"
          size="small"
          :loading="addLoading"
          @click="submitAddRole"
        >
          <iconify icon="ph:shield-plus-bold" />
          <span>{{ t('roles.addDialog.create') }}</span>
        </prime-button>
      </template>
    </prime-dialog>

    <!-- ===== Edit Role Dialog ===== -->
    <prime-dialog
      v-model:visible="showEditDialog"
      :header="t('roles.editDialog.header')"
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
            >{{ t('roles.editDialog.roleName') }} <span class="tw:text-red-400">*</span></label
          >
          <prime-input-text
            v-model="editForm.name"
            class="app-input tw:w-full"
          />
        </div>
        <div class="tw:space-y-1">
          <label class="tw:text-sm tw:font-medium">{{ t('roles.editDialog.description') }}</label>
          <prime-textarea
            v-model="editForm.description"
            class="app-input tw:w-full"
            :placeholder="t('roles.editDialog.descriptionPlaceholder')"
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
          <span>{{ t('common.cancel') }}</span>
        </prime-button>
        <prime-button
          severity="success"
          size="small"
          :loading="editLoading"
          @click="submitEditRole"
        >
          <iconify icon="ph:floppy-disk-bold" />
          <span>{{ t('roles.editDialog.save') }}</span>
        </prime-button>
      </template>
    </prime-dialog>

    <!-- ===== Delete Confirm Dialog ===== -->
    <prime-dialog
      :visible="!!confirmDeleteRole"
      :header="t('roles.deleteDialog.header')"
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
          {{ t('roles.deleteDialog.confirmText', { name: confirmDeleteRole?.name }) }}
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
          <span>{{ t('common.cancel') }}</span>
        </prime-button>
        <prime-button
          severity="danger"
          size="small"
          :loading="deleteLoading"
          @click="confirmAndDelete"
        >
          <iconify icon="ph:trash-bold" />
          <span>{{ t('roles.deleteDialog.delete') }}</span>
        </prime-button>
      </template>
    </prime-dialog>

    <!-- ===== Permissions Dialog ===== -->
    <prime-dialog
      v-model:visible="showPermissionsDialog"
      :header="t('roles.permissionsDialog.header', { name: permissionsRole?.name ?? '' })"
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
            <span>{{ t('roles.permissionsDialog.selectedSummary', { selected: selectedCount, total: permissions.length }) }}</span>
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
              {{ t('roles.permissionsDialog.clearAll') }}
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
          <span>{{ t('common.cancel') }}</span>
        </prime-button>
        <prime-button
          severity="success"
          size="small"
          :loading="permissionsSaving"
          :disabled="permissionsLoading"
          @click="savePermissions"
        >
          <iconify icon="ph:floppy-disk-bold" />
          <span>{{ t('roles.permissionsDialog.save') }}</span>
        </prime-button>
      </template>
    </prime-dialog>
  </section>
</template>
