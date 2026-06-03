# BtDamageResolver & BtDamageResolverClient — Review (Opus 4.7)

Date: 2026-05-26
Reviewer: Claude Opus 4.7 (no code changes performed)

Scope:
- `BtDamageResolver/src` (Orleans grain server, Api, Common, Services, Silo)
- `BtDamageResolverClient/src/BlazorServer` (Blazor Server UI)
- Infra, build pipeline, tests, project metadata, Docker, CI, configuration

This is intentionally exhaustive — minor things are included on purpose, as requested. Findings are grouped by area, file paths and line numbers are given where known. A "top fixes" section per area ranks impact.

---

# PART A — SERVER (BtDamageResolver)

---

# PART B — BLAZOR CLIENT (BtDamageResolverClient/BlazorServer)

Many findings are individually minor but compound on weak ARM hardware where every extra render, DOM node, and SignalR round-trip is visible.

## B1. Render performance

- **No `ShouldRender()` overrides** anywhere except `FormNumber.razor:43` (`!_isBeingEdited`) and (now) `ComponentPlayerState`. Remaining components still re-render on every event/parameter change. Add render guards to the editable forms (`FormWeaponEntry`, `FormFiringSolution`, all `FormPaperDoll*` (10 files), `FormDamageReport`, `FormUnitEntry`). _(Read-only All Units path — `ComponentGameState`/`ComponentPlayerState`/`ComponentUnit`/`ComponentWeaponEntry` — done.)_
## B2. DOM / HTML structure (excessive nesting)

- **Wrapper-div proliferation.** Every cell wrapped in `resolver_div_componentcontainer > resolver_div_componentrow > resolver_div_componentcell`, often 5-7 deep. Browser layout & style recalc scale with node count; hits ARM hard.
- **Identical markup repeated rather than sub-componentised:**
  - Three modals at end of `FormUnitEntry.razor:209-279` — extract a `Modal` component.
  - The `componentrow > componentcell + componentcell` label/value pattern is repeated 80+ times — should be a `<LabelValue>` component.
- **`MainLayout` adds yet another wrapping div** (`<div class="resolver_content">`).
- **`ContainerReorderableList`** wraps every item in `<div class="reorderableitem">` and, if `ShowDragHandle`, also in `componentrow > draghandle + componentcell` — three extra divs per item plus handlers.

## B3. SignalR / Blazor Server specifics

- **Double-hop architecture (biggest single inefficiency).** The architecture is: server-side code (`ResolverCommunicator`) sends to Redis → `ClientToServerCommunicator` (Redis subscriber on the Blazor server) → **calls `_hubConnection.SendAsync`** back to the SignalR hub endpoint → `ClientHub` forwards to client. Each Redis message thus does an HTTP/WebSocket round-trip from the server *to itself* before reaching the browser.
  - `Communication/ClientToServerCommunicator.cs:37,45,53,61,69,77,85,93` — all `_hubConnection.SendAsync(...)`.
  - `Hubs/ClientHub.cs:17,23,29,35,41,47,53,59` — forwarder that just relays to `Clients.Client(connectionId)`.
  - The whole `ClientHub` could be removed and the Redis subscriber could directly notify the Blazor circuit (e.g., via the singleton `UserStateController`/event aggregator).
- **Per-circuit Redis subscription churn / leak.** `ResolverCommunicator.Reset()` (`ResolverCommunicator.cs:368`) constructs a new `ClientToServerCommunicator` per Connect. `Disconnect()` (line 89) just nulls the reference without calling `Stop()` → previous Redis subscription leaks.
- **Drag/drop handlers per item over SignalR.** `ContainerReorderableList` attaches six handlers per item (`@ondragstart`, `@ondragenter`, `@ondragend`, `@ondrop`, plus `:stopPropagation`). On Blazor Server, `@ondragenter` fires repeatedly during drag — each is a SignalR roundtrip. With 100+ items per page this floods the connection.
- **`onmousemove`/`onmouseout` are inline JS (good — no round-trip)** but the markup embeds a per-render JSON-ish tooltip string in `data-tooltip-content` (`FormWeaponEntry.razor:18`) recomputed every render.
- **Large SignalR payloads.** `Startup.cs:67-68,81` sets `ApplicationMaxBufferSize`/`TransportMaxBufferSize`/`MaximumReceiveMessageSize` = 1 MB → implies large payloads. The whole `GameState` is serialized on every player update; combined with key=timestamp invalidating subtrees, every update re-renders a huge chunk of DOM and ships large diffs.
- **`_dataHelper.Unpack<>` runs synchronously on the circuit thread** (`Pages/Index.razor:106, 112, 120, 126, 132, 138, 144`). Heavy decompress + JSON deserialize blocks the circuit, freezing UI on ARM.
- **`DisconnectedCircuitRetentionPeriod = 1 hour`** (`Startup.cs:63`) — keeps server memory holding stale circuits for an hour; pressure on small ARM hosts.

