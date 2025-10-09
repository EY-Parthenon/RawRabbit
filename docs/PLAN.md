# RawRabbit .NET 9 Upgrade Plan

## Executive Summary

**Objective**: Migrate RawRabbit from .NET Standard 1.5 / .NET Framework 4.5.1 to .NET 9, improving security, performance, and maintainability while maintaining backward compatibility where possible.

**Duration**: 10-12 weeks
**Branch**: `upgrade`
**Target Framework**: .NET 9.0
**Success Criteria**: 90%+ test coverage, all security audits passed, zero critical bugs

---

## Current State Analysis

### Existing Framework Targets
- **Core Library**: .NET Standard 1.5, .NET Framework 4.5.1
- **Dependencies**:
  - RabbitMQ.Client 5.0.1 (needs update)
  - Newtonsoft.Json 10.0.1 (needs update)
- **Project Count**: 25 separate projects
- **Architecture**: Middleware-based pipeline pattern

### Known Challenges
1. **Breaking Changes**: .NET 9 removes many APIs from .NET Framework 4.5.1
2. **Dependency Updates**: RabbitMQ.Client and JSON libraries need major version updates
3. **Multi-Targeting**: Need to decide on single target vs. multi-target strategy
4. **Testing**: Integration tests require RabbitMQ instance
5. **Security**: Legacy cryptography APIs need replacement

---

## Multi-Stage Upgrade Plan

## Stage 1: Foundation & Assessment (Week 1-2)

### Goals
- Establish baseline metrics
- Identify all breaking changes
- Set up documentation infrastructure
- Configure agent coordination

### Tasks

#### 1.1 Environment Setup
- [x] Create `upgrade` branch
- [ ] Initialize Hive Mind coordination system
- [ ] Configure 6-agent mesh topology
- [ ] Set up documentation structure

#### 1.2 Discovery & Analysis
**Agent**: Migration Architect

- [ ] Analyze all 25 .csproj files for target frameworks
- [ ] Map complete dependency tree
- [ ] Identify deprecated APIs across all projects
- [ ] Document current NuGet package versions
- [ ] Create dependency upgrade matrix
- [ ] Estimate migration complexity per component

**Deliverables**:
- `docs/migration-roadmap.md`
- `docs/dependency-matrix.md`
- `docs/adr/0001-migration-strategy.md`

#### 1.3 Security Baseline
**Agent**: Security Specialist

- [ ] Run vulnerability scan on current codebase
- [ ] Audit all NuGet dependencies for known CVEs
- [ ] Review authentication/authorization patterns
- [ ] Identify insecure cryptography usage
- [ ] Document current security posture

**Deliverables**:
- `docs/security-baseline-report.md`
- `docs/adr/0002-security-architecture.md`

#### 1.4 Documentation Infrastructure
**Agent**: Documentation Specialist

- [ ] Create ADR directory structure at `docs/adr/`
- [ ] Create ADR template (`docs/adr/template.md`)
- [ ] Initialize `docs/HISTORY.md`
- [ ] Create test report directory at `docs/test/`
- [ ] Set up documentation standards

**Deliverables**:
- ADR infrastructure ready
- Documentation templates created

---

## Stage 2: Architecture & Design (Week 2-3)

### Goals
- Design .NET 9 target architecture
- Decide on migration strategy (big-bang vs. incremental)
- Plan component migration order
- Validate security approach

### Tasks

#### 2.1 Architecture Design
**Agent**: Migration Architect

- [ ] Design .NET 9 target architecture
- [ ] Decide: Single target (.NET 9) or multi-target (.NET 9 + .NET Standard 2.1)
- [ ] Plan NuGet package upgrade strategy
- [ ] Define component migration order based on dependencies
- [ ] Identify API contract changes
- [ ] Design middleware pipeline compatibility layer (if needed)

**Key Decisions**:
1. **Target Framework Strategy**:
   - Option A: Single target .NET 9 (recommended - clean break)
   - Option B: Multi-target .NET 9 + .NET Standard 2.1 (compatibility)
2. **Dependency Update Strategy**:
   - RabbitMQ.Client: 5.0.1 → 7.x (latest)
   - Newtonsoft.Json: 10.0.1 → 13.x or migrate to System.Text.Json
3. **Breaking Changes Approach**:
   - Create compatibility shims where necessary
   - Update consumer guidance documentation

**Deliverables**:
- `docs/adr/0003-target-framework-selection.md`
- `docs/adr/0004-dependency-update-strategy.md`
- `docs/architecture-design.md`

#### 2.2 Security Architecture Review
**Agent**: Security Specialist

