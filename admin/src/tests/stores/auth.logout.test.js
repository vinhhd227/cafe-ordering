import { useAuthStore } from '@/stores/auth'

// Mock toàn bộ auth.service để không gọi API thật
vi.mock('@/services/auth.service.js', () => ({
  login: vi.fn(),
  refresh: vi.fn(),
  register: vi.fn(),
  logout: vi.fn().mockResolvedValue({}),
}))

// Mock axios instance (auth store import api để dùng trong service)
vi.mock('@/services/axios', () => ({
  default: { post: vi.fn(), interceptors: { request: { use: vi.fn() }, response: { use: vi.fn() } } },
}))

import { logout as logoutRequest } from '@/services/auth.service.js'

// Helper: tạo auth store đã ở trạng thái đăng nhập
function setupLoggedInStore() {
  const auth = useAuthStore()
  auth.accessToken = 'fake-token'
  auth.user = { id: '1', username: 'staff01', fullName: 'Nhân viên', roles: ['Staff'], permissions: ['order.read'] }
  auth.expiresAt = new Date(Date.now() + 15 * 60 * 1000).toISOString()
  auth.refreshAttempts = 0
  auth.refreshing = false
  auth._refreshPromise = null
  localStorage.setItem('user', JSON.stringify(auth.user))
  return auth
}

describe('useAuthStore — logout()', () => {
  afterEach(() => {
    localStorage.clear()
    vi.clearAllMocks()
  })

  it('xóa accessToken', () => {
    const auth = setupLoggedInStore()
    auth.logout()
    expect(auth.accessToken).toBeNull()
  })

  it('xóa user khỏi store', () => {
    const auth = setupLoggedInStore()
    auth.logout()
    expect(auth.user).toBeNull()
  })

  it('xóa user khỏi localStorage', () => {
    const auth = setupLoggedInStore()
    auth.logout()
    expect(localStorage.getItem('user')).toBeNull()
  })

  it('xóa expiresAt', () => {
    const auth = setupLoggedInStore()
    auth.logout()
    expect(auth.expiresAt).toBeNull()
  })

  it('reset refreshAttempts về 0', () => {
    const auth = setupLoggedInStore()
    auth.refreshAttempts = 3
    auth.logout()
    expect(auth.refreshAttempts).toBe(0)
  })

  it('reset refreshing về false', () => {
    const auth = setupLoggedInStore()
    auth.refreshing = true
    auth.logout()
    expect(auth.refreshing).toBe(false)
  })

  it('reset _refreshPromise về null', () => {
    const auth = setupLoggedInStore()
    auth._refreshPromise = Promise.resolve()
    auth.logout()
    expect(auth._refreshPromise).toBeNull()
  })

  it('hủy refresh timer nếu đang chạy', () => {
    const auth = setupLoggedInStore()
    auth.refreshTimer = setTimeout(() => {}, 60_000)
    const clearSpy = vi.spyOn(global, 'clearTimeout')

    auth.logout()

    expect(clearSpy).toHaveBeenCalled()
    expect(auth.refreshTimer).toBeNull()
  })

  it('gọi logoutRequest lên server', () => {
    const auth = setupLoggedInStore()
    auth.logout()
    expect(logoutRequest).toHaveBeenCalledOnce()
  })

  it('isAuthenticated trả về false sau khi logout', () => {
    const auth = setupLoggedInStore()
    expect(auth.isAuthenticated).toBe(true)
    auth.logout()
    expect(auth.isAuthenticated).toBe(false)
  })
})
