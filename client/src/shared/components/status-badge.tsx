import type { ReactNode } from "react";

export function StatusBadge({
  children,
  tone = "neutral",
}: {
  children: ReactNode;
  tone?: "neutral" | "success" | "danger" | "pending";
}) {
  return <span className={`status-badge status-${tone}`}>{children}</span>;
}
