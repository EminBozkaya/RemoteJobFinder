# JobScanner — Çalışma Rehberi

Bu doküman projenin **ne işe yaradığını, parçaların nasıl bir araya geldiğini ve nasıl
çalıştırılıp test edileceğini** sade bir dille anlatır. Mimari/tasarım detayı için
[`PLAN.md`](PLAN.md), kurallar için [`../CLAUDE.md`](../CLAUDE.md).

---

## 1. Bir cümlede

Türkiye'den **tam remote** çalışacak bir .NET geliştiricisi için, iş ilanı API'lerinden
ilanları çekip, Türkiye'den **gerçekten başvurulabilir** olanları eleyip puanlayan ve bir
panelde sunan sistem. İsteğe bağlı olarak yüksek skorlu ilanlara **ilana özel CV + cover letter**
üretir. Sistem **başvuru göndermez** — materyali hazırlar, sen gerçek platformda başvurursun.

---

## 2. Parçalar — hangisi ne işe yarar?

Proje 6 .NET projesi + 2 dış servisten oluşur. Bunlardan **3'ü çalıştırılabilir** (Worker, Api,
Web), 3'ü **kütüphanedir** (tek başına çalışmaz, diğerleri kullanır).

| Parça | Tür | Ne yapar |
|---|---|---|
| **JobScanner.Domain** | kütüphane | Saf iş nesneleri (ilan, eşleşme, kriter, enum'lar). Dış bağımlılığı yok. |
| **JobScanner.Application** | kütüphane | İş mantığı + portlar (arayüzler): tarama hattı, eleme (RuleFilter), **karar** (EligibilityDecider), **puanlama** (ScoringEngine), materyal orkestrasyonu (MaterialService). Saf C#. |
| **JobScanner.Infrastructure** | kütüphane | Dış dünya: EF Core/PostgreSQL, **API adaptörleri** (Jobicy/RemoteOK/WWR/Remotive/Arbeitnow), **LLM** (Ollama), CV dosyası okuma, link canlılık kontrolü. |
| **JobScanner.Worker** | ▶ ÇALIŞIR | **Veriyi ÜRETEN taraf.** Arka plan tarayıcı: ilanları çeker, eler, puanlar, DB'ye yazar. |
| **JobScanner.Api** | ▶ ÇALIŞIR | **Veriyi SUNAN taraf.** Read/mutate HTTP servisi: `GET /matches`, durum değişiklikleri, materyal üretimi. Veri üretmez, DB'den okur. |
| **JobScanner.Web** | ▶ ÇALIŞIR | **Arayüz.** React SPA; eşleşmeleri gösterir, butonlar Api'yi çağırır. |
| **PostgreSQL** | dış servis | Tek veri deposu (port 5432). |
| **Ollama** | dış servis | Lokal LLM (llama3.1, port 11434). Fact çıkarımı + materyal üretimi. |

> **Bağımlılık yönü (Clean Architecture):** `Worker/Api → Infrastructure → Application → Domain`.
> İçeriye doğru bağımlılık; Domain hiçbir şeye bağımlı değil.

---

## 3. Genel akış

```
  İş ilanı API'leri (Jobicy, RemoteOK, WWR, Remotive, Arbeitnow)
        │   ① Worker periyodik çeker
        ▼
  ┌─────────────────────── JobScanner.Worker ───────────────────────┐
  │ fetch → normalize (HTML→metin) → DEDUP (yeni/değişen mi?)        │
  │   └─ yeni/değişen ilanlar için:                                  │
  │        liveness (HEAD: link ölü mü?)                             │
  │        → LLM fact extraction (Ollama) ──► facts cache            │
  │        → KARAR (saf C#: EligibilityDecider)                      │
  │        → PUAN (saf C#: ScoringEngine)                            │
  │        → user_job_matches'e yaz                                  │
  └──────────────────────────────┬──────────────────────────────────┘
                                  ▼
                          PostgreSQL  ◄──────── ④ kullanıcı aksiyonları (durum)
                                  ▲                       │
        ② GET /matches           │                       │
  ┌────────────── JobScanner.Api ┴───────────────┐       │
  │ /matches (oku) · save/open/apply/dismiss      │◄──────┘
  │ /materials (CV + cover letter üret)           │
  └───────────────────┬───────────────────────────┘
                      ▼  ③ HTTP (REST/JSON)
              JobScanner.Web (React SPA)  ◄── kullanıcı burada gezer
```

**Önemli ayrım:** Worker veriyi **üretir**; Api + Web sadece **gösterir/değiştirir**. Yani
panelde güncel ilan görmek için **Worker'ın daha önce çalışmış olması** gerekir. Api, Worker'ı
tetiklemez; ikisi bağımsız çalışır.

---

## 4. Worker ne zaman çalışır? (en çok karışan kısım)

Worker iki modda çalışabilir, `appsettings.json → Pipeline` ile ayarlı:

| Mod | Davranış | Ne zaman |
|---|---|---|
| **Sürekli (varsayılan)** | Açılışta **hemen 1 tarama**, sonra **her `IntervalHours` saatte** (varsayılan **8 saat**) bir tarama. Süreç açık kaldıkça döner. | `dotnet run --project src/JobScanner.Worker` |
| **Tek seferlik (RunOnce)** | 1 tarama yapar, **DB'yi migrate eder**, çıkar. | `Pipeline__RunOnce=true` env'i ile |

- Worker'ı **terminalde sen başlatırsın**; kapattığında periyodik tarama durur (Windows servisi
  olarak da kurulabilir ama bu projede gerekli değil).
