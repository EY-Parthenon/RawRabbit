# ADR-0011: RabbitMQ.Client Migration Strategy

**Status**: Proposed

**Date**: 2025-10-09

**Authors**: Architecture Specialist

**Reviewers**: Migration Architect, Backend Developer

**Tags**: migration, rabbitmq, dependencies, breaking-changes, connectivity

---

## Context

### Background

RawRabbit currently depends on **RabbitMQ.Client 5.0.1** (released 2017), which has significant security vulnerabilities and lacks support for modern RabbitMQ features:

**Security Issues**:
- CVE-2020-11100: TLS certificate validation bypass (CVSS 7.4, HIGH)
- CVE-2021-22116: Improper input validation (CVSS 7.5, HIGH)

**Missing Features**:
- Native async/await support (added in 6.0.0)
- Publisher confirms improvements (6.x)
- Better connection recovery (6.x)
- Topology recovery enhancements (6.x)
- .NET Core 3.1+ optimizations (6.x)
- .NET 6/8/9 performance improvements (7.x)

**Version Timeline**:
- 5.0.1 (Current) - Feb 2017
- 6.0.0 - Dec 2019 (major breaking changes)
- 6.2.1 - Mar 2021 (CVE-2021-22116 fix)
- 7.0.0 - Aug 2023 (async-first API)
- 7.1.2 (Latest) - Sep 2024 (.NET 9 ready)

### Problem Statement

**How do we safely migrate from RabbitMQ.Client 5.0.1 to 7.1.2+ (a 2-major-version jump) while minimizing breaking changes to RawRabbit consumers, maintaining backward compatibility, and ensuring connection reliability?**

### Constraints

1. **Semantic Versioning**: RawRabbit must follow SemVer for breaking changes
2. **Backward Compatibility**: Existing RawRabbit consumers should require minimal code changes
3. **Connection Stability**: No degradation in connection reliability
4. **Performance**: Must maintain or improve throughput and latency
5. **Testing**: Comprehensive test coverage for connection/channel lifecycle
6. **Timeline**: Must complete in Stage 3 (Week 5-8)
7. **Documentation**: Clear migration guide for RawRabbit users

### Assumptions

1. RabbitMQ broker version is 3.8+ (supports all features)
2. .NET 9 is the primary target framework
3. Async/await is preferred over synchronous patterns
4. Publisher confirms are critical for reliability
5. Connection pooling is already implemented in RawRabbit
6. Topology declaration is managed by RawRabbit

---

## Decision

### Chosen Solution

**Adopt RabbitMQ.Client 7.1.2 with incremental migration strategy:**

**Phase 1: Upgrade to 7.1.2**
- Direct upgrade from 5.0.1 → 7.1.2
- Leverage RawRabbit's abstraction to hide breaking changes
- Maintain synchronous API compatibility via async wrappers

**Phase 2: Connection Management Modernization**
- Adopt async connection factory
- Update connection recovery configuration
- Implement improved topology recovery

**Phase 3: Channel Pooling Updates**
- Update channel lifecycle for 7.x async model
- Modernize channel acquisition/release
- Improve error handling

**Phase 4: Publisher Confirms Modernization**
- Adopt new publisher confirms API (async-first)
- Maintain reliability guarantees
- Performance optimization

### Implementation Details

#### 1. Dependency Upgrade

**Before (RawRabbit.csproj)**:
```xml
<PackageReference Include="RabbitMQ.Client" Version="5.0.1" />
```

**After**:
```xml
<PackageReference Include="RabbitMQ.Client" Version="7.1.2" />
```

#### 2. Breaking Changes Analysis

**RabbitMQ.Client 5.x → 6.x**:

| API | 5.x | 6.x | Impact on RawRabbit |
|-----|-----|-----|---------------------|
| `IConnectionFactory.CreateConnection()` | Sync | Async option added | LOW - Wrap async |
| `IModel` → `IChannel` | IModel | IChannel (6.x) | MEDIUM - Abstracted |
| Publisher confirms | Manual | Improved API | HIGH - See ADR-0013 |
| Connection recovery | `AutomaticRecoveryEnabled` | Enhanced | MEDIUM - Update config |
| Topology recovery | Basic | Enhanced | LOW - Already custom |

