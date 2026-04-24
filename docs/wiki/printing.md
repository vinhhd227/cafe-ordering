---
title: Hệ thống In tem & Bill
tags: [printing, thermal, escpos, label]
updated: 2026-04-24
---

# Hệ thống In tem & Bill

In tem đồ uống và bill/hóa đơn lên máy in thermal. Thiết kế mở rộng cho nhiều loại máy, nhiều kênh kết nối.

---

## Kiến trúc

```
POST /api/admin/print/drink-labels    POST /api/admin/print/bill
        │                                      │
PrintDrinkLabelsHandler              PrintBillHandler
        │                                      │
        ├─ IPrintFormatter (chọn theo FormatterType)
        │       ├── EscPosPrintFormatter  ← ESC/POS byte generation
        │       └── TsplPrintFormatter    ← TSPL (TSC label printers)
        │
        └─ IPrinterTransport (chọn theo TransportType)
                ├── UsbDeviceTransport    ← /dev/usb/lp0
                ├── TcpPrinterTransport   ← TCP socket IP:port (planned)
                └── WebUsbTransport       ← return bytes → frontend WebUSB (planned)
```

**2 abstraction độc lập:**
- `IPrintFormatter` — ngôn ngữ lệnh (ESC/POS, TSPL, ZPL...)
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
| `EscPos` | `ESC_POS` | ESC/POS — hầu hết máy thermal thông dụng |
| `Tspl` | `TSPL` | TSPL — TSC label printers (ITP3300, ...) |
| `Zpl` | `ZPL` | Zebra printers (planned) |
| `StarPrnt` | `STAR_PRNT` | Star Micronics (planned) |

### PrintTransportType

| Giá trị | DB | Mô tả |
|---------|----|-------|
| `UsbDevice` | `USB_DEVICE` | USB gắn trực tiếp vào server (`/dev/usb/lp0`) |
| `Tcp` | `TCP` | Ethernet/WiFi qua TCP socket (planned) |
| `WebUsb` | `WEB_USB` | USB gắn vào máy client, browser dùng WebUSB API |

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
config.Delete(deletedBy) / Restore()
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

Record truyền vào formatter để generate bytes cho tem đồ uống:

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

## ReceiptData (DTO)

Record truyền vào formatter để generate bytes cho bill:

```csharp
public record ReceiptData(
    string                     OrderNumber,
    string?                    TableCode,
    IReadOnlyList<ReceiptItem> Items,
    decimal                    Subtotal,
    decimal                    Discount,
    decimal                    Total,
    string?                    PaymentMethod,
    DateTime                   PrintedAt,
    string?                    CafeName          = null,
    string?                    CafeAddress       = null,
    string?                    CafePhone         = null,
    string?                    QrRaw             = null,   // nội dung QR code (VietQR, URL, ...)
    string?                    BankAccountName   = null,
    string?                    BankAccountNumber = null,
    string?                    BankBranch        = null,
    string?                    WifiName          = null,
    string?                    WifiPassword      = null
);
```

Các trường CafeInfo đọc từ `IConfiguration` trong `PrintBillHandler` — override qua env vars trong production.

---

## CafeInfo — Cấu hình thông tin quán

Khai báo trong `appsettings.json`, override qua env var (docker-compose prod):

```json
"CafeInfo": {
  "Name": "Cafe",
  "Address": "",
  "Phone": "",
  "QrRaw": "",
  "BankAccountName": "",
  "BankAccountNumber": "",
  "BankBranch": "",
  "WifiName": "",
  "WifiPassword": ""
}
```

Env var tương ứng (double underscore = nested key):
```
CAFE_NAME, CAFE_ADDRESS, CAFE_PHONE
CAFE_QR_RAW
CAFE_BANK_ACCOUNT_NAME, CAFE_BANK_ACCOUNT_NUMBER, CAFE_BANK_BRANCH
CAFE_WIFI_NAME, CAFE_WIFI_PASSWORD
```

