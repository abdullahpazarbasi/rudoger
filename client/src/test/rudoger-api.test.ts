import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { apiClient } from "../api/client/api-client";
import {
  addPackaging,
  createOrder,
  createOrderTransition,
  createProduct,
  createStockItem,
  createStockMovement,
  deletePackaging,
  deleteProduct,
  exchangeToken,
  getHealth,
  getOrder,
  getOrderPlacement,
  getOrderTransition,
  getProduct,
  getStockItem,
  listOrders,
  listProducts,
  listStockItems,
  listStockMovements,
  patchPackaging,
  patchProduct,
} from "../api/client/rudoger-api";
import { authStore } from "../features/authn/auth-store";

vi.mock("../api/client/api-client", () => ({
  apiBaseUrl: "http://localhost",
  apiClient: { GET: vi.fn(), POST: vi.fn(), PATCH: vi.fn(), DELETE: vi.fn() },
}));
const clientMock = apiClient as unknown as {
  GET: ReturnType<typeof vi.fn>;
  POST: ReturnType<typeof vi.fn>;
  PATCH: ReturnType<typeof vi.fn>;
  DELETE: ReturnType<typeof vi.fn>;
};
const ok = (data: unknown) =>
  Promise.resolve({ data, response: new Response(null, { status: 200 }) });
const id = (suffix: number) => `01990101-0000-7000-8000-${String(suffix).padStart(12, "0")}`;
const packaging = {
  id: id(2),
  level: 0,
  uomCode: "EA",
  conversionFactor: "1",
  barcode: null,
  weightInKg: null,
  lengthInMm: null,
  widthInMm: null,
  heightInMm: null,
};
const product = {
  id: id(1),
  sku: "SKU",
  name: "Product",
  baseUomCode: "EA",
  basePriceAmount: "12",
  basePriceCurrencyCode: "TRY",
  packagings: [packaging],
};
const stock = {
  id: id(3),
  productId: id(1),
  baseUomCode: "EA",
  onHandQuantity: "10",
  reservedQuantity: "2",
  availableQuantity: "8",
};
const movement = {
  id: id(4),
  stockItemId: id(3),
  type: "Receipt",
  onHandQuantityDelta: "5",
  reservedQuantityDelta: "0",
  referenceType: "MANUAL",
  referenceId: null,
  idempotencyKey: "key",
  correlationId: "corr",
  sourceEventId: id(5),
  occurredAtUtc: "2026-01-01T00:00:00Z",
};
const order = {
  id: id(6),
  orderNumber: "ORD-1",
  status: "Placed",
  userId: id(7),
  lines: [
    {
      id: id(8),
      num: 1,
      productId: id(1),
      uomCode: "EA",
      quantity: "2",
      unitPriceAmount: "12",
      unitPriceCurrencyCode: "TRY",
    },
  ],
  pendingTransitionId: null,
  pendingTransitionTarget: null,
};
const placement = {
  id: id(9),
  orderId: id(6),
  userId: id(7),
  status: "Pending",
  lines: [{ num: 1, productId: id(1), uomCode: "EA", quantity: "2" }],
  failureCode: null,
  failureDetail: null,
};
const transition = { id: id(10), orderId: id(6), target: "Shipped", status: "Pending" };

function authenticate(): void {
  const token = `h.${btoa(JSON.stringify({ exp: Math.floor(Date.now() / 1000) + 60 }))}.s`;
  authStore.setSession({ accessToken: token, tokenType: "Bearer", expiresIn: 60 });
}

