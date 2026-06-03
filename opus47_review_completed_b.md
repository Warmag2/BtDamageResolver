# PART B — Blazor client — completed review items

## B1. Render performance

### Controller event model fix + All Units render redesign (`@key` instability, read-only path)

**Bug found and fixed in `UserStateController`.** The `GameState` setter unconditionally called
`NotifyPlayerUnitListUpdated()` on every game-state push, firing `OnPlayerUnitListUpdated`
(meant to signal "this player reordered units") on every server packet. `UpdateUnitList()`
computed whether the unit list actually changed but discarded that information.

Changes:
- `UpdateUnitList()` now returns `bool` (whether the game unit list membership/identity changed).
- Added event `OnGameStateUpdated`, raised by the `GameState` setter (via `NotifyGameStateUpdated()`)
  whenever a newer game state is accepted.
- The `GameState` setter now raises `OnGameUnitListUpdated` only when `UpdateUnitList()` reports a
  real change, and `OnGameStateUpdated` on every accepted push. The unconditional
  player-unit-list notification was removed; reorders still fire it explicitly from
  `FormUnitEntry` / `FormGameState`.

**Render redesign for the read-only "All Units" subtree** (`ComponentGameState → ComponentPlayerState → ComponentUnit → ComponentWeaponEntry`).

Root problem: each server push deserializes a brand-new `GameState` object graph
(`Index.razor` → `_dataHelper.Unpack<GameState>()`), so any component holding a child object
(`UnitEntry`) as a `[Parameter]` is orphaned the instant the next push lands. The old
`@key="{Id}_{TimeStamp}"` pattern hid this by destroying and recreating whole subtrees on
every mutation.

New design (changes flow downward naturally; Blazor diffs instead of recreating):
- **`ComponentGameState`** subscribes to `OnGameStateUpdated` and re-renders on each push. It reads
  the fresh `GameState.Players` and passes each fresh `Player` object down with a **stable**
  `@key="{PlayerId}"`. This is the refresh driver, so it intentionally has no render gate.
- **`ComponentPlayerState`** now takes the whole `Player` (`PlayerState`) object. Because it looks
  players up by `PlayerId` it has no orphan problem; re-running its `foreach` hands fresh
  `UnitEntry` references down. It has a `ShouldRender()` gate comparing `Player.TimeStamp` so
  players whose state did not change skip their whole subtree. (Verified safe: every client unit
  edit calls `NotifyPlayerDataUpdated()` → `PlayerState.TimeStamp = UtcNow`, and turn resolution
  sets `playerState.TimeStamp = TurnTimeStamp` in `GameActor.TurnLogic.cs`.)
- **`ComponentUnit`** is now a pure presentational component with no subscription. Its
  `@key="{unitEntry.Id}_{unitEntry.TimeStamp}"` moves recreation down to the smallest item, so
  only units whose timestamp actually changed are recreated; unchanged units diff in place.
- **`Index.razor:46`** key for `ComponentGameState` made stable (`{IsConnectedToGame}`), removing
  the per-turn recreation of the entire All Units subtree.

Result: a single unit change re-renders only that unit (and its player container), not the whole
tree; unchanged players short-circuit via `ShouldRender`.

*Still open in the broader `@key` bullet:* the editable Dashboard/Data-tab forms
(`FormGameState`/`Index.razor:42`, `FormUnitEntry`, `FormWeaponEntry`, `FormFiringSolution`) and the
`Index.razor:50,54` keys (`FormDamageReports`, `FormOptions`).

### Hot `OnTargetNumbersUpdated` subscribers re-rendering on every packet

`ComponentWeaponEntry`, `ComponentHeatAmmoEstimate` and `FormWeaponEntry` all subscribed
`OnTargetNumbersUpdated += InvokeStateChange`, so every weapon row of every unit/player re-rendered
on every target-number packet — even packets for unrelated units. Target numbers are stored
per-unit (`_targetNumbers[UnitId]` with a `TimeStamp`).

