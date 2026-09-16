# ADR 0003: Şema sahipliğine dayalı tek SQL Server veritabanı

- Durum: Kabul edildi
- Tarih: 2026-09-16

## Karar

Uygulama tek bir Microsoft SQL Server veritabanı kullanır. Her bounded context bir şemaya (`authn`, `product`, `inventory`, `order` veya `logging`) ve bağımsız bir EF Core migration geçmişi tablosuna sahiptir. Foreign key'lere yalnızca aynı şema içinde izin verilir. Bağlamlar arası tanımlayıcılar düz GUID v7 değerleridir ve dahili API'ler üzerinden doğrulanır.

Veritabanı kısıtları yapısal kalır: birincil anahtarlar, sınırlı zorunlu sütunlar, indeksler, unique indeksler, hassasiyet ve bağlam içi foreign key'ler. İş kararları aggregate'lerde ve uygulama servislerinde yaşar.

## Sonuçlar

Operasyonel dağıtım basit kalırken bağlam sahipliği görünür ve test edilebilir olur. Bağlamlar arası referans bütünlüğü, yasak şema bağlaşımı yerine iş akışları tarafından sağlanır.