- [ ] Review proposed .NET 9 architecture
- [ ] Validate against security requirements
- [ ] Identify security improvements from .NET 9
- [ ] Plan migration of deprecated crypto APIs
- [ ] Design enhanced security features

**Deliverables**:
- `docs/adr/0005-security-review-results.md`
- Updated security checklist

#### 2.3 Test Strategy
**Agent**: QA Engineer

- [ ] Design comprehensive test strategy
- [ ] Plan regression test suite
- [ ] Define performance benchmarking approach
- [ ] Set up test reporting infrastructure
- [ ] Plan RabbitMQ test environment

**Deliverables**:
- `docs/test-strategy.md`
- Test reporting templates

---

## Stage 3: Core Migration (Week 3-5)

### Goals
- Migrate core RawRabbit library
- Update fundamental dependencies
- Establish migration patterns for other components

### Component Migration Order

#### 3.1 RawRabbit (Core) - Week 3
**Priority**: CRITICAL - Foundation for all other components

**Tasks**:
1. Update `src/RawRabbit/RawRabbit.csproj`:
   ```xml
   <TargetFrameworks>net9.0</TargetFrameworks>
   ```
2. Update RabbitMQ.Client to 7.x
3. Update Newtonsoft.Json to 13.x or migrate to System.Text.Json
4. Refactor deprecated APIs:
   - Replace `AppDomain.CurrentDomain` usage
   - Update reflection APIs
   - Migrate cryptography APIs
5. Update `SimpleDependencyInjection` for .NET 9 compatibility
6. Fix middleware pipeline for .NET 9

**Testing**:
- [ ] Run existing unit tests
- [ ] Add .NET 9 specific tests
- [ ] Validate middleware pipeline
- [ ] Performance benchmark vs. baseline

**Deliverables**:
- Migrated RawRabbit.csproj
- Test report in `docs/test/core-migration.md`
- `docs/adr/0006-core-api-changes.md` (if significant changes)

#### 3.2 Configuration & Common - Week 3-4
**Dependencies**: None (can run parallel with 3.1)

Components:
- Configuration classes
- Common utilities
- Extension methods

**Tasks**:
- [ ] Update target framework
- [ ] Refactor any framework-specific code
- [ ] Update tests
- [ ] Validate configuration loading

#### 3.3 Channel Management - Week 4
**Dependencies**: Core (3.1)

**Tasks**:
- [ ] Update channel pooling for .NET 9
- [ ] Validate connection management
- [ ] Test channel lifecycle
- [ ] Update error handling

---

## Stage 4: Operations & Enrichers (Week 5-7)

### Goals
- Migrate all operation packages
- Migrate all enricher packages
- Maintain API compatibility

### 4.1 Operations Migration (Week 5-6)

**Migration Order** (parallel where possible):
1. **Publish** - Fire-and-forget publishing
2. **Subscribe** - Message consumption
3. **Request/Respond** - RPC operations (migrate together)
4. **Get** - Single message retrieval
5. **MessageSequence** - Choreographed flows
6. **StateMachine** - Stateful handling
7. **Tools** - Utility operations

**Per-Operation Workflow**:
1. Update .csproj to .NET 9
2. Update package references
3. Refactor deprecated APIs
4. Run/update tests (save to `docs/test/operations/`)
5. Security review
6. Update `docs/HISTORY.md`

#### 4.2 Enrichers Migration (Week 6-7)

**Migration Order**:
1. **MessageContext** - Core enricher (migrate first)
2. **Attributes** - Attribute-based config
3. **Polly** - Resilience policies (may need Polly update)
4. **GlobalExecutionId** - Distributed tracing
5. **QueueSuffix** - Dynamic naming
6. **HttpContext** - ASP.NET integration (ASP.NET Core compatibility)
7. **RetryLater** - Delayed retry
8. **Serialization Enrichers** (parallel):
   - Protobuf
   - MessagePack
   - ZeroFormatter

**Critical Items**:
- [ ] Polly package update (check compatibility)
- [ ] ASP.NET Core integration testing
- [ ] Serialization library compatibility

---

## Stage 5: Dependency Injection & Samples (Week 7-8)

### 5.1 DI Adapters (Week 7)

**Components**:
1. **ServiceCollection** - Microsoft.Extensions.DependencyInjection (primary)
2. **Autofac** - Check Autofac .NET 9 compatibility
3. **Ninject** - Check Ninject status (may be deprecated)

**Tasks**:
- [ ] Update Microsoft.Extensions.DependencyInjection integration
- [ ] Verify Autofac compatibility with .NET 9
- [ ] Evaluate Ninject support (consider deprecation ADR)
- [ ] Test DI registration patterns
- [ ] Update documentation

