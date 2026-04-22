import type { Issue, IssuesData, ShootoutGroup } from './types.ts';

const escape = (s: string): string =>
  s.replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]!));

const shortModel = (model: string | null): string => {
  if (!model) return 'unknown';
  return model.replace(/^ollama\d*-/, '').replace(/^claude-/, '');
};

const modelBadgeClass = (model: string | null): string => {
  if (!model) return '';
  if (model.startsWith('claude-') || model.startsWith('anthropic-')) return 'cloud';
  if (model.startsWith('ollama')) return 'local';
  return '';
};

// Lazy-GIF frame: ship a tiny 1×1 transparent placeholder in `src`, stash the
// real URL in `data-src`. An IntersectionObserver in main.ts swaps `src` when
// the image enters the viewport and wipes it back out when it leaves, so only
// the handful of currently-visible GIFs are decoded/animated at any time.
// Browsers keep every animated GIF running in a hidden buffer, so without this
// 500+ GIFs absolutely crush scroll performance.
const BLANK_PX =
  'data:image/gif;base64,R0lGODlhAQABAAAAACH5BAEKAAEALAAAAAABAAEAAAICTAEAOw==';
const frame = (url: string, alt: string) =>
  `<div class="frame"><img class="lazy-gif" src="${BLANK_PX}" data-src="${escape(url)}" alt="${escape(alt)}" loading="lazy" decoding="async" /></div>`;

const pendingFrame = (label = 'TODO') =>
  `<div class="frame"><div class="pending">${escape(label)}</div></div>`;

/**
 * Shootout card: columns-per-model layout.
 *
 * Structure (CSS grid, N columns = N models):
 *   - Header row: model badges
 *   - Iter rows 1..autoCount   (auto-iters)
 *   - Feedback banner (full-width row, spans all columns): "💬 Feedback: ..."
 *   - Iter row autoCount+1
 *   - ...repeats per feedback round
 */