Fix: each component now subscribes a guarded `CheckRefresh` that re-renders only when
`GetTargetNumberUpdate(<unitId>)?.TimeStamp` for **its own** unit advances (seeded in
`OnInitialized`). Unit-level granularity is correct because a unit's `TargetNumberUpdate` covers all
its weapons, so changing one weapon legitimately refreshes that unit's rows while leaving every
other unit untouched.

### Cascading `_userStateController` access in `FormUnitEntry` markup
`FormUnitEntry.razor` dereferenced `_userStateController.PlayerState.IsReady` 29 times and
`_userStateController.ComparisonTime` 8 times per render. `ComparisonTime` (`UserStateController.cs:253`)
is a computed property (`PlayerOptions?.HighlightUnalteredFields == true && GameState != null ? ... : DateTime.MinValue`),
so it re-evaluated that whole conditional on every reference, ×N units per render.

Fix: cache the values once at the top of the `@if(IsConnectedToGame)` block as locals
(`isReady`, `comparisonTime`, `isInvalidUnit`) and reference those throughout. Values are stable
within a single render pass, so behaviour is unchanged; ~37 chained/computed accesses collapse to 3.

### `FormTools` recreated on every state push (+ latent modal-close bug)
`FormGameState.razor:51` keyed `<FormTools @key="@GameState?.TimeStamp">`. `GameState.TimeStamp`
advances on every inbound push, so `FormTools` was disposed and rebuilt on every state update, even
while collapsed (the Tools accordion always renders its `ChildContent`, only CSS-hiding it). It was
also a latent bug: `FormTools` holds its open-modal flags (`_showModalKick`, `_showModalMoveUnit`)
as local state, so any push while the admin had the Kick/Move-Unit modal open recreated the
component and silently closed the modal.

Fix: removed the `@key` entirely. `FormTools` is a single child (not a list sibling, so it needs no
key) and reads everything live from `UserStateController` with no `OnInitialized`-cached display
state, so dropping the key is strictly safe — it now diffs in place, preserving modal state and
avoiding per-push recreation.

### Index-level editable `@key`s (Index:42/50/54) — deliberately left as-is
Considered converting `FormGameState`/`FormDamageReports`/`FormOptions` keys (on `TurnTimeStamp` /
`DamageReportContainer.TimeStamp` / options `TimeStamp`) to stable keys. Decided **not to**: these
churn only on turn resolution (~hourly), per new damage report (turn-tied), and option changes
(≈game start) — none hot. A stable-key conversion would force reworking every editable leaf form
(`FormSelect`, `FormComboBox`, `FormNumberPicker`, `FormRadio`, `FormPickSet`, …) which cache their
displayed value in `OnInitialized` and rely on key churn to refresh — and whose value setters have
side effects (e.g. `FormSelect.SelectedOptionInternal` fires `OnChanged.InvokeAsync`). That is a
high-risk correctness change (stale-display / phantom-edit feedback loops on the editable path) for
no measurable perf gain, so the keys were left unchanged.

### Inline lambdas as `OnChanged` — mostly required, two convertible
The review flagged inline lambdas (`(T x) => Handler(x)`) on form `OnChanged` callbacks as per-render
delegate allocations to hoist into method groups. Investigation found this is **mostly not
actionable**:
- **Generic form components** (`FormSelect`, `FormRadio`, `FormComboBox`, `FormPickSet`, all
  `@typeparam`) reject a method group for `EventCallback<T>` — the compiler cannot infer the
  component's type parameter from a method group (build error CS1503). The typed lambda is mandatory.
  This covers the bulk of the call sites (`FormUnitEntry` UnitType/Features/MovementClass/Movement/
  Stance, all of `FormFiringSolution`, `FormWeaponEntry` WeaponName/Ammo).
- **Loop-capturing lambdas** (`ContainerReorderableList` per-item index, `FormOptions`/
  `FormDamageReport` `@foreach`) close over per-iteration state, so they cannot become static method
  groups either. (`FormOptions`/`FormDamageReport` also render rarely — not hot.)
