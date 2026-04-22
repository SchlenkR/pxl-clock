import './styles.css';
import { render } from './render.ts';
import data from './data.json';
import type { IssuesData } from './types.ts';

const app = document.getElementById('app');
if (app) app.innerHTML = render(data as IssuesData);

/* ─── Zoom (4 sizes) — sticky bar at top of the shootout section ─── */
const ZOOM_SIZES = [110, 150, 190, 230]; // px
const DEFAULT_ZOOM = 1; // 150px (label "2/4")
let zoomIdx = DEFAULT_ZOOM;

const applyZoom = () => {
  document.documentElement.style.setProperty('--col-width', `${ZOOM_SIZES[zoomIdx]}px`);
  document.querySelectorAll<HTMLElement>('[data-zoom-level]').forEach((el) => (el.textContent = String(zoomIdx + 1)));
  for (const fn of overflowUpdaters) fn();
};

document.querySelectorAll<HTMLButtonElement>('[data-zoom-in]').forEach((b) =>
  b.addEventListener('click', () => {
    zoomIdx = Math.min(ZOOM_SIZES.length - 1, zoomIdx + 1);
    applyZoom();
  }),
);
document.querySelectorAll<HTMLButtonElement>('[data-zoom-out]').forEach((b) =>
  b.addEventListener('click', () => {
    zoomIdx = Math.max(0, zoomIdx - 1);
    applyZoom();
  }),
);

/* ─── Horizontal scroll per shootout ────────────────────────────────
   Only the rows viewport actually scrolls (overflow-x:auto). The head
   row lives inside an overflow:hidden clip and we translate it via CSS
   transform to match `viewport.scrollLeft`. No dual-container scroll
   feedback loop → no jitter, rock-solid column alignment. */
const overflowUpdaters: (() => void)[] = [];

for (const body of document.querySelectorAll<HTMLElement>('.shootout-body[data-scroller]')) {
  const viewport = body.querySelector<HTMLElement>(':scope > .shootout-viewport');
  const headrow = body.querySelector<HTMLElement>(':scope > .head-strip .shootout-headrow');
  const prev = body.querySelector<HTMLButtonElement>(':scope > .head-strip > .scroll-chev.prev');
  const next = body.querySelector<HTMLButtonElement>(':scope > .head-strip > .scroll-chev.next');
  if (!viewport || !headrow || !prev || !next) continue;

  const update = () => {
    const sl = viewport.scrollLeft;
    headrow.style.transform = `translate3d(${-sl}px, 0, 0)`;
    const canLeft = sl > 2;
    const canRight = sl < viewport.scrollWidth - viewport.clientWidth - 2;
    prev.disabled = !canLeft;
    next.disabled = !canRight;
    body.classList.toggle('can-scroll-left', canLeft);
    body.classList.toggle('can-scroll-right', canRight);
  };

  const page = () => Math.max(1, Math.floor(viewport.clientWidth * 0.8));
  prev.addEventListener('click', () => viewport.scrollBy({ left: -page(), behavior: 'smooth' }));
  next.addEventListener('click', () => viewport.scrollBy({ left: page(), behavior: 'smooth' }));
  viewport.addEventListener('scroll', update, { passive: true });
  window.addEventListener('resize', update);
  overflowUpdaters.push(update);
  update();
}

applyZoom();

/* ─── Sticky offset for stacked sticky bars ─────────────────────────
   The global model filter at the top of the page is sticky (top:0). Inside
   grid-view shootouts, each card has its own sticky head-strip that would
   otherwise pin at top:0 too and overlap the filter. We measure the filter's
   rendered height and expose it as `--sticky-offset`; the head-strip reads
   it as its `top` value and stacks neatly underneath. */
const globalFilter = document.querySelector<HTMLElement>('.global-filter');
if (globalFilter) {
  const updateStickyOffset = () => {
    document.documentElement.style.setProperty(
      '--sticky-offset',
      `${globalFilter.offsetHeight}px`,
    );
  };
  updateStickyOffset();
  window.addEventListener('resize', updateStickyOffset);
  new ResizeObserver(updateStickyOffset).observe(globalFilter);
}

/* ─── View toggle: grid vs panel ───────────────────────────────────
   Both views are rendered in the DOM; CSS hides the inactive one based on
   `body[data-view]`. State on body so it can drive any matching selector
   (incl. hiding feedback comments in the singles section in panel mode). */
type View = 'grid' | 'panel' | 'flat';
const setView = (view: View) => {
  document.body.dataset.view = view;
  document.querySelectorAll<HTMLButtonElement>('.view-btn').forEach((b) => {
    b.classList.toggle('active', b.dataset.view === view);
  });
  for (const fn of overflowUpdaters) fn();
};
document.querySelectorAll<HTMLButtonElement>('.view-btn').forEach((b) => {
  b.addEventListener('click', () => {
    const v = (b.dataset.view as View) ?? 'grid';
    setView(v);
    history.replaceState(null, '', `#${v}`);
  });
});
// Initial view: respect URL hash (#grid / #panel / #flat), default to grid.
const initialView: View = (['grid', 'panel', 'flat'] as const).includes(
  location.hash.slice(1) as View,
) ? (location.hash.slice(1) as View) : 'grid';
setView(initialView);

/* ─── Global model filter ────────────────────────────────────────
   Each .model-toggle in the sticky filter bar carries its model name in
   data-model. Clicking a toggle flips its `.active` class and we show/hide
   every matching element across all views:
     - flat view: .flat-cell[data-model]
     - panel view: .panel-row[data-model]
     - ideas list: .idea-card[data-model]
     - grid view: per-shootout refresh (recomputes grid-template-columns
       and hides col-head + cells for excluded models — see the shootout
       loop below, which registers an apply() fn into shootoutRefreshFns). */
