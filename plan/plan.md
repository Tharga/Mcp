# Plan: Expose session mode on `ThargaMcpOptions`

Branch: `feature/session-mode` (from `master`)
Scope: `plan/feature.md`

## Steps

- [x] **1. Upgrade NuGet packages (mandatory, up front)** — done.
      `xunit.v3` 3.2.2 → 4.0.0 and `xunit.runner.visualstudio` 3.1.5 → 4.0.0.
      Build 0 warnings, 36/36 tests pass. `Tharga.Mcp` and the sample had no updates.

      **This was not a version bump — it was a test-platform migration.** xunit.v3 4.0.0
      pulls Microsoft.Testing.Platform 2.x, whose VSTest bridge the .NET 10 SDK refuses
      outright: *"Testing with VSTest target is no longer supported by
      Microsoft.Testing.Platform on .NET 10 SDK and later."* Every `dotnet test` failed
      before a single test ran. Three consequential changes followed:
      - **`global.json` added** with `test.runner = Microsoft.Testing.Platform`. This is the
        only opt-in the .NET 10 SDK accepts — the `TestingPlatformDotnetTestSupport` MSBuild
        property is the .NET 9 mechanism and is ignored here, and `dotnet.config` is not read
        for this. No `sdk` section, so CI's `setup-dotnet` with `dotnet-version: 10.0.x` is
        unaffected.
      - **Coverage collector swapped.** `coverlet.collector` is VSTest-only, so
        `--collect:"XPlat Code Coverage"` no longer exists — CI would have collected nothing.
        Replaced with `Microsoft.Testing.Extensions.CodeCoverage` 18.10.0, which provides the
        MTP-native `--coverage`. `coverlet.msbuild` removed too rather than leaving two
        coverage systems in one project.
      - **CI test step rewritten** in `.github/workflows/build.yml` to
        `dotnet test -c Release --no-build --results-directory ./coverage -- --coverage
        --coverage-output-format cobertura --coverage-output coverage.cobertura.xml`.
        Verified locally that this writes `./coverage/coverage.cobertura.xml`, which is
        exactly the path the existing Codecov step already points at, so that step is
        unchanged. `--verbosity normal` dropped: under MTP it sets MSBuild verbosity, not
        test verbosity, so it was only ever noise here.

- [x] **2. Add the `McpSessionMode` contract** — done. `Tharga.Mcp/McpSessionMode.cs`.
      New `Tharga.Mcp/McpSessionMode.cs` with `Stateless`, `Stateful` and
      `StatefulForInitializeClients`. XML docs on each member say what it costs, not just what
      it is — `Stateful` refuses `2026-07-28`+ requests with `-32022 UnsupportedProtocolVersion`
      and forces a downgrade; `Stateless` disables the GET/DELETE/`/sse` endpoints and client
      sampling, elicitation and roots.

- [x] **3. Map it to the SDK** — done. `ToSdkSessionMode` throws on an unmapped value.
      Add `ToSdkSessionMode` to `Internal/McpTypeMappers.cs`, matching the file's existing
      `ToSdkX` naming. Throw on an unmapped value rather than defaulting, so adding a member
      without mapping it fails loudly instead of silently selecting stateless.

- [x] **4. Expose it on `ThargaMcpOptions`** — done. Defaults to `Stateless`.
      `public McpSessionMode SessionMode { get; set; } = McpSessionMode.Stateless;` with XML
      docs pointing at the escape hatch and naming the release that made it necessary.

- [x] **5. Wire it through `AddThargaMcp`** — done.
      `WithHttpTransport()` → `WithHttpTransport(o => o.SessionMode = ...)`, with the lambda
      closing over the singleton options instance so the value is read after the host's
      `configure(builder)` callback has run. See the decision in `plan/feature.md` — the
      eager read is the failure mode here.

- [x] **6. Tests** — done. 8 new tests in `Transport/SessionModeTests.cs`; 44/44 pass,
      0 build warnings.

      **Mutation-checked rather than assumed.** The ordering fix was temporarily reverted to
      the eager read and the suite re-run: 4 tests failed with
      *"Expected HttpServerSessionMode.StatefulForInitializeClients, but found
      HttpServerSessionMode.Stateless"*. The tests pin the trap they were written for.

      A seventh angle was added beyond the plan — `Both_enums_declare_the_same_members`
      compares `Enum.GetNames` on both sides, so an SDK member with no counterpart fails the
      build. The originally planned mapping test only catches members we add, not members the
      SDK adds, and the latter is the one that arrives without warning on an upgrade.


      New `Tharga.Mcp.Tests/Transport/SessionModeTests.cs`:
      - default is stateless when the host sets nothing;
      - a value set inside the `AddThargaMcp` callback reaches the resolved
        `HttpServerTransportOptions` (pins the ordering trap from step 5);
      - every `McpSessionMode` member maps to the same-named `HttpServerSessionMode` member,
        driven by `Enum.GetValues` so a new member cannot slip through;
      - `StatefulForInitializeClients` survives the round trip, which the SDK's own
        `Stateless` bool cannot represent — this is the case that justifies the enum.
      Run the full suite.

