# RawRabbit Modernization Plan

**Project**: RawRabbit
**Current Version**: .NET Standard 1.5 / .NET Framework 4.5.1
**Target Version**: .NET 8 LTS
**Plan Created**: 2025-11-09
**Plan Owner**: Migration Coordinator
**Executive Sponsor**: [To Be Assigned]

---

## Executive Summary

### Overview

This plan outlines the comprehensive modernization strategy for forking and modernizing the abandoned RawRabbit project from .NET Standard 1.5 / .NET Framework 4.5.1 to .NET 8 LTS. The project was last updated in June 2018 and requires extensive dependency updates, particularly the critical RabbitMQ.Client migration from 5.0.1 to 7.x.

### Objectives

**Primary Objectives** (MUST achieve):
- ✅ Migrate all 25 projects from netstandard1.5/net451 → net8.0
- ✅ Update RabbitMQ.Client 5.0.1 → 7.x (critical path)
- ✅ Eliminate all CRITICAL and HIGH security vulnerabilities
- ✅ Achieve 100% test pass rate (156 existing tests)
- ✅ Maintain or improve performance (no >10% regressions)

**Secondary Objectives** (SHOULD achieve):
- ✅ Improve code coverage from ~50-65% to ≥80%
- ✅ Remove abandoned ZeroFormatter enricher
- ✅ Apply modern C# patterns (nullable reference types, records, pattern matching)
- ✅ Update all 29 dependencies to latest compatible versions
- ✅ Create comprehensive migration guide for downstream consumers

**Out of Scope** (explicitly NOT doing):
- ❌ Adding new features or operations
- ❌ UI/UX changes (this is a library)
- ❌ Complete architectural rewrite
- ❌ Supporting .NET Framework 4.5.1 (dropping legacy support)
- ❌ Community engagement (this is an internal fork)

### Timeline

- **Start Date**: [TBD - Post-Approval]
- **End Date**: [Start + 20 weeks]
- **Core Duration**: 15 weeks (75 working days)
- **Contingency Buffer**: 5 weeks (30% additional time)
- **Total**: 20 weeks (~5 calendar months)

### Team

- **Team Size**: 2 senior .NET developers (primary) + 1 QA engineer + 1 part-time architect
- **Key Roles**:
  - **Migration Coordinator** (Dev Lead): Overall orchestration, Phase 0-7 oversight
  - **Security Specialist** (Dev 1): Phase 1 security remediation, ongoing security reviews
  - **RabbitMQ Expert** (Dev 2): Phase 3 RabbitMQ.Client migration (critical path)
  - **QA Engineer**: Test infrastructure, Phase 5 performance, Phase 7 validation
  - **Architect** (Part-time): Phase 2 architecture decisions, ADR reviews
- **External Resources**:
  - Consider RabbitMQ consultant for Phase 3 review (3-5 days, $6,000-10,000)

### Budget Estimate

**Labor Costs** (assuming $150/hour blended rate):
- Core Team: 75 days × 2 devs × 8 hours × $150 = $180,000
- QA Engineer: 40 days × 8 hours × $120 = $38,400
- Architect: 15 days × 8 hours × $200 = $24,000
- Contingency (30%): $72,720
- **Total Labor**: $315,120

**External Costs**:
- RabbitMQ Consultant (optional): $6,000-10,000
- **Total Project**: $321,120-325,120

### Success Criteria

**Technical Success**:
- ✅ All 25 projects successfully target net8.0
- ✅ Solution builds with zero errors/warnings
- ✅ 100% test pass rate (all 156+ tests passing)
- ✅ Security score ≥45 (zero CRITICAL/HIGH vulnerabilities)
- ✅ Code coverage ≥80% (up from ~50-65%)
- ✅ No performance regression >10% on benchmarks
- ✅ RabbitMQ.Client 7.x integrated and tested

**Business Success**:
- ✅ Delivered within 20 weeks
- ✅ Cost within budget ±10%
- ✅ Comprehensive migration guide (800+ lines)
- ✅ All documentation updated
- ✅ Internal NuGet packages published
- ✅ Team trained on .NET 8 and RabbitMQ.Client 7.x

---

## Assessment Summary

**Overall Assessment Score**: 62/100 - ⚠️ **PROCEED WITH CAUTION**

### Assessment Breakdown

| Dimension | Score | Key Finding |
|-----------|-------|-------------|
| Technical Viability | 68/100 | Clear path but RabbitMQ.Client 5→7 is massive undertaking |
| Business Value | 48/100 | Abandoned project, only valuable if forking/maintaining |
| Risk Profile | HIGH | 2 CRITICAL risks, 5 HIGH risks |
| Resources | 60/100 | Need senior .NET dev + RabbitMQ expertise |
| Code Quality | 74/100 | Excellent middleware architecture, clean code |
| Test Coverage | 64/100 | 156 tests, ~50-65% coverage, needs expansion |
| Security | 35/100 | 7 years of unpatched CVEs - URGENT |
| Dependencies | 55/100 | 29 packages, ~90% outdated |

### Critical Assessment Findings

**Strengths**:
1. ✅ **Excellent Architecture**: Middleware pipeline pattern is elegant and extensible
2. ✅ **Clean Code**: Well-structured, follows SOLID principles, average 49 LOC per file
3. ✅ **Modular Design**: 25 projects cleanly separated (core, operations, enrichers, DI)
4. ✅ **Test Foundation**: 156 unit tests provide safety net for refactoring
5. ✅ **Clear Migration Path**: .NET Standard 1.5 → .NET 8 is well-documented

**Critical Risks**:
1. 🔴 **RabbitMQ.Client 5.0.1 → 7.x**: Massive breaking changes (API redesign, async overhaul)
2. 🔴 **Abandoned Project**: You become permanent owner, no upstream support
3. 🟡 **Security Emergency**: 7 years of unpatched CVEs if currently in production
4. 🟡 **ZeroFormatter**: Abandoned enricher must be removed (breaking change)
5. 🟡 **Team Expertise Gap**: Requires RabbitMQ.Client 7.x knowledge (rare skill)

### Key Risks from Assessment

| Risk ID | Risk | Probability | Impact | Severity | Mitigation Strategy |
|---------|------|-------------|--------|----------|---------------------|
| R01 | RabbitMQ.Client 5→7 breaks core functionality | **HIGH** | **CRITICAL** | **CRITICAL** | Phased migration (5→6→7), extensive integration testing, RabbitMQ consultant review |
| R02 | Abandoned project long-term liability | **HIGH** | **HIGH** | **CRITICAL** | Accept as strategic decision, plan quarterly maintenance |
| R03 | ZeroFormatter replacement breaks serialization | **MEDIUM** | **HIGH** | **HIGH** | Remove enricher entirely, announce breaking change to consumers |
| R04 | Hidden async/threading bugs surface | **MEDIUM** | **HIGH** | **HIGH** | Comprehensive integration tests with real RabbitMQ instance |
| R05 | Dependency conflicts across 29 packages | **MEDIUM** | **MEDIUM** | **MEDIUM** | Careful lock file management, update one at a time |
| R06 | Team lacks RabbitMQ/async expertise | **MEDIUM** | **HIGH** | **HIGH** | 1 week training, RabbitMQ consultant, pair programming |
| R07 | Timeline overruns due to complexity | **HIGH** | **MEDIUM** | **HIGH** | 30% contingency buffer, weekly checkpoints, phased approach |
| R08 | Breaking API changes to downstream consumers | **MEDIUM** | **HIGH** | **HIGH** | Semantic versioning (3.0.0), comprehensive migration guide |

### Mitigation Strategies

**For CRITICAL Risks**:

**R01: RabbitMQ.Client Migration**
- **Prevention**: Allocate 12-18 days (30% of timeline) to this single dependency
- **Approach**: Incremental migration 5.0.1 → 6.x → 7.x (two-step process)
- **Detection**: Integration tests against real RabbitMQ instance (Docker)
- **Response**: RabbitMQ consultant review before Phase 3 completion
- **Contingency**: If 7.x fails, settle for 6.x LTS and document upgrade path

**R02: Abandoned Project Liability**
- **Prevention**: Strategic fork decision documented and approved by executive sponsor
- **Approach**: Allocate 2 days/quarter for ongoing maintenance (written into plan)
- **Detection**: Quarterly dependency vulnerability scans
- **Response**: Security patches within 2 weeks of CVE disclosure
- **Contingency**: Plan migration to MassTransit if maintenance burden exceeds capacity

---

## Scope

### In Scope

**Framework Migration**:
- ✅ Migrate all 25 projects: netstandard1.5;net451 → net8.0
- ✅ Remove .NET Framework 4.5.1 support (breaking change)
- ✅ Single target framework (simplification)

**Core Library** (1 project):
- ✅ RawRabbit (core bus client, pipe infrastructure)

**Operations** (8 projects):
- ✅ RawRabbit.Operations.Get
- ✅ RawRabbit.Operations.MessageSequence
- ✅ RawRabbit.Operations.Publish
- ✅ RawRabbit.Operations.Request
- ✅ RawRabbit.Operations.Respond
- ✅ RawRabbit.Operations.StateMachine
- ✅ RawRabbit.Operations.Subscribe
- ✅ RawRabbit.Operations.Tools

**Enrichers** (10 projects):
- ✅ RawRabbit.Enrichers.Attributes
- ✅ RawRabbit.Enrichers.GlobalExecutionId
- ✅ RawRabbit.Enrichers.HttpContext
- ✅ RawRabbit.Enrichers.MessageContext (+ Subscribe, Respond variants)
- ✅ RawRabbit.Enrichers.MessagePack
- ✅ RawRabbit.Enrichers.Polly
- ✅ RawRabbit.Enrichers.Protobuf
- ✅ RawRabbit.Enrichers.QueueSuffix
- ✅ RawRabbit.Enrichers.RetryLater
- ❌ RawRabbit.Enrichers.ZeroFormatter (**REMOVE** - abandoned dependency)

**Dependency Injection** (3 projects):
- ✅ RawRabbit.DependencyInjection.Autofac
- ✅ RawRabbit.DependencyInjection.Ninject
- ✅ RawRabbit.DependencyInjection.ServiceCollection

**Legacy Compatibility** (1 project):
- ✅ RawRabbit.Compatibility.Legacy

**Dependency Updates** (29 packages):
- ✅ RabbitMQ.Client: 5.0.1 → 7.x (CRITICAL PATH)
- ✅ Newtonsoft.Json: 10.0.1 → 13.x
- ✅ Polly: 5.3.1 → 8.x
- ✅ Autofac: 4.1.0 → 8.x
- ✅ MessagePack: 1.7.3.4 → 2.x
- ✅ protobuf-net: 2.3.2 → 3.x
- ✅ All test dependencies (xUnit, Moq, etc.)
- ❌ ZeroFormatter: **REMOVE**

**Testing**:
- ✅ Migrate 4 test projects to .NET 8
- ✅ Ensure 100% pass rate on 156 existing tests
- ✅ Add integration tests for RabbitMQ.Client 7.x
- ✅ Expand coverage from ~50-65% to ≥80%
- ✅ Performance benchmarks (BenchmarkDotNet)

**Documentation**:
- ✅ CHANGELOG.md (breaking changes, new features, security fixes)
- ✅ MIGRATION-GUIDE.md (800+ lines, comprehensive upgrade path)
- ✅ Update README.md
- ✅ Architecture Decision Records (ADRs) for major decisions
- ✅ API documentation updates

### Out of Scope

**Not Modernizing**:
- ❌ Sample projects (will update if time permits, but not required)
- ❌ Performance test project (will verify it runs but not optimize)
- ❌ Documentation website (focus on markdown docs only)

**Not Adding**:
- ❌ New features, operations, or enrichers
- ❌ Support for new message brokers (Kafka, Azure Service Bus, etc.)
- ❌ GraphQL, gRPC, or other protocol support

