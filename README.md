# 🏦 BankApp — ASP.NET Core Bankacılık Uygulaması

Müşteri, hesap, IBAN, para transferi ve hesap hareketleri yönetimi sunan; katmanlı mimari ile geliştirilmiş bir **ASP.NET Core MVC** bankacılık uygulaması.

> Basit bir arayüz, detaylı bir backend: iş kuralları servis katmanında, tüm para hareketleri veritabanı transaction'ı içinde ve birim testlerle doğrulanmış şekilde çalışır.

## ✨ Özellikler

- **Müşteri kaydı ve girişi** — T.C. kimlik numarası algoritma kontrolü, PBKDF2 ile şifre hash'leme, cookie tabanlı kimlik doğrulama
- **Çoklu hesap** — Her müşteri birden fazla hesap açabilir (limit konfigürasyondan)
- **Benzersiz IBAN** — Her hesaba ISO 13616 / MOD-97 kontrol haneli, geçerli formatta `TR` IBAN üretilir; veritabanında unique index ile korunur
- **Para transferi**
  - Kendi hesapları arası: **0,00 ₺** komisyon
  - Başka müşterinin hesabına: **3,99 ₺** komisyon (`appsettings.json` üzerinden ayarlanabilir)
  - Form → Onay ekranı → Dekont akışı, alıcı adı maskeleme (`Ay** De***`)
- **Hesap hareketleri (ekstre)** — Giden/gelen transfer, para yatırma ve **komisyon ayrı satır** olarak; her satırda işlem sonrası bakiye
- **Transfer geçmişi** — Gelen ve giden tüm transferler, referans numarasıyla

## 🧱 Mimari

```
BankApp.slnx
├── src
│   ├── BankApp.Core       → Entity'ler, enum'lar, özel exception'lar, IBAN / TCKN yardımcıları
│   ├── BankApp.Data       → EF Core DbContext, Fluent API konfigürasyonları, migration'lar
│   ├── BankApp.Services   → İş kuralları (CustomerService, AccountService, TransferService), DTO'lar
│   └── BankApp.Web        → ASP.NET Core MVC (Controller'lar, View'lar, ViewModel'ler)
└── tests
    └── BankApp.Tests      → xUnit birim testleri (in-memory SQLite)
```

Bağımlılık yönü: `Web → Services → Data → Core`

### Veri modeli

| Tablo | Açıklama |
|---|---|
| `Customers` | Müşteri bilgileri (TCKN ve e-posta unique) |
| `Accounts` | Müşteriye ait hesaplar (IBAN unique, bakiye `decimal(18,2)`) |
| `Transfers` | Transfer kaydı: gönderen, alıcı, tutar, komisyon, referans no |
| `AccountTransactions` | Hesap hareketleri: işaretli tutar (+/-), işlem sonrası bakiye, karşı taraf |

### Transfer akışı (`TransferService.TransferAsync`)

1. Veritabanı transaction'ı başlatılır
2. Tutar (pozitif, en fazla 2 ondalık, üst limit), IBAN (format + MOD-97) ve hesap sahipliği doğrulanır
3. Alıcı aynı müşteriyse komisyon 0, değilse 3,99 ₺ hesaplanır
4. `tutar + komisyon` için bakiye kontrolü yapılır
5. Gönderenden tutar düşülür → *Giden Transfer* hareketi
6. Komisyon varsa ayrıca düşülür → *Komisyon* hareketi
7. Alıcıya tutar eklenir → *Gelen Transfer* hareketi
8. Commit — herhangi bir adımda hata olursa hiçbir değişiklik kalıcı olmaz

## 🛠️ Teknolojiler

- .NET 10, ASP.NET Core MVC
- Entity Framework Core 10 + SQLite (Code-First, Migrations)
- ASP.NET Core Cookie Authentication, `PasswordHasher` (PBKDF2)
- Bootstrap 5
- xUnit

## 🚀 Çalıştırma

**Gereksinim:** [.NET 10 SDK](https://dotnet.microsoft.com/download) veya Visual Studio 2022 (17.14+) / 2026

**Visual Studio ile:** `BankApp.slnx` dosyasını açın, `BankApp.Web` projesini başlangıç projesi yapın ve F5'e basın.

**Komut satırı ile:**

```bash
dotnet run --project src/BankApp.Web
```

Uygulama ilk açılışta veritabanını (`bankapp.db`) otomatik oluşturur ve demo veriyi ekler.

| Demo kullanıcı | Şifre |
|---|---|
| `ahmet@demo.com` | `Demo123!` |
| `ayse@demo.com` | `Demo123!` |

## ✅ Testler

```bash
dotnet test
```

Test edilen senaryolardan bazıları:
- Kendi hesabına transferde komisyon alınmaması, başka müşteriye 3,99 ₺ alınması
- Komisyonun hesap hareketlerinde ayrı satır olarak görünmesi
- Komisyon dahil yetersiz bakiyede işlemin tamamen geri alınması
- Başka müşterinin hesabından transfer / hareket görüntüleme denemesinin engellenmesi
- Üretilen IBAN'ların geçerli ve benzersiz olması, TCKN algoritma doğrulaması
