/**
 * useTableCache — lưu trạng thái phân trang + bộ lọc của table vào sessionStorage.
 *
 * @param {string} tableKey  Định danh duy nhất cho từng table, VD: 'products', 'users'.
 *                           Key lưu trong storage sẽ là `table_cache_<tableKey>` để
 *                           tránh xung đột giữa các table.
 *
 * Cache tồn tại 5 phút kể từ lần lưu cuối. Hết TTL → xoá và trả về null.
 * Tương lai: truyền thêm bất kỳ field nào vào `save()` (filters, sort…) sẽ tự lưu.
 */

const CACHE_TTL_MS = 5 * 60 * 1000 // 5 phút

export function useTableCache(tableKey) {
  const storageKey = `table_cache_${tableKey}`

  /**
   * Lưu trạng thái hiện tại.
   * @param {Object} state  Bất kỳ object nào: { page, rows, first, search, … }
   */
  const save = (state) => {
    try {
      sessionStorage.setItem(
        storageKey,
        JSON.stringify({ ...state, _savedAt: Date.now() }),
      )
    } catch {
      // sessionStorage đầy hoặc private browsing — bỏ qua
    }
  }

  /**
   * Khôi phục trạng thái đã lưu.
   * @returns {Object|null}  State đã lưu, hoặc null nếu hết hạn / chưa có.
   */
  const restore = () => {
    try {
      const raw = sessionStorage.getItem(storageKey)
      if (!raw) return null

      const data = JSON.parse(raw)
      if (Date.now() - data._savedAt > CACHE_TTL_MS) {
        sessionStorage.removeItem(storageKey)
        return null
      }
      // Xoá trường nội bộ trước khi trả ra ngoài
      const { _savedAt, ...state } = data
      return state
    } catch {
      return null
    }
  }

  /** Xoá cache của table này (dùng khi người dùng thay đổi bộ lọc, v.v.) */
  const clear = () => {
    try {
      sessionStorage.removeItem(storageKey)
    } catch {}
  }

  return { save, restore, clear }
}