- **İlk açılışta** ayrıca: DB migration uygulanır + seed kullanıcı/profil oluşturulur
  (`local@jobscanner.dev`).
- Test/geliştirme için pratik olan **RunOnce**: bir kez tara, gör, çık.

---

## 5. Bilmen gereken mekanizmalar

- **Dedup (tekrar önleme):** İlan kimliği `(SourceName, ExternalId)` + `IdentityKey`. İçerik
  değişimi `VersionHash` ile yakalanır. Aynı ilan ikinci kez **yeniden işlenmez** (LLM token'ı
  boşa gitmez). → Worker'ı iki kez çalıştırırsan ikincide "unchanged" görürsün.
- **Facts cache:** LLM'in çıkardığı **ham gerçekler** saklanır (anahtar: JobId + PromptVersion +
  ModelVersion + VersionHash). **Karar cache'lenmez** — kriterini değiştirince token harcamadan
  yeniden hesaplanır.
- **LLM yalnız "gerçek çıkarır", karar VERMEZ.** "TR'den çalışılır mı? EOR var mı? taşınma şart mı?"
  gibi gerçekleri JSON döndürür. **Uygun/Uygun değil kararı ve puan saf C#'ta** hesaplanır.
- **`MaxLlmCallsPerRun = 100`:** Bir taramada en fazla 100 LLM çağrısı (maliyet/süre koruması; ücretsiz
  lokal Ollama için yüksek tutuldu). Daha çok yeni ilan varsa kalanı **sonraki taramada** işlenir —
  dedup+cache sayesinde hiçbir ilan kalıcı olarak atlanmaz.
- **Durum makinesi:** Her eşleşme `New → Saved/Opened → Applied/Dismissed`. İlan arşivlenirse açık
  eşleşmeler `Expired` olur. Durum **beyana dayalı** (sistem başvuruyu doğrulayamaz).
- **Liveness gate:** İlan URL'sine HTTP HEAD atılır; ölü/erişilemez linkler elenir.
- **Stale arşiv:** `StaleAfterDays = 30` gündür görülmeyen ilanlar arşivlenir.
- **Materyal üretimi (Faz 4):** Panelde "Materyal üret" → `data/cv.md`'den, **ilan diliyle**
  cover letter + uyarlanmış CV. `(profil, ilan)` başına saklanır; CV/ilan değişmedikçe tekrar
  token harcamaz. LLM **uydurmaz**, ana CV'yi yeniden vurgular.

---

## 6. Nasıl çalıştırılır / test edilir (adım adım)

**Önkoşullar:** PostgreSQL (5432) ayakta, Ollama (11434) + `llama3.1` yüklü.

### 1) Veriyi üret (Worker — bir kez)
```bash
# Tek tarama yapıp çıksın; gerçek LLM kararları için Ollama açık olmalı:
Pipeline__RunOnce=true dotnet run --project src/JobScanner.Worker
```
Bittiğinde DB'de ilanlar + eşleşmeler hazır. (Sürekli güncel kalsın istersen `RunOnce` olmadan
çalıştır; 8 saatte bir kendi tarar.)

### 2) Api'yi başlat
```bash
# Materyal üretimini de test edeceksen LLM'i bu oturuma aç (repoyu değiştirmeden):
$env:Llm__Enabled="true"; $env:Llm__Provider="ollama"; $env:Llm__Model="llama3.1"
dotnet run --project src/JobScanner.Api          # → http://localhost:5163
```

