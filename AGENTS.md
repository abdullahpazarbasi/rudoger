# Project Guidelines

## Amaç

İş başvurum için verilen assignment'da istenen projeyi geliştirmek.

## Proje Tanımı

Ürün, stok ve sipariş yönetimi yapılabilen basit bir Web API uygulaması

## Proje Ekleri

- [**DICTIONARY.md**](./DICTIONARY.md) : Ubiquitous language için güncel tutulması gereken sözlüktür.
- [**docs/architecture/ARCHITECTURE.md**](./docs/architecture/ARCHITECTURE.md) : Proje mimarisini diyagramlarla açıklar.
- [**README.md**](./README.md) : Projeyi değerlendirecek otorite için güncel tutulması gereken yol göstericidir.

## Uygulama Özellikleri

### Ürün Yönetimi

- Domain: product
- Listelemede ID listesi ile süzme mümkün

Ürünü;

- Tekil ekleme (`POST`)
- Listeleme (`GET`)
- Tekil görüntüleme (`GET`)
- Tekil yamama (`PATCH`)
- Tekil silme (`DELETE`)

**Önemli Kural:** Ürün ile level 0 ürün paketleme bir girilir
**Önemli Kural:** Stoku ve/veya herhangi statüde siparişi bulunan ürün silinemez (409)

#### Temel ürün yönetimi modelleri

Product:

- Id
- SKU
- Name
- BaseUomCode
- BasePriceAmount
- BasePriceCurrencyCode

Product Packaging (child model):

- Id
- ProductId
- Level (level = 0 for base UoM code)
- UomCode
- ConversionFactor (base UoM koduna bağıl)
- Barcode
- WeightInKg
- LengthInMm
- WidthInMm
- HeightInMm

### Stok Yönetimi

- Domain: inventory
- Ürün bazında stok girişi
- Stok miktarı sorgulama
- Stok miktarı receipt/adjustment/deduction
- `RESERVED` | `COMMITTED` | `RELEASED`

**Önemli Varsayım:** Stoklamada bir kapasite sınırı bulunmadığı varsayıldı
**Önemli Varsayım:** Tekil warehouse varsayıldı

#### Temel stok yönetimi modelleri

StockItem:

- Id
- ProductId
- BaseUomCode
- OnHandQuantity
- ReservedQuantity
- AvailableQuantity (calculated: OnHandQuantity - ReservedQuantity)

StockMovement:

- Id
- StockItemId
- Type
- OnHandQuantityDelta
- ReservedQuantityDelta
- ReferenceType
- ReferenceId
- IdempotencyKey
- CorrelationId
- SourceEventId

| Type         | OnHand delta | Reserved delta |
| :----------- | -----------: | -------------: |
| `RECEIPT`    |  `+quantity` |            `0` |
| `ADJUSTMENT` |  `±quantity` |            `0` |
| `DEDUCTION`  |  `-quantity` |            `0` |
| `RESERVED`   |          `0` |    `+quantity` |
| `COMMITTED`  |  `-quantity` |    `-quantity` |
| `RELEASED`   |          `0` |    `-quantity` |

### Sipariş Yönetimi

- Domain: order
- Yeni sipariş oluşturma (çoklu ürün)
- Yeni siparişte ilgili ürünlerin stok yeterliliğini check etmek önemli
- Sipariş görüntüleme
- Sipariş listeleme
- Sipariş durumu ilerletme

**Önemli Varsayım:** Sipariş `PLACED` durumuna geçtiğinde ödemenin alınmış olduğu varsayılır
**Önemli Kural:** Sipariş durumu `SHIPPED` öncesinde ilgili stok rezervasyona tabidir. Sipariş durumu `SHIPPED` yapılırken ilgili stok da `COMMITTED` yapılmalıdır; `CANCELLED` yapılırken de ilgili stok `RELEASED` yapılmalıdır
**Önemli Kural:** Bir siparişte farklı para birimleri kabul edilmez

#### Temel sipariş yönetimi modelleri

Order:

- Id
- OrderNumber
- Status (`PLACED` | `SHIPPED` | `CANCELLED`)
- UserId (JWT:sub)

OrderLine (child model):

- Id
- OrderId
- Num (line number)
- ProductId
- UomCode
- Quantity
- UnitPriceAmount
- UnitPriceCurrencyCode

### Kullanıcı Kimliklendirme