- **Convertible (done):** the two non-generic `FormNumberPickerDisplayOnly` `OnChanged` lambdas in
  `FormWeaponEntry` (Amount, Modifier) → method groups `@OnWeaponAmountChanged`/`@OnWeaponModifierChanged`,
  consistent with how `FormUnitEntry` already binds `FormNumberPicker`. Removes one closure
  allocation per weapon row per render.

The only allocation-free path for the generic-component callbacks would be pre-built
`EventCallback<T>` fields constructed in `OnInitialized` — verbose for marginal benefit, not pursued.

### Large objects as parameters — deliberately left as-is (architectural)
The review suggested replacing whole-object parameters (`UnitEntry`, `WeaponBay`, `WeaponEntry`,
`DamageReport`, `DamagePaperDoll`) with minimal value-type parameters so Blazor's
`ChangeDetection.MayHaveChanged` could skip unchanged children. Not done: the entire client edits
these domain objects **in place** and calls `NotifyPlayerDataUpdated()`; decomposing them into
scalar parameters would break that mutation model and force a rewrite of every form. Verified the
"unreliable `MayHaveChanged`" is not causing an active bug — the only two `ShouldRender` overrides
(`ComponentPlayerState`, `FormNumber`) gate on an explicit timestamp / edit-flag, not on parameter
equality. Render-skipping where it actually matters is already handled by the targeted
`@key`/`ShouldRender` work.

### `<Virtualize>` — scoped to the FormComboBox option list only
Skipped `<Virtualize>` on the lists (`ComponentGameState`, `FormDamageReports`, `FormGameList`):
realistic scale is small (a few players, a few dozen units), and the All Units list now uses a
per-unit subscription + `ShouldRender` model that windowed rendering would conflict with.

Fixed the real DOM-bloat case instead — `FormComboBox` (`Generic/FormComboBox.razor`). Previously it
rendered a `<div>` for **every** option always (the closed dropdown was only CSS-hidden, and
non-matching options were only `display:none`), so every weapon-name combobox on the page held
hundreds of hidden nodes permanently. Now:
- option `<div>`s are rendered only while the dropdown is actually open (`@if (!_comboBoxItemsHidden)`),
  so closed comboboxes contribute zero option nodes;
- when open, options are **filtered** by the search string (`Options.Where(... Contains ...)`) instead
  of rendered-and-hidden.
Filtering semantics are identical (same `Contains` predicate; empty search still shows all), the
selected-item `active` styling is preserved, and selection/focus/suppress-blur behaviour is unchanged.

### Per-keystroke `@bind`/`@oninput` SignalR round-trips — no change needed
- `FormComboBox` uses `@oninput="AdjustOptionList"` for live filtering. The per-keystroke round-trip
  is inherent to Blazor Server (the input event must reach the server to filter); removing it would
  require client-side JS filtering or debouncing — out of scope for this UI.
- `FormText` uses plain `@bind` (default `onchange`), so it commits on blur/Enter, **not** per
  keystroke; its `InvalidOptionGenerator()` runs once per commit (finishing a unit-name edit) — not
  a hot path.
- `AdjustOptionList`'s `Options.Where(o => o.Key.Equals(...)).ToList()` only collects exact matches
  (~1 element), so its allocation is negligible; the genuine cost (the combobox's permanent
  hidden-option DOM) was already removed by the dropdown open-gating + filtering fix above.
No code change made.

### Global event subscriptions in granular components — partly already fixed, one redundant subscription removed
The review flagged four granular components subscribing to global controller events and re-rendering
on unrelated changes. Re-checked each against the current code:
- **`FormWeaponBay` — already fixed.** It no longer subscribes to `OnPlayerUnitListUpdated` (or any
  controller event); it only forwards an `OnUpdated` `EventCallback` upward. No per-bay re-render on
  unit-list changes. ✅
- **`FormFiringSolution:74` and `FormDamageInstance:86` → `OnGameUnitListUpdated` — legitimate, kept.**
  Both render target-unit dropdowns whose contents depend on the live unit list, so they genuinely
  must refresh when units are added/removed/renamed.
