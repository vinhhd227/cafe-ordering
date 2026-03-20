import { defineStore } from "pinia";

export const useThemeStore = defineStore("theme", {
  state: () => ({
    isDark: true,
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
      // Default to dark if no saved preference
      this.applyTheme(savedTheme ? savedTheme === "dark" : true);
      this.initialized = true;
    },
  },
});