- [x] **7. Bump `MAJOR_MINOR`** — done. Set to `1.2` for the additive option, then moved to
      `2.0` when the `IsDeveloper` removal was added to scope (step 11).
      Additive public API, so a minor. `.github/workflows/build.yml`. The tag lookup is
      already guarded with `|| true` from the 1.1 series, so starting `1.2` will not break
      `Compute version`.

- [x] **8. Docs** — done. Both surfaces updated, as the workflow requires when both exist.
      - **New** `docs/articles/session-mode.md` — the docs follow one file per area and this is
        a transport concern belonging to neither `authorization` nor `scopes`. Registered in
        `articles/toc.yml` and `articles/index.md`.
      - **README** — new "Session mode" section with the mode table.
      - **Corrected a claim this feature invalidated.** `getting-started.md:63` read *"Legacy
        stateful behavior is opt-in through the SDK's own transport options"* — precisely the
        gap PlutusWave filed, stated in the docs as though it were a solution. Rewritten to
        point at `SessionMode`.
      - `docs/index.md` "What's in the box" gained a Session mode bullet.
      README section on the option, and a `docs/` article (the site follows one file per
      area — check whether this belongs in the existing `getting-started` /
      `authorization` set or wants its own). Land as a separate `docs:` commit.

- [~] **9. Push and hand over for testing**
      **The user must run the push** — `git push` is in the `deny` list in
      `~/.claude/settings.json`, so it is blocked for this session rather than merely
      requiring approval:

          git push -u origin feature/session-mode

      **Do not open the PR** — the user tests from origin first. Implementation is complete
      and verified locally: 3 commits, build 0 warnings, 44/44 tests, coverage file lands
      where the Codecov step expects it.

- [ ] **10. Close-out (only after the user confirms)**
      Re-run the outdated check; update `Requests.md` ask 2 to Done with evidence and add the
      consumer follow-up entry; fix the two stale backlog records found during startup
      (`Toolkit/Mcp.md` says "0 pending" and still lists the already-shipped
      ModelContextProtocol 2.0.0 upgrade); archive `plan/feature.md` to the Plan directory
      `done/`; `git rm -r plan`; final commit `feat: session mode complete`; open the PR.

- [x] **11. Remove `IMcpContext.IsDeveloper`** — done (added to scope 2026-08-16).
      Removed from the interface, from `FallbackContext`, and from the three test fakes.
      `Scope`'s XML doc now states that it is *the* authorization signal on the context, which
      is what `IsDeveloper`'s doc wrongly claimed for itself. Build 0 warnings, 44/44 pass —
      no test needed changing beyond dropping the member, which is itself the evidence that
      nothing in this package depended on it.
      `docs/articles/providers.md` updated: the interface snippet, a note on what `UserId` /
      `TeamId` are for versus what `Scope` is for, a 2.0.0 removal callout, and a correction
      to the fallback-context description, which said *"all other fields null"* when
      `IsDeveloper` was `true` — understating how open the unbridged fallback is.

      **Consumer breakage to file at close-out:** `Tharga.Team.Mcp` on `origin/master`
      implements `IMcpContext` (`TeamMcpContext.cs:37`) and gates two resources on the member
      (`TeamSystemResourceProvider.cs:63`, `:135`). It will not compile against 2.0.0 and needs
      those gates rewritten. Any change there must start by loading Tharga.Team's own project
      rules.

## Notes

Startup sweep (2026-08-15): working tree clean, `master` level with origin, no open GitHub
issues on `Tharga/Mcp`, no open upstream requests. Two pending requests existed for this repo
and this branch now satisfies **both** — the session-mode ask under `## Tharga.Team — MCP`
(ask 2) and `IMcpContext.IsDeveloper` under `## Tharga.Mcp`.

## Last session

2026-08-15 — **Steps 1–8 complete; implementation is done and verified.** Three commits on
`feature/session-mode`:

1. `chore(deps):` xunit.v3 4.0.0 + the Microsoft.Testing.Platform migration it forced
   (`global.json` opt-in, coverage collector swap, CI test command rewrite).
2. `feat:` `McpSessionMode`, `ThargaMcpOptions.SessionMode`, the mapper, the wiring,
   8 tests, `MAJOR_MINOR` → `1.2`.
3. `docs:` README section, new `session-mode.md` article, and the correction to
   `getting-started.md`.

Build 0 warnings, 44/44 tests pass, and the CI-equivalent coverage command was run locally
and writes `./coverage/coverage.cobertura.xml` as the Codecov step expects.

**Next:** the user pushes `feature/session-mode` (push is denied to the session) and tests
from origin. No PR yet, by design. Step 10 close-out waits on the user confirming the
feature is done.

2026-08-16 — **Scope grew: `IMcpContext.IsDeveloper` removed (step 11).** The user asked what
the member did, and on being told the package never reads it, decided to remove it. The
release is now **2.0.0** rather than 1.2.0, because removing an interface member breaks every
implementer.

I raised that `Tharga.Team.Mcp` still gates on it and would break; the user reaffirmed the
removal, so it proceeded. Filing the Team-side work is now part of close-out.

Sequencing was not chosen by the user, so the removal went on this same branch as its own
commit. Splitting it into a separate PR is still cheap if wanted — nothing is pushed.

The PR title must name the break, since CI generates release notes from it.
