# Polly 5.x → 8.x Migration Guide for RawRabbit

**Status**: Implementation Guide (Code changes NOT yet applied)
**Estimated Effort**: 3-5 days
**Priority**: HIGH (but after RabbitMQ.Client)
**Affected Files**: ~8 files in RawRabbit.Enrichers.Polly

---

## Executive Summary

Polly 8.0 introduced a **complete API redesign** in 2023. The concept of "Policies" was replaced with "Resilience Pipelines", requiring significant code updates for any consumer using custom Polly policies with RawRabbit.

**Good News**: The RawRabbit.Enrichers.Polly project is relatively small (~15 files, most are middleware wrappers). The core plugin architecture (`PolicyMiddleware`) is a simple pass-through that lets users inject custom policies.

**Challenge**: Users with custom Polly 5.x policies will need to migrate their code to Polly 8.x API.

---

## API Mapping Table

| Polly 5.x | Polly 8.x | Change Type | Notes |
|-----------|-----------|-------------|-------|
| `Policy` class | `ResiliencePipeline` class | BREAKING | Complete redesign |
| `Policy.Handle<T>()` | `ResiliencePipelineBuilder.AddRetry()` | BREAKING | Builder pattern |
| `Policy.RetryAsync()` | `.AddRetry(new RetryStrategyOptions)` | BREAKING | Options-based |
| `Policy.WaitAndRetryAsync()` | `.AddRetry()` with backoff | BREAKING | Integrated into retry |
| `Policy.CircuitBreakerAsync()` | `.AddCircuitBreaker()` | BREAKING | Similar concept |
| `Policy.TimeoutAsync()` | `.AddTimeout()` | BREAKING | Similar concept |
| `PolicyRegistry` | `ResiliencePipelineRegistry` | BREAKING | New registry type |
| `Context` class | `ResilienceContext` class | BREAKING | New context type |
| `PolicyBuilder` | `ResiliencePipelineBuilder` | BREAKING | New builder |
| `ExecuteAsync()` | `ExecuteAsync()` | Compatible | Method name unchanged |

---

## Current RawRabbit.Enrichers.Polly Architecture

### Plugin Design (Good News!)

RawRabbit's Polly enricher uses a **plugin architecture** that:
1. **Does NOT embed specific Polly policies** in RawRabbit code
2. **Allows users to inject custom policies** via `PolicyAction` delegate
3. **Wraps middleware** to apply policies at integration points

**Example Current Usage** (Polly 5.x):
```csharp
// User code (RawRabbit 2.x + Polly 5.x)
services.AddRawRabbit(cfg => cfg
    .UsePolly(ctx =>
    {
        // User provides Polly 5.x policies
        ctx.Properties.Add(PolicyKeys.BasicPublish, Policy
            .Handle<BrokerUnreachableException>()
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
    })
);
```

**This means**:
- ✅ RawRabbit enricher code is mostly **glue code** (small surface area)
- ✅ Users inject Polly policies (they own the Polly API usage)
- ⚠️ Users will need to update THEIR code to Polly 8.x
- ⚠️ RawRabbit needs to update `PolicyKeys` and context passing

---

## Affected Files

### Category 1: Core Plugin (3 files) - HIGH PRIORITY

**Files**:
1. `src/RawRabbit.Enrichers.Polly/PollyPlugin.cs` - Plugin registration
2. `src/RawRabbit.Enrichers.Polly/Middleware/PolicyMiddleware.cs` - Core middleware
3. `src/RawRabbit.Enrichers.Polly/PipeContextExtensions.cs` - Context helpers

**Current Design**:
- `PolicyMiddleware` invokes user-provided `PolicyAction`
- Users store Polly policies in `IPipeContext` using `PolicyKeys`
- Middleware wrappers retrieve and execute policies

**Required Changes**: MINIMAL
- Update `PolicyKeys` documentation (Polly 5.x → 8.x examples)
- Validate `IPipeContext` can store `ResiliencePipeline` objects
- Update XML docs and comments

**Estimated Effort**: 1 day

---

### Category 2: Middleware Wrappers (9 files) - MEDIUM PRIORITY

