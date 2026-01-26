RiftVeil är root-mappen. I den ska det skapas en backend och en frontend mapp, där respektive filer ska installeras. I .gitignore ska även RiftVeil/_docs och allt under läggas till

```
RiftVeil
  backend
  frontend
  _docs
```

.NET ska använda 8, long term. Inte 9.

## Repo setup

- [ ] Skapa repo på GitHub (monorepo)
- [ ] Skapa `.gitignore` (inkludera t.ex. `**/*.db`, `**/*.db-shm`, `**/*.db-wal`, `bin/`, `obj/`, `node_modules/`, `.env.local`)
- [ ] Git init + initial commit
- [ ] Push till GitHub
- [ ] Skapa kort README

## Backend setup

- [x] Skapa .NET solution med 4 projekt (API, Application, Domain, Infrastructure)
- [x] Sätt projekt-referenser enligt beroenden:
  - `Domain` har inga beroenden
  - `Application` → `Domain`
  - `Infrastructure` → `Application` (+ ev `Domain` om behövs)
  - `API` → `Application` + `Infrastructure`
- [x] Installera Swashbuckle (Swagger) i API-projektet
- [x] Installera Entity Framework Core-paket (SQLite + Design/Tools) enligt behov
- [x] Setup Swagger i `Program.cs`
- [x] Setup enkel CORS för `http://localhost:5173`
- [x] Skapa `HealthController` med `GET /api/health`

### Verifiering (backend)

- [x] Verifiera att `dotnet build` lyckas från solution-roten
- [x] Verifiera att `dotnet run` (API-projektet) startar utan fel
- [x] Verifiera Swagger UI via den URL som loggas vid start (typiskt `/swagger`)

## Database setup (SQLite, lokalt i AppData)

Mål: SQLite används lokalt för dev. Databasen behöver inte vara “synlig i repo” och ska inte committas.

- [ ] Konfigurera connection string i `appsettings.Development.json`
  - [ ] SQLite-fil ligger i AppData (lokal path), t.ex. via `Environment.SpecialFolder.LocalApplicationData`
- [ ] Skapa `ApplicationDbContext` (lägg den där den passar i arkitekturen, vanligtvis `Infrastructure`)
- [ ] Registrera DbContext i `Program.cs` (API)

### EF tooling (lokalt tool manifest)

- [ ] Skapa tool manifest i backend-roten (eller repo-roten, välj en plats och håll dig till den)
  - [ ] `dotnet new tool-manifest`
- [ ] Installera `dotnet-ef` lokalt
  - [ ] `dotnet tool install dotnet-ef`

### Migrations

- [ ] Skapa initial migration
- [ ] Applicera migration
- [ ] Verifiera databasen (fil skapas i AppData + tabeller finns)

## Frontend setup

- [ ] Skapa React + Vite + TypeScript app
- [ ] Installera dependencies
  - [ ] `react-router-dom`
  - [ ] `@tanstack/react-query`
  - [ ] `date-fns` och `date-fns-tz`
- [ ] Skapa `.env.local` med `VITE_API_BASE_URL` (hardkodat OK i MVP)
- [ ] Setup `QueryClientProvider` i `main.tsx`
- [ ] Skapa API client (`src/lib/api.ts`)
- [ ] Uppdatera `App.tsx` för att anropa `GET /api/health` och visa resultat

### Verifiering (frontend)

- [ ] Verifiera `npm run dev` startar
- [ ] Verifiera frontend visar health-responsen
- [ ] Verifiera inga CORS errors i console
