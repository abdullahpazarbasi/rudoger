import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { apiClient } from "../api/client/api-client";
import { queryClient } from "../app/providers/query-client";
import { authStore } from "../features/authn/auth-store";

function token(exp: number) {
  return `h.${btoa(JSON.stringify({ exp }))}.s`;
}

describe("shared OpenAPI client middleware", () => {
  beforeEach(() => {
    authStore.clear();
    vi.restoreAllMocks();
  });
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("adds a new correlation id and bearer token to requests", async () => {
    authStore.setSession({
      accessToken: token(Math.floor(Date.now() / 1000) + 60),
      tokenType: "Bearer",
      expiresIn: 60,
    });
    let captured: Request | undefined;
    vi.stubGlobal(
      "fetch",
      vi.fn((request: Request) => {
        captured = request;
        return Promise.resolve(
          new Response(JSON.stringify({ items: [], pageNumber: 1, pageSize: 20, totalCount: 0 }), {
            status: 200,
            headers: { "Content-Type": "application/json" },
          }),
        );
      }),
    );
    await apiClient.GET("/api/v1/product/products", { params: { query: { pageNumber: 1 } } });
    expect(captured?.headers.get("Authorization")).toContain("Bearer");
    expect(captured?.headers.get("X-Correlation-Id")).toMatch(/[0-9a-f-]{36}/);
  });

  it("marks protected 401 responses expired but leaves login failures anonymous", async () => {
    authStore.setSession({
      accessToken: token(Math.floor(Date.now() / 1000) + 60),
      tokenType: "Bearer",
      expiresIn: 60,
    });
    vi.stubGlobal(
      "fetch",
      vi.fn().mockImplementation(() =>
        Promise.resolve(
          new Response("{}", {
            status: 401,
            headers: { "Content-Type": "application/json" },
          }),
        ),
      ),
    );
    await apiClient.GET("/api/v1/product/products", { params: { query: {} } });
    expect(authStore.getSnapshot().status).toBe("expired");
    authStore.clear();
    await apiClient.POST("/api/v1/authn/tokens", { body: { username: "u", password: "p" } });
    expect(authStore.getSnapshot().status).toBe("anonymous");
  });

  it("invalidates health when the network layer fails", async () => {
    const invalidate = vi.spyOn(queryClient, "invalidateQueries").mockResolvedValue();
    vi.stubGlobal("fetch", vi.fn().mockRejectedValue(new TypeError("offline")));
    await expect(
      apiClient.GET("/api/v1/product/products", { params: { query: {} } }),
    ).rejects.toThrow("offline");
    expect(invalidate).toHaveBeenCalledWith({ queryKey: ["health"] });
  });

  it("wraps non-Error network failures", async () => {
    vi.stubGlobal("fetch", vi.fn().mockRejectedValue({ reason: "offline" }));
    await expect(
      apiClient.GET("/api/v1/product/products", { params: { query: {} } }),
    ).rejects.toThrow("The API request failed.");
  });
});
