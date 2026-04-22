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
