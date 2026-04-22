export interface Iteration {
  index: number;
  gifUrl: string;
  csUrl: string;
}

/** User/maintainer comment that triggered a feedback iteration. */
export interface FeedbackComment {
  /** Which implementor iteration this feedback produced. */
  iterIndex: number;
  author: string;
  body: string;
  createdAt: string;
}

export interface Issue {
  number: number;
  title: string;
  cleanTitle: string;
  description: string;
  model: string | null;
  labels: string[];
  folder: string;
  /** ISO timestamp of the last issue/comment activity (GitHub's `updated_at`). */
  updatedAt: string;
  iterations: Iteration[];
  /**
   * Number of iterations requested up-front in the issue body
   * (parsed from "Mach N Iterationen"). Iterations ≤ autoCount are
   * auto-iters (the initial batch). Iterations > autoCount are feedback-driven.
   */
  autoCount: number;
  /** Feedback comments ordered by `iterIndex`. */
  feedback: FeedbackComment[];
  url: string;
  state: 'open' | 'closed';
  isShootout: boolean;
}

export interface ShootoutGroup {
  key: string;
  cleanTitle: string;
  description: string;
  variants: Issue[];
  /**
   * Feedback rounds shared across the shootout (one entry per feedback iter).
   * Uses the first-variant's feedback text as the canonical version.
   */
  feedbackRounds: { iterIndex: number; text: string }[];
  /** Max iteration index across all variants — defines the grid row count. */
  maxIter: number;
  /** autoCount common to all variants (should be identical). */
  autoCount: number;
}

export interface IssuesData {
  generatedAt: string;
  issues: Issue[];
  shootouts: ShootoutGroup[];
  singles: Issue[];
}
