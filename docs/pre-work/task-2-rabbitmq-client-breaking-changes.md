# Task 2: RabbitMQ.Client Breaking Changes Research (5.x → 7.x)

**Date**: 2025-10-09
**Task ID**: task-2-rabbitmq
**Branch**: pre-work
**Status**: Complete

## Executive Summary

RabbitMQ.Client has undergone major breaking changes from version 5.0.1 (currently used by RawRabbit) to version 7.x (target version). The migration path requires upgrading through version 6.x, which introduced significant memory management changes, and then to version 7.x, which completely redesigned the API to be fully asynchronous.

### Critical Breaking Changes

1. **Version 7.0**: Complete API redesign - all methods are now async-only
2. **Version 7.0**: IModel renamed to IChannel
3. **Version 6.0**: Memory model changed from `byte[]` to `ReadOnlyMemory<byte>`
4. **Version 6.0**: BasicProperties construction changed
5. **Version 7.0**: Consumer model completely async
6. **Version 7.0**: Publisher confirms integrated into async API

### Impact Severity: **HIGH**

- **39 files** use IModel interface (must be renamed to IChannel)
- **27 files** use BasicPublish/CreateBasicProperties (API signature changes)
- **16 files** use consumer interfaces (complete async rewrite required)
- **27 files** use topology operations (QueueDeclare, ExchangeDeclare, etc. - all async)
- **4 files** create connections (must use async CreateConnectionAsync)

---

## Breaking Changes by Version

### Version 6.0.0 Breaking Changes

#### 1. Memory Model Change: `byte[]` → `ReadOnlyMemory<byte>`

**Description**: The client now uses the System.Memory library for all message payloads.

**Before (5.x)**:
```csharp
// Consumer delivery
void HandleBasicDeliver(
    string consumerTag,
    ulong deliveryTag,
    bool redelivered,
    string exchange,
    string routingKey,
    IBasicProperties properties,
    byte[] body  // ← byte array
)

// Publishing
channel.BasicPublish(exchange, routingKey, false, properties, body);
```

**After (6.x)**:
```csharp
// Consumer delivery
void HandleBasicDeliver(
    string consumerTag,
    ulong deliveryTag,
    bool redelivered,
    string exchange,
    string routingKey,
    IBasicProperties properties,
    ReadOnlyMemory<byte> body  // ← ReadOnlyMemory<byte>
)

// Publishing
channel.BasicPublish(exchange, routingKey, false, properties,
    new ReadOnlyMemory<byte>(body));
```

**⚠️ CRITICAL**: The `ReadOnlyMemory<byte>` is only valid within the delivery handler scope. Applications must copy or deserialize the data before the handler returns.

**Impact on RawRabbit**:
- All middleware that processes message bodies must be updated
- Consumer factories must handle ReadOnlyMemory instead of byte[]
- Serialization/deserialization logic needs updating

---

#### 2. BasicProperties Construction

**Description**: BasicProperties is no longer publicly constructable in 6.0.

**Before (5.x)**:
```csharp
var properties = new BasicProperties
{
    ContentType = "application/json",
    DeliveryMode = 2,
    Headers = new Dictionary<string, object>()
};
```

**After (6.x)**:
```csharp
var properties = channel.CreateBasicProperties();
properties.ContentType = "application/json";
properties.DeliveryMode = 2;
properties.Headers = new Dictionary<string, object>();
```

**Impact on RawRabbit**:
- BasicPropertiesMiddleware must be updated
- All message publishing code must use CreateBasicProperties()

---

#### 3. AsyncEventingBasicConsumer Requires DispatchConsumersAsync

**Description**: In version 6.x, you must explicitly enable async consumer dispatch.

**Before (5.x)**:
```csharp
var consumer = new AsyncEventingBasicConsumer(channel);
consumer.Received += async (sender, args) => {
    // Handle message
};
```

