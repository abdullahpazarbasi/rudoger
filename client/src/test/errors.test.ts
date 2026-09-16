import { describe, expect, it, vi } from "vitest";
import {
  ClientError,
  contractError,
  networkError,
  normalizeError,
  problemError,
  sessionUnavailableError,
} from "../api/errors/client-error";
import {
  problemCode,
  translateFieldName,
  translateFailure,
  translateKnownDetail,
  translateProblemTitle,
  translateValidationMessage,
} from "../api/errors/problem-translations";
import { applyServerFieldErrors, serverFieldPath } from "../shared/forms/server-field-errors";

describe("Turkish error boundary", () => {
  it("preserves every RFC 9457 detail while translating known problems", () => {
    const response = new Response(null, {
      status: 409,
      headers: { "X-Correlation-Id": "header-correlation" },
    });
    const error = problemError(
      {
        type: "https://rudoger.dev/problems/insufficient-stock",
        title: "Conflict",
        detail: "The deduction exceeds available stock.",
        status: 409,
        instance: "/stock",
        correlationId: "body-correlation",
        errors: { quantity: ["Too large", 4] },
      },
      response,
    );
    expect(error.details).toMatchObject({
      title: "İşlem için yeterli kullanılabilir stok yok.",
      status: 409,
      instance: "/stock",
      correlationId: "body-correlation",
      originalTitle: "Conflict",
      originalDetail: "The deduction exceeds available stock.",
      fieldErrors: { quantity: ["Miktar için sunucu doğrulaması başarısız oldu."] },
      originalFieldErrors: { quantity: ["Too large"] },
    });
    expect(error.details.detail).toBe("İşlem için yeterli kullanılabilir stok yok.");
  });

  it("uses response and title fallbacks for unknown problem shapes", () => {
    const response = new Response(null, {
      status: 500,
      headers: { "X-Correlation-Id": "from-header" },
    });
    expect(problemError({ title: "Custom failure" }, response).details).toMatchObject({
      title: "İstek tamamlanamadı.",
      detail: "İstek tamamlanamadı.",
      originalTitle: "Custom failure",
      status: 500,
      correlationId: "from-header",
    });
    expect(problemError(null, response).details.fieldErrors).toEqual({});
    expect(
      problemError(
        {
          type: "https://rudoger.dev/problems/invalid-request",
          title: "İstek geçerli değil.",
          errors: { ignored: "not-an-array" },
        },
        response,
      ).details,
    ).toMatchObject({ originalTitle: null, fieldErrors: { ignored: [] } });
  });

  it("normalizes network, contract, browser and unknown errors", () => {
    expect(networkError(new Error("ECONNREFUSED")).details.originalDetail).toBe("ECONNREFUSED");
    expect(networkError("offline").details.originalDetail).toContain("alınamadı");
    expect(contractError(new Error("schema mismatch")).details.kind).toBe("contract");
    expect(contractError("bad").details.originalDetail).toBe("bad");
    expect(sessionUnavailableError("expired").details.type).toBe("client/session-expired");
    expect(sessionUnavailableError("anonymous").details.type).toBe(
      "client/authentication-required",
    );
    const existing = new ClientError(sessionUnavailableError("expired").details);
    expect(normalizeError(existing)).toBe(existing);
    expect(normalizeError(new TypeError("fetch failed")).details.kind).toBe("network");
    expect(normalizeError(new Error("boom")).details.kind).toBe("unknown");
    expect(normalizeError(42).details.originalDetail).toBe("42");
  });

  it("translates codes and exact details without inventing unknown translations", () => {
    expect(problemCode("https://x/product-in-use")).toBe("product-in-use");
    expect(problemCode(null)).toBeNull();
    expect(problemCode("https://x/")).toBeNull();
    expect(translateProblemTitle("https://x/product-in-use", "fallback")).toContain("etkin");
    expect(translateProblemTitle("https://x/unknown", "fallback")).toBe("fallback");
    expect(translateFailure("insufficient-stock", "original")).toContain("yeterli");
    expect(translateFailure("unknown", "original")).toBe("original");
    expect(translateFailure("unknown", null)).toContain("tamamlanamadı");
    expect(translateFailure(null, "raw")).toBe("raw");
    expect(translateKnownDetail("The username or password is invalid.")).toContain("parola");
    expect(translateKnownDetail("untouched")).toBeNull();
    expect(translateKnownDetail(null)).toBeNull();
    expect(translateFieldName("$.Lines[0].ProductId")).toBe("1. satır · Ürün");
    expect(translateValidationMessage("Password", "The Password field is required.")).toBe(
      "Parola zorunludur.",
    );
    expect(translateFieldName("CustomField")).toBe("CustomField");
    expect(translateFieldName("")).toBe("");
    expect(translateValidationMessage("Sku", "Sku cannot exceed 64 characters.")).toBe(
      "SKU en fazla 64 karakter olabilir.",
    );
  });

  it("maps translated API validation errors back to matching form fields", () => {
    const error = problemError(
      {
        type: "https://rudoger.dev/problems/invalid-request",
        errors: { "$.Lines[0].Quantity": ["The JSON value could not be converted."] },
      },
      new Response(null, { status: 400 }),
    );
    const setError = vi.fn();
    applyServerFieldErrors(error, setError);
    expect(serverFieldPath("$.Lines[0].Quantity")).toBe("lines.0.quantity");
    expect(setError).toHaveBeenCalledWith("lines.0.quantity", {
      type: "server",
      message: "1. satır · Miktar beklenen veri türü veya biçimiyle uyuşmuyor.",
    });
    setError.mockClear();
    const emptyError = problemError(
      { type: "https://rudoger.dev/problems/invalid-request", errors: { "": [], Ignored: [] } },
      new Response(null, { status: 400 }),
    );
    applyServerFieldErrors(emptyError, setError);
    expect(setError).not.toHaveBeenCalled();
  });
});
