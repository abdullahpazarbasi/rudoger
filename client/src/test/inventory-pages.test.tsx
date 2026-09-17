import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ReactNode } from "react";
import { MemoryRouter, Route, Routes, useLocation } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  createStockItem,
  createStockMovement,
  getProduct,
  getStockItem,
  listProducts,
  listStockItems,
  listStockMovements,
} from "../api/client/rudoger-api";
import type { Product, StockItem, StockMovement } from "../api/contracts";
import { authStore } from "../features/authn/auth-store";
import { InventoryPage } from "../features/inventory/inventory-page";
import { StockDetailPage } from "../features/inventory/stock-detail-page";

vi.mock("../api/client/rudoger-api", () => ({
  createStockItem: vi.fn(),
  createStockMovement: vi.fn(),
  getProduct: vi.fn(),
  getStockItem: vi.fn(),
  listProducts: vi.fn(),
  listStockItems: vi.fn(),
  listStockMovements: vi.fn(),
}));
const product: Product = {
  id: "01990101-0000-7000-8000-000000000001",
  sku: "SKU",
  name: "Ürün",
  baseUomCode: "EA",
  basePriceAmount: 10,
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
      id: "01990101-0000-7000-8000-000000000007",
      level: 1,
      uomCode: "CASE",
      conversionFactor: 10,
      barcode: null,
      weightInKg: null,
      lengthInMm: null,
      widthInMm: null,
      heightInMm: null,
    },
  ],
};
const stock: StockItem = {
  id: "01990101-0000-7000-8000-000000000003",
  productId: product.id,
  baseUomCode: "EA",
  onHandQuantity: 10,
  reservedQuantity: 2,
  availableQuantity: 8,
};
const movement: StockMovement = {
  id: "01990101-0000-7000-8000-000000000004",
  stockItemId: stock.id,
  type: "Reserved",
  uomCode: "EA",
  quantity: 2,
  onHandQuantityDelta: 0,
  reservedQuantityDelta: 2,
  referenceType: "ORDER",
  referenceId: "01990101-0000-7000-8000-000000000005",
  idempotencyKey: "key",
  correlationId: "correlation",
  occurredAtUtc: "2026-01-01T00:00:00Z",
};
function authenticate() {
  authStore.setSession({
    accessToken: `h.${btoa(JSON.stringify({ exp: Math.floor(Date.now() / 1000) + 60 }))}.s`,
    tokenType: "Bearer",
    expiresIn: 60,
  });
}
function Location() {
  const location = useLocation();
  return <output>{location.pathname}</output>;
}
function renderAt(children: ReactNode, entry: string) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={[entry]}>
        <Routes>
          <Route path="/stok" element={children} />
          <Route path="/stok/:stockItemId" element={<StockDetailPage />} />
          <Route path="*" element={<Location />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("inventory use cases", () => {
  beforeEach(() => {
    authStore.clear();
    authenticate();
    vi.clearAllMocks();
    vi.mocked(listProducts).mockResolvedValue({
      items: [product],
      pageNumber: 1,
      pageSize: 100,
      totalCount: 1,
    });
    vi.mocked(listStockItems).mockResolvedValue({
      items: [stock],
      pageNumber: 1,
      pageSize: 20,
      totalCount: 21,
    });
    vi.mocked(createStockItem).mockResolvedValue(stock);
    vi.mocked(getProduct).mockResolvedValue(product);
    vi.mocked(getStockItem).mockResolvedValue(stock);
    vi.mocked(listStockMovements).mockResolvedValue({
      items: [movement],
      pageNumber: 1,
      pageSize: 20,
      totalCount: 21,
    });
    vi.mocked(createStockMovement).mockResolvedValue({ ...movement, type: "Adjustment" });
  });

  it("filters, pages and opens a stock record with stable intent data", async () => {
    const user = userEvent.setup();
    renderAt(<InventoryPage />, "/stok");
    expect(await screen.findByText(stock.productId)).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText("Ürün ID’si"), { target: { value: product.id } });
    fireEvent.click(screen.getByRole("button", { name: "Filtrele" }));
    await waitFor(() =>
      expect(vi.mocked(listStockItems)).toHaveBeenCalledWith(
        expect.objectContaining({ productId: product.id }),
        expect.any(AbortSignal),
      ),
    );
    fireEvent.click(screen.getByRole("button", { name: "Temizle" }));
    fireEvent.click(screen.getByRole("button", { name: "Sonraki" }));
    fireEvent.click(screen.getByRole("button", { name: "Stok kaydı aç" }));
    const dialog = screen.getByRole("dialog", { name: "Stok kaydı aç" });
    await waitFor(() => expect(within(dialog).getByLabelText("Ürün")).toHaveTextContent("SKU"));
    await user.selectOptions(within(dialog).getByLabelText("Ürün"), product.id);
    await user.selectOptions(within(dialog).getByLabelText("UoM"), "CASE");
    await user.clear(within(dialog).getByLabelText("Açılış miktarı"));
    await user.type(within(dialog).getByLabelText("Açılış miktarı"), "5");
    await user.click(within(dialog).getByRole("button", { name: "Stok Kaydını Aç" }));
    await waitFor(() =>
      expect(vi.mocked(createStockItem)).toHaveBeenCalledWith(
        product.id,
        "CASE",
        5,
        expect.stringContaining("stock-open-"),
      ),
    );
    expect(await screen.findByRole("heading", { name: "Stok kaydı" })).toBeInTheDocument();
    expect(vi.mocked(getStockItem)).toHaveBeenCalledWith(stock.id, expect.any(AbortSignal));
  });

  it("validates manual movement rules and shows the read-only ledger", async () => {
    const user = userEvent.setup();
    renderAt(<InventoryPage />, `/stok/${stock.id}`);
    expect(await screen.findByRole("heading", { name: "Bakiyeler" })).toBeInTheDocument();
    expect(screen.getByText("Rezerve edildi")).toBeInTheDocument();
    await waitFor(() => expect(screen.getByLabelText("UoM")).toHaveValue("EA"));
    await user.selectOptions(screen.getByLabelText("Hareket türü"), "Adjustment");
    await user.selectOptions(screen.getByLabelText("UoM"), "CASE");
    await user.type(screen.getByLabelText("Miktar"), "0");
    await user.click(screen.getByRole("button", { name: "Hareketi İşle" }));
    expect(await screen.findByText("Düzeltme miktarı sıfır olamaz.")).toBeInTheDocument();
    await user.clear(screen.getByLabelText("Miktar"));
    await user.type(screen.getByLabelText("Miktar"), "-2");
    await user.click(screen.getByRole("button", { name: "Hareketi İşle" }));
    await waitFor(() =>
      expect(vi.mocked(createStockMovement)).toHaveBeenCalledWith(
        stock.id,
        "Adjustment",
        "CASE",
        -2,
        expect.stringContaining("stock-movement-"),
      ),
    );
    await user.selectOptions(screen.getByLabelText("Hareket türü"), "Receipt");
    await user.type(screen.getByLabelText("Miktar"), "0");
    await user.click(screen.getByRole("button", { name: "Hareketi İşle" }));
    expect(await screen.findByText("Miktar sıfırdan büyük olmalıdır.")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Sonraki" }));
    await waitFor(() =>
      expect(vi.mocked(listStockMovements)).toHaveBeenCalledWith(
        stock.id,
        2,
        expect.any(AbortSignal),
      ),
    );
  });

  it("shows empty inventory and validates and reports stock opening failures", async () => {
    const user = userEvent.setup();
    vi.mocked(listStockItems).mockResolvedValueOnce({
      items: [],
      pageNumber: 1,
      pageSize: 20,
      totalCount: 0,
    });
    vi.mocked(createStockItem).mockRejectedValue(new Error("stock create failed"));
    renderAt(<InventoryPage />, "/stok");
    expect(await screen.findByText("Stok kaydı bulunamadı.")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Stok kaydı aç" }));
    const dialog = screen.getByRole("dialog", { name: "Stok kaydı aç" });
    await waitFor(() => expect(within(dialog).getByLabelText("Ürün")).toHaveTextContent("SKU"));
    await user.clear(within(dialog).getByLabelText("Açılış miktarı"));
    await user.type(within(dialog).getByLabelText("Açılış miktarı"), "-1");
    await user.click(within(dialog).getByRole("button", { name: "Stok Kaydını Aç" }));
    expect(within(dialog).getByText("Ürün seçin.")).toBeInTheDocument();
    expect(
      within(dialog).getByText("Açılış miktarı sıfır veya pozitif olmalıdır."),
    ).toBeInTheDocument();
    await user.selectOptions(within(dialog).getByLabelText("Ürün"), product.id);
    await user.clear(within(dialog).getByLabelText("Açılış miktarı"));
    await user.type(within(dialog).getByLabelText("Açılış miktarı"), "5");
    await user.click(within(dialog).getByRole("button", { name: "Stok Kaydını Aç" }));
    expect(
      await within(dialog).findByText("Beklenmeyen bir istemci hatası oluştu."),
    ).toBeInTheDocument();
  });

  it("renders ledger empty/error states and protects details after expiry", async () => {
    vi.mocked(listStockMovements).mockResolvedValueOnce({
      items: [],
      pageNumber: 1,
      pageSize: 20,
      totalCount: 0,
    });
    const empty = renderAt(<InventoryPage />, `/stok/${stock.id}`);
    expect(await screen.findByText("Hareket bulunamadı.")).toBeInTheDocument();
    empty.unmount();

    vi.mocked(listStockMovements).mockRejectedValueOnce(new Error("ledger failed"));
    const failed = renderAt(<InventoryPage />, `/stok/${stock.id}`);
    expect(await screen.findByText("Beklenmeyen bir istemci hatası oluştu.")).toBeInTheDocument();
    failed.unmount();

    authStore.clear();
    authStore.setSession({ accessToken: "h.eyJleHAiOjF9.s", tokenType: "Bearer", expiresIn: 0 });
    renderAt(<InventoryPage />, `/stok/${stock.id}`);
    expect(
      screen.getByText("Oturum yenilenene kadar stok verisi gösterilemiyor."),
    ).toBeInTheDocument();
  });
});
