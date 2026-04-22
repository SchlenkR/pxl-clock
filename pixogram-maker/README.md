# Pixogram Maker

AI-driven pipeline that turns GitHub Issue descriptions into animated pixograms (24x24 pixel GIF animations) for the PXL Clock. Multiple AI agents collaborate in distinct creative roles, iterating on code until a working animation is rendered and posted back to the issue.

## Pipeline Overview

A GitHub Issue describes a visual idea. The pipeline processes it through a sequence of AI agents:

```
Issue
  -> Safety Check (content classification)
  -> Approval Gate (maintainer approval)
  -> Loop:
       Triage (decide next step)
        |
        |-- Normal rotation:
        |     Visionary -> Craftsman -> Implementor -> Maverick -> Craftsman -> Implementor -> ...
        |
        |-- User feedback after Implementor:
        |     Specific request ("make it bluer")       -> Craftsman -> Implementor
        |     Vague/open ("mach du mal weiter")        -> Maverick -> Craftsman -> Implementor
        |     Strong rejection ("ganz anderer Ansatz") -> Visionary -> Craftsman -> Implementor
        |
        |-- Done: maintainer says finished, or max iterations without new feedback
  -> GIF posted as comment on the issue
```

The **Triage** agent examines the full conversation after each step and decides what happens next. In normal rotation, Directors alternate (Visionary, then Maverick, then Visionary...) to keep iterations fresh. When a user posts feedback, Triage routes to the appropriate role based on the nature of the feedback — specific requests go straight to Craftsman, vague feedback goes to Maverick for creative reinterpretation, and strong rejections go to Visionary for a fresh direction. The Craftsman translates a Director's vision into a concrete, implementable specification. The Implementor generates C# code, renders it via `Pxl.Render`, and retries within the same agent session if rendering fails.


## Architectural Decisions

### Multi-Agent Role Separation

Each pipeline stage uses a dedicated AI agent with its own prompt and (optionally) its own backend/model. This means:

- Prompts stay focused and short — each agent has one job
- Different roles can use different models (e.g. a strong coder for Implementor, a cheaper model for Triage)
- The Triage agent acts as orchestrator, deciding the next step based on conversation state — the pipeline doesn't hardcode the sequence

### Structured XML Conversation Format

All conversation data passed to AI agents uses a structured XML format with CDATA sections:

```xml
<issue number="35" author="SchlenkR" labels="...">
  <title><![CDATA[...]]></title>
  <body><![CDATA[...]]></body>
</issue>
<conversation>
  <comment id="123" author="github-actions" role="director/visionary" time="...">
    <![CDATA[comment body here]]>
  </comment>
  <skipped count="2" reason="untrusted authors" />
</conversation>
```

**Why XML with CDATA:** GitHub comments contain arbitrary HTML, markdown, code blocks, and potentially XML-like content. CDATA sections prevent user content from colliding with our structural tags. This is safer and more robust than escaping.

**Why structured at all:** Agents can reference specific comments by `id` or filter by `role` attribute. Prompts use concrete references like `role="implementor"` instead of vague phrases like "the previous iteration". This eliminates ambiguity.

A shared format description (`conversation-format.md`) is auto-injected into all conversation-based prompts, so every agent understands the XML structure without duplicating the explanation in each prompt.

### Conversation Views

Not every agent needs the full conversation history. Two views exist:

- **Full** — all trusted comments. Used by Directors, Triage, and Summary.
- **Implementor** — only the last Implementor comment + everything after it (the current cycle). Used by Craftsman and Implementor.

The Implementor view keeps the context focused on what's relevant for the current iteration, avoiding noise from early Director discussions. Both views use the same XML format.

### Trust Model: Maintainers vs. Trusted Authors

Two separate concepts with different purposes:

| Concept | Purpose | Example |
|---------|---------|---------|
| **Maintainers** | Approval gate (who gets pinged), `role="maintainer"` in conversation | `SchlenkR` |
| **Trusted Authors** | Whose comments are included in the conversation at all | `SchlenkR, nojaf, ursenzler, github-actions` |

Additionally, the **issue author** is always trusted (their comments are included regardless of the trusted authors list).

Comments from untrusted authors are excluded from the conversation and reported as `<skipped>` elements. This prevents random GitHub users from injecting content into the AI pipeline.

The pipeline bot account (`github-actions`) must be trusted (so its own previous comments are visible to agents) but is not a maintainer (should not be pinged for approval).

### Prompt Injection Defense (Defense in Depth)

Multiple layers work together:

1. **Trust filtering** — Only comments from trusted authors reach the AI. Untrusted comments are stripped entirely.
2. **CDATA wrapping** — User content is structurally isolated from pipeline markup.
3. **Regex-based injection detection** — Compiled patterns scan all comments for known injection techniques (instruction bypass, role reassignment, jailbreak keywords, prompt extraction, social engineering, etc.) in English and German. Detected patterns are flagged in the XML as `flags="injection-suspect: ..."` attributes with a warning comment.
4. **Prompt structure** — For prompts receiving raw user input (safety-check, iteration-count), the instructions come *before* the user content. The AI has its task and rules established before encountering potentially adversarial input.
5. **Explicit warnings** — Prompts explicitly state that user content may contain injection attempts and should not be followed as instructions.