- **`FormDamageReports:36-37` → `OnDamageReportsUpdated` / `OnPlayerOptionsUpdated` — legitimate, kept.**
  This is the list container; it rebuilds the filtered turn→reports view on those events.
- **`FormDamageReport:187` → `OnDamageReportsUpdated` — redundant, removed.** An individual report
  card's content depends only on its `DamageReport` parameter (immutable once created) plus local
  `_attackLogVisible` / `_logLineVisibility` toggles — none of which change because a *new* report
  arrives. Its parent chain (`FormDamageReports` → `FormDamageReportContainer` → card) already
  re-renders and cascades on the exact same `OnDamageReportsUpdated` event, so every existing card was
  re-rendering **twice** per new report. Removed the subscription (and the now-empty `IDisposable`
  implementation / `Dispose`). The card still re-renders via the parent cascade; `Delete()` still works
  (it calls `NotifyDamageReportsChanged()`, which the parent container handles). Build green, 12/0.

### `OnInitialized` allocations under key churn — memoized the pick-bracket lists; rest are stable-key
The review flagged four leaf components whose `OnInitialized` does allocation/scan work that the
timestamp `@key` churn was assumed to re-run constantly. Re-checked each against the actual keys:

- **`FormNumberPickerDisplayOnly.OnInitialized` → `BracketCreatorDelegate()` (line 137) — real recurring
  allocation, fixed.** This is the only one of the four with genuinely *value-based* keys: in
  `FormUnitEntry` it is keyed on `@UnitEntry.Consumables.Heat`, `@UnitEntry.Consumables.Penalty` and
  `{UnitEntry.Id}_AmmoUsage_{key}_{value}` (`FormUnitEntry.razor:150,156,194`), so every heat/penalty/
  ammo change recreates the component and re-runs `OnInitialized`, which called
  `BracketCreatorDelegate()` → `MakeSimplePickBrackets` and freshly allocated a `List<PickBracket>` of
  up to ~100 items (Sinks 0-60, Ammo 0-100) each time. The bracket lists are **deterministic and
  read-only** (consumers — `FormNumber`, `FormNumberPicker`, `FormNumberPickerDisplayOnly` — only read
  `.Min`/`.Max` and iterate; none mutate). Fix: memoized `MakeSimplePickBrackets` behind a static
  `ConcurrentDictionary<(begin,interval,end), List<PickBracket>>` (`CommonData.cs`), so every
  parameterless `FormPickBrackets*` factory now returns a shared cached instance and the per-change
  rebuild collapses to a dictionary lookup. Thread-safe (static, shared across circuits). Build green.

- **`FormPickSet.OnInitialized` (line 91-92), `FormSelect.OnInitialized` (line 54-56),
  `FormComboBox.OnInitialized` (line 176-179) — not actually "constant", left as-is.** All three are
  bound with **stable** `@key`s tied to identity, not value: `FormPickSet`/`FormSelect` use
  `{UnitEntry.Id}_Features` / `{UnitEntry.Id}_Type` / `{WeaponEntry.Id}_Ammo`, and `FormComboBox` uses
  `{WeaponEntry.Id}_WeaponName` (`FormUnitEntry.razor:48,84`, `FormWeaponEntry.razor:51,57`). These
  recreate only when the unit/weapon identity changes or the whole Dashboard subtree is rebuilt on a
  turn roll (`FormGameState` keyed on `TurnTimeStamp`, Index.razor:42) — infrequent, not per-push. Their
  `OnInitialized` work (`ToDictionary`/`ToHashSet`, double `Any`+`FirstOrDefault` scans over small
  option dicts) therefore does not run constantly. Reworking the value-cache/refresh model to drop these
  was already triaged out as high-risk in the "Index-level editable `@key`s" entry above, so the
  micro-scan reductions were not pursued for no measurable gain.

