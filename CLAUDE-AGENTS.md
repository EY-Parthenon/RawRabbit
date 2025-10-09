# .NET 9 Upgrade Project - Working Processes

## 🎯 Project Overview

This document defines the working processes for upgrading RawRabbit from legacy .NET Framework to .NET 9, with a focus on security improvements and comprehensive documentation through Architecture Decision Records (ADRs).

## 🔧 System Initialization

**REQUIRED FIRST STEP**: Initialize the Hive Mind system before starting any agent work.

```bash
# Initialize Hive Mind with SQLite database
npx claude-flow@alpha hive-mind init
```

This creates:
- `.hive-mind/` directory with persistent state
- `hive.db` - SQLite database for agent coordination
- `config.json` - Hive Mind configuration

**Initialization creates**:
- ✅ Persistent memory across sessions
- ✅ Agent coordination database
- ✅ Cross-agent communication infrastructure
- ✅ Performance metrics tracking
- ✅ Neural pattern learning storage

**Verify initialization**:
```bash
ls -la .hive-mind/
# Should show: config.json, hive.db
```

## 📋 Agent Coordination

Agent definitions are maintained in `.claude-flow/config.json`. All agents use mesh topology for peer-to-peer coordination with fault tolerance.

**Configuration**: Max 6 concurrent agents with session ID `dotnet9-upgrade`

**Directory Structure**:
- `.claude-flow/` - Agent configuration and metrics
- `.hive-mind/` - Persistent state and coordination database

## 🔄 Working Process Phases

### Phase 1: Discovery & Planning (Week 1-2)

**Objective**: Understand current state and plan migration strategy

**Parallel Agent Execution**:
```javascript
[Single Message - All agents spawned concurrently]:
  Task("Migration Architect", "
    - Analyze current RawRabbit .NET framework version
    - Map all dependencies and NuGet packages
    - Identify .NET 9 compatibility issues
    - Create migration roadmap
    - Store findings in memory: migration/discovery
    - Record ADR-001: Migration Strategy Decision

    HOOKS:
    npx claude-flow@alpha hooks pre-task --description 'Phase 1 Discovery'
    npx claude-flow@alpha hooks session-restore --session-id 'dotnet9-upgrade'
    npx claude-flow@alpha hooks post-edit --file '[file]' --memory-key 'migration/discovery'
    npx claude-flow@alpha hooks notify --message 'Discovery complete'
    npx claude-flow@alpha hooks post-task --task-id 'discovery'
  ", "migration-planner")

  Task("Security Specialist", "
    - Perform security baseline audit on current codebase
    - Identify vulnerabilities in current .NET version
    - Research .NET 9 security improvements and features
    - Create comprehensive security upgrade checklist
    - Store findings in memory: security/baseline
    - Record ADR-002: Security Architecture Decisions

    HOOKS:
    npx claude-flow@alpha hooks pre-task --description 'Security Baseline'
    npx claude-flow@alpha hooks session-restore --session-id 'dotnet9-upgrade'
    npx claude-flow@alpha hooks post-edit --file '[file]' --memory-key 'security/baseline'
    npx claude-flow@alpha hooks notify --message 'Security audit complete'
    npx claude-flow@alpha hooks post-task --task-id 'security-audit'
  ", "security-manager")

  Task("Documentation Specialist", "
    - Set up ADR directory structure at docs/adr/
    - Create ADR template with standard sections
    - Initialize migration documentation structure
    - Set up changelog and version tracking
    - Store templates in memory: docs/templates

    HOOKS:
    npx claude-flow@alpha hooks pre-task --description 'Documentation Setup'
    npx claude-flow@alpha hooks session-restore --session-id 'dotnet9-upgrade'
    npx claude-flow@alpha hooks post-edit --file 'docs/adr/template.md' --memory-key 'docs/templates'
    npx claude-flow@alpha hooks notify --message 'Documentation structure ready'
    npx claude-flow@alpha hooks post-task --task-id 'docs-setup'
  ", "researcher")

  TodoWrite { todos: [
    {content: "Complete dependency analysis", status: "in_progress", activeForm: "Analyzing dependencies"},
    {content: "Security baseline audit", status: "in_progress", activeForm: "Performing security audit"},
    {content: "Setup ADR structure", status: "in_progress", activeForm: "Setting up ADR structure"},
    {content: "Identify breaking changes", status: "pending", activeForm: "Identifying breaking changes"},
    {content: "Create migration roadmap", status: "pending", activeForm: "Creating migration roadmap"},
    {content: "Document security requirements", status: "pending", activeForm: "Documenting security requirements"}
  ]}
```

