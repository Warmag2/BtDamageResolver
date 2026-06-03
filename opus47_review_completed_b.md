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

## B2. LabelValue component (label/value pattern repeated 80+ times) — ASSESSED, NO CHANGE

Considered extracting the `componentrow > componentcell + componentcell` label/value pattern into a shared
`<LabelValue>` component. Decided against it — it provides no performance benefit and was judged not to
improve readability.

Reasoning (Blazor Server specifics):
- **No DOM reduction.** `<LabelValue>` would render the identical `componentrow > cell + cell` markup, so
  node count is unchanged. The B2 "excessive nesting" concern is about browser layout/style-recalc scaling
  with node count; this extraction does not reduce nodes and therefore yields nothing there.
- **Slightly higher render cost, not lower.** Each child component is a render boundary with its own
  `ComponentState`, lifecycle, and per-render parameter-diffing. Replacing 80+ inline fragments with 80+
  component instances adds bookkeeping/allocations; inline markup is part of the parent render tree and is
  cheaper.
- **The only win would be a `ShouldRender()` skip** on unchanged rows, but it does not pay off here: these
  rows live inside components that already churn via `@key`; a row whose value changed re-renders anyway; and
  Blazor still parameter-diffs each child to decide to skip, which for a 3-node fragment costs more than it
  saves. It would also require capturing previous parameter values (added complexity for no measurable gain).

The dominant performance levers in this app are elsewhere (whole-`GameState` serialization + `@key` subtree
invalidation, already tracked separately), not trivial label/value pairs. No code change made.

## B2. MainLayout wrapping div — ALREADY RESOLVED (no action needed)

The review flagged `MainLayout` adding an extra `<div class="resolver_content">` wrapper. This no longer
exists: `Shared/MainLayout.razor` is now just `@inherits LayoutComponentBase` + `@Body`, and the
`resolver_content` class is absent from the entire project (grep of `wwwroot` and all components is clean).
The wrapper was removed in earlier work, so there is nothing left to do. No code change made.

## B2. ContainerReorderableList wrapper divs — ASSESSED, NO CHANGE (functional)

The review flagged `ContainerReorderableList` wrapping every item in an extra `reorderableitem` div plus, when
`ShowDragHandle` is set, a `componentrow > draghandle + componentcell`. Assessed against the current
implementation (which has since been reworked with a sentinel drop-target and drag-over states):

- `resolver_div_reorderableitem` is **functional, not gratuitous** — it is the draggable/drop-target element
  carrying all the `@ondragstart/@ondragenter/@ondragend/@ondrop` handlers and the `--dropover` visual state.
  It cannot be removed without losing drag-and-drop.
- The non-handle path renders `@ItemTemplate` directly inside that single wrapper — already minimal.
- The `componentrow > draghandle + cell` only appears in `ShowDragHandle` mode and exists to lay out the ⠿
  drag handle beside the content; the handle is the drag trigger in that mode, so it is required for the UX.

The only further reduction would be merging the grid-row layout onto `resolver_div_reorderableitem` itself to
drop one div in handle mode — but that element already juggles `draggable`, the drag handlers, the
`--dropover` modifier, `ItemClass`, and `ItemClassSelector`; overlaying `display:grid` row semantics risks the
drag/drop visuals. Not worth the regression risk for one saved div in one mode.

Used in 3 places (FormGameState unit list + FormUnitEntry weapon bays with handles; FormWeaponBay weapons
without). The higher-value concern for this component — per-item drag handlers each causing a SignalR
round-trip during drag — is tracked separately under B3 and is unrelated to DOM nesting. No code change made.

## B2. Wrapper-div proliferation (general) — COVERED BY SPECIFIC ITEMS

The umbrella concern (`resolver_div_componentcontainer > resolver_div_componentrow > resolver_div_componentcell`
nesting 5-7 deep) was addressed by working through every concrete instance under B2 rather than a broad
sweep, which the subgrid CSS system actively resists (`.resolver_div_componentgroup > *:not(.resolver_div_componentrow)`
forces full-width, blocking a naive auto-flow flatten without shared CSS changes).

Concrete instances resolved/assessed:
- ComponentUnit — flattened to `componentgroup > componentrow > cell` (committed and tested).
- FormWeaponEntry — already emits direct grid cells (no container/row wrappers).
- FormUnitEntry — assessed already conforming to the tested ComponentUnit pattern; deeper flatten declined.
- 10 FormPaperDoll* variants — consolidated into the shared `FormPaperDollRegion`, dropping unused `<g>` wrappers.
- 6 modal blocks — consolidated into `ContainerModal`.
- LabelValue extraction — assessed; no DOM reduction and a net render-cost increase, so declined.
- MainLayout `resolver_content` wrapper — already removed in earlier work.
- ContainerReorderableList wrappers — functional (drag/drop + handle layout), not removable.

No further broad DOM surgery is warranted: the remaining nesting is the deliberate subgrid layout system, and
flattening it wholesale would require shared CSS changes with broad regression risk for negligible node savings.
Closed as covered.

## B3. Per-circuit Redis subscription churn / leak — FIXED

`ResolverCommunicator` opened a fresh `ClientToServerCommunicator` on every Connect (`Reset()` calls `new`,
and the base `RedisCommunicator` constructor immediately `Start()`s — opening a `ConnectionMultiplexer` and a
Redis subscription). The old communicator was never released: `Reset()` overwrote the field without disposing
the previous instance, and `Disconnect()` simply set it to `null`. Each connect/disconnect cycle therefore
leaked a live Redis connection plus subscription that kept receiving and processing messages. Even the
class-level `Dispose` only called `Stop()` (which `CloseAsync()`es but does not dispose the multiplexer).

Fix (`Communication/ResolverCommunicator.cs`): added a null-safe `TeardownCommunicator()` helper that
`Dispose()`s the current communicator (the base `Dispose` unsubscribes and disposes the multiplexer) and
nulls the field. It is now called:
- in `Reset()` before constructing the replacement communicator,
- in `Disconnect()` (replacing the bare `_clientToServerCommunicator = null;`), after the disconnect request is sent,
- in `Dispose(bool)` (replacing the previous `Stop()` call, which left the multiplexer undisposed).

`Dispose()` is used rather than `Stop()` because the old communicator is never restarted (a new instance is
always created), and the base `Dispose` is null-guarded so it is safe to call even if already torn down.

Build: `dotnet build` of BlazorServer succeeds (0 errors, 12 pre-existing warnings).

## B3. DisconnectedCircuitMaxRetained raised to 256 (+ deferred CPU fix documented) — DONE

Set `options.DisconnectedCircuitMaxRetained = 256;` in the `Configure<CircuitOptions>` block
(`Startup.cs`), alongside the existing `DisconnectedCircuitRetentionPeriod = 1 hour` (kept
deliberately — phone users idle legitimately mid-game during movement/fire decisions).

