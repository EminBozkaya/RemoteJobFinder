# JobScanner.Web

React SPA (Vite + TypeScript) — JobScanner.Api'den eşleşmeleri gösterir, durum makinesi
mutasyonlarını (`save`/`open`/`apply`/`dismiss`) çağırır.

## Stack
- Vite + React 19 + TypeScript
- TanStack Query (server state)
- Tailwind v4 (`@tailwindcss/vite` plugin, CSS-first config)
- shadcn-style komponentler (Radix Slot + cva), `@/*` path alias

## Çalıştır
1. JobScanner.Api'yi başlat (`dotnet run --project src/JobScanner.Api`) → `:5163`
2. Bu dizinde:
   ```
   npm install
   npm run dev
   ```
3. Tarayıcıda `http://localhost:5173` aç.

Vite `/api/*` isteklerini `:5163`'e proxy'liyor (CORS dert değil).

## Yapı
```
src/
├── api/         # tipler + fetch client (BASE='/api')
├── components/  # paylaşılan UI (Button, Badge)
├── features/    # alan-özgü modüller (matches/)
├── lib/         # cn() helper
├── App.tsx      # QueryClientProvider + ana sayfa
├── main.tsx     # React kök
└── index.css    # Tailwind v4 + tema
```

## Build
`npm run build` — `tsc -b && vite build`, çıktı `dist/`.
