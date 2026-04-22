<script setup>
import {
  listTables,
  createTable,
  updateTable,
  toggleActive,
  deleteTable,
  markAvailable,
  closeSession,
  regenerateQrToken,
} from "@/services/table.service";
import { listZones } from "@/services/zone.service";
import QRCode from "qrcode";

const cache = useTableCache("tables");
const { can } = usePermission();
const { t } = useI18n();

// ── Table state ────────────────────────────────────────────────────
const loading = ref(false);
const errorMessage = ref("");
const tables = ref([]);
const rows = ref(20);
const first = ref(0);

// ── Zones ───────────────────────────────────────────────────────────
const zones = ref([]);
const loadZones = async () => {
  try {
    const res = await listZones();
    zones.value = res.data ?? [];
  } catch {
    // non-critical, ignore
  }
};
const zoneOptions = computed(() =>
  zones.value.map((z) => ({ label: z.name, value: z.id }))
);

// ── Filters ────────────────────────────────────────────────────────
const statusFilter = ref(null);
const activeFilter = ref(null);
const zoneFilter = ref(null);

const statusOptions = computed(() => [
  { label: t("tables.status.Available"), value: "Available" },
  { label: t("tables.status.Occupied"),  value: "Occupied" },
  { label: t("tables.status.Cleaning"),  value: "Cleaning" },
]);

const activeOptions = computed(() => [
  { label: t("tables.activeStatus.active"),   value: true },
  { label: t("tables.activeStatus.inactive"), value: false },
]);

// ── Summary ────────────────────────────────────────────────────────
const summary = computed(() => {
  const all = tables.value;
  return {
    total: all.length,
    available: all.filter((t) => t.status === "Available").length,
    occupied: all.filter((t) => t.status === "Occupied").length,
    cleaning: all.filter((t) => t.status === "Cleaning").length,
  };
});

// ── Filtered rows (client-side) ─────────────────────────────────
const filtered = computed(() => {
  return tables.value.filter((t) => {
    if (statusFilter.value !== null && t.status !== statusFilter.value)
      return false;
    if (activeFilter.value !== null && t.isActive !== activeFilter.value)
      return false;
    if (zoneFilter.value !== null && t.zoneId !== zoneFilter.value)
      return false;
    return true;
  });
});

const pagedRows = computed(() => {
  return filtered.value.slice(first.value, first.value + rows.value);
});

// ── Load ───────────────────────────────────────────────────────────
const load = async () => {
  loading.value = true;
  errorMessage.value = "";
  try {
    const res = await listTables();
    tables.value = res.data ?? [];
  } catch (err) {
    errorMessage.value = err?.response?.data?.title ?? t("tables.error.loadFailed");
  } finally {
    loading.value = false;
  }
};

// ── Actions ────────────────────────────────────────────────────────
const handleToggleActive = async (row) => {
  try {
    await toggleActive(row.id);
    await load();
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.title ?? t("tables.error.updateFailed", { code: row.code });
  }
};

const handleMarkAvailable = async (row) => {
  try {
    await markAvailable(row.id);
    await load();
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.title ??
      t("tables.error.markAvailableFailed", { code: row.code });
  }
};

const handleCloseSession = async (row) => {
  if (!row.activeSessionId) return;
  try {
    await closeSession(row.activeSessionId);
    await load();
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.title ??
      t("tables.error.closeSessionFailed", { code: row.code });
  }
};

const handleDelete = async (row) => {
  try {
    await deleteTable(row.id);
    await load();
  } catch (err) {
    errorMessage.value =
      err?.response?.data?.title ?? t("tables.error.deleteFailed", { code: row.code });
  }
};

// ── Add table dialog ───────────────────────────────────────────────
const showAddDialog = ref(false);
const newCode = ref("");
const newZoneId = ref(null);
const addError = ref("");
const addLoading = ref(false);

const openAddDialog = () => {
  newCode.value = "";
  newZoneId.value = null;
  addError.value = "";
  showAddDialog.value = true;
};

const handleAddTable = async () => {
  if (!newCode.value.trim()) {
    addError.value = t("tables.validation.codeRequired");
    return;
  }
  addLoading.value = true;
  addError.value = "";
  try {
    await createTable(newCode.value.trim(), newZoneId.value);
    await load();
    showAddDialog.value = false;
    newCode.value = "";
  } catch (err) {
    addError.value =
      err?.response?.data?.errors?.join(", ") ||
      err?.response?.data?.title ||
      t("tables.error.createFailed");
  } finally {
    addLoading.value = false;
  }
};

// ── Edit dialog ────────────────────────────────────────────────────
const showEditDialog = ref(false);
const editRow = ref(null);
const editCode = ref("");
const editZoneId = ref(null);
const editError = ref("");
const editLoading = ref(false);

const openEditDialog = (row) => {
  editRow.value = row;
  editCode.value = row.code;
  editZoneId.value = row.zoneId ?? null;
  editError.value = "";
  showEditDialog.value = true;
};

const handleEditTable = async () => {
  if (!editCode.value.trim()) {
    editError.value = t("tables.validation.codeRequired");
    return;
  }
  editLoading.value = true;
  editError.value = "";
  try {
    await updateTable(editRow.value.id, {
      code: editCode.value.trim(),
      zoneId: editZoneId.value,
    });
    await load();
    showEditDialog.value = false;
  } catch (err) {
    editError.value =
      err?.response?.data?.errors?.join(", ") ||
      err?.response?.data?.title ||
      t("tables.error.editFailed");
  } finally {
    editLoading.value = false;
  }
};

// ── QR dialog ──────────────────────────────────────────────────────
const showQrDialog = ref(false);
const qrRow = ref(null);
const qrUrl = ref("");
const qrCardDataUrl = ref("");
const qrCardLoading = ref(false);
const cardColor = ref("#10b981");
const qrStyle = ref("square");
const qrStyleOptions = [
  { value: "square", label: "■" },
  { value: "rounded", label: "▣" },
  { value: "dots", label: "●" },
];
const markerBorder = ref("rounded");
const markerBorderOptions = [
  { value: "square", label: "□" },
  { value: "rounded", label: "▢" },
  { value: "circle", label: "○" },
];
const markerCenter = ref("rounded");
const markerCenterOptions = [
  { value: "square", label: "■" },
  { value: "rounded", label: "▣" },
  { value: "dot", label: "●" },
];
const errorLevel = ref("L");
const errorLevelOptions = ["L", "M", "Q", "H"];
const qrCustomizerRef = ref(null);