const shootoutCard = (g: ShootoutGroup): string => {
  const firstIssueUrl = g.variants[0]?.url ?? 'https://github.com/SchlenkR/pxl-clock/issues';

  // Flatten iterations with their actual generating model (from the Implementor
  // comment's Config Set marker). Rows = iteration index; columns = distinct
  // models. Cell (model, iter) = that specific GIF. If multiple issues in the
  // cluster have the same (model, iter), we keep the one with the highest
  // issue number (most recent).
  type IterRef = { gifUrl: string; issueUrl: string; issueTitle: string; issueNumber: number; iterIndex: number };
  const cellMap = new Map<string, IterRef>();  // key = `${model}|${iterIndex}`
  const modelSet = new Set<string>();
  const iterSet = new Set<number>();
  for (const issue of g.variants) {
    for (const it of issue.iterations) {
      const model = it.model ?? issue.model ?? 'unknown';
      modelSet.add(model);
      iterSet.add(it.index);
      const key = `${model}|${it.index}`;
      const existing = cellMap.get(key);
      if (!existing || issue.number > existing.issueNumber) {
        cellMap.set(key, {
          gifUrl: it.gifUrl,
          issueUrl: issue.url,
          issueTitle: issue.cleanTitle,
          issueNumber: issue.number,
          iterIndex: it.index,
        });
      }
    }
  }

  const modelKeys = [...modelSet].sort();
  const iterKeys = [...iterSet].sort((a, b) => a - b);
  const colCount = modelKeys.length;
  const styleAttr = `style="--col-count:${colCount}"`;

  // Column header: one toggle button per distinct model. Click collapses
  // the column to a narrow circle; click again expands. The model name is
  // stashed in data-model so the JS can find all cells belonging to the
  // column and recompute the row's grid-template-columns.
  const headRow = `
    <div class="shootout-headrow" ${styleAttr}>
      ${modelKeys
        .map(
          (m) => `
        <button type="button" class="col-head col-toggle badge ${modelBadgeClass(m)}" data-model="${escape(m)}" title="Click to collapse/expand column">
          <span class="col-head-label">${escape(shortModel(m))}</span>
        </button>`,
        )
        .join('')}
    </div>`;

  const rows: string[] = [];
  for (const n of iterKeys) {
    // Insert the full-width feedback banner immediately before the iteration
    // it triggered. Feedback rounds are collected across variants during fetch.
    const round = g.feedbackRounds.find((r) => r.iterIndex === n);
    if (round) {
      rows.push(`
        <div class="feedback-bar">
          <span class="feedback-label">💬 Feedback for iter ${round.iterIndex}</span>
          <p class="feedback-text">${escape(round.text)}</p>
        </div>`);
    }

    const cells = modelKeys
      .map((m) => {
        const ref = cellMap.get(`${m}|${n}`);
        if (!ref) return `
          <a class="cell empty" aria-hidden="true" data-model="${escape(m)}">
            ${pendingFrame('not rendered')}
            <span class="cell-label muted">iter ${n}</span>
          </a>`;
        return `
          <a class="cell" href="${escape(ref.issueUrl)}" target="_blank" rel="noopener" title="#${ref.issueNumber} · iter ${ref.iterIndex}" data-model="${escape(m)}">
            ${frame(ref.gifUrl, `${ref.issueTitle} iter ${ref.iterIndex} (${shortModel(m)})`)}
            <span class="cell-label">iter ${ref.iterIndex}</span>
          </a>`;
      })
      .join('');
    rows.push(`<div class="shootout-row" ${styleAttr}>${cells}</div>`);
  }

  const totalIters = cellMap.size;

  // Panel view: one horizontal film-strip per model with all its iterations.
  // No feedback bars, no model-as-column grid — just the renders side by side.
  const iterCountStyle = `style="--iter-count:${iterKeys.length}"`;
  const panelRows = modelKeys
    .map((m) => {
      const firstRef = iterKeys.map((n) => cellMap.get(`${m}|${n}`)).find((r): r is IterRef => r !== undefined);
      const headUrl = firstRef?.issueUrl ?? g.variants[0]?.url ?? '#';
      const cells = iterKeys
        .map((n) => {
          const ref = cellMap.get(`${m}|${n}`);
          if (!ref) return `
            <a class="cell empty" aria-hidden="true">
              ${pendingFrame('—')}
              <span class="cell-label muted">iter ${n}</span>
            </a>`;
          return `
            <a class="cell" href="${escape(ref.issueUrl)}" target="_blank" rel="noopener" title="#${ref.issueNumber} · iter ${ref.iterIndex}">
              ${frame(ref.gifUrl, `${ref.issueTitle} iter ${ref.iterIndex} (${shortModel(m)})`)}
              <span class="cell-label">iter ${ref.iterIndex}</span>
            </a>`;
        })
        .join('');
      return `
        <div class="panel-row" data-model="${escape(m)}">
          <header class="panel-row-head">
            <a class="badge ${modelBadgeClass(m)}" href="${escape(headUrl)}" target="_blank" rel="noopener">${escape(shortModel(m))}</a>
          </header>
          <div class="panel-row-strip" ${iterCountStyle}>${cells}</div>
        </div>`;
    })
    .join('');

  return `
    <article class="shootout">
      <header class="shootout-head">
        <h3><a class="title-link" href="${escape(firstIssueUrl)}" target="_blank" rel="noopener">${escape(g.cleanTitle)}</a></h3>
        <p class="desc">${escape(g.description)}</p>
        <div class="stats">
          <span class="badge ok">${modelKeys.length} models</span>
          <span class="badge">${totalIters} iterations</span>
          <span class="badge">${g.variants.length} issue${g.variants.length === 1 ? '' : 's'}</span>
        </div>
      </header>
      <div class="shootout-body view-only-grid" data-scroller>
        <div class="head-strip">
          <button class="scroll-chev prev" type="button" aria-label="Scroll left">‹</button>
          <div class="head-clip">
            ${headRow}
          </div>
          <button class="scroll-chev next" type="button" aria-label="Scroll right">›</button>
        </div>
        <div class="shootout-viewport">
          ${rows.join('')}
        </div>
      </div>
      <div class="shootout-body view-only-panel">
        ${panelRows}
      </div>
    </article>`;
};

/**
 * Single (non-shootout) issue card.
 *
 * Layout: one horizontal CSS grid of equal-width columns, one column per
 * iteration. Auto-iters are plain [GIF] cells. Feedback-iters are taller —
 * same column but with the triggering comment stacked above the GIF. All
 * GIFs align at the bottom of the row so the eye scans a clean baseline.
 *
 * Comment is clamped to 3 lines by default; hovering expands it in-place
 * (elevated z-index, soft background — no modal).
 */