### `@foreach` without `@key` — added stable keys to the two attack-log-entry loops
The review flagged two key-less `@foreach` loops rendering editable leaf components:
- `FormOptions.razor:53` — `FormToggle` per `PlayerOptions.AttackLogEntryVisibility` entry.
- `FormDamageReport.razor:152` — `FormCheckbox` per `_logLineVisibility` entry.

Both collections are derived from the full `AttackLogEntryType` enum set
(`PlayerOptions.cs:19` seeds `AttackLogEntryVisibility` via `Enum.GetValues<AttackLogEntryType>()...`,
and `_logLineVisibility` is a `ToDictionary` copy of it — `UserStateController.cs:421`), so their
membership, order and count are stable; positional diffing was not actually misbehaving. The risk is
latent: `FormToggle`/`FormCheckbox` cache `Checked` into `_checked` in `OnInitialized` and never
re-sync on parameter change, so if the list order ever shifted, positional matching would leave a
toggle showing the wrong entry's state.

Fix: added `@key="@attackLogEntryType.Key"` (the enum value) to both loops, binding each
toggle/checkbox to its own entry. Because the keys are stable the components are still never recreated
(no behaviour change, no extra render), but identity is now explicit and order-independent, matching
the codebase convention (e.g. `FormRadio @key=` in the same `FormOptions` file). Build green, 0 errors.

### Tab switching rebuilds tab content every time — deliberately left as-is
The review flagged that `ContainerTab.razor:3` renders `ChildContent` only while its tab is the
active selection (`@if (TabIdentity == TabSelection && Enabled)`), so switching tabs tears down the
previous tab's subtree and builds the newly-selected one from scratch each time.

Decided **not to** change this — the behaviour is intentional:
- Only one tab's content exists in the DOM at a time, which is the whole point of the construct. Keeping
  all five tab subtrees (`FormData`, `FormGameState`, `ComponentGameState`, `FormDamageReports`,
  `FormOptions`) permanently realized and merely CSS-hidden would *increase* steady-state DOM size and
  keep every hidden form subscribed/diffing on every server push — the opposite of what the ARM-perf
  work is trying to achieve.
- Tab switches are explicit, infrequent, user-initiated actions (not driven by inbound packets), so a
  one-time rebuild on switch is acceptable and not a hot path.
- The per-push recreation concern the bullet tied to the timestamp `@key`s is a separate issue and was
  already addressed: the All Units key is stable (`{IsConnectedToGame}`), and the remaining
  Dashboard/DamageReports/Options keys churn only on infrequent events (turn roll, new damage report,
  option change) — see the "Index-level editable `@key`s" and "Index timestamp keys" entries above.

No code change made.

### Index timestamp keys recreating modals/tabs — already resolved, verified
The review restated item "1d" (Index `@key`s on timestamps tearing down modals/tabs on every state
change). Re-verified against current code; no further change needed:
- **The real per-push offender was the `FormTools` `@key="GameState.TimeStamp"`**, already removed
  earlier this session. `FormTools` (Kick / Move-Unit modals, local `_show…` flags) now diffs in place
  on every push, so those admin modals no longer close when a packet arrives.
- **FormUnitEntry's Save/Load/Delete modals** live inside `FormGameState` (Dashboard) and are keyed by
  the stable `{unitEntry.Id}`. `FormGameState`'s own key (Index.razor:42) is
  `{IsConnectedToGame}_{GameState.TurnTimeStamp}` — **`TurnTimeStamp`, not `TimeStamp`** — so it only
  recreates on a turn roll, not on routine pushes (ready toggles, edits, target numbers). Those modals
  therefore survive normal state changes.
- The remaining recreation (FormGameState rebuilding on turn resolution) is the deliberate behaviour
  kept in the 1d triage: turn rolls are infrequent, major events where a full Dashboard refresh is
  acceptable. The DamageReports / Options tab keys (`DamageReportContainer.TimeStamp`,
  `PlayerOptions/GameOptions.TimeStamp`) likewise churn only on their own infrequent events.
No code change made.

---

## B2. FormWeaponEntry / FormTextArea — DOM and binding fixes — DONE

