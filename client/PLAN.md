# Rudoger Browser GUI Uygulama Planı

## 1. Amaç ve sınırlar

Rudoger Web API için Türkçe, yalın ve minimal bir browser GUI geliştirilecek. İstemci bir pazarlama sayfası değil; ilk ekranda doğrudan ürün, stok ve sipariş işlemlerine erişilen bir çalışma yüzeyi olacak.

- Tüm uygulama kaynakları, yapılandırması, testleri ve dokümantasyonu `client/` altında tutulacak.
- Mevcut .NET solution, modüller, Compose stack’i, CI ve kök dokümanlar değiştirilmeyecek.
- Gerekirse yalnızca ignore yapılandırmalarına dokunulabilecek; öncelik `client/.gitignore` kullanmak olacak.
- İstemci API’nin mevcut REST/OpenAPI sözleşmesini kullanacak.
- Logging BC için istemci ekranı yapılmayacak; bu BC’nin dışarı açılmış bir sorgu endpoint’i bulunmuyor.
- Desteklenmeyen arama, sıralama veya raporlama davranışları tüm veriyi çekip istemcide taklit edilmeyecek.
- Uygulama sırasında yeni bir API endpoint’ine ihtiyaç ortaya çıkarsa API tarafında değişiklik yapılmadan önce kullanıcıya sorulacak.
- Şu anki sözleşmeyle yeni endpoint gerekmiyor.

## 2. Teknoloji tabanı

16 Eylül 2026 tarihinde doğrulanan güncel kararlı sürümler başlangıç tabanı olacak. Uygulamaya başlanırken sürümler yeniden doğrulanacak, kararlı sürümler exact olarak sabitlenecek ve `package-lock.json` commit kapsamına alınacak. Manifestte `latest` veya kontrolsüz sürüm aralığı bırakılmayacak.

