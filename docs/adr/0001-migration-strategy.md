# ADR 0001: .NET 9 Migration Strategy - Incremental vs. Big-Bang

**Status**: Accepted
**Date**: 2025-10-09
**Deciders**: Migration Architect (Stage 1.2)
**Technical Story**: RawRabbit .NET 9 Upgrade - Stage 1.2 Discovery & Analysis

## Context and Problem Statement

RawRabbit is a mature RabbitMQ client library with 32 projects targeting legacy frameworks (net451, netstandard1.5-1.6, netcoreapp1.x-2.x). We need to upgrade to .NET 9 while maintaining stability, minimizing risk, and ensuring backward compatibility. The fundamental question is: **Should we migrate all projects simultaneously (big-bang) or incrementally (phased approach)?**

## Decision Drivers

### Technical Factors
- **32 projects** with varying complexity levels (20 simple, 8 medium, 3 complex)
- **Tight interdependencies** - Core library (RawRabbit) is foundation for all others
- **Critical dependency updates** - RabbitMQ.Client 5.0.1 → 6.8.1 has breaking changes
- **Deprecated APIs** - System.Web.HttpContext, old project formats
- **Multi-targeting requirements** - Need netstandard2.0 for backward compatibility

### Risk Factors
- **Production usage** - Unknown number of downstream consumers
- **Breaking changes** in key dependencies (RabbitMQ.Client, Polly 8.x, MessagePack 2.x)
- **Testing coverage** - Need to maintain test integrity throughout migration
- **Rollback complexity** - Must be able to revert if critical issues found

### Business Factors
- **Timeline pressure** - Desire to complete within 6-8 weeks
- **Stability requirements** - Library is critical infrastructure for users
- **Maintenance burden** - Supporting multiple branches/versions is costly
- **Developer velocity** - Team size and capacity constraints

## Considered Options

### Option 1: Big-Bang Migration
**Description**: Upgrade all 32 projects simultaneously in a single release cycle

**Pros**:
- Single, coordinated release
- No version sprawl
- Clear "before/after" state
- Simpler branch management
- Faster time to completion

**Cons**:
- HIGH RISK - All eggs in one basket
- Difficult to isolate failures
- Large surface area for bugs
- Hard to rollback partial failures
- Overwhelming testing scope
- Long integration period

### Option 2: Incremental Migration (Phased)
**Description**: Migrate projects in logical phases based on dependencies and complexity

**Pros**:
- Controlled risk - isolate failures per phase
- Incremental validation - test each phase thoroughly
- Easier rollback - revert specific phases
- Parallel work possible after core
- Learn from early phases
- Maintain stability throughout

**Cons**:
- Longer overall timeline (potentially)
- Version management complexity
- Multiple releases required
- Coordination overhead
- Branch management complexity

### Option 3: Hybrid Approach
**Description**: Core library big-bang, then incremental for extensions

**Pros**:
- Balance of speed and safety
- Core stability established quickly
- Extensions can proceed in parallel
- Moderate complexity

**Cons**:
- Still inherits some risks of big-bang
- Coordination challenges between phases
- Unclear decision boundaries

## Decision Outcome

**Chosen Option**: **Option 2 - Incremental Migration (Phased)** with structured phases

### Rationale

1. **Risk Mitigation**: The high-risk nature of RabbitMQ.Client breaking changes (v5→v6) makes incremental approach essential. If core migration fails, we can roll back without impacting extensions.

2. **Dependency Graph Structure**: RawRabbit's architecture naturally supports phased migration:
   ```
   Phase 1: RawRabbit (core) → Foundation
   Phase 2: Operations (depend on core) → Build on foundation
   Phase 3: Enrichers (depend on operations) → Add features
   Phase 4: DI/Compat (depend on all above) → Integration
   Phase 5: Tests → Validation
   Phase 6: Samples → Documentation
   ```

3. **Testing Integrity**: Incremental approach allows us to maintain passing tests at each phase, preventing integration hell.

4. **Production Safety**: Users can adopt phases gradually rather than forced big-bang upgrade.

5. **Learning Opportunity**: Early phases (especially core RabbitMQ.Client upgrade) will reveal issues that inform later phases.

6. **Parallel Work**: After Phase 1 completes, multiple teams can work on Phase 2/3 simultaneously.

### Implementation Strategy

