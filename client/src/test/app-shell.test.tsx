import { QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import type { ReactNode } from "react";
import { MemoryRouter, Route, Routes } from "react-router";
import { Toaster } from "sonner";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { getHealth } from "../api/client/rudoger-api";
import { ClientError } from "../api/errors/client-error";
import { ErrorBoundary } from "../app/error-boundary";
import { AppLayout } from "../app/layout/app-layout";
import { AppProviders } from "../app/providers/app-providers";
import { queryClient } from "../app/providers/query-client";
import { HomeRoute } from "../app/router/home-route";
import { router } from "../app/router/router";
import { authStore } from "../features/authn/auth-store";
import { NotFoundPage } from "../shared/components/not-found-page";

vi.mock("../api/client/rudoger-api", () => ({ getHealth: vi.fn() }));
function token(exp: number) {
  return `h.${btoa(JSON.stringify({ exp, preferred_username: "abdullah" }))}.s`;
}
function Boom(): ReactNode {
  throw new Error("render failed");
}

describe("application shell", () => {
  beforeEach(() => {
    authStore.clear();
    queryClient.clear();
    vi.mocked(getHealth).mockResolvedValue({ healthy: true, status: "Healthy", checks: {} });
  });

  it("renders complete browser errors in the top-level boundary", () => {
    vi.spyOn(console, "error").mockImplementation(() => undefined);
    render(
      <ErrorBoundary>
        <Boom />
      </ErrorBoundary>,
    );
    expect(screen.getByText("Beklenmeyen bir istemci hatası oluştu.")).toBeInTheDocument();
    fireEvent.click(screen.getByText("Teknik ayrıntılar"));
    expect(screen.getByText("render failed")).toBeInTheDocument();
  });

  it("runs provider lifecycles and admits children after health succeeds", async () => {
    render(
      <AppProviders>
        <p>İstemci içeriği</p>
      </AppProviders>,
    );
    expect(screen.getByText("İstemci içeriği")).toBeInTheDocument();
    await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
    expect(vi.mocked(getHealth)).toHaveBeenCalled();
  });

  it("shows navigation, user state and preserves returnTo on logout", async () => {
    authStore.setSession({
      accessToken: token(Math.floor(Date.now() / 1000) + 60),
      tokenType: "Bearer",
      expiresIn: 60,
    });
    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={["/urunler?x=1"]}>
          <Routes>
            <Route element={<AppLayout />}>
              <Route path="/urunler" element={<p>Ürün içeriği</p>} />
            </Route>
            <Route path="/giris" element={<p>Giriş rotası</p>} />
          </Routes>
          <Toaster />
        </MemoryRouter>
      </QueryClientProvider>,
    );
    expect(screen.getByText("abdullah")).toBeInTheDocument();
    expect(screen.getByText("Oturum açık")).toBeInTheDocument();
    expect(screen.getByText("Ürün içeriği")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Çıkış" }));
    expect(await screen.findByText("Giriş rotası")).toBeInTheDocument();
    expect(authStore.getSnapshot().status).toBe("anonymous");
  });

  it("routes home by auth state and renders the not-found fallback", () => {
    expect(router).toBeDefined();
    const view = render(
      <MemoryRouter initialEntries={["/"]}>
        <Routes>
          <Route path="/" element={<HomeRoute />} />
          <Route path="/giris" element={<p>Anonim</p>} />
          <Route path="/urunler" element={<p>Oturumlu</p>} />
        </Routes>
      </MemoryRouter>,
    );
    expect(screen.getByText("Anonim")).toBeInTheDocument();
    view.unmount();
    authStore.setSession({
      accessToken: token(Math.floor(Date.now() / 1000) + 60),
      tokenType: "Bearer",
      expiresIn: 60,
    });
    render(
      <MemoryRouter initialEntries={["/"]}>
        <Routes>
          <Route path="/" element={<HomeRoute />} />
          <Route path="/urunler" element={<p>Oturumlu</p>} />
        </Routes>
      </MemoryRouter>,
    );
    expect(screen.getByText("Oturumlu")).toBeInTheDocument();
    render(
      <MemoryRouter>
        <NotFoundPage />
      </MemoryRouter>,
    );
    expect(screen.getByRole("heading", { name: "Sayfa bulunamadı" })).toBeInTheDocument();
  });

  it("retries only eligible query failures once", () => {
    const retry = queryClient.getDefaultOptions().queries?.retry;
    expect(typeof retry).toBe("function");
    const retryFunction = retry as (count: number, error: Error) => boolean;
    const clientError = new ClientError({
      kind: "problem",
      title: "x",
      detail: "x",
      status: 400,
      type: null,
      instance: null,
      correlationId: null,
      fieldErrors: {},
      originalFieldErrors: {},
      originalTitle: null,
      originalDetail: null,
    });
    expect(retryFunction(0, clientError)).toBe(false);
    expect(retryFunction(0, new Error("offline"))).toBe(true);
    expect(retryFunction(1, new Error("offline"))).toBe(false);
  });
});
