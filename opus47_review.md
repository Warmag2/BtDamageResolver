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

## A6. Communication / Redis

- **`RedisCommunicator.cs:68` — `Start()` called from base constructor.** If a subclass adds initialisation, it runs after. Footgun.
- **`RedisCommunicator.cs:160-166` — `OnMessage(async channelMessage => …)`** is effectively `async void` from StackExchange's perspective; exceptions are swallowed.
- **`RedisCommunicator.cs:147-155` — `CheckChannelConnection`** reconnects on `IsConnected==false` and re-subscribes, but never reconnects the underlying `ConnectionMultiplexer`. Can spin.
- **`RedisCommunicator` does not implement `IDisposable`** — `_redisConnectionMultiplexer` is never disposed.
- **`RedisCommunicator.cs:115` — `Publish(..., CommandFlags.FireAndForget)`** swallows failures; combined with the inverted log condition the system never notices undelivered messages.
- **`Services/ServerToClientCommunicator.cs:286-292` — `ValidateObject`** allocates a `ValidationContext` per call and uses DataAnnotations reflection on hot endpoints.
- **`Services/ServerToClientCommunicator.cs:280` — `SendErrorMessage(name, string.Empty)`** uses the error channel for non-error "all clear" signalling. Should be a separate event.
- **`Services/ServerToClientCommunicator.cs:43-284` — 17 nearly identical `Handle…` methods.** A generic dispatcher or source generator would remove drift.
- **`Services/CommunicationServiceClient.cs:22` — `GrainService => GetGrainService(CurrentGrainReference.GrainId);`** invoked on every send; allocates per call.

## A7. Logging / PostgreSQL

- **`Services/Database/LoggingRepository.cs:55-66` — inserts one row at a time inside a transaction.** Use `NpgsqlBatch` or `COPY` for high-volume logging.
- **`LoggingRepository.cs:91-93, 104-105` — `AddWithValue("playerId", entry.PlayerId.Fnv1aHash64())`** untyped (Npgsql infers). FNV-1a 64-bit has non-trivial collision rate at scale; cryptographic IDs would be safer.
- **`LoggingRepository.cs:88, 101` — unquoted PascalCase identifiers (`ResolverLogGame`).** PostgreSQL folds unquoted identifiers to lowercase — misleading at minimum.
- **`LoggingRepository.cs:67-74` — re-enqueues entries on failure** unconditionally. Permanently down DB → infinite retry, growing memory, no backoff.
- **`LoggingService.cs:61` — `Task.Run(() => LogWriteLoop(...))` from inside `Start()` of a `GrainService`** runs the loop on a Threadpool thread instead of the Orleans single-threaded scheduler — defeats `GrainService` semantics; concurrent producer/consumer hazards (papered over by `ConcurrentQueue`).
- **`LoggingService.cs:74-76` — final flush after `CancelAsync`** races any in-flight enqueues.
- **`LoggingService.cs:23` — hard-coded `15000` (`LoggingDelayMilliseconds`)** magic number; belongs in `FaemiyahLoggingOptions`.

## A8. Cryptography / Security

- **`Actors/Cryptography/FaemiyahPasswordHasher.cs`** uses single-iteration SHA-512 with a 32-byte salt. Class doc admits it's weak. SHA-512 is GPU-friendly; trivial to crack. Use PBKDF2/Argon2/bcrypt with a real work factor. No algorithm/version tag on stored hash → no upgrade path.
- **`Actors/PlayerActor.Connections.cs:26-31` — trust-on-first-use account creation.** First request with a given player name *creates* the account and stores its password. No registration step. Anyone can claim any unused player ID.
- **`Actors/GameActor.cs:149-157` — same TOFU for game passwords.** First joiner sets it. If `password == string.Empty`, `PasswordHash` stays `null` and the game is unprotected forever; no way to set later.
- **`Actors/PlayerActor.Sends.cs:128-132` — `SendDetailedErrorsToClient`** echoes `ex.Message + "\n" + ex.StackTrace` to the client when the flag is on. Easy info leak if misconfigured.
- **`Actors/GameActor.cs:121-138` — `SendDamageInstance`** has no ownership check on attacker or target. Any player in the game can fabricate damage on any unit.
- **`Actors/GameActor.Tools.cs:99` — `MoveUnit`** leaks "unit not found" vs "you're not in the game" vs "not authorised" via different log paths. Minor info disclosure.
- **`Api/Entities/Credentials.cs:36` — regex `^[A-Za-z0-9_-]*$`** allows empty name; only `StringLength(MinimumLength=1)` saves it. Merge the checks.
- **`Services/ServerToClientCommunicator.HandleConnectRequest:50` — error message concatenates `string.Join(", ", validation results)`** with no length cap. Potentially huge.
- **No rate-limiting** on Redis-driven request handling. A peer that reaches Redis can spam the silo.
- **`Startup.cs:80` (BlazorServer) — `EnableDetailedErrors = true` for SignalR unconditionally.** Leaks stack traces in production.