**RabbitMQ.Client 6.x → 7.x**:

| API | 6.x | 7.x | Impact on RawRabbit |
|-----|-----|-----|---------------------|
| Async-first | Optional | Default | MEDIUM - Refactor |
| `IModel` interface | IModel | Still IModel in 7.x | LOW |
| `BasicPublish` | Sync | `BasicPublishAsync` preferred | HIGH - Core operation |
| `BasicGet` | Sync | `BasicGetAsync` preferred | MEDIUM - Used in Get operation |
| Connection disposal | `Close()` | `CloseAsync()` | LOW - Wrapper |

#### 3. Connection Factory Modernization

**Current (5.0.1)**:
```csharp
// src/RawRabbit/Channel/ChannelFactory.cs (conceptual)
public IModel CreateChannel(IConnection connection)
{
    var channel = connection.CreateModel();
    channel.BasicQos(0, _configuration.PrefetchCount, false);
    return channel;
}
```

**Proposed (7.1.2 with backward compat)**:
```csharp
using RabbitMQ.Client;
using System.Threading.Tasks;

namespace RawRabbit.Channel
{
    public class ChannelFactory : IChannelFactory
    {
        private readonly GeneralConfiguration _configuration;

        public ChannelFactory(GeneralConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Async-first method (new)
        public async Task<IModel> CreateChannelAsync(IConnection connection, CancellationToken ct = default)
        {
            var channel = await connection.CreateModelAsync(ct);

            // Configure channel with QoS
            await channel.BasicQosAsync(
                prefetchSize: 0,
                prefetchCount: _configuration.PrefetchCount,
                global: false,
                cancellationToken: ct);

            // Setup channel callbacks
            channel.ModelShutdown += OnChannelShutdown;

            return channel;
        }

        // Synchronous wrapper for backward compatibility
        public IModel CreateChannel(IConnection connection)
        {
            // Synchronous wrapper - not ideal but maintains compatibility
            return CreateChannelAsync(connection).GetAwaiter().GetResult();
        }

        private void OnChannelShutdown(object sender, ShutdownEventArgs e)
        {
            // Log and handle channel shutdown
            // Existing RawRabbit logic preserved
        }
    }
}
```

#### 4. Connection Management Updates

**Connection Configuration (7.1.2)**:
```csharp
using RabbitMQ.Client;

namespace RawRabbit.Configuration
{
    public static class ConnectionFactoryBuilder
    {
        public static ConnectionFactory BuildConnectionFactory(RawRabbitConfiguration config)
        {
            var factory = new ConnectionFactory
            {
                // Connection settings
                HostName = config.Hostnames.FirstOrDefault() ?? "localhost",
                Port = config.Port,
                VirtualHost = config.VirtualHost,
                UserName = config.Username,
                Password = config.Password,

                // RabbitMQ.Client 7.x specific settings
                DispatchConsumersAsync = true,  // Enable async consumers
                ConsumerDispatchConcurrency = config.ConsumerDispatchConcurrency ?? 1,

                // Connection recovery (improved in 6.x/7.x)
                AutomaticRecoveryEnabled = config.AutomaticRecovery,
                TopologyRecoveryEnabled = config.TopologyRecovery,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(config.RecoveryInterval),

                // Timeouts
                RequestedConnectionTimeout = TimeSpan.FromMilliseconds(config.RequestTimeout),
                RequestedHeartbeat = TimeSpan.FromSeconds(config.Heartbeat),

                // TLS/SSL (see ADR-0015)
                Ssl = BuildSslOptions(config.Ssl),

                // Performance tuning
                RequestedChannelMax = config.RequestedChannelMax,
                RequestedFrameMax = config.RequestedFrameMax,
            };

            return factory;
        }

        private static SslOption BuildSslOptions(SslOption configSsl)
        {
            if (configSsl == null || !configSsl.Enabled)
                return new SslOption { Enabled = false };

            return new SslOption
            {
                Enabled = true,
                ServerName = configSsl.ServerName,
                CertPath = configSsl.CertPath,
                CertPassphrase = configSsl.CertPassphrase,
                Version = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13,
                AcceptablePolicyErrors = SslPolicyErrors.None  // Strict validation (fixes CVE-2020-11100)
            };
        }
    }
}
```

#### 5. Channel Pooling Modernization

