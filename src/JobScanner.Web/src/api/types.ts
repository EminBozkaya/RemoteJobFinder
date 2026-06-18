// JobScanner.Api Application.Abstractions.MatchView'in TS karsiligi.
export type DecisionKind = 'Eligible' | 'Uncertain' | 'Ineligible'
export type StateKind = 'New' | 'Saved' | 'Opened' | 'Applied' | 'Dismissed' | 'Expired'

export type ScoreContribution = { Criterion: string; Contribution: number }

export type MatchView = {
  profileId: number
  jobId: number
  title: string
  company: string
  url: string
  applyUrl: string | null
  score: number
  decision: DecisionKind
  state: StateKind
  postedAt: string | null
  scoreBreakdownJson: string // JSON encoded ScoreContribution[]
  decisionReasonsJson: string // JSON encoded string[]
}

export type MatchAction = 'save' | 'open' | 'apply' | 'dismiss'

export type MatchesQuery = {
  profileId?: number
  minScore?: number
  take?: number
}
