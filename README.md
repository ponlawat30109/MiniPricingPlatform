# Mini Pricing Platform

A .NET 9 pricing platform with an Angular 22 Operations Console for calculating quotes, maintaining pricing rules, and processing bulk quote jobs.

## Repository layout

- `apps/rule-service` — pricing-rule API with concurrency-safe JSON persistence.
- `apps/pricing-service` — single-quote pricing and bounded in-memory bulk jobs.
- `apps/pricing-web` — Angular Operations Console with Quotes, Rules, and Jobs workspaces.
- `tests` — isolated xUnit test projects that use temporary rule storage.
- `data` — runtime rules and sample bulk input mounted into Rule Service by Docker Compose.

Bulk jobs are intentionally stored in memory. They do not survive Pricing Service restarts, and horizontal scaling requires external durable storage.

## Prerequisites

For local development:

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Node.js `22.22.3+`, `24.15+`, or `26+`
- npm, included with Node.js

For the container workflow, install Docker Desktop with Docker Compose support. Docker replaces the local .NET and Node.js requirements.

## Initial local setup

From the repository root in PowerShell:

```powershell
dotnet restore PricingPlatform.sln

Set-Location apps/pricing-web
npm ci
Set-Location ../..
```

## Run locally

Start each application in a separate terminal from the repository root.

Terminal 1 — Rule Service on port 5000:

```powershell
dotnet run --project apps/rule-service --no-launch-profile
```

Terminal 2 — Pricing Service on port 8080:

```powershell
dotnet run --project apps/pricing-service --no-launch-profile --urls http://localhost:8080
```

Terminal 3 — Angular development server on port 4200:

```powershell
Set-Location apps/pricing-web
npm start
```

Open:

- [Operations Console](http://localhost:4200)
- [Rule Service Swagger](http://localhost:5000/swagger/index.html)
- [Pricing Service Swagger](http://localhost:8080/swagger/index.html)

The Angular development build calls the two APIs directly on ports 5000 and 8080. Both `localhost:4200` and `127.0.0.1:4200` are allowed development origins.

If a restricted Windows account reports that it cannot open the `.NET Runtime` event-log source, set this variable in that service terminal before running it:

```powershell
$env:Logging__EventLog__LogLevel__Default = 'None'
```

## Run with Docker

Stop local applications using ports 3000, 5000, or 8080, then run:

```powershell
docker compose up --build
```

Open:

- [Operations Console](http://localhost:3000)
- [Rule Service Swagger](http://localhost:5000/swagger/index.html)
- [Pricing Service Swagger](http://localhost:8080/swagger/index.html)

Stop the stack with:

```powershell
docker compose down
```

Compose mounts the repository's `data` directory at `/app/data` in Rule Service. Rule changes made through the console or API therefore persist in the host file `data/rules.json`.

The production Angular container uses same-origin nginx paths (`/rule-api` and `/pricing-api`) and proxies them to the corresponding Compose services.

## Delivery areas and sample rules

The supplied demonstration catalog recognizes these selectable area values:

- `Bangkok` — no area surcharge.
- `Bangkok Metropolitan Region` — ฿30 surcharge.
- `Other Provinces` — ฿80 surcharge.

The console also accepts custom area text. Area matching is trimmed, case-insensitive, and exact; a custom value receives no surcharge unless a matching rule exists.

Runtime rules are stored in `data/rules.json`. The supplied rates and July 2026 promotion are demonstration data, not approved commercial tariffs.

## API examples

PowerShell examples use `curl.exe` to avoid the PowerShell `curl` alias.

Calculate one quote:

```powershell
curl.exe -X POST http://localhost:8080/quotes/price `
  -H "Content-Type: application/json" `
  -d '{"weight":15,"area":"Other Provinces"}'
```

Submit a JSON bulk job:

```powershell
curl.exe -X POST http://localhost:8080/quotes/bulk `
  -H "Content-Type: application/json" `
  -d '{"quotes":[{"weight":2.5,"area":"Bangkok"},{"weight":25,"area":"Bangkok Metropolitan Region"}]}'
```

Submit the sample CSV:

```powershell
curl.exe -X POST http://localhost:8080/quotes/bulk `
  -F "file=@data/bulk_quotes.csv"
```

Both bulk submissions return HTTP 202 with:

```json
{ "job_id": "..." }
```

Poll the returned job identifier:

```powershell
curl.exe http://localhost:8080/jobs/{job_id}
```

Validation and operational failures use RFC 7807 problem details. Rule creation uses `/rules/promotion`, `/rules/surcharge`, and `/rules/weight-tier`; editing uses `PUT /rules/{id}`.

## Development checks

Run backend tests from the repository root:

```powershell
dotnet test PricingPlatform.sln --no-restore
```

Run Angular checks from `apps/pricing-web`:

```powershell
npm run lint
npm test
npm run build
```

Playwright uses the Angular server at `http://127.0.0.1:4200` and the installed Chrome browser. Keep `npm start` running in another terminal, then run:

```powershell
npm run e2e
```

Validate Compose without starting containers:

```powershell
docker compose config --quiet
```

## Security configuration

Angular environment files are compiled into browser bundles. Keep only public API URLs and
feature flags in them; they are not a safe place for credentials.

Backend secrets must come from environment variables, .NET user secrets, or the deployment
platform's secret storage. Never place passwords, API keys, tokens, certificates, or connection
strings in committed `appsettings` files.
