# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

RawRabbit is a modern .NET client library for RabbitMQ communication. This is version 2.x, which uses a **middleware-oriented architecture** with a **pipe-based execution model** for message handling. The library targets both .NET Standard 1.5 and .NET Framework 4.5.1.

**Core Dependencies:**
- RabbitMQ.Client 5.0.1
- Newtonsoft.Json 10.0.1

## Build & Test Commands

**Build entire solution:**
```bash
dotnet build RawRabbit.sln
```

**Build specific project:**
```bash
dotnet build src/RawRabbit/RawRabbit.csproj
dotnet build src/RawRabbit.Operations.Publish/RawRabbit.Operations.Publish.csproj
```

**Run all tests:**
```bash
dotnet test test/RawRabbit.Tests/RawRabbit.Tests.csproj
dotnet test test/RawRabbit.IntegrationTests/RawRabbit.IntegrationTests.csproj
```

**Run integration tests:**
```bash
dotnet test test/RawRabbit.IntegrationTests/RawRabbit.IntegrationTests.csproj
```

**Note:** Integration tests require a running RabbitMQ instance (default localhost:5672).

**Create NuGet packages:**
```bash
dotnet pack src/RawRabbit/RawRabbit.csproj -c Release
```

## Architecture

### Modular Package Structure

RawRabbit is split into **25 separate projects** organized into three categories:

**Core Library:** `src/RawRabbit/`
- Contains the base client, pipe infrastructure, channel management, and configuration

**Operations (message patterns):** `src/RawRabbit.Operations.*/`
- `Publish` - Fire-and-forget publishing
- `Subscribe` - Message consumption
- `Request` - RPC request (uses direct reply-to)
- `Respond` - RPC response handler
- `Get` - Single message retrieval
- `MessageSequence` - Choreographed message flows
- `StateMachine` - Stateful message handling
- `Tools` - Utility operations

**Enrichers (plugins):** `src/RawRabbit.Enrichers.*/`
- `Attributes` - Attribute-based configuration
- `MessageContext` - Message metadata handling
- `Polly` - Resilience policies (retry, circuit breaker)
- `GlobalExecutionId` - Distributed tracing
- `QueueSuffix` - Dynamic queue naming
- `HttpContext` - ASP.NET integration
- `RetryLater` - Delayed retry mechanism
- `Protobuf`, `MessagePack`, `ZeroFormatter` - Alternative serialization

**Dependency Injection Adapters:** `src/RawRabbit.DependencyInjection.*/`
- `ServiceCollection` (Microsoft.Extensions.DependencyInjection)
- `Autofac`
- `Ninject`

### Pipe and Middleware Architecture

RawRabbit 2.x uses a **middleware pipeline pattern** inspired by ASP.NET Core. This is the most important architectural concept to understand:

**Key Types:**
- `IPipeContext` - Request-scoped property bag that flows through the pipeline
- `IPipeBuilder` - Fluent API for constructing middleware chains
- `Middleware` - Base class for all middleware components
- `StagedMiddleware` - Middleware that executes at specific stages

**Middleware Chain Execution:**
1. Each operation (Publish, Subscribe, Request, etc.) builds a middleware pipeline
2. Middleware components are added via `IPipeBuilder.Use<TMiddleware>()`
3. The pipeline executes sequentially, with each middleware calling `next()`
4. Middleware can short-circuit by not calling next, or modify the `IPipeContext`

**Example pipeline for PublishAsync:**
```
1. [PublishConfigurationMiddleware] - Set publish configuration
2. [ExchangeDeclareMiddleware] - Ensure exchange exists
3. [BodySerializationMiddleware] - Serialize message to bytes
4. [BasicPublishMiddleware] - Actual RabbitMQ publish call
```

**Modifying Pipelines:**
Operations can be customized per-call using context configuration:

```csharp
await client.PublishAsync(message, ctx => ctx
    .UsePublishConfiguration(cfg => cfg
        .OnExchange("custom_exchange")
        .WithRoutingKey("custom_key")));
```

### Client Factory and Instantiation

**Factory Pattern:**
- `RawRabbitFactory.CreateSingleton()` - Primary entry point
- Returns `Disposable.BusClient` which wraps an `InstanceFactory`
- `InstanceFactory` creates operation-specific clients on-demand

