import { usePermission } from '@/composables/usePermission'
import { useAuthStore } from '@/stores/auth'

describe('usePermission', () => {
  describe('can()', () => {
    it('trả về false khi chưa đăng nhập', () => {
      const { can } = usePermission()
      expect(can('product.read')).toBe(false)
    })

    it('trả về true với mọi permission khi là Admin', () => {
      const auth = useAuthStore()
      auth.user = { roles: ['Admin'], permissions: [] }

      const { can } = usePermission()
      expect(can('product.read')).toBe(true)
      expect(can('user.delete')).toBe(true)
      expect(can('any.permission')).toBe(true)
    })

    it('trả về true khi user có permission được yêu cầu', () => {
      const auth = useAuthStore()
      auth.user = { roles: ['Staff'], permissions: ['product.read', 'order.read'] }

      const { can } = usePermission()
      expect(can('product.read')).toBe(true)
      expect(can('order.read')).toBe(true)
    })

    it('trả về false khi user không có permission được yêu cầu', () => {
      const auth = useAuthStore()
      auth.user = { roles: ['Staff'], permissions: ['product.read'] }

      const { can } = usePermission()
      expect(can('user.delete')).toBe(false)
    })
  })
})
