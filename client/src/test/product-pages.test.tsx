import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  addPackaging,
  createProduct,
  deletePackaging,
  deleteProduct,
  getProduct,
  listProducts,
  patchPackaging,
  patchProduct,
} from "../api/client/rudoger-api";
import type { Product } from "../api/contracts";
import { authStore } from "../features/authn/auth-store";
import { ProductDetailPage } from "../features/products/product-detail-page";
import { ProductsPage } from "../features/products/products-page";

vi.mock("../api/client/rudoger-api", () => ({
  addPackaging: vi.fn(),
  createProduct: vi.fn(),
  deletePackaging: vi.fn(),
  deleteProduct: vi.fn(),
  getProduct: vi.fn(),
  listProducts: vi.fn(),
  patchPackaging: vi.fn(),
  patchProduct: vi.fn(),
}));

const product: Product = {
  id: "01990101-0000-7000-8000-000000000001",
  sku: "SKU-1",
  name: "Ürün Bir",
  baseUomCode: "EA",
  basePriceAmount: 12,
  basePriceCurrencyCode: "TRY",
  packagings: [
    {
      id: "01990101-0000-7000-8000-000000000002",
      level: 0,
      uomCode: "EA",
      conversionFactor: 1,
      barcode: null,
      weightInKg: null,
      lengthInMm: null,
      widthInMm: null,
      heightInMm: null,
    },
    {
      id: "01990101-0000-7000-8000-000000000003",
      level: 1,
      uomCode: "BOX",
      conversionFactor: 10,
      barcode: "BOX-1",
      weightInKg: 2,
      lengthInMm: 10,
      widthInMm: 20,
      heightInMm: 30,
    },
  ],
};