#### Phase 1: Foundation (Week 1-2)
**Critical Path**: MUST complete before others
- RawRabbit (core library)
- RawRabbit.Operations.Tools
- **Success Criteria**: Core builds, all existing tests pass, RabbitMQ.Client 6.8.1 working
- **Go/No-Go**: If Phase 1 fails, abort and reassess

#### Phase 2: Simple Operations & Enrichers (Week 3)
**Parallel Execution**: Can be done simultaneously
- Batch 1: All SIMPLE operations (6 projects)
- Batch 2: Simple enrichers (5 projects)
- **Success Criteria**: All projects build with new core, tests pass

#### Phase 3: Complex Operations & Enrichers (Week 4-5)
**Sequential with dependencies**
- RawRabbit.Enrichers.HttpContext (COMPLEX - System.Web removal)
- RawRabbit.Enrichers.Polly (MEDIUM - Polly update)
- RawRabbit.Operations.MessageSequence (MEDIUM)
- RawRabbit.Operations.StateMachine (MEDIUM)
- Remaining enrichers
- **Success Criteria**: All functionality preserved, integration tests pass

#### Phase 4: Dependency Injection & Compatibility (Week 5)
**After all core work complete**
- DI adapters (Autofac, Ninject, ServiceCollection)
- Compatibility layer
- **Success Criteria**: All DI scenarios work, backward compatibility maintained

#### Phase 5: Test Projects (Week 6)
**Validation phase**
- Convert old-style projects to SDK format
- Update test frameworks
- Run full test suite
- **Success Criteria**: 100% test pass rate, no regressions

#### Phase 6: Samples & Documentation (Week 7)
**Polish phase**
- Update sample applications
- Update documentation
- Create migration guides
- **Success Criteria**: All samples run, docs complete

### Migration Principles

1. **Never Break Main Branch**: Each phase merges only when complete and tested
2. **Feature Branch Per Phase**: `feature/dotnet9-phase-1-core`, etc.
3. **Automated Testing**: CI/CD validation at each phase
4. **Rollback Plan**: Each phase can independently roll back
5. **Communication**: Stakeholder updates after each phase completion

### Version Strategy

#### During Migration
- **Current**: 2.0.x (net451, netstandard1.x) - maintained for critical bugs only
- **Next**: 2.1.0-alpha.X (incremental preview releases per phase)
- **Future**: 2.1.0 (final .NET 9 release)

#### Tagging Strategy
```
v2.1.0-alpha.1  - Phase 1 complete (core)
v2.1.0-alpha.2  - Phase 2 complete (operations)
v2.1.0-alpha.3  - Phase 3 complete (enrichers)
v2.1.0-beta.1   - Phase 4 complete (DI)
v2.1.0-rc.1     - Phase 5 complete (tests)
v2.1.0          - Phase 6 complete (final)
```

### Dependency Update Timing

#### Phase 1 (Core)
- RabbitMQ.Client: 5.0.1 → 6.8.1 (CRITICAL - must happen in Phase 1)
- Newtonsoft.Json: 10.0.1 → 13.0.3
- Framework: netstandard2.0, net9.0

#### Phase 3 (Enrichers)
- Polly: 5.3.1 → 7.2.4 (NOT 8.x yet - too risky)
- MessagePack: 1.7.3.4 → 2.5.140
- protobuf-net: 2.3.2 → 3.2.30
- **DEPRECATE**: ZeroFormatter

#### Phase 4 (DI)
- Autofac: 4.1.0 → 8.0.0
- Ninject: 3.3.4 → 3.3.6 (mark as obsolete)
- Microsoft.Extensions.DependencyInjection: 1.0.2 → 9.0.0

#### Phase 5 (Tests)
- xunit: 2.3.0 → 2.9.0
- Moq: 4.7.137 → 4.20.70
- BenchmarkDotNet: 0.10.3 → 0.14.0

### Risk Mitigation

#### Phase 1 Failure (Core)
- **Impact**: CRITICAL - blocks all other work
- **Mitigation**: Allocate 2 weeks, add buffer time
- **Rollback**: Revert to 2.0.x, reassess strategy
- **Contingency**: Consider RabbitMQ.Client 6.x fork if needed

#### Phase 3 Failure (Complex Enrichers)
- **Impact**: HIGH - but isolated
- **Mitigation**: Can skip problematic enrichers
- **Rollback**: Mark as obsolete, continue with others
- **Contingency**: Release without problematic enrichers

#### Integration Issues
- **Impact**: MEDIUM - fixable
- **Mitigation**: Comprehensive integration tests in Phase 5
- **Rollback**: Roll back specific phases
- **Contingency**: Extended beta period