**Not Changing**:
- ❌ Core middleware architecture (it's excellent as-is)
- ❌ Public API surface (minimize breaking changes where possible)
- ❌ Library philosophy or design patterns

**Explicitly Removing**:
- ❌ .NET Framework 4.5.1 target (breaking change, documented)
- ❌ ZeroFormatter enricher (abandoned dependency)
- ❌ Ninject support (consider removing if time-consuming, deprioritize)

### Success Criteria

**Technical Success** (ALL required):
- ✅ All 25 projects build successfully on net8.0
- ✅ Zero compilation errors or warnings
- ✅ 100% test pass rate (156+ tests, all passing)
- ✅ Security scan shows zero CRITICAL/HIGH vulnerabilities
- ✅ Code coverage ≥80% (currently ~50-65%)
- ✅ Performance benchmarks show ≤10% regression (acceptable) or improvements
- ✅ RabbitMQ.Client 7.x integrated with full integration test suite passing
- ✅ No broken downstream dependencies (semantic versioning applied)

**Business Success** (ALL required):
- ✅ Project delivered within 20 weeks (including 30% contingency)
- ✅ Budget variance ≤10% of estimate
- ✅ MIGRATION-GUIDE.md completed (≥800 lines)
- ✅ CHANGELOG.md documents all breaking changes
- ✅ Internal NuGet packages published to private feed
- ✅ Team has learned .NET 8 and RabbitMQ.Client 7.x (knowledge transfer complete)
- ✅ Ongoing maintenance plan documented (quarterly updates)

---

## Phase Breakdown

### Phase 0: Discovery & Assessment

**Duration**: 3-5 days (24-40 hours)
**Team**: Migration Coordinator (lead), Security Specialist, Architect
**Dependencies**: None (starting phase)
**Agents**: Migration Coordinator + Security Agent + Architect Agent

**Objectives**:
- Establish comprehensive baseline for all metrics
- Document current architecture and dependencies
- Identify all breaking changes and affected code
- Create detailed effort estimates for remaining phases
- Validate assessment findings with hands-on analysis

**Tasks**:

**Task 0.1: Project Inventory & Structure Analysis** (6 hours)
- Owner: Migration Coordinator
- Map all 25 source projects with dependency graph
- Document 4 test projects and their coverage
- Analyze project references and identify circular dependencies (if any)
- Create module migration order based on dependency hierarchy
- **Deliverable**: `docs/project-structure.md` with dependency graph

**Task 0.2: Security Vulnerability Baseline** (6 hours)
- Owner: Security Specialist
- Run `dotnet list package --vulnerable --include-transitive` on all projects
- Categorize vulnerabilities: CRITICAL, HIGH, MEDIUM, LOW
- Research CVE details for all CRITICAL/HIGH issues
- Calculate initial security score (expect <40 based on assessment)
- Document remediation plan for each CVE
- **Deliverable**: `docs/security-baseline.md` with CVE list and remediation matrix

**Task 0.3: Test Infrastructure Baseline** (8 hours)
- Owner: QA Engineer
- Attempt to run all 156 tests on current framework (may fail without .NET SDK)
- If runnable: Capture pass rate, failures, coverage metrics
- If not runnable: Document blockers and workarounds
- Set up test infrastructure for .NET 8 (test runners, coverage tools)
- Create baseline for performance benchmarks (BenchmarkDotNet)
- **Deliverable**: `docs/test-baseline.md` with current state and targets

**Task 0.4: RabbitMQ.Client Breaking Changes Analysis** (12 hours) ⚠️ CRITICAL
- Owner: RabbitMQ Expert (Dev 2)
- Research RabbitMQ.Client 5.0.1 → 6.x breaking changes (official migration guide)
- Research RabbitMQ.Client 6.x → 7.x breaking changes
- Map RawRabbit API usage to breaking changes
- Identify all files touching RabbitMQ.Client (~60 files estimated)
- Estimate effort for each breaking change category
- Decide: Two-phase migration (5→6→7) or direct (5→7)?
- **Deliverable**: `docs/rabbitmq-migration-strategy.md` with detailed change map

**Task 0.5: Dependency Analysis & Upgrade Matrix** (4 hours)
- Owner: Migration Coordinator
- Document all 29 unique package dependencies with current → target versions
- Identify version conflicts and resolution strategy
- Research breaking changes for each major dependency:
  - Polly 5.3.1 → 8.x
  - Autofac 4.1.0 → 8.x
  - MessagePack 1.7.3.4 → 2.x
  - protobuf-net 2.3.2 → 3.x
- Plan ZeroFormatter removal strategy (announcement, migration path for consumers)
- **Deliverable**: `docs/dependency-matrix.md` with upgrade plan

**Task 0.6: Code Pattern Analysis** (4 hours)
- Owner: Architect
- Analyze usage of obsolete .NET APIs (binary serialization, etc.)
- Identify async/await patterns that need modernization
- Review reflection usage for compatibility
- Check for platform-specific code (net451 conditionals)
- Estimate code impact percentage (validate assessment's 35-45% estimate)
- **Deliverable**: `docs/code-patterns.md` with affected areas

**Exit Criteria**:
- ✅ All baseline documents created and reviewed
- ✅ Security score calculated (expect <40)
- ✅ RabbitMQ.Client migration strategy decided (5→6→7 recommended)
- ✅ Test infrastructure validated or blockers documented
- ✅ Dependency upgrade order established
- ✅ Effort estimates refined based on hands-on analysis
- ✅ Phase 1-7 timelines adjusted if needed

**Risks**:
- ⚠️ Test infrastructure may not run on current system (no .NET SDK)
  - **Mitigation**: Document expected state, set up .NET 8 environment
- ⚠️ RabbitMQ.Client 5→7 changes may be worse than expected
  - **Mitigation**: Allocate extra buffer (already 30%), consider 5→6→7 phased

**Quality Gate 1**: Post-Assessment Review
- **GO if**: All baselines captured, security score calculated, RabbitMQ strategy clear
- **NO-GO if**: Cannot establish baseline, RabbitMQ migration appears impossible
- **Decision Maker**: Executive Sponsor + Migration Coordinator

---

### Phase 1: Security Remediation

**Duration**: 5-8 days (40-64 hours)
**Team**: Security Specialist (lead), Dev 1, QA Engineer
**Dependencies**: Phase 0 complete
**Agents**: Security Agent (lead) + Coder Agent + Tester Agent

**Objectives**:
- Eliminate all CRITICAL vulnerabilities (target: 0)
- Eliminate all HIGH vulnerabilities (target: 0)
- Achieve security score ≥45
- Maintain 100% test pass rate (or document baseline if tests not running)
- Prepare foundation for framework migration

**Priority**: Update dependencies with security patches WITHOUT breaking changes first, then address dependencies requiring code changes.

**Tasks**:

**Task 1.1: Fix CRITICAL Vulnerabilities** (8-12 hours) 🔴 URGENT
- Owner: Security Specialist
- Address Newtonsoft.Json 10.0.1 CVEs:
  - CVE-2018-11093 (High severity) - Update to 10.0.3 minimum
  - Update to 13.x (latest stable, ~2024)
  - Test serialization compatibility
- Address any CRITICAL CVEs in transitive dependencies
- Verify fixes with `dotnet list package --vulnerable`
- Run test suite after each update
- **Deliverable**: Zero CRITICAL CVEs, updated packages documented

**Task 1.2: Fix HIGH Vulnerabilities** (12-16 hours)
- Owner: Security Specialist + Dev 1
- Update RabbitMQ.Client (if CVEs exist in 5.0.1):
  - **NOTE**: May defer to Phase 3 if breaking changes required
  - Patch to latest 5.x if available (security only)
- Update xUnit, Moq, Microsoft.NET.Test.Sdk (test dependencies)
- Update ASP.NET Core dependencies (samples, enrichers)
- Address transitive dependency CVEs
- **Deliverable**: Zero HIGH CVEs or documented deferrals

**Task 1.3: Safe Dependency Updates** (8-12 hours)
- Owner: Dev 1
- Update dependencies with minimal breaking changes:
  - Newtonsoft.Json: 10.0.1 → 13.x ✅ (mostly compatible)
  - xUnit: 2.3.0 → 2.9.x ✅ (compatible)
  - Moq: 4.7.137 → 4.20.x ✅ (mostly compatible)
  - Microsoft.NET.Test.Sdk: 15.0.0 → 17.x ✅
  - Stateless: 3.0.0 → 5.x ✅ (check compatibility)
- Run full test suite after each update
- Document any API changes required
- **Deliverable**: Safe dependencies updated, tests passing

**Task 1.4: Security Scan Validation** (4 hours)
- Owner: Security Specialist
- Re-run `dotnet list package --vulnerable --include-transitive`
- Calculate new security score (target: ≥45)
- Document remaining MEDIUM/LOW vulnerabilities
- Create remediation plan for Phase 3-4 issues
- **Deliverable**: Security score ≥45, updated security report

**Task 1.5: Test Suite Validation** (4-8 hours)
- Owner: QA Engineer
- Run complete test suite (156+ tests)
- Target: 100% pass rate (or maintain baseline if already failing)
- Document any new failures introduced by updates
- Fix test failures or rollback problematic updates
- Update test infrastructure if needed
- **Deliverable**: Test status report, all critical tests passing

**Exit Criteria**:
- ✅ Security score ≥45 (REQUIRED)
- ✅ Zero CRITICAL vulnerabilities (REQUIRED)
- ✅ Zero HIGH vulnerabilities (REQUIRED or documented deferrals with plan)
- ✅ Test pass rate maintained or improved (100% or documented baseline)
- ✅ Safe dependencies updated (Newtonsoft.Json, xUnit, Moq, etc.)
- ✅ No breaking changes introduced to public API

**Risks**:
- ⚠️ Newtonsoft.Json 10→13 may break serialization edge cases
  - **Mitigation**: Comprehensive serialization tests, rollback plan
- ⚠️ RabbitMQ.Client CVEs may require breaking migration
  - **Mitigation**: Defer to Phase 3, document security exceptions
- ⚠️ Dependency conflicts between updated packages
  - **Mitigation**: Update one at a time, test thoroughly

**Quality Gate 2**: Post-Security Review
- **GO if**: Security score ≥45, zero CRITICAL/HIGH, tests stable
- **NO-GO if**: Unable to fix critical CVEs, test failures cascading
- **Decision Maker**: Security Lead + Migration Coordinator

---

### Phase 2: Architecture & Design

**Duration**: 4-6 days (32-48 hours)
**Team**: Architect (lead), Migration Coordinator
**Dependencies**: Phase 1 complete
**Agents**: Architect Agent (lead) + Migration Coordinator

**Objectives**:
- Document all major migration decisions in ADRs
- Create comprehensive dependency migration strategy
- Enumerate all breaking changes with impact analysis
- Define module migration order and parallel execution plan
- Align team on approach before heavy lifting begins

**Tasks**:

**Task 2.1: Create Migration ADRs** (12 hours)
- Owner: Architect
- **ADR-001: Target Framework Selection**
  - Decision: .NET 8 LTS (not .NET 9 STS)
  - Rationale: LTS support until Nov 2026, more stable
  - Consequences: Drop .NET Framework 4.5.1 support

- **ADR-002: RabbitMQ.Client Migration Strategy**
  - Decision: Two-phase migration (5.0.1 → 6.8.x → 7.x) OR direct (5.0.1 → 7.x)
  - Rationale: Based on Task 0.4 analysis
  - Consequences: Timeline impact, testing strategy

- **ADR-003: ZeroFormatter Enricher Removal**
  - Decision: Remove entirely, do not replace
  - Rationale: Abandoned project, no maintained alternative
  - Consequences: Breaking change, document in migration guide

- **ADR-004: Dependency Update Strategy**
  - Decision: Update all to latest LTS/stable versions
  - Rationale: Minimize future maintenance burden
  - Consequences: More breaking changes now, less later

- **ADR-005: Versioning Strategy**
  - Decision: Semantic versioning, major version bump (3.0.0)
  - Rationale: Breaking changes require major version
  - Consequences: Clear signal to consumers

- **Deliverable**: 5 ADRs in `docs/adr/` directory (MADR format)

**Task 2.2: Dependency Migration Matrix** (8 hours)
- Owner: Migration Coordinator
- Create comprehensive matrix for all 29 packages:

| Package | Current | Target | Breaking? | Phase | Effort | Notes |
|---------|---------|--------|-----------|-------|--------|-------|
| RabbitMQ.Client | 5.0.1 | 7.x | YES | 3 | 60h | CRITICAL PATH |
| Newtonsoft.Json | 13.x | 13.x | NO | 1 | 0h | Already updated |
| Polly | 5.3.1 | 8.x | YES | 4 | 12h | Builder API changes |
| Autofac | 4.1.0 | 8.x | YES | 4 | 8h | Registration changes |
| MessagePack | 1.7.3.4 | 2.x | YES | 4 | 8h | API redesign |
| protobuf-net | 2.3.2 | 3.x | YES | 4 | 6h | Minor changes |
| ... | ... | ... | ... | ... | ... | ... |

- Identify dependency conflicts and resolution order
- Document breaking changes for each
- **Deliverable**: `docs/dependency-matrix.xlsx` (or markdown table)

**Task 2.3: Breaking Changes Enumeration** (10 hours)
- Owner: Architect + Migration Coordinator
- **Framework Breaking Changes** (netstandard1.5 → net8.0):
  - List all System.* API changes affecting RawRabbit
  - Estimate code impact (files, lines)
  - Create search patterns for automated detection

- **RabbitMQ.Client Breaking Changes** (5.0.1 → 7.x):
  - `IModel` → `IChannel` renames
  - Connection factory changes
  - Consumer API redesign (EventingBasicConsumer → async consumer)
  - Exception handling changes
  - Topology management changes

- **Dependency Breaking Changes**:
  - Polly 5→8: Builder pattern changes
  - Autofac 4→8: Registration syntax
  - MessagePack 1→2: Serializer initialization
  - protobuf-net 2→3: Source generation

- **Deliverable**: `docs/breaking-changes-guide.md` (comprehensive, 500+ lines)

**Task 2.4: Module Migration Order** (6 hours)
- Owner: Migration Coordinator
- Analyze project dependency graph
- Define migration sequence (bottom-up):
  1. **RawRabbit** (core) - foundation for everything
  2. **Operations** (8 projects) - parallel after core
  3. **Enrichers** (9 projects, minus ZeroFormatter) - parallel after operations
  4. **DI Containers** (3 projects) - parallel after core
  5. **Legacy Compatibility** - last
  6. **Tests** - throughout, updated with each module

- Identify parallel execution opportunities:
  - After core migrates, 3 developers can work on operations simultaneously

- **Deliverable**: `docs/migration-order.md` with Gantt chart

**Task 2.5: Team Alignment Workshop** (4 hours)
- Owner: Migration Coordinator + Architect
- Review all ADRs with team
- Walk through dependency matrix
- Discuss RabbitMQ.Client migration approach
- Assign ownership for Phase 3 modules
- Q&A and risk discussion
- **Deliverable**: Meeting notes, team sign-off on approach

**Exit Criteria**:
- ✅ All 5 ADRs completed and approved
- ✅ Dependency matrix comprehensive (all 29 packages mapped)
- ✅ Breaking changes guide complete (≥500 lines)
- ✅ Module migration order established with timeline
- ✅ Team alignment achieved (workshop complete, questions answered)
- ✅ Phase 3 assignments confirmed

**Risks**:
- ⚠️ Team may disagree on RabbitMQ.Client migration approach
  - **Mitigation**: Architect makes final call, document rationale in ADR
- ⚠️ Breaking changes analysis may reveal showstoppers
  - **Mitigation**: Identify early, escalate to executive sponsor for go/no-go

**Quality Gate 3**: Post-Architecture Review
- **GO if**: All ADRs approved, team aligned, no showstoppers identified
- **NO-GO if**: Unresolvable breaking changes, team not aligned, no viable path
- **Decision Maker**: Architect + Migration Coordinator + Executive Sponsor

---

### Phase 3: Framework & Core Dependency Migration

**Duration**: 12-18 days (96-144 hours) ⚠️ CRITICAL PATH
**Team**: All developers (parallel execution)
**Dependencies**: Phase 2 complete
**Agents**: Coder Agent (multiple instances) + Tester Agent

**Objectives**:
- Migrate all 25 projects from netstandard1.5;net451 → net8.0
- Update RabbitMQ.Client 5.0.1 → 7.x (CRITICAL PATH, highest risk)
- Achieve 100% solution build success
- Maintain 100% test pass rate throughout
- Fix all compilation errors and API changes

**Parallel Execution Strategy**:

```
Timeline (days 1-18):

Day 1-3:   Core (RawRabbit) [Coordinator]
Day 4-7:   RabbitMQ.Client Migration [RabbitMQ Expert] + Core [Coordinator]
Day 8-10:  Operations Tier 1 (Publish, Subscribe) [Dev 1] + Tier 2 (Request, Respond) [Dev 2]
Day 11-12: Operations Tier 3 (Get, Tools) [Dev 1] + Tier 4 (MessageSeq, StateMachine) [Dev 2]
Day 13-15: Enrichers Tier 1 (Polly, MessageContext) [Dev 1] + Tier 2 (Protobuf, MessagePack) [Dev 2]
Day 16-17: DI Containers (Autofac, ServiceCollection, Ninject) [Dev 1]
Day 18:    Integration & Final Build [All]
```

**Tasks**:

**Task 3.1: Migrate Core Library (RawRabbit)** (24 hours) 🔴 FOUNDATION
- Owner: Migration Coordinator
- Update RawRabbit.csproj:
  - Change `<TargetFrameworks>netstandard1.5;net451</TargetFrameworks>` → `<TargetFramework>net8.0</TargetFramework>`
  - Remove .NET Framework 4.5.1 conditional compilation
  - Update package references to .NET 8 compatible versions
- Fix compilation errors:
  - Update System.* API changes
  - Fix async/await patterns if needed
  - Update reflection APIs
- Run RawRabbit.Tests
- **Deliverable**: RawRabbit core builds and tests pass on net8.0

**Task 3.2: RabbitMQ.Client Migration** (48-72 hours) 🔴 CRITICAL PATH, HIGHEST RISK
- Owner: RabbitMQ Expert (Dev 2)
- **CRITICAL**: This is the highest-risk task in the entire project

**Subtask 3.2a: Phase 1 - RabbitMQ.Client 5.0.1 → 6.8.x** (24-36 hours)
  - Update package reference: `RabbitMQ.Client` 5.0.1 → 6.8.x (latest 6.x LTS)
  - **Breaking Changes**:
    - Connection factory API changes
    - `IModel` still exists but async methods added
    - Event-based consumer changes
    - Exception handling changes
  - Update files in `src/RawRabbit/Channel/`:
    - `ChannelFactory.cs`
    - `IChannelFactory.cs`
    - Connection management code
  - Update files in `src/RawRabbit/Consumer/`:
    - Consumer implementations
    - Event handlers
  - Fix all middleware touching RabbitMQ.Client (~60 files):
    - `src/RawRabbit/Pipe/Middleware/*`
    - `src/RawRabbit.Operations.*/Middleware/*`
  - Run integration tests against RabbitMQ (Docker)
  - **Deliverable**: RabbitMQ.Client 6.x integrated, integration tests passing

**Subtask 3.2b: Phase 2 - RabbitMQ.Client 6.8.x → 7.x** (24-36 hours)
  - Update package reference: `RabbitMQ.Client` 6.8.x → 7.x (latest stable)
  - **Breaking Changes**:
    - `IModel` → `IChannel` rename (MASSIVE impact)
    - All channel operations now async by default
    - Consumer API complete redesign
    - Connection lifetime management changes
  - Update abstractions:
    - Rename/wrap `IModel` → `IChannel` throughout
    - Update all channel operations to async
    - Redesign consumer implementations
  - Fix all ~60 files again for 7.x breaking changes
  - Run comprehensive integration tests
  - Performance benchmarks (ensure no regression)
  - **Deliverable**: RabbitMQ.Client 7.x integrated, all tests passing

**Contingency**: If 7.x migration fails or shows critical issues:
  - STOP at RabbitMQ.Client 6.8.x LTS
  - Document upgrade path to 7.x for future
  - Proceed with rest of project on 6.8.x

**Task 3.3: Migrate Operations (Parallel Execution)** (32 hours total, 16 hours per dev)
- Owners: Dev 1 + Dev 2 (parallel)

**Dev 1 Stream**:
  - RawRabbit.Operations.Publish (8h)
  - RawRabbit.Operations.Subscribe (8h)
  - RawRabbit.Operations.Get (6h)
  - RawRabbit.Operations.Tools (4h)

**Dev 2 Stream**:
  - RawRabbit.Operations.Request (8h)
  - RawRabbit.Operations.Respond (8h)
  - RawRabbit.Operations.MessageSequence (6h)
  - RawRabbit.Operations.StateMachine (6h)

**For Each Operation**:
  1. Update .csproj: netstandard1.5;net451 → net8.0
  2. Fix compilation errors
  3. Update middleware for RabbitMQ.Client 7.x changes
  4. Run operation-specific tests
  5. Code review

- **Deliverable**: All 8 operation projects migrated and tested

**Task 3.4: Migrate Enrichers (Parallel Execution)** (28 hours total, 14 hours per dev)
- Owners: Dev 1 + Dev 2 (parallel)

**Dev 1 Stream**:
  - RawRabbit.Enrichers.Polly (8h - Polly 5→8 breaking changes)
  - RawRabbit.Enrichers.MessageContext (+ variants) (12h)
  - RawRabbit.Enrichers.Attributes (2h)
  - RawRabbit.Enrichers.QueueSuffix (2h)
  - RawRabbit.Enrichers.RetryLater (4h)

**Dev 2 Stream**:
  - RawRabbit.Enrichers.Protobuf (6h - protobuf-net 2→3)
  - RawRabbit.Enrichers.MessagePack (8h - MessagePack 1→2 breaking changes)
  - RawRabbit.Enrichers.GlobalExecutionId (2h)
  - RawRabbit.Enrichers.HttpContext (4h)
  - ❌ RawRabbit.Enrichers.ZeroFormatter: **SKIP** (remove in Phase 4)

- **Deliverable**: 9 enricher projects migrated (excluding ZeroFormatter)

**Task 3.5: Migrate DI Containers** (12 hours)
- Owner: Dev 1

  - RawRabbit.DependencyInjection.Autofac (6h - Autofac 4→8)
  - RawRabbit.DependencyInjection.ServiceCollection (3h - easy)
  - RawRabbit.DependencyInjection.Ninject (3h - or consider removing)

- **Deliverable**: 3 DI container projects migrated

**Task 3.6: Migrate Legacy Compatibility** (4 hours)
- Owner: Migration Coordinator
- RawRabbit.Compatibility.Legacy
- **Deliverable**: Legacy compatibility layer migrated

**Task 3.7: Integration & Full Solution Build** (8 hours)
- Owner: All developers (pair/mob programming)
- Merge all parallel streams
- Resolve merge conflicts
- Full solution build: `dotnet build RawRabbit.sln`
- Target: Zero errors, zero warnings
- Fix any integration issues
- **Deliverable**: Entire solution builds successfully on net8.0

**Task 3.8: Test Suite Execution** (8 hours)
- Owner: QA Engineer
- Run complete test suite: `dotnet test RawRabbit.sln`
- Target: 100% pass rate (156+ tests)
- Document any failures
- Work with developers to fix failures
- **Deliverable**: 100% test pass rate

**Exit Criteria**:
- ✅ All 25 projects successfully target net8.0 (REQUIRED)
- ✅ Solution builds with zero errors (REQUIRED)
- ✅ RabbitMQ.Client 6.x or 7.x integrated (REQUIRED - 7.x preferred, 6.x acceptable)
- ✅ 100% test pass rate (156+ tests all passing) (REQUIRED)
- ✅ Integration tests against real RabbitMQ passing (REQUIRED)
- ✅ No warnings in build output (nice-to-have)
- ✅ ZeroFormatter project excluded from build

**Risks**:
- 🔴 **CRITICAL**: RabbitMQ.Client 5→7 migration fails
  - **Mitigation**: Two-phase approach (5→6→7), RabbitMQ consultant review, fallback to 6.x
  - **Contingency**: Accept 6.x LTS, document 7.x upgrade path
- ⚠️ Merge conflicts during integration (Day 18)
  - **Mitigation**: Daily integration builds, Git workflow discipline
- ⚠️ Test failures cascade across projects
  - **Mitigation**: Fix tests immediately after each module migration
- ⚠️ Timeline overrun on RabbitMQ.Client migration
  - **Mitigation**: 30% buffer already allocated, can extend to 20-24 days if needed

**Quality Gate 4**: Post-Framework Migration Review
- **GO if**: 100% build success, 100% test pass, RabbitMQ.Client integrated
- **NO-GO if**: Cannot achieve 100% test pass, RabbitMQ.Client integration fails completely
- **Decision Maker**: Migration Coordinator + RabbitMQ Expert + QA Engineer

---

### Phase 4: API Modernization & Code Quality

**Duration**: 8-12 days (64-96 hours)
**Team**: Dev 1 + Dev 2 + QA Engineer
**Dependencies**: Phase 3 complete
**Agents**: Coder Agent + Tester Agent + Architect Agent

**Objectives**:
- Remove ZeroFormatter enricher (breaking change)
- Update remaining dependencies (Polly, Autofac, MessagePack, protobuf-net)
- Apply modern C# 12 patterns (nullable reference types, records, pattern matching)
- Improve code coverage from ~50-65% to ≥80%
- Reduce code complexity and technical debt
- Modernize async/await patterns

**Tasks**:

**Task 4.1: Remove ZeroFormatter Enricher** (4 hours)
- Owner: Dev 1
- Delete `src/RawRabbit.Enrichers.ZeroFormatter/` project
- Remove from solution file
- Remove integration test references
- Document breaking change in CHANGELOG.md
- Add migration note to MIGRATION-GUIDE.md
- **Deliverable**: ZeroFormatter removed, documented

**Task 4.2: Update Polly Enricher (5.3.1 → 8.x)** (12 hours)
- Owner: Dev 1
- Update `RawRabbit.Enrichers.Polly` package reference
- **Breaking Changes**:
  - Policy builder API changed
  - PolicyRegistry changes
  - Async policy patterns updated
- Refactor `src/RawRabbit.Enrichers.Polly/`:
  - `PollyExtension.cs`
  - `Middleware/*`
- Update integration with RawRabbit middleware pipeline
- Run Polly enricher tests
- **Deliverable**: Polly 8.x integrated

**Task 4.3: Update MessagePack Enricher (1.7.3.4 → 2.x)** (10 hours)
- Owner: Dev 2
- Update `RawRabbit.Enrichers.MessagePack` package reference
- **Breaking Changes**:
  - Serializer initialization changed
  - Attribute changes
  - Performance API updates
- Refactor serialization code
- Test with various message types
- Performance benchmarks (ensure improvement)
- **Deliverable**: MessagePack 2.x integrated

**Task 4.4: Update protobuf-net Enricher (2.3.2 → 3.x)** (8 hours)
- Owner: Dev 2
- Update `RawRabbit.Enrichers.Protobuf` package reference
- **Breaking Changes**:
  - Source generation support
  - API modernizations
- Refactor serialization code
- Test with protobuf messages
- **Deliverable**: protobuf-net 3.x integrated

**Task 4.5: Update Autofac DI (4.1.0 → 8.x)** (8 hours)
- Owner: Dev 1
- Update `RawRabbit.DependencyInjection.Autofac` package reference
- **Breaking Changes**:
  - Registration syntax changes
  - Module system updates
- Refactor DI registration code
- Test Autofac integration
- **Deliverable**: Autofac 8.x integrated

**Task 4.6: Apply Nullable Reference Types** (16 hours)
- Owner: Dev 1 + Dev 2 (parallel, split modules)
- Enable `<Nullable>enable</Nullable>` in all .csproj files
- Add nullability annotations to public APIs:
  - `?` for nullable parameters
  - `!` for null-forgiving where appropriate
- Fix nullability warnings (target: zero warnings)
- Update middleware pipeline for nullability
- **Deliverable**: Nullable reference types enabled, zero warnings

**Task 4.7: Apply Modern C# Patterns** (12 hours)
- Owner: Dev 1 + Dev 2 (pair programming)
- **Pattern Matching**: Replace `is` + cast with pattern matching
- **Records**: Convert immutable DTOs to records where appropriate
- **Init-only Properties**: Replace readonly with init where applicable
- **Target-typed new**: Simplify object instantiation
- **String Interpolation**: Modernize string formatting
- Run tests after each refactoring pass
- **Deliverable**: Modern C# patterns applied across codebase

**Task 4.8: Expand Test Coverage (50-65% → 80%)** (24 hours)
- Owner: QA Engineer + Dev 1
- Generate coverage report: `dotnet test --collect:"XPlat Code Coverage"`
- Identify untested areas (focus on critical paths)
- Add unit tests:
  - Middleware components
  - Enricher logic
  - Error handling paths
  - Edge cases in serialization
- Add integration tests:
  - End-to-end message flows
  - RabbitMQ topology operations
  - Retry scenarios
- Target: ≥80% coverage
- **Deliverable**: Code coverage ≥80%, 200+ tests

**Task 4.9: Code Quality Improvements** (12 hours)
- Owner: Dev 2
- Reduce cyclomatic complexity:
  - Extract complex methods
  - Simplify conditional logic
- Fix code smells:
  - Large methods (>50 lines)
  - Duplicate code
  - Magic numbers/strings
- Improve naming:
  - Clarify variable names
  - Consistent naming conventions
- Run static analysis (if tools available)
- **Deliverable**: Code quality metrics improved

**Task 4.10: Async/Await Modernization** (8 hours)
- Owner: Migration Coordinator
- Review all async methods for best practices:
  - ConfigureAwait(false) where appropriate
  - ValueTask<T> for hot paths (if beneficial)
  - Avoid async void (except event handlers)
  - Proper cancellation token propagation
- Fix any async anti-patterns
- **Deliverable**: Modern async patterns throughout

**Exit Criteria**:
- ✅ ZeroFormatter enricher removed and documented
- ✅ All enricher dependencies updated (Polly 8.x, MessagePack 2.x, protobuf-net 3.x)
- ✅ Autofac 8.x integrated
- ✅ Nullable reference types enabled with zero warnings
- ✅ Modern C# patterns applied
- ✅ Code coverage ≥80% (up from ~50-65%)
- ✅ 100% test pass rate maintained (200+ tests)
- ✅ Code quality metrics improved

**Risks**:
- ⚠️ Polly/MessagePack breaking changes worse than expected
  - **Mitigation**: Research upfront, consider sticking with older versions if critical
- ⚠️ Nullability warnings overwhelming
  - **Mitigation**: Focus on public API, suppress internal warnings if necessary
- ⚠️ Test coverage expansion time-consuming
  - **Mitigation**: Focus on critical paths, defer nice-to-have tests

**Quality Gate 5**: Post-API Modernization Review
- **GO if**: All dependencies updated, coverage ≥80%, modern patterns applied
- **NO-GO if**: Critical dependency update fails, coverage <70%
- **Decision Maker**: Migration Coordinator + Architect

---

### Phase 5: Performance Optimization & Validation

**Duration**: 5-7 days (40-56 hours)
**Team**: Dev 2 (lead), QA Engineer
**Dependencies**: Phase 4 complete
**Agents**: Coder Agent + Tester Agent

**Objectives**:
- Establish performance baselines (before/after comparison)
- Identify and fix performance regressions (if any)
- Optimize hot paths using .NET 8 features (Span<T>, ValueTask)
- Validate performance ≥ baseline (no >10% regression)
- Document performance improvements

**Tasks**:

**Task 5.1: Baseline Performance Benchmarks** (8 hours)
- Owner: QA Engineer
- Set up BenchmarkDotNet suite (already exists in `test/RawRabbit.PerformanceTest`)
- Define benchmark scenarios:
  - Publish throughput (messages/sec)
  - Subscribe latency (ms)
  - Request/Response round-trip time
  - Serialization performance (Newtonsoft.Json, MessagePack, Protobuf)
  - Middleware pipeline overhead
- Run benchmarks on .NET 8 (current state)
- **Deliverable**: Baseline performance report

**Task 5.2: Identify Performance Bottlenecks** (8 hours)
- Owner: Dev 2
- Use .NET profiler (dotnet-trace, JetBrains Profiler, or VS Profiler)
- Profile hot paths:
  - Message publishing
  - Message consumption
  - Serialization/deserialization
  - Channel operations
- Identify top 10 bottlenecks by CPU time
- Prioritize optimizations (high impact, low effort first)
- **Deliverable**: Bottleneck analysis document

**Task 5.3: Apply .NET 8 Performance Features** (16 hours)
- Owner: Dev 2
- **Span<T> and Memory<T>**:
  - Replace byte[] allocations with Span<T> in serialization
  - Use Memory<T> for buffer management
  - Reduce GC pressure

- **ValueTask<T>**:
  - Convert hot-path Task<T> → ValueTask<T>
  - Especially in middleware pipeline (frequently awaited)

- **ArrayPool<T>**:
  - Pool byte arrays in serialization paths
  - Reduce allocations

- **String Interning**:
  - Intern routing keys and queue names (if beneficial)

- **Struct Enums** (if applicable):
  - Consider struct optimizations for frequently passed types

- Run benchmarks after each optimization
- **Deliverable**: Performance optimizations applied

**Task 5.4: Fix Performance Regressions** (8-12 hours)
- Owner: Dev 2
- Compare current benchmarks to baseline
- Identify any regressions >10%
- Root cause analysis for regressions
- Fix or rollback changes causing regressions
- Re-run benchmarks
- **Deliverable**: Zero regressions >10%

**Task 5.5: Integration Performance Testing** (8 hours)
- Owner: QA Engineer
- Set up RabbitMQ instance (Docker)
- Run load tests:
  - 1,000 messages/sec publish
  - 10,000 total messages
  - Measure latency distribution (p50, p95, p99)
  - Measure memory consumption
  - Measure CPU utilization
- Compare to expected performance
- **Deliverable**: Load test report

**Task 5.6: Performance Documentation** (4 hours)
- Owner: Dev 2
- Document performance improvements:
  - Before/after benchmarks
  - Optimization techniques applied
  - Expected gains
- Add performance best practices to docs
- **Deliverable**: Performance documentation

**Exit Criteria**:
- ✅ Baseline benchmarks established
- ✅ Performance ≥ baseline (no regression >10%) (REQUIRED)
- ✅ Optimizations applied (Span<T>, ValueTask, ArrayPool)
- ✅ Load tests passing (1,000 msg/sec sustained)
- ✅ Performance documentation complete

**Risks**:
- ⚠️ .NET 8 runtime performance worse than expected
  - **Mitigation**: Research known issues, optimize further, document
- ⚠️ RabbitMQ.Client 7.x slower than 5.0.1
  - **Mitigation**: Profile and optimize, consider 6.x if critical
- ⚠️ Time spent on diminishing returns
  - **Mitigation**: Set time box, accept "good enough" performance

**Quality Gate 6**: Post-Performance Review
- **GO if**: Performance ≥ baseline, no critical regressions
- **NO-GO if**: Critical performance regression (>20%) cannot be fixed
- **Decision Maker**: Migration Coordinator + Architect

---

### Phase 6: Comprehensive Documentation

**Duration**: 5-7 days (40-56 hours)
**Team**: Migration Coordinator (lead), all developers (contributors)
**Dependencies**: Phase 5 complete
**Agents**: Documentation Agent (lead) + Migration Coordinator

**Objectives**:
- Create comprehensive CHANGELOG.md
- Create detailed MIGRATION-GUIDE.md (≥800 lines)
- Update all README files
- Compile all ADRs
- Document architecture changes
- Create release notes

**Tasks**:

**Task 6.1: CHANGELOG.md** (12 hours)
- Owner: Migration Coordinator
- Document all changes in keep-a-changelog format:

  **[3.0.0] - 2025-XX-XX**

  **Added**:
  - .NET 8 LTS support
  - Nullable reference types throughout
  - Modern C# 12 patterns
  - Performance optimizations (Span<T>, ValueTask)
  - 50+ new tests (coverage 50% → 80%)

  **Changed**:
  - RabbitMQ.Client 5.0.1 → 7.x (BREAKING)
  - Polly 5.3.1 → 8.x (BREAKING)
  - MessagePack 1.7.3.4 → 2.x (BREAKING)
  - Autofac 4.1.0 → 8.x (BREAKING)
  - Newtonsoft.Json 10.0.1 → 13.x
  - All test dependencies updated

  **Removed**:
  - .NET Framework 4.5.1 support (BREAKING)
  - .NET Standard 1.5 support (BREAKING)
  - ZeroFormatter enricher (BREAKING)

  **Fixed**:
  - All CRITICAL/HIGH security vulnerabilities (7 years of CVEs)
  - Async/await anti-patterns
  - Code quality issues

  **Security**:
  - CVE-2018-11093 (Newtonsoft.Json)
  - [List all fixed CVEs]

- **Deliverable**: Complete CHANGELOG.md

**Task 6.2: MIGRATION-GUIDE.md** (24 hours) 🔴 CRITICAL DELIVERABLE
- Owner: Migration Coordinator + Dev 1
- Create comprehensive upgrade guide (≥800 lines):

  **Table of Contents**:
  1. Overview
  2. Prerequisites
  3. Breaking Changes Summary
  4. Step-by-Step Migration
  5. RabbitMQ.Client 5 → 7 Migration
  6. Dependency Updates
  7. Code Changes Required
  8. Testing Strategy
  9. Troubleshooting
  10. Rollback Plan

  **Key Sections**:
  - **Breaking Changes**: Detailed list with before/after code examples
  - **RabbitMQ.Client Migration**: Comprehensive guide for the biggest change
    - `IModel` → `IChannel` mapping
    - Consumer API changes
    - Connection factory changes
  - **ZeroFormatter Removal**: Migration path for affected users
  - **Code Examples**: 30+ before/after snippets
  - **Common Issues**: FAQ and troubleshooting

- **Deliverable**: MIGRATION-GUIDE.md (≥800 lines, comprehensive)

**Task 6.3: Update README.md** (4 hours)
- Owner: Dev 1
- Update main README:
  - Change .NET version requirements
  - Update NuGet package versions
  - Update code samples for .NET 8
  - Add migration guide link
  - Update badges (build, NuGet versions)
- **Deliverable**: Updated README.md

**Task 6.4: Architecture Documentation** (8 hours)
- Owner: Architect
- Update architecture documentation:
  - `docs/architecture.md`: Update for .NET 8 changes
  - `docs/middleware-pipeline.md`: Document any changes
  - Diagrams (if any): Update versions
- Create new documentation:
  - `docs/rabbitmq-integration.md`: RabbitMQ.Client 7.x integration guide
  - `docs/performance.md`: Performance best practices
- **Deliverable**: Architecture docs updated

**Task 6.5: ADR Compilation** (4 hours)
- Owner: Architect
- Compile all ADRs from Phase 2:
  - ADR-001 through ADR-005
- Create ADR index: `docs/adr/README.md`
- Link ADRs from main docs
- **Deliverable**: ADR index and links

**Task 6.6: API Documentation** (6 hours)
- Owner: Dev 2
- Update XML documentation comments:
  - Add missing docs to public APIs
  - Update examples for .NET 8
  - Document breaking changes in obsolete APIs (if any)
- Generate API documentation (if DocFX or similar used)
- **Deliverable**: API docs updated

**Task 6.7: Release Notes** (4 hours)
- Owner: Migration Coordinator
- Create release notes for 3.0.0:
  - Executive summary
  - Highlights (top 5 changes)
  - Breaking changes
  - Security fixes
  - Performance improvements
  - Upgrade instructions (link to migration guide)
- **Deliverable**: RELEASE-NOTES-3.0.0.md

**Task 6.8: Documentation Review** (4 hours)
- Owner: All team members
- Peer review all documentation:
  - Technical accuracy
  - Clarity and completeness
  - Code examples compile
  - Links work
- Fix issues found in review
- **Deliverable**: Documentation reviewed and approved

**Exit Criteria**:
- ✅ CHANGELOG.md complete (all changes documented)
- ✅ MIGRATION-GUIDE.md ≥800 lines (comprehensive) (REQUIRED)
- ✅ README.md updated
- ✅ Architecture docs updated
- ✅ All ADRs compiled and indexed
- ✅ API documentation updated
- ✅ Release notes complete
- ✅ Documentation peer reviewed

**Risks**:
- ⚠️ Migration guide may be overwhelming (800+ lines)
  - **Mitigation**: Clear structure, lots of examples, FAQ section
- ⚠️ Documentation may become stale during Phase 7 changes
  - **Mitigation**: Minor updates allowed, major structure locked

**Quality Gate 7**: Post-Documentation Review
- **GO if**: All documentation complete, migration guide ≥800 lines, reviewed
- **NO-GO if**: Migration guide insufficient, critical gaps
- **Decision Maker**: Migration Coordinator + all team members

---

### Phase 7: Final Validation & Release

**Duration**: 4-6 days (32-48 hours)
**Team**: QA Engineer (lead), all developers (support)
**Dependencies**: Phase 6 complete
**Agents**: Tester Agent (lead) + Security Agent + Migration Coordinator

**Objectives**:
- Execute complete test suite (unit, integration, E2E, performance)
- Final security scan (verify score ≥45, zero CRITICAL/HIGH)
- Prepare release artifacts (NuGet packages, Git tags)
- Production readiness assessment
- GO/NO-GO decision

**Tasks**:

**Task 7.1: Complete Unit Test Execution** (6 hours)
- Owner: QA Engineer
- Run all unit tests: `dotnet test --filter "FullyQualifiedName~Tests" --no-build`
- Target: 100% pass rate (200+ tests)
- Generate coverage report
- Verify coverage ≥80%
- **Deliverable**: Unit test report (100% pass, ≥80% coverage)

**Task 7.2: Complete Integration Test Execution** (8 hours)
- Owner: QA Engineer
- Set up RabbitMQ instance (Docker): `docker run -d -p 5672:5672 rabbitmq:3`
- Run integration tests: `dotnet test --filter "FullyQualifiedName~IntegrationTests"`
- Test scenarios:
  - Publish/Subscribe flows
  - Request/Response (RPC)
  - Topology operations (queue/exchange declare, bind)
  - Retry scenarios (RetryLater enricher)
  - State machine transitions
  - All enrichers (Polly, MessagePack, Protobuf, etc.)
- Target: 100% pass rate
- **Deliverable**: Integration test report (100% pass)

**Task 7.3: End-to-End Scenario Testing** (8 hours)
- Owner: QA Engineer + Dev 1
- Test real-world scenarios:
  - **Scenario 1**: Multi-service pub/sub
  - **Scenario 2**: Request/response with timeout
  - **Scenario 3**: Message context propagation
  - **Scenario 4**: Polly retry policies
  - **Scenario 5**: Delayed retry with RetryLater
  - **Scenario 6**: State machine workflow
- Use sample applications (if available)
- Verify behavior matches expectations
- **Deliverable**: E2E test report

**Task 7.4: Performance Validation** (4 hours)
- Owner: QA Engineer
- Re-run BenchmarkDotNet suite
- Compare to Phase 5 baseline
- Verify no regressions introduced since Phase 5
- Document final performance numbers
- **Deliverable**: Final performance report

**Task 7.5: Final Security Scan** (4 hours)
- Owner: Security Specialist
- Run vulnerability scan: `dotnet list package --vulnerable --include-transitive`
- Verify security score ≥45 (REQUIRED)
- Verify zero CRITICAL vulnerabilities (REQUIRED)
- Verify zero HIGH vulnerabilities (REQUIRED)
- Document any remaining MEDIUM/LOW vulnerabilities
- **Deliverable**: Final security report (score ≥45)

**Task 7.6: Build Validation** (4 hours)
- Owner: Migration Coordinator
- Clean build from scratch:
  ```bash
  git clean -xdf
  dotnet restore
  dotnet build --configuration Release
  dotnet pack --configuration Release --output ./artifacts
  ```
- Verify zero errors, zero warnings
- Verify NuGet packages created (25 packages)
- Test package installation in clean project
- **Deliverable**: Clean build success, packages validated

**Task 7.7: Release Artifact Preparation** (6 hours)
- Owner: Migration Coordinator
- Create Git tag: `v3.0.0`
- Package NuGet packages:
  - All 25 projects → NuGet .nupkg files
  - Version: 3.0.0
  - Include dependencies with correct versions
- Prepare release notes
- Create GitHub release (or internal release)
- **Deliverable**: Release artifacts ready

**Task 7.8: Production Readiness Assessment** (4 hours)
- Owner: Migration Coordinator + Architect
- Review all quality gates (1-7):
  - ✅ Gate 1: Assessment complete
  - ✅ Gate 2: Security score ≥45
  - ✅ Gate 3: Architecture approved
  - ✅ Gate 4: Framework migrated, 100% tests pass
  - ✅ Gate 5: API modernized, coverage ≥80%
  - ✅ Gate 6: Performance validated
  - ✅ Gate 7: Documentation complete

- Checklist review:
  - [ ] All tests passing (100%)
  - [ ] Security score ≥45
  - [ ] Documentation complete
  - [ ] Performance validated
  - [ ] Release artifacts prepared
  - [ ] Team trained
  - [ ] Rollback plan documented

- **Deliverable**: Production readiness report

**Task 7.9: GO/NO-GO Decision Meeting** (2 hours)
- Owner: Executive Sponsor
- Attendees: Executive Sponsor, Migration Coordinator, Architect, Security Lead, QA Lead
- Review production readiness assessment
- Review all quality gate results
- Discuss any remaining risks
- **Decision**: GO or NO-GO for release
- **Deliverable**: GO/NO-GO decision documented

**Exit Criteria** (ALL REQUIRED for GO):
- ✅ 100% unit test pass rate (200+ tests)
- ✅ 100% integration test pass rate
- ✅ 100% E2E scenario tests pass
- ✅ Security score ≥45 (REQUIRED)
- ✅ Zero CRITICAL vulnerabilities (REQUIRED)
- ✅ Zero HIGH vulnerabilities (REQUIRED)
- ✅ Code coverage ≥80% (REQUIRED)
- ✅ Performance ≥ baseline (no >10% regression) (REQUIRED)
- ✅ Clean build with zero errors/warnings (REQUIRED)
- ✅ All documentation complete (REQUIRED)
- ✅ Release artifacts prepared (REQUIRED)
- ✅ Production readiness assessment approved (REQUIRED)

**Risks**:
- ⚠️ Last-minute test failure discovered
  - **Mitigation**: Fix immediately, re-run validation, or NO-GO if critical
- ⚠️ Security scan reveals new CVE
  - **Mitigation**: Assess severity, patch if CRITICAL/HIGH, or NO-GO
- ⚠️ Performance regression found
  - **Mitigation**: Root cause analysis, fix or document, or NO-GO if >20%

**Quality Gate 8**: Final GO/NO-GO Decision
- **GO if**: ALL exit criteria met, no blockers, team confident
- **NO-GO if**: ANY critical exit criterion fails (security, tests, performance)
- **Decision Maker**: Executive Sponsor

---

## Timeline & Milestones

### Gantt Chart

```
Week 1:     Phase 0 (Discovery)                      ████
Week 2:     Phase 1 (Security)                           ████████
Week 3-4:   Phase 2 (Architecture)                           ████████
Week 5-7:   Phase 3 (Framework Migration) ⚠️               ████████████████
            - Core (Days 1-3)                                ████
            - RabbitMQ.Client (Days 4-10) 🔴                     ████████████
            - Operations (Days 8-12)                                 ████████
            - Enrichers (Days 13-15)                                         ████
            - Integration (Days 16-18)                                           ████
Week 8-9:   Phase 4 (API Modernization)                                           ████████████
Week 10-11: Phase 5 (Performance)                                                             ████████
Week 12-13: Phase 6 (Documentation)                                                                   ████████
Week 14-15: Phase 7 (Validation)                                                                              ████████
Week 16-20: Contingency Buffer (30%)                                                                                  ████████████████
```

**Timeline Breakdown**:
- **Weeks 1-2**: Foundation (Discovery + Security) - 8-13 days
- **Weeks 3-4**: Planning (Architecture) - 4-6 days
- **Weeks 5-7**: Heavy Lifting (Framework Migration) 🔴 - 12-18 days (CRITICAL PATH)
- **Weeks 8-9**: Polish (API Modernization) - 8-12 days
- **Weeks 10-11**: Optimization (Performance) - 5-7 days
- **Weeks 12-13**: Documentation - 5-7 days
- **Weeks 14-15**: Final Validation - 4-6 days
- **Weeks 16-20**: Contingency (30% buffer) - 5 weeks

**Total**: 46-69 days core work + 25 days contingency = **71-94 days** (14-19 weeks)
**Planned**: 15 weeks core + 5 weeks buffer = **20 weeks total**

### Milestones

| Milestone | Target Week | Deliverables | Success Criteria | Owner |
|-----------|-------------|--------------|------------------|-------|
| **M0: Kickoff** | Week 1 | Project structure, baselines | All baselines documented | Coordinator |
| **M1: Assessment Complete** | Week 2 | Security baseline, RabbitMQ strategy | Security score calculated, plan approved | Security Lead |
| **M2: Security Remediated** | Week 3 | Security score ≥45 | Zero CRITICAL/HIGH CVEs | Security Lead |
| **M3: Architecture Approved** | Week 4 | 5 ADRs, dependency matrix | Team aligned, ADRs approved | Architect |
| **M4: Core Migrated** | Week 5 | RawRabbit core on net8.0 | Core builds and tests pass | Coordinator |
| **M5: RabbitMQ.Client Migrated** 🔴 | Week 7 | RabbitMQ.Client 7.x integrated | Integration tests pass | RabbitMQ Expert |
| **M6: Framework Migration Complete** | Week 7 | All 25 projects on net8.0 | Solution builds, 100% tests pass | Coordinator |
| **M7: Dependencies Updated** | Week 9 | Polly, MessagePack, Autofac updated | All dependencies current | Dev 1 + Dev 2 |
| **M8: Code Modernized** | Week 9 | Modern C#, coverage ≥80% | Coverage target met | QA Engineer |
| **M9: Performance Validated** | Week 11 | Benchmarks, optimizations | No regressions >10% | Dev 2 |
| **M10: Documentation Complete** | Week 13 | CHANGELOG, migration guide | Migration guide ≥800 lines | Coordinator |
| **M11: Validation Complete** | Week 15 | All tests pass, security ≥45 | All quality gates passed | QA Engineer |
| **M12: GO/NO-GO Decision** | Week 15 | Production readiness assessment | GO decision | Executive Sponsor |
| **M13: Release** | Week 15 | NuGet packages, Git tag | v3.0.0 released | Coordinator |

### Critical Path

**The critical path runs through RabbitMQ.Client migration**:

```
Phase 0 (1 week) → Phase 1 (1 week) → Phase 2 (1 week) →
Phase 3: RabbitMQ.Client Migration (2-3 weeks) 🔴 →
Phase 4 (2 weeks) → Phase 5 (1 week) → Phase 6 (1 week) → Phase 7 (1 week)
```

**Total Critical Path**: 10-12 weeks + 30% buffer = 13-16 weeks

Any delay in RabbitMQ.Client migration directly impacts project completion date.

---

## Resource Allocation

### Team Structure

**Core Team** (full-time):
1. **Migration Coordinator** (Senior .NET Developer)
   - Role: Overall project orchestration, Phase 0-7 leadership
   - Allocation: 100% (15 weeks)
   - Skills: .NET Framework → Core migration, project management

2. **RabbitMQ Expert** (Senior .NET Developer)
   - Role: Phase 3 RabbitMQ.Client migration (CRITICAL PATH)
   - Allocation: 100% (15 weeks, focused on weeks 5-7)
   - Skills: RabbitMQ, AMQP, async/await, distributed systems

3. **QA Engineer**
   - Role: Testing, performance validation, Phase 7 lead
   - Allocation: 50% (8 weeks full-time equivalent)
   - Skills: xUnit, integration testing, BenchmarkDotNet

**Part-Time Support**:
4. **Architect**
   - Role: Phase 2 architecture decisions, ADR reviews
   - Allocation: 20% (3 weeks full-time equivalent)
   - Skills: System architecture, decision-making, technical writing

**External Resources** (optional):
5. **RabbitMQ Consultant**
   - Role: Phase 3 review, RabbitMQ.Client 7.x expertise
   - Allocation: 3-5 days (as needed)
   - Cost: $6,000-10,000

### Phase-by-Phase Allocation

| Phase | Duration | Coordinator | RabbitMQ Expert | QA Engineer | Architect |
|-------|----------|-------------|-----------------|-------------|-----------|
| 0: Discovery | 3-5 days | 100% (Lead) | 50% | 25% | 25% |
| 1: Security | 5-8 days | 50% | 50% (Lead) | 25% | 0% |
| 2: Architecture | 4-6 days | 50% | 0% | 0% | 100% (Lead) |
| 3: Framework | 12-18 days | 75% (Lead) | 100% (Critical) | 25% | 0% |
| 4: API Modernization | 8-12 days | 50% | 50% | 50% | 25% |
| 5: Performance | 5-7 days | 25% | 75% (Lead) | 100% (Support) | 0% |
| 6: Documentation | 5-7 days | 100% (Lead) | 25% | 0% | 50% |
| 7: Validation | 4-6 days | 75% | 25% | 100% (Lead) | 25% |

### Capacity Planning

**Total Project Hours**:
- Migration Coordinator: 15 weeks × 40 hours = 600 hours
- RabbitMQ Expert: 15 weeks × 40 hours = 600 hours
- QA Engineer: 8 weeks × 40 hours = 320 hours
- Architect: 3 weeks × 40 hours = 120 hours
- **Total**: 1,640 hours

**Cost Breakdown** (blended rates):
- Coordinator: 600h × $150 = $90,000
- RabbitMQ Expert: 600h × $150 = $90,000
- QA Engineer: 320h × $120 = $38,400
- Architect: 120h × $200 = $24,000
- **Subtotal**: $242,400
- Contingency (30%): $72,720
- **Total Labor**: $315,120
- RabbitMQ Consultant (optional): $6,000-10,000
- **Grand Total**: $321,120-325,120

### Hiring Needs

**If team doesn't exist, hire**:
- ✅ **Senior .NET Developer** with migration experience (2 positions)
  - Must have: .NET Framework → Core experience, RabbitMQ knowledge (nice to have)
  - Interview focus: Async/await, dependency migration, debugging skills

- ✅ **QA Engineer** with .NET testing experience
  - Must have: xUnit, integration testing, performance testing
  - Interview focus: Test design, BenchmarkDotNet, Docker/RabbitMQ

**If hiring, add 2-3 weeks for onboarding**.

---

## Risk Management

### Risk Register

| ID | Risk | Probability | Impact | Severity | Mitigation | Owner | Status |
|----|------|-------------|--------|----------|------------|-------|--------|
| **R01** | RabbitMQ.Client 5→7 breaks core functionality | **HIGH** | **CRITICAL** | **CRITICAL** | Two-phase migration (5→6→7), extensive integration testing, RabbitMQ consultant review, fallback to 6.x LTS | RabbitMQ Expert | Active |
| **R02** | Abandoned project long-term liability | **HIGH** | **HIGH** | **CRITICAL** | Strategic fork decision documented, quarterly maintenance plan, budget allocated | Executive Sponsor | Accepted |
| **R03** | ZeroFormatter replacement breaks serialization | **MEDIUM** | **HIGH** | **HIGH** | Remove enricher entirely, announce breaking change, provide migration path in guide | Dev 1 | Planned |
| **R04** | Hidden async/threading bugs surface | **MEDIUM** | **HIGH** | **HIGH** | Comprehensive integration tests with real RabbitMQ, load testing, code review | QA Engineer | Monitoring |
| **R05** | Dependency conflicts across 29 packages | **MEDIUM** | **MEDIUM** | **MEDIUM** | Lock file management, update one dependency at a time, test after each | Coordinator | Monitoring |
| **R06** | Team lacks RabbitMQ/async expertise | **MEDIUM** | **HIGH** | **HIGH** | 1 week training on RabbitMQ.Client 7.x, consultant review, pair programming | Coordinator | Planned |
| **R07** | Timeline overruns due to complexity | **HIGH** | **MEDIUM** | **HIGH** | 30% contingency buffer, weekly checkpoints, phased approach, early escalation | Coordinator | Monitoring |
| **R08** | Breaking API changes to downstream consumers | **MEDIUM** | **HIGH** | **HIGH** | Semantic versioning (3.0.0), comprehensive migration guide (800+ lines), early communication | Coordinator | Planned |
| **R09** | Performance regression from framework changes | **LOW** | **MEDIUM** | **MEDIUM** | Baseline benchmarks, continuous monitoring, optimization in Phase 5 | Dev 2 | Monitoring |
| **R10** | Test infrastructure doesn't run on current system | **MEDIUM** | **LOW** | **MEDIUM** | Document expected state, set up .NET 8 environment, may need to baseline during Phase 1 | QA Engineer | Known |
| **R11** | Key team member leaves mid-project | **LOW** | **MEDIUM** | **MEDIUM** | Knowledge sharing sessions, comprehensive documentation, pair programming | Coordinator | Monitoring |
| **R12** | Security scan reveals new CRITICAL CVE mid-project | **LOW** | **HIGH** | **HIGH** | Immediate patch process, security monitoring, escalation path | Security Lead | Monitoring |
| **R13** | .NET 8 introduces unexpected breaking changes | **LOW** | **MEDIUM** | **MEDIUM** | Research .NET 8 changes upfront (Phase 0), test early, community resources | Coordinator | Monitoring |
| **R14** | RabbitMQ.Client consultant unavailable | **MEDIUM** | **MEDIUM** | **MEDIUM** | Line up consultant early, have backup options, allocate more internal time | Coordinator | Planned |
| **R15** | Documentation insufficient for consumers | **LOW** | **HIGH** | **HIGH** | 800-line migration guide requirement, peer review, consumer feedback | Coordinator | Monitoring |

### Risk Response Strategies

**For CRITICAL Risks**:

**R01: RabbitMQ.Client Migration Failure**
- **Prevention**:
  - Allocate 12-18 days (30% of project) to this single dependency
  - Two-phase approach: 5.0.1 → 6.8.x LTS → 7.x (safer than direct)
  - RabbitMQ Expert dedicated to this task
  - Daily integration testing against real RabbitMQ instance

- **Detection**:
  - Integration tests fail after RabbitMQ.Client update
  - Performance benchmarks show >20% regression
  - API incompatibilities cannot be resolved

- **Response**:
  - **If 5.0.1 → 6.x fails**: Escalate immediately, consider staying on 5.0.1 with security patches
  - **If 6.x → 7.x fails**: STOP at 6.8.x LTS (supported), document 7.x upgrade path for future
  - Engage RabbitMQ consultant for expert review

- **Contingency**:
  - Acceptable outcome: RabbitMQ.Client 6.8.x LTS (still modern, supported until 2026)
  - Document upgrade path to 7.x in Phase 8 (future work)
  - Allocate +5 days if consultant engagement needed

**R02: Abandoned Project Liability**
- **Prevention**: This is a strategic decision, not a technical risk
- **Acceptance**:
  - Executive sponsor approved forking decision
  - Quarterly maintenance allocated (2 days/quarter = 8 days/year)
  - Security monitoring in place (CVE alerts)

- **Contingency**:
  - If maintenance burden exceeds capacity (>5 days/quarter):
    - Re-evaluate: Plan migration to MassTransit (actively maintained)
    - Cost-benefit analysis: Is internal fork still cheaper?

**For HIGH Risks**:

**R06: Team Expertise Gap**
- **Prevention**:
  - Week 1 (Phase 0): Dedicate 2-3 days to RabbitMQ.Client 7.x research
  - Study official migration guide: https://www.rabbitmq.com/dotnet-api-guide.html
  - Hands-on tutorials with RabbitMQ.Client 7.x
  - Pair programming during Phase 3 (RabbitMQ Expert + Coordinator)

- **Response**:
  - If expertise gap is larger than expected: Engage consultant earlier (Week 4 vs Week 7)
  - Budget: $6,000-10,000 for 3-5 days consultant time

**R07: Timeline Overruns**
- **Prevention**:
  - 30% contingency buffer (5 weeks)
  - Weekly checkpoint meetings
  - Burn-down chart tracking
  - Early escalation (don't wait until crisis)

- **Response** (if trending toward >30% overrun):
  - **Week 8**: If Phase 3 overruns, descope Phase 4 secondary objectives
  - **Week 12**: If still behind, defer Phase 5 optimizations to post-release
  - **Week 15**: If critical, NO-GO decision, extend timeline

**R08: Breaking Changes Impact**
- **Prevention**:
  - Semantic versioning 3.0.0 (clear signal)
  - 800+ line migration guide (comprehensive)
  - Early communication to internal consumers (if any)
  - Deprecation warnings in 2.x (if time permits)

- **Response**:
  - If consumers report migration issues: Provide support, update guide
  - Consider releasing 2.x security patch as interim option

### Risk Monitoring

**Weekly Risk Review** (30 minutes):
- Review risk register
- Update probabilities based on current state
- Identify new risks
- Escalate HIGH/CRITICAL risks to executive sponsor

**Risk Escalation Path**:
1. Team Member → Migration Coordinator (within 24 hours)
2. Migration Coordinator → Architect/Security Lead (within 24 hours)
3. Architect → Engineering Manager (within 48 hours)
4. Engineering Manager → CTO/Executive Sponsor (within 72 hours)

**Risk Reporting**:
- Weekly status report includes top 5 risks
- Monthly executive update includes risk mitigation progress
- GO/NO-GO decision reviews all CRITICAL/HIGH risks

---

## Quality Gates & Decision Points

### Gate Criteria

**Gate 1: Post-Assessment Review** (End of Phase 0)
- **Criteria**:
  - ✅ All baselines documented (security, test, performance)
  - ✅ RabbitMQ.Client migration strategy decided (5→6→7 or 5→7)
  - ✅ Dependency matrix complete (all 29 packages mapped)
  - ✅ Effort estimates refined and approved
  - ✅ Budget approved ($321k-325k)

- **GO if**: All criteria met, no showstoppers, budget approved
- **NO-GO if**: RabbitMQ migration appears impossible, budget rejected
- **Decision Maker**: Executive Sponsor + Migration Coordinator
- **Outcome**: Proceed to Phase 1 or cancel project

**Gate 2: Post-Security Remediation** (End of Phase 1)
- **Criteria**:
  - ✅ Security score ≥45 (REQUIRED)
  - ✅ Zero CRITICAL vulnerabilities (REQUIRED)
  - ✅ Zero HIGH vulnerabilities (REQUIRED or documented deferrals)
  - ✅ Test pass rate maintained or improved
  - ✅ Safe dependencies updated (Newtonsoft.Json, xUnit, etc.)

- **GO if**: Security score ≥45, zero CRITICAL/HIGH
- **NO-GO if**: Cannot remediate critical CVEs, cascading test failures
- **Decision Maker**: Security Lead + Migration Coordinator
- **Outcome**: Proceed to Phase 2 or halt for security fixes

**Gate 3: Post-Architecture Planning** (End of Phase 2)
- **Criteria**:
  - ✅ All 5 ADRs approved (framework, RabbitMQ, ZeroFormatter, dependencies, versioning)
  - ✅ Dependency matrix complete and reviewed
  - ✅ Breaking changes guide complete (≥500 lines)
  - ✅ Team aligned (workshop complete)
  - ✅ Module migration order established

- **GO if**: All ADRs approved, team aligned, no unresolvable breaking changes
- **NO-GO if**: Showstopper breaking changes, team not aligned, no viable path
- **Decision Maker**: Architect + Migration Coordinator + Executive Sponsor
- **Outcome**: Proceed to Phase 3 or re-plan

**Gate 4: Post-Framework Migration** (End of Phase 3) 🔴 CRITICAL
- **Criteria**:
  - ✅ All 25 projects target net8.0 (REQUIRED)
  - ✅ Solution builds with zero errors (REQUIRED)
  - ✅ RabbitMQ.Client 6.x or 7.x integrated (7.x preferred, 6.x acceptable) (REQUIRED)
  - ✅ 100% test pass rate (156+ tests) (REQUIRED)
  - ✅ Integration tests against RabbitMQ passing (REQUIRED)

- **GO if**: All criteria met, especially 100% test pass and RabbitMQ integration
- **NO-GO if**: Cannot achieve 100% test pass, RabbitMQ integration fails entirely
- **Decision Maker**: Migration Coordinator + RabbitMQ Expert + QA Engineer
- **Outcome**: Proceed to Phase 4 or extend Phase 3 (use buffer)

**Gate 5: Post-API Modernization** (End of Phase 4)
- **Criteria**:
  - ✅ All enricher dependencies updated (Polly 8.x, MessagePack 2.x, protobuf-net 3.x)
  - ✅ ZeroFormatter removed and documented
  - ✅ Nullable reference types enabled, zero warnings
  - ✅ Code coverage ≥80% (up from ~50-65%)
  - ✅ Modern C# patterns applied
  - ✅ 100% test pass rate maintained (200+ tests)

- **GO if**: Coverage ≥80%, all dependencies updated, tests passing
- **NO-GO if**: Critical dependency update fails, coverage <70%
- **Decision Maker**: Migration Coordinator + Architect
- **Outcome**: Proceed to Phase 5 or fix issues

**Gate 6: Post-Performance Validation** (End of Phase 5)
- **Criteria**:
  - ✅ Baseline benchmarks established
  - ✅ Performance ≥ baseline (no regression >10%) (REQUIRED)
  - ✅ Optimizations applied (Span<T>, ValueTask, etc.)
  - ✅ Load tests passing (1,000 msg/sec sustained)
  - ✅ Performance documentation complete

- **GO if**: Performance ≥ baseline (no >10% regression)
- **NO-GO if**: Critical performance regression (>20%) cannot be fixed
- **Decision Maker**: Migration Coordinator + Architect
- **Outcome**: Proceed to Phase 6 or optimize further

**Gate 7: Post-Documentation** (End of Phase 6)
- **Criteria**:
  - ✅ CHANGELOG.md complete
  - ✅ MIGRATION-GUIDE.md ≥800 lines (comprehensive) (REQUIRED)
  - ✅ README.md updated
  - ✅ All ADRs compiled and indexed
  - ✅ Architecture docs updated
  - ✅ Release notes complete
  - ✅ Documentation peer reviewed

- **GO if**: Migration guide ≥800 lines, all docs complete and reviewed
- **NO-GO if**: Migration guide insufficient (<800 lines or major gaps)
- **Decision Maker**: Migration Coordinator + all team members
- **Outcome**: Proceed to Phase 7 or complete documentation

**Gate 8: Final GO/NO-GO Decision** (End of Phase 7) 🔴 CRITICAL
- **Criteria** (ALL REQUIRED):
  - ✅ 100% unit test pass rate (200+ tests)
  - ✅ 100% integration test pass rate
  - ✅ 100% E2E scenario tests pass
  - ✅ Security score ≥45
  - ✅ Zero CRITICAL vulnerabilities
  - ✅ Zero HIGH vulnerabilities
  - ✅ Code coverage ≥80%
  - ✅ Performance ≥ baseline (no >10% regression)
  - ✅ Clean build with zero errors/warnings
  - ✅ All documentation complete (CHANGELOG, migration guide, README, ADRs)
  - ✅ Release artifacts prepared (NuGet packages, Git tag)
  - ✅ Production readiness assessment approved

- **GO if**: ALL criteria met, no blockers, team confident
- **NO-GO if**: ANY critical criterion fails
- **Decision Maker**: Executive Sponsor (final authority)
- **Outcome**: RELEASE v3.0.0 or defer with action plan

### Decision Matrix

| Scenario | Gate 1 | Gate 2 | Gate 3 | Gate 4 | Gate 5 | Gate 6 | Gate 7 | Gate 8 | Action |
|----------|--------|--------|--------|--------|--------|--------|--------|--------|--------|
| All criteria met | GO | GO | GO | GO | GO | GO | GO | GO | **RELEASE** |
| Security score 40 (not 45) | - | NO-GO | - | - | - | - | - | NO-GO | Fix security |
| RabbitMQ.Client 7.x fails, 6.x works | - | - | - | **GO** | - | - | - | GO | Accept 6.x |
| Test pass rate 95% (not 100%) | - | - | - | NO-GO | - | - | - | NO-GO | Fix tests |
| Coverage 75% (not 80%) | - | - | - | - | **GO** | - | - | GO | Document deviation |
| Performance regression 15% | - | - | - | - | - | NO-GO | - | - | Optimize or accept |
| Migration guide 700 lines (not 800) | - | - | - | - | - | - | NO-GO | - | Expand guide |

---

## Contingency Plans

### Scenario 1: RabbitMQ.Client 7.x Migration Fails

**Trigger**: Phase 3, Task 3.2b fails - cannot migrate to RabbitMQ.Client 7.x

**Impact**:
- CRITICAL - This is the highest-risk task
- Timeline: Potentially +5-10 days if troubleshooting
- Cost: Potentially +$6,000-10,000 for consultant

**Response Plan**:
1. **Immediate** (Day 1 of failure):
   - STOP further 7.x migration work
   - Assess: Is 6.8.x working? (from Task 3.2a)
   - Decision point: Accept 6.8.x LTS vs. keep trying 7.x

2. **If 6.8.x is working** (Preferred path):
   - **ACCEPT 6.8.x LTS** as final state
   - RabbitMQ.Client 6.8.x is LTS, supported until ~2026
   - Document decision in ADR-006
   - Update MIGRATION-GUIDE.md: Note 6.8.x vs. 7.x, future upgrade path
   - Update timeline: Reclaim 2-3 days from Phase 3
   - **Outcome**: Proceed to Phase 4, successful modernization on 6.8.x

3. **If 6.8.x also has issues**:
   - Engage RabbitMQ consultant immediately (within 24 hours)
   - Consultant reviews integration approach
   - Pair programming: Consultant + RabbitMQ Expert
   - Timeline: +3-5 days
   - Cost: +$6,000-10,000

4. **If consultant cannot resolve**:
   - Escalate to executive sponsor (NO-GO scenario)
   - Options:
     - A) Stay on RabbitMQ.Client 5.0.1, patch security issues only
     - B) Abandon modernization, migrate to MassTransit instead
     - C) Extend timeline significantly (+4-6 weeks) for custom RabbitMQ wrapper

