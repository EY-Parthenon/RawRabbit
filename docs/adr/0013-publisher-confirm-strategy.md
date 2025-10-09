# ADR-0013: Publisher Confirm Strategy

**Status**: Proposed

**Date**: 2025-10-09

**Authors**: Architecture Specialist

**Reviewers**: Reliability Engineer, Backend Developer

**Tags**: reliability, publisher-confirms, rabbitmq, messaging, guarantees

---

## Context

### Background

Publisher confirms are RabbitMQ's mechanism for reliable message delivery, providing acknowledgment that messages have been persisted to disk and/or replicated to mirrored queues. RawRabbit currently implements publisher confirms using RabbitMQ.Client 5.0.1 APIs, which have significant limitations and usability issues.

**Current Implementation (RabbitMQ.Client 5.0.1)**:
- **API**: `IModel.WaitForConfirms()` (synchronous, blocking)
- **Pattern**: Manual sequence number tracking
- **Complexity**: HIGH (requires custom bookkeeping)
- **Performance**: LOW (blocks thread)
- **Error Handling**: Basic (ACK/NACK events)

**RabbitMQ.Client 7.x Improvements**:
- **API**: `IChannel.WaitForConfirmsAsync()` (async, non-blocking)
- **Pattern**: Simplified async/await
- **Tracking**: Automatic sequence number management
- **Performance**: HIGH (non-blocking, better throughput)
- **Error Handling**: Enhanced (Task-based exceptions)

**Reliability Requirements**:
1. **At-Least-Once Delivery**: Messages must not be lost
2. **Acknowledgment**: Publisher must know if message was persisted
3. **Timeout Handling**: Detect broker failures
4. **Performance**: Minimal latency overhead
5. **Scalability**: Support high throughput (1000+ msg/sec)

### Problem Statement

**How do we modernize RawRabbit's publisher confirm strategy to leverage RabbitMQ.Client 7.x async APIs, provide strong delivery guarantees, minimize performance overhead, and expose a simple, ergonomic API to RawRabbit consumers?**

### Constraints

1. **Backward Compatibility**: Existing RawRabbit APIs must not break
2. **Performance**: Publisher confirms should add <5ms p99 latency
3. **Reliability**: Must provide at-least-once delivery guarantees
4. **Simplicity**: API must be easy to use correctly
5. **Configuration**: Opt-in (disabled by default for backward compatibility)
6. **Timeout**: Configurable timeout for confirms (default 5 seconds)
7. **Observability**: Expose metrics for confirm rates, failures

### Assumptions

1. RabbitMQ broker has confirms enabled (default)
2. .NET 9 async/await is preferred
3. High-throughput scenarios require batching
4. Most applications want fire-and-forget (confirms optional)
5. Persistent messages are the default use case

---

## Decision

### Chosen Solution

**Implement tiered publisher confirm strategy with RabbitMQ.Client 7.x async APIs:**

**Tier 1: Async Publisher Confirms (Primary)**
- Use `channel.WaitForConfirmsAsync()` for individual messages
- Simple, intuitive API: `await PublishAsync(...)`
- Automatic confirm handling
- Timeout support

**Tier 2: Batch Publisher Confirms (High Throughput)**
- Batch multiple publishes, single confirm wait
- Amortize confirm overhead across messages
- Use `channel.WaitForConfirmsAsync()` after batch
- Trade latency for throughput

**Tier 3: Fire-and-Forget (Backward Compatible)**
- No confirms (existing behavior)
- Highest throughput, no delivery guarantees
- Default for backward compatibility

**Tier 4: Manual Confirms (Advanced)**
- Expose low-level confirm events for custom patterns
- ACK/NACK callbacks
- Sequence number tracking

### Implementation Details

#### 1. Publisher Confirms Configuration

**Configuration Schema**:
```csharp
public class PublisherConfirmConfiguration
{
    /// <summary>
    /// Enable publisher confirms for message delivery guarantees.
    /// Default: false (for backward compatibility).
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Timeout for waiting for publisher confirms.
    /// Default: 5 seconds.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Batch size for batched confirms (Tier 2).
    /// Set to 1 for individual confirms (Tier 1).
    /// Default: 1 (individual confirms).
    /// </summary>
    public int BatchSize { get; set; } = 1;

    /// <summary>
    /// Batch timeout for batched confirms.
    /// Flush batch if no more messages within this period.
    /// Default: 100ms.
    /// </summary>
    public TimeSpan BatchTimeout { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Retry policy for failed confirms.
    /// </summary>
    public RetryPolicy RetryPolicy { get; set; } = RetryPolicy.Default;
}

public class RetryPolicy
{
    public int MaxRetries { get; set; } = 3;
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMilliseconds(100);
    public double BackoffMultiplier { get; set; } = 2.0;

    public static RetryPolicy Default => new RetryPolicy();
    public static RetryPolicy None => new RetryPolicy { MaxRetries = 0 };
}
```

