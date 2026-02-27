import { defineStore } from "pinia";

const STORAGE_KEY = "admin_table_state";

function loadFromStorage() {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? JSON.parse(raw) : {};
  } catch {
    return {};
  }
}

/**
 * useTableStateStore — lưu trạng thái bảng (phân trang, bộ lọc, cột)
 * vào localStorage thông qua Pinia.
 *
 * Dữ liệu tồn tại qua các lần refresh / đóng-mở tab.
 * Mỗi bảng được định danh bằng tableKey (VD: 'products', 'users').
 */
export const useTableStateStore = defineStore("tableState", {
  state: () => ({
    tables: loadFromStorage(),
  }),

  actions: {
    save(tableKey, state) {
      this.tables[tableKey] = { ...state, _savedAt: Date.now() };
      this._persist();
    },

    restore(tableKey) {
      const entry = this.tables[tableKey];
      if (!entry) return null;
      const { _savedAt, ...state } = entry;
      return state;
    },

    clear(tableKey) {
      delete this.tables[tableKey];
      this._persist();
    },

    _persist() {
      try {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(this.tables));
      } catch {
        // localStorage đầy — bỏ qua
      }
    },
  },
});
