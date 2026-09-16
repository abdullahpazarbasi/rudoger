# Rudoger Mimarisi

Bu belge, depoda gözlemlenebilen mimariyi anlatır. Diyagramlar, ayrı bir C4 kütüphanesine bağımlı olmadan render edilebilsin diye C4 anlam yükü taşıyan Mermaid flowchart ve sequence diyagramları olarak çizilmiştir. Bir kutunun içinde gösterilen dizin, o kutuyu gerçekleyen depo konumudur.

## Kapsam ve gösterim

- Düz oklar eşzamanlı çalışma zamanı çağrılarını veya veri akışlarını gösterir.
- Kesikli oklar yoklama (polling), gerçekleştirim (implementation) veya yaşam döngüsü ilişkilerini gösterir.
- Deployment diyagramındaki düz oklar Compose `depends_on` koşullarını ve çalışma zamanı bağlantılarını birlikte gösterir; ayrım ok etiketlerinde belirtilmiştir.
- "Dahili API", tüketici bounded context'in Infrastructure projesinden sağlayıcı bounded context'in Presentation sözleşmesine yapılan süreç içi çağrıdır. HTTP kullanmaz.
- Authn, Product, Inventory ve Order event sourced'dur. Okuma projeksiyonları, event ekleme ile aynı yerel transaction içinde güncellenir.
- Logging, event sourced bir bounded context değil, ekle/güncelle tarzı bir operasyonel günlüktür.
- Diyagramlardaki tanımlayıcılar (sınıf, event, tablo ve uç nokta adları) kodla birebir eşleşsin diye İngilizce bırakılmıştır.

## 1. Sistem Bağlamı

```mermaid
flowchart TB
    consumer["API tüketicisi<br/>İnsan kullanıcı veya çağıran sistem<br/>Kimlik bilgilerini JWT ile takas eder"]
    rudoger["Rudoger<br/>Ürün, stok ve sipariş yönetimi Web API'si<br/>JWT kimlik doğrulama, OpenAPI, denetim günlüğü<br/>Depo kökü: ./"]

    consumer -->|"HTTP + JSON REST<br/>JWT bearer token"| rudoger
    rudoger -->|"JSON kaynaklar<br/>RFC 9457 problem+json<br/>X-Correlation-Id"| consumer
    consumer -.->|"202 Accepted sonrası<br/>Location URL'sini<br/>uç duruma kadar yoklar"| rudoger

    classDef person fill:#08427b,color:#fff,stroke:#052e56;
    classDef system fill:#1168bd,color:#fff,stroke:#0b4884;
    class consumer person;
    class rudoger system;
```

**Sorumluluklar.** Rudoger, depodaki tek iş sistemidir. Kullanıcıları doğrular, ürünleri ve paketlemeleri yönetir, tek depolu stoku yönetir, sipariş yerleştirme ve durum geçişlerini koordine eder ve her HTTP ile dahili API çağrısını kayıt altına alır.

**Veri akışı.** Tüketici, kimlik bilgilerini bir JWT ile takas eder, ardından kimlik doğrulamalı REST istekleri gönderir. API ya eşzamanlı kaynak sonuçları ya da eşzamansız sipariş iş akışları için `Location` URL'si taşıyan `202 Accepted` döndürür. Tüketici, süreç `Succeeded` veya `Failed` olana kadar bu URL'yi yoklar.

**Varsayım.** "API tüketicisi" bilinçli olarak genel tutulmuştur. Depoda birinci taraf bir iş arayüzü yoktur ve çağıranın insan tarafından kullanılan bir istemci mi yoksa başka bir sistem mi olduğu belirlenmemiştir. Swagger UI bir belgeleme/test konteyneridir, iş ön yüzü değildir.

## 2. Konteyner

```mermaid
flowchart TB
    consumer["API tüketicisi<br/>Tarayıcı, HTTP istemcisi veya çağıran sistem"]

    subgraph system["Rudoger sistemi — compose.yaml"]
        swagger["Swagger UI + Nginx<br/>OpenAPI gezgini ve ters vekil<br/>docker/swagger-ui"]
        api["Rudoger API<br/>ASP.NET Core 10 modüler monolit<br/>REST, JWT, health, OpenAPI, hosted worker'lar<br/>Host/Rudoger.Api"]
        migrate["migrate<br/>Tek seferlik konteyner<br/>Beş EF Core migration setini uygular<br/>Host/Rudoger.Api"]
        seed["seed<br/>Tek seferlik konteyner<br/>Varsayılan Authn kullanıcılarını oluşturur<br/>Host/Rudoger.Api"]
        database[("Microsoft SQL Server 2022<br/>Veritabanı: Rudoger<br/>Şemalar: authn, product, inventory, order, logging<br/>Eşlemeler: Modules/*/Infrastructure")]
    end

    consumer -->|"HTTP :8080<br/>JSON REST istekleri<br/>JWT ve uygulama yanıtları"| api
    consumer -->|"HTTP :8081/docs<br/>Belge gezgini"| swagger
    swagger -->|"Ters vekil<br/>OpenAPI belgesi ve Try it out"| api
    api -->|"EF Core, TDS üzerinden<br/>Beş bağımsız DbContext"| database
    migrate -->|"Şema migration'ları"| database
    seed -->|"Seed verisi"| database

    classDef external fill:#08427b,color:#fff,stroke:#052e56;
    classDef container fill:#438dd5,color:#fff,stroke:#1f5f99;
    classDef data fill:#2e7d32,color:#fff,stroke:#1b5e20;
    class consumer external;
    class swagger,api,migrate,seed container;
    class database data;
```

