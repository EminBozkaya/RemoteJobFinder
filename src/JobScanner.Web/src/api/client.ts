import type { MatchAction, MatchView, MatchesQuery } from './types'

// Dev'de vite.config.ts /api -> :5163 proxy'ler. Prod'da ayni origin'den servis.
const BASE = '/api'

function authHeaders(): Record<string, string> {
  // Self-host public deploy: kullanici localStorage.jobscanner_token set ettiyse otomatik gonder.
  const token = typeof window !== 'undefined' ? window.localStorage.getItem('jobscanner_token') : null
  return token ? { Authorization: `Bearer ${token}` } : {}
}

async function http<T = unknown>(path: string, init: RequestInit = {}): Promise<T> {
  const resp = await fetch(`${BASE}${path}`, {
    headers: { Accept: 'application/json', ...authHeaders(), ...init.headers },
    ...init,
  })
  if (!resp.ok) {
    throw new Error(`API ${resp.status} ${resp.statusText} - ${path}`)
  }
  if (resp.status === 204) return undefined as T
  return (await resp.json()) as T
}

export async function fetchMatches(q: MatchesQuery = {}): Promise<MatchView[]> {
  const params = new URLSearchParams()
  if (q.profileId != null) params.set('profileId', String(q.profileId))
  if (q.minScore != null) params.set('minScore', String(q.minScore))
  if (q.take != null) params.set('take', String(q.take))
  const qs = params.toString()
  return http<MatchView[]>(`/matches${qs ? `?${qs}` : ''}`)
}

export async function mutateMatch(profileId: number, jobId: number, action: MatchAction): Promise<void> {
  await http(`/matches/${profileId}/${jobId}/${action}`, { method: 'POST' })
}