**Deliverables**:
- Migration strategy document
- Security baseline audit report
- ADR-001: Migration Strategy
- ADR-002: Security Architecture
- Dependency upgrade matrix
- Documentation structure

---

### Phase 2: Architecture & Design (Week 2-3)

**Objective**: Design target architecture and validate security approach

**Sequential Execution with Coordination**:

**Step 1 - Architecture Design**:
```javascript
Task("Migration Architect", "
  - Design .NET 9 target architecture
  - Define component migration order
  - Plan database schema updates if needed
  - Identify API contract changes
  - Record ADR-003: Framework Architecture
  - Store design in memory: migration/architecture

  HOOKS:
  npx claude-flow@alpha hooks pre-task --description 'Architecture Design'
  npx claude-flow@alpha hooks session-restore --session-id 'dotnet9-upgrade'
  npx claude-flow@alpha memory query 'migration/discovery'
  npx claude-flow@alpha hooks post-edit --memory-key 'migration/architecture'
  npx claude-flow@alpha hooks notify --message 'Architecture design complete'
", "migration-planner")
```

**Step 2 - Security Review** (waits for architecture):
```javascript
Task("Security Specialist", "
  - Review proposed architecture from memory
  - Validate against security requirements
  - Identify security concerns and mitigations
  - Record ADR-004: Security Review Results
  - Update security checklist

  HOOKS:
  npx claude-flow@alpha hooks pre-task --description 'Security Review'
  npx claude-flow@alpha hooks session-restore --session-id 'dotnet9-upgrade'
  npx claude-flow@alpha memory query 'migration/architecture'
  npx claude-flow@alpha hooks post-edit --memory-key 'security/reviews/architecture'
  npx claude-flow@alpha hooks notify --message 'Architecture security validated'
", "security-manager")
```

**Step 3 - Documentation** (captures all decisions):
```javascript
Task("Documentation Specialist", "
  - Create ADR-003: Framework Architecture
  - Create ADR-004: Security Review Results
  - Document architectural decisions and rationale
  - Update migration documentation
  - Link related ADRs

  HOOKS:
  npx claude-flow@alpha hooks pre-task --description 'ADR Documentation'
  npx claude-flow@alpha hooks session-restore --session-id 'dotnet9-upgrade'
  npx claude-flow@alpha memory query 'migration/architecture'
  npx claude-flow@alpha memory query 'security/reviews/architecture'
  npx claude-flow@alpha hooks post-edit --file 'docs/adr/0003-framework-architecture.md' --memory-key 'docs/adr'
  npx claude-flow@alpha hooks notify --message 'ADRs documented'
", "researcher")
```

**Deliverables**:
- ADR-003: Framework Architecture
- ADR-004: Security Review Results
- Component migration plan
- Updated security checklist

---

### Phase 3: Implementation (Week 3-8)

**Objective**: Migrate components to .NET 9 with continuous testing and security validation

**Component-by-Component Parallel Workflow**:

For each component (e.g., RawRabbit.Core, RawRabbit.Operations, etc.):

