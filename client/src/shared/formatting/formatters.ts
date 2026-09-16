const numberFormatter = new Intl.NumberFormat("tr-TR", { maximumFractionDigits: 6 });
const dateFormatter = new Intl.DateTimeFormat("tr-TR", { dateStyle: "medium", timeStyle: "short" });

export function formatNumber(value: number): string {
  return numberFormatter.format(value);
}

export function formatMoney(value: number, currencyCode: string): string {
  try {
    return new Intl.NumberFormat("tr-TR", { style: "currency", currency: currencyCode }).format(
      value,
    );
  } catch {
    return `${formatNumber(value)} ${currencyCode}`;
  }
}

export function formatDateTime(value: string): string {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : dateFormatter.format(date);
}

export const orderStatusLabels = {
  Placed: "Alındı",
  Shipped: "Gönderildi",
  Cancelled: "İptal edildi",
} as const;

export const orderStatusTones = {
  Placed: "pending",
  Shipped: "success",
  Cancelled: "danger",
} as const;

export const movementTypeLabels = {
  Receipt: "Giriş",
  Adjustment: "Düzeltme",
  Deduction: "Düşüm",
  Reserved: "Rezerve edildi",
  Committed: "Kesinleştirildi",
  Released: "Serbest bırakıldı",
} as const;
