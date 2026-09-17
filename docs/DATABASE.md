# Veritabanı İşlemleri

Rudoger tek bir Microsoft SQL Server veritabanı kullanır; her bounded context kendi şemasına ve kendi EF Core migration geçmişine sahiptir (bkz. [ADR 0003](./architecture/0003-database-boundaries.md)). Bu belge; migration ve seed yaşam döngüsünü, host araçları için bağlantı dizesi üretimini, metin dump alma ve geri yükleme adımlarını anlatır.

Komutlar depo kökünden çalıştırılır. Compose sarmalayıcıları [.env](../.env) ve [.env.local](../.env.local) dosyalarını birlikte okur; yapılandırma önceliği için [DEVELOPMENT.md](./DEVELOPMENT.md) belgesine bakın.

## Migration ve seed

Şema değişiklikleri ve seed verileri sunucu süreci tarafından örtük olarak uygulanmaz. Compose stack'inde `migrate` ve `seed`, API başlamadan önce sırayla çalışıp biten tek seferlik servislerdir; `api` servisi `seed` başarıyla tamamlanmadan başlamaz.

Her bounded context kendi `DbContext` ve migration history tablosuna sahip olduğundan migration baseline'ı context başına tek `Initial` migration'dır. `migrate` komutu bunların tamamını tek adımda uygular.

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

Aynı komutlar host üzerindeki .NET SDK ile de çalıştırılabilir; `ConnectionStrings__Rudoger` ortam değişkeninin ayarlanması gerekir (bkz. [DEVELOPMENT.md › Host SDK ile çalıştırma](./DEVELOPMENT.md#host-sdk-ile-çalıştırma)):

```sh
dotnet run --project Host/Rudoger.Api/Rudoger.Api.csproj -- migrate
dotnet run --project Host/Rudoger.Api/Rudoger.Api.csproj -- seed
```

## Host için bağlantı dizesi

Host üzerinden SQL Server'a bağlanmaya uygun bağlantı dizesini yazdırmak için:

Windows PowerShell:

```powershell
.\scripts\db-connection-string.ps1
```

Linux/macOS Bash:

```bash
./scripts/db-connection-string.sh
```

Betik, yapılandırma önceliğine göre [.env](../.env) ve [.env.local](../.env.local) dosyalarını ve süreç ortamını okur; Compose içi sunucu adını host için `localhost` ve `MSSQL_PORT` değerine dönüştürür. Farklı bir veritabanı adı için PowerShell'de `-DatabaseName <ad>`, Bash'te `--database <ad>` kullanılır.

> **Not:**
>
> Çıktı, parolayı içerdiğinden log veya issue içeriğine eklemeyin.

## Dump alma

Depodaki [`Rudoger.DatabaseDump`](../Tools/DatabaseDump) aracı, [SQL Server Management Objects (SMO)](https://learn.microsoft.com/sql/relational-databases/server-management-objects-smo/overview-smo) üzerinden uygulama şemasını, bağımlı database nesnelerini ve tablo verisini yeniden çalıştırılabilir DDL/DML ifadeleri içeren UTF-8 `.sql` dosyasına yazar. Araç .NET 10 SDK ile kaynak koddan çalışır; ayrıca global bir dump aracı kurulması gerekmez. İlk çalıştırmada NuGet bağımlılıklarının indirilebilmesi gerekir.

Üretilen metin dump'ı fiziksel SQL Server backup'ı değildir. Server-level login, SQL Server Agent job ve sunucu yapılandırması gibi database dışındaki nesneleri içermez.

> **Not:**
>
> Transaction açısından tutarlı bir dump için export süresince veritabanına yazma yapılmamalıdır.

Rudoger'da önce API servisini durdurup export tamamlandıktan sonra yeniden başlatın:

Windows PowerShell:

```powershell
.\scripts\compose.ps1 stop api
.\scripts\dump-database.ps1
.\scripts\compose.ps1 start api
```

Linux/macOS Bash:

```bash
./scripts/compose.sh stop api
./scripts/dump-database.sh
./scripts/compose.sh start api
```

Varsayılan çıktı `artifacts/database/rudoger-<UTC timestamp>.sql` yoluna atomik olarak yazılır. Üretim başarısız olursa kısmi dump bırakılmaz. Farklı bir yol seçmek için:

Windows PowerShell:

```powershell
.\scripts\dump-database.ps1 -OutputPath .\artifacts\database\rudoger.sql
```

Linux/macOS Bash:

```bash
./scripts/dump-database.sh --output ./artifacts/database/rudoger.sql
```

Mevcut dosya varsayılan olarak ezilmez. Bilinçli olarak değiştirmek için PowerShell'de `-Force`, Bash'te `--force` kullanın. Betikler connection string'i yazdırmaz veya komut satırı argümanına koymaz; [.env](../.env), [.env.local](../.env.local) ve süreç ortamı önceliğini `db-connection-string` betiklerinden alır.

## Geri yükleme

Dump hedef database'i oluşturmaz. Önce boş `RudogerRestored` database'ini oluşturup ardından metin dump'ını container içindeki [`sqlcmd`](https://learn.microsoft.com/sql/tools/sqlcmd/sqlcmd-run-transact-sql-script-files) ile çalıştırmak için:

Windows PowerShell:

```powershell
.\scripts\compose.ps1 exec -T mssql bash -lc '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b -Q "CREATE DATABASE [RudogerRestored]"'
Get-Content -Raw -LiteralPath .\artifacts\database\rudoger.sql |
    .\scripts\compose.ps1 exec -T mssql bash -lc '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b -d RudogerRestored'
```

Linux/macOS Bash:

```bash
./scripts/compose.sh exec -T mssql bash -lc \
  '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b -Q "CREATE DATABASE [RudogerRestored]"'
./scripts/compose.sh exec -T mssql bash -lc \
  '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b -d RudogerRestored' \
  < ./artifacts/database/rudoger.sql
```

Geri yükleme hedefi yeni ve boş bir database olmalıdır. Komutlar hata durumunda durur; aynı dump dolu bir database üzerine uygulanmamalıdır.

Geri yüklenen database'e host üzerinden bağlanmak için bağlantı dizesini `--database RudogerRestored` (PowerShell'de `-DatabaseName RudogerRestored`) ile üretebilirsiniz.
