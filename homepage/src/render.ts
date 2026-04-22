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

const frame = (url: string, alt: string) =>
  `<div class="frame"><img src="${escape(url)}" alt="${escape(alt)}" loading="lazy" /></div>`;

const pendingFrame = (label = 'TODO') =>
  `<div class="frame"><div class="pending">${escape(label)}</div></div>`;

/**
 * One cell in the shootout grid or ideas grid — a single iteration GIF.
 * Wrapped in a link to the issue. Pending cells have no link.
 */
const iterCell = (issue: Issue, iterIndex: number): string => {
  const it = issue.iterations.find((i) => i.index === iterIndex);
  const label = iterIndex.toString().padStart(3, '0');
  if (it) {
    return `
      <a class="cell" href="${escape(issue.url)}" target="_blank" rel="noopener" title="#${issue.number} iter ${iterIndex}">
        ${frame(it.gifUrl, `${issue.cleanTitle} iter ${iterIndex}`)}
        <span class="cell-label">iter ${iterIndex}</span>
      </a>`;
  }
  return `
    <div class="cell pending-cell">
      ${pendingFrame('TODO')}
      <span class="cell-label muted">iter ${iterIndex}</span>
    </div>`;
};

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
  const colCount = g.variants.length;
  const firstIssueUrl = g.variants[0]?.url ?? 'https://github.com/SchlenkR/pxl-clock/issues';

  const styleAttr = `style="--col-count:${colCount}"`;

  // Header row: model badges (clickable → issue)
  const headRow = `
    <div class="shootout-headrow" ${styleAttr}>
      ${g.variants
        .map(
          (v) => `
        <a class="col-head badge ${modelBadgeClass(v.model)}" href="${escape(v.url)}" target="_blank" rel="noopener" title="Issue #${v.number}">
          ${escape(shortModel(v.model))}
        </a>`,
        )
        .join('')}
    </div>`;

  // Helper: one iter-row (grid of variant cells for the given iter index)
  const row = (idx: number) => `
    <div class="shootout-row" ${styleAttr}>
      ${g.variants.map((v) => iterCell(v, idx)).join('')}
    </div>`;

  const blocks: string[] = [headRow];

  // Auto-iter rows
  for (let i = 1; i <= g.autoCount; i++) blocks.push(row(i));

  // Feedback banners full-width, followed by that round's iter row
  for (const rnd of g.feedbackRounds) {
    blocks.push(`
      <div class="feedback-bar">
        <span class="feedback-label">💬 Feedback for iter ${rnd.iterIndex}</span>
        <p class="feedback-text">${escape(rnd.text)}</p>
      </div>`);
    blocks.push(row(rnd.iterIndex));
  }

  // Any iterations beyond our known rounds (user-bumped) get a minimal banner
  const accountedFor = new Set<number>([
    ...Array.from({ length: g.autoCount }, (_, k) => k + 1),
    ...g.feedbackRounds.map((r) => r.iterIndex),
  ]);
  const extraIters = Array.from(
    new Set(g.variants.flatMap((v) => v.iterations.map((it) => it.index))),
  )
    .filter((idx) => !accountedFor.has(idx))
    .sort((a, b) => a - b);
  for (const idx of extraIters) {
    blocks.push(`
      <div class="feedback-bar extra">
        <span class="feedback-label">↳ iter ${idx}</span>
      </div>`);
    blocks.push(row(idx));
  }

  return `
    <article class="shootout">
      <header class="shootout-head">
        <h3><a class="title-link" href="${escape(firstIssueUrl)}" target="_blank" rel="noopener">${escape(g.cleanTitle)}</a></h3>
        <p class="desc">${escape(g.description)}</p>
        <div class="stats">
          <span class="badge ok">${g.variants.length} models</span>
          <span class="badge">${g.variants.reduce((n, v) => n + v.iterations.length, 0)} iterations</span>
          <span class="badge">${g.autoCount} auto · ${g.feedbackRounds.length} feedback</span>
        </div>
      </header>
      <div class="shootout-body">
        ${blocks.join('')}
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
    <article class="idea-card">
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
  const backdropTiles = data.issues
    .filter((i) => i.iterations.length > 0)
    .slice()
    .sort(() => Math.random() - 0.5)
    .slice(0, 18)
    .map((i) => {
      const g = i.iterations.at(-1)!;
      return `<img src="${escape(g.gifUrl)}" alt="" loading="lazy" />`;
    })
    .join('');

  const builtAt = new Date(data.generatedAt).toISOString().slice(0, 19).replace('T', ' ');
  const totalIters = data.issues.reduce((n, i) => n + i.iterations.length, 0);
  const renderableSingles = data.singles.filter((i) => i.iterations.length > 0);

  return `
  <header class="hero">
    <div class="hero-backdrop" aria-hidden="true">${backdropTiles}</div>
    <div class="container hero-inner">
      <div class="hero-row">
        <img class="hero-logo" src="logo.svg" alt="PXL" width="150" height="50" />
        <div class="hero-text">
          <p class="kicker">24×24 LEDs · glass · wood · programmable in C#</p>
          <h1>Same prompt. Seven AI models. <span class="accent">One pixel canvas.</span></h1>
        </div>
      </div>
      <div class="ctas">
        <a class="btn" href="https://www.pxlclock.com/?ref=RONALD">Get the clock →</a>
        <a class="btn secondary" href="${escape(submitUrl)}">Submit an idea</a>
        <a class="btn secondary" href="https://discord.gg/KDbVdKQh5j">Discord</a>
      </div>
    </div>
  </header>

  <section id="shootout">
    <div class="container">
      <div class="section-head">
        <span class="kicker">🤖 shootout</span>
        <h2>Same prompt. Seven different AI models.</h2>
        <p class="lead">
          We asked seven language models to turn the same description into a 24×24 animation.
          Each column is one model, each row one iteration. The two feedback comments are exactly
          the same for every model. Click any tile (or model badge) for the full issue thread.
        </p>
      </div>
      ${
        data.shootouts.length === 0
          ? '<p class="empty">Shootout data still being generated — check back soon.</p>'
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