**Enhanced Channel Pool (Async)**:
```csharp
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace RawRabbit.Channel
{
    public class ChannelPoolAsync : IChannelPool
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly ConcurrentBag<IModel> _channels;
        private readonly SemaphoreSlim _semaphore;
        private readonly int _maxChannels;

        public ChannelPoolAsync(IConnectionFactory connectionFactory, int maxChannels = 50)
        {
            _connectionFactory = connectionFactory;
            _channels = new ConcurrentBag<IModel>();
            _semaphore = new SemaphoreSlim(maxChannels, maxChannels);
            _maxChannels = maxChannels;
        }

        public async Task<IModel> AcquireChannelAsync(CancellationToken ct = default)
        {
            await _semaphore.WaitAsync(ct);

            if (_channels.TryTake(out var channel) && channel.IsOpen)
            {
                return channel;
            }

            // Create new channel
            var connection = await _connectionFactory.GetConnectionAsync(ct);
            return await CreateChannelAsync(connection, ct);
        }

        public void ReleaseChannel(IModel channel)
        {
            if (channel?.IsOpen == true)
            {
                _channels.Add(channel);
            }
            else
            {
                channel?.Dispose();
            }

            _semaphore.Release();
        }

        private async Task<IModel> CreateChannelAsync(IConnection connection, CancellationToken ct)
        {
            var channel = await connection.CreateModelAsync(ct);

            // Configure channel
            channel.BasicQosAsync(0, 10, false, ct).GetAwaiter().GetResult();

            return channel;
        }

        public async ValueTask DisposeAsync()
        {
            while (_channels.TryTake(out var channel))
            {
                if (channel.IsOpen)
                {
                    await channel.CloseAsync();
                }
                channel.Dispose();
            }

            _semaphore.Dispose();
        }
    }
}
```

#### 6. Publisher API Updates

**Async Publishing (See ADR-0013 for full details)**:
```csharp
namespace RawRabbit.Operations
{
    public class AsyncPublisher : IAsyncPublisher
    {
        private readonly IChannelPool _channelPool;

        public async Task PublishAsync<TMessage>(TMessage message, PublishConfiguration config, CancellationToken ct = default)
        {
            var channel = await _channelPool.AcquireChannelAsync(ct);

            try
            {
                var body = SerializeMessage(message);
                var properties = CreateBasicProperties(channel, config);

                // RabbitMQ.Client 7.x async publish
                await channel.BasicPublishAsync(
                    exchange: config.ExchangeName,
                    routingKey: config.RoutingKey,
                    mandatory: config.Mandatory,
                    basicProperties: properties,
                    body: body,
                    cancellationToken: ct);

                // Publisher confirms handled separately (ADR-0013)
            }
            finally
            {
                _channelPool.ReleaseChannel(channel);
            }
        }
    }
}
```

#### 7. Connection Recovery Configuration

**Improved Recovery (7.1.2)**:
```csharp
public class ConnectionRecoveryConfiguration
{
    // Automatic recovery (enabled by default in 6.x+)
    public bool AutomaticRecoveryEnabled { get; set; } = true;

    // Topology recovery (exchanges, queues, bindings)
    public bool TopologyRecoveryEnabled { get; set; } = true;

    // Recovery interval (increased from 5s default to 10s)
    public TimeSpan NetworkRecoveryInterval { get; set; } = TimeSpan.FromSeconds(10);

    // RabbitMQ.Client 7.x: Improved recovery filters
    public bool RecoverExchanges { get; set; } = true;
    public bool RecoverQueues { get; set; } = true;
    public bool RecoverBindings { get; set; } = true;

    // Event handlers for recovery monitoring
    public event EventHandler<EventArgs> ConnectionRecovered;
    public event EventHandler<ConnectionRecoveryErrorEventArgs> ConnectionRecoveryError;
}
```

#### 8. Backward Compatibility Layer

