import { useAuthStore } from '@/stores/auth'

vi.mock('@/services/auth.service.js', () => ({
  login: vi.fn(),
  refresh: vi.fn(),
  register: vi.fn(),
  logout: vi.fn().mockResolvedValue({}),
  checkUsername: vi.fn(),
}))

vi.mock('@/services/axios', () => ({
  default: { post: vi.fn(), interceptors: { request: { use: vi.fn() }, response: { use: vi.fn() } } },
}))

import { login as loginRequest } from '@/services/auth.service.js'

// JWT claim name cho roles (ASP.NET Core convention)
const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'

// Tạo fake JWT có payload thật để parseJwt trong store có thể decode
function makeToken({
  id = '1',
  username = 'staff01',
  fullName = 'Nhân viên',
  roles = ['Staff'],
  permissions = ['order.read'],
} = {}) {
  const payload = {
    sub: id,
    username,
    fullName,
    [ROLE_CLAIM]: roles.length === 1 ? roles[0] : roles,
    permission:
      permissions.length === 0
        ? undefined
        : permissions.length === 1
          ? permissions[0]
          : permissions,
  }
  const encoded = btoa(unescape(encodeURIComponent(JSON.stringify(payload))))
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=+$/, '')
  return `eyJhbGciOiJIUzI1NiJ9.${encoded}.sig`
}

describe('useAuthStore — login()', () => {
  afterEach(() => {
    localStorage.clear()
    vi.clearAllMocks()
  })

  it('lưu accessToken vào store', async () => {
    const token = makeToken()
    loginRequest.mockResolvedValue({
      data: { accessToken: token, expiresAt: new Date(Date.now() + 15 * 60 * 1000).toISOString() },
    })
    const auth = useAuthStore()
    await auth.login({ username: 'staff01', password: 'Abc@1234' })
    expect(auth.accessToken).toBe(token)
  })

  it('parse user từ JWT và lưu vào store', async () => {
    const token = makeToken({
      id: '42',
      username: 'admin01',
      fullName: 'Admin User',
      roles: ['Admin'],
      permissions: ['user.read', 'product.create'],
    })
    loginRequest.mockResolvedValue({ data: { accessToken: token, expiresAt: '' } })
    const auth = useAuthStore()
    await auth.login({ username: 'admin01', password: 'pass' })
    expect(auth.user).toMatchObject({
      id: '42',
      username: 'admin01',
      fullName: 'Admin User',
      roles: ['Admin'],
      permissions: ['user.read', 'product.create'],
    })
  })

  it('lưu user vào localStorage', async () => {
    const token = makeToken()
    loginRequest.mockResolvedValue({ data: { accessToken: token, expiresAt: '' } })
    const auth = useAuthStore()
    await auth.login({ username: 'staff01', password: 'pass' })
    const stored = JSON.parse(localStorage.getItem('user'))
    expect(stored.username).toBe('staff01')
  })

  it('lưu expiresAt vào store', async () => {
    const token = makeToken()
    const expiresAt = new Date(Date.now() + 15 * 60 * 1000).toISOString()
    loginRequest.mockResolvedValue({ data: { accessToken: token, expiresAt } })
    const auth = useAuthStore()
    await auth.login({ username: 'staff01', password: 'pass' })
    expect(auth.expiresAt).toBe(expiresAt)
  })

  it('isAuthenticated trả về true sau khi login thành công', async () => {
    const token = makeToken()
    loginRequest.mockResolvedValue({ data: { accessToken: token, expiresAt: '' } })
    const auth = useAuthStore()
    expect(auth.isAuthenticated).toBe(false)
    await auth.login({ username: 'staff01', password: 'pass' })
    expect(auth.isAuthenticated).toBe(true)
  })

  it('gọi loginRequest với đúng payload', async () => {
    const token = makeToken()
    loginRequest.mockResolvedValue({ data: { accessToken: token, expiresAt: '' } })
    const auth = useAuthStore()
    await auth.login({ username: 'staff01', password: 'Abc@1234' })
    expect(loginRequest).toHaveBeenCalledWith({ username: 'staff01', password: 'Abc@1234' })
  })

  it('ném lỗi khi service thất bại, không thay đổi state', async () => {
    loginRequest.mockRejectedValue({ response: { status: 401, data: { message: 'Sai tài khoản hoặc mật khẩu' } } })
    const auth = useAuthStore()
    await expect(auth.login({ username: 'wrong', password: 'wrong' })).rejects.toBeDefined()
    expect(auth.accessToken).toBeNull()
    expect(auth.user).toBeNull()
    expect(auth.isAuthenticated).toBe(false)
  })

  it('roles luôn là array dù JWT trả về string đơn', async () => {
    const token = makeToken({ roles: ['Staff'] }) // makeToken gửi string đơn cho 1 role
    loginRequest.mockResolvedValue({ data: { accessToken: token, expiresAt: '' } })
    const auth = useAuthStore()
    await auth.login({ username: 'staff01', password: 'pass' })
    expect(Array.isArray(auth.user.roles)).toBe(true)
    expect(auth.user.roles).toContain('Staff')
  })

  it('permissions luôn là array dù JWT trả về string đơn', async () => {
    const token = makeToken({ permissions: ['order.read'] }) // string đơn
    loginRequest.mockResolvedValue({ data: { accessToken: token, expiresAt: '' } })
    const auth = useAuthStore()
    await auth.login({ username: 'staff01', password: 'pass' })
    expect(Array.isArray(auth.user.permissions)).toBe(true)
    expect(auth.user.permissions).toContain('order.read')
  })
})
