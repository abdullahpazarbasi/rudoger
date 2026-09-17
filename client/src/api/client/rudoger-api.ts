import { z, type ZodType } from "zod";
import {
  orderPlacementSchema,
  orderSchema,
  orderTransitionSchema,
  productPackagingSchema,
  productSchema,
  stockItemSchema,
  stockMovementSchema,
  type CreateOrderInput,
  type CreateProductInput,
  type Order,
  type OrderPlacement,
  type OrderTransition,
  type OrderTransitionTarget,
  type PackagingInput,
  type Page,
  type Product,
  type ProductPackaging,
  type StockItem,
  type StockMovement,
  type StockMovementType,
  type TokenResult,
} from "../contracts";
import {
  ClientError,
  contractError,
  networkError,
  problemError,
  sessionUnavailableError,
} from "../errors/client-error";
import { authStore } from "../../features/authn/auth-store";
import { apiBaseUrl, apiClient } from "./api-client";
import type { ReplaceOperation } from "./patch";

interface ApiResult {
  data?: unknown;
  error?: unknown;
  response: Response;
}

const pageSchema = <T>(item: ZodType<T>): ZodType<Page<T>> =>
  z.object({
    items: z.array(item),
    pageNumber: z.union([z.number(), z.string()]).transform(Number),
    pageSize: z.union([z.number(), z.string()]).transform(Number),
    totalCount: z.union([z.number(), z.string()]).transform(Number),
  });

const tokenSchema = z.object({
  accessToken: z.string().min(1),
  tokenType: z.string().min(1),
  expiresIn: z.union([z.number(), z.string()]).transform(Number),
});

function requireSession(): void {
  const state = authStore.getSnapshot();
  if (state.status !== "authenticated") {
    throw sessionUnavailableError(state.status);
  }
}

async function call<T>(
  operation: () => Promise<ApiResult>,
  schema: ZodType<T>,
  protectedRequest = true,
): Promise<T> {
  if (protectedRequest) {
    requireSession();
  }
  let result: ApiResult;
  try {
    result = await operation();
  } catch (error) {
    if (error instanceof ClientError) {
      throw error;
    }
    throw networkError(error);
  }
  if (!result.response.ok || result.error !== undefined) {
    throw problemError(result.error, result.response);
  }
  try {
    return schema.parse(result.data);
  } catch (error) {
    throw contractError(error);
  }
}

async function command(operation: () => Promise<ApiResult>): Promise<void> {
  requireSession();
  let result: ApiResult;
  try {
    result = await operation();
  } catch (error) {
    throw networkError(error);
  }
  if (!result.response.ok || result.error !== undefined) {
    throw problemError(result.error, result.response);
  }
}

export async function exchangeToken(username: string, password: string): Promise<TokenResult> {
  return call(
    () => apiClient.POST("/api/v1/authn/tokens", { body: { username, password } }),
    tokenSchema,
    false,
  );
}

export interface ProductFilters {
  ids: string[];
  pageNumber: number;
  pageSize?: number;
}

export async function listProducts(
  filters: ProductFilters,
  signal?: AbortSignal,
): Promise<Page<Product>> {
  const query: { ids?: string[]; pageNumber: number; pageSize?: number } = {
    pageNumber: filters.pageNumber,
  };
  if (filters.ids.length > 0) {
    query.ids = filters.ids;
  }
  if (filters.pageSize !== undefined) {
    query.pageSize = filters.pageSize;
  }
  return call(
    () => apiClient.GET("/api/v1/product/products", { params: { query }, signal: signal ?? null }),
    pageSchema(productSchema),
  );
}

export async function getProduct(productId: string, signal?: AbortSignal): Promise<Product> {
  return call(
    () =>
      apiClient.GET("/api/v1/product/products/{productId}", {
        params: { path: { productId } },
        signal: signal ?? null,
      }),
    productSchema,
  );
}

export async function createProduct(input: CreateProductInput): Promise<Product> {
  return call(() => apiClient.POST("/api/v1/product/products", { body: input }), productSchema);
}

export async function patchProduct(
  productId: string,
  operations: ReplaceOperation[],
): Promise<Product> {
  return call(
    () =>
      apiClient.PATCH("/api/v1/product/products/{productId}", {
        params: { path: { productId } },
        body: operations,
        bodySerializer: (body) => JSON.stringify(body),
        headers: { "Content-Type": "application/json-patch+json" },
      }),
    productSchema,
  );
}

export async function deleteProduct(productId: string): Promise<void> {
  return command(() =>
    apiClient.DELETE("/api/v1/product/products/{productId}", {
      params: { path: { productId } },
    }),
  );
}

export async function addPackaging(
  productId: string,
  input: PackagingInput,
): Promise<ProductPackaging> {
  return call(
    () =>
      apiClient.POST("/api/v1/product/products/{productId}/packagings", {
        params: { path: { productId } },
        body: input,
      }),
    productPackagingSchema,
  );
}

export async function patchPackaging(
  productId: string,
  packagingId: string,
  operations: ReplaceOperation[],
): Promise<ProductPackaging> {
  return call(
    () =>
      apiClient.PATCH("/api/v1/product/products/{productId}/packagings/{packagingId}", {
        params: { path: { productId, packagingId } },
        body: operations,
        bodySerializer: (body) => JSON.stringify(body),
        headers: { "Content-Type": "application/json-patch+json" },
      }),
    productPackagingSchema,
  );
}

export async function deletePackaging(productId: string, packagingId: string): Promise<void> {
  return command(() =>
    apiClient.DELETE("/api/v1/product/products/{productId}/packagings/{packagingId}", {
      params: { path: { productId, packagingId } },
    }),
  );
}