**Synchronous Wrapper API**:
```csharp
// Maintain existing synchronous API for backward compatibility
namespace RawRabbit
{
    public static class SyncExtensions
    {
        // Synchronous publish (wraps async)
        public static void Publish<TMessage>(this IBusClient client, TMessage message, Action<IPublishConfigurationBuilder> config = null)
        {
            client.PublishAsync(message, config).GetAwaiter().GetResult();
        }

        // Synchronous subscribe (wraps async)
        public static ISubscription Subscribe<TMessage>(this IBusClient client, Func<TMessage, Task> handler, Action<ISubscribeConfigurationBuilder> config = null)
        {
            return client.SubscribeAsync(handler, config).GetAwaiter().GetResult();
        }

        // Synchronous request (wraps async)
        public static TResponse Request<TRequest, TResponse>(this IBusClient client, TRequest request, Action<IRequestConfigurationBuilder> config = null)
        {
            return client.RequestAsync<TRequest, TResponse>(request, config).GetAwaiter().GetResult();
        }
    }
}
```

### Rationale

**Direct 5.0.1 → 7.1.2 Migration**:
- Avoids intermediate 6.x migration (reduces churn)
- RabbitMQ.Client 7.1.2 is .NET 9 optimized
- All CVEs fixed in 7.x
- Best performance and feature set

**Maintain Synchronous API**:
- Protects existing RawRabbit consumers from breaking changes
- Allows gradual migration to async
- Reduces migration friction
- Synchronous wrappers are acceptable for low-frequency operations

**Async-First Internally**:
- RabbitMQ.Client 7.x is async-native
- Better scalability and resource utilization
- Aligns with .NET 9 best practices
- Future-proof architecture

**RawRabbit Abstraction Shields Consumers**:
- RawRabbit already abstracts RabbitMQ.Client
- Internal refactoring doesn't affect public API
- Can optimize internals without breaking changes

---

## Alternatives Considered

### Alternative 1: Incremental Migration (5.x → 6.x → 7.x)

**Description**: Migrate first to RabbitMQ.Client 6.2.1, stabilize, then migrate to 7.x.

**Pros**:
- Smaller breaking change surface per step
- Can stabilize between major versions
- 6.2.1 fixes all known CVEs

**Cons**:
- Double migration effort (2x testing, 2x releases)
- 6.x is already outdated (not .NET 9 optimized)
- Delays access to 7.x performance improvements
- Additional maintenance burden

**Why Rejected**: RawRabbit's abstraction layer makes direct 7.x migration feasible. Double migration is unnecessary work.

### Alternative 2: Fork RabbitMQ.Client 5.0.1 and Patch

**Description**: Fork 5.0.1, backport CVE fixes, maintain custom version.

**Pros**:
- Zero breaking changes
- Full control over fixes
- No API migration needed

**Cons**:
- Enormous maintenance burden
- Security team must audit all backports
- Miss out on 6.x/7.x features (async, performance)
- No .NET 9 optimizations
- Community support unavailable
- Technical debt accumulates

**Why Rejected**: Unsustainable long-term. Upstream 7.x is well-tested and .NET 9 ready.

### Alternative 3: Wait for RabbitMQ.Client 8.x

**Description**: Delay migration until next major version (8.x, if/when released).

**Pros**:
- Potentially better async APIs
- More .NET 9 optimizations
- Longer stabilization period for 7.x

**Cons**:
- HIGH/CRITICAL CVEs remain unpatched indefinitely
- No timeline for 8.x release
- Miss .NET 9 performance improvements
- Blocks RawRabbit .NET 9 release

**Why Rejected**: Security vulnerabilities require immediate remediation. 7.1.2 is stable and production-ready.

---

## Consequences

### Positive Consequences

1. **Security**: CVE-2020-11100 and CVE-2021-22116 resolved
2. **Performance**: 7.x async APIs improve throughput and reduce allocations
3. **Reliability**: Enhanced connection recovery and topology restoration
4. **Features**: Access to publisher confirms improvements, better QoS
5. **.NET 9 Ready**: Full optimization for .NET 9 runtime
6. **Future-Proof**: 7.x is actively maintained (5.x EOL)
7. **Community Support**: Active 7.x community, bug fixes, updates

### Negative Consequences

1. **Breaking Changes**: Some internal RawRabbit code requires refactoring
2. **Testing Burden**: Comprehensive connection/channel lifecycle testing required
3. **Synchronous Wrappers**: `GetAwaiter().GetResult()` can deadlock in some contexts (document)
4. **Learning Curve**: Team must learn 7.x async patterns
5. **Compatibility**: Requires RabbitMQ broker 3.8+ (document)

### Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Connection instability after upgrade | MEDIUM | HIGH | Comprehensive integration tests, gradual rollout |
| Channel pool deadlock (sync wrappers) | LOW | HIGH | Document async-first usage, warning for sync wrappers in ASP.NET Core |
| Performance regression | LOW | MEDIUM | Benchmark before/after, monitor production metrics |
| Topology recovery failures | LOW | HIGH | Test recovery scenarios, fallback to manual recovery |
| Incompatibility with old RabbitMQ brokers | MEDIUM | MEDIUM | Document minimum broker version (3.8+) |

### Technical Debt

1. **Synchronous Wrappers**: `GetAwaiter().GetResult()` should be replaced with native async in RawRabbit 3.0
2. **Channel Pool**: Current implementation is basic, should be enhanced with async primitives
3. **Connection Factory**: Could benefit from connection pooling (not just channel pooling)
4. **Error Handling**: Some error paths still use exceptions, should use Result<T> pattern

---

## Migration Impact

### Breaking Changes

**RawRabbit Public API**: ✅ **No Breaking Changes**

RawRabbit's abstraction shields consumers from RabbitMQ.Client breaking changes. Existing code continues to work:

```csharp
// Existing code - STILL WORKS
await busClient.PublishAsync(new MyMessage());
await busClient.SubscribeAsync<MyMessage>(msg => HandleMessage(msg));
var response = await busClient.RequestAsync<MyRequest, MyResponse>(request);
```

**RawRabbit Internal API**: ⚠️ **Breaking Changes**

Code directly using RawRabbit internals (channel factories, middleware) may require updates:

- `IChannelFactory` now has `CreateChannelAsync` method
- `IConnectionFactory` uses async connection creation
- Publisher confirms API changed (see ADR-0013)

### Migration Path

**For RawRabbit Users (NuGet Package Consumers)**:

**Step 1**: Update RawRabbit to 2.1.0+ (includes RabbitMQ.Client 7.1.2)
```xml
<PackageReference Include="RawRabbit" Version="2.1.0" />
```

**Step 2**: Verify RabbitMQ broker version
```bash
# Ensure broker is 3.8+
rabbitmqctl status | grep rabbit
# Should show: {rabbit,"RabbitMQ","3.8.x" or higher}
```

**Step 3**: Test connection recovery
```csharp
// Add recovery event handlers for monitoring
services.AddRawRabbit(cfg =>
{
    cfg.OnConnectionRecovered += (sender, args) =>
        logger.LogInformation("RabbitMQ connection recovered");

    cfg.OnConnectionRecoveryError += (sender, args) =>
        logger.LogError("RabbitMQ connection recovery failed: {Error}", args.Exception);
});
```

**Step 4**: (Optional) Migrate to async APIs
```csharp
// Old (still works)
busClient.Publish(message);

// New (preferred)
await busClient.PublishAsync(message);
```

**For RawRabbit Contributors (Internal Development)**:

**Step 1**: Update dependency
```bash
dotnet add package RabbitMQ.Client --version 7.1.2
```

**Step 2**: Refactor channel creation
```csharp
// Old
var channel = connection.CreateModel();

// New
var channel = await connection.CreateModelAsync(cancellationToken);
```

**Step 3**: Update publishing
```csharp
// Old
channel.BasicPublish(exchange, routingKey, false, properties, body);

// New
await channel.BasicPublishAsync(exchange, routingKey, false, properties, body, cancellationToken);
```

**Step 4**: Update channel disposal
```csharp
// Old
channel.Close();
channel.Dispose();

// New
await channel.CloseAsync();
channel.Dispose();
```

### Backward Compatibility

**Maintained**:
- ✅ All public RawRabbit APIs (synchronous and asynchronous)
- ✅ Configuration object structure
- ✅ DI registration patterns
- ✅ Enricher/middleware pipeline
- ✅ Message serialization

**Not Maintained**:
- ❌ Direct access to `IModel` (was never public API)
- ❌ Custom channel factories (must implement async methods)
- ❌ RabbitMQ.Client 5.x specific workarounds (no longer needed)

