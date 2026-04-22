import './styles.css';
import { render } from './render.ts';
import data from './data.json';
import type { IssuesData } from './types.ts';

const app = document.getElementById('app');
if (app) app.innerHTML = render(data as IssuesData);

/* ─── Zoom (4 sizes) — sticky bar at top of the shootout section ─── */
const ZOOM_SIZES = [110, 150, 190, 230]; // px
const DEFAULT_ZOOM = 2; // 190px
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
  b.addEventListener('click', () => setView((b.dataset.view as View) ?? 'grid'));
});
setView('grid');

/* ─── Flat-view tooltip: pick above/below based on viewport room ───
   On hover, measure the cell's distance from the top of the viewport vs.
   the tooltip's height. If there isn't enough room above, drop a
   `.tip-below` class so CSS flips the tooltip under the cell. */
const TOOLTIP_RESERVE = 110; // approximate tooltip height + arrow + gap
for (const cell of document.querySelectorAll<HTMLElement>('.flat-cell')) {
  cell.addEventListener('mouseenter', () => {
    const rect = cell.getBoundingClientRect();
    const tip = cell.querySelector<HTMLElement>('.cell-tip');
    const tipHeight = tip?.offsetHeight || TOOLTIP_RESERVE;
    if (rect.top < tipHeight + 16) {
      cell.classList.add('tip-below');
    } else {
      cell.classList.remove('tip-below');
    }
  });
}
