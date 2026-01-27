# ☕ Cafe Ordering System

A cafe management and ordering platform built with a **Monorepo** architecture. This project uses **.NET 10** for the backend and **Vue 3/Vite** for the frontend, orchestrated with **Docker**.

---

## 🏗 Project Structure

The project is organized into two main services:

* **`/api`**: Backend Web API built with ASP.NET Core (.NET 10).
    * Follows Clean Architecture principles (Domain, Application, Infrastructure, EndPoints).
    * Containerized using Docker for consistent environments across development and production.
* **`/client`**: Frontend web application built with Vue 3 and Vite.
    * Managed with `pnpm` for fast and disk-efficient dependency management.
    * Optimized with a dedicated `Dockerfile.dev` for hot-reloading inside Docker.

---

## 🚀 Quick Start (Using Docker)

The fastest way to get the entire ecosystem (Database, API, and Client) up and running without manual local setup.

### Prerequisites
* [Docker Desktop](https://www.docker.com/products/docker-desktop/) installed and running.

### Installation Steps
1.  **Clone the repository:**
    ```bash
    git clone <your-repo-url>
    cd cafe-ordering
    ```

2.  **Launch with Docker Compose:**
    ```bash
    docker-compose -f docker-compose.dev.yml up --build
    ```

3.  **Access the applications:**
    * **Frontend (Client):** `http://localhost:5173`
    * **Backend (API):** `http://localhost:8080`
    * **OpenAPI (Development):** `http://localhost:8080/openapi/v1.json`

---

## 💻 Local Development

If you prefer to run services individually on your host machine:

### Backend (API)
1.  Navigate to the api directory: `cd api`
2.  Restore dependencies: `dotnet restore`
3.  Run the application: `dotnet run`

### Frontend (Client)
1.  Navigate to the client directory: `cd client`
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

* **Environment Variables**: Ensure you have configured your `.env` (for client) and `appsettings.Development.json` (for api) before running.
* **Architecture Mismatch**: If you encounter errors related to `rollup-linux-arm64-musl`, ensure your `.dockerignore` correctly excludes `node_modules`. This prevents host-machine binaries from leaking into the Alpine-based Docker container.
* **Database Migrations**: On the first run, the API may wait for the Database container to be healthy before applying migrations (once migrations are added).
* **IDE Configuration**: The `.idea` folder contains project-specific settings for JetBrains IDEs. It is recommended to keep this excluded from Git unless sharing specific Run Configurations.

---

## 🤝 Contributing
Contributions are welcome! Please feel free to submit a **Pull Request** or open an **Issue** for any bugs or feature requests.