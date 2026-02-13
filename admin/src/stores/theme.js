import { defineStore } from "pinia";

export const useThemeStore = defineStore("theme", {
  state: () => ({
    isDark: false,
    initialized: false,
  }),
  actions: {
    applyTheme(value) {
      this.isDark = value;
      document.documentElement.classList.toggle("app-dark", value);
      localStorage.setItem("theme", value ? "dark" : "light");
    },
    toggleTheme() {
      this.applyTheme(!this.isDark);
    },
    init() {
      if (this.initialized) return;
      const savedTheme = localStorage.getItem("theme");
      if (savedTheme) {
        this.applyTheme(savedTheme === "dark");
      } else {
        this.applyTheme(document.documentElement.classList.contains("app-dark"));
      }
      this.initialized = true;
    },
  },
});