**After (6.x)**:
```csharp
var factory = new ConnectionFactory
{
    DispatchConsumersAsync = true  // ← REQUIRED!
};
var connection = factory.CreateConnection();
var channel = connection.CreateModel();
var consumer = new AsyncEventingBasicConsumer(channel);
consumer.Received += async (sender, args) => {
    // Handle message
};
```

**⚠️ WARNING**: If `DispatchConsumersAsync` is not set, messages are silently dropped with no error!

**Impact on RawRabbit**:
- ConnectionFactory configuration must be updated
- ConsumerFactory implementation needs changes

---

#### 4. .NET Framework Requirements

**Description**: Version 6.0+ requires .NET Framework 4.6.1 or .NET Standard 2.0 minimum.

**Impact on RawRabbit**:
- RawRabbit already targets .NET Standard 2.0, so this is compatible

---

#### 5. Publisher Confirms Always Enabled

**Description**: Publisher confirms can no longer be disabled.

**Before (5.x)**:
```csharp
// Could disable publisher confirms
channel.ConfirmSelect();  // Optional
```

**After (6.x)**:
```csharp
// Publisher confirms always enabled when ConfirmSelect() is called
channel.ConfirmSelect();  // Always active once called
```

**Impact on RawRabbit**:
- PublishAcknowledgeMiddleware behavior remains similar
- Performance impact minimal (confirms already used in RawRabbit)

---

### Version 7.0.0 Breaking Changes

#### 1. Complete Async API: IModel → IChannel

**Description**: The entire API has been redesigned to use async/await. IModel is renamed to IChannel.

**Before (6.x)**:
```csharp
// Connection and channel creation
var factory = new ConnectionFactory();
var connection = factory.CreateConnection();
var channel = connection.CreateModel();

// Topology operations
channel.ExchangeDeclare("my-exchange", "topic", durable: true);
channel.QueueDeclare("my-queue", durable: true, exclusive: false,
    autoDelete: false, arguments: null);
channel.QueueBind("my-queue", "my-exchange", "routing.key");

// Publishing
channel.BasicPublish("my-exchange", "routing.key", false, properties, body);

// Consumer
var consumer = new AsyncEventingBasicConsumer(channel);
channel.BasicConsume("my-queue", false, consumer);
```

**After (7.x)**:
```csharp
// Connection and channel creation - ALL ASYNC
var factory = new ConnectionFactory();
var connection = await factory.CreateConnectionAsync();
var channel = await connection.CreateChannelAsync();  // ← IChannel, not IModel

// Topology operations - ALL ASYNC
await channel.ExchangeDeclareAsync("my-exchange", "topic", durable: true);
await channel.QueueDeclareAsync("my-queue", durable: true, exclusive: false,
    autoDelete: false, arguments: null);
await channel.QueueBindAsync("my-queue", "my-exchange", "routing.key");

// Publishing - ASYNC with publisher confirms
await channel.BasicPublishAsync("my-exchange", "routing.key", false,
    properties, body);

// Consumer - IAsyncBasicConsumer
var consumer = new AsyncEventingBasicConsumer(channel);
await channel.BasicConsumeAsync("my-queue", false, consumer);
```

**Impact on RawRabbit**:
- **ALL 39 files using IModel** must be updated to IChannel
- **ALL middleware** must be converted to async
- **ALL channel operations** must use async methods
- Connection factory must use CreateConnectionAsync()

---

#### 2. BasicProperties Constructor Restored

**Description**: In version 7.0, you can again directly instantiate BasicProperties.

**Before (6.x)**:
```csharp
var properties = channel.CreateBasicProperties();
```

**After (7.x)**:
```csharp
var properties = new BasicProperties();  // ← Direct construction restored
```

**Impact on RawRabbit**:
- Simplifies BasicPropertiesMiddleware
- No need for channel reference to create properties

---

#### 3. Publisher Confirms via Async API

**Description**: Publisher confirms are now integrated into the async publish API.

**Before (6.x)**:
```csharp
channel.ConfirmSelect();
channel.BasicPublish(exchange, routingKey, mandatory, properties, body);
channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
```