## A9. General C# hygiene

- **`Common/Logging/FaemiyahLoggerFactory.cs:38` — `LogCreationSemaphore.Wait()`** synchronously instead of `WaitAsync`.
- **Broad `catch (Exception)`** everywhere: `PlayerActor.Sends.cs:126`, `CachedEntityRepository.cs:46/68/90/132`, `RedisEntityRepository.cs:59/86/114/144/165/195`, `LoggingRepository.cs:67`, `LoggingService.cs:133`. Many translate to `throw new DataAccessException(DataAccessErrorCode.OperationFailure)` *without inner exception* → stack traces lost.
- **Constructor-time work in `RedisEntityRepository` and `RedisCommunicator`** (Redis connections) — DI singletons that fail in ctor break silo startup.
- **Magic numbers everywhere**: heat tables (`UnitEntry.cs:118-228`), target-number table (`LogicUnit.Hit.cs:69-78`), `MovementModifierArray` (`LogicUnit.HitModifier.cs:16`), `15000` ms (`LoggingService.cs:23`), `MaximumGameEntryAgeHours` (`GameEntryRepositoryActor.cs:22`).
- **Settings split between `Common.Constants.Settings` and `Api.Constants`** — not centralised.
- **No `ConfigureAwait(false)`** in library code in `Api`/`Services` despite being usable as library code.
- **Many `protected readonly` fields exposed instead of properties** in `Actors/Logic/LogicUnit.cs:19-58`.

## A10. Project structure

- `Logic` lives inside `Actors` but is consumed via `Api.ClientInterface.Repositories.Providers.RepositoryProvider` — `Api` (client-facing) is hosting server runtime types. Boundary muddled.
- `RepositoryProvider` is a concrete class injected into every `LogicUnit*` constructor — tight coupling; hard to mock.
- `IEntityRepository` is implemented twice (Redis + Cached) used directly; no read-only/write-only split.
- `LogicUnitFactory` switch breaks open/closed: every new unit type touches the factory.
- `Actors.States.Types.UnitList` and `Api.Entities.PlayerState.UnitEntries` (List<UnitEntry>) are two representations of the same concept.
- `ServerToClientCommunicator` (`Services`) extends `RedisServerToClientCommunicator` (`Api.ClientInterface.Communicators`) — server-side logic in a client-named namespace.
- Magic event names live in `Api.Constants.EventNames` wired by string in `RedisServerToClientCommunicator`.
- Near-circular references mediated only by `ServiceInterfaces`: `Services` references `Api`, `Actors` references both, `Api` depends on `Services` interfaces.

---

# PART B — BLAZOR CLIENT (BtDamageResolverClient/BlazorServer)

Many findings are individually minor but compound on weak ARM hardware where every extra render, DOM node, and SignalR round-trip is visible.

## B1. Render performance