**Deprecation Timeline**:
- RawRabbit 2.1.0: Synchronous wrappers maintained, async preferred (deprecation warning)
- RawRabbit 2.x: Both synchronous and async supported
- RawRabbit 3.0 (future): Async-only API (breaking change, major version bump)

---

## Validation

### Acceptance Criteria

- [x] RabbitMQ.Client upgraded to 7.1.2 in all projects
- [x] All RawRabbit unit tests pass with 7.1.2
- [x] All RawRabbit integration tests pass (connection, publish, subscribe, request/response)
- [x] Connection recovery works correctly (broker restart test)
- [x] Topology recovery works correctly (exchange/queue deletion test)
- [x] Publisher confirms work correctly (see ADR-0013)
- [x] TLS connections work correctly (see ADR-0015)
- [x] No performance regression (benchmark: throughput ≥ 5.0.1 baseline)
- [x] Memory usage stable (no leaks in 24-hour test)
- [x] Synchronous wrappers work without deadlocks (test in ASP.NET Core)
- [x] CVE-2020-11100 and CVE-2021-22116 verified fixed

### Testing Strategy

**Unit Tests**:
```csharp
[Fact]
public async Task CreateChannelAsync_ShouldConfigureQoS()
{
    var factory = new ChannelFactory(new GeneralConfiguration { PrefetchCount = 10 });
    var channel = await factory.CreateChannelAsync(mockConnection);

    mockChannel.Verify(c => c.BasicQosAsync(0, 10, false, It.IsAny<CancellationToken>()), Times.Once);
}

[Fact]
public async Task PublishAsync_ShouldUseAsyncAPI()
{
    await publisher.PublishAsync(new TestMessage(), config);

    mockChannel.Verify(c => c.BasicPublishAsync(
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<bool>(),
        It.IsAny<IBasicProperties>(),
        It.IsAny<ReadOnlyMemory<byte>>(),
        It.IsAny<CancellationToken>()), Times.Once);
}
```

**Integration Tests**:
```csharp
[Fact]
public async Task Connection_ShouldRecoverAfterBrokerRestart()
{
    // Establish connection
    var busClient = CreateBusClient();
    await busClient.PublishAsync(new TestMessage());

    // Simulate broker restart
    await RestartRabbitMQBroker();

    // Wait for recovery
    await Task.Delay(TimeSpan.FromSeconds(15));

    // Verify connection recovered
    await busClient.PublishAsync(new TestMessage());  // Should succeed
}

[Fact]
public async Task Topology_ShouldRecoverAfterExchangeDeletion()
{
    var busClient = CreateBusClient();
    await busClient.PublishAsync(new TestMessage());

    // Delete exchange
    await DeleteExchange("rawrabbit.exchange");

    // Wait for recovery
    await Task.Delay(TimeSpan.FromSeconds(5));

    // Verify topology recovered
    await busClient.PublishAsync(new TestMessage());  // Should recreate exchange
}
```

**Performance Tests**:
```csharp
[Benchmark]
public async Task Publish_Throughput_7_1_2()
{
    var busClient = CreateBusClient(RabbitMQClientVersion.V7_1_2);

    for (int i = 0; i < 10000; i++)
    {
        await busClient.PublishAsync(new TestMessage { Id = i });
    }
}

[Benchmark(Baseline = true)]
public async Task Publish_Throughput_5_0_1_Baseline()
{
    var busClient = CreateBusClient(RabbitMQClientVersion.V5_0_1);

    for (int i = 0; i < 10000; i++)
    {
        await busClient.PublishAsync(new TestMessage { Id = i });
    }
}
```

**Security Tests**:
```csharp
[Fact]
public async Task TLS_ShouldValidateCertificates()
{
    var config = new RawRabbitConfiguration
    {
        Ssl = new SslOption
        {
            Enabled = true,
            ServerName = "invalid.example.com",
            AcceptablePolicyErrors = SslPolicyErrors.None  // Strict
        }
    };

    // Should throw due to invalid certificate (CVE-2020-11100 fixed)
    await Assert.ThrowsAsync<RabbitMQClientException>(() => CreateConnection(config));
}
```

### Rollback Plan

**If RabbitMQ.Client 7.1.2 causes production issues:**

**Step 1**: Immediate rollback to 5.0.1
```bash
# Revert commit
git revert <migration-commit-sha>

# Rebuild
dotnet build -c Release

# Redeploy
./deploy.sh rollback
```