**After (7.x)**:
```csharp
// Enable confirms when creating channel
var channel = await connection.CreateChannelAsync(
    new CreateChannelOptions { PublisherConfirmationsEnabled = true }
);

// Publish and await confirmation
try {
    await channel.BasicPublishAsync(exchange, routingKey, mandatory,
        properties, body, cancellationToken);
    // Success - message confirmed
} catch (PublishException ex) {
    // Message was nack'd or returned
}
```

**Impact on RawRabbit**:
- PublishAcknowledgeMiddleware requires complete rewrite
- ReturnCallbackMiddleware integration needs updating
- Timeouts now handled via CancellationToken

---

#### 4. Consumer Interface: IBasicConsumer → IAsyncBasicConsumer

**Description**: All consumer interfaces are now async-only.

**Before (6.x)**:
```csharp
var consumer = new AsyncEventingBasicConsumer(channel);
consumer.Received += async (sender, args) => {
    await ProcessMessageAsync(args.Body.ToArray());
};
// DispatchConsumersAsync = true required
```

**After (7.x)**:
```csharp
var consumer = new AsyncEventingBasicConsumer(channel);
consumer.Received += async (sender, args) => {
    await ProcessMessageAsync(args.Body.ToArray());
};
// No DispatchConsumersAsync needed - all async by default
```

**Impact on RawRabbit**:
- ConsumerFactory implementation simplified
- DispatchConsumersAsync no longer needed
- All consumers are async by default

---

#### 5. Memory Lifetime Warning

**Description**: The `ReadOnlyMemory<byte>` in message delivery events is only valid during the event handler.

**Critical Code Pattern**:
```csharp
// ❌ WRONG - Memory is invalid after handler returns
consumer.Received += async (sender, args) => {
    await Task.Delay(1000);
    var data = args.Body.ToArray();  // ← May be invalid!
};

// ✅ CORRECT - Copy memory immediately
consumer.Received += async (sender, args) => {
    var data = args.Body.ToArray();  // ← Copy immediately
    await Task.Delay(1000);
    ProcessData(data);
};
```

**Impact on RawRabbit**:
- ConsumerMessageHandlerMiddleware must copy body data early
- Any async processing must work with copied data

---

#### 6. Connection Recovery Unchanged

**Description**: Connection recovery behavior remains the same, but uses async API.

**Before (6.x)**:
```csharp
var factory = new ConnectionFactory
{
    AutomaticRecoveryEnabled = true,
    NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
    TopologyRecoveryEnabled = true
};
var connection = factory.CreateConnection();
```

**After (7.x)**:
```csharp
var factory = new ConnectionFactory
{
    AutomaticRecoveryEnabled = true,  // ← Same properties
    NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
    TopologyRecoveryEnabled = true
};
var connection = await factory.CreateConnectionAsync();  // ← Async
```

**Impact on RawRabbit**:
- Configuration unchanged
- Only connection creation becomes async

---

## Impact Analysis on RawRabbit Components

### High Impact Components (Require Significant Changes)

#### 1. Channel Management (39 files)
- **Files**: All files using IModel
- **Change**: IModel → IChannel rename + async conversion
- **Effort**: High
- **Files Affected**:
  - `/src/RawRabbit/Channel/ChannelFactory.cs`
  - `/src/RawRabbit/Channel/Abstraction/IChannelFactory.cs`
  - `/src/RawRabbit/Channel/AutoScalingChannelPool.cs`
  - `/src/RawRabbit/Channel/DynamicChannelPool.cs`
  - `/src/RawRabbit/Channel/ResilientChannelPool.cs`
  - `/src/RawRabbit/Channel/StaticChannelPool.cs`
  - All middleware files

#### 2. Publishing Pipeline (27 files)
- **Files**: BasicPublishMiddleware, PublishAcknowledgeMiddleware, ReturnCallbackMiddleware
- **Change**: Async API + new publisher confirms pattern
- **Effort**: High
- **Critical Files**:
  - `/src/RawRabbit/Pipe/Middleware/BasicPublishMiddleware.cs`
  - `/src/RawRabbit.Operations.Publish/Middleware/PublishAcknowledgeMiddleware.cs`
  - `/src/RawRabbit.Operations.Publish/Middleware/ReturnCallbackMiddleware.cs`
  - `/src/RawRabbit.Enrichers.Polly/Middleware/BasicPublishMiddleware.cs`

