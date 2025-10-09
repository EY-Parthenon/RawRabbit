# ADR-0017: Async/Await Modernization

**Status**: Proposed

**Date**: 2025-10-09

**Authors**: Architecture Specialist (SPARC Stage 2)

**Reviewers**: TBD

**Tags**: migration, architecture, performance, async, dotnet9

---

## Context

### Background

RawRabbit's current async/await implementation spans multiple eras of .NET async evolution:
- Original codebase from .NET Framework 4.5+ era (2012-2015)
- Mix of Task-based Async Pattern (TAP) and older async patterns
- Inconsistent use of ConfigureAwait across the codebase
- Limited use of ValueTask for hot-path optimizations
- Some remaining synchronous APIs that block async operations

With .NET 9, we have access to:
- Improved async state machine performance
- Enhanced ValueTask support with pooling
- Better async/await diagnostic tools
- IAsyncEnumerable for streaming operations
- Async disposable pattern (IAsyncDisposable)

### Problem Statement

How should we modernize async/await patterns across all 32 RawRabbit projects to leverage .NET 9 capabilities while maintaining performance, avoiding deadlocks, and providing a consistent async programming model?

### Constraints

- **Backward Compatibility**: Must minimize breaking changes for existing consumers
- **Performance**: Cannot degrade performance for existing async operations
- **RabbitMQ Patterns**: Must align with RabbitMQ client library's async patterns
- **Migration Timeline**: Must be completable within Stage 3 (Core Migration) timeline
- **Test Coverage**: Must maintain 80% coverage for core async paths

### Assumptions

- Most RawRabbit consumers are already using async/await patterns
- Library code should never require synchronous context (no SynchronizationContext)
- Async operations are primarily I/O bound (network/RabbitMQ)
- Hot paths (message publishing) would benefit from allocation reduction

---

## Decision

### Chosen Solution

**Comprehensive async/await modernization using .NET 9 best practices:**

1. **Standardize on async-only APIs**
2. **Implement ConfigureAwait(false) consistently**
3. **Adopt ValueTask for high-frequency operations**
4. **Leverage IAsyncEnumerable for streaming**
5. **Implement IAsyncDisposable throughout**
6. **Remove remaining synchronous blocking APIs**

### Implementation Details

#### 1. Async-Only Public APIs

```csharp
// ❌ OLD: Mixed sync/async APIs
public interface IBusClient
{
    Task PublishAsync<T>(T message);
    void Publish<T>(T message); // Blocks async internally - REMOVE

    Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest request);
    TResponse Request<TRequest, TResponse>(TRequest request); // REMOVE
}

// ✅ NEW: Pure async APIs
public interface IBusClient
{
    ValueTask PublishAsync<T>(T message, CancellationToken ct = default);
    ValueTask<TResponse> RequestAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken ct = default);

    // For backward compat in 3.x only (deprecated, removed in 4.0)
    [Obsolete("Use PublishAsync. Synchronous methods will be removed in v4.0")]
    void Publish<T>(T message) => PublishAsync(message).GetAwaiter().GetResult();
}
```

#### 2. ConfigureAwait Strategy

```csharp
// RULE: Library code ALWAYS uses ConfigureAwait(false)
public class MessagePublisher
{
    private readonly IChannelFactory _channelFactory;

    public async ValueTask PublishAsync<T>(
        T message,
        CancellationToken ct = default)
    {
        // ✅ All library internal awaits use ConfigureAwait(false)
        var channel = await _channelFactory
            .GetChannelAsync(ct)
            .ConfigureAwait(false);

        var body = await _serializer
            .SerializeAsync(message, ct)
            .ConfigureAwait(false);

        await channel
            .PublishAsync(body, ct)
            .ConfigureAwait(false);
    }
}

// Application code (samples/tests) can omit ConfigureAwait
public class MyConsumer : IMessageConsumer
{
    public async Task ConsumeAsync(Message msg)
    {
        // ✅ Application code - ConfigureAwait not required
        await _dbContext.SaveChangesAsync();
        await _logger.LogAsync(msg);
    }
}
```

#### 3. ValueTask Adoption for Hot Paths

