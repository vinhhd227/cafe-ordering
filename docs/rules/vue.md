# Frontend Rules — Vue 3 / Vite

## Auto-imports (không cần import thủ công)

Các symbol sau tự động available trong mọi `.vue` file — **đừng import lại**, linter sẽ báo duplicate:

```js
// Vue core
ref, reactive, computed, watch, watchEffect,
onMounted, onUnmounted, onBeforeMount, ...

// Vue Router
useRouter, useRoute

// Vee-Validate
useField, useForm

// Vee-Validate + Zod
toTypedSchema

// Zod
z

// Pinia
useStore

// PrimeVue
useToast, useConfirm

// UI constants (src/layout/ui.js)
btnIcon, inputCustom, labelCustom, passwordCustom

// Composables (src/composables/ — auto-scan)
useTableCache, usePermission, ...

// Stores (src/stores/ — auto-scan)
useAuthStore, useThemeStore, useTableStateStore, ...
```

Khi thêm composable mới vào `src/composables/` hoặc store vào `src/stores/` → tự động được scan, không cần config thêm.

## Tailwind CSS — prefix `tw:` bắt buộc

Tất cả Tailwind classes **phải có prefix `tw:`** để tránh conflict với PrimeVue:

```html
<!-- ✅ Đúng -->
<div class="tw:flex tw:gap-2 tw:text-sm tw:rounded-xl">

<!-- ❌ Sai -->
<div class="flex gap-2 text-sm rounded-xl">
```

Modifier syntax:
```html
class="tw:hover:text-primary-400! tw:focus:ring-2! tw:dark:bg-gray-800"
```

## PrimeVue Components

- Prefix `prime-` cho tất cả PrimeVue components:

```html
<prime-button label="Lưu" />
<prime-data-table :value="items" />
<prime-input-text v-model="name" />
<prime-dialog v-model:visible="show" />
```

- Icons dùng Iconify, không dùng PrimeIcons:

```html
<iconify icon="ph:user-bold" />
<iconify icon="ph:trash" />
```

- `appCard` constant cho card/panel background — glassmorphism style dark mode:

```html
<!-- Dùng trên prime-card hoặc div đóng vai trò card -->
<prime-card :class="appCard">...</prime-card>
<div :class="appCard">...</div>
```

`appCard` hiện tại: `tw:bg-white! tw:dark:bg-white/3! tw:border! tw:border-slate-200! tw:shadow-sm! tw:dark:border-white/15! tw:dark:shadow-xl tw:dark:backdrop-blur-md!`
- Light mode: nền trắng, border slate-200, shadow nhẹ
- Dark mode: nền `white/3` (trong suốt nhẹ) + blur 12px → glassmorphism effect
- **Không tự set `bg-*` hay `backdrop-blur` trên card** — dùng `appCard` để đồng nhất toàn app

- `btnIcon` constant cho icon-only buttons:

```html
<prime-button :class="btnIcon" @click="delete">
  <iconify icon="ph:trash" />
</prime-button>
```

## Dark Mode

- Class `app-dark` thêm vào root `<div>` — PrimeVue tự nhận qua CSS selector
- Quản lý qua `useThemeStore()` (init trong `main.js`)
- Không hardcode màu, dùng PrimeVue design tokens: `text-primary-500`, `surface-100`, ...

## Axios Service Pattern

Mỗi feature có file service riêng: `src/services/[feature].service.js`

```js
// ✅ Luôn gửi {} cho PUT/POST không có body thực sự
export const activateUser = (id) => api.put(`/admin/users/${id}/activate`, {})
export const deactivateUser = (id) => api.put(`/admin/users/${id}/deactivate`, {})

// ✅ Có body thực sự
export const createProduct = (data) => api.post('/admin/products', data)
```

**Lý do:** FastEndpoints 6 yêu cầu `Content-Type: application/json` ngay cả khi không có body. Thiếu `{}` → `415 Unsupported Media Type`.

## Router Guards

Meta fields trong route config:

```js
{
  path: '/users',
  meta: {
    requiresAuth: true,       // redirect về /login nếu chưa đăng nhập
    adminOnly: true,          // kiểm tra role Admin
    requiredClaim: 'user.read' // kiểm tra trong user.permissions array
  }
}
```

## Pinia Auth Store (`useAuthStore`)

- `hydrateFromRefresh()` — gọi trong `main.js` khi app khởi động
- `scheduleTokenRefresh()` — tự động refresh 30 giây trước khi token hết hạn
- Concurrent refresh được dedup bằng `_refreshPromise` cache

Token flow:
1. App load → `hydrateFromRefresh()` → lấy access token mới
2. Mọi request → Bearer token tự động thêm qua request interceptor
3. 401 → auto refresh → retry (chỉ 1 lần, `_retry` flag)
4. Refresh thất bại → logout

## File Structure

```
src/
├── services/[feature].service.js   ← Axios wrappers theo feature
├── stores/[name].js                ← Pinia stores
├── composables/use[Name].js        ← Composables
├── views/[feature]/                ← Pages
│   ├── List.vue
│   ├── Create.vue
│   └── Edit.vue
└── components/                     ← Shared components
```

## Environment

- `admin/.env` commit vào git (không chứa secret)
- `VITE_API_BASE_URL=http://localhost:5095/api` — trỏ thẳng vào backend
- Vite proxy `/api → localhost:5095` trong `vite.config.js` là fallback