// ── Card canvas helpers ─────────────────────────────────────────────
const loadImage = (src) =>
  new Promise((resolve, reject) => {
    const img = new Image();
    img.onload = () => resolve(img);
    img.onerror = reject;
    img.src = src;
  });

const rrect = (ctx, x, y, w, h, r) => {
  ctx.beginPath();
  ctx.moveTo(x + r, y);
  ctx.lineTo(x + w - r, y);
  ctx.quadraticCurveTo(x + w, y, x + w, y + r);
  ctx.lineTo(x + w, y + h - r);
  ctx.quadraticCurveTo(x + w, y + h, x + w - r, y + h);
  ctx.lineTo(x + r, y + h);
  ctx.quadraticCurveTo(x, y + h, x, y + h - r);
  ctx.lineTo(x, y + r);
  ctx.quadraticCurveTo(x, y, x + r, y);
  ctx.closePath();
};

const generateCardCanvas = async (rowOverride = null, urlOverride = null) => {
  const row = rowOverride ?? qrRow.value;
  const url = urlOverride ?? qrUrl.value;
  const W = 360, H = 480, S = 2;
  const canvas = document.createElement("canvas");
  canvas.width = W * S;
  canvas.height = H * S;
  const ctx = canvas.getContext("2d");
  ctx.scale(S, S);

  // ── Background ──────────────────────────────────────────────────
  ctx.fillStyle = "#ffffff";
  rrect(ctx, 0, 0, W, H, 20);
  ctx.fill();

  // ── Header ──────────────────────────────────────────────────────
  ctx.fillStyle = cardColor.value;
  ctx.beginPath();
  ctx.moveTo(20, 0);
  ctx.lineTo(W - 20, 0);
  ctx.quadraticCurveTo(W, 0, W, 20);
  ctx.lineTo(W, 88);
  ctx.lineTo(0, 88);
  ctx.lineTo(0, 20);
  ctx.quadraticCurveTo(0, 0, 20, 0);
  ctx.closePath();
  ctx.fill();

  // Logo circle (white bg)
  ctx.fillStyle = "rgba(255,255,255,0.2)";
  ctx.beginPath();
  ctx.arc(50, 44, 30, 0, Math.PI * 2);
  ctx.fill();
  const logo = await loadImage("/logo.svg");
  ctx.drawImage(logo, 24, 18, 52, 52);

  // Brand text — vertically centered in header (H=88, center=44)
  // Line 1 baseline y=38, line 2 baseline y=57 → midpoint ≈ 44
  ctx.fillStyle = "#ffffff";
  ctx.font = "bold 20px system-ui, -apple-system, sans-serif";
  ctx.fillText("5AM Coffee", 96, 38);
  ctx.font = "13px system-ui, -apple-system, sans-serif";
  ctx.fillStyle = "rgba(255,255,255,0.88)";
  ctx.fillText("Quét mã để đặt món", 96, 57);

  // Zone name — vertically centered in header (badge center at y=44)
  if (row.zoneName) {
    ctx.fillStyle = "rgba(255,255,255,0.25)";
    rrect(ctx, W - 104, 30, 94, 28, 8);
    ctx.fill();
    ctx.fillStyle = "#fff";
    ctx.font = "12px system-ui, sans-serif";
    ctx.textAlign = "right";
    ctx.fillText(row.zoneName, W - 14, 49);
    ctx.textAlign = "left";
  }

  // ── QR Code ─────────────────────────────────────────────────────
  const QR = 220;
  const qx = (W - QR) / 2, qy = 126;

  // QR shadow + white bg
  ctx.shadowColor = "rgba(0,0,0,0.10)";
  ctx.shadowBlur = 16;
  ctx.fillStyle = "#fff";
  rrect(ctx, qx - 10, qy - 10, QR + 20, QR + 20, 14);
  ctx.fill();
  ctx.shadowBlur = 0;

  // Generate QR — draw modules manually for custom style
  const qrData = QRCode.create(url, { errorCorrectionLevel: errorLevel.value });
  const mods = qrData.modules;
  const sz = mods.size;
  const cell = QR / sz;

  // white background
  ctx.fillStyle = "#ffffff";
  ctx.fillRect(qx, qy, QR, QR);

  // Finder pattern zones (top-left, top-right, bottom-left, each 7×7)
  const isFinderZone = (r, c) =>
    (r < 7 && c < 7) || (r < 7 && c >= sz - 7) || (r >= sz - 7 && c < 7);

  // Draw one finder pattern with configurable border + center styles
  const drawFinder = (startR, startC) => {
    const fx = qx + startC * cell, fy = qy + startR * cell;

    // Outer border
    ctx.fillStyle = "#1e293b";
    if (markerBorder.value === "circle") {
      ctx.beginPath();
      ctx.arc(fx + 3.5 * cell, fy + 3.5 * cell, 3.5 * cell, 0, Math.PI * 2);
      ctx.fill();
    } else if (markerBorder.value === "rounded") {
      rrect(ctx, fx, fy, 7 * cell, 7 * cell, cell * 1.1);
      ctx.fill();
    } else {
      ctx.fillRect(fx, fy, 7 * cell, 7 * cell);
    }

    // White inner area
    ctx.fillStyle = "#ffffff";
    if (markerBorder.value === "circle") {
      ctx.beginPath();
      ctx.arc(fx + 3.5 * cell, fy + 3.5 * cell, 2.5 * cell, 0, Math.PI * 2);
      ctx.fill();
    } else if (markerBorder.value === "rounded") {
      rrect(ctx, fx + cell, fy + cell, 5 * cell, 5 * cell, cell * 0.65);
      ctx.fill();
    } else {
      ctx.fillRect(fx + cell, fy + cell, 5 * cell, 5 * cell);
    }

    // Center marker
    ctx.fillStyle = "#1e293b";
    if (markerCenter.value === "dot") {
      ctx.beginPath();
      ctx.arc(fx + 3.5 * cell, fy + 3.5 * cell, 1.5 * cell, 0, Math.PI * 2);
      ctx.fill();
    } else if (markerCenter.value === "rounded") {
      rrect(ctx, fx + 2 * cell, fy + 2 * cell, 3 * cell, 3 * cell, cell * 0.5);
      ctx.fill();
    } else {
      ctx.fillRect(fx + 2 * cell, fy + 2 * cell, 3 * cell, 3 * cell);
    }
  };
  drawFinder(0, 0);
  drawFinder(0, sz - 7);
  drawFinder(sz - 7, 0);

  // Draw data modules (skip finder zones)
  ctx.fillStyle = "#1e293b";
  for (let r = 0; r < sz; r++) {
    for (let c = 0; c < sz; c++) {
      if (isFinderZone(r, c)) continue;
      if (!mods.data[r * sz + c]) continue;
      const mx = qx + c * cell, my = qy + r * cell;
      if (qrStyle.value === "dots") {
        ctx.beginPath();
        ctx.arc(mx + cell / 2, my + cell / 2, cell * 0.42, 0, Math.PI * 2);
        ctx.fill();
      } else if (qrStyle.value === "rounded") {
        rrect(ctx, mx + cell * 0.08, my + cell * 0.08, cell * 0.84, cell * 0.84, cell * 0.28);
        ctx.fill();
      } else {
        ctx.fillRect(mx, my, cell, cell);
      }
    }
  }

  // ── Table code ──────────────────────────────────────────────────
  const infoY = qy + QR + 44;
  ctx.fillStyle = "#94a3b8";
  ctx.font = "11px system-ui, sans-serif";
  ctx.textAlign = "center";
  ctx.fillText("SỐ BÀN", W / 2, infoY);

  ctx.fillStyle = "#1e293b";
  ctx.font = `bold 28px system-ui, sans-serif`;
  ctx.fillText(row.code, W / 2, infoY + 34);
  ctx.textAlign = "left";

  return canvas;
};

