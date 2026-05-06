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

## Proposed Implementation Plan

### Phase 1 — Remove Dead CSS
- Trim `site.css` to only: `html/body` font, `a/.btn-link` color, `.btn-primary`, `.valid.modified`, `.invalid`, `.validation-message`, `#blazor-error-ui` and `.dismiss`

### Phase 2 — Fix CSS Bugs
- `background-color: none` → `transparent`
- Remove `content` property from button modifier classes
- Remove duplicate `.resolver_div_pickeritem`
- Remove empty rules
- Remove `background-color: beige` from `.flexbox-column`
- Drop obsolete vendor prefixes

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

### Phase 5 — Replace Layout Tables with Flex Form Sections

Every `<table>` used for label+input pair alignment (not for tabular data) should be replaced with a flexbox form pattern. This also allows logically related compact fields to share a row.

#### New CSS form pattern

```css
/* A section groups one conceptual block of fields */
.resolver_form_section { display: flex; flex-direction: column; gap: 0.4rem; }

/* A row holds one or more fields side-by-side, wraps on narrow screens */
.resolver_form_row { display: flex; flex-wrap: wrap; gap: 0.5rem 1rem; align-items: center; }

/* A field is a label+input pair */
.resolver_form_field { display: flex; align-items: center; gap: 0.3rem; }

/* Full-width field (e.g. the Name text input) */
.resolver_form_field.wide { flex-basis: 100%; }

/* Section header (replaces <th colspan="2">) */
.resolver_form_section_header { font-weight: bold; font-size: 0.9em; margin-bottom: 0.2rem; }
```

Conditional visibility: `VisualStyleController.HideElement()` already returns `display:none`, so applying it on a `<div class="resolver_form_field">` instead of a `<tr>` works without any C# changes.

#### FormUnitEntry.razor — Left panel (Unit static data)

Current (8-row label/input table):
```
Name        [text___________]
Gunnery     [0][1][2][3][4][5][6][7][8]
Piloting    [0][1][2][3][4][5][6][7][8]
Type        [dropdown_______]
Tonnage     [picker]
Speed       [picker]
Jump Jets   [picker]
Features +  [chip] [chip] ...
```

Proposed compact flex layout:
```
[Name text_____________________________]   ← wide field, full row
[Gunnery [0-8]]  [Piloting [0-8]]         ← two compact pickers, same row
[Type [dropdown___]]                       ← full row (select can be wide)
[Tonnage [picker]] [Speed [picker]] [Jump Jets [picker]]  ← 3 conditionals, same row
[Features + ] [chip] [chip] ...            ← full row
```

Markup change: replace `<table>` + `<tr>`/`<td>` with `<div class="resolver_form_section">` containing `<div class="resolver_form_row">` groups. Gunnery and Piloting share one row; Tonnage/Speed/Jump Jets share one row and wrap gracefully when some are hidden.

#### FormUnitEntry.razor — Middle panel (Unit parameters)

Current (7-row label/input table):
```
[Unit parameters header]
Troopers       [picker]
Dissipation    [picker]
Movement       [Stand][Walk][Run][Immobile][...]
Evasion        [Evading toggle]
Hexes moved    [0][1][2]...[n]
Stance         [Stand][Crouch][Prone][...]
Status effects [Narced][Tagged]
```

Proposed:
```
Unit parameters
[Troopers [picker]] [Dissipation [picker]]   ← both compact, same row
[Movement [radio buttons]]                    ← full row (can be many options)
[Evasion [toggle]] [Status: [Narced][Tagged]] ← small toggles, share a row
[Hexes moved [radio buttons]]                 ← full row
[Stance [radio buttons]]                      ← full row
```

#### FormFiringSolution.razor

Current (5-row table):
```
Target     [combobox_______]
Distance   [0][1][2]...[50]
Modifier   [-2][-1][0][+1][+2]
Direction  [Front][Left][Right][Rear] (conditional)
Cover      [None][Partial][Full] (conditional)
```

Proposed:
```
[Target [combobox_____________]]           ← full row
[Distance [picker]] [Modifier [radio]]     ← both compact, same row
[Direction [radio]] [Cover [radio]]        ← both conditional, same row (wrap if too wide)
```

#### FormDamageInstance.razor

Current (5-row table):
```
Target     [combobox_______]
Damage     [number input]
Clustering [1][2][5]
Direction  [radio] (conditional)
Cover      [radio] (conditional)
```

Proposed:
```
[Target [combobox_______]]                 ← full row
[Damage [input]] [Clustering [1][2][5]]    ← same row
[Direction [radio]] [Cover [radio]]        ← conditionals, same row
[Execute button]
```

#### ComponentHeatAmmoEstimate.razor

Currently uses a `<table>` with single-column rows just for stacking. Replace with `<div>` elements directly — no table needed at all.

#### FormWeaponBay.razor / FormWeaponEntry.razor — Keep as `<table>`

The weapon list is genuine tabular data (multiple weapons, same columns: target#, state, amount, modifier, weapon, ammo, buttons). Keep as `<table>` but add an `overflow-x: auto` wrapper div in `FormUnitEntry.razor` around the weapons table section. On mobile, the user scrolls horizontally within that block.

### Phase 6 — Visual Block Clarity
- Add a border or subtle background to `resolver_div_componentcontainer` for unit cards (currently alternating colors only)
- Add `gap` between unit cards in the list
- Add `gap` between unit detail cells
- Ensure accordion sections have consistent padding inside their content area

---

## Files to Change

| File | Changes |
|------|---------|
| `src/BlazorServer/wwwroot/css/site.css` | Remove ~90% dead boilerplate |
| `src/BlazorServer/wwwroot/css/Resolver.css` | Fix bugs, modernize layout classes, add breakpoints, add new form section classes |
| `src/BlazorServer/Shared/FormUnitEntry.razor` | Replace two layout tables (static data + unit parameters) with flex form sections; wrap weapon table in overflow-x:auto div |
| `src/BlazorServer/Shared/FormFiringSolution.razor` | Replace layout table with flex form section |
| `src/BlazorServer/Shared/FormDamageInstance.razor` | Replace layout table with flex form section |
| `src/BlazorServer/Shared/ComponentHeatAmmoEstimate.razor` | Replace single-column table with plain divs |
