---
title: Hệ thống In tem & Bill
tags: [printing, thermal, escpos, label]
updated: 2026-04-22
---

# Hệ thống In tem & Bill

In tem đồ uống lên máy in thermal. Thiết kế mở rộng cho nhiều loại máy, nhiều kênh kết nối.

---

## Kiến trúc

```
POST /api/admin/print/drink-labels
        │
PrintDrinkLabelsHandler
        │
        ├─ IPrintFormatter (chọn theo FormatterType)
        │       └── EscPosPrintFormatter  ← ESC/POS byte generation
        │
        └─ IPrinterTransport (chọn theo TransportType)
                ├── UsbDeviceTransport    ← /dev/usb/lp0 (đã implement)
                ├── TcpPrinterTransport   ← TCP socket IP:port (planned)
                └── WebUsbTransport       ← return bytes → frontend WebUSB (planned)
```

**2 abstraction độc lập:**
- `IPrintFormatter` — ngôn ngữ lệnh (ESC/POS, ZPL, STAR PRNT)
- `IPrinterTransport` — kênh kết nối (USB, TCP, WebUSB)

Thêm máy mới = thêm 1 formatter hoặc 1 transport, không đụng code cũ.

---

## SmartEnums

### PrinterRole

| Giá trị | DB | Mô tả |
|---------|----|-------|
| `DrinkLabel` | `DRINK_LABEL` | Máy in tem đồ uống |
| `Receipt` | `RECEIPT` | Máy in bill/hóa đơn |
| `Kitchen` | `KITCHEN` | Máy in phiếu bếp (planned) |

### PrintFormatterType

| Giá trị | DB | Mô tả |
|---------|----|-------|
| `EscPos` | `ESC_POS` | EPSON ESC/POS — hầu hết máy thermal thông dụng |
| `Zpl` | `ZPL` | Zebra printers (planned) |
| `StarPrnt` | `STAR_PRNT` | Star Micronics (planned) |

### PrintTransportType

| Giá trị | DB | Mô tả |
|---------|----|-------|
| `UsbDevice` | `USB_DEVICE` | USB gắn trực tiếp vào server |
| `Tcp` | `TCP` | Ethernet/WiFi qua TCP socket (planned) |
| `WebUsb` | `WEB_USB` | USB gắn vào máy client, browser dùng WebUSB API (planned) |

---

## PrinterConfig Entity

**File:** `Api.Core/Aggregates/PrinterAggregate/PrinterConfig.cs`
**Base:** `SoftDeletableEntity<int>`, `IAggregateRoot`

| Property | Type | Mô tả |
|----------|------|-------|
| `Name` | string | Tên hiển thị — VD: "Quầy Bar - ITP3300" |
| `Role` | `PrinterRole` | Loại máy (tem / bill / bếp) |
| `FormatterType` | `PrintFormatterType` | Ngôn ngữ lệnh |
| `TransportType` | `PrintTransportType` | Kênh kết nối |
| `ConnectionParams` | string (JSON) | Thông số kết nối (xem bên dưới) |
| `PaperWidthMm` | int | Khổ giấy: 58 / 80 / ... |
| `IsDefault` | bool | Máy mặc định cho role này |
| `IsActive` | bool | |

**ConnectionParams JSON theo transport:**
```json
// UsbDevice
{ "devicePath": "/dev/usb/lp0" }

// Tcp
{ "host": "192.168.1.50", "port": 9100 }

// WebUsb
{ "vendorId": "0x04B8", "productId": "0x0E27" }
```

**Behaviors:**
```csharp
PrinterConfig.Create(name, role, formatterType, transportType, connectionParams, paperWidthMm)
config.Update(name, formatterType, transportType, connectionParams, paperWidthMm)
config.SetAsDefault() / UnsetDefault()
config.Activate() / Deactivate()
config.Delete(deletedBy) / Restore()   // kế thừa từ SoftDeletableEntity
```

---

## Specifications

| Spec | Dùng khi |
|------|----------|
| `AllPrinterConfigsSpec` | List tất cả (group by role, default đầu tiên) |
| `PrinterConfigsByRoleSpec(role)` | List theo role |
| `PrinterConfigByIdSpec(id)` | Lấy theo id |
| `DefaultPrinterByRoleSpec(role)` | Lấy default printer của role — dùng trong print handler |

---

## DrinkLabelData (DTO)

Record truyền vào formatter để generate bytes:

```csharp
public record DrinkLabelData(
    string   OrderNumber,
    string?  TableCode,
    string   ProductName,
    int      Quantity,
    string?  Temperature,   // "HOT" / "COLD"
    string?  IceLevel,      // "LESS" / "NORMAL" / "MORE"
    string?  SugarLevel,    // "LESS" / "NORMAL" / "MORE"
    string?  Note,
    bool     IsTakeaway,
    int      ItemIndex,     // 1-based
    int      TotalItems,
    DateTime PrintedAt
);
```

---

## ESC/POS Label layout (58mm)

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Ban: A1                  ORD-001
────────────────────────────────
        Trà Sữa Trân Châu        ← BOLD + 2x size
