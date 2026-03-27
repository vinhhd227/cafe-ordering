vi.mock('@/services/axios', () => ({
  default: {
    post: vi.fn(),
    interceptors: { request: { use: vi.fn() }, response: { use: vi.fn() } },
  },
}))

import api from '@/services/axios'
import { login, register, checkUsername } from '@/services/auth.service.js'

describe('auth.service', () => {
  afterEach(() => vi.clearAllMocks())

  // ─── login ───────────────────────────────────────────────────────────────

  describe('login()', () => {
    it('gọi POST /auth/login với payload', () => {
      const payload = { username: 'staff01', password: 'Abc@1234' }
      login(payload)
      expect(api.post).toHaveBeenCalledWith('/auth/login', payload)
    })

    it('trả về response từ API khi thành công', async () => {
      const mockResponse = { data: { accessToken: 'token', expiresAt: '2026-03-27T10:00:00Z' } }
      api.post.mockResolvedValue(mockResponse)
      const res = await login({ username: 'staff01', password: 'Abc@1234' })
      expect(res).toBe(mockResponse)
    })

    it('ném lỗi 401 khi sai tài khoản hoặc mật khẩu', async () => {
      api.post.mockRejectedValue({ response: { status: 401, data: { message: 'Sai tài khoản hoặc mật khẩu' } } })
      await expect(login({ username: 'wrong', password: 'wrong' })).rejects.toMatchObject({
        response: { status: 401 },
      })
    })

    it('ném lỗi 400 khi thiếu trường bắt buộc', async () => {
      api.post.mockRejectedValue({ response: { status: 400 } })
      await expect(login({ username: '' })).rejects.toMatchObject({ response: { status: 400 } })
    })
  })

  // ─── register ────────────────────────────────────────────────────────────

  describe('register()', () => {
    const validPayload = {
      fullName: 'Nguyễn Văn A',
      username: 'nguyenvana',
      email: 'a@example.com',
      password: 'Abc@1234',
    }

    it('gọi POST /auth/register với payload', () => {
      register(validPayload)
      expect(api.post).toHaveBeenCalledWith('/auth/register', validPayload)
    })

    it('trả về response từ API khi đăng ký thành công', async () => {
      const mockResponse = { data: {} }
      api.post.mockResolvedValue(mockResponse)
      const res = await register(validPayload)
      expect(res).toBe(mockResponse)
    })

    it('ném lỗi 400 khi username đã tồn tại', async () => {
      api.post.mockRejectedValue({
        response: { status: 400, data: { message: 'Username already taken' } },
      })
      await expect(register({ ...validPayload, username: 'taken' })).rejects.toMatchObject({
        response: { status: 400 },
      })
    })

    it('ném lỗi 400 khi email đã được dùng', async () => {
      api.post.mockRejectedValue({
        response: { status: 400, data: { message: 'Email already in use' } },
      })
      await expect(register({ ...validPayload, email: 'taken@example.com' })).rejects.toMatchObject({
        response: { status: 400 },
      })
    })
  })

  // ─── checkUsername ────────────────────────────────────────────────────────

  describe('checkUsername()', () => {
    it('gọi POST /auth/check-username với payload', () => {
      checkUsername({ username: 'staff01' })
      expect(api.post).toHaveBeenCalledWith('/auth/check-username', { username: 'staff01' })
    })

    it('trả về available: true khi username khả dụng', async () => {
      api.post.mockResolvedValue({ data: { available: true } })
      const res = await checkUsername({ username: 'newuser' })
      expect(res.data.available).toBe(true)
    })

    it('trả về available: false khi username đã tồn tại', async () => {
      api.post.mockResolvedValue({ data: { available: false } })
      const res = await checkUsername({ username: 'takenuser' })
      expect(res.data.available).toBe(false)
    })
  })
})
