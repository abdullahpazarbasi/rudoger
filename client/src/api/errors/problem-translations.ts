const titles: Record<string, string> = {
  "authentication-failed": "Kimlik doğrulama başarısız oldu.",
  "authentication-required": "Kimlik doğrulaması gerekiyor.",
  "access-forbidden": "Bu işlem için yetkiniz yok.",
  "resource-not-found": "İstenen kaynak bulunamadı.",
  "product-not-found": "İstenen ürün bulunamadı.",
  "order-not-found": "İstenen sipariş bulunamadı.",
  "order-placement-not-found": "İstenen sipariş oluşturma süreci bulunamadı.",
  "order-transition-not-found": "İstenen sipariş durum geçişi bulunamadı.",
  "method-not-allowed": "Bu HTTP yöntemi desteklenmiyor.",
  "unsupported-media-type": "Gönderilen içerik türü desteklenmiyor.",
  "invalid-request": "İstek geçerli değil.",
  "paging-invalid": "Sayfalama değerleri geçerli aralıkta değil.",
  "patch-document-required": "Bir JSON Patch belgesi gönderilmelidir.",
  "patch-operation-unsupported": "JSON Patch belgesi desteklenmeyen bir işlem veya yol içeriyor.",
  "stock-movement-type-invalid": "Gönderilen stok hareketi türü desteklenmiyor.",
  "order-transition-target-invalid": "Gönderilen sipariş durum hedefi desteklenmiyor.",
  "unexpected-error": "Beklenmeyen bir sunucu hatası oluştu.",
  "concurrency-conflict": "Kaynak başka bir işlem tarafından değiştirildi.",
  "request-logging-unavailable": "Zorunlu istek günlüğü kullanılamıyor.",
  "product-sku-conflict": "Bu SKU başka bir üründe kullanılıyor.",
  "product-barcode-conflict": "Bu barkod başka bir packaging kaydında kullanılıyor.",
  "product-has-stock": "Stok kaydı bulunan ürün silinemez.",
  "product-has-orders": "Bir siparişte kullanılan ürün silinemez.",
  "product-in-use": "Ürün etkin bir işlem tarafından kullanılıyor.",
  "product-deleted": "Ürün silinmiş durumda.",
  "product-packaging-required": "Ürün için level-zero packaging zorunludur.",
  "base-packaging-required": "Level-zero packaging zorunludur ve silinemez.",
  "base-packaging-immutable":
    "Level-zero packaging’in level, UoM ve dönüşüm katsayısı değiştirilemez.",
  "base-packaging-already-exists": "Ürünün yalnızca bir level-zero packaging kaydı olabilir.",
  "base-packaging-invalid":
    "Level-zero packaging, temel UoM ve bir dönüşüm katsayısı kullanmalıdır.",
  "product-packaging-duplicate":
    "Packaging level, UoM, kimlik ve barkod değerleri benzersiz olmalıdır.",
  "product-packaging-not-found": "İstenen packaging kaydı bulunamadı.",
  "packaging-not-found": "İstenen UoM kodu bu ürün için tanımlı değil.",
  "packaging-level-invalid": "Packaging level negatif olamaz.",
  "packaging-measurement-invalid": "Girilen packaging ölçüsü pozitif olmalıdır.",
  "product-currency-invalid": "Para birimi kodu üç ASCII harften oluşmalıdır.",
  "stock-item-already-exists": "Bu ürün için stok kaydı zaten var.",
  "stock-item-not-found": "İstenen stok kaydı bulunamadı.",
  "stock-item-missing-for-product": "Bu ürün için stok kaydı bulunamadı.",
  "stock-item-product-unavailable": "Stok kaydının ilgili olduğu ürün kullanılabilir değil.",
  "stock-item-uom-unavailable": "İstenen UoM kodu bu ürün için sunulmuyor.",
  "stock-item-product-rejected": "Ürün bu stok işlemini reddetti.",
  "stock-adjustment-quantity-invalid": "Stok düzeltme miktarı sıfır olamaz.",
  "stock-movement-type-forbidden":
    "Bu stok hareketi yalnızca sipariş iş akışı tarafından oluşturulabilir.",
  "stock-balance-invalid": "Stok bakiyeleri eldeki ≥ ayrılmış ≥ sıfır koşulunu sağlamalıdır.",
  "insufficient-stock": "İşlem için yeterli kullanılabilir stok yok.",
  "stock-reservation-conflict": "Bu referans için etkin bir stok rezervasyonu zaten var.",
  "stock-reservation-not-found": "Bu referans için etkin stok rezervasyonu bulunamadı.",
  "idempotency-key-conflict": "Aynı idempotency anahtarı farklı bir istek için kullanılmış.",
  "order-lines-invalid": "Sipariş 1 ile 100 arasında satır içermelidir.",
  "order-lines-duplicate": "Aynı ürün ve UoM ikilisi siparişte birden fazla kez kullanılamaz.",
  "order-currency-mixed": "Bir siparişte farklı para birimleri kullanılamaz.",
  "order-product-unavailable": "Siparişteki ürünlerden biri kullanılabilir değil.",
  "order-product-uom-unavailable":
    "Sipariş satırlarından biri, ürünün sunmadığı bir UoM kodu istiyor.",
  "order-product-rejected": "Siparişteki ürünlerden biri bu siparişi reddetti.",
  "order-stock-unavailable": "Siparişteki ürünlerden birinin stok kaydı yok.",
  "order-stock-insufficient": "Sipariş satırlarından biri ürünün kullanılabilir miktarını aşıyor.",
  "order-stock-reservation-missing": "Bu sipariş için ayrılmış stok artık tutulmuyor.",
  "order-stock-rejected": "Siparişin gerektirdiği stok işlemi reddedildi.",
  "order-line-uom-invalid": "Sipariş satırındaki UoM kodu geçerli değil.",
  "order-line-quantity-invalid": "Sipariş satırı miktarı pozitif olmalıdır.",
  "order-transition-invalid": "Sipariş mevcut durumundan ilerletilemez.",
  "order-transition-pending": "Siparişin zaten bekleyen bir durum geçişi var.",
  "order-transition-not-pending": "İstenen sipariş geçişi bekleyen durumda değil.",
  "order-placement-completed": "Sipariş oluşturma süreci daha önce tamamlanmış.",
  "user-id-required": "Kullanıcı kimliği zorunludur.",
  "product-id-required": "Ürün kimliği zorunludur.",
  "packaging-id-required": "Packaging kimliği zorunludur.",
  "usage-operation-id-required": "Kullanım işlemi kimliği zorunludur.",
  "usage-claim-conflict": "İşlem kimliği farklı bir kullanım talebinde kullanılıyor.",
  "stock-item-id-invalid": "Stok ve ürün kimlikleri geçerli olmalıdır.",
  "stock-reference-id-required": "Stok rezervasyonu referans kimliği zorunludur.",
  "stock-operation-id-required": "Stok hareketi işlem kimliği zorunludur.",
  "order-identity-invalid": "Sipariş ve kullanıcı kimlikleri geçerli olmalıdır.",
  "order-placement-identity-invalid":
    "Sipariş oluşturma sürecinin kimlik bilgileri geçerli olmalıdır.",
  "order-line-identity-invalid": "Sipariş satırı kimlik bilgileri geçerli olmalıdır.",
};