## B4. C# code in components

- **LINQ allocations in render paths:**
  - `FormPaperDoll.razor:52` `location.Value.SelectMany(l => l.Value).Sum()` per location per render (also lines 63, 193).
  - `FormPaperDoll.razor:122` `SelectMany(...).SelectMany(...).GroupBy(...)` per render.
  - `FormDamageReport.razor:81` `SelectMany(...).SelectMany(...).Sum()` per render.
  - `FormDamageReport.razor:9,10` `UnitEntries.Exists(...)` twice on stable params.
  - `ComponentGameState.razor:7-10` materialises `spectatorList` per render.
  - `FormDamageReports.razor:16` `_damageReportsToShow.Reverse()` allocates per render.
  - `FormDamageReports.razor:19` `singleTurnDamageReports.Value.GroupBy(...).ToList()` per render.
  - `FormDamageReportContainer.razor:7` `DamageReports[0].BlankCopy()` + merge loop in markup.
  - `ComponentWeaponEntry.razor:10-11` `GetTargetNumberUpdateSingleWeapon` lookup per render.
  - `FormWeaponEntry.razor:11-16` similar + `_commonData.FormMapWeaponAmmo(...)` (CommonData.cs:544-546) returns a fresh `SortedDictionary` every render.
  - `CommonData.cs:222, 297, 302-310` allocate new dictionaries per call; used per render per unit.
  - `CommonData.cs:418` `GetSavedUnitNames` hits Redis (`_unitRepository.GetAllKeys()`) per render (used inline at `FormUnitEntry.razor:269`, `FormData.razor:77`).
- **`FormPaperDoll.razor:204-214` — `TranslateDamageToColor`** decimal arithmetic + `Math.Clamp` + `ToString("X2", …)` + string interpolation, per location per paperdoll per render.
- **`UserStateController.cs:168`** — `UnitList` setter does `string.Join("-", _unitList.Select(...))` then `.Fnv1aHash64()` per replacement. `UpdateUnitList` (line 468) builds new `ConcurrentDictionary`, multiple LINQ scans; runs per inbound state.
- **`UserStateController.cs:423-425`** — `DamageReportConcernsPlayer` uses `Exists` twice per damage report per render of `FormDamageReports`.
- **Sync over async:** `Pages/Index.razor:164` calls `_formServer.Connect(credentials)` from `OnAfterRenderAsync` without awaiting; `Connect()` does sync Redis setup.
- **`FormGameList.razor:50`** `OrderByDescending(...)` materialised via spread `[.. ...]` per `OnParametersSet`.
- **`FormDamageReports.BuildDamageReportsToShow` (line 51)** — new `SortedDictionary`, `Reverse()` allocation, nested foreach — runs on every options update + every new damage report.
- **`FormRadio.razor:20`** — per-instance `Guid.NewGuid().ToString().Replace("-", "")` for radio name; recreated each time key invalidates the component.
- **JS interop overall light (good)** but tooltip strings are rebuilt and inlined as DOM attributes per render.
- **`BaseFaemiyahComponent.InvokeStateChange`** (line 13) doesn't await `InvokeAsync` — fire-and-forget swallows exceptions.

## B5. CSS issues (`wwwroot/css/Resolver.css`)

