# RawRabbit Documentation

This directory contains all documentation for the RawRabbit project, including API documentation, guides, and planning materials for the .NET 9 upgrade.

---

## 📁 Directory Structure

```
docs/
├── README.md (this file)
├── HISTORY.md (Work history tracking for .NET 9 upgrade)
│
├── planning/ (⭐ .NET 9 Upgrade Planning)
│   ├── PLAN.md (Primary upgrade plan - START HERE)
│   ├── Agent reviews (Migration, Security, QA, DevOps, etc.)
│   └── Supporting documents (dependency graph, action items, etc.)
│
├── getting-started/ (User guides for RawRabbit)
│   ├── installation.md
│   ├── configuration.md
│   ├── register-client.md
│   ├── client-lifetime.md
│   ├── logging.md
│   └── modify-pre-defined-operations.md
│
├── operations/ (RabbitMQ operations documentation)
│   ├── publish.md
│   ├── subscribe.md
│   ├── request.md (RPC request)
│   ├── respond.md (RPC respond)
│   ├── get.md
│   ├── message-sequence.md
│   └── statemachine.md
│
├── enrichers/ (Middleware enrichers documentation)
│   ├── attribute-routing.md
│   ├── global-execution-id.md
│   ├── httpcontext.md
│   ├── message-context.md
│   ├── polly.md
│   └── queue-name-suffix.md
│
├── rabbitmq-features/ (RabbitMQ feature documentation)
│   └── [RabbitMQ-specific features]
│
├── index.rst (Sphinx documentation index)
├── conf.py (Sphinx configuration)
├── Makefile (Documentation build - Linux/Mac)
└── make.bat (Documentation build - Windows)
```

---

## 📚 Documentation Categories

### 1. ⭐ .NET 9 Upgrade Planning (`planning/`)

**Purpose**: Comprehensive planning and review documents for upgrading RawRabbit from .NET Standard 1.5 / .NET Framework 4.5.1 to .NET 9.

**Key Documents**:
- **[planning/PLAN.md](planning/PLAN.md)** - 8-stage, 13-15 week upgrade plan (PRIMARY DOCUMENT)
- **[planning/PLAN-UPDATES-v1.1.md](planning/PLAN-UPDATES-v1.1.md)** - Consolidated agent feedback and proposed updates
- **[planning/REVIEW-SUMMARY.md](planning/REVIEW-SUMMARY.md)** - Executive summary of critical findings
- **[planning/IMMEDIATE-ACTIONS.md](planning/IMMEDIATE-ACTIONS.md)** - Pre-Stage 1 checklist (must complete before starting)

**Agent Reviews**:
- [planning/PLAN-REVIEW.md](planning/PLAN-REVIEW.md) - Migration Architect's technical review
- [planning/security-specialist-review.md](planning/security-specialist-review.md) - Security assessment
- [planning/qa-review-net9-upgrade.md](planning/qa-review-net9-upgrade.md) - QA testing strategy
- [planning/devops-review.md](planning/devops-review.md) - DevOps infrastructure review
- [planning/dotnet-modernizer-review.md](planning/dotnet-modernizer-review.md) - Code transformation review
- [planning/DOCUMENTATION-REVIEW.md](planning/DOCUMENTATION-REVIEW.md) - Documentation review

**Supporting Documents**:
- [planning/dependency-graph.mermaid](planning/dependency-graph.mermaid) - Visual component dependency graph
- [planning/security-review-plan.md](planning/security-review-plan.md) - Security validation process

**Who Should Read**:
- **Project Managers**: Start with REVIEW-SUMMARY.md, then PLAN.md
- **Developers**: Read IMMEDIATE-ACTIONS.md, then relevant PLAN.md sections
- **Security/Compliance**: Read security-specialist-review.md
- **QA/Testing**: Read qa-review-net9-upgrade.md

---

### 2. 📖 User Documentation

#### Getting Started (`getting-started/`)

**Purpose**: Onboarding guides for developers new to RawRabbit.

**Documents**:
- **[installation.md](getting-started/installation.md)** - How to install RawRabbit packages
- **[configuration.md](getting-started/configuration.md)** - Configure RabbitMQ connection settings
- **[register-client.md](getting-started/register-client.md)** - Register RawRabbit with DI containers
- **[client-lifetime.md](getting-started/client-lifetime.md)** - Managing client lifecycle
- **[logging.md](getting-started/logging.md)** - Configure logging and diagnostics
- **[modify-pre-defined-operations.md](getting-started/modify-pre-defined-operations.md)** - Customize operations

**Recommended Reading Order**:
1. installation.md
2. register-client.md
3. configuration.md
4. Then explore operations/ or enrichers/

---

#### Operations (`operations/`)

**Purpose**: Documentation for RabbitMQ messaging operations (publish/subscribe, RPC, etc.).