Key finding documented in the code comment: because `ResolverCommunicator` / `ClientToServerCommunicator`
/ `HubConnection` / `UserStateController` are **scoped** (`Startup.cs:103-107`), a retained disconnected
circuit keeps its Redis subscription alive and continues deserializing + forwarding its game's messages to a
dead connection. So this cap bounds background **CPU**, not just memory. User accepted the cost (will scale
hardware if user count grows); a full reload (Ctrl-F5) sidesteps it by creating a fresh circuit that re-requests
game state.

The proper "make retention CPU-free" fix (a `CircuitHandler` that pauses the Redis subscription on
disconnect and resumes + refetches state on reconnect) is deferred and captured as a standalone task in
`task_disconnected_circuit_redis.md` — it is regression-prone (stale-state-on-reconnect) and overlaps the
double-hop architecture item, so it will be attempted later.

Build: `dotnet build` of BlazorServer succeeds (0 errors, 12 pre-existing warnings).

## B3. Drag/drop dragenter SignalR flooding — FIXED

`ContainerReorderableList` drove its drag visuals (`--dragging` on the container, `--dropover` on the
hovered item/sentinel) from **server** state (`_draggedIndex`/`_dragOverIndex`/`_dragOverSentinel`).
Every `@ondragenter` was therefore a SignalR roundtrip plus a full-list re-render to move the highlight, and
because `dragenter` bubbles from the child elements inside each item it re-fired many times for the same
index. With 100+ items (e.g. weapon lists) this floods the circuit on ARM.

Fix: the drag *visuals* are now handled entirely client-side by a delegated, capture-phase IIFE in
`wwwroot/js/Resolver.js` (same delegation pattern as the tooltip handler). It tracks a single
`activeContainer` (so nested reorderable lists — the unit list contains per-unit weapon lists — never clobber
each other), adds `--dragging` on `dragstart`, moves the scoped `--dropover` on `dragenter`, and clears
everything on `drop`/`dragend`. Listeners use the **capture** phase so the existing
`@ondragstart/@ondragend/@ondrop :stopPropagation` modifiers (needed for the nested-list drop logic) cannot
block them.

`ContainerReorderableList.razor` now keeps only the data operations on the server: `@ondragstart` →
`StartDrag` (records source index), `@ondrop` → `Drop` (reorders using server `_draggedIndex` + captured
target index), `@ondragend` → `CancelDrag`. The `@ondragenter`/`@ondragleave` handlers, the
`--dragging`/`--dropover` class bindings, and the `_dragOverIndex`/`_dragOverSentinel` fields +
`SetDragOver`/`SetDragOverSentinel` methods were removed. Per whole drag, roundtrips drop from
O(items×children crossed) to exactly dragstart + drop (+ dragend). Reorder logic is unchanged.

Build: `dotnet build` of BlazorServer succeeds (0 errors, 12 pre-existing warnings). Visual drag/drop
behavior to be confirmed by the user in-browser.

## B3. Synchronous Unpack<> on circuit thread — ASSESSED, no change

Claim was that `_dataHelper.Unpack<>` (LZMA/Brotli decompress + JSON deserialize) in the
`_hubConnection.On<byte[]>` handlers (`Pages/Index.razor:106,112,120,126,132,138,144`) blocks the Blazor
circuit and freezes the UI on ARM.

Finding: the `HubConnection` is built with a plain `HubConnectionBuilder` (`Startup.cs:113-120`) with no
captured `SynchronizationContext`, so these `On` callbacks run on the SignalR **client receive loop**
(a thread-pool thread), not the Blazor render Dispatcher. Each handler does the `Unpack` there, mutates the
scoped `UserStateController`, then calls `InvokeStateChange()` which marshals only `StateHasChanged` onto
the Dispatcher. So decompress/deserialize does not directly block rendering; the two are sequential, not
contending.

Decision: do **not** wrap `Unpack` in `Task.Run`. It would not reduce CPU (the work is unavoidable and
identical), and on the few-core ARM target parallelizing CPU-bound work yields no throughput gain while risking
contention with rendering (which currently does not overlap the deserialize). The genuine CPU levers are
payload size/frequency (the "large SignalR payloads" item) and the double-hop removal; this concern is folded
into those.

Noted out-of-scope: the handlers mutate scoped `UserStateController` from the receive-loop thread (off
Dispatcher) — a pre-existing latent thread-safety smell, not addressed here.

## B3. Per-render tooltip string recomputation — FIXED (FormWeaponEntry) / ASSESSED (paper dolls)

`FormWeaponEntry` computed its `data-tooltip-content` via `GetTargetNumberText` on **every** render
(an O(n^2) `Aggregate` string build over the calculation-log lines). This component re-renders on every
weapon toggle/edit, yet the tooltip only changes when the unit's target-number data changes.