const openQrDialog = async (row) => {
  qrRow.value = row;
  qrUrl.value = `${import.meta.env.VITE_ORDERING_BASE_URL ?? ""}/order/${row.id}?token=${row.qrToken}`;
  qrCardDataUrl.value = "";
  showQrDialog.value = true;
  await nextTick();
  qrCardLoading.value = true;
  try {
    const canvas = await generateCardCanvas();
    qrCardDataUrl.value = canvas.toDataURL("image/png");
  } finally {
    qrCardLoading.value = false;
  }
};

const downloadQr = async () => {
  qrCardLoading.value = true;
  try {
    const canvas = await generateCardCanvas();
    const link = document.createElement("a");
    link.download = `qr-${qrRow.value.code}.png`;
    link.href = canvas.toDataURL("image/png");
    link.click();
  } finally {
    qrCardLoading.value = false;
  }
};

const refreshCard = async () => {
  if (!qrRow.value) return;
  qrCardLoading.value = true;
  try {
    const canvas = await generateCardCanvas();
    qrCardDataUrl.value = canvas.toDataURL("image/png");
  } finally {
    qrCardLoading.value = false;
  }
};

watch(cardColor, () => { refreshCard(); });
watch(qrStyle, () => { refreshCard(); });
watch(markerBorder, () => { refreshCard(); });
watch(markerCenter, () => { refreshCard(); });
watch(errorLevel, () => { refreshCard(); });

const regenerateLoading = ref(false);
const regenerateError = ref("");

const handleRegenerateQrToken = async () => {
  regenerateLoading.value = true;
  regenerateError.value = "";
  try {
    const res = await regenerateQrToken(qrRow.value.id);
    const updated = res.data;
    // Update the row in the tables list so re-opening uses the new token
    const idx = tables.value.findIndex((t) => t.id === updated.id);
    if (idx !== -1) tables.value[idx] = updated;
    await openQrDialog(updated);
  } catch (err) {
    regenerateError.value = err?.response?.data?.title ?? t("tables.error.regenerateQrFailed");
  } finally {
    regenerateLoading.value = false;
  }
};

// ── Bulk QR export ─────────────────────────────────────────────────
const exportAllQrLoading = ref(false)
const exportAllQrProgress = ref(0)
const exportAllQrTotal = ref(0)
const toast = useToast()

const exportAllQrPdf = async () => {
  const tablesToExport = filtered.value
  if (!tablesToExport.length) return

  exportAllQrLoading.value = true
  exportAllQrProgress.value = 0
  exportAllQrTotal.value = tablesToExport.length

  try {
    const { jsPDF } = await import('jspdf')
    // A4 portrait: 210×297mm, 2 cols × 3 rows = 6 cards per page
    // Margins: 8mm, gaps: 4mm
    // Height per card: (297 - 16 - 8) / 3 = 91mm → width = 91 * 3/4 ≈ 68mm
    const pdf = new jsPDF({ format: 'a4', unit: 'mm' })
    const MARGIN = 8, COL_GAP = 4, ROW_GAP = 4
    const COLS = 2, ROWS = 3
    const cardH = (297 - MARGIN * 2 - ROW_GAP * (ROWS - 1)) / ROWS  // ≈ 91.7mm
    const cardW = cardH * (3 / 4)                                      // ≈ 68.7mm
    const totalW = COLS * cardW + (COLS - 1) * COL_GAP
    const startX = (210 - totalW) / 2  // center horizontally

    for (let i = 0; i < tablesToExport.length; i++) {
      const row = tablesToExport[i]
      const url = `${import.meta.env.VITE_ORDERING_BASE_URL ?? ''}/order/${row.id}?token=${row.qrToken}`
      const canvas = await generateCardCanvas(row, url)

      const pageIdx = Math.floor(i / (COLS * ROWS))
      const slotIdx = i % (COLS * ROWS)
      const col = slotIdx % COLS
      const rowIdx = Math.floor(slotIdx / COLS)

      if (i > 0 && slotIdx === 0) pdf.addPage()

      const x = startX + col * (cardW + COL_GAP)
      const y = MARGIN + rowIdx * (cardH + ROW_GAP)
      pdf.addImage(canvas.toDataURL('image/png'), 'PNG', x, y, cardW, cardH)
      exportAllQrProgress.value = i + 1
    }

    pdf.save('qr-tables.pdf')
  } catch (err) {
    toast.add({ severity: 'error', summary: t('tables.error.exportFailed'), life: 4000 })
  } finally {
    exportAllQrLoading.value = false
    exportAllQrProgress.value = 0
    exportAllQrTotal.value = 0
  }
}

// ── Tag helpers ────────────────────────────────────────────────────
const statusTag = (status) => {
  switch (status) {
    case "Available": return { label: t("tables.status.Available"), severity: "success" };
    case "Occupied":  return { label: t("tables.status.Occupied"),  severity: "info" };
    case "Cleaning":  return { label: t("tables.status.Cleaning"),  severity: "warn" };
    default:          return { label: status,                       severity: "secondary" };
  }
};

