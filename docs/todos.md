# Todos / Ý tưởng cần làm

## Trang thông báo — Infinite scroll

**Ý tưởng:** Chuyển trang thông báo từ paging sang infinite scroll.

**Việc cần làm:**
- [ ] Backend: thêm param `cursor` (hoặc `lastId`) thay cho `page` — trả về N items tiếp theo sau id đó
- [ ] Frontend (`admin/src/views/notifications/`): bỏ `<Paginator>`, dùng `IntersectionObserver` trên sentinel element cuối list để trigger load thêm
- [ ] Hiển thị skeleton loader khi đang fetch thêm
- [ ] Xử lý trường hợp không còn data (ẩn loader, hiện "Đã tải hết")

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
