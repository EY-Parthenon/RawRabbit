# RawRabbit .NET 9 Upgrade - Planning Documentation

This directory contains comprehensive planning and review documentation for upgrading RawRabbit from .NET Standard 1.5 / .NET Framework 4.5.1 to .NET 9.

---

## 📋 Quick Start by Role

| Role | Start Here | Then Read | Why |
|------|------------|-----------|-----|
| **Project Manager** | [REVIEW-SUMMARY.md](#review-summarymd) | [PLAN.md](#planmd) | Get 5-minute critical findings overview, then detailed timeline |
| **Developer** | [IMMEDIATE-ACTIONS.md](#immediate-actionsmd) | [PLAN.md](#planmd) Stages 1-5 | Complete pre-work checklist, then follow implementation stages |
| **Lead Architect** | [PLAN.md](#planmd) | [PLAN-REVIEW.md](#plan-reviewmd) | Understand the plan, then review technical deep-dive |
| **Security Engineer** | [security-specialist-review.md](#security-specialist-reviewmd) | [security-review-plan.md](#security-review-planmd) | Understand 11 critical issues, then review validation process |
| **QA Engineer** | [qa-review-net9-upgrade.md](#qa-review-net9-upgrademd) | [PLAN.md](#planmd) Stage 6 | Understand testing strategy, then review integration testing plan |
| **DevOps Engineer** | [devops-review.md](#devops-reviewmd) | [PLAN.md](#planmd) Stage 7-8 | Understand infrastructure needs, then review deployment stages |

---

## 📁 Document Inventory

### ⭐ Primary Plan Document

#### [PLAN.md](PLAN.md)
**Size**: 18KB | **Version**: 1.1 | **Status**: ✅ Active

**Purpose**: The authoritative 8-stage, 13-15 week migration plan for upgrading RawRabbit to .NET 9.

**Key Contents**:
- Executive summary with revised timeline (13-15 weeks)
- Success criteria: 75% test coverage, 9 security checkpoints, CVE resolution
- 8 detailed stages with week-by-week breakdown
- 25 project migration order organized by dependency tiers
- Security, testing, and deployment requirements
- Known challenges and risks

**When to Use**:
- Starting the upgrade project
- Understanding overall timeline and scope
- Planning resource allocation
- Reviewing stage-by-stage implementation steps

**Critical Sections**:
- Lines 1-50: Executive summary with revised metrics
- Lines 100-200: Stage 1 (Foundation & Security Audit)
- Lines 540-551: Timeline summary table

**Dependencies**: References all other planning documents

---

### 📊 Review and Analysis Documents

#### [PLAN-REVIEW.md](PLAN-REVIEW.md)
**Size**: 26KB | **Version**: 1.0 | **Status**: ✅ Complete

**Purpose**: Migration Architect's comprehensive technical review of the original PLAN.md, identifying critical gaps and providing detailed recommendations.

**Key Findings**:
- ❌ **Incorrect dependency order**: MessageSequence placed in Tier 1, actually belongs in Tier 3
- ⏱️ **Unrealistic timeline**: 10-12 weeks insufficient, needs 13-15 weeks
- 🔒 **Deprecated dependencies**: ZeroFormatter (archived 2018), Ninject (unmaintained 2017)
- 🧪 **Unrealistic test coverage**: 90% unachievable, revised to 75%
- 📦 **Component analysis**: 25 projects categorized by complexity and dependency depth

**When to Use**:
- Understanding technical migration challenges
- Reviewing dependency migration order
- Analyzing component complexity
- Planning test infrastructure

**Critical Sections**:
- Dependency order corrections
- Test coverage analysis
- Component-by-component migration recommendations

---

#### [REVIEW-SUMMARY.md](REVIEW-SUMMARY.md)
**Size**: 8.8KB | **Version**: 1.0 | **Status**: ✅ Complete

**Purpose**: Executive summary of critical findings from all agent reviews, designed for quick stakeholder briefing.

**Key Contents**:
- 11 critical issues requiring immediate attention
- Risk severity ratings (🚨 BLOCKER, ⚠️ CRITICAL, ⚠️ HIGH)
- Timeline impact summary
- Immediate action items
- Success criteria revisions

**When to Use**:
- Executive briefings (5-10 minute read)
- Stakeholder communication
- Risk assessment meetings
- Go/no-go decision support

**Critical Issues Highlighted**:
1. Critical CVEs in dependencies (BLOCKER)
2. Hardcoded credentials (CRITICAL SECURITY)
3. Incorrect dependency migration order (HIGH)
4. Deprecated dependencies (HIGH)
5. Unrealistic test coverage goals (HIGH)

---

#### [PLAN-UPDATES-v1.1.md](PLAN-UPDATES-v1.1.md)
**Size**: 24KB | **Version**: 1.0 | **Status**: 📋 Proposed

**Purpose**: Consolidated update proposal showing exactly what changes were made from PLAN.md v1.0 → v1.1, with full traceability to agent review findings.

**Key Contents**:
- 11 critical findings summary
- Major revisions by topic (timeline, security, testing, dependencies)
- Section-by-section updates with before/after comparisons
- Approval checklist for stakeholders

**When to Use**:
- Understanding what changed and why between plan versions
- Reviewing update rationale before approval
- Tracking decision history
- Auditing plan evolution

**Format**:
```markdown
## Section X: [Topic]
**Original**: [previous text]
**Updated**: [new text]
**Rationale**: [why changed]
**Source**: [which agent review identified this]
```

---

#### [IMMEDIATE-ACTIONS.md](IMMEDIATE-ACTIONS.md)
**Size**: 15KB | **Version**: 1.0 | **Status**: ✅ Active

**Purpose**: Pre-Stage 1 checklist of 10 critical tasks that MUST be completed before starting Stage 1 of the migration.

**Timeline**: Week 0 (5 business days)

**Critical Tasks**:
1. ✅ Install .NET 9 SDK
2. ✅ Research RabbitMQ.Client 7.x breaking changes
3. ✅ Verify ZeroFormatter/Ninject .NET 9 compatibility
4. ✅ Setup Docker RabbitMQ test environment
5. ✅ Review async/await patterns
6. ✅ Cryptographic API audit
7. ✅ Test framework compatibility check
8. ✅ CI/CD pipeline assessment
9. ✅ Security scanning tools setup
10. ✅ Baseline performance benchmarks

**When to Use**:
- Before starting Stage 1
- Onboarding new team members
- Ensuring environment readiness
- Risk mitigation

**Format**: Each task includes purpose, steps, deliverables, estimated time, and success criteria.

---

### 🔒 Security Documentation

#### [security-specialist-review.md](security-specialist-review.md)
**Size**: 64KB | **Version**: 1.0 | **Status**: ✅ Complete

**Purpose**: Comprehensive security assessment identifying 11 critical security issues and proposing expanded security checkpoint model.

**Critical Findings**:

**🚨 BLOCKER (Must Fix Before Any Release)**:
1. **CVE-2020-11100** (RabbitMQ.Client): Memory DoS vulnerability (HIGH)
2. **CVE-2021-22116** (RabbitMQ.Client): Memory exhaustion (HIGH)
3. **CVE-2024-21907** (Newtonsoft.Json): DoS vulnerability (CRITICAL)
4. **CVE-2024-21908** (Newtonsoft.Json): RCE vulnerability (CRITICAL)
5. **TypeNameHandling.Auto**: Enables deserialization attacks (CRITICAL)

**⚠️ CRITICAL (Fix During Migration)**:
6. Hardcoded credentials (`guest/guest` in RawRabbitConfiguration.Local)
7. Insecure JSON serialization defaults
8. Legacy cryptography usage (potentially non-FIPS compliant)

**⚠️ HIGH (Address in Security Stages)**:
9. Incomplete input validation
10. Missing secrets management
11. No threat modeling documentation

**When to Use**:
- Security audit preparation
- CVE remediation planning
- Compliance review
- Security checkpoint implementation

**Key Sections**:
- CVE analysis with CVSS scores
- Remediation recommendations
- Security checkpoint expansion (4 → 9)
- Cryptographic inventory requirements

---

#### [security-review-plan.md](security-review-plan.md)
**Size**: 32KB | **Version**: 1.0 | **Status**: ✅ Complete

**Purpose**: Detailed security validation process with 9 comprehensive checkpoints throughout the migration lifecycle.

**9 Security Checkpoints**:

| Stage | Checkpoint | Focus |
|-------|-----------|-------|
| Pre-Stage 1 | Checkpoint 0 | Baseline security audit, vulnerability scanning |
| Stage 1 | Checkpoint 1 | Cryptographic inventory, secrets audit |
| Stage 2 | Checkpoint 2 | Threat modeling, ADR security review |
| Stage 3 | Checkpoint 3 | Core library CVE resolution validation |
| Stage 4 | Checkpoint 4 | Dependency security validation |
| Stage 5 | Checkpoint 5 | DI container security review |
| Stage 6 | Checkpoint 6 | Penetration testing, security integration tests |
| Stage 7 | Checkpoint 7 | SBOM generation, package signing, supply chain security |
| Stage 8 | Checkpoint 8 | Production security monitoring setup |

**When to Use**:
- Planning security validation activities
- Scheduling security reviews
- Compliance documentation
- Security gate reviews

**Tools Specified**:
- OWASP Dependency-Check
- Snyk
- GitHub Advanced Security
- Coverlet (code coverage)
- Docker (RabbitMQ test environment)

---

### 🧪 Testing Documentation

#### [qa-review-net9-upgrade.md](qa-review-net9-upgrade.md)
**Size**: 39KB | **Version**: 1.0 | **Status**: ✅ Complete

**Purpose**: Comprehensive QA strategy review proposing realistic test coverage targets and infrastructure requirements.

**Key Revisions**:

**Test Coverage Targets** (Revised from 90% overall):
- **Core Library**: 80% coverage (high complexity, critical path)
- **Operations**: 70% coverage (RabbitMQ integration)
- **Enrichers**: 60% coverage (middleware, less critical)
- **DI Containers**: 50% coverage (adapter pattern, lower risk)
- **Overall Target**: 75% weighted average

**Test Infrastructure Requirements**:
1. Docker Compose for RabbitMQ test instances
2. GitHub Actions workflow for CI/CD
3. Coverlet for code coverage reporting
4. xUnit test framework migration
5. Integration test isolation strategy

**When to Use**:
- Test planning and resource estimation
- Coverage target negotiation
- Test infrastructure setup
- Stage 6 (Integration Testing) preparation

**Critical Sections**:
- Component-specific coverage justification
- Test pyramid strategy
- Performance benchmarking approach
- Regression testing plan

---

### ⚙️ Infrastructure Documentation

#### [devops-review.md](devops-review.md)
**Size**: 26KB | **Version**: 1.0 | **Status**: ✅ Complete

**Purpose**: DevOps infrastructure assessment covering CI/CD, containerization, deployment, and monitoring requirements.

**Key Recommendations**:

**CI/CD Pipeline**:
- GitHub Actions workflow for .NET 9 builds
- Multi-stage pipeline (build → test → security scan → package → deploy)
- Branch protection rules
- Automated semantic versioning

**Containerization**:
- Docker support for RabbitMQ test environments
- docker-compose.test.yml for integration testing
- Optional Dockerfile for sample applications

**Deployment Strategy**:
- NuGet.org package publishing
- Symbol package (.snupkg) generation
- Package signing with code certificates
- SBOM (Software Bill of Materials) generation

**Monitoring**:
- Application Insights integration (optional)
- Custom telemetry for RabbitMQ operations
- Performance counters

**When to Use**:
- CI/CD pipeline setup (Stage 7)
- Deployment planning (Stage 8)
- Monitoring strategy
- Infrastructure cost estimation

---

### 📝 Additional Reviews

#### [dotnet-modernizer-review.md](dotnet-modernizer-review.md)
**Size**: 27KB | **Version**: 1.0 | **Status**: ✅ Complete

**Purpose**: Code modernization specialist review focusing on .NET 9 idioms, nullable reference types, async patterns, and modern C# features.

**Key Topics**:
- Nullable reference types migration strategy
- C# 13 feature adoption opportunities
- async/await pattern improvements
- LINQ optimization
- Dependency injection modernization

**When to Use**:
- Code modernization during migration
- Code review guidelines
- .NET 9 feature adoption decisions
- Performance optimization planning

---

#### [DOCUMENTATION-REVIEW.md](DOCUMENTATION-REVIEW.md)
**Size**: 29KB | **Version**: 1.0 | **Status**: ✅ Complete

**Purpose**: Documentation quality assessment with recommendations for user documentation, API docs, migration guides, and architecture decision records.

**Documentation Deliverables**:
1. Migration guide for library users
2. Breaking changes documentation
3. API reference updates (XML comments)
4. Architecture Decision Records (ADRs)
5. Security documentation
6. Performance comparison benchmarks

**When to Use**:
- Stage 7 (Documentation phase) planning
- ADR creation guidance
- Migration guide preparation
- User communication strategy

---

### 📊 Visual Aids

#### [dependency-graph.mermaid](dependency-graph.mermaid)
**Size**: 4.4KB | **Version**: 1.0 | **Status**: ✅ Complete

**Purpose**: Visual dependency graph showing correct migration order for 25 RawRabbit projects organized in 5 tiers.

**Tier Breakdown**:
- **Tier 0** (Foundation): RawRabbit.Core, RawRabbit.Operations
- **Tier 1** (Extensions): Logging, Polly, HttpContext
- **Tier 2** (Advanced Features): StateMachine, MessageSequence
- **Tier 3** (Enrichers): Advanced middleware
- **Tier 4** (DI Containers): Ninject, Autofac, StructureMap, etc.

**When to Use**:
- Understanding component dependencies
- Planning migration order
- Identifying blockers
- Architecture discussions

**How to View**:
- GitHub markdown preview
- VS Code with Mermaid extension
- Online Mermaid editor (mermaid.live)

---

## 🗺️ Document Relationships

### Decision Flow

```
REVIEW-SUMMARY.md (Executive Summary)
          ↓
    PLAN.md (Authoritative Plan)
          ↓
   IMMEDIATE-ACTIONS.md (Pre-Stage 1)
          ↓
  [Execute Stages 1-8 from PLAN.md]
```

### Supporting Documentation Flow

```
PLAN.md (Primary)
    ├─→ PLAN-REVIEW.md (Technical deep-dive)
    ├─→ security-specialist-review.md (Security issues)
    │   └─→ security-review-plan.md (Security process)
    ├─→ qa-review-net9-upgrade.md (Testing strategy)
    ├─→ devops-review.md (Infrastructure)
    ├─→ dotnet-modernizer-review.md (Code modernization)
    ├─→ DOCUMENTATION-REVIEW.md (Doc requirements)
    └─→ dependency-graph.mermaid (Visual aid)
```

### Update History Flow

```
PLAN.md v1.0 (Original)
    ↓
[3 Agent Reviews: Migration, Security, QA]
    ↓
PLAN-REVIEW.md + security-specialist-review.md + qa-review-net9-upgrade.md
    ↓
PLAN-UPDATES-v1.1.md (Consolidated changes)
    ↓
PLAN.md v1.1 (Current, Updated)
```

---

## 📍 Finding Information by Question

| Question | Document | Section |
|----------|----------|---------|
| **How long will the upgrade take?** | [PLAN.md](#planmd) | Executive Summary, Timeline Summary (lines 540-551) |
| **What are the critical security issues?** | [security-specialist-review.md](#security-specialist-reviewmd) | Critical Findings (lines 1-100) |
| **What CVEs need fixing?** | [PLAN.md](#planmd) or [security-specialist-review.md](#security-specialist-reviewmd) | Dependencies section, CVE Analysis |
| **What do I do before starting?** | [IMMEDIATE-ACTIONS.md](#immediate-actionsmd) | Full document (10 tasks) |
| **What's the correct migration order?** | [dependency-graph.mermaid](#dependency-graphmermaid) | Visual graph, or [PLAN-REVIEW.md](#plan-reviewmd) |
| **What test coverage is required?** | [qa-review-net9-upgrade.md](#qa-review-net9-upgrademd) | Revised Coverage Targets |
| **What security checkpoints exist?** | [security-review-plan.md](#security-review-planmd) | 9 Security Checkpoints table |
| **What changed from v1.0 to v1.1?** | [PLAN-UPDATES-v1.1.md](#plan-updates-v11md) | Full document with before/after |
| **What infrastructure do I need?** | [devops-review.md](#devops-reviewmd) | CI/CD, Docker, Deployment sections |
| **What are the biggest risks?** | [REVIEW-SUMMARY.md](#review-summarymd) | Critical Issues (11 items) |
| **Which dependencies are deprecated?** | [PLAN-REVIEW.md](#plan-reviewmd) | Deprecated Dependencies section |
| **What modern .NET features can we use?** | [dotnet-modernizer-review.md](#dotnet-modernizer-reviewmd) | C# 13 Features, Modernization |
| **What documentation needs updating?** | [DOCUMENTATION-REVIEW.md](#documentation-reviewmd) | Documentation Deliverables |

---

## 🎯 Recommended Reading Paths

### Path 1: Executive/Manager (30 minutes)
1. **[REVIEW-SUMMARY.md](#review-summarymd)** (10 min) - Critical findings
2. **[PLAN.md](#planmd)** - Executive Summary only (10 min)
3. **[IMMEDIATE-ACTIONS.md](#immediate-actionsmd)** - Skim checklist (5 min)
4. **[security-specialist-review.md](#security-specialist-reviewmd)** - Critical Findings only (5 min)

**Outcome**: Understand timeline (13-15 weeks), critical risks (11 issues), security concerns (CVEs), and immediate pre-work needed.

---

### Path 2: Developer/Implementer (2-3 hours)
1. **[IMMEDIATE-ACTIONS.md](#immediate-actionsmd)** (30 min) - Complete all 10 tasks
2. **[PLAN.md](#planmd)** (60 min) - Read Stages 1-5 in detail
3. **[dependency-graph.mermaid](#dependency-graphmermaid)** (10 min) - Understand migration order
4. **[PLAN-REVIEW.md](#plan-reviewmd)** (30 min) - Component-specific recommendations
5. **[dotnet-modernizer-review.md](#dotnet-modernizer-reviewmd)** (20 min) - Modernization patterns

**Outcome**: Ready to start Stage 1 with clear understanding of migration order, technical challenges, and modernization opportunities.

---

### Path 3: Security Engineer (1.5 hours)
1. **[security-specialist-review.md](#security-specialist-reviewmd)** (45 min) - All 11 critical issues
2. **[security-review-plan.md](#security-review-planmd)** (30 min) - 9-checkpoint process
3. **[PLAN.md](#planmd)** - Stage 1 security tasks (15 min)

**Outcome**: Understand all CVEs, hardcoded credentials, security process, and checkpoint requirements.

---

### Path 4: QA Engineer (1 hour)
1. **[qa-review-net9-upgrade.md](#qa-review-net9-upgrademd)** (40 min) - Testing strategy
2. **[PLAN.md](#planmd)** - Stage 6 (Integration Testing) (20 min)

**Outcome**: Understand test coverage targets (75%), infrastructure requirements (Docker, GitHub Actions), and testing phases.

---

### Path 5: DevOps Engineer (1 hour)
1. **[devops-review.md](#devops-reviewmd)** (40 min) - Infrastructure requirements
2. **[PLAN.md](#planmd)** - Stage 7-8 (Documentation, Deployment) (20 min)

**Outcome**: Understand CI/CD pipeline, Docker setup, deployment strategy, and monitoring requirements.

---

### Path 6: Architect/Lead (4-5 hours)
1. **[PLAN.md](#planmd)** (90 min) - Full plan, all stages
2. **[PLAN-REVIEW.md](#plan-reviewmd)** (90 min) - Technical deep-dive
3. **[dependency-graph.mermaid](#dependency-graphmermaid)** (15 min) - Dependency visualization
4. **[security-specialist-review.md](#security-specialist-reviewmd)** (45 min) - Security analysis
5. **[qa-review-net9-upgrade.md](#qa-review-net9-upgrademd)** (30 min) - Testing strategy
6. **[devops-review.md](#devops-reviewmd)** (30 min) - Infrastructure

**Outcome**: Complete understanding of technical architecture, risks, timeline, security, testing, and infrastructure requirements.

---

## 📊 Document Status & Versions

| Document | Version | Status | Last Updated | Size |
|----------|---------|--------|--------------|------|
| **PLAN.md** | 1.1 | ✅ Active | 2025-10-09 | 18KB |
| **PLAN-REVIEW.md** | 1.0 | ✅ Complete | 2025-10-09 | 26KB |
| **PLAN-UPDATES-v1.1.md** | 1.0 | 📋 Proposed | 2025-10-09 | 24KB |
| **REVIEW-SUMMARY.md** | 1.0 | ✅ Complete | 2025-10-09 | 8.8KB |
| **IMMEDIATE-ACTIONS.md** | 1.0 | ✅ Active | 2025-10-09 | 15KB |
| **security-specialist-review.md** | 1.0 | ✅ Complete | 2025-10-09 | 64KB |
| **security-review-plan.md** | 1.0 | ✅ Complete | 2025-10-09 | 32KB |
| **qa-review-net9-upgrade.md** | 1.0 | ✅ Complete | 2025-10-09 | 39KB |
| **devops-review.md** | 1.0 | ✅ Complete | 2025-10-09 | 26KB |
| **dotnet-modernizer-review.md** | 1.0 | ✅ Complete | 2025-10-09 | 27KB |
| **DOCUMENTATION-REVIEW.md** | 1.0 | ✅ Complete | 2025-10-09 | 29KB |
| **dependency-graph.mermaid** | 1.0 | ✅ Complete | 2025-10-09 | 4.4KB |

**Legend**:
- ✅ **Active**: Primary document, actively used for project execution
- ✅ **Complete**: Review/analysis completed, stable reference document
- 📋 **Proposed**: Awaiting stakeholder approval/review

---

## 🔗 Related Documentation

**Parent Directory**: [/docs/README.md](../README.md) - Overall docs structure

**Project Root**:
- `/README.md` - RawRabbit project overview
- `/CLAUDE.md` - Development guide for Claude Code agents
- `/CLAUDE-AGENTS.md` - Agent coordination workflows
- `/.claude-flow/config.json` - Agent configuration

**Work History**: [/docs/HISTORY.md](../HISTORY.md) - Chronological work log

**To Be Created During Upgrade**:
- `/docs/adr/` - Architecture Decision Records (Stage 2)
- `/docs/test/` - Test reports and coverage (Stage 6)
- `/docs/security/` - Security audit reports (Stages 1, 3, 6)
- `/docs/MIGRATION-GUIDE.md` - User upgrade guide (Stage 7)
- `/docs/BREAKING-CHANGES.md` - Breaking changes (Stage 7)

---

## 🚨 Critical Reminders

### Before Starting Stage 1
1. ✅ Complete ALL 10 tasks in [IMMEDIATE-ACTIONS.md](#immediate-actionsmd)
2. ✅ Ensure .NET 9 SDK installed
3. ✅ Setup Docker RabbitMQ test environment
4. ✅ Understand CVEs and security requirements

### During Implementation
1. ✅ Follow migration order from [dependency-graph.mermaid](#dependency-graphmermaid)
2. ✅ Address hardcoded credentials immediately (Stage 1)
3. ✅ Resolve critical CVEs in Stage 3 (RabbitMQ.Client, Newtonsoft.Json)
4. ✅ Create ADRs for all major decisions (Stage 2+)
5. ✅ Complete security checkpoints at each stage gate

### Success Criteria (from PLAN.md v1.1)
- ✅ 75%+ test coverage (component-specific targets)
- ✅ 9 security checkpoints passed
- ✅ Zero CRITICAL or HIGH CVEs remaining
- ✅ All 25 projects successfully migrated
- ✅ Full integration test suite passing
- ✅ Documentation complete (migration guide, breaking changes, ADRs)

---

## ❓ Questions or Issues?

- **General planning questions**: See [PLAN.md](#planmd)
- **Security concerns**: See [security-specialist-review.md](#security-specialist-reviewmd)
- **Testing questions**: See [qa-review-net9-upgrade.md](#qa-review-net9-upgrademd)
- **Infrastructure questions**: See [devops-review.md](#devops-reviewmd)
- **What changed between versions**: See [PLAN-UPDATES-v1.1.md](#plan-updates-v11md)

---

**Last Updated**: 2025-10-09
**Maintained By**: RawRabbit .NET 9 Upgrade Team
**Total Planning Documentation**: 12 files, ~293KB
**Project Status**: Planning Phase Complete, Ready for Stage 1 (pending IMMEDIATE-ACTIONS.md completion)
