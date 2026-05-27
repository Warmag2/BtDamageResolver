# BtDamageResolver — Review items completed

Items moved here from `opus47_review.md` as they are addressed.

## A2. Orleans patterns & concurrency

- **No `[Reentrant]` on `GameActor` or `PlayerActor`.** Every request is serialised by the grain turn-based scheduler. `CheckGameStateUpdateEvents` issues many awaited cross-grain/Redis/Postgres calls — even read-only `RequestGameState` blocks behind fire events.
  - **Not applicable (false positive)**: investigation showed the two suspect call sites in `GameActor.TurnLogic.cs:209,279` use `IsUnitInGame` as a plain method on `this` (not via `GrainFactory`), so no scheduler dispatch / reentrancy is involved — the `await` is just the `Task<bool>` return type. The single true external grain call is `PlayerActor.Sends.cs:32`, where `IsUnitInGame` is synchronous (`Task.FromResult`) and occupies one Orleans turn. Adding `[Reentrant]` / `[AlwaysInterleave]` would give no measurable benefit for this code shape. The real perf wins live in A2.2–A2.5 (replace O(P·U·W) scans with a `Dictionary<Guid, UnitEntry>` index) and A2.7 (`CheckGameStateUpdateEvents` storm).

## A3. State, serialization, aliasing

- **A3.18 `Api/Entities/Credentials.cs:36, 43` — `[StringLength(32 …)]` for name and password; password regex `^\S*$` allows the empty string.**
  - **Partially fixed**: bumped password `StringLength` from 32 → 128 (modern password managers routinely generate 64+ char passwords; SHA-512 hashing is fixed-output anyway so wire size is the only concern). Name length left at 32 — fine for player/game names. The empty-password regex is **not** changed: empty value is intentionally treated as "no password" by `PlayerActor.Connections` / `GameActor`, and replacing that semantic is part of the A8 security overhaul (PBKDF2 + explicit registration flow), not this minor item. Merging the `Required`+`MinimumLength=1`+regex on Name was also skipped — they are not redundant in DataAnnotations semantics (`Required` checks null; `MinimumLength=1` checks the rendered string), and the rendering relies on both attributes being present for distinct user-visible error messages.

- **A3.17 `UnitEntry.GenerateName` uses `int.Parse` / `string.Concat` without `CultureInfo.InvariantCulture` and `char.IsNumber` instead of `char.IsAsciiDigit`.**
  - **Fixed**: switched to `char.IsAsciiDigit` (sidesteps Unicode numeric chars like `½`, `Ⅷ` that would pass `IsNumber` but throw `FormatException` in `int.Parse`), pinned `int.Parse` to `CultureInfo.InvariantCulture`, and explicitly formatted the resulting integer with `InvariantCulture` so the interpolated `$"..."` no longer pulls `CurrentCulture` group separators (e.g. fi-FI thin-space) into copy names. Added `System.Globalization` using.

- **A3.16 `UnitEntry.FromUnit` does not update `TimeStamp`.**
  - **Fixed**: added `TimeStamp = DateTime.UtcNow` to the FromUnit assignment block. Not a current bug — the only caller (`FormUnitEntry.razor:591`) immediately overwrites `Id` with `Guid.NewGuid()`, so server-side `UnitList.IsNewOrNewer` short-circuits to `true` on the unknown Id without consulting `TimeStamp`. The one-line fix aligns the three data-replacement paths (`UnitEntry()` ctor, `Copy()`, `FromUnit()`) and removes the footgun for any future caller that loads template data without resetting Id.