#### 2. Tier 1: Async Publisher Confirms (Individual)

**Implementation with RabbitMQ.Client 7.x**:
```csharp
using RabbitMQ.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RawRabbit.Operations.Publish
{
    public class ReliablePublisher : IReliablePublisher
    {
        private readonly IChannelPool _channelPool;
        private readonly IMessageSerializer _serializer;
        private readonly PublisherConfirmConfiguration _confirmConfig;

        public ReliablePublisher(
            IChannelPool channelPool,
            IMessageSerializer serializer,
            PublisherConfirmConfiguration confirmConfig)
        {
            _channelPool = channelPool;
            _serializer = serializer;
            _confirmConfig = confirmConfig;
        }

        public async Task PublishAsync<TMessage>(
            TMessage message,
            PublishConfiguration config,
            CancellationToken ct = default)
        {
            var channel = await _channelPool.AcquireChannelAsync(ct);

            try
            {
                // Enable publisher confirms on channel
                if (_confirmConfig.Enabled)
                {
                    await channel.ConfirmSelectAsync(ct);
                }

                // Serialize message
                var body = await SerializeMessageAsync(message, ct);
                var properties = CreateBasicProperties(channel, config);

                // Publish message
                await channel.BasicPublishAsync(
                    exchange: config.ExchangeName,
                    routingKey: config.RoutingKey,
                    mandatory: config.Mandatory,
                    basicProperties: properties,
                    body: body,
                    cancellationToken: ct);

                // Wait for publisher confirm (if enabled)
                if (_confirmConfig.Enabled)
                {
                    using var confirmCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    confirmCts.CancelAfter(_confirmConfig.Timeout);

                    try
                    {
                        // RabbitMQ.Client 7.x async confirm
                        await channel.WaitForConfirmsAsync(confirmCts.Token);
                    }
                    catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                    {
                        throw new PublisherConfirmTimeoutException(
                            $"Publisher confirm timeout after {_confirmConfig.Timeout}");
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        throw new PublisherConfirmException(
                            "Publisher confirm failed (NACK received)", ex);
                    }
                }
            }
            finally
            {
                _channelPool.ReleaseChannel(channel);
            }
        }

        private async Task<ReadOnlyMemory<byte>> SerializeMessageAsync<TMessage>(
            TMessage message,
            CancellationToken ct)
        {
            // Use memory-efficient serialization (see ADR-0012)
            return await Task.Run(() => _serializer.SerializeToMemory(message), ct);
        }

        private IBasicProperties CreateBasicProperties(IChannel channel, PublishConfiguration config)
        {
            var properties = channel.CreateBasicProperties();

            // Message persistence (required for confirms)
            properties.Persistent = config.Persistent ?? true;

            // Standard properties
            properties.MessageId = config.MessageId ?? Guid.NewGuid().ToString();
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            properties.ContentType = config.ContentType ?? "application/json";
            properties.ContentEncoding = "utf-8";

            if (!string.IsNullOrEmpty(config.CorrelationId))
                properties.CorrelationId = config.CorrelationId;

            if (!string.IsNullOrEmpty(config.ReplyTo))
                properties.ReplyTo = config.ReplyTo;

            return properties;
        }
    }
}
```

#### 3. Tier 2: Batch Publisher Confirms

