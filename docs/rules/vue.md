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

**Ưu tiên class có sẵn thay vì arbitrary values** — dùng scale mặc định của Tailwind khi có thể:

```html
<!-- ✅ Đúng -->
<div class="tw:max-h-100">   <!-- 400px = 100 * 4px -->
<div class="tw:w-[360px]">   <!-- không có class chuẩn tương đương -->

<!-- ❌ Sai — arbitrary value khi có class sẵn -->
<div class="tw:max-h-[400px]">
```

**Dùng tên class hiện đại** — Tailwind v3+ có các alias ngắn hơn:

| Cũ | Mới |
|----|-----|
| `tw:flex-shrink-0` | `tw:shrink-0` |
| `tw:flex-shrink` | `tw:shrink` |
| `tw:flex-grow-0` | `tw:grow-0` |
| `tw:flex-grow` | `tw:grow` |
| `tw:overflow-ellipsis` | `tw:text-ellipsis` |

**Không dùng inline style cho sizing/spacing** — dùng Tailwind thay thế:

```html
<!-- ✅ Đúng -->
<prime-dialog v-model:visible="show" class="tw:w-[360px]">

<!-- ❌ Sai -->
<prime-dialog v-model:visible="show" :style="{ width: '360px' }">
```

Inline style chỉ chấp nhận khi giá trị là dynamic (tính từ biến JS) và không có Tailwind tương đương, ví dụ: `:style="{ backgroundColor: primaryColor }"`.

## Label và form elements

`<label>` **phải có `for`** trỏ đúng vào `id` của input đi kèm:

```html
<!-- ✅ Đúng -->
<label for="cafe-name" class="...">Tên quán</label>
<prime-input-text id="cafe-name" v-model="cafeName" />

<!-- ❌ Sai — label không có for -->
<label class="...">Tên quán</label>
<prime-input-text v-model="cafeName" />
```

Nếu không có input/form component đi kèm (chỉ hiển thị text), **dùng `<p>` hoặc `<span>`** thay vì `<label>`:

## Button — ưu tiên prime-button

Luôn dùng `<prime-button>` thay vì `<button>` HTML thuần:

```html
<!-- ✅ Đúng -->
<prime-button severity="secondary" outlined @click="handleClick">
  <iconify icon="ph:trash-bold" />
  <span>Xóa</span>
</prime-button>

<!-- ❌ Sai -->
<button class="..." @click="handleClick">Xóa</button>
```

`<button>` HTML chỉ chấp nhận trong các trường hợp đặc biệt khi `prime-button` không phù hợp về mặt kỹ thuật (ví dụ: button bên trong template preview xuất PNG/PDF vì `html-to-image`/`html2canvas` capture toàn bộ DOM).

```html
<!-- ✅ Đúng — chỉ là text mô tả, không liên kết với input -->
<p class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">Template</p>

<!-- ❌ Sai -->
<label class="tw:text-xs tw:uppercase tw:tracking-widest tw:text-muted">Template</label>
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

## Dynamic font loading (MenuDesign pattern)

Khi cần load font động trong một component (ví dụ: MenuDesign), dùng pattern sau:

**fontOptions là `ref([])` không phải `const`** — vì user có thể thêm font upload vào danh sách:

```js
const fontOptions = ref([
  { label: 'Georgia (Serif)', value: 'Georgia, serif' },
  // Google Fonts có thêm field googleFamily
  { label: 'Playfair Display', value: "'Playfair Display', serif", googleFamily: 'Playfair+Display:wght@400;700;900' },
])
```

**Google Fonts — inject `<link>` động:**

```js
const loadedGoogleFonts = new Set()

const ensureGoogleFont = async (googleFamily) => {
  if (loadedGoogleFonts.has(googleFamily)) return
  loadedGoogleFonts.add(googleFamily)
  const link = document.createElement('link')
  link.rel = 'stylesheet'
  link.href = `https://fonts.googleapis.com/css2?family=${googleFamily}&display=swap`
  document.head.appendChild(link)
  await document.fonts.ready
}

watch(menuFont, async (val) => {
  const opt = fontOptions.value.find(f => f.value === val)
  if (opt?.googleFamily) await ensureGoogleFont(opt.googleFamily)
})
```

**Upload font tùy chỉnh — base64 + FontFace API:**

```js
const handleFontUpload = async (event) => {
  const file = event.target.files?.[0]
  if (!file) return
  const fontName = `UploadedFont-${Date.now()}`
  const base64 = await new Promise(resolve => {
    const reader = new FileReader()
    reader.onload = e => resolve(e.target.result)
    reader.readAsDataURL(file)
  })
  // Inject @font-face CSS — cần thiết để html-to-image embed vào PNG export
  const style = document.createElement('style')
  style.textContent = `@font-face { font-family: '${fontName}'; src: url('${base64}'); }`
  document.head.appendChild(style)
  // FontFace API — render ngay trong preview
  const face = new FontFace(fontName, `url(${base64})`)
  await face.load()
  document.fonts.add(face)
  fontOptions.value.push({ label: file.name.replace(/\.[^.]+$/, ''), value: `'${fontName}', sans-serif`, isCustom: true })
  menuFont.value = `'${fontName}', sans-serif`
  event.target.value = '' // reset để upload lại cùng file
}
```

- Font upload chỉ tồn tại trong session hiện tại (không persist qua reload)
- Trước khi export PNG/PDF: `await document.fonts.ready` để đảm bảo font đã load

## Environment

- `admin/.env` commit vào git (không chứa secret)
- `VITE_API_BASE_URL=http://localhost:5095/api` — trỏ thẳng vào backend
- Vite proxy `/api → localhost:5095` trong `vite.config.js` là fallback
