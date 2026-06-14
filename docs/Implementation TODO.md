# Implementation TODO

This checklist tracks concrete implementation status for the framework roadmap.

## Current status

- [x] Core abstractions for candidate programs and evolution results
- [x] Roslyn AST security evaluation and policy integration hooks
- [x] Roslyn dynamic in-memory compilation service
- [x] Collectible execution helper for isolated runtime invocation
- [x] Evolution orchestrator with execution-budget telemetry
- [x] Multi-team adversarial review orchestration with role rotation
- [x] Deferred and accepted flaw carry-forward across review rounds
- [x] Adversarial fitness feedback bridge integration in evolution flow
- [x] Unit and integration test coverage for adversarial orchestration flow

## Next implementation increments

- [x] Add richer flaw adjudication heuristics for conflict-heavy rounds
- [ ] Expand adversarial telemetry for per-round convergence diagnostics
- [ ] Add end-to-end sample wiring mutation + fitness + adversarial loop
- [ ] Add additional negative integration tests for malformed adjudication outputs