const singleCard = (i: Issue): string => {
  if (i.iterations.length === 0) return '';

  const cells = i.iterations
    .slice()
    .sort((a, b) => a.index - b.index)
    .map((it) => {
      const isFeedback = it.index > i.autoCount;
      const fb = isFeedback ? i.feedback.find((f) => f.iterIndex === it.index) : null;
      const commentHtml = fb
        ? `<div class="iter-comment" data-full="${escape(fb.body)}" data-author="@${escape(fb.author)}" tabindex="0"><span class="iter-comment-label">💬 @${escape(fb.author)}</span> ${escape(fb.body)}</div>`
        : isFeedback
          ? `<div class="iter-comment unknown" data-full="(no triggering comment found)" tabindex="0"><span class="iter-comment-label">💬 ?</span> (no triggering comment found)</div>`
          : '';
      return `
        <div class="iter-cell${isFeedback ? ' with-comment' : ''}">
          ${commentHtml}
          <a class="iter-art" href="${escape(i.url)}" target="_blank" rel="noopener" title="#${i.number} iter ${it.index}">
            ${frame(it.gifUrl, `${i.cleanTitle} iter ${it.index}`)}
            <span class="cell-label">iter ${it.index}</span>
          </a>
        </div>`;
    })
    .join('');

  return `
    <article class="idea-card"${i.model ? ` data-model="${escape(i.model)}"` : ''}>
      <header class="idea-card-head">
        <h3><a class="title-link" href="${escape(i.url)}" target="_blank" rel="noopener">${escape(i.cleanTitle)}</a></h3>
        <div class="idea-card-sub">
          <span class="num">#${i.number}</span>
          ${i.model ? `<span class="badge ${modelBadgeClass(i.model)}">${escape(shortModel(i.model))}</span>` : ''}
          <span class="iter-count">${i.iterations.length} iter</span>
        </div>
      </header>
      <div class="iter-grid" style="--iter-count:${i.iterations.length}">${cells}</div>
    </article>`;
};

const submitUrl =
  'https://github.com/SchlenkR/pxl-clock/issues/new?labels=pixogram-idea&body=Beschreib%20deine%20Pixogram-Idee%20hier...%0A%0AMach%203%20Iterationen.';