```javascript
[Single Message - Per Component]:
  Task(".NET Modernizer", "
    Component: [COMPONENT_NAME]

    - Upgrade .csproj to .NET 9 target framework
    - Update NuGet package references
    - Refactor deprecated API usage
    - Implement .NET 9 performance improvements
    - Replace obsolete patterns with modern equivalents
    - Run hooks for coordination
    - Commit changes with detailed message

    HOOKS:
    npx claude-flow@alpha hooks pre-task --description 'Upgrade [COMPONENT_NAME]'
    npx claude-flow@alpha hooks session-restore --session-id 'dotnet9-upgrade'
    npx claude-flow@alpha hooks post-edit --file 'src/[COMPONENT_NAME]/*' --memory-key 'migration/components/[COMPONENT_NAME]'
    npx claude-flow@alpha hooks notify --message '[COMPONENT_NAME] upgraded'
  ", "backend-dev")

  Task("QA Engineer", "
    Component: [COMPONENT_NAME]

    - Review component changes from memory
    - Write unit tests for new code
    - Create regression tests for existing functionality
    - Run performance benchmarks (before/after)
    - Validate functionality against requirements
    - Document test results in memory

    HOOKS:
    npx claude-flow@alpha hooks pre-task --description 'Test [COMPONENT_NAME]'
    npx claude-flow@alpha hooks session-restore --session-id 'dotnet9-upgrade'
    npx claude-flow@alpha memory query 'migration/components/[COMPONENT_NAME]'
    npx claude-flow@alpha hooks post-edit --file 'tests/[COMPONENT_NAME].Tests/*' --memory-key 'testing/results/[COMPONENT_NAME]'
    npx claude-flow@alpha hooks notify --message '[COMPONENT_NAME] tests complete'
  ", "tester")

  Task("Security Specialist", "
    Component: [COMPONENT_NAME]

    - Security code review of changes
    - Validate authentication/authorization updates
    - Check for common vulnerabilities (OWASP)
    - Verify secure coding practices
    - Scan for dependency vulnerabilities
    - Document security findings
    - Record ADR if security-critical changes made

    HOOKS:
    npx claude-flow@alpha hooks pre-task --description 'Security Review [COMPONENT_NAME]'
    npx claude-flow@alpha hooks session-restore --session-id 'dotnet9-upgrade'
    npx claude-flow@alpha memory query 'migration/components/[COMPONENT_NAME]'
    npx claude-flow@alpha hooks post-edit --memory-key 'security/reviews/[COMPONENT_NAME]'
    npx claude-flow@alpha hooks notify --message '[COMPONENT_NAME] security validated'
  ", "security-manager")

  TodoWrite { todos: [
    {content: "Upgrade [COMPONENT_NAME] to .NET 9", status: "in_progress", activeForm: "Upgrading [COMPONENT_NAME]"},
    {content: "Test [COMPONENT_NAME]", status: "in_progress", activeForm: "Testing [COMPONENT_NAME]"},
    {content: "Security review [COMPONENT_NAME]", status: "in_progress", activeForm: "Reviewing [COMPONENT_NAME] security"},
    {content: "Document [COMPONENT_NAME] changes", status: "pending", activeForm: "Documenting [COMPONENT_NAME] changes"}
  ]}
```

**Component Migration Order** (based on dependencies):
1. RawRabbit.Configuration
2. RawRabbit.Common
3. RawRabbit.Core
4. RawRabbit.Channel
5. RawRabbit.Pipe
6. RawRabbit.Operations
7. RawRabbit.Enrichers
8. RawRabbit.DependencyInjection
9. RawRabbit.Instantiation
10. Sample applications

**Per-Component Workflow**:
1. Modernizer upgrades code
2. Tester validates functionality
3. Security reviews changes
4. Documentation records changes
5. Repeat for next component

**Deliverables**:
- Modernized components
- Test suites with 90%+ coverage
- Security review reports
- Component-specific ADRs (as needed)
- Migration changelog

---

### Phase 4: Integration & Testing (Week 8-10)

**Objective**: Validate complete system and prepare for deployment

**Parallel Testing Execution**:
```javascript
[Single Message - Integration Testing]:
  Task("QA Engineer", "
    - Run full integration test suite across all components
    - Performance benchmarking of complete system
    - Load testing and stress testing
    - Compatibility testing with RabbitMQ versions
    - Regression testing against baseline
    - Document all test results

    HOOKS:
    npx claude-flow@alpha hooks pre-task --description 'Integration Testing'
    npx claude-flow@alpha hooks session-restore --session-id 'dotnet9-upgrade'
    npx claude-flow@alpha hooks post-edit --memory-key 'testing/integration'
    npx claude-flow@alpha hooks notify --message 'Integration tests complete'
  ", "tester")

  Task("Security Specialist", "
    - Full application security scan
    - Penetration testing
    - Dependency vulnerability audit
    - Authentication/authorization validation
    - Data protection compliance check
    - Security checkpoint report

    HOOKS:
    npx claude-flow@alpha hooks pre-task --description 'Security Testing'
    npx claude-flow@alpha hooks session-restore --session-id 'dotnet9-upgrade'
    npx claude-flow@alpha hooks post-edit --memory-key 'security/final-audit'
    npx claude-flow@alpha hooks notify --message 'Security testing complete'
  ", "security-manager")

  Task("DevOps Engineer", "
    - Build complete application with .NET 9
    - Update Docker containers
    - Run automated deployment to staging
    - Validate CI/CD pipelines
    - Monitor build performance and metrics
    - Prepare production deployment scripts

    HOOKS:
    npx claude-flow@alpha hooks pre-task --description 'Build & Deploy'
    npx claude-flow@alpha hooks session-restore --session-id 'dotnet9-upgrade'
    npx claude-flow@alpha hooks post-edit --memory-key 'devops/build'
    npx claude-flow@alpha hooks notify --message 'Build and deployment ready'
  ", "cicd-engineer")

  TodoWrite { todos: [
    {content: "Run integration tests", status: "in_progress", activeForm: "Running integration tests"},
    {content: "Security penetration testing", status: "in_progress", activeForm: "Performing penetration testing"},
    {content: "Build and containerize", status: "in_progress", activeForm: "Building and containerizing"},
    {content: "Performance benchmarking", status: "pending", activeForm: "Benchmarking performance"},
    {content: "Staging deployment", status: "pending", activeForm: "Deploying to staging"},
    {content: "Validation testing", status: "pending", activeForm: "Validating deployment"}
  ]}
```

