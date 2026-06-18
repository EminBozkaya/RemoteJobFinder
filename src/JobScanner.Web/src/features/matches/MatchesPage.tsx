import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { fetchMatches } from '@/api/client'
import { Button } from '@/components/ui/Button'
import { MatchCard } from './MatchCard'

export function MatchesPage() {
  const [minScore, setMinScore] = useState(0)
  const [take, setTake] = useState(50)

  const { data, isLoading, isError, error, refetch, isFetching } = useQuery({
    queryKey: ['matches', { minScore, take }],
    queryFn: () => fetchMatches({ minScore, take }),
  })

  return (
    <main className="mx-auto max-w-5xl px-4 py-8">
      <header className="mb-6 flex items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">JobScanner</h1>
          <p className="text-sm text-[color:var(--color-muted)]">
            TR'den yasal remote eşleşmeler · puana göre sıralı
          </p>
        </div>
        <Button variant="outline" size="sm" onClick={() => refetch()} disabled={isFetching}>
          {isFetching ? 'Yükleniyor…' : 'Yenile'}
        </Button>
      </header>

      <div className="mb-6 flex flex-wrap items-end gap-4 rounded-md border border-[color:var(--color-border)] bg-[color:var(--color-card)] p-4">
        <label className="flex flex-col gap-1">
          <span className="text-xs uppercase tracking-wide text-[color:var(--color-muted)]">Min skor: {minScore.toFixed(1)}</span>
          <input
            type="range" min={0} max={10} step={0.5} value={minScore}
            onChange={(e) => setMinScore(parseFloat(e.target.value))}
            className="w-48"
          />
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
        <p className="ml-auto text-sm text-[color:var(--color-muted)]">
          {data ? `${data.length} eşleşme` : ''}
        </p>
      </div>

      {isLoading && <p className="text-sm text-[color:var(--color-muted)]">Eşleşmeler yükleniyor…</p>}
      {isError && (
        <p className="rounded-md border border-[color:var(--color-danger)]/40 bg-[color:var(--color-danger)]/10 p-3 text-sm text-[color:var(--color-danger)]">
          Yüklenemedi: {(error as Error).message}. API çalışıyor mu? (varsayılan :5163)
        </p>
      )}
      {data && data.length === 0 && (
        <p className="rounded-md border border-[color:var(--color-border)] p-6 text-center text-sm text-[color:var(--color-muted)]">
          Filtreyle eşleşen kayıt yok. Min skoru düşürmeyi dene.
        </p>
      )}

      <div className="space-y-3">
        {data?.map((m) => <MatchCard key={`${m.profileId}-${m.jobId}`} m={m} />)}
      </div>
    </main>
  )
}