const fieldLabels: Record<string, string> = {
  username: "Kullanıcı adı",
  password: "Parola",
  sku: "SKU",
  name: "Ürün adı",
  baseUomCode: "Temel UoM",
  basePriceAmount: "Temel fiyat",
  basePriceCurrencyCode: "Para birimi",
  packagings: "Packaging",
  level: "Level",
  uomCode: "UoM kodu",
  conversionFactor: "Dönüşüm katsayısı",
  barcode: "Barkod",
  weightInKg: "Ağırlık",
  lengthInMm: "Uzunluk",
  widthInMm: "Genişlik",
  heightInMm: "Yükseklik",
  productId: "Ürün",
  openingQuantity: "Açılış miktarı",
  type: "Hareket türü",
  quantity: "Miktar",
  lines: "Sipariş satırları",
  target: "Hedef durum",
};

export function problemCode(type: string | null): string | null {
  if (type === null) {
    return null;
  }
  const slash = type.lastIndexOf("/");
  return type.slice(slash + 1) || null;
}

export function translateProblemTitle(type: string | null, fallback: string): string {
  const code = problemCode(type);
  return code === null ? fallback : (titles[code] ?? fallback);
}

export function translateFailure(code: string | null, detail: string | null): string | null {
  if (code === null) {
    return detail;
  }
  return titles[code] ?? detail ?? "İşlem tamamlanamadı.";
}

export function translateKnownDetail(detail: string | null): string | null {
  if (detail === null) {
    return null;
  }
  const exact: Record<string, string> = {
    "The username or password is invalid.": "Kullanıcı adı veya parola geçerli değil.",
    "A valid bearer token is required to access this resource.":
      "Bu kaynağa erişmek için geçerli bir bearer token gerekiyor.",
    "The authenticated principal is not allowed to access this resource.":
      "Kimliği doğrulanmış kullanıcının bu kaynağa erişim yetkisi yok.",
    "No endpoint or resource matched the request.":
      "İstekle eşleşen endpoint veya kaynak bulunamadı.",
    "The request could not be completed.": "İstek tamamlanamadı.",
    "The request cannot be accepted because its mandatory audit record could not be created.":
      "Zorunlu denetim kaydı oluşturulamadığı için istek kabul edilemiyor.",
  };
  return exact[detail] ?? null;
}

export function translateFieldName(path: string): string {
  const normalized = path
    .replace(/^\$\.?/, "")
    .replaceAll(/\[(\d+)\]/g, ".$1")
    .split(".")
    .filter(Boolean);
  const field = normalized.at(-1) ?? path;
  const camelCaseField = `${field.charAt(0).toLowerCase()}${field.slice(1)}`;
  const label = fieldLabels[camelCaseField] ?? field;
  const index = normalized.findLast((segment) => /^\d+$/.test(segment));
  return index === undefined ? label : `${Number(index) + 1}. satır · ${label}`;
}

export function translateValidationMessage(path: string, message: string): string {
  const label = translateFieldName(path);
  if (/field is required\.?$/i.test(message) || /request body is required\.?$/i.test(message)) {
    return `${label} zorunludur.`;
  }
  if (/could not be converted|is not valid for|input was not valid/i.test(message)) {
    return `${label} beklenen veri türü veya biçimiyle uyuşmuyor.`;
  }
  const lengthLimit = /cannot exceed (\d+) characters/i.exec(message);
  if (lengthLimit !== null) {
    return `${label} en fazla ${Number(lengthLimit[1])} karakter olabilir.`;
  }
  return `${label} için sunucu doğrulaması başarısız oldu.`;
}
