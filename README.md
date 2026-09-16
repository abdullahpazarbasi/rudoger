# Rudoger

Rudoger; ürün, stok ve sipariş yönetimi için geliştirilmiş bir .NET 10 Web API uygulamasıdır. Clean Architecture kullanan modüler monolit olarak tasarlanmıştır. Beş bounded context, event-sourced iş modülleri, SQL Server projection'ları, process içi dahili API'ler, yeniden denemeye dayanıklı iş akışları, JWT kimlik doğrulaması ve zorunlu istek günlüğü içerir.

## Hızlı başlangıç

Gereksinim: Compose v2 destekli Docker Engine. Host üzerinde .NET SDK bulunması gerekmez.

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

API [http://localhost:8080](http://localhost:8080), sağlık kapısı [http://localhost:8080/health](http://localhost:8080/health), OpenAPI belgesi [http://localhost:8080/openapi/v1.json](http://localhost:8080/openapi/v1.json), Swagger UI ise [http://localhost:8081/docs/](http://localhost:8081/docs/) adresinden sunulur. Migration'lar ve seed verileri API başlamadan önce açıkça tanımlanmış tek seferlik servisler tarafından uygulanır.

Seed kullanıcı bilgileri:

| Kullanıcı adı | Şifre      |
| ------------- | ---------- |
| `abdullah`    | `12345678` |
| `murat`       | `12345678` |
| `gokhan`      | `12345678` |

Bu kullanıcı bilgilerini yalnızca assignment/demo ortamlarında kullanın.

Stack'i durdurmak için:

Windows PowerShell:

```powershell
.\scripts\compose.ps1 down
```

Linux/macOS Bash:

```bash
./scripts/compose.sh down
```

SQL Server veri volume'ünün de silinmesi isteniyorsa yukarıdaki komuta `--volumes` ekleyin:

Windows PowerShell:

```powershell
.\scripts\compose.ps1 down --volumes
```

Linux/macOS Bash:

```bash
./scripts/compose.sh down --volumes
```

### OpenAPI belgesini GUI ile açma

Swagger UI, ana Compose stack'inin `swagger` servisidir; ayrıca belge indirmeniz veya ayrı bir konteyner başlatmanız gerekmez. Tarayıcıda [http://localhost:8081/docs/](http://localhost:8081/docs/) adresini açın.

UI, OpenAPI belgesini ve “Try it out” isteklerini kendi origin'i üzerinden reverse-proxy eder. Tarayıcı `localhost:8081` ile konuşurken Nginx, Compose ağı içinde `api:8080` adresine ulaşır.

### Veritabanı bağlantı dizesi

Host üzerinden SQL Server'a bağlanmaya uygun bağlantı dizesini yazdırmak için:

Windows PowerShell:

```powershell
.\scripts\db-connection-string.ps1
```

Linux/macOS Bash:

```bash
./scripts/db-connection-string.sh
```

Betik, yapılandırma önceliğine göre `.env` ve `.env.local` dosyalarını ve süreç ortamını okur; Compose içi sunucu adını host için `localhost` ve `MSSQL_PORT` değerine dönüştürür. Çıktı parolayı içerdiğinden log veya issue içeriğine eklemeyin.

## Veritabanı yaşam döngüsü

Şema değişiklikleri ve seed verileri sunucu süreci tarafından örtük olarak uygulanmaz.

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

İki komut da idempotent'tır. Seed komutu eksik varsayılan kullanıcıları oluşturur, mevcut kullanıcıların kimlik bilgilerini değiştirmez.

Host SDK tabanlı geliştirme için .NET SDK 10.0.112'yi veya `global.json` içinde sabitlenmiş uyumlu sürümü kullanın:

Windows PowerShell:

```powershell
$env:ConnectionStrings__Rudoger = .\scripts\db-connection-string.ps1
dotnet restore Rudoger.slnx
dotnet run --project Host/Rudoger.Api/Rudoger.Api.csproj -- migrate
dotnet run --project Host/Rudoger.Api/Rudoger.Api.csproj -- seed
dotnet run --project Host/Rudoger.Api/Rudoger.Api.csproj -- serve
```

Linux/macOS Bash:

```bash
export ConnectionStrings__Rudoger="$(./scripts/db-connection-string.sh)"
dotnet restore Rudoger.slnx
dotnet run --project Host/Rudoger.Api/Rudoger.Api.csproj -- migrate
dotnet run --project Host/Rudoger.Api/Rudoger.Api.csproj -- seed
dotnet run --project Host/Rudoger.Api/Rudoger.Api.csproj -- serve
```

`scripts/dotnet.ps1` ve `scripts/dotnet.sh`, eşdeğer komutları resmî SDK imajında çalıştırır:

Windows PowerShell:

```powershell
.\scripts\dotnet.ps1 build Rudoger.slnx
```

Linux/macOS Bash:

```bash
./scripts/dotnet.sh build Rudoger.slnx
```

## Yapılandırma

Yapılandırma önceliği `appsettings.json` → `.env` → `.env.local` → süreç ortamı → komut satırı şeklindedir. `.env.local` Git tarafından yok sayılır.

Zorunlu secret'lar:

- `ConnectionStrings__Rudoger`: Compose dışında çalıştırıldığında kullanılacak SQL Server bağlantı dizesi.
- `MSSQL_SA_PASSWORD`: SQL Server konteyner parolası.
- `Jwt__Secret`: En az 32 karakter.

İsteğe bağlı port değişkenleri `API_PORT`, `MSSQL_PORT` ve `SWAGGER_PORT`'tur. Compose, SQL Server'a kendi ağında her zaman `mssql:1433` üzerinden bağlanır; host araçları için `db-connection-string` betiğini kullanın.

## API davranışı

Token exchange, sağlık ve OpenAPI kapıları dışındaki tüm endpoint'ler `Authorization: Bearer <JWT>` ister. İstemciler `X-Correlation-Id` sağlayabilir; her yanıt etkin değeri geri döndürür. Hatalar, RFC 9457 uyumlu `application/problem+json` biçimini kullanır ve correlation ID'yi içerir.

Yeniden denenebilir oluşturma/hareket komutları `Idempotency-Key` ister. Aynı anahtar aynı girdilerle yeniden kullanıldığında ilk sonuç döndürülür. Farklı bir girdiyle kullanıldığında HTTP 409 döndürülür.

### Ana kaynaklar

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

Ürün ve packaging patch endpoint'leri yalnızca belgelenmiş değiştirilebilir alanlardaki `replace` ve `test` işlemlerini içeren RFC 6902 belgelerini kabul eder. Base UoM ve level-zero packaging'in yapısal kimliği değiştirilemez.

Sipariş oluşturma ve durum geçişleri, `Location` header'ıyla birlikte HTTP 202 döndürür. İlgili süreç kaynağını `Succeeded` veya `Failed` durumuna gelene kadar sorgulayın. Birim fiyatlar sunucu tarafında snapshot olarak alınır: temel fiyat, seçilen packaging'in conversion factor değeriyle çarpılır. Stok her zaman base UoM cinsinden tutulur. Başarılı sipariş oluşturma stoku reserve eder; sevkiyat commit, iptal ise release eder.

Çalıştırılabilir bir istek akışı [`Rudoger.http`](Rudoger.http) içinde bulunur.

## Mimari

Her bounded context dört projeden oluşur:

```text
Modules/{Authn,Product,Inventory,Order,Logging}/
  Domain/
  Application/
  Infrastructure/
  Presentation/
Host/Rudoger.Api/
BuildingBlocks/{Domain,Application,Infrastructure,Presentation}/
```

Bağımlılıklar iç katmanlara yönelir. Context'ler arası çağrılara yalnızca tüketici Infrastructure projesinden sağlayıcı Presentation contract'ına doğru izin verilir. Mimari testleri bu kuralı uygular.

Authn, Product, Inventory ve Order; jenerik JSON event envelope'larını saklar ve read projection'larını aynı yerel transaction içinde günceller. Her biri kendi `Events` tablosuna sahiptir ve yarış durumlarını unique `(StreamId, Version)` kısıtıyla çözer. Logging, sıradan ve yalnızca sona ekleme yapılan operasyonel bir tablodur. Beş context tek veritabanını paylaşır ancak ayrı şemalara sahiptir ve şemalar arası foreign key kullanmaz.

Order ve Inventory outbox worker'ları, iş akışlarını en az bir kez yürütür. Unique idempotency/source event ID'leri yeniden oynatmayı güvenli kılar. Karar kayıtları için [`docs/architecture`](docs/architecture) dizinine bakın.

## Doğrulama

Windows PowerShell:

```powershell
dotnet tool restore
dotnet build Rudoger.slnx --no-restore
dotnet format Rudoger.slnx --verify-no-changes --no-restore
dotnet test Tests/Unit/Rudoger.UnitTests.csproj `
  --settings coverage.runsettings `
  --collect "XPlat Code Coverage" `
  --results-directory TestResults/UnitCoverage
dotnet reportgenerator `
  "-reports:TestResults/UnitCoverage/**/coverage.cobertura.xml" `
  -targetdir:TestResults/UnitCoverageReport `
  -reporttypes:TextSummary `
  minimumCoverageThresholds:lineCoverage=95
dotnet test Tests/Architecture/Rudoger.ArchitectureTests.csproj
dotnet test Tests/Integration/Rudoger.IntegrationTests.csproj
dotnet test Tests/EndToEnd/Rudoger.EndToEndTests.csproj
```

Linux/macOS Bash:

```bash
dotnet tool restore
dotnet build Rudoger.slnx --no-restore
dotnet format Rudoger.slnx --verify-no-changes --no-restore
dotnet test Tests/Unit/Rudoger.UnitTests.csproj \
  --settings coverage.runsettings \
  --collect "XPlat Code Coverage" \
  --results-directory TestResults/UnitCoverage
dotnet reportgenerator \
  "-reports:TestResults/UnitCoverage/**/coverage.cobertura.xml" \
  -targetdir:TestResults/UnitCoverageReport \
  -reporttypes:TextSummary \
  minimumCoverageThresholds:lineCoverage=95
dotnet test Tests/Architecture/Rudoger.ArchitectureTests.csproj
dotnet test Tests/Integration/Rudoger.IntegrationTests.csproj
dotnet test Tests/EndToEnd/Rudoger.EndToEndTests.csproj
```

Unit test coverage kapısı tüm Domain assembly'leri için %95 line coverage ister; eşik, `dotnet-tools.json` ile sabitlenen [ReportGenerator](https://github.com/danielpalme/ReportGenerator) yerel aracının `minimumCoverageThresholds` ayarıyla uygulanır ve özet raporu `TestResults/UnitCoverageReport/Summary.txt` dosyasına yazılır. Güncel Release ölçümü %99,1'dir. Application, kalıcılık, migration, JWT, middleware, dahili API, outbox worker ve Product → Inventory → Order akışının tamamı gerçek bir Testcontainers SQL Server örneğiyle sınanır. Integration ve uçtan uca testler Docker gerektirir.

CI; build, format, unit test coverage, mimari, integration, uçtan uca ve production konteyner imajı kontrollerini tekrarlar.
