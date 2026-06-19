import { useMutation } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { generateMaterials } from '@/api/client'
import type { ApplicationMaterial } from '@/api/types'
import { Button } from '@/components/ui/Button'

type Tab = 'cover' | 'cv'

function download(filename: string, text: string) {
  const blob = new Blob([text], { type: 'text/markdown;charset=utf-8' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = filename
  a.click()
  URL.revokeObjectURL(url)
}

function slug(s: string): string {
  return s.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/(^-|-$)/g, '').slice(0, 40) || 'ilan'
}

export function MaterialsDialog({
  profileId, jobId, title, company, onClose,
}: {
  profileId: number
  jobId: number
  title: string
  company: string
  onClose: () => void
}) {
  const [tab, setTab] = useState<Tab>('cover')
  const [copied, setCopied] = useState(false)

  const gen = useMutation<ApplicationMaterial, Error, boolean>({
    mutationFn: (force: boolean) => generateMaterials(profileId, jobId, force),
  })

  // Açılışta bir kez üret/getir (taze kayıt varsa token harcamaz).
  useEffect(() => {
    gen.mutate(false)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  // Esc ile kapat
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => { if (e.key === 'Escape') onClose() }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [onClose])

  const material = gen.data
  const activeText = material ? (tab === 'cover' ? material.coverLetter : material.tailoredCvMarkdown) : ''

  const copy = async () => {
    await navigator.clipboard.writeText(activeText)
    setCopied(true)
    setTimeout(() => setCopied(false), 1500)
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
      onClick={onClose}
    >
      <div
        className="flex max-h-[90vh] w-full max-w-3xl flex-col overflow-hidden rounded-lg border border-[color:var(--color-border)] bg-[color:var(--color-card)] shadow-xl"
        onClick={(e) => e.stopPropagation()}
      >
        <header className="flex items-start justify-between gap-3 border-b border-[color:var(--color-border)] p-4">
          <div className="min-w-0">
            <h3 className="truncate text-base font-semibold">Başvuru materyali</h3>
            <p className="truncate text-sm text-[color:var(--color-muted)]">{title} · {company}</p>
          </div>
          <Button size="sm" variant="ghost" onClick={onClose}>Kapat ✕</Button>
        </header>

        <div className="flex items-center gap-2 border-b border-[color:var(--color-border)] px-4 py-2">
          <Button size="sm" variant={tab === 'cover' ? 'default' : 'outline'} onClick={() => setTab('cover')}>
            Ön Yazı
          </Button>
          <Button size="sm" variant={tab === 'cv' ? 'default' : 'outline'} onClick={() => setTab('cv')}>
            CV
          </Button>
          {material && (
            <span className="ml-2 text-xs text-[color:var(--color-muted)]">dil: {material.language}</span>
          )}
          <div className="ml-auto flex gap-2">
            <Button size="sm" variant="outline" disabled={!material} onClick={copy}>
              {copied ? 'Kopyalandı ✓' : 'Kopyala'}
            </Button>
            <Button
              size="sm"
              variant="outline"
              disabled={!material}
              onClick={() => download(`${tab === 'cover' ? 'cover-letter' : 'cv'}-${slug(company)}-${slug(title)}.md`, activeText)}
            >
              İndir (.md)
            </Button>
            <Button size="sm" variant="ghost" disabled={gen.isPending} onClick={() => gen.mutate(true)}>
              Yeniden üret
            </Button>
          </div>
        </div>

        <div className="overflow-auto p-4">
          {gen.isPending && (
            <p className="text-sm text-[color:var(--color-muted)]">
              Materyal üretiliyor… (lokal LLM'de CPU'da bir dakikayı bulabilir)
            </p>
          )}
          {gen.isError && (
            <div className="text-sm text-[color:var(--color-danger)]">
              <p>Üretilemedi: {gen.error.message}</p>
              <Button className="mt-2" size="sm" variant="outline" onClick={() => gen.mutate(false)}>
                Tekrar dene
              </Button>
            </div>
          )}
          {material && !gen.isPending && (
            <pre className="whitespace-pre-wrap break-words font-sans text-sm leading-relaxed">{activeText}</pre>
          )}
        </div>
      </div>
    </div>
  )
}
