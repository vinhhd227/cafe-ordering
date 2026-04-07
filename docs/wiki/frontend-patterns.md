---
title: Frontend Patterns
tags: [frontend, vue, pinia, axios, conventions]
updated: 2026-04-07
---

# Frontend Patterns

Áp dụng cho cả `admin/` và `client/`. Admin phức tạp hơn (nhiều feature hơn), client đơn giản hơn (chỉ dành cho khách đặt món).

Xem thêm: [[architecture]], [[auth-flow]]

---

## Auto-imports (không cần import thủ công)

`vite.config.js` cấu hình `unplugin-auto-import` — các symbol sau **không cần `import`** trong bất kỳ `.vue` file:

```js
// Vue core
ref, reactive, computed, watch, watchEffect, onMounted, onUnmounted, ...

// Vue Router
useRouter, useRoute

// Vee-Validate + Zod
useField, useForm, toTypedSchema, z

// Pinia
useStore

// PrimeVue
useToast, useConfirm

// UI constants (admin/src/layout/ui.js)
btnIcon, inputCustom, labelCustom, passwordCustom

// Tất cả composables trong src/composables/ (tự động scan)
useTableCache, usePermission, useLocale, useSidebar, ...

// Tất cả stores trong src/stores/ (tự động scan)
useAuthStore, useThemeStore, useNotificationStore, ...
```

> Thêm composable mới vào `src/composables/` hoặc store vào `src/stores/` → tự động được scan, không cần config thêm.

---

## Tailwind với prefix `tw:`

Tất cả Tailwind classes phải có prefix `tw:` để tránh conflict với PrimeVue:

```html
<!-- Đúng -->
<div class="tw:flex tw:gap-2 tw:text-sm tw:rounded-xl">
<button class="tw:hover:text-primary-400! tw:focus:ring-2!">

<!-- Sai — không có prefix -->
<div class="flex gap-2">
```

---

## UI Components

- **PrimeVue** với prefix `prime-` (đã config trong `primeVue.js`):
  ```html
  <prime-button label="Lưu" />
  <prime-data-table :value="items" />
  <prime-input-text v-model="name" />
  ```

- **Iconify** cho icons:
  ```html
  <iconify icon="ph:user-bold" class="tw:text-xl" />
  ```

- **btnIcon** — constant cho icon-only buttons:
  ```js
  // admin/src/layout/ui.js
  export const btnIcon = 'tw:w-8! tw:h-8! tw:p-0! tw:flex tw:items-center tw:justify-center'

  // Dùng trong template (auto-import):
  <prime-button :class="btnIcon" severity="secondary">
    <iconify icon="ph:trash-bold" />
  </prime-button>
  ```

---

## Dark Mode

- Class `app-dark` thêm vào root `<div>` — PrimeVue tự nhận qua CSS selector
- `useThemeStore()` quản lý state, init trong `main.js`

---

## Axios Service Pattern

### Instance singleton (`services/axios.js`)

```js
// Request interceptor:
// - Tự động thêm Bearer token: Authorization: Bearer <accessToken>
// - Xóa Content-Type nếu config.data là null/undefined (tránh 415 khi retry)

// Response interceptor:
// - 401 → tự động refresh token → retry request gốc (chỉ 1 lần, _retry flag)
// - Refresh thất bại → logout
```

### Service file per feature

Mỗi feature có file `services/[feature].service.js` riêng:
```js
// services/table.service.js
export const getTables = (params) => api.get('/admin/tables', { params })
export const createTable = (data) => api.post('/admin/tables', data)
export const activateTable = (id) => api.put(`/admin/tables/${id}/activate`, {})
//                                                                             ^^
//                                         LUÔN gửi {} cho PUT/POST không có body
```

> **Quan trọng:** FastEndpoints 6 yêu cầu `Content-Type: application/json` ngay cả khi không có body. Frontend phải gửi `{}` cho tất cả PUT/POST endpoint không payload thực sự. Nếu không → `415 Unsupported Media Type`.

---

## Pinia Stores

### Auth Store (`stores/auth.js`)

```js
const auth = useAuthStore()
auth.isAuthenticated   // bool
auth.user              // { id, username, fullName, role, permissions }
auth.accessToken       // string | null

await auth.login({ username, password })
await auth.logout()
await auth.hydrateFromRefresh()  // gọi khi app mount
auth.scheduleTokenRefresh()      // tự động gọi sau login/refresh
```

**Token flow:**
1. App load → `hydrateFromRefresh()` → POST /api/auth/refresh (cookie) → khôi phục session
2. Mọi request → Bearer token tự động (request interceptor)
3. 401 → auto refresh → retry (tối đa 1 lần)
4. Refresh thất bại → `auth.logout()` → redirect `/login`
5. `scheduleTokenRefresh()` → setTimeout 30s trước hết hạn → proactive refresh

### Cart Store (`stores/cart.js`) — chỉ trong `/client`

```js
const cart = useCartStore()
cart.items           // CartItem[]
cart.total           // decimal
cart.itemCount       // int

cart.addItem(product, quantity, options)
cart.removeItem(productId)
cart.updateQuantity(productId, quantity)
cart.clear()
```

---

## Router Guards (`router/index.js`)

```js
// Meta options:
{ meta: { requiresAuth: true } }           // phải đăng nhập
{ meta: { adminOnly: true } }              // phải là Admin role
{ meta: { requiredClaim: 'table.create' } } // phải có permission claim

// Guard logic:
// 1. Đợi hydration hoàn thành (auth.hydrateFromRefresh)
// 2. requiresAuth + !isAuthenticated → redirect /login
// 3. adminOnly + role != 'Admin' → redirect /403
// 4. requiredClaim + !user.permissions.includes(claim) → redirect /403
```

---

## i18n

Dự án dùng `vue-i18n`. Locale files: `src/i18n/locales/vi.json` và `en.json`.

```js
// Composable (auto-import)
const { t } = useLocale()
// Template
{{ t('table.create') }}
```

Xem thêm: `docs/rules/i18n.md`

---

## Form Validation (Vee-Validate + Zod)

```js
// Định nghĩa schema (auto-import z từ zod)
const schema = toTypedSchema(z.object({
  name: z.string().min(1, 'Bắt buộc'),
  price: z.number().positive('Phải > 0'),
}))

// Setup form
const { handleSubmit, errors } = useForm({ validationSchema: schema })
const { value: name } = useField('name')
const { value: price } = useField('price')

// Submit
const onSubmit = handleSubmit(async (values) => {
  await createProduct(values)
})
```

---

## File quan trọng

| File | Mô tả |
|------|-------|
| `admin/vite.config.js` | Vite config, auto-import, Tailwind prefix, proxy |
| `admin/src/main.js` | App entry, hydrate auth, mount |
| `admin/src/router/index.js` | Routes + guards |
| `admin/src/layout/ui.js` | UI class constants |
| `admin/src/layout/nav.js` | Navigation menu config |
| `admin/src/plugins/primeVue.js` | PrimeVue setup với prefix |
| `admin/src/plugins/iconify.js` | Iconify setup |
| `admin/src/services/axios.js` | Axios instance + interceptors |
