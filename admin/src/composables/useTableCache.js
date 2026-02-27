import { useTableStateStore } from "@/stores/tableState";

/**
 * useTableCache — lưu trạng thái phân trang + bộ lọc của table.
 *
 * @param {string} tableKey  Định danh duy nhất cho từng table, VD: 'products', 'users'.
 *
 * Dữ liệu được lưu vào localStorage thông qua Pinia (useTableStateStore),
 * tồn tại qua các lần refresh và đóng-mở tab.
 * Truyền bất kỳ field nào vào save() (filters, sort, colDefs…) sẽ tự lưu.
 */
export function useTableCache(tableKey) {
  const store = useTableStateStore();

  /** Lưu trạng thái hiện tại. */
  const save = (state) => store.save(tableKey, state);

  /** Khôi phục trạng thái đã lưu, hoặc null nếu chưa có. */
  const restore = () => store.restore(tableKey);

  /** Xóa cache của table này. */
  const clear = () => store.clear(tableKey);

  return { save, restore, clear };
}
