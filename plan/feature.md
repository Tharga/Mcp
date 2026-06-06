# Feature: icon-and-docs

## Goal

Land two cross-project requests for Tharga.Mcp in a single PR, mirroring how Tharga.Fortnox shipped them (PR #15 and PR #16):

1. **PackageIconUrl migration** — move from `thargelion.se/wp-content/uploads/...` to the canonical `https://thargelion.net/assets/component-mcp.png` so package metadata on nuget.org no longer depends on WordPress upload paths.
2. **Documentation site** — DocFX-built docs deployed via GitHub Pages, served at `mcp.tharga.net`.

Tracked in `$DOC_ROOT/Tharga/Requests.md`:
- *"Migrate PackageIconUrl to thargelion.net/assets/"* → `### Tharga.Mcp` (line 1640, parent at 1625)
- *"Documentation sites under tharga.net"* → `### Tharga.Mcp` (line 1764, parent at 1740)

## Pre-reqs (confirmed)

- `https://thargelion.net/assets/component-mcp.png` is uploaded.
- DNS `mcp.tharga.net` → `tharga.github.io`.
- `github-pages` environment configured in the Tharga/Mcp repo.

## Scope

**In scope:**
- `Tharga.Mcp/Tharga.Mcp.csproj` — update `<PackageIconUrl>` to `https://thargelion.net/assets/component-mcp.png`.
- New `docs/` folder, layout mirroring Tharga.Fortnox:
  - `CNAME` → `mcp.tharga.net`
  - `docfx.json` — adapted to `Tharga.Mcp` (app name/title, src csproj, point logo + favicon at `https://thargelion.net/assets/component-mcp.png` instead of a local copy)
  - `toc.yml` (Home / Articles / API)
  - `index.md` — landing page (Tharga.Mcp-specific)
  - `articles/` — 4 articles + toc + index:
    - `getting-started.md` — install + minimal `AddThargaMcp` / `UseThargaMcp`
    - `providers.md` — `IMcpToolProvider` / `IMcpResourceProvider`, `McpScope`, list/call dispatch
    - `scopes.md` — User/Team/System hierarchy filter, how Platform.Mcp populates context
    - `authorization.md` — `RequireAuth` flag, `UseAuthorization()` wiring
  - `templates/thg/public/main.css` — CSS to constrain the URL-loaded logo to 32px height (since the source is 150×150 like Fortnox)
- New `.github/workflows/docs.yml` — DocFX build + GitHub Pages deploy, mirroring Fortnox
- `README.md` — add a "Docs:" link pointing at `mcp.tharga.net`
- Mark both Requests.md entries Done with summary

**Out of scope:**
- Per-project shared docs template (each project gets its own template/thg now; that's the Fortnox pattern)
- Article content beyond the 4 listed — keep this PR scoped; future articles can be incremental

## Acceptance criteria

- `Tharga.Mcp.csproj` `PackageIconUrl` resolves to the canonical URL.
- `docs/` builds locally with `docfx docs/docfx.json` (verify before commit).
- `docs.yml` workflow file is syntactically valid; GitHub Pages deploy happens on next master push.
- README has a docs link.
- Both Requests.md entries marked Done.
- All existing 27 tests still pass; nothing in `Tharga.Mcp` code changes.

## Done condition

- PR merged via CI
- Close feature per shared-instructions
