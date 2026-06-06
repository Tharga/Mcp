# Plan: icon-and-docs

Branch: `feature/icon-and-docs`
Feature: see `plan/feature.md`

## Steps

### 1. PackageIconUrl migration [~]
- Update `<PackageIconUrl>` in `Tharga.Mcp/Tharga.Mcp.csproj` to `https://thargelion.net/assets/component-mcp.png`

### 2. Docs scaffolding [ ]
- Create `docs/CNAME` → `mcp.tharga.net`
- Create `docs/docfx.json` (adapted from Fortnox)
- Create `docs/toc.yml`
- Create `docs/index.md` (landing)
- Create `docs/articles/toc.yml` + `index.md` + 4 article files
- Create `docs/templates/thg/public/main.css` (size constraint for the URL-loaded logo)

### 3. Docs workflow [ ]
- Copy `.github/workflows/docs.yml` from Fortnox, adjust the `paths` filter to `Tharga.Mcp/**.cs`

### 4. README link [ ]
- Add `**Docs:** [mcp.tharga.net](https://mcp.tharga.net)` line

### 5. Local docs build verification [ ]
- `dotnet tool install -g docfx` (if not present) then `docfx docs/docfx.json` — verify `docs/_site` is produced without errors
- Spot-check the generated HTML in `docs/_site` opens with the logo loaded from the URL and the navbar correctly sized

### 6. Close Requests.md entries [ ]
- PackageIconUrl `### Tharga.Mcp` → Done
- Docs site `### Tharga.Mcp` → Done

### 7. Close feature [ ]
- Archive plan/ to done/
- Final commit

## Commit milestones

- After step 1: `chore: migrate PackageIconUrl to thargelion.net/assets`
- After step 4: `docs: add DocFX site for mcp.tharga.net`
- After step 6: `chore: close icon and docs site requests`
- After step 7: `feat: icon-and-docs complete`

## Progress log

_(updated during implementation)_

## Last session

_(updated at end of session)_