- Çalışma ortamı: Node.js `24.21.0` LTS ve npm `11.19.0`. [Node.js sürüm kaydı](https://nodejs.org/download/release/latest-v24.x/)
- Temel: React `19.3.0`, React DOM `19.3.0`, TypeScript `5.9.2` (OpenAPI araç zincirinin desteklediği en güncel 5.x), Vite `8.3.0`, `@vitejs/plugin-react` `6.1.1`. [React](https://www.npmjs.com/package/react?activeTab=versions), [TypeScript](https://www.npmjs.com/package/typescript?activeTab=versions), [Vite](https://www.npmjs.com/package/vite?activeTab=versions)
- Stil: Tailwind CSS ve `@tailwindcss/vite` `4.3.3`, Prettier `3.9.7`, `prettier-plugin-tailwindcss` `0.8.1`. [Tailwind CSS](https://www.npmjs.com/package/tailwindcss?activeTab=versions), [Prettier](https://www.npmjs.com/package/prettier?activeTab=versions)
- Reaktif sunucu durumu: TanStack React Query `5.102.8`. [TanStack React Query](https://www.npmjs.com/package/%40tanstack/react-query?activeTab=versions)
- Rotalama: React Router `8.4.0`. [React Router](https://www.npmjs.com/package/react-router)
- Formlar: React Hook Form `7.88.0`, `@hookform/resolvers` `5.9.1`, Zod `4.6.5`. [React Hook Form](https://www.npmjs.com/package/react-hook-form?activeTab=versions), [Zod](https://www.npmjs.com/package/zod?activeTab=versions)
- API sözleşmesi: `openapi-typescript` `7.13.0` ve `openapi-fetch` `0.17.0`. [OpenAPI TypeScript](https://www.npmjs.com/package/openapi-typescript?activeTab=versions), [OpenAPI Fetch](https://www.npmjs.com/package/openapi-fetch)
- Erişilebilir modal ve snackbar: Radix Dialog `1.1.23`, Sonner `2.0.8`. [Radix Dialog](https://www.npmjs.com/package/%40radix-ui/react-dialog), [Sonner](https://www.npmjs.com/package/sonner?activeTab=versions)
- Test: Vitest `5.0.1`, MSW `2.15.0`, Testing Library React `16.3.3`, User Event `14.6.7`, Jest DOM `7.0.1` ve jsdom `30.0.1`. [Vitest](https://www.npmjs.com/package/vitest), [MSW](https://www.npmjs.com/package/msw?activeTab=versions), [Testing Library](https://www.npmjs.com/search?q=%40testing-library%2Freact)
- Statik analiz: ESLint `10.10.0`, typescript-eslint `8.70.0` ve React Hooks ESLint eklentisi `7.1.1`.

## 3. Dizin ve mimari

```text
client/
  openapi/
    rudoger-v1.json
  scripts/
    sync-openapi.mjs
  src/
    api/
      generated/
      client/
      errors/
      query-keys/
    app/
      providers/
      router/
      layout/
    features/
      authn/
      products/
      inventory/
      orders/
    shared/
      components/
      forms/
      theme/
      formatting/
      accessibility/
    test/
  .env
  .env.dist
  .gitignore
  .npmrc
  package.json
  package-lock.json
  tsconfig*.json
  vite.config.ts
  vitest.config.ts
  eslint.config.js
  PLAN.md
```

- `strict`, `noUncheckedIndexedAccess` ve benzeri güvenli TypeScript seçenekleri açılacak.
- API verisi, auth durumu, tema tercihi ve geçici form durumu birbirinden ayrılacak.
- Sunucudan gelen veriler yerel state’e kopyalanmayacak; React Query cache’i tek kaynak olacak.
- Query key’leri, hata çevirileri, API client ve tema/auth state makineleri merkezî tutulacak.
- Özellikler birbirlerinin iç uygulamalarına bağımlı olmayacak; ortak sözleşmeler `api/` ve `shared/` üzerinden kullanılacak.

## 4. API sözleşmesi ve bağlantı

- `/openapi/v1.json` belgesi `client/openapi/rudoger-v1.json` içine kontrollü bir snapshot olarak alınacak.
- `api:sync` snapshot’ı güncelleyecek ve TypeScript API tiplerini yeniden üretecek.
- `api:check`, committed snapshot ile generated tiplerin uyumlu olduğunu çevrimdışı doğrulayacak.
- Tüm çağrılar tek bir `openapi-fetch` client üzerinden geçecek.
- Her isteğe yeni `X-Correlation-Id` eklenecek; yanıt ve problem details içindeki correlation ID korunacak.
- Yeniden denenebilir komutlarda `Idempotency-Key`, aynı kullanıcı niyeti ve payload boyunca sabit tutulacak. Yeni işlem niyetinde yeni anahtar üretilecek.
- Development sırasında Vite proxy `/api`, `/health` ve `/openapi` yollarını varsayılan olarak `http://localhost:8080` adresine yönlendirecek.
- API’de CORS yapılandırması bulunmadığı için yalıtılmış geliştirmede aynı-origin proxy kullanılacak. Ayrı-origin production yayını daha sonra istenirse reverse proxy veya API CORS kararı ayrıca alınacak.

## 5. Rotalar ve use case’ler

| Rota | Use case’ler |
| --- | --- |
| `/giris` | Kullanıcı adı/parola ile token exchange |
| `/urunler` | Sayfalama, ID listesiyle filtreleme, ürün ekleme |
| `/urunler/:productId` | Ürün görüntüleme, RFC 6902 patch, silme, packaging listeleme/ekleme/yamama/silme |
| `/stok` | Stok kayıtlarını sayfalama ve ürün ID ile filtreleme, stok açma |
| `/stok/:stockItemId` | Bakiye görüntüleme, receipt/adjustment/deduction, hareket defteri |
| `/siparisler` | Sipariş listeleme ve çok satırlı sipariş oluşturma |
| `/siparisler/:orderId` | Sipariş, fiyat snapshot’ları ve geçiş durumu; shipped/cancelled geçişi |

Domain uyarlamaları:

- Ürün oluşturma formu zorunlu level-zero packaging satırını kendisi oluşturacak.
- Level-zero packaging’in level, UoM ve conversion factor alanları değiştirilemeyecek ve satır silinemeyecek.
- Product patch yalnızca değişen alanlardan `replace` operasyonları üretecek.
- Stok hareket formu yalnızca `Receipt`, `Adjustment` ve `Deduction` sunacak. Order’a ait reserved/committed/released hareketleri salt okunur gösterilecek.
- Sipariş satırlarında ürün ve ilgili packaging UoM seçilecek; miktar pozitif, Product/UoM ikilisi benzersiz ve satır sayısı 1–100 olacak.
- Farklı para birimleri daha submit öncesinde gösterilecek; sunucu doğrulaması yine nihai otorite olacak.
- Order placement `Pending` iken reaktif olarak sorgulanacak; `Succeeded` veya `Failed` olduğunda polling duracak.
- Order transition `Pending` iken sorgulanacak, `Succeeded` olduğunda ilgili sipariş ve stok query’leri invalidate edilecek.

## 6. Reaktif state yaklaşımı

- Sunucu state’i, cache, polling, iptal ve mutation invalidation işlemleri React Query ile yönetilecek.
- Auth, `anonymous | authenticated | expired` durumlarına sahip typed bir state machine olacak.
- Auth store `useSyncExternalStore` ile tüketilecek; aynı bilgiyi birden fazla Context veya component state’inde tutan paralel yollar oluşturulmayacak.
- Formlar React Hook Form’un alan bazlı subscription modeliyle çalışacak.
- Tema store’u `system | light | dark` değerine sahip olacak ve `matchMedia` değişikliklerini reaktif izleyecek.
- Placement/transition polling yalnızca süreç `Pending` olduğu sürece etkin kalacak.
- `AbortSignal` route değişimi ve query iptalinde gerçek fetch çağrısına aktarılacak.

## 7. JWT süresi dolma davranışı

- Token yalnızca `sessionStorage` ve bellek store’unda tutulacak; refresh token veya cookie/BFF mekanizması eklenmeyecek.
- JWT’nin `exp` claim’i üzerinden tek bir expiry zamanlayıcısı kurulacak; sekme yeniden görünür olduğunda süre tekrar değerlendirilecek.
- Korunan bir çağrıdan gelen `401` de session durumunu `expired` yapacak.
- Süre dolduğunda mevcut rota korunacak ve otomatik `/giris` yönlendirmesi yapılmayacak.
- Korunan mutation formlarındaki submit butonları disabled olacak. Login formu bu global engelden etkilenmeyecek.
- Ekranda kalıcı bir snackbar gösterilecek: “Oturumunuzun süresi doldu.”
- Snackbar’daki “Yeniden Giriş Yap” butonuna basılınca mevcut rota `returnTo` olarak korunacak, token temizlenecek ve login sayfasına gidilecek.
- Mutation katmanı da expired token ile istek göndermeyi reddedecek; disabled buton tek güvenlik çizgisi olmayacak.

## 8. Health gate

- Uygulama açılışında anonim `/health` sorgusu yapılacak.
- Yalnızca HTTP başarı durumu ve gövdedeki `status === "Healthy"` birlikte sağlıklı kabul edilecek.
- `Unhealthy`, `Degraded`, ağ hatası, timeout veya geçersiz response durumunda kapatılamayan erişilebilir bir modal açılacak.
- Modal Escape, backdrop veya close ikonu ile kapatılamayacak.
- “Yeniden Erişmeyi Dene” butonu yeni bir health talebi başlatacak.
- Talep sağlıklı dönene kadar modal açık ve uygulama işlemleri bloke kalacak.
- Health retry yalnızca kullanıcı eylemiyle yapılacak; görünmez sonsuz otomatik retry döngüsü kurulmayacak.
- Normal API çağrılarındaki bağlantı hataları health query’sini invalidate ederek aynı gate’in yeniden değerlendirilmesini sağlayacak.

## 9. Hata modeli ve Türkçeleştirme

Tek merkezî `ClientError` union kullanılacak:

- RFC 9457 problem details
- Model validation hataları
- Ağ/timeout/abort hataları
- JSON veya sözleşme çözümleme hataları
- Browser/render hataları
- Bilinmeyen hatalar

Davranış:

- HTTP status, problem type/code, title, detail, instance, correlation ID ve alan hataları kaybedilmeyecek.
- Mevcut API’deki problem type’ları, domain/conflict kodları ve dinamik mesaj şablonları typed Türkçe hata kataloğunda karşılanacak.
- Form alanı hataları ilgili alanın yanında ve formun hata özetinde gösterilecek.
- İşlem özeti snackbar’da, tüm ayrıntılar erişilebilir detay panelinde gösterilecek.
- Bilinmeyen sunucu mesajı uydurma bir çeviriyle değiştirilmeden; Türkçe bağlam açıklamasıyla birlikte özgün teknik metin olarak korunacak.
- Production’da kullanıcının görmediği bir `console.error` yoluna güvenilmeyecek.
- Error boundary; hata adı, Türkçe açıklama ve mevcut teknik ayrıntıları gösterecek.

## 10. Görsel tasarım ve erişilebilirlik

- Görsel yön: nötr gri yüzeyler, tek vurgu rengi, sistem fontları, az gölge ve az radius.
- Pazarlama hero’su, dekoratif görsel, gereksiz dashboard kartları veya animasyon kullanılmayacak.
- Ana navigasyon üst çubukta Ürünler, Stok ve Siparişler olarak yer alacak.
- Masaüstünde kompakt tablolar; dar ekranda okunabilir satır/card dönüşümü kullanılacak.
- “Sistem | Açık | Koyu” kontrolü erişilebilir bir segmented radio group olacak.
- Sistem modu işletim sistemi değişikliklerini canlı izleyecek; seçim `localStorage` içinde saklanacak.
- Focus görünürlüğü, klavye kullanımı, modal focus trap, reduced-motion, semantik başlık sırası ve en az WCAG AA kontrastı doğrulanacak.
- Ana metin en az `1rem`, sürekli kullanılan kontrol etiketleri en az `0.875rem` olacak.

## 11. Test ve kalite kapıları

Vitest, Testing Library ve MSW ile en az şu davranışlar kapsanacak:

- JWT zamanında expiry durumuna geçme
- Expire olduğunda yönlendirme yapılmaması
- Korumalı submit butonlarının disabled olması
- Snackbar eylemiyle login’e yönlenme ve `returnTo`
- Health modalının sağlıklı yanıt gelmeden kapanmaması
- Problem details ve validation hatalarının eksiksiz Türkçeleştirilmesi
- Correlation ve idempotency anahtarlarının doğru yaşam döngüsü
- Tema tercihi ve sistem teması değişikliği
- RFC 6902 patch üretimi
- Level-zero packaging kuralları
- Stok hareket doğrulamaları
- Sipariş satırı ve para birimi doğrulamaları
- Placement/transition polling ve terminal durumda durma
- Mutation sonrası hedefli query invalidation
- Klavye ve temel erişilebilirlik davranışları

Kalite komutları:

```text
npm run api:check
npm run typecheck
npm run lint
npm run format:check
npm run test:coverage
npm run build
npm run check
```

- Genel istemci line ve branch coverage eşiği en az `%90` olacak.
- Auth expiry, health gate ve hata dönüştürücü modülleri `%100` branch coverage ile korunacak.
- `npm run check` ağ bağlantısı veya çalışan API gerektirmeden tamamlanabilecek.
- Gerçek API ile ayrıca login → product → stock → order → transition smoke akışı doğrulanacak.

## 12. Tamamlanma ölçütleri

Plan ancak aşağıdakilerin tamamı sağlandığında uygulanmış kabul edilecek:

- Değişiklikler izin verilen ignore dosyaları dışında `client/` dizininden taşmıyor.
- Mevcut API endpoint’lerinin istemciye uygun tüm use case’leri erişilebilir durumda.
- Yeni API endpoint’i veya server davranışı sessizce varsayılmamış.
- JWT expiry ve health gate davranışları istenen kullanıcı akışına tam uyuyor.
- API ve istemci hatalarının hiçbir ayrıntısı kaybedilmiyor.
- Tema kontrolü üç modda çalışıyor.
- OpenAPI snapshot ve generated tipler uyumlu.
- Typecheck, lint, Prettier, test/coverage ve production build temiz geçiyor.
- Mevcut .NET proje dosyaları, Compose yapısı ve dokümanları değiştirilmemiş.
