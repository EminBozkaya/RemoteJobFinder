import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MatchesPage } from '@/features/matches/MatchesPage'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { staleTime: 30_000, refetchOnWindowFocus: false },
  },
})

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <MatchesPage />
    </QueryClientProvider>
  )
}