- **Subgrid + nested grid (slow on ARM browsers).** Lines 709, 789 use `grid-template-columns: subgrid`; subgrid is significantly more expensive than flat grid, especially on older Blink/WebKit ARM builds. Combined with wrapper-div depth, every value change re-lays out nested subgrids.
- **`display:inline-grid` + `display:flex` mixed.** `.resolver_div_componentblock` (line 690) declares `display: flex;` twice. `.resolver_div_componentgroup` inline-grid containing `componentrow` flex containing `componentcell` flex — every cell triggers flex measurement.
- **Sibling/`:hover` selectors with `~`** (lines 196-229, 271-287): `.resolver_label_toggleradio:hover input:checked ~ .resolver_span_toggleradio` invalidates sibling style on hover. Cheap individually × hundreds of toggles → style recalc cost on every mouse move.
- **`> *` universal selector** (line 725): `.resolver_div_componentgroup > *:not(.resolver_div_componentrow)` — expensive matching.
- **`SVG polygon:hover` recolor** (line 1002) — forces repaint per polygon × dozens of paperdolls.
- **`transition: height 0.1s, background-color 0.1s, outline-color 0.1s`** on every drag sentinel (line 854) — adds across many lists.
- **`box-shadow: inset 0 0 0.5rem` on every damage report card** (lines 948, 953). Soft shadow blur is one of the most expensive paint operations on weak GPUs.
- **Per-component font declarations** (`Lucida Console`/`Tahoma` lines 763, 882, 901) — minor font lookup per element.
- **`!important` overuse** (lines 267, 323, 369) — indicates specificity fight; not perf but smell.
- **CSS file is 29 KB and not minified.**

## B6. Communication layer (Communication/, Logic/, Hubs/ClientHub.cs)

- **Double-hop hub** — see B3 above.
- **`ResolverCommunicator.Disconnect` (line 89)** sets `_clientToServerCommunicator = null` without `Stop()` → Redis subscription leak.
- **`SendErrorMessage` fire-and-forget** (line 373) uses `_ = ….ContinueWith(...)` allocating a continuation. Use `try/catch await` in async method.
- **`ResolverCommunicator.SendRequest` swallows exceptions** and re-emits another SignalR error message, potentially in a fast loop if the connection is bad.
- **`Pages/Index.razor` `InvokeStateChange` per inbound packet** (lines 101, 115, 121, 127, 133, 139) — every Redis message triggers a re-render of the page-level component; combined with timestamp keys, every state update destroys & rebuilds the visible UI.
- **`UserStateController.GameState` setter** (line 117) runs `UpdateUnitList` on every set, including the no-op rejection path (`_gameState.TimeStamp >= value.TimeStamp` at line 129).
- **Inconsistent notification paths:** `GameOptions`/`PlayerOptions` setters (lines 92, 97) don't fire listeners; `Index.razor:127, 139` sets them directly → `OnGameOptionsUpdated`/`OnPlayerOptionsUpdated` don't fire on inbound updates, only on outbound user changes. Causes stale renders + extra refetches.
- **`CommonData.GetSavedUnitNames` / `GetGameEntries`** (lines 418, 434) hit Redis per render of components using them inline as `Options="…"`. Move to cached state.

## B7. Miscellaneous Blazor

- **`_Host.cshtml:21` — `ServerPrerendered`** prerenders the entire app twice (once SSR, once when interactive starts). Since almost everything is conditional on `IsConnectedToGame`, prerender adds no value. Consider `Server` mode.
- **`_Host.cshtml:15`** imports full Bootstrap but uses very little of it.
- **`FormFiringSolution.razor:25`** keys on `_userStateController.UnitListHash` → entire combobox subtree invalidates on any unit add/remove. Could be parameter-driven instead.
- **`FormNumberPickerDisplayOnly.razor:81`** invokes both `OnChanged.InvokeAsync(value)` and `OnChangedWithHint.InvokeAsync((Hint, value))` even when only one is wired — allocates a `ValueTuple` for nothing.
- **`Pages/Index.razor:58`** has a singleton tooltip div; multiple tooltip implementations live in different places (`FormGameState.razor:38`) — code duplication.
- **`FormDamageReport.razor:87, 116`** iterates `FiringUnitIds` twice.
- **`ComponentUnit.razor:30`** — `UnitEntry.Features.Any()` should be `.Count > 0` on `HashSet`.
- **`FormPickSet.razor:60, 70`** uses `DateTime.Now` (local) inconsistently with `DateTime.UtcNow` used elsewhere.