**Deliverables**:
- Integration test results
- Performance benchmark report
- Security audit report
- Staging deployment
- Build artifacts

---

### Phase 5: Documentation & Deployment (Week 10-12)

**Objective**: Finalize documentation and deploy to production

**Final Coordination**:
```javascript
[Single Message - Final Phase]:
  Task("Documentation Specialist", "
    - Finalize all ADR records (review and validate)
    - Create comprehensive deployment runbook
    - Generate complete changelog with all changes
    - Update README and API documentation
    - Create migration guide for users
    - Archive all decision records

    HOOKS:
    npx claude-flow@alpha hooks pre-task --description 'Final Documentation'
    npx claude-flow@alpha hooks session-restore --session-id 'dotnet9-upgrade'
    npx claude-flow@alpha memory query 'docs/adr'
    npx claude-flow@alpha hooks post-edit --memory-key 'docs/final'
    npx claude-flow@alpha hooks notify --message 'Documentation complete'
  ", "researcher")

  Task("DevOps Engineer", "
    - Production deployment preparation
    - Rollback procedures and testing
    - Monitoring and alerting setup
    - Performance monitoring dashboard
    - Post-deployment validation plan
    - Execute production deployment

    HOOKS:
    npx claude-flow@alpha hooks pre-task --description 'Production Deployment'
    npx claude-flow@alpha hooks session-restore --session-id 'dotnet9-upgrade'
    npx claude-flow@alpha hooks post-edit --memory-key 'devops/production'
    npx claude-flow@alpha hooks notify --message 'Production deployment complete'
    npx claude-flow@alpha hooks session-end --export-metrics true
  ", "cicd-engineer")

  Task("Migration Architect", "
    - Final architecture review
    - Validate all ADRs are recorded
    - Create post-migration report
    - Document lessons learned
    - Archive project metrics and outcomes

    HOOKS:
    npx claude-flow@alpha hooks pre-task --description 'Final Review'
    npx claude-flow@alpha hooks session-restore --session-id 'dotnet9-upgrade'
    npx claude-flow@alpha hooks post-edit --memory-key 'migration/final'
    npx claude-flow@alpha hooks notify --message 'Migration complete'
    npx claude-flow@alpha hooks session-end --export-metrics true
  ", "migration-planner")

  TodoWrite { todos: [
    {content: "Finalize ADR records", status: "in_progress", activeForm: "Finalizing ADR records"},
    {content: "Create deployment runbook", status: "in_progress", activeForm: "Creating deployment runbook"},
    {content: "Production deployment", status: "in_progress", activeForm: "Deploying to production"},
    {content: "Post-deployment validation", status: "pending", activeForm: "Validating production deployment"},
    {content: "Generate final reports", status: "pending", activeForm: "Generating final reports"},
    {content: "Archive project", status: "pending", activeForm: "Archiving project"}
  ]}
```

**Deliverables**:
- Complete ADR repository
- Deployment runbook
- Comprehensive changelog
- Migration guide
- Production deployment
- Post-migration report

---

## 📝 ADR Workflow & Standards

### ADR Template

Location: `docs/adr/template.md`

```markdown
# ADR-XXX: [Decision Title]

## Status
[Proposed | Accepted | Deprecated | Superseded]

## Context
[What is the issue we're trying to solve?]
[What are the constraints and requirements?]

## Decision
[What is the change we're making?]
[Why this approach over alternatives?]

## Consequences
### Positive
- [What becomes easier or better]

### Negative
- [What becomes harder or worse]

### Risks
- [What risks are introduced]

## Alternatives Considered
### Alternative 1: [Name]
- Description
- Pros
- Cons
- Why rejected

### Alternative 2: [Name]
- Description
- Pros
- Cons
- Why rejected

## Related Decisions
- [ADR-001: Related Decision 1]
- [ADR-002: Related Decision 2]

## References
- [Links to documentation, RFCs, discussions]
```