const activeModels = new Set<string>();
for (const btn of document.querySelectorAll<HTMLButtonElement>('.model-toggle')) {
  const m = btn.dataset.model;
  if (m) activeModels.add(m);
}
const shootoutRefreshFns: (() => void)[] = [];
const applyModelFilter = () => {
  for (const cell of document.querySelectorAll<HTMLElement>('.flat-cell[data-model]')) {
    const m = cell.dataset.model!;
    cell.style.display = activeModels.has(m) ? '' : 'none';
  }
  for (const row of document.querySelectorAll<HTMLElement>('.panel-row[data-model]')) {
    const m = row.dataset.model!;
    row.style.display = activeModels.has(m) ? '' : 'none';
  }
  for (const card of document.querySelectorAll<HTMLElement>('.idea-card[data-model]')) {
    const m = card.dataset.model!;
    card.style.display = activeModels.has(m) ? '' : 'none';
  }
  for (const fn of shootoutRefreshFns) fn();
};
for (const btn of document.querySelectorAll<HTMLButtonElement>('.model-toggle')) {
  btn.addEventListener('click', () => {
    const m = btn.dataset.model;
    if (!m) return;
    if (activeModels.has(m)) {
      activeModels.delete(m);
      btn.classList.remove('active');
    } else {
      activeModels.add(m);
      btn.classList.add('active');
    }
    applyModelFilter();
  });
}
for (const btn of document.querySelectorAll<HTMLButtonElement>('[data-filter-action]')) {
  btn.addEventListener('click', () => {
    const enable = btn.dataset.filterAction === 'all';
    for (const toggle of document.querySelectorAll<HTMLButtonElement>('.model-toggle')) {
      const m = toggle.dataset.model;
      if (!m) continue;
      if (enable) {
        activeModels.add(m);
        toggle.classList.add('active');
      } else {
        activeModels.delete(m);
        toggle.classList.remove('active');
      }
    }
    applyModelFilter();
  });
}

/* ─── Grid-view column collapse ──────────────────────────────────────
   Click a .col-toggle header → that column in every row of THIS shootout
   shrinks to a narrow circle (~36px). Collapsed state is tracked per-
   shootout (different shootouts may have different model sets, and
   collapsing should only affect the one clicked). */
for (const shootout of document.querySelectorAll<HTMLElement>('.shootout')) {
  const body = shootout.querySelector<HTMLElement>('.shootout-body[data-scroller]');
  if (!body) continue;

  const gridRows = body.querySelectorAll<HTMLElement>('.shootout-headrow, .shootout-row');
  const toggleBtns = body.querySelectorAll<HTMLButtonElement>('.col-toggle');

  // Ordered list of this shootout's models — drives grid-template-columns.
  const models = [...toggleBtns].map((b) => b.dataset.model!).filter(Boolean);
  const collapsed = new Set<string>();

  const apply = () => {
    // Only visible (globally-active) models get a column track. Cells and
    // col-heads for excluded models are `display:none`d; since `display:none`
    // takes an element out of grid flow entirely, the remaining cells fall
    // into the remaining tracks in order — no empty gaps.
    const visibleModels = models.filter((m) => activeModels.has(m));
    const cols = visibleModels
      .map((m) => (collapsed.has(m) ? '36px' : 'var(--col-width)'))
      .join(' ');
    for (const row of gridRows) {
      row.style.gridTemplateColumns = cols;
    }
    for (const btn of toggleBtns) {
      const m = btn.dataset.model!;
      btn.classList.toggle('collapsed', collapsed.has(m));
      btn.style.display = activeModels.has(m) ? '' : 'none';
    }
    for (const cell of body.querySelectorAll<HTMLElement>('.shootout-row .cell[data-model]')) {
      const m = cell.dataset.model!;
      cell.classList.toggle('col-collapsed', collapsed.has(m));
      cell.style.display = activeModels.has(m) ? '' : 'none';
    }
    // Recompute overflow state — collapsed/hidden columns change scroll width.
    for (const fn of overflowUpdaters) fn();
  };

  shootoutRefreshFns.push(apply);

  for (const btn of toggleBtns) {
    btn.addEventListener('click', () => {
      const m = btn.dataset.model;
      if (!m) return;
      if (collapsed.has(m)) collapsed.delete(m);
      else collapsed.add(m);
      apply();
    });
  }
}

/* ─── GIF load/unload on viewport entry ──────────────────────────────
   Each .lazy-gif starts with a 1×1 placeholder in `src` and the real URL in
   `data-src`. An IntersectionObserver with a generous rootMargin loads GIFs
   just before they scroll in and unloads them (swap back to placeholder)
   once they're well out of view. Browsers keep every animated GIF running in
   the background; without this, 500+ GIFs on-page destroy scroll perf. */
const GIF_ROOT_MARGIN = '400px 0px 400px 0px';
const gifObserver = new IntersectionObserver(
  (entries) => {
    for (const e of entries) {
      const img = e.target as HTMLImageElement;
      const real = img.dataset.src;
      if (!real) continue;
      if (e.isIntersecting) {
        if (img.src !== real) img.src = real;
      } else {
        // Swap back to the blank placeholder → stops the GIF animating.
        if (img.src === real) {
          img.src =
            'data:image/gif;base64,R0lGODlhAQABAAAAACH5BAEKAAEALAAAAAABAAEAAAICTAEAOw==';
        }
      }
    }
  },
  { rootMargin: GIF_ROOT_MARGIN, threshold: 0.01 },
);
for (const img of document.querySelectorAll<HTMLImageElement>('img.lazy-gif')) {
  gifObserver.observe(img);
}