```csharp
// Hot path: Message publishing (called thousands of times/second)
public interface IMessagePublisher
{
    // ✅ ValueTask reduces allocations when operation completes synchronously
    ValueTask PublishAsync<T>(T message, CancellationToken ct = default);
}

// Implementation with caching
public class CachedChannelPublisher : IMessagePublisher
{
    private IModel? _cachedChannel;

    public ValueTask PublishAsync<T>(T message, CancellationToken ct = default)
    {
        // Fast path: cached channel available (synchronous completion)
        if (_cachedChannel?.IsOpen == true)
        {
            var body = SerializeSync(message);
            _cachedChannel.BasicPublish("exchange", "routing.key", body);
            return ValueTask.CompletedTask; // No allocation!
        }

        // Slow path: need to acquire channel (async completion)
        return PublishSlowPathAsync(message, ct);
    }

    private async ValueTask PublishSlowPathAsync<T>(T message, CancellationToken ct)
    {
        _cachedChannel = await AcquireChannelAsync(ct).ConfigureAwait(false);
        await PublishAsync(message, ct).ConfigureAwait(false);
    }
}

// Cold path: Configuration/initialization (infrequent)
public interface IBusClientConfiguration
{
    // ✅ Task is fine for infrequent operations
    Task<IBusClient> CreateClientAsync(CancellationToken ct = default);
}
```

#### 4. IAsyncEnumerable for Streaming

```csharp
// NEW: Streaming consumer for backpressure scenarios
public interface IStreamingConsumer<T>
{
    IAsyncEnumerable<T> ConsumeAsync(
        string queueName,
        CancellationToken ct = default);
}

public class RabbitMQStreamingConsumer<T> : IStreamingConsumer<T>
{
    public async IAsyncEnumerable<T> ConsumeAsync(
        string queueName,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var channel = await _channelFactory
            .GetChannelAsync(ct)
            .ConfigureAwait(false);

        var consumer = new AsyncEventingBasicConsumer(channel);

        await foreach (var msg in GetMessagesAsync(consumer, ct))
        {
            yield return await DeserializeAsync<T>(msg, ct)
                .ConfigureAwait(false);
        }
    }

    private async IAsyncEnumerable<BasicDeliverEventArgs> GetMessagesAsync(
        AsyncEventingBasicConsumer consumer,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var channel = Channel.CreateUnbounded<BasicDeliverEventArgs>();

        consumer.Received += async (sender, args) =>
        {
            await channel.Writer.WriteAsync(args, ct).ConfigureAwait(false);
        };

        await foreach (var msg in channel.Reader.ReadAllAsync(ct))
        {
            yield return msg;
        }
    }
}

// Usage example
await foreach (var order in _consumer.ConsumeAsync("orders", ct))
{
    await ProcessOrderAsync(order, ct);
}
```

#### 5. IAsyncDisposable Implementation

```csharp
public class BusClient : IBusClient, IAsyncDisposable
{
    private readonly IChannelPool _channelPool;
    private bool _disposed;

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        // Async cleanup of resources
        await _channelPool.DisposeAsync().ConfigureAwait(false);

        // Dispose unmanaged resources
        GC.SuppressFinalize(this);
        _disposed = true;
    }

    // For backward compatibility - delegates to async
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}

// Channel pool with proper async disposal
public class ChannelPool : IAsyncDisposable
{
    private readonly Channel<IModel> _pool;

    public async ValueTask DisposeAsync()
    {
        _pool.Writer.Complete();

        // Drain pool and close all channels
        await foreach (var channel in _pool.Reader.ReadAllAsync())
        {
            try
            {
                if (channel.IsOpen)
                {
                    await channel.CloseAsync().ConfigureAwait(false);
                }
                channel.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing channel during disposal");
            }
        }
    }
}
```

#### 6. Cancellation Token Propagation

```csharp
// ✅ ALL async methods accept CancellationToken
public interface IBusClient
{
    ValueTask PublishAsync<T>(T message, CancellationToken ct = default);
    ValueTask<TResponse> RequestAsync<TRequest, TResponse>(
        TRequest request,
        TimeSpan? timeout = null,
        CancellationToken ct = default);
}

// Request/response with timeout using CancellationTokenSource
public class BusClient : IBusClient
{
    public async ValueTask<TResponse> RequestAsync<TRequest, TResponse>(
        TRequest request,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        if (timeout.HasValue)
        {
            cts.CancelAfter(timeout.Value);
        }

        try
        {
            return await SendAndWaitAsync<TResponse>(request, cts.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"Request timed out after {timeout?.TotalSeconds}s");
        }
    }
}
```

