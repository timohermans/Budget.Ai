## Context

The overview page (`Features/Budget/OverviewController.cs` → `SummaryCalculator.Calculate` → `Views/Overview/Index.cshtml`) is fully URL-driven: weeks and IBAN sections expand via route segments (`/budget/{year}/{month}/{weekOrIban}`), htmx does full-body swaps with `hx-push-url` on every interaction. Both the week lists and the IBAN lists inherit their transaction order from a single ordering: `SummaryCalculator`'s `window` sort (currently date desc, then name desc) buckets transactions in iteration order. `WeekSummary.Transactions` and `BalanceSummary.Transactions` are get-only `List<TransactionTemplateModel>` that can be sorted in place. `Program.cs` forces InvariantCulture.

## Goals / Non-Goals

**Goals:**

- Sort order lives entirely in the URL (`?sort=`) so htmx navigation, bookmarks, and shared links all behave identically
- One sort application point covering both week and IBAN transaction lists
- Zero impact on the domain types (`Summary`, `WeekSummary`, `BalanceSummary`) and the fixed-transaction detail card

**Non-Goals:**

- Sorting the fixed income/expense detail card (`_FixedTransactions`)
- Client-side or persisted-in-localStorage sorting
- Sorting the week cards or IBAN cards themselves (calendar buckets keep their order)
- Any persistence beyond the URL (no cookie, no per-user setting)

## Decisions

### Sort state in the URL, applied server-side

The page already treats the URL as the single source of truth (route segments drive expansion, `hx-push-url` everywhere). A `?sort=` query parameter fits that model: the dropdown does an `hx-get` on change, the controller re-renders, htmx swaps the body and pushes the URL. Alternatives rejected:

- *Alpine DOM reordering*: instant, but only affects the currently rendered list, silently resets on any htmx navigation, and needs fragile re-application after every swap.
- *localStorage + client re-sort*: splits the source of truth, must be re-applied per user per browser, invisible in shared URLs.

### Sort applied in the controller after `SummaryCalculator.Calculate`

The `OverviewSort` enum and its parsing live in `Features/Budget` (vertical slice — sorting is an overview concern, not domain). After `Calculate` returns, the controller re-sorts `summary.Weeks.Values` and `summary.IbanBalances.Values` transaction lists in place (get-only lists, so `List.Sort(comparer)`). Alternatives rejected:

- *Threading sort into `Calculate`'s window ordering*: one sort site, but drags a Features-level enum into Domain and changes a stable signature; the post-sort iterates a handful of small lists, which is trivially cheap.
- *Sorting in the Razor view*: view logic stays declarative; comparison semantics don't belong there.

Both week and IBAN lists get the identical comparer, so behavior stays consistent between sections.

### Sort key definitions (all tie-break on date descending)

| Sort | Key |
|---|---|
| `datum (eind - begin)` (default) | `Date desc`, then displayed name desc (unchanged current behavior) |
| `datum (begin - eind)` | `Date asc` |
| `hoogste uitgaves` | signed `Amount asc` → `-450, -50, -3.50, +25, +1000`: one monotonic key that yields expenses by size desc, then income by size asc |
| `kleinste uitgaves` | pivot (`Amount < 0 ? 0 : 1`), then `Math.Abs(Amount) asc` → expenses smallest-first, income pinned after, same income tail order as hoogste |
| `winkel (a - z)` / `winkel (z - a)` | displayed name (`DisplayName ?? NameOtherParty`), `StringComparer.CurrentCultureIgnoreCase` (invariant under forced culture, so "Zara" does not sort before "apple") |

Every option appends `ThenByDescending(Date)` for deterministic ties. Amount options are expense-centric by design: the literal signed "hoog - laag" would put income first, which is useless — the user explicitly chose "hoogste uitgaves"/"kleinste uitgaves" semantics with income sinking to the bottom.

### URL threading in the view

The dropdown is a `<select data-testid="sort-select">` whose `hx-get` targets the current page *including the expanded week/IBAN segment* (so changing sort doesn't collapse the expansion) with the new `sort` value, `hx-push-url` on. Default sort renders URLs without the parameter (clean `/budget/...`). Every existing URL construction in `Index.cshtml` that must preserve sort gains `?sort=@Model.Sort` when non-default: month prev/today/next links, week summary `hx-get`, IBAN summary `hx-get`. The fixed income/expense `hx-get`s and the upload form are left untouched per scope.

### Value encoding

`sort` values are URL-friendly kebab strings: `datum-begin-eind`, `hoogste-uitgaves`, `kleinste-uitgaves`, `winkel-a-z`, `winkel-z-a`; missing, empty, or unrecognized values parse to the default (`datum (eind - begin)`). Parsing is a straight `TryParse`-style switch — no exceptions on garbage input.

## Risks / Trade-offs

- [URL duplication across ~7 view URL constructions] → each is an explicit `?sort=@Model.Sort` interpolation in one view; if it grows unwieldy, a small local URL helper inside the view/controller is the escape hatch (still feature-local).
- [Income placement may surprise on first use] → income transactions sort after expenses by explicit user decision; the reverse "income first" option was deliberately rejected as near-useless.
- [Culture-dependent name order] → `Program.cs` forces InvariantCulture, so `CurrentCultureIgnoreCase` is deterministic per deployment; Dutch-specific collation nuances (e.g. ĳ) are accepted as a non-issue for shop names.
- [Select changes trigger GET requests with no antiforgery concern] → read-only GET, same as existing hx-get navigation.

## Migration Plan

No data or schema changes; deploy with the next image build. Rollback = revert the tag bump.