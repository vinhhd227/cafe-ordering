// stores/auth.js
import { defineStore } from "pinia";
import {
  login,
  refresh,
  //register,
  logout as logoutRequest,
} from "@/services/auth.service.js";

const ROLE_CLAIM =
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

const parseJwt = (token) => {
  try {
     const payload = token.split(".")[1];

    const base64 = payload
      .replaceAll("-", "+")
      .replaceAll("_", "/");

    const json = atob(base64);

    // decode UTF-8 đúng cách
    const bytes = Uint8Array.from(json, (c) => c.codePointAt(0));
    const decoded = new TextDecoder().decode(bytes);

    return JSON.parse(decoded);
  } catch {
    return {};
  }
};

const userFromToken = (token) => {
  const p = parseJwt(token);
  return {
    id: p.sub ?? "",
    username: p.username ?? "",
    fullName: p.fullName ?? "",
    avatarUrl: p.avatarUrl ?? null,
    roles: toArray(p[ROLE_CLAIM]),
    permissions: toArray(p.permission),
  };
};

const toArray = (value) => {
  if (Array.isArray(value)) return value;
  if (value) return [value];
  return [];
};

export const useAuthStore = defineStore("auth", {
  state: () => ({
    accessToken: null,
    user: null,
    expiresAt: null,
    refreshTimer: null,
    refreshAttempts: 0,
    hydrated: false,
    hydrating: false,
    refreshing: false,
  }),
  getters: {
    isAuthenticated: (state) => !!state.accessToken,
  },

  actions: {
    async login(payload) {
      const res = await login(payload);
      this.accessToken = res.data.accessToken;
      this.user = userFromToken(res.data.accessToken);
      localStorage.setItem("user", JSON.stringify(this.user));
      this.expiresAt = res.data.expiresAt;
      this.scheduleTokenRefresh();
      return res;
    },

    async doRefreshToken() {
      // Dedup: nếu đang refresh thì trả về promise hiện tại
      if (this.refreshing && this._refreshPromise) {
        return this._refreshPromise;
      }

      // Set refreshing = true TRƯỚC khi tăng attempts để tránh race condition:
      // nếu 2 request cùng bị 401, cả 2 thấy refreshing = false đồng thời
      // → cả 2 cùng vào, attempts = 2 → logout ngay mà không thực sự refresh
      this.refreshing = true;
      this.refreshAttempts += 1;

      if (this.refreshAttempts > 1) {
        this.refreshing = false;
        this.logout();
        throw new Error("Refresh attempt exceeded");
      }

      this._refreshPromise = (async () => {
        const res = await refresh();
        this.accessToken = res.data.accessToken;
        this.expiresAt = res.data.expiresAt;
        this.user = userFromToken(res.data.accessToken);
        localStorage.setItem("user", JSON.stringify(this.user));
        this.scheduleTokenRefresh();
        this.refreshAttempts = 0;
        return res;
      })()
        .catch((err) => {
          this.clearTokenRefresh();
          // Refresh token không còn hợp lệ → force logout ngay, không chờ lần thứ 2
          const status = err?.response?.status;
          if (status === 401 || status === 403) {
            this.logout();
          }
          throw err;
        })
        .finally(() => {
          this.refreshing = false;
          this._refreshPromise = null;
        });

      return this._refreshPromise;
    },

    async hydrateFromRefresh() {
      if (this.hydrated) return;
      if (this.hydrating && this._hydratePromise) {
        return this._hydratePromise;
      }
      this.refreshAttempts = 0; // clean slate on every page load
      this.hydrating = true;
      this._hydratePromise = (async () => {
        try {
          await this.doRefreshToken();
        } catch (err) {
          console.error("Refresh token failed:", err);
          this.accessToken = null;
        } finally {
          this.hydrated = true;
          this.hydrating = false;
        }
      })();
      return this._hydratePromise;
    },

    clearTokenRefresh() {
      if (this.refreshTimer) {
        clearTimeout(this.refreshTimer);
        this.refreshTimer = null;
      }
    },

    scheduleTokenRefresh() {
      this.clearTokenRefresh();
      if (!this.accessToken) return;
      const payload = parseJwt(this.accessToken);
      if (!payload.exp) return;
      const expiresAtMs = payload.exp * 1000; // exp là Unix timestamp tính bằng giây
      const refreshAtMs = Math.max(expiresAtMs - 30_000, Date.now() + 1_000);
      const delay = refreshAtMs - Date.now();
      this.refreshTimer = setTimeout(() => {
        this.doRefreshToken().catch(() => {
          // Proactive refresh thất bại (network blip, server restart, ...)
          // Reset attempts để interceptor vẫn có thể retry khi 401 thực sự xảy ra
          this.refreshAttempts = 0;
        });
      }, delay);
    },

    logout() {
      this.accessToken = null;
      localStorage.removeItem("user");
      this.user = null;
      this.expiresAt = null;
      this.clearTokenRefresh();
      this.refreshAttempts = 0;
      this.refreshing = false;
      this._refreshPromise = null;
      this.hydrated = false;
      logoutRequest().catch(() => {});
      const loc = globalThis.location;

      if (loc && loc.pathname !== "/login") {
        loc.replace("/login");
      }
    },
  },
});
