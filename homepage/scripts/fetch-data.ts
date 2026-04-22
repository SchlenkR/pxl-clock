/**
 * Prebuild: fetch GitHub issues + branch artifacts + comment history.
 * Writes src/data.json.
 *
 * Key enrichment:
 * - autoCount: "Mach N Iterationen" in the body defines the initial batch size.
 * - feedback:  for iterations beyond autoCount, record the user/maintainer
 *              comment that triggered them (for the homepage to display next
 *              to the corresponding GIF).
 */
import { execSync } from 'node:child_process';
import { mkdir, writeFile } from 'node:fs/promises';
import { resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import type {
  FeedbackComment,
  Issue,
  IssuesData,
  Iteration,
  ShootoutGroup,
} from '../src/types.ts';

const OWNER = 'SchlenkR';
const REPO = 'pxl-clock';
const ARTIFACT_BRANCH = 'pixogram-maker';
const API = 'https://api.github.com';

const TITLE_SUFFIX_RE = /\s*[\[\(]\s*(model-)?[a-zA-Z0-9._\/-]+\s*[\]\)]\s*$/;
const MACH_N_RE = /Mach\s+(\d+)\s+Iterationen?/i;
// Pipeline-appended footer: `🤖 **Config Set:** \`opus-4.7\`` → "opus-4.7"
const CONFIGSET_RE = /Config Set:\*\*\s*`([^`]+)`/;

// Pipeline role tags — a comment containing any of these is from the bot,
// not a user trigger.
const PIPELINE_TAGS = [
  '**[Implementor]**',
  '**[Director/Visionary]**',
  '**[Director/Maverick]**',
  '**[Director]**',
  '**[Admin]**',
  '**[Craftsman]**',
];

const BOT_AUTHOR_RE = /^(github-actions|dependabot|copilot)(\[bot\])?$/i;
const COMMENT_HIDDEN_MARKER = '<!-- pixogram-bot-note -->';

// Resolve a GitHub token: prefer env (CI), fall back to `gh auth token`
// (local dev). Cached after first call to avoid spawning gh per-request.
let cachedToken: string | undefined;
let tokenResolved = false;
const ghToken = (): string | undefined => {
  if (tokenResolved) return cachedToken;
  tokenResolved = true;
  cachedToken = process.env.GITHUB_TOKEN || process.env.GH_TOKEN;
  if (!cachedToken) {
    try {
      cachedToken = execSync('gh auth token', {
        encoding: 'utf8',
        stdio: ['ignore', 'pipe', 'ignore'],
      }).trim() || undefined;
    } catch {
      // gh not installed or not logged in — fall through unauthenticated.
    }
  }
  return cachedToken;
};

const headers = (): Record<string, string> => {
  const h: Record<string, string> = {
    Accept: 'application/vnd.github+json',
    'X-GitHub-Api-Version': '2022-11-28',
    'User-Agent': 'pxl-clock-homepage-builder',
  };
  const token = ghToken();
  if (token) h.Authorization = `Bearer ${token}`;
  return h;
};

async function ghJson<T>(path: string): Promise<T> {
  const res = await fetch(`${API}${path}`, { headers: headers() });
  if (!res.ok) {
    const body = await res.text().catch(() => '');
    throw new Error(`GET ${path} → ${res.status}: ${body.slice(0, 200)}`);
  }
  return res.json() as Promise<T>;
}

async function pool<T, R>(items: T[], size: number, fn: (t: T) => Promise<R>): Promise<R[]> {
  const results: R[] = new Array(items.length);
  let cursor = 0;
  const workers = Array.from({ length: Math.min(size, items.length) }, async () => {
    while (cursor < items.length) {
      const i = cursor++;
      results[i] = await fn(items[i]!);
    }
  });
  await Promise.all(workers);
  return results;
}

const stripTitleSuffix = (t: string) => t.replace(TITLE_SUFFIX_RE, '').trim();

const normalizeDescription = (body: string | null): string => {
  if (!body) return '';
  const firstBlock = body.split(/\n\s*\n/)[0] ?? '';
  return firstBlock
    .replace(/<!--[\s\S]*?-->/g, '')
    .replace(/\s+/g, ' ')
    .replace(/Mach\s+\d+\s+Iterationen?\.?/gi, '')
    .trim()
    .toLowerCase();
};

const parseAutoCount = (body: string | null, fallback = 3): number => {
  if (!body) return fallback;
  const m = body.match(MACH_N_RE);
  if (!m) return fallback;
  const n = parseInt(m[1]!, 10);
  return Number.isFinite(n) && n > 0 ? n : fallback;
};

const sanitizeForFolder = (title: string): string => {
  const cleaned = title
    .toLowerCase()
    .split('')
    .map((c) => (/[\p{L}\p{N}]/u.test(c) ? c : /[\s_-]/.test(c) ? '_' : ''))
    .join('');
  return cleaned.replace(/_+/g, '_').replace(/^_|_$/g, '').slice(0, 40).replace(/_+$/, '');
};

const branchRaw = (folder: string, file: string) =>
  `https://raw.githubusercontent.com/${OWNER}/${REPO}/${ARTIFACT_BRANCH}/${encodeURIComponent(folder)}/${encodeURIComponent(file)}`;
