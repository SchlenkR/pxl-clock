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
       -> Director/Visionary (emotional/conceptual vision)
       -> Director/Maverick (creative twist on existing result)
       -> Craftsman (precise technical specification)
       -> Implementor (C# code generation + render + retry loop)
  -> GIF posted as comment on the issue
```

The **Triage** agent examines the full conversation after each step and decides what happens next. Directors alternate (Visionary, then Maverick, then Visionary...) to keep iterations fresh. The Craftsman translates a Director's vision into a concrete, implementable specification. The Implementor generates C# code, renders it via `Pxl.Render`, and retries within the same agent session if rendering fails.

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

A `ConfigSet` assigns a backend to each pipeline role. Switching the entire pipeline from Anthropic to Copilot (or mixing backends per role) is a single configuration change.

### Protocol Logging

Every workflow run writes a timestamped log to `output/issue-{N}/{timestamp}.log`. This captures:

- Every AI call and response
- Every render attempt and result
- Triage decisions
- Gate results (approval, safety)

Logs are local-only, never posted to GitHub. They serve as a debugging and auditing trail.

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
| `CONFIG_SET` | Which backend config to use (`anthropic` or `copilot`) |

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
