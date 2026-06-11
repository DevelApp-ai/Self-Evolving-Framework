# Implementation TODO from TDS

Source: `docs/Evolving C# Program Design Specification.md`

## Phase 1 — Core foundation

- [x] Candidate and evolution result core models
- [x] Roslyn in-memory dynamic compilation service
- [x] Collectible `AssemblyLoadContext` isolated execution
- [x] AST-based namespace/invocation security evaluation
- [x] Orchestrator feedback snapshot semantics
- [x] Initial guardrail for obvious infinite loops (`while(true)`, `for(;;)`, `do { } while(true)`)

## Phase 2 — Security hardening

- [x] Add AST JSON serialization output for policy evaluation input
- [x] Add OPA WebAssembly policy evaluation adapter
- [x] Add default deny-list Rego policy package and fixtures
- [x] Add integration tests for policy-driven deny/allow scenarios

## Phase 3 — Generative mutation/crossover

- [x] Add Semantic Kernel-backed mutator implementation
- [ ] Add crossover abstraction and implementation
- [ ] Feed compiler/security/runtime diagnostics into mutation prompts
- [ ] Add tests for deterministic prompt construction and response extraction

## Phase 4 — Evolution engine

- [ ] Integrate GeneticSharp population lifecycle
- [ ] Add chromosome model for source-code candidates
- [ ] Add selection strategy configuration (elite/tournament)
- [ ] Add adaptive mutation-rate strategy hooks

## Phase 5 — Behavioral validation

- [ ] Add Playwright-based post-compilation behavioral evaluator
- [ ] Add execution-flow derived fitness scoring
- [ ] Add integration tests for browser-flow scoring and failure penalties

## Phase 6 — Operational resilience

- [ ] Add execution budget/timeout policy options on orchestration boundary
- [ ] Add cancellation/failure diagnostics propagation
- [ ] Add performance/resource telemetry for evolution runs