**Files**:
1. `src/RawRabbit.Enrichers.Polly/Middleware/BasicPublishMiddleware.cs`
2. `src/RawRabbit.Enrichers.Polly/Middleware/ExplicitAckMiddleware.cs`
3. `src/RawRabbit.Enrichers.Polly/Middleware/QueueDeclareMiddleware.cs`
4. `src/RawRabbit.Enrichers.Polly/Middleware/ExchangeDeclareMiddleware.cs`
5. `src/RawRabbit.Enrichers.Polly/Middleware/QueueBindMiddleware.cs`
6. `src/RawRabbit.Enrichers.Polly/Middleware/ConsumerCreationMiddleware.cs`
7. `src/RawRabbit.Enrichers.Polly/Middleware/HandlerInvocationMiddleware.cs`
8. `src/RawRabbit.Enrichers.Polly/Middleware/PooledChannelMiddleware.cs`
9. `src/RawRabbit.Enrichers.Polly/Middleware/TransientChannelMiddleware.cs`

**Current Pattern** (Polly 5.x):
```csharp
// Example: BasicPublishMiddleware (conceptual)
var policy = context.Get<IAsyncPolicy>(PolicyKeys.BasicPublish);
if (policy != null)
{
    await policy.ExecuteAsync(() => base.InvokeAsync(context, token));
}
else
{
    await base.InvokeAsync(context, token);
}
```

**Updated Pattern** (Polly 8.x):
```csharp
// Updated for Polly 8.x
var pipeline = context.Get<ResiliencePipeline>(PolicyKeys.BasicPublish);
if (pipeline != null)
{
    await pipeline.ExecuteAsync(async ct => await base.InvokeAsync(context, ct), token);
}
else
{
    await base.InvokeAsync(context, token);
}
```

**Required Changes**:
- Update policy retrieval: `IAsyncPolicy` → `ResiliencePipeline`
- Update execution: `policy.ExecuteAsync()` → `pipeline.ExecuteAsync()`
- Validate async patterns (should be compatible)

**Estimated Effort**: 2 days

---

### Category 3: Services (1 file) - LOW PRIORITY

**Files**:
1. `src/RawRabbit.Enrichers.Polly/Services/ChannelFactory.cs`

**Current Design**:
- Wraps RawRabbit's `ChannelFactory` with Polly policies
- Applies connection-level policies

**Required Changes**:
- Update `ConnectionPolicies` class (if it stores Polly 5.x types)
- Update policy execution in `CreateChannelAsync()`

**Estimated Effort**: 1 day

---

### Category 4: Helper Classes (2 files) - LOW PRIORITY

**Files**:
1. `src/RawRabbit.Enrichers.Polly/PolicyKeys.cs` - Constants for policy keys
2. `src/RawRabbit.Enrichers.Polly/RetryKey.cs` - Retry-specific keys

**Required Changes**:
- Update XML documentation comments
- Add code examples showing Polly 8.x usage
- No actual code changes (just string constants)

**Estimated Effort**: 0.5 days

---

### Category 5: Tests (3 files) - HIGH PRIORITY

**Files**:
1. `test/RawRabbit.Enrichers.Polly.Tests/*`

**Required Changes**:
- Update test policies from Polly 5.x → 8.x
- Update assertions for new API
- Validate middleware behavior with Polly 8.x

**Estimated Effort**: 1 day

---

## Migration Strategy

### Phase 3A: Research & Planning (0.5 days)

**Tasks**:
1. Review Polly 8.x official migration guide
2. Identify all Polly 5.x types in RawRabbit code (`IAsyncPolicy`, `Policy`, etc.)
3. Create code example library (before/after)
4. Plan backward compatibility strategy (if any)

**Deliverables**:
- List of all Polly 5.x usages
- Migration code examples
- Test strategy

### Phase 3B: Update Core Plugin (1 day)

**Tasks**:
1. Update `PolicyMiddleware` to accept `ResiliencePipeline`
2. Update `PolicyOptions` class
3. Update XML documentation
4. Create migration code examples

**Validation**:
- Plugin registration works
- Context can store `ResiliencePipeline` objects

### Phase 3C: Update Middleware Wrappers (2 days)

**Tasks**:
1. Update all 9 middleware wrappers
2. Change `IAsyncPolicy` → `ResiliencePipeline`
3. Update execution patterns
4. Test each middleware independently

