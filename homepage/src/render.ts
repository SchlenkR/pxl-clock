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

  // Column header: one badge per distinct model. Click → first issue that used it.
  const headRow = `
    <div class="shootout-headrow" ${styleAttr}>
      ${modelKeys
        .map((m) => {
          const firstRef = iterKeys
            .map((n) => cellMap.get(`${m}|${n}`))
            .find((r): r is IterRef => r !== undefined);
          const url = firstRef?.issueUrl ?? g.variants[0]?.url ?? '#';
          return `
        <a class="col-head badge ${modelBadgeClass(m)}" href="${escape(url)}" target="_blank" rel="noopener">
          ${escape(shortModel(m))}
        </a>`;
        })
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
          <div class="cell empty" aria-hidden="true">
            ${pendingFrame('not rendered')}
            <span class="cell-label muted">iter ${n}</span>
          </div>`;
        return `
          <a class="cell" href="${escape(ref.issueUrl)}" target="_blank" rel="noopener" title="#${ref.issueNumber} · iter ${ref.iterIndex}">
            ${frame(ref.gifUrl, `${ref.issueTitle} iter ${ref.iterIndex} (${shortModel(m)})`)}
            <span class="cell-label">iter ${ref.iterIndex}</span>
          </a>`;
      })
      .join('');
    rows.push(`<div class="shootout-row" ${styleAttr}>${cells}</div>`);
  }

  const totalIters = cellMap.size;

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
      <div class="shootout-body" data-scroller>
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
    <div class="hero-collage" aria-hidden="true">
      <div class="hero-tile hero-tile-1" style="background-image:url(hero-1.jpg)"></div>
      <div class="hero-tile hero-tile-2" style="background-image:url(hero-2.jpg)"></div>
      <div class="hero-tile hero-tile-3" style="background-image:url(hero-3.jpg)"></div>
    </div>
    <div class="container hero-inner">
      <div class="hero-copy">
        <img class="hero-logo" src="logo.svg" alt="PXL" width="150" height="50" />
        <h1>24×24 pixels. Real glass. <span class="accent">Programmable in C#.</span></h1>
        <p class="hero-lead">
          PXL Clock is a 27×27&nbsp;cm LED frame for your shelf. You write the animations yourself in C# — or you describe what you want and let an AI do it.
        </p>
        <p class="hero-lead">
          The AI part lives on <a href="https://github.com/SchlenkR/pxl-clock">GitHub</a>. Open an issue with your idea, an agent picks it up, runs it through several models, and posts back what each of them made. Everything below is what came out — same prompt, model by model.
        </p>
        <div class="ctas">
          <a class="btn" href="https://www.pxlclock.com/?ref=RONALD">Get the real clock →</a>
          <a class="btn secondary" href="${escape(submitUrl)}">Submit an idea</a>
          <a class="btn secondary" href="https://discord.gg/KDbVdKQh5j">Discord</a>
        </div>
      </div>
    </div>
  </header>

  <div class="zoom-bar" data-zoom-bar>
    <button type="button" class="zoom-btn" data-zoom-in aria-label="Larger">+</button>
    <span class="zoom-label"><span data-zoom-level>3</span>/4</span>
    <button type="button" class="zoom-btn" data-zoom-out aria-label="Smaller">−</button>
  </div>

  <section id="shootout">
    <div class="container">
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