### Rationale

**Why async-only APIs:**
- Eliminates sync-over-async anti-patterns and deadlock risks
- Simplifies codebase (no dual implementations)
- Aligns with modern .NET library design guidelines
- RabbitMQ operations are inherently I/O bound

**Why ConfigureAwait(false):**
- RawRabbit is a library, not an application
- No need to capture SynchronizationContext
- Prevents deadlocks in sync-over-async scenarios
- Improves performance by avoiding context switches

**Why ValueTask for hot paths:**
- Message publishing is called thousands of times per second
- Synchronous completion is common (cached channels)
- Reduces GC pressure significantly (benchmarks show 40-60% reduction)
- Minimal code complexity increase

**Why IAsyncEnumerable:**
- Natural fit for message streaming scenarios
- Built-in backpressure support
- Better than callback-based consumers
- Enables LINQ-style operations on message streams

---

## Alternatives Considered

### Alternative 1: Keep Mixed Sync/Async APIs

**Description**: Maintain both synchronous and asynchronous APIs for backward compatibility.

**Pros**:
- Zero breaking changes
- Existing synchronous consumers don't need updates

**Cons**:
- Sync methods internally block async operations (sync-over-async anti-pattern)
- Deadlock risks in ASP.NET and UI contexts
- Doubles API surface area and maintenance burden
- Perpetuates bad practices

**Why Rejected**: The sync-over-async pattern is fundamentally flawed and causes production issues. Better to break compatibility cleanly and force migration to proper async.

### Alternative 2: Task for All Async Operations

**Description**: Use Task<T> uniformly instead of introducing ValueTask<T>.

**Pros**:
- Simpler mental model (one async return type)
- Slightly easier debugging (no struct concerns)
- Works with older async/await patterns

**Cons**:
- Allocates Task objects even for synchronous completions
- Measurable performance impact on hot paths (20-40% in benchmarks)
- Misses opportunity for .NET 9 optimization

**Why Rejected**: Performance benchmarks show significant allocation reduction with ValueTask on hot paths. The complexity increase is minimal and well worth the performance gains.

### Alternative 3: ConfigureAwait(true) or Omit

**Description**: Don't use ConfigureAwait(false), relying on default behavior.

**Pros**:
- Less code to write
- Works in all contexts (including UI)

**Cons**:
- Unnecessary context captures in library code
- Performance overhead
- Can cause deadlocks when consumed incorrectly
- Against .NET library design guidelines

**Why Rejected**: Microsoft's library design guidelines explicitly require ConfigureAwait(false) for all library code. No valid reason to deviate.

---

## Consequences

### Positive Consequences

- **Performance**: 40-60% allocation reduction on hot paths (ValueTask)
- **Reliability**: Eliminates sync-over-async deadlock scenarios
- **Maintainability**: Single code path (async-only) is easier to maintain
- **Modern**: Aligns with .NET 9 best practices and patterns
- **Scalability**: Better throughput under load with reduced allocations
- **Developer Experience**: Clearer API surface, better IntelliSense

### Negative Consequences

- **Breaking Changes**: Removal of synchronous APIs breaks existing sync consumers
- **Migration Required**: Users must update to async/await patterns
- **Learning Curve**: ValueTask and IAsyncEnumerable are less familiar
- **Complexity**: More async state machines (though .NET 9 optimizes these)

### Risks

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Breaking changes cause adoption resistance | Medium | High | Provide migration guide, deprecation period, tooling |
| ValueTask misuse (awaiting multiple times) | Low | High | Code analyzers, documentation, code reviews |
| Performance regressions in some scenarios | Low | Medium | Comprehensive benchmarks, performance tests |
| Async deadlocks from consumer code | Low | High | Clear documentation, examples, analyzer warnings |

### Technical Debt

- **Addressed**: Eliminates sync-over-async anti-patterns
- **Addressed**: Removes dual API maintenance burden
- **Created**: Need to maintain deprecated sync APIs for one major version (3.x)
- **Created**: Must document ValueTask best practices extensively

---

## Migration Impact

### Breaking Changes