const activeTag = (isActive) =>
  isActive
    ? { label: t("tables.activeStatus.active"),   severity: "success" }
    : { label: t("tables.activeStatus.inactive"), severity: "danger" };

// ── Filter helpers ─────────────────────────────────────────────────
const activeFilterCount = computed(() => {
  let n = 0;
  if (statusFilter.value !== null) n++;
  if (activeFilter.value !== null) n++;
  if (zoneFilter.value !== null) n++;
  return n;
});

const clearFilters = () => {
  statusFilter.value = null;
  activeFilter.value = null;
  zoneFilter.value = null;
  first.value = 0;
};

const filterPanel = ref(null);
const filterDrawerVisible = ref(false);

const isMobile = ref(window.innerWidth < 640);
const _onResize = () => { isMobile.value = window.innerWidth < 640; };
onMounted(() => window.addEventListener('resize', _onResize));
onUnmounted(() => window.removeEventListener('resize', _onResize));

const openFilter = (e) => {
  if (isMobile.value) filterDrawerVisible.value = true;
  else filterPanel.value.toggle(e);
};

// ── Cache ──────────────────────────────────────────────────────────
onMounted(() => {
  const cached = cache.restore();
  if (cached) {
    rows.value = cached.rows ?? 20;
    first.value = cached.first ?? 0;
    statusFilter.value = cached.statusFilter ?? null;
    activeFilter.value = cached.activeFilter ?? null;
    zoneFilter.value = cached.zoneFilter ?? null;
  }
  load();
  loadZones();
});

onBeforeRouteLeave(() => {
  cache.save({
    rows: rows.value,
    first: first.value,
    statusFilter: statusFilter.value,
    activeFilter: activeFilter.value,
    zoneFilter: zoneFilter.value,
  });
});

watch([statusFilter, activeFilter, zoneFilter], () => {
  first.value = 0;
});

// ── Mobile action drawer ───────────────────────────────────────────
const drawerTable = ref(null);
const drawerVisible = ref(false);

const openDrawer = (row) => {
  drawerTable.value = row;
  drawerVisible.value = true;
};

// ── Widget visibility ──────────────────────────────────────────────
const { isVisible: wVisible, toggle: wToggle, hiddenCount: wHidden, widgets: wDefs, colsPerRow: wCols, setColsPerRow: wSetCols } =
  useWidgetSettings('tables', [
    { id: 'total',     label: t('tables.widgets.total.label'),     preview: '20', description: t('tables.widgets.total.description') },
    { id: 'available', label: t('tables.widgets.available.label'), preview: '12', description: t('tables.widgets.available.description'), labelClass: 'tw:text-emerald-400' },
    { id: 'occupied',  label: t('tables.widgets.occupied.label'),  preview: '6',  description: t('tables.widgets.occupied.description'),  labelClass: 'tw:text-blue-400' },
    { id: 'cleaning',  label: t('tables.widgets.cleaning.label'),  preview: '2',  description: t('tables.widgets.cleaning.description'),  labelClass: 'tw:text-yellow-400' },
  ], { defaultCols: 4 })
const W_COLS_CLASS = { 1: 'tw:grid-cols-1', 2: 'tw:grid-cols-2', 3: 'tw:grid-cols-3', 4: 'tw:grid-cols-4' }
const wColsClass = computed(() => W_COLS_CLASS[wCols.value] ?? 'tw:grid-cols-2')

const columns = computed(() => [
  { field: 'code',            header: t('tables.columns.code'),    width: '8rem' },
  { field: 'zoneName',        header: t('tables.columns.zone'),    width: '9rem' },
  { field: 'status',          header: t('tables.columns.status'),  width: '8rem' },
  { field: 'isActive',        header: t('tables.columns.active'),  width: '7rem' },
  { field: 'activeSessionId', header: t('tables.columns.session'), width: '8rem' },
  { key: 'actions',           header: t('tables.columns.actions'), width: '18rem', toggleable: false },
])
</script>

