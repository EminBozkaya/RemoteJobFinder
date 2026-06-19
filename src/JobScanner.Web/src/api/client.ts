import type { ApplicationMaterial, MatchAction, MatchView, MatchesQuery } from './types'

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
  if (q.source) params.set('source', q.source)
  const qs = params.toString()
  return http<MatchView[]>(`/matches${qs ? `?${qs}` : ''}`)
}

export async function mutateMatch(profileId: number, jobId: number, action: MatchAction): Promise<void> {
  await http(`/matches/${profileId}/${jobId}/${action}`, { method: 'POST' })
}

// Taze saklı materyal varsa onu döner (token harcamadan), yoksa LLM ile üretir.
// force=true önceki materyali yok sayıp yeniden üretir. Hata gövdesindeki mesajı yüzeye çıkarır.
export async function generateMaterials(
  profileId: number, jobId: number, force = false,
): Promise<ApplicationMaterial> {
  const resp = await fetch(`${BASE}/matches/${profileId}/${jobId}/materials${force ? '?force=true' : ''}`, {
    method: 'POST',
    headers: { Accept: 'application/json', ...authHeaders() },
  })
  if (!resp.ok) {
    let msg = `${resp.status} ${resp.statusText}`
    try {
      const body = await resp.json() as { message?: string; error?: string }
      msg = body.message ?? body.error ?? msg
    } catch { /* gövde JSON değil */ }
    throw new Error(msg)
  }
  return (await resp.json()) as ApplicationMaterial
}