const branchBlob = (folder: string, file: string) =>
  `https://github.com/${OWNER}/${REPO}/blob/${ARTIFACT_BRANCH}/${encodeURIComponent(folder)}/${encodeURIComponent(file)}`;

interface RawIssue {
  number: number;
  title: string;
  body: string | null;
  state: string;
  html_url: string;
  labels: Array<string | { name?: string }>;
  updated_at: string;
  pull_request?: unknown;
}

interface RawContent {
  name: string;
  type: string;
}

interface RawComment {
  body: string;
  user: { login: string } | null;
  created_at: string;
}

async function listAllIssues(): Promise<RawIssue[]> {
  const out: RawIssue[] = [];
  let page = 1;
  while (true) {
    const batch = await ghJson<RawIssue[]>(
      `/repos/${OWNER}/${REPO}/issues?state=all&labels=pixogram-idea&per_page=100&page=${page}`,
    );
    out.push(...batch);
    if (batch.length < 100) break;
    page++;
  }
  return out.filter((i) => !i.pull_request);
}

async function listFolder(folder: string): Promise<string[]> {
  try {
    const contents = await ghJson<RawContent[] | { message: string }>(
      `/repos/${OWNER}/${REPO}/contents/${encodeURIComponent(folder)}?ref=${ARTIFACT_BRANCH}`,
    );
    if (!Array.isArray(contents)) return [];
    return contents.map((c) => c.name);
  } catch (e: unknown) {
    if (e instanceof Error && /404/.test(e.message)) return [];
    throw e;
  }
}

async function listComments(issueNumber: number): Promise<RawComment[]> {
  const out: RawComment[] = [];
  let page = 1;
  while (true) {
    const batch = await ghJson<RawComment[]>(
      `/repos/${OWNER}/${REPO}/issues/${issueNumber}/comments?per_page=100&page=${page}`,
    );
    out.push(...batch);
    if (batch.length < 100) break;
    page++;
  }
  return out;
}

const isPipelineComment = (body: string): boolean =>
  body.includes(COMMENT_HIDDEN_MARKER) || PIPELINE_TAGS.some((tag) => body.includes(tag));

const isBot = (login: string | null | undefined): boolean =>
  !login || BOT_AUTHOR_RE.test(login);

const stripComment = (body: string): string => {
  // Trim HTML comments and fold whitespace. Keep original punctuation.
  return body
    .replace(/<!--[\s\S]*?-->/g, '')
    .replace(/\r?\n+/g, ' ')
    .replace(/\s+/g, ' ')
    .trim();
};

/**
 * Walk comments chronologically, find each Implementor comment (these
 * correspond in order to iteration index 1, 2, 3…) and extract its
 * Config Set → model mapping from the footer. Iterations that predate
 * the Config Set marker era stay unmapped.
 */
interface IterMeta {
  model: string | null;
  createdAt: string;
}
function extractIterMeta(comments: RawComment[]): Map<number, IterMeta> {
  const sorted = [...comments].sort(
    (a, b) => new Date(a.created_at).getTime() - new Date(b.created_at).getTime(),
  );
  let implIndex = 0;
  const map = new Map<number, IterMeta>();
  for (const c of sorted) {
    if (!c.body.includes('**[Implementor]**')) continue;
    implIndex++;
    const m = c.body.match(CONFIGSET_RE);
    map.set(implIndex, {
      model: m ? m[1]! : null,
      createdAt: c.created_at,
    });
  }
  return map;
}

/**
 * Walk comments chronologically. Whenever an Implementor comment is hit,
 * check the iteration index: if > autoCount, find the most recent
 * non-pipeline human comment since the last Implementor — that is the trigger.
 */
