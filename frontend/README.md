# Berynx Construction 360 — Frontend

Independent Angular 22 standalone application. The workspace includes lazy
route loading, shared API and error-handling foundations, authentication guard
integration points, accessible loading/empty states, and a responsive shell.
No ERP workflows are implemented in Gate 0.
Angular Forms is included; future standalone feature components can import
`ReactiveFormsModule` without changing the workspace foundation.

## Requirements

- Node.js `24.15+` (verified locally with `24.19.0`)
- npm `11.6.2` (verified locally)

The workspace pins Angular packages to the `22.3` release line. Use the local
workspace CLI through npm scripts rather than the machine's global CLI.

## Run

```powershell
cd frontend
npm install
npm start
```

The development server listens at `http://localhost:4200`. Requests under
`/api` and `/health` are proxied to `http://localhost:5000` as configured in
`proxy.conf.json`.

## Build and tests

```powershell
npm run build
npm test
```

Vitest is Angular CLI's configured unit-test runner. Route guards and
authentication session checks are integration points only; the backend does
not yet expose a login or session endpoint.