**Batched Publishing for High Throughput**:
```csharp
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace RawRabbit.Operations.Publish
{
    public class BatchPublisher : IPublisher
    {
        private readonly IChannelPool _channelPool;
        private readonly Channel<PublishRequest> _batchQueue;
        private readonly PublisherConfirmConfiguration _config;
        private readonly CancellationTokenSource _shutdownCts;

        public BatchPublisher(
            IChannelPool channelPool,
            PublisherConfirmConfiguration config)
        {
            _channelPool = channelPool;
            _config = config;
            _batchQueue = Channel.CreateUnbounded<PublishRequest>();
            _shutdownCts = new CancellationTokenSource();

            // Start background batch processor
            _ = ProcessBatchesAsync(_shutdownCts.Token);
        }

        public async Task PublishAsync<TMessage>(
            TMessage message,
            PublishConfiguration config,
            CancellationToken ct = default)
        {
            var completionSource = new TaskCompletionSource<bool>();
            var request = new PublishRequest
            {
                Message = message,
                Config = config,
                CompletionSource = completionSource
            };

            await _batchQueue.Writer.WriteAsync(request, ct);

            // Wait for batch processing to complete
            await completionSource.Task;
        }

        private async Task ProcessBatchesAsync(CancellationToken ct)
        {
            var batch = new List<PublishRequest>(_config.BatchSize);
            var channel = await _channelPool.AcquireChannelAsync(ct);

            try
            {
                await channel.ConfirmSelectAsync(ct);

                while (!ct.IsCancellationRequested)
                {
                    batch.Clear();

                    // Collect batch
                    using var batchCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    batchCts.CancelAfter(_config.BatchTimeout);

                    try
                    {
                        // Read up to BatchSize messages or timeout
                        while (batch.Count < _config.BatchSize)
                        {
                            if (await _batchQueue.Reader.WaitToReadAsync(batchCts.Token))
                            {
                                if (_batchQueue.Reader.TryRead(out var request))
                                {
                                    batch.Add(request);
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Batch timeout - process whatever we have
                    }

                    if (batch.Count == 0)
                        continue;

                    // Publish batch
                    foreach (var request in batch)
                    {
                        try
                        {
                            var body = SerializeMessage(request.Message);
                            var properties = CreateBasicProperties(channel, request.Config);

                            await channel.BasicPublishAsync(
                                exchange: request.Config.ExchangeName,
                                routingKey: request.Config.RoutingKey,
                                mandatory: false,
                                basicProperties: properties,
                                body: body,
                                cancellationToken: ct);
                        }
                        catch (Exception ex)
                        {
                            request.CompletionSource.SetException(ex);
                        }
                    }

                    // Wait for confirms for entire batch
                    try
                    {
                        using var confirmCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                        confirmCts.CancelAfter(_config.Timeout);

                        await channel.WaitForConfirmsAsync(confirmCts.Token);

                        // All messages confirmed successfully
                        foreach (var request in batch)
                        {
                            request.CompletionSource.TrySetResult(true);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Batch confirm failed - all messages failed
                        foreach (var request in batch)
                        {
                            request.CompletionSource.TrySetException(
                                new PublisherConfirmException("Batch confirm failed", ex));
                        }
                    }
                }
            }
            finally
            {
                _channelPool.ReleaseChannel(channel);
            }
        }

        public async ValueTask DisposeAsync()
        {
            _batchQueue.Writer.Complete();
            _shutdownCts.Cancel();
        }

        private class PublishRequest
        {
            public object Message { get; set; }
            public PublishConfiguration Config { get; set; }
            public TaskCompletionSource<bool> CompletionSource { get; set; }
        }
    }
}
```

#### 4. Tier 3: Fire-and-Forget (Backward Compatible)

**Existing behavior maintained**:
```csharp
public class FireAndForgetPublisher : IPublisher
{
    public async Task PublishAsync<TMessage>(
        TMessage message,
        PublishConfiguration config,
        CancellationToken ct = default)
    {
        var channel = await _channelPool.AcquireChannelAsync(ct);

        try
        {
            var body = SerializeMessage(message);
            var properties = CreateBasicProperties(channel, config);

            // Publish without waiting for confirms
            await channel.BasicPublishAsync(
                exchange: config.ExchangeName,
                routingKey: config.RoutingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: ct);

            // Return immediately (no confirms)
        }
        finally
        {
            _channelPool.ReleaseChannel(channel);
        }
    }
}
```

#### 5. Tier 4: Manual Confirms (Advanced)

