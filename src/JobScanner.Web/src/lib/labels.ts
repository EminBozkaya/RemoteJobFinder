import type { DecisionKind, LegitimacyKind, StateKind } from '@/api/types'

// Backend enum'larini TR insan-okur etikete cevirir. Ileride i18n eklendiginde
// burasi locale-dispatch'e cevrilecek.
export const decisionLabels: Record<DecisionKind, string> = {
  Eligible: 'Uygun',
  Uncertain: 'Belirsiz',
  Ineligible: 'Uygun değil',
}

export const stateLabels: Record<StateKind, string> = {
  New: 'Yeni',
  Saved: 'Kaydedildi',
  Opened: 'Açıldı',
  Applied: 'Başvuruldu',
  Dismissed: 'Reddedildi',
  Expired: 'Süresi doldu',
}

export const legitimacyLabels: Record<LegitimacyKind, string> = {
  High: 'Güvenilir',
  Caution: 'Dikkat',
  Suspicious: 'Şüpheli',
}

// LLM/decider sinyal anahtarlarinin TR aciklamalari (rozet tooltip'i icin).
export const legitimacySignalLabels: Record<string, string> = {
  'ghost-language': 'JD jenerik/evergreen ifadelerle dolu',
  'recruiter-agency': 'Şirket değil, recruiter ajansı paylaşmış',
  'low-llm-confidence': 'LLM çıkarımı düşük güven',
}

export function explainSignal(signal: string): string {
  if (signal.startsWith('long-running-')) {
    const days = signal.replace('long-running-', '').replace('d', '')
    return `İlan ${days} gündür feed'lerde dolaşıyor`
  }
  return legitimacySignalLabels[signal] ?? signal
}