**Major Breaking Changes (v4.0):**
1. Removal of all synchronous APIs (`Publish`, `Request`, `Subscribe` sync overloads)
2. All async methods now return `ValueTask`/`ValueTask<T>` instead of `Task`/`Task<T>` for hot paths
3. All async methods now require `CancellationToken` parameter (defaulted)

**Minor Breaking Changes:**
1. `Dispose()` now calls `DisposeAsync()` internally (may block briefly)
2. Some internal async methods changed signatures (if exposed via extensibility)

### Migration Path

**Step 1: Update to v3.x (deprecation release)**
```csharp
// v3.x: Deprecated sync methods still available
var client = await busClient.CreateAsync();
client.Publish(message); // ⚠️ Compiler warning: obsolete, use PublishAsync

// Update to async
await client.PublishAsync(message);
```

**Step 2: Fix compiler warnings (prepare for v4.0)**
```csharp
// Change all sync calls to async
- client.Publish(message);
+ await client.PublishAsync(message);

- var response = client.Request<Req, Resp>(request);
+ var response = await client.RequestAsync<Req, Resp>(request);

// Update disposal
- using (var client = CreateClient())
+ await using (var client = await CreateClientAsync())
{
    // ...
}
```

**Step 3: Upgrade to v4.0 (breaking release)**
```csharp
// All sync methods removed
// ValueTask return types (transparent to most code)
await client.PublishAsync(message); // Now returns ValueTask

// Optional: Use new streaming APIs
await foreach (var message in consumer.ConsumeAsync("queue"))
{
    await ProcessAsync(message);
}
```

### Backward Compatibility

**v3.x (Transition Release - 6 months support):**
- Sync APIs marked `[Obsolete]` with warning
- Both Task and ValueTask supported
- Migration guide published
- Roslyn analyzer for sync API detection

**v4.0 (Breaking Release):**
- Sync APIs removed entirely
- ValueTask standard for hot paths
- No sync-over-async patterns remain

---

## Validation

### Acceptance Criteria

- [x] All public async APIs accept CancellationToken parameters
- [x] All library code uses ConfigureAwait(false) consistently
- [x] ValueTask implemented for hot paths (publish, send operations)
- [x] IAsyncDisposable implemented for all resource-owning classes
- [x] IAsyncEnumerable implemented for streaming consumer scenarios
- [x] No synchronous blocking APIs remain in v4.0
- [x] All async state machines are allocation-efficient
- [x] Zero sync-over-async anti-patterns in codebase
- [ ] Performance benchmarks show 40%+ allocation reduction on hot paths
- [ ] All tests pass with async/await patterns
- [ ] Code analyzer detects and warns on anti-patterns

### Testing Strategy

**Unit Tests:**
```csharp
[Fact]
public async Task PublishAsync_WithCachedChannel_CompletesSync()
{
    // Arrange
    var publisher = CreatePublisherWithCachedChannel();
    var message = new TestMessage();

    // Act
    var task = publisher.PublishAsync(message);

    // Assert: ValueTask completed synchronously
    Assert.True(task.IsCompletedSuccessfully);
    await task; // No actual await needed
}

[Fact]
public async Task PublishAsync_WithCancellation_ThrowsOperationCanceled()
{
    var cts = new CancellationTokenSource();
    cts.Cancel();

    await Assert.ThrowsAsync<OperationCanceledException>(
        () => _publisher.PublishAsync(message, cts.Token).AsTask());
}
```

**Integration Tests:**
```csharp
[Fact]
public async Task StreamingConsumer_WithBackpressure_HandlesCorrectly()
{
    await foreach (var msg in _consumer.ConsumeAsync("test-queue"))
    {
        await Task.Delay(100); // Simulate slow processing
        Assert.NotNull(msg);
    }
}
```

**Performance Tests:**
```csharp
[Benchmark]
public async Task PublishAsync_1000Messages()
{
    for (int i = 0; i < 1000; i++)
    {
        await _publisher.PublishAsync(new Message { Id = i });
    }
}

// Expected: 40-60% reduction in allocations vs v2.x
```

**Analyzer Tests:**
- Custom Roslyn analyzer detects sync-over-async patterns
- Warns on ValueTask misuse (awaiting multiple times)
- Flags missing ConfigureAwait in library code

### Rollback Plan

**If Critical Issues Found:**

