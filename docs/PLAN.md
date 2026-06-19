# Remote İş İlanı Sistemi — İmplementasyon Planı (final)

> Bu doküman, Claude Code ile kodlamaya başlanacak seviyede teknik referanstır. Önceki tüm
> dokümanların (Brief, Mimari v1/v2, Master Plan) **yerini alır** ve 4 yapay zekâ değerlendirmesinin
> sentezini + alınan nihai kararları içerir.

---

## 1. Sabit Kararlar (artık tartışmaya kapalı)

1. **Sadece tam remote** işler. (Hibrit/onsite kapsam dışı.)
2. **Veri:** API'lerden yapılandırılmış çekim; **web scraping yok.**
3. **DB:** Her ortamda (lokal + sunucu) **Docker'da PostgreSQL**. **SQLite yok** (çift-DB/migration/FTS derdini ortadan kaldırır). **Redis** stack'te hazır ama Faz 1-2'de bağımlılık kurulmaz.
4. **Telegram yok.** Tek yüzey web paneli (Faz 3). `INotifier` soyutlaması durur; ileride istenirse e-posta digest.
5. **Başvuru:** Orijinal kaynağa **yönlendirme**; sistem başvuru göndermez. Durum kullanıcı beyanıyla takip edilir.
6. **LLM = fact extractor, karar verici değil.** LLM ilandan yapılandırılmış gerçekler çıkarır; eleme/karar C# kural motorundadır.
7. **Kriter motoru:** Backend'de esnek ama başlangıçta **güçlü-tipli** alanlarla; dinamik Target/Operator UI yok.

---

## 2. Mimari İlkeler

- **Clean Architecture**, bağımlılık içe doğru: `Worker/Api → Infrastructure → Application → Domain`.
- **Kimlik ≠ içerik versiyonu:** İlan kimliği `Source + ExternalId` ve normalize bileşik anahtar; içerik değişimi `VersionHash` ile yakalanır.
- **Cache, kararı değil ham gerçeği tutar:** LLM çıktısı `JobId + PromptVersion + ModelVersion + VersionHash` ile cache'lenir; uygunluk **kararı** her profil için bu gerçeklerden ucuzca C#'ta hesaplanır.
- **Saf fonksiyon + test edilebilirlik:** RuleFilter, Decider, ScoringEngine yan etkisiz; LLM/DB/HTTP kenarda.
- **Versiyonlama 1. günden:** Prompt/Model versiyonları + ~20 ilanlık "gold dataset" regresyon testi.

---

## 3. Çözüm Yapısı

```
JobScanner.sln
├── src/
│   ├── JobScanner.Domain/          # entity / value object / enum — bağımlılık yok
│   ├── JobScanner.Application/      # portlar (interface) + pipeline orchestration
│   ├── JobScanner.Infrastructure/  # EF Core(Npgsql), API client, LLM(IChatClient), (Redis ileride)
│   ├── JobScanner.Worker/          # arka plan tarayıcı host (Faz 1-2)
│   ├── JobScanner.Api/             # ASP.NET Core Web API (Faz 3)
│   └── JobScanner.Web/             # React + Vite + TS (Faz 3)
├── tests/
│   ├── JobScanner.UnitTests/        # RuleFilter, Decider, Scoring, Normalizer
│   └── JobScanner.GoldSetTests/     # prompt regresyon (extraction doğruluğu)
└── deploy/
    └── docker-compose.yml           # postgres (+ redis), worker
```

---

## 4. Domain Modeli

