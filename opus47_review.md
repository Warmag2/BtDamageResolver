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

- **No `ShouldRender()` overrides** anywhere except `FormNumber.razor:43` (`!_isBeingEdited`), `ComponentPlayerState`, and (now) `FormPaperDoll` (guards the whole paper-doll subtree on `DamagePaperDoll`/`DamageReport` reference change). Remaining components still re-render on every event/parameter change. Add render guards to the editable forms (`FormWeaponEntry`, `FormFiringSolution`, `FormDamageReport`, `FormUnitEntry`). _(Read-only All Units path — `ComponentGameState`/`ComponentPlayerState`/`ComponentUnit`/`ComponentWeaponEntry` — done.)_

---

# PART C — INFRA, BUILD, TESTS, CONFIG

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