**Documents**:
- **[publish.md](operations/publish.md)** - Fire-and-forget message publishing
- **[subscribe.md](operations/subscribe.md)** - Message consumption and handling
- **[request.md](operations/request.md)** - RPC request (synchronous call)
- **[respond.md](operations/respond.md)** - RPC respond (handle requests)
- **[get.md](operations/get.md)** - Single message retrieval
- **[message-sequence.md](operations/message-sequence.md)** - Choreographed message flows
- **[statemachine.md](operations/statemachine.md)** - Stateful message handling

**Common Use Cases**:
- **Simple messaging**: publish.md + subscribe.md
- **Request/Response (RPC)**: request.md + respond.md
- **Complex workflows**: message-sequence.md or statemachine.md

---

#### Enrichers (`enrichers/`)

**Purpose**: Middleware enrichers that add functionality to the message pipeline.

**Documents**:
- **[attribute-routing.md](enrichers/attribute-routing.md)** - Attribute-based configuration
- **[global-execution-id.md](enrichers/global-execution-id.md)** - Distributed tracing
- **[httpcontext.md](enrichers/httpcontext.md)** - ASP.NET Core integration
- **[message-context.md](enrichers/message-context.md)** - Core enricher functionality
- **[polly.md](enrichers/polly.md)** - Resilience policies (retry, circuit breaker)
- **[queue-name-suffix.md](enrichers/queue-name-suffix.md)** - Dynamic queue naming

**When to Use Enrichers**:
- Need distributed tracing? → global-execution-id.md
- Need retry logic? → polly.md
- Integrating with ASP.NET Core? → httpcontext.md
- Want custom middleware? → message-context.md

---

### 3. 📝 Project Documentation

#### [HISTORY.md](HISTORY.md)

**Purpose**: Chronological work log for the .NET 9 upgrade project.

**Format**:
```markdown
## YYYY-MM-DD - Brief Description
### What was changed
- List of changes
### Why it was changed
- Rationale
### Impact on the codebase
- Effects of the change
```

**Who Should Read**: Anyone wanting to understand the project's evolution and decisions made during the upgrade.

---

### 4. 🔧 Documentation Build System

#### Sphinx Documentation