describe("Rudoger API anti-corruption boundary", () => {
  beforeEach(() => {
    authStore.clear();
    vi.clearAllMocks();
    authenticate();
  });
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("parses every read model and page", async () => {
    clientMock.GET.mockImplementation((path: string) => {
      if (path === "/api/v1/product/products")
        return ok({ items: [product], pageNumber: "1", pageSize: "20", totalCount: "1" });
      if (path.includes("product/products/")) return ok(product);
      if (path === "/api/v1/inventory/stock-items")
        return ok({ items: [stock], pageNumber: 1, pageSize: 20, totalCount: 1 });
      if (path.endsWith("/movements"))
        return ok({ items: [movement], pageNumber: 1, pageSize: 20, totalCount: 1 });
      if (path.includes("inventory/stock-items/")) return ok(stock);
      if (path === "/api/v1/order/orders")
        return ok({ items: [order], pageNumber: 1, pageSize: 20, totalCount: 1 });
      if (path.includes("order-placements")) return ok(placement);
      if (path.includes("transitions")) return ok(transition);
      return ok(order);
    });
    expect(
      (await listProducts({ ids: [id(1)], pageNumber: 1, pageSize: 10 })).items[0]?.basePriceAmount,
    ).toBe(12);
    expect((await getProduct(id(1))).id).toBe(id(1));
    expect(
      (await listStockItems({ productId: id(1), pageNumber: 1 })).items[0]?.availableQuantity,
    ).toBe(8);
    expect((await getStockItem(id(3))).onHandQuantity).toBe(10);
    expect((await listStockMovements(id(3), 1)).items[0]?.type).toBe("Receipt");
    expect((await listOrders(1)).items[0]?.status).toBe("Placed");
    expect((await getOrder(id(6))).lines[0]?.quantity).toBe(2);
    expect((await getOrderPlacement(id(9))).status).toBe("Pending");
    expect((await getOrderTransition(id(6), id(10))).target).toBe("Shipped");
  });

  it("sends commands, headers, numeric enums and JSON Patch media types", async () => {
    clientMock.POST.mockImplementation((path: string) =>
      path.includes("transitions")
        ? ok(transition)
        : path.includes("/orders")
          ? ok(placement)
          : path.endsWith("/movements")
            ? ok(movement)
            : path.includes("stock-items")
              ? ok(stock)
              : path.includes("packagings")
                ? ok(packaging)
                : ok(product),
    );
    clientMock.PATCH.mockImplementation((path: string) =>
      path.includes("packagings") ? ok(packaging) : ok(product),
    );
    clientMock.DELETE.mockResolvedValue({ response: new Response(null, { status: 204 }) });
    const input = {
      sku: "SKU",
      name: "Product",
      baseUomCode: "EA",
      basePriceAmount: 12,
      basePriceCurrencyCode: "TRY",
      packagings: [
        {
          id: null,
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
    expect((await createProduct(input)).sku).toBe("SKU");
    expect((await patchProduct(id(1), [{ op: "replace", path: "/name", value: "New" }])).name).toBe(
      "Product",
    );
    await deleteProduct(id(1));
    await addPackaging(id(1), {
      ...input.packagings[0]!,
      level: 1,
      uomCode: "BOX",
      conversionFactor: 10,
    });
    await patchPackaging(id(1), id(2), [{ op: "replace", path: "/barcode", value: "X" }]);
    await deletePackaging(id(1), id(2));
    await createStockItem(id(1), 5, "stock-key");
    await createStockMovement(id(3), "Adjustment", -2, "movement-key");
    await createOrder({ lines: [{ productId: id(1), uomCode: "EA", quantity: 2 }] }, "order-key");
    await createOrderTransition(id(6), "Cancelled");
    const movementCall = clientMock.POST.mock.calls.find((call) =>
      call[0].toString().endsWith("/movements"),
    );
    expect(movementCall?.[1]).toMatchObject({
      params: { header: { "Idempotency-Key": "movement-key" } },
      body: { type: 1, quantity: -2 },
    });
    const transitionCall = clientMock.POST.mock.calls.find((call) =>
      call[0].toString().endsWith("/transitions"),
    );
    expect(transitionCall?.[1]).toMatchObject({ body: { target: 1 } });
    expect(clientMock.PATCH.mock.calls[0]?.[1]).toMatchObject({
      headers: { "Content-Type": "application/json-patch+json" },
    });
  });

  it("allows login anonymously but rejects protected calls before the network", async () => {
    authStore.clear();
    clientMock.POST.mockReturnValue(
      ok({ accessToken: "jwt", tokenType: "Bearer", expiresIn: "60" }),
    );
    expect((await exchangeToken("u", "p")).expiresIn).toBe(60);
    await expect(getProduct(id(1))).rejects.toMatchObject({
      details: { type: "client/authentication-required" },
    });
    expect(clientMock.GET).not.toHaveBeenCalled();
  });

  it("normalizes server, network and contract failures", async () => {
    clientMock.GET.mockResolvedValueOnce({
      error: { type: "https://x/insufficient-stock", detail: "raw" },
      response: new Response(null, { status: 409 }),
    });
    await expect(getProduct(id(1))).rejects.toMatchObject({
      details: { kind: "problem", status: 409 },
    });
    clientMock.GET.mockRejectedValueOnce(new TypeError("offline"));
    await expect(getProduct(id(1))).rejects.toMatchObject({ details: { kind: "network" } });
    clientMock.GET.mockReturnValueOnce(ok({ unexpected: true }));
    await expect(getProduct(id(1))).rejects.toMatchObject({ details: { kind: "contract" } });
    clientMock.DELETE.mockRejectedValueOnce(new Error("offline"));
    await expect(deleteProduct(id(1))).rejects.toMatchObject({ details: { kind: "network" } });
  });

  it("strictly evaluates health responses and preserves invalid bodies", async () => {
    vi.stubGlobal(
      "fetch",
      vi
        .fn()
        .mockResolvedValueOnce(
          new Response(JSON.stringify({ status: "Healthy", checks: {} }), { status: 200 }),
        )
        .mockResolvedValueOnce(
          new Response(
            JSON.stringify({
              status: "Degraded",
              checks: { db: { status: "Degraded", description: null } },
            }),
            { status: 200 },
          ),
        )
        .mockResolvedValueOnce(new Response("{}", { status: 500 }))
        .mockRejectedValueOnce(new TypeError("offline")),
    );
    expect((await getHealth()).healthy).toBe(true);
    expect((await getHealth()).healthy).toBe(false);
    await expect(getHealth()).rejects.toMatchObject({ details: { kind: "problem" } });
    await expect(getHealth()).rejects.toMatchObject({ details: { kind: "network" } });
  });
});
