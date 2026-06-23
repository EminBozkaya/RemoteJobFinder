import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { useState } from 'react'
import { MatchesPage } from '@/features/matches/MatchesPage'
import { ProfilePage } from '@/features/profile/ProfilePage'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { staleTime: 30_000, refetchOnWindowFocus: false },
  },
})

export default function App() {
  const [view, setView] = useState<'matches' | 'settings'>('matches')

  return (
    <QueryClientProvider client={queryClient}>
      {view === 'matches'
        ? <MatchesPage onOpenSettings={() => setView('settings')} />
        : <ProfilePage onBack={() => setView('matches')} />}
    </QueryClientProvider>
  )
}