**Sorumluluklar.** API, beş bounded context'in tamamını içeren tek bir dağıtılabilir süreçtir. `migrate` ve `seed`, aynı imajdan üretilen ve API başlamadan önce sırayla çalışıp biten tek seferlik konteynerlerdir. Swagger UI belge kabuğunu sunar ve API trafiğini kendi origin'inden vekiller. SQL Server, bounded context başına bir şema ve migration geçmişi barındıran tek veritabanına sahiptir.

**Veri akışı.** Tüketiciler API'yi doğrudan veya Swagger'ın Nginx vekili üzerinden çağırabilir. API, aynı veritabanına karşı ayrı EF Core DbContext'leri kullanır. Kod tabanında broker, önbellek, harici kimlik sağlayıcı, ödeme servisi veya ayrı bir depo servisi yoktur.

## 3. Arka Uç Bileşenleri

### 3.1 HTTP girişi ve kesişen bileşenler

```mermaid
flowchart LR
    request["Gelen HTTP isteği<br/>JSON REST, isteğe bağlı JWT bearer"]

    subgraph host["Middleware hattı, sırayla — Host/Rudoger.Api/Program.cs"]
        direction TB
        auth["1. JWT bearer kimlik doğrulama<br/>Claim'leri kurar<br/>Modules/Authn/Infrastructure"]
        correlation["2. CorrelationIdMiddleware<br/>X-Correlation-Id türetir veya üretir<br/>BuildingBlocks/Presentation"]
        audit["3. ApiRequestLoggingMiddleware<br/>Sırları maskeler, başlangıç kaydı yazılamazsa 503<br/>Modules/Logging/Presentation"]
        errors["4. ExceptionHandlingMiddleware + status code pages<br/>RFC 9457 problem details<br/>BuildingBlocks/Presentation"]
        authorization["5. ASP.NET Core yetkilendirme<br/>Product, Inventory, Order için JWT zorunlu<br/>Modules/Authn/Infrastructure"]
        endpoints["6. REST controller'ları<br/>Modules/{Authn,Product,Inventory,Order}/Presentation"]
        auth --> correlation --> audit --> errors --> authorization --> endpoints
    end

    subgraph authn["Authn bağlamı"]
        direction TB
        authnUsecase["Kimlik takası, seed ve User aggregate<br/>Modules/Authn/Application<br/>Modules/Authn/Domain"]
        authnStore["AuthnRepository, parola hash'leme, JWT üretimi, AuthnDbContext<br/>Modules/Authn/Infrastructure"]
        authnDb[("authn.Events<br/>authn.Users")]
        authnUsecase --> authnStore --> authnDb
    end

    subgraph business["İş bağlamları: Product, Inventory, Order"]
        direction TB
        applications["İş uygulama servisleri<br/>Modules/{Product,Inventory,Order}/Application"]
        gateways["Bağlamlar arası gateway'ler<br/>Dahili çağrıları InternalCallLogger ile kaydeder<br/>Modules/{Product,Inventory,Order}/Infrastructure"]
        applications -. "Bağlamlar arası işlem gerektiğinde" .-> gateways
    end

    subgraph logging["Logging bağlamı"]
        direction TB
        logStore["RequestLogStore ve InternalCallLogger<br/>Modules/Logging/Infrastructure"]
        logDb[("logging.RequestLogs")]
        logStore --> logDb
    end

    request --> host
    host -->|"6: /api/v1/authn/tokens"| authn
    host -->|"6: Product, Inventory ve Order controller'ları"| business
    host -->|"3: denetim kaydını başlat ve tamamla"| logging

    classDef component fill:#85bbf0,color:#111,stroke:#1f5f99;
    classDef data fill:#2e7d32,color:#fff,stroke:#1b5e20;
    class request,auth,correlation,audit,errors,authorization,endpoints,authnUsecase,authnStore,applications,gateways,logStore component;
    class authnDb,logDb data;
```

Ok etiketlerindeki numaralar, çağrıyı başlatan middleware adımını gösterir.

