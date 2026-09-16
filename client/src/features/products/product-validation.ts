import { z } from "zod";
import type { CreateProductInput, PackagingInput } from "../../api/contracts";

const requiredNumber = (label: string, options?: { nonnegative?: boolean; positive?: boolean }) =>
  z
    .string()
    .trim()
    .min(1, `${label} zorunludur.`)
    .transform(Number)
    .pipe(
      z
        .number()
        .refine(
          (value) => options?.positive !== true || value > 0,
          `${label} sıfırdan büyük olmalıdır.`,
        )
        .refine((value) => options?.nonnegative !== true || value >= 0, `${label} negatif olamaz.`),
    );

const optionalPositive = (label: string) =>
  z
    .string()
    .trim()
    .transform((value) => (value === "" ? null : Number(value)))
    .pipe(z.number().positive(`${label} sıfırdan büyük olmalıdır.`).nullable());

export interface PackagingDraft {
  level: string;
  uomCode: string;
  conversionFactor: string;
  barcode: string;
  weightInKg: string;
  lengthInMm: string;
  widthInMm: string;
  heightInMm: string;
}

export interface ProductDraft {
  sku: string;
  name: string;
  baseUomCode: string;
  basePriceAmount: string;
  basePriceCurrencyCode: string;
  packagings: PackagingDraft[];
}

export const emptyPackaging = (level: number): PackagingDraft => ({
  level: String(level),
  uomCode: "",
  conversionFactor: level === 0 ? "1" : "",
  barcode: "",
  weightInKg: "",
  lengthInMm: "",
  widthInMm: "",
  heightInMm: "",
});

const packagingDraftSchema = z.object({
  level: requiredNumber("Level", { nonnegative: true }).pipe(
    z.number().int("Level tam sayı olmalıdır."),
  ),
  uomCode: z
    .string()
    .trim()
    .min(1, "UoM kodu zorunludur.")
    .max(16, "UoM kodu en fazla 16 karakter olabilir.")
    .transform((value) => value.toUpperCase()),
  conversionFactor: requiredNumber("Dönüşüm katsayısı", { positive: true }),
  barcode: z
    .string()
    .trim()
    .max(64, "Barkod en fazla 64 karakter olabilir.")
    .transform((value) => (value === "" ? null : value)),
  weightInKg: optionalPositive("Ağırlık"),
  lengthInMm: optionalPositive("Uzunluk"),
  widthInMm: optionalPositive("Genişlik"),
  heightInMm: optionalPositive("Yükseklik"),
});

export const productDraftSchema = z
  .object({
    sku: z
      .string()
      .trim()
      .min(1, "SKU zorunludur.")
      .max(64, "SKU en fazla 64 karakter olabilir.")
      .transform((value) => value.toUpperCase()),
    name: z
      .string()
      .trim()
      .min(1, "Ürün adı zorunludur.")
      .max(200, "Ürün adı en fazla 200 karakter olabilir."),
    baseUomCode: z
      .string()
      .trim()
      .min(1, "Temel UoM zorunludur.")
      .max(16, "Temel UoM en fazla 16 karakter olabilir.")
      .transform((value) => value.toUpperCase()),
    basePriceAmount: requiredNumber("Temel fiyat", { nonnegative: true }),
    basePriceCurrencyCode: z
      .string()
      .trim()
      .regex(/^[A-Za-z]{3}$/, "Para birimi üç harfli ISO kodu olmalıdır.")
      .transform((value) => value.toUpperCase()),
    packagings: z.array(packagingDraftSchema).min(1, "En az temel packaging satırı bulunmalıdır."),
  })
  .superRefine((value, context) => {
    const levelZero = value.packagings.filter((item) => item.level === 0);
    if (levelZero.length !== 1) {
      context.addIssue({
        code: "custom",
        path: ["packagings"],
        message: "Tam olarak bir level 0 packaging bulunmalıdır.",
      });
    }
    const basePackaging = levelZero[0];
    if (
      basePackaging !== undefined &&
      (basePackaging.uomCode !== value.baseUomCode || basePackaging.conversionFactor !== 1)
    ) {
      context.addIssue({
        code: "custom",
        path: ["packagings", 0, "uomCode"],
        message: "Level 0 satırı temel UoM ve 1 katsayısını kullanmalıdır.",
      });
    }
    for (const key of ["level", "uomCode", "barcode"] as const) {
      const seen = new Set<string | number>();
      value.packagings.forEach((item, index) => {
        const candidate = item[key];
        if (candidate !== null && candidate !== "" && seen.has(candidate)) {
          context.addIssue({
            code: "custom",
            path: ["packagings", index, key],
            message: "Packaging level, UoM ve dolu barkod değerleri benzersiz olmalıdır.",
          });
        }
        if (candidate !== null && candidate !== "") {
          seen.add(candidate);
        }
      });
    }
  })
  .transform((value): CreateProductInput => ({
    ...value,
    packagings: value.packagings.map((packaging): PackagingInput => ({ id: null, ...packaging })),
  }));

export const standalonePackagingSchema = packagingDraftSchema.transform(
  (value): PackagingInput => ({
    id: null,
    ...value,
  }),
);
