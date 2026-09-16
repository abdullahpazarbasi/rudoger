import { useSyncExternalStore } from "react";
import { themeStore, type ThemePreference } from "./theme-store";

export function useTheme(): readonly [ThemePreference, (theme: ThemePreference) => void] {
  const theme = useSyncExternalStore(
    themeStore.subscribe.bind(themeStore),
    themeStore.getSnapshot,
    themeStore.getSnapshot,
  );
  return [theme, themeStore.set.bind(themeStore)] as const;
}