**Contingency Budget**: $10,000 for consultant, +10 days timeline

---

### Scenario 2: Timeline Overrun >30%

**Trigger**: Week 12, project is >30% behind schedule (>6 weeks delay)

**Impact**:
- HIGH - Exhausted contingency buffer
- Cost: Potentially +$50,000-100,000 (additional labor)
- Business: Delayed modernization, continued security exposure

**Response Plan**:
1. **Root Cause Analysis** (immediately):
   - Identify phase(s) causing delay
   - Was it RabbitMQ.Client? Dependency conflicts? Team capacity?
   - Document lessons learned

2. **Descope Secondary Objectives**:
   - **Phase 4**: Skip modern C# patterns (nullable types, records) → Save 12-16 hours
   - **Phase 5**: Skip performance optimizations → Save 16-24 hours
   - **Phase 6**: Reduce migration guide from 800 to 500 lines → Save 8 hours
   - **Total Savings**: ~5-7 days

3. **Increase Team Capacity**:
   - Add third developer (if budget allows)
   - Increase QA Engineer to full-time (from 50%)
   - Overtime (not recommended, diminishing returns)

4. **Phased Release**:
   - **Release 3.0.0-alpha**: Framework + RabbitMQ.Client only (end of Phase 3)
   - **Release 3.0.0-beta**: Add dependency updates (end of Phase 4)
   - **Release 3.0.0-final**: Complete modernization (end of Phase 7)
   - Allows partial value delivery while finishing work

