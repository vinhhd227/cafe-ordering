# ☕ Cafe Ordering System

Nền tảng quản lý và gọi món cho quán cafe, xây theo kiến trúc **Monorepo**. Dự án dùng **.NET 10** cho backend và **Vue 3/Vite** cho frontend, được điều phối bằng **Docker**.

English version: [README.md](README.md)

---

## 🏗 Cấu Trúc Dự Án

Dự án gồm 3 service chính:

* **`/api`**: Backend Web API bằng ASP.NET Core (.NET 10).
    * Theo Clean Architecture (Domain, Application, Infrastructure, EndPoints).
    * Được container hóa bằng Docker để đồng nhất môi trường dev/prod.
* **`/client`**: Ứng dụng frontend cho khách bằng Vue 3 + Vite.
    * Dùng `pnpm` để quản lý dependencies.
    * Có `Dockerfile.dev` tối ưu cho hot-reload trong Docker.
* **`/admin`**: Dashboard quản trị bằng Vue 3 + Vite.
    * Dùng chung workflow Docker dev với client.
    * Chạy cổng riêng để dùng song song với `/client`.

---

## ⚙️ Thiết Lập Cho Developer

Sau khi clone repo, hãy tạo các file config local **trước khi chạy** (các file này đã được git ignore để tránh lộ credentials).

### 1. API — `appsettings.Development.json`

```bash
cp api/src/Api.Web/appsettings.Development.json.example \
   api/src/Api.Web/appsettings.Development.json
```

Mở file vừa tạo và thay toàn bộ giá trị `CHANGE_ME`:

| Key | Mô tả |
|---|---|
| `ConnectionStrings:DefaultConnection` | Mật khẩu PostgreSQL local |
| `Jwt:Key` | Chuỗi ngẫu nhiên ít nhất 32 ký tự |
| `SmtpSettings:Username/Password` | Tài khoản SMTP (có thể bỏ qua ở môi trường dev) |

> **Tip:** Chạy `grep -r "CHANGE_ME" .` để kiểm tra chỗ nào chưa thay.

### 2. Docker — `docker-compose.dev.override.yml`

```bash
cp docker-compose.dev.override.yml.example docker-compose.dev.override.yml
```

Điền credentials thực vào file rồi chạy:

```bash
docker-compose -f docker-compose.dev.yml -f docker-compose.dev.override.yml up --build
```

### 3. Vue Apps — `.env.local`

```bash
cp client/.env.local.example client/.env.local
cp admin/.env.local.example admin/.env.local
```

---

## 🚀 Chạy Nhanh Bằng Docker

Đây là cách nhanh nhất để chạy toàn bộ hệ thống (Database, API, Client, Admin) mà không cần cài đặt thủ công từng phần.

### Yêu cầu
* Đã cài và mở [Docker Desktop](https://www.docker.com/products/docker-desktop/).

### Tùy chọn: Truy cập từ thiết bị khác cùng mạng
Tạo file `.env` ở root để Compose inject thông tin mạng host:

```bash
cat > .env <<'EOF'
HOST_IP=192.168.1.10
API_PORT=8080
EOF
```

Khi đó các thiết bị khác có thể truy cập:
* Client: `http://<HOST_IP>:5173`
* Admin: `http://<HOST_IP>:5174`

### Các bước cài đặt
1.  **Clone repository:**
    ```bash
    git clone <your-repo-url>
    cd cafe-ordering
    ```

2.  **Tạo file config local** (xem phần [Thiết Lập Cho Developer](#️-thiết-lập-cho-developer)).

3.  **Chạy Docker Compose:**
    ```bash
    docker-compose -f docker-compose.dev.yml -f docker-compose.dev.override.yml up --build
    ```

4.  **Truy cập ứng dụng:**
    * **Frontend (Client):** `http://localhost:5173`
    * **Frontend (Admin):** `http://localhost:5174`
    * **Backend (API):** `http://localhost:8080`
    * **OpenAPI (Development):** `http://localhost:8080/openapi/v1.json`

---

## 💻 Phát Triển Local (Không Docker)

Nếu bạn muốn chạy từng service trực tiếp trên máy:

Trước khi chạy local, cần đồng bộ config giữa frontend và API:
* CORS ở API phải chứa đúng origin frontend bạn dùng (ví dụ `http://localhost:5173` và `http://localhost:5174`).
* `client/.env.local` và `admin/.env.local` phải đặt `VITE_API_BASE_URL` trỏ đúng backend (ví dụ `http://localhost:8080/api`).

### Backend (API)
0. Đảm bảo đã có `api/src/Api.Web/appsettings.Development.json` và điền đúng giá trị local (`ConnectionStrings`, `Jwt`, `SmtpSettings`), không để `CHANGE_ME`.
1. Vào thư mục api: `cd api`
2. Restore dependencies: `dotnet restore`
3. Chạy app: `dotnet run`

### Frontend (Client/Admin)
Dùng cùng một bộ lệnh cho cả `client` và `admin`:
1. Vào thư mục ứng dụng: `cd client` hoặc `cd admin`
2. Cài dependencies: `pnpm install`
3. Chạy dev server: `pnpm dev`

---

## 🛠 Tech Stack

| Thành phần | Công nghệ |
| :--- |:---|
| **Backend** | .NET 10, ASP.NET Core OpenAPI |
| **Frontend** | Vue 3, Vite, pnpm |
| **Database** | PostgreSQL (Docker) |
| **DevOps** | Docker, Docker Compose, Alpine Linux |
| **Tooling** | WebStorm / Rider, Postman |

---

## 🔄 Luồng Hệ Thống

Hiện tại API đang có endpoint mẫu (`/weatherforecast`) và Vue client ở trạng thái scaffold. Luồng dự kiến:

1. **Khách hàng**: Quét QR -> mở web gọi món -> đặt món.
2. **Backend**: **.NET API** nhận order -> lưu vào **PostgreSQL**.
3. **Thông báo realtime**: **SSE** đẩy đơn mới đến **Admin Web**.
4. **In hóa đơn**: **Admin Web** gọi luồng in (hoặc API gửi lệnh ESC/POS).

---

## 📝 Ghi Chú Quan Trọng

* **Biến môi trường**: Client/Admin dùng `VITE_API_BASE_URL` (từ Compose) để gọi API. Nếu cần truy cập từ thiết bị khác, đặt `HOST_IP` trong `.env` ở root.
* **Architecture mismatch**: Nếu gặp lỗi `rollup-linux-arm64-musl`, kiểm tra `.dockerignore` đã exclude `node_modules` đúng chưa.
* **Database migration**: Ở lần chạy đầu, API có thể đợi DB healthy trước khi apply migration (khi migration được thêm vào).
* **IDE config**: Thư mục `.idea` chứa cấu hình cục bộ của JetBrains IDE, nên để ngoài Git trừ khi thật sự cần chia sẻ.

---

## 🤝 Đóng Góp

Mọi đóng góp đều được chào đón. Bạn có thể tạo **Pull Request** hoặc mở **Issue** cho bug và đề xuất tính năng.
