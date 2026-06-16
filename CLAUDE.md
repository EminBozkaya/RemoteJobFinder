# CLAUDE.md — JobScanner (Claude Code çalışma kuralları)

Bu dosya Claude Code için kalıcı talimatlardır. Tam tasarım için **`docs/PLAN.md`**'ye bak; bu dosya
o planı ihlal etmeden kodlaman için guardrail'ler ve konvansiyonlar içerir.

## Proje
Türkiye'de yaşayan bir B2B contractor için **tam remote** iş ilanlarını API'lerden çekip, Türkiye'den
gerçekten başvurulabilir olanları eleyip puanlayan bir sistem. Detay: `docs/PLAN.md`.

## Şu anki faz
**Faz 1 — Çekirdek.** Kapsam ve sıra: `docs/PLAN.md` §14; bitti kriteri: §15. Faz 1'de **LLM yok, UI yok**.

## Mutlak kurallar (ASLA ihlal etme)
- **Clean Architecture**, bağımlılık içe doğru: `Worker/Api → Infrastructure → Application → Domain`. Domain'in dış bağımlılığı yoktur.
- İş mantığını (RuleFilter, Decider, Scoring) **saf, yan etkisiz** tut; Infrastructure/Worker'a iş mantığı koyma.
- **DB yalnız PostgreSQL** (Npgsql + EF Core). **SQLite ekleme.**
- **Web scraping yok.** Veri yalnız `IJobSource` API adaptörlerinden gelir.
- **LLM yalnız fact extractor** (`IEligibilityExtractor`), yapılandırılmış JSON gerçekler döndürür. Uygunluk **kararını LLM vermez**; karar saf C#'ta `IEligibilityDecider`'dadır.
- İlan kimliği = `(SourceName, ExternalId)` + `IdentityKey`; içerik değişimi `VersionHash` ile. **Tüm-içerik hash'ini kimlik olarak kullanma.**
- Cache **ham çıkarılmış gerçekleri** tutar, anahtar `JobId + PromptVersion + ModelVersion + VersionHash`. **Kararı/verdict'i cache'leme.**
- LLM erişimi **Microsoft.Extensions.AI `IChatClient`** üzerinden (sağlayıcı-bağımsız). Tek sağlayıcıyı hard-code etme.
- **Telegram yok**, Faz 1-2'de **web UI yok.** `INotifier` arayüzü olabilir, implementasyonu olmaz.
- Başvuru = kaynağa yönlendirme (`ApplyUrl`/`Url`). Sistem **başvuru göndermez.**
- Kriterler şimdilik **güçlü-tipli config alanları.** Dinamik Target/Operator kriter UI'ı **yapma.**
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

## Çözüm yapısı
`docs/PLAN.md` §3. Özet: `src/{Domain, Application, Infrastructure, Worker}` (+ ileride `Api`, `Web`), `tests/`, `deploy/`.

## Komutlar
```bash
# Build & test
dotnet build
dotnet test

# Worker'ı çalıştır
dotnet run --project src/JobScanner.Worker

# EF Core migration
dotnet ef migrations add <Name> -p src/JobScanner.Infrastructure -s src/JobScanner.Worker
dotnet ef database update    -p src/JobScanner.Infrastructure -s src/JobScanner.Worker

# Lokal Postgres
docker compose -f deploy/docker-compose.yml up -d
```
Secret'lar (DB connection, LLM api key) env var / `dotnet user-secrets` ile; repo'ya girmez.

## Faz 1 kabul kontrolü
Worker'ı **iki kez** çalıştır → ikinci sefer hiçbir ilan yeniden işlenmemeli (dedup doğrulaması). Sonuçlar Postgres'te + metrikler logda.
