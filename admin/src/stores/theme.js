import { defineStore } from "pinia";
import { updatePreset } from "@primeuix/themes";

export const PRIMARY_COLORS = [
  { name: "emerald", label: "Emerald", hex: "#10b981", darkBg: "#08110e", lightBg: "#f2f8f5" },
  { name: "violet",  label: "Violet",  hex: "#8b5cf6", darkBg: "#0c0917", lightBg: "#f3f0f9" },
  { name: "blue",    label: "Blue",    hex: "#3b82f6", darkBg: "#090c18", lightBg: "#f0f3f9" },
  { name: "rose",    label: "Rose",    hex: "#f43f5e", darkBg: "#17080b", lightBg: "#f9f0f2" },
  { name: "amber",   label: "Amber",   hex: "#f59e0b", darkBg: "#151008", lightBg: "#f8f5ee" },
  { name: "sky",     label: "Sky",     hex: "#0ea5e9", darkBg: "#080f17", lightBg: "#f0f5f9" },
  { name: "orange",  label: "Orange",  hex: "#f97316", darkBg: "#150d08", lightBg: "#f9f3ee" },
];

// Google Fonts có Vietnamese subset chất lượng
export const FONTS = [
  { name: "Inter",             label: "Inter",             family: "'Inter', system-ui, sans-serif",             url: "https://fonts.googleapis.com/css2?family=Inter:ital,opsz,wght@0,14..32,100..900;1,14..32,100..900&display=swap" },
  { name: "Plus Jakarta Sans", label: "Plus Jakarta Sans", family: "'Plus Jakarta Sans', system-ui, sans-serif", url: "https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:ital,wght@0,200..800;1,200..800&display=swap" },
  { name: "Outfit",            label: "Outfit",            family: "'Outfit', system-ui, sans-serif",            url: "https://fonts.googleapis.com/css2?family=Outfit:wght@100..900&display=swap" },
  { name: "DM Sans",           label: "DM Sans",           family: "'DM Sans', system-ui, sans-serif",           url: "https://fonts.googleapis.com/css2?family=DM+Sans:ital,opsz,wght@0,9..40,100..1000;1,9..40,100..1000&display=swap" },
  { name: "Be Vietnam Pro",    label: "Be Vietnam Pro",    family: "'Be Vietnam Pro', system-ui, sans-serif",    url: "https://fonts.googleapis.com/css2?family=Be+Vietnam+Pro:ital,wght@0,100;0,300;0,400;0,500;0,600;0,700;0,800;0,900&display=swap" },
  { name: "Nunito",            label: "Nunito",            family: "'Nunito', system-ui, sans-serif",            url: "https://fonts.googleapis.com/css2?family=Nunito:ital,wght@0,200..1000;1,200..1000&display=swap" },
  { name: "Manrope",           label: "Manrope",           family: "'Manrope', system-ui, sans-serif",           url: "https://fonts.googleapis.com/css2?family=Manrope:wght@200..800&display=swap" },
];

const loadedFonts = new Set();
const ensureFontLoaded = (url) => {
  if (loadedFonts.has(url)) return;
  loadedFonts.add(url);
  const link = document.createElement("link");
  link.rel = "stylesheet";
  link.href = url;
  document.head.appendChild(link);
};

const buildPalette = (colorName) =>
  Object.fromEntries(
    [50, 100, 200, 300, 400, 500, 600, 700, 800, 900, 950].map((k) => [
      k,
      `{${colorName}.${k}}`,
    ]),
  );

const injectColorVars = (color) => {
  const r = parseInt(color.hex.slice(1, 3), 16);
  const g = parseInt(color.hex.slice(3, 5), 16);
  const b = parseInt(color.hex.slice(5, 7), 16);
  let el = document.getElementById("app-dynamic-theme");
  if (!el) {
    el = document.createElement("style");
    el.id = "app-dynamic-theme";
    document.head.appendChild(el);
  }
  el.textContent = [
    `:root { --app-background-glow: rgba(${r},${g},${b},0.15); --app-bg: ${color.lightBg}; }`,
    `:root.app-dark { --app-background-glow: rgba(${r},${g},${b},0.35); --app-bg: ${color.darkBg}; }`,
  ].join("\n");
};

export const useThemeStore = defineStore("theme", {
  state: () => ({
    isDark: false,
    initialized: false,
    primaryColor: "emerald",
    font: "Inter",
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
    applyPrimaryColor(colorName) {
      this.primaryColor = colorName;
      localStorage.setItem("primaryColor", colorName);
      updatePreset({ semantic: { primary: buildPalette(colorName) } });
      const color = PRIMARY_COLORS.find((c) => c.name === colorName);
      if (color) injectColorVars(color);
    },
    applyFont(fontName) {
      const font = FONTS.find((f) => f.name === fontName) ?? FONTS[0];
      this.font = font.name;
      localStorage.setItem("font", font.name);
      ensureFontLoaded(font.url);
      document.documentElement.style.setProperty("--app-font", font.family);
    },
    init() {
      if (this.initialized) return;
      const savedTheme = localStorage.getItem("theme");
      if (savedTheme) {
        this.applyTheme(savedTheme === "dark");
      } else {
        this.applyTheme(document.documentElement.classList.contains("app-dark"));
      }
      const savedColor = localStorage.getItem("primaryColor");
      const colorName = savedColor && PRIMARY_COLORS.some((c) => c.name === savedColor)
        ? savedColor
        : "emerald";
      this.applyPrimaryColor(colorName);
      const savedFont = localStorage.getItem("font");
      const fontName = savedFont && FONTS.some((f) => f.name === savedFont)
        ? savedFont
        : "Inter";
      this.applyFont(fontName);
      this.initialized = true;
    },
  },
});
