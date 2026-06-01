# Debug Handoff Brief — edit-propagation regression

Branch: `perfreviewbranch2` (Warmag2/BtDamageResolver)
HEAD: `9be4c6c` (chore: Nuget versions) → `84e9400` (Much more perf review) → `5aa0df6` (More perf review) → `d4563e5` (perfreviewbranch tip)

## Symptom
- Loading a unit works visually, BUT interacting with any item does not update it.
- Never receive target-number updates ("the target is not actually updating").
- User statement: "There is something wrong with the entire update chain. ANY update upstream does not work." Certain it came with `5aa0df6`, used extensively after.

## Key deduction
Loading a unit is **client-only** (`ModalLoad` → `_commonData.GetUnit(name)`, no server round-trip).
So the break is in the **edit → server → target-numbers** round-trip, NOT in client-only rendering.
=> Focus on the Redis round-trip and server processing, not on Blazor @key/render churn.

## Uncommitted working-tree changes (B2 DOM flatten, unrelated to regression)
- `BtDamageResolverClient/src/BlazorServer/Shared/ComponentUnit.razor` (flattened)
- `BtDamageResolverClient/src/BlazorServer/wwwroot/css/Resolver.css` (`.resolver_div_unitcard` flex rule)

## What `5aa0df6` changed (the suspect commit)
Client (BlazorServer):
- `Logic/UserStateController.cs` GameState setter:
  - BEFORE: `UpdateUnitList(); NotifyPlayerUnitListUpdated();` (fired OnPlayerUnitListUpdated on EVERY server push)
  - AFTER: `if(UpdateUnitList()) NotifyGameUnitListUpdated(); NotifyGameStateUpdated();`
  - => `OnPlayerUnitListUpdated` is NO LONGER fired on server pushes. New events only consumed by the read-only All-Units view (ComponentGameState/PlayerState/Unit).
- `ComponentGameState/ComponentPlayerState/ComponentUnit.razor`: stable @key + event subscriptions (All-Units view only).
- `FormWeaponBay.razor`: REMOVED its `OnInitialized` subscription to `OnPlayerUnitListUpdated` and its IDisposable/Dispose.
- `Pages/Index.razor`: All-Units `<ComponentGameState>` @key `GameState.TimeStamp` → `GameState.TurnTimeStamp`.
- `Startup.cs`: EnableDetailedErrors now gated by ASPNETCORE_ENVIRONMENT (benign).
- csproj: Api/Common PackageReference `0.0.445` → `0.0.446`.

Api/Common (pulled into client via NuGet 0.0.446 from CustomNugets feed):
- `RedisCommunicator.cs`:
  - `OnMessage(async lambda)` → `OnMessage(ProcessChannelMessage)`; new `ProcessChannelMessage` wraps deserialize+RunProcessorMethod in **try/catch that LOGS** previously-swallowed exceptions. (Happy path identical; method group binds to `Func<ChannelMessage,Task>` overload — verified correct.)
  - `SendEnvelope` now returns void (was `long`); still `Publish(..., CommandFlags.FireAndForget)` — logic unchanged.
  - Added `IDisposable` + `Dispose(bool)` that `Unsubscribe()` + `UnsubscribeAll()` + disposes the connection multiplexer.
- `RedisClientToServerCommunicator.SubscribeAdditional`: inline lambda → `ProcessChannelMessage` (receive side).
- `RedisServerToClientCommunicator`: removed zero/!=1 client-count warnings; **renamed** `GetPlayerOptions` handler `HandleGetGameOptionsRequest` → `HandleGetPlayerOptionsRequest` (verify both do the same thing).
- Common: Serilog → Microsoft.Extensions.Logging.Console; removed `ConfigurationUtilities.InitializeLogging`. JSON serializer options method UNCHANGED.

## Ruled out (static analysis)
- Client send wiring intact: leaf OnChanged → On<Field>Changed → SendUpdate → NotifyPlayerDataUpdated → OnPlayerStateUpdated → Index.SendPlayerState → `_resolverCommunicator.SendPlayerState`.
- Target-number receive/display path intact & unchanged: Index `TargetNumbers` handler → `RecordTargetNumberUpdates` → `OnTargetNumbersUpdated` → FormWeaponEntry/ComponentWeaponEntry/ComponentHeatAmmoEstimate `CheckRefresh`.
- Communicator method-group binding correct (binds to async overload).
- ResolverCommunicator (Scoped, IDisposable) creates `ClientToServerCommunicator` via `new` (not DI), Dispose calls `.Stop()` not `.Dispose()` — so new IDisposable on RedisCommunicator is NOT auto-disposed by DI here.

## Prime suspects (in priority order) for the live Redis-queue debug
1. **Send actually published?** Latch onto the client→server Redis channel; edit a field; confirm an Envelope is published. If NOT → break is client-side send path (despite code looking intact). If YES → break is server-side processing/return.
2. **New ProcessChannelMessage error logs** on either side (server-to-client or client-to-server processors) — exceptions that were silently swallowed pre-5aa0df6 now get logged. Check both Blazor-server and Silo logs.
3. **`HandleGetGameOptionsRequest` → `HandleGetPlayerOptionsRequest` rename** — confirm semantics preserved.
4. **Logging refactor** (Serilog→MEL): if logging throws inside a processor/actor path it could abort processing. Verify logging actually initializes & writes on both server and Silo with 0.0.446.
5. **NuGet 0.0.446 actually built from this source?** Confirm CustomNugets `Faemiyah.BtDamageResolver.Api/Common 0.0.446` matches current Api/Common source (rebuild via build.bat if unsure). (User previously de-prioritized stale-nuget theory, but worth a sanity check since the bump is in 5aa0df6.)

## Client-side secondary regression (may be separate from round-trip break)
GameState setter no longer fires `OnPlayerUnitListUpdated` on server push. The main editing view (FormGameState subscribes to it) may rely on it to refresh on authoritative server pushes / cross-client updates. NOTE: Index.razor GameState handler still calls `InvokeStateChange()` which cascades through ContainerTab→FormGameState (most components use default ShouldRender=true; only ComponentPlayerState & FormNumber override it), so cascade MAY compensate — verify empirically. If editing view still doesn't reflect server pushes after the round-trip is fixed, restore `NotifyPlayerUnitListUpdated()` in the GameState setter (additive).

## Review-tracking task state (opus47)
- Completed & moved to `opus47_review_completed_b.md`: ContainerTab (intentional), @foreach @key, OnInitialized churn (bracket memoization in CommonData.cs).
- In progress (NOT yet verified/moved): B2 DOM flattening — ComponentUnit done (uncommitted, not visually verified); remaining: FormWeaponEntry, FormUnitEntry, FormPaperDoll variants, B2 misc (FormTextArea echo, MainLayout wrapper div, ContainerReorderableList wrappers).
- Resume order: FIX REGRESSION FIRST, then visually verify ComponentUnit, then continue B2.

## Build/run notes
- Client build: `cd BtDamageResolverClient/src/BlazorServer; dotnet build --nologo -clp:ErrorsOnly` (~5s, 12 pre-existing warnings, 0 errors).
- Release flow: build.bat (rollversion + producenugets) then bump client PackageReference to match.
