import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { fetchProfile, updateProfile } from '@/api/client'
import { LANGUAGE_LEVELS, type Language, type LanguageLevel, type Profile, type ProfileEdit, type Skill } from '@/api/types'
import { Button } from '@/components/ui/Button'

const LEVEL_LABELS: Record<LanguageLevel, string> = {
  Beginner: 'Başlangıç',
  Intermediate: 'Orta',
  Advanced: 'İleri',
  Native: 'Anadil',
}

const inputCls = 'rounded-md border border-[color:var(--color-border)] bg-[color:var(--color-card)] px-2 py-1.5 text-sm'

/** Etiket girişi: yaz → Enter → çip; çipe tıkla → sil. */
function TagInput({ label, hint, tags, onChange }: {
  label: string; hint?: string; tags: string[]; onChange: (next: string[]) => void
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
            className="inline-flex items-center gap-1 rounded-full bg-[color:var(--color-accent)]/10 px-2 py-0.5 text-xs hover:bg-[color:var(--color-danger)]/15"
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

function SkillsEditor({ skills, onChange }: { skills: Skill[]; onChange: (s: Skill[]) => void }) {
  const patch = (i: number, p: Partial<Skill>) => onChange(skills.map((s, idx) => (idx === i ? { ...s, ...p } : s)))
  return (
    <div className="flex flex-col gap-1.5">
      <span className="text-sm font-medium">Yetkinlikler</span>
      <span className="text-xs text-[color:var(--color-muted)]">
        Teknoloji + öz-puan (1–10, puanlamaya) + tecrübe yılı (ilanın istediği yılla kıyaslanır).
      </span>
      <div className="flex flex-col gap-2">
        {skills.map((s, i) => (
          <div key={i} className="flex flex-wrap items-center gap-2 rounded-md border border-[color:var(--color-border)] p-2">
            <input
              value={s.name}
              onChange={(e) => patch(i, { name: e.target.value })}
              placeholder="ör. C#"
              className={`${inputCls} min-w-32 flex-1`}
            />
            <label className="flex items-center gap-1 text-xs text-[color:var(--color-muted)]">
              puan
              <input
                type="range" min={1} max={10} step={1} value={s.selfRating}
                onChange={(e) => patch(i, { selfRating: parseInt(e.target.value, 10) })}
              />
              <span className="w-5 tabular-nums text-[color:var(--color-fg)]">{s.selfRating}</span>
            </label>
            <label className="flex items-center gap-1 text-xs text-[color:var(--color-muted)]">
              yıl
              <input
                type="number" min={0} max={50} value={s.years}
                onChange={(e) => patch(i, { years: parseInt(e.target.value || '0', 10) })}
                className={`${inputCls} w-16`}
              />
            </label>
            <Button type="button" variant="ghost" size="sm" onClick={() => onChange(skills.filter((_, idx) => idx !== i))}>✕</Button>
          </div>
        ))}
      </div>
      <div>
        <Button type="button" variant="outline" size="sm" onClick={() => onChange([...skills, { name: '', selfRating: 5, years: 1 }])}>
          + Yetkinlik ekle
        </Button>
      </div>
    </div>
  )
}

function LanguagesEditor({ languages, onChange }: { languages: Language[]; onChange: (l: Language[]) => void }) {
  const patch = (i: number, p: Partial<Language>) => onChange(languages.map((l, idx) => (idx === i ? { ...l, ...p } : l)))
  return (
    <div className="flex flex-col gap-1.5">
      <span className="text-sm font-medium">Diller</span>
      <div className="flex flex-col gap-2">
        {languages.map((l, i) => (
          <div key={i} className="flex flex-wrap items-center gap-2 rounded-md border border-[color:var(--color-border)] p-2">
            <input
              value={l.name}
              onChange={(e) => patch(i, { name: e.target.value })}
              placeholder="ör. İngilizce"
              className={`${inputCls} min-w-32 flex-1`}
            />
            <select value={l.level} onChange={(e) => patch(i, { level: e.target.value as LanguageLevel })} className={inputCls}>
              {LANGUAGE_LEVELS.map((lvl) => <option key={lvl} value={lvl}>{LEVEL_LABELS[lvl]}</option>)}
            </select>
            <Button type="button" variant="ghost" size="sm" onClick={() => onChange(languages.filter((_, idx) => idx !== i))}>✕</Button>
          </div>
        ))}
      </div>
      <div>
        <Button type="button" variant="outline" size="sm" onClick={() => onChange([...languages, { name: '', level: 'Intermediate' }])}>
          + Dil ekle
        </Button>
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
    forbiddenKeywords: profile.forbiddenKeywords,
    skills: profile.skills,
    languages: profile.languages,
    softSkills: profile.softSkills,
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
      <SkillsEditor skills={form.skills} onChange={(v) => setForm({ ...form, skills: v })} />
      <LanguagesEditor languages={form.languages} onChange={(v) => setForm({ ...form, languages: v })} />
      <TagInput
        label="Soft skill'ler"
        tags={form.softSkills}
        onChange={(v) => setForm({ ...form, softSkills: v })}
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
          <input value={form.residenceCountry} onChange={(e) => setForm({ ...form, residenceCountry: e.target.value })} className={inputCls} />
        </label>
        <label className="flex flex-col gap-1.5">
          <span className="text-sm font-medium">Zaman dilimi toleransı (saat)</span>
          <input
            type="number" min={0} max={24} value={form.timezoneToleranceHours}
            onChange={(e) => setForm({ ...form, timezoneToleranceHours: parseInt(e.target.value || '0', 10) })}
            className={inputCls}
          />
        </label>
        <label className="flex flex-col gap-1.5">
          <span className="text-sm font-medium">Min. skor: {form.minScoreToShow.toFixed(1)}</span>
          <input
            type="range" min={0} max={10} step={0.5} value={form.minScoreToShow}
            onChange={(e) => setForm({ ...form, minScoreToShow: parseFloat(e.target.value) })}
          />
        </label>
      </div>

      <div className="flex items-center gap-3">
        <Button onClick={() => save.mutate()} disabled={save.isPending}>
          {save.isPending ? 'Kaydediliyor…' : 'Kaydet ve yeniden hesapla'}
        </Button>
        {save.isSuccess && (
          <span className="text-sm text-[color:var(--color-muted)]">✓ Kaydedildi · {save.data.recomputed} eşleşme yeniden hesaplandı</span>
        )}
        {save.isError && <span className="text-sm text-[color:var(--color-danger)]">Hata: {(save.error as Error).message}</span>}
      </div>
    </div>
  )
}