```csharp
public enum WorkMode       { Remote }                       // şimdilik tek değer
public enum MatchState     { New, Saved, Opened, Applied, Dismissed, Expired }
public enum Decision       { Eligible, Ineligible, Uncertain }
public enum EngagementType { Unknown, Employee, Contractor, B2B, Freelance, EmployeeViaEor }
public enum JobStatus      { Active, Archived }

public sealed record RawJob(
    string SourceName, string ExternalId, string Title, string Company,
    string DescriptionHtml, string Url, string? ApplyUrl,
    string? PostedAtRaw, IReadOnlyDictionary<string, object?> Extra);

public sealed class JobPosting
{
    public long    Id              { get; init; }            // surrogate PK
    public required string SourceName  { get; init; }
    public required string ExternalId  { get; init; }
    public required string IdentityKey { get; init; }        // normalize bileşik anahtar (çapraz-kaynak dedup)
    public required string Title       { get; init; }
    public required string Company     { get; init; }
    public required string DescriptionText { get; init; }    // HTML temizlenmiş
    public required string Url         { get; init; }
    public string?  ApplyUrl       { get; init; }            // yönlendirme hedefi (varsa)
    public WorkMode WorkMode       { get; init; } = WorkMode.Remote;
    public DateTimeOffset? PostedAt { get; init; }
    public DateTimeOffset  FirstSeenAt { get; init; }
    public DateTimeOffset  LastSeenAt  { get; init; }
    public DateTimeOffset? ExpiryDate  { get; init; }
    public required string VersionHash { get; init; }        // içerik değişimi tespiti
    public string   SourceExtraJson { get; init; } = "{}";   // jsonb — heterojen API alanları
    public JobStatus Status        { get; init; } = JobStatus.Active;
}

// CACHE — LLM'in çıkardığı HAM GERÇEKLER (karar değil)
public sealed record EligibilityFacts(
    long JobId, string PromptVersion, string ModelVersion, string VersionHash,
    bool? RequiresWorkAuth, IReadOnlyList<string>? AllowedCountries, bool? RequiresCitizenship,
    bool? AllowsB2BContractor, EngagementType EngagementType,
    bool? MentionsEor, string? EorPlatform, string? DataBoundary,
    string? TimezoneRequirementRaw,            // ör. "EST 9-5", "CET core hours"
    bool? IsRecruiterAgency, double Confidence, DateTimeOffset ExtractedAt, string RawJson);

public sealed class UserJobMatch                // (ProfileId, JobId)
{
    public required long ProfileId { get; init; }
    public required long JobId     { get; init; }
    public double  Score           { get; set; }
    public string  ScoreBreakdownJson { get; set; } = "[]"; // [{criterion, contribution}]
    public Decision Decision       { get; set; }            // C#'ta facts'ten hesaplanır
    public string  DecisionReasonsJson { get; set; } = "[]";
    public MatchState State        { get; set; } = MatchState.New;
    public string? Feedback        { get; set; }            // GoodFit | BadFit | null
    public DateTimeOffset? OpenedAt { get; set; }
    public DateTimeOffset? AppliedAt { get; set; }
    public DateTimeOffset  CreatedAt { get; init; }
}
```

`User` ve `CriteriaProfile` (1:N) tabloları şemada yer alır; **lokal modda tek seed kullanıcı + tek profil**.

---

## 5. Application — Portlar & Pipeline

```csharp
public interface IJobSource { string Name { get; } Task<IReadOnlyList<RawJob>> FetchAsync(SourceQuery q, CancellationToken ct); }
public interface IJobNormalizer { JobPosting Normalize(RawJob raw); }       // IdentityKey + VersionHash üretir
public interface IDeduplicator { Task<DedupResult> ClassifyAsync(JobPosting job, CancellationToken ct); } // New | Unchanged | Changed
public interface IRuleFilter { RuleResult Evaluate(JobPosting job); }       // ucuz, tipli: Eliminate | Pass | NeedsExtraction
public interface IEligibilityExtractor { Task<EligibilityFacts> ExtractAsync(JobPosting job, CancellationToken ct); } // LLM, FACTS
public interface IEligibilityDecider { (Decision d, IReadOnlyList<string> reasons) Decide(EligibilityFacts f, CriteriaProfile p); } // saf C#
public interface IScoringEngine { JobScore Score(JobPosting job, EligibilityFacts f, CriteriaProfile p); } // saf C#
public interface IFactsCache { Task<EligibilityFacts?> GetAsync(long jobId, string promptV, string modelV, string versionHash, CancellationToken ct); Task SetAsync(EligibilityFacts f, CancellationToken ct); }
public interface IJobRepository { /* upsert, touchLastSeen, get */ }
public interface IUserMatchRepository { /* exists, upsert, setState, getRanked */ }
public interface IJobEnricher { Task EnrichAsync(JobPosting job, CancellationToken ct); } // REZERVE (ileride; şimdi no-op)
public interface INotifier { Task NotifyAsync(/* ... */); }                  // REZERVE (impl yok)
```