5. **Executive Decision**:
   - Present options to executive sponsor
   - Extend timeline vs. descope vs. increase budget
   - Get approval for chosen path

**Contingency Budget**: +$50,000-100,000 or accept descoped objectives

---

### Scenario 3: Security Scan Reveals New CRITICAL CVE Mid-Project

**Trigger**: Week 8 (during Phase 4), new CVE-2025-XXXX disclosed for .NET 8 or dependency

**Impact**:
- CRITICAL (if in RabbitMQ.Client or .NET runtime)
- Timeline: +1-3 days for immediate patch
- Risk: May require re-migration if dependency changes

**Response Plan**:
1. **Immediate Assessment** (within 4 hours):
   - Security Lead reviews CVE details
   - Severity: CRITICAL, HIGH, MEDIUM, LOW?
   - Affected package and version
   - Patch available?

2. **CRITICAL CVE Response** (within 24 hours):
   - STOP all work except security patch
   - Update affected package immediately
   - Run full test suite
   - If tests fail: Fix immediately (allocate entire team)
   - If patch breaks RabbitMQ.Client integration: Escalate

3. **HIGH CVE Response** (within 72 hours):
   - Prioritize patch in current phase
   - Update package during next dependency update window
   - Monitor for exploit activity

4. **If Patch Breaks Integration**:
   - Assess: Can we work around? Alternative package?
   - If no workaround: Escalate to executive sponsor
   - May need to delay release until patch is compatible

