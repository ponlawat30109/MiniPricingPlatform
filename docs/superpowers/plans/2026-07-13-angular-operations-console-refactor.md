# Angular Operations Console and Targeted Full-Stack Refactor

## Goal

Replace Next.js with an Angular 22 standalone operations console, reorganize the repository into `apps`, `tests`, `data`, and `docs`, and refactor the .NET 9 services for deterministic pricing, safe JSON persistence, validation, and bounded in-memory jobs.

## Architecture

- `apps/pricing-web`: Angular 22, Angular CDK, standalone components, signals, typed reactive forms, lazy routes.
- `apps/rule-service`: endpoints, validation, domain models, and JSON infrastructure separated into focused units.
- `apps/pricing-service`: endpoints, pricing engine, Rule Service client, and bounded background jobs separated.
- `tests`: isolated xUnit projects that never mutate runtime data.

## Required behavior

- Preserve current API routes and request shapes; add `PUT /rules/{id}` and RFC 7807 errors.
- Price in phases: highest-priority matching weight tier, all matching surcharges, then all matching promotions.
- Preserve bulk `202` response `{ "job_id": string }`; expose typed job state and failure details.
- Provide Quotes, Rules, and Jobs routes with a restrained light-first Operations Console and persisted dark mode.
- Avoid gradients, glassmorphism, decorative blobs, glows, repetitive cards, excessive radii/shadows, and ornamental animation.
- Keep JSON rule persistence and in-memory jobs; document their deployment limitations.

## Verification

Run .NET unit/integration tests, Angular unit tests and lint, Angular production build, browser workflow checks, responsive/accessibility checks, and Docker Compose validation. Generated artifacts must not be tracked.