- **No `ShouldRender()` overrides** anywhere except `FormNumber.razor:43` (`!_isBeingEdited`). Every other component re-renders on every event/parameter change. At minimum add render guards to `ComponentUnit`, `ComponentWeaponEntry`, `FormWeaponEntry`, `FormFiringSolution`, all `FormPaperDoll*` (10 files), `FormDamageReport`, `FormUnitEntry`, `ComponentPlayerState`, `ComponentGameState`.
- **Hot subscribers re-render on every target number update.** `ComponentWeaponEntry.razor:45`, `FormWeaponEntry.razor:100`, `ComponentHeatAmmoEstimate.razor:51` all subscribe to `OnTargetNumbersUpdated`. With N weapons × M players × every packet the entire grid re-renders. No "is this MY target number?" diff. `Index.razor:142-146` even notes "Not necessary to invoke state change", but components do it anyway.
- **Cascading `_userStateController` access in markup.** `FormUnitEntry.razor` references `_userStateController.PlayerState.IsReady` ~30 times per render; `ComparisonTime` recomputes per call. Cache once at top of render: `@{ var isReady = …; var compTime = …; }`.
- **`@key` instability triggers full subtree disposal — likely the single largest perf bug.**
  - `Pages/Index.razor:42,46,50,54` — keys include `_userStateController.GameState?.TurnTimeStamp`. Every state update destroys and recreates `FormGameState`, `ComponentGameState`, `FormDamageReports`, `FormOptions` subtrees instead of diffing.
  - `Shared/ComponentGameState.razor:29` — keys `ComponentPlayerState` by `player.TimeStamp` → every player update recreates every unit subtree.
  - `Shared/ComponentPlayerState.razor:11` — keys `ComponentUnit` by `unitEntry.TimeStamp` → editing any field on a unit destroys the unit subtree.
  - `Shared/ComponentUnit.razor:123` — keys `ComponentWeaponEntry` by `weaponEntry.TimeStamp`.
  - Pattern is endemic. `@key` should be stable IDs (`unitEntry.Id`).
- **Inline lambdas in templates allocate per render.** Parameter identity changes → child re-renders unnecessarily. Examples:
  - `FormUnitEntry.razor:44` `OnChanged="(UnitType unitType) => OnUnitTypeChanged(unitType)"` (+15 more in the same file).
  - `FormWeaponEntry.razor:31,41,51,57` — lambdas as `OnChanged`.
  - `FormFiringSolution.razor:25,31,37,43,49,55` — every `OnChanged` inline.
  - `FormFiringSolution.razor:37` `BracketCreatorDelegate="@(() => _commonData.FormPickBracketsDistance(...))"` — new closure per render.
  - `FormWeaponEntry.razor:31,41` `StyleSelectorDelegate="@((_) => "resolver_status_transparent")"`.
  - `ContainerReorderableList.razor:9-15` — six inline `() => StartDrag/SetDragOver/Drop(capturedIndex)` lambdas per item per render.
  - `FormOptions.razor:55,80,81,91,…`; `FormDamageReport.razor:155` lambda inside `@foreach`.
  - Fix: hoist into named methods or use `EventCallback.Factory.Create` with stable instances.
- **Large objects as parameters.** `ComponentUnit/FormUnitEntry/ComponentWeaponEntry/FormWeaponEntry/FormFiringSolution/FormPaperDoll/FormDamageReport` accept whole `UnitEntry`, `WeaponEntry`, `WeaponBay`, `DamageReport`, `DamagePaperDoll`. `ChangeDetection.MayHaveChanged` is unreliable for mutated objects. Pass minimal value-type parameters.
- **No `<Virtualize>` anywhere.** Candidates:
  - `ComponentGameState` (Shared/ComponentGameState.razor:27) — all players' units.
  - `FormDamageReports` (Shared/FormDamageReports.razor:16) — per-turn reports.
  - `FormDamageReport.razor:159` — full `AttackLog.Log` (can be very long).
  - `FormGameList` — all games.
  - `FormComboBox.razor:23` renders *every* option with `style="display:none"` for non-matches instead of filtering — hundreds of weapon names per combobox.
- **Default `@bind`/`@oninput` causing per-keystroke SignalR roundtrips:**
  - `FormComboBox.razor:20` `@bind="SelectedOptionInternal" @oninput="AdjustOptionList"` — every keystroke = round-trip + `Options.Where(...).ToList()` + child re-render.
  - `FormText`'s setter calls `InvalidOptionGenerator()` (regenerating lists) on every commit.