### When to Create ADRs

**Required** for:
- ✅ Framework version selection (ADR-001)
- ✅ Security architecture changes (ADR-002+)
- ✅ API design decisions
- ✅ Database schema changes
- ✅ Authentication/authorization patterns
- ✅ Deployment strategy changes
- ✅ Breaking changes requiring migration
- ✅ Performance optimization strategies
- ✅ Third-party library selections
- ✅ Infrastructure changes

**Optional** for:
- Minor refactoring without API changes
- Code style updates
- Documentation improvements
- Bug fixes without design impact

### ADR Numbering & Storage

- **Format**: `XXXX-title-kebab-case.md` (e.g., `0001-migration-strategy.md`)
- **Numbering**: Sequential starting at 0001
- **Location**: `docs/adr/`
- **Index**: Maintain `docs/adr/README.md` with all ADRs listed

### ADR Review Process

1. **Proposal**: Agent proposes decision and drafts ADR
2. **Security Review**: If security-related, Security Specialist validates
3. **Architecture Review**: Migration Architect reviews for alignment
4. **Documentation**: Documentation Specialist creates final ADR file
5. **Commit**: ADR committed to repository with descriptive message
6. **Reference**: ADR number referenced in related code comments

### ADR Status Lifecycle

- **Proposed**: Initial draft, under discussion
- **Accepted**: Approved and implemented
- **Deprecated**: No longer recommended but still in use
- **Superseded**: Replaced by newer ADR (link to replacement)

---

## 🔐 Security Review Checkpoints

### Checkpoint 1: Pre-Migration Baseline
**Timing**: Phase 1 (Week 1)
**Responsible**: Security Specialist

**Activities**:
- Current vulnerability scan
- Dependency security audit
- Authentication/authorization review
- Data protection assessment
- Identify security debt

**Output**: Security Baseline Report + ADR-002

---

### Checkpoint 2: Component Security Review
**Timing**: Phase 3 (Per Component)
**Responsible**: Security Specialist

**Activities**:
- Code review of component changes
- API security validation
- Input validation checks
- Error handling review
- Dependency update verification

**Output**: Per-component security reports

---

### Checkpoint 3: Integration Security Test
**Timing**: Phase 4 (Week 8-9)
**Responsible**: Security Specialist + QA Engineer

**Activities**:
- Full application security scan
- Penetration testing
- Authentication flow testing
- Authorization boundary testing
- Data protection compliance

**Output**: Integration Security Report

---

### Checkpoint 4: Pre-Production Security Audit
**Timing**: Phase 5 (Week 11)
**Responsible**: Security Specialist

**Activities**:
- Final vulnerability scan
- Security configuration review
- Deployment security validation
- Monitoring and alerting setup
- Incident response preparation

**Output**: Production Security Clearance

---

## 🤝 Agent Coordination Protocols

### Memory Namespaces

All agents use structured memory namespaces for coordination:

- `migration/discovery` - Initial analysis and findings
- `migration/architecture` - Design decisions and architecture
- `migration/components/{name}` - Per-component migration work
- `security/baseline` - Security baseline audit
- `security/reviews/{component}` - Component security reviews
- `security/final-audit` - Final security validation
- `docs/adr` - ADR records and templates
- `docs/templates` - Documentation templates
- `testing/results` - Test execution results
- `testing/benchmarks` - Performance benchmarks
- `devops/pipelines` - CI/CD configurations
- `devops/build` - Build artifacts and logs

### Communication Pattern

**Before Starting Work**:
```bash
npx claude-flow@alpha hooks pre-task --description "[task description]"
npx claude-flow@alpha hooks session-restore --session-id "dotnet9-upgrade"
```

**During Work**:
```bash
npx claude-flow@alpha hooks post-edit --file "[file path]" --memory-key "[namespace/key]"
npx claude-flow@alpha hooks notify --message "[status update]"
```

**After Completing Work**:
```bash
npx claude-flow@alpha hooks post-task --task-id "[task-id]"
npx claude-flow@alpha hooks session-end --export-metrics true
```

**Querying Shared Context**:
```bash
npx claude-flow@alpha memory query "[namespace/key]"
npx claude-flow@alpha memory search "[search term]"
```