### Prompt Caching Optimization

The placement of `{{conversation}}` in prompts is deliberate:

- **Conversation-based prompts** (triage, directors, implementor, summary): Conversation goes at the **beginning**. Multiple agents in the same iteration share the same conversation prefix — placing it first maximizes prompt cache hits.
- **Single-input prompts** (safety-check, iteration-count): Static instructions go at the **beginning**, variable user content at the end. The instructions are the stable, cacheable part.

This ordering also aligns with the security goal: for safety-check and iteration-count, instructions-first means the AI's task is established before it sees potentially adversarial user content.

### Conversation Compaction

As iterations accumulate, the conversation grows. Director monologues, full Craftsman specifications, and Implementor code blocks can easily push past context limits. Instead of truncating blindly, the pipeline uses **AI-driven summarization** — inspired by [Microsoft's Agent Framework compaction](https://learn.microsoft.com/en-us/agent-framework/agents/conversations/compaction) but adapted to our role-based structure.

**How it works:**

1. Before each loop iteration, the full conversation is built **without compaction** and its token count estimated (~4 chars/token heuristic)
2. If estimated tokens exceed the compaction threshold (default 0.8) × the context length budget (both defined per `ConfigSet`, since different models have different context windows), compaction triggers
3. A dedicated compaction AI call (uses a smaller/cheaper model, e.g. Haiku for Anthropic, GPT-5.4-mini for Copilot) summarizes the full conversation into a structured document (~1500 tokens): original request, creative evolution per iteration, user feedback, current visual state, key decisions
4. The summary is uploaded as a release asset (`compaction.md`) alongside the GIFs — persistent across workflow runs
5. Subsequent conversation builds inject `<compaction-summary>` followed by only the **current cycle's comments** verbatim

**Why this approach over alternatives:**

- **vs. sliding window / truncation**: Dropping old messages loses creative context — why certain colors were chosen, what the user rejected, which directions were explored. The summary preserves decisions while dropping bulk (code, full specs, GIF URLs).
- **vs. token-level compression (LLMLingua)**: Our content is structured with clear roles. Role-aware summarization is more effective than generic token pruning because it knows what matters (user feedback, creative direction) vs. what's bulk (C# code, pixel coordinates).
- **vs. no compaction**: A Craftsman spec alone can be 3000+ tokens, an Implementor code block 5000+. After 4-5 iterations, conversations easily hit 60k+ tokens. Without compaction, later iterations either fail or lose prompt quality.

**What the summary preserves** (in priority order): the original request (user's words), creative evolution per Director, all user/maintainer feedback, current visual state description, what worked and what didn't, iteration count and last direction.

**What it drops**: full C# source code, complete Craftsman specifications (only key decisions kept), GIF URLs, HTML details blocks, repetitive pipeline role tags.

The latest Implementor cycle always stays verbatim — compaction only replaces **older** history. This means the current working state is never lost to summarization.

### Implementor Retry Loop (Stateful Agent Session)

When the Implementor's generated code fails to render, the error is sent back to the **same agent session** (not a fresh prompt). This is a deliberate design choice:

- The agent retains full context of what it just generated and why
- Error feedback is conversational: "this failed with error X, fix it"
- Each retry builds on the previous attempt, not starting from scratch
- Up to `MAX_IMPLEMENTOR_RETRIES` attempts before giving up

If all attempts fail, nothing is posted to GitHub — no broken results are published.

### Backend Abstraction

All AI calls go through a polymorphic `SelectedBackend` type that hides the concrete provider:

- **Anthropic** — Direct API (streaming SSE)
- **Copilot** — GitHub Copilot SDK
- **Ollama** — Local/remote Ollama instance
- **Docker** — Claude CLI in a container

A `ConfigSet` assigns a backend to each pipeline role, plus model-specific parameters like context window budget and compaction threshold. Switching the entire pipeline from Anthropic to Copilot (or mixing backends per role) is a single configuration change. Compaction uses a dedicated (typically smaller/cheaper) model since it's a summarization task, not creative generation.

### Protocol Logging

Every workflow run writes a timestamped log to `output/issue-{N}/{timestamp}.log`. This captures:

- Every AI call and response
- Every render attempt and result
- Triage decisions
- Gate results (approval, safety)

Logs are local-only, never posted to GitHub. They serve as a debugging and auditing trail.

### Deterministic vs. AI-Driven Decisions (Lessons Learned)

A key insight from building this pipeline: you need to be very intentional about *what* is decided by AI and *what* is decided deterministically in code. Getting this boundary wrong leads to runaway loops, unpredictable behavior, or overly rigid workflows.

**Deterministic (hardcoded in the workflow logic):**

| Decision | Why deterministic? |
|----------|-------------------|
| Safety check pass/fail | Security gate — must be reliable and auditable, not up to AI interpretation |
| Approval gate (maintainer check) | Authorization — who is allowed is a policy decision, not a judgment call |
| Trust filtering (whose comments are included) | Security — the AI should never decide whether to trust an input source |
| Maximum iteration guard | Resource protection — prevents runaway AI loops from burning API credits indefinitely |
| User-feedback override (resume after max iterations) | Policy — deterministically checks if a human commented after the last result, then allows Triage to continue |
| Compaction trigger (token count > threshold) | Threshold is a numeric check — *whether* to compact is deterministic, only *how* to compact is AI-driven |
| Role detection in conversation builder | Pattern matching on known tags — must be exact, not probabilistic |
| Prompt injection flag detection | Regex-based scanning — flags suspicious patterns deterministically, AI then sees the flags |

**AI-driven (decided by an AI agent):**

| Decision | Why AI? |
|----------|---------|
| Next action / routing (Triage) | Requires understanding conversation context, user intent, creative state — no fixed rule can capture this |
| Creative direction (Visionary, Maverick) | Inherently creative — the whole point is that AI generates novel visual ideas |
| Specification writing (Craftsman) | Translating abstract vision into concrete pixel-level spec requires language understanding |
| Code generation (Implementor) | Writing C# code from a specification is a generation task |
| User feedback classification | "Make it bluer" vs. "I don't know, you decide" vs. "completely different approach" — nuance that requires NLU |
| Conversation compaction | Summarizing creative history while preserving what matters requires understanding, not just truncation |
| Iteration count extraction | Parsing free-form issue text for an optional iteration count (falls back to default if AI can't find one) |
| Safety classification | Content moderation of the issue description — requires understanding intent and context |

**Hybrid (AI extracts, code enforces):**

The most interesting category. The *iteration count* is a good example: an AI agent reads the issue body and extracts a number (AI-driven extraction), but the workflow loop uses that number as a hard limit (deterministic enforcement). Similarly, Triage *recommends* the next action (AI), but the workflow *enforces* that max iterations can't be exceeded without new user feedback (deterministic guard).

**The lesson:** Let AI handle understanding, creativity, and nuance. Let code handle enforcement, security, and resource limits. When in doubt, make the *gate* deterministic and let AI operate freely *within* the gate. A runaway AI loop taught us this the hard way — removing the deterministic iteration guard to "let Triage decide" caused 6 uncontrolled iterations on a real issue.

### Design Principle: Composable Pipeline Functions

Every pipeline function must be **composable** — callable directly in-process or delegable to an external sub-agent (separate process, container, or remote worker). This is a hard constraint on how we write code in this project.

**What this means in practice:**

1. **Explicit inputs, explicit outputs.** No function reads global mutable state (`Backends.*`, environment variables) implicitly. All dependencies are passed as parameters. Return types are structured (discriminated unions, `Result<'a, 'b>`), not side effects.

2. **Separation of I/O from logic.** A function like `parseSafetyCheckResponse: string → SafetyResult` is pure. The I/O wrapper `runSafetyCheck: SafetyCheckConfig → Issue → Async<SafetyResult>` handles the AI call. The pure core is testable; the wrapper is swappable.

3. **Proxy-ready signatures.** Every pipeline step (safety check, triage, director, craftsman, implementor, compaction) should have a signature clean enough that a "proxy" can either:
   - Call it directly in-process (fast, simple)
   - Serialize the inputs, send them to a sub-agent (separate process/container), and deserialize the output

4. **No mixed concerns.** A function that calls AI, renders a GIF, uploads to GitHub, and posts a comment is not composable. Break it into steps that can be composed by the caller.

**Current status:** `Conversation.fs` already follows this principle (mostly pure functions). `Triage.fs` has good signatures but reads global state. `Workflow.fs` is a monolith that needs decomposition. See the composability analysis for details.

**Why this matters:** We want the option to run pipeline steps as parallel GitHub Actions matrix jobs (one per issue), as separate containers, or as sub-agents — without rewriting the core logic. Composability is what makes this possible.

## Field Notes: Making a Local-LLM Pipeline Actually Fast

The pipeline runs entirely on a local Mac Studio (Ollama, qwen3.x models). No cloud APIs, no throttling, no per-token billing — but the naive setup was roughly **11 minutes per issue** for three iterations. A day of careful instrumentation brought this down to **~7–8 minutes** with several surprising findings along the way. These notes document what we found, in case it helps others building similar pipelines.

### Finding 1: The classic pipe deadlock, hiding in plain sight

**Symptom:** `Pxl.Render` would occasionally hang forever when invoked as a subprocess — no progress, no exit.

**Root cause:** The wrapper was reading the subprocess streams synchronously:

```fsharp
let stdout = p.StandardOutput.ReadToEnd()  // blocks here
let stderr = p.StandardError.ReadToEnd()
```

When the child filled its stderr buffer (e.g. a broken script throwing an exception every frame → 1200+ log lines), stderr blocked waiting for the parent to drain it, while the parent was stuck reading stdout. Deadlock.

**Fix:** Async stream reads via event handlers — [`OutputDataReceived`](src/pixograms/Workflow.fs#L141) / [`BeginOutputReadLine`](src/pixograms/Workflow.fs#L143). Both streams drain in parallel, no blocking.

**Lesson:** Any time you `ReadToEnd()` on a subprocess that can produce unbounded output, you have a deadlock-in-waiting. Use async readers or pre-declare the expected volume.

### Finding 2: Fail-fast is a library concern, not a caller concern

**Symptom:** Even after fixing the deadlock, `Pxl.Render` would run to completion on hopelessly broken scripts — emitting "Index out of bounds" on every single frame of a 30s render (1200 frames × noise = wasted wall-time).

**Initial (wrong) fix:** Caller-side — the `Pxl.Render` CLI caught `OnError` into a mutable, signaled a `ManualResetEventSlim`, and checked the flag from `OnFrameRendered`. It worked, but only because we owned both sides. Any other caller hitting the same class of script would hang the same way.

**Real fix:** Extend the Pxl library API itself. The underlying `Evaluation.startScene` already had a `FrameAction = Continue | Stop` type for `OnFrameRendered`. We extended it to `OnError` too:

```fsharp
// Before: OnError: exn -> unit
// After:  OnError: exn -> FrameAction
```

Now any caller — our CLI, the daemon, the simulator host — can simply return `Stop` from their error handler. The evaluation loop respects it and exits cleanly. `Pxl.Render`'s 25-line mutable-plus-event dance collapsed to three lines.

**Lesson:** When the workaround needs to be repeated at every call site, the API is wrong. Fix the library.

### Finding 3: The fail-fast / fail-safe distinction

A subtle but important categorization became clear only after the fail-fast work:

- **Fail-fast errors** — Per-frame exceptions. The scene is broken; every subsequent frame will throw too. Stopping immediately is correct.
- **Fail-safe hangs** — Infinite loops, busy-waits, allocation storms. No exception is thrown — the scene just never returns. Only an external timeout can catch these.

Both exist. A naive "one timeout to rule them all" treats both the same way, and the timeout has to be generous enough for the slowest legitimate render. That window is also how long a broken script wastes before being killed.

**Fix:** Two independent mechanisms. Fail-fast via `FrameAction.Stop` (reacts in <1s). Fail-safe via a 5-minute process timeout (only kicks in for true hangs).

**Lesson:** Not all "script broken" states look the same from outside. Design for both.

### Finding 4: Think mode is slow — and the fallback is slower

qwen3.x models have a native "thinking" mode that streams internal reasoning before the final answer. It dramatically improves multi-turn adherence but costs real time: our Implementor with thinking was 80–150s per call; without thinking, the same model ran at 35–40s.

Turning thinking off sounds like a free 3x speedup. In practice:

- **When it worked (~2/3 of calls):** 38s single-shot. Net 9× faster than the baseline's 355s-per-iteration (which retried 4× in think mode).
- **When it failed (~1/3 of calls):** The no-think model imitated the comment format from earlier conversation turns and emitted bullet-list summaries without any code block. The pipeline then had to fall back to think-mode, which took 155s for a call that would have taken 80s with thinking on from the start.

Total: ~30% faster on average, but noisier. Worth it for our workload; likely not worth it if failures dominate.

**Lesson:** Measure the fallback path, not just the happy path. A 3× speedup with a 4× fallback is only a win if fallbacks are rare.

### Finding 5: The `// ---` marker check was too strict

The original code-extraction logic required responses to contain a literal `// ---` frontmatter marker (our prompt's contract). Missing marker → immediate fallback to think-mode.

But small models sometimes drop the marker while still producing valid C# code. Every such case triggered a 155s fallback for a response that would have compiled just fine.

**Fix:** A cheap heuristic *in addition to* the strict check. If the extracted text looks like C# code — has `{` and `}` and `;`, references `Renderer` / `ctx.` / `void Frame` / `DrawingContext`, is at least 300 chars — accept it anyway. If it's actually broken, the render-retry loop will catch it for ~40s, which is still ~3x cheaper than a think-fallback.

```fsharp
let looksLikeCSharp (s: string) =
    s.Length > 300
    && s.Contains '{' && s.Contains '}' && s.Contains ';'
    && (s.Contains "Renderer" || s.Contains "ctx." || ...)
```

**Lesson:** When the strict check is cheap and the fallback is expensive, be lenient.

### Finding 6: Retry prompts grow unbounded if you're not careful

The Implementor retry loop originally accumulated all previous attempts + errors into the next prompt:

```
[system]
[user:conversation]
[assistant:attempt-1]
[user:error-1]
[assistant:attempt-2]
[user:error-2]
...
```

After 3-4 retries, the prompt ballooned to 15k+ tokens, much of it failed code the model should ignore anyway. Worse, the accumulator state leaked between iterations via a mutable variable.

**Fix:** Stateless retry — send only `baseMessages + [last_code; last_error]`. The model sees: "here's the task, here's what you just wrote, here's the error, fix it."

**Lesson:** More context isn't automatically more signal. For error-correction, only the most recent attempt matters.

### Finding 7: `prompt_eval tok/s` is a lying metric

This is the finding we had to undo. We had initially diagnosed a "cache miss" pattern from Ollama's `prompt_eval_count` divided by `prompt_eval_duration` — values like `200,000 tok/s` looked like cache hits and `8,000 tok/s` looked like cache misses. A 30× difference.

Then we actually read the Ollama docs: `prompt_eval_count` reports the **total prompt size**, not the number of tokens actually re-evaluated. On a cache hit, the cached prefix is skipped by the model but still counted in the response. Divide that by a tiny `duration` and the apparent throughput explodes to meaningless numbers. Divide it by a moderate duration and it looks like a cache miss, even when most of the prefix was cached.

**Lesson:** The right metric is `prompt_eval_duration` directly — that's the actual wall-clock spent on the prefill. We changed the log format to:

```
prompt_eval: 13998 tok in 2.87s, gen: 3159 tok @ 71.6 tok/s
```

Once we logged `duration` instead of `tok/s`, the cache behavior became legible. More on what we actually found below.

### Finding 8: The retry history was secretly cache-hostile

After we rejected the first "num_ctx fix" story, we started looking at *what* our retry loop was actually sending. Here's what it did:

```
Attempt 1:  [system, ...conv]                               (~10k tokens)
Attempt 2:  [system, ...conv, code1, err1]                  (~14k tokens)
Attempt 3:  [system, ...conv, code2, err2]    ← replaced!   (~14k tokens)
```

The comment above this block read: *"Trim retry context: send only the last code attempt + this error. Keeps prompt small (cache-friendly) and avoids confusing the model with prior failed attempts."*

That comment was wrong. "Small prompt" is **not** the same as "cache-friendly." For llama.cpp-style prefix caching, the only thing that matters is whether the byte prefix of this call matches the byte prefix of a previous call. Swapping `code1+err1` for `code2+err2` at the same position invalidates the cache from that point on. The prompt was shorter, sure, but every call was a fresh re-evaluation.

**Fix:** make the retry history append-only:

```
Attempt 2:  [system, ...conv, code1, err1]
Attempt 3:  [system, ...conv, code1, err1, code2, err2]
Attempt 4:  [system, ...conv, code1, err1, code2, err2, code3, err3]
```

Now every attempt's prefix is a byte-strict extension of the previous attempt's prompt. Cache hits become deterministic.

The measured effect, with the corrected duration metric:

| Attempt | Total tokens | `prompt_eval_duration` | New tokens processed |
|---|---|---|---|
| 1 | 10250 | 0.37s | (mostly warm from prior runs) |
| 2 | 13998 | 2.87s | ~3700 (at ~1300 tok/s) |
| 3 | 17162 | 2.71s | ~3200 (at ~1200 tok/s) |
| 4 | 19907 | 2.35s | ~2700 (at ~1200 tok/s) |

Total prompt grows 2× but duration stays flat — exactly the shape you'd expect if only the appended tail is actually being computed.

Without this fix, Attempt 4 would have been ~19k tokens × ~1200 tok/s = **~16 seconds** of prefill. With append-only: **2.35 seconds**. That's a ~7× speedup per retry, for free, once you stop trying to be clever.

**Lesson:** Shorter prompts are not automatically faster prompts. For a cache, *stability* of the prefix beats *size* of the whole.

### Finding 9: Byte-stable prefix — even GitHub label order matters

Once append-only was in place, we also had to audit everything else in the prompt prefix for *byte* stability. Anything that shifts between calls — even by one character — kills the cache hit at that offset.

What we audited:
- **System prompt templates** (lazy-loaded from disk once): stable. ✅
- **`llms.txt` API reference** (injected into the system prompt, also lazy): stable. ✅
- **Issue body, comment bodies**: stable (GitHub returns them verbatim). ✅
- **Issue header line** — this one bit us:

```fsharp
let labels = String.Join(", ", issue.Labels)  // order not guaranteed
messages.Add(ChatMessage.user $"[Issue #{number} by @{author}, labels: {labels}]\n...")
```

GitHub's REST API does not guarantee label order across calls. If the order flips between iteration N and iteration N+1, every byte after this line mismatches the cache. **Fix:**

```fsharp
let labels = String.Join(", ", issue.Labels |> List.sort)
```

Three lines in one file. One-character change in terms of behavior. Easy to miss — and the kind of thing no prompt-engineering guide will tell you to look for, because it's a consequence of the *server-side cache semantics*, not of the model.

**Also worth setting on the Ollama host:**
- `OLLAMA_NUM_PARALLEL=4` — four independent KV-cache slots. Triage, Visionary, Maverick, and Implementor each get their own slot, so they don't evict each other.
- `OLLAMA_KEEP_ALIVE=-1` — never unload. A 5-minute default means the first call after a compile failure can dump the slot.
- `OLLAMA_MULTIUSER_CACHE=true` — enables cache forking across slots.
- `OLLAMA_FLASH_ATTENTION=1`, `OLLAMA_KV_CACHE_TYPE=q8_0` — smaller KV footprint so more slots fit.

With these set, the non-Implementor calls (Triage, Directors, Summary) all ran at 0.3–0.7s per call regardless of prompt size — their slots stayed hot across iterations.

**Lesson:** When you're running local, *configure your inference server* first. Then worry about prompt engineering. The ratio of wins available from server-side config is much higher than most people think.

### Finding 10: Render-timeout tuning matters more than you'd think

A 60-second render timeout sounded generous — until a legitimately complex scene (pixel dragon with 400+ entities) needed 2m03s to simulate 30 seconds of animation at 40fps (1200 frames × ~100ms each). The workflow killed it every time and retried 5 times, all of which also timed out. Issue abandoned with zero output.

**Fix:** Bump the timeout to 5 minutes. Infinite loops still get killed eventually; complex-but-working scenes finish. The window opened up because Finding 2 made per-frame exceptions fail-fast in <1s regardless of timeout — the timeout now protects only against true hangs, not error floods.

**Lesson:** After you add a fast-path, your safety timeouts can be much more generous. Tighten only what still needs tightening.

### Summary: What moved the needle

| Change | Before | After | Mechanism |
|---|---|---|---|
| Async pipe reads | hang forever | 1–2s exit | No stderr buffer stall |
| `OnError: exn -> FrameAction` | 30s wasted on broken scripts | <1s exit | Fail-fast at library level |
| Thinking off for Implementor | 80–150s/call | 35–40s/call | ~3x fewer tokens in reasoning phase |
| Code-block heuristic | expensive fallbacks | cheap accept | Avoid unnecessary retry |
| Stateless retry | 15k+ tokens/call | ~12k tokens/call | Drop failed-attempt accumulator |
| Append-only retry history | Attempt 4 = ~16s prefill | Attempt 4 = ~2.3s prefill | Byte-stable prefix, cache hits |
| Sorted label order + byte audit | Random cache evictions | Deterministic cache reuse | Prefix bytes stable across calls |
| `num_ctx=40960` | Prompts > default were truncated | Full prompt fits in one slot | Still required — not a speedup, a correctness fix |
| `OLLAMA_NUM_PARALLEL=4`, `KEEP_ALIVE=-1` | Agents evicted each other's slots | Each agent keeps its own slot | Server-side config, not prompt work |
| 300s render timeout | complex scenes died | complex scenes finish | Fail-fast made room for this |

The retry prefill number (16s → 2.3s per retry) is the clearest per-call win. Total wall-clock improvements on any given issue are dominated by render success rate, not Ollama throughput — so the *combined* headline is harder to pin down than it first looks. What we can say: the Ollama side is no longer the bottleneck. When a run takes long now, it's because the generated C# needs 3 minutes to render, not because the model is slow.

**The deepest takeaway, worth calling out:** the single most useful instrument wasn't a fix, it was the logging change in Finding 7. For the entire first bench cycle we were debugging with `tok/s` and chasing phantom cache behavior. The real picture only emerged once we logged `prompt_eval_duration` directly. When you're optimizing against a black box, invest in making your metrics *honest* before you invest in the fix.

## Field Notes: Models We've Tried

Different backends behave very differently — not just in speed, but in how they reason, fail, and drift. These are observations from running the same feedback loop (5 rounds of varied prompts: specific / vague / dealer / rejection / aesthetic) on the same issues across models.

### Currently configured sets

| ConfigSet | Backend | Size | Character |
|---|---|---|---|
| `claude-sonnet-4.6/haiku-4.5` | Anthropic API | hosted | reliable baseline |
| `copilot-sonnet-4.6/haiku-4.5` | GitHub Copilot SDK | hosted | same model, different provider |
| `copilot-gpt-5.4/gpt-5.4-mini` | GitHub Copilot SDK | hosted | strong coder, empty-response risk without no-tools prompt |
| `ollama1-gemma4-26b` | Ollama local | 28 GB (Q8_0 MoE 26B-A4B) | visually ambitious, runtime-fragile |
| `ollama1-qwen36-35b` | Ollama local | 21 GB (NVFP4 MoE 35B-A3B) | fast, disciplined, think-loop prone |
| `ollama1-qwen36-coding-mxfp8` | Ollama local | 37 GB (MXFP8 MoE) | coding fine-tune, best Implementor speed so far |
| `ollama1-qwen36-coding-nothink` | Ollama local | same as above, `think=false` for Implementor | 3× faster when it works, 2× slower when fallback triggers |
| `ollama1-qwen3.5-27b-q8` | Ollama local | 29 GB | dense 27B, smooth baseline |
| `ollama1-gpt-oss-120b` | Ollama local | 65 GB (MXFP4 MoE 117B-A5.1B) | structured thinking, clean retries |
| `ollama1-nemotron-3-super` | Ollama local | 87 GB (Q4_K_M MoE 120B-A12B) | largest we fit on 128 GB, untested so far |
| `ollama1-nemotron-cascade-2` | Ollama local | 24 GB (Q4_K_M MoE 30B-A3B) | smallest Nemotron, untested so far |

NVFP4 note: on Apple Silicon, the `nvfp4` Ollama tags are GGUFs packed into FP4-scaled sub-blocks and run via the normal Metal path — llama.cpp does not yet support NVIDIA's native NVFP4 Tensor-Core kernels (tracking in [ggml-org/llama.cpp#16668](https://github.com/ggml-org/llama.cpp/discussions/16668)). Real NVFP4 inference still requires TensorRT-LLM on Hopper/Blackwell.

### Gemma 4 26B (MoE, 3.8B active)

- **Visual ambition is high.** A "dealer" prompt ("mach mal was, ich lass dich") reliably produces dramatic scenes — screen shake, sparks, shockwaves, glitch-text overlays.
- **Drift toward cosmic/singularity motifs.** Across unrelated issues (Tetris, DNA, shooting stars), the Maverick role tends to pull concepts into "black hole" / "vortex" / "orbital debris" territory. A strong model prior that's hard to steer around with prompting alone.
- **Runtime fragility.** Ambitious scenes routinely exhaust all 5 Implementor retries with `IndexOutOfRangeException` or similar runtime bugs. On one feedback round ([dealer] on issue #69), Gemma produced *no* output — every retry crashed differently. The retries don't converge toward a working version; they generate *different* buggy versions.
- **Pace:** ~7–15 min per iteration on M2 Ultra. Long thinking phases.

### Qwen 3.6 35B-A3B (MoE)

- **Fastest Ollama option for this workload.** ~3B active params means the Implementor call returns in 35–80s depending on think mode.
- **Disciplined.** Implementor code is conservative and usually runs first try. Low retry count.
- **Think-loop risk.** With thinking *on*, earlier qwen3.x versions self-reinforced reasoning indefinitely on ambiguous Triage decisions — we had to disable thinking for Triage to fix it. See commit `b5f726b` / `e049b76`.
- **Coding fine-tune (`mxfp8`) is materially better at Implementor** than the general-purpose tag. Not surprising — it's the same base with code-weighted SFT.

### GPT-OSS 120B (MoE, 5.1B active, MXFP4)

- **Structured root-cause thinking.** When an Implementor retry triggers, the thinking block *quotes the offending code* and derives the precondition for the crash:
  > *"Error 'Index and length must refer to a location within the string' occurs likely from substring operation on `timeText`. In code: `var displayText = timeText.Substring(0, timeText.Length - glitch);` If glitch becomes negative or larger than length..."*
  This reads like a human debugging, not a LLM pattern-matching on "error → fix" pairs.
- **Triage reasoning is very explicit.** GPT-OSS enumerates conversation comments numerically (`[1]`, `[2]`, …), walks the routing rules step-by-step, then outputs the decision. Audit trail for free.
- **Fast on this hardware.** 5 min per full iteration on M2 Ultra (vs. Gemma's 7–15 min). Feels like the sweet spot for local: big enough to reason well, small enough (~65 GB MXFP4) to stay warm in RAM.
- Uses the same `no-tools` system prompt as GPT-5.4 to avoid empty responses from phantom tool calls.

### Observations across the set

- **Local ≠ slow.** GPT-OSS 120B on M2 Ultra completes a full workflow iteration in the same ballpark as a Copilot Sonnet 4.6 round-trip. Latency is not the reason to pick hosted.
- **MoE active-param count predicts throughput better than total.** 117B-A5.1B (GPT-OSS) beats 26B-A4B (Gemma) on wall-clock despite being 4× larger on disk, because 4× more RAM doesn't slow token generation — only active params do.
- **Failure *mode* is more informative than failure *rate*.** Gemma fails by producing inventive-but-broken code; GPT-OSS fails by producing boring-but-correct code. Which is "better" depends on what the feedback prompt was asking for.
- **Heterogeneous model setups** (see Mode Collapse section below) become interesting here — running Visionary on Gemma for creative wildness while keeping Implementor on GPT-OSS for code reliability is a near-zero-effort config change with real upside.

## Open Problem: Ideation & Mode Collapse

In practice, iterations within a single issue tend to look **visually similar** — and even fresh issues often land in the same stylistic neighborhood. The Maverick role was introduced to counteract this, but it hasn't produced the conceptual jumps we'd hoped for. A literature survey confirmed this is a structural property of current LLMs, not a prompting issue.

### Why this happens (research-backed)

- **Mode collapse** in RLHF-aligned models systematically narrows output distributions (Padmakumar & He 2024, Kirk et al. 2024, Mohammadi 2024). The very alignment that makes instruction-tuned models pleasant to use also compresses their stylistic range.
- LLMs are strong in **exploratory** creativity (new points in the same conceptual space) but architecturally weak in **transformational** creativity — the kind that produces "Matrix-rain as a clock" or "Pac-Man drawing the digits" (Franceschelli & Musolesi 2024, based on Boden's taxonomy).
- **Homogeneous multi-agent setups amplify priors rather than break them**. Visionary and Maverick running on the same backend end up reinforcing each other's assumptions instead of diverging (Zhang et al. 2024 on social conformity in LLM agents; Liang et al. 2024 on debate dynamics). This directly explains why our Maverick role underperforms — it shares a model with Visionary.

### Approaches that could fit our scale

Full Quality-Diversity machinery (MAP-Elites, OpenELM, 1024-cell behavior grids) is overkill for ~5 issues and ~10 users. But several validated ideas translate cleanly:

- **Model heterogeneity** — different backends for different creative roles (ReConcile, Chen/Saha/Bansal 2023). Multiple Ollama models are already available; rotating them between Visionary/Maverick is a near-free change with strong literature support.
- **External inspiration injection** — Wikidata concepts, Oblique Strategies cards, named palettes (Lospec), trope catalogs. Validated by the CMU Kittur lab (SOLVENT, BIOSPARK, Inkspire) for producing cross-domain analogies.
- **Anti-archive / denial prompting** — showing the Director what already exists and forbidding its reuse (Lu et al. 2024 "Benchmarking LLM Creativity"). The per-issue gallery already provides the raw material.
- **Behavior-space categorization** — the core idea from QDAIF (Bradley et al. 2023) and OMNI (Zhang et al. 2023) without the full QD framework: pick 3-4 coarse axes (motion type, time representation, figure presence, palette character), place existing iterations on the grid, and use **empty cells** as an explicit to-do list for the pipeline.

### Directions we're considering

Nothing committed yet, ordered by effort-to-impact:

1. **Heterogeneous models per role** — Maverick on a different backend than Visionary. Cheapest and most literature-backed single change.
2. **External seed injection for Directors** — each Director turn receives a random Oblique Strategy + Wikidata concept + named palette as a constraint.
3. **Anti-archive in Director prompts** — attach existing iteration thumbnails with an explicit "do something visually different" instruction.
4. **Denial prompting** — maintain an evolving ban-list of techniques already used ("no sine waves, no `hh:mm` digit matrices") to force exploration into unused regions.
5. **Behavior-space categorization** — extract a few coarse axes from existing issues, then treat empty cells as prompts for targeted generation.

The underlying reframing (from the QD literature): **reward the pipeline for filling empty cells in a behavior space, not just for producing "good" ideas.** That turns the convergence problem into a coverage problem — something algorithms are known to be able to solve.

## Configuration

All configuration is via environment variables (loaded from `.env` locally, from GitHub repository variables in CI):

| Variable | Purpose |
|----------|---------|
| `MAINTAINERS` | Comma-separated list of GitHub usernames for approval gate |
| `TRUSTED_AUTHORS` | Comma-separated list of GitHub usernames whose comments are included |
| `DEFAULT_ITERATIONS` | Fallback iteration count if not specified in the issue |
| `MAX_IMPLEMENTOR_RETRIES` | Max render attempts per Implementor step |
| `AI_TIMEOUT_MS` | Timeout for AI calls |
| `GIF_DURATION_SECONDS` | Duration of rendered GIF |
| `GIF_SCALE` | Pixel scale factor for GIF |
| `CONFIG_SET` | Which backend config to use (see "Models We've Tried" for the full list — Claude/Copilot hosted or Ollama local) |

Context window budget (`ContextLengthTokens`) and compaction threshold (`CompactionThreshold`) are **not** environment variables — they are defined per `ConfigSet` in code, since they depend on the model's capabilities.

## CLI

```
dotnet run -- workflow <issue>              # Full workflow on one issue
dotnet run -- triage <issue>                # Safety + approval check only
dotnet run -- conversation <issue>          # Show Full conversation XML
dotnet run -- conversation <issue> implementor  # Show Implementor view XML
dotnet run -- triage-all                    # Triage all untriaged issues
dotnet run -- workflow-all                  # Workflow on all eligible issues
dotnet run                                  # Interactive mode (menu)
```

## Module Structure

```
Program.fs          CLI entry point, interactive menu
  |
Workflow.fs         Workflow orchestration, gates, rendering, GIF upload
  |
Conversation.fs     Trust model, injection detection, XML conversation builder
  |
Triage.fs           Safety check, iteration count, next-action decision, agent calls
  |
Config.fs           Environment config, backend selection, AI call helpers
  |
GitHub.fs           Octokit wrapper: issues, comments, labels, releases

AiBase/             Shared library: IAgent interface, backend implementations
                    (Anthropic, Copilot, Ollama, Docker), streaming events
```

Compilation order matches dependency order (top to bottom = last to first compiled).
