import {
  translateKnownDetail,
  translateProblemTitle,
  translateValidationMessage,
} from "./problem-translations";

export type ClientErrorKind = "problem" | "network" | "contract" | "browser" | "unknown";

export interface ClientErrorDetails {
  kind: ClientErrorKind;
  title: string;
  detail: string;
  status: number | null;
  type: string | null;
  instance: string | null;
  correlationId: string | null;
  fieldErrors: Record<string, string[]>;
  originalFieldErrors: Record<string, string[]>;
  originalTitle: string | null;
  originalDetail: string | null;
}

export class ClientError extends Error {
  public readonly details: ClientErrorDetails;

  public constructor(details: ClientErrorDetails, options?: ErrorOptions) {
    super(details.detail, options);
    this.name = "ClientError";
    this.details = details;
  }
}

function asRecord(value: unknown): Record<string, unknown> {
  return typeof value === "object" && value !== null ? (value as Record<string, unknown>) : {};
}

function asString(value: unknown): string | null {
  return typeof value === "string" ? value : null;
}

function fieldErrors(value: unknown): Record<string, string[]> {
  const record = asRecord(value);
  return Object.fromEntries(
    Object.entries(record).map(([field, messages]) => [
      field,
      Array.isArray(messages)
        ? messages.filter((item): item is string => typeof item === "string")
        : [],
    ]),
  );
}

export function problemError(body: unknown, response: Response): ClientError {
  const problem = asRecord(body);
  const type = asString(problem.type);
  const originalTitle = asString(problem.title);
  const originalDetail = asString(problem.detail);
  const title = translateProblemTitle(type, "İstek tamamlanamadı.");
  const detail = translateKnownDetail(originalDetail) ?? title;
  const originalFieldErrors = fieldErrors(problem.errors);
  const translatedFieldErrors = Object.fromEntries(
    Object.entries(originalFieldErrors).map(([field, messages]) => [
      field,
      messages.map((message) => translateValidationMessage(field, message)),
    ]),
  );
  return new ClientError({
    kind: "problem",
    title,
    detail,
    status: typeof problem.status === "number" ? problem.status : response.status,
    type,
    instance: asString(problem.instance),
    correlationId: asString(problem.correlationId) ?? response.headers.get("X-Correlation-Id"),
    fieldErrors: translatedFieldErrors,
    originalFieldErrors,
    originalTitle: originalTitle === title ? null : originalTitle,
    originalDetail: originalDetail === null || originalDetail === detail ? null : originalDetail,
  });
}

export function networkError(error: unknown): ClientError {
  const detail =
    error instanceof Error ? error.message : "Ağ hatasının teknik ayrıntısı alınamadı.";
  return new ClientError(
    {
      kind: "network",
      title: "API’ye erişilemiyor.",
      detail: "Sunucuya ulaşılamadı. Bağlantınızı ve API durumunu kontrol edin.",
      status: null,
      type: null,
      instance: null,
      correlationId: null,
      fieldErrors: {},
      originalFieldErrors: {},
      originalTitle: null,
      originalDetail: detail,
    },
    { cause: error },
  );
}

export function contractError(error: unknown): ClientError {
  return new ClientError(
    {
      kind: "contract",
      title: "API yanıtı beklenen sözleşmeyle uyuşmuyor.",
      detail: "Sunucudan gelen veri güvenli biçimde çözümlenemedi.",
      status: null,
      type: null,
      instance: null,
      correlationId: null,
      fieldErrors: {},
      originalFieldErrors: {},
      originalTitle: null,
      originalDetail: error instanceof Error ? error.message : String(error),
    },
    { cause: error },
  );
}

export function sessionUnavailableError(status: "anonymous" | "expired"): ClientError {
  const expired = status === "expired";
  return new ClientError({
    kind: "browser",
    title: expired ? "Oturumunuzun süresi doldu." : "Oturum açmanız gerekiyor.",
    detail: expired
      ? "İşlem gönderilmedi. Yeniden giriş yaptıktan sonra aynı işlemi tekrar deneyin."
      : "Bu işlem yalnızca oturum açmış kullanıcılar tarafından yapılabilir.",
    status: null,
    type: expired ? "client/session-expired" : "client/authentication-required",
    instance: null,
    correlationId: null,
    fieldErrors: {},
    originalFieldErrors: {},
    originalTitle: null,
    originalDetail: null,
  });
}

export function normalizeError(error: unknown): ClientError {
  if (error instanceof ClientError) {
    return error;
  }
  if (error instanceof TypeError) {
    return networkError(error);
  }
  const detail = error instanceof Error ? error.message : String(error);
  return new ClientError(
    {
      kind: "unknown",
      title: "Beklenmeyen bir istemci hatası oluştu.",
      detail: "İşlem tamamlanamadı.",
      status: null,
      type: null,
      instance: null,
      correlationId: null,
      fieldErrors: {},
      originalFieldErrors: {},
      originalTitle: null,
      originalDetail: detail,
    },
    { cause: error },
  );
}
