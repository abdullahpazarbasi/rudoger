# Ubiquitous Language

Bu sözlük; kod, API sözleşmeleri, testler ve dokümantasyon tarafından paylaşılan kanonik kavram dağarcığıdır.

## Shared Terms

### Bounded Context (BC)

Kendi modeline, event store veya operasyonel verisine, projection'ına, API sözleşmelerine ve veritabanı şemasına sahip özerk iş sınırıdır. Rudoger; Authn, Product, Inventory, Order ve Logging bounded context'lerinden oluşur.

### Internal API

Tüketici bounded context'in Infrastructure katmanından, sağlayıcı bounded context'in Presentation sözleşmesine yapılan process içi istektir. HTTP kullanmaz; ancak dış istekler gibi correlation kapsamında izlenir ve günlüğe alınır.

### Correlation ID

`X-Correlation-Id`, event metadata, stock movement, internal call, log ve problem response boyunca taşınan operasyon kimliğidir.

### Idempotency Key

Yeniden denenebilir bir command için client tarafından sağlanan kimliktir. Aynı anahtar ve anlamsal olarak aynı girdi ilk sonucu döndürür; aynı anahtarın farklı bir girdiyle kullanılması conflict oluşturur.

### Operation ID

Bounded context'ler arası tek bir işi tanımlayan kararlı kimliktir. Çağıran taraf üretir, Internal API sözleşmesinde taşınır; sağlayıcı taraf yeniden denemeleri idempotent yapmak için bunu kullanır. Product bunu Usage Claim'de, Inventory ise Stock Movement'ta tutar. Inventory'nin `StockMovements` tablosundaki kolon adı, proje şartnamesine bağlı kalmak için `SourceEventId` olarak korunur; kavramın adı Operation ID'dir.

### Published Contract

Bir bounded context'in dış dünyaya açtığı temsildir ve yalnızca o context'in Presentation katmanına aittir: HTTP request/response record'ları, contract enum'ları, Internal API arayüzleri ve Failure Code kümesi. Application view model'leri ve domain tipleri published contract'ın parçası değildir; bu yüzden tel üzerinde görünmezler.

### Failure Code

Bir problem response'unun veya bir Order Placement başarısızlığının kararlı makine-okunur kimliğidir. Her failure code onu yayınlayan context'e aittir; `RFC 9457` problem type URI'sinin son parçası olarak görünür.

### Anti-corruption Layer (ACL)

Tüketici bounded context'in Infrastructure katmanındaki gateway'de bulunan çeviri katmanıdır. Sağlayıcı context'in yayınladığı Failure Code ve metinlerini tüketicinin kendi dağarcığına çevirir; böylece bir context'in modeli, başka bir context'in API'sı üzerinden tahmin edilemez.

### Event Stream

Tek bir aggregate'a ait event'lerin, sıfırdan başlayan ve kesintisiz ilerleyen version sırasına göre tutulduğu kayıttır.

### Event Envelope

Bir domain event'in genel saklama biçimidir: EventId, StreamId, AggregateType, Version, EventType, SchemaVersion, Payload, Metadata ve OccurredAtUtc alanlarından oluşur.

### Aggregate Version

Bir event stream'deki son event sırasını gösteren optimistic concurrency değeridir. Aynı stream ve version ikilisi yalnızca bir kez yazılabilir.

### Projection

Event append işlemiyle aynı yerel transaction içinde event'lerden türetilen, sorgulamaya yönelik ilişkisel durumdur.

### Outbox Message

Kaynak transaction commit edildikten sonra background worker tarafından sahiplenilip yeniden denenebilen dayanıklı yerel workflow talimatıdır.

### Unit of Measure (UoM)

Bir miktarın hangi birimle ifade edildiğini belirleyen, büyük harfe normalize edilmiş koddur. Örnekleri `EA`, `BOX` ve `CASE` olabilir.

### Base UoM

Product ve Inventory bakiyelerinin kanonik olarak tutulduğu temel ölçü birimidir. Diğer UoM miktarları, Product Packaging üzerindeki Conversion Factor ile bu birime çevrilir.

### Problem Details

API hatalarının `RFC 9457` uyumlu temsilidir. Hata türü, başlık, ayrıntı, durum kodu, instance ve correlation ID gibi alanları kayıpsız taşır.

## Authn Terms

### User

Benzersiz ve normalize edilmiş username ile tek yönlü password hash'e sahip, kimliği doğrulanmış aktördür. GUID v7 kimliği JWT `sub` claim'i olur.

### Token Exchange

Username ve password çiftini kısa ömürlü bir JWT ile değiştiren login işlemidir. Refresh token ürün kapsamının dışındadır.

## Product Terms

### Product

SKU ile tanımlanan satılabilir öğedir. Name, değiştirilemez Base UoM, base price/currency ve packaging seçeneklerinin sahibidir.

### Product Packaging

Product'a ait; level, UoM code, Base UoM'a göre Conversion Factor, isteğe bağlı barcode ve fiziksel ölçüler içeren temsildir.