**Dependency Injection:**
The library uses its own simple IoC container (`SimpleDependencyInjection`) by default, but supports external DI containers:

```csharp
var client = RawRabbitFactory.CreateSingleton(new RawRabbitOptions
{
    ClientConfiguration = config,
    DependencyInjection = ioc => ioc
        .AddSingleton<IChannelFactory, CustomChannelFactory>(),
    Plugins = p => p
        .UseProtobuf()
        .UsePolly(/* resilience policies */)
});
```

### Message Acknowledgement Model

RawRabbit treats acknowledgements as **first-class return types** from message handlers:

**Return Types:**
- `Ack()` - Acknowledge successful processing
- `Nack(requeue: bool)` - Negative acknowledgement
- `Reject(requeue: bool)` - Reject message
- `Retry.In(TimeSpan)` - Delayed retry via dead-letter exchange

**Example:**
```csharp
await client.SubscribeAsync<MyMessage>(msg => {
    if (CannotProcess(msg))
        return new Nack(requeue: true);

    Process(msg);
    return new Ack();
});
```

### Configuration System

**RawRabbitConfiguration** contains:
- Connection settings (hostnames, credentials, vhost, port)
- Timeouts (request, publish confirm, recovery interval)
- Default topology (exchange type, durability, auto-delete)
- Recovery and SSL options

**Performance Configurations:**
```csharp
config.AsHighPerformance(); // Disables persistent delivery, uses Direct exchange
config.AsLegacy(); // Compatible with 1.x routing (no global ID)
```

**Configuration can be loaded from JSON:**
```csharp
var config = new ConfigurationBuilder()
    .AddJsonFile("rawrabbit.json")
    .Build()
    .Get<RawRabbitConfiguration>();
```

## Working with the Codebase

### Documentation Requirements

**IMPORTANT:** All work must be documented in two ways:

**1. Work History (`docs/HISTORY.md`):**
- Maintain a chronological record of all work done on the codebase
- Include date, description of changes, and rationale
- Update this file whenever you make significant changes (features, refactoring, bug fixes)
- Format:
  ```markdown
  ## YYYY-MM-DD - Brief Description
  - What was changed
  - Why it was changed
  - Impact on the codebase
  ```

**2. Architecture Decision Records (`docs/adr/`):**
- Record all significant architecture decisions as ADR files
- Use numbered format: `docs/adr/NNNN-title-kebab-case.md`
- Create an ADR when:
  - Adding new operations or enrichers
  - Changing middleware architecture or execution flow
  - Modifying DI patterns or factory behavior
  - Changing message handling patterns
  - Updating topology defaults or configuration structure
  - Making breaking API changes
- ADR Template:
  ```markdown
  # ADR-NNNN: [Decision Title]

  ## Status
  [Proposed | Accepted | Deprecated | Superseded]

  ## Context
  [What is the issue/requirement that motivates this decision?]

  ## Decision
  [What is the change we're making?]

  ## Consequences
  ### Positive
  - [Benefits of this decision]

  ### Negative
  - [Drawbacks or costs]

  ### Risks
  - [Potential risks]

  ## Alternatives Considered
  - [Other options that were evaluated and why they were rejected]

  ## References
  - [Links to related issues, PRs, documentation]
  ```

### Adding a New Operation

1. **Create ADR** documenting the decision to add this operation (`docs/adr/NNNN-add-operation-name.md`)
2. Create project in `src/RawRabbit.Operations.YourOperation/`
3. Reference `RawRabbit` core project
4. Create middleware classes inheriting from `Middleware`
5. Create an extension method on `IBusClient` that:
   - Builds the pipe with required middleware
   - Invokes the pipe with a new `PipeContext`
6. Add to solution and update NuGet dependencies
7. **Update `docs/HISTORY.md`** with details of the new operation

### Adding a New Enricher/Plugin

1. **Create ADR** documenting the enricher design (`docs/adr/NNNN-add-enricher-name.md`)
2. Create project in `src/RawRabbit.Enrichers.YourEnricher/`
3. Create middleware that modifies `IPipeContext` properties
4. Create extension method on plugin configuration:
   ```csharp
   public static IClientBuilder UseYourEnricher(this IClientBuilder builder)
   {
       builder.Register(pipe => pipe.Use<YourMiddleware>());
       return builder;
   }
   ```
