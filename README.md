# Rudoger

Rudoger; ürün, stok ve sipariş yönetimi için geliştirilmiş bir `.NET 10 Web API` uygulamasıdır. `Clean Architecture` kullanan, event sourced bir `modüler monolit` olarak tasarlanmıştır.

## İçindekiler

- [Genel bakış](#genel-bakış)
- [Hızlı başlangıç](#hızlı-başlangıç)
- [API'yi deneme](#apiyi-deneme)
- [API davranışı](#api-davranışı)
- [Mimari](#mimari)
- [Veritabanı](#veritabanı)
- [Doğrulama](#doğrulama)
- [Belgeler](#belgeler)

## Genel bakış

- Beş bounded context: Authn, Product, Inventory, Order ve Logging
- Logging dışındaki dört bounded context'te event sourcing; SQL Server projection'ları aynı transaction içinde güncellenir
- Bounded context'ler arası etkileşim yalnızca process içi dahili API'lar üzerinden
- Outbox worker'larıyla yeniden denemeye dayanıklı sipariş iş akışları
- JWT üzerinden kimlik doğrulaması
- İstisnasız, hassas verileri maskeleyen istek günlüğü
- `RFC 9457` hata yanıtları, `RFC 6902` patch, `X-Correlation-Id` ve `Idempotency-Key` desteği

Alan terimleri [docs/DICTIONARY.md](./docs/DICTIONARY.md), mimari ise diyagramlarıyla birlikte [docs/architecture/ARCHITECTURE.md](./docs/architecture/ARCHITECTURE.md) içinde belgelenmiştir.

> **Not:**
>
> [`client/`](./client) altındaki browser GUI ekstra olarak geliştirilmiştir; bkz. [client/README.md](./client/README.md).

## Hızlı başlangıç

**Gereksinim:** Compose v2 destekli Docker Engine. Host üzerinde .NET SDK bulunması gerekmez.

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

Stack; `mssql`, `migrate`, `seed`, `api` ve `swagger` servislerinden oluşur. Migration'lar ve seed verileri, API başlamadan önce tek seferlik `migrate` ve `seed` servisleri tarafından uygulanır.

- API: [http://localhost:8080](http://localhost:8080)
- Sağlık kapısı: [http://localhost:8080/health](http://localhost:8080/health)
- OpenAPI belgesi: [http://localhost:8080/openapi/v1.json](http://localhost:8080/openapi/v1.json)
- Swagger UI: [http://localhost:8081/docs/](http://localhost:8081/docs/)

`.env.local` Git tarafından yok sayılır; değişkenlerin açıklaması için [docs/DEVELOPMENT.md](./docs/DEVELOPMENT.md#yapılandırma) belgesine bakın.

Stack'i durdurmak için `down`, SQL Server veri volume'ünü de silmek için `down --volumes` kullanın:

Windows PowerShell:

```powershell
.\scripts\compose.ps1 down --volumes
```

Linux/macOS Bash:

```bash
./scripts/compose.sh down --volumes
```

## API'yi deneme

Seed ile oluşturulan kullanıcılar:

| Kullanıcı adı | Şifre      |
| ------------- | ---------- |
| `abdullah`    | `12345678` |
| `murat`       | `12345678` |
| `gokhan`      | `12345678` |

> **Not:**
>
> Bu kullanıcı bilgileri yalnızca assignment/demo ortamları içindir.

Swagger UI stack'in parçasıdır; OpenAPI belgesi ve “Try it out” istekleri kendi origin'i üzerinden `api` servisine reverse-proxy edilir. Her request gövdesi, sırayla izlendiğinde uçtan uca bir akış oluşturan çalışır bir örnekle dolu gelir:

1. `POST /api/v1/authn/tokens` — örnek kimlik bilgileriyle token alın; yanıttaki `accessToken` değerini sağ üstteki **Authorize** düğmesine girin.
2. `POST /api/v1/product/products` — örnek ürünü (`COFFEE-001`; `EA` ve `CASE` packaging'leri) oluşturun; yanıttaki `id` değerini not edin.
3. `POST /api/v1/inventory/stock-items` — `productId` alanına ürün ID'sini yazıp stok açın. `Idempotency-Key` header'ı zorunludur.
4. `POST /api/v1/inventory/stock-items/{stockItemId}/movements` — örnek receipt hareketini uygulayın.
5. `POST /api/v1/order/orders` — sipariş verin; `HTTP 202` yanıtındaki `Location` adresini durum `Succeeded` olana kadar sorgulayın.
6. `POST /api/v1/order/orders/{orderId}/transitions` — `Shipped` geçişini başlatın ve aynı şekilde izleyin.

Örneklerdeki `00000000-…` biçimindeki ID'ler, önceki yanıtlardan kopyalanması gereken yer tutuculardır.

## API davranışı

Token exchange, sağlık ve OpenAPI kapıları dışındaki tüm endpoint'ler `Authorization: Bearer <JWT>` ister. İstemciler `X-Correlation-Id` sağlayabilir; her yanıt etkin değeri geri döndürür. Hatalar `RFC 9457` uyumlu `application/problem+json` biçimindedir ve correlation ID'yi içerir; problem type URI'si her zaman yanıtı veren context'e ait kararlı bir failure code ile biter.

Yeniden denenebilir oluşturma/hareket komutları `Idempotency-Key` ister. Aynı anahtar aynı girdilerle yeniden kullanıldığında ilk sonuç, farklı bir girdiyle kullanıldığında `HTTP 409` döndürülür.

| Metot ve path                                                                    | Amaç                                              |
| -------------------------------------------------------------------------------- | ------------------------------------------------- |
| `POST /api/v1/authn/tokens`                                                      | Kullanıcı adı/parola çiftini JWT ile takas etme   |
| `POST /api/v1/product/products`                                                  | Ürünü level-zero packaging ile birlikte oluşturma |
| `GET /api/v1/product/products?ids=...`                                           | Ürünleri sayfalama/filtreleme                     |
| `GET/PATCH/DELETE /api/v1/product/products/{productId}`                          | Ürün kaynağı işlemleri                            |
| `GET/POST /api/v1/product/products/{productId}/packagings`                       | Packaging kayıtlarını listeleme/ekleme            |
| `GET/PATCH/DELETE /api/v1/product/products/{productId}/packagings/{packagingId}` | Packaging kaynağı işlemleri                       |
| `POST /api/v1/inventory/stock-items`                                             | Tek depolu stock item kaydını açma                |
| `GET /api/v1/inventory/stock-items`                                              | Stock item kayıtlarını sayfalama/filtreleme       |
| `GET /api/v1/inventory/stock-items/{stockItemId}`                                | On-hand, reserved ve available miktarlarını okuma |
| `POST /api/v1/inventory/stock-items/{stockItemId}/movements`                     | Receipt, adjustment veya deduction uygulama       |
| `GET /api/v1/inventory/stock-items/{stockItemId}/movements`                      | Hareket defterini sayfalama                       |
| `POST /api/v1/order/orders`                                                      | Sipariş oluşturma sürecini başlatma               |
| `GET /api/v1/order/order-placements/{placementId}`                               | Sipariş oluşturma sürecinin durumunu izleme       |
| `GET /api/v1/order/orders`                                                       | Tüm siparişleri sayfalama                         |
| `GET /api/v1/order/orders/{orderId}`                                             | Bir siparişi okuma                                |
| `POST /api/v1/order/orders/{orderId}/transitions`                                | Sevkiyat veya iptal geçişini başlatma             |
| `GET /api/v1/order/orders/{orderId}/transitions/{transitionId}`                  | Durum geçişi sürecini izleme                      |

- **Patch:** Ürün ve packaging patch endpoint'leri yalnızca değiştirilebilir alanlardaki `replace` ve `test` işlemlerini içeren `RFC 6902` belgelerini kabul eder. Base UoM ve level-zero packaging'in yapısal kimliği değiştirilemez.
- **Eşzamansız süreçler:** Sipariş oluşturma ve durum geçişleri `Location` header'ıyla `HTTP 202` döndürür; süreç kaynağı `Succeeded` veya `Failed` olana kadar sorgulanır.
- **UoM ve fiyat:** Stok açılışı, receipt, adjustment, deduction ve sipariş satırları ürünün tanımlı herhangi bir UoM kodunu kabul eder; bakiyeler base UoM cinsinden tutulur, hareket defteri girilen UoM ve miktarı da korur. Birim fiyat sunucu tarafında snapshot olarak alınır: temel fiyat × seçilen packaging'in conversion factor'ü.
- **Stok yaşam döngüsü:** Başarılı sipariş oluşturma stoku reserve eder; sevkiyat commit, iptal ise release eder.

## Mimari

```text
Modules/{Authn,Product,Inventory,Order,Logging}/
  Domain/
  Application/
  Infrastructure/
  Presentation/
Host/Rudoger.Api/
BuildingBlocks/{Domain,Application,Infrastructure,Presentation}/
Tests/{Unit,Architecture,Integration,EndToEnd}/
Tools/DatabaseDump/
```

Her bounded context dört ayrı projeden oluşur; bağımlılıklar iç katmanlara yönelir. Temel kararlar:

- Context'ler arası çağrılara yalnızca tüketici Infrastructure projesinden sağlayıcı Presentation contract'ına, HTTP kullanmadan izin verilir; mimari testler bu kuralı uygular ([ADR 0001](./docs/architecture/0001-modular-monolith-and-internal-apis.md)).
- Authn, Product, Inventory ve Order jenerik JSON event envelope'ları saklar, projection'ları aynı transaction'da günceller ve yarışları unique `(StreamId, Version)` kısıtıyla çözer; Order ve Inventory outbox worker'ları iş akışlarını en az bir kez yürütür ([ADR 0002](./docs/architecture/0002-event-sourcing-and-workflows.md)).
- Beş context tek veritabanını paylaşır ancak ayrı şemalara sahiptir ve şemalar arası foreign key kullanmaz ([ADR 0003](./docs/architecture/0003-database-boundaries.md)).
- Idempotency, korelasyon, problem details ve zorunlu denetim günlüğü tasarımın parçasıdır ([ADR 0004](./docs/architecture/0004-api-reliability-and-audit.md)).
- HTTP sözleşmesi Presentation katmanının kendi record ve enum'larından oluşur; context'ler arası gateway'ler birer anti-corruption layer'dır ve failure code'larını tüketicinin dağarcığına çevirir. Yayınlanan sözleşmenin iç modeli sızdırmadığı unit ve uçtan uca testlerle doğrulanır.

Sistem bağlamı, konteyner, bileşen, deployment ve kritik akışların sequence diyagramları için [docs/architecture/ARCHITECTURE.md](./docs/architecture/ARCHITECTURE.md) belgesine bakın; belgenin sonundaki “Birincil uygulama kanıtları” bölümü her kararı gerçekleyen kaynak dosyaları listeler.

## Veritabanı

Şema değişiklikleri ve seed verileri sunucu süreci tarafından örtük olarak uygulanmaz. Her bounded context kendi `DbContext` ve migration history tablosuna sahiptir; `migrate` tüm context'lerin migration'larını tek adımda uygular, `seed` eksik varsayılan kullanıcıları oluşturur. İki komut da idempotent'tır.

Windows PowerShell:

```powershell
.\scripts\migrate.ps1
.\scripts\seed.ps1
```

Linux/macOS Bash:

```bash
./scripts/migrate.sh
./scripts/seed.sh
```

Host için bağlantı dizesi üretme, metin dump alma ve geri yükleme adımları [docs/DATABASE.md](./docs/DATABASE.md) içindedir.

## Doğrulama

Dört test katmanı vardır: unit ([Tests/Unit](./Tests/Unit)), mimari ([Tests/Architecture](./Tests/Architecture)), integration ([Tests/Integration](./Tests/Integration)) ve uçtan uca ([Tests/EndToEnd](./Tests/EndToEnd)). Integration ve uçtan uca testler gerçek bir SQL Server örneğini Testcontainers ile başlattığından Docker gerektirir.

```sh
dotnet tool restore
dotnet build Rudoger.slnx
dotnet format Rudoger.slnx --verify-no-changes --no-restore
dotnet test Rudoger.slnx --no-build
```

Unit test coverage kapısı tüm Domain assembly'leri için %95 line coverage ister; güncel Release ölçümü %98,9'dur. Coverage raporunu üreten tam tarif, test katmanlarının kapsamı ve CI adımları [docs/DEVELOPMENT.md](./docs/DEVELOPMENT.md#doğrulama) belgesindedir. [CI](./.github/workflows/ci.yml); build, format, unit test coverage, mimari, integration, uçtan uca, production konteyner imajı ve Compose stack'i kontrollerini her push'ta tekrarlar.

## Belgeler

| Belge                                                                    | İçerik                                                                |
| ------------------------------------------------------------------------ | --------------------------------------------------------------------- |
| [docs/DICTIONARY.md](./docs/DICTIONARY.md)                               | Ubiquitous language sözlüğü; bounded context başına alan terimleri    |
| [docs/architecture/ARCHITECTURE.md](./docs/architecture/ARCHITECTURE.md) | C4 tarzı Mermaid diyagramları, kritik akışların sequence diyagramları |
| [docs/architecture/](./docs/architecture)                                | Mimari karar kayıtları (ADR 0001–0004)                                |
| [docs/DATABASE.md](./docs/DATABASE.md)                                   | Migration/seed, host bağlantı dizesi, dump ve geri yükleme            |
| [docs/DEVELOPMENT.md](./docs/DEVELOPMENT.md)                             | Yapılandırma, host SDK ile çalıştırma, tam doğrulama tarifi, CI       |
| [client/README.md](./client/README.md)                                   | Ekstra geliştirilen browser GUI                                       |