---

# PART C — INFRA, BUILD, TESTS, CONFIG

## C1. .NET versions & NuGet packages

- **Mixed TFMs.** All BtDamageResolver projects target `net10.0` but `CompressionLzma/src/CompressionLzma/CompressionLzma.csproj:4` targets `net9.0`.
- **No `global.json`** to pin SDK version, no `<RollForward>` policy → non-reproducible across machines.
- **Mixed package family versions:** `Microsoft.Orleans.*` at `10.1.0` vs `Microsoft.Extensions.Hosting/Logging`/`Microsoft.AspNetCore.SignalR.Client` at `10.0.8`; `Microsoft.VisualStudio.Web.CodeGeneration.Design` at `10.0.2`. No central `Directory.Packages.props`.
- **Stale dep:** `System.ComponentModel.Annotations 5.0.0` in `Api.csproj` is essentially superseded — remove.
- **Heavy deps for client:** `NuGet.Packaging`/`NuGet.Protocol 7.6.0` in `BlazorServer.csproj` — confirm needed.
- **Custom NuGets checked in:** `CustomNugets\*.nupkg` (seven versions `0.0.405`-`0.0.434` of `Api`/`Common`). Active is `0.0.440` → none match referenced version; repo bloat.
- **Compiled binaries committed:** `BuildPipeline/BuildPipeline.dll`, `.exe`, `.runtimeconfig.json` with no source — opaque + Windows-only EXE in Git.
- **No vulnerability/dependency scanning** (no `dotnet list package --vulnerable`, no Dependabot, no Renovate, no CodeQL).

## C2. Project file / `Directory.Build.props`