**Sorumluluklar.** `Program.cs`; kimlik doğrulama, korelasyon, zorunlu istek günlüğü, standart hata yanıtları, yetkilendirme, controller'lar, health ve OpenAPI'yi bu sırayla bir araya getirir. Token takası, health ve OpenAPI uç noktaları anonimdir; Product, Inventory ve Order controller'ları JWT yetkilendirmesi ister.

**Veri akışı.** Kimlik doğrulama claim'leri kurar; korelasyon `X-Correlation-Id` değerini türetir veya üretir; günlükleme, uç nokta çalışmadan önce maskelenmiş bir başlangıç kaydı yazar ve sonrasında kaydı tamamlar. Bilinen domain, çakışma, eşzamanlılık, bulunamadı, doğrulama ve kimlik doğrulama hataları `application/problem+json` yanıtlarına dönüşür. Dahili API gateway'leri aynı korelasyon bağlamını kullanır ve `InternalCallLogger` üzerinden `logging.RequestLogs` tablosuna `Internal` kanalıyla yazar.

### 3.2 İş bounded context'leri ve dahili API'ler

```mermaid
flowchart TB
    subgraph product["Product bağlamı — Modules/Product"]
        direction LR
        pp["Presentation<br/>ProductsController ve IProductInternalApi<br/>Modules/Product/Presentation"]
        pa["Application<br/>Ürün ve kullanım talebi use case'leri, portlar<br/>Modules/Product/Application"]
        pd["Domain<br/>Product aggregate'i ve iş kuralları<br/>Modules/Product/Domain"]
        pi["Infrastructure<br/>Repository, EF Core, Inventory ve Order gateway'leri<br/>Modules/Product/Infrastructure"]
        pdb[("product şeması<br/>Events, Products, ProductPackagings, ProductUsageClaims")]
        pp --> pa --> pd
        pa --> pi --> pdb
    end

    subgraph order["Order bağlamı — Modules/Order"]
        direction LR
        op["Presentation<br/>OrdersController ve IOrderInternalApi<br/>Modules/Order/Presentation"]
        oa["Application<br/>Sipariş use case'leri ve OrderWorkflowService, portlar<br/>Modules/Order/Application"]
        od["Domain<br/>Order ve OrderPlacement aggregate'leri<br/>Modules/Order/Domain"]
        oi["Infrastructure<br/>Repository'ler, EF Core, Product ve Inventory gateway'leri, OrderOutboxWorker<br/>Modules/Order/Infrastructure"]
        odb[("order şeması<br/>Events, Orders, OrderLines, OrderPlacements, OrderPlacementLines, OrderTransitions, OutboxMessages")]
        op --> oa --> od
        oa --> oi --> odb
    end

    subgraph inventory["Inventory bağlamı — Modules/Inventory"]
        direction LR
        ip["Presentation<br/>StockItemsController ve IInventoryInternalApi<br/>Modules/Inventory/Presentation"]
        ia["Application<br/>Stok use case'leri ve dahili servis, portlar<br/>Modules/Inventory/Application"]
        id["Domain<br/>StockItem aggregate'i ve iş kuralları<br/>Modules/Inventory/Domain"]
        ii["Infrastructure<br/>Repository, EF Core, Product gateway'i, InventoryOutboxWorker<br/>Modules/Inventory/Infrastructure"]
        idb[("inventory şeması<br/>Events, StockItems, StockMovements, OutboxMessages")]
        ip --> ia --> id
        ia --> ii --> idb
    end

    product -->|"HasAnyOrder"| order
    product -->|"HasAnyStock"| inventory
    order -->|"ClaimOffer / ReleaseUsage"| product
    inventory -->|"ClaimOffer / ReleaseUsage"| product
    order -->|"Reserve / Commit / Release / Compensate"| inventory

    classDef component fill:#85bbf0,color:#111,stroke:#1f5f99;
    classDef domain fill:#ffd54f,color:#111,stroke:#a66f00;
    classDef data fill:#2e7d32,color:#fff,stroke:#1b5e20;
    class pp,pa,pi,ip,ia,ii,op,oa,oi component;
    class pd,id,od domain;
    class pdb,idb,odb data;
```

Bağlamlar arası oklar, çağıran bağlamın Infrastructure gateway'inden çıkıp hedef bağlamın Presentation sözleşmesine (`I*InternalApi`) ulaşır; okunabilirlik için bağlam sınırından bağlam sınırına çizilmiştir.

**Sorumluluklar.** Product; katalog, paketleme, fiyat ve geçici kullanım taleplerinin sahibidir. Inventory; temel UoM cinsinden stok bakiyelerinin ve hareketlerinin sahibidir. Order; eşzamansız yerleştirme, fiyat anlık görüntüleri, rezervasyon koordinasyonu ve shipped/cancelled geçişlerinin sahibidir. Her bağlam, Clean Architecture katmanlarını ayrı projelerde korur.

