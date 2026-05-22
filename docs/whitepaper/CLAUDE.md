# `docs/whitepaper/` — Maintenance guide

This folder hosts the static port of <https://gold.nine-chronicles.com/> — the
public NCG token whitepaper. It exists because NCG is listed on multiple
exchanges and the project has a disclosure obligation.

The page is rarely read but **must stay accurate and stable**. Treat changes
here with the same care as a press release.

---

## Files

```
docs/whitepaper/
  index.html        Source of truth. Hand-edited.
  styles.css        Hand-edited. Tokens at the top under :root.
  robots.txt        Allow-list including AI crawlers (GPTBot, ClaudeBot, etc).
  sitemap.xml       Single page + section anchors.
  assets/
    icons/          NCG / WNCG token icons. PNG, kept lossless. Exchanges
                    hot-link these — never rename or recompress.
    diagrams/       hero, blockchain-network, ncg-economics. WebP.
                    hero ships in 3 sizes (380/720/1440) for srcset.
    screenshots/    In-game screenshots. WebP.
  README.md         Public-facing docs (publishing, hosting, etc).
  CLAUDE.md         You are here.
```

There is **no build step**. Open `index.html` directly, or:

```sh
cd docs/whitepaper && python3 -m http.server 4811
```

---

## Editing rules

### Treat the page as a frozen artifact

- **Do not rewrite copy** without an explicit ask. The wording mirrors the
  original Notion whitepaper and is the version exchanges have linked.
- **Do not silently update token numbers, supply schedules, or distribution
  rows.** Those are factual claims with regulatory weight. If a change is
  requested, double-check the source (`assets/` was originally exported from
  Notion; an updated Notion export is the canonical source).
- **Do not "modernize" the entity names.** The footer reads
  `© Nine Chronicles Ltd`. Other legal entities (Planetarium Labs, Nine
  Corporation) were intentionally removed. Don't add them back even if you
  see them in CoinDesk article titles or commit history.
- **Foundation row in the distribution table** says
  "Nine Chronicles Ltd team allocation". The original whitepaper said
  "Planetarium Labs & Nine Corporation team" — that was changed deliberately
  to consolidate the legal entity. Don't revert.

### Style is locked

- Design references: `nine-chronicles.com` (gold `#ffc533` / `#cd9128` accent)
  and `nine-corporation.com` (warm-black `#0a0908` / `#141210` background,
  cream `#f4f1ec` text). Keep this paired palette — both the dark and the
  light theme are derived from it.
- Fonts: **Inter** (display + body) and **JetBrains Mono** (numbers, eyebrow
  labels). Loaded from Google Fonts via the preload + onload swap pattern so
  they don't render-block. Don't switch to a different family.
- Card-style links (`.resources a`, `.articles a`, `.screenshots a`,
  `.brand-asset a`) carry `class="nolink-underline"` so the body-link
  underline rule doesn't apply. New card-style links should add the same
  class; new prose links should not.

### Distribution table specifics

- Each `<tr class="row-summary">` pairs with a `<tr class="row-detail" id="d-…">`.
  The toggle lives on a real `<button class="row-toggle">` in the first cell —
  ARIA forbids `aria-expanded`/`aria-controls` on a `<tr>`, so don't move it
  back to the row.
- The Category column is hidden on mobile via `col.col-category { display:none }`
  + `td.col-category-cell { display:none }`. The pill is shown inline next to
  the row name through a duplicated `.category--mobile` span. When you add a
  row, add **both** the category cell and the mobile chip.
- The TOTAL row uses the same horizontal padding as the data rows so the
  right edges line up — do not give it extra padding "for breathing room",
  it'll desynchronize the % column edge.
- `<table>` uses `table-layout: fixed` with explicit `<colgroup>` widths.
  If you add or remove a column, update the colgroup percentages **and** the
  720px-breakpoint override.

### Images

- All `<img>` tags must have explicit `width` and `height` attributes (CLS).
- Use WebP for photos and diagrams. Keep PNG only for the token icons.
- The hero image has a `srcset` (380/720/1440). If you replace it, regenerate
  all three with `cwebp -resize <w> 0 src.webp -o hero-<w>.webp`.
- Token icons in `assets/icons/` are referenced externally — keep the
  filenames stable: `ncg-color.png`, `ncg-grayscale.png`, `ncg-white.png`,
  same for `wncg-*.png`.

### Accessibility / SEO baseline

The page hits Lighthouse `a11y 96 / bp 100 / seo 100` mobile and desktop.
Don't regress these. In particular:

- Lists of "stat cards" / "metric cards" are `<ul>` with `role="list"` and
  `list-style: none`. Do not switch them to `<dl>` again — Lighthouse
  flagged `<dl>` containing wrappers as a structure violation.
- Body-prose links must be visually distinguishable beyond colour
  (underline). The `.content a:not(.nolink-underline)` rule handles this
  globally.
- Card thumbnails (`.screenshots a img`) need an `alt` describing the
  screen, not "screenshot".

---

## What "publishing" means here

GitHub Pages serves this from `/docs/whitepaper/` (Settings → Pages →
Branch + folder). The custom domain `gold.nine-chronicles.com` is mapped via
a `CNAME` file at the folder root if/when we cut over from Notion.

There is no CI. A push to the configured branch deploys within ~1 minute.

If a deploy is needed off GitHub Pages, `npx vercel deploy --prod` from this
folder works without configuration.

---

## What NOT to do here

- Don't introduce a build step (Astro, Next.js, Vite, etc). The user
  explicitly chose hand-edited HTML/CSS so changes can be made directly.
- Don't add JS frameworks. The current `<script>` blocks are vanilla and
  ~50 lines total — keep it that way.
- Don't add analytics, ads, social embeds, comment widgets, popups.
- Don't add a CSS minify step. The 7KB savings are dwarfed by the loss of
  editability for a page that updates ~yearly.
- Don't auto-translate the page. Korean/JP/CN press links stay in their
  source language; the body copy stays English-only by decision.
- Don't reach into the parent `NineChronicles/` repo for assets or scripts.
  This folder is self-contained on purpose.

---

## When in doubt

Ask before changing token numbers, supply schedules, lockup terms, the
contract address (`0xf203Ca1769ca8e9e8FE1DA9D147DB68B6c919817`), or anything
in the `<script type="application/ld+json">` graph — those are the bits an
exchange or aggregator might consume programmatically.
