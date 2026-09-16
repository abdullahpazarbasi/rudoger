# ADR 0004: Idempotency, korelasyon, problem details ve zorunlu denetim günlükleri

- Durum: Kabul edildi
- Tarih: 2026-09-16

## Karar

Yeniden denenebilen komutlar `Idempotency-Key` kabul eder. Tekrarlanan bir anahtar, yalnızca istek anlamı eşleştiğinde özgün sonucu döndürür; farklı girdiyle yeniden kullanım HTTP 409 döndürür. Tüm genel ve dahili operasyonlar `X-Correlation-Id` değerini yayar. Geçersiz veya eksik gelen değerler bir GUID v7 dizesiyle değiştirilir.

Başarısız tüm HTTP yanıtları RFC 9457 `application/problem+json` biçimini kullanır ve `correlationId` içerir. Her HTTP isteği ve dahili API çağrısı `logging.RequestLogs` tablosuna kaydedilir. Kimlik doğrulama sırları, bearer token'lar, cookie'ler ve token biçimli JSON özellikleri maskelenir. İlk HTTP günlüğü kalıcı hale getirilemezse istek HTTP 503 ile kapalı biçimde (fail closed) başarısız olur.

## Sonuçlar

İstemciler desteklenen komutları güvenle yeniden deneyebilir ve operasyonun tamamı boyunca tek bir tanımlayıcı kullanabilir. Denetim kaydının kalıcılığı, tasarım gereği API erişilebilirliğinin parçasıdır. Günlük tamamlama hataları, özgün yanıt çoktan gönderilmiş olabileceği için süreç günlükleyicisi üzerinden raporlanır.