- **A3.15 `UnitEntry.WeaponBays` uses `new` to hide `Unit.WeaponBays`.**
  - **Not a bug at current state**: `Unit.WeaponBays` is `List<WeaponBayReference>` (static templates); `UnitEntry.WeaponBays` shadows it with `List<WeaponBay>` (live state with ammo/firing solution/timestamp). The deeper concern — "any base-class code touching `WeaponBays` sees the wrong (empty) list" — has no instances in this codebase: `Unit`'s only methods (`CanMountWeapon`, `IsAmmoTracking`, `IsHeatTracking`, `HasFeature`, `SetFeature`) do not touch `WeaponBays`. Cross-grain JSON transport via Orleans's `AddJsonSerializer` (`Silo/Program.cs:123`) correctly serializes only the most-derived shadowed property in System.Text.Json (.NET 7+ behaviour). Removing the shadow would require a costly redesign (parallel `LiveWeaponBays` / different name) that contradicts the model intent ("UnitEntry IS-A Unit with richer weapon-bay state"). Leaving as-is.

- **A3.14 `PlayerActor.Internal.cs:62-72` `SendOnlyThisPlayerGameStateToClient` allocates a new `SortedDictionary` per send.**
  - **Already covered by A3.7 / A3.8 (not a bug)**: this single-entry `SortedDictionary` allocation is the same point already adjudicated under A3.7 — the DTO type is `SortedDictionary<string, PlayerState>` because the UI consumes its stable sort order, and constructing a single-entry instance per send (≤30 players, only on actual update events) is sub-microsecond. Removed from review.

- **A3.1 `PlayerActor.Internal.cs:44-55` `GetPlayerState` returns a new `PlayerState` but `UnitEntries.ToList()` is shallow.**
  - **Documented as A3.1 in the original first pass**: covered by the remarks block on `GetPlayerState` and the cross-grain JSON round-trip via `AddJsonSerializer`. The duplicate bullet that survived under A3 has been removed from the review document.

- **A3.13 `UnitEntry.Copy()` omits `Evading` and `Stance`.**
  - **Fixed**: added `Evading = Evading` and `Stance = Stance` to the new-instance initializer. These are per-turn situational flags consistent with `Movement`, `MovementClass`, `Narced`, `Tagged` (which were already copied). The deeper fragility (manual property-by-property copy) is left as-is per user preference (bespoke deep-copy is the same problem as A3.13 — relying on the JSON serializer round-trip is preferred where possible).

- **A3.12 `UnitList.IsNewOrNewer` uses `<` instead of `<=`.**
  - **Not a bug**: `<` is the correct cache-invalidation semantics. The method answers "is this incoming unit newer than the cached copy?" — equal timestamps mean the client retransmitted unchanged state and target-number recomputation should be skipped. The review's `<=` suggestion would cause spurious recomputes on every unchanged retransmit. The hypothetical "two distinct edits at the same millisecond on Windows-resolution clock" edge case would need a sequence number or content hash to fix, not `<=`; on .NET 10 / Linux, `DateTime.UtcNow` resolution is sub-microsecond so it is essentially impossible in practice.

- **A3.11 entity types lack `IEquatable<>` / `ToString()` (serializer "thrash" / meaningless log messages).**
  - **Partially fixed, partially not-a-bug**: The "all-public-mutable properties" and "no `IEquatable<>`" parts are not bugs — these are DTOs that require setters for the JSON serializer, and they are not used as dictionary keys / in HashSets / compared by value. The "serializer thrash" claim is incorrect (JSON serializer does not consult `IEquatable`). The only real symptom was `PlayerActor.Sends.cs:81` interpolating a whole `UnitEntry` into a user-visible error string, yielding `"Unit Faemiyah.BtDamageResolver.Api.Entities.UnitEntry has the following errors..."`. Fixed in-place by using `{unit.Name} ({unit.Id})` instead, which is more useful than a generic `ToString()` override would have been.

- **A3.10 `DamageReportContainer` grows unbounded across turns.**
  - **Not a bug (intentional)**: games typically run 6–15 turns and the client UI (`FormDamageReports.razor`) shows full history in a per-turn accordion. The dictionary is reset when all players leave (`GameActor.cs:220`). At this scale, serialization and join-time `GetAll()` cost are negligible.