### Success Metrics

#### Per-Phase Metrics
- Build success: 100% of projects in phase
- Test pass rate: 100% of existing tests
- Performance: Within 5% of baseline
- Breaking changes: Documented and justified

#### Overall Metrics
- Total timeline: 6-8 weeks (as planned)
- Test coverage: Maintain or improve
- Breaking changes: Minimize (documented in CHANGELOG)
- Consumer impact: Provide clear migration path

## Consequences

### Positive Consequences
- **Controlled Risk**: Each phase validated before proceeding
- **Incremental Value**: Users can adopt phases as released
- **Better Testing**: Each phase thoroughly tested in isolation
- **Learning**: Early phases inform later decisions
- **Rollback Safety**: Can revert specific phases without full rollback
- **Parallel Work**: After Phase 1, multiple streams possible

### Negative Consequences
- **Timeline Extension**: Potentially longer than big-bang (but safer)
- **Coordination Overhead**: Managing multiple phases requires discipline
- **Version Management**: More complex branching/tagging strategy
- **Multiple Releases**: More releases means more communication
- **Branch Maintenance**: Need to maintain feature branches longer

### Mitigations for Negative Consequences
1. **Timeline**: Use parallel work in Phases 2-3 to compress schedule
2. **Coordination**: Use project management tools, regular standups
3. **Versions**: Automated versioning scripts, clear documentation
4. **Releases**: Automated release notes, changelog generation
5. **Branches**: Strict merge policies, automated CI/CD

## Validation

### Phase 1 Validation (Critical)
- [ ] RawRabbit builds on net9.0 and netstandard2.0
- [ ] RabbitMQ.Client 6.8.1 integration works
- [ ] All core unit tests pass
- [ ] Integration tests with real RabbitMQ pass
- [ ] Performance benchmarks within 5% of baseline
- [ ] No breaking API changes to public interfaces

### Phase 2-3 Validation (Important)
- [ ] All projects build with new core
- [ ] All existing tests pass
- [ ] Integration tests between layers work
- [ ] Sample applications still run

### Phase 4 Validation (Integration)
- [ ] All DI containers work correctly
- [ ] Compatibility layer functions as expected
- [ ] Cross-container tests pass

### Phase 5 Validation (Comprehensive)
- [ ] Full test suite passes (unit + integration)
- [ ] Performance tests show no regression
- [ ] Load tests validate stability
- [ ] Chaos tests validate resilience

### Phase 6 Validation (Polish)
- [ ] All samples run and documented
- [ ] Migration guide complete and tested
- [ ] Breaking changes documented
- [ ] Release notes complete

## Alternative Paths Forward

### If Phase 1 Fails (RabbitMQ.Client 6.x)
**Path A**: Stay on RabbitMQ.Client 5.x, minimal framework upgrade
- Pros: Lower risk, faster completion
- Cons: Technical debt remains, no new features

**Path B**: Fork RabbitMQ.Client 5.x, port to .NET 9
- Pros: Control over codebase
- Cons: Maintenance burden, no upstream fixes

**Path C**: Switch to alternative RabbitMQ library (e.g., MassTransit)
- Pros: Modern, maintained
- Cons: Complete rewrite, API breaking changes

### If Timeline Slips
**Path A**: Extend timeline, maintain quality
**Path B**: Ship partial migration (Phase 1-3 only)
**Path C**: Release as beta, extend alpha period

## Related Decisions
- ADR 0002: Target Framework Selection (TBD - Stage 2)
- ADR 0003: RabbitMQ.Client Version Strategy (TBD - Stage 2)
- ADR 0004: Polly v7 vs v8 Decision (TBD - Stage 3)
- ADR 0005: ZeroFormatter Deprecation (TBD - Stage 3)

## References
- [Migration Roadmap](../stage-1/migration-roadmap.md)
- [Dependency Matrix](../stage-1/dependency-matrix.md)
- [.NET 9 Migration Guide](https://docs.microsoft.com/dotnet/core/migration)
- [RabbitMQ.Client 6.0 Migration Guide](https://www.rabbitmq.com/dotnet-api-guide.html)

## Notes
- This ADR represents the outcome of Stage 1.2 Discovery & Analysis
- Decision based on analysis of 32 projects, dependency audit, and risk assessment
- Assumes team capacity of 1-2 developers full-time
- Timeline assumes no major blockers in Phase 1 (critical path)
- Success depends on comprehensive testing at each phase