**Deliverables**:
- `docs/adr/0007-di-adapter-support.md` (if changes to supported adapters)

### 5.2 Sample Applications (Week 8)

**Components**:
- ConsoleApp.Sample
- AspNet.Sample
- Messages.Sample

**Tasks**:
- [ ] Migrate to .NET 9
- [ ] Update to ASP.NET Core (if still using ASP.NET)
- [ ] Test end-to-end scenarios
- [ ] Update sample documentation
- [ ] Create .NET 9 quickstart guide

---

## Stage 6: Integration & Testing (Week 8-9)

### Goals
- Full system integration testing
- Performance validation
- Security validation

### 6.1 Integration Testing (Week 8-9)
**Agent**: QA Engineer

**Test Suites**:
1. **End-to-End Tests**:
   - [ ] Publish/Subscribe scenarios
   - [ ] Request/Response (RPC) flows
   - [ ] Message sequencing
   - [ ] Error handling and retries
   - [ ] Connection recovery

2. **Performance Tests**:
   - [ ] Throughput benchmarks (messages/sec)
   - [ ] Latency measurements (p50, p95, p99)
   - [ ] Memory consumption
   - [ ] Connection pool efficiency
   - [ ] Compare with baseline (.NET Standard 1.5)

3. **RabbitMQ Compatibility**:
   - [ ] Test with RabbitMQ 3.11.x
   - [ ] Test with RabbitMQ 3.12.x (latest)
   - [ ] Test various exchange types
   - [ ] Test clustering scenarios

4. **Integration Tests**:
   - [ ] All 25 projects working together
   - [ ] Cross-component middleware pipeline
   - [ ] Plugin composition scenarios

**Deliverables**:
- Complete test reports in `docs/test/integration/`
- Performance comparison report
- Compatibility matrix

### 6.2 Security Testing (Week 9)
**Agent**: Security Specialist

**Security Validation**:
- [ ] Dependency vulnerability scan (post-upgrade)
- [ ] Static code analysis (security rules)
- [ ] TLS/SSL connection testing
- [ ] Authentication/authorization validation
- [ ] Penetration testing (if applicable)
- [ ] Security compliance checklist

**Deliverables**:
- `docs/security-audit-final.md`
- Security clearance sign-off

---

## Stage 7: Documentation & Polish (Week 9-10)

### Goals
- Complete all documentation
- Finalize ADRs
- Prepare release artifacts

### 7.1 Documentation Completion
**Agent**: Documentation Specialist

**Tasks**:
- [ ] Finalize all ADR records
- [ ] Complete `docs/HISTORY.md` with all changes
- [ ] Update README.md with .NET 9 requirements
- [ ] Create migration guide for users
- [ ] Update all code samples
- [ ] Generate API documentation
- [ ] Create changelog

**Deliverables**:
- `docs/MIGRATION-GUIDE.md` - For users upgrading
- `docs/CHANGELOG.md` - All changes
- Complete ADR repository
- Updated API documentation

### 7.2 Build & Packaging
**Agent**: DevOps Engineer

**Tasks**:
- [ ] Update CI/CD pipelines for .NET 9
- [ ] Configure NuGet package generation
- [ ] Test package installation scenarios
- [ ] Validate package metadata
- [ ] Create Docker images (if applicable)
- [ ] Prepare release notes

**Deliverables**:
- Updated CI/CD configuration
- NuGet packages (all 25 projects)
- Release notes

---

## Stage 8: Deployment & Validation (Week 10-12)

### Goals
- Deploy to production
- Monitor for issues
- Gather feedback

### 8.1 Staged Rollout (Week 10-11)

**Phase 1: Alpha Release**
- [ ] Deploy to internal testing environment
- [ ] Gather internal feedback
- [ ] Fix critical issues

**Phase 2: Beta Release**
- [ ] Release to early adopters
- [ ] Monitor telemetry
- [ ] Address reported issues

**Phase 3: Release Candidate**
- [ ] Feature freeze
- [ ] Final bug fixes
- [ ] Documentation review

### 8.2 Production Release (Week 12)

**Pre-Release Checklist**:
- [ ] All tests passing (90%+ coverage)
- [ ] Security audit passed
- [ ] Documentation complete
- [ ] Performance validated
- [ ] Breaking changes documented
- [ ] Migration guide available

**Release Activities**:
- [ ] Publish NuGet packages
- [ ] Tag release in Git
- [ ] Publish release notes
- [ ] Update documentation site
- [ ] Announce on communication channels

### 8.3 Post-Release Monitoring (Week 12+)

**Success Metrics**:
- Zero critical bugs in first 2 weeks
- Performance meets or exceeds baseline
- Successful user migrations
- Community feedback positive