Nong | Da: It | Duong: Vua
Ghi chu: it ngot
────────────────────────────────
14:30 22/04                  1/3
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
[FEED 3 + PARTIAL CUT]
```

Charwidth tự động theo khổ giấy: `paperWidthMm <= 60 ? 32 : 48` chars.

---

## API Endpoints (Phase 2 — planned)

| Method | Path | Permission | Mô tả |
|--------|------|------------|-------|
| `GET` | `/api/admin/printers` | `printer.read` | Danh sách máy in |
| `POST` | `/api/admin/printers` | `printer.create` | Thêm máy in |
| `PUT` | `/api/admin/printers/{id}` | `printer.update` | Cập nhật |
| `DELETE` | `/api/admin/printers/{id}` | `printer.delete` | Xóa |
| `PUT` | `/api/admin/printers/{id}/set-default` | `printer.update` | Đặt mặc định |
| `POST` | `/api/admin/printers/{id}/test` | `printer.update` | Test kết nối |
| `POST` | `/api/admin/print/drink-labels` | `order.print` | In tem đồ uống |

**PrintDrinkLabels request:**
```json
{
  "orderId": 42,
  "itemIds": [1, 3],   // null = in tất cả items
  "printerId": null,   // null = dùng default của role
  "role": null,        // null = DrinkLabel
  "copiesPerItem": 1
}
```

**Response — WebUsb transport:**
```json
{
  "success": false,
  "requiresClientPrint": true,
  "bytes": "G0BAG0Bh...",   // base64 ESC/POS bytes
  "error": null
}
```
Frontend nhận `bytes`, dùng WebUSB API để ghi trực tiếp xuống máy in.

---

## Permissions

| Permission | Admin | Staff |
|------------|-------|-------|
| `printer.read` | ✅ | ✅ |
| `printer.create` | ✅ | — |
| `printer.update` | ✅ | — |
| `printer.delete` | ✅ | — |
| `order.print` | ✅ | ✅ |

---

## Frontend (Phase 3 — planned)

```
admin/src/views/printers/
├── List.vue     ← group by role, badge "Mặc định", nút Test Connection
├── Create.vue
└── Edit.vue

admin/src/components/printing/
└── DrinkLabelPreview.vue   ← simulate label trên màn hình (CSS, không cần backend)

admin/src/services/
└── printer.service.js
```

Form params động theo transport:
- `UsbDevice` → Device Path (`/dev/usb/lp0`)
- `Tcp` → IP Address + Port
- `WebUsb` → Vendor ID + Product ID

Print dialog trong `orders/Detail.vue`: chọn items, số bản, máy in, xem preview trước khi in.

---

## Thêm Transport mới

Ví dụ thêm TCP transport:

```csharp
// 1. Tạo TcpPrinterTransport.cs
public class TcpPrinterTransport(ILogger<TcpPrinterTransport> logger) : IPrinterTransport
{
    public bool Supports(PrintTransportType type) => type == PrintTransportType.Tcp;

    public async Task SendAsync(byte[] data, string connectionParamsJson, CancellationToken ct = default)
    {
        var p = Parse(connectionParamsJson); // { host, port }
        using var client = new TcpClient();
        await client.ConnectAsync(p.Host, p.Port, ct);
        await using var stream = client.GetStream();
        await stream.WriteAsync(data, ct);
    }

    public async Task<bool> TestConnectionAsync(string connectionParamsJson, CancellationToken ct = default)
    {
        var p = Parse(connectionParamsJson);
        using var client = new TcpClient();
        try { await client.ConnectAsync(p.Host, p.Port, ct); return true; }
        catch { return false; }
    }
}

// 2. Đăng ký trong PrintingServiceExtensions.cs
services.AddScoped<IPrinterTransport, TcpPrinterTransport>();
```

**Lưu ý:** Nếu nhiều transport cùng register, handler resolve bằng `IEnumerable<IPrinterTransport>` và dùng `transport.Supports(config.TransportType)` để chọn đúng instance.

---

## Thêm Formatter mới

```csharp
// Implement IPrintFormatter, return Supports(ZPL) = true
// Đăng ký: services.AddScoped<IPrintFormatter, ZplPrintFormatter>();
```

---

## File liên quan

| File                                                             | Mô tả                 |
| ---------------------------------------------------------------- | --------------------- |
| `Api.Core/Aggregates/PrinterAggregate/PrinterConfig.cs`          | Entity                |
| `Api.Core/Aggregates/PrinterAggregate/PrinterRole.cs`            | SmartEnum role        |
| `Api.Core/Aggregates/PrinterAggregate/PrintFormatterType.cs`     | SmartEnum formatter   |
| `Api.Core/Aggregates/PrinterAggregate/PrintTransportType.cs`     | SmartEnum transport   |
| `Api.Core/Aggregates/PrinterAggregate/Specifications/`           | 4 spec classes        |
| `Api.Infrastructure/Printing/Abstractions/IPrintFormatter.cs`    | Interface             |
| `Api.Infrastructure/Printing/Abstractions/IPrinterTransport.cs`  | Interface             |
| `Api.Infrastructure/Printing/Abstractions/DrinkLabelData.cs`     | DTO                   |
| `Api.Infrastructure/Printing/Formatters/EscPosPrintFormatter.cs` | ESC/POS               |
| `Api.Infrastructure/Printing/Transports/UsbDeviceTransport.cs`   | USB device            |
| `Api.Infrastructure/Printing/PrintingServiceExtensions.cs`       | DI registration       |
| `Api.Infrastructure/Data/Config/PrinterConfigConfiguration.cs`   | EF config             |
| `Api.Infrastructure/Identity/PermissionRegistry.cs`              | printer.* permissions |