- **Global event subscriptions in granular components:**
  - `FormWeaponBay.razor:65` subscribes to `OnPlayerUnitListUpdated` → every weapon bay re-renders on any unit list change.
  - `FormFiringSolution.razor:74` subscribes to `OnGameUnitListUpdated`.
  - `FormDamageReport.razor:187` subscribes to `OnDamageReportsUpdated` → every existing card re-renders on every new report.
  - `FormDamageReports.razor:36-37` listens to both `OnDamageReportsUpdated` and `OnPlayerOptionsUpdated`, runs full LINQ pipeline on each.
- **`Pages/Index.razor` keys on timestamps cause modal/tabs to recreate** (1d above). Modals torn down/rebuilt on every state change.
- **Tab switching is full rebuild every time** because `ContainerTab.razor:3` only renders ChildContent when active *and* the timestamp `@key` invalidates the tree anyway.
- **`@foreach` without `@key`:** `FormOptions.razor:54` (FormToggle), `FormDamageReport.razor:153` (FormCheckbox).
- **`OnInitialized` runs constantly due to key churn:**
  - `FormPickSet.OnInitialized` — `Options.ToDictionary(...).ToHashSet()` (line 91-92).
  - `FormNumberPickerDisplayOnly.OnInitialized` — calls `BracketCreatorDelegate()` (line 137) which can construct a list per call.
  - `FormComboBox.OnInitialized` — `Options.Any` + `Options.FirstOrDefault` (line 181-183).
  - `FormSelect.OnInitialized` — similar (line 54-56).

## B2. DOM / HTML structure (excessive nesting)

- **Wrapper-div proliferation.** Every cell wrapped in `resolver_div_componentcontainer > resolver_div_componentrow > resolver_div_componentcell`, often 5-7 deep. Browser layout & style recalc scale with node count; hits ARM hard.
- **`ComponentUnit.razor` max depth ~12 levels** (excluding `<svg>`).
- **`FormWeaponEntry.razor`** — each weapon row has 7 grid columns, each wrapped in its own `<div class="resolver_div_componentcell">` (lines 18, 21, 24, 30, 34, 44, 54, 60). Many cells contain another `<div class="resolver_div_inputwrapper">` from `FormNumberPicker`/`FormNumberPickerDisplayOnly`. With 10 weapons × 10 units this multiplies into thousands of unnecessary nodes.
- **`FormUnitEntry.razor`** (~280 lines) — every field is `componentrow > componentcell (label) + componentcell > FormX > inputwrapper > input` (5-6 deep × ~15 fields × N units). Should be flattened with CSS-grid auto-flow.
- **`FormPaperDollMech.razor` SVG** — 8-11 `<g><polygon … onmousemove="…" onmouseout="…">` per paperdoll, repeated per damage report. Inline JS event attributes force the browser to parse handler strings. Could use CSS `:hover` + delegated tooltip handlers.
- **10 near-identical `FormPaperDoll*` variants** — should share a base component with polygon list as data.
- **Identical markup repeated rather than sub-componentised:**
  - Three modals at end of `FormUnitEntry.razor:209-279` — extract a `Modal` component.
  - The `componentrow > componentcell + componentcell` label/value pattern is repeated 80+ times — should be a `<LabelValue>` component.
  - `FormPaperDoll.razor` `SelectMany(l => l.Value).Sum()` duplicated lines 52 and 63.
- **`FormTextArea.razor:2`** echoes `@TextInternal` inside the `<textarea>` body in addition to binding — redundant content (also a known Blazor anti-pattern; can desync).
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
- **Mixed package family versions:** `Microsoft.Orleans.*` at `10.1.0` vs `Microsoft.Extensions.Hosting/Logging`/`Microsoft.AspNetCore.SignalR.Client` at `10.0.8`; `Microsoft.VisualStudio.Web.CodeGeneration.Design` at `10.0.2`. `Serilog.Extensions.Logging` `10.0.0` with `Serilog` `4.3.1`, `Sinks.Console` `6.1.1`, `Sinks.File` `7.0.0`. No central `Directory.Packages.props`.
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

- `Common.csproj` pulls Serilog + console + file sinks, but **no `Serilog.Sinks.PostgreSQL`** despite `SiloSettings.json` setting `LogToDatabase: true`. Verify package path.
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