5. Users activate via: `Plugins = p => p.UseYourEnricher()`
6. **Update `docs/HISTORY.md`** with details of the new enricher

### Middleware Development Patterns

**Reading from context:**
```csharp
var config = context.Get<PublishConfiguration>();
var message = context.GetMessage();
```

**Writing to context:**
```csharp
context.Properties.TryAdd("key", value);
context.Properties.AddOrReplace("key", value);  // Overwrite if exists
```

**Staged middleware** (executes at marked stages):
```csharp
public class MyMiddleware : StagedMiddleware
{
    public override string StageMarker => "MyStage";

    public override Task InvokeAsync(IPipeContext context, CancellationToken token)
    {
        // Execute at "MyStage" marker
        return Next.InvokeAsync(context, token);
    }
}
```

### Testing Patterns

**Unit Tests:** Located in `test/RawRabbit.Tests/`
- Test middleware in isolation
- Mock `IPipeContext` and verify behavior

**Integration Tests:** Located in `test/RawRabbit.IntegrationTests/`
- Require running RabbitMQ instance
- Use `RawRabbitFactory` from test namespace
- Test full end-to-end message flows

**Performance Tests:** Located in `test/RawRabbit.PerformanceTest/`

## Key Files and Locations

**Core abstractions:**
- `src/RawRabbit/IBusClient.cs` - Main client interface
- `src/RawRabbit/Pipe/IPipeContext.cs` - Context contract
- `src/RawRabbit/Pipe/PipeBuilder.cs` - Middleware chain builder

**Factory:**
- `src/RawRabbit/Instantiation/RawRabbitFactory.cs` - Client factory entry point

**Configuration:**
- `src/RawRabbit/Configuration/RawRabbitConfiguration.cs` - Configuration model

**Channel Management:**
- `src/RawRabbit/Channel/` - Channel pooling and lifecycle

**Documentation:**
- `docs/` - Sphinx-based documentation source
- `docs/getting-started/` - User guides for common scenarios
- `docs/HISTORY.md` - **Required:** Chronological record of all work done
- `docs/adr/` - **Required:** Architecture Decision Records (ADRs)

## Common Patterns

### Request/Response (RPC)

Uses RabbitMQ's **direct reply-to** feature for performance:
```csharp
// Responder
client.RespondAsync<Request, Response>(async req => new Response());

// Requester
var response = await client.RequestAsync<Request, Response>(new Request());
```

### Publish/Subscribe with Custom Topology

```csharp
await client.SubscribeAsync<MyMessage>(msg => {
    // Handle message
    return Task.FromResult(new Ack());
}, ctx => ctx
    .UseSubscribeConfiguration(cfg => cfg
        .FromDeclaredQueue(q => q
            .WithName("my_queue")
            .WithDurability(true)
            .WithArgument(QueueArgument.DeadLetterExchange, "dlx"))
        .OnDeclaredExchange(e => e
            .WithName("my_exchange")
            .WithType(ExchangeType.Topic))));
```

### Plugin Composition

```csharp
var client = RawRabbitFactory.CreateSingleton(new RawRabbitOptions {
    Plugins = p => p
        .UseGlobalExecutionId()
        .UseMessageContext<MyContext>()
        .UsePolly(c => c.UsePolicy(retryPolicy, PolicyKeys.QueueDeclare))
});
```

## Multi-Targeting

Projects target both `netstandard1.5` and `net451`. When adding framework-specific code:

```csharp
#if NET451
    // .NET Framework specific
#else
    // .NET Standard specific
#endif
```

Or use conditional compilation in `.csproj`:
```xml
<ItemGroup Condition=" '$(TargetFramework)' == 'net451' ">
  <Reference Include="System" />
</ItemGroup>
```

---

# .NET 9 Upgrade Project

## Upgrade Project Overview

**Objective**: Migrate RawRabbit from .NET Standard 1.5 / .NET Framework 4.5.1 to .NET 9 with security improvements and comprehensive ADR documentation.

