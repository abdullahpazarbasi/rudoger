import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ReactNode } from "react";
import { MemoryRouter, Route, Routes } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  createOrder,
  createOrderTransition,
  getOrder,
  getOrderPlacement,
  getOrderTransition,
  listOrders,
  listProducts,
} from "../api/client/rudoger-api";
import type { Order, OrderPlacement, OrderTransition, Product } from "../api/contracts";
import { authStore } from "../features/authn/auth-store";
import { OrderDetailPage } from "../features/orders/order-detail-page";
import { OrdersPage } from "../features/orders/orders-page";

vi.mock("../api/client/rudoger-api", () => ({
  createOrder: vi.fn(),
  createOrderTransition: vi.fn(),
  getOrder: vi.fn(),
  getOrderPlacement: vi.fn(),
  getOrderTransition: vi.fn(),
  listOrders: vi.fn(),
  listProducts: vi.fn(),
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
  ],
};
const order: Order = {
  id: "01990101-0000-7000-8000-000000000003",
  orderNumber: "ORD-1",
  status: "Placed",
  userId: "01990101-0000-7000-8000-000000000004",
  lines: [
    {
      id: "01990101-0000-7000-8000-000000000005",
      num: 1,
      productId: product.id,
      uomCode: "EA",
      quantity: 2,
      unitPriceAmount: 10,
      unitPriceCurrencyCode: "TRY",
    },
  ],
  pendingTransitionId: null,
  pendingTransitionTarget: null,
};
const placement: OrderPlacement = {
  id: "01990101-0000-7000-8000-000000000006",
  orderId: order.id,
  userId: order.userId,
  status: "Succeeded",
  lines: [{ num: 1, productId: product.id, uomCode: "EA", quantity: 2 }],
  failureCode: null,
  failureDetail: null,
};
const transition: OrderTransition = {
  id: "01990101-0000-7000-8000-000000000007",
  orderId: order.id,
  target: "Shipped",
  status: "Succeeded",
};
function authenticate() {
  authStore.setSession({
    accessToken: `h.${btoa(JSON.stringify({ exp: Math.floor(Date.now() / 1000) + 60 }))}.s`,
    tokenType: "Bearer",
    expiresIn: 60,
  });
}
function renderAt(children: ReactNode, entry: string) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={[entry]}>
        <Routes>
          <Route path="/siparisler" element={children} />
          <Route path="/siparisler/:orderId" element={<OrderDetailPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("order use cases", () => {
  beforeEach(() => {
    authStore.clear();
    authenticate();
    vi.clearAllMocks();
    vi.mocked(listOrders).mockResolvedValue({
      items: [order],
      pageNumber: 1,
      pageSize: 20,
      totalCount: 21,
    });
    vi.mocked(listProducts).mockResolvedValue({
      items: [product],
      pageNumber: 1,
      pageSize: 100,
      totalCount: 1,
    });
    vi.mocked(createOrder).mockResolvedValue({ ...placement, status: "Pending" });
    vi.mocked(getOrderPlacement).mockResolvedValue(placement);
    vi.mocked(getOrder).mockResolvedValue(order);
    vi.mocked(createOrderTransition).mockResolvedValue({ ...transition, status: "Pending" });
    vi.mocked(getOrderTransition).mockResolvedValue(transition);
  });

  it("lists, pages, validates unique lines and polls a placement to success", async () => {
    const user = userEvent.setup();
    renderAt(<OrdersPage />, "/siparisler");
    expect(await screen.findByText("ORD-1")).toBeInTheDocument();
    expect(screen.getByText("Alındı")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Sonraki" }));
    await waitFor(() =>
      expect(vi.mocked(listOrders)).toHaveBeenCalledWith(2, expect.any(AbortSignal)),
    );
    fireEvent.click(screen.getByRole("button", { name: "Yeni sipariş" }));
    const dialog = screen.getByRole("dialog", { name: "Yeni sipariş" });
    await waitFor(() => expect(within(dialog).getByLabelText("Ürün")).toHaveTextContent("SKU"));
    await user.click(within(dialog).getByRole("button", { name: "Siparişi Oluştur" }));
    expect(within(dialog).getByText("Ürün seçin.")).toBeInTheDocument();
    expect(within(dialog).getByText("UoM seçin.")).toBeInTheDocument();
    expect(within(dialog).getByText("Miktar sıfırdan büyük olmalıdır.")).toBeInTheDocument();
    await user.selectOptions(within(dialog).getByLabelText("Ürün"), product.id);
    await user.type(within(dialog).getByLabelText("Miktar"), "2");
    await user.click(within(dialog).getByRole("button", { name: "Satır ekle" }));
    const products = within(dialog).getAllByLabelText("Ürün");
    const quantities = within(dialog).getAllByLabelText("Miktar");
    await user.selectOptions(products[1]!, product.id);
    await user.type(quantities[1]!, "1");
    await user.click(within(dialog).getByRole("button", { name: "Siparişi Oluştur" }));
    expect(within(dialog).getByText("Ürün/UoM ikilisi benzersiz olmalıdır.")).toBeInTheDocument();
    await user.click(within(dialog).getAllByRole("button", { name: "Kaldır" })[1]!);
    await user.click(within(dialog).getByRole("button", { name: "Siparişi Oluştur" }));
    await waitFor(() =>
      expect(vi.mocked(createOrder)).toHaveBeenCalledWith(
        { lines: [{ productId: product.id, uomCode: "EA", quantity: 2 }] },
        expect.stringContaining("order-place-"),
      ),
    );
    expect(await screen.findByRole("heading", { name: "ORD-1" })).toBeInTheDocument();
    expect(vi.mocked(getOrderPlacement)).toHaveBeenCalled();
  });

  it("confirms and polls a shipped transition", async () => {
    renderAt(<OrdersPage />, `/siparisler/${order.id}`);
    expect(await screen.findByRole("heading", { name: "ORD-1" })).toBeInTheDocument();
    expect(screen.getByText("₺20,00")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Gönderildi Yap" }));
    const dialog = screen.getByRole("dialog", { name: "Sipariş gönderildi yapılsın mı?" });
    fireEvent.click(within(dialog).getByRole("button", { name: "Onayla" }));
    await waitFor(() =>
      expect(vi.mocked(createOrderTransition)).toHaveBeenCalledWith(order.id, "Shipped"),
    );
    await waitFor(() => expect(vi.mocked(getOrderTransition)).toHaveBeenCalled());
  });

  it("shows all order statuses, empty results and cancellation transitions", async () => {
    vi.mocked(listOrders).mockResolvedValueOnce({
      items: [
        {
          ...order,
          id: "01990101-0000-7000-8000-000000000011",
          orderNumber: "ORD-S",
          status: "Shipped",
        },
        {
          ...order,
          id: "01990101-0000-7000-8000-000000000012",
          orderNumber: "ORD-C",
          status: "Cancelled",
        },
      ],
      pageNumber: 1,
      pageSize: 20,
      totalCount: 2,
    });
    const first = renderAt(<OrdersPage />, "/siparisler");
    expect(await screen.findByText("Gönderildi")).toBeInTheDocument();
    expect(screen.getByText("İptal edildi")).toBeInTheDocument();
    first.unmount();

    vi.mocked(listOrders).mockResolvedValueOnce({
      items: [],
      pageNumber: 1,
      pageSize: 20,
      totalCount: 0,
    });
    const second = renderAt(<OrdersPage />, "/siparisler");
    expect(await screen.findByText("Sipariş bulunamadı.")).toBeInTheDocument();
    second.unmount();

    renderAt(<OrdersPage />, `/siparisler/${order.id}`);
    fireEvent.click(await screen.findByRole("button", { name: "İptal Et" }));
    const dialog = screen.getByRole("dialog", { name: "Sipariş iptal edilsin mi?" });
    expect(within(dialog).getByText("Ayrılmış stok serbest bırakılacak.")).toBeInTheDocument();
    fireEvent.click(within(dialog).getByRole("button", { name: "Onayla" }));
    await waitFor(() =>
      expect(vi.mocked(createOrderTransition)).toHaveBeenCalledWith(order.id, "Cancelled"),
    );
  });

  it("renders an already pending cancellation and terminal order without mutation actions", async () => {
    vi.mocked(getOrder).mockResolvedValueOnce({
      ...order,
      pendingTransitionId: transition.id,
      pendingTransitionTarget: "Cancelled",
    });
    vi.mocked(getOrderTransition).mockResolvedValueOnce({
      ...transition,
      target: "Cancelled",
      status: "Pending",
    });
    const pending = renderAt(<OrdersPage />, `/siparisler/${order.id}`);
    expect(await screen.findByText(/İptal geçişi/)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Gönderildi Yap" })).toBeDisabled();
    pending.unmount();

    vi.mocked(getOrder).mockResolvedValueOnce({ ...order, status: "Cancelled" });
    renderAt(<OrdersPage />, `/siparisler/${order.id}`);
    expect(await screen.findByText("İptal edildi")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Gönderildi Yap" })).not.toBeInTheDocument();
  });

  it("rejects mixed currencies before submitting", async () => {
    const user = userEvent.setup();
    const usdProduct: Product = {
      ...product,
      id: "01990101-0000-7000-8000-000000000021",
      sku: "USD",
      basePriceCurrencyCode: "USD",
      packagings: [{ ...product.packagings[0]!, id: "01990101-0000-7000-8000-000000000022" }],
    };
    vi.mocked(listProducts).mockResolvedValue({
      items: [product, usdProduct],
      pageNumber: 1,
      pageSize: 100,
      totalCount: 2,
    });
    renderAt(<OrdersPage />, "/siparisler");
    fireEvent.click(await screen.findByRole("button", { name: "Yeni sipariş" }));
    const dialog = screen.getByRole("dialog", { name: "Yeni sipariş" });
    await waitFor(() => expect(within(dialog).getByLabelText("Ürün")).toHaveTextContent("USD"));
    await user.selectOptions(within(dialog).getByLabelText("Ürün"), product.id);
    await user.type(within(dialog).getByLabelText("Miktar"), "1");
    await user.click(within(dialog).getByRole("button", { name: "Satır ekle" }));
    await user.selectOptions(within(dialog).getAllByLabelText("Ürün")[1]!, usdProduct.id);
    await user.type(within(dialog).getAllByLabelText("Miktar")[1]!, "1");
    await user.click(within(dialog).getByRole("button", { name: "Siparişi Oluştur" }));
    expect(
      within(dialog).getByText("Bir siparişte farklı para birimleri kullanılamaz."),
    ).toBeInTheDocument();
    expect(vi.mocked(createOrder)).not.toHaveBeenCalled();
  });

  it("reports placement command errors without losing the draft", async () => {
    const user = userEvent.setup();
    vi.mocked(createOrder).mockRejectedValueOnce(new Error("placement unavailable"));
    renderAt(<OrdersPage />, "/siparisler");
    fireEvent.click(await screen.findByRole("button", { name: "Yeni sipariş" }));
    const dialog = screen.getByRole("dialog", { name: "Yeni sipariş" });
    await waitFor(() => expect(within(dialog).getByLabelText("Ürün")).toHaveTextContent("SKU"));
    await user.selectOptions(within(dialog).getByLabelText("Ürün"), product.id);
    await user.type(within(dialog).getByLabelText("Miktar"), "1");
    await user.click(within(dialog).getByRole("button", { name: "Siparişi Oluştur" }));
    expect(
      await within(dialog).findByText("Beklenmeyen bir istemci hatası oluştu."),
    ).toBeInTheDocument();
    expect(within(dialog).getByLabelText("Miktar")).toHaveValue(1);
  });

  it("renders a failed placement with translated and original details", async () => {
    vi.mocked(getOrderPlacement).mockResolvedValue({
      ...placement,
      status: "Failed",
      failureCode: "insufficient-stock",
      failureDetail: "The reservation exceeds available stock.",
    });
    renderAt(<OrdersPage />, "/siparisler");
    await screen.findByText("ORD-1");
    fireEvent.click(screen.getByRole("button", { name: "Yeni sipariş" }));
    const dialog = screen.getByRole("dialog", { name: "Yeni sipariş" });
    await waitFor(() => expect(within(dialog).getByLabelText("Ürün")).toHaveTextContent("SKU"));
    fireEvent.change(within(dialog).getByLabelText("Ürün"), { target: { value: product.id } });
    fireEvent.change(within(dialog).getByLabelText("Miktar"), { target: { value: "2" } });
    fireEvent.click(within(dialog).getByRole("button", { name: "Siparişi Oluştur" }));
    expect(await within(dialog).findByText("Sipariş oluşturulamadı")).toBeInTheDocument();
    expect(within(dialog).getByText(/yeterli kullanılabilir stok/)).toBeInTheDocument();
  });
});
