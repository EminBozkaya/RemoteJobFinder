import { useQuery } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { fetchMatches } from '@/api/client'
import { KNOWN_SOURCES, type DecisionKind, type KnownSource } from '@/api/types'
import { Button } from '@/components/ui/Button'
import { ThemeToggle } from '@/components/ThemeToggle'
import { decisionLabels } from '@/lib/labels'
import { MatchCard } from './MatchCard'

const ALL_DECISIONS: DecisionKind[] = ['Eligible', 'Uncertain', 'Ineligible']

export function MatchesPage({ onOpenSettings }: { onOpenSettings?: () => void }) {
  const [minScore, setMinScore] = useState(0)
  const [take, setTake] = useState(50)
  const [source, setSource] = useState<KnownSource | ''>('')
  const [decisions, setDecisions] = useState<Set<DecisionKind>>(new Set(['Eligible', 'Uncertain']))

  const { data, isLoading, isError, error, refetch, isFetching } = useQuery({
    queryKey: ['matches', { minScore, take, source }],
    queryFn: () => fetchMatches({ minScore, take, source: source || undefined }),
  })

  const filtered = useMemo(
    () => (data ?? []).filter((m) => decisions.has(m.decision)),
    [data, decisions],
  )

  function toggleDecision(d: DecisionKind) {
    setDecisions((prev) => {
      const next = new Set(prev)
      if (next.has(d)) next.delete(d)
      else next.add(d)
      return next
    })
  }

  return (
    <main className="mx-auto max-w-5xl px-4 py-8">
      <header className="mb-6 flex items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">JobScanner</h1>
          <p className="text-sm text-[color:var(--color-muted)]">
            TR'den yasal remote eşleşmeler · puana göre sıralı
          </p>
        </div>
        <div className="flex items-center gap-2">
          <ThemeToggle />
          {onOpenSettings && (
            <Button variant="outline" size="sm" onClick={onOpenSettings}>⚙ Kriterler</Button>
          )}
          <Button variant="outline" size="sm" onClick={() => refetch()} disabled={isFetching}>
            {isFetching ? 'Yükleniyor…' : 'Yenile'}
          </Button>
        </div>
      </header>

      <div className="mb-6 grid gap-4 rounded-md border border-[color:var(--color-border)] bg-[color:var(--color-card)] p-4 sm:grid-cols-2 lg:grid-cols-4">
        <label className="flex flex-col gap-1">
          <span className="text-xs uppercase tracking-wide text-[color:var(--color-muted)]">
            Min skor: {minScore.toFixed(1)}
          </span>
          <input
            type="range" min={0} max={10} step={0.5} value={minScore}
            onChange={(e) => setMinScore(parseFloat(e.target.value))}
          />
        </label>

        <label className="flex flex-col gap-1">
          <span className="text-xs uppercase tracking-wide text-[color:var(--color-muted)]">Kaynak</span>
          <select
            value={source}
            onChange={(e) => setSource(e.target.value as KnownSource | '')}
            className="rounded-md border border-[color:var(--color-border)] bg-[color:var(--color-card)] px-2 py-1 text-sm"
          >
            <option value="">Tümü</option>
            {KNOWN_SOURCES.map((s) => <option key={s} value={s}>{s}</option>)}
          </select>
        </label>

        <label className="flex flex-col gap-1">
          <span className="text-xs uppercase tracking-wide text-[color:var(--color-muted)]">Limit</span>
          <select
            value={take}
            onChange={(e) => setTake(parseInt(e.target.value, 10))}
            className="rounded-md border border-[color:var(--color-border)] bg-[color:var(--color-card)] px-2 py-1 text-sm"
          >
            {[10, 25, 50, 100, 200].map((n) => <option key={n} value={n}>{n}</option>)}
          </select>
        </label>

        <fieldset className="flex flex-col gap-1">
          <legend className="text-xs uppercase tracking-wide text-[color:var(--color-muted)]">Karar</legend>
          <div className="flex flex-wrap gap-3 pt-1 text-sm">
            {ALL_DECISIONS.map((d) => (
              <label key={d} className="flex items-center gap-1.5">
                <input
                  type="checkbox"
                  checked={decisions.has(d)}
                  onChange={() => toggleDecision(d)}
                />
                {decisionLabels[d]}
              </label>
            ))}
          </div>
        </fieldset>
      </div>

      <p className="mb-3 text-sm text-[color:var(--color-muted)]">
        {data ? `${filtered.length} / ${data.length} eşleşme` : ''}
      </p>

      {isLoading && <p className="text-sm text-[color:var(--color-muted)]">Eşleşmeler yükleniyor…</p>}
      {isError && (
        <p className="rounded-md border border-[color:var(--color-danger)]/40 bg-[color:var(--color-danger)]/10 p-3 text-sm text-[color:var(--color-danger)]">
          Yüklenemedi: {(error as Error).message}. API çalışıyor mu? (varsayılan :5163)
        </p>
      )}
      {data && filtered.length === 0 && (
        <p className="rounded-md border border-[color:var(--color-border)] p-6 text-center text-sm text-[color:var(--color-muted)]">
          Filtreyle eşleşen kayıt yok. Min skoru düşür, kaynağı değiştir veya karar
          filtresine "Uygun değil"i ekle.
        </p>
      )}

      <div className="space-y-3">
        {filtered.map((m) => <MatchCard key={`${m.profileId}-${m.jobId}`} m={m} />)}
      </div>
    </main>
  )
}