**Branch**: `upgrade`

**Key Requirements:**
- All work must be consistent with architectural decisions recorded in ADRs
- All functionality must be confirmed by testing
- Save all test reports in `docs/test/`
- Maintain `docs/HISTORY.md` with all work done
- Record all architecture decisions in `docs/adr/` as ADR files

## Agent Coordination Setup

### System Initialization

**REQUIRED FIRST STEP** - Initialize Hive Mind before starting upgrade work:

```bash
# Initialize Hive Mind with SQLite database
npx claude-flow@alpha hive-mind init

# Verify initialization
ls -la .hive-mind/
# Should show: config.json, hive.db
```

### Agent Configuration

Agent definitions are in `.claude-flow/config.json`:
- **Max 6 concurrent agents**
- **Mesh topology** (peer-to-peer coordination with fault tolerance)
- **Session ID**: `dotnet9-upgrade`

**Available Agents:**
1. **Migration Architect** (`migration-planner`) - Strategic planning, ADRs
2. **Security Specialist** (`security-manager`) - Security audits, validation
3. **.NET Modernizer** (`backend-dev`) - Code transformation to .NET 9
4. **QA Engineer** (`tester`) - Testing & quality assurance (save reports to `docs/test/`)
5. **Documentation Specialist** (`researcher`) - ADR management, HISTORY.md
6. **DevOps Engineer** (`cicd-engineer`) - Build, deployment, automation

## 5-Phase Migration Process

### Phase 1: Discovery & Planning (Week 1-2)

**Objective**: Understand current state and plan migration strategy

**Agents**: Migration Architect, Security Specialist, Documentation Specialist

**Key Activities:**
- Analyze current .NET framework version and dependencies
- Security baseline audit
- Set up ADR structure at `docs/adr/`
- Identify .NET 9 compatibility issues
- Create migration roadmap

**Deliverables:**
- `docs/adr/0001-migration-strategy.md`
- `docs/adr/0002-security-architecture.md`
- Security baseline audit report
- Dependency upgrade matrix
- Migration roadmap

### Phase 2: Architecture & Design (Week 2-3)

**Objective**: Design target architecture and validate security

**Agents**: Migration Architect, Security Specialist, Documentation Specialist

**Key Activities:**
- Design .NET 9 target architecture
- Define component migration order
- Security review of proposed architecture
- Document all decisions as ADRs

**Deliverables:**
- `docs/adr/0003-framework-architecture.md`
- `docs/adr/0004-security-review-results.md`
- Component migration plan
- Updated security checklist

### Phase 3: Implementation (Week 3-8)

**Objective**: Migrate components to .NET 9 with continuous testing

**Agents**: .NET Modernizer, QA Engineer, Security Specialist

**Component Migration Order** (based on dependencies):
1. RawRabbit (core)
2. RawRabbit.Operations.* (all operations)
3. RawRabbit.Enrichers.* (all enrichers)
4. RawRabbit.DependencyInjection.* (DI adapters)
5. Sample applications

**Per-Component Workflow:**
1. Modernizer upgrades .csproj to .NET 9
2. Modernizer updates NuGet packages
3. Modernizer refactors deprecated APIs
4. Tester writes/runs tests (save reports to `docs/test/`)
5. Security Specialist reviews changes
6. Documentation Specialist updates `docs/HISTORY.md`
7. Create component-specific ADR if architecture changes

**Deliverables:**
- Modernized components targeting .NET 9
- Test suites with 90%+ coverage (reports in `docs/test/`)
- Security review reports
- Component-specific ADRs (as needed)
- Updated `docs/HISTORY.md`

### Phase 4: Integration & Testing (Week 8-10)

**Objective**: Validate complete system and prepare for deployment

**Agents**: QA Engineer, Security Specialist, DevOps Engineer

**Key Activities:**
- Full integration test suite (save reports to `docs/test/`)
- Performance benchmarking (before/after comparison)
- Security penetration testing
- Build complete application with .NET 9
- Validate CI/CD pipelines
- Deployment preparation

**Deliverables:**
- Integration test results (in `docs/test/`)
- Performance benchmark report
- Security audit report
- Staging deployment
- Build artifacts

