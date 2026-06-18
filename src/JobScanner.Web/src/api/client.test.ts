import { afterEach, beforeEach, describe, expect, test, vi } from 'vitest'
import { fetchMatches, mutateMatch } from './client'

type FetchSpy = ReturnType<typeof vi.fn>

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

describe('api/client', () => {
  let fetchMock: FetchSpy

  beforeEach(() => {
    fetchMock = vi.fn().mockResolvedValue(jsonResponse([]))
    vi.stubGlobal('fetch', fetchMock)
    window.localStorage.clear()
  })
  afterEach(() => vi.unstubAllGlobals())

  test('fetchMatches: bos sorgu icin /matches', async () => {
    await fetchMatches({})
    const url = fetchMock.mock.calls[0][0] as string
    expect(url).toBe('/api/matches')
  })

  test('fetchMatches: tum parametreleri querystring olur', async () => {
    await fetchMatches({ profileId: 1, minScore: 6.5, take: 25, source: 'RemoteOK' })
    const url = fetchMock.mock.calls[0][0] as string
    expect(url).toMatch(/^\/api\/matches\?/)
    expect(url).toContain('profileId=1')
    expect(url).toContain('minScore=6.5')
    expect(url).toContain('take=25')
    expect(url).toContain('source=RemoteOK')
  })

  test('fetchMatches: bos source param eklenmez', async () => {
    await fetchMatches({ source: '' })
    const url = fetchMock.mock.calls[0][0] as string
    expect(url).not.toContain('source=')
  })

  test('localStorage token varsa Authorization header gonderilir', async () => {
    window.localStorage.setItem('jobscanner_token', 'secret')
    await fetchMatches({})
    const init = fetchMock.mock.calls[0][1] as RequestInit
    const headers = init.headers as Record<string, string>
    expect(headers.Authorization).toBe('Bearer secret')
  })

  test('localStorage token yoksa Authorization header gonderilmez', async () => {
    await fetchMatches({})
    const init = fetchMock.mock.calls[0][1] as RequestInit
    const headers = init.headers as Record<string, string>
    expect(headers.Authorization).toBeUndefined()
  })

  test('mutateMatch: POST + dogru path', async () => {
    fetchMock.mockResolvedValue(new Response(null, { status: 204 }))
    await mutateMatch(1, 42, 'apply')
    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit]
    expect(url).toBe('/api/matches/1/42/apply')
    expect(init.method).toBe('POST')
  })

  test('non-OK yanit hataya cevirilir', async () => {
    fetchMock.mockResolvedValue(new Response('not found', { status: 404, statusText: 'Not Found' }))
    await expect(mutateMatch(1, 999, 'dismiss')).rejects.toThrow(/404/)
  })
})