**Validation**:
- All middleware compile
- Policy execution works correctly
- Error handling preserved

### Phase 3D: Update Services (1 day)

**Tasks**:
1. Update `ChannelFactory` wrapper
2. Update `ConnectionPolicies` class
3. Test connection-level policies

**Validation**:
- Channel creation with policies works
- Connection recovery with policies works

### Phase 3E: Update Tests (1 day)

**Tasks**:
1. Migrate all test policies to Polly 8.x
2. Update test assertions
3. Add new tests for Polly 8.x features

**Validation**:
- 100% test pass rate
- Code coverage maintained

---

## Code Examples

### Example 1: Basic Retry Policy

**BEFORE (Polly 5.x)**:
```csharp
// RawRabbit 2.x consumer code
services.AddRawRabbit(cfg => cfg
    .UsePolly(ctx =>
    {
        // Polly 5.x Policy API
        ctx.Properties.Add(PolicyKeys.BasicPublish, Policy
            .Handle<BrokerUnreachableException>()
            .RetryAsync(3));
    })
);
```

**AFTER (Polly 8.x)**:
```csharp
// RawRabbit 3.0 consumer code
services.AddRawRabbit(cfg => cfg
    .UsePolly(ctx =>
    {
        // Polly 8.x ResiliencePipeline API
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                ShouldHandle = new PredicateBuilder().Handle<BrokerUnreachableException>()
            })
            .Build();

        ctx.Properties.Add(PolicyKeys.BasicPublish, pipeline);
    })
);
```

---

### Example 2: Wait and Retry with Exponential Backoff

**BEFORE (Polly 5.x)**:
```csharp
ctx.Properties.Add(PolicyKeys.BasicPublish, Policy
    .Handle<BrokerUnreachableException>()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: retryAttempt =>
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
```

**AFTER (Polly 8.x)**:
```csharp
var pipeline = new ResiliencePipelineBuilder()
    .AddRetry(new RetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        BackoffType = DelayBackoffType.Exponential,
        Delay = TimeSpan.FromSeconds(2),
        ShouldHandle = new PredicateBuilder().Handle<BrokerUnreachableException>()
    })
    .Build();

ctx.Properties.Add(PolicyKeys.BasicPublish, pipeline);
```

---

### Example 3: Circuit Breaker

**BEFORE (Polly 5.x)**:
```csharp
ctx.Properties.Add(PolicyKeys.QueueDeclare, Policy
    .Handle<BrokerUnreachableException>()
    .CircuitBreakerAsync(
        exceptionsAllowedBeforeBreaking: 3,
        durationOfBreak: TimeSpan.FromSeconds(30)));
```

**AFTER (Polly 8.x)**:
```csharp
var pipeline = new ResiliencePipelineBuilder()
    .AddCircuitBreaker(new CircuitBreakerStrategyOptions
    {
        FailureRatio = 0.5,
        MinimumThroughput = 3,
        BreakDuration = TimeSpan.FromSeconds(30),
        ShouldHandle = new PredicateBuilder().Handle<BrokerUnreachableException>()
    })
    .Build();

ctx.Properties.Add(PolicyKeys.QueueDeclare, pipeline);
```

---

### Example 4: Combining Multiple Policies

**BEFORE (Polly 5.x)**:
```csharp
var retryPolicy = Policy
    .Handle<BrokerUnreachableException>()
    .RetryAsync(3);

var circuitBreakerPolicy = Policy
    .Handle<BrokerUnreachableException>()
    .CircuitBreakerAsync(3, TimeSpan.FromSeconds(30));

var combinedPolicy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);

ctx.Properties.Add(PolicyKeys.BasicPublish, combinedPolicy);
```

**AFTER (Polly 8.x)**:
```csharp
// Polly 8.x: Chain strategies in a single pipeline
var pipeline = new ResiliencePipelineBuilder()
    .AddRetry(new RetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        ShouldHandle = new PredicateBuilder().Handle<BrokerUnreachableException>()
    })
    .AddCircuitBreaker(new CircuitBreakerStrategyOptions
    {
        FailureRatio = 0.5,
        MinimumThroughput = 3,
        BreakDuration = TimeSpan.FromSeconds(30),
        ShouldHandle = new PredicateBuilder().Handle<BrokerUnreachableException>()
    })
    .Build();

ctx.Properties.Add(PolicyKeys.BasicPublish, pipeline);
```