**Contingency**: Allocate 2-3 days buffer for mid-project security patches (already in 30% buffer)

---

### Scenario 4: Key Team Member Leaves Mid-Project

**Trigger**: Week 6 (during Phase 3), RabbitMQ Expert or Migration Coordinator gives notice

**Impact**:
- CRITICAL (if RabbitMQ Expert during Phase 3)
- Timeline: +2-4 weeks for knowledge transfer and backfill
- Cost: Recruiting, onboarding, potential consultant

**Response Plan**:

**If RabbitMQ Expert Leaves**:
1. **Immediate** (Day 1 of notice):
   - Assess progress on RabbitMQ.Client migration
   - If nearly complete: Have them finish before departure
   - If early stage: Knowledge transfer to Migration Coordinator

2. **Knowledge Transfer** (2 weeks):
   - Daily pairing sessions
   - Document all RabbitMQ integration decisions
   - Record walkthroughs of critical code
   - Update ADR-002 with all details

3. **Backfill Options**:
   - **Option A**: Promote Migration Coordinator to RabbitMQ Expert role (if capable)
   - **Option B**: Hire replacement (2-3 weeks recruiting + 1 week onboarding)
   - **Option C**: Engage RabbitMQ consultant full-time (expensive but fast)

4. **Timeline Impact**:
   - Best case: +1 week (knowledge transfer)
   - Worst case: +4 weeks (recruiting + onboarding)