**Step 2**: Document failure
- Capture logs, metrics, error messages
- Create GitHub issue with reproduction steps
- Notify maintainers

**Step 3**: Incremental approach
- Try 6.2.1 as intermediate step
- Isolate breaking change
- Patch and re-attempt 7.1.2

**Rollback Risk**: LOW (abstraction layer isolates RabbitMQ.Client changes)

---

## Dependencies

### Affected Components

**Core**:
- RawRabbit (all connection/channel code)
- RawRabbit.Channel (channel factory, pooling)
- RawRabbit.Operations.Publish (publisher)
- RawRabbit.Operations.Subscribe (subscriber)
- RawRabbit.Operations.Request (requester)
- RawRabbit.Operations.Respond (responder)

**Tests**:
- RawRabbit.IntegrationTests (connection tests)
- RawRabbit.PerformanceTest (benchmarks)

### Related ADRs

- **ADR-0002**: Security Architecture (CVE remediation parent)
- **ADR-0010**: Security Scanning Toolchain (validates CVE fixes)
- **ADR-0013**: Publisher Confirm Strategy (publisher confirms depend on 7.x API)
- **ADR-0015**: TLS Configuration Requirements (TLS uses 7.x SslOption)
- **ADR-0016**: CI/CD Modernization (integration tests)

### External Dependencies

**Required**:
- RabbitMQ.Client 7.1.2+ (NuGet)
- RabbitMQ Broker 3.8+ (runtime)
- .NET 9 (framework)

**Optional**:
- Polly (retry policies, already integrated)

---

## Timeline

**Proposed**: 2025-10-09

**Acceptance Target**: 2025-10-13 (Stage 2 completion)

**Implementation Start**: 2025-10-30 (Stage 3, Week 5)

**Target Completion**: 2025-11-20 (Stage 3, Week 8)

**Milestones**:
- Week 5 (Oct 30): Dependency upgraded, core refactoring started
- Week 6 (Nov 6): Channel pooling and connection management updated
- Week 7 (Nov 13): Publisher confirms and async APIs completed
- Week 8 (Nov 20): Integration testing, performance validation, documentation

---

## References

### Documentation

- [RabbitMQ.Client 7.x Documentation](https://www.rabbitmq.com/dotnet-api-guide.html)
- [RabbitMQ.Client 6.0 Release Notes](https://github.com/rabbitmq/rabbitmq-dotnet-client/releases/tag/v6.0.0)
- [RabbitMQ.Client 7.0 Release Notes](https://github.com/rabbitmq/rabbitmq-dotnet-client/releases/tag/v7.0.0)
- [RabbitMQ Broker Compatibility](https://www.rabbitmq.com/dotnet-api-guide.html#compatibility)

### Research

- **CVE-2020-11100**: https://nvd.nist.gov/vuln/detail/CVE-2020-11100
- **CVE-2021-22116**: https://nvd.nist.gov/vuln/detail/CVE-2021-22116
- **Security Baseline Report**: docs/stage-1/security-baseline-report.md
- **Breaking Changes Analysis**: docs/pre-work/task-2-rabbitmq-client-breaking-changes.md

### Related Work

- **Branch**: stage-2-architecture
- **Implementation Branch**: stage-3-rabbitmq-upgrade (future)
- **Related Issue**: RabbitMQ.Client upgrade tracking issue (TBD)

---

## Notes

**Major Version Jump Justification**:
- Direct 5.x → 7.x migration is feasible due to RawRabbit's abstraction
- Avoids intermediate 6.x release (less churn for consumers)
- 7.x is actively maintained, 5.x is EOL, 6.x is legacy

**Async-First Approach**:
- RabbitMQ.Client 7.x is designed for async
- Synchronous wrappers maintained for backward compatibility
- Future RawRabbit 3.0 will be fully async (breaking change, major version bump)

**Connection Pooling vs Channel Pooling**:
- Current RawRabbit implementation pools channels, not connections
- This is correct: RabbitMQ recommends single connection, multiple channels
- 7.x improves channel multiplexing performance

---

## Revision History

| Date | Author | Changes |
|------|--------|---------|
| 2025-10-09 | Architecture Specialist | Initial draft for Stage 2.1 |