---

## Impact on Downstream Consumers

### Breaking Change for Consumers

**Who is affected**:
- Any RawRabbit consumer using `.UsePolly()` with custom policies
- Any code storing policies in `IPipeContext`

**What breaks**:
- Polly 5.x `Policy` API no longer exists
- `IAsyncPolicy` interface replaced with `ResiliencePipeline`
- `PolicyBuilder` replaced with `ResiliencePipelineBuilder`

**Migration effort for consumers**:
- **Simple policies**: 15-30 minutes per policy
- **Complex policies**: 1-2 hours per policy
- **Large codebases**: 1-3 days total

---

## Testing Strategy

### Unit Testing

**Setup**:
- Create Polly 8.x test policies
- Validate policy execution
- Test error handling

**Tests**:
- Retry policy execution
- Circuit breaker state transitions
- Timeout handling
- Policy combination (chaining)

### Integration Testing

**Setup**:
```bash
# Docker RabbitMQ for integration tests
docker run -d --name rabbitmq -p 5672:5672 rabbitmq:3
```

**Tests**:
- Publish with retry policy (simulate broker failure)
- Consumer creation with circuit breaker
- Connection recovery with timeout policy
- End-to-end resilience scenarios

---

## Backward Compatibility

**Question**: Should we support Polly 5.x and 8.x simultaneously?

**Answer**: NO - Breaking change is acceptable for major version (3.0.0)

**Rationale**:
- Polly 5.x and 8.x cannot coexist (type conflicts)
- Semantic versioning allows breaking changes in major versions
- Clear migration path provided
- Consumers have time to test before upgrading

---

## Checklist for Developers

### Pre-Implementation
- [ ] Read Polly 8.x migration guide: https://www.pollydocs.org/migration.html
- [ ] Review this document
- [ ] Update NuGet package: `Polly 5.3.1` → `8.4.2` ✅ (DONE)
- [ ] Backup current code (Git branch)

### During Implementation
- [ ] Update `PolicyMiddleware` and core classes
- [ ] Update all 9 middleware wrappers
- [ ] Update `ChannelFactory` wrapper
- [ ] Update all tests
- [ ] Update XML documentation
- [ ] Create code examples

### Post-Implementation
- [ ] All tests passing
- [ ] Integration tests passing
- [ ] Update MIGRATION-GUIDE.md with Polly 8.x examples
- [ ] Update CHANGELOG.md
- [ ] Code review

---

## Resources

### Official Documentation
- [Polly 8.0 Migration Guide](https://www.pollydocs.org/migration.html)
- [Polly 8.0 Release Notes](https://github.com/App-vNext/Polly/releases/tag/8.0.0)
- [Polly Documentation](https://www.pollydocs.org/)
- [Resilience Pipelines](https://www.pollydocs.org/pipelines/index.html)

### Internal Resources
- [ADR-004](adr/004-dependency-update-strategy.md) - Dependency update strategy
- [CHANGELOG.md](../CHANGELOG.md) - Breaking changes documentation
- [MIGRATION-GUIDE.md](../MIGRATION-GUIDE.md) - Consumer migration guide

---

## Status Tracking

| Task | Status | Estimated | Actual | Owner |
|------|--------|-----------|--------|-------|
| Research & Planning | ⚠️ TODO | 0.5 days | - | TBD |
| Update Core Plugin | ⚠️ TODO | 1 day | - | TBD |
| Update Middleware | ⚠️ TODO | 2 days | - | TBD |
| Update Services | ⚠️ TODO | 1 day | - | TBD |
| Update Tests | ⚠️ TODO | 1 day | - | TBD |
| **TOTAL** | **0% Complete** | **5.5 days** | **0 days** | **-** |

---

**Last Updated**: 2025-11-09
**Document Owner**: Migration Coordinator
**Depends On**: RabbitMQ.Client 6.x migration (can run in parallel if resourced)
**Next Review**: After RabbitMQ.Client migration complete
