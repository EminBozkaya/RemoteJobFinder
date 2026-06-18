import { useEffect, useState } from 'react'
import { Button } from '@/components/ui/Button'

type Theme = 'light' | 'dark' | 'system'
const KEY = 'jobscanner_theme'

function applyTheme(t: Theme) {
  const root = document.documentElement
  const resolved =
    t === 'system'
      ? (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light')
      : t
  root.dataset.theme = resolved
}

function readStoredTheme(): Theme {
  const v = typeof window !== 'undefined' ? window.localStorage.getItem(KEY) : null
  return v === 'light' || v === 'dark' || v === 'system' ? v : 'system'
}

export function ThemeToggle() {
  const [theme, setTheme] = useState<Theme>(readStoredTheme)

  useEffect(() => {
    applyTheme(theme)
    window.localStorage.setItem(KEY, theme)
  }, [theme])

  useEffect(() => {
    if (theme !== 'system') return
    const mq = window.matchMedia('(prefers-color-scheme: dark)')
    const handler = () => applyTheme('system')
    mq.addEventListener('change', handler)
    return () => mq.removeEventListener('change', handler)
  }, [theme])

  const next: Record<Theme, Theme> = { light: 'dark', dark: 'system', system: 'light' }
  const label: Record<Theme, string> = { light: '☀ Light', dark: '☾ Dark', system: '⊙ Auto' }

  return (
    <Button
      variant="outline"
      size="sm"
      onClick={() => setTheme(next[theme])}
      title="Tema değiştir (light → dark → auto)"
    >
      {label[theme]}
    </Button>
  )
}
