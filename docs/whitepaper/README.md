# Nine Chronicles Gold — Whitepaper site

Static, single-page HTML port of <https://gold.nine-chronicles.com/>.

## Files

```
docs/whitepaper/
  index.html          ← edit content here
  styles.css          ← edit visual design here
  assets/
    icons/            ← NCG / WNCG token icons (PNG, kept lossless for exchange use)
    diagrams/         ← hero, blockchain network, economics (WebP)
    screenshots/      ← in-game screenshots (WebP)
```

There is no build step. Just open `index.html` in a browser, or serve the folder:

```sh
cd docs/whitepaper
python3 -m http.server 4811
# → http://127.0.0.1:4811/
```

## Editing

- All copy lives in `index.html` between the labelled `<section>` blocks (`§ 01` … `§ 10`).
- The distribution table is plain HTML — to change a row, edit the `<tr class="row-summary">` and its paired `<tr class="row-detail">`.
- Theme tokens (colors, fonts, spacing) are at the top of `styles.css` under `:root` (dark) and `[data-theme="light"]` (light).

## Publishing on GitHub Pages

This site is intended to be served from the `docs/whitepaper/` subfolder of a GitHub repository.

GitHub Pages serves `docs/` (root of the docs folder), so to keep this site at `/whitepaper/` you have two options:

### Option A — Serve from `docs/whitepaper/` as the Pages source

If the repo is dedicated to the whitepaper:

1. **Settings → Pages**
2. Source: **Deploy from a branch**
3. Branch: `main` (or `development`), folder: **`/docs/whitepaper`**
4. Save. Wait ~1 min for the first build.

### Option B — Serve from `docs/` and treat whitepaper as a subpath

If `docs/` already hosts other content, set Pages source to `/docs` and the whitepaper will be reachable at `<your-pages-url>/whitepaper/`.

### Option C — Custom domain (`gold.nine-chronicles.com`)

To point the existing custom domain at GitHub Pages:

1. Add a file `CNAME` next to `index.html` containing exactly: `gold.nine-chronicles.com`
2. **Settings → Pages → Custom domain** → enter `gold.nine-chronicles.com` and save.
3. At the DNS provider (e.g. Cloudflare), change the existing record:
   - Remove the current Notion/oopy CNAME.
   - Add `CNAME` `gold` → `<github-org>.github.io.` (proxied off if Cloudflare; HTTPS will be issued by GitHub).
4. Re-check **Settings → Pages**, tick **Enforce HTTPS** once the cert is issued (a few minutes).

## Publishing on Vercel (alternative)

If you ever switch off GitHub Pages, Vercel will serve this folder out of the box:

```sh
cd docs/whitepaper
npx vercel deploy --prod
```

No `vercel.json` is required — there are no rewrites or build steps.

## Asset hygiene

- Token icons are kept as **PNG** in `assets/icons/`. Exchanges and aggregators frequently
  hot-link or download these files, so we want long-term stable filenames and lossless quality.
- Game screenshots and diagrams were converted to **WebP** (q≈82–90) to keep the page light
  (~1 MB total assets vs. 8.5 MB originals) without visible quality loss.
- If you replace any image, keep the same filename to avoid breaking external references.

## License & attribution

Content © Planetarium Labs / Nine Corporation. The HTML/CSS structure of this site is the
project's own work — no third-party JavaScript, no analytics, no external runtime.