#### 3. Consumer Pipeline (16 files)
- **Files**: ConsumerFactory, ConsumerCreationMiddleware, ConsumerConsumeMiddleware
- **Change**: Async consumers + memory handling
- **Effort**: High
- **Critical Files**:
  - `/src/RawRabbit/Consumer/ConsumerFactory.cs`
  - `/src/RawRabbit/Consumer/IConsumerFactory.cs`
  - `/src/RawRabbit/Pipe/Middleware/ConsumerCreationMiddleware.cs`
  - `/src/RawRabbit/Pipe/Middleware/ConsumerConsumeMiddleware.cs`
  - `/src/RawRabbit/Pipe/Middleware/ConsumerMessageHandlerMiddleware.cs`

#### 4. Topology Operations (27 files)
- **Files**: QueueDeclareMiddleware, ExchangeDeclareMiddleware, QueueBindMiddleware
- **Change**: All operations become async
- **Effort**: Medium
- **Critical Files**:
  - `/src/RawRabbit/Pipe/Middleware/QueueDeclareMiddleware.cs`
  - `/src/RawRabbit/Pipe/Middleware/ExchangeDeclareMiddleware.cs`
  - `/src/RawRabbit/Pipe/Middleware/QueueBindMiddleware.cs`
  - `/src/RawRabbit/Common/TopologyProvider.cs`

### Medium Impact Components

#### 5. Connection Factory (4 files)
- **Files**: ChannelFactory.cs
- **Change**: CreateConnectionAsync + configuration updates
- **Effort**: Medium
- **Files**:
  - `/src/RawRabbit/Channel/ChannelFactory.cs`

#### 6. Basic Properties (5+ files)
- **Files**: BasicPropertiesMiddleware
- **Change**: Version 6.x requires CreateBasicProperties(), 7.x allows direct construction
- **Effort**: Low (but changes twice)
- **Files**:
  - `/src/RawRabbit/Pipe/Middleware/BasicPropertiesMiddleware.cs`

### Low Impact Components

#### 7. Configuration
- **Change**: Add DispatchConsumersAsync for 6.x (removed in 7.x)
- **Effort**: Low

#### 8. Message Serialization
- **Change**: Handle ReadOnlyMemory<byte> instead of byte[]
- **Effort**: Low (implicit conversion available)

---

## Migration Strategy Recommendations

### Phased Approach

#### Phase 1: Upgrade to 6.x (Preparation)
1. Update memory handling: `byte[]` → `ReadOnlyMemory<byte>`
2. Update BasicProperties construction to use `CreateBasicProperties()`
3. Add `DispatchConsumersAsync = true` to ConnectionFactory
4. Update .NET target framework verification
5. Run full test suite

**Risk**: Medium - Memory lifetime issues can cause subtle bugs

#### Phase 2: Upgrade to 7.x (Async Transformation)
1. Rename all `IModel` to `IChannel`
2. Convert all middleware to async
3. Update connection/channel creation to async
4. Rewrite publisher confirms logic
5. Update consumer creation to async
6. Remove `CreateBasicProperties()` calls (use constructor)
7. Remove `DispatchConsumersAsync` (no longer needed)
8. Update all topology operations to async
9. Run full test suite

**Risk**: High - Entire API surface changes to async

### Alternative: Direct 5.x → 7.x Migration

**Pros**:
- Single migration effort
- Skip version 6.x quirks (CreateBasicProperties requirement)
- Cleaner final API (7.x is better designed)

**Cons**:
- Large change set increases risk
- Harder to isolate issues
- All breaking changes hit at once

**Recommendation**: Use phased approach for production systems, direct migration for new development.

---

## Code Examples: Before and After

### Example 1: Simple Publisher