**Veri akışı.** Bağlamlar arası çağrılar yalnızca tüketicinin Infrastructure gateway'inden çıkar ve sağlayıcının Presentation sözleşmesini hedefler. Süreç içi çağrılardır, ancak her bounded context bağımsız commit eder. Ürün silme, Inventory ve Order'a sorar. Inventory, stok açarken veya elle hareket girerken Product'ı talep eder. Order, Product tekliflerini talep eder ve Inventory'ye rezervasyon, commit, release veya telafi komutları verir. Her gateway çağrısı, 3.1'de gösterilen `InternalCallLogger` üzerinden `logging.RequestLogs` tablosuna yazılır.

### 3.3 Event store, projeksiyonlar, outbox'lar ve worker yoklaması

```mermaid
flowchart TB
    usecase["İş uygulama servisi<br/>Komutu alır, aggregate'i yükler ve kaydeder<br/>Modules/{Authn,Product,Inventory,Order}/Application"]
    aggregate["Aggregate kökü<br/>Kuralları uygular, commit edilmemiş domain event'leri biriktirir<br/>Modules/{Authn,Product,Inventory,Order}/Domain"]
    repository["Bağlam repository'si ve projector'ü<br/>Event akışını yükler, projeksiyonu günceller<br/>Modules/{Authn,Product,Inventory,Order}/Infrastructure"]
    eventStore["Jenerik EventStore&lt;TDbContext&gt;<br/>JSON event zarfı: EventId, StreamId, AggregateType, Version, EventType, SchemaVersion, Payload, Metadata, OccurredAtUtc<br/>BuildingBlocks/Infrastructure"]
    transaction["Tek yerel EF Core transaction'ı<br/>Events'e ekle + okuma projeksiyonunu güncelle<br/>+ gerekiyorsa outbox mesajı ekle"]
    sql[("Bağlama ait SQL şeması<br/>Events ve projeksiyon tabloları<br/>Unique (StreamId, Version) indeksi")]

    orderWorker["OrderOutboxWorker<br/>Sipariş yerleştirme ve geçiş iş akışları<br/>Modules/Order/Infrastructure"]
    inventoryWorker["InventoryOutboxWorker<br/>Product kullanım talebini serbest bırakır<br/>Modules/Inventory/Infrastructure"]

    usecase --> aggregate --> repository --> eventStore --> transaction --> sql
    sql -->|"Sıralı akış event'leri"| eventStore
    eventStore -->|"Yeniden kurulan geçmiş"| aggregate

    orderWorker -. "order.OutboxMessages'ı yoklar, boşta 500 ms bekler<br/>Kaydı 1 dakikalığına kilitler ve işler<br/>İşlendi olarak işaretler veya yeniden dener" .-> sql
    inventoryWorker -. "inventory.OutboxMessages'ı yoklar, boşta 500 ms bekler<br/>Kaydı 1 dakikalığına kilitler ve işler<br/>İşlendi olarak işaretler veya yeniden dener" .-> sql

    classDef component fill:#85bbf0,color:#111,stroke:#1f5f99;
    classDef domain fill:#ffd54f,color:#111,stroke:#a66f00;
    classDef data fill:#2e7d32,color:#fff,stroke:#1b5e20;
    class usecase,repository,eventStore,transaction,orderWorker,inventoryWorker component;
    class aggregate domain;
    class sql data;
```

**Sorumluluklar.** Jenerik event store, kararlı event zarflarını serileştirir ve yarışları unique `(StreamId, Version)` kısıtı ile çözer. Repository projector'leri okuma modellerini aynı transaction içinde günceller. Order ve Inventory, yerel transaction'larına outbox satırları ekler ve bunları en az bir kez işler.

**Veri akışı ve yoklama.** Repository'ler aggregate'i yeniden kurmak için sıralı event'leri yükler; yeni event'leri ve projeksiyonları atomik olarak kaydeder. Her hosted worker, vadesi gelmiş ve kilitsiz tek bir satırı tekrar tekrar kilitleyerek alır. Uygun satır yoksa yeniden yoklamadan önce **500 ms** bekler. Kilit bir dakika sürer; hatalar 60 saniyede sınırlanan üstel yeniden deneme gecikmeleri kullanır. Kaynak event ID'leri ve idempotency anahtarları, tekrarlanan Inventory etkilerini güvenli kılar.

## 4. Deployment