export function render(data: IssuesData): string {
  const builtAt = new Date(data.generatedAt).toISOString().slice(0, 19).replace('T', ' ');
  const totalIters = data.issues.reduce((n, i) => n + i.iterations.length, 0);
  const renderableSingles = data.singles.filter((i) => i.iterations.length > 0);

  // All distinct models across all iterations (shootout issues + singles) —
  // used to build the global sticky filter row (one toggle chip per model).
  const allModels = [
    ...new Set([
      ...data.issues.flatMap((i) => i.iterations.map((it) => it.model ?? i.model ?? 'unknown')),
      ...data.singles.flatMap((i) => i.iterations.map((it) => it.model ?? i.model ?? 'unknown')),
    ]),
  ].sort();

  return `
  <div class="hero-scene" aria-hidden="true"></div>
  <header class="hero">
    <div class="container hero-inner">
      <h1>Program <img class="hero-logo-inline" src="logo.svg" alt="PXL" /> in<br/><span class="accent-csharp">C#</span> or with <span class="accent-ai">AI</span>.</h1>
      <div class="hero-copy">
        <p class="hero-lead primary">
          PXL Clock is a 27×27&nbsp;cm LED frame for your shelf. You write the animations yourself in <span class="accent-csharp">C#</span> — or you describe what you want and let an <span class="accent-ai">AI</span> do it.
        </p>
        <p class="hero-lead secondary">
          The <span class="accent-ai">AI</span> part lives on <a href="https://github.com/SchlenkR/pxl-clock">GitHub</a>. Open an issue with your idea, an agent picks it up, runs it through several models, and posts back what each of them made. Everything below is what came out — same prompt, model by model.
        </p>
      </div>
      <div class="ctas">
        <a class="btn" href="https://www.pxlclock.com/?ref=RONALD">Get the real clock →</a>
        <a class="btn secondary" href="${escape(submitUrl)}">Submit an idea</a>
        <a class="btn secondary" href="https://discord.gg/KDbVdKQh5j">Discord</a>
      </div>
    </div>
  </header>

  <div class="zoom-bar" data-zoom-bar>
    <button type="button" class="zoom-btn" data-zoom-in aria-label="Larger">+</button>
    <span class="zoom-label"><span data-zoom-level>3</span>/4</span>
    <button type="button" class="zoom-btn" data-zoom-out aria-label="Smaller">−</button>
    <hr class="zoom-divider" />
    <button type="button" class="view-btn" data-view="grid" aria-label="Grid view (models as columns, iterations as rows)" title="Grid view">▦</button>
    <button type="button" class="view-btn" data-view="panel" aria-label="Panel view (one row per model, all iterations as a strip)" title="Panel view">≡</button>
    <button type="button" class="view-btn" data-view="flat" aria-label="Flat view (all pixograms, newest first)" title="Flat view">▤</button>
  </div>

  <div class="global-filter">
    <div class="container">
      <div class="flat-filter" role="group" aria-label="Filter by model">
        <span class="flat-filter-label">Models</span>
        ${allModels
          .map(
            (m) => `
          <button type="button" class="badge model-toggle ${modelBadgeClass(m)} active" data-model="${escape(m)}">
            ${escape(shortModel(m))}
          </button>`,
          )
          .join('')}
      </div>
    </div>
  </div>

  <section id="flat" class="view-only-flat">
    <div class="container">
      <div class="flat-grid">
        ${data.issues
          .flatMap((issue) =>
            issue.iterations.map((it) => ({ issue, it, sortKey: `${issue.updatedAt}|${String(it.index).padStart(4, '0')}` }))
          )
          .sort((a, b) => b.sortKey.localeCompare(a.sortKey))
          .map(({ issue, it }) => {
            const model = it.model ?? issue.model ?? 'unknown';
            return `<a class="cell flat-cell" href="${escape(issue.url)}" target="_blank" rel="noopener" data-model="${escape(model)}">
              ${frame(it.gifUrl, `${issue.cleanTitle} iter ${it.index}`)}
              <div class="cell-meta">
                <span class="cell-meta-row">
                  <span class="cell-num">#${issue.number} · iter ${it.index}</span>
                  <span class="badge ${modelBadgeClass(model)}">${escape(shortModel(model))}</span>
                </span>
                <p class="cell-desc">${escape(issue.cleanTitle)}</p>
              </div>
              <div class="cell-tip" role="tooltip">
                <span class="cell-tip-title">${escape(issue.cleanTitle)}</span>
                <span class="cell-tip-meta">
                  <span class="badge ${modelBadgeClass(model)}">${escape(shortModel(model))}</span>
                  <span class="cell-tip-num">#${issue.number} · iter ${it.index}</span>
                </span>
              </div>
            </a>`;
          })
          .join('')}
      </div>
    </div>
  </section>

  <section id="shootout">
    <div class="container">
      ${
        data.shootouts.length === 0
          ? '<p class="section-empty">Shootout data still being generated — check back soon.</p>'
          : data.shootouts.map(shootoutCard).join('')
      }
    </div>
  </section>

  <section id="ideas">
    <div class="container">
      <div class="section-head">
        <span class="kicker">✨ community ideas</span>
        <h2>Every other pixogram the pipeline rendered.</h2>
        <p class="lead">
          Anyone can file a GitHub issue with a pixogram idea. The bot picks it up, produces the
          requested number of iterations, and any follow-up comment triggers one more iteration
          driven by that feedback.
        </p>
      </div>
      <div class="ideas-list">${renderableSingles.map(singleCard).join('')}</div>
      <div class="submit-cta">
        <a class="btn" href="${escape(submitUrl)}">Submit your own →</a>
      </div>
    </div>
  </section>

  <footer>
    <div class="container footer-inner">
      <div>
        <span class="pixel-mini">PXL CLOCK</span>
        <p class="muted">Built by Cumin &amp; Potato GmbH. Pixograms generated by the open-source pipeline in the repo.</p>
      </div>
      <div class="footer-links">
        <a href="https://www.pxlclock.com/?ref=RONALD">Shop</a>
        <a href="https://github.com/SchlenkR/pxl-clock">GitHub</a>
        <a href="https://discord.gg/KDbVdKQh5j">Discord</a>
      </div>
      <div class="muted small">
        Last build: <time datetime="${escape(data.generatedAt)}">${escape(builtAt)} UTC</time>
        · ${totalIters} pixograms rendered
      </div>
    </div>
  </footer>`;
}