- **A3.9 `[.. _gameActorState.State.PlayerIds]` per-broadcast allocation in `GameActor.Distribution.cs`.**
  - **Not a bug (not worth fixing)**: at the user's ≤30 player upper bound (and ~2–6 typical), the `List<string>` materialization is sub-microsecond per send. Eliminating it would require changing `SendToMany`'s signature chain from `List<string>` to `IEnumerable<string>` across `CommunicationServiceClient` / `CommunicationService` / `ICommunicationService` / `RedisServerToClientCommunicator`. The "non-deterministic order" concern is moot because each consumer just iterates and sends to each player independently — order doesn't affect correctness.

- **A3.7 / A3.8 `SortedDictionary<string, PlayerState>` for `GameActorState.PlayerStates` / `GameState.Players` / `SendOnlyThisPlayerGameStateToClient`.**
  - **Not a bug (intentional)**: the sort order is consumed by the UI to render a stable, alphabetically ordered player list in `ComponentGameState.razor` / `FormGameState.razor`. Switching to `Dictionary` would let player rows reshuffle on every update because `Dictionary` does not guarantee iteration order across mutations. The per-send single-entry `SortedDictionary` allocation in `PlayerActor.Internal.cs` is a trivial cost and keeps the DTO type consistent.

- **A3.6 parameterless `LeaveGame()` "auth bypass"**.
  - **Not a bug**: the parameterless overload at `PlayerActor.Connections.cs:122` reads `_playerActorState.State.AuthenticationToken` and delegates to the authenticated overload — it self-authenticates rather than bypassing auth. It is not exposed on the wire protocol (`HandleLeaveGameRequest` always carries a token), and its only caller is the in-cluster fire-and-forget at `GameActor.Tools.cs:42` (KickPlayer deadlock avoidance). Cluster peers are trusted.

- **A3.4 + A3.5 `PlayerActor` disconnect-path persistence ordering.**
  - **Fixed**: `MarkDisconnectedStateAndSendToClient` now calls `await _playerActorState.WriteStateAsync()` immediately after clearing `GameId`/`UpdateTimeStamp` and before sending anything to the client. `Disconnect` reordered to also write before responding (previously wrote after `SendDataToClient` + `LogPlayerAction`). Eliminates the window where the client believes it has disconnected while persistent storage still holds the old `GameId`.

- **A3.3 `GetGameState` mutates `playerState.TimeStamp` in actor state while supposedly returning a snapshot.**
  - **Fixed**: removed the in-method `foreach` that mutated `player.Value.TimeStamp = timeStampNow` when `markStateAsNew` was true. `CheckForFireEvent` already sets every `playerState.TimeStamp = TurnTimeStamp` at the moment the fire occurs (and persists via `WriteStateAsync`), so the re-stamp in `GetGameState` was redundant and made an ostensibly-pure read mutate actor state at distribution time. The `markStateAsNew` flag now only affects the top-level `GameState.TimeStamp` as documented.

- **A3.1 `SendPlayerState` / `GetPlayerState` cross-grain aliasing of `PlayerState` / `UnitEntry`.**
  - **Documented (not coded around)**: bespoke deep-copy for `UnitEntry` is fragile (cf. A3.13 — every new property risks silent omission). The actual safety guarantee comes from `AddJsonSerializer` for `Faemiyah.BtDamageResolver.*` types in `Silo/Program.cs:123`, which round-trips every cross-grain / Redis call. Added XML remarks on both `GameActor.SendPlayerState` and `PlayerActor.GetPlayerState` documenting the dependency so a future serializer swap would flag the assumption.

- **A2.12 `CheckGameStateUpdateEvents` catch wraps original exception in `InvalidOperationException`.**
  - **Fixed**: replaced `throw new InvalidOperationException(...)` with bare `throw;` so the original stack trace and exception type are preserved when faulting back through Orleans. The descriptive log message (already present) carries the same context the wrapper added.

