# Claude Preview — Quy tắc sử dụng

## Nguyên tắc cốt lõi

- **User tự chạy** frontend dev server và backend — Claude không tự khởi động
- Claude **không được kill** process nào của user
- Claude **không gọi `preview_start`** trừ khi user yêu cầu xem preview

---

## Khi user yêu cầu xem preview

### Bước 1 — Lấy serverId bằng `preview_start`

`preview_start` sẽ reuse server nếu Claude đã từng start trước đó trên cùng port.  
Nếu port đang bị chiếm bởi server của user, tool sẽ phân bổ port khác (`autoPort: true`) — lúc đó **nhờ user dừng server của mình trước**, rồi gọi lại.

### Bước 2 — Dùng serverId để screenshot/navigate/inspect

```
preview_screenshot(serverId) → xem layout tổng thể
preview_navigate(serverId, url) → chuyển trang
preview_inspect(serverId, selector) → kiểm tra CSS cụ thể
```

---

## Cấu hình `.claude/launch.json`

Dùng Node + `shell: true` để hoạt động trên cả Mac lẫn Windows:

```json
{
  "version": "0.0.1",
  "configurations": [
    {
      "name": "admin-dev",
      "runtimeExecutable": "node",
      "runtimeArgs": ["-e", "require('child_process').spawn('pnpm',['dev'],{stdio:'inherit',shell:true})"],
      "port": 5173,
      "autoPort": true,
      "cwd": "admin"
    },
    {
      "name": "client-dev",
      "runtimeExecutable": "node",
      "runtimeArgs": ["-e", "require('child_process').spawn('pnpm',['dev'],{stdio:'inherit',shell:true})"],
      "port": 5174,
      "autoPort": true,
      "cwd": "client"
    }
  ]
}
```

---

## Không làm

- ❌ Tự gọi `preview_start` khi không được yêu cầu
- ❌ Kill process của user để giải phóng port
- ❌ Chạy `pnpm dev` / `dotnet run` qua Bash
