import { describe, expect, it, vi } from "vitest";
import { IdempotencyIntent, createIdempotencyKey } from "../api/client/idempotency";
import { changedFields } from "../api/client/patch";
import {
  orderPlacementSchema,
  orderSchema,
  productSchema,
  stockMovementSchema,
} from "../api/contracts";
import { formatDateTime, formatMoney, formatNumber } from "../shared/formatting/formatters";
import {
  emptyPackaging,
  productDraftSchema,
  standalonePackagingSchema,
} from "../features/products/product-validation";

describe("domain boundary helpers", () => {
  it("builds replace operations only for changed fields", () => {
    expect(
      changedFields(
        { name: "Old", price: 2 },
        { name: "New", price: 2 },
        { name: "/name", price: "/price" },
      ),
    ).toEqual([{ op: "replace", path: "/name", value: "New" }]);
    expect(changedFields({ name: "Same" }, { name: "Same" }, { name: "/name" })).toEqual([]);
  });

  it("keeps an idempotency key for the same intent and rotates it for a new one", () => {
    vi.spyOn(crypto, "randomUUID")
      .mockReturnValueOnce("00000000-0000-4000-8000-000000000001")
      .mockReturnValueOnce("00000000-0000-4000-8000-000000000002")
      .mockReturnValueOnce("00000000-0000-4000-8000-000000000003");
    expect(createIdempotencyKey("test")).toContain("00000000-0000-4000-8000-000000000001");
    const intent = new IdempotencyIntent("order");
    const first = intent.keyFor({ quantity: 2 });
    expect(intent.keyFor({ quantity: 2 })).toBe(first);
    expect(intent.keyFor({ quantity: 3 })).not.toBe(first);
    intent.reset();
    expect(intent.keyFor({ quantity: 3 })).not.toBe(first);
  });

  it("enforces product and packaging rules before submission", () => {
    expect(emptyPackaging(0)).toMatchObject({ level: "0", conversionFactor: "1" });
    const valid = productDraftSchema.parse({
      sku: " abc ",
      name: " Ürün ",
      baseUomCode: " ea ",
      basePriceAmount: "12.5",
      basePriceCurrencyCode: "try",
      packagings: [
        { ...emptyPackaging(0), uomCode: "EA" },
        { ...emptyPackaging(1), uomCode: "BOX", conversionFactor: "10", barcode: "B-1" },
      ],
    });
    expect(valid).toMatchObject({
      sku: "ABC",
      baseUomCode: "EA",
      basePriceAmount: 12.5,
      basePriceCurrencyCode: "TRY",
    });
    expect(valid.packagings[0]).toMatchObject({ id: null, level: 0, conversionFactor: 1 });
    const invalid = productDraftSchema.safeParse({
      sku: "",
      name: "",
      baseUomCode: "EA",
      basePriceAmount: "-1",
      basePriceCurrencyCode: "TL",
      packagings: [
        { ...emptyPackaging(0), uomCode: "BOX" },
        { ...emptyPackaging(0), uomCode: "EA" },
      ],
    });
    expect(invalid.success).toBe(false);
    expect(
      standalonePackagingSchema.safeParse({
        ...emptyPackaging(1),
        uomCode: "BOX",
        conversionFactor: "0",
      }).success,
    ).toBe(false);
  });

  it("normalizes decimal strings and string enums returned by the API", () => {
    const product = productSchema.parse({
      id: "01990101-0000-7000-8000-000000000001",
      sku: "A",
      name: "A",
      baseUomCode: "EA",
      basePriceAmount: "3.25",
      basePriceCurrencyCode: "TRY",
      packagings: [
        {
          id: "01990101-0000-7000-8000-000000000010",
          level: "0",
          uomCode: "EA",
          conversionFactor: "1",
          barcode: null,
          weightInKg: "1.5",
          lengthInMm: 10,
          widthInMm: "20",
          heightInMm: 30,
        },
      ],
    });
    expect(product.basePriceAmount).toBe(3.25);
    expect(product.packagings[0]?.weightInKg).toBe(1.5);
    expect(
      stockMovementSchema.parse({
        id: "01990101-0000-7000-8000-000000000002",
        stockItemId: "01990101-0000-7000-8000-000000000003",
        type: "Reserved",
        uomCode: "EA",
        quantity: "2",
        onHandQuantityDelta: "0",
        reservedQuantityDelta: "2",
        referenceType: "ORDER",
        referenceId: null,
        idempotencyKey: "x",
        correlationId: "c",
        occurredAtUtc: "2026-01-01T00:00:00Z",
      }).type,
    ).toBe("Reserved");
    expect(
      orderSchema.parse({
        id: "01990101-0000-7000-8000-000000000005",
        orderNumber: "O-1",
        status: "Placed",
        userId: "01990101-0000-7000-8000-000000000006",
        lines: [],
        pendingTransitionId: null,
        pendingTransitionTarget: null,
      }).status,
    ).toBe("Placed");
    expect(orderPlacementSchema.safeParse({ id: "bad" }).success).toBe(false);
  });

  it("formats Turkish numbers, money and dates with safe fallbacks", () => {
    expect(formatNumber(1234.5)).toContain("1.234");
    expect(formatMoney(10, "TRY")).toContain("10");
    expect(formatMoney(10, "INVALID")).toBe("10 INVALID");
    expect(formatDateTime("not-a-date")).toBe("not-a-date");
    expect(formatDateTime("2026-01-01T00:00:00Z")).not.toBe("2026-01-01T00:00:00Z");
  });
});