### 3) Web'i başlat
```bash
cd src/JobScanner.Web
npm install        # ilk sefer
npm run dev        # → http://localhost:5173
```

### 4) Tarayıcıda test et
- Eşleşmeleri gör (skor, karar, kaynak rozeti, güvenilirlik).
- **Kaydet / Açıldı / Başvurdum / İlgilenmiyorum** → durum makinesini dene.
- **"✍ Materyal üret"** → (önce `data/cv.md`'yi `data/cv.md.example`'dan oluştur) cover letter + CV.

> **CV materyali için:** `cp data/cv.md.example data/cv.md` → kendi bilgilerinle doldur. `data/cv.md`
> gitignore'lu (kişisel veri). Dosya yoksa "CV bulunamadı" der — bu doğru davranıştır.

---

## 7. Sürüm / yayın (release-please)

`main`'e her push'ta GitHub'daki **release-please botu** commit başlıklarını (`feat`/`fix`) okur,
bir "release PR" hazır tutar. O PR'ı merge edince git tag + GitHub Release + `CHANGELOG.md`
otomatik oluşur. Commit mesajı: **İngilizce başlık + Türkçe gövde.** Detay: [`../CLAUDE.md`](../CLAUDE.md).

---

## 8. Detaylı teknik akış (kod düzeyinde, adım adım)

Bir taramanın tüm yolculuğu. Her adımda **[Sonuç]** (ne olur) + **[Arka plan]** (hangi sınıf/metot,
hangi argüman/eşik). Spine: `JobScanner.Application/Pipeline/JobScanPipeline.RunAsync()`.

### Adım 0 — Tetikleme
- **[Sonuç]** Worker başlatılır → DB hazırlanır → tarama başlar.
- **[Arka plan]** `Worker/ScannerHostedService.ExecuteAsync` → `InitializeDatabaseAsync`
  (`db.Database.MigrateAsync` + `DbSeeder.SeedAsync` → seed profil `local@jobscanner.dev`,
  kriterler: ResidenceCountry=`TR`, RequiredKeywords, MinScoreToShow=5.0) → `RunScanAsync` →
  `JobScanPipeline.RunAsync(ct)`.

### Adım 1 — Kaynaklardan çekme (paralel)
- **[Sonuç]** 5 API'den ham ilanlar paralel toplanır; biri patlasa diğerleri sürer.
- **[Arka plan]** `FetchAllSourcesParallelAsync` → `SourceQuery(Tags=[".net","c#","react"], Geo, Count=50)`;
  `Parallel.ForEachAsync(MaxDegreeOfParallelism=4)`; her `IJobSource.FetchAsync(query, ct)`.
  HTTP katmanında: ortak User-Agent + **gzip auto-decompress** + **Polly** (30s attempt / 60s total / circuit breaker).
  Kaynak başına `try-catch` → hata `sourceErrors++`, run düşmez.

| Kaynak (`IJobSource`) | Tür | Çekme mantığı |
|---|---|---|
| `JobicyJobSource` | REST JSON | `tag` başına ayrı GET (`BuildUrl`: count 1–50 clamp, geo, tag) → birleştir; bozuk kayıt (`Id==0 \|\| Url boş`) atlanır |
| `RemoteOkJobSource` | REST JSON | tek çağrı, client-side tag filtre (gzip ile ~479KB→117KB) |
| `WeWorkRemotelyJobSource` | RSS 2.0 | feed parse |
| `RemotiveJobSource` | REST JSON | `search` per-tag iterasyon + merge (24s gecikmeli veri, ToS) |
| `ArbeitnowJobSource` | REST JSON | client-side `remote=true` + tag filtre; `visa_sponsorship` çıkarılır |

- **Çıktı:** `IReadOnlyList<RawJob>` (ham: SourceName, ExternalId, Title, Company, DescriptionHtml, Url, …).

### Adım 2 — Normalize
- **[Sonuç]** Ham ilan, tutarlı + karşılaştırılabilir hâle gelir.
- **[Arka plan]** `Infrastructure/Normalization/Normalizer.Normalize(RawJob)`:
  - `HtmlToText` (**AngleSharp**) → düz metin; `Collapse` → fazla boşluk tek boşluk.
  - **IdentityKey** = `lower(company)|lower(title)` (çapraz-kaynak dedup için).
  - **VersionHash** = `SHA-256(title∣company∣desc∣url∣applyUrl)` (içerik değişimi tespiti — kimlik DEĞİL).
  - `WorkMode=Remote`, `PostedAt` parse, `FirstSeenAt/LastSeenAt=now`, `SourceExtraJson` (jsonb).
  - Aynı run içinde `(SourceName, ExternalId)` tekrarları `GroupBy` ile tekilleştirilir.

