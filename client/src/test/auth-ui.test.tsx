import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { useLocation, MemoryRouter, Route, Routes } from "react-router";
import { Toaster } from "sonner";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { exchangeToken, listProducts } from "../api/client/rudoger-api";
import { ProtectedRoute } from "../app/router/protected-route";
import { authStore } from "../features/authn/auth-store";
import { LoginPage } from "../features/authn/login-page";
import { SessionExpiryNotice } from "../features/authn/session-expiry-notice";
import { ProductsPage } from "../features/products/products-page";

vi.mock("../api/client/rudoger-api", () => ({
  createProduct: vi.fn(),
  exchangeToken: vi.fn(),
  listProducts: vi.fn(),
}));
const exchangeTokenMock = vi.mocked(exchangeToken);
const listProductsMock = vi.mocked(listProducts);

function token(exp: number): string {
  return `header.${btoa(JSON.stringify({ exp, preferred_username: "abdullah", sub: "01990101-0000-7000-8000-000000000001" }))}.signature`;
}

function LocationView() {
  const location = useLocation();
  return (
    <output>
      {location.pathname}
      {location.search}
    </output>
  );
}
function client() {
  return new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
}

describe("authentication UI", () => {
  beforeEach(() => {
    authStore.clear();
    exchangeTokenMock.mockReset();
    listProductsMock.mockReset();
  });

  it("redirects only anonymous users from protected routes", () => {
    render(
      <MemoryRouter initialEntries={["/urunler"]}>
        <Routes>
          <Route path="/giris" element={<p>Giriş</p>} />
          <Route
            path="/urunler"
            element={
              <ProtectedRoute>
                <p>Korunan</p>
              </ProtectedRoute>
            }
          />
        </Routes>
      </MemoryRouter>,
    );
    expect(screen.getByText("Giriş")).toBeInTheDocument();
  });

  it("keeps an expired user on the current route and disables protected submit entry points", () => {
    authStore.setSession({ accessToken: token(1), tokenType: "Bearer", expiresIn: 0 });
    render(
      <QueryClientProvider client={client()}>
        <MemoryRouter initialEntries={["/urunler"]}>
          <ProtectedRoute>
            <ProductsPage />
          </ProtectedRoute>
        </MemoryRouter>
      </QueryClientProvider>,
    );
    expect(screen.getByRole("heading", { name: "Ürünler" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Yeni ürün" })).toBeDisabled();
    expect(screen.getByText(/Oturum yenilenene kadar/)).toBeInTheDocument();
    expect(listProductsMock).not.toHaveBeenCalled();
  });

  it("navigates to login with returnTo only after the snackbar action", async () => {
    authStore.setSession({ accessToken: token(1), tokenType: "Bearer", expiresIn: 0 });
    render(
      <MemoryRouter initialEntries={["/stok?urun=abc"]}>
        <SessionExpiryNotice />
        <Toaster />
        <LocationView />
      </MemoryRouter>,
    );
    expect(screen.getByText("/stok?urun=abc")).toBeInTheDocument();
    expect(await screen.findByText("Oturumunuzun süresi doldu.")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Yeniden Giriş Yap" }));
    await waitFor(() => expect(screen.getByText(/\/giris\?returnTo=/)).toBeInTheDocument());
    expect(authStore.getSnapshot().status).toBe("anonymous");
  });

  it("validates login fields, reports API errors and returns to the intended route", async () => {
    exchangeTokenMock.mockRejectedValueOnce(new Error("bad credentials")).mockResolvedValueOnce({
      accessToken: token(Math.floor(Date.now() / 1000) + 60),
      tokenType: "Bearer",
      expiresIn: 60,
    });
    render(
      <QueryClientProvider client={client()}>
        <MemoryRouter initialEntries={["/giris?returnTo=%2Fstok"]}>
          <Routes>
            <Route path="/giris" element={<LoginPage />} />
            <Route
              path="/stok"
              element={
                <>
                  <p>Stok ekranı</p>
                  <LocationView />
                </>
              }
            />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>,
    );
    fireEvent.click(screen.getByRole("button", { name: "Giriş Yap" }));
    expect(await screen.findByText("Kullanıcı adı zorunludur.")).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText("Kullanıcı adı"), { target: { value: "abdullah" } });
    fireEvent.change(screen.getByLabelText("Parola"), { target: { value: "wrong" } });
    fireEvent.click(screen.getByRole("button", { name: "Giriş Yap" }));
    expect(await screen.findByText("Beklenmeyen bir istemci hatası oluştu.")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Giriş Yap" }));
    expect(await screen.findByText("Stok ekranı")).toBeInTheDocument();
  });

  it("redirects an authenticated user only to a safe local return path", () => {
    authStore.setSession({
      accessToken: token(Math.floor(Date.now() / 1000) + 60),
      tokenType: "Bearer",
      expiresIn: 60,
    });
    render(
      <QueryClientProvider client={client()}>
        <MemoryRouter initialEntries={["/giris?returnTo=//evil.example"]}>
          <Routes>
            <Route path="/giris" element={<LoginPage />} />
            <Route path="/urunler" element={<p>Güvenli hedef</p>} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>,
    );
    expect(screen.getByText("Güvenli hedef")).toBeInTheDocument();
  });

  it("disables the login action while authentication is pending", async () => {
    exchangeTokenMock.mockReturnValue(new Promise(() => undefined));
    render(
      <QueryClientProvider client={client()}>
        <MemoryRouter initialEntries={["/giris"]}>
          <LoginPage />
        </MemoryRouter>
      </QueryClientProvider>,
    );
    fireEvent.change(screen.getByLabelText("Kullanıcı adı"), { target: { value: "abdullah" } });
    fireEvent.change(screen.getByLabelText("Parola"), { target: { value: "12345678" } });
    fireEvent.click(screen.getByRole("button", { name: "Giriş Yap" }));
    expect(await screen.findByRole("button", { name: "Giriş yapılıyor…" })).toBeDisabled();
  });
});