RawRabbit uses [Sphinx](https://www.sphinx-doc.org/) for generating HTML documentation.

**Files**:
- **[index.rst](index.rst)** - Main documentation index (reStructuredText format)
- **[conf.py](conf.py)** - Sphinx configuration
- **[Makefile](Makefile)** - Build commands (Linux/Mac)
- **[make.bat](make.bat)** - Build commands (Windows)

**Build Documentation**:
```bash
# Linux/Mac
cd docs/
make html

# Windows
cd docs/
make.bat html
```

**Output**: Generated HTML documentation in `docs/_build/html/`

---

## 🚀 Quick Start Guide

### For New Developers

1. **Learn RawRabbit basics**:
   - Read [getting-started/installation.md](getting-started/installation.md)
   - Read [getting-started/register-client.md](getting-started/register-client.md)
   - Try [operations/publish.md](operations/publish.md) and [operations/subscribe.md](operations/subscribe.md)

2. **Understand the .NET 9 upgrade**:
   - Read [planning/REVIEW-SUMMARY.md](planning/REVIEW-SUMMARY.md) (5-minute overview)
   - Skim [planning/PLAN.md](planning/PLAN.md) executive summary

3. **Find specific information**:
   - Operations: Browse `operations/` directory
   - Middleware: Browse `enrichers/` directory
   - Upgrade details: Browse `planning/` directory

---

### For Contributors to .NET 9 Upgrade

1. **Start with immediate actions**:
   - Read [planning/IMMEDIATE-ACTIONS.md](planning/IMMEDIATE-ACTIONS.md)
   - Complete all Pre-Stage 1 tasks

2. **Understand the plan**:
   - Read [planning/PLAN.md](planning/PLAN.md) in full
   - Review [planning/dependency-graph.mermaid](planning/dependency-graph.mermaid)

3. **Consult specialized reviews**:
   - **Technical**: [planning/PLAN-REVIEW.md](planning/PLAN-REVIEW.md)
   - **Security**: [planning/security-specialist-review.md](planning/security-specialist-review.md)
   - **Testing**: [planning/qa-review-net9-upgrade.md](planning/qa-review-net9-upgrade.md)

4. **Track work**:
   - Update [HISTORY.md](HISTORY.md) after completing tasks
   - Create ADRs in `adr/` directory (to be created during upgrade)

---

### For Security / Compliance Teams

1. **Critical security issues**:
   - Read [planning/security-specialist-review.md](planning/security-specialist-review.md)
   - Focus on CVE analysis (RabbitMQ.Client, Newtonsoft.Json)
   - Review hardcoded credentials section

2. **Security process**:
   - Read [planning/security-review-plan.md](planning/security-review-plan.md)
   - Note 9 security checkpoints (expanded from 4)

3. **Compliance requirements**:
   - SBOM generation planned (Stage 7)
   - Package signing planned (Stage 7)
   - Supply chain security addressed

---

### For QA / Testing Teams

1. **Test strategy**:
   - Read [planning/qa-review-net9-upgrade.md](planning/qa-review-net9-upgrade.md)
   - Note revised coverage targets (75% overall, component-specific)

2. **Test infrastructure**:
   - Docker RabbitMQ setup required (docker-compose.test.yml)
   - GitHub Actions workflow needed
   - Coverlet integration for coverage

3. **Testing phases**:
   - Review [planning/PLAN.md](planning/PLAN.md) Stage 6 (Integration Testing)
   - Performance benchmarking requirements
   - Regression testing strategy

---

## 📍 Navigation Tips

### Finding Information by Topic

| Topic | Location |
|-------|----------|
| **Installation** | `getting-started/installation.md` |
| **Configuration** | `getting-started/configuration.md` |
| **Publish/Subscribe** | `operations/publish.md`, `operations/subscribe.md` |
| **Request/Response (RPC)** | `operations/request.md`, `operations/respond.md` |
| **Middleware/Enrichers** | `enrichers/` directory |
| **.NET 9 Upgrade Plan** | `planning/PLAN.md` |
| **Security Issues** | `planning/security-specialist-review.md` |
| **Test Strategy** | `planning/qa-review-net9-upgrade.md` |
| **Work History** | `HISTORY.md` |

---

### Finding Information by Role

| Role | Start Here | Then Read |
|------|------------|-----------|
| **New Developer** | `getting-started/installation.md` | `operations/publish.md`, `operations/subscribe.md` |
| **Project Manager** | `planning/REVIEW-SUMMARY.md` | `planning/PLAN.md` executive summary |
| **Architect** | `planning/PLAN.md` | `planning/PLAN-REVIEW.md` |
| **Security Engineer** | `planning/security-specialist-review.md` | `planning/security-review-plan.md` |
| **QA Engineer** | `planning/qa-review-net9-upgrade.md` | `planning/PLAN.md` Stage 6 |
| **DevOps Engineer** | `planning/devops-review.md` | `planning/PLAN.md` Stage 7-8 |

---

## 🔗 Related Documentation

**Project Root Documentation**:
- `/README.md` - RawRabbit project overview and quickstart
- `/CLAUDE.md` - Development guide for Claude Code agents
- `/CLAUDE-AGENTS.md` - Agent coordination workflows
- `/.claude-flow/config.json` - Agent configuration

**To Be Created During Upgrade**:
- `/docs/adr/` - Architecture Decision Records (ADRs)
- `/docs/test/` - Test reports and coverage
- `/docs/security/` - Security audit reports
- `/docs/MIGRATION-GUIDE.md` - User upgrade guide
- `/docs/BREAKING-CHANGES.md` - Breaking changes documentation

---

## 📊 Documentation Status

### Existing Documentation (Stable)

| Section | Status | Last Updated |
|---------|--------|--------------|
| getting-started/ | ✅ Stable | 2.x release |
| operations/ | ✅ Stable | 2.x release |
| enrichers/ | ✅ Stable | 2.x release |
| rabbitmq-features/ | ✅ Stable | 2.x release |

### .NET 9 Upgrade Documentation (In Progress)

| Document | Version | Status | Last Updated |
|----------|---------|--------|--------------|
| planning/PLAN.md | v1.1 | ✅ Active | 2025-10-09 |
| planning/PLAN-UPDATES-v1.1.md | 1.0 | 📋 Proposed | 2025-10-09 |
| planning/PLAN-REVIEW.md | 1.0 | ✅ Complete | 2025-10-09 |
| planning/security-specialist-review.md | 1.0 | ✅ Complete | 2025-10-09 |
| planning/qa-review-net9-upgrade.md | 1.0 | ✅ Complete | 2025-10-09 |
| HISTORY.md | - | ✅ Active | 2025-10-09 |

---

## ❓ Questions or Issues?

- **RawRabbit usage questions**: Check `getting-started/`, `operations/`, or `enrichers/`
- **.NET 9 upgrade questions**: Check `planning/PLAN.md` or relevant agent reviews
- **Security concerns**: See `planning/security-specialist-review.md`
- **Testing questions**: See `planning/qa-review-net9-upgrade.md`

---

## 🏗️ Building Documentation

### Generate HTML Documentation

```bash
# Install Sphinx (if not already installed)
pip install sphinx sphinx-rtd-theme

# Build HTML documentation
cd docs/
make html

# View documentation
open _build/html/index.html  # macOS
xdg-open _build/html/index.html  # Linux
start _build/html/index.html  # Windows
```

### Clean Build

```bash
cd docs/
make clean
make html
```

---

**Last Updated**: 2025-10-09
**Maintained By**: RawRabbit Development Team
**Documentation Version**: 2.x (upgrading to 3.0 for .NET 9)