**If Migration Coordinator Leaves**:
1. Promote senior developer to coordinator role
2. Knowledge transfer on project plan, risks, decisions
3. Executive sponsor takes more active role
4. Timeline impact: +1-2 weeks

**Prevention**:
- Pair programming throughout project (knowledge sharing)
- Comprehensive documentation (ADRs, guides)
- Weekly knowledge transfer sessions
- Retention bonuses for critical team members (if budget allows)

**Contingency Budget**: $10,000-20,000 for consultant or recruiting costs

---

### Scenario 5: Test Pass Rate Drops Below 80% and Cannot Be Fixed

**Trigger**: End of Phase 3 or Phase 4, test pass rate is 75% (40+ failing tests)

**Impact**:
- CRITICAL - Blocks Quality Gate 4 or 5
- Timeline: +1-2 weeks to fix tests
- Quality: Cannot release with failing tests

**Response Plan**:
1. **Triage Failing Tests** (immediately):
   - Categorize failures:
     - Type A: Test needs update for new framework/API (fix test)
     - Type B: Real regression in code (fix code)
     - Type C: Flaky test (fix or remove)
     - Type D: Obsolete test (remove)

2. **Focus on Type B (Real Regressions)** (highest priority):
   - These are actual bugs introduced by migration
   - Fix immediately, all hands on deck
   - Cannot proceed without fixing these