### Pipeline (fact-extraction'lı)

```csharp
public async Task RunAsync(CancellationToken ct)
{
    // 1. Fetch — paralel + Polly + toleranslı; biri patlarsa diğerleri sürer
    var raw = await FetchAllSourcesParallelAsync(ct);

    // 2. Normalize (çekirdek + JSONB + IdentityKey + VersionHash)
    var normalized = raw.Select(_normalizer.Normalize);

    foreach (var job in normalized)
    {
        // 3. Dedup: New / Unchanged / Changed
        var dr = await _dedup.ClassifyAsync(job, ct);
        if (dr.Kind == DedupKind.Unchanged) { await _jobs.TouchLastSeenAsync(job, ct); continue; } // pahalı işi ATLA
        await _jobs.UpsertAsync(job, ct);

        // 4. Ucuz tipli kural elemesi
        var rf = _ruleFilter.Evaluate(job);
        if (rf.Decision == FilterDecision.Eliminate) continue;

        // 5. Fact extraction — cache'te (JobId+PromptV+ModelV+VersionHash) yoksa LLM çağır
        EligibilityFacts facts = await _factsCache.GetAsync(job.Id, PromptV, ModelV, job.VersionHash, ct)
                                 ?? await ExtractAndCacheAsync(job, ct);

        // 6. Her aktif profil için: KARAR + PUAN (saf C#, token yok)
        foreach (var profile in await _profiles.GetActiveAsync(ct))
        {
            if (await _matches.IsClosedAsync(profile.Id, job.Id, ct)) continue;  // Applied/Dismissed → atla
            var (decision, reasons) = _decider.Decide(facts, profile);
            if (decision == Decision.Ineligible) continue;
            var score = _scoring.Score(job, facts, profile);
            await _matches.UpsertAsync(profile.Id, job.Id, score, decision, reasons, MatchState.New, ct);
        }
    }
    // 7. Bildirim YOK — sonuçlar DB'de; Faz 2 minimal görünüm / Faz 3 web panel
    _log.LogInformation("Run metrics: fetched={F} new+changed={N} eliminated=... extracted=... matches=...", /* ... */);
}
```

---

## 6. Eleme & Karar Mantığı