function matchFeedbackToIters(
  comments: RawComment[],
  autoCount: number,
): FeedbackComment[] {
  const sorted = [...comments].sort(
    (a, b) => new Date(a.created_at).getTime() - new Date(b.created_at).getTime(),
  );
  let implIndex = 0;
  let pendingTrigger: RawComment | null = null;
  const feedback: FeedbackComment[] = [];

  for (const c of sorted) {
    const isImpl = c.body.includes('**[Implementor]**');
    if (isImpl) {
      implIndex++;
      if (implIndex > autoCount && pendingTrigger) {
        feedback.push({
          iterIndex: implIndex,
          author: pendingTrigger.user?.login ?? 'unknown',
          body: stripComment(pendingTrigger.body).slice(0, 400),
          createdAt: pendingTrigger.created_at,
        });
      }
      pendingTrigger = null;
      continue;
    }
    // Skip pipeline comments (Director, Triage notes, etc.) and bot authors
    if (isPipelineComment(c.body)) continue;
    if (isBot(c.user?.login)) continue;
    pendingTrigger = c;
  }
  return feedback;
}

async function main() {
  console.log(`fetch: ${OWNER}/${REPO} issues with pixogram-idea label...`);
  const rawIssues = await listAllIssues();
  console.log(`  ${rawIssues.length} issues`);

  const issues = await pool(rawIssues, 6, async (raw): Promise<Issue> => {
    const labels = raw.labels
      .map((l) => (typeof l === 'string' ? l : l.name ?? ''))
      .filter((s): s is string => !!s);
    const modelLabel = labels.find((l) => l.startsWith('model-'));
    const model = modelLabel ? modelLabel.replace(/^model-/, '') : null;
    const cleanTitle = stripTitleSuffix(raw.title);
    // Branch folder is derived from the ORIGINAL title (incl. `[model-...]` suffix)
    // because the F# workflow used the raw Issue.Title at commit time.
    // `cleanTitle` is for display only.
    const folder = `issue-${raw.number}_${sanitizeForFolder(raw.title)}`;
    const autoCount = parseAutoCount(raw.body);

    const files = await listFolder(folder);
    const gifs = files.filter((f) => f.endsWith('.gif')).sort();

    // Always pull comments: we need per-iteration model extraction from the
    // ConfigSet footer. `feedback` mapping is a free byproduct.
    let feedback: FeedbackComment[] = [];
    let iterMeta = new Map<number, IterMeta>();
    if (gifs.length > 0) {
      const comments = await listComments(raw.number);
      iterMeta = extractIterMeta(comments);
      if (gifs.length > autoCount) {
        feedback = matchFeedbackToIters(comments, autoCount);
      }
    }

    const iterations: Iteration[] = gifs.map((gif) => {
      const base = gif.replace(/\.gif$/, '');
      const index = parseInt(base, 10);
      const meta = iterMeta.get(index);
      return {
        index,
        gifUrl: branchRaw(folder, gif),
        csUrl: branchBlob(folder, `${base}.cs`),
        // Specific model that produced this iter. Null if pre-marker era.
        model: meta?.model ?? null,
        // Timestamp of the Implementor comment — proxy for GIF creation date.
        createdAt: meta?.createdAt ?? null,
      };
    });

    return {
      number: raw.number,
      title: raw.title,
      cleanTitle,
      description: normalizeDescription(raw.body),
      model,
      labels,
      folder,
      updatedAt: raw.updated_at,
      iterations,
      autoCount,
      feedback,
      url: raw.html_url,
      state: raw.state as 'open' | 'closed',
      isShootout: labels.includes('bench-7model'),
    };
  });

  const kept = issues.filter((i) => !i.labels.includes('pixogram-ignore'));

  // Shootout detection: cluster issues by description similarity.
  // Word-level trigrams + Jaccard, union-find, threshold 0.5. Robust to small
  // rewordings / punctuation differences. For identical normalized descriptions,
  // Jaccard = 1.0 and the threshold is trivially met.
  const SIM_THRESHOLD = 0.5;

  const wordTrigrams = (text: string): Set<string> => {
    const words = text.split(/\s+/).filter((w) => w.length > 0);
    const s = new Set<string>();
    for (let i = 0; i + 3 <= words.length; i++) {
      s.add(words.slice(i, i + 3).join(' '));
    }
    // Very short descriptions: fall back to the whole text as a single shingle,
    // forcing exact-match behaviour for edge cases.
    if (s.size === 0 && words.length > 0) s.add(words.join(' '));
    return s;
  };

  const jaccard = (a: Set<string>, b: Set<string>): number => {
    if (a.size === 0 || b.size === 0) return 0;
    let inter = 0;
    for (const x of a) if (b.has(x)) inter++;
    return inter / (a.size + b.size - inter);
  };

  // Union-find keyed by issue number
  const parent = new Map<number, number>();
  const find = (n: number): number => {
    let p = parent.get(n) ?? n;
    while (p !== (parent.get(p) ?? p)) p = parent.get(p) ?? p;
    parent.set(n, p);
    return p;
  };
  const union = (a: number, b: number): void => {
    const ra = find(a);
    const rb = find(b);
    if (ra !== rb) parent.set(ra, rb);
  };

  const withDesc = kept.filter((i) => i.description);
  const shingles = new Map<number, Set<string>>();
  for (const i of withDesc) shingles.set(i.number, wordTrigrams(i.description));

  for (let i = 0; i < withDesc.length; i++) {
    for (let j = i + 1; j < withDesc.length; j++) {
      const a = withDesc[i]!;
      const b = withDesc[j]!;
      const sim = jaccard(shingles.get(a.number)!, shingles.get(b.number)!);
      if (sim >= SIM_THRESHOLD) union(a.number, b.number);
    }
  }

  const clusters = new Map<number, Issue[]>();
  for (const i of withDesc) {
    const root = find(i.number);
    const list = clusters.get(root) ?? [];
    list.push(i);
    clusters.set(root, list);
  }
  const buckets = new Map<string, Issue[]>();
  for (const [rootNumber, list] of clusters) {
    // Use the lowest-numbered variant's description as the canonical key for display.
    const canonical = list.slice().sort((a, b) => a.number - b.number)[0]!;
    buckets.set(`cluster-${rootNumber}-${canonical.description.slice(0, 64)}`, list);
  }
  const singles: Issue[] = kept.filter((i) => !i.description);

  const shootouts: ShootoutGroup[] = [];
  for (const [desc, bucket] of buckets) {
    if (bucket.length > 1) {
      const variants = bucket
        .slice()
        .sort((a, b) => (a.model ?? '').localeCompare(b.model ?? ''));
      // Mixed-legacy shootouts may have different autoCounts per variant — use max
      // so all auto-iters land in the auto-rows, and only true feedback goes below.
      const autoCount = Math.max(...variants.map((v) => v.autoCount));
      const maxIter = Math.max(
        autoCount,
        ...variants.map((v) => v.iterations.length),
        ...variants.flatMap((v) => v.feedback.map((f) => f.iterIndex)),
      );
      // Feedback text per round: pick any variant that has that round's feedback.
      const feedbackByRound = new Map<number, string>();
      for (const v of variants) {
        for (const f of v.feedback) {
          if (!feedbackByRound.has(f.iterIndex)) feedbackByRound.set(f.iterIndex, f.body);
        }
      }
      const feedbackRounds = [...feedbackByRound.entries()]
        .map(([iterIndex, text]) => ({ iterIndex, text }))
        .sort((a, b) => a.iterIndex - b.iterIndex);
      shootouts.push({
        key: desc,
        cleanTitle: variants[0]!.cleanTitle,
        description: variants[0]!.description,
        variants,
        feedbackRounds,
        maxIter,
        autoCount,
      });
    } else {
      singles.push(...bucket);
    }
  }

  // Sort by last activity (most recent first). For shootouts, use the max
  // `updatedAt` across variants — a new comment/iter on ANY variant bubbles
  // the whole group back to the top.
  const latestUpdate = (variants: Issue[]): string =>
    variants.reduce((m, v) => (v.updatedAt > m ? v.updatedAt : m), '');
  shootouts.sort((a, b) => latestUpdate(b.variants).localeCompare(latestUpdate(a.variants)));
  singles.sort((a, b) => b.updatedAt.localeCompare(a.updatedAt));

  const data: IssuesData = {
    generatedAt: new Date().toISOString(),
    issues: kept,
    shootouts,
    singles,
  };

  const here = dirname(fileURLToPath(import.meta.url));
  const out = resolve(here, '../src/data.json');
  await mkdir(dirname(out), { recursive: true });
  await writeFile(out, JSON.stringify(data, null, 2), 'utf8');
  console.log(
    `✓ wrote ${out}: ${kept.length} issues, ${shootouts.length} shootouts, ${singles.length} singles, ` +
      `${kept.reduce((n, i) => n + i.iterations.length, 0)} iterations`,
  );
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
