// JobScanner.Api Application.Abstractions.MatchView'in TS karsiligi.
export type DecisionKind = 'Eligible' | 'Uncertain' | 'Ineligible'
export type StateKind = 'New' | 'Saved' | 'Opened' | 'Applied' | 'Dismissed' | 'Expired'
export type LegitimacyKind = 'High' | 'Caution' | 'Suspicious'

export type ScoreContribution = { Criterion: string; Contribution: number }

export type MatchView = {
  profileId: number
  jobId: number
  sourceName: string
  title: string
  company: string
  url: string
  applyUrl: string | null
  score: number
  decision: DecisionKind
  state: StateKind
  legitimacy: LegitimacyKind
  postedAt: string | null
  scoreBreakdownJson: string // JSON encoded ScoreContribution[]
  decisionReasonsJson: string // JSON encoded string[]
  legitimacySignalsJson: string // JSON encoded string[]
}

export type MatchAction = 'save' | 'open' | 'apply' | 'dismiss'

// Faz 5a: arayüzden düzenlenebilir kriter profili.
export type Profile = {
  id: number
  name: string
  residenceCountry: string
  requiredKeywords: string[]
  forbiddenKeywords: string[]
  niceKeywords: string[]
  timezoneToleranceHours: number
  minScoreToShow: number
}

export type ProfileEdit = {
  residenceCountry: string
  requiredKeywords: string[]
  forbiddenKeywords: string[]
  niceKeywords: string[]
  timezoneToleranceHours: number
  minScoreToShow: number
}

// Faz 4: ilana özel üretilmiş başvuru materyali (cover letter + uyarlanmış CV).
export type ApplicationMaterial = {
  profileId: number
  jobId: number
  coverLetter: string
  tailoredCvMarkdown: string
  language: string
  generatedAt: string
}

export type MatchesQuery = {
  profileId?: number
  minScore?: number
  take?: number
  source?: string
}

export const KNOWN_SOURCES = ['Jobicy', 'RemoteOK', 'WeWorkRemotely', 'Remotive', 'Arbeitnow'] as const
export type KnownSource = typeof KNOWN_SOURCES[number]