- **Ucuz kademe (RuleFilter, tipli):** `WorkMode != Remote` → ele; `ForbiddenKeywords` (başlık/açıklama, FTS) → ele; net coğrafi tuzaklar (regex). Belirsizler → `NeedsExtraction`.
- **Fact extraction (LLM):** yalnız kural elemesinden geçenler; yapılandırılmış JSON döner (bkz. `EligibilityFacts`). **Karar vermez.**
- **Karar (C# `IEligibilityDecider`, profil bazında):**
  - Sert eleyiciler: `RequiresWorkAuth==true` (ve TR izin yoksa) · `RequiresCitizenship` · `AllowedCountries` TR içermiyor · `DataBoundary` AB. → `Ineligible`.
  - **EOR düzeltmesi (YZ4):** `MentionsEor` tek başına **eleyici değil**. `EngagementType==EmployeeViaEor` bilgi/yumuşak; `Contractor/B2B/platform ödemesi` → uygun. Yalnız yukarıdaki sert şartlar eler.
  - `Confidence` düşük veya çelişki → `Uncertain` (kullanıcıya "kontrol et" etiketiyle gösterilir).

> Faydası: kriter değişince cache çöpe gitmez — gerçek aynı, sadece C# kararı yeniden hesaplanır. Token harcanmaz.

---

## 7. Puanlama (başlangıç formülü — YZ4)

```
TimezoneFit (0-3): UTC+3'e fark 0-2s = 3 | 3-4s = 1.5 | 5s+ = 0   (kullanıcı toleransına göre, facts'teki ham gereksinimden hesaplanır)
Freshness  (0-2): yarı-ömür ~7 gün
MatchPercent: profilin must/nice/negative keyword eşleşmesi (başlık ağırlıklı + alias'lar)
Raw   = MatchPercent*5 + TimezoneFit + Freshness
Final = Math.Clamp(Raw, 0, 10)
```

**Açıklanabilirlik:** her kriterin katkısı `ScoreBreakdownJson`'a yazılır (UI + kalibrasyon). ML yok; ağırlıklı toplam.

---

## 8. Kaynaklar (`IJobSource`)

- Başlangıç: **Jobicy** (geo + jobGeo + tam açıklama + tag), **Remotive** (job_type), **Arbeitnow** (remote + visa flag).
- Eklenecek güncel remote API'ler: **RemoteOK**, **We Work Remotely (RSS)**.
- **İleride (asıl hacim): ATS katmanı** — Greenhouse / Lever / Ashby. Soyutlama hazır; Faz 1'de kurulmaz.
- Çekim sırasında her kaynağın **güncel endpoint/şartları doğrulanır**; `SourceExtraJson` ile şema farkları taşınır.

---

## 9. Konfigürasyon (tipli + versiyonlu)

```jsonc
{
  "Pipeline": { "IntervalHours": 8 },
  "Sources": { "Jobicy": { "Enabled": true, "Tags": [".net","c#","react"], "Count": 50 } },
  "Profiles": [{
    "Name": "Senior .NET Remote",
    "WorkMode": "Remote",
    "ResidenceCountry": "TR",
    "RequiredKeywords": [".net","c#"],
    "ForbiddenKeywords": ["php","wordpress","unpaid","commission only"],
    "ContractTypes": ["b2b","contractor"],
    "TimezoneToleranceHours": 4,
    "SalaryMin": null, "SalaryCurrency": "USD",
    "MinScoreToShow": 5.0
  }],
  "Llm": { "Provider": "ollama|anthropic|openai", "Endpoint": "http://localhost:11434",
           "Model": "...", "PromptVersion": "v1", "MaxCallsPerRun": 50, "MaxTokens": 400 },
  "FeatureFlags": { "ExtractionV2": false }
}
```

`Provider` `IChatClient` factory'sini seçer (OllamaChatClient / OpenAIChatClient / Anthropic). Prompt'lar
**modele göre varyantlı** (Prompt Registry) + toleranslı JSON parse (regex fallback).

---

## 10. Dayanıklılık & Gözlemlenebilirlik

- **Paralel fetch** (`Parallel.ForEachAsync`) + kaynak başına `try-catch` + **Polly** (retry + circuit breaker).
- **Toleranslı deserialization:** `JsonSerializerOptions` ile bilinmeyen alanları yok say; şema değişimi Worker'ı düşürmesin.
- **Yapısal log + metrik:** her run — fetched / new+changed / eliminated / extracted (LLM çağrı sayısı) / cache-hit / matches.
- **Gold dataset testi:** ~20 etiketli ilan ("uygun" / "kesin elenmeli"); prompt/model değişince extraction doğruluğunu ölçen konsol testi.

---

## 11. Dağıtım (Docker)

- `docker-compose.yml`: `postgres` (+ ileride `redis`) + `worker` (Faz 3'te `api`, statik `web`).
- Secret'lar **env var**: `ConnectionStrings__Db`, `Llm__ApiKey` (bulut sağlayıcıda), vb.
- Lokal kullanım = `docker compose up`; teknik kullanıcı hedefli (bilinçli karar).
- Worker kendi içinde zamanlanır (~8 saat); container ayakta kalır.

---

## 12. Başvuru Akışı

- Sistem başvuru göndermez. Panelde **"İlana Git"** → `ApplyUrl` (yoksa `Url`) yeni sekmede açılır.
- Kullanıcı gerçek platformda başvurur; panele dönüp durumu işaretler: **Başvurdum / İlgilenmiyorum / Kaydet**.
- `Opened` durumu + dönüşte nazik "başvurdun mu?" hatırlatması (opsiyonel UX). Durum **beyana dayalı** (dış sonuç doğrulanamaz).
- Asıl katma değer Faz 4: yüksek skorluya özel **CV + cover letter** üretimi (göndermeyiz, materyali hazırlarız).

---

## 13. Fazlar

| Faz | İçerik |
|---|---|
| **1 — Çekirdek** | Domain + Application(pipeline; extraction/decision stub) + Infrastructure(Jobicy source, Normalizer, Deduplicator, RuleFilter, EfJobRepository) + Worker. **Postgres**. Çıktı: log + DB. LLM/UI yok. |
| **2 — Zekâ + durum** | `IEligibilityExtractor` (cache + gold-set testli) + `IEligibilityDecider` + Scoring + `UserJobMatch` durum makinesi + ilan yaşam döngüsü (Expired/arşiv). **Minimal görünüm** (CLI listesi veya basit read endpoint) — Telegram olmadığı için sonuçları görmenin köprüsü. |
| **3 — Platform** | Web API (Identity+JWT) + React SPA + auth + çok-kullanıcı/çok-profil + apply/dismiss butonları. Kaynak genişletme (RemoteOK/WWR; sonra ATS). (Gerekirse Redis.) |
| **4 — Materyal üretimi** | İlana özel cover letter + uyarlanmış CV üretimi (on-demand, SPA butonu). |

> **Faz 4 uygulandı (on-demand).** `IApplicationMaterialGenerator` (LLM, `IChatClient`) ana CV'den
> (`data/cv.md`) ilana özel cover letter + uyarlanmış CV üretir; dil ilan diline uyar. Materyal
> `(profil, ilan)` başına saklanır, `SourceCvHash+PromptVersion+ModelVersion+JobVersionHash` ile
> tazelenir. **Karar/puan hâlâ saf C#'tadır**; LLM yalnız gerçek-çıkarımı + materyal üretir, karar vermez.
> Sistem materyali **göndermez**, yalnız hazırlar.

---

## 14. Faz 1 — Görev Listesi (sıralı, Claude Code için)

1. `dotnet new sln`; 4 proje (Domain/Application/Infrastructure/Worker) + Clean Architecture referansları.
2. `deploy/docker-compose.yml`: postgres servisi (lokal geliştirme).
3. **Domain:** enum'lar + `RawJob` + `JobPosting` (+ `User`/`CriteriaProfile`/`UserJobMatch` modelleri şemada hazır).
4. **Application:** port interface'leri + `JobScanPipeline` (extraction/decision/scoring şimdilik stub).
5. **Infrastructure:**
   - `JobicyJobSource` — `HttpClient` + `System.Text.Json` (toleranslı), `geo`+`tag` ile çek → `RawJob`.
   - `Normalizer` — AngleSharp HTML→metin; `IdentityKey` (lower+trim şirket+başlık) + `VersionHash`.
   - `Deduplicator` + `EfJobRepository` (EF Core + Npgsql); ilk migration; unique `(SourceName, ExternalId)`, index `IdentityKey`; Postgres FTS (tsvector/GIN) Title+Description.
   - Tipli, config-driven `RuleFilter` (WorkMode + ForbiddenKeywords/RequiredKeywords).
   - Polly politikaları + paralel fetch sarma.
6. **Worker:** DI + config binding; **tek sefer** çalıştırma + zamanlama; yapısal metrik logu.
7. Gerçek Jobicy verisiyle çalıştır; DB'yi incele; **iki kez çalıştır → ikinci sefer hiçbir ilan yeniden işlenmemeli** (dedup doğrulaması).

---

## 15. Faz 1 — Definition of Done

- En az 1 güncel kaynaktan gerçek remote ilanlar çekiliyor, çekirdek + `SourceExtraJson` olarak normalize ediliyor.
- Dedup kimlik+versiyon ile çalışıyor: ikinci run pahalı işi atlıyor, değişen ilanı güncelliyor.
- Tipli RuleFilter uyguluyor; sonuçlar Postgres'e yazılıyor.
- Her run metrikleri loglanıyor (fetched / new+changed / eliminated / persisted).
- Birim testleri: Normalizer (IdentityKey/VersionHash), RuleFilter (saf mantık).

---

## 16. Kasıtlı Olarak Ertelenenler (not edildi, atlanmadı)

ATS kaynak katmanı · Redis bağımlılığı · Semantic Kernel/MCP/Agent (opsiyonel Faz 3+ portföy vitrini) ·
çoklu-profil bildirim çakışması · audit log · rate-limiting · prompt-injection sertleştirmesi ·
`IJobEnricher` gerçek implementasyonu (arayüz rezerve) · feedback tabanlı ML.