**Version 5.x (Current RawRabbit)**:
```csharp
public class PublishMiddleware
{
    public Task InvokeAsync(IPipeContext context)
    {
        var channel = context.Get<IModel>(PipeKey.Channel);
        var exchange = context.Get<string>(PipeKey.ExchangeName);
        var routingKey = context.Get<string>(PipeKey.RoutingKey);
        var body = context.Get<byte[]>(PipeKey.Body);

        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = 2
        };

        channel.BasicPublish(exchange, routingKey, false, properties, body);

        return Task.CompletedTask;
    }
}
```

**Version 7.x (Target)**:
```csharp
public class PublishMiddleware
{
    public async Task InvokeAsync(IPipeContext context)
    {
        var channel = context.Get<IChannel>(PipeKey.Channel);  // ← IChannel
        var exchange = context.Get<string>(PipeKey.ExchangeName);
        var routingKey = context.Get<string>(PipeKey.RoutingKey);
        var body = context.Get<ReadOnlyMemory<byte>>(PipeKey.Body);  // ← ReadOnlyMemory

        var properties = new BasicProperties  // ← Direct construction
        {
            ContentType = "application/json",
            DeliveryMode = 2
        };

        await channel.BasicPublishAsync(exchange, routingKey, false,
            properties, body);  // ← Async
    }
}
```

---

### Example 2: Consumer with Publisher Confirms

**Version 5.x (Current RawRabbit)**:
```csharp
public class ConsumerFactory : IConsumerFactory
{
    public IBasicConsumer CreateConsumer(IModel channel, Action<byte[]> handler)
    {
        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (sender, args) =>
        {
            handler(args.Body);
            channel.BasicAck(args.DeliveryTag, false);
        };
        return consumer;
    }
}
```

**Version 7.x (Target)**:
```csharp
public class ConsumerFactory : IConsumerFactory
{
    public IAsyncBasicConsumer CreateConsumer(IChannel channel,
        Func<ReadOnlyMemory<byte>, Task> handler)  // ← Async handler
    {
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (sender, args) =>
        {
            var body = args.Body.ToArray();  // ← Copy immediately!
            await handler(new ReadOnlyMemory<byte>(body));
            await channel.BasicAckAsync(args.DeliveryTag, false);  // ← Async
        };
        return consumer;
    }
}
```

---

### Example 3: Channel Pool with Topology

**Version 5.x (Current RawRabbit)**:
```csharp
public class ChannelFactory : IChannelFactory
{
    private readonly IConnectionFactory _connectionFactory;

    public IModel CreateChannel()
    {
        var connection = _connectionFactory.CreateConnection();
        var channel = connection.CreateModel();

        channel.ExchangeDeclare("my-exchange", "topic", durable: true);
        channel.QueueDeclare("my-queue", durable: true,
            exclusive: false, autoDelete: false);
        channel.QueueBind("my-queue", "my-exchange", "#");

        return channel;
    }
}
```

**Version 7.x (Target)**:
```csharp
public class ChannelFactory : IChannelFactory
{
    private readonly IConnectionFactory _connectionFactory;

    public async Task<IChannel> CreateChannelAsync(  // ← Async
        CancellationToken ct = default)
    {
        var connection = await _connectionFactory.CreateConnectionAsync(ct);
        var channel = await connection.CreateChannelAsync(ct);  // ← IChannel

        await channel.ExchangeDeclareAsync("my-exchange", "topic",
            durable: true, cancellationToken: ct);  // ← Async
        await channel.QueueDeclareAsync("my-queue", durable: true,
            exclusive: false, autoDelete: false, cancellationToken: ct);
        await channel.QueueBindAsync("my-queue", "my-exchange", "#",
            cancellationToken: ct);

        return channel;
    }
}
```

---

## Testing Considerations

### Critical Test Scenarios

1. **Memory Lifetime Testing**
   - Verify message body is copied before async operations
   - Test that data remains valid across async boundaries
   - Ensure no memory corruption in high-throughput scenarios

