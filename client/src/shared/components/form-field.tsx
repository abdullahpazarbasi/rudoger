import type { InputHTMLAttributes, ReactNode, SelectHTMLAttributes } from "react";

interface FieldShellProps {
  label: string;
  htmlFor: string;
  error?: string | undefined;
  hint?: string | undefined;
  children: ReactNode;
}

function FieldShell({ label, htmlFor, error, hint, children }: FieldShellProps) {
  const descriptionId = `${htmlFor}-description`;
  return (
    <div className="grid gap-1.5">
      <label className="text-sm font-semibold" htmlFor={htmlFor}>
        {label}
      </label>
      {children}
      {(error ?? hint) !== undefined && (
        <p
          className={`text-sm ${error === undefined ? "text-[var(--muted)]" : "text-[var(--danger)]"}`}
          id={descriptionId}
        >
          {error ?? hint}
        </p>
      )}
    </div>
  );
}

interface TextFieldProps extends InputHTMLAttributes<HTMLInputElement> {
  label: string;
  error?: string | undefined;
  hint?: string | undefined;
}

export function TextField({ label, error, hint, id, ...props }: TextFieldProps) {
  const inputId = id ?? props.name ?? crypto.randomUUID();
  const describedBy =
    error !== undefined || hint !== undefined ? `${inputId}-description` : undefined;
  return (
    <FieldShell error={error} hint={hint} htmlFor={inputId} label={label}>
      <input
        aria-describedby={describedBy}
        aria-invalid={error !== undefined}
        className="field"
        id={inputId}
        {...props}
      />
    </FieldShell>
  );
}

interface SelectFieldProps extends SelectHTMLAttributes<HTMLSelectElement> {
  label: string;
  error?: string | undefined;
  children: ReactNode;
}

export function SelectField({ label, error, id, children, ...props }: SelectFieldProps) {
  const inputId = id ?? props.name ?? crypto.randomUUID();
  return (
    <FieldShell error={error} htmlFor={inputId} label={label}>
      <select
        aria-describedby={error === undefined ? undefined : `${inputId}-description`}
        aria-invalid={error !== undefined}
        className="field"
        id={inputId}
        {...props}
      >
        {children}
      </select>
    </FieldShell>
  );
}
