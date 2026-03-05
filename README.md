# ☕ Cafe Ordering System

A cafe management and ordering platform built with a **Monorepo** architecture. This project uses **.NET 10** for the backend and **Vue 3/Vite** for the frontend, orchestrated with **Docker**.

Vietnamese version: [README.vi.md](README.vi.md)

---

## 🏗 Project Structure

The project is organized into three main services:

* **`/api`**: Backend Web API built with ASP.NET Core (.NET 10).
    * Follows Clean Architecture principles (Domain, Application, Infrastructure, EndPoints).
    * Containerized using Docker for consistent environments across development and production.
* **`/client`**: Frontend web application built with Vue 3 and Vite.
    * Managed with `pnpm` for fast and disk-efficient dependency management.
    * Optimized with a dedicated `Dockerfile.dev` for hot-reloading inside Docker.
* **`/admin`**: Admin dashboard built with Vue 3 and Vite.
    * Shares Docker development workflow with the client app.
    * Runs on a dedicated dev port for parallel usage with `/client`.

---

## ⚙️ Developer Setup

After cloning the repository, create local config files **before running** (these files are git-ignored to avoid committing credentials).

### 1. API — `appsettings.Development.json`

```bash
cp api/src/Api.Web/appsettings.Development.json.example \
   api/src/Api.Web/appsettings.Development.json
```

Open the created file and replace all `CHANGE_ME` values:

| Key | Description |
|---|---|
| `ConnectionStrings:DefaultConnection` | Local PostgreSQL password |
| `Jwt:Key` | Random string with at least 32 characters |
| `SmtpSettings:Username/Password` | SMTP account credentials (optional in dev) |

> **Tip:** Run `grep -r "CHANGE_ME" .` to find any remaining placeholders.

### 2. Docker — `docker-compose.dev.override.yml`

```bash
cp docker-compose.dev.override.yml.example docker-compose.dev.override.yml
```

Fill real credentials in that file, then run:

```bash
docker-compose -f docker-compose.dev.yml -f docker-compose.dev.override.yml up --build
```

### 3. Vue Apps — `.env.local`

```bash
cp client/.env.local.example client/.env.local
cp admin/.env.local.example admin/.env.local
```

---

## 🚀 Quick Start (Using Docker)

The fastest way to get the entire ecosystem (Database, API, Client, and Admin) up and running without manual local setup.

### Prerequisites
* [Docker Desktop](https://www.docker.com/products/docker-desktop/) installed and running.

### Optional: Access from other devices on the same network
Create a root `.env` file so Compose can inject host network settings:

```bash
cat > .env <<'EOF'
HOST_IP=192.168.1.10
API_PORT=8080
EOF
```

Then other devices can access:
* Client: `http://<HOST_IP>:5173`
* Admin: `http://<HOST_IP>:5174`

### Installation Steps
1.  **Clone the repository:**
    ```bash
    git clone <your-repo-url>
    cd cafe-ordering
    ```

2.  **Create local config files** (see [Developer Setup](#️-developer-setup)).

3.  **Launch with Docker Compose:**
    ```bash
    docker-compose -f docker-compose.dev.yml -f docker-compose.dev.override.yml up --build
    ```

4.  **Access the applications:**
    * **Frontend (Client):** `http://localhost:5173`
    * **Frontend (Admin):** `http://localhost:5174`
    * **Backend (API):** `http://localhost:8080`
    * **OpenAPI (Development):** `http://localhost:8080/openapi/v1.json`

---

## 💻 Local Development

If you prefer to run services individually on your host machine:

Before running services locally, make sure frontend and API settings match:
* API CORS must include the frontend origins you are using (for example `http://localhost:5173` and `http://localhost:5174`).
* `client/.env.local` and `admin/.env.local` must set `VITE_API_BASE_URL` to the correct backend URL (for example `http://localhost:8080/api`).

### Backend (API)
0. Ensure `api/src/Api.Web/appsettings.Development.json` exists and has valid local values (`ConnectionStrings`, `Jwt`, `SmtpSettings`), not `CHANGE_ME`.
1.  Navigate to the api directory: `cd api`
2.  Restore dependencies: `dotnet restore`
3.  Run the application: `dotnet run`

### Frontend (Client/Admin)
Use the same commands for both `client` and `admin`:
1.  Navigate to the app directory: `cd client` or `cd admin`
2.  Install dependencies: `pnpm install`
3.  Start the dev server: `pnpm dev`

---

## 🛠 Tech Stack

| Component | Technology                                 |
| :--- |:-------------------------------------------|
| **Backend** | .NET 10, ASP.NET Core OpenAPI            |
| **Frontend** | Vue 3, Vite, pnpm                        |
| **Database** | PostgreSQL (Docker)                      |
| **DevOps** | Docker, Docker Compose, Alpine Linux       |
| **Tooling** | WebStorm / Rider, Postman                 |

---

## 🔄 System Workflow

Currently, the API exposes a sample endpoint (`/weatherforecast`) and the Vue client is scaffolded. The workflow below is intended and will be updated as features land:

1. **Customer**: Scans QR code -> Opens **Order Web** -> Places an order.
2. **Backend**: **.NET API** receives order -> Saves to **PostgreSQL**.
3. **Real-time Notification**: **SSE** pushes the new order alert to **Admin Web**.
4. **Printing**: **Admin Web** triggers the Receipt Printer (or API sends ESC/POS command).

---
## 📝 Important Notes

* **Environment Variables**: Client/Admin use `VITE_API_BASE_URL` (from Compose) to call API. Set `HOST_IP` in root `.env` when accessing from other devices.
* **Architecture Mismatch**: If you encounter errors related to `rollup-linux-arm64-musl`, ensure your `.dockerignore` correctly excludes `node_modules`. This prevents host-machine binaries from leaking into the Alpine-based Docker container.
* **Database Migrations**: On the first run, the API may wait for the Database container to be healthy before applying migrations (once migrations are added).
* **IDE Configuration**: The `.idea` folder contains project-specific settings for JetBrains IDEs. It is recommended to keep this excluded from Git unless sharing specific Run Configurations.

---

## 🤝 Contributing
Contributions are welcome! Please feel free to submit a **Pull Request** or open an **Issue** for any bugs or feature requests.
