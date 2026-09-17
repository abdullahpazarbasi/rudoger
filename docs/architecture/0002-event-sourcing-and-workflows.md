# ADR 0002: Event sourcing, aynı transaction'da projeksiyon ve outbox iş akışları

## Bağlam

İş durumu denetlenebilir olmalı ve yarış durumları aggregate sürümüyle çözülmelidir. Sipariş oluşturma veya geçişi, başka bir bounded context'in sahibi olduğu stoku da değiştirir; bu nedenle tek bir veritabanı transaction'ı iki bağlamı birden kapsamamalıdır.

## Karar

Authn, Product, Inventory ve Order yalnızca ekleme yapılan (append-only) JSON event zarfları kullanır. Her bağlam, unique `(StreamId, Version)` indeksine sahip jenerik bir `Events` tablosuna sahiptir. Okuma projeksiyonu, event ekleme ile aynı yerel EF Core transaction'ında güncellenir. Optimistic akış çakışmaları HTTP 409 yanıtına dönüşür.

Sipariş oluşturma bir `OrderPlacement` süreci başlatır ve HTTP 202 döndürür. Order outbox worker'ı mesajları atomik olarak kilitler, ürün tekliflerini alır, temel UoM cinsinden stok rezerve eder ve gerçek Order'ı yalnızca tüm rezervasyonlar başarılı olduktan sonra oluşturur. İş hatası rezervasyonları telafi eder ve uç durumlu (terminal) başarısız bir yerleştirme kaydeder. Sevkiyat rezervasyonları commit eder; iptal ise serbest bırakır. Inventory, başarılı yerel commit sonrasında geçici Product kullanım taleplerini serbest bırakmak için kendi outbox'ını kullanır.

Snapshot ve event upcasting bilinçli olarak kapsam dışıdır. Yine de event tipi adları ve şema sürümleri kararlı ve merkezidir.

## Sonuçlar

Her bounded context transaction açısından yalıtık kalır. İstemciler açık pending/succeeded/failed süreç kaynaklarını gözlemler ve `Location` içindeki URL'yi yoklamak zorundadır. Kaynak event ID'leri ve idempotency anahtarları unique olduğu ve domain komutları yeniden oynatmaya duyarlı olduğu için worker'ın en az bir kez (at-least-once) yürütülmesi güvenlidir.