**FormWeaponEntry.razor** (B2 "each weapon row has 7 grid columns each wrapped in its own div"):
Reviewed. The component is already flat — it emits 7 top-level grid cells directly (grid-column 1-7)
with no `componentcontainer`/`componentrow` wrappers; the parent supplies the grid. The only remaining
nesting is the `resolver_div_inputwrapper` inside the generic leaf pickers
(FormNumberPickerDisplayOnly/FormComboBox/FormSelect). That wrapper is functional — it groups the
display/edit element with the dropdown button and anchors the absolutely-positioned picker list — so it
cannot be removed without breaking the picker. No change made; considered adequately flat.

**FormTextArea.razor:2** (B2 "echoes @TextInternal inside the textarea body in addition to binding"):
Fixed. Removed the `@TextInternal` body content from the `<textarea>` so the value is driven solely by
`@bind="TextInternal"`. The body echo (plus its indentation/newlines) duplicated the bound value, polluted
the initial textarea content with whitespace, and could desync from the bound value — a known Blazor
anti-pattern. Build clean (0 errors).

## Edit-propagation regression (5aa0df6) — ROOT CAUSE + FIX — DONE

Root cause (found by manual Redis-queue debugging): `PlayerState` is a computed property over
`GameState.Players[PlayerName]`, so every server push rebuilds the whole object graph (new GameState →
new PlayerState → new UnitEntry instances). The editing components (`FormUnitEntry`) cache `UnitEntry`
as a `[Parameter]`, only refreshed when the parent `FormGameState` re-renders. Commit 5aa0df6 removed the
unconditional `OnPlayerUnitListUpdated` notify from the GameState setter, so `FormGameState` stopped
re-rendering on pushes and stayed bound to orphaned objects — edits mutated stale copies and were lost
when the (fresh) PlayerState was sent.

Fix: `FormGameState` re-subscribed to a per-push event (`OnGameStateUpdated`) so it re-binds its child
components to the freshly deserialized instances. Complementary optimization in the `UserStateController.GameState`
setter: the unit-list rebuild + `NotifyGameUnitListUpdated`/`NotifyGameStateUpdated` calls were moved
INSIDE the timestamp-acceptance `if`, so the editing tree only re-renders when a push actually replaces
the held state (stale/duplicate pushes leave existing references intact and need no re-bind). Build clean.

## B2. FormPaperDoll duplicated damage sum — DONE

`FormPaperDoll.razor` recomputed `location.Value.SelectMany(l => l.Value).Sum()` on line 63 even though
line 52 already stores it in `damageInLocation`. Replaced the line-63 recomputation with the existing
`damageInLocation` local. Pure dedup, no behavior change. Build clean.

## B2. FormUnitEntry flatten — ASSESSED, NO CHANGE NEEDED

`FormUnitEntry.razor` already conforms to the same flattened structure adopted (and visually tested) in
`ComponentUnit.razor`: a `resolver_div_componentgroup` (2-column `inline-grid`) whose `resolver_div_componentrow`
children use `grid-template-columns: subgrid` to align label + value cells. That is one wrapper row per field —
the intended target shape.

A deeper "CSS-grid auto-flow" flatten (dropping the per-field `componentrow` so label/value cells auto-place
into the two grid columns) was considered and declined as not worth the risk:
- The shared rule `.resolver_div_componentgroup > *:not(.resolver_div_componentrow) { grid-column: 1 / -1; }`
  forces bare children to span full width, so auto-flow needs either shared CSS changes (affecting every form
  that uses componentgroup) or a separate isolated grid class.
- ~8 fields are conditionally hidden via `HideElement` on the row; without the row each field's two cells must
  be hidden together (via `@if`) to avoid column misalignment.
- Net saving is ~1 `<div>` per field against meaningful risk to the primary editing surface.

Decision: leave FormUnitEntry as-is; it is already adequately flat and consistent with ComponentUnit.

## B2. 10 near-identical FormPaperDoll* variants — SHARED COMPONENT EXTRACTED