- Domain: authn
- Exchange/login kapısı (kullanıcı adı ve şifre çifti beklenir, JWT döndürülür)
- JWT kullanılır
- Refresh Token biliçli olarak kapsam dışı
- Varsayılan kullanıcılar ve şifreleri (seed ile oluşturulur):
  - `abdullah`:`12345678`
  - `murat`:`12345678`
  - `gokhan`:`12345678`

### API İstek Kaydı

- API talepleri istisnasız günlüklenir
- CQS burada geçerli değil. Basit bir log tablosu kullanılır
- password ve JWT özel olarak maskelenecek

## Teknik Gereksinimler

- .NET 10 LTS Web API kullanılır
- C# 14 kullanılır
- RDBMS olarak MSSQL kullanılır
- API end-point'leri RESTful tasarlanır
- Containerization by Docker
- OpenAPI
- `.editorconfig`

## Değerlendirme Kriterleri

- DB migration ve seed script'leri sunulacak
- Proje yapısındaki gelişmişliğe dikkat edilecek
- Kod okunabilirliğine bakılacak
- Doğrulamalara dikkat edilecek
- Hata yönetimine dikkat edilecek
- Model ve DB tasarımına dikkat edilecek
- Genel kod kalitesine dikkat edilecek

## Takip Edilmesi Şart Olan Paradigma, Yaklaşım, Kural ve İlkeler

- SPoT
- Separation of Concerns
- DDD
- Workaround'lardan ve hack'lerden uzak durulacak. Varsayılan olarak best-practice'ler takip edilecek
- authn, product, inventory, order ve logging ayrı BC'lerdir
- BC'ler (Bounded Context'ler) arası etkileşimler yalnızca dahili API etkileşimleri üzerinden mümkün olur
- X-Correlation-Id takibi (internal API taleplerinde de propagate edilecek ve hata raporlarında da bulunacak)
- Entity ID tipi varsayılan olarak GUID v7
- CQS/CQRS (özgür)
- İş BC'lerinde Event Sourcing (projection in the same transaction)
- Her iş BC'sinin kendi event store'u jenerik olacak (CLR tipi değil) ve snapshot ile upcasting kapsam dışı
- Event envelope alanları: EventId, StreamId, AggregateType, Version, EventType, SchemaVersion, Payload, Metadata, OccurredAtUtc
- Event unique constraint: (StreamId, Version)
- OOP
- Her class/record/enum/interface ayrı dosyada
- Domain unit test line coverage'ı %95+
- Clean Architecture (katmanlar dizinlere kuralı da uygulanacak)
- Modül başına ayrı csproj'lar: `Modules/{Authn,Product,Inventory,Order,Logging}/{Domain,Application,Infrastructure,Presentation}` + `Host/Rudoger.Api`
- Katmanlar arası sızma asla kabul edilemez (özel test çalıştırılır)
- Tek veritabanı ama BC başına ayrı veritabanı şeması ve şemalar arası FK yok (authn, product, inventory, order, logging)
- Patch için `RFC 6902` benimsenir
- API end-point resource path'leri `/api/v1/{domain}/{collection}/{item}/{collection}/{item}` naming convention'ına göre belirlenir
- Race condition'da event version esas alınacak
- GitHub Actions ile CI
- Hata raporlama standardı `RFC 9457`
- Docker container'ları: `mssql` + `migrate` + `seed` + `api` + `swagger`
- DotEnv: `.env` + `.env.local` + `.env.dist`

## Internal API ne demek?

Bir istemci BC'nin infrastructure'ındaki bir gateway istemcisinden bir sunucu BC'nin presentation'ındaki bir gateway sunucusuna araya HTTP sokmadan istek verip cevap almak tarzında mekanizma. Normal RESTful API'dan farkı HTTP iletişimi olmaması

## Geliştirme Kuralları

- Stage'e almak ve commit'lemek yasak
- Senin (agent) kullanıcın ile iletişimin Türkçe olmalı. `README.md` ve `docs/` altındaki belgeler de Türkçe yazılır (kod tanımlayıcıları, yollar ve uç nokta adları İngilizce kalır). Diğer her şey (kod, yorumlar, commit mesajları, testler, yapılandırma) İngilizce
- **Definition of Done:**
  - Her davranış değişikliği uygun `automated test` ile kapsanır
  - Mimari, sızmalara karşı teftiş ve tamir edilir
  - `Line coverage` hedefi tutturulmalıdır
  - `dotnet format` temiz çıkmalıdır