- **A2.11 `.Ignore()` on `IPlayerActor.LeaveGame()` silently swallows exceptions.**
  - **Fixed**: replaced `.Ignore()` with a reusable `Task.LogAndForget(ILogger, messageTemplate, args)` extension in `BtDamageResolver/src/Actors/Extensions/TaskExtensions.cs`. Preserves the deliberate fire-and-forget pattern (required to avoid the deadlock documented in the existing comment) but logs failures with structured context. `NotifyPlayerOfKickAsync` helper from the first attempt was dropped in favor of the cleaner extension API.

- **A2.10 `LoggingService` `[Reentrant]` unnecessary; `Task.Run` outside grain scheduler; Stop-time race.**
  - **Partially fixed**: removed the unnecessary `[Reentrant]` attribute (and dropped the `Orleans.Concurrency` using). The `Task.Run` background drain loop is correct for this queue-drain pattern (`ConcurrentQueue` + loop-owned DB connection) and was left as-is. Stop-time race is best-effort by design; not worth elaborate fix.

- **A2.9 `[StatelessWorker(1)]` + singleton `CachedEntityRepository` shared with `LogicUnit` — latent thread-safety bug.**
  - **Fixed**: `Dictionary<TKey, TEntity>` replaced with `ConcurrentDictionary<TKey, TEntity>`. The cache is a DI singleton accessed concurrently from both the owning `*RepositoryActor` grain (single-threaded writes) and directly from many `LogicUnit` instances during fire events (concurrent reads from arbitrary grain activations). Practically safe today only because the only mutated repository at runtime is `GameEntry` (not consumed by `LogicUnit`); now safe by construction. Added XML remarks documenting the concurrent access pattern.

- **A2.8 `GameEntryRepositoryActor.CleanupOldEntries` invoked on every `Get`/`GetAll`.**
  - **Fixed (with clarification)**: the review's claim that each deletion triggers a `Distribute()` broadcast was incorrect — the original code already used `base.Delete(...)` which bypassed the override. The real issue was that the full scan ran on every read (including the broadcast-path `GetAll`). Added a 5-minute throttle via `_lastCleanup` / `CleanupInterval`. When old entries are actually deleted, a single `Distribute()` now fires at the end of cleanup so connected lobby clients see the updated list (rare event, harmless broadcast).

- **A2.7 `CheckGameStateUpdateEvents` broadcast storm — every per-keystroke `SendPlayerState` triggered a full game-entry repository update + Postgres log + state writes + Redis broadcasts to all players.**
  - **Partially fixed**: added `bool refreshGameEntry = false` parameter to `CheckGameStateUpdateEvents`; `GameEntryRepository.AddOrUpdate` is now only called when `refreshGameEntry || fireEventHappened`. Callers updated: `JoinGame` and `LeaveGame` pass `refreshGameEntry: true`; routine `SendPlayerState`, `MoveUnit`, `ForceReady` callers do not refresh the lobby entry. `LogGameAction(GameActionType.Update)` kept (small DB rows). Client-side debounce intentionally skipped (real update rate is ~1/sec/player, not enough to matter). This removes the lobby-broadcast-to-all-observers spam that occurred on every form edit.

- **A2.6 `PlayerActor.PerformConnectionActions` four sequential awaited grain calls.**
  - **Deferred (low priority)**: this runs once per client connect, not steady-state. The 4 calls all target the same single-threaded `GameActor`, so `Task.WhenAll` does not parallelize them. A real fix would batch them into one `GameActor.RequestAllConnectionData(askingPlayerId)` turn (saves ~3 round-trips on connect). Not the ARM-slowness culprit; skip for now.

