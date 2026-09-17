# Rudoger Client

Rudoger Web API için geliştirilmiş Türkçe, yalın ve erişilebilir browser GUI'dir. Ürün, packaging, stok ve sipariş işlemlerini API'nin mevcut REST/OpenAPI sözleşmesi üzerinden sunar; desteklenmeyen sorguları tüm veriyi çekip browser'da taklit etmez.

## Gereksinimler

- Node.js `>=24.21.0 <25`
- npm
- Yerelde `http://localhost:8080` adresinde çalışan Rudoger API

## Kurulum ve çalıştırma

Önce repository kökünde API stack'ini başlatın:

Windows PowerShell:

```powershell
Copy-Item .env.dist .env.local
.\scripts\compose.ps1 up --build
```

Linux/macOS Bash:

```bash
cp .env.dist .env.local
./scripts/compose.sh up --build
```

Ardından client dizininde:

Windows PowerShell:

```powershell
Set-Location client
Copy-Item .env.dist .env
npm ci
npm run dev
```

Linux/macOS Bash:

```bash
cd client
cp .env.dist .env
npm ci
npm run dev
```

Development server; `/api`, `/health` ve `/openapi` isteklerini aynı origin üzerinden `http://localhost:8080` adresine proxy eder. API'de CORS bulunmadığı için varsayılan geliştirme bağlantısı bu kanonik proxy yoludur.

## Uygulama kapsamı

| Rota                   | İşlemler                                                                 |
| ---------------------- | ------------------------------------------------------------------------ |
| `/giris`               | Username/password ile token exchange                                     |
| `/urunler`             | Listeleme, ID ile filtreleme ve Product oluşturma                        |
| `/urunler/:productId`  | Product görüntüleme, RFC 6902 patch, silme ve Product Packaging yönetimi |
| `/stok`                | Stock Item listeleme ve seçilen Product Packaging UoM ile stok açma      |
| `/stok/:stockItemId`   | Bakiye, Receipt/Adjustment/Deduction ve movement geçmişi                 |
| `/siparisler`          | Order listeleme ve çok satırlı Order oluşturma                           |
| `/siparisler/:orderId` | Order ayrıntısı ile Shipped/Cancelled transition işlemleri               |

Stok açma ve manual movement formları Product Packaging kayıtlarını UoM seçeneklerinin kanonik kaynağı olarak kullanır. Client, girilen miktarı ve UoM değerini API'ye gönderir; Base UoM dönüşümünün nihai otoritesi API'dir.

## Mimari kararlar

- OpenAPI snapshot'ı `openapi/rudoger-v1.json`, üretilen TypeScript sözleşmesi `src/api/generated/` altında tutulur.
- Tüm HTTP çağrıları merkezî `openapi-fetch` client üzerinden geçer.
- Server state, polling ve mutation invalidation için React Query tek kaynaktır; API verisi paralel component state'lerine kopyalanmaz.
- Auth durumu typed `anonymous | authenticated | expired` state machine ile yönetilir. JWT yalnızca memory ve `sessionStorage` içinde tutulur.
- JWT süresi dolunca mevcut rota korunur; otomatik yönlendirme yapılmaz. Korumalı gönderimler durdurulur ve kullanıcıya “Yeniden Giriş Yap” eylemi sunulur.
- API sağlıksızsa health gate kapanmaz; yalnızca başarılı manuel retry sonrasında uygulama tekrar açılır.
- RFC 9457 alanları ve correlation ID kaybedilmeden, kullanıcıya dönük açıklamalar Türkçeleştirilir.
- Tema `system | light | dark` seçeneklerini destekler ve sistem tercihini reaktif izler.

## OpenAPI sözleşmesi

Çalışan API'den snapshot ve üretilen tipleri güncellemek için:

```sh
npm run api:sync
```

Repository'deki snapshot ile generated tiplerin uyumunu ağ bağlantısı olmadan doğrulamak için:

```sh
npm run api:check
```

`OPENAPI_URL` varsayılan olarak `http://localhost:8080/openapi/v1.json` değerindedir. Browser çağrılarında `VITE_API_BASE_URL` boş bırakıldığında aynı-origin proxy kullanılır.

## Kalite ve doğrulama

Tüm statik analiz, test, coverage ve production build kapılarını çalıştırmak için:

```sh
npm run check
```

Bu komut sırasıyla OpenAPI uyumunu, TypeScript typecheck'i, ESLint'i, Prettier kontrolünü, coverage testlerini ve production build'i çalıştırır. Client genelinde line ve branch coverage eşiği en az %90'dır; auth expiry, health gate ve hata dönüştürme modüllerinde branch coverage %100'dür.

Çalışan gerçek API ile uçtan uca smoke akışı için:

```sh
npm run smoke:api
```

Gerekirse hedef API `SMOKE_API_BASE_URL` ortam değişkeniyle değiştirilebilir.
