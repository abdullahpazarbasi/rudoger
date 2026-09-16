import type { ReactNode } from "react";

interface WorkPageProps {
  title: string;
  description: string;
  action?: ReactNode;
  children: ReactNode;
}

export function WorkPage({ title, description, action, children }: WorkPageProps) {
  return (
    <section>
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">{title}</h1>
          <p className="mt-1 text-[var(--muted)]">{description}</p>
        </div>
        {action}
      </div>
      <div className="mt-6">{children}</div>
    </section>
  );
}