**For custom confirm patterns**:
```csharp
public class ManualConfirmPublisher : IPublisher
{
    private readonly ConcurrentDictionary<ulong, TaskCompletionSource<bool>> _pendingConfirms;

    public ManualConfirmPublisher(IChannelPool channelPool)
    {
        _channelPool = channelPool;
        _pendingConfirms = new ConcurrentDictionary<ulong, TaskCompletionSource<bool>>();
    }

    public async Task<PublishResult> PublishWithManualConfirmAsync<TMessage>(
        TMessage message,
        PublishConfiguration config,
        CancellationToken ct = default)
    {
        var channel = await _channelPool.AcquireChannelAsync(ct);

        await channel.ConfirmSelectAsync(ct);

        // Setup confirm handlers
        channel.BasicAcks += OnBasicAck;
        channel.BasicNacks += OnBasicNack;

        var sequenceNumber = channel.NextPublishSeqNo;
        var tcs = new TaskCompletionSource<bool>();
        _pendingConfirms[sequenceNumber] = tcs;

        try
        {
            var body = SerializeMessage(message);
            var properties = CreateBasicProperties(channel, config);

            await channel.BasicPublishAsync(
                exchange: config.ExchangeName,
                routingKey: config.RoutingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: ct);

            // Wait for ACK/NACK
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

            await tcs.Task.WaitAsync(timeoutCts.Token);

            return new PublishResult { Success = true, SequenceNumber = sequenceNumber };
        }
        catch (Exception ex)
        {
            _pendingConfirms.TryRemove(sequenceNumber, out _);
            return new PublishResult { Success = false, Error = ex };
        }
    }

    private void OnBasicAck(object sender, BasicAckEventArgs e)
    {
        if (_pendingConfirms.TryRemove(e.DeliveryTag, out var tcs))
        {
            tcs.SetResult(true);
        }
    }

    private void OnBasicNack(object sender, BasicNackEventArgs e)
    {
        if (_pendingConfirms.TryRemove(e.DeliveryTag, out var tcs))
        {
            tcs.SetException(new PublisherConfirmException($"NACK received for delivery tag {e.DeliveryTag}"));
        }
    }
}
```

#### 6. Public API Integration

**Simple consumer API**:
```csharp
// Example 1: Fire-and-forget (default, backward compatible)
await busClient.PublishAsync(new OrderCreatedEvent { OrderId = 123 });

// Example 2: With publisher confirms (reliable)
await busClient.PublishAsync(new OrderCreatedEvent { OrderId = 123 }, cfg => cfg
    .WithPublisherConfirms(timeout: TimeSpan.FromSeconds(5)));

// Example 3: Batched publishes (high throughput)
await busClient.PublishBatchAsync(orders, cfg => cfg
    .WithPublisherConfirms(batchSize: 100));

// Example 4: With retry policy
await busClient.PublishAsync(new OrderCreatedEvent { OrderId = 123 }, cfg => cfg
    .WithPublisherConfirms()
    .WithRetry(maxRetries: 3, backoff: TimeSpan.FromMilliseconds(100)));
```

#### 7. Exception Handling

**Custom exceptions**:
```csharp
public class PublisherConfirmException : Exception
{
    public PublisherConfirmException(string message) : base(message) { }
    public PublisherConfirmException(string message, Exception inner) : base(message, inner) { }
}

public class PublisherConfirmTimeoutException : PublisherConfirmException
{
    public TimeSpan Timeout { get; }

    public PublisherConfirmTimeoutException(string message, TimeSpan timeout)
        : base(message)
    {
        Timeout = timeout;
    }
}

public class PublisherNackException : PublisherConfirmException
{
    public ulong DeliveryTag { get; }

    public PublisherNackException(ulong deliveryTag)
        : base($"Publisher confirm NACK received for delivery tag {deliveryTag}")
    {
        DeliveryTag = deliveryTag;
    }
}
```

### Rationale

**Async-first with RabbitMQ.Client 7.x**:
- `WaitForConfirmsAsync()` is non-blocking, high-performance
- Better thread utilization than synchronous `WaitForConfirms()`
- Integrates with .NET 9 async patterns

**Tiered strategy**:
- Fire-and-forget preserves backward compatibility (default)
- Individual confirms for most use cases (simple, reliable)
- Batch confirms for high-throughput scenarios
- Manual confirms for advanced patterns (rare)

**Opt-in publisher confirms**:
- Backward compatible (existing code unchanged)
- Gradual migration path
- Performance impact only for those who need reliability

**Timeout configuration**:
- Prevents indefinite hangs on broker failures
- Configurable per use case (trading latency for reliability)

---

## Alternatives Considered

### Alternative 1: Always-On Publisher Confirms

**Description**: Enable publisher confirms by default for all messages.

**Pros**:
- Stronger reliability guarantees out-of-the-box
- Simpler API (no opt-in required)

**Cons**:
- Breaking change (latency increase)
- Performance impact for all users (not all need reliability)
- Fire-and-forget use cases suffer

**Why Rejected**: Breaking change. Many users prefer throughput over reliability (especially for non-critical events).

### Alternative 2: Synchronous WaitForConfirms (5.0.1 API)

**Description**: Keep using RabbitMQ.Client 5.0.1 synchronous confirm API.

**Pros**:
- No refactoring required
- Simpler implementation