2. **Publisher Confirms**
   - Test successful publish with confirmation
   - Test nack handling (PublishException)
   - Test timeout scenarios with CancellationToken
   - Test basic.return handling

3. **Consumer Async Behavior**
   - Verify async consumers process messages correctly
   - Test concurrent message processing
   - Verify error handling in async handlers
   - Test acknowledgment timing

4. **Connection Recovery**
   - Test automatic reconnection after network failure
   - Verify topology recovery
   - Test consumer recovery
   - Verify publisher state after recovery

5. **Backward Compatibility**
   - Ensure API surface remains compatible where possible
   - Test existing RawRabbit clients with new implementation

---

## Security Considerations

### CVE Fixes in Version 7.x

The primary driver for this upgrade is security:

1. **CVE-2020-11100**: Fixed in 6.0+
   - Affects RabbitMQ.Client 5.x
   - Severity: High

2. **CVE-2021-22116**: Fixed in 6.2+
   - Affects RabbitMQ.Client ≤ 6.1.x
   - Severity: Medium

**Recommendation**: Target RabbitMQ.Client 7.1.2+ (latest stable) for maximum security.

---

## Performance Implications

### Version 6.x Performance
- **Memory**: Significant reduction in allocations due to ReadOnlyMemory
- **Throughput**: 10-30% improvement in high-throughput scenarios
- **Latency**: Slightly higher due to memory pooling overhead

### Version 7.x Performance
- **Async Overhead**: Minimal - ValueTask used for hot paths
- **Publisher Confirms**: More efficient than WaitForConfirms pattern
- **Memory**: Continues 6.x improvements
- **Throughput**: Similar to 6.x, may improve with better async utilization

---

## References

### Official Documentation
- [RabbitMQ.Client v7 Migration Guide](https://github.com/rabbitmq/rabbitmq-dotnet-client/blob/main/v7-MIGRATION.md)
- [RabbitMQ.Client Releases](https://github.com/rabbitmq/rabbitmq-dotnet-client/releases)
- [.NET Client API Guide](https://www.rabbitmq.com/client-libraries/dotnet-api-guide)

### Community Resources
- [Migration Guide Discussion](https://github.com/rabbitmq/rabbitmq-dotnet-client/discussions/1720)
- [Brighter Framework Migration](https://github.com/BrighterCommand/Brighter/issues/3386)
- [NServiceBus RabbitMQ Transport Upgrade](https://docs.particular.net/transports/upgrades/rabbitmq-6to7)

### CVE Information
- CVE-2020-11100: RabbitMQ.Client < 6.0.0
- CVE-2021-22116: RabbitMQ.Client ≤ 6.1.x

---

## Next Steps

1. **Stage 3 (Core Migration)**: Update RawRabbit.csproj to RabbitMQ.Client 7.x
2. **Stage 4 (API Updates)**: Implement IModel → IChannel rename and async conversion
3. **Stage 5 (Testing)**: Comprehensive test suite execution
4. **Stage 6 (Documentation)**: Update all API documentation and samples

---

## Appendix: File Impact Matrix

| Component | Files Affected | Change Type | Effort | Priority |
|-----------|---------------|-------------|---------|----------|
| Channel Management | 39 | IModel→IChannel, Async | High | Critical |
| Publishing | 27 | Async API, Confirms | High | Critical |
| Consumers | 16 | Async, Memory | High | Critical |
| Topology | 27 | Async API | Medium | High |
| Connection | 4 | Async Creation | Medium | High |
| Properties | 5 | Constructor | Low | Medium |
| Configuration | 2 | Options | Low | Medium |
| Serialization | 10 | ReadOnlyMemory | Low | High |

**Total Estimated Files**: 130+ files requiring changes

**Estimated Effort**:
- Version 6.x migration: 40-60 hours
- Version 7.x migration: 80-120 hours
- Total: 120-180 hours (3-4 weeks full-time)

---

**Document Version**: 1.0
**Last Updated**: 2025-10-09
**Author**: Claude (Research Agent)
**Review Status**: Ready for Stage 3 Planning