```mermaid
flowchart TB
    browser["Tarayıcı veya API istemcisi<br/>Docker host'un dışından bağlanır"]

    subgraph dockerHost["Docker host — compose.yaml, tek Compose ağı"]
        apiImage["Ortak API imajı<br/>Dockerfile, build bağlamı: ./<br/>Kaynak: Host/Rudoger.Api<br/>migrate, seed ve api aynı imajı kullanır"]
        swagger["swagger servisi<br/>swagger-ui v5.32.11 + Nginx<br/>Konteyner portu 8080<br/>docker/swagger-ui/default.conf.template"]
        api["api servisi<br/>Komut: serve<br/>Konteyner portu 8080<br/>Healthcheck: her 10 s GET /health"]

        subgraph prep["Tek seferlik veritabanı hazırlığı"]
            direction RL
            migrate["migrate servisi<br/>Komut: migrate<br/>Beş EF Core migration setini uygular"]
            seed["seed servisi<br/>Komut: seed<br/>Eksik varsayılan Authn kullanıcılarını oluşturur"]
            seed -->|"depends_on: migrate<br/>başarıyla tamamlandı"| migrate
        end

        mssql["mssql servisi<br/>SQL Server 2022 CU27<br/>Konteyner portu 1433<br/>Healthcheck: her 10 s sqlcmd SELECT 1<br/>Kalıcı veri: mssql-data volume, /var/opt/mssql"]
    end

    browser -->|"SWAGGER_PORT, varsayılan 8081<br/>/docs"| swagger
    browser -->|"API_PORT, varsayılan 8080<br/>JSON REST"| api

    swagger -->|"depends_on: api sağlıklı<br/>Ters vekil: api:8080"| api
    api -->|"depends_on: seed başarıyla tamamlandı"| prep
    prep -->|"depends_on: mssql sağlıklı<br/>Migration'ları ve seed satırlarını yazar"| mssql
    api -->|"Çalışma zamanı EF Core bağlantıları<br/>/health DB bağlantısını doğrular"| mssql

    apiImage -.-> api
    apiImage -.-> prep

    classDef external fill:#08427b,color:#fff,stroke:#052e56;
    classDef node fill:#999,color:#fff,stroke:#555;
    classDef container fill:#438dd5,color:#fff,stroke:#1f5f99;
    class browser external;
    class apiImage node;
    class migrate,seed,api,swagger,mssql container;
```

**Sorumluluklar.** Compose; SQL Server'ı, iki tek seferlik veritabanı yaşam döngüsü komutunu, uzun ömürlü API'yi ve Swagger UI'ı çalıştırır. `migrate`, `seed` ve `api` aynı Dockerfile'dan üretilir ve davranışı çalıştırılabilir dosyanın komutuyla seçer. Adlandırılmış volume SQL Server verisini kalıcı kılar.

**Veri akışı ve yoklama.** Başlangıç sırasıyla SQL sağlığı, başarılı migration, başarılı seed ve API sağlığı ile kapılanır; diyagramdaki `depends_on` okları bağımlı servisten beklediği servise doğru okunur. SQL ve API healthcheck'leri her **10 saniyede** bir yoklar. `/health`, SQL bağlantısını `AuthnDbContext` üzerinden doğrular. Swagger, Compose ağında `api:8080` adresine vekillik eder.

**Varsayım.** Depo tarafından tanımlanan tek deployment topolojisi budur. Tek bir Compose host'unu temsil eder; üretim yük dengeleyicisi, TLS sonlandırma, gizli anahtar deposu, replika veya yüksek erişilebilirlik topolojisi çıkarsanmamıştır.

## 5. Kritik Operasyon Sequence Diyagramları

### 5.1 Stok kalemi açma ve geçici Product talebini serbest bırakma

```mermaid
sequenceDiagram
    autonumber
    actor Client as İstemci
    participant Inventory as Inventory<br/>Presentation + Application<br/>Modules/Inventory
    participant InventoryDb as inventory<br/>şeması
    participant Product as Product<br/>dahili API'si<br/>Modules/Product/Presentation
    participant ProductDb as product<br/>şeması
    participant Worker as Inventory<br/>OutboxWorker<br/>Modules/Inventory/Infrastructure

    Client->>Inventory: POST /api/v1/inventory/<br/>stock-items<br/>Idempotency-Key başlığı ile
    Inventory->>InventoryDb: Oluşturma idempotency<br/>anahtarını ve ProductId'yi<br/>sorgula
    alt Aynı girdiyle mevcut anahtar
        Inventory-->>Client: 201, özgün StockItem
    else Yeni istek
        Inventory->>Product: ClaimOffer(usage = INVENTORY, operationId)
        Product->>ProductDb: ProductUsageClaimed ekle<br/>+ talebi projekte et
        Note over Product,ProductDb: Tek Product transaction'ı
        Product-->>Inventory: BaseUomCode
        Inventory->>InventoryDb: StockItemOpened ve varsa<br/>StockReceived ekle, projeksiyonu yaz,<br/>serbest bırakma outbox mesajı ekle
        Note over Inventory,InventoryDb: Tek Inventory transaction'ı
        Inventory-->>Client: 201 StockItem
    end

    loop Vadesi gelen Inventory outbox mesajını yokla, boşsa 500 ms bekle
        Worker->>InventoryDb: En eski vadesi gelmiş kilitsiz mesajı<br/>atomik olarak kilitle (1 dakika)
    end
    Worker->>Product: ReleaseUsage(productId, operationId)
    Product->>ProductDb: ProductUsageReleased ekle<br/>+ talebi kaldır
    alt Serbest bırakma başarılı
        Worker->>InventoryDb: Outbox mesajını işlendi olarak işaretle
    else Serbest bırakma başarısız
        Worker->>InventoryDb: Kilidi kaldır, üstel gecikmeyle<br/>yeniden dene (en fazla 60 s)
    end
```