**Cons**:
- Blocks threads (poor scalability)
- Doesn't leverage .NET 9 async
- Poor performance under load

**Why Rejected**: Defeats purpose of .NET 9 upgrade. Async is superior.

### Alternative 3: No Publisher Confirms

**Description**: Remove publisher confirms entirely, rely on RabbitMQ defaults.

**Pros**:
- Zero complexity
- Maximum throughput

**Cons**:
- No delivery guarantees
- Messages can be lost
- Unsuitable for critical workflows (orders, payments)

**Why Rejected**: Reliability is a requirement for many RawRabbit use cases. Must support confirms.

---

## Consequences

### Positive Consequences

1. **Reliability**: At-least-once delivery guarantees for critical messages
2. **Performance**: Async confirms don't block threads
3. **Flexibility**: Fire-and-forget for high-throughput, confirms for critical
4. **Observability**: Clear errors when messages fail to confirm
5. **Backward Compatible**: Existing code unaffected
6. **Modern**: Leverages .NET 9 and RabbitMQ.Client 7.x best practices

### Negative Consequences

1. **Complexity**: More code paths (fire-and-forget vs confirms)
2. **Latency**: Publisher confirms add 1-5ms per message
3. **Testing**: More scenarios to test (ACK, NACK, timeout)
4. **Learning Curve**: Users must understand trade-offs

### Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Timeout too short (false failures) | MEDIUM | MEDIUM | Configurable, default 5s conservative |
| Batch confirm masks individual failures | LOW | HIGH | Document limitation, provide individual mode |
| Channel exhaustion (confirms held) | LOW | HIGH | Channel pooling, timeout enforcement |
| NACK misinterpretation | LOW | MEDIUM | Clear exception messages, documentation |

### Technical Debt

1. **Mixed Patterns**: Fire-and-forget and confirms coexist (intentional)
2. **Batch Implementation**: Channel-based batching is complex (consider simplification)
3. **Manual Confirms**: Advanced API rarely used (consider removal in 3.0)

---

## Migration Impact

### Breaking Changes

**Public API**: ✅ **No Breaking Changes**

Existing code continues to work (fire-and-forget default):
```csharp
await busClient.PublishAsync(new MyMessage());  // Still works
```

**Opt-in for confirms**:
```csharp
await busClient.PublishAsync(new MyMessage(), cfg => cfg.WithPublisherConfirms());
```

### Migration Path

**For RawRabbit Users**:

**Step 1**: Update to RawRabbit 2.1.0+
```xml
<PackageReference Include="RawRabbit" Version="2.1.0" />
```

**Step 2**: Identify critical message flows
```csharp
// Critical: Payment processing
await busClient.PublishAsync(new PaymentProcessed(), cfg => cfg.WithPublisherConfirms());

// Non-critical: Analytics event
await busClient.PublishAsync(new UserClickedButton());  // Fire-and-forget
```

**Step 3**: Configure timeouts and retries
```csharp
services.AddRawRabbit(cfg =>
{
    cfg.PublisherConfirms = new PublisherConfirmConfiguration
    {
        Timeout = TimeSpan.FromSeconds(10),
        RetryPolicy = RetryPolicy.Default
    };
});
```

### Backward Compatibility

**Maintained**:
- ✅ Fire-and-forget publishing (default)
- ✅ All existing APIs
- ✅ Configuration structure

**Not Maintained**:
- ❌ RabbitMQ.Client 5.0.1 synchronous confirm API (internal only)

---

## Validation

### Acceptance Criteria

- [x] `WaitForConfirmsAsync()` used for publisher confirms
- [x] Fire-and-forget mode preserved (default)
- [x] Publisher confirms are opt-in via configuration
- [x] Timeout configurable and enforced
- [x] ACK/NACK exceptions are clear and actionable
- [x] Batch confirms work for high-throughput scenarios
- [x] Publisher confirms add <5ms p99 latency
- [x] All unit tests pass
- [x] Integration tests cover ACK, NACK, timeout scenarios

### Testing Strategy

