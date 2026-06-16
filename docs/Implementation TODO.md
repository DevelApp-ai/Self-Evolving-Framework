# Local-First Cloud-Fallback Implementation TODO

- [x] Add initial routing domain contracts and option records for local/cloud endpoints, fallback policy, health monitoring, telemetry, and sandbox execution abstraction.
- [x] Implement baseline local-first router, fallback policy, circuit-breaker endpoint health monitor, and routed Semantic Kernel chat service with prompt cache key propagation.
- [x] Add unit tests for policy decisions, routing order, circuit breaker behavior, and cloud fallback telemetry behavior.
- [x] Add integration test proving `SemanticKernelEvolutionMutator` can succeed via cloud fallback when local endpoint fails.
- [x] Switch release target to minor semver progression for this feature stream.
- [x] Add concrete endpoint adapters for Ollama and Mistral HTTP APIs (beyond delegating test endpoint wrappers).
- [x] Add explicit routing toggles (`EnableLocalRouting`, `EnableCloudFallback`) so local-first/cloud-fallback behavior can be configured per environment and release rollout.
- [x] Add mutator integration coverage proving routing toggles can explicitly force cloud-only or local-only execution paths for 1.3.0 release readiness.
- [x] Add execution-budget-aware timeout harmonization with `EvolutionOrchestratorOptions`.
- [x] Wire runtime sandbox executor into agent-driven execution paths and block host execution in production mode.
- [x] Add explicit telemetry sink integration for external observability systems.