---

## Risk Management

### High-Risk Items

1. **RabbitMQ.Client Major Version Update**
   - **Risk**: Breaking API changes
   - **Mitigation**: Thorough testing, compatibility layer if needed
   - **Contingency**: Consider staying on compatible version initially

2. **Multi-Project Dependencies**
   - **Risk**: Circular dependencies or version conflicts
   - **Mitigation**: Clear migration order, careful dependency management
   - **Contingency**: Temporary package version overrides

3. **Integration Test Environment**
   - **Risk**: RabbitMQ availability for testing
   - **Mitigation**: Docker-based RabbitMQ for CI/CD
   - **Contingency**: Manual testing with local RabbitMQ

4. **Breaking Changes Impact**
   - **Risk**: Users unable to upgrade
   - **Mitigation**: Comprehensive migration guide, deprecation warnings
   - **Contingency**: Extended support for previous version

### Mitigation Strategies

- **Parallel Testing**: Run tests on both old and new frameworks during transition
- **Compatibility Shims**: Create adapter layers for breaking changes
- **Documentation**: Extensive migration guides and ADR records
- **Community Communication**: Early announcements of breaking changes
- **Incremental Rollback**: Git branch structure allows easy rollback

---

## Success Criteria

### Technical Criteria
- ✅ All 25 projects targeting .NET 9
- ✅ All tests passing with 90%+ code coverage
- ✅ Zero high/critical security vulnerabilities
- ✅ Performance equal to or better than baseline
- ✅ RabbitMQ.Client updated to 7.x
- ✅ All deprecated APIs replaced

### Documentation Criteria
- ✅ All ADRs documented in `docs/adr/`
- ✅ Complete `docs/HISTORY.md`
- ✅ Migration guide for users
- ✅ All test reports in `docs/test/`
- ✅ Updated README and API docs

### Quality Criteria
- ✅ Security audit passed (all 4 checkpoints)
- ✅ No critical bugs in first 2 weeks post-release
- ✅ Build succeeds on Windows, Linux, macOS
- ✅ NuGet packages published successfully

---

## Resource Requirements

### Agent Allocation

| Agent Role | Stages | Workload |
|------------|--------|----------|
| Migration Architect | 1, 2, 8 | 25% |
| Security Specialist | 1, 2, 6, 8 | 20% |
| .NET Modernizer | 3, 4, 5 | 35% |
| QA Engineer | 3, 4, 5, 6 | 30% |
| Documentation Specialist | 1, 2, 7, 8 | 15% |
| DevOps Engineer | 7, 8 | 15% |

### Infrastructure Requirements
- RabbitMQ instance for integration testing
- CI/CD pipeline (GitHub Actions / Azure Pipelines)
- NuGet package repository
- Documentation hosting
- Development machines with .NET 9 SDK

---

## Timeline Summary

| Stage | Duration | Key Milestone |
|-------|----------|---------------|
| 1. Foundation | Week 1-2 | Baseline established |
| 2. Architecture | Week 2-3 | Design approved |
| 3. Core Migration | Week 3-5 | Core library on .NET 9 |
| 4. Operations/Enrichers | Week 5-7 | All packages migrated |
| 5. DI & Samples | Week 7-8 | Examples working |
| 6. Integration Testing | Week 8-9 | System validated |
| 7. Documentation | Week 9-10 | Docs complete |
| 8. Deployment | Week 10-12 | Production release |

**Total Duration**: 10-12 weeks

---

## Communication Plan

### Internal Communication
- Weekly status updates in `docs/HISTORY.md`
- ADRs for all major decisions
- Daily coordination via agent hooks and memory

### External Communication
- Blog post announcing upgrade project
- Regular updates on progress
- Migration guide published early
- Breaking changes announced ahead of time
- Beta program for early adopters

---

## Appendix

### Key Documents
- `CLAUDE.md` - Development guide
- `docs/HISTORY.md` - Work history
- `docs/adr/` - Architecture decisions
- `docs/test/` - Test reports
- `docs/PLAN.md` - This document

### Tools & Technologies
- .NET 9 SDK
- RabbitMQ (3.11+ for testing)
- claude-flow (agent coordination)
- Git (version control)
- NuGet (package management)

### References
- [.NET 9 Release Notes](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9)
- [RabbitMQ.Client Documentation](https://www.rabbitmq.com/dotnet.html)
- [Breaking Changes in .NET 9](https://learn.microsoft.com/en-us/dotnet/core/compatibility/9.0)

---

**Plan Version**: 1.0
**Created**: 2025-10-09
**Last Updated**: 2025-10-09
**Status**: Proposed
**Next Review**: After Stage 1 completion
