# BtDamageResolver — Review items completed

Items moved here from `opus47_review.md` as they are addressed.

## A4. Logic & ExpressionSolver

- **A4.1 `Expression.Construct` recursive allocations + substring slicing.**
  - **Resolved by A4.6 cache (no code change)**: every deterministic expression now hits the `MathExpression._resultCache` lookup path and never allocates an `Expression` tree on subsequent calls. The cache-miss path (dice expressions) intrinsically requires re-parsing every call because the result is non-deterministic — re-allocation is unavoidable for correctness there. With dice expressions in BT being short (`"1d6"`, `"2d10"`, etc.), per-call constructor cost is negligible.

- **A4.3 `Expression.Parse` mutates the instance (`_tokens.RemoveAt`, `_expressions.RemoveAt`).**
  - **Not worth fixing**: `Expression` instances are never reused — `MathExpression.Evaluate` constructs a fresh one per call and discards it after `Parse()` returns. The "cannot be parsed twice" framing in the review is correct in the abstract but irrelevant in practice: no caller does it. Making `Expression` immutable would be a sizable refactor (recursive copy on `CollapseOneStage`) for zero behavioral improvement. With the A4.6 cache, even repeated literal expressions only construct the tree once.

- **A4.6 + A4.9 `MathExpression.Parse` rebuilds the parse tree on every call; called per cluster / per special-damage entry / per fire event.**
  - **Fixed**: `MathExpression` now holds a `ConcurrentDictionary<string, int> _resultCache`. For deterministic expressions (those that do not contain the dice token `'d'`) the result is memoized; the first call evaluates and stores, subsequent calls do a hash lookup. Dice-bearing expressions bypass the cache and evaluate every call (correct — must re-roll). The cache is unbounded but the set of unique expression strings is finite (rules data + a handful of `$"{Unit.Tonnage}/N"` shapes ≈ ~100 entries), so memory is not a concern. `ConcurrentDictionary` is required because `MathExpression` is a DI singleton (`Silo/Program.cs:184`) accessed from many `LogicUnit` activations concurrently. Note: expressions containing `Round(...)` won't be cached because the function name contains a `'d'`; that's an acceptable miss — `Round` calls are rare and still correct.

- **A4.7 `LogicUnitFactory.CreateFrom` big `switch` allocating a new `LogicUnit{Type}` per call.**
  - **Not a bug**: `UnitType` is a closed enum that grows ~never. The "open/closed principle violation" framing is theoretical — adding a new unit type is a multi-file event regardless, and a strategy-pattern registry would add indirection without removing any of the actual work. The per-call allocation cost is fundamental: `LogicUnit` instances are stateful per `UnitEntry` (they carry the unit reference), so they cannot be pooled across activations.

- **A4.10 `LogicUnit.Hit.cs:60-81` `GetHitChanceForTargetNumber` missing explicit `case 2` / inconsistent `>12` vs `<3` style.**
  - **Fixed**: switch now has explicit `case < 3: return 1m;` and `default: return 0m;` arms, with cases 3–12 listed in between. No more implicit fall-through to a `1m` default.

- **A4.8 `LogicUnit.Damage.cs:25, 100, 146, 413` — repeated `DamageReport` construction with `GetDamagePaperDoll` lookup + clone.**
  - **Not a bug (intentional)**: each `DamageReport` is the result-carrying DTO for one (attacker × target × weapon) combat resolution; the embedded `DamagePaperDoll`, `FiringUnitIds`, `FiringUnitNames`, etc. are all load-bearing fields the consumer reads. The `GetDamagePaperDoll(...)` repo lookup is already cached at the `CachedEntityRepository` level (singleton, in-memory), and the subsequent `.ToDamagePaperDoll()` clone is mandatory because damage tracking mutates the paperdoll during resolution. No fat to trim.

- **A4.15 `LogicUnit.HitModifier.cs:25-143` — `ResolveHitModifier` 25-call chain of `attackLog.Append` + compute + sum at the bottom.**
  - **Fixed**: replaced the ~115-line compute/log/sum triple-listing with a `(string Label, int Value)[]` modifier table iterated once for both log emission and total. Each modifier now appears in exactly one place (was three: local declaration, log append, sum expression). Eval order, log content, and target-number arithmetic are unchanged. Body shrank from ~115 lines to ~40. The pre-existing footnote that the "`damageReport` may be null" was misleading (the method does not take a `damageReport`; only an `AttackLog`) and was removed — both callers always pass a non-null `AttackLog`, the difference being whether it is the live one off `hitCalculationDamageReport.AttackLog` or a throwaway one created in `GameActor.TurnLogic.cs:307` for target-number hover-tooltip projection.