**Sorumluluklar.** Inventory, kanonik temel UoM'yi alıp kendi stok durumunu commit ederken silinmeyi önlemek için Product'ı geçici olarak talep eder. Yerel Inventory transaction'ı, bu talebi serbest bırakmak için gereken outbox işini de oluşturur.

**Veri akışı ve yoklama.** `Idempotency-Key`, stok oluşturmayı tekrarlanabilir kılar. Inventory worker'ı outbox'ını 500 ms boşta bekleme ile yoklar, Product dahili API'sini süreç içinde çağırır ve mesajı işaretler veya yeniden zamanlar. Elle giriş, düzeltme ve düşüm hareketleri de aynı Product talebi serbest bırakma desenini kullanır.

### 5.2 Siparişi eşzamansız yerleştirme

```mermaid
sequenceDiagram
    autonumber
    actor Client as İstemci
    participant Order as Order<br/>Presentation + Application<br/>Modules/Order
    participant OrderDb as order<br/>şeması
    participant Worker as Order<br/>OutboxWorker<br/>Modules/Order/Infrastructure
    participant Product as Product<br/>dahili API'si<br/>Modules/Product/Presentation
    participant ProductDb as product<br/>şeması
    participant Inventory as Inventory<br/>dahili API'si<br/>Modules/Inventory/Presentation
    participant InventoryDb as inventory<br/>şeması

    Client->>Order: POST /api/v1/order/orders<br/>JWT + Idempotency-Key<br/>+ sipariş satırları
    Order->>OrderDb: Yerleştirme idempotency<br/>anahtarını sorgula
    alt Aynı satırlarla mevcut anahtar
        Order-->>Client: 202, özgün yerleştirme<br/>ve Location başlığı
    else Yeni yerleştirme
        Order->>OrderDb: OrderPlacementRequested ekle<br/>Pending yerleştirmeyi projekte et<br/>+ outbox mesajı ekle
        Note over Order,OrderDb: Tek Order transaction'ı
        Order-->>Client: 202 Pending yerleştirme<br/>+ Location başlığı
    end

    par İstemcinin durum yoklaması
        loop Succeeded veya Failed olana dek, istemcinin seçtiği aralıkla
            Client->>Order: GET Location<br/>/api/v1/order/order-placements/{id}
            Order->>OrderDb: Yerleştirme projeksiyonunu oku
            Order-->>Client: Pending, Succeeded veya Failed
        end
    and Arka plan iş akışı yoklaması
        loop Vadesi gelen Order outbox mesajını yokla, boşsa 500 ms bekle
            Worker->>OrderDb: En eski vadesi gelmiş kilitsiz mesajı<br/>atomik olarak kilitle (1 dakika)
        end
        loop Her farklı Product için
            Worker->>Product: ClaimOffer(productId, placementId, UoM kodları)
            Product->>ProductDb: Kullanım talebini ekle,<br/>fiyat ve dönüşümleri döndür
        end
        Worker->>Worker: Tek para birimini doğrula,<br/>temel miktarları ve fiyat<br/>anlık görüntülerini hesapla
        loop Her farklı Product için
            Worker->>Inventory: Reserve(temel miktar, orderId, sourceEventId)
            Inventory->>InventoryDb: StockReserved ekle<br/>+ bakiyeleri güncelle
        end
        alt İş kuralları başarılı
            Worker->>OrderDb: OrderPlaced ekle<br/>+ Order/OrderLines projeksiyonu
            Worker->>OrderDb: OrderPlacementSucceeded ekle<br/>+ Succeeded projeksiyonu
        else Domain, çakışma veya bulunamadı hatası
            loop Her Product grubu için
                Worker->>Inventory: CompensateReservation(orderId, releaseSourceEventId)
                Inventory->>InventoryDb: Rezervasyon varsa<br/>idempotent olarak serbest bırak
            end
            Worker->>OrderDb: OrderPlacementFailed ekle<br/>+ hata ayrıntıları
        else Altyapı veya beklenmeyen hata
            Worker->>Worker: Temizlik sonrası<br/>hatayı yukarı ilet
        end
        loop Başarıyla talep edilen her Product teklifi için
            Worker->>Product: ReleaseUsage(productId, placementId)
            Product->>ProductDb: Kullanım serbest bırakmayı ekle<br/>+ talebi kaldır
        end
        alt İş akışı ve Product talep temizliği başarıyla döndü
            Worker->>OrderDb: Outbox mesajını<br/>işlendi olarak işaretle
        else Altyapı veya temizlik hatası yukarı çıktı
            Worker->>OrderDb: Kilidi kaldır, üstel gecikmeyle<br/>yeniden dene (en fazla 60 s)
        end
    end
```