### Adım 3 — Dedup (yeni / değişmemiş / değişmiş)
- **[Sonuç]** Daha önce işlenmiş ve **değişmemiş** ilanlar pahalı işten muaf tutulur (token tasarrufu).
- **[Arka plan]** `Persistence/Deduplicator.ClassifyAsync` → `FindByIdentityAsync(SourceName, ExternalId)`:
  - kayıt yok → **New**
  - var & `VersionHash` aynı → **Unchanged** → `TouchLastSeenAsync` + `continue` (atla)
  - var & `VersionHash` farklı → **Changed**
  - New/Changed → `UpsertAsync` (DB'ye yaz), `newOrChanged++`.

### Adım 4 — Ucuz kural elemesi (token yok)
- **[Sonuç]** Net uygunsuzlar LLM'e gitmeden elenir.
- **[Arka plan]** `Application/Filtering/RuleFilter.Evaluate(job)`:
  - `WorkMode != Remote` → **Eliminate**
  - `haystack = Title + "\n" + DescriptionText`; **ForbiddenKeywords** (`php`, `wordpress`, `unpaid`,
    `commission only`) içeriyorsa → **Eliminate**
  - **RequiredKeywords** tanımlı ve hiçbiri yoksa → **Eliminate**
  - aksi halde **Pass**. (Eşleşme: `Contains`, `OrdinalIgnoreCase`.)

### Adım 4.5 — Liveness gate (ölü link)
- **[Sonuç]** Linki ölmüş ilanlara LLM harcanmaz; arşivlenir.
- **[Arka plan]** `IJobLivenessChecker.IsAliveAsync(job.Url)` → HTTP **HEAD**:
  `false` (404/410) → `ArchiveOneAsync` + atla; `null` (belirsiz) → ihtiyatla **devam et**.

### Adım 5 — Fact extraction (LLM — tek yapay zekâ adımı)
- **[Sonuç]** İlan metninden **yapılandırılmış gerçekler** çıkar. (Karar DEĞİL.)
- **[Arka plan]** Önce `IFactsCache.GetAsync(jobId, PromptVersion, ModelVersion, VersionHash)` —
  **cache varsa LLM çağrısı YOK.** Yoksa:
  - `llmCalls >= MaxLlmCallsPerRun` (**100**) → `break` (kalan ilanlar sonraki run'a).
  - `Infrastructure/Llm/LlmEligibilityExtractor.ExtractAsync(job)` → **Ollama `IChatClient`**, prompt
    **v4**, `Temperature=0`, `ResponseFormat=Json` → `EligibilityFacts`:
    `RequiresWorkAuth, RequiresRelocation, BackgroundCheckCountry, AllowedCountries, RequiresCitizenship,
    AllowsB2BContractor, EngagementType, MentionsEor, EorPlatform, DataBoundary, TimezoneRequirementRaw,
    IsRecruiterAgency, IsLikelyGhost, Confidence`.
  - `IFactsCache.SetAsync(facts)` → cache'e yaz. Hata → `extractionErrors++`, ilanı atla (run düşmez).

### Adım 5.5 — Legitimacy (ghost-job tespiti, saf C#)
- **[Sonuç]** İlana güvenilirlik etiketi (karardan bağımsız: Eligible ama Suspicious olabilir).
- **[Arka plan]** `Application/Deciding/Legitimacy.Evaluate(facts, job, now)` sinyaller:
  `IsLikelyGhost`→`ghost-language`; `IsRecruiterAgency`→`recruiter-agency`; `Confidence<0.5`→`low-llm-confidence`;
  `FirstSeenAt 60+ gün`→`long-running-Nd`. Sinyal sayısı: 0→**High**, 1→**Caution**, 2+→**Suspicious**.

### Adım 6 — Karar + Puan (her aktif profil için, saf C#, token yok)
- **[Sonuç]** İlanın senin için **uygun mu** ve **kaç puan** olduğu hesaplanır; geçerse panele yazılır.
- **[Arka plan]** `IsClosedAsync` (Applied/Dismissed) ise atla (kullanıcı kararına saygı). Sonra:

  **a) `EligibilityDecider.Decide(facts, profile)`** — sert eleyiciler → **Ineligible**:
  - `RequiresCitizenship==true` · `RequiresRelocation==true` · yabancı-ülke adli sicil
    (`RequiresForeignBackgroundCheck`) · `RequiresWorkAuth==true && TR izinli değil` ·
    `AllowedCountries` global değil ve TR içermiyor · `DataBoundary` AB ve TR AB değil.
  - **EOR pozitif** (eleyici değil, tercih): `MentionsEor` veya `EngagementType=EmployeeViaEor`.
  - `Confidence < MinConfidence` (**0.4**) → **Uncertain**. Aksi halde **Eligible**.
  - > Not: çalışma TÜRÜ (B2B/employee/EOR) tek başına eleyici değildir; önemli olan **coğrafya + izin + taşınma**.

  **b) `ScoringEngine.Score(job, facts, profile)`** → `0–10` (clamp). Formül (Faz 5b):
  `SkillFit×5 + TimezoneFit(0–3) + Freshness(0–2) + EngagementFit(0–1) + ExperienceFit(−2..0)`:
  - **SkillFit (0–1):** kullanıcının yetkinliklerinin (Skills) ilanda görünmesi, **öz-puanla (1–10) ağırlıklı**
    (başlıkta tam **1.0**, gövdede **0.6**). Yetkinlik yoksa 1.0 (kısıt yok). *Keyword'lerin yerini aldı.*
  - **ExperienceFit (−2..0):** ilan "X için min N yıl" istiyorsa (`facts.RequiredExperience`) ve kullanıcının
    yılı azsa **yumuşak ceza** (eleme değil); sahip olunmayan yetkinlik SkillFit'i zaten düşürür.
  - **TimezoneFit:** TR=UTC+3; kısıt yoksa 3.0; |Δ| ≤ tolerans (**4s**) → 3.0; ≤ tol+2 → 1.5; üstü 0.
  - **Freshness:** ~7 günlük yarı-ömür.
  - **EngagementFit:** EOR/EmployeeViaEor → **1.0** (ideal, şirket gerekmez); B2B/Contractor/Freelance → **0.4**; diğer 0.
  - `score.Final < profile.MinScoreToShow` (**5.0**) → panele yazılmaz.

  > **Faz 5b not:** Yetkinlikler/diller/soft-skill'ler profilde tutulur, arayüzden düzenlenir; öz-puan
  > scoring'e, tecrübe yılı ilan şartına beslenir. İlanın istediği yıl çıkarımı prompt **v5** ile gelir;
  > model/prompt sürümü değişince içerik değişmeyen ilanlar da yeniden çıkarılır (cache tazelenir).

  **c)** Geçerse `IUserMatchRepository.UpsertAsync(...)` → `user_job_matches` (score, breakdown JSON,
  decision, reasons JSON, legitimacy, signals JSON). State korunur (varsa).

