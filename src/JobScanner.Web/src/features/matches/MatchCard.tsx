import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { mutateMatch } from '@/api/client'
import type { DecisionKind, LegitimacyKind, MatchAction, MatchView, ScoreContribution, StateKind } from '@/api/types'
import { Badge } from '@/components/ui/Badge'
import { Button } from '@/components/ui/Button'
import { decisionLabels, explainSignal, legitimacyLabels, stateLabels } from '@/lib/labels'
import { cn } from '@/lib/utils'

type Tone = 'neutral' | 'success' | 'warning' | 'danger' | 'accent'

const decisionTone: Record<DecisionKind, Tone> = {
  Eligible: 'success',
  Uncertain: 'warning',
  Ineligible: 'danger',
}

const stateTone: Record<StateKind, Tone> = {
  New: 'neutral',
  Saved: 'accent',
  Opened: 'accent',
  Applied: 'success',
  Dismissed: 'danger',
  Expired: 'neutral',
}

const legitimacyTone: Record<LegitimacyKind, Tone> = {
  High: 'success',
  Caution: 'warning',
  Suspicious: 'danger',
}

const legitimacyIcon: Record<LegitimacyKind, string> = {
  High: '',
  Caution: '⚠',
  Suspicious: '🚩',
}

function scoreTone(score: number): Tone {
  if (score >= 7) return 'success'
  if (score >= 5) return 'warning'
  return 'danger'
}

function safeParse<T>(json: string, fallback: T): T {
  try { return JSON.parse(json) as T } catch { return fallback }
}

function formatPosted(iso: string | null): string {
  if (!iso) return '—'
  const d = new Date(iso)
  if (isNaN(d.getTime())) return '—'
  const days = Math.floor((Date.now() - d.getTime()) / (1000 * 60 * 60 * 24))
  if (days <= 0) return 'bugün'
  if (days === 1) return '1 gün önce'
  if (days < 30) return `${days} gün önce`
  return d.toLocaleDateString()
}

export function MatchCard({ m }: { m: MatchView }) {
  const [expanded, setExpanded] = useState(false)
  const qc = useQueryClient()

  const mutation = useMutation({
    mutationFn: (action: MatchAction) => mutateMatch(m.profileId, m.jobId, action),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['matches'] }),
  })

  const breakdown = safeParse<ScoreContribution[]>(m.scoreBreakdownJson, [])
  const reasons = safeParse<string[]>(m.decisionReasonsJson, [])
  const signals = safeParse<string[]>(m.legitimacySignalsJson, [])
  const applyHref = m.applyUrl || m.url
  const showLegitimacy = m.legitimacy !== 'High'

  return (
    <article
      className={cn(
        'rounded-lg border border-[color:var(--color-border)] bg-[color:var(--color-card)] p-4 shadow-sm',
        'transition-shadow hover:shadow-md',
      )}
    >
      <header className="flex items-start justify-between gap-3">
        <div className="min-w-0 flex-1">
          <h3 className="truncate text-base font-semibold">{m.title}</h3>
          <p className="truncate text-sm text-[color:var(--color-muted)]">
            {m.company || 'Bilinmeyen şirket'} · {formatPosted(m.postedAt)}
          </p>
          <p className="mt-1 text-xs text-[color:var(--color-muted)]">
            Kaynak: <span className="font-medium text-[color:var(--color-fg)]">{m.sourceName}</span>
          </p>
        </div>
        <div className="flex shrink-0 flex-wrap items-center justify-end gap-2">
          <Badge tone={scoreTone(m.score)}>★ {m.score.toFixed(1)}</Badge>
          <Badge tone={decisionTone[m.decision]}>{decisionLabels[m.decision]}</Badge>
          <Badge tone={stateTone[m.state]}>{stateLabels[m.state]}</Badge>
          {showLegitimacy && (
            <Badge
              tone={legitimacyTone[m.legitimacy]}
              title={signals.map(explainSignal).join(' · ') || undefined}
            >
              {legitimacyIcon[m.legitimacy]} {legitimacyLabels[m.legitimacy]}
            </Badge>
          )}
        </div>
      </header>

      <div className="mt-3 flex flex-wrap items-center gap-2">
        <Button asChild size="sm">
          <a href={applyHref} target="_blank" rel="noopener noreferrer">Kaynağa git ↗</a>
        </Button>
        <Button
          size="sm"
          variant="outline"
          disabled={mutation.isPending || m.state !== 'New'}
          onClick={() => mutation.mutate('save')}
        >Kaydet</Button>
        <Button
          size="sm"
          variant="outline"
          disabled={mutation.isPending || m.state === 'Applied' || m.state === 'Dismissed'}
          onClick={() => mutation.mutate('open')}
        >Açıldı işaretle</Button>
        <Button
          size="sm"
          variant="outline"
          disabled={mutation.isPending}
          onClick={() => mutation.mutate('apply')}
        >Başvurdum</Button>
        <Button
          size="sm"
          variant="ghost"
          disabled={mutation.isPending}
          onClick={() => mutation.mutate('dismiss')}
        >İlgilenmiyorum</Button>
        <Button
          size="sm"
          variant="ghost"
          className="ml-auto text-[color:var(--color-muted)]"
          onClick={() => setExpanded((x) => !x)}
        >{expanded ? 'Gizle' : 'Detay'}</Button>
      </div>

      {expanded && (
        <div className="mt-4 grid gap-4 text-sm sm:grid-cols-2">
          <section>
            <h4 className="mb-1 font-semibold">Puan kırılımı</h4>
            <ul className="space-y-1">
              {breakdown.length === 0 && <li className="text-[color:var(--color-muted)]">—</li>}
              {breakdown.map((c, i) => (
                <li key={i} className="flex justify-between gap-3">
                  <span className="text-[color:var(--color-muted)]">{c.Criterion}</span>
                  <span className="tabular-nums">{c.Contribution.toFixed(2)}</span>
                </li>
              ))}
            </ul>
          </section>
          <section>
            <h4 className="mb-1 font-semibold">Karar gerekçeleri</h4>
            <ul className="list-disc space-y-1 pl-4">
              {reasons.length === 0 && <li className="text-[color:var(--color-muted)] list-none">—</li>}
              {reasons.map((r, i) => <li key={i}>{r}</li>)}
            </ul>
            {signals.length > 0 && (
              <>
                <h4 className="mt-3 mb-1 font-semibold">Güvenilirlik sinyalleri</h4>
                <ul className="list-disc space-y-1 pl-4 text-[color:var(--color-muted)]">
                  {signals.map((s, i) => <li key={i}>{explainSignal(s)}</li>)}
                </ul>
              </>
            )}
          </section>
        </div>
      )}

      {mutation.isError && (
        <p className="mt-2 text-xs text-[color:var(--color-danger)]">
          Mutasyon hatası: {(mutation.error as Error).message}
        </p>
      )}
    </article>
  )
}