**Sorumluluklar.** İlk istek bir Order değil, kalıcı bir yerleştirme süreci oluşturur. Worker; Product fiyatlarının ve dönüşüm çarpanlarının anlık görüntüsünü alır, temel UoM cinsinden stok rezerve eder, Order'ı yalnızca rezervasyonlar başarılı olduktan sonra oluşturur ve iş hatasında rezervasyonları telafi eder.

**Veri akışı ve yoklama.** API hemen `202` döndürür. İstemci `Location` kaynağını kendi seçtiği aralıkla yoklar; kodda üretim istemcisi için bir aralık öngörülmemiştir. Bundan bağımsız olarak `OrderOutboxWorker` boştayken her 500 ms'de bir yoklar. Gateway çağrıları süreç içidir ve dahili çağrı olarak kayıt altına alınır. Her bounded context ayrı commit ettiği için idempotent kaynak event ID'leri ve telafi, yeniden denemeleri korur.

### 5.3 Siparişi eşzamansız olarak sevk etme veya iptal etme

```mermaid
sequenceDiagram
    autonumber
    actor Client as İstemci
    participant Order as Order<br/>Presentation + Application<br/>Modules/Order
    participant OrderDb as order<br/>şeması
    participant Worker as Order<br/>OutboxWorker<br/>Modules/Order/Infrastructure
    participant Inventory as Inventory<br/>dahili API'si<br/>Modules/Inventory/Presentation
    participant InventoryDb as inventory<br/>şeması

    Client->>Order: POST /api/v1/order/orders/<br/>{orderId}/transitions<br/>hedef = Shipped veya Cancelled
    Order->>OrderDb: Order aggregate'ini<br/>yeniden kur
    Order->>OrderDb: OrderTransitionRequested ekle<br/>Pending geçişi projekte et<br/>+ outbox mesajı ekle
    Note over Order,OrderDb: Tek Order transaction'ı
    Order-->>Client: 202 geçiş + Location başlığı

    par İstemcinin durum yoklaması
        loop Uç duruma dek, istemcinin seçtiği aralıkla
            Client->>Order: GET Location<br/>/api/v1/order/orders/{orderId}/<br/>transitions/{transitionId}
            Order->>OrderDb: Geçiş projeksiyonunu oku
            Order-->>Client: Pending veya Succeeded
        end
    and Arka plan geçiş iş akışı
        loop Vadesi gelen Order outbox mesajını yokla, boşsa 500 ms bekle
            Worker->>OrderDb: ProcessTransition mesajını<br/>atomik olarak kilitle (1 dakika)
        end
        Worker->>OrderDb: Order'ı ve etkin stok<br/>operasyonlarını yeniden kur
        alt Tüm Inventory çağrıları başarılı
            loop Siparişteki her farklı Product için
                alt hedef = Shipped
                    Worker->>Inventory: Commit(productId, orderId, sourceEventId)
                    Inventory->>InventoryDb: StockCommitted ekle<br/>OnHand -= miktar<br/>Reserved -= miktar
                else hedef = Cancelled
                    Worker->>Inventory: Release(productId, orderId, sourceEventId)
                    Inventory->>InventoryDb: StockReleased ekle<br/>Reserved -= miktar
                end
            end
            Worker->>OrderDb: OrderShipped veya OrderCancelled ekle<br/>Order ve geçiş projeksiyonlarını güncelle
            Worker->>OrderDb: Outbox mesajını<br/>işlendi olarak işaretle
        else Herhangi bir çağrı başarısız
            Worker->>OrderDb: Kilidi kaldır, üstel gecikmeyle<br/>yeniden dene (en fazla 60 s)
        end
    end
```

**Sorumluluklar.** Geçiş kararının sahibi Order, stok etkisinin sahibi Inventory'dir. Sevkiyat rezervasyonu hem eldeki hem de rezerve bakiyeden düşer; iptal yalnızca rezerve bakiyeyi serbest bırakır.

**Veri akışı ve yoklama.** Geçiş isteği ve outbox satırı birlikte commit edilir. İstemci döndürülen geçiş URL'sini yoklarken aynı Order worker yoklama döngüsü isteği işler. Hatalar worker'ın yeniden deneme politikasına düşer; Inventory kaynak event ID'leri tekrarlanan commit/release komutlarını idempotent kılar.

