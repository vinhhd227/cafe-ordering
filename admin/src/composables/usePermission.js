import { useAuthStore } from '@/stores/auth'

export function usePermission() {
  const auth = useAuthStore()

  /**
   * Returns true if the current user has the given permission.
   * Admin role bypasses all checks (always returns true).
   */
  const can = (permission) => {
    if (!auth.user) return false
    if (auth.user.roles.includes('Admin')) return true
    return auth.user.permissions.includes(permission)
  }

  return { can }
}
