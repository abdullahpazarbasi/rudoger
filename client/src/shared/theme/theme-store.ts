export type ThemePreference = "system" | "light" | "dark";

const STORAGE_KEY = "rudoger.theme";
const listeners = new Set<() => void>();

function isTheme(value: string | null): value is ThemePreference {
  return value === "system" || value === "light" || value === "dark";
}

let preference: ThemePreference = (() => {
  if (typeof localStorage === "undefined") {
    return "system";
  }
  const stored = localStorage.getItem(STORAGE_KEY);
  return isTheme(stored) ? stored : "system";
})();

function resolvedTheme(): "light" | "dark" {
  if (preference !== "system") {
    return preference;
  }
  return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
}

function applyTheme(): void {
  const resolved = resolvedTheme();
  document.documentElement.classList.toggle("dark", resolved === "dark");
  document.documentElement.style.colorScheme = resolved;
}

export const themeStore = {
  getSnapshot: (): ThemePreference => preference,
  subscribe(listener: () => void): () => void {
    listeners.add(listener);
    return () => listeners.delete(listener);
  },
  set(next: ThemePreference): void {
    if (preference === next) {
      return;
    }
    preference = next;
    localStorage.setItem(STORAGE_KEY, next);
    applyTheme();
    listeners.forEach((listener) => listener());
  },
};

export function startThemeLifecycle(): () => void {
  const media = window.matchMedia("(prefers-color-scheme: dark)");
  const onMediaChange = (): void => {
    if (preference === "system") {
      applyTheme();
    }
  };
  const onStorage = (event: StorageEvent): void => {
    if (event.key !== STORAGE_KEY || !isTheme(event.newValue)) {
      return;
    }
    preference = event.newValue;
    applyTheme();
    listeners.forEach((listener) => listener());
  };
  media.addEventListener("change", onMediaChange);
  window.addEventListener("storage", onStorage);
  applyTheme();
  return () => {
    media.removeEventListener("change", onMediaChange);
    window.removeEventListener("storage", onStorage);
  };
}
