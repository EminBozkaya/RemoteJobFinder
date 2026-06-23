import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { fetchProfile, updateProfile } from '@/api/client'
import type { Profile, ProfileEdit } from '@/api/types'
import { Button } from '@/components/ui/Button'

/** Etiket girişi: yaz → Enter → çip; çipe tıkla/× → sil. */
function TagInput({ label, hint, tags, onChange }: {
  label: string
  hint?: string
  tags: string[]
  onChange: (next: string[]) => void
}) {
  const [draft, setDraft] = useState('')

  const add = () => {
    const v = draft.trim()
    if (v && !tags.some((t) => t.toLowerCase() === v.toLowerCase())) onChange([...tags, v])
    setDraft('')
  }

  return (
    <div className="flex flex-col gap-1.5">
      <span className="text-sm font-medium">{label}</span>
      {hint && <span className="text-xs text-[color:var(--color-muted)]">{hint}</span>}
      <div className="flex flex-wrap items-center gap-1.5 rounded-md border border-[color:var(--color-border)] bg-[color:var(--color-card)] p-2">
        {tags.map((t) => (
          <button
            key={t}
            type="button"
            onClick={() => onChange(tags.filter((x) => x !== t))}
            className="inline-flex items-center gap-1 rounded-full bg-[color:var(--color-accent)]/10 px-2 py-0.5 text-xs text-[color:var(--color-fg)] hover:bg-[color:var(--color-danger)]/15"
            title="Kaldır"
          >
            {t} <span className="text-[color:var(--color-muted)]">✕</span>
          </button>
        ))}
        <input
          value={draft}
          onChange={(e) => setDraft(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === 'Enter') { e.preventDefault(); add() }
            else if (e.key === 'Backspace' && draft === '' && tags.length > 0) onChange(tags.slice(0, -1))
          }}
          onBlur={add}
          placeholder="yaz, Enter…"
          className="min-w-24 flex-1 bg-transparent px-1 py-0.5 text-sm outline-none"
        />
      </div>
    </div>
  )
}

export function ProfilePage({ onBack }: { onBack: () => void }) {
  const { data, isLoading, isError, error } = useQuery({ queryKey: ['profile'], queryFn: fetchProfile })

  return (
    <main className="mx-auto max-w-2xl px-4 py-8">
      <header className="mb-6 flex items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Kriterler</h1>
          <p className="text-sm text-[color:var(--color-muted)]">
            Değişiklikler token harcamadan, saklı verilerden yeniden hesaplanır.
          </p>
        </div>
        <Button variant="ghost" size="sm" onClick={onBack}>← Eşleşmeler</Button>
      </header>

      {isLoading && <p className="text-sm text-[color:var(--color-muted)]">Yükleniyor…</p>}
      {isError && <p className="text-sm text-[color:var(--color-danger)]">Profil okunamadı: {(error as Error).message}</p>}
      {data && <ProfileForm profile={data} />}
    </main>
  )
}

function ProfileForm({ profile }: { profile: Profile }) {
  const qc = useQueryClient()
  const [form, setForm] = useState<ProfileEdit>(() => ({
    residenceCountry: profile.residenceCountry,
    requiredKeywords: profile.requiredKeywords,
    forbiddenKeywords: profile.forbiddenKeywords,
    niceKeywords: profile.niceKeywords,
    timezoneToleranceHours: profile.timezoneToleranceHours,
    minScoreToShow: profile.minScoreToShow,
  }))

  const save = useMutation({
    mutationFn: () => updateProfile(profile.id, form),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['matches'] })
      qc.invalidateQueries({ queryKey: ['profile'] })
    },
  })

  return (
        <div className="flex flex-col gap-5">
          <TagInput
            label="Gerekli teknolojiler (puanı artırır)"
            hint="Başlıkta geçerse tam, gövdede kısmi puan. Eşleşmese bile ilan elenmez."
            tags={form.requiredKeywords}
            onChange={(v) => setForm({ ...form, requiredKeywords: v })}
          />
          <TagInput
            label="Bonus teknolojiler (nice-to-have)"
            tags={form.niceKeywords}
            onChange={(v) => setForm({ ...form, niceKeywords: v })}
          />
          <TagInput
            label="Yasaklı kelimeler (ilanı tamamen eler)"
            hint="Başlık/gövdede geçen ilan listeye hiç girmez (ör. php, wordpress)."
            tags={form.forbiddenKeywords}
            onChange={(v) => setForm({ ...form, forbiddenKeywords: v })}
          />

          <div className="grid gap-4 sm:grid-cols-3">
            <label className="flex flex-col gap-1.5">
              <span className="text-sm font-medium">İkamet ülkesi</span>
              <input
                value={form.residenceCountry}
                onChange={(e) => setForm({ ...form, residenceCountry: e.target.value })}
                className="rounded-md border border-[color:var(--color-border)] bg-[color:var(--color-card)] px-2 py-1.5 text-sm"
              />
            </label>
            <label className="flex flex-col gap-1.5">
              <span className="text-sm font-medium">Zaman dilimi toleransı (saat)</span>
              <input
                type="number" min={0} max={24}
                value={form.timezoneToleranceHours}
                onChange={(e) => setForm({ ...form, timezoneToleranceHours: parseInt(e.target.value || '0', 10) })}
                className="rounded-md border border-[color:var(--color-border)] bg-[color:var(--color-card)] px-2 py-1.5 text-sm"
              />
            </label>
            <label className="flex flex-col gap-1.5">
              <span className="text-sm font-medium">Min. skor: {form.minScoreToShow.toFixed(1)}</span>
              <input
                type="range" min={0} max={10} step={0.5}
                value={form.minScoreToShow}
                onChange={(e) => setForm({ ...form, minScoreToShow: parseFloat(e.target.value) })}
              />
            </label>
          </div>

          <div className="flex items-center gap-3">
            <Button onClick={() => save.mutate()} disabled={save.isPending}>
              {save.isPending ? 'Kaydediliyor…' : 'Kaydet ve yeniden hesapla'}
            </Button>
            {save.isSuccess && (
              <span className="text-sm text-[color:var(--color-muted)]">
                ✓ Kaydedildi · {save.data.recomputed} eşleşme yeniden hesaplandı
              </span>
            )}
            {save.isError && (
              <span className="text-sm text-[color:var(--color-danger)]">Hata: {(save.error as Error).message}</span>
            )}
          </div>
        </div>
  )
}
