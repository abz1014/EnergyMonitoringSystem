# Sprint 6 — Senior UX Design Review

## Starting point (honest baseline)

Before this sprint: `site.css` was the untouched Visual Studio scaffold (22 lines, zero custom
rules). Every visual decision in the app — color, spacing, radius, shadow — was an inline
`style="..."` attribute, hand-typed per element, repeated thousands of times across 37 pages.
Icons were HTML entities (`&#9888;`, `&#128202;`) rather than a real icon set — inconsistent
glyph coverage and rendering across browsers/fonts. The shared scaffold `Error.cshtml` was
completely unstyled (black text on white), breaking the dark theme the instant any controller's
`try/catch` fired. There was no loading-state pattern, and "empty state" was reinvented per page
(28 pages independently wrote the identical `p-5 text-center` block, but it was never a shared
component, so a future change to it required editing 28 files instead of one).

This is the actual gap against Power BI / Grafana / Schneider EcoStruxure / ABB Ability /
Siemens Energy Manager: those tools all share one property this app didn't have — a **token-based
design system** (fixed type scale, fixed spacing scale, one icon set, one card treatment) that
every screen inherits automatically. Without that, consistency depends on every page author
re-typing the same values correctly forever, which is the actual reason inconsistencies creep in
at scale — not a lack of individual page polish.

## What was built (applies to all 37 pages automatically via `_Layout.cshtml`)

**Typography.** Added Inter (the typeface Grafana, Linear, and most modern data-dense dashboards
use — high x-height, strong numeral legibility at small sizes) plus JetBrains Mono reserved for
tabular metric figures (`.metric-value`/`.kpi-value`) where monospaced tabular numerals make a
column of changing values easier to scan at a glance, the same convention Power BI's card visuals
use. System font stack as fallback so nothing breaks if the CDN is unreachable.

**Spacing & elevation tokens.** `--space-1` through `--space-8`, `--radius-sm/md/lg`,
`--shadow-sm/md/lg` added to `:root`. This is the actual fix for "dashboard density" — a fixed
scale is what lets a reviewer (or future page author) reason about whether something should be
denser or airier, instead of every page guessing its own padding.

**Color.** Background darkened slightly (`#0F172A` → `#0B1220`) and border tokens desaturated
(`#334155` → `#2A3749`) for better contrast against card surfaces — both Grafana's and Power BI's
dark themes use a near-black canvas with a clearly lighter card surface, and the prior values sat
too close together to read as "elevated." Added explicit status-color tokens
(`--status-good/warn/bad/info`) so future pages reference the same semantic colors instead of
typing `#10B981` from memory each time (already a low-grade source of inconsistency — some pages
in this app use `#10B981`, others `#22C55E`, for the same "good" meaning).

**Card hierarchy.** New `.card-elevated` class: consistent surface, border, shadow, and a
restrained hover lift (`translateY(-2px)` + shadow increase) for interactive cards — matching the
drill-down links added in Sprint 3. Purely additive; existing inline-styled cards are unaffected
and keep working.

**Icons.** Replaced all 35 sidebar entries' HTML-entity icons with Bootstrap Icons (CDN,
`bootstrap-icons@1.11.3`) — crisp SVG glyphs at any zoom level instead of font-dependent text
characters, and a real one-to-one icon per page instead of `&#9889;` (lightning bolt) reused for
six unrelated Power Quality pages because it was the closest available entity. Also fixed the
sidebar brand, group-collapse caret, and topbar hamburger/home link to use real icons.

**Loading states.** New `.skeleton` shimmer-animation component, ready for any async section to
use ahead of the eventual SignalR live-data work mentioned for the next phase. Not yet wired into
a specific page in this sprint — see Follow-up below.

**Empty / error states.** New `.state-card` / `.state-empty` / `.state-error` components.
Mechanically swapped into **all 28 pages** that shared the identical empty-state markup (Alarm,
AlarmResponse, Anomaly, Budget, CapacityHeadroom, Carbon, Comparison, CostDashboard, DataQuality,
DemandCurve, DiversityFactor, EquipmentHealth, FloorMap, FlowMonitoring, Forecast, Frequency,
Heatmap, LineVoltageBalance, LoadAnalytics, PowerQuality, Qbr, ReactivePower, Reports, RootCause,
TimeOfUse, TransformerLoading, VoltageImbalance, VoltageRange) — dashed border instead of solid
(visually distinct from a real data card, the same convention Notion/Linear use to distinguish
"nothing here yet" from "here is data"), consistent padding. The shared scaffold `Error.cshtml`
was completely rebuilt: it previously had zero styling and broke the dark theme on any unhandled
exception; it now matches the rest of the app and gives the user a way back (Go Back / Dashboard
buttons) instead of a dead end.

**Animations.** One restrained page-content fade-in-up (`0.28s`, custom ease curve) on every
page load, respecting `prefers-reduced-motion`. Card hover lift on `.card-elevated`. No
decorative animation beyond this — matches the brief's "do not redesign for aesthetics alone."

**Responsiveness.** Already substantially handled before this sprint (mobile sidebar collapse,
touch target sizing, table horizontal scroll, chart height caps — confirmed present in
`_Layout.cshtml`'s existing media queries). No regressions introduced; not re-litigated.

**Tables & forms (global, via `site.css`).** Dark-themed scrollbar (every page, was previously a
light OS-default scrollbar against a near-black canvas). Table row hover highlight and clearer
border contrast on every `.table-dark` instance app-wide. `.form-select`/`.form-control` given a
correct dark-theme default so a page that forgot to inline-override them no longer flashes white.

## Verification

Clean rebuild, 0 errors. Full route sweep: 37/37 controllers healthy. 51/51 tests pass. All
changes are additive CSS/markup-class changes (no controller logic touched), so the existing
4 prior sprints' verified calculations are unaffected.

## Honest follow-up list (not done in this sprint)

The token system and icon set apply everywhere automatically, but most pages' individual KPI
cards, charts containers, and inline `style="background-color: #1E293B; border-radius: 8px;"`
blocks have **not** been migrated to `.card-elevated` — that's thousands of individual inline
style attributes across 37 files, and doing it correctly (verifying each page's specific layout
isn't broken by the swap) is a multi-sprint effort, not something to rush through in one pass.
What exists today is a real design system that new pages should build on immediately (LoadAnalytics
already does, since it was built in Sprint 4) and that old pages can be migrated into
incrementally without anything breaking in the meantime, since the new classes are purely additive.

Recommended order for that follow-up, highest-traffic first: Dashboard KPI cards → Briefing cards
(already has its own `.bp-card`, worth reconciling with `.card-elevated` rather than keeping two
near-identical systems) → LiveMonitoring → the Power Quality / Financial page groups.