Fix (`Shared/FormWeaponEntry.razor`): memoize the tooltip string on the existing `_targetNumberTimeStamp`
(the same key that already gates `CheckRefresh`/re-render). New `GetCachedTooltipContent(AttackLog)` recomputes
only when that timestamp changes; otherwise it returns the cached string. The cache is a single `string` field
on the component instance (`@key`'d by `WeaponEntry.Id`), so it is GC'd with the component and holds at most
one tooltip per on-screen weapon entry — strictly less allocation/GC churn than rebuilding every render, no leak.
Also replaced the O(n^2) `Aggregate` concatenation with an O(n) `string.Concat(... Select(...))`.

Paper-doll regions (`FormPaperDollRegion` → `FormPaperDoll.GetDamageText(Location)`) were assessed and left
as-is: they operate on immutable damage-report data and only render when a new damage report arrives (the report
views are `@key`'d by `DamageReportContainer.TimeStamp`), not on interactive churn, so eager per-render
computation is acceptable. Their per-region LINQ is tracked separately under B4.

Build: `dotnet build` of BlazorServer succeeds (0 errors, 12 pre-existing warnings).

## B3. Large SignalR payloads — partially optimized; protocol refactor declined

Two sub-concerns:

1. **Whole-GameState-per-update protocol (declined, by design).** Sending the entire `GameState` on every
   update (rather than per-unit deltas) is a deliberate design choice: it is eventually consistent and
   self-healing — a client that misses an update is corrected by the next one with no desync, and it cleanly
   supports operations like unit reordering. Per-unit deltas would require a full client/server CRUD protocol
   (AddOrUpdate/Delete per unit) and introduce a silent-desync failure mode when a listener misses a message.
   Decision: keep whole-GameState; do not refactor.

2. **Redundant per-recipient envelope serialization (FIXED).** `GameActor.DistributeGameStateToPlayers` already
   builds the `GameState` once and `RedisServerToClientCommunicator.SendToMany` already **compresses** it once
   (`DataHelper.Pack` outside the loop). However, the old loop then called `SendSingle` →
   `SendEnvelope` per client, and each call re-ran `JsonSerializer.Serialize(envelope)` — which base64-encodes
   the entire already-compressed payload into a fresh string — once per recipient. Since the envelope is identical
   for all recipients, this was N× redundant.

   Fix (`Api/ClientInterface/Communicators/RedisCommunicator.cs` + `RedisServerToClientCommunicator.cs`):
   split `SendEnvelope` into `PublishToChannel(target, serializedEnvelope)` and added
   `SendEnvelopeToMany(targets, envelope)` which serializes the envelope **once** and publishes the same payload
   string to each channel. `SendToMany` now uses it. Wire format and per-client delivery are unchanged (each
   client still receives the identical envelope JSON on its own Redis channel); only the redundant repeat
   serialization is removed. CPU/allocation saved scales with (players − 1) per broadcast.

The remaining client-side cost noted in the original bullet (`@key`=timestamp invalidating render subtrees) is
intertwined with the known @key-churn behavior that prior regressions hinged on, and is out of scope here; the
per-region paperdoll LINQ is tracked under B4.

Build: server (Silo) builds (0 errors). BlazorServer unaffected.

---

## B3. Double-hop architecture removed (ClientHub / self-looping HubConnection eliminated)

**Problem.** Inbound server→browser events took a redundant in-process hop. The Blazor server's
`ClientToServerCommunicator` (a Redis subscriber, per-circuit) received each event off Redis and then called
`_hubConnection.SendAsync(EventName, connectionId, data)` over a SignalR **client** `HubConnection` pointed at the
Blazor server's *own* `/ClientHub`. `ClientHub` simply relayed it back (`Clients.Client(connectionId).SendAsync`) to
the same `HubConnection`'s `.On<byte[]>` handlers in `Index.razor`. The whole `HubConnection`/`ClientHub` pair was an
in-process message bus implemented as a self-looping WebSocket; `connectionId` was used solely for this self-routing
(the server addresses circuits by playerId via the Redis channel, never by connectionId).

**Fix.** Replaced the self-loop with a direct in-process dispatcher.
- New `Communication/ClientMessageDispatcher.cs` (scoped per circuit): `On(name, Func<byte[],Task>)` / `Off(name)` /
  `DispatchAsync(name, data)`. A `SemaphoreSlim(1,1)` gate serializes handler execution.
- `ClientToServerCommunicator`: each `HandleX` now `await _dispatcher.DispatchAsync(EventNames.X, data)` instead of
  `_hubConnection.SendAsync(...)`; `connectionId` dropped.
- `ResolverCommunicator`: takes the dispatcher instead of `HubConnection`; `SendErrorMessage` now packs a
  `ClientErrorEvent` and dispatches `EventNames.ErrorMessage` (previously it called a non-existent `ReceiveErrorMessage`
  hub method, so local error messages were silently lost — this is now fixed and they reach the existing handler).
- `Index.razor`: subscribes via `_dispatcher.On(...)` instead of `_hubConnection.On<byte[]>(...)`; removed
  `_hubConnection.StartAsync()`; `DisposeAsync` now `Off`s each event (no hub to dispose).
- `Startup.cs`: removed `AddScoped<ClientHub>()`, the `AddScoped<HubConnection>` factory, and
  `MapHub<ClientHub>("/ClientHub")`; added `AddScoped<ClientMessageDispatcher>()`; rewired the `ResolverCommunicator`
  factory to inject the dispatcher. Deleted `Hubs/ClientHub.cs`.

**Why the serialization gate.** The client subscribes to two independent Redis queues — the player channel and the
common `ClientStreamAddress` (used by `SendToAll`) — whose `OnMessage` loops can run concurrently. The old design
funnelled both through the single `HubConnection` receive loop, so handlers (which mutate the scoped
`UserStateController`) ran one-at-a-time. The `SemaphoreSlim` gate preserves exactly that ordering, so there is no new
concurrency hazard. Handlers continue to run off the Blazor render dispatcher (as before), so no decompression/Unpack
CPU is moved onto the render path — consistent with the CPU-over-memory priority.

**Eliminated per inbound event:** one `HubConnection.SendAsync` WebSocket frame out + the `ClientHub` relay +
one inbound WebSocket frame back (plus the SignalR client connection, its reconnect machinery, and the `/ClientHub`
endpoint entirely).

**Behavior change to note:** local (client-originated) error messages now actually display (the old path was a
silent no-op against a missing hub method); `ClientErrorEvent(errorMessage)` leaves `InvalidUnitIds` null, identical
to the server's own error path, which the handler already tolerates.

Build: BlazorServer builds (0 errors; pre-existing 12 warnings unchanged).

## B4 (partial). Per-render allocation/IO hot-path fixes (option-map memoization + saved-unit-names snapshot)

**Scope.** Addresses the option-map allocation and per-render Redis bullets of B4. The remaining B4 items
(damage-report render-path LINQ in `FormPaperDoll`/`FormDamageReport`/`FormDamageReports`, `TranslateDamageToColor`,
`GetTargetNumberUpdateSingleWeapon`/`DamageReportConcernsPlayer` lookups, `UserStateController` UnitList/UpdateUnitList,
`FormGameList` OrderByDescending, `FormRadio` Guid, sync-over-async `Connect`, `BaseFaemiyahComponent.InvokeStateChange`)
remain open in `opus47_review.md`.

**Problem.** Several option-map builders ran in editable-form render paths and allocated a fresh dictionary on every
render:
- `CommonData.FormMapWeaponAmmo(weaponName)` built a new `SortedDictionary` from `DictionaryWeapon[...].Ammo.Keys`
  every render of every `FormWeaponEntry` (the hottest editable path — once per weapon per unit), and `FormWeaponEntry`
  computed it unconditionally in its `@{ }` block even for weapons with no ammo (the value is only consumed inside
  `@if (_commonData.WeaponHasAmmo(...))`).
- `CommonData.FormMapCover(unitType)` / `CommonData.FormMapStance(type)` allocated a new `Dictionary` per call; used as
  `FormRadio` `Options` in `FormFiringSolution` / `FormDamageInstance` / `FormUnitEntry`, i.e. per render per unit.
- `CommonData.GetSavedUnitNames()` was bound inline as `FormComboBox Options` inside the `FormUnitEntry` Load modal
  (`@if (_showModalLoad)`), so every re-render *while the modal was open* issued a Redis `GetAllKeys()` round-trip and
  rebuilt the dictionary, also churning the combo box's `Options` identity.

**Fix.**
- Memoized `FormMapWeaponAmmo`, `FormMapCover`, `FormMapStance` behind `static ConcurrentDictionary` caches (keyed by
  weapon name / `UnitType`), mirroring the existing `SimplePickBracketCache` / `_mapWeaponNames*` pattern. The cached
  maps are immutable game-data projections and are consumed read-only as `Options` (same contract as the already-cached
  `FormMapWeaponName`), so sharing a single instance is safe and additionally stabilizes the `Options` reference passed
  to the form components.
- `FormWeaponEntry`: removed the unconditional `var mapWeaponAmmo = ...` from the `@{ }` block and inlined the now-cheap
  memoized call at the single `FormSelect` use site (only evaluated when the weapon has ammo). `CorrectAmmoForWeapon`
  keeps calling the (now memoized) method.
- `FormUnitEntry`: snapshot `GetSavedUnitNames()` into a `_savedUnitNames` field in `ShowModalLoad()` (when the modal
  opens) and bind the field; the Redis read now happens once per modal open instead of once per render. Confirmed with
  the user that a snapshot at open time is always current enough — loading units is rare (≈4–30× per match).
  (`FormData.razor:77` also calls `GetSavedUnitNames()` but only from the event-driven `RefreshDataList`, not a render
  path, so it was left unchanged.)

Build: BlazorServer builds (0 errors; pre-existing 12 warnings unchanged).

## B1/B4 (partial). `ShouldRender` guard on the paper-doll subtree + cached combined damage report

**Render trigger established.** The damage-report panel (`FormDamageReports` → `FormDamageReportContainer` →
`FormDamageReport` → `FormPaperDoll` → N `FormPaperDollRegion`) lives under `<ContainerTab TabIdentity="DamageReports">`,
which only emits its `ChildContent` while that tab is selected. While it *is* open, `Index` re-renders on every
`GameState`/`PlayerOptions`/`GameOptions`/`GameEntries`/`Error`/`Connection` broadcast (it calls `StateHasChanged` in
those handlers; **not** for `TargetNumbers`). A `GameState` broadcast fires whenever *any* player edits anything, so an
unrelated player's edit cascaded a full re-render down to every `FormPaperDoll`, recomputing per-region damage colours
and tooltip strings (`SelectMany`/`Sum`/`string.Join` per region) plus `TranslateDamageToColor` (decimal arithmetic +
`ToString("X2")`) per region. The `@key` on `FormDamageReports` (`DamageReportContainer.TimeStamp`, ~once/turn) only
governs instance reuse-vs-recreate, **not** whether the retained instance re-renders — so it did not prevent any of this.

We chose a `ShouldRender` guard over memoizing the LINQ: it eliminates the redundant *re-render* entirely (and with it
all the per-region work), which is strictly better than shrinking the cost of a render that should not happen.

**Fix 1 — `FormPaperDoll.razor` (`ShouldRender` guard).** `FormPaperDoll` (and its paper-doll variants +
`FormPaperDollRegion` children) is a pure projection of two `[Parameter]`s, `DamagePaperDoll` and `DamageReport`, with no
internal interactive state. Added:
```csharp
protected override bool ShouldRender()
    => !ReferenceEquals(_lastRenderedPaperDoll, DamagePaperDoll)
       || !ReferenceEquals(_lastRenderedDamageReport, DamageReport);
protected override void OnAfterRender(bool firstRender)
{
    _lastRenderedPaperDoll = DamagePaperDoll;
    _lastRenderedDamageReport = DamageReport;
}
```
Initial render is always forced; `OnAfterRender` records the rendered baseline so subsequent parent re-renders with the
same two object references are skipped, taking the whole child subtree with them. Damage reports arrive as new
deserialized instances, so reference equality is the correct, cheapest change signal. **Invariant relied upon:** a
displayed `DamageReport`/`DamagePaperDoll` is not mutated in place after display (they are replaced wholesale per turn).

**Fix 2 — `FormDamageReportContainer.razor` (cache the combined report).** Previously it rebuilt the merged report in the
markup `@{ }` block on *every* render (`DamageReports[0].BlankCopy()` + `Merge` loop), and in combined mode handed
`FormPaperDoll` a brand-new `DamagePaperDoll` instance each render — which would have defeated Fix 1's guard in that mode.
Moved the merge into `OnParametersSet`, rebuilding only when the source set actually changes (reference-wise
`SequenceEqual` against a `ToList()` snapshot of the previous `DamageReports`). The parent passes a fresh `List` each
render but the `DamageReport` elements are the stable instances stored in `DamageReportContainer`, so within a turn the
sequence is equal and the cached combined report (and its `DamagePaperDoll`) stays reference-stable; across turns the
whole `FormDamageReports` is recreated via `@key`. Net: the merge now runs ~once per container per turn instead of every
render, and combined mode benefits from Fix 1's guard.

**Deliberately NOT done — guarding `FormDamageReport`.** With Fixes 1+2 the expensive paper-doll work is already guarded
in both combined and split views. `FormDamageReport` still re-renders on unrelated broadcasts, but its own per-render cost
is small (a couple of `List.Exists` + dict lookups + an `AttackLog.Log` string loop), and a reference-only guard there
would (a) need a force-render flag for the attack-log visibility checkboxes (private `_logLineVisibility` + InvokeStateChange)
and (b) freeze header bits derived from *live* `UnitList`/`PlayerState` (attacker/defender names, incoming/outgoing CSS) at
their turn-creation values. Not worth the correctness risk. (Rubber-duck concurred.) Its B4 LINQ bullets
(`FormDamageReport.razor:81`, `:9,10`) remain open in the review.

**Flagged, pre-existing, NOT fixed here — `DamageReport.Merge` mutates source reports.** `Merge`
(`Api/Entities/DamageReport.cs:157`) does `ConsumablesAttackers.Add(consumable.Key, consumable.Value)`, storing the
source report's `Consumables` **by reference**; if a later merged report in the same target+phase group shares an
attacker, line 153 `value.Merge(...)` then mutates that shared (source) `Consumables`. `DamagePaperDoll.Merge` (line 161)
is worth auditing for the same aliasing. This bug is independent of this change — the old code ran the same merge on every
render, so Fix 2 strictly *reduces* its frequency (to once/turn) rather than introducing it. Recommended separate fix:
`...Add(consumable.Key, consumable.Value.Copy())` (and verify `DamagePaperDoll.Merge` deep-copies nested lists). Left for
a dedicated correctness pass since `Merge` is a shared Api entity also used server-side.

Build: BlazorServer builds (0 errors; pre-existing 12 warnings unchanged).

---

## B4 (partial). FormDamageReport total-damage sum cached per report

**`FormDamageReport.razor:80` `Total damage to target` sum** was computed inline in markup as
`DamageReport.DamagePaperDoll.DamageCollection.SelectMany(dc => dc.Value.SelectMany(l => l.Value)).Sum()`,
iterating the entire damage collection (all locations × all attackers × all damage entries) on **every** render of
the component. While the DamageReports tab is open, `FormDamageReport` re-renders on every `Index` StateHasChanged
(any player edit broadcasts a GameState update), so this ran far more often than once/turn during rapid clicking.

Fix: the sum is a **pure projection** of `DamageReport.DamagePaperDoll`, which is replaced wholesale per turn and never
mutated in place. Moved the computation into `OnParametersSet`, gated on `!ReferenceEquals(_lastSummedDamageReport,
DamageReport)`, caching into `_totalDamageToTarget`; markup now renders `@_totalDamageToTarget`. Recomputes only when a
new report instance arrives. (OnParametersSet runs on every parent render, but the reference-equality gate skips the
LINQ on the common no-change path; the value is set before the first render.)

**Intentionally left live — the two `UnitEntries.Exists(...)` ownership checks (lines 8-9).** The original bullet
(`FormDamageReport.razor:9,10`) called these "on stable params", but they read `_userStateController.PlayerState.UnitEntries`,
which is **live** state — the user can add/delete units mid-turn, changing `attackingUnitOwnedByYou`/`defendingUnitOwnedByYou`
and the derived header strings (and the incoming/outgoing CSS class) without a new `DamageReport`. Caching them on the report
reference would reintroduce exactly the staleness risk that made us skip a full `ShouldRender` guard on this component. They are
O(units) over a small list (4-30 units), twice — cheap. Left in the per-render `@{ }` block by design. Marked discussed.

Build: BlazorServer builds (0 errors; pre-existing 12 warnings unchanged).
---

## B4 (no-action). ComponentGameState spectatorList — discussed, intentionally left as-is

`ComponentGameState.razor:8,10` builds `playerStateList` (lazy `Where`, enumerated once — already optimal) and
`spectatorList` (`Where(IsSpectator).Select(PlayerId).ToList()`). Reviewed and **deliberately not changed**: this component
subscribes only to `OnGameStateUpdated`, so it re-renders ~once per player edit (not on every event), and the data is tiny
(`GameState.Players` = players in one match, typically <10). Materialising a short spectator-id list is negligible next to
rendering the `ComponentPlayerState` children below it. Caching it would require change-detection against `GameState`, adding
state and regression surface for zero measurable gain — a bad trade. `spectatorList[^1]` (line 23) is safely guarded by
`.Any()`. No-action.

---

## B4. FormDamageReports — full subtree gated with ShouldRender via a live broad signature; markup LINQ precomputed

**Problem.** `FormDamageReports` is `@key`'d on `DamageReportContainer.TimeStamp` (Index.razor:49, FormGameState.razor:47),
so the whole component is recreated whenever damage reports change and the container contents are fixed for an instance's
lifetime. Yet the component re-renders on every `Index` StateHasChanged while its tab is open (parent re-render ⇒
`OnParametersSet`). The old code called `BuildDamageReportsToShow()` unconditionally from `OnParametersSet` (full filter over
all reports across all turns, calling `DamageReportConcernsPlayer` per report) AND the markup additionally did
`_damageReportsToShow.Reverse()` + per-turn `GroupBy(...)` + per-group `.ToList()`, then re-rendered the entire subtree
(accordions → `FormDamageReportContainer` → `FormDamageReport` → `FormPaperDoll`). All of this ran on every gamestate
broadcast (i.e. constantly during rapid clicking), even though the result almost never changes between container versions.

**Why a naive cache/relocation doesn't help.** Moving the markup LINQ into `BuildDamageReportsToShow` alone is a no-op for
CPU, because `BuildDamageReportsToShow` already ran every render via `OnParametersSet`. The win requires *gating*.

**Why the gate's signature must be broad (not a `DamageReport`-reference guard).** The subtree renders from **live** state, not
just the (fixed) reports: the filter depends on `PlayerOptions.ShowOtherPlayersDamageReports`/`ShowMovementDamageReports`,
`PlayerState.IsSpectator`, and `PlayerState.UnitEntries` (via `DamageReportConcernsPlayer`); and `FormDamageReport` headers
depend on the global `UnitList` (`UnitList[id].PlayerId`). `PlayerState` is a computed property off `GameState`, so these can
change on any gamestate update. A guard keyed only on the report set would go stale.

**Fix.** A single `ShouldRender` gate driven by one signature read **live from everything the whole subtree renders
from**, so when nothing relevant changed we neither rebuild nor re-render the subtree: `(DamageReportContainer.TimeStamp,
hasPlayerState, ShowOtherPlayersDamageReports, ShowMovementDamageReports, IsSpectator, ownUnitIdHash, globalUnitListHash,
OnlyNewest)`. `RefreshState()` (called from `OnParametersSet` and the data-change events) compares the signature; on change it
sets `_shouldRender = true` and rebuilds `_damageReportsToShow`, otherwise `ShouldRender()` returns false and the entire
subtree (accordions → `FormDamageReportContainer` → `FormDamageReport` → `FormPaperDoll`) is skipped. The same rebuild is
folded into this one gate — there is no separate cache.

Two distinct unit signals are both included on purpose: `ownUnitIdHash` is a `HashCode` over the player's own
`PlayerState.UnitEntries[].Id`, read live (no round-trip lag) — this is the **filter** input (`DamageReportConcernsPlayer`).
`globalUnitListHash` is `UserStateController.UnitListHash` (`unitId:playerId:unitName` over all players) — this is needed
because `FormDamageReport` **headers** derive from live global `UnitList` (`UnitList[id].PlayerId`), the exact reason we
declined to guard `FormDamageReport` directly earlier; folding the global hash into the parent gate keeps those headers
correct. Critically, editing weapons/armor/firing-solution fields (the bulk of rapid clicking) changes **neither**
`DamageReportContainer.TimeStamp` **nor** `UnitListHash`, so the damage-report subtree is now fully skipped on those events;
only unit rename/add/remove, spectator/option toggles, or new damage reports trigger a re-render.

`BuildDamageReportsToShow` also precomputes the reversed + grouped display structure once
(`List<(int Turn, List<List<DamageReport>> ReportGroups)>`), so the markup does no `Reverse`/`GroupBy`/`ToList` per render.
Dropped the inner `DamageReports.Reverse()` in the build (proven no-op: source and `result` are both `SortedDictionary`,
re-sorted ascending; display order comes from the outer `filtered.Reverse()`).

**Accepted thin edge (user-approved).** `FormDamageReport` line 90 calls `GetUnitType(firingUnitId)`; unit type is not part of
`UnitListHash` (which hashes the unit *name*), so a unit-type change with an unchanged name could be momentarily missed by the
gate. Considered negligible (a unit-type edit normally changes other signature inputs too) and explicitly accepted.

**Side benefits.** The precomputed per-group lists are stable instances across renders, which strengthens the
`FormDamageReportContainer` combined-report cache (its `SequenceEqual` check). Also resolves the related B4 bullet
`UserStateController.cs:423-425` (`DamageReportConcernsPlayer` double-`Exists` "per render of FormDamageReports") — it now runs
only on a real rebuild.

**Behavior preserved.** Identical display ordering (turns newest-first; groups in `GroupBy` first-occurrence order), identical
empty/`OnlyNewest` semantics (a turn that filters to zero still yields an empty accordion). No `@key` added on
`ContainerAccordion`/`FormDamageReportContainer` — positional reconciliation and the accordion's init-time `Enabled` read are
unchanged from the original; the accordion's open/close is internal local state, so skipping a parent render never disturbs it
(`ContainerAccordion.razor:15,28` — `_enabled` set once in `OnInitialized`, toggled by its own `@onclick`). `Delete` and
attack-log-checkbox interactions still work: `DamageReportContainer.Remove` bumps `TimeStamp` (so the gate fires), and the
attack-log checkbox re-renders `FormDamageReport` via its own `StateHasChanged` (a child's self-render is not gated by the
parent's `ShouldRender`). Rubber-duck consulted on the plan.

Build: BlazorServer builds (0 errors; pre-existing 12 warnings unchanged). Needs in-browser verification: both damage-report
tabs (full list + "only newest"), spectator view, show-other-players / show-movement toggles, and unit add/remove while the
damage tab is open.
---

## B4 (no-action). GetTargetNumberUpdateSingleWeapon lookup — discussed, already optimal

`ComponentWeaponEntry.razor:10` and `FormWeaponEntry.razor:12` call
`_userStateController.GetTargetNumberUpdateSingleWeapon(unitId, weaponEntryId)` in the render block. The original bullet flagged
this as a per-render "LINQ allocation", but the method is two O(1) `Dictionary.TryGetValue` lookups with no allocation
(UserStateController.cs:502-510). Both components are already render-gated: each has a `CheckRefresh` subscribed to
`OnTargetNumbersUpdated` that calls `InvokeStateChange` only when *this unit's* target-number timestamp actually changed
(not on every target-number packet), and `FormWeaponEntry` additionally memoizes the tooltip string on that same timestamp
(`GetCachedTooltipContent`). The remaining O(1) lookup on the (already-rare) renders is negligible. No-action.
---

## B4 (no-action). UnitList setter hash / UpdateUnitList allocation — discussed, deliberately left as-is

`UserStateController.cs` runs `UpdateUnitList()` from the `GameState` setter on every inbound game state (line 145). The
original bullet flagged (a) the `UnitList` setter's `string.Join(...).Fnv1aHash64()` and (b) the per-inbound
`ConcurrentDictionary` rebuild + LINQ scans.

(a) is **already gated**: the `UnitList` setter (and thus the hash) only runs when `UpdateUnitList` detects a real identity
change — `UnitList.Count != newUnitList.Count`, a new key (`newUnitList.Any(u => !UnitList.ContainsKey(u.Key))`), or a changed
`PlayerId`/`Name`/`Type` (lines 535-551). Value-only pushes (the common case during rapid editing) return `false` and never
touch the hash.

(b) The residual is a bounded `ConcurrentDictionary` rebuild (+ N tuples, N = total units ~8-60) and an `Any(...)` scan per
inbound state, discarded on the no-change path. Eliminating it requires reworking the change-detection to compare against the
existing `UnitList` before allocating — but `GameState`/`UpdateUnitList` is the most regression-prone code in the app (the
recent identity-retention, double-hop, and unit-reappearance fixes all live here, and the method carries an explicit "Be
careful about this optimization" warning). The reward (a small bounded allocation a few times/sec) does not justify the risk.
Deliberately left as-is.
## Sync-over-async: Index.razor:164 Connect() (NO-ACTION)

**Item:** `Pages/Index.razor:164` calls `_formServer.Connect(credentials)` from `OnAfterRenderAsync` without awaiting; `Connect()` does sync Redis setup.

**Investigation:**
- `Index.OnAfterRenderAsync(firstRender)` does `var credentials = await _localStorage.GetUserCredentials();` then, if non-null, `_formServer.Connect(credentials)`.
- `FormServer.Connect` -> `ResolverCommunicator.Connect` (void): sets `_playerName`, calls `Reset()`, then `_clientToServerCommunicator.Send(RequestNames.Connect, ...)`.
- `Reset()` -> `TeardownCommunicator()` + `new ClientToServerCommunicator(...)`. The base `RedisCommunicator` constructor (RedisCommunicator.cs:78-79) does the blocking `ConnectionMultiplexer.Connect(_connectionString)` + `Subscribe()`.

**Findings:**
1. The "sync-over-async" label is inaccurate. `Connect()` is plain `void`; nothing is blocked-on (no `.Result`/`.Wait()` on a Task). It simply runs synchronously inline -- there is no Task being sync-waited, so the classic sync-over-async deadlock risk does not apply.
2. Cannot convert the lifecycle method to the sync `OnAfterRender(bool)`: line 164 is a genuine `await _localStorage.GetUserCredentials()` (Blazored.LocalStorage JS interop, inherently async -- reading browser localStorage from a Server circuit has no synchronous equivalent). The async method is legitimate and necessary.
3. The only real residual is the blocking `ConnectionMultiplexer.Connect` inside the synchronous Connect path. It is a ONE-TIME cost at login (not a per-render/per-edit recurring cost, which is this review's focus). If Redis is local it is negligible; the only downside is a briefly frozen circuit if Redis is slow/unreachable (rare).
4. A proper async fix is disproportionately invasive: the blocking call lives in a CONSTRUCTOR in the shared `Api` project (`RedisCommunicator`), used by BOTH client and server (Orleans Silo constructs these too). Going async would require `ConnectAsync` + moving connection setup out of the ctor into an async init/factory, then cascading `async Task` up through `ResolverCommunicator.Connect` and `FormServer.Connect` -- touching the comm layer just refactored for the double-hop removal. High regression risk for a one-time operation.

**Decision (user-approved): NO-ACTION.** The item is a mischaracterization (no Task is sync-waited) plus a one-time blocking-connect whose proper fix is an invasive shared-comm-layer async refactor not justified by a once-per-login cost.
## FormGameList.razor:50 sort + hidden FormServer re-rendering on every packet (FIXED via conditional render)

**Item:** `FormGameList.razor:50` `OrderByDescending(...)` materialised via spread `[.. ...]` per `OnParametersSet`.

**Investigation (the real issue was broader than the sort):**
- `FormGameList` is used only inside `FormServer` (the connected-but-not-in-game lobby branch), receiving `Games="@_userStateController.GameEntries.Values.ToList()"`. The sort itself is over a handful of `GameEntry` rows -- trivial CPU.
- KEY FINDING: `VisualStyleController.HideElement(bool)` returns only `"display:none"` (VisualStyleController.cs:18). It is a CSS hide, NOT a conditional (`@if`) removal. So while in-game, the whole `FormServer` subtree under `Index.razor:26` was still being rendered into the render tree and re-rendered on EVERY Index re-render (i.e. every inbound packet), even though invisible. `FormServer` has no `ShouldRender` override and is a child of `Index`, so it re-rendered whenever Index did. That meant the hidden lobby's `FormGameList` re-sorted the game list and rebuilt its table rows on every packet during gameplay -- pointless work on the hot path.

**Fix (user-proposed conditional render; rubber-duck-reviewed, no blocking issues):**
- `Index.razor:26-28`: replaced the CSS-hidden `<div style="@HideElement(IsConnectedToGame)">...<FormServer @ref="_formServer"/>...</div>` with a conditional block: `@if (!_userStateController.IsConnectedToGame) { <div class="resolver_div_componentcontainer"><FormServer @key="_userStateController.PlayerName"></FormServer></div> }`. While in-game, `FormServer` is now removed from the render tree entirely (not merely hidden), so it -- and the `FormGameList` sort/table -- are no longer rebuilt per packet.
- Dropped the `@ref="_formServer"` coupling: `FormServer.Connect` and `FormServer.LeaveGame` are pure delegations to the injected `ResolverCommunicator` (FormServer.razor:86-89, 101-104), which `Index` already injects. So:
  - Removed the `private FormServer _formServer;` field.
  - `Index.razor:168` (OnAfterRenderAsync firstRender auto-connect): `_formServer.Connect(credentials)` -> `_resolverCommunicator.Connect(credentials)`.
  - `Index.razor:200` (LeaveGame, called while in-game): `_formServer.LeaveGame()` -> `_resolverCommunicator.LeaveGame()`. This was the snag with a naive `@if` (the ref would be null in-game); calling the communicator directly avoids it.

**Why safe:**
- `FormServer` is now disposed when joining a game and recreated when leaving. `FormServer.Dispose()` unsubscribes `OnGameEntriesReceived`; `OnInitialized()` re-subscribes and calls `RefreshGameList()` (fetches only when `!IsConnectedToGame`) on return to lobby -- behaviorally correct (fresh game list on returning to lobby).
- `JoinGame`/`Connect`/`LeaveGame` are synchronous void delegations with no pending continuation inside `FormServer`, so disposing it after `IsConnectedToGame` flips is fine.
- `FormServer`'s own internal `<FormCredentials OnSubmit="@Connect">` login form is unchanged.
- Build: 0 errors, same 12-warning baseline.

**Payoff:** The entire hidden lobby subtree (connection header, game-list table, sort) is removed from the per-packet render path during gameplay, strictly better than micro-optimizing the sort. Net code reduction (removed a field + the @ref). NEEDS in-browser verification: login (auto-connect from stored credentials), lobby game-list display + sort order, join a game (lobby disappears), leave a game (lobby reappears with fresh list), password-protected join modal.
## FormRadio.razor:20 Guid-based radio group name (NO-ACTION)

**Item:** `FormRadio.razor:20` per-instance `Guid.NewGuid().ToString().Replace("-", "")` for radio name; recreated each time key invalidates the component.

**Investigation:** `_name` is a `readonly` FIELD INITIALIZER -- it runs exactly once per component instance construction, not per render. The cost is a single `Guid.NewGuid()` + `ToString()` + `Replace` per instance: negligible. A Guid is the correct mechanism here -- HTML radio buttons must share a `name` to behave as one group, and the name must be unique across different radio groups on the page; a Guid guarantees that.

**Findings:** The review's "recreated each time key invalidates" framing is really pointing at INSTANCE CHURN driven by timestamp `@key`s, which is the separate Part B / B1 key concern (the single largest win). The Guid generation cost itself is not the problem; eliminating the churn (stable keys) is handled there. Replacing the Guid with a static `Interlocked`-incremented counter would save microseconds-per-instance and add cross-circuit-uniqueness reasoning for no meaningful benefit, and regenerating the name on recreation has no DOM-diff cost because the whole component is rebuilt anyway when its key changes.

**Decision (user-approved): NO-ACTION.** Negligible per-instance cost; the underlying churn concern belongs to the B1 stable-key work, not here.
## Tooltip strings rebuilt and inlined as DOM attributes per render (ALREADY ADDRESSED)

**Item:** Tooltip strings are rebuilt and inlined as DOM attributes per render.

**Investigation:** There are exactly two tooltip-string producers in the client:
1. `FormPaperDollRegion.razor:6,11` -- `data-tooltip-content="@FormPaperDoll.GetDamageText(Location)"`. `GetDamageText` (FormPaperDoll.razor:188) does `SelectMany/ToList/string.Join` per region. This is now gated by `FormPaperDoll.ShouldRender` (FormPaperDoll.razor:171-175), which suppresses the ENTIRE paper-doll subtree (the FormPaperDoll* variant and its per-region children) unless `DamagePaperDoll`/`DamageReport` changes by reference. So `GetDamageText` no longer runs on the unrelated gamestate broadcasts that re-render the DamageReports tab.
2. `FormWeaponEntry.razor:13,17` -- `data-tooltip-content="@tooltipContent"` where `tooltipContent = GetCachedTooltipContent(...)`. Already memoized via `_cachedTooltipContent`/`_cachedTooltipKey` keyed on `_targetNumberTimeStamp` (FormWeaponEntry.razor:221-229); the expensive `GetTargetNumberText` only runs when the unit's target-number data changes.

The two static tooltip target divs (Index.razor:58 `resolver_tooltip_paperdoll`, FormGameState.razor:38 `resolver_tooltip_targetnumber`) are empty containers populated by JS (react-tooltip-style `data-tooltip-id` wiring) -- they carry no per-render strings.

**Decision (user-approved): ALREADY ADDRESSED -- no new code.** Both tooltip-string producers were covered by prior completed items (FormPaperDoll ShouldRender guard; FormWeaponEntry tooltip memoization).
## BaseFaemiyahComponent.InvokeStateChange fire-and-forget (NO-ACTION -- correct/idiomatic usage)

**Item:** `BaseFaemiyahComponent.InvokeStateChange` (line 13) doesn`t await `InvokeAsync` -- fire-and-forget swallows exceptions.

**What it is:** A convenience wrapper -- `protected void InvokeStateChange() => InvokeAsync(StateHasChanged);` -- to avoid typing `InvokeAsync(StateHasChanged)` at every event-handler subscription site.

**Assessment (usage is correct):**
- `InvokeAsync(StateHasChanged)` is the RIGHT call here because the events that call it (UserStateController events) are raised from BACKGROUND threads (the Redis/dispatcher message-processing threads in the communication layer), not the renderer`s synchronization context. `InvokeAsync` marshals `StateHasChanged` onto the renderer`s dispatcher -- exactly what a cross-thread render trigger requires. It is a safe superset of a bare `StateHasChanged()` (runs inline-ish when already on-context).
- Not awaiting is acceptable for a render trigger: there is no need to block the handler until the render completes, and the callers are synchronous `void` delegates (`+= InvokeStateChange`) that cannot await. The textbook `await InvokeAsync(StateHasChanged)` form only matters inside an already-async method that needs to sequence post-render work -- not this use case.
- Making it awaitable would force `async void` (callers are sync delegates), which is a worse anti-pattern. There is no logger in the base component, so adding real exception diagnostics would be disproportionate.
- The only realistic fault of the discarded Task is `ObjectDisposedException` during a teardown race (event fires while the component is being disposed) -- benign and commonly ignored.

**Decision (user-confirmed): NO-ACTION.** Correct, idiomatic Blazor cross-thread render trigger; the wrapper exists purely as an editing convenience and is used appropriately.
## B5. CSS issues (whole section) -- trivial cleanups DONE; render-perf items NO-ACTION (perf-moot)

**Context / decisive reframing (user):** The app is Blazor Server. The ARM box only HOSTS it -- server-side component rendering + SignalR DOM diffing happen on ARM, but the actual CSS layout/paint runs in each user`s browser on capable hardware (quad-core i7s, modern phones). User: "I care very little about CSS render performance because I have quad-core i7s and modern phones actually rendering the page." So the entire premise of B5 ("expensive on ARM browsers") does not apply -- the ARM constraint is about server-side C# render work (B1/B4), not browser paint.

**DONE (safe trivial cleanups -- dead/duplicate declarations, valuable regardless of perf):**
- `.resolver_div_componentblock` (Resolver.css ~691) declared `display: flex;` TWICE -> removed the duplicate.
- `.resolver_div_unitname` (Resolver.css ~775) declared `font-weight: bold;` TWICE -> removed the duplicate.
(Both are no-op duplicate property declarations; removing them changes no computed style. No build impact -- CSS is a static asset.)

**NO-ACTION (perf-moot -- client-side paint on capable hardware; line numbers per the review, several were slightly off):**
- Subgrid + nested grid (actual `subgrid` at lines 710 and 806, not 709/789). Load-bearing: `subgrid` is the mechanism aligning the label/input columns across `resolver_div_componentrow`s; removing it would break form-column alignment. Browser layout cost is borne client-side.
- `display:inline-grid` + `display:flex` nesting "every cell triggers flex measurement" -- client-side layout, intentional structure.
- Sibling/`:hover ~` selectors (e.g. `.resolver_label_toggleradio:hover input:checked ~ .resolver_span_toggleradio`) -- drive the toggle/reminder visuals; client-side style recalc on capable hardware.
- `> *:not(...)` universal selector (line ~726) -- intentional layout rule; client-side match cost.
- SVG `polygon:hover` recolor (~1002), drag-sentinel `transition` (~854), damage-card `box-shadow: inset` (~948/953), per-component font declarations (~763/882/901) -- all client-side paint, on capable hardware.

**LOW-PRIORITY / non-perf (left as-is, not perf-related):**
- `!important` overuse (lines ~267/323/369) -- specificity smell, not perf; changing it risks visual-specificity regressions for no perf benefit.
- CSS file ~29 KB not minified -- a one-time static-asset download (browser-cached), not SignalR traffic; negligible under this hosting model.

**Decision (user-directed): do trivial cleanups now, defer/close the rest.** Trivial duplicate removals applied; all CSS render-perf items closed as perf-moot given client-side rendering on capable hardware.
## B6. Double-hop hub (ALREADY ADDRESSED -- see completed B3)

**Item:** "Double-hop hub -- see B3 above."

**Decision:** Already addressed. The double-hop architecture (ClientHub / self-looping HubConnection) was removed in prior completed work -- see "B3. Double-hop architecture removed (ClientHub / self-looping HubConnection eliminated)" in this completed log. No further action; cross-reference closed.
## B6. ResolverCommunicator.Disconnect Redis subscription leak (ALREADY ADDRESSED)

**Item:** `ResolverCommunicator.Disconnect` (line 89) sets `_clientToServerCommunicator = null` without `Stop()` -> Redis subscription leak.

**Investigation:** Current code no longer nulls the field directly. `ResolverCommunicator.Disconnect` (ResolverCommunicator.cs:80-90) sends the Disconnect request then calls `TeardownCommunicator()`. `TeardownCommunicator` (375-382) calls `_clientToServerCommunicator.Dispose()` BEFORE setting it to null. `RedisCommunicator.Dispose(bool)` (RedisCommunicator.cs:237-250) unsubscribes `_listenedMessageQueue`, calls `RedisSubscriber.UnsubscribeAll()`, and disposes the `ConnectionMultiplexer`. So the Redis subscription + connection are fully released on disconnect.

**Decision:** ALREADY ADDRESSED -- the prior completed B3 "Per-circuit Redis subscription churn / leak -- FIXED" work introduced `TeardownCommunicator` + the proper `Dispose` path. The review item describes pre-fix code. No further action.
## B6. SendErrorMessage fire-and-forget continuation (REFACTORED to async/await)

**Item:** `SendErrorMessage` fire-and-forget (line ~373) uses `_ = ...ContinueWith(...)` allocating a continuation. Use try/catch await in async method.

**Context:** `SendErrorMessage` is called only from error paths (Connect catch, SendRequest catch, CheckAuthentication guard) -- all synchronous void contexts -- so the dispatch is necessarily fire-and-forget. The continuation only allocates when an error actually occurs (rare), so this is a clarity refactor, not a perf win (user chose to do it anyway).

**Change (ResolverCommunicator.cs):**
- Rewrote the body from `_ = _dispatcher.DispatchAsync(...).ContinueWith(t => _logger.LogError(t.Exception, ...), TaskContinuationOptions.OnlyOnFaulted);` to a try/catch await:
  ```
  private async Task SendErrorMessage(string errorMessage)
  {
      try { await _dispatcher.DispatchAsync(EventNames.ErrorMessage, _dataHelper.Pack(new ClientErrorEvent(errorMessage))); }
      catch (Exception ex) { _logger.LogError(ex, "Failed to dispatch error message"); }
  }
  ```
- Chose `async Task` (NOT `async void`): an initial `async void` version tripped SonarAnalyzer S3168 ("Return Task instead") and pushed the warning count 12 -> 13. Returning `Task` and fire-and-forgetting at the three call sites with `_ = SendErrorMessage(...)` (lines 73, 351, 359) keeps it idiomatic and restores the 12-warning baseline. The method fully handles its own exceptions, so the discarded Task never faults unobserved.

**Build:** 0 errors, 12 warnings (baseline restored).