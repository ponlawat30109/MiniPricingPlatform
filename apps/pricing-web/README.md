# Pricing Operations Console

Angular 22 standalone application for calculating delivery quotes, maintaining pricing rules, and monitoring bulk quote jobs.

For full-platform setup, Docker instructions, and API examples, see the [repository README](../../README.md).

## Prerequisites

- Node.js `22.22.3+`, `24.15+`, or `26+`
- npm
- Rule Service running at `http://localhost:5000`
- Pricing Service running at `http://localhost:8080`

## Install and run

From this directory:

```powershell
npm ci
npm start
```

Open [http://localhost:4200](http://localhost:4200). The development server accepts connections on all interfaces; both `localhost` and `127.0.0.1` are supported by the APIs' development CORS policies.

## Workspaces

- `/quotes` — calculate a quote and inspect the ordered pricing breakdown.
- `/rules` — review, create, edit, and delete weight, area-surcharge, and promotion rules.
- `/jobs` — submit JSON or CSV bulk jobs, follow status, and export completed results.

Delivery-area inputs suggest Bangkok, Bangkok Metropolitan Region, and Other Provinces while still accepting custom text.

## API configuration

Local development uses `src/environments/environment.ts`:

- Rule Service: `http://localhost:5000`
- Pricing Service: `http://localhost:8080`

Production builds use `src/environments/environment.production.ts` with same-origin `/rule-api` and `/pricing-api` paths. The container's nginx configuration proxies those paths to the Docker Compose services, so feature code does not contain deployment URLs.

## Commands

```powershell
npm start       # Angular development server on port 4200
npm run lint    # Angular ESLint
npm test        # Vitest unit tests
npm run build   # optimized production build
npm run e2e     # Playwright browser tests
```

The Playwright suite expects:

- The Angular development server already running at `http://127.0.0.1:4200`.
- Google Chrome installed; the Playwright configuration uses the Chrome channel.

The browser tests mock API responses to cover quote, rule-panel, job, theme, mobile, and custom-area interactions deterministically. Use the full local or Docker setup for unmocked API testing.

## Production container

The multi-stage Docker build compiles Angular and serves the generated application with nginx on port 80. From the repository root, Compose exposes it at [http://localhost:3000](http://localhost:3000).