- **A2.2–A2.5 (`ProcessFireEvent` / `ProcessUnitTargetNumbers` / `GetAllUnitsWhichTargetUnit` / `GetUnit` quadratic-ish scans).**
  - **Not worth fixing at this scale**: with the practical ceiling of ~30 units per game, the worst case is ~30k ops per target-number refresh (sub-millisecond on any modern CPU including ARM). A `Dictionary<Guid, UnitEntry>` index would either need cross-mutation invalidation (high bug risk for ~µs savings) or a transient local rebuild (still microseconds saved). The "Blazor slow on ARM" symptoms are not caused by these scans; real culprits are in B (rendering), A4 (`MathExpression` reparsing per fire), A2.7 (broadcast storm), and A2.10 (`LoggingService` `Task.Run` outside grain). Dropped as false positives at the actual data scale.

## A1. Critical correctness bugs

- **`Actors/Logic/LogicUnit.Damage.cs:66-87` — `RapidFireWrapper` awaits the same `Task<int>` multiple times.** The caller (`ResolveTotalOutgoingDamage`, lines 312-323) passes a *single* `Task` produced by calling the damage method once. Inside `for (var ii = 0; ii < hits; ii++) { damage += await singleFireDamageCalculation; }` the `Task` only completes once. Every subsequent `await` returns the cached result and the underlying calculation never re-runs. Damage is computed once, multiplied by `hits`; side effects (logging, per-hit rolls) only happen for the first hit. Signature should accept `Func<Task<int>>`.
  - **Fixed**: `RapidFireWrapper` now takes `Func<Task<int>> singleFireDamageCalculation`; all four call sites (`LogicUnit.Damage.cs:317,321`, `LogicUnitBattleArmor.cs:92`, `LogicUnitInfantry.cs:152`) now pass a lambda that is invoked per hit.

- **`Api/ClientInterface/Communicators/RedisCommunicator.cs:135-140` — `SendSingle` log condition is inverted.** Warns `"instead of 1 as expected"` when `clientCount != 0`, i.e. on every successful delivery. Should be `clientCount != 1`.
  - **Fixed**: changed condition to `clientCount != 1` so the warning fires only on 0 or >1 deliveries.

- **`Api/ClientInterface/Repositories/CachedEntityRepository.cs:35` — `FillCache().Result` blocks on async work inside a constructor.**
  - **Fixed**: `FillCache` no longer had any awaits; converted it to `private int FillCache()` and removed the `.Result`. No deadlock risk and no spurious state machine.

- **`Api/ResolverRandom.cs:10-17` — `Random` is not thread-safe** and is registered as a DI singleton shared across all `LogicUnit` instances on all grains.
  - **Fixed**: replaced the per-instance `Random` field with `Random.Shared` (thread-safe, available in .NET 6+). Removed the constructor and `_rand` field. Added an XML remarks block explaining the thread-safety requirement.

- **`Actors/Logic/LogicUnit.DamageResolution.cs:152-192` — `RollHitLocation` has an unbounded `do/while` loop.**
  - **Not a bug** (user judgement). The only `Location.Reroll` in the codebase is the through-armor critical case, which resolves with probability ≥ 3/4 per roll, so unbounded loop probability is negligible. A proper fix would enumerate valid hit locations with their cumulative weights and draw from that — overkill for this scenario. Left as-is.

- **`Actors/Logic/LogicUnit.Heat.cs:142-145` — `ResolveHeatForSingleHit` ignores the `rangeBracket` parameter** and unconditionally returns `weapon.Heat[RangeBracket.Short]`.
  - **Not a bug** (false positive). For non-aerospace weapons heat is range-independent in BT rules, and `Weapon.Heat` is only populated for `RangeBracket.Short` when filled via `CollectionExtensions.Fill` (other brackets are zeroed because `RangeAerospace` defaults to Short for non-aerospace weapons). Indexing by `rangeBracket` would return 0 for Medium/Long/etc. and produce wrong heat. Only `LogicUnitAerospace` (and capital-ship logic) overrides this. Added an in-code comment in `LogicUnit.Heat.cs` documenting the intentional behaviour so this doesn't get "fixed" by a future reviewer.