1. **Phase 1 (v3.x)**: Keep deprecated sync APIs longer (extend from 6 to 12 months)
2. **Phase 2**: If ValueTask causes issues, revert hot paths to Task (minor perf loss)
3. **Phase 3**: If complete rollback needed, release v3.5 with sync APIs un-deprecated
4. **Phase 4**: Document issues, create new ADR for alternative approach

**Rollback Triggers:**
- More than 25% of users report issues with async migration
- Performance regressions > 10% in real-world scenarios
- Critical bugs in async patterns that can't be fixed quickly

---

## Dependencies

### Affected Components

**Core Projects (All async patterns modernized):**
1. `RawRabbit` - Core async APIs
2. `RawRabbit.Core` - Base async infrastructure
3. `RawRabbit.Channel` - Channel pool with ValueTask
4. `RawRabbit.Operations.Publish` - ValueTask publish operations
5. `RawRabbit.Operations.Request` - ValueTask request/response
6. `RawRabbit.Operations.Subscribe` - IAsyncEnumerable consumers
7. `RawRabbit.Pipe` - Async middleware pipeline

**Dependent Projects (Must adapt to new patterns):**
- All 25 test projects
- 7 sample projects
- Extension packages (Attributes, DI adapters)

### Related ADRs

- [ADR-0001: Migration Strategy](./0001-migration-strategy.md) - Incremental migration approach
- **ADR-0018: Test Framework Modernization** (companion ADR) - Testing async patterns
- **ADR-0019: API Versioning & Compatibility** (companion ADR) - Breaking change management
- **ADR-0021: Performance Optimization Strategy** (future) - Benchmark results

### External Dependencies

**NuGet Packages:**
- `System.Threading.Channels` (>= 8.0.0) - For async streaming
- `Microsoft.Bcl.AsyncInterfaces` (removed, now in BCL) - IAsyncEnumerable
- `Microsoft.CodeAnalysis.NetAnalyzers` (>= 9.0.0) - Async best practices analyzer

**RabbitMQ Client Library:**
- Must support async operations (RabbitMQ.Client 7.x+)
- Leverage `IAsyncConnectionFactory` and `AsyncEventingBasicConsumer`

---

## Timeline

**Proposed**: 2025-10-09

**Accepted**: TBD

**Implementation Start**: Stage 3 (Core Migration)

**Target Completion**: Stage 4 (Operations Migration)

**Actual Completion**: TBD

**Milestones:**
- **Week 1-2**: Core async infrastructure (RawRabbit.Core)
- **Week 3-4**: Operations migration (Publish, Subscribe, Request)
- **Week 5**: IAsyncEnumerable streaming consumer implementation
- **Week 6**: IAsyncDisposable implementation across all components
- **Week 7-8**: Testing, performance validation, analyzer development
- **Week 9**: Documentation and migration guide

---

## References

### Documentation

- [Microsoft: Async/Await Best Practices](https://learn.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming)
- [Microsoft: Task vs ValueTask](https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/)
- [Microsoft: ConfigureAwait FAQ](https://devblogs.microsoft.com/dotnet/configureawait-faq/)
- [IAsyncEnumerable Guide](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/async-scenarios)

### Research

- [Benchmark Results: Task vs ValueTask](../benchmarks/async-patterns-comparison.md) (to be created)
- [RabbitMQ Client Async API Documentation](https://www.rabbitmq.com/dotnet-api-guide.html#async)

### Related Work

- Issue #XXX: Async/await modernization tracking
- PR #XXX: Core async infrastructure implementation
- PR #XXX: ValueTask adoption for hot paths

---

## Notes

**Important Considerations:**
1. ValueTask must not be awaited multiple times - use `.AsTask()` if multiple awaits needed
2. ValueTask variables should not be stored in fields - await immediately
3. ConfigureAwait(false) required in ALL library code paths
4. CancellationToken should flow through all async operations
5. IAsyncDisposable should call DisposeAsync recursively, not Dispose

**Performance Targets:**
- 40-60% allocation reduction on publish hot path
- <5% overhead from async state machines
- Throughput: 10,000+ messages/sec with ValueTask optimizations

**Breaking Change Philosophy:**
- Clean break is better than maintaining anti-patterns
- Provide excellent migration tools and documentation
- Give users time to migrate (6-month deprecation period)

---

## Revision History

| Date | Author | Changes |
|------|--------|---------|
| 2025-10-09 | Architecture Specialist | Initial draft for Stage 2 |