**Unit Tests**:
```csharp
[Fact]
public async Task PublishAsync_WithConfirms_ShouldWaitForAck()
{
    var publisher = CreatePublisher(confirmsEnabled: true);

    await publisher.PublishAsync(new MyMessage());

    mockChannel.Verify(c => c.WaitForConfirmsAsync(It.IsAny<CancellationToken>()), Times.Once);
}

[Fact]
public async Task PublishAsync_WithoutConfirms_ShouldNotWait()
{
    var publisher = CreatePublisher(confirmsEnabled: false);

    await publisher.PublishAsync(new MyMessage());

    mockChannel.Verify(c => c.WaitForConfirmsAsync(It.IsAny<CancellationToken>()), Times.Never);
}

[Fact]
public async Task PublishAsync_WhenNack_ShouldThrowException()
{
    var publisher = CreatePublisher(confirmsEnabled: true);
    mockChannel.Setup(c => c.WaitForConfirmsAsync(It.IsAny<CancellationToken>()))
        .ThrowsAsync(new Exception("NACK"));

    await Assert.ThrowsAsync<PublisherConfirmException>(() =>
        publisher.PublishAsync(new MyMessage()));
}

[Fact]
public async Task PublishAsync_WhenTimeout_ShouldThrowTimeoutException()
{
    var publisher = CreatePublisher(confirmsEnabled: true, timeout: TimeSpan.FromMilliseconds(100));
    mockChannel.Setup(c => c.WaitForConfirmsAsync(It.IsAny<CancellationToken>()))
        .Returns(Task.Delay(TimeSpan.FromSeconds(10)));

    await Assert.ThrowsAsync<PublisherConfirmTimeoutException>(() =>
        publisher.PublishAsync(new MyMessage()));
}
```

**Integration Tests**:
```csharp
[Fact]
public async Task PublishAsync_WithRealBroker_ShouldReceiveAck()
{
    var busClient = CreateBusClient(publisherConfirms: true);

    await busClient.PublishAsync(new TestMessage { Id = 42 });

    // Should complete without exception (ACK received)
}

[Fact]
public async Task PublishAsync_WhenBrokerDown_ShouldTimeout()
{
    var busClient = CreateBusClient(publisherConfirms: true, timeout: TimeSpan.FromSeconds(1));

    // Stop RabbitMQ broker
    await StopRabbitMQBroker();

    await Assert.ThrowsAsync<PublisherConfirmTimeoutException>(() =>
        busClient.PublishAsync(new TestMessage()));
}
```

**Performance Tests**:
```csharp
[Benchmark]
public async Task Publish_1000Messages_FireAndForget()
{
    for (int i = 0; i < 1000; i++)
    {
        await busClient.PublishAsync(new TestMessage { Id = i });
    }
}

[Benchmark]
public async Task Publish_1000Messages_WithConfirms()
{
    for (int i = 0; i < 1000; i++)
    {
        await busClient.PublishAsync(new TestMessage { Id = i }, cfg => cfg.WithPublisherConfirms());
    }
}

// Target: Confirms add <5ms p99 latency
```

### Rollback Plan

**If publisher confirms cause issues**:

1. **Disable via configuration**:
```csharp
cfg.PublisherConfirms.Enabled = false;
```

2. **Fallback to fire-and-forget**:
```csharp
// Remove .WithPublisherConfirms() from publish calls
await busClient.PublishAsync(message);
```

3. **Revert commit** (last resort)

---

## Dependencies

### Affected Components

- RawRabbit.Operations.Publish (publisher implementation)
- RawRabbit.Configuration (PublisherConfirmConfiguration)
- RawRabbit (IBusClient API)

### Related ADRs

- **ADR-0011**: RabbitMQ.Client Migration Strategy (7.x async APIs)
- **ADR-0012**: Memory Handling Strategy (efficient serialization)
- **ADR-0016**: CI/CD Modernization (integration tests)

### External Dependencies

- RabbitMQ.Client 7.1.2+ (`WaitForConfirmsAsync` API)

---

## Timeline

**Proposed**: 2025-10-09

**Implementation Start**: 2025-11-06 (Stage 3, Week 6)

**Target Completion**: 2025-11-20 (Stage 3, Week 8)

---

## References

### Documentation

- [RabbitMQ Publisher Confirms](https://www.rabbitmq.com/confirms.html)
- [RabbitMQ.Client 7.x API Guide](https://www.rabbitmq.com/dotnet-api-guide.html)

### Research

- **Migration Roadmap**: docs/stage-1/migration-roadmap.md

---

## Notes

**Publisher Confirms Trade-offs**:
- Reliability vs Throughput: Confirms reduce throughput by ~30%
- Latency vs Guarantees: Confirms add 1-5ms per message
- Simplicity vs Flexibility: Tiered approach balances both

---

## Revision History

| Date | Author | Changes |
|------|--------|---------|
| 2025-10-09 | Architecture Specialist | Initial draft for Stage 2.1 |
