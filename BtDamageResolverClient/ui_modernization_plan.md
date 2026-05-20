# UI Modernization Plan: CSS Cleanup & Mobile Responsiveness

## Problem Statement

The UI uses a hybrid old-school layout (CSS `display:table` + `float:left`) that:
- Is not mobile-responsive (unit cards and detail panels don't stack on small screens)
- Has significant dead CSS inherited from the Blazor Server template
- Has several CSS bugs and anti-patterns

The goal is to clean up all dead/buggy CSS, modernize the layout to flexbox, and add proper responsive breakpoints so the UI is usable on a phone.

---

## Findings

### 1. Dead CSS in `site.css` (~90% unused boilerplate)

`site.css` is the default Blazor Server template stylesheet. Almost none of it applies to this app:

| Rule | Status |
|------|--------|
| `.sidebar`, `.sidebar .top-row`, `.sidebar .navbar-brand`, etc. | **Dead** — no sidebar in the app |
| `.top-row`, `.main .top-row`, `.main .top-row > a`, etc. | **Dead** — no `.main` or `.top-row` elements exist |
| `.navbar-toggler` | **Dead** — no navbar |
| `.content { padding-top: 1.1rem; }` | **Dead** — `.content` class never used |
| `@media (min-width: 768px) { app { flex-direction: row; } .sidebar { ... } }` | **Dead** — sidebar layout |
| `@media (max-width: 767.98px) { .main .top-row:not(.auth) { display: none; } }` | **Dead** |
| `app { display: flex; flex-direction: column; }` | **Conflicts** with actual app layout |
| **Keep:** `html, body { font-family... }` | ✓ Used |
| **Keep:** `a, .btn-link { color: ... }` | ✓ Used |
| **Keep:** `#blazor-error-ui { ... }` and `.dismiss { ... }` | ✓ Used in `_Host.cshtml` |
| **Keep:** `.valid.modified`, `.invalid`, `.validation-message` | ✓ Blazor form validation |

**Action:** Trim `site.css` to just the 5–6 rules that are actually used.

---

### 2. CSS Bugs in `Resolver.css`

| Bug | Location | Fix |
|-----|----------|-----|
| `background-color: none` | `.resolver_status_transparent` (line 911) | Change to `transparent` |
| `content: '+'` / `content: 'X'` on real elements | `.resolver_button_add`, `.resolver_button_delete`, `.resolver_button_leave` | Remove — `content` only works on `::before`/`::after` |
| Duplicate `.resolver_div_pickeritem` rule | Lines 307–313 and 316–319 | Merge into one |
| Empty rules with no declarations | `.resolver_tr_unitinformation { }`, `.resolver_td_unitinformation_label { }` | Delete |
| `background-color: beige` on `.flexbox-column` | Line 628 | Remove — debugging artifact |
| Outdated vendor prefixes (mostly harmless) | `user-select`, `transform` | Remove `-webkit-`/`-moz-`/`-ms-` variants |

---

### 3. Layout Anti-Patterns

#### 3a. `display: table` + `display: table-row` hybrid layout

The main unit card system uses:
```
.resolver_div_componentlistcontainer  { position: relative }
.resolver_div_componentcontainer      { display: table; float: left }  ← non-wrapping
.resolver_div_componentrow            { display: table-row }           ← margin/padding ignored!
.resolver_div_componentcell           { float: left }                  ← float inside table-row
```

Problems:
- `margin`, `padding`, `border-radius` on `display: table-row` **have no effect** (CSS spec)
- `float: left` on unit cards means they sit side-by-side with no wrapping breakpoints
- Cannot reflow to single-column on phones
- `float: left` on cells inside a table-row creates conflicting layout modes

**Fix:** Replace with a flexbox layout:
- `resolver_div_componentlistcontainer` → `display: flex; flex-wrap: wrap; gap: ...`
- `resolver_div_componentcontainer` → flex item with appropriate `min-width` / `width`
- `resolver_div_componentrow` → `display: flex; flex-wrap: wrap; align-items: flex-start`
- `resolver_div_componentcell` → flex item (remove float)

#### 3b. Unit entry detail panels are horizontally floated

Inside each unit card, `FormUnitEntry` renders three `resolver_div_componentcell` blocks (unit static data table, unit parameters + firing solution table, weapons table) as `float: left`. On a phone, these sit side by side at tiny widths.

**Fix:** Flex row that wraps to column at a breakpoint (e.g., ≤768px).

#### 3c. Weapon table has 8 columns, no overflow handling

`FormWeaponEntry.razor` renders a `<tr>` with: target number | state toggle | amount | modifier | weapon name | ammo | ↑↓ buttons | delete button.

There is no `overflow-x: auto` wrapper, so on small screens the entire page widens.

**Fix:** Wrap the weapon table in a div with `overflow-x: auto`.

#### 3d. `float: left` scattered elsewhere

Other elements using `float: left` that won't cooperate on mobile:
- `.resolver_div_damagereport { float: left }` — damage report sections
- `.resolver_div_imagecontainer { float: left }` — paper doll image
- `.resolver_accordion_indicator { float: left }` — harmless but can be flexbox
- `.resolver_modal_title { float: left }` — modal header
- `.resolver_div_tab { float: left }` — tab buttons

---

### 4. Mobile Responsiveness Gaps

| Element | Issue | Fix |
|---------|-------|-----|
| Unit cards | Float side-by-side, no wrap | Flex wrap, full-width below ~900px |
| Unit detail panels | Three columns shrink to unreadable | Stack vertically below ~768px |
| Weapon table | 8 columns overflow | `overflow-x: auto` wrapper |
| Paper doll | Fixed 18rem height | Cap with `max-height` or make relative |
| Modal | `width: 50%` — fine on phone, but `margin: 10% auto` wastes space | `width: min(90vw, 640px); margin: 5vh auto` |
| Combobox dropdown | Fixed `width: 16rem` | `width: min(16rem, 90vw)` |
| Picker dropdown | Fixed `max-width: 15rem` | Same treatment |
| Attack log indentation | Fixed `padding-left: 1–3rem` | Acceptable; reduce slightly |

---

### 5. Space Efficiency Issues

- `resolver_table td/th` has `padding-left: 1rem; padding-right: 1rem` — generous on desktop, wastes space on phones; could be tightened with a mobile breakpoint
- `.resolver_div_componentrow { margin: 1em; padding: 0.5em; border-radius: 1em; }` — these have **no effect** because the element is `display: table-row`; once layout is switched to flexbox, these values should be reconsidered intentionally
- `.resolver_div_accordioncontent { padding: 0; }` and `.resolver_div_tabcontent { padding: 0; }` — fine but redundant
- Accordion sections in `FormGameState` could use `padding` inside their content area for visual separation from the button

---

## Implementation Plan

### ~~Phase 1 — Remove Dead CSS~~ ✓ Done
`site.css` deleted. Live rules merged into top of `Resolver.css`. Open-iconic import dropped (unused).

### ~~Phase 2 — Fix CSS Bugs~~ ✓ Done
- `background-color: none` → `transparent` on `.resolver_status_transparent`
- Removed `content` property from `.resolver_button_add/delete/leave`
- Merged duplicate `.resolver_div_pickeritem` rules
- Deleted empty `.resolver_tr_unitinformation` and `.resolver_td_unitinformation_label`
- Removed `background-color: beige` debug artifact from `.flexbox-column`
- Dropped all `-webkit-`/`-moz-`/`-ms-` vendor prefixes

### Phase 3 — Modernize Container Layout (flexbox)
- `resolver_div_componentlistcontainer` → `display: flex; flex-wrap: wrap`
- `resolver_div_componentcontainer` → becomes a flex item
- `resolver_div_componentrow` → `display: flex; flex-wrap: wrap; align-items: flex-start`
- `resolver_div_componentcell` → flex item, remove `float: left`
- `resolver_div_damagereport`, `resolver_div_imagecontainer`, `resolver_modal_title`, etc. → remove floats, use flex
- Accordion indicator → remove `float: left`, use inline-flex
- Tab container → already `float: left` per tab, can stay or switch to flex

### Phase 4 — Add Responsive Breakpoints
- **≤768px (phone portrait):**
  - Unit cards: `width: 100%`
  - Unit detail cells: `flex-direction: column; width: 100%`
  - Weapon table wrapper: `overflow-x: auto`
  - Modal: `width: 90vw; margin: 4vh auto`
  - Combobox: `width: min(16rem, 90vw)`
  - Table padding: reduce `1rem` → `0.4rem`
- **768px–1024px (tablet):**
  - Unit cards can be 2-up or full-width depending on content
  - Unit name switches from horizontal (mobile) to vertical sidebar (desktop) — already handled

### ~~Phase 5 — Replace Layout Tables with CSS Grid Divs~~ ✓ Mostly done

**Approach used:** Rather than introducing new CSS form section classes as originally planned, the existing `resolver_div_componentgroup` / `resolver_div_componentrow` / `resolver_div_componentcell` CSS grid classes were reused throughout, with:
- Inline `style` to set custom `grid-template-columns` when the default doesn't fit (e.g. 2-col, 5-col)
- A `tablerows` modifier class added to `resolver_div_componentgroup` for alternating row colors

**Genuine data display tables (`FormDataDisplay*`) were intentionally kept as `<table>`** — they display tabular data, not layout.

**The game list (`FormGameList` + `FormGameEntry`) was reverted to `<table>`** — the converted result did not render well enough to keep.

#### Status per file

| File | Status | Notes |
|------|--------|-------|
| `FormWeaponBay.razor` | ✓ Done (prior session) | Uses `resolver_div_weaponentrylist` (8-col subgrid) |
| `FormWeaponEntry.razor` | ✓ Done (prior session) | Emits `resolver_div_componentrow resolver_div_weaponentry` with 8 cells |
| `FormUnitEntry.razor` | ✓ Done (prior session) | Uses `resolver_div_componentgroup` / row / cell throughout |
| `FormServer.razor` | ✓ Done | Converted from table to `resolver_div_componentgroup` / row / cell |
| `ComponentHeatAmmoEstimate.razor` | ✓ Done | Single-column table → plain nested divs |
| `FormDamageInstance.razor` | ✓ Done | 2-col componentgroup with inline grid-template-columns |
| `ComponentWeaponEntry.razor` | ✓ Done | Emits `resolver_div_componentrow resolver_div_weaponentry` with 5 cells (read-only context) |
| `ComponentUnit.razor` | ⚠️ Converted but rendering incorrectly | Needs investigation and fix |
| `FormGameList.razor` | ✗ Reverted to `<table>` | Converted result didn't render well |
| `FormGameEntry.razor` | ✗ Reverted to `<tr>/<td>` | Reverted together with FormGameList |
| `FormDataDisplayClusterTable.razor` | ✓ Kept as `<table>` | Genuine 2D dynamic matrix |
| `FormDataDisplayWeapon.razor` | ✓ Kept as `<table>` | Tabular data |
| `FormDataDisplayUnit.razor` | ✓ Kept as `<table>` | Tabular data |
| `FormDataDisplayPaperDoll.razor` | ✓ Kept as `<table>` | Tabular data |
| `FormDataDisplayCriticalDamageTable.razor` | ✓ Kept as `<table>` | Tabular data |

#### Dead CSS to clean up (now unused after table removals)
`resolver_tr_unitinformation`, `resolver_td_unitinformation_label`, `resolver_td_unitinformation_data`, `resolver_tr_weaponentry`, `resolver_td_targetnumber`

Note: `resolver_table_gamelist` is still used (FormGameList reverted to table).

### Phase 6 — Visual Block Clarity
- Add a border or subtle background to `resolver_div_componentcontainer` for unit cards (currently alternating colors only)
- Add `gap` between unit cards in the list
- Add `gap` between unit detail cells
- Ensure accordion sections have consistent padding inside their content area

---

## Input Component Improvements (completed this session, outside original plan)

### FormComboBox
- Added `combobox` modifier class to `resolver_div_inputwrapper` with `max-width: 20rem` to prevent it from expanding beyond its container.
- The hidden-select sizer approach (to shrink-to-content) was tried but abandoned — it drove grid column width up through the whole componentgroup. `max-width` cap is the practical compromise.

### FormNumber / FormNumberPicker / FormNumberPickerDisplayOnly — Refactored
`FormNumber` is now the shared number input primitive used by both `FormNumberPicker` and `FormNumberPickerDisplayOnly`. Key behaviors:
- Commits value only on blur / Enter / Esc (not on every keystroke)
- Spinner arrows commit immediately when the input is **not** focused; deferred to blur/Enter/Esc when focused
- `ShouldRender()` returns false while focused, preventing parent re-renders from overwriting mid-typing DOM input value (important for partial inputs like a leading `-`)
- Exposes `FocusAsync()` for programmatic focus from parent components
- `Width` parameter removed (was unused). `OnInitialized` and `_typingMode` removed (both redundant)

`FormNumberPickerDisplayOnly` now auto-focuses the inline input when switching from span to edit mode (so Enter/Esc work immediately without an extra click).

---

## Files Changed

| File | Changes |
|------|---------|
| ~~`src/BlazorServer/wwwroot/css/site.css`~~ | ✓ Deleted — live rules merged into `Resolver.css` |
| `src/BlazorServer/wwwroot/css/Resolver.css` | ✓ Bugs fixed; `tablerows` modifier added; `combobox` wrapper modifier added; dead CSS cleanup pending |
| `src/BlazorServer/Shared/FormServer.razor` | ✓ Converted to `resolver_div_componentgroup` / row / cell |
| `src/BlazorServer/Shared/ComponentHeatAmmoEstimate.razor` | ✓ Table → plain divs |
| `src/BlazorServer/Shared/FormDamageInstance.razor` | ✓ Table → 2-col componentgroup |
| `src/BlazorServer/Shared/ComponentWeaponEntry.razor` | ✓ tr → componentrow (5 cells, read-only context) |
| `src/BlazorServer/Shared/ComponentUnit.razor` | ⚠️ Converted but rendering incorrectly — needs fix |
| `src/BlazorServer/Shared/Generic/FormComboBox.razor` | ✓ Added `combobox` modifier class |
| `src/BlazorServer/Shared/Generic/FormNumber.razor` | ✓ Full rewrite — commit-on-blur pattern, spinner support, ShouldRender |
| `src/BlazorServer/Shared/Generic/FormNumberPicker.razor` | ✓ Now uses FormNumber instead of bare input |
| `src/BlazorServer/Shared/Generic/FormNumberPickerDisplayOnly.razor` | ✓ Now uses FormNumber with auto-focus |