---

## ESC/POS — Label layout (tem đồ uống)

Charwidth tự động theo khổ giấy: `paperWidthMm <= 60 ? 32 : 48` chars.

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Ban: A1                  ORD-001
────────────────────────────────
        Tra Sua Tran Chau        ← BOLD + 2x size
Nong | Da: It | Duong: Vua
Ghi chu: it ngot
────────────────────────────────
14:30 22/04                  1/3
[FEED + CUT]
```

---

## ESC/POS — Bill layout

```
================================================
5AM COFFEE                        ← BOLD
Khu dau gia DG1, Xuan Phuong, HN
Tel: 0865262826
================================================
Order: ORD-001              Ban: A1
Thoi gian: 14:30 24/04/2026
------------------------------------------------
 # Ten mon               SL   Don gia    T.tien
------------------------------------------------
 1 Ca phe sua da          2   30.000d    60.000d
 2 Tra sua                1   45.000d    45.000d
------------------------------------------------
Tong:                              105.000d
Giam gia:                          -10.000d
THANH TOAN:                         95.000d   ← BOLD
================================================
[QR CODE — native GS ( k]
HO KINH DOANH 5AM COFFEE
8807484976
BIDV-PGD Van Bao
================================================
Wifi: TenMang
Pass: MatKhau
================================================
Cam on quy khach!
[FEED + CUT]
```

**Lưu ý ESC/POS trên máy in Trung Quốc:**
- Cut command: dùng `ESC i` (`0x1B 0x69`) — **không** dùng `GS V B`
- QR code: dùng native `GS ( k` — **không** dùng raster/bitmap
- `GS v 0` (raster image) không được hỗ trợ trên nhiều máy Trung Quốc
- Charwidth 80mm = 48 chars: hiển thị cột đơn giá; 58mm = 32 chars: bỏ cột đơn giá, in sub-line `x.000d/cai`

---

## API Endpoints

| Method | Path | Permission | Mô tả |
|--------|------|------------|-------|
| `GET` | `/api/admin/printers` | `printer.read` | Danh sách máy in |
| `POST` | `/api/admin/printers` | `printer.create` | Thêm máy in |
| `PUT` | `/api/admin/printers/{id}` | `printer.update` | Cập nhật |
| `DELETE` | `/api/admin/printers/{id}` | `printer.delete` | Xóa |
| `PUT` | `/api/admin/printers/{id}/set-default` | `printer.update` | Đặt mặc định |
| `POST` | `/api/admin/printers/{id}/test` | `printer.update` | Test kết nối |
| `POST` | `/api/admin/print/drink-labels` | `order.print` | In tem đồ uống |
| `POST` | `/api/admin/print/bill` | `order.print` | In bill |

**PrintDrinkLabels request:**
```json
{
  "orderId": 42,
  "itemIds": [1, 3],   // null = in tất cả items
  "printerId": null,   // null = dùng default DRINK_LABEL printer
  "copiesPerItem": 1
}
```

**PrintBill request:**
```json
{
  "orderId": 42,
  "printerId": null    // null = dùng default RECEIPT printer
}
```

**Response — server-side print:**
```json
{ "success": true, "requiresClientPrint": false, "bytes": null, "error": null }
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

## Frontend

```
admin/src/views/printers/
└── List.vue     ← group by role, badge "Mặc định", nút Test Connection

admin/src/components/printing/
├── PrintDrinkLabelsDialog.vue   ← chọn items, số bản, máy in
└── PrintBillDialog.vue          ← chọn máy in RECEIPT, in bill

admin/src/services/
└── printer.service.js
```

**Nơi tích hợp:**
- `orders/Detail.vue` — nút "In tem" + nút "In bill" (v-if `can('order.print')`)
- Cả 2 dialog hỗ trợ WebUSB fallback: nếu `requiresClientPrint = true` → dùng WebUSB API

---

## Networking — Máy in qua switch nội bộ (TCP)

Khi dùng transport `TCP`, server và máy in chỉ cần cùng subnet — **không cần router**:

```
Server (eth0: 192.168.2.1)
       |
    [Switch]
       |
Máy in (192.168.2.20:9100)
```

- Server vừa có WiFi (internet) vừa có LAN (eth0 nối switch) — 2 interface độc lập
- Máy in set IP tĩnh thủ công (qua LCD / in trang config / tool của hãng)
- Cấu hình printer trong app: transport = `TCP`, host = IP máy in, port = `9100`

**Set IP tĩnh cho eth0 trên server (Netplan):**
```yaml
# /etc/netplan/01-lan.yaml
network:
  ethernets:
    eth0:
      addresses: [192.168.2.1/24]
  version: 2
```

**Test kết nối:**
```bash
nc -zv 192.168.2.20 9100
```

---

## Thêm Transport mới (ví dụ TCP)

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

---

## Thêm Formatter mới

```csharp
// Implement IPrintFormatter:
//   Supports(type) → true nếu type khớp
//   FormatDrinkLabel(data, config) → byte[]
//   FormatReceipt(data, config) → byte[]  (throw NotSupportedException nếu không hỗ trợ)
//   FormatTestPage(config) → byte[]
// Đăng ký: services.AddScoped<IPrintFormatter, ZplPrintFormatter>();
```

---

## File liên quan

| File | Mô tả |
|------|-------|
| `Api.Core/Aggregates/PrinterAggregate/PrinterConfig.cs` | Entity |
| `Api.Core/Aggregates/PrinterAggregate/PrinterRole.cs` | SmartEnum role |
| `Api.Core/Aggregates/PrinterAggregate/PrintFormatterType.cs` | SmartEnum formatter |
| `Api.Core/Aggregates/PrinterAggregate/PrintTransportType.cs` | SmartEnum transport |
| `Api.Core/Aggregates/PrinterAggregate/Specifications/` | 4 spec classes |
| `Api.Infrastructure/Printing/Abstractions/IPrintFormatter.cs` | Interface formatter |
| `Api.Infrastructure/Printing/Abstractions/IPrinterTransport.cs` | Interface transport |
| `Api.Infrastructure/Printing/Abstractions/DrinkLabelData.cs` | DTO tem đồ uống |
| `Api.Infrastructure/Printing/Abstractions/ReceiptData.cs` | DTO bill |
| `Api.Infrastructure/Printing/Formatters/EscPosPrintFormatter.cs` | ESC/POS |
| `Api.Infrastructure/Printing/Formatters/TsplPrintFormatter.cs` | TSPL (TSC) |
| `Api.Infrastructure/Printing/Transports/UsbDeviceTransport.cs` | USB device |
| `Api.Infrastructure/Printing/PrintingService.cs` | Orchestration |
| `Api.Infrastructure/Printing/PrintingServiceExtensions.cs` | DI registration |
| `Api.Infrastructure/Data/Config/PrinterConfigConfiguration.cs` | EF config |
| `Api.UseCases/Printing/Interfaces/IPrintingService.cs` | Service interface |
| `Api.UseCases/Printing/PrintLabels/PrintDrinkLabelsHandler.cs` | Handler in tem |
| `Api.UseCases/Printing/PrintBill/PrintBillHandler.cs` | Handler in bill |
| `Api.Web/Endpoints/Printing/PrintDrinkLabels.cs` | Endpoint in tem |
| `Api.Web/Endpoints/Printing/PrintBill.cs` | Endpoint in bill |
| `Api.Infrastructure/Identity/PermissionRegistry.cs` | printer.* permissions |
| `admin/src/components/printing/PrintDrinkLabelsDialog.vue` | Dialog in tem |
| `admin/src/components/printing/PrintBillDialog.vue` | Dialog in bill |
| `admin/src/views/printers/List.vue` | Trang quản lý máy in |
| `admin/src/services/printer.service.js` | Axios wrappers |