<template>
  <!-- ── Add table dialog ─────────────────────────────────────────── -->
  <prime-dialog
    v-model:visible="showAddDialog"
    :header="t('tables.add.title')"
    :modal="true"
    :style="{ width: '24rem' }"
  >
    <div class="tw:space-y-4">
      <div class="tw:space-y-1.5">
        <label
          for="tableCode"
          class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted"
        >
          {{ t('tables.form.tableCode') }}
        </label>
        <prime-input-text
          id="tableCode"
          v-model="newCode"
          :placeholder="t('tables.form.tableCodePlaceholder')"
          class="app-input tw:w-full tw:font-mono"
          @keyup.enter="handleAddTable"
        />
        <p class="tw:text-[11px] app-text-subtle">
          {{ t('tables.form.tableCodeHint') }}
        </p>
      </div>
      <div class="tw:space-y-1.5">
        <label class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
          {{ t('tables.form.zone') }} <span class="app-text-subtle">{{ t('tables.form.optional') }}</span>
        </label>
        <prime-select
          v-model="newZoneId"
          :options="zoneOptions"
          option-label="label"
          option-value="value"
          :placeholder="t('tables.form.noZone')"
          show-clear
          class="app-input tw:w-full"
        />
      </div>
      <p v-if="addError" class="tw:text-xs tw:text-red-400">{{ addError }}</p>
    </div>
    <template #footer>
      <prime-button severity="secondary" outlined @click="showAddDialog = false">
        {{ t('common.cancel') }}
      </prime-button>
      <prime-button
        severity="success"
        :loading="addLoading"
        @click="handleAddTable"
      >
        <iconify icon="ph:plus-bold" />
        <span>{{ t('tables.add.submit') }}</span>
      </prime-button>
    </template>
  </prime-dialog>

  <!-- ── Edit dialog ──────────────────────────────────────────────── -->
  <prime-dialog
    v-model:visible="showEditDialog"
    :header="t('tables.edit.title')"
    :modal="true"
    :style="{ width: '24rem' }"
  >
    <div class="tw:space-y-4">
      <div class="tw:space-y-1.5">
        <label class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
          {{ t('tables.form.tableCode') }}
        </label>
        <prime-input-text
          v-model="editCode"
          :placeholder="t('tables.form.tableCodeEditPlaceholder')"
          class="app-input tw:w-full tw:font-mono"
          @keyup.enter="handleEditTable"
        />
      </div>
      <div class="tw:space-y-1.5">
        <label class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">
          {{ t('tables.form.zone') }} <span class="app-text-subtle">{{ t('tables.form.optional') }}</span>
        </label>
        <prime-select
          v-model="editZoneId"
          :options="zoneOptions"
          option-label="label"
          option-value="value"
          :placeholder="t('tables.form.noZone')"
          show-clear
          class="app-input tw:w-full"
        />
      </div>
      <p v-if="editError" class="tw:text-xs tw:text-red-400">{{ editError }}</p>
    </div>
    <template #footer>
      <prime-button severity="secondary" outlined @click="showEditDialog = false">
        {{ t('common.cancel') }}
      </prime-button>
      <prime-button
        severity="primary"
        :loading="editLoading"
        @click="handleEditTable"
      >
        <iconify icon="ph:check-bold" />
        <span>{{ t('tables.edit.submit') }}</span>
      </prime-button>
    </template>
  </prime-dialog>

  <!-- ── QR dialog ────────────────────────────────────────────────── -->
  <prime-dialog
    v-model:visible="showQrDialog"
    :header="`QR — ${qrRow?.code ?? ''}`"
    :modal="true"
    :style="{ width: '20rem' }"
  >
    <div class="tw:flex tw:flex-col tw:items-center tw:gap-3">
      <div
        v-if="qrCardLoading"
        class="tw:w-[260px] tw:h-[360px] tw:rounded-xl tw:bg-slate-100 tw:dark:bg-slate-800 tw:flex tw:items-center tw:justify-center"
      >
        <iconify icon="ph:circle-notch-bold" class="tw:animate-spin tw:text-2xl tw:text-muted" />
      </div>
      <img
        v-else-if="qrCardDataUrl"
        :src="qrCardDataUrl"
        class="tw:w-full tw:max-w-[260px] tw:rounded-xl tw:shadow-md"
        alt="QR Card"
      />
      <p
        v-if="qrUrl"
        class="tw:text-xs tw:font-mono tw:text-muted tw:text-center tw:break-all tw:px-2"
      >{{ qrUrl }}</p>
      <p v-if="regenerateError" class="tw:text-xs tw:text-red-400 tw:text-center">
        {{ regenerateError }}
      </p>
    </div>
    <template #footer>
      <div class="tw:flex tw:flex-wrap tw:gap-2 tw:w-full">
        <prime-button
          severity="secondary"
          outlined
          size="small"
          v-tooltip.top="t('tables.qr.customizeTooltip')"
          @click="qrCustomizerRef.toggle($event)"
        >
          <iconify icon="ph:sliders-bold" />
        </prime-button>
        <prime-button
          severity="warn"
          outlined
          size="small"
          :loading="regenerateLoading"
          v-tooltip.top="t('tables.qr.regenerateTooltip')"
          @click="handleRegenerateQrToken"
        >
          <iconify icon="ph:arrows-clockwise-bold" />
          <span>{{ t('tables.qr.regenerate') }}</span>
        </prime-button>
        <prime-button severity="primary" size="small" :loading="qrCardLoading" @click="downloadQr">
          <iconify icon="ph:download-bold" />
          <span>{{ t('tables.qr.downloadPng') }}</span>
        </prime-button>
      </div>
    </template>
  </prime-dialog>

  <!-- ── QR customizer popover ──────────────────────────────────── -->
  <prime-popover ref="qrCustomizerRef">
    <div class="tw:flex tw:flex-col tw:gap-3" style="min-width: 210px">
      <!-- Header color -->
      <div class="tw:flex tw:items-center tw:gap-2">
        <span class="tw:text-xs tw:text-muted tw:w-28 tw:shrink-0">{{ t('tables.qr.customizer.headerColor') }}</span>
        <input
          type="color"
          :value="cardColor"
          @input="cardColor = $event.target.value"
          class="tw:w-7 tw:h-7 tw:rounded tw:cursor-pointer tw:border-0 tw:bg-transparent tw:p-0.5"
        />
        <button
          class="tw:text-xs tw:text-muted tw:underline tw:cursor-pointer tw:bg-transparent tw:border-0 tw:p-0"
          @click="cardColor = '#10b981'"
        >{{ t('tables.qr.customizer.reset') }}</button>
      </div>
      <!-- Pattern -->
      <div class="tw:flex tw:items-center tw:gap-2">
        <span class="tw:text-xs tw:text-muted tw:w-28 tw:shrink-0">{{ t('tables.qr.customizer.pattern') }}</span>
        <div class="tw:flex tw:gap-1">
          <button v-for="opt in qrStyleOptions" :key="opt.value"
            @click="qrStyle = opt.value"
            :class="['tw:text-sm tw:w-8 tw:h-7 tw:rounded tw:border tw:cursor-pointer tw:transition-colors',
              qrStyle === opt.value ? 'tw:bg-primary-500 tw:text-white tw:border-primary-500' : 'tw:text-muted tw:border-surface-400 tw:bg-transparent']"
          >{{ opt.label }}</button>
        </div>
      </div>
      <!-- Marker border -->
      <div class="tw:flex tw:items-center tw:gap-2">
        <span class="tw:text-xs tw:text-muted tw:w-28 tw:shrink-0">{{ t('tables.qr.customizer.markerBorder') }}</span>
        <div class="tw:flex tw:gap-1">
          <button v-for="opt in markerBorderOptions" :key="opt.value"
            @click="markerBorder = opt.value"
            :class="['tw:text-sm tw:w-8 tw:h-7 tw:rounded tw:border tw:cursor-pointer tw:transition-colors',
              markerBorder === opt.value ? 'tw:bg-primary-500 tw:text-white tw:border-primary-500' : 'tw:text-muted tw:border-surface-400 tw:bg-transparent']"
          >{{ opt.label }}</button>
        </div>
      </div>
      <!-- Marker center -->
      <div class="tw:flex tw:items-center tw:gap-2">
        <span class="tw:text-xs tw:text-muted tw:w-28 tw:shrink-0">{{ t('tables.qr.customizer.markerCenter') }}</span>
        <div class="tw:flex tw:gap-1">
          <button v-for="opt in markerCenterOptions" :key="opt.value"
            @click="markerCenter = opt.value"
            :class="['tw:text-sm tw:w-8 tw:h-7 tw:rounded tw:border tw:cursor-pointer tw:transition-colors',
              markerCenter === opt.value ? 'tw:bg-primary-500 tw:text-white tw:border-primary-500' : 'tw:text-muted tw:border-surface-400 tw:bg-transparent']"
          >{{ opt.label }}</button>
        </div>
      </div>
      <!-- Precision -->
      <div class="tw:flex tw:items-center tw:gap-2">
        <span class="tw:text-xs tw:text-muted tw:w-28 tw:shrink-0">{{ t('tables.qr.customizer.precision') }}</span>
        <div class="tw:flex tw:gap-1">
          <button v-for="lvl in errorLevelOptions" :key="lvl"
            @click="errorLevel = lvl"
            :class="['tw:text-xs tw:font-mono tw:w-8 tw:h-7 tw:rounded tw:border tw:cursor-pointer tw:transition-colors',
              errorLevel === lvl ? 'tw:bg-primary-500 tw:text-white tw:border-primary-500' : 'tw:text-muted tw:border-surface-400 tw:bg-transparent']"
          >{{ lvl }}</button>
        </div>
      </div>
    </div>
  </prime-popover>

  <section class="tw:space-y-8">
    <!-- ── Header ────────────────────────────────────────────────── -->
    <div class="tw:flex tw:flex-wrap tw:items-end tw:justify-between tw:gap-4">
      <div>
        <p
          class="tw:text-xs tw:uppercase tw:tracking-[0.3em] tw:text-emerald-300"
        >
          {{ t('tables.groupLabel') }}
        </p>
        <h1 class="tw:mt-2 tw:text-3xl tw:font-semibold">{{ t('tables.title') }}</h1>
        <p class="tw:mt-2 tw:text-sm tw:text-muted">
          {{ t('tables.subtitle') }}
        </p>
      </div>
      <div class="tw:flex tw:items-center tw:gap-2">
        <widget-settings-button
          :widgets="wDefs"
          :hidden-count="wHidden"
          :cols-per-row="wCols"
          @toggle="wToggle"
          @update:cols-per-row="wSetCols"
        />
        <prime-button
          severity="secondary"
          outlined
          size="small"
          v-tooltip.top="t('tables.qr.exportAllTooltip')"
          :loading="exportAllQrLoading"
          :class="btnIcon"
          @click="exportAllQrPdf"
        >
          <span v-if="!exportAllQrLoading" class="tw:relative tw:inline-flex">
            <iconify icon="ph:qr-code-bold" />
          </span>
          <span v-if="exportAllQrLoading" class="tw:text-[10px] tw:tabular-nums tw:leading-none">
            {{ exportAllQrProgress }}/{{ exportAllQrTotal }}
          </span>
        </prime-button>
        <prime-button
          v-if="can('table.create')"
          severity="success"
          size="small"
          @click="openAddDialog"
        >
          <iconify icon="ph:plus-bold" />
          <span>{{ t('tables.addTable') }}</span>
        </prime-button>
      </div>
    </div>

    <!-- ── Summary stats (mobile compact bar) ───────────────────── -->
    <div class="tw:sm:hidden tw:rounded-xl tw:border tw:border-slate-200 tw:dark:border-white/15 tw:bg-slate-50 tw:dark:bg-white/5 tw:p-4 tw:grid tw:grid-cols-2">
      <div class="tw:flex tw:flex-col tw:gap-0.5 tw:pb-3 tw:pr-4">
        <span class="tw:text-[11px] tw:uppercase tw:tracking-widest app-text-subtle">{{ t('tables.widgets.total.label') }}</span>
        <span class="tw:text-sm tw:font-bold">{{ summary.total }}</span>
      </div>
      <div class="tw:flex tw:flex-col tw:gap-0.5 tw:pb-3 tw:pl-4 tw:border-l tw:border-slate-200 tw:dark:border-white/10">
        <span class="tw:text-[11px] tw:uppercase tw:tracking-widest tw:text-emerald-500 tw:dark:text-emerald-400">{{ t('tables.widgets.available.label') }}</span>
        <span class="tw:text-sm tw:font-bold">{{ summary.available }}</span>
      </div>
      <div class="tw:flex tw:flex-col tw:gap-0.5 tw:pt-3 tw:pr-4 tw:border-t tw:border-slate-200 tw:dark:border-white/10">
        <span class="tw:text-[11px] tw:uppercase tw:tracking-widest tw:text-blue-500 tw:dark:text-blue-400">{{ t('tables.widgets.occupied.label') }}</span>
        <span class="tw:text-sm tw:font-bold">{{ summary.occupied }}</span>
      </div>
      <div class="tw:flex tw:flex-col tw:gap-0.5 tw:pt-3 tw:pl-4 tw:border-t tw:border-l tw:border-slate-200 tw:dark:border-white/10">
        <span class="tw:text-[11px] tw:uppercase tw:tracking-widest tw:text-yellow-500 tw:dark:text-yellow-400">{{ t('tables.widgets.cleaning.label') }}</span>
        <span class="tw:text-sm tw:font-bold">{{ summary.cleaning }}</span>
      </div>
    </div>

    <!-- ── Summary stats (desktop) ───────────────────────────────── -->
    <div :class="['tw:hidden tw:sm:grid tw:gap-3', wColsClass]">
      <widget-stat v-if="wVisible('total')"     :label="t('tables.widgets.total.label')"     :value="summary.total" />
      <widget-stat v-if="wVisible('available')" :label="t('tables.widgets.available.label')" :value="summary.available" label-class="tw:text-emerald-400" />
      <widget-stat v-if="wVisible('occupied')"  :label="t('tables.widgets.occupied.label')"  :value="summary.occupied"  label-class="tw:text-blue-400" />
      <widget-stat v-if="wVisible('cleaning')"  :label="t('tables.widgets.cleaning.label')"  :value="summary.cleaning"  label-class="tw:text-yellow-400" />
    </div>

    <!-- ── Error ──────────────────────────────────────────────────── -->
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
      :value="pagedRows"
      :loading="loading"
      :totalRecords="filtered.length"
      :rowsPerPageOptions="[10, 20, 50]"
      :columns="columns"
      @page="(e) => (first = e.first)"
    >
      <template #toolbar-left>
        <div class="tw:flex tw:items-center tw:gap-2">
          <prime-button
            :severity="activeFilterCount > 0 ? 'success' : 'secondary'"
            :outlined="activeFilterCount === 0"
            v-tooltip.top="t('tables.filter.filtersTooltip')"
            @click="openFilter($event)"
            :class="btnIcon"
          >
            <span class="tw:relative tw:inline-flex">
              <iconify icon="ph:funnel-bold" />
              <prime-badge
                v-if="activeFilterCount > 0"
                :value="activeFilterCount"
                severity="danger"
                class="tw:absolute! tw:-top-2.5! tw:-right-2.5! tw:scale-75! tw:origin-top-right!"
              />
            </span>
          </prime-button>

          <prime-popover ref="filterPanel">
            <div class="tw:flex tw:flex-col tw:gap-4">
              <p class="tw:text-sm tw:font-semibold">{{ t('tables.filter.title') }}</p>

              <div class="tw:space-y-1.5">
                <label
                  for="statusFilter"
                  class="tw:text-xs tw:text-muted tw:uppercase tw:tracking-widest"
                >
                  {{ t('tables.filter.status') }}
                </label>
                <prime-select
                  id="statusFilter"
                  v-model="statusFilter"
                  :options="statusOptions"
                  option-label="label"
                  option-value="value"
                  :placeholder="t('tables.filter.allStatuses')"
                  show-clear
                  class="app-input tw:w-full"
                />
              </div>

              <div class="tw:space-y-1.5">
                <label class="tw:text-xs tw:text-muted tw:uppercase tw:tracking-widest">
                  {{ t('tables.filter.active') }}
                </label>
                <prime-select
                  v-model="activeFilter"
                  :options="activeOptions"
                  option-label="label"
                  option-value="value"
                  :placeholder="t('tables.filter.allActive')"
                  show-clear
                  class="app-input tw:w-full"
                />
              </div>

              <div v-if="zoneOptions.length > 0" class="tw:space-y-1.5">
                <label class="tw:text-xs tw:text-muted tw:uppercase tw:tracking-widest">
                  {{ t('tables.filter.zone') }}
                </label>
                <prime-select
                  v-model="zoneFilter"
                  :options="zoneOptions"
                  option-label="label"
                  option-value="value"
                  :placeholder="t('tables.filter.allZones')"
                  show-clear
                  class="app-input tw:w-full"
                />
              </div>

              <prime-button
                v-if="activeFilterCount > 0"
                severity="danger"
                outlined
                size="small"
                @click="clearFilters"
              >
                <iconify icon="ph:x-bold" />
                <span>{{ t('tables.filter.clearFilters') }}</span>
              </prime-button>
            </div>
          </prime-popover>
        </div>
      </template>

      <template #mobile-card="{ data }">
        <div
          :class="[
            'tw:rounded-xl tw:border tw:border-slate-200 tw:dark:border-white/10',
            'tw:bg-white tw:dark:bg-white/5 tw:p-3 tw:flex tw:flex-col tw:gap-2',
            !data.isActive && 'tw:opacity-60',
          ]"
        >
          <!-- Code + Status -->
          <div class="tw:flex tw:items-start tw:justify-between tw:gap-1">
            <span class="tw:font-mono tw:font-bold tw:text-sm tw:leading-tight">{{ data.code }}</span>
            <prime-tag
              :value="statusTag(data.status).label"
              :severity="statusTag(data.status).severity"
              class="tw:text-[11px]! tw:px-1.5! tw:py-0.5! tw:flex-shrink-0"
            />
          </div>
          <!-- Zone + Inactive badge -->
          <div class="tw:flex tw:items-center tw:gap-1.5 tw:flex-wrap">
            <span class="tw:text-xs tw:text-muted">{{ data.zoneName || '—' }}</span>
            <prime-tag v-if="!data.isActive" :value="t('tables.activeStatus.inactive')" severity="danger" class="tw:text-[10px]! tw:px-1! tw:py-0!" />
          </div>
          <!-- Actions -->
          <div class="tw:border-t tw:border-slate-200 tw:dark:border-white/10 tw:pt-2">
            <prime-button severity="secondary" outlined size="small" fluid @click="openDrawer(data)">
              <iconify icon="ph:dots-three-bold" />
              <span>{{ t('common.moreActions') }}</span>
            </prime-button>
          </div>
        </div>
      </template>

      <template #col-code="{ data }">
        <span class="tw:font-mono tw:font-semibold tw:text-sm">{{ data.code }}</span>
      </template>

      <template #col-zoneName="{ data }">
        <span v-if="data.zoneName" class="tw:text-sm">{{ data.zoneName }}</span>
        <span v-else class="app-text-subtle">—</span>
      </template>

      <template #col-status="{ data }">
        <prime-tag
          :value="statusTag(data.status).label"
          :severity="statusTag(data.status).severity"
        />
      </template>

      <template #col-isActive="{ data }">
        <prime-tag
          :value="activeTag(data.isActive).label"
          :severity="activeTag(data.isActive).severity"
        />
      </template>

      <template #col-activeSessionId="{ data }">
        <span v-if="data.activeSessionId" class="tw:text-xs tw:text-muted tw:font-mono">
          {{ data.activeSessionId.slice(0, 8) }}…
        </span>
        <span v-else class="app-text-subtle">—</span>
      </template>

      <template #col-actions="{ data }">
        <div class="tw:flex tw:justify-end tw:flex-wrap tw:gap-2">
          <prime-button
            v-show="data.status === 'Occupied' && data.activeSessionId"
            severity="info"
            outlined
            size="small"
            v-tooltip.top="t('tables.actions.closeSessionTooltip')"
            @click="handleCloseSession(data)"
            :class="btnIcon"
          >
            <iconify icon="ph:x-circle-bold" />
          </prime-button>

          <prime-button
            v-show="data.status === 'Cleaning'"
            severity="success"
            outlined
            size="small"
            v-tooltip.top="t('tables.actions.markAvailableTooltip')"
            @click="handleMarkAvailable(data)"
          >
            <iconify icon="ph:check-circle-bold" />
          </prime-button>

          <prime-button
            v-if="can('table.create')"
            severity="secondary"
            outlined
            size="small"
            v-tooltip.top="t('tables.actions.editTooltip')"
            @click="openEditDialog(data)"
            :class="btnIcon"
          >
            <iconify icon="ph:pencil-bold" />
          </prime-button>

          <prime-button
            severity="warn"
            outlined
            size="small"
            v-tooltip.top="t('tables.actions.showQrTooltip')"
            @click="openQrDialog(data)"
            :class="btnIcon"
          >
            <iconify icon="ph:qr-code-bold" />
          </prime-button>

          <prime-button
            :disabled="data.status == 'Occupied'"
            :severity="data.status == 'Occupied' ? 'secondary' : data.isActive ? 'danger' : 'success'"
            outlined
            size="small"
            @click="handleToggleActive(data)"
            :class="btnIcon"
          >
            <iconify :icon="data.isActive ? 'ph:toggle-left-bold' : 'ph:toggle-right-bold'" />
          </prime-button>

          <prime-button
            :disabled="data.status == 'Occupied' || !can('table.delete')"
            :severity="data.status !== 'Occupied' ? 'danger' : 'secondary'"
            outlined
            size="small"
            v-tooltip.top="t('tables.actions.deleteTooltip')"
            @click="handleDelete(data)"
            :class="btnIcon"
          >
            <iconify icon="ph:trash-bold" />
          </prime-button>
        </div>
      </template>
    </AppTable>

    <!-- ── Filter drawer (mobile) ────────────────────────────────── -->
    <prime-drawer
      v-model:visible="filterDrawerVisible"
      position="bottom"
      :style="{ height: 'auto', maxHeight: '90dvh' }"
      :pt="{ root: { class: 'tw:rounded-t-2xl' }, content: { class: 'tw:overflow-y-auto' } }"
    >
      <template #header>
        <div class="tw:flex tw:items-center tw:justify-between tw:w-full tw:pr-2">
          <span class="tw:font-semibold">{{ t('tables.filter.title') }}</span>
          <prime-button
            v-if="activeFilterCount > 0"
            severity="danger"
            outlined
            size="small"
            @click="clearFilters"
          >
            <iconify icon="ph:x-bold" />
            <span>{{ t('tables.filter.clearFilters') }}</span>
          </prime-button>
        </div>
      </template>

      <div class="tw:flex tw:flex-col tw:gap-4 tw:pb-6">
        <div class="tw:space-y-1.5">
          <label class="tw:text-xs tw:text-muted tw:uppercase tw:tracking-widest">
            {{ t('tables.filter.status') }}
          </label>
          <prime-select
            v-model="statusFilter"
            :options="statusOptions"
            option-label="label"
            option-value="value"
            :placeholder="t('tables.filter.allStatuses')"
            show-clear
            class="app-input tw:w-full"
          />
        </div>

        <div class="tw:space-y-1.5">
          <label class="tw:text-xs tw:text-muted tw:uppercase tw:tracking-widest">
            {{ t('tables.filter.active') }}
          </label>
          <prime-select
            v-model="activeFilter"
            :options="activeOptions"
            option-label="label"
            option-value="value"
            :placeholder="t('tables.filter.allActive')"
            show-clear
            class="app-input tw:w-full"
          />
        </div>

        <div v-if="zoneOptions.length > 0" class="tw:space-y-1.5">
          <label class="tw:text-xs tw:text-muted tw:uppercase tw:tracking-widest">
            {{ t('tables.filter.zone') }}
          </label>
          <prime-select
            v-model="zoneFilter"
            :options="zoneOptions"
            option-label="label"
            option-value="value"
            :placeholder="t('tables.filter.allZones')"
            show-clear
            class="app-input tw:w-full"
          />
        </div>
      </div>
    </prime-drawer>

    <!-- ── Mobile action drawer ───────────────────────────────────── -->
    <prime-drawer
      v-model:visible="drawerVisible"
      position="bottom"
      :style="{ height: 'auto' }"
      :pt="{ root: { class: 'tw:rounded-t-2xl' } }"
    >
      <template #header>
        <div class="tw:flex tw:items-center tw:gap-2">
          <span class="tw:font-mono tw:font-bold">{{ drawerTable?.code }}</span>
          <prime-tag
            v-if="drawerTable"
            :value="statusTag(drawerTable.status).label"
            :severity="statusTag(drawerTable.status).severity"
            class="tw:text-[11px]! tw:px-1.5! tw:py-0.5!"
          />
          <prime-tag v-if="drawerTable && !drawerTable.isActive" :value="t('tables.activeStatus.inactive')" severity="danger" class="tw:text-[11px]! tw:px-1.5! tw:py-0.5!" />
          <span v-if="drawerTable?.zoneName" class="tw:text-xs tw:text-muted">· {{ drawerTable.zoneName }}</span>
        </div>
      </template>

      <div v-if="drawerTable" class="tw:flex tw:flex-col tw:gap-2 tw:pb-4">
        <prime-button
          v-if="drawerTable.status === 'Occupied' && drawerTable.activeSessionId"
          :label="t('tables.actions.closeSession')"
          severity="info" outlined fluid
          @click="handleCloseSession(drawerTable); drawerVisible = false"
        >
          <template #icon><iconify icon="ph:x-circle-bold" /></template>
        </prime-button>

        <prime-button
          v-if="drawerTable.status === 'Cleaning'"
          :label="t('tables.actions.markAvailable')"
          severity="success" outlined fluid
          @click="handleMarkAvailable(drawerTable); drawerVisible = false"
        >
          <template #icon><iconify icon="ph:check-circle-bold" /></template>
        </prime-button>

        <prime-button
          v-if="can('table.create')"
          :label="t('tables.actions.edit')"
          severity="secondary" outlined fluid
          @click="openEditDialog(drawerTable); drawerVisible = false"
        >
          <template #icon><iconify icon="ph:pencil-bold" /></template>
        </prime-button>

        <prime-button
          :label="t('tables.actions.showQr')"
          severity="warn" outlined fluid
          @click="openQrDialog(drawerTable); drawerVisible = false"
        >
          <template #icon><iconify icon="ph:qr-code-bold" /></template>
        </prime-button>

        <prime-button
          :label="drawerTable.isActive ? t('tables.actions.deactivate') : t('tables.actions.activate')"
          :severity="drawerTable.isActive ? 'danger' : 'success'"
          outlined fluid
          :disabled="drawerTable.status === 'Occupied'"
          @click="handleToggleActive(drawerTable); drawerVisible = false"
        >
          <template #icon>
            <iconify :icon="drawerTable.isActive ? 'ph:toggle-left-bold' : 'ph:toggle-right-bold'" />
          </template>
        </prime-button>

        <prime-button
          v-if="can('table.delete')"
          :label="t('tables.actions.delete')"
          severity="danger" outlined fluid
          :disabled="drawerTable.status === 'Occupied'"
          @click="handleDelete(drawerTable); drawerVisible = false"
        >
          <template #icon><iconify icon="ph:trash-bold" /></template>
        </prime-button>
      </div>
    </prime-drawer>
  </section>
</template>