3. **Fix Type A (Test Updates)**:
   - Update tests for new APIs
   - Update expected behaviors for .NET 8
   - Usually straightforward

4. **Handle Type C (Flaky Tests)**:
   - Fix root cause (timing issues, async problems)
   - If cannot fix quickly: Mark as [Skip] with issue tracker
   - Document as technical debt

5. **Remove Type D (Obsolete Tests)**:
   - If test is for removed feature (e.g., .NET 4.5.1 specific): Delete
   - Document removal in CHANGELOG

6. **If Cannot Reach 100%**:
   - Minimum acceptable: 95% pass rate (10 tests can fail)
   - Document all failures with plans to fix
   - Executive sponsor approval required
   - Post-release work to fix remaining 5%

**Contingency**: Allocate +1-2 weeks from buffer to fix tests

---

### Scenario 6: Performance Regression >20% Cannot Be Fixed

**Trigger**: End of Phase 5, benchmarks show 25% slower performance vs. baseline

**Impact**:
- HIGH - Blocks Quality Gate 6
- Acceptable regression: ≤10%
- Current: 25% (unacceptable)

**Response Plan**:
1. **Root Cause Analysis** (2-3 days):
   - Profile with dotnet-trace or JetBrains Profiler
   - Identify bottleneck (RabbitMQ.Client? Serialization? Framework?)
   - Compare .NET 8 vs. old framework (apples to apples)

