# Immediate Actions - Before Stage 1

**Target Completion**: Within 1 week before Stage 1 kickoff
**Status**: ⚠️ NOT STARTED

---

## Priority 1: Blockers 🚨

### 1. Install .NET 9 SDK
**Status**: ❌ NOT INSTALLED
**Owner**: DevOps / Development Team
**Timeline**: Day 1

```bash
# Linux installation
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --version 9.0.100

# Verify installation
dotnet --version  # Should show 9.0.100 or higher
dotnet --list-sdks

# Test project creation
dotnet new console -n TestNet9 -f net9.0
cd TestNet9
dotnet build
dotnet run
```

**Deliverable**: Confirmation that .NET 9 SDK is installed and functional

---

### 2. Research RabbitMQ.Client 5.x → 7.x Breaking Changes
**Status**: ❌ NOT STARTED
**Owner**: Migration Architect
**Timeline**: Day 1-2

**Tasks**:
- [ ] Review [RabbitMQ.Client GitHub Releases](https://github.com/rabbitmq/rabbitmq-dotnet-client/releases)
- [ ] Read migration guides for 6.x and 7.x
- [ ] Test basic connection with .NET 9 + RabbitMQ.Client 7.x
- [ ] Document API changes affecting RawRabbit

**Key Areas to Investigate**:
```csharp
// Areas likely affected:
- ConnectionFactory API changes
- IModel → IChannel renaming (in 7.x)
- Async/await patterns
- Consumer interface changes
- Connection recovery mechanisms
- Event handling patterns
```

**Test Script** (create in `/home/laird/src/EYP/RawRabbit/tests/rabbitmq-client-7-test.cs`):
```csharp
using RabbitMQ.Client;
using System;

// Test basic RabbitMQ.Client 7.x functionality
var factory = new ConnectionFactory { HostName = "localhost" };
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

channel.QueueDeclare(queue: "test_queue",
                     durable: false,
                     exclusive: false,
                     autoDelete: false,
                     arguments: null);

Console.WriteLine("RabbitMQ.Client 7.x test successful!");
```

**Deliverable**: Document with breaking changes analysis

---

### 3. Verify ZeroFormatter .NET 9 Compatibility
**Status**: ❌ NOT STARTED
**Owner**: Migration Architect
**Timeline**: Day 2

**Research Steps**:
- [ ] Check [ZeroFormatter repository](https://github.com/neuecc/ZeroFormatter) status
- [ ] Verify last commit date and .NET support
- [ ] Search for active forks supporting .NET 9
- [ ] Test ZeroFormatter with .NET 9 sample project
- [ ] Evaluate alternatives: MemoryPack, MessagePack, Protobuf

**Test Command**:
```bash
# Try to install ZeroFormatter in .NET 9 project
dotnet new console -n ZeroFormatterTest -f net9.0
cd ZeroFormatterTest
dotnet add package ZeroFormatter
dotnet build  # Will this work?
```

**Expected Result**: Likely FAILS (archived 2018, no .NET Core 3.0+ support)

**Decision Matrix**:
| Result | Action |
|--------|--------|
| ✅ Works with .NET 9 | Keep, update version |
| ⚠️ Works with fork | Evaluate fork maintenance |
| ❌ Doesn't work | Deprecate, create ADR 0008 |

**Deliverable**: ZeroFormatter compatibility report + deprecation recommendation

---

### 4. Check Ninject .NET 9 Support
**Status**: ❌ NOT STARTED
**Owner**: Migration Architect
**Timeline**: Day 2

**Research Steps**:
- [ ] Check [Ninject repository](https://github.com/ninject/Ninject) latest release
- [ ] Review issue tracker for .NET 9 support
- [ ] Test Ninject with .NET 9 project
- [ ] Check NuGet package for recent updates

**Test Command**:
```bash
# Test Ninject with .NET 9
dotnet new console -n NinjectTest -f net9.0
cd NinjectTest
dotnet add package Ninject
dotnet build
```

**Last Known Status**:
- Last release: 2017 (v3.3.4)
- .NET Standard 2.0 support (should work with .NET 9)
- BUT: Community maintenance unclear

**Decision Criteria**:
- If actively maintained → Keep
- If works but unmaintained → Deprecate with warning
- If doesn't build → Deprecate immediately

**Migration Guide Required** (if deprecated):
```markdown
# Migrating from Ninject to Microsoft.Extensions.DependencyInjection

## Before (Ninject)
var kernel = new StandardKernel();
kernel.Bind<IBusClient>().To<BusClient>();

## After (MS.DI)
var services = new ServiceCollection();
services.AddRawRabbit();
```

**Deliverable**: Ninject compatibility report + deprecation decision

---

## Priority 2: Planning 📋

### 5. Update PLAN.md with Corrected Dependency Order
**Status**: ❌ NOT STARTED
**Owner**: Migration Architect
**Timeline**: Day 3
**Depends On**: Tasks 1-4 completed

**Changes Required**:

#### A. Stage 3 Revision
Replace current text with corrected order:

```markdown
### Stage 3: Core & Tier 1 (Week 3-4.5)

#### 3.1 RawRabbit Core (Week 3)
**Priority**: CRITICAL - Foundation for all other components

**Dependencies**:
- RabbitMQ.Client 5.0.1 → 7.x
- Newtonsoft.Json 10.0.1 → 13.x

**Tasks**:
1. Update .csproj to .NET 9
2. Migrate RabbitMQ.Client to 7.x (see ADR 0011)
3. Update Newtonsoft.Json to 13.x
4. Refactor deprecated APIs
5. Update SimpleDependencyInjection
6. Fix middleware pipeline

#### 3.2 Tier 1 Operations (Week 3.5-4) - PARALLEL
**Dependencies**: Core only

Migrate in parallel:
1. Operations.Publish
2. Operations.Subscribe
3. Operations.Request
4. Operations.Respond
5. Operations.Get
6. Operations.Tools

#### 3.3 Tier 1 Enrichers (Week 3.5-4) - PARALLEL
**Dependencies**: Core only

Migrate in parallel:
1. Enrichers.MessageContext
2. Enrichers.Attributes
3. Enrichers.GlobalExecutionId
4. Enrichers.QueueSuffix
5. Enrichers.Polly (+ Polly package update)
6. Enrichers.RetryLater
```

#### B. Stage 4 Revision
```markdown
### Stage 4: Tier 2-3 Operations (Week 4.5-6.5)

#### 4.1 Tier 2 Composite Operations (Week 4.5-5.5)
**Dependencies**: Tier 1 operations

1. **MessageContext.Subscribe**
   - Depends on: Operations.Subscribe

2. **MessageContext.Respond**
   - Depends on: Operations.Respond

3. **Operations.StateMachine**
   - Depends on: Operations.Subscribe
   - External: Stateless package update

#### 4.2 Tier 3 Complex Integration (Week 5-6.5)

1. **Operations.MessageSequence** ⚠️ COMPLEX
   **Dependencies** (5 projects):
   - GlobalExecutionId (Tier 1)
   - Operations.Publish (Tier 1)
   - MessageContext.Subscribe (Tier 2)
   - Operations.StateMachine (Tier 2)
   - Operations.Tools (Tier 1)

   **Note**: MUST migrate AFTER all dependencies complete.
   Allocate extra testing time due to complexity.

2. **Enrichers.HttpContext** (Week 5.5-6)
   - ASP.NET Classic → ASP.NET Core migration
   - See ADR 0010

3. **Serialization Enrichers** (Week 5.5-6)
   - Protobuf
   - MessagePack
   - ZeroFormatter (see ADR 0008 for deprecation decision)
```

**Deliverable**: Updated `/home/laird/src/EYP/RawRabbit/docs/PLAN.md` v1.1

---

### 6. Add Missing ADRs to Stage 2
**Status**: ❌ NOT STARTED
**Owner**: Migration Architect
**Timeline**: Day 3

**Update Stage 2, Section 2.1**:

Add after existing ADR 0007:

```markdown
**Deliverables**:
- `docs/adr/0003-target-framework-selection.md`
- `docs/adr/0004-dependency-update-strategy.md`
- `docs/adr/0008-zeroformatter-deprecation.md` (NEW)
- `docs/adr/0009-ninject-deprecation.md` (NEW)
- `docs/adr/0010-aspnet-core-migration.md` (NEW)
- `docs/adr/0011-rabbitmq-client-compatibility.md` (NEW)
- `docs/adr/0012-json-serializer-strategy.md` (NEW)
- `docs/architecture-design.md`
```

**ADR Templates to Create**:

Create these files with template content:

1. `/home/laird/src/EYP/RawRabbit/docs/adr/0008-zeroformatter-deprecation.md`
2. `/home/laird/src/EYP/RawRabbit/docs/adr/0009-ninject-deprecation.md`
3. `/home/laird/src/EYP/RawRabbit/docs/adr/0010-aspnet-core-migration.md`
4. `/home/laird/src/EYP/RawRabbit/docs/adr/0011-rabbitmq-client-compatibility.md`
5. `/home/laird/src/EYP/RawRabbit/docs/adr/0012-json-serializer-strategy.md`

**Deliverable**: 5 ADR template files created

---

### 7. Setup Docker RabbitMQ Test Environment
**Status**: ❌ NOT STARTED
**Owner**: DevOps Engineer
**Timeline**: Day 4

**Create**: `/home/laird/src/EYP/RawRabbit/docker/rabbitmq/docker-compose.yml`

```yaml
version: '3.8'

services:
  rabbitmq-3.12:
    image: rabbitmq:3.12-management-alpine
    container_name: rawrabbit-test-rabbitmq-3.12
    ports:
      - "5672:5672"   # AMQP
      - "15672:15672" # Management UI
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5

  rabbitmq-3.11:
    image: rabbitmq:3.11-management-alpine
    container_name: rawrabbit-test-rabbitmq-3.11
    ports:
      - "5673:5672"   # AMQP (different port)
      - "15673:15672" # Management UI
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5
```

**Test Commands**:
```bash
cd /home/laird/src/EYP/RawRabbit/docker/rabbitmq
docker-compose up -d

# Verify RabbitMQ 3.12 is running
curl http://localhost:15672
docker logs rawrabbit-test-rabbitmq-3.12

# Verify RabbitMQ 3.11 is running
curl http://localhost:15673
docker logs rawrabbit-test-rabbitmq-3.11

# Access management UI
# RabbitMQ 3.12: http://localhost:15672 (guest/guest)
# RabbitMQ 3.11: http://localhost:15673 (guest/guest)
```

**README**: Create `/home/laird/src/EYP/RawRabbit/docker/rabbitmq/README.md`
```markdown
# RabbitMQ Test Environment

## Quick Start
docker-compose up -d

## Stop
docker-compose down

## Clean Up
docker-compose down -v  # Removes volumes too

## Logs
docker logs rawrabbit-test-rabbitmq-3.12
docker logs rawrabbit-test-rabbitmq-3.11
```

**Deliverable**: Working Docker Compose environment with both RabbitMQ versions

---

### 8. Extend Timeline to 12-14 Weeks
**Status**: ❌ NOT STARTED
**Owner**: Migration Architect
**Timeline**: Day 3

**Update**: `/home/laird/src/EYP/RawRabbit/docs/PLAN.md`

Change:
```markdown
**Duration**: 10-12 weeks
```

To:
```markdown
**Duration**: 12-14 weeks
**Rationale**: Added buffer for:
- RabbitMQ.Client 7.x compatibility work (+0.5 weeks)
- Additional ADR documentation (+0.5 weeks)
- MessageSequence complex dependency testing (+0.5 weeks)
- Deprecation handling (ZeroFormatter/Ninject) (+0.5 weeks)
```

Update Timeline Summary table:
```markdown
| Stage | Duration | Key Milestone |
|-------|----------|---------------|
| 1. Foundation | Week 1-2 | Baseline established |
| 2. Architecture | Week 2-3.5 | Design approved (+0.5 week) |
| 3. Core Migration | Week 3.5-5 | Core library on .NET 9 |
| 4. Operations/Enrichers | Week 5-8 | All packages migrated (+1 week) |
| 5. DI & Samples | Week 8-9 | Examples working |
| 6. Integration Testing | Week 9-10.5 | System validated (+0.5 week) |
| 7. Documentation | Week 10.5-11.5 | Docs complete |
| 8. Deployment | Week 11.5-14 | Production release (+0.5 week) |

**Total Duration**: 12-14 weeks
```

**Deliverable**: Updated timeline in PLAN.md

---

### 9. Create Visual Dependency Graph
**Status**: ✅ COMPLETED
**Location**: `/home/laird/src/EYP/RawRabbit/docs/dependency-graph.mermaid`

**Additional Task**: Render to SVG for documentation

```bash
# Install mermaid-cli if needed
npm install -g @mermaid-js/mermaid-cli

# Render to SVG
cd /home/laird/src/EYP/RawRabbit/docs
mmdc -i dependency-graph.mermaid -o dependency-graph.svg -b transparent
```

**Deliverable**: ✅ Already created, optionally render to SVG

---

### 10. Create BREAKING-CHANGES.md Template
**Status**: ❌ NOT STARTED
**Owner**: Documentation Specialist
**Timeline**: Day 5

**Create**: `/home/laird/src/EYP/RawRabbit/docs/BREAKING-CHANGES.md`

```markdown
# Breaking Changes in RawRabbit 3.0 (.NET 9)

**Last Updated**: [Date]
**Migration Guide**: See `MIGRATION-GUIDE.md`

---

## Summary

RawRabbit 3.0 is a **major version upgrade** with significant breaking changes.
All users MUST migrate to .NET 9 or later.

---

## Framework Requirements

### Before (v2.x)
- .NET Standard 1.5+
- .NET Framework 4.5.1+

### After (v3.0)
- ✅ .NET 9.0+
- ❌ .NET Standard (removed)
- ❌ .NET Framework (removed)

---

## Dependency Changes

### RabbitMQ.Client
- **Before**: 5.0.1
- **After**: 7.x
- **Impact**: [To be documented after research]

### Newtonsoft.Json
- **Before**: 10.0.1
- **After**: 13.x
- **Impact**: Minimal (backward compatible)

---

## Removed Features

### ZeroFormatter Enricher
- **Status**: [DEPRECATED / REMOVED]
- **Reason**: [To be filled after decision]
- **Migration Path**: Use MessagePack or Protobuf

### Ninject DI Adapter
- **Status**: [DEPRECATED / KEPT]
- **Reason**: [To be filled after decision]
- **Migration Path**: Use Microsoft.Extensions.DependencyInjection or Autofac

### ASP.NET Classic Support
- **Status**: REMOVED
- **Reason**: .NET Framework 4.5.1 no longer supported
- **Migration Path**: Migrate to ASP.NET Core 9.x

---

## API Changes

### [To be filled during migration]

---

## Migration Checklist

- [ ] Upgrade project to .NET 9
- [ ] Update RawRabbit packages to 3.0
- [ ] Update RabbitMQ.Client to 7.x
- [ ] Replace ZeroFormatter if used
- [ ] Replace Ninject if used
- [ ] Migrate ASP.NET to ASP.NET Core
- [ ] Run tests
- [ ] Review deprecation warnings

---

## Support

- Migration Guide: `docs/MIGRATION-GUIDE.md`
- Troubleshooting: `docs/TROUBLESHOOTING.md`
- ADRs: `docs/adr/`
```

**Deliverable**: BREAKING-CHANGES.md template created

---

## Progress Tracking

| Task | Status | Owner | Due |
|------|--------|-------|-----|
| 1. Install .NET 9 SDK | ❌ | DevOps | Day 1 |
| 2. RabbitMQ.Client research | ❌ | Architect | Day 1-2 |
| 3. ZeroFormatter verification | ❌ | Architect | Day 2 |
| 4. Ninject verification | ❌ | Architect | Day 2 |
| 5. Update PLAN.md dependency order | ❌ | Architect | Day 3 |
| 6. Add missing ADRs | ❌ | Architect | Day 3 |
| 7. Docker RabbitMQ setup | ❌ | DevOps | Day 4 |
| 8. Extend timeline | ❌ | Architect | Day 3 |
| 9. Visual dependency graph | ✅ | Architect | DONE |
| 10. BREAKING-CHANGES.md | ❌ | Docs | Day 5 |

---

## Completion Criteria

Before proceeding to Stage 1, ALL of the following must be TRUE:

- [ ] .NET 9 SDK installed and `dotnet --version` shows 9.0.x
- [ ] RabbitMQ.Client 7.x breaking changes documented
- [ ] ZeroFormatter decision made (keep/deprecate/replace)
- [ ] Ninject decision made (keep/deprecate)
- [ ] PLAN.md updated with corrected dependency order
- [ ] 5 new ADR templates created
- [ ] Docker RabbitMQ environment tested and working
- [ ] Timeline extended to 12-14 weeks in PLAN.md
- [ ] BREAKING-CHANGES.md template created
- [ ] Team review completed and approved

**Estimated Total Time**: 5 working days

---

**Document Status**: ACTIVE
**Next Review**: After all tasks completed