export interface StockItemFilters {
  productId?: string;
  pageNumber: number;
  pageSize?: number;
}

export async function listStockItems(
  filters: StockItemFilters,
  signal?: AbortSignal,
): Promise<Page<StockItem>> {
  return call(
    () =>
      apiClient.GET("/api/v1/inventory/stock-items", {
        params: { query: filters },
        signal: signal ?? null,
      }),
    pageSchema(stockItemSchema),
  );
}

export async function getStockItem(stockItemId: string, signal?: AbortSignal): Promise<StockItem> {
  return call(
    () =>
      apiClient.GET("/api/v1/inventory/stock-items/{stockItemId}", {
        params: { path: { stockItemId } },
        signal: signal ?? null,
      }),
    stockItemSchema,
  );
}

export async function createStockItem(
  productId: string,
  uomCode: string,
  openingQuantity: number,
  idempotencyKey: string,
): Promise<StockItem> {
  return call(
    () =>
      apiClient.POST("/api/v1/inventory/stock-items", {
        params: { header: { "Idempotency-Key": idempotencyKey } },
        body: { productId, uomCode, openingQuantity },
      }),
    stockItemSchema,
  );
}

const movementTypeNumbers: Record<
  Extract<StockMovementType, "Receipt" | "Adjustment" | "Deduction">,
  number
> = {
  Receipt: 0,
  Adjustment: 1,
  Deduction: 2,
};

export async function createStockMovement(
  stockItemId: string,
  type: Extract<StockMovementType, "Receipt" | "Adjustment" | "Deduction">,
  uomCode: string,
  quantity: number,
  idempotencyKey: string,
): Promise<StockMovement> {
  return call(
    () =>
      apiClient.POST("/api/v1/inventory/stock-items/{stockItemId}/movements", {
        params: {
          path: { stockItemId },
          header: { "Idempotency-Key": idempotencyKey },
        },
        body: { type: movementTypeNumbers[type], uomCode, quantity },
      }),
    stockMovementSchema,
  );
}

export async function listStockMovements(
  stockItemId: string,
  pageNumber: number,
  signal?: AbortSignal,
): Promise<Page<StockMovement>> {
  return call(
    () =>
      apiClient.GET("/api/v1/inventory/stock-items/{stockItemId}/movements", {
        params: { path: { stockItemId }, query: { pageNumber } },
        signal: signal ?? null,
      }),
    pageSchema(stockMovementSchema),
  );
}

export async function listOrders(pageNumber: number, signal?: AbortSignal): Promise<Page<Order>> {
  return call(
    () =>
      apiClient.GET("/api/v1/order/orders", {
        params: { query: { pageNumber } },
        signal: signal ?? null,
      }),
    pageSchema(orderSchema),
  );
}

export async function getOrder(orderId: string, signal?: AbortSignal): Promise<Order> {
  return call(
    () =>
      apiClient.GET("/api/v1/order/orders/{orderId}", {
        params: { path: { orderId } },
        signal: signal ?? null,
      }),
    orderSchema,
  );
}

export async function createOrder(
  input: CreateOrderInput,
  idempotencyKey: string,
): Promise<OrderPlacement> {
  return call(
    () =>
      apiClient.POST("/api/v1/order/orders", {
        params: { header: { "Idempotency-Key": idempotencyKey } },
        body: input,
      }),
    orderPlacementSchema,
  );
}

export async function getOrderPlacement(
  placementId: string,
  signal?: AbortSignal,
): Promise<OrderPlacement> {
  return call(
    () =>
      apiClient.GET("/api/v1/order/order-placements/{placementId}", {
        params: { path: { placementId } },
        signal: signal ?? null,
      }),
    orderPlacementSchema,
  );
}

const transitionTargetNumbers: Record<OrderTransitionTarget, number> = {
  Shipped: 0,
  Cancelled: 1,
};

export async function createOrderTransition(
  orderId: string,
  target: OrderTransitionTarget,
): Promise<OrderTransition> {
  return call(
    () =>
      apiClient.POST("/api/v1/order/orders/{orderId}/transitions", {
        params: { path: { orderId } },
        body: { target: transitionTargetNumbers[target] },
      }),
    orderTransitionSchema,
  );
}

export async function getOrderTransition(
  orderId: string,
  transitionId: string,
  signal?: AbortSignal,
): Promise<OrderTransition> {
  return call(
    () =>
      apiClient.GET("/api/v1/order/orders/{orderId}/transitions/{transitionId}", {
        params: { path: { orderId, transitionId } },
        signal: signal ?? null,
      }),
    orderTransitionSchema,
  );
}

export interface HealthStatus {
  healthy: boolean;
  status: string;
  checks: Record<string, { status: string; description: string | null }>;
}

export async function getHealth(signal?: AbortSignal): Promise<HealthStatus> {
  let response: Response;
  try {
    response = await fetch(`${apiBaseUrl}/health`, {
      headers: { Accept: "application/json", "X-Correlation-Id": crypto.randomUUID() },
      signal: signal ?? null,
    });
  } catch (error) {
    throw networkError(error);
  }
  const body: unknown = await response.json().catch(() => null);
  const schema = z.object({
    status: z.string(),
    checks: z.record(
      z.string(),
      z.object({ status: z.string(), description: z.string().nullable() }),
    ),
  });
  const parsed = schema.safeParse(body);
  if (!parsed.success) {
    if (!response.ok) {
      throw problemError(body, response);
    }
    throw contractError(parsed.error);
  }
  return {
    healthy: response.ok && parsed.data.status === "Healthy",
    status: parsed.data.status,
    checks: parsed.data.checks,
  };
}