function authenticate(): void {
  authStore.setSession({
    accessToken: `h.${btoa(JSON.stringify({ exp: Math.floor(Date.now() / 1000) + 60 }))}.s`,
    tokenType: "Bearer",
    expiresIn: 60,
  });
}
function wrapper(children: React.ReactNode, entries = ["/urunler"]) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={entries}>
        <Routes>
          <Route path="/urunler" element={children} />
          <Route path="/urunler/:productId" element={<ProductDetailPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("product use cases", () => {
  beforeEach(() => {
    authStore.clear();
    authenticate();
    vi.clearAllMocks();
    vi.mocked(listProducts).mockResolvedValue({
      items: [product],
      pageNumber: 1,
      pageSize: 20,
      totalCount: 21,
    });
    vi.mocked(getProduct).mockResolvedValue(product);
    vi.mocked(createProduct).mockResolvedValue(product);
    vi.mocked(patchProduct).mockResolvedValue({ ...product, name: "Güncel Ürün" });
    vi.mocked(addPackaging).mockResolvedValue(product.packagings[1]!);
    vi.mocked(patchPackaging).mockResolvedValue({ ...product.packagings[1]!, barcode: "NEW" });
    vi.mocked(deletePackaging).mockResolvedValue();
    vi.mocked(deleteProduct).mockResolvedValue();
  });

  it("filters, pages and creates products with a locked level-zero packaging", async () => {
    wrapper(<ProductsPage />);
    expect(await screen.findByText("Ürün Bir")).toBeInTheDocument();
    expect(screen.getByText(/21 kayıt/)).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText("Ürün ID’leri"), { target: { value: "invalid" } });
    fireEvent.click(screen.getByRole("button", { name: "Filtrele" }));
    expect(screen.getByText(/geçerli bir ürün ID’si değil/)).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Temizle" }));
    fireEvent.click(screen.getByRole("button", { name: "Sonraki" }));
    await waitFor(() =>
      expect(vi.mocked(listProducts)).toHaveBeenCalledWith(
        expect.objectContaining({ pageNumber: 2 }),
        expect.any(AbortSignal),
      ),
    );

    fireEvent.click(screen.getByRole("button", { name: "Yeni ürün" }));
    const dialog = screen.getByRole("dialog", { name: "Yeni ürün" });
    fireEvent.click(within(dialog).getByRole("button", { name: "Ürünü Oluştur" }));
    expect(await within(dialog).findByText("SKU zorunludur.")).toBeInTheDocument();
    expect(within(dialog).getByText("Ürün adı zorunludur.")).toBeInTheDocument();
    fireEvent.change(within(dialog).getByLabelText("SKU"), { target: { value: "new" } });
    fireEvent.change(within(dialog).getByLabelText("Ürün adı"), { target: { value: "Yeni ürün" } });
    fireEvent.change(within(dialog).getByLabelText("Temel UoM"), { target: { value: "ea" } });
    fireEvent.change(within(dialog).getByLabelText("Temel fiyat"), { target: { value: "5" } });
    expect(within(dialog).getByLabelText("Level")).toBeDisabled();
    fireEvent.click(within(dialog).getByRole("button", { name: "Packaging ekle" }));
    const uoms = within(dialog).getAllByLabelText("UoM kodu");
    const factors = within(dialog).getAllByLabelText("Dönüşüm katsayısı");
    fireEvent.change(uoms[1]!, { target: { value: "box" } });
    fireEvent.change(factors[1]!, { target: { value: "10" } });
    fireEvent.click(within(dialog).getByRole("button", { name: "Ürünü Oluştur" }));
    await waitFor(() =>
      expect(vi.mocked(createProduct)).toHaveBeenCalledWith(
        expect.objectContaining({
          sku: "NEW",
          baseUomCode: "EA",
          packagings: expect.arrayContaining([
            expect.objectContaining({ level: 0, conversionFactor: 1 }),
            expect.objectContaining({ uomCode: "BOX" }),
          ]),
        }),
      ),
    );
  });

  it("patches and deletes products and manages packaging", async () => {
    wrapper(<ProductsPage />, [`/urunler/${product.id}`]);
    expect(await screen.findByRole("heading", { name: "Ürün Bir" })).toBeInTheDocument();
    let rows = screen.getAllByRole("row");
    fireEvent.click(within(rows[1]!).getByRole("button", { name: "Düzenle" }));
    let dialog = screen.getByRole("dialog", { name: "Packaging düzenle" });
    expect(within(dialog).getByLabelText("Level")).toBeDisabled();
    expect(within(dialog).getByLabelText("UoM kodu")).toBeDisabled();
    fireEvent.click(within(dialog).getByRole("button", { name: "Pencereyi kapat" }));

    fireEvent.change(screen.getByLabelText("Ürün adı"), { target: { value: "Güncel Ürün" } });
    fireEvent.click(screen.getByRole("button", { name: "Değişiklikleri Kaydet" }));
    await waitFor(() =>
      expect(vi.mocked(patchProduct)).toHaveBeenCalledWith(product.id, [
        { op: "replace", path: "/name", value: "Güncel Ürün" },
      ]),
    );

    rows = screen.getAllByRole("row");
    fireEvent.click(within(rows[2]!).getByRole("button", { name: "Düzenle" }));
    dialog = screen.getByRole("dialog", { name: "Packaging düzenle" });
    fireEvent.change(within(dialog).getByLabelText("Barkod"), { target: { value: "NEW" } });
    fireEvent.click(within(dialog).getByRole("button", { name: "Kaydet" }));
    await waitFor(() => expect(vi.mocked(patchPackaging)).toHaveBeenCalled());

    fireEvent.click(screen.getByRole("button", { name: "Packaging ekle" }));
    dialog = screen.getByRole("dialog", { name: "Packaging ekle" });
    fireEvent.change(within(dialog).getByLabelText("UoM kodu"), { target: { value: "PAL" } });
    fireEvent.change(within(dialog).getByLabelText("Dönüşüm katsayısı"), {
      target: { value: "100" },
    });
    fireEvent.click(within(dialog).getByRole("button", { name: "Kaydet" }));
    await waitFor(() => expect(vi.mocked(addPackaging)).toHaveBeenCalled());

    fireEvent.click(within(rows[2]!).getByRole("button", { name: "Sil" }));
    dialog = screen.getByRole("dialog", { name: "Packaging silinsin mi?" });
    fireEvent.click(within(dialog).getByRole("button", { name: "Sil" }));
    await waitFor(() =>
      expect(vi.mocked(deletePackaging)).toHaveBeenCalledWith(
        product.id,
        product.packagings[1]!.id,
      ),
    );

    fireEvent.click(screen.getByRole("button", { name: "Ürünü sil" }));
    dialog = screen.getByRole("dialog", { name: "Ürün silinsin mi?" });
    fireEvent.click(within(dialog).getByRole("button", { name: "Sil" }));
    await waitFor(() => expect(vi.mocked(deleteProduct)).toHaveBeenCalledWith(product.id));
  });

  it("shows empty/error list states and creation failures", async () => {
    vi.mocked(listProducts).mockResolvedValueOnce({
      items: [],
      pageNumber: 1,
      pageSize: 20,
      totalCount: 0,
    });
    const empty = wrapper(<ProductsPage />);
    expect(await screen.findByText("Bu ölçütlerde ürün bulunamadı.")).toBeInTheDocument();
    empty.unmount();

    vi.mocked(listProducts).mockRejectedValueOnce(new Error("list failed"));
    const failed = wrapper(<ProductsPage />);
    expect(await screen.findByText("Beklenmeyen bir istemci hatası oluştu.")).toBeInTheDocument();
    failed.unmount();

    vi.mocked(createProduct).mockRejectedValueOnce(new Error("create failed"));
    wrapper(<ProductsPage />);
    await screen.findByText("Ürün Bir");
    fireEvent.click(screen.getByRole("button", { name: "Yeni ürün" }));
    const dialog = screen.getByRole("dialog", { name: "Yeni ürün" });
    fireEvent.change(within(dialog).getByLabelText("SKU"), { target: { value: "new" } });
    fireEvent.change(within(dialog).getByLabelText("Ürün adı"), { target: { value: "Yeni ürün" } });
    fireEvent.change(within(dialog).getByLabelText("Temel UoM"), { target: { value: "ea" } });
    fireEvent.change(within(dialog).getByLabelText("Temel fiyat"), { target: { value: "5" } });
    fireEvent.click(within(dialog).getByRole("button", { name: "Ürünü Oluştur" }));
    expect(
      await within(dialog).findByText("Beklenmeyen bir istemci hatası oluştu."),
    ).toBeInTheDocument();
  });

  it("renders detail request failures and the expired-session fallback", async () => {
    vi.mocked(getProduct).mockRejectedValueOnce(new Error("detail failed"));
    const failed = wrapper(<ProductsPage />, [`/urunler/${product.id}`]);
    expect(await screen.findByText("Beklenmeyen bir istemci hatası oluştu.")).toBeInTheDocument();
    failed.unmount();

    authStore.clear();
    authStore.setSession({ accessToken: "h.eyJleHAiOjF9.s", tokenType: "Bearer", expiresIn: 0 });
    wrapper(<ProductsPage />, [`/urunler/${product.id}`]);
    expect(
      screen.getByText("Oturum yenilenene kadar ürün verisi gösterilemiyor."),
    ).toBeInTheDocument();
  });
});
