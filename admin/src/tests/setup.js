import { createPinia, setActivePinia } from 'pinia'

// Tạo Pinia instance mới trước mỗi test để tránh state bị chia sẻ giữa các test
beforeEach(() => {
  setActivePinia(createPinia())
})