The 10 `FormPaperDoll*` variant components each repeated the same interactive-shape boilerplate per
location: `fill="@FormPaperDoll.GetDamageColor(Location.X)" stroke="black" stroke-width="100"` plus a long
`onmousemove="ShowTooltip(...GetDamageText(Location.X)...)" onmouseout="HideTooltip(...)"` tooltip handler —
duplicated ~70 times across the files and individually wrapped in a redundant `<g id/class>` element.

Extracted `FormPaperDollRegion.razor`, a shared sub-component that renders one interactive shape
(`<polygon>` or, via the `Path` parameter, `<path>`) with the fill/stroke/tooltip wiring centralised
(`RoundJoin` parameter for `stroke-linejoin="round"`). Each variant now expresses its geometry as a flat list
of `<FormPaperDollRegion ... Points="..." />` entries — "polygon list as data" — instead of hand-repeated
markup, and the `<g>` wrappers (confirmed unused by CSS/JS) were dropped, reducing SVG node count per the
B2 nesting goal.

Converted: AerospaceFighter, AerospaceCapital, AerospaceDropshipAerodyne, AerospaceDropshipSpheroid,
Building (interactive `<path>` + `<polygon>`), Mech (incl. the three conditional rear-torso regions),
Vehicle, VehicleVtol, BattleArmor (per-trooper loop). Static decorations (the `#A0A0A0` internals, the
hidden Vehicle turret-internal, and the Spheroid black `<rect>` docking tabs) were left inline in their
original document order, so z-order is preserved exactly.

Left unchanged: `FormPaperDollTrooper.razor` — its fill is index-based (`#FF0000`/`#E0E0E0` by trooper index),
not `GetDamageColor`, so it does not fit the damage-colour region model.

Note: the `FormPaperDollMech` inline-JS → CSS `:hover` item (separate B2 bullet) is still open; this change
only de-duplicates the variants and removes the `<g>` nesting, it does not alter the tooltip mechanism.

Build: `dotnet build` of BlazorServer succeeds (0 errors, 12 pre-existing warnings). Visual verification of
the rendered paperdolls is recommended (the only non-obvious risk is SVG-namespace rendering of the
`<polygon>`/`<path>` through the component boundary — this is the standard Blazor SVG-child-component pattern
and the namespace is resolved at DOM insertion by the physical parent `<svg>`).

## B2. FormPaperDoll SVG inline-JS event attributes — DELEGATED HANDLERS

Each interactive paper-doll shape carried inline `onmousemove="ShowTooltip(...)"` /
`onmouseout="HideTooltip(...)"` attributes, forcing the browser to parse a handler string for every
`<polygon>`/`<path>` across dozens of regions per damage report. CSS `:hover` already supplies the highlight
recolour, but the tooltip needs cursor-following HTML content, so it cannot be pure CSS.

Replaced the per-shape inline handlers with a single set of document-level delegated listeners
(`wwwroot/js/Resolver.js`, appended IIFE). Shapes now declare `data-tooltip-id="resolver_tooltip_paperdoll"`
and `data-tooltip-content="@FormPaperDoll.GetDamageText(Location)"`; the delegated `mouseover`/`mousemove`/
`mouseout` listeners match `evt.target.closest("[data-tooltip-id]")`, populate the referenced tooltip div,
and follow the cursor. `mousemove` re-reads `data-tooltip-content` and refreshes only when it changes,
preserving the original live-update behaviour; `mouseout` uses a `relatedTarget`-contains guard; null guards
throughout. Applied to `FormPaperDollRegion.razor` (both polygon and path branches) and
`FormPaperDollTrooper.razor`.

Intentionally left as-is: the global `ShowTooltip`/`HideTooltip` functions remain, because
`FormWeaponEntry.razor` is a second consumer (via `resolver_tooltip_targetnumber` in `FormGameState.razor`)
and does not carry `data-tooltip-id`, so the delegated listeners ignore it.

