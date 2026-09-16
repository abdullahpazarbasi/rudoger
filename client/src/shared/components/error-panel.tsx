import { normalizeError } from "../../api/errors/client-error";
import { translateFieldName } from "../../api/errors/problem-translations";

interface ErrorPanelProps {
  error: unknown;
  compact?: boolean;
}

export function ErrorPanel({ error, compact = false }: ErrorPanelProps) {
  const normalized = normalizeError(error).details;
  const fieldErrors = Object.entries(normalized.fieldErrors);
  const originalFieldErrors = Object.entries(normalized.originalFieldErrors);
  return (
    <section
      aria-live="polite"
      className={`rounded-lg border border-[color-mix(in_srgb,var(--danger)_35%,var(--border))] bg-[var(--danger-soft)] text-[var(--danger)] ${compact ? "p-3" : "p-4"}`}
      role="alert"
    >
      <h2 className="font-semibold">{normalized.title}</h2>
      <p className="mt-1 text-sm">{normalized.detail}</p>
      {fieldErrors.length > 0 && (
        <div className="mt-3 text-sm">
          <p className="font-semibold">Alan doğrulama hataları</p>
          <ul className="mt-1 list-disc space-y-1 pl-5">
            {fieldErrors.map(([field, messages]) => (
              <li key={field}>
                <span className="font-semibold">{translateFieldName(field)}:</span>{" "}
                {messages.join(" ")}
              </li>
            ))}
          </ul>
        </div>
      )}
      <details className="mt-3 text-sm">
        <summary className="cursor-pointer font-semibold">Teknik ayrıntılar</summary>
        <dl className="mt-2 grid gap-1 break-all">
          {normalized.status !== null && (
            <div>
              <dt className="inline font-semibold">HTTP durumu: </dt>
              <dd className="inline">{normalized.status}</dd>
            </div>
          )}
          {normalized.type !== null && (
            <div>
              <dt className="inline font-semibold">Problem türü: </dt>
              <dd className="inline">{normalized.type}</dd>
            </div>
          )}
          {normalized.instance !== null && (
            <div>
              <dt className="inline font-semibold">İstek yolu: </dt>
              <dd className="inline">{normalized.instance}</dd>
            </div>
          )}
          {normalized.correlationId !== null && (
            <div>
              <dt className="inline font-semibold">Correlation ID: </dt>
              <dd className="inline">{normalized.correlationId}</dd>
            </div>
          )}
          {normalized.originalTitle !== null && (
            <div>
              <dt className="inline font-semibold">Özgün başlık: </dt>
              <dd className="inline">{normalized.originalTitle}</dd>
            </div>
          )}
          {normalized.originalDetail !== null && (
            <div>
              <dt className="inline font-semibold">Özgün ayrıntı: </dt>
              <dd className="inline">{normalized.originalDetail}</dd>
            </div>
          )}
          {originalFieldErrors.map(([field, messages]) => (
            <div key={field}>
              <dt className="inline font-semibold">Özgün alan hatası · {field}: </dt>
              <dd className="inline">{messages.join(" ")}</dd>
            </div>
          ))}
        </dl>
      </details>
    </section>
  );
}