## A5. Repositories & caching

- **A5.6 `RedisEntityRepository` catches `DbException` — StackExchange.Redis throws `RedisException`, not `DbException`; those branches never fired and the generic catch wrapped the failure without preserving the inner exception, losing stack traces.**
  - **Fixed**: dropped the dead `catch (DbException ex)` branches from all six methods and the `System.Data.Common` using. The remaining `catch (Exception ex)` now wraps with `new DataAccessException(DataAccessErrorCode.OperationFailure, "<context>", ex)` — Redis (or any other) failures now carry both a descriptive message and the original exception as `InnerException`. Same change applied to `CachedEntityRepository` for consistency. The `catch (DataAccessException) throw;` re-throw patterns are preserved in `GetAll`/`UpdateAsync` to keep the `NotFound` vs `OperationFailure` distinction.

- **A5.9 `CachedEntityRepository.DeleteAsync` short-circuits to `return false` if the cache lacks the key, even when the backing repository still holds it.**
  - **Fixed**: now always delegates to `_repository.DeleteAsync(key)` and uses its return value as the source of truth. Cache eviction (`_cache.TryRemove(key, out _)`) runs after, so a stale cache entry can't shadow a real backing-store record. Single-silo deployment couldn't actually hit the bug (cache is authoritative there), but the fix removes the latent multi-instance hazard for the cost of a single round-trip and makes the semantics match the method name.

- **A5.5 `RedisEntityRepository.UpdateAsync` — `KeyExistsAsync` then `AddAsync` is not atomic.**
  - **Fixed**: replaced the two-step check-then-add with a single `connection.StringSetAsync(key, value, when: When.Exists)`. Redis evaluates the `XX` condition atomically; the call returns `false` if the key didn't exist, in which case we throw `DataAccessException(DataAccessErrorCode.NotFound)`. Same semantics as before in the happy path; multi-silo safe. Also dropped the indirect call to `AddAsync` — the inline `StringSetAsync` is the same operation, and the catch block in `UpdateAsync` already produces a proper context-rich `DataAccessException` so the previous "into redis database" log line from `AddAsync` is not lost.

- **A5.7 `RedisEntityRepository.GetServer` — `_connectionString.Split(',')[0]` for endpoint extraction.**
  - **Fixed**: replaced the string-parsing hack with `_redisConnectionMultiplexer.GetEndPoints()[0]` and the `GetServer(EndPoint)` overload. The multiplexer already knows its parsed endpoints, so we let it speak for itself rather than re-parsing the connection string. Eliminates the latent failure mode where a user puts options first (`password=x,redis:6379`) — `SE.Redis` accepts that ordering but `Split(',')[0]` would have returned `password=x` and `GetServer` would throw. With the field no longer needed elsewhere, `_connectionString` was dropped from the type entirely — `connectionString` parameter is consumed directly inside the constructor.

- **A5.11 `CachedEntityRepository.GetAllKeys` N+1 fetch loop with two minor footguns.**
  - **Fixed (cleanup, kept N+1)**: the N+1 framing was misleading at this scale — the typical mid-run case is 0–2 missing keys (entries added by another process, e.g. the client adding a `Unit` while the server holds it cached); pipelining/batching would not pay off. Loop kept, but two latent bugs fixed: (1) `_cache.TryAdd(item.GetName(), item)` → `_cache.TryAdd(key, item)` (semantically identical but avoids the extra method call and reads cleaner), and (2) null-check the result of `_repository.Get(key)` — if a concurrent delete races between `GetAllKeys` and the per-key fetch, `Get(key)` returns null and the old code would NRE on `item.GetName()`. Also replaced the LINQ `.Where(...)` with an inline `continue` for readability. Added a one-line comment naming the use case (client/process-added entries) since this loop is the only thing standing in for proper cache-invalidation in multi-process deployments.

