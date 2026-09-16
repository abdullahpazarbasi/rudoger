# ADR 0001: Süreç içi dahili API'lere sahip modüler monolit

- Durum: Kabul edildi
- Tarih: 2026-09-16

## Bağlam

Authn, Product, Inventory, Order ve Logging birbirinden bağımsız dil ve sahiplik sınırlarına sahiptir. Assignment, bounded context'lerin API'ler üzerinden haberleşmesini isterken aynı dağıtılabilir birim içindeki çağrılarda HTTP kullanılmasından kaçınılmasını gerektirir.

## Karar

Rudoger bir modüler monolittir. Her bounded context kendi Domain, Application, Infrastructure ve Presentation projelerine sahiptir. Tüketici bir Infrastructure projesi, başka bir bağlamın Presentation sözleşmesine referans verebilir ve onu süreç içinde çağırabilir. Domain ve Application projeleri hiçbir zaman başka bir bounded context'e referans vermez.

Mimari testler bu bağımlılık yönlerini zorlar. Dahili çağrılar ortam korelasyon bağlamını taşır ve istek günlüğüne `Internal` kanalıyla yazılır.

## Sonuçlar

API tek bir süreç olarak dağıtılır ve bağlam başına transaction özerkliğini korur. Entegrasyon açık sözleşmeler üzerinden yapıldığı için modül ayrıştırma olanağı korunur. Mevcut modüller arasında dağıtık ağ hatası yoktur; ancak her bağlam bağımsız commit ettiği için iş akışları yine de yeniden deneme ve idempotency modeller.
