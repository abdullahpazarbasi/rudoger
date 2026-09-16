import type { FieldValues, Path, UseFormSetError } from "react-hook-form";
import { normalizeError } from "../../api/errors/client-error";

export function serverFieldPath(path: string): string {
  return path
    .replace(/^\$\.?/, "")
    .replaceAll(/\[(\d+)\]/g, ".$1")
    .split(".")
    .filter(Boolean)
    .map((segment) =>
      /^\d+$/.test(segment) ? segment : `${segment.charAt(0).toLowerCase()}${segment.slice(1)}`,
    )
    .join(".");
}

export function applyServerFieldErrors<T extends FieldValues>(
  error: unknown,
  setError: UseFormSetError<T>,
): void {
  const fieldErrors = normalizeError(error).details.fieldErrors;
  for (const [field, messages] of Object.entries(fieldErrors)) {
    const path = serverFieldPath(field);
    if (path !== "" && messages.length > 0) {
      setError(path as Path<T>, { type: "server", message: messages.join(" ") });
    }
  }
}