- **A5.13 Two serializers in the system — divergent `JsonSerializerOptions` configurations between Orleans's `AddJsonSerializer` and `DataHelper`'s DI-injected `IOptions<JsonSerializerOptions>`.**
  - **Fixed (single source of configuration)**: extracted the configuration into a private `ApplyDefaultJsonSerializerOptions(JsonSerializerOptions)` in `ConfigurationUtilities` and added a public `CreateJsonSerializerOptions()` helper that calls it. The `ConfigureJsonSerializerOptions(IServiceCollection)` extension now delegates to the same private method (for the `IOptions<>` path used by `DataHelper`/`RedisEntityRepository`). `Silo/Program.cs` line 123 now passes `CreateJsonSerializerOptions()` to `AddJsonSerializer(isSupported, jsonSerializerOptions)`. Two `JsonSerializerOptions` instances exist (one for Orleans's `JsonCodec`, one for the DI-resolved `IOptions<>`) but their configuration comes from one code path — adding a converter or changing `DefaultIgnoreCondition` in the future updates both. Did NOT collapse to a single instance via `OptionsWrapper<>` because that bypasses the `IOptions<>` plumbing and silently breaks any future `services.Configure<JsonSerializerOptions>(...)` registration — the bug-avoidance goal is fully served by sharing the configuration callback, not the instance.

- **A5.15 LZMA is expensive on weak ARM CPUs; every inbound state message in `Pages/Index.razor` (8 call sites) blocks the Blazor circuit thread on `_dataHelper.Unpack<>`.**
  - **Fixed (wire-format change + configurable provider)**: introduced `CompressionOptions { CompressionProvider Provider; int Quality; }` in `Api/ClientInterface/Compression/` (kept in `Api` rather than `Common` so the Api NuGet stays self-contained and doesn't grow a `Common` dependency). `DataHelper` now takes `IOptions<CompressionOptions>` and dispatches: Brotli uses `BrotliEncoder.TryCompress(quality, window: 22)` for compression and `BrotliStream(CompressionMode.Decompress)` for decompression (no quality needed); LZMA still delegates to `SevenZip.Compression.LZMA.CompressionHelper`. `Quality` (0-11) is also exposed on `CompressionHelper.Compress(byte[], int quality = 4)` and mapped through to LZMA's `NumFastBytes` (5-273); `Algorithm` and `Dictionary` stay at the previous hardcoded defaults. Settings block `CompressionOptions { Provider, Quality }` added to `SiloSettings.json` and `CommunicationSettings.json`, both set to `Brotli` / `4` to flip the wire format. Code default stays `Lzma` so any unbound consumer keeps the old behavior. `CompressionTesterApp/Program.cs` updated to construct the new two-arg `DataHelper`.

- **A5.14 `DataHelper.Pack/Unpack` round-trip through a UTF-8 `string`.**
  - **Fixed**: `Serialize` now uses `JsonSerializer.SerializeToUtf8Bytes(input, _jsonSerializerOptions)` directly. `Deserialize` now uses the `JsonSerializer.Deserialize<T>(byte[], options)` overload that reads UTF-8 bytes directly. Removed the `System.Text` using and dropped two per-call allocations (intermediate `string` + intermediate `byte[]`) from every `_dataHelper.Pack`/`Unpack` call — relevant because the Blazor client's `Pages/Index.razor` hits `Unpack<>` on every inbound state update (eight call sites).

- **A4.17 `Actors/GameActor.cs:121-138` — `SendDamageInstance` lacks an "ownership" check on attacker/target.**
  - **Not a bug (intentional design)**: `DamageInstance` is the out-of-program-scope damage channel — falls off a cliff, artillery, bombs, etc. It is meant to let any player in a game apply external damage to any unit to speed gameplay along. The only gate that matters is "sender is in this game", which is already enforced at `GameActor.cs:133` (`!_gameActorState.State.PlayerIds.Contains(sendingPlayerId) => return false`). Closed.

- **A4.2 `Expression.cs:174-180, 229, 258` — `decimal.Parse` / `int.Parse` use current culture.**
  - **Fixed**: both `decimal.Parse` call sites in `Construct` now pin to `CultureInfo.InvariantCulture` (lines 241, 270). Previously, on `fi-FI` etc., decimal points became commas → silent parse failures when expressions (e.g. weapon `Rapid` data) used `.`. The `decimal.ToInt32` calls on intermediate `Parse()` results inside `CollapseOneStage` are culture-independent (already-decimal value).

- **A4.4 `ExpressionExtensions.IsToken` calls `Enum.GetValues<Token>().Any(...)` per character.**
  - **Fixed**: `Expression` now holds a `static readonly HashSet<char> _tokenChars` populated once from `Enum.GetValues<Token>()`. `IsToken` is now an O(1) hash lookup. The extension method form was also dropped in favor of a static `Expression.IsToken(char)` (see next item).

- **(extra) `ExpressionExtensions` class removed.**
  - The single `IsToken(this char)` extension was folded into `Expression` as a static method. The extension class added no value — it sat in the same namespace as its only caller and was only consumed by the same project. Test fixture (`ExpressionTests`) updated to call `Expression.IsToken(input)` directly.

- **A4.5 `Expression.cs:131-153` — `ExtractFunctionType` allocates an `Enum.GetNames<ExpressionFunction>()` array per call and uses `SingleOrDefault` (throws on >1 match).**
  - **Fixed**: cached `static readonly string[] _functionNames = Enum.GetNames<ExpressionFunction>()` once at type-init time. Replaced the `SingleOrDefault` LINQ chain with a plain `foreach` that breaks on the first prefix match. Behavior is unchanged for the current enum (`None`, `Round`, `Ceil`, `Floor` — none are prefixes of each other), but the per-call reflection allocation is gone, and the future "two function names share a prefix" footgun is also gone (first-match instead of throw-on-ambiguity).

- **A4.11 `Actors/Logic/LogicUnit.Damage.cs:10` — unused `using Microsoft.CodeAnalysis;`.**
  - **Fixed**: removed.

- **A4.12 `Actors/Logic/LogicUnit.General.cs` — misspelled local `hitCalclulationDamageReport` and `weapon.SpecialFeatures.Select(...).ToList()` discarded allocation.**
  - **Typo fixed** in source; only the review document still contains the misspelling. The `.ToList()` allocation in `LogicUnit.General.cs:41` remains (functionally identical to the collection-expression `[.. ...]` style used in `LogicUnit.Damage.cs:99` — both materialize a `List<WeaponFeature>`). At 30-unit ceiling this is sub-microsecond per fire event; eliminating the allocation would require changing `GetDamagePaperDoll`'s parameter type from `List<WeaponFeature>` to `IEnumerable<WeaponFeature>` across the call chain. Not worth the contract change.

- **A4.13 `Actors/Logic/LogicUnit.Fetching.cs:42-54` — `FormWeapon` is `async Task` with no awaits.**
  - **Fixed**: signature is now `protected Weapon FormWeapon(WeaponEntry weaponEntry)` (sync). State machine allocation eliminated. All call sites (`LogicUnit.Ammo.cs:23`, `LogicUnit.General.cs:102`, `LogicUnit.Heat.cs:27`, `LogicUnit.HitModifier.cs:20`, `LogicUnitAerospaceLarge.cs:91`) call it without `await`.

- **A4.14 `Actors/Logic/LogicUnit.Ammo.cs:51-69` — `ProjectAmmo` is `async Task` with only one (now-sync) await.**
  - **Fixed**: `ProjectAmmo` is now sync (`public (decimal Estimate, int Max) ProjectAmmo(...)`). Its single caller (`GameActor.TurnLogic.cs:312`) consumes the tuple synchronously. `ResolveAmmo` remains `Task`-returning (virtual override point — `LogicUnitAerospaceLarge` keeps the async signature for shape consistency even though its override has no awaits either; that's a downstream cleanup, not the current item).

- **A4.16 `Actors/GameActor.TurnLogic.cs:225` — claim of redundant `Turn` re-stamp**.
  - **Not a bug**: the two `Turn` assignments are in separate code paths. `ProcessFireEvent` (`TurnLogic.cs:232`) sets `Turn` on the list of fire-event damage reports it just produced. `SendDamageInstance` (`GameActor.cs:139`) sets `Turn` on its own single damage report produced by `ProcessDamageInstance`. Neither path touches the other's reports. Review item was based on misreading.

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
