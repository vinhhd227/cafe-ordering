import { defineStore } from 'pinia'

export const useCartStore = defineStore('cart', {
  state: () => ({
    items: [], // CartItem[]
  }),

  getters: {
    total: (state) =>
      state.items.reduce((acc, item) => acc + item.price * item.quantity, 0),

    count: (state) => state.items.length,
  },

  actions: {
    /**
     * Thêm sản phẩm vào giỏ hàng.
     * Nếu đã có cùng id + options thì tăng quantity thay vì thêm dòng mới.
     */
    addItem(product, options, quantity = 1) {
      const key      = JSON.stringify(options)
      const existing = this.items.find(
        (i) => i.id === product.id && JSON.stringify(i.options) === key,
      )
      if (existing) {
        existing.quantity += quantity
      } else {
        this.items.push({ ...product, quantity, options })
      }
    },

    /** Giảm 1 đơn vị, xóa hẳn nếu quantity về 0. */
    removeItem(cartItem) {
      const idx = this.items.findIndex(
        (i) =>
          i.id === cartItem.id &&
          JSON.stringify(i.options) === JSON.stringify(cartItem.options),
      )
      if (idx === -1) return
      this.items[idx].quantity--
      if (this.items[idx].quantity === 0) this.items.splice(idx, 1)
    },

    /** Tăng 1 đơn vị. */
    increaseItem(cartItem) {
      const idx = this.items.findIndex(
        (i) =>
          i.id === cartItem.id &&
          JSON.stringify(i.options) === JSON.stringify(cartItem.options),
      )
      if (idx !== -1) this.items[idx].quantity++
    },

    /** Xóa toàn bộ giỏ hàng (sau khi đặt hàng thành công). */
    clear() {
      this.items = []
    },
  },
})
