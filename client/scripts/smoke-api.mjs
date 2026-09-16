const baseUrl = process.env.SMOKE_API_BASE_URL ?? "http://127.0.0.1:8080";
const username = process.env.SMOKE_USERNAME ?? "abdullah";
const password = process.env.SMOKE_PASSWORD ?? "12345678";

let accessToken = null;

async function request(path, options = {}) {
  const headers = new Headers(options.headers);
  headers.set("Accept", "application/json");
  headers.set("X-Correlation-Id", crypto.randomUUID());
  if (options.body !== undefined) {
    headers.set("Content-Type", "application/json");
  }
  if (accessToken !== null) {
    headers.set("Authorization", `Bearer ${accessToken}`);
  }

  const response = await fetch(`${baseUrl}${path}`, {
    ...options,
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
    headers,
  });
  const text = await response.text();
  const body = text === "" ? null : JSON.parse(text);
  if (!response.ok) {
    throw new Error(`${options.method ?? "GET"} ${path} failed with ${response.status}: ${text}`);
  }
  return body;
}

async function poll(path, terminalStatuses) {
  const deadline = Date.now() + 30_000;
  while (Date.now() < deadline) {
    const result = await request(path);
    if (terminalStatuses.has(result.status)) {
      return result;
    }
    await new Promise((resolve) => setTimeout(resolve, 250));
  }
  throw new Error(`Timed out while polling ${path}.`);
}

function assert(condition, message) {
  if (!condition) {
    throw new Error(message);
  }
}

const health = await request("/health");
assert(health.status === "Healthy", `Expected Healthy, received ${health.status}.`);

const token = await request("/api/v1/authn/tokens", {
  method: "POST",
  body: { username, password },
});
assert(typeof token.accessToken === "string", "Token exchange did not return an access token.");
accessToken = token.accessToken;

const suffix = `${Date.now().toString(36)}-${crypto.randomUUID().slice(0, 8)}`.toUpperCase();
const product = await request("/api/v1/product/products", {
  method: "POST",
  body: {
    sku: `SMOKE-${suffix}`,
    name: `Client smoke ${suffix}`,
    baseUomCode: "EA",
    basePriceAmount: 12.5,
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
  },
});

const stock = await request("/api/v1/inventory/stock-items", {
  method: "POST",
  headers: { "Idempotency-Key": `client-smoke-stock-${suffix}` },
  body: { productId: product.id, openingQuantity: 10 },
});

const placement = await request("/api/v1/order/orders", {
  method: "POST",
  headers: { "Idempotency-Key": `client-smoke-order-${suffix}` },
  body: { lines: [{ productId: product.id, uomCode: "EA", quantity: 2 }] },
});
const completedPlacement = await poll(
  `/api/v1/order/order-placements/${placement.id}`,
  new Set(["Succeeded", "Failed"]),
);
assert(
  completedPlacement.status === "Succeeded",
  `Order placement failed: ${completedPlacement.failureCode ?? "unknown"} - ${completedPlacement.failureDetail ?? "no detail"}`,
);

const transition = await request(`/api/v1/order/orders/${completedPlacement.orderId}/transitions`, {
  method: "POST",
  body: { target: 0 },
});
const completedTransition = await poll(
  `/api/v1/order/orders/${completedPlacement.orderId}/transitions/${transition.id}`,
  new Set(["Succeeded"]),
);
assert(completedTransition.status === "Succeeded", "Order transition did not succeed.");

const finalStock = await request(`/api/v1/inventory/stock-items/${stock.id}`);
assert(Number(finalStock.onHandQuantity) === 8, "Shipment did not deduct on-hand stock.");
assert(Number(finalStock.reservedQuantity) === 0, "Shipment left stock reserved.");

process.stdout.write(
  `${JSON.stringify(
    {
      health: health.status,
      productId: product.id,
      stockItemId: stock.id,
      orderId: completedPlacement.orderId,
      transitionStatus: completedTransition.status,
      finalOnHandQuantity: Number(finalStock.onHandQuantity),
      finalReservedQuantity: Number(finalStock.reservedQuantity),
    },
    null,
    2,
  )}\n`,
);
