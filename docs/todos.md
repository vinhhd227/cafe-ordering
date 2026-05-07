# Todos / Ý tưởng cần làm

## Trang thông báo — Infinite scroll

**Ý tưởng:** Chuyển trang thông báo từ paging sang infinite scroll.

**Việc cần làm:**
- [ ] Backend: thêm param `cursor` (hoặc `lastId`) thay cho `page` — trả về N items tiếp theo sau id đó
- [ ] Frontend (`admin/src/views/notifications/`): bỏ `<Paginator>`, dùng `IntersectionObserver` trên sentinel element cuối list để trigger load thêm
- [ ] Hiển thị skeleton loader khi đang fetch thêm
- [ ] Xử lý trường hợp không còn data (ẩn loader, hiện "Đã tải hết")

---

## Identity Service — Technical Debt

- [ ] Thêm trigram index (`pg_trgm`) cho `UserName` và `FullName` khi bảng Users lớn lên — hiện tại `LIKE '%text%'` full scan, chưa cần vội
- [ ] Xem xét optimistic concurrency cho `SetRolePermissionsAsync` và `ResetUserPasswordAsync` khi scale — hiện tại last-write-wins, đủ cho SMB
- [ ] Optimize `GetUserPermissionsAsync` thành 1 JOIN query thay vì 2 query — hiện tại đủ tốt
- [ ] `ChangeUserRoleAsync`: đổi thứ tự Add trước Remove để giảm risk transient failure giữa transaction
- [ ] Access token không revoke được ngay lập tức (JWT stateless limitation) — nếu cần: hoặc rút ngắn expiry (~5 phút) hoặc thêm `jti` blacklist vào Redis
  
## Jwt Service — Technical Debt
- [x] `JwtService.ValidateToken()` đã đổi thành `GetPrincipalFromExpiredToken()` cho rõ intent
- [ ] JWT token phình to khi permissions nhiều (80 permissions × ~25 chars = ~2KB chỉ riêng claims, dễ vượt cookie 4KB / reverse proxy header limit)
  - Hướng xử lý: bỏ `permission` claims khỏi JWT, chỉ giữ `roles`
  - Server resolve `role → permissions` qua IMemoryCache hoặc Redis, invalidate khi `SetRolePermissionsAsync` được gọi
  - Auth middleware/policy handler đọc permissions từ cache thay vì từ token
- [ ] Thiếu `token_type` claim trong access token — nếu sau này có email verify/password reset/websocket token thì dễ reuse nhầm. Thêm `new("token_type", "access")` vào claims và validate trong `GetPrincipalFromExpiredToken`
- [ ] Thiếu `kid` (Key ID) header trong JWT — cần khi rotate signing key zero-downtime (multi-key scenario). Hiện tại single key nên chưa cần

---

## Networking — Máy in qua switch nội bộ

**Ý tưởng:** Nối server và máy in vào cùng 1 switch (không qua router) để gửi lệnh in qua LAN.

- Server có WiFi (kết nối internet) + cổng LAN (eth0) nối switch
- Máy in nối switch, assign IP tĩnh thủ công
- Hai thiết bị cùng subnet → giao tiếp trực tiếp, không cần router

**Việc cần làm:**
- [ ] Xác định model máy in để biết cách set IP tĩnh (LCD / in trang config / tool hãng)
- [ ] Assign IP tĩnh cho eth0 trên server (subnet riêng, ví dụ `192.168.2.1/24`)
- [ ] Set IP tĩnh cho máy in (ví dụ `192.168.2.20`)
- [ ] Cấu hình printer trong app: transport = TCP, host = IP máy in, port = 9100
- [ ] Test kết nối từ server: `nc -zv 192.168.2.20 9100`