### Level-zero Packaging

Product ile atomik olarak oluşturulan zorunlu packaging kaydıdır. Product'ın Base UoM değerini ve tam olarak bir Conversion Factor değerini kullanır; level, UoM ve factor alanları değiştirilemez.

### Conversion Factor

Bir packaging biriminin kaç Base UoM birimine karşılık geldiğini gösteren pozitif katsayıdır. Price snapshot ve inventory miktar dönüşümlerinde kullanılır.

### Usage Claim

Inventory veya Order işlemi Product verisini doğrularken ya da snapshot alırken tutulan kısa ömürlü ve idempotent Product korumasıdır.

### Logical Deletion

Product'ın event geçmişi ve projection kayıtları korunurken normal sorgulardan gizlendiği terminal durumdur. Stock veya herhangi bir Order referansı bulunan Product silinemez.

## Inventory Terms

### Stock Item

Tek bir Product için, tek warehouse varsayımı altında Base UoM ile tutulan stok bakiyesidir.

### Opening Quantity

Stock Item oluşturulurken client'ın seçtiği UoM ile bildirdiği ilk miktardır. İstenen UoM ve miktar audit amacıyla korunur; bakiye karşılığı Base Quantity olarak hesaplanır.

### Base Quantity

Bir UoM miktarının Conversion Factor uygulanarak Base UoM'a çevrilmiş halidir. On-hand ve reserved bakiye değişimleri bu değerle yapılır.

### On-hand Quantity

Fiziksel olarak elde bulunan toplam Base Quantity değeridir.

### Reserved Quantity

Placed Order kayıtlarına ayrılmış, henüz shipped veya cancelled olmamış Base Quantity değeridir.

### Available Quantity

`OnHandQuantity - ReservedQuantity` olarak hesaplanan kullanılabilir Base Quantity değeridir.

### Stock Movement

Receipt, adjustment, deduction, reservation, commit veya release sonucunda oluşan değiştirilemez inventory ledger kaydıdır. Manual movement, istenen UoM ve quantity değerlerini korurken bakiye delta'ları Product'ın Base UoM değeriyle tutulur. Kaydın Operation ID'si iç idempotency amacıyla tutulur ve public API temsilinde yayınlanmaz.

### Receipt

On-hand Quantity değerini artıran manual Stock Movement'tır.

### Adjustment

`on-hand >= reserved >= 0` invariant'ını koruyarak On-hand Quantity değerini artıran veya azaltan manual Stock Movement'tır.

### Deduction

Bir Order'dan bağımsız olarak reserved olmayan On-hand Quantity değerini azaltan manual Stock Movement'tır.

### Reservation

Order placed olduğunda On-hand Quantity değerini değiştirmeden Reserved Quantity değerini artıran Stock Movement'tır.

### Commit

Order shipped olduğunda On-hand Quantity ve Reserved Quantity değerlerini birlikte azaltan Stock Movement'tır.

### Release

Order cancelled olduğunda veya placement telafi edildiğinde On-hand Quantity değerini değiştirmeden Reserved Quantity değerini azaltan Stock Movement'tır.

## Order Terms

### Order Placement

Order oluşmadan önce yaratılan asynchronous process resource'tur. Pending, Succeeded veya Failed durumundadır ve kararlı reservation ile compensation Operation ID'lerinin sahibidir. Failed durumundaki Failure Code ve ayrıntı metni Order'a aittir: Product ve Inventory başarısızlıkları gateway'deki Anti-corruption Layer'da Order dağarcığına çevrilir.

### Order

Başarılı bir placement'ın ticari sonucudur. JWT subject'e aittir ve değiştirilemez line price snapshot'larını içerir.

### Order Line

Server tarafından hesaplanan unit price (Product base price × seçilen packaging'in Conversion Factor değeri), currency ve teknik Base Quantity ile birlikte tutulan Product, UoM ve quantity bileşimidir.

### Placed

İlk Order durumudur. Ödemenin alındığı varsayılır ve stock reserved kalır.

### Shipped

Reserved Product miktarlarının tamamı commit edildikten sonra ulaşılan terminal Order durumudur.

### Cancelled

Reserved Product miktarlarının tamamı release edildikten sonra ulaşılan terminal Order durumudur.

### Order Transition

Placed Order'ı Shipped veya Cancelled durumuna taşıyan asynchronous process'tir.

### Compensation

Order Placement tamamlanamadığında daha önce başarıyla yapılmış reservation işlemlerini kararlı kimliklerle release ederek iş akışını tutarlı duruma getiren telafi adımıdır.

## Logging Terms

### Request Log

Correlation, süre, sonuç ve maskelenmiş request verisi dahil olmak üzere bir HTTP request'in veya Internal API call'un zorunlu operasyon kaydıdır.

### Sensitive Data Masking

Password, authorization, cookie, JWT ve token değerlerinin audit verisi kalıcılaştırılmadan önce güvenli temsillerle değiştirilmesidir.