Security hardening (behaviour change beyond pure refactor): `GetDamageText` previously wrapped its output in
single quotes for the inline-JS string literal; those are removed for the data-attribute approach, and the
user-entered firing-unit name is now `System.Net.WebUtility.HtmlEncode(...)`-encoded. The data-attribute
path is strictly safer than the old inline JS (a name containing `'` could previously break out of the
handler string), and the encoding renders any markup in a unit name as inert literal text.

Build: `dotnet build` of BlazorServer succeeds (0 errors, 12 pre-existing warnings). Visual verification of
paper-doll tooltips is recommended (hover shows location + damage, follows the cursor, hides on leave).

### Follow-up finalization (tooltip mechanism unified)

After the initial paper-doll conversion, the tooltip delegation was completed across the whole app:

- `FormPaperDollRegion.razor` gained an optional `Fill` parameter (falls back to `GetDamageColor(Location)` when null). `FormPaperDollTrooper.razor` now routes through `FormPaperDollRegion` too, passing its index-based literal fill (`#FF0000` killed / `#E0E0E0` alive); the unused `<g id="Trooper">` wrapper was dropped. Every paper-doll variant now goes through the shared region component.
- `FormWeaponEntry.razor:18` was migrated from inline `onmousemove="ShowTooltip(...)"`/`onmouseout="HideTooltip(...)"` to `data-tooltip-id="resolver_tooltip_targetnumber"` + `data-tooltip-content`, so it uses the same delegated listeners. The delegated `mousemove` re-reads `data-tooltip-content` each move, preserving live target-number updates.
- The now-unused global `ShowTooltip`/`HideTooltip` functions were deleted from `Resolver.js`; a single document-level delegated IIFE is the sole tooltip mechanism (resolves the "multiple tooltip implementations / code duplication" item — two display divs remain, but one shared implementation drives both).
- `Pages/_Host.cshtml:17` — added `asp-append-version="true"` to the `Resolver.js` `<script>` tag (matching `Resolver.css`). Without it, a cached `Resolver.js` is served after a deploy; this previously caused all tooltips to vanish until a hard refresh once the razor stopped emitting inline handlers.

Still open (separate micro-perf note, not addressed here): the `data-tooltip-content` string is recomputed every render for paper-doll regions and `FormWeaponEntry`. This is inherent to the data-attribute approach and is left as a future optimisation.

## B2. Repeated modal markup — SHARED ContainerModal COMPONENT EXTRACTED

The modal dialog skeleton was hand-duplicated six times across three components, each ~20 lines of identical
`resolver_modal_background > resolver_modal > header(title + × close) > body > footer(primary + Cancel)`
markup:
- `FormUnitEntry.razor` — Delete Unit, Save Unit, Load Unit
- `FormTools.razor` — Kick Player, Move Unit to Player
- `FormServer.razor` — Password required

Extracted `Shared/Generic/ContainerModal.razor` (following the existing `Container*` convention for structural
wrapper components). It renders the full background/header/body/footer chrome and exposes:
- `Title` (string) — header text
- `ChildContent` (RenderFragment) — modal body
- `PrimaryText` (string, default `"Submit"`) — primary button label
- `OnPrimary` (EventCallback) — primary button action
- `OnClose` (EventCallback) — invoked by both the header `×` and the footer `Cancel` button

All six call sites were converted to `<ContainerModal ...>body</ContainerModal>`, removing ~110 lines of
duplicated markup. The Delete modal passes `PrimaryText="Delete"`; the rest use the `"Submit"` default.

Dropped the stray `id="passwordModal"` that every modal carried — it is a copy-paste leftover (the same id
appeared on all six modals, which is invalid duplicate-id HTML) and is not referenced by any CSS or JS
(`wwwroot` grep clean). `role="dialog"` is preserved on the shared component.

Build: `dotnet build` of BlazorServer succeeds (0 errors, 12 pre-existing warnings). Visual verification
recommended: open each dialog (unit Delete/Save/Load, Tools Kick/Move Unit, Server password-protected join)
and confirm the header title, body, primary action, and both close paths (× and Cancel) behave as before.
