# CLAUDE.md — JobScanner (Claude Code çalışma kuralları)

Bu dosya Claude Code için kalıcı talimatlardır. Tam tasarım için **`docs/PLAN.md`**'ye bak; bu dosya
o planı ihlal etmeden kodlaman için guardrail'ler ve konvansiyonlar içerir.

## Proje
Türkiye'de yaşayan bir B2B contractor için **tam remote** iş ilanlarını API'lerden çekip, Türkiye'den
gerçekten başvurulabilir olanları eleyip puanlayan bir sistem. Detay: `docs/PLAN.md`.

## Şu anki faz
**Faz 1–5b tamam (v0.6.0).** Çalışan sistem: 5 kaynak (Jobicy, RemoteOK, WWR, Remotive, Arbeitnow) →
dedup → RuleFilter → liveness → LLM fact extraction (prompt v5, cache'li) → saf C# karar + puan →
`JobScanner.Api` + React SPA (`JobScanner.Web`): eşleşme listesi, durum makinesi, materyal üretimi (Faz 4),
arayüzden düzenlenebilir kriter profili + anında recompute (Faz 5a), yetkinlik/dil/tecrübe (Faz 5b).

**Sıradaki iş:** sunucuya kurulum (Docker: `deploy/docker-compose.full.yml`, Oracle VM) → sonra
opsiyonel **Faz 5c** (AI terim normalizasyonu + ağırlık ayarı) → en son **Faz 6** (yarı-otomatik başvuru).
Kapsam ve faz detayları: `docs/PLAN.md` §13. Yeni faza başlamadan önce PLAN'daki ilgili bölümü oku.

## Mutlak kurallar (ASLA ihlal etme)
- **Clean Architecture**, bağımlılık içe doğru: `Worker/Api → Infrastructure → Application → Domain`. Domain'in dış bağımlılığı yoktur.
- İş mantığını (RuleFilter, Decider, Scoring) **saf, yan etkisiz** tut; Infrastructure/Worker'a iş mantığı koyma.
- **DB yalnız PostgreSQL** (Npgsql + EF Core). **SQLite ekleme.**
- **Web scraping yok.** Veri yalnız `IJobSource` API adaptörlerinden gelir.
- **Uygunluk kararını LLM ASLA vermez**; karar/puan saf C#'ta (`IEligibilityDecider` + `ScoringEngine`). LLM iki rolde kullanılır: (1) **fact extractor** (`IEligibilityExtractor`) yapılandırılmış JSON gerçekler döndürür; (2) **Faz 4 materyal üretici** (`IApplicationMaterialGenerator`) ilana özel cover letter + uyarlanmış CV üretir (göndermez, hazırlar). Üretim eleme/puanı etkilemez.
- İlan kimliği = `(SourceName, ExternalId)` + `IdentityKey`; içerik değişimi `VersionHash` ile. **Tüm-içerik hash'ini kimlik olarak kullanma.**
- Cache **ham çıkarılmış gerçekleri** tutar, anahtar `JobId + PromptVersion + ModelVersion + VersionHash`. **Kararı/verdict'i cache'leme.**
- LLM erişimi **Microsoft.Extensions.AI `IChatClient`** üzerinden (sağlayıcı-bağımsız). Tek sağlayıcıyı hard-code etme.
- **Telegram yok.** Tek yüzey web paneli (`JobScanner.Web`). `INotifier` arayüzü olabilir, implementasyonu olmaz.
- Başvuru = kaynağa yönlendirme (`ApplyUrl`/`Url`). Sistem **başvuru göndermez** (Faz 6 gelene kadar; o da kullanıcı onaylı yarı-otomatik).
- Kriterler **güçlü-tipli alanlar** (`CriteriaProfile`; DB'de, arayüzden yapılandırılmış editörle düzenlenir). Generic Target/Operator kural-motoru UI'ı **yapma.**
- Dayanıklılık: **paralel fetch + kaynak başına try-catch + Polly**; toleranslı JSON deserialization (bilinmeyen alanları yok say). Bir kaynak patlayınca run çökmez.
- Versiyonlama: `PromptVersion` + `ModelVersion` sakla; gold-set testlerini yeşil tut.

## Kapsam dışı (şimdilik ekleme — gold-plating yapma)
Redis bağımlılığı · Semantic Kernel / MCP / agent · audit log · rate-limiting · prompt-injection sertleştirmesi ·
`IJobEnricher` gerçek implementasyonu (yalnız arayüz) · feedback ML · ATS kaynakları. Bunlar yalnız `docs/PLAN.md`'deki ilgili ileri faz gelince eklenir.

## Teknoloji
.NET 10 (LTS) · EF Core + Npgsql · Microsoft.Extensions.AI (`IChatClient`) · AngleSharp (HTML→metin) ·
Polly · xUnit. `<Nullable>enable</Nullable>`, en güncel C#. (İleride: Microsoft `RulesEngine`.)

## Kod konvansiyonları
- DTO/value object'ler `record`; sınıflar varsayılan `sealed`.
- Baştan sona `async` + I/O'da `CancellationToken`.
- İmzalarda `IReadOnlyList`/`IReadOnlyCollection`.
- DI constructor injection; kayıt Worker composition root'ta.
- Dosya başına tek tip; katman içi klasörler (Sources, Filtering, Persistence, Llm).
- Config `IOptions<>` ile bağlanır; magic string yok.
- Yapısal log (`ILogger`); her run sonunda metrik satırı (fetched/new+changed/eliminated/extracted/matches).
- Test: saf mantık (Normalizer, RuleFilter, Decider, Scoring) unit; extraction gold-set ile.

## Commit & sürüm konvansiyonu
- **Conventional Commits + release-please.** `feat`/`fix`/`docs`/`chore`/`ci`/`refactor`/`perf`/`test`; `feat!:` veya `BREAKING CHANGE:` major bump.
- **Commit mesajı: İngilizce başlık (subject) + Türkçe gövde (body).** CHANGELOG başlıktan üretilir; İngilizce başlık sürüm notlarını uluslararası okunur kılar, Türkçe ayrıntı gövdede kalır.
  ```
  feat: job-tailored CV + cover letter generation

  Faz 4: yüksek skorlu ilanlara on-demand başvuru materyali.
  - IApplicationMaterialGenerator (LLM) + MaterialService
  ```
- `feat`/`fix` etiketini doğru seç (sürüm artışını etkiler). Geçmiş commit'leri yeniden yazma.

## Çözüm yapısı
`docs/PLAN.md` §3. Özet: `src/{Domain, Application, Infrastructure, Worker, Api, Web}`, `tests/{UnitTests, GoldSetTests}`, `deploy/`.

## Komutlar
```bash
# Build & test
dotnet build
dotnet test

# Worker'ı çalıştır (tek tarama: Pipeline__RunOnce=true)
dotnet run --project src/JobScanner.Worker

# API (:5163) + SPA (:5173)
dotnet run --project src/JobScanner.Api
cd src/JobScanner.Web && npm run dev      # test: npm test · build: npm run build

# EF Core migration
dotnet ef migrations add <Name> -p src/JobScanner.Infrastructure -s src/JobScanner.Worker
dotnet ef database update    -p src/JobScanner.Infrastructure -s src/JobScanner.Worker

# Lokal Postgres (yalnız DB)
docker compose -f deploy/docker-compose.yml up -d

# Tam yığın (postgres + ollama + worker + api + web) — sunucu kurulumu, bkz. docs/REHBER.md §10
docker compose -f deploy/docker-compose.full.yml --env-file deploy/.env up -d --build
```
Secret'lar (DB connection, LLM api key) env var / `dotnet user-secrets` ile; repo'ya girmez.

## Kabul kontrolü (her pipeline değişikliğinden sonra)
`dotnet test` yeşil (unit + gold-set). Worker'ı **iki kez** çalıştır → ikinci sefer hiçbir ilan yeniden işlenmemeli (dedup doğrulaması). Sonuçlar Postgres'te + metrikler logda.
