import { useTheme } from "./use-theme";
import type { ThemePreference } from "./theme-store";

const options: Array<{ value: ThemePreference; label: string }> = [
  { value: "system", label: "Sistem" },
  { value: "light", label: "Açık" },
  { value: "dark", label: "Koyu" },
];

export function ThemeToggle() {
  const [theme, setTheme] = useTheme();
  return (
    <fieldset
      aria-label="Görünüm"
      className="inline-flex rounded-lg border bg-[var(--surface-subtle)] p-0.5"
    >
      <legend className="sr-only">Görünüm</legend>
      {options.map((option) => (
        <label
          className={`cursor-pointer rounded-md px-2.5 py-1.5 text-sm font-medium ${theme === option.value ? "bg-[var(--surface)] text-[var(--text)] shadow-sm" : "text-[var(--muted)]"}`}
          key={option.value}
        >
          <input
            checked={theme === option.value}
            className="sr-only"
            name="theme"
            onChange={() => setTheme(option.value)}
            type="radio"
            value={option.value}
          />
          {option.label}
        </label>
      ))}
    </fieldset>
  );
}
