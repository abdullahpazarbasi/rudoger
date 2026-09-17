import { z } from "zod";

const decimalSchema = z.union([z.number(), z.string()]).transform(Number);
const integerSchema = z.union([z.number(), z.string()]).transform(Number);
const nullableDecimalSchema = z
  .union([z.number(), z.string(), z.null()])
  .transform((value) => (value === null ? null : Number(value)));

export const productPackagingSchema = z.object({
  id: z.uuid(),
  level: integerSchema,
  uomCode: z.string(),
  conversionFactor: decimalSchema,
  barcode: z.string().nullable(),
  weightInKg: nullableDecimalSchema,
  lengthInMm: nullableDecimalSchema,
  widthInMm: nullableDecimalSchema,
  heightInMm: nullableDecimalSchema,
});

export const productSchema = z.object({
  id: z.uuid(),
  sku: z.string(),
  name: z.string(),
  baseUomCode: z.string(),
  basePriceAmount: decimalSchema,
  basePriceCurrencyCode: z.string(),
  packagings: z.array(productPackagingSchema),
});

export const stockItemSchema = z.object({
  id: z.uuid(),
  productId: z.uuid(),
  baseUomCode: z.string(),
  onHandQuantity: decimalSchema,
  reservedQuantity: decimalSchema,
  availableQuantity: decimalSchema,
});

export const stockMovementTypeSchema = z.enum([
  "Receipt",
  "Adjustment",
  "Deduction",
  "Reserved",
  "Committed",
  "Released",
]);

export const stockMovementSchema = z.object({
  id: z.uuid(),
  stockItemId: z.uuid(),
  type: stockMovementTypeSchema,
  uomCode: z.string(),
  quantity: decimalSchema,
  onHandQuantityDelta: decimalSchema,
  reservedQuantityDelta: decimalSchema,
  referenceType: z.string(),
  referenceId: z.uuid().nullable(),
  idempotencyKey: z.string(),
  correlationId: z.string(),
  occurredAtUtc: z.string(),
});

export const orderStatusSchema = z.enum(["Placed", "Shipped", "Cancelled"]);
export const orderPlacementStatusSchema = z.enum(["Pending", "Succeeded", "Failed"]);
export const orderTransitionStatusSchema = z.enum(["Pending", "Succeeded"]);
export const orderTransitionTargetSchema = z.enum(["Shipped", "Cancelled"]);

export const orderLineSchema = z.object({
  id: z.uuid(),
  num: integerSchema,
  productId: z.uuid(),
  uomCode: z.string(),
  quantity: decimalSchema,
  unitPriceAmount: decimalSchema,
  unitPriceCurrencyCode: z.string(),
});

export const orderSchema = z.object({
  id: z.uuid(),
  orderNumber: z.string(),
  status: orderStatusSchema,
  userId: z.uuid(),
  lines: z.array(orderLineSchema),
  pendingTransitionId: z.uuid().nullable(),
  pendingTransitionTarget: orderTransitionTargetSchema.nullable(),
});

export const orderPlacementSchema = z.object({
  id: z.uuid(),
  orderId: z.uuid(),
  userId: z.uuid(),
  status: orderPlacementStatusSchema,
  lines: z.array(
    z.object({
      num: integerSchema,
      productId: z.uuid(),
      uomCode: z.string(),
      quantity: decimalSchema,
    }),
  ),
  failureCode: z.string().nullable(),
  failureDetail: z.string().nullable(),
});

export const orderTransitionSchema = z.object({
  id: z.uuid(),
  orderId: z.uuid(),
  target: orderTransitionTargetSchema,
  status: orderTransitionStatusSchema,
});

export interface Page<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
}

export interface TokenResult {
  accessToken: string;
  tokenType: string;
  expiresIn: number;
}

export type Product = z.infer<typeof productSchema>;
export type ProductPackaging = z.infer<typeof productPackagingSchema>;
export type StockItem = z.infer<typeof stockItemSchema>;
export type StockMovement = z.infer<typeof stockMovementSchema>;
export type StockMovementType = z.infer<typeof stockMovementTypeSchema>;
export type Order = z.infer<typeof orderSchema>;
export type OrderPlacement = z.infer<typeof orderPlacementSchema>;
export type OrderTransition = z.infer<typeof orderTransitionSchema>;
export type OrderTransitionTarget = z.infer<typeof orderTransitionTargetSchema>;

export interface PackagingInput {
  id: string | null;
  level: number;
  uomCode: string;
  conversionFactor: number;
  barcode: string | null;
  weightInKg: number | null;
  lengthInMm: number | null;
  widthInMm: number | null;
  heightInMm: number | null;
}

export interface CreateProductInput {
  sku: string;
  name: string;
  baseUomCode: string;
  basePriceAmount: number;
  basePriceCurrencyCode: string;
  packagings: PackagingInput[];
}

export interface CreateOrderInput {
  lines: Array<{ productId: string; uomCode: string; quantity: number }>;
}