- `Directory.Build.props` is almost empty (only `SonarAnalyzer.CSharp`). Missing across the board:
  - `<Nullable>enable</Nullable>` — no project enables NRT.
  - `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.
  - `<AnalysisMode>`/`<AnalysisLevel>`.
  - `<ImplicitUsings>enable</ImplicitUsings>`.
  - `<LangVersion>latest</LangVersion>`.
  - `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>` — `.editorconfig` is advisory only.
  - `<GenerateDocumentationFile>true</GenerateDocumentationFile>` — packaged NuGets ship without XML docs.
  - Common metadata (`Authors`, `Company`, `Copyright`, `RepositoryUrl`, `PackageProjectUrl`) duplicated per project.
- `Directory.Build.props:7` condition `$(MSBuildProjectExtension) == '.csproj'` is redundant.
- `Api.csproj`/`Common.csproj` `<Copyright>Faemiyah 2020</Copyright>` — stale.
- **Per-project boilerplate** — every Silo/Tool csproj duplicates `<None Update>` for `*.json` Debug-conditional copies.
- `Tests.csproj` only references `Actors.csproj` → cannot test `Api`, `Common`, `Services`, `BlazorServer`, repositories etc. without rebuilding.
- `Tests.csproj` lacks `coverlet.collector`/`coverlet.msbuild` → no coverage instrumentation.
- `CompressionLzma.csproj` has empty `<Company />`, no `<PackageProjectUrl>`/`<Description>`; `RepositoryUrl` trailing slash inconsistent.
- `Silo.csproj` puts `<ServerGarbageCollection>`/`<ConcurrentGarbageCollection>` as project properties — better as runtime config.
- `BlazorServer.csproj` uses `<None Include="...">` (not `Update`); `appsettings.json` isn't referenced — relies on Web SDK conventions.

## C3. CI / build pipeline

- **`.github/workflows/` is empty.** Zero CI: no build, no test, no lint, no docker build, no release.
- **No Dependabot/Renovate, no CodeQL, no PR/issue templates, no CODEOWNERS.**
- **Windows-only build scripts** (`build.bat`, `build_producenugets.bat`, `build_pushnugets.bat`, `build_rollversion.bat`, `export_mechs.bat`, `refresh.bat`, `prune.bat`); only `refresh.sh` is cross-platform.
- `build_rollversion.bat` invokes the checked-in opaque `BuildPipeline\BuildPipeline.exe`.
- `refresh.bat` references `../CustomNugets/Dockerfile` which doesn't exist (the real one is `infra/sdk/Dockerfile` — used correctly by `refresh.sh`). Windows users following the README are broken.
- `refresh.sh` has **CRLF line endings** (contains 0x0D bytes). Bash on Linux will fail with `bad interpreter` or syntax errors. `.gitattributes` declares `* text auto` but lacks `*.sh text eol=lf`.

## C4. Docker

- All app Dockerfiles `FROM resolversdk:latest` and infra dockerfiles use `:latest` (`grafana/grafana:latest`, `postgres:latest`, `redis:latest`). Non-reproducible.
- Base images `mcr.microsoft.com/dotnet/runtime:10.0`/`aspnet:10.0` use floating major tag — pin to patched tag and consider chiseled images.
- **No `--platform`/multi-arch builds.** User specifically mentions ARM servers. Microsoft images are multi-arch but build needs `buildx` and explicit targets.
- **No `.dockerignore` anywhere.** `COPY src/` slurps `bin/`, `obj/`, `.vs/`, `*.user`, `BlazorServer.csproj.user`. Balloons context, breaks caching.
- **No `HEALTHCHECK`** in any Dockerfile (Silo, BlazorServer, redis, postgres, grafana, sdk). `docker-compose.yml` `depends_on:` won't wait for readiness without health conditions.
- **No restore layer caching.** Dockerfiles `COPY ["src/", "src/"]` then `dotnet publish` — restore reruns every change. Should `COPY *.csproj`, `restore`, then `COPY .`, `publish`.
- Silo `Dockerfile:13` `USER app` — relies on upstream image; document or use distroless.
- **No `EXPOSE 8080` in BlazorServer Dockerfile** despite compose mapping `8787:8080`.
- `DataImporter/Dockerfile` `COPY` is fragile and `importdata.sh` isn't `chmod +x` — relies on `CMD ["sh", ...]`.
- `infra/grafana/Dockerfile` has no `USER`; `WORKDIR /etc/grafana/provisioning` + `COPY . .` will copy the Dockerfile itself in.
- `infra/postgresql/Dockerfile` hardcodes Postgres major `18` in mount path; `:latest` bumping to v19 breaks data.
- `infra/redis/Dockerfile` uses `:latest`; `redis.conf` has `protected-mode no` (OK behind network); CMD passes `--requirepass $REDIS_PASSWORD` shell-form — silent no-password if env is unset.
- **`docker-compose.yml`:**
  - Fixed `container_name:` → single stack per host.
  - `restart: on-failure` everywhere but no `mem_limit`/`cpus`.
  - No `networks:` isolation; Postgres `65432`, Redis `63790`, Grafana `63000` all externally exposed needlessly.
  - **Single shared `RESOLVER_PASSWORD` and `RESOLVER_USER`** reused for Postgres user/pwd, Redis password, Grafana admin user/pwd (lines 10, 11, 27, 45, 57-60, 79, 93). Compromise of one = all.
  - `image: resolver:latest` etc. — relies on local builds; no registry release path.
  - `DataImporter` long-lived with `restart: on-failure` — should be `restart: "no"` and run via `docker compose run`.

## C5. Tests

- **One test file**: `BtDamageResolver/tests/Tests/ExpressionTests.cs` covering only `ExpressionExtensions.IsToken` and the math parser. `Actors`, `Services`, `Api`, `Common`, `BlazorServer`, repositories, communication, grains, damage resolution — **zero unit tests**.
- **No integration tests** (no Orleans TestCluster, no Testcontainers for Redis/Postgres).
- **No coverage tooling** (`coverlet`, codecov badge, etc.).
- `CompressionTesterApp` is in `tests/` but is `OutputType=Exe`, not a test project. Misclassified.
- Test method naming inconsistent (`Test_Token_IsAToken_ReturnsTrue` vs `ReturnsCorrectResult`). Empty `[SetUp]` is dead code.
- `AwesomeAssertions 9.x` — community fork of FluentAssertions (since FA went paid). Track maintenance.
- No `Tests.runsettings`, no parallelization config, no `LangVersion`/`Nullable` settings in test project. No `NUnit.Analyzers`.

## C6. Logging / observability

- **No OpenTelemetry / metrics / tracing** despite Orleans providing rich metrics. No Prometheus exporter.
- **Grafana datasource is Postgres only** — no system/runtime metrics dashboards. Only `dashboard_resolver_events.json`.
- **No `/health` or `/metrics` endpoint** in BlazorServer `Startup.cs` (no `AddHealthChecks()`/`MapHealthChecks`).
- Silo `Program.cs:61, 103` writes startup/shutdown errors with `Console.WriteLine(ex)` instead of the configured logger.
- `Startup.cs:80` `EnableDetailedErrors = true` unconditionally — leaks stack traces in production.

## C7. Configuration / secrets

- **`SiloSettings.json` committed with placeholder credentials:**
  ```
  "ConnectionString": "User ID=USERNAME;Password=PASSWORD;..."
  "ConnectionString": "redis:6379,password=PASSWORD"
  ```
  If a dev runs locally without env override the app attempts to connect literally. Better: omit + fail-fast validation.
- **`SiloSettings.Release.json`** and `CommunicationSettings.Release.json` are empty `{}` — pointless.
- **Hardcoded cluster identifiers** `ClusterId = "faemiyah"`, `ServiceId = "Resolver"` in Silo `Program.cs`.
- **Hardcoded buffer sizes** in `Startup.cs` (1 MB) — should be configuration.
- **Hardcoded timeouts** throughout Silo `Program.cs` (15s, 1 day, etc.).
- **Shared `RESOLVER_PASSWORD`/`RESOLVER_USER`** across all services (see C4).
- BlazorServer data-protection keys persisted to `/app/dpkeys/` (`Startup.cs:75`) with no rotation / no encryption-at-rest.
- `.gitignore:165` references `infra/dpkeys/` but actual location is a docker volume — misleading.
- `RESOLVER_ENVIRONMENT` env var set but never consumed in code.

## C8. Misc

- **`README.md`** is incomplete: `TODO: Write something actually useful here.` (line 9). References folder `BtDamageResolverInfrastructure` (line 16) which doesn't exist — actual folder is `infra/`. Primary "how to run" instruction is broken.
- README references `refresh.sh` for Linux only; doesn't mention `refresh.bat` is broken.
- README has placeholder `INSERT_YOUR_READ_ONLY_NUGET_FEED_PAT_HERE` for a credential — risk of accidental commit.
- **`TODO.txt` / `CHANGELOG.txt`** at repo root instead of `.md` — not surfaced on GitHub. `CHANGELOG.txt` is not in Keep-a-Changelog format.
- **`.editorconfig` is 800 lines** but `EnforceCodeStyleInBuild` is unset → advisory only.
- **`.gitignore`** lacks `*.csproj.user`; `BlazorServer.csproj.user` is committed.
- **CRLF line endings on `refresh.sh`** (verified — will fail on Linux).
- **UTF-8 BOM on many files** (`*.csproj`, `.env_sample`, `Tests.csproj`, `redis.conf`, etc.). For `.sh` scripts it's fatal; for dotenv parsers can break parsing.
- **`BTDamageResolver.slnx`** uses capital "BT" while folder is `BtDamageResolver` — minor inconsistency.
- `LogToConsole: true` AND `LogToDatabase: true` in `SiloSettings.json` — every Orleans log line written twice.
- `infra/postgresql/scripts` numbered 01-06 — only run on empty volume (`docker-entrypoint-initdb.d` semantics). Verify idempotency.
- `Tests/Tests.csproj` does not reference `NUnit.Analyzers`.

---

# PART D — TOP IMPACT FIXES (ranked)

## D1. Blazor server perf (likely root cause of slowness on ARM)

1. **Remove timestamp-based `@key`s** (B1: Index.razor, ComponentGameState, ComponentPlayerState, ComponentUnit). Use stable IDs. Single largest win.
2. **Drop the `ClientHub` SignalR round-trip loop.** Wire Redis subscriber directly to the circuit's `UserStateController`/event aggregator.
3. **Implement `ShouldRender()`** and stop subscribing every weapon/bay to global events.
4. **Replace inline lambdas with cached delegates / named methods.**
5. **Reduce DOM nesting** in `ComponentUnit`/`FormWeaponEntry`/`FormUnitEntry`/`FormPaperDoll`. Flatten `componentcontainer>componentrow>componentcell` layers.
6. **Hoist LINQ/string work out of markup** (`FormPaperDoll`, `FormDamageReport`, `FormDamageReports`, `CommonData.FormMap*`).
7. **Cache `SortedDictionary` results** in `CommonData.FormMap*` — currently reallocated per render.
8. **Eliminate subgrid + nested grid** in hot tables; use flat CSS grid or `<table>`.
9. **Drop `box-shadow: inset 0 0 0.5rem`** on damage report cards (slow paint on weak GPUs).
10. **Fix Redis subscriber leak in `ResolverCommunicator.Disconnect()`.**
11. **Offload `_dataHelper.Unpack<>`** to a background thread (`Task.Run`) for large payloads.
12. **Set `_Host.cshtml` to `Server` mode** (drop `ServerPrerendered`).

## D2. Server correctness / security

1. **Fix `RapidFireWrapper`** — pass `Func<Task<int>>`, not a pre-awaited `Task<int>`.
2. **Fix `ResolveHeatForSingleHit`** to honor `rangeBracket`.
3. **Fix `SendSingle` log condition inversion** in `RedisCommunicator`.
4. ~~**`ResolverRandom` thread safety**~~ — done.
5. **Replace `FaemiyahPasswordHasher`** with PBKDF2/Argon2 + work factor + algorithm tag.
6. **Add ownership/authorization** to `SendDamageInstance`, `LeaveGame()` parameterless variant, etc.
7. **Replace trust-on-first-use** account/game password creation with explicit registration / "first user is creator" flow.
8. **`CachedEntityRepository` thread safety** + refresh policy.
9. **Fix `RedisEntityRepository`**: switch to async `StringGetAsync`/`MGET`/`SCAN`; pass `EndPoint` to `GetServer`; catch `RedisException` not `DbException`; preserve inner exceptions in `DataAccessException`.
10. **Stop unbounded retry in `LoggingRepository`** — add backoff and ceiling.
11. ~~**Bound `RollHitLocation` loop**~~ — not a bug; rejected.
12. **Stop cross-grain object aliasing**: deep-copy `PlayerState`/`UnitEntry` when crossing grain boundaries.

## D3. Server perf

1. **Replace LZMA with LZ4/Brotli** for Redis messages.
2. **Cache `MathExpression.Parse`** results for deterministic expressions.
3. **Fix `IsToken` / `ExtractFunctionType` reflection per char** with static sets/dictionaries.
4. **Replace `RedisEntityRepository` sync calls** with async and `MGET`/`SCAN`.
5. **Make hot grain methods `[Reentrant]`** for read paths (e.g. `GetGameState`, `RequestDamageReports`).
6. **Index `UnitEntry` lookups** instead of `Single(...).Single(...)` scans (`GetUnit`, `IsUnitInGame`).
7. **Batch Postgres log writes** with `NpgsqlBatch` / `COPY`.

## D4. Infra / build

1. **Add CI** (build + test + docker build) via GitHub Actions; add Dependabot + CodeQL.
2. **Pin `global.json`** and align package versions (Directory.Packages.props).
3. **Add `.dockerignore`**, `HEALTHCHECK`s, restore-layer caching, multi-arch (`linux/amd64,linux/arm64`) builds for ARM.
4. **Split passwords** per service in `docker-compose.yml`.
5. **Remove checked-in compiled NuGets and `BuildPipeline.exe`**.
6. **Fix `refresh.sh` line endings** (`.gitattributes` rule).
7. **Fix README** (folder name, broken `refresh.bat`).
8. **Untrack `BlazorServer.csproj.user`** and add to `.gitignore`.
9. **Enable `Nullable`, `TreatWarningsAsErrors`, `EnforceCodeStyleInBuild`** in `Directory.Build.props`.
10. **Add real unit/integration test coverage** (Orleans TestCluster, Testcontainers).
