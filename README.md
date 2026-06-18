# JobScanner

Türkiye'de yaşayan bir B2B/contractor/EOR adayı için **tam remote** iş ilanlarını API'lerden
çekip, Türkiye'den **gerçekten yasal başvurulabilir** olanları eleyip puanlayan bir sistem.

- LLM yalnız ilan metninden **gerçek çıkarır** (TR'den çalışabilir mi? EOR var mı? taşınma şart mı?).
- Karar/puan **saf C#'ta** hesaplanır: kriter değişince token harcanmadan yeniden hesap.
- Veri yalnız **yapılandırılmış API'lerden** gelir; **web scraping yok**.

📖 Tam mimari ve plan: [`docs/PLAN.md`](docs/PLAN.md) · 📌 Çalışma kuralları: [`CLAUDE.md`](CLAUDE.md)

---

## Mimari

```
Worker / Api  →  Infrastructure  →  Application  →  Domain
                                                     (bağımlılığı yok)
```

- **JobScanner.Domain** — saf entity / value object / enum
- **JobScanner.Application** — portlar (interface) + `JobScanPipeline` + saf is mantığı
  (RuleFilter, EligibilityDecider, ScoringEngine)
- **JobScanner.Infrastructure** — EF Core(Npgsql), `IJobSource` adaptörleri
  (Jobicy/RemoteOK/WeWorkRemotely), LLM (`IChatClient`)
- **JobScanner.Worker** — arka plan tarayıcı host'u (8 saatte bir varsayılan)
- **JobScanner.Api** — read-only `GET /matches` + durum makinesi mutasyonları
- **JobScanner.Web** — React SPA (Vite + Tailwind + shadcn-style)

---

## Önkoşullar

| Bileşen | Versiyon | Notlar |
|---|---|---|
| .NET SDK | 10.x | Worker + Api için |
| Node.js | 22.x | SPA build için |
| PostgreSQL | 16+ | Docker compose ile veya lokal kurulum |
| Ollama | son sürüm | Lokal LLM (varsayılan); bulut sağlayıcılar opsiyonel |

---

## Hızlı kurulum

### 1. Repo'yu klonla

```bash
git clone https://github.com/EminBozkaya/RemoteJobFinder.git
cd RemoteJobFinder
```

### 2. Postgres'i başlat

Docker compose:
```bash
docker compose -f deploy/docker-compose.yml up -d
```

Veya lokal Postgres'in varsa: `jobscanner` rolü + DB oluştur, connection string'i
`appsettings.json`'da güncelle.

### 3. Ollama'yı kur ve model indir

[ollama.com/download](https://ollama.com/download) → kur → bir model çek:
```bash
ollama pull llama3.1
# veya daha güçlüsü:
ollama pull qwen2.5:7b-instruct
```

> CPU-only sistemlerde 7-8B parametre yeterli (~10-15 token/s). Modeli değiştirirsen
> `appsettings.json` → `Llm:Model` güncelle (cache otomatik tazelenir).

### 4. Worker'ı çalıştır (ilk veri çekimi)

```bash
dotnet run --project src/JobScanner.Worker
```

İlk açılışta:
- DB migration uygulanır
- Seed user + profil oluşur (`local@jobscanner.dev`)
- Tarama başlar (Jobicy + RemoteOK + WWR'den ilan çeker, LLM ile gerçek çıkarır)

> İlk run ~5-10 dk sürer (CPU'da LLM extraction). Sonraki run'larda cache + dedup sayesinde
> sadece yeni/değişen ilanlar işlenir.

### 5. API'yi başlat

```bash
dotnet run --project src/JobScanner.Api
# → http://localhost:5163
```

### 6. SPA'yı başlat

```bash
cd src/JobScanner.Web
npm install
npm run dev
# → http://localhost:5173
```

Tarayıcıda aç; eşleşmeleri gör, "Kaydet" / "Açıldı işaretle" / "Başvurdum" / "İlgilenmiyorum"
butonları durum makinesini tetikler.

---

## Yapılandırma

Tüm ayarlar `src/JobScanner.Worker/appsettings.json` üzerinden (Worker), API kendi
`src/JobScanner.Api/appsettings.json`'unu kullanır.

### Kriter profili (seed)

İlk açılışta tek profil seed edilir (`src/JobScanner.Worker/DbSeeder.cs`). Kendi
kriterlerin için DB'de güncelle veya seeder'ı düzenle:

```json
{
  "ResidenceCountry": "TR",
  "RequiredKeywords": [".net", "c#"],
  "ForbiddenKeywords": ["php", "wordpress"],
  "TimezoneToleranceHours": 4
}
```

### LLM sağlayıcı

```json
"Llm": {
  "Enabled": true,
  "Provider": "ollama",
  "Endpoint": "http://localhost:11434",
  "Model": "llama3.1",
  "PromptVersion": "v3"
}
```

`Provider` `IChatClient` factory'sini seçer ([ChatClientFactory.cs](src/JobScanner.Infrastructure/Llm/ChatClientFactory.cs)). Şu an Ollama bağlı; OpenAI/Anthropic
eklemek için ilgili `Microsoft.Extensions.AI` paketi + bir case yeterli.

### Kaynaklar

5 IJobSource adaptörü, her biri bağımsız `Enabled` flag'i ile:
- **Jobicy** — REST JSON, `tag` per-tag iteration, geo opsiyonel
- **RemoteOK** — REST JSON tek çağrı, client-side tag filtre
- **WeWorkRemotely** — RSS 2.0
- **Remotive** — REST JSON, `search` per-tag iteration; 24h delayed data (ToS)
- **Arbeitnow** — REST JSON, EU-friendly, `remote=true` + tag client-side filtre; `visa_sponsorship` flag çıkarılır

---

## Public deploy & güvenlik

JobScanner.Api'yi Oracle VM / VPS gibi internete açık bir yerde çalıştırıyorsan
**bearer token koruması** aç:

```bash
export JOBSCANNER_API_TOKEN=$(openssl rand -hex 32)
dotnet run --project src/JobScanner.Api
```

Bu set edildiğinde `/health` hariç tüm endpoint'ler `Authorization: Bearer <token>`
ister. SPA tarafında token'ı tarayıcının localStorage'ına kaydet:

```js
// Tarayıcı DevTools console'da bir kez çalıştır:
localStorage.setItem('jobscanner_token', 'YOUR_TOKEN_HERE')
```

Token set değilse middleware OFF (lokal dev'i etkilemez).

---

## Test

```bash
dotnet test
```

123+ unit/gold-set test:
- Domain: durum makinesi geçişleri (Save/Open/Apply/Dismiss/Expire)
- Application: RuleFilter, EligibilityDecider, ScoringEngine, TimezoneParser
- Infrastructure: Normalizer, RSS parse, LLM JSON parse (fake `IChatClient`)
- GoldSet: ~25 etiketli senaryo ile karar mantığı regresyonu

---

## Faz durumu

| Faz | İçerik | Durum |
|---|---|---|
| 1 — Çekirdek | Tarama hattı + dedup + Postgres | ✅ |
| 2 — Zekâ + durum | LLM extractor + karar/puan + UserJobMatch durum makinesi + read API | ✅ |
| 2 kalibrasyon | TR-merkezli uygunluk (EOR öncelikli, relocation/foreign-bg eleyici) | ✅ |
| 3.1 — Mutasyonlar | API POST endpoint'leri (save/open/apply/dismiss) | ✅ |
| 3.2 — Kaynaklar | RemoteOK + We Work Remotely | ✅ |
| 3.5 — Kaynak genişletme | Remotive + Arbeitnow | ✅ |
| 3.4 — SPA | React + Tailwind + shadcn | ✅ |
| 3 — Ghost-job & Liveness | Block G legitimacy sinyalleri + HEAD liveness gate | ✅ |
| 4 — Otomasyon | Yüksek skorluya CV + cover letter üretimi | ⏳ |

---

## Katkıda bulunma & sürümleme

Repo **Conventional Commits** + **Release-Please** kullanır. Commit mesajları:

```
feat:  yeni özellik
fix:   bug onarımı
docs:  doküman
chore: bakım (deps, config)
ci:    CI/CD
refactor: yapı değişikliği
perf:  performans
test:  test
```

`feat!:` veya `BREAKING CHANGE:` notu major bump yapar.

`main` branch'ine push edilince [release-please workflow'u](.github/workflows/release-please.yml)
otomatik bir "Release vX.Y.Z" PR'ı açar/günceller. PR merge edilince git tag + GitHub release +
[CHANGELOG.md](CHANGELOG.md) güncellenmesi kendiliğinden olur.

PR'lar [auto-labeler](.github/labeler.yml) ile dosya değişikliklerine göre etiketlenir
(`frontend`, `backend-core`, `migrations`, `docs`, vb).

---

## Komutlar (cheat sheet)

```bash
# Build & test
dotnet build && dotnet test

# Worker (tek tarama: Pipeline:RunOnce=true env)
dotnet run --project src/JobScanner.Worker

# API
dotnet run --project src/JobScanner.Api

# SPA dev
cd src/JobScanner.Web && npm run dev

# EF Core migration
dotnet ef migrations add <Name> -p src/JobScanner.Infrastructure -s src/JobScanner.Infrastructure
dotnet ef database update -p src/JobScanner.Infrastructure -s src/JobScanner.Infrastructure
```

---

## Lisans

Bu proje OSS olarak yayımlanmıştır; isteyen klonlayıp kendi kullanımı için
çalıştırabilir. Kaynaklara erişim için ilgili API'lerin kullanım koşullarına uy
(özellikle RemoteOK'in API ToS'unda "geri bağlantı" şartı vardır).
