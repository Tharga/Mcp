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

- [x] **7. Bump `MAJOR_MINOR` `1.1` → `1.2`** — done.
      Additive public API, so a minor. `.github/workflows/build.yml`. The tag lookup is
      already guarded with `|| true` from the 1.1 series, so starting `1.2` will not break
      `Compute version`.

- [~] **8. Docs** — in progress.
      README section on the option, and a `docs/` article (the site follows one file per
      area — check whether this belongs in the existing `getting-started` /
      `authorization` set or wants its own). Land as a separate `docs:` commit.

- [ ] **9. Push and hand over for testing**
      Push the branch. **Do not open the PR** — the user tests from origin first.

- [ ] **10. Close-out (only after the user confirms)**
      Re-run the outdated check; update `Requests.md` ask 2 to Done with evidence and add the
      consumer follow-up entry; fix the two stale backlog records found during startup
      (`Toolkit/Mcp.md` says "0 pending" and still lists the already-shipped
      ModelContextProtocol 2.0.0 upgrade); archive `plan/feature.md` to the Plan directory
      `done/`; `git rm -r plan`; final commit `feat: session mode complete`; open the PR.

## Notes

Startup sweep (2026-08-15): working tree clean, `master` level with origin, no open GitHub
issues on `Tharga/Mcp`, no open upstream requests. Two pending requests exist for this repo —
this one, and `IMcpContext.IsDeveloper` under `## Tharga.Mcp`, which is deliberately left for
a separate feature.

## Last session

2026-08-15 — Feature opened. Startup sweep done, request selected, API shape and the xunit
majors confirmed with the user, branch created, plan written. Nothing implemented yet;
step 1 is next.