### Inter-Agent Dependencies

**Sequential Dependencies**:
- Architecture Design → Security Review → Documentation
- Code Changes → Testing → Security Review
- Integration Testing → Deployment Preparation

**Parallel Execution**:
- Discovery phase (3 agents concurrently)
- Component migration (3 agents per component)
- Integration testing (3 agents concurrently)

### Conflict Resolution

If agents encounter conflicts:
1. Store conflict in memory with namespace `conflicts/{id}`
2. Notify Migration Architect via hooks
3. Migration Architect reviews and decides
4. Decision recorded as ADR
5. All agents notified of resolution

---

## 📊 Progress Tracking & Reporting

### Daily Progress Updates

Each agent reports to memory at end of day:
```bash
npx claude-flow@alpha memory store "progress/[agent-name]/[date]" "[summary]"
```

### Weekly Status Reports

**Generated by**: Documentation Specialist
**Frequency**: Every Friday
**Contents**:
- Completed components
- ADRs created this week
- Security findings
- Test coverage metrics
- Blockers and risks
- Next week's plan

### Project Metrics

Tracked automatically via claude-flow:
- Task completion rate
- Agent utilization
- Token usage
- Performance metrics
- Error rates

### Success Criteria

Project considered complete when:
- ✅ All components migrated to .NET 9
- ✅ All tests passing with 90%+ coverage
- ✅ Security audit passed
- ✅ All ADRs documented
- ✅ Production deployment successful
- ✅ No critical bugs in first 2 weeks

---

## 🚀 Quick Start Commands

### Step 0: System Initialization (REQUIRED ONCE)
```bash
# Initialize Hive Mind system (do this first!)
npx claude-flow@alpha hive-mind init

# Optional: Use wizard for interactive setup
npx claude-flow@alpha hive-mind wizard

# Verify initialization
ls -la .hive-mind/
```

### Step 1: Initialize Project
```bash
# Start discovery phase
npx claude-flow@alpha swarm "Start .NET 9 migration discovery phase"

# Check agent status
npx claude-flow@alpha agent list

# View memory contents
npx claude-flow@alpha memory search "migration"
```

### Component Migration
```bash
# Migrate specific component
npx claude-flow@alpha swarm "Migrate RawRabbit.Core to .NET 9"

# Check test results
npx claude-flow@alpha memory query "testing/results/RawRabbit.Core"
```

### Documentation
```bash
# Generate ADR
npx claude-flow@alpha swarm "Create ADR for [decision]"

# View all ADRs
ls docs/adr/
```

### Monitoring
```bash
# Check system status
npx claude-flow@alpha status

# View performance metrics
npx claude-flow@alpha analysis token-usage

# Check agent health
npx claude-flow@alpha monitoring agents
```

---

## 📚 References

### Configuration Files
- `.claude-flow/config.json` - Agent definitions and workflow
- `.hive-mind/config.json` - Hive Mind system configuration
- `.hive-mind/hive.db` - SQLite coordination database

### Documentation
- `docs/adr/` - ADR Repository
- `docs/migration-roadmap.md` - Generated in Phase 1
- `docs/security-checklist.md` - Generated in Phase 1
- `docs/test-reports/` - Generated throughout project

### Metrics & Monitoring
- `.claude-flow/metrics/performance.json` - Performance metrics
- `.claude-flow/metrics/task-metrics.json` - Task execution metrics
- `.claude-flow/metrics/agent-metrics.json` - Agent utilization metrics
- `.claude-flow/metrics/system-metrics.json` - System health metrics

---

## 🛠️ Troubleshooting

### Hive Mind Issues
```bash
# Reset Hive Mind (if corrupted)
rm -rf .hive-mind
npx claude-flow@alpha hive-mind init

# Check Hive Mind status
npx claude-flow@alpha status
```

### Agent Coordination Issues
```bash
# List active agents
npx claude-flow@alpha agent list

# Terminate stuck agent
npx claude-flow@alpha agent terminate <agent-id>

# Check memory consistency
npx claude-flow@alpha memory search "migration"
```

### Performance Issues
```bash
# View performance metrics
npx claude-flow@alpha analysis token-usage

# Check bottlenecks
npx claude-flow@alpha monitoring agents

# View system health
npx claude-flow@alpha status
```

---

**Document Version**: 1.1.0
**Last Updated**: 2025-10-09
**Session ID**: dotnet9-upgrade
**Hive Mind**: Initialized ✅