### Phase 5: Documentation & Deployment (Week 10-12)

**Objective**: Finalize documentation and deploy

**Agents**: Documentation Specialist, DevOps Engineer, Migration Architect

**Key Activities:**
- Finalize all ADR records
- Complete `docs/HISTORY.md` with all work done
- Create deployment runbook
- Generate comprehensive changelog
- Production deployment
- Post-migration validation

**Deliverables:**
- Complete ADR repository at `docs/adr/`
- Final `docs/HISTORY.md`
- Deployment runbook
- Migration guide for users
- Production deployment
- Post-migration report

## Security Review Checkpoints

### Checkpoint 1: Pre-Migration Baseline (Phase 1)
- Current vulnerability scan
- Dependency security audit
- Authentication/authorization review
- **Output**: Security Baseline Report + ADR-0002

### Checkpoint 2: Component Security Review (Phase 3)
- Code review of each component
- API security validation
- Dependency update verification
- **Output**: Per-component security reports

### Checkpoint 3: Integration Security Test (Phase 4)
- Full application security scan
- Penetration testing
- Authentication/authorization flow testing
- **Output**: Integration Security Report

### Checkpoint 4: Pre-Production Security Audit (Phase 5)
- Final vulnerability scan
- Security configuration review
- Deployment security validation
- **Output**: Production Security Clearance

## Agent Coordination Protocols

### Memory Namespaces

Agents coordinate via structured memory:
- `migration/discovery` - Initial analysis
- `migration/architecture` - Design decisions
- `migration/components/{name}` - Per-component work
- `security/baseline` - Security baseline
- `security/reviews/{component}` - Security reviews
- `docs/adr` - ADR records
- `testing/results` - Test results
- `devops/pipelines` - CI/CD configurations

### Communication Hooks

**Before Starting Work:**
```bash
npx claude-flow@alpha hooks pre-task --description "[task]"
npx claude-flow@alpha hooks session-restore --session-id "dotnet9-upgrade"
```

**During Work:**
```bash
npx claude-flow@alpha hooks post-edit --file "[file]" --memory-key "[namespace/key]"
npx claude-flow@alpha hooks notify --message "[status update]"
```

**After Completing Work:**
```bash
npx claude-flow@alpha hooks post-task --task-id "[task-id]"
npx claude-flow@alpha hooks session-end --export-metrics true
```

## Quick Start Commands

### Initialize Upgrade Project
```bash
# Ensure Hive Mind is initialized
npx claude-flow@alpha hive-mind init

# Start Phase 1 Discovery
npx claude-flow@alpha swarm "Start .NET 9 migration discovery phase"

# Check agent status
npx claude-flow@alpha agent list

# View progress
npx claude-flow@alpha memory search "migration"
```

### Component Migration
```bash
# Migrate specific component
npx claude-flow@alpha swarm "Migrate RawRabbit core to .NET 9"

# Check test results
npx claude-flow@alpha memory query "testing/results/RawRabbit"

# View test reports
ls docs/test/
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

## Success Criteria

Project considered complete when:
- ✅ All components migrated to .NET 9
- ✅ All tests passing with 90%+ coverage (reports in `docs/test/`)
- ✅ Security audit passed (all 4 checkpoints)
- ✅ All ADRs documented in `docs/adr/`
- ✅ `docs/HISTORY.md` complete with all work done
- ✅ Production deployment successful
- ✅ No critical bugs in first 2 weeks

## Testing Requirements

**All functionality must be confirmed by testing:**
- Write tests before or alongside implementation (TDD encouraged)
- Achieve 90%+ code coverage
- Save all test reports to `docs/test/`
- Include unit tests, integration tests, and performance benchmarks
- Document test results in `docs/HISTORY.md`

## Documentation Requirements Reminder

**Two required documentation practices for upgrade:**

1. **`docs/HISTORY.md`** - Chronological record of ALL work
   - Update after each significant change
   - Include date, description, rationale, and impact

2. **`docs/adr/`** - Architecture Decision Records
   - Create ADR for all architectural changes
   - Number sequentially (0001, 0002, etc.)
   - Use provided template
   - Reference ADRs in code comments

**All software must be consistent with ADR decisions.**