### 5.4 Product'ı yalnızca bağlamlar arası kurallar izin verdiğinde silme

```mermaid
sequenceDiagram
    autonumber
    actor Client as İstemci
    participant Product as Product<br/>Presentation + Application<br/>Modules/Product
    participant ProductDb as product<br/>şeması
    participant Gateway as Product<br/>Infrastructure gateway'leri<br/>Modules/Product/Infrastructure
    participant Inventory as Inventory<br/>dahili API'si<br/>Modules/Inventory/Presentation
    participant Order as Order<br/>dahili API'si<br/>Modules/Order/Presentation

    Client->>Product: DELETE /api/v1/product/<br/>products/{productId}
    Product->>ProductDb: Product aggregate'ini<br/>yeniden kur
    Product->>Gateway: HasAnyStock(productId)
    Gateway->>Inventory: Süreç içi dahili API çağrısı,<br/>InternalCallLogger ile kaydedilir
    Inventory->>Inventory: inventory şemasından<br/>StockItem projeksiyonunu oku
    Inventory-->>Gateway: Sıfırdan farklı OnHand<br/>veya Reserved var mı?
    Gateway-->>Product: Sonuç
    alt Ürünün stoku var
        Product-->>Client: 409 product-has-stock
    else Stok yok
        Product->>Gateway: HasAnyOrder(productId)
        Gateway->>Order: Süreç içi dahili API çağrısı,<br/>InternalCallLogger ile kaydedilir
        Order->>Order: order şemasından<br/>OrderLines projeksiyonunu sorgula
        Order-->>Gateway: Herhangi bir Order<br/>referans veriyor mu?
        Gateway-->>Product: Sonuç
        alt Ürüne referans veren Order var
            Product-->>Client: 409 product-has-orders
        else Order referansı yok
            Product->>Product: Etkin kullanım talebi varsa<br/>reddet (409 product-in-use)
            Product->>ProductDb: ProductDeleted ekle<br/>+ projeksiyonda IsDeleted işaretle
            Note over Product,ProductDb: Tek Product transaction'ı
            Product-->>Client: 204 No Content
        end
    end
```

**Sorumluluklar.** Product kendi etkin kullanım talebi kuralını uygular ve stok ile Order referanslarını sahibi olan bounded context'lere sorar. Şemalar arası foreign key veya bağlamlar arası doğrudan tablo erişimi kullanılmaz.

**Veri akışı.** Product Infrastructure gateway'leri, Inventory ve Order Presentation sözleşmelerini süreç içinde çağırır ve bu çağrıları kayıt altına alır. Başarılı silme, tek Product transaction'ında bir event ile yumuşak silme projeksiyon güncellemesidir; herhangi bir stok, Order referansı veya etkin operasyon çakışma üretir.

## Varsayımlar ve bilinçli eksiklikler

1. Deployment diyagramı yalnızca depoya işlenmiş Compose topolojisini modeller. Depo, bulut veya çok düğümlü bir üretim topolojisi tanımlamaz.
2. İstemci yoklaması, açık `202 + Location` sözleşmesinin parçasıdır; ancak aralığı ve zaman aşımı API tarafından yapılandırılmaz. Uçtan uca testin 200 ms aralığı test davranışıdır ve üretim gereksinimi olarak sunulmaz.
3. Worker yoklama aralıkları, kilit süresi, yeniden deneme üst sınırı ve Compose healthcheck aralıkları varsayım değildir; `OrderOutboxWorker`, `InventoryOutboxWorker` ve `compose.yaml` içinde açıkça yer alır.
4. Mesaj broker'ı, önbellek, harici kimlik doğrulama sağlayıcısı, ödeme işlemcisi, depo servisi, refresh token servisi veya ön yüz uygulaması bulunmadığı için hiçbiri gösterilmemiştir.

## Birincil uygulama kanıtları

- Kompozisyon ve middleware sırası: `Host/Rudoger.Api/Program.cs`
- Konteyner topolojisi ve sağlık yoklaması: `compose.yaml`, `Dockerfile`, `docker/swagger-ui/default.conf.template`
- Katman ve bounded context bağımlılık kuralları: `Tests/Architecture/ModuleDependencyTests.cs` ve tüm proje referansları
- Event transaction'ları ve eşzamanlılık: `BuildingBlocks/Infrastructure/EventStore.cs`
- Dahili API gateway'leri: `Modules/{Product,Inventory,Order}/Infrastructure/*Gateway.cs`
- Eşzamansız iş akışları ve yoklama: `Modules/Order/Infrastructure/OrderOutboxWorker.cs`, `Modules/Inventory/Infrastructure/InventoryOutboxWorker.cs`
- Sipariş orkestrasyonu: `Modules/Order/Application/OrderWorkflowService.cs`
- Veritabanı sahipliği: `Modules/*/Infrastructure` altındaki beş `*DbContext.cs` dosyası