2. **Mitigation Strategies**:
   - **If RabbitMQ.Client 7.x is slower**:
     - Research known performance issues in 7.x
     - Consider reverting to 6.x LTS (if that's faster)
     - Optimize integration code (connection pooling, etc.)

   - **If .NET 8 runtime is slower**:
     - This is unlikely (.NET 8 is generally faster)
     - Check for debug mode vs. release mode
     - Verify proper optimization flags

   - **If serialization is slower**:
     - Profile Newtonsoft.Json, MessagePack, Protobuf
     - Consider System.Text.Json migration (major work)
     - Optimize hot paths with Span<T>

3. **Accept Regression Decision**:
   - If regression is <25% and cannot be fixed:
     - Document in RELEASE-NOTES.md
     - Explain trade-off (security + modern framework vs. performance)
     - Get executive sponsor approval
     - Plan Phase 8 (post-release) for further optimization

4. **NO-GO Decision**:
   - If regression is >25% AND performance is business-critical:
     - Cannot release
     - Options:
       - Allocate +2-4 weeks for deep optimization
       - Revert to older framework (abandon modernization)
       - Migrate to alternative library (MassTransit)

**Contingency**: Accept up to 20% regression with documentation, or +2-4 weeks optimization time

---

### Scenario 7: Documentation Deemed Insufficient (Migration Guide <800 Lines)

**Trigger**: End of Phase 6, migration guide is 650 lines, team review finds major gaps

**Impact**:
- MEDIUM - Blocks Quality Gate 7
- Business: Downstream consumers cannot migrate successfully
- Timeline: +2-3 days to expand documentation

**Response Plan**:
1. **Gap Analysis** (4 hours):
   - Review migration guide against checklist
   - Missing sections? Insufficient examples? Unclear instructions?
   - Get feedback from team (mock consumer perspective)

2. **Expand Migration Guide** (16-24 hours):
   - Add missing sections (RabbitMQ.Client details most likely)
   - Add 20-30 more code examples (before/after)
   - Expand troubleshooting section
   - Add FAQ (10-15 common questions)
   - Target: 800-1000 lines

3. **Peer Review** (4 hours):
   - Have non-expert team member read guide
   - Can they understand the migration steps?
   - Identify confusing areas
   - Refine based on feedback

4. **If Still Insufficient**:
   - Allocate +3 days from buffer
   - Consider creating video walkthroughs (alternative to text)
   - Create migration examples repository (code samples)

**Contingency**: +2-3 days from buffer to complete documentation

---

## Communication Plan

### Status Reporting

**Daily Standup** (15 minutes, team only):
- Format: What did I do? What will I do? Any blockers?
- Attendees: Migration Coordinator, RabbitMQ Expert, Dev 1, Dev 2, QA Engineer
- Time: 9:00 AM daily
- Output: Quick sync, blocker identification

**Weekly Status Report** (written, 30 minutes to prepare):
- Audience: Stakeholders, Engineering Manager
- Content:
  - Phase progress (X% complete)
  - Milestones achieved this week
  - Milestones planned next week
  - Top 5 risks and mitigations
  - Blockers needing escalation
  - Budget status (hours spent vs. planned)
- Format: Email or project management tool
- Timing: Every Friday by 4 PM

**Bi-Weekly Executive Update** (30 minutes meeting):
- Audience: Executive Sponsor, CTO, Engineering Manager
- Content:
  - High-level progress (on track / at risk / behind)
  - Major accomplishments
  - Quality gate results
  - Top 3 risks
  - Budget and timeline status
  - Decisions needed
- Timing: Every other Wednesday at 2 PM

**Monthly Board Update** (if applicable):
- Audience: Board of Directors, C-Suite
- Content:
  - Project status (Red/Yellow/Green)
  - Business impact (security improvements, modernization progress)
  - Timeline and budget
  - Major risks
- Timing: Monthly board meeting

### Escalation Path

**Issue Severity Levels**:
- **P0 (Critical)**: Project-blocking, immediate escalation
  - Example: RabbitMQ.Client migration completely fails
- **P1 (High)**: Major risk, escalate within 24 hours
  - Example: Timeline overrun >20%
- **P2 (Medium)**: Needs attention, escalate within 3 days
  - Example: Test pass rate drops to 90%
- **P3 (Low)**: Monitor, escalate if unresolved in 1 week
  - Example: Documentation gap

**Escalation Chain**:
1. **Level 1: Team Lead / Migration Coordinator**
   - For: P3, P2 issues
   - Response time: 24-48 hours
   - Authority: Adjust task priorities, reallocate team

2. **Level 2: Architect / Security Lead**
   - For: P2, P1 issues (technical)
   - Response time: 12-24 hours
   - Authority: Technical decisions, ADR changes

3. **Level 3: Engineering Manager**
   - For: P1 issues (resource/timeline)
   - Response time: 24 hours
   - Authority: Budget adjustments, team expansion, timeline extension

4. **Level 4: CTO**
   - For: P1, P0 issues
   - Response time: 12 hours
   - Authority: Major timeline/budget changes, strategic decisions

5. **Level 5: Executive Sponsor**
   - For: P0 issues, GO/NO-GO decisions
   - Response time: 24 hours
   - Authority: Final authority on all decisions, project cancellation

**Escalation Examples**:
- **P0**: RabbitMQ.Client 7.x migration fails completely
  - **Path**: Coordinator → Architect (same day) → CTO (next day) → Executive Sponsor (decision meeting)

- **P1**: Timeline overrun 25% (beyond buffer)
  - **Path**: Coordinator → Engineering Manager (same day) → CTO (decision within 48 hours)

- **P2**: Test pass rate drops to 85%
  - **Path**: QA Engineer → Coordinator (same day) → Architect (if code issue) → resolve or escalate

### Communication Channels

**Primary**:
- **Slack/Teams**: Daily communication, quick questions
  - Channel: #rawrabbit-modernization
  - @mentions for urgent items

**Secondary**:
- **Email**: Weekly status reports, formal communications
- **Project Management Tool** (Jira/Azure DevOps): Task tracking, progress
- **Video Calls**: Standups, weekly sync, executive updates
- **Documentation**: Shared drive for ADRs, reports, guides

### Stakeholder Matrix

| Stakeholder | Role | Interest | Communication | Frequency |
|-------------|------|----------|---------------|-----------|
| Executive Sponsor | Decision authority | High | Email, meetings | Bi-weekly |
| CTO | Technical oversight | High | Email, escalations | Monthly |
| Engineering Manager | Resource allocation | High | Weekly report, meetings | Weekly |
| Architect | Technical direction | High | Daily standup, ADR reviews | Daily |
| Security Lead | Security compliance | Medium | Weekly report, Gate 2 review | Weekly |
| QA Lead | Quality assurance | Medium | Weekly report, Gate 7/8 reviews | Weekly |
| Downstream Consumers | Migration impact | Medium | Migration guide, release notes | At release |

---

## Appendices

### A. Dependency Migration Matrix

| Package | Current | Target | Breaking Changes? | Phase | Effort (hours) | Notes |
|---------|---------|--------|-------------------|-------|----------------|-------|
| **RabbitMQ.Client** | 5.0.1 | 7.x | **YES (MAJOR)** | 3 | 48-72 | CRITICAL PATH - API redesign, async overhaul |
| Newtonsoft.Json | 10.0.1 | 13.x | Minor | 1 | 2 | Mostly compatible, CVE fixes |
| Polly | 5.3.1 | 8.x | **YES** | 4 | 12 | Builder API changes, async policies |
| Autofac | 4.1.0 | 8.x | **YES** | 4 | 8 | Registration syntax changes |
| Ninject | 3.2.2 / 4.0.0-beta | 4.x | Minor | 4 | 4 | Consider removing |
| MessagePack | 1.7.3.4 | 2.x | **YES** | 4 | 10 | Serializer initialization, attributes |
| protobuf-net | 2.3.2 | 3.x | **YES** | 4 | 8 | Source generation, API updates |
| ZeroFormatter | N/A | **REMOVE** | **YES** | 4 | 4 | Delete enricher entirely |
| xUnit | 2.3.0 | 2.9.x | NO | 1 | 2 | Compatible upgrade |
| Moq | 4.7.137 | 4.20.x | Minor | 1 | 2 | Mostly compatible |
| Microsoft.NET.Test.Sdk | 15.0.0 | 17.x | NO | 1 | 1 | Compatible |
| BenchmarkDotNet | 0.10.3 | 0.13.x | Minor | 5 | 2 | Performance testing |
| Stateless | 3.0.0 | 5.x | Minor | 4 | 2 | State machine library |
| Microsoft.Extensions.DependencyInjection | 1.0.2 | 8.x | Minor | 4 | 4 | DI container |
| Microsoft.Extensions.Configuration.* | 1.0.2 / 2.0.0 | 8.x | Minor | 4 | 4 | Configuration system |
| Microsoft.AspNetCore.* | 1.0.3 / 2.0.0 | 8.x | Minor | 4 | 4 | ASP.NET Core integration |
| Serilog.* | 2.0.2 / 3.0.0 | Latest | Minor | 4 | 2 | Logging (samples) |

**Total Effort**: ~180-210 hours for all dependency updates (distributed across Phases 1, 3, 4)

### B. RabbitMQ.Client 5.0.1 → 7.x Breaking Changes

**Phase 1: 5.0.1 → 6.8.x** (Estimated 24-36 hours):

| Breaking Change | Affected Code | Remediation | Effort |
|-----------------|---------------|-------------|--------|
| Connection factory API changes | `src/RawRabbit/Channel/ChannelFactory.cs` | Update factory instantiation | 4h |
| Async methods added (but sync still exists) | All channel operations | Begin async migration | 8h |
| Event-based consumer changes | `src/RawRabbit/Consumer/` | Update consumer implementations | 8h |
| Exception handling changes | Error handling middleware | Update exception types | 4h |
| Topology management API updates | Queue/exchange declaration | Update API calls | 4h |

**Phase 2: 6.8.x → 7.x** (Estimated 24-36 hours):

| Breaking Change | Affected Code | Remediation | Effort |
|-----------------|---------------|-------------|--------|
| `IModel` → `IChannel` rename | ~60 files throughout codebase | Rename all references, update abstractions | 12h |
| All operations async by default | All channel operations | Convert sync → async patterns | 12h |
| Consumer API complete redesign | `src/RawRabbit/Consumer/` | Rewrite consumer implementations | 8h |
| Connection lifetime management | Connection factory, disposal | Update connection handling | 4h |
| Acknowledgment patterns changed | Ack/Nack middleware | Update acknowledgment calls | 4h |

**Total RabbitMQ.Client Migration**: 48-72 hours (2-3 weeks dedicated work)

**Affected Files** (estimated 60 files):
- `src/RawRabbit/Channel/*.cs` (5 files)
- `src/RawRabbit/Consumer/*.cs` (3 files)
- `src/RawRabbit/Pipe/Middleware/*.cs` (15 files)
- `src/RawRabbit.Operations.*/Middleware/*.cs` (30 files)
- `src/RawRabbit.Operations.Tools/*.cs` (7 files)

### C. Test Strategy

**Unit Tests** (156+ existing → 200+ target):
- Framework: xUnit 2.9.x
- Mocking: Moq 4.20.x
- Coverage Target: ≥80% (up from ~50-65%)
- Execution: `dotnet test --filter "FullyQualifiedName~Tests"`
- Timing: <2 minutes for full suite

**Integration Tests** (existing + new):
- Framework: xUnit 2.9.x
- RabbitMQ: Docker container (`docker run -d -p 5672:5672 rabbitmq:3`)
- Scenarios:
  - Publish/Subscribe message flows
  - Request/Response (RPC with direct reply-to)
  - Topology operations (queue/exchange/binding)
  - Enricher integrations (Polly, MessagePack, Protobuf)
  - Retry scenarios (RetryLater enricher)
  - State machine workflows
- Execution: `dotnet test --filter "FullyQualifiedName~IntegrationTests"`
- Timing: 5-10 minutes (depends on RabbitMQ)

**Performance Tests**:
- Framework: BenchmarkDotNet 0.13.x
- Scenarios:
  - Publish throughput (messages/sec)
  - Subscribe latency (ms)
  - Request/Response round-trip time (ms)
  - Serialization performance (Newtonsoft.Json, MessagePack, Protobuf)
  - Middleware pipeline overhead (μs)
- Baseline: Capture in Phase 0 or Phase 5
- Execution: `dotnet run --project test/RawRabbit.PerformanceTest -c Release`
- Timing: 30-60 minutes for full suite

**E2E Scenario Tests** (new in Phase 7):
- Real-world scenarios:
  - Multi-service pub/sub
  - Request/response with timeout
  - Message context propagation across services
  - Polly retry policies in action
  - Delayed retry with RetryLater
  - State machine workflow (multi-step)
- Execution: Manual or automated scripts
- Timing: 1-2 hours

**Test Execution Strategy**:
- **Daily**: Unit tests (every commit)
- **Daily**: Integration tests (during Phase 3-7)
- **Weekly**: Performance benchmarks (during Phase 5)
- **Pre-Release**: Full suite (unit + integration + E2E + performance)

### D. Deployment Strategy

**Internal Fork Deployment**:
- Target: Internal NuGet feed (Azure Artifacts, ProGet, or similar)
- Not publishing to public NuGet.org (this is a fork)

**NuGet Package Creation**:
```bash
# Clean build
dotnet clean
dotnet restore

# Build Release
dotnet build --configuration Release

# Pack all projects
dotnet pack --configuration Release --output ./artifacts --no-build

# Expected output: 25 .nupkg files in ./artifacts/
```

**Package Versioning**:
- Version: **3.0.0** (major version bump for breaking changes)
- Semantic Versioning: MAJOR.MINOR.PATCH
  - MAJOR = 3 (breaking changes)
  - MINOR = 0 (new release)
  - PATCH = 0 (initial release)
- Future releases: 3.0.1 (patch), 3.1.0 (minor features), 4.0.0 (next breaking change)

**Git Tagging**:
```bash
git tag -a v3.0.0 -m "RawRabbit 3.0.0 - .NET 8 Modernization"
git push origin v3.0.0
```

**Internal NuGet Feed Upload**:
```bash
# Example for Azure Artifacts
dotnet nuget push ./artifacts/*.nupkg --source "RawRabbit-Internal" --api-key <API_KEY>
```

**Rollback Plan**:
- Keep RawRabbit 2.x packages available on internal feed
- Consumers can pin to 2.x if 3.x migration fails
- Rollback instructions in MIGRATION-GUIDE.md

**Consumer Migration Timeline**:
- **Week 16-20**: Internal teams review migration guide
- **Week 21+**: Gradual migration (service by service)
- **6 months**: All internal services on 3.0.0
- **12 months**: Deprecate 2.x support

---

## Post-Release Plan (Phase 8 - Future Work)

**Not part of this modernization, but planned for future**:

**Quarterly Maintenance** (2 days/quarter):
- Dependency vulnerability scans
- Update dependencies with security patches
- Monitor for .NET 9, .NET 10 releases
- Community monitoring (RabbitMQ.Client updates)

**Future Enhancements** (if time/budget permits):
- Migrate to System.Text.Json (replace Newtonsoft.Json for better performance)
- Add .NET 9 support (when LTS)
- RabbitMQ.Client 8.x migration (when released)
- Performance optimizations (based on Phase 5 findings)
- Additional enrichers (e.g., OpenTelemetry integration)

**Long-Term Decision Point** (12 months post-release):
- Evaluate: Continue maintaining internal fork vs. migrate to MassTransit
- Cost-benefit analysis: Maintenance burden vs. migration cost
- If migration to MassTransit: Plan 6-month transition

---

## Approval & Sign-Off

**Plan Approval**:
- [ ] Migration Coordinator: ________________________ Date: ________
- [ ] Architect: ________________________ Date: ________
- [ ] Security Lead: ________________________ Date: ________
- [ ] QA Lead: ________________________ Date: ________
- [ ] Engineering Manager: ________________________ Date: ________
- [ ] Executive Sponsor: ________________________ Date: ________

**Budget Approval**:
- [ ] Approved Budget: $________________
- [ ] Approved Timeline: ________ weeks
- [ ] Contingency Authorized: Yes / No
- [ ] External Consultant Authorized: Yes / No

**GO Decision**:
- [ ] Proceed with modernization as planned
- [ ] Proceed with modifications: _____________________________
- [ ] Defer until: ___________
- [ ] Do not proceed (rationale: _______________________________)

---

**Plan Version**: 1.0
**Created**: 2025-11-09
**Last Updated**: 2025-11-09
**Next Review**: [Every 2 weeks during execution]

**Document Status**: DRAFT - Pending Approval

---

**END OF PLAN**