### Adım 7 — Yaşam döngüsü
- **[Sonuç]** Bayatlamış ilanlar arşivlenir; açık eşleşmeleri "Süresi doldu" olur.
- **[Arka plan]** `ArchiveStaleAsync(now − StaleAfterDays=30)` → `ExpireOpenMatchesForArchivedJobsAsync`.

### Adım 8 — Metrik
- **[Arka plan]** `RunMetrics` log satırı: `fetched / new+changed / unchanged / eliminated / extracted /
  matches / sourceErrors / extractionErrors / archived / expired / deadLinks`.

---

## 9. Kullanıcı tarafı (panelde ne oluyor?)

- **Listeleme:** SPA `GET /api/matches` → `EfUserMatchRepository.GetRankedAsync` (Expired/Dismissed hariç,
  skora göre sıralı) → kartlar (skor, karar, kaynak rozeti, güvenilirlik).
- **Durum değişimi:** "Kaydet/Açıldı/Başvurdum/İlgilenmiyorum" → `POST /matches/{p}/{j}/{action}` →
  domain durum makinesi (`UserJobMatch.Save/Open/Apply/Dismiss`). Idempotent; beyana dayalı.
- **Materyal üretimi (Faz 4):** "✍ Materyal üret" → `POST /matches/{p}/{j}/materials` →
  `MaterialService.GetOrGenerateAsync`: ilan + profil + `data/cv.md` → taze saklı varsa onu döner,
  yoksa `LlmApplicationMaterialGenerator` (Ollama) ile **ilan diliyle** cover letter + uyarlanmış CV
  üretir, saklar. Tazeleme: `SourceCvHash + PromptVersion + ModelVersion + JobVersionHash`.
- **Başvuru:** Sistem göndermez; "Kaynağa git" ilanın `ApplyUrl`/`Url`'üne yönlendirir, sen gerçek
  platformda başvurursun.
