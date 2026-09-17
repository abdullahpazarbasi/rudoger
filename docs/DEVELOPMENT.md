# Geliştirme Rehberi

Bu belge; yapılandırma önceliğini, API'yi Compose stack'i dışında çalıştırma yollarını ve CI ile aynı doğrulama tarifini anlatır. Stack'i yalnızca ayağa kaldırmak için [README.md › Hızlı başlangıç](../README.md#hızlı-başlangıç) yeterlidir. Veritabanı migration, seed, dump ve geri yükleme adımları [DATABASE.md](./DATABASE.md) içindedir.

## Yapılandırma

Yapılandırma önceliği `appsettings.json` → [.env](../.env) → [.env.local](../.env.local) → süreç ortamı → komut satırı şeklindedir. `.env.local` Git tarafından yok sayılır; [.env.dist](../.env.dist) her zorunlu değişken için bir yer tutucu içerir.

Zorunlu secret'lar:

- `ConnectionStrings__Rudoger`: Stack dışında çalıştırıldığında kullanılacak SQL Server bağlantı dizesi.
- `MSSQL_SA_PASSWORD`: SQL Server konteyner parolası.
- `Jwt__Secret`: En az 32 karakter.

İsteğe bağlı değişkenler:

- `API_PORT` (varsayılan `8080`), `MSSQL_PORT` (varsayılan `1433`) ve `SWAGGER_PORT` (varsayılan `8081`): Host'a yayınlanan portlar.
- `Jwt__Issuer` (varsayılan `rudoger-api`), `Jwt__Audience` (varsayılan `rudoger-clients`) ve `Jwt__LifetimeMinutes` (varsayılan `60`).

Compose, SQL Server'a kendi ağında her zaman `mssql:1433` üzerinden bağlanır; host araçları için `db-connection-string` betiğini kullanın (bkz. [DATABASE.md › Host için bağlantı dizesi](./DATABASE.md#host-için-bağlantı-dizesi)).

## Host SDK ile çalıştırma

Host SDK tabanlı geliştirme için .NET SDK 10.0.112'yi veya [global.json](../global.json) içinde sabitlenmiş uyumlu sürümü kullanın. SQL Server için Compose stack'indeki `mssql` servisi yeterlidir.

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

## SDK imajı ile çalıştırma

Host'a .NET SDK kurmadan `dotnet` komutlarını çalıştırmak için `scripts/dotnet.ps1` ve `scripts/dotnet.sh`, eşdeğer komutları resmî SDK imajında (`mcr.microsoft.com/dotnet/sdk:10.0`) depo kökü bağlı olarak çalıştırır:

Windows PowerShell:

```powershell
.\scripts\dotnet.ps1 build Rudoger.slnx
```

Linux/macOS Bash:

```bash
./scripts/dotnet.sh build Rudoger.slnx
```

## Doğrulama

Aşağıdaki tarif CI ile aynı kapıları uygular. Integration ve uçtan uca testler gerçek bir SQL Server örneğini Testcontainers ile başlattığından Docker gerektirir.

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

### Coverage kapısı

Unit test coverage kapısı tüm Domain assembly'leri için %95 line coverage ister. [coverage.runsettings](../coverage.runsettings) yalnızca `Rudoger.*.Domain` assembly'lerini ölçer ve migration dosyalarını dışarıda bırakır. Eşik, [dotnet-tools.json](../dotnet-tools.json) ile sabitlenen [ReportGenerator](https://github.com/danielpalme/ReportGenerator) yerel aracının `minimumCoverageThresholds` ayarıyla uygulanır; özet rapor `TestResults/UnitCoverageReport/Summary.txt` dosyasına yazılır.

`TestResults/UnitCoverage` altında birden fazla çalıştırmanın çıktısı birikirse ReportGenerator bunları birleştirir (`Parser: MultiReport`). Tek bir çalıştırmanın temiz ölçümü için dizini önceden boşaltın.

### Test katmanları

- **Unit** ([Tests/Unit](../Tests/Unit)): Aggregate'ler, guard'lar, event type registry, sayfalama, hassas veri maskeleme, dahili çağrı günlükleme, gateway failure çevirileri, yayınlanan sözleşmenin iç modeli sızdırmadığı (`PublishedContractTests`) ve database dump aracı.
- **Architecture** ([Tests/Architecture](../Tests/Architecture)): Katman yönü ve bounded context'ler arası bağımlılık kuralları (`ModuleDependencyTests`).
- **Integration** ([Tests/Integration](../Tests/Integration)): Sağlık kapısının veritabanı bağlantısını raporlaması, migration'ların yalıtık şemaları ve seed kullanıcılarını oluşturması ve event store'un yarış/projeksiyon çakışma semantiği; gerçek bir Testcontainers SQL Server örneğiyle çalışır.
- **End-to-end** ([Tests/EndToEnd](../Tests/EndToEnd)): Çalışan API üzerinden JWT, middleware, dahili API, outbox worker ve Product → Inventory → Order akışının tamamı (`ProductInventoryOrderJourneyTests`), yayınlanan hata dağarcığı (`PublishedVocabularyTests`) ve OpenAPI belgesinin bearer şeması ile request örnekleri (`OpenApiDocumentTests`).

## CI

[GitHub Actions iş akışı](../.github/workflows/ci.yml) her push ve pull request'te sırasıyla şunları çalıştırır:

1. Araç sözleşmeleri: `bash -n` ile betik sözdizimi, `db-connection-string` betiklerinin beklenen çıktıyı ürettiği, `dump-database` betiklerinin `--help` çıktısı, PowerShell betiklerinin ayrıştırılabildiği ve `docker compose config` doğrulaması.
2. Restore, yerel araç restore, Release build ve `dotnet format --verify-no-changes`.
3. Unit testler, coverage toplama ve %95 eşik kontrolü.
4. Architecture, integration ve uçtan uca testler.
5. Production konteyner imajı build'i.
6. Compose stack'ini ayağa kaldırıp Swagger UI'ın ve reverse-proxy edilen OpenAPI belgesinin sunulduğunu doğrulama.
