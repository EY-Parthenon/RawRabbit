# RawRabbit .NET 9 Migration Test Strategy

**Stage**: 2.3 - Test Strategy Design
**Date**: 2025-10-09
**Author**: QA Engineer
**Session ID**: dotnet9-upgrade
**Branch**: stage-2-architecture
**Status**: Complete

---

## Executive Summary

This document defines the comprehensive testing strategy for the RawRabbit .NET 9 migration project. The strategy encompasses unit testing, integration testing, performance validation, regression detection, and security testing across all 32 projects in 6 migration phases.

### Testing Approach Overview

**Test-Driven Migration Strategy**:
1. Establish baseline metrics from .NET Standard 1.5 / .NET Framework 4.5.1
2. Migrate incrementally with continuous test validation
3. Detect regressions early with automated threshold checks
4. Validate compatibility with RabbitMQ 3.11.x and 3.12.x
5. Ensure security improvements through CVE remediation validation

### Coverage Targets Per Component

| Component | Coverage Target | Priority | Test Types |
|-----------|----------------|----------|------------|
| RawRabbit (Core) | 80%+ | CRITICAL | Unit, Integration, Performance |
| Operations.* | 70%+ | HIGH | Unit, Integration, Performance |
| Enrichers.* | 60%+ | MEDIUM | Unit, Integration |
| DependencyInjection.* | 50%+ | MEDIUM | Unit, Integration |
| Compatibility.Legacy | 60%+ | LOW | Unit |
| Overall Project | 75%+ | - | All types |

### Regression Testing Strategy

**Automated Regression Detection**:
- **BLOCKER Thresholds**: Stop migration if exceeded
  - Mean execution time: +20%
  - P95 latency: +25%
  - Throughput: -15%
- **WARNING Thresholds**: Review and document if exceeded
  - Memory allocations: +30%
  - Gen2 collections: +50%
  - GC pause time: +40%

### Integration Test Infrastructure

**Docker-Based RabbitMQ Environment**:
- RabbitMQ 3.11.x (LTS) and 3.12.x (latest)
- Automated test environment provisioning
- Cleanup and teardown scripts
- Multi-node cluster support for advanced scenarios
- SSL/TLS certificate management

---

## 1. Unit Testing Strategy

### 1.1 xUnit Framework Configuration

**Framework**: xUnit 2.6.2+ (.NET 9 compatible)

**Project Structure**:
```
test/
├── RawRabbit.Tests/                    # Core library tests
├── RawRabbit.Operations.Tests/         # Operations tests
├── RawRabbit.Enrichers.Tests/          # Enrichers tests
├── RawRabbit.DI.Tests/                 # DI adapter tests
└── RawRabbit.Compatibility.Tests/      # Compatibility tests
```

**Test Project Configuration** (`.csproj`):
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net6.0;net8.0;net9.0</TargetFrameworks>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit" Version="2.6.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.4" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="Moq" Version="4.20.70" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="coverlet.collector" Version="6.0.0" />
  </ItemGroup>
</Project>
```

**xUnit Best Practices**:
1. Use `[Fact]` for deterministic tests
2. Use `[Theory]` with `[InlineData]` for parameterized tests
3. Use `ITestOutputHelper` for test diagnostics
4. Use `IClassFixture<T>` for shared test context
5. Use `ICollectionFixture<T>` for shared resources across test classes

### 1.2 Test Data Management

**Test Data Strategy**:

**1. In-Memory Test Data**:
```csharp
public static class TestData
{
    public static RawRabbitConfiguration GetTestConfiguration() => new()
    {
        VirtualHost = "/",
        Username = "test-user",
        Password = "test-password",
        Port = 5672,
        Hostnames = new List<string> { "localhost" },
        Timeout = TimeSpan.FromSeconds(5)
    };

    public static MessageContext GetTestMessageContext() => new()
    {
        GlobalMessageId = Guid.NewGuid(),
        ExecutionId = Guid.NewGuid()
    };
}
```

**2. Builder Pattern for Complex Objects**:
```csharp
public class ConfigurationBuilder
{
    private string _username = "test";
    private string _password = "test";
    private int _port = 5672;

    public ConfigurationBuilder WithUsername(string username)
    {
        _username = username;
        return this;
    }

    public ConfigurationBuilder WithPort(int port)
    {
        _port = port;
        return this;
    }

    public RawRabbitConfiguration Build() => new()
    {
        Username = _username,
        Password = _password,
        Port = _port,
        Hostnames = new List<string> { "localhost" }
    };
}
```

**3. AutoFixture for Random Data**:
```csharp
using AutoFixture;

public class MessageTests
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void Message_ShouldSerialize_WithRandomData()
    {
        var message = _fixture.Create<TestMessage>();
        // Test with randomized data
    }
}
```

### 1.3 Mocking Strategy

**Framework**: Moq 4.20.70 (primary), NSubstitute 5.1.0 (alternative)

**Mocking Principles**:
1. Mock external dependencies only (RabbitMQ.Client, I/O, network)
2. Do NOT mock internal domain logic
3. Use strict mocks for critical paths
4. Use loose mocks for logging/telemetry
5. Verify important interactions

**Example: Mocking RabbitMQ IModel**:
```csharp
using Moq;
using RabbitMQ.Client;

public class ChannelFactoryTests
{
    private readonly Mock<IConnection> _connectionMock;
    private readonly Mock<IModel> _channelMock;

    public ChannelFactoryTests()
    {
        _connectionMock = new Mock<IConnection>();
        _channelMock = new Mock<IModel>(MockBehavior.Strict);

        _connectionMock
            .Setup(c => c.CreateModel())
            .Returns(_channelMock.Object);
    }

    [Fact]
    public void CreateChannel_ShouldConfigureBasicQos()
    {
        // Arrange
        _channelMock
            .Setup(m => m.BasicQos(0, 50, false))
            .Verifiable();

        // Act
        var factory = new ChannelFactory(_connectionMock.Object);
        factory.CreateChannel();

        // Assert
        _channelMock.Verify(m => m.BasicQos(0, 50, false), Times.Once);
    }
}
```

**Example: Mocking Async Operations**:
```csharp
[Fact]
public async Task PublishAsync_ShouldInvokeBasicPublish()
{
    // Arrange
    var messageMock = new Mock<IBasicMessage>();
    _channelMock
        .Setup(m => m.BasicPublish(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<IBasicProperties>(),
            It.IsAny<ReadOnlyMemory<byte>>()))
        .Verifiable();

    // Act
    await publisher.PublishAsync(messageMock.Object);

    // Assert
    _channelMock.Verify();
}
```

### 1.4 Coverage Measurement

**Tool**: Coverlet 6.0.0 (integrated with dotnet test)

**Execution**:
```bash
# Generate coverage report
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

# Generate HTML report with ReportGenerator
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator \
    -reports:"./coverage/**/coverage.cobertura.xml" \
    -targetdir:"./coverage/report" \
    -reporttypes:"Html;Badges"
```

**Coverage Configuration** (`coverlet.runsettings`):
```xml
<?xml version="1.0" encoding="utf-8" ?>
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat Code Coverage">
        <Configuration>
          <Format>cobertura,json,opencover</Format>
          <Exclude>[*.Tests]*,[*.Samples]*</Exclude>
          <ExcludeByAttribute>Obsolete,GeneratedCode,CompilerGenerated</ExcludeByAttribute>
          <ExcludeByFile>**/Migrations/**/*.cs</ExcludeByFile>
          <IncludeDirectory>../src</IncludeDirectory>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

**Coverage Enforcement** (CI/CD):
```yaml
# .github/workflows/test.yml
- name: Test with coverage
  run: dotnet test --configuration Release --collect:"XPlat Code Coverage" --settings coverlet.runsettings

- name: Check coverage thresholds
  run: |
    COVERAGE=$(grep -oP 'line-rate="\K[0-9.]+' coverage/**/coverage.cobertura.xml | head -1)
    COVERAGE_PCT=$(echo "$COVERAGE * 100" | bc)
    if (( $(echo "$COVERAGE_PCT < 75" | bc -l) )); then
      echo "Coverage $COVERAGE_PCT% is below 75% threshold"
      exit 1
    fi
```

### 1.5 Per-Component Coverage Targets

**Core Library (RawRabbit) - 80% Target**:
- Critical Path Coverage: 95%+ (connection, channel management, publish/subscribe)
- Configuration: 90%+
- Error Handling: 85%+
- Middleware Pipeline: 80%+

**Operations (RawRabbit.Operations.*) - 70% Target**:
- Publish: 75%+
- Subscribe: 75%+
- Request/Response: 75%+
- MessageSequence: 70%+
- StateMachine: 65%+

**Enrichers (RawRabbit.Enrichers.*) - 60% Target**:
- Attributes: 70%+
- MessageContext: 70%+
- Polly: 65%+
- HttpContext: 60%+
- Serialization (MessagePack, Protobuf): 60%+

**DI Adapters (RawRabbit.DependencyInjection.*) - 50% Target**:
- Autofac: 60%+
- Ninject: 60%+
- ServiceCollection: 60%+

**Compatibility Layer - 60% Target**:
- RawRabbit.Compatibility.Legacy: 60%+

---

## 2. Integration Testing Strategy

### 2.1 RabbitMQ Test Environment (Docker)

**Environment Setup**: See `docker-compose.yml` in project root (created alongside this document)

**RabbitMQ Configurations**:

**1. Single Node (Development)**:
- RabbitMQ 3.12.x (latest stable)
- Management plugin enabled
- Ports: 5672 (AMQP), 15672 (Management)
- Default user: guest/guest (localhost only)

**2. Single Node with SSL/TLS (Security Testing)**:
- RabbitMQ 3.12.x with TLS
- Self-signed certificates for testing
- Ports: 5671 (AMQPS), 15671 (Management HTTPS)
- Client certificate authentication

**3. Clustered (High Availability Testing)**:
- 3-node RabbitMQ cluster
- Quorum queues for reliability testing
- Federation plugin for distributed scenarios

**Docker Compose Example** (see separate file):
```yaml
# See docker-compose.yml in project root
```

### 2.2 End-to-End Test Scenarios

**Scenario 1: Basic Publish/Subscribe**:
```csharp
[Collection("RabbitMQ")]
public class PublishSubscribeTests : IAsyncLifetime
{
    private readonly RabbitMqFixture _fixture;
    private IBusClient _publisher;
    private IBusClient _subscriber;

    public PublishSubscribeTests(RabbitMqFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _publisher = _fixture.CreateBusClient();
        _subscriber = _fixture.CreateBusClient();
    }

    [Fact]
    public async Task Subscribe_ShouldReceivePublishedMessage()
    {
        // Arrange
        var receivedMessage = new TaskCompletionSource<TestMessage>();
        await _subscriber.SubscribeAsync<TestMessage>(msg =>
        {
            receivedMessage.SetResult(msg);
            return Task.CompletedTask;
        });

        var expectedMessage = new TestMessage
        {
            Id = Guid.NewGuid(),
            Content = "Test content"
        };

        // Act
        await Task.Delay(100); // Allow subscription to register
        await _publisher.PublishAsync(expectedMessage);

        // Assert
        var received = await receivedMessage.Task
            .WaitAsync(TimeSpan.FromSeconds(5));

        received.Should().BeEquivalentTo(expectedMessage);
    }

    public async Task DisposeAsync()
    {
        await _publisher.ShutdownAsync();
        await _subscriber.ShutdownAsync();
    }
}
```

**Scenario 2: Request/Response Pattern**:
```csharp
[Fact]
public async Task Request_ShouldReceiveResponse()
{
    // Arrange
    var responder = _fixture.CreateBusClient();
    await responder.RespondAsync<TestRequest, TestResponse>(request =>
        Task.FromResult(new TestResponse
        {
            RequestId = request.Id,
            Result = $"Processed: {request.Content}"
        }));

    var requester = _fixture.CreateBusClient();

    // Act
    var response = await requester.RequestAsync<TestRequest, TestResponse>(
        new TestRequest
        {
            Id = Guid.NewGuid(),
            Content = "Test request"
        });

    // Assert
    response.Should().NotBeNull();
    response.Result.Should().StartWith("Processed:");
}
```

**Scenario 3: Message Sequencing**:
```csharp
[Fact]
public async Task MessageSequence_ShouldExecuteStepsInOrder()
{
    // Arrange
    var executionOrder = new List<string>();
    var sequence = _fixture.CreateMessageSequence()
        .PublishAsync<Step1Message>(msg =>
        {
            executionOrder.Add("Step1");
        })
        .When<Step1Response>(
            (sequence, response) => sequence.PublishAsync<Step2Message>())
        .When<Step2Response>(
            (sequence, response) =>
            {
                executionOrder.Add("Step2");
                sequence.Complete();
            });

    // Act
    await sequence.ExecuteAsync();

    // Assert
    executionOrder.Should().ContainInOrder("Step1", "Step2");
}
```

### 2.3 Cross-Component Testing

**Test Matrix**:

| Component | Dependencies | Test Scenarios |
|-----------|-------------|----------------|
| Operations.Publish | RawRabbit, Enrichers.MessageContext | Publish with/without context |
| Operations.Subscribe | RawRabbit, Enrichers.MessageContext | Subscribe with middleware |
| Operations.Request | RawRabbit, Operations.Publish | Timeout handling, correlation |
| Operations.Respond | RawRabbit, Enrichers.MessageContext.Respond | Response routing |
| Operations.MessageSequence | All operations | Multi-step workflows |
| Enrichers.Polly | Operations.* | Retry policies on failure |
| Enrichers.HttpContext | Operations.Subscribe | HTTP context propagation |

**Cross-Component Test Example**:
```csharp
[Fact]
public async Task Publish_WithPollyRetry_ShouldRetryOnTransientFailure()
{
    // Arrange
    var attemptCount = 0;
    var channelMock = new Mock<IModel>();
    channelMock
        .Setup(m => m.BasicPublish(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<IBasicProperties>(),
            It.IsAny<ReadOnlyMemory<byte>>()))
        .Callback(() =>
        {
            attemptCount++;
            if (attemptCount < 3)
                throw new BrokerUnreachableException(
                    new Exception("Simulated failure"));
        });

    var publisher = CreatePublisherWithPolly(channelMock.Object);

    // Act
    await publisher.PublishAsync(new TestMessage());

    // Assert
    attemptCount.Should().Be(3); // Initial + 2 retries
}
```

### 2.4 Test Data Setup/Teardown

**RabbitMQ Fixture** (shared across tests):
```csharp
using Xunit;

public class RabbitMqFixture : IAsyncLifetime
{
    private const string RabbitMqHost = "localhost";
    private const int RabbitMqPort = 5672;

    public RawRabbitConfiguration Configuration { get; private set; }

    public async Task InitializeAsync()
    {
        // Wait for RabbitMQ to be ready
        await WaitForRabbitMq();

        Configuration = new RawRabbitConfiguration
        {
            VirtualHost = "/test",
            Username = "guest",
            Password = "guest",
            Port = RabbitMqPort,
            Hostnames = new List<string> { RabbitMqHost },
            RequestTimeout = TimeSpan.FromSeconds(10)
        };

        // Create test vhost
        await CreateVirtualHost("/test");
    }

    public IBusClient CreateBusClient()
    {
        return RawRabbitFactory.CreateSingleton(Configuration);
    }

    public async Task DisposeAsync()
    {
        // Cleanup: Delete test vhost
        await DeleteVirtualHost("/test");
    }

    private async Task WaitForRabbitMq()
    {
        var retries = 30;
        while (retries-- > 0)
        {
            try
            {
                using var connection = new ConnectionFactory
                {
                    HostName = RabbitMqHost,
                    Port = RabbitMqPort
                }.CreateConnection();
                connection.Close();
                return;
            }
            catch
            {
                await Task.Delay(1000);
            }
        }
        throw new Exception("RabbitMQ not available after 30 seconds");
    }

    private async Task CreateVirtualHost(string vhost)
    {
        // Use RabbitMQ Management API
        using var client = new HttpClient();
        var url = $"http://{RabbitMqHost}:15672/api/vhosts/{vhost}";
        var auth = Convert.ToBase64String(
            Encoding.UTF8.GetBytes("guest:guest"));
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", auth);

        var response = await client.PutAsync(url,
            new StringContent("{}", Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
    }

    private async Task DeleteVirtualHost(string vhost)
    {
        using var client = new HttpClient();
        var url = $"http://{RabbitMqHost}:15672/api/vhosts/{vhost}";
        var auth = Convert.ToBase64String(
            Encoding.UTF8.GetBytes("guest:guest"));
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", auth);

        await client.DeleteAsync(url);
    }
}

[CollectionDefinition("RabbitMQ")]
public class RabbitMqCollection : ICollectionFixture<RabbitMqFixture>
{
}
```

### 2.5 Environment Configuration

**Test Configuration** (`appsettings.test.json`):
```json
{
  "RabbitMq": {
    "Hostnames": ["localhost"],
    "Port": 5672,
    "VirtualHost": "/test",
    "Username": "guest",
    "Password": "guest",
    "RequestTimeout": "00:00:10",
    "RecoveryInterval": "00:00:05",
    "PersistentDeliveryMode": true,
    "AutoCloseConnection": true,
    "AutoDelete": true,
    "Ssl": {
      "Enabled": false
    }
  }
}
```

**Environment Variables** (CI/CD):
```bash
# .env.test
RABBITMQ_HOST=localhost
RABBITMQ_PORT=5672
RABBITMQ_VHOST=/test
RABBITMQ_USER=guest
RABBITMQ_PASS=guest
RABBITMQ_MANAGEMENT_PORT=15672
```

---

## 3. Performance Testing Strategy

### 3.1 Baseline Metrics (from .NET Standard 1.5)

**Benchmark Scenarios**:

| Scenario | Baseline (netstandard1.5) | Target (net9.0) | Threshold |
|----------|---------------------------|-----------------|-----------|
| Simple Publish | 250 μs (mean) | ≤ 300 μs | +20% BLOCKER |
| Simple Subscribe | 180 μs (mean) | ≤ 216 μs | +20% BLOCKER |
| Request/Response | 450 μs (mean) | ≤ 540 μs | +20% BLOCKER |
| Publish (1KB message) | 280 μs (mean) | ≤ 336 μs | +20% BLOCKER |
| Publish (10KB message) | 650 μs (mean) | ≤ 780 μs | +20% BLOCKER |
| Throughput (msg/s) | 4,000 msg/s | ≥ 3,400 msg/s | -15% BLOCKER |

**Memory Baseline**:

| Scenario | Baseline Allocations | Target | Threshold |
|----------|---------------------|--------|-----------|
| Publish (simple) | 1,200 B | ≤ 1,560 B | +30% WARNING |
| Subscribe (simple) | 850 B | ≤ 1,105 B | +30% WARNING |
| Gen2 Collections | 5 collections/10k msgs | ≤ 7.5 | +50% WARNING |

### 3.2 BenchmarkDotNet Configuration

**Benchmark Project Setup**:
```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
[RankColumn]
public class PublishBenchmarks
{
    private IBusClient _client;
    private TestMessage _message;

    [GlobalSetup]
    public void Setup()
    {
        _client = RawRabbitFactory.CreateSingleton(new RawRabbitConfiguration
        {
            Hostnames = new List<string> { "localhost" },
            Port = 5672,
            Username = "guest",
            Password = "guest"
        });

        _message = new TestMessage
        {
            Id = Guid.NewGuid(),
            Content = "Benchmark test message"
        };
    }

    [Benchmark]
    public async Task PublishAsync_SimpleMessage()
    {
        await _client.PublishAsync(_message);
    }

    [Benchmark]
    public async Task PublishAsync_1KB_Message()
    {
        var largeMessage = new TestMessage
        {
            Id = Guid.NewGuid(),
            Content = new string('X', 1024)
        };
        await _client.PublishAsync(largeMessage);
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _client.ShutdownAsync();
    }
}

public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddJob(Job.Default
            .WithRuntime(CoreRuntime.Core60)
            .WithId(".NET 6"));

        AddJob(Job.Default
            .WithRuntime(CoreRuntime.Core80)
            .WithId(".NET 8"));

        AddJob(Job.Default
            .WithRuntime(CoreRuntime.Core90)
            .WithId(".NET 9"));
    }
}
```

**Execution**:
```bash
cd benchmark/RawRabbit.Benchmarks
dotnet run -c Release --framework net9.0 --filter "*PublishBenchmarks*"
```

### 3.3 Key Performance Metrics

**Latency Metrics** (per operation):
- **Mean**: Average execution time
- **P50 (Median)**: 50th percentile
- **P95**: 95th percentile (regression threshold: +25%)
- **P99**: 99th percentile
- **Max**: Worst-case latency

**Throughput Metrics**:
- **Messages per second**: Sustained rate
- **Bytes per second**: Network throughput
- **Operations per second**: Combined publish/subscribe

**Memory Metrics**:
- **Allocated**: Total bytes allocated per operation
- **Gen0/Gen1/Gen2 collections**: GC pressure
- **Memory traffic**: Bytes allocated + freed

**CPU Metrics**:
- **CPU cycles**: Processor cycles per operation
- **CPU time**: Actual CPU time consumed

### 3.4 Regression Detection Thresholds

**BLOCKER Thresholds** (stop migration if exceeded):
```csharp
public static class RegressionThresholds
{
    // Latency thresholds
    public const double MeanExecutionTimeThreshold = 1.20;      // +20%
    public const double P95LatencyThreshold = 1.25;             // +25%

    // Throughput thresholds
    public const double ThroughputThreshold = 0.85;             // -15%

    // Validation
    public static bool IsBlocker(BenchmarkResult baseline, BenchmarkResult current)
    {
        var meanRatio = current.Mean / baseline.Mean;
        var p95Ratio = current.P95 / baseline.P95;
        var throughputRatio = current.Throughput / baseline.Throughput;

        return meanRatio > MeanExecutionTimeThreshold ||
               p95Ratio > P95LatencyThreshold ||
               throughputRatio < ThroughputThreshold;
    }
}
```

**WARNING Thresholds** (review and document):
```csharp
public static class WarningThresholds
{
    // Memory thresholds
    public const double MemoryAllocationThreshold = 1.30;       // +30%
    public const double Gen2CollectionThreshold = 1.50;         // +50%
    public const double GcPauseTimeThreshold = 1.40;            // +40%

    // Validation
    public static bool IsWarning(BenchmarkResult baseline, BenchmarkResult current)
    {
        var allocRatio = current.BytesAllocated / baseline.BytesAllocated;
        var gen2Ratio = current.Gen2Collections / baseline.Gen2Collections;

        return allocRatio > MemoryAllocationThreshold ||
               gen2Ratio > Gen2CollectionThreshold;
    }
}
```

### 3.5 Performance Test Scenarios

**Scenario 1: High-Throughput Publishing**:
```csharp
[Benchmark]
[Arguments(1000)]
[Arguments(10000)]
public async Task PublishBurst(int messageCount)
{
    var tasks = new List<Task>(messageCount);
    for (int i = 0; i < messageCount; i++)
    {
        tasks.Add(_client.PublishAsync(new TestMessage
        {
            Id = Guid.NewGuid(),
            SequenceNumber = i
        }));
    }
    await Task.WhenAll(tasks);
}
```

**Scenario 2: Concurrent Publish/Subscribe**:
```csharp
[Benchmark]
public async Task ConcurrentPubSub()
{
    var publishTask = Task.Run(async () =>
    {
        for (int i = 0; i < 1000; i++)
            await _client.PublishAsync(new TestMessage());
    });

    var subscribeTask = Task.Run(async () =>
    {
        var count = 0;
        var tcs = new TaskCompletionSource<bool>();
        await _client.SubscribeAsync<TestMessage>(msg =>
        {
            if (Interlocked.Increment(ref count) >= 1000)
                tcs.SetResult(true);
            return Task.CompletedTask;
        });
        await tcs.Task;
    });

    await Task.WhenAll(publishTask, subscribeTask);
}
```

**Scenario 3: Message Size Impact**:
```csharp
[Benchmark]
[Arguments(100)]      // 100 bytes
[Arguments(1_024)]    // 1 KB
[Arguments(10_240)]   // 10 KB
[Arguments(102_400)]  // 100 KB
public async Task PublishBySize(int messageSize)
{
    var message = new TestMessage
    {
        Content = new string('X', messageSize)
    };
    await _client.PublishAsync(message);
}
```

---

## 4. Regression Testing Plan

### 4.1 BLOCKER Thresholds

**Critical Regressions (MUST FIX before proceeding)**:

**1. Execution Time Regression (+20%)**:
- **Threshold**: Mean execution time > 1.2x baseline
- **Detection**: Automated BenchmarkDotNet comparison
- **Action**: Investigate and optimize before migration continues
- **Examples**:
  - Publish operation: 250 μs → 300 μs (BLOCKER at 301 μs)
  - Subscribe operation: 180 μs → 216 μs (BLOCKER at 217 μs)

**2. P95 Latency Regression (+25%)**:
- **Threshold**: 95th percentile latency > 1.25x baseline
- **Detection**: Percentile analysis in BenchmarkDotNet
- **Action**: Profile and identify latency hotspots
- **Examples**:
  - Request/Response P95: 550 μs → 688 μs (BLOCKER at 689 μs)

**3. Throughput Degradation (-15%)**:
- **Threshold**: Messages per second < 0.85x baseline
- **Detection**: Throughput benchmark comparison
- **Action**: Review connection pooling, serialization efficiency
- **Examples**:
  - Publish throughput: 4,000 msg/s → 3,400 msg/s (BLOCKER at 3,399 msg/s)

**Automated Detection Script**:
```bash
#!/bin/bash
# scripts/check-performance-regression.sh

BASELINE_FILE="benchmark/baseline-netstandard15.json"
CURRENT_FILE="benchmark/results-net9.json"

python3 << EOF
import json
import sys

with open("$BASELINE_FILE") as f:
    baseline = json.load(f)

with open("$CURRENT_FILE") as f:
    current = json.load(f)

blockers = []

for benchmark in baseline["Benchmarks"]:
    name = benchmark["FullName"]
    baseline_mean = benchmark["Statistics"]["Mean"]
    baseline_p95 = benchmark["Statistics"]["P95"]

    current_bench = next(b for b in current["Benchmarks"]
                        if b["FullName"] == name)
    current_mean = current_bench["Statistics"]["Mean"]
    current_p95 = current_bench["Statistics"]["P95"]

    # Check mean execution time
    if current_mean / baseline_mean > 1.20:
        blockers.append(f"BLOCKER: {name} mean time +{((current_mean/baseline_mean - 1) * 100):.1f}%")

    # Check P95 latency
    if current_p95 / baseline_p95 > 1.25:
        blockers.append(f"BLOCKER: {name} P95 latency +{((current_p95/baseline_p95 - 1) * 100):.1f}%")

if blockers:
    print("\\n".join(blockers))
    sys.exit(1)
else:
    print("No performance blockers detected")
EOF
```

### 4.2 WARNING Thresholds

**Non-Critical Regressions (REVIEW and DOCUMENT)**:

**1. Memory Allocations (+30%)**:
- **Threshold**: Bytes allocated per operation > 1.3x baseline
- **Detection**: BenchmarkDotNet MemoryDiagnoser
- **Action**: Document reason, investigate if approaching GC pressure
- **Examples**:
  - Publish allocations: 1,200 B → 1,560 B (WARNING at 1,561 B)

**2. Gen2 Collections (+50%)**:
- **Threshold**: Gen2 GC count > 1.5x baseline
- **Detection**: GC statistics during benchmark run
- **Action**: Review large object allocations, consider object pooling
- **Examples**:
  - Gen2 collections: 5 per 10k messages → 7.5 (WARNING at 8)

**3. GC Pause Time (+40%)**:
- **Threshold**: Total GC pause time > 1.4x baseline
- **Detection**: ETW tracing or dotnet-trace
- **Action**: Profile GC behavior, review allocation patterns

**Warning Report Template**:
```markdown
## Performance Warning: [Scenario Name]

**Metric**: [Memory Allocations / Gen2 Collections / GC Pause Time]
**Baseline**: [Value]
**Current**: [Value]
**Change**: +[Percentage]%
**Threshold**: +[Threshold]% (WARNING)

### Analysis
[Why did this regression occur?]

### Impact Assessment
- [ ] Low: Acceptable for .NET 9 benefits
- [ ] Medium: Monitor in production
- [ ] High: Requires optimization in future release

### Mitigation Plan
[If applicable, describe optimization plan]

### Decision
- [x] ACCEPT with documentation
- [ ] DEFER to post-migration optimization
- [ ] REJECT (requires immediate fix)
```

### 4.3 Automated Regression Detection

**CI/CD Integration** (GitHub Actions):
```yaml
# .github/workflows/performance-regression.yml
name: Performance Regression Check

on:
  pull_request:
    branches: [2.0]

jobs:
  benchmark:
    runs-on: ubuntu-latest
    services:
      rabbitmq:
        image: rabbitmq:3.12-management
        ports:
          - 5672:5672
          - 15672:15672
        options: >-
          --health-cmd "rabbitmq-diagnostics -q ping"
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET 9
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 9.0.x

      - name: Restore dependencies
        run: dotnet restore

      - name: Run benchmarks
        run: |
          cd benchmark/RawRabbit.Benchmarks
          dotnet run -c Release --framework net9.0 --exporters json

      - name: Download baseline
        run: |
          wget https://github.com/laird/RawRabbit/releases/download/v2.0-baseline/benchmark-baseline.json \
            -O benchmark/baseline.json

      - name: Check for regressions
        run: bash scripts/check-performance-regression.sh

      - name: Upload results
        uses: actions/upload-artifact@v3
        with:
          name: benchmark-results
          path: |
            benchmark/results/
            BenchmarkDotNet.Artifacts/

      - name: Comment PR
        if: failure()
        uses: actions/github-script@v7
        with:
          script: |
            github.rest.issues.createComment({
              issue_number: context.issue.number,
              owner: context.repo.owner,
              repo: context.repo.repo,
              body: '⚠️ Performance regression detected! See artifact for details.'
            })
```

### 4.4 Rollback Criteria

**When to Rollback a Migration**:

**1. Critical Performance Regression**:
- Any BLOCKER threshold exceeded
- Production impact projected
- No optimization path identified within 2 weeks

**2. Functional Regression**:
- Integration tests fail consistently
- Data loss or corruption detected
- Breaking changes to public API

**3. Security Regression**:
- New CRITICAL or HIGH CVEs introduced
- Encryption/TLS functionality broken
- Authentication bypass discovered

**Rollback Process**:
```bash
# 1. Revert to previous branch
git checkout <previous-stable-branch>

# 2. Verify all tests pass
dotnet test

# 3. Run regression suite
dotnet run -c Release --project benchmark/RawRabbit.Benchmarks

# 4. Document rollback reason
# Create rollback report in docs/rollbacks/rollback-YYYY-MM-DD.md

# 5. Create GitHub issue for regression
gh issue create --title "Performance Regression: [Details]" \
  --label "performance,regression,blocker" \
  --body "See rollback report: docs/rollbacks/rollback-YYYY-MM-DD.md"
```

---

## 5. RabbitMQ Compatibility Testing

### 5.1 RabbitMQ Version Matrix

**Supported Versions**:

| RabbitMQ Version | Status | Test Priority | Notes |
|-----------------|--------|---------------|-------|
| 3.11.x (LTS) | Supported | HIGH | Long-term support version |
| 3.12.x (Latest) | Supported | CRITICAL | Primary test target |
| 3.13.x (Future) | Compatible | MEDIUM | Forward compatibility check |

**Version-Specific Test Configuration**:
```yaml
# docker-compose.test.yml
version: '3.8'
services:
  rabbitmq-3-11:
    image: rabbitmq:3.11-management
    ports:
      - "5672:5672"
      - "15672:15672"

  rabbitmq-3-12:
    image: rabbitmq:3.12-management
    ports:
      - "5673:5672"
      - "15673:15672"

  rabbitmq-3-13:
    image: rabbitmq:3.13-management
    ports:
      - "5674:5672"
      - "15674:15672"
```

### 5.2 Exchange Type Testing

**Test Coverage**:

**1. Direct Exchange**:
```csharp
[Theory]
[InlineData("3.11")]
[InlineData("3.12")]
public async Task DirectExchange_ShouldRouteByRoutingKey(string rabbitVersion)
{
    // Arrange
    var client = CreateClientForVersion(rabbitVersion);
    var receivedMessages = new ConcurrentBag<TestMessage>();

    await client.SubscribeAsync<TestMessage>(
        msg => { receivedMessages.Add(msg); return Task.CompletedTask; },
        cfg => cfg.WithExchange(ex => ex
            .WithType(ExchangeType.Direct)
            .WithName("test.direct")));

    // Act
    await client.PublishAsync(
        new TestMessage { Content = "Direct routing" },
        cfg => cfg.WithExchange(ex => ex.WithName("test.direct"))
                  .WithRoutingKey("test.routing.key"));

    // Assert
    await Task.Delay(100);
    receivedMessages.Should().HaveCount(1);
}
```

**2. Topic Exchange**:
```csharp
[Fact]
public async Task TopicExchange_ShouldMatchWildcardPattern()
{
    // Arrange
    var client = _fixture.CreateBusClient();
    var messagesReceived = 0;

    await client.SubscribeAsync<TestMessage>(
        msg => { Interlocked.Increment(ref messagesReceived); return Task.CompletedTask; },
        cfg => cfg.WithExchange(ex => ex
            .WithType(ExchangeType.Topic)
            .WithName("test.topic"))
            .WithRoutingKey("events.*.created"));

    // Act
    await client.PublishAsync(new TestMessage(),
        cfg => cfg.WithRoutingKey("events.user.created")); // Match
    await client.PublishAsync(new TestMessage(),
        cfg => cfg.WithRoutingKey("events.order.created")); // Match
    await client.PublishAsync(new TestMessage(),
        cfg => cfg.WithRoutingKey("events.user.updated")); // No match

    // Assert
    await Task.Delay(100);
    messagesReceived.Should().Be(2);
}
```

**3. Fanout Exchange**:
```csharp
[Fact]
public async Task FanoutExchange_ShouldBroadcastToAllQueues()
{
    // Arrange
    var subscriber1Messages = new List<TestMessage>();
    var subscriber2Messages = new List<TestMessage>();

    await _subscriber1.SubscribeAsync<TestMessage>(
        msg => { subscriber1Messages.Add(msg); return Task.CompletedTask; },
        cfg => cfg.WithExchange(ex => ex
            .WithType(ExchangeType.Fanout)
            .WithName("test.fanout")));

    await _subscriber2.SubscribeAsync<TestMessage>(
        msg => { subscriber2Messages.Add(msg); return Task.CompletedTask; },
        cfg => cfg.WithExchange(ex => ex
            .WithType(ExchangeType.Fanout)
            .WithName("test.fanout")));

    // Act
    await _publisher.PublishAsync(new TestMessage { Content = "Broadcast" });
    await Task.Delay(100);

    // Assert
    subscriber1Messages.Should().HaveCount(1);
    subscriber2Messages.Should().HaveCount(1);
}
```

**4. Headers Exchange**:
```csharp
[Fact]
public async Task HeadersExchange_ShouldMatchHeaders()
{
    // Arrange
    var receivedMessages = new ConcurrentBag<TestMessage>();

    await _subscriber.SubscribeAsync<TestMessage>(
        msg => { receivedMessages.Add(msg); return Task.CompletedTask; },
        cfg => cfg.WithExchange(ex => ex
            .WithType(ExchangeType.Headers)
            .WithName("test.headers"))
            .WithArgument("x-match", "all")
            .WithArgument("format", "json")
            .WithArgument("version", "1.0"));

    // Act
    await _publisher.PublishAsync(
        new TestMessage(),
        cfg => cfg.WithProperties(props => props.Headers = new Dictionary<string, object>
        {
            ["format"] = "json",
            ["version"] = "1.0"
        }));

    // Assert
    await Task.Delay(100);
    receivedMessages.Should().HaveCount(1);
}
```

### 5.3 Connection Scenario Testing

**Scenario 1: Single Connection**:
```csharp
[Fact]
public async Task SingleConnection_ShouldReuseChannel()
{
    // Arrange
    var config = new RawRabbitConfiguration
    {
        Hostnames = new List<string> { "localhost" },
        Port = 5672,
        AutoCloseConnection = false
    };

    var client = RawRabbitFactory.CreateSingleton(config);

    // Act
    await client.PublishAsync(new TestMessage());
    await client.PublishAsync(new TestMessage());

    // Assert
    // Verify only 1 connection created (via RabbitMQ Management API)
    var connections = await GetRabbitMqConnections();
    connections.Should().HaveCount(1);
}
```

**Scenario 2: Connection Pooling**:
```csharp
[Fact]
public async Task ConnectionPool_ShouldDistributeLoad()
{
    // Arrange
    var config = new RawRabbitConfiguration
    {
        Hostnames = new List<string> { "localhost" },
        Port = 5672,
        // Enable connection pooling (custom configuration)
        MaxConnections = 5
    };

    // Act
    var tasks = Enumerable.Range(0, 100)
        .Select(i => Task.Run(async () =>
        {
            var client = RawRabbitFactory.CreateSingleton(config);
            await client.PublishAsync(new TestMessage());
        }))
        .ToList();

    await Task.WhenAll(tasks);

    // Assert
    var connections = await GetRabbitMqConnections();
    connections.Should().HaveCountLessThanOrEqualTo(5);
}
```

**Scenario 3: Clustered RabbitMQ**:
```csharp
[Fact]
public async Task ClusteredRabbitMQ_ShouldFailover()
{
    // Arrange
    var config = new RawRabbitConfiguration
    {
        Hostnames = new List<string>
        {
            "rabbitmq-node1",
            "rabbitmq-node2",
            "rabbitmq-node3"
        },
        Port = 5672,
        AutomaticRecoveryEnabled = true
    };

    var client = RawRabbitFactory.CreateSingleton(config);
    var messagesReceived = 0;

    await client.SubscribeAsync<TestMessage>(msg =>
    {
        Interlocked.Increment(ref messagesReceived);
        return Task.CompletedTask;
    });

    // Act
    await client.PublishAsync(new TestMessage()); // Node 1 handles

    // Simulate node1 failure
    await StopRabbitMqNode("rabbitmq-node1");

    await Task.Delay(2000); // Wait for recovery

    await client.PublishAsync(new TestMessage()); // Node 2/3 handles

    // Assert
    await Task.Delay(500);
    messagesReceived.Should().Be(2); // Both messages received despite node failure
}
```

### 5.4 Error Scenario Testing

**Scenario 1: Connection Loss**:
```csharp
[Fact]
public async Task ConnectionLoss_ShouldRecover()
{
    // Arrange
    var config = new RawRabbitConfiguration
    {
        Hostnames = new List<string> { "localhost" },
        AutomaticRecoveryEnabled = true,
        NetworkRecoveryInterval = TimeSpan.FromSeconds(2)
    };

    var client = RawRabbitFactory.CreateSingleton(config);
    var messagesReceived = new List<TestMessage>();

    await client.SubscribeAsync<TestMessage>(msg =>
    {
        messagesReceived.Add(msg);
        return Task.CompletedTask;
    });

    await client.PublishAsync(new TestMessage { Id = Guid.NewGuid() });
    await Task.Delay(100);

    // Act: Simulate connection loss
    await RestartRabbitMq();
    await Task.Delay(3000); // Wait for recovery

    await client.PublishAsync(new TestMessage { Id = Guid.NewGuid() });
    await Task.Delay(500);

    // Assert
    messagesReceived.Should().HaveCount(2);
}
```

**Scenario 2: Channel Errors**:
```csharp
[Fact]
public async Task ChannelError_ShouldRecreateChannel()
{
    // Arrange
    var client = _fixture.CreateBusClient();

    // Act: Trigger channel error (invalid exchange)
    try
    {
        await client.PublishAsync(
            new TestMessage(),
            cfg => cfg.WithExchange(ex => ex
                .WithName("nonexistent.exchange")
                .WithAutoDelete(false)
                .WithDurability(false)));
    }
    catch (OperationInterruptedException)
    {
        // Expected
    }

    // Assert: Client should recover and allow new operations
    await client.PublishAsync(new TestMessage()); // Should succeed
}
```

**Scenario 3: Message Nack/Reject**:
```csharp
[Fact]
public async Task MessageNack_ShouldRequeue()
{
    // Arrange
    var attemptCount = 0;
    var successMessage = new TaskCompletionSource<TestMessage>();

    await _subscriber.SubscribeAsync<TestMessage>(
        msg =>
        {
            if (Interlocked.Increment(ref attemptCount) < 3)
                throw new Exception("Simulated processing failure");

            successMessage.SetResult(msg);
            return Task.CompletedTask;
        },
        cfg => cfg.WithPrefetchCount(1)
                  .WithNoAck(false)); // Enable manual ack

    // Act
    await _publisher.PublishAsync(new TestMessage());

    // Assert
    var result = await successMessage.Task.WaitAsync(TimeSpan.FromSeconds(10));
    attemptCount.Should().Be(3); // Original + 2 retries
}
```

---

## 6. Security Testing

### 6.1 TLS/SSL Connection Validation

**Test Environment Setup**:
```yaml
# docker-compose.ssl.yml
version: '3.8'
services:
  rabbitmq-ssl:
    image: rabbitmq:3.12-management
    ports:
      - "5671:5671"   # AMQPS
      - "15671:15671" # Management HTTPS
    environment:
      RABBITMQ_SSL_CACERTFILE: /etc/rabbitmq/ca-cert.pem
      RABBITMQ_SSL_CERTFILE: /etc/rabbitmq/server-cert.pem
      RABBITMQ_SSL_KEYFILE: /etc/rabbitmq/server-key.pem
      RABBITMQ_SSL_VERIFY: verify_peer
      RABBITMQ_SSL_FAIL_IF_NO_PEER_CERT: "true"
    volumes:
      - ./test/certificates:/etc/rabbitmq/
```

**Certificate Generation** (for testing):
```bash
#!/bin/bash
# test/certificates/generate-test-certs.sh

# Generate CA
openssl genrsa -out ca-key.pem 2048
openssl req -new -x509 -key ca-key.pem -out ca-cert.pem -days 365 \
    -subj "/CN=Test CA"

# Generate server certificate
openssl genrsa -out server-key.pem 2048
openssl req -new -key server-key.pem -out server-req.pem \
    -subj "/CN=localhost"
openssl x509 -req -in server-req.pem -CA ca-cert.pem -CAkey ca-key.pem \
    -CAcreateserial -out server-cert.pem -days 365

# Generate client certificate
openssl genrsa -out client-key.pem 2048
openssl req -new -key client-key.pem -out client-req.pem \
    -subj "/CN=test-client"
openssl x509 -req -in client-req.pem -CA ca-cert.pem -CAkey ca-key.pem \
    -CAcreateserial -out client-cert.pem -days 365
```

**SSL Connection Test**:
```csharp
[Fact]
public async Task SslConnection_ShouldEstablishSecureConnection()
{
    // Arrange
    var config = new RawRabbitConfiguration
    {
        Hostnames = new List<string> { "localhost" },
        Port = 5671,
        Ssl = new SslOption
        {
            Enabled = true,
            ServerName = "localhost",
            CertPath = "test/certificates/client-cert.pem",
            CertPassphrase = "",
            Version = SslProtocols.Tls12 | SslProtocols.Tls13
        }
    };

    // Act
    var client = RawRabbitFactory.CreateSingleton(config);
    await client.PublishAsync(new TestMessage());

    // Assert
    // Connection should succeed
    var isConnected = await IsClientConnected(client);
    isConnected.Should().BeTrue();
}
```

**Certificate Validation Test**:
```csharp
[Fact]
public async Task SslConnection_WithInvalidCert_ShouldFail()
{
    // Arrange
    var config = new RawRabbitConfiguration
    {
        Hostnames = new List<string> { "localhost" },
        Port = 5671,
        Ssl = new SslOption
        {
            Enabled = true,
            ServerName = "localhost",
            CertPath = "test/certificates/invalid-cert.pem",
            AcceptablePolicyErrors = SslPolicyErrors.None // Strict validation
        }
    };

    // Act & Assert
    await Assert.ThrowsAsync<BrokerUnreachableException>(async () =>
    {
        var client = RawRabbitFactory.CreateSingleton(config);
        await client.PublishAsync(new TestMessage());
    });
}
```

### 6.2 Authentication Testing

**Basic Authentication Test**:
```csharp
[Theory]
[InlineData("guest", "guest", true)]  // Valid credentials
[InlineData("invalid", "invalid", false)]  // Invalid credentials
public async Task Authentication_ShouldValidateCredentials(
    string username, string password, bool shouldSucceed)
{
    // Arrange
    var config = new RawRabbitConfiguration
    {
        Hostnames = new List<string> { "localhost" },
        Port = 5672,
        Username = username,
        Password = password
    };

    // Act & Assert
    if (shouldSucceed)
    {
        var client = RawRabbitFactory.CreateSingleton(config);
        await client.PublishAsync(new TestMessage());
        // Should not throw
    }
    else
    {
        await Assert.ThrowsAsync<BrokerUnreachableException>(async () =>
        {
            var client = RawRabbitFactory.CreateSingleton(config);
            await client.PublishAsync(new TestMessage());
        });
    }
}
```

**Client Certificate Authentication**:
```csharp
[Fact]
public async Task ClientCertAuth_ShouldAuthenticateWithCertificate()
{
    // Arrange
    var config = new RawRabbitConfiguration
    {
        Hostnames = new List<string> { "localhost" },
        Port = 5671,
        Ssl = new SslOption
        {
            Enabled = true,
            ServerName = "localhost",
            CertPath = "test/certificates/client-cert.pem",
            CertPassphrase = "",
            // No username/password - certificate-based auth only
        }
    };

    // Act
    var client = RawRabbitFactory.CreateSingleton(config);
    await client.PublishAsync(new TestMessage());

    // Assert
    var isConnected = await IsClientConnected(client);
    isConnected.Should().BeTrue();
}
```

### 6.3 CVE Validation

**Test CVE-2024-21907 Fix (Newtonsoft.Json DoS)**:
```csharp
[Fact]
public async Task Serialization_ShouldHandleDeeplyNestedJson()
{
    // Arrange: Create deeply nested JSON that would trigger CVE-2024-21907
    var deeplyNestedMessage = CreateDeeplyNestedObject(depth: 1000);

    // Act
    var exception = await Record.ExceptionAsync(async () =>
    {
        await _client.PublishAsync(deeplyNestedMessage);
    });

    // Assert: Should NOT cause DoS or excessive memory consumption
    exception.Should().BeNull();

    // Verify memory consumption is reasonable
    var memoryAfter = GC.GetTotalMemory(forceFullCollection: true);
    memoryAfter.Should().BeLessThan(100 * 1024 * 1024); // < 100 MB
}

private object CreateDeeplyNestedObject(int depth)
{
    if (depth == 0) return new { Value = "leaf" };
    return new { Nested = CreateDeeplyNestedObject(depth - 1) };
}
```

**Test CVE-2024-21908 Fix (Newtonsoft.Json RCE)**:
```csharp
[Fact]
public async Task Serialization_ShouldRejectTypeNameHandling()
{
    // Arrange: Attempt to exploit TypeNameHandling vulnerability
    var maliciousJson = @"{
        ""$type"": ""System.Windows.Data.ObjectDataProvider, PresentationFramework"",
        ""MethodName"": ""Start"",
        ""ObjectInstance"": {
            ""$type"": ""System.Diagnostics.Process, System""
        }
    }";

    // Act & Assert: Should reject or sanitize
    var exception = await Record.ExceptionAsync(async () =>
    {
        // Attempt to deserialize malicious payload
        var message = JsonConvert.DeserializeObject<TestMessage>(maliciousJson);
        await _client.PublishAsync(message);
    });

    // Should either throw or safely ignore $type directive
    // (depending on JsonSerializerSettings configuration)
}
```

**Test CVE-2020-11100 Fix (RabbitMQ.Client TLS bypass)**:
```csharp
[Fact]
public async Task TlsConnection_ShouldEnforceCertificateValidation()
{
    // Arrange: Configure with strict certificate validation
    var config = new RawRabbitConfiguration
    {
        Hostnames = new List<string> { "localhost" },
        Port = 5671,
        Ssl = new SslOption
        {
            Enabled = true,
            ServerName = "wrong-hostname", // Intentional mismatch
            AcceptablePolicyErrors = SslPolicyErrors.None // Strict
        }
    };

    // Act & Assert: Should reject connection due to hostname mismatch
    await Assert.ThrowsAsync<BrokerUnreachableException>(async () =>
    {
        var client = RawRabbitFactory.CreateSingleton(config);
        await client.PublishAsync(new TestMessage());
    });
}
```

### 6.4 Secrets Management Testing

**Test Environment Variable Configuration**:
```csharp
[Fact]
public void Configuration_ShouldLoadFromEnvironmentVariables()
{
    // Arrange
    Environment.SetEnvironmentVariable("RABBITMQ_USERNAME", "env-user");
    Environment.SetEnvironmentVariable("RABBITMQ_PASSWORD", "env-password");

    // Act
    var config = RawRabbitConfiguration.FromEnvironment();

    // Assert
    config.Username.Should().Be("env-user");
    config.Password.Should().Be("env-password");

    // Cleanup
    Environment.SetEnvironmentVariable("RABBITMQ_USERNAME", null);
    Environment.SetEnvironmentVariable("RABBITMQ_PASSWORD", null);
}
```

**Test Hardcoded Credential Detection**:
```csharp
[Fact]
public void Configuration_ShouldWarnOnHardcodedCredentials()
{
    // Arrange
    var config = new RawRabbitConfiguration
    {
        Username = "guest",
        Password = "guest"
    };

    // Act
    var warnings = config.Validate();

    // Assert
    warnings.Should().Contain(w =>
        w.Contains("guest/guest") &&
        w.Contains("production"));
}
```

---

## 7. Migration-Specific Testing

### 7.1 Phase-Based Test Plans

**Phase 1: Foundation (Core Library)**
**Duration**: Weeks 1-2
**Components**: RawRabbit, RawRabbit.Operations.Tools

**Test Plan**:
```markdown
## Phase 1 Test Checklist

### Unit Tests
- [ ] Connection management (80%+ coverage)
- [ ] Channel factory (80%+ coverage)
- [ ] Configuration loading (90%+ coverage)
- [ ] Middleware pipeline (85%+ coverage)
- [ ] Error handling (85%+ coverage)

### Integration Tests
- [ ] Basic publish/subscribe (RabbitMQ 3.11, 3.12)
- [ ] Connection recovery
- [ ] Channel errors and recovery
- [ ] SSL/TLS connections
- [ ] Authentication (basic, client cert)

### Performance Tests
- [ ] Publish latency baseline
- [ ] Subscribe latency baseline
- [ ] Throughput baseline
- [ ] Memory allocation baseline

### Regression Tests
- [ ] Compare .NET Standard 1.5 vs .NET 9
- [ ] Verify no BLOCKER thresholds exceeded
- [ ] Document any WARNING thresholds exceeded

### Security Tests
- [ ] CVE-2024-21907 validation (if migrating to System.Text.Json)
- [ ] CVE-2020-11100 validation (RabbitMQ.Client upgrade)
- [ ] TLS 1.2+ enforcement
- [ ] Credential validation

### Acceptance Criteria
- [ ] All tests pass on .NET 6, 8, 9
- [ ] Coverage: Core ≥ 80%
- [ ] No BLOCKER performance regressions
- [ ] All HIGH/CRITICAL CVEs resolved
```

**Phase 2: Simple Operations & Enrichers (Batch 1)**
**Duration**: Week 3
**Components**: Operations.Publish, Operations.Subscribe, Operations.Request, Operations.Respond, Operations.Get, Simple Enrichers (5)

**Test Plan**:
```markdown
## Phase 2 Test Checklist

### Unit Tests
- [ ] Each operation: 70%+ coverage
- [ ] Enricher middleware: 60%+ coverage
- [ ] Configuration builders: 80%+ coverage

### Integration Tests
- [ ] Publish with enrichers (GlobalExecutionId, Attributes)
- [ ] Subscribe with enrichers
- [ ] Request/Response pattern
- [ ] Cross-enricher compatibility

### Performance Tests
- [ ] Publish with enrichers (vs baseline)
- [ ] Subscribe with enrichers (vs baseline)
- [ ] Request/Response latency

### Regression Tests
- [ ] Enricher overhead < 10% vs no enricher
- [ ] No BLOCKER thresholds exceeded

### Acceptance Criteria
- [ ] All tests pass on .NET 6, 8, 9
- [ ] Coverage: Operations ≥ 70%, Enrichers ≥ 60%
- [ ] Enricher overhead acceptable
```

**Phase 3: Complex Operations & Enrichers (Batch 2)**
**Duration**: Weeks 4-5
**Components**: RawRabbit.Enrichers.HttpContext, RawRabbit.Enrichers.Polly, RawRabbit.Operations.MessageSequence, RawRabbit.Operations.StateMachine, Remaining enrichers (6)

**Test Plan**:
```markdown
## Phase 3 Test Checklist

### Unit Tests
- [ ] HttpContext enricher (60%+ coverage, ASP.NET Core only)
- [ ] Polly enricher (65%+ coverage)
- [ ] MessageSequence (70%+ coverage)
- [ ] StateMachine (65%+ coverage)
- [ ] Serialization enrichers (MessagePack, Protobuf, ZeroFormatter): 60%+

### Integration Tests
- [ ] HttpContext propagation in ASP.NET Core
- [ ] Polly retry policies (transient failures)
- [ ] Multi-step MessageSequence workflows
- [ ] StateMachine transitions
- [ ] MessagePack serialization round-trip
- [ ] Protobuf serialization round-trip

### Performance Tests
- [ ] Polly retry overhead
- [ ] MessageSequence multi-step latency
- [ ] Serialization performance: MessagePack vs Newtonsoft.Json vs System.Text.Json

### Regression Tests
- [ ] Polly retry should not exceed +30% latency
- [ ] MessageSequence memory allocations < +30%
- [ ] Serialization performance acceptable

### Acceptance Criteria
- [ ] All tests pass on .NET 6, 8, 9
- [ ] Coverage: Operations ≥ 70%, Enrichers ≥ 60%
- [ ] HttpContext ASP.NET Core compatible (no System.Web)
- [ ] Polly policies configurable and functional
```

**Phase 4: Dependency Injection & Compatibility**
**Duration**: Week 6
**Components**: RawRabbit.DependencyInjection.Autofac, Ninject, ServiceCollection, RawRabbit.Compatibility.Legacy

**Test Plan**:
```markdown
## Phase 4 Test Checklist

### Unit Tests
- [ ] Autofac registration (60%+ coverage)
- [ ] Ninject registration (60%+ coverage)
- [ ] ServiceCollection registration (60%+ coverage)
- [ ] Compatibility layer (60%+ coverage)

### Integration Tests
- [ ] Autofac: Resolve IBusClient and dependencies
- [ ] Ninject: Resolve IBusClient and dependencies
- [ ] ServiceCollection: Resolve IBusClient and dependencies
- [ ] Compatibility: Legacy API still functional

### DI Container Compatibility
- [ ] Autofac 8.x (.NET 9 compatible)
- [ ] Ninject 3.3.6+ (.NET Standard 2.0+)
- [ ] Microsoft.Extensions.DependencyInjection 9.0.0

### Acceptance Criteria
- [ ] All DI adapters work with .NET 6, 8, 9
- [ ] Coverage: DI adapters ≥ 50%
- [ ] Compatibility layer maintains API surface
```

**Phase 5: Test Projects**
**Duration**: Week 7
**Components**: RawRabbit.Enrichers.Polly.Tests, RawRabbit.IntegrationTests, RawRabbit.Tests, RawRabbit.PerformanceTest

**Test Plan**:
```markdown
## Phase 5 Test Checklist

### Test Project Migration
- [ ] Convert RawRabbit.Enrichers.Polly.Tests to SDK-style .csproj
- [ ] Update all test projects to .NET 6, 8, 9 multi-targeting
- [ ] Update test dependencies (xUnit, Moq, etc.)

### Test Execution
- [ ] All unit tests pass on .NET 6, 8, 9
- [ ] All integration tests pass on .NET 6, 8, 9
- [ ] Performance tests execute successfully

### Coverage Validation
- [ ] Overall coverage ≥ 75%
- [ ] Core library ≥ 80%
- [ ] Operations ≥ 70%
- [ ] Enrichers ≥ 60%
- [ ] DI adapters ≥ 50%

### Acceptance Criteria
- [ ] 100% test projects migrated
- [ ] All tests green on .NET 6, 8, 9
- [ ] Coverage requirements met
```

**Phase 6: Samples & Documentation**
**Duration**: Week 8
**Components**: RawRabbit.AspNet.Sample, RawRabbit.ConsoleApp.Sample, RawRabbit.Messages.Sample, Documentation

**Test Plan**:
```markdown
## Phase 6 Test Checklist

### Sample Projects
- [ ] AspNet.Sample runs on .NET 9
- [ ] ConsoleApp.Sample runs on .NET 9
- [ ] Messages.Sample compatible with .NET 9

### Sample Validation
- [ ] Each sample demonstrates core functionality
- [ ] Samples include README with run instructions
- [ ] Samples use updated NuGet packages

### Documentation
- [ ] README.md updated for .NET 9
- [ ] Migration guide created
- [ ] Breaking changes documented
- [ ] API documentation updated

### Acceptance Criteria
- [ ] All samples functional on .NET 9
- [ ] Documentation complete and accurate
- [ ] Migration guide assists users
```

### 7.2 Dependency Validation

**Dependency Version Matrix**:

| Package | Current | Target | Breaking Changes | Test Strategy |
|---------|---------|--------|------------------|---------------|
| RabbitMQ.Client | 5.0.1 | 7.1.2+ | YES (async API) | Integration tests |
| Newtonsoft.Json | 10.0.1 | 13.0.3+ or System.Text.Json | MINOR | Serialization tests |
| Autofac | 4.1.0 | 8.1.0+ | YES (registration API) | DI integration tests |
| Ninject | 3.3.4 | 3.3.6+ | NO | DI integration tests |
| Microsoft.Extensions.DI | 1.0.2 | 9.0.0+ | MINOR | DI integration tests |
| Polly | 5.3.1 | 8.5.0+ | YES (policy API) | Retry/circuit breaker tests |
| MessagePack | 1.7.3.4 | 2.5.140+ | YES (serialization API) | Serialization tests |

**Dependency Validation Test**:
```csharp
[Theory]
[InlineData("RabbitMQ.Client", "7.1.2")]
[InlineData("Newtonsoft.Json", "13.0.3")]
[InlineData("Autofac", "8.1.0")]
public async Task DependencyVersion_ShouldBeCompatible(
    string packageName, string expectedVersion)
{
    // Arrange
    var assemblyPath = typeof(IBusClient).Assembly.Location;
    var dependencies = GetPackageDependencies(assemblyPath);

    // Act
    var actualVersion = dependencies
        .FirstOrDefault(d => d.Name == packageName)
        ?.Version;

    // Assert
    actualVersion.Should().NotBeNull($"{packageName} should be referenced");
    Version.Parse(actualVersion).Should()
        .BeGreaterOrEqualTo(Version.Parse(expectedVersion));
}
```

### 7.3 API Compatibility Testing

**Public API Surface Test**:
```csharp
[Fact]
public void PublicApi_ShouldMaintainBackwardCompatibility()
{
    // Arrange
    var assembly = typeof(IBusClient).Assembly;
    var publicTypes = assembly.GetExportedTypes();

    var expectedTypes = new[]
    {
        "RawRabbit.IBusClient",
        "RawRabbit.Configuration.RawRabbitConfiguration",
        "RawRabbit.Operations.IPublishOperation",
        "RawRabbit.Operations.ISubscribeOperation",
        "RawRabbit.Operations.IRequestOperation",
        "RawRabbit.Operations.IRespondOperation",
        // ... more types
    };

    // Act
    var actualTypeNames = publicTypes.Select(t => t.FullName).ToList();

    // Assert
    foreach (var expectedType in expectedTypes)
    {
        actualTypeNames.Should().Contain(expectedType,
            $"Public API type {expectedType} should be preserved");
    }
}
```

**Method Signature Compatibility Test**:
```csharp
[Fact]
public void IBusClient_PublishAsync_ShouldHaveExpectedSignature()
{
    // Arrange
    var type = typeof(IBusClient);
    var method = type.GetMethod("PublishAsync", new[] { typeof(object) });

    // Assert
    method.Should().NotBeNull("PublishAsync(object) should exist");
    method.ReturnType.Should().Be(typeof(Task),
        "PublishAsync should return Task");
    method.IsGenericMethod.Should().BeTrue(
        "PublishAsync should be generic");
}
```

### 7.4 Breaking Change Verification

**Breaking Change Test Suite**:
```csharp
public class BreakingChangeTests
{
    [Fact]
    public void RabbitMqClient_ShouldUseAsyncApi()
    {
        // Verify RabbitMQ.Client 7.x async API is used
        var method = typeof(IModel).GetMethod("BasicPublishAsync");
        method.Should().NotBeNull(
            "RabbitMQ.Client 7.x should have BasicPublishAsync");
    }

    [Fact]
    public void AutofacRegistration_ShouldUseNewApi()
    {
        // Verify Autofac 8.x registration API
        var builder = new ContainerBuilder();
        builder.RegisterRawRabbit(); // Extension method

        var container = builder.Build();
        var client = container.Resolve<IBusClient>();

        client.Should().NotBeNull();
    }

    [Fact]
    public void PollyPolicy_ShouldUseV8Api()
    {
        // Verify Polly 8.x policy API
        var policy = Policy
            .Handle<BrokerUnreachableException>()
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        policy.Should().NotBeNull();
    }
}
```

---

## 8. Test Infrastructure

### 8.1 CI/CD Integration (GitHub Actions)

**Main Test Workflow** (`.github/workflows/test.yml`):
```yaml
name: Test Suite

on:
  push:
    branches: [2.0, 'stage-*']
  pull_request:
    branches: [2.0]

jobs:
  unit-tests:
    name: Unit Tests
    runs-on: ${{ matrix.os }}
    strategy:
      matrix:
        os: [ubuntu-latest, windows-latest, macos-latest]
        dotnet: ['6.0.x', '8.0.x', '9.0.x']

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET ${{ matrix.dotnet }}
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ matrix.dotnet }}

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --configuration Release --no-restore

      - name: Run unit tests
        run: |
          dotnet test --configuration Release --no-build \
            --collect:"XPlat Code Coverage" \
            --results-directory ./coverage \
            --filter "Category!=Integration"

      - name: Generate coverage report
        run: |
          dotnet tool install -g dotnet-reportgenerator-globaltool
          reportgenerator \
            -reports:"./coverage/**/coverage.cobertura.xml" \
            -targetdir:"./coverage/report" \
            -reporttypes:"Html;Badges;Cobertura"

      - name: Check coverage thresholds
        run: |
          COVERAGE=$(grep -oP 'line-rate="\K[0-9.]+' ./coverage/report/Cobertura.xml)
          COVERAGE_PCT=$(echo "$COVERAGE * 100" | bc)
          echo "Coverage: $COVERAGE_PCT%"
          if (( $(echo "$COVERAGE_PCT < 75" | bc -l) )); then
            echo "::error::Coverage $COVERAGE_PCT% is below 75% threshold"
            exit 1
          fi

      - name: Upload coverage to Codecov
        uses: codecov/codecov-action@v3
        with:
          files: ./coverage/report/Cobertura.xml
          flags: unittests

      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v3
        with:
          name: test-results-${{ matrix.os }}-${{ matrix.dotnet }}
          path: |
            ./coverage/
            TestResults/

  integration-tests:
    name: Integration Tests
    runs-on: ubuntu-latest
    strategy:
      matrix:
        rabbitmq: ['3.11', '3.12']
        dotnet: ['6.0.x', '8.0.x', '9.0.x']

    services:
      rabbitmq:
        image: rabbitmq:${{ matrix.rabbitmq }}-management
        ports:
          - 5672:5672
          - 15672:15672
        options: >-
          --health-cmd "rabbitmq-diagnostics -q ping"
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET ${{ matrix.dotnet }}
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ matrix.dotnet }}

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --configuration Release --no-restore

      - name: Wait for RabbitMQ
        run: |
          for i in {1..30}; do
            if curl -f http://localhost:15672/api/overview -u guest:guest; then
              echo "RabbitMQ is ready"
              break
            fi
            echo "Waiting for RabbitMQ... ($i/30)"
            sleep 2
          done

      - name: Run integration tests
        env:
          RABBITMQ_HOST: localhost
          RABBITMQ_PORT: 5672
        run: |
          dotnet test --configuration Release --no-build \
            --filter "Category=Integration" \
            --logger "trx;LogFileName=integration-tests.trx"

      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v3
        with:
          name: integration-results-rabbitmq${{ matrix.rabbitmq }}-dotnet${{ matrix.dotnet }}
          path: TestResults/

  performance-tests:
    name: Performance Tests
    runs-on: ubuntu-latest

    services:
      rabbitmq:
        image: rabbitmq:3.12-management
        ports:
          - 5672:5672
        options: >-
          --health-cmd "rabbitmq-diagnostics -q ping"
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET 9
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 9.0.x

      - name: Run benchmarks
        run: |
          cd benchmark/RawRabbit.Benchmarks
          dotnet run -c Release --framework net9.0 --exporters json

      - name: Download baseline
        run: |
          wget https://github.com/laird/RawRabbit/releases/download/v2.0-baseline/benchmark-baseline.json \
            -O benchmark/baseline.json || echo "Baseline not found, skipping comparison"

      - name: Compare to baseline
        if: hashFiles('benchmark/baseline.json') != ''
        run: bash scripts/check-performance-regression.sh

      - name: Upload benchmark results
        uses: actions/upload-artifact@v3
        with:
          name: benchmark-results
          path: |
            BenchmarkDotNet.Artifacts/
            benchmark/results/
```

### 8.2 Docker Setup for RabbitMQ

**Multi-Configuration Docker Compose** (`docker-compose.test.yml`):
```yaml
version: '3.8'

services:
  # Single node (default)
  rabbitmq:
    image: rabbitmq:3.12-management
    container_name: rawrabbit-test-rabbitmq
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
      RABBITMQ_DEFAULT_VHOST: /
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "-q", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5
    networks:
      - test-network

  # SSL/TLS node
  rabbitmq-ssl:
    image: rabbitmq:3.12-management
    container_name: rawrabbit-test-rabbitmq-ssl
    ports:
      - "5671:5671"
      - "15671:15671"
    environment:
      RABBITMQ_SSL_CACERTFILE: /etc/rabbitmq/ca-cert.pem
      RABBITMQ_SSL_CERTFILE: /etc/rabbitmq/server-cert.pem
      RABBITMQ_SSL_KEYFILE: /etc/rabbitmq/server-key.pem
      RABBITMQ_SSL_VERIFY: verify_peer
      RABBITMQ_SSL_FAIL_IF_NO_PEER_CERT: "false"
    volumes:
      - ./test/certificates:/etc/rabbitmq/
    networks:
      - test-network

  # Cluster node 1
  rabbitmq-node1:
    image: rabbitmq:3.12-management
    hostname: rabbitmq-node1
    environment:
      RABBITMQ_ERLANG_COOKIE: 'secret-cookie'
      RABBITMQ_NODENAME: rabbit@rabbitmq-node1
    ports:
      - "5673:5672"
      - "15673:15672"
    networks:
      - test-network

  # Cluster node 2
  rabbitmq-node2:
    image: rabbitmq:3.12-management
    hostname: rabbitmq-node2
    environment:
      RABBITMQ_ERLANG_COOKIE: 'secret-cookie'
      RABBITMQ_NODENAME: rabbit@rabbitmq-node2
    ports:
      - "5674:5672"
      - "15674:15672"
    networks:
      - test-network
    depends_on:
      - rabbitmq-node1

  # Cluster node 3
  rabbitmq-node3:
    image: rabbitmq:3.12-management
    hostname: rabbitmq-node3
    environment:
      RABBITMQ_ERLANG_COOKIE: 'secret-cookie'
      RABBITMQ_NODENAME: rabbit@rabbitmq-node3
    ports:
      - "5675:5672"
      - "15675:15672"
    networks:
      - test-network
    depends_on:
      - rabbitmq-node1

networks:
  test-network:
    driver: bridge
```

**Cluster Setup Script** (`scripts/setup-rabbitmq-cluster.sh`):
```bash
#!/bin/bash
set -e

echo "Starting RabbitMQ cluster setup..."

# Start all nodes
docker-compose -f docker-compose.test.yml up -d rabbitmq-node1 rabbitmq-node2 rabbitmq-node3

# Wait for nodes to be ready
echo "Waiting for nodes to start..."
sleep 15

# Join node2 to cluster
docker exec rabbitmq-node2 rabbitmqctl stop_app
docker exec rabbitmq-node2 rabbitmqctl reset
docker exec rabbitmq-node2 rabbitmqctl join_cluster rabbit@rabbitmq-node1
docker exec rabbitmq-node2 rabbitmqctl start_app

# Join node3 to cluster
docker exec rabbitmq-node3 rabbitmqctl stop_app
docker exec rabbitmq-node3 rabbitmqctl reset
docker exec rabbitmq-node3 rabbitmqctl join_cluster rabbit@rabbitmq-node1
docker exec rabbitmq-node3 rabbitmqctl start_app

echo "Cluster setup complete!"
docker exec rabbitmq-node1 rabbitmqctl cluster_status
```

### 8.3 Test Data Management

**Test Data Factory**:
```csharp
public static class TestDataFactory
{
    private static readonly Faker _faker = new();

    public static TestMessage CreateMessage(Action<TestMessage> configure = null)
    {
        var message = new TestMessage
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Content = _faker.Lorem.Sentence(),
            Metadata = new Dictionary<string, string>
            {
                ["Source"] = "TestDataFactory",
                ["Environment"] = "Test"
            }
        };

        configure?.Invoke(message);
        return message;
    }

    public static List<TestMessage> CreateMessages(int count)
    {
        return Enumerable.Range(0, count)
            .Select(_ => CreateMessage())
            .ToList();
    }

    public static RawRabbitConfiguration CreateConfiguration(
        Action<RawRabbitConfiguration> configure = null)
    {
        var config = new RawRabbitConfiguration
        {
            Hostnames = new List<string> { "localhost" },
            Port = 5672,
            VirtualHost = "/test",
            Username = "guest",
            Password = "guest",
            RequestTimeout = TimeSpan.FromSeconds(10),
            RecoveryInterval = TimeSpan.FromSeconds(5)
        };

        configure?.Invoke(config);
        return config;
    }
}
```

### 8.4 Parallel Test Execution

**xUnit Parallel Configuration** (`xunit.runner.json`):
```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "methodDisplay": "method",
  "methodDisplayOptions": "all",
  "diagnosticMessages": false,
  "internalDiagnosticMessages": false,
  "maxParallelThreads": 4,
  "parallelizeAssembly": true,
  "parallelizeTestCollections": true,
  "preEnumerateTheories": true,
  "shadowCopy": false,
  "stopOnFail": false
}
```

**Collection Configuration**:
```csharp
// Non-parallelizable tests (shared RabbitMQ)
[CollectionDefinition("RabbitMQ", DisableParallelization = true)]
public class RabbitMqCollection : ICollectionFixture<RabbitMqFixture>
{
}

// Parallelizable tests (isolated)
[Collection("Unit")]
public class IsolatedTests
{
    // These can run in parallel
}
```

### 8.5 Test Report Generation

**Report Generator Script** (`scripts/generate-test-report.sh`):
```bash
#!/bin/bash

REPORT_DATE=$(date +%Y-%m-%d)
OUTPUT_DIR="docs/test"

# Run tests with logging
dotnet test --configuration Release \
  --logger "trx;LogFileName=test-results.trx" \
  --logger "html;LogFileName=test-results.html" \
  --collect:"XPlat Code Coverage" \
  --results-directory ./TestResults

# Generate coverage report
reportgenerator \
  -reports:"./TestResults/**/coverage.cobertura.xml" \
  -targetdir:"./TestResults/CoverageReport" \
  -reporttypes:"Html;Badges;MarkdownSummaryGithub"

# Extract metrics
TOTAL_TESTS=$(grep -oP 'total="\K[0-9]+' ./TestResults/test-results.trx | head -1)
PASSED=$(grep -oP 'passed="\K[0-9]+' ./TestResults/test-results.trx | head -1)
FAILED=$(grep -oP 'failed="\K[0-9]+' ./TestResults/test-results.trx | head -1)
COVERAGE=$(grep -oP 'line-rate="\K[0-9.]+' ./TestResults/CoverageReport/Cobertura.xml)
COVERAGE_PCT=$(echo "$COVERAGE * 100" | bc)

# Generate markdown report
cat > "$OUTPUT_DIR/unit/unit-test-$REPORT_DATE-net9-migration.md" << EOF
# Unit Test Report: .NET 9 Migration

**Date**: $REPORT_DATE
**Target Framework**: net9.0
**Test Framework**: xUnit 2.6.2

## Summary
- Total Tests: $TOTAL_TESTS
- Passed: $PASSED
- Failed: $FAILED
- Skipped: 0
- Success Rate: $(echo "scale=1; $PASSED * 100 / $TOTAL_TESTS" | bc)%

## Coverage
- Overall: ${COVERAGE_PCT}%

See detailed coverage report: ./TestResults/CoverageReport/index.html

## Failed Tests
$(if [ "$FAILED" -gt 0 ]; then
    grep -A 5 'outcome="Failed"' ./TestResults/test-results.trx
else
    echo "None"
fi)

## Notes
Test execution successful. All tests passing on .NET 9.
EOF

echo "Test report generated: $OUTPUT_DIR/unit/unit-test-$REPORT_DATE-net9-migration.md"
```

---

## 9. Test Reporting

### 9.1 Report Templates

**Unit Test Report Template**:
```markdown
# Unit Test Report: [Description]

**Date**: YYYY-MM-DD
**Target Framework**: net6.0 / net8.0 / net9.0
**Test Framework**: xUnit 2.6.2
**Branch**: [branch-name]
**Commit**: [commit-hash]

## Summary
- Total Tests: XXX
- Passed: XXX
- Failed: XXX
- Skipped: XXX
- Duration: XX.XXs
- Success Rate: XX.X%

## Coverage
- Overall: XX.X%
- RawRabbit (Core): XX.X%
- RawRabbit.Operations.*: XX.X%
- RawRabbit.Enrichers.*: XX.X%
- RawRabbit.DependencyInjection.*: XX.X%

## Coverage by Component

| Component | Lines Covered | Total Lines | Coverage % | Target | Status |
|-----------|--------------|-------------|-----------|--------|--------|
| RawRabbit | XXX | XXX | XX.X% | 80% | ✅/⚠️/❌ |
| Operations.Publish | XXX | XXX | XX.X% | 70% | ✅/⚠️/❌ |
| ... | ... | ... | ... | ... | ... |

## Failed Tests

### [Test Class].[Test Method]
**Error**: [Error message]
**Stack Trace**:
```
[Stack trace]
```
**Resolution**: [How to fix]

## Performance Notes
[Any performance observations during test execution]

## Notes
[Any relevant observations, blockers, or follow-up actions]
```

**Integration Test Report Template**:
```markdown
# Integration Test Report: [Component]

**Date**: YYYY-MM-DD
**Target Framework**: net9.0
**RabbitMQ Version**: X.X.X
**Branch**: [branch-name]

## Test Environment
- OS: [Operating System]
- .NET SDK: [Version]
- RabbitMQ: [Version and setup]
- Docker: [Version]

## Test Scenarios

### 1. [Scenario Name]
- **Status**: ✅ PASS / ❌ FAIL
- **Duration**: XXs
- **Description**: [What was tested]
- **Expected**: [Expected outcome]
- **Actual**: [Actual outcome]
- **Notes**: [Any observations]

### 2. [Scenario Name]
- **Status**: ✅ PASS / ❌ FAIL
- **Duration**: XXs
- **Description**: [What was tested]
- **Expected**: [Expected outcome]
- **Actual**: [Actual outcome]
- **Notes**: [Any observations]

## Issues Found

### Issue #1: [Title]
**Severity**: CRITICAL / HIGH / MEDIUM / LOW
**Description**: [Detailed description]
**Reproduction Steps**:
1. [Step 1]
2. [Step 2]
**Expected Behavior**: [Expected]
**Actual Behavior**: [Actual]
**Workaround**: [If available]
**GitHub Issue**: #XXX

## Notes
[Any relevant observations or follow-up actions]
```

**Performance Test Report Template**:
```markdown
# Performance Benchmark Report: [Description]

**Date**: YYYY-MM-DD
**Framework**: net9.0
**BenchmarkDotNet Version**: 0.13.x
**Branch**: [branch-name]

## System Configuration
- OS: [Operating System]
- CPU: [Processor model]
- Memory: [RAM size]
- .NET Runtime: [Version]

## Benchmark Results

### [Benchmark Name]

| Metric | Baseline (netstandard1.5) | Current (net9.0) | Change | Threshold | Status |
|--------|--------------------------|------------------|--------|-----------|--------|
| Mean | XX.XX μs | XX.XX μs | +/- X.X% | +20% | ✅/⚠️/❌ |
| P50 | XX.XX μs | XX.XX μs | +/- X.X% | - | - |
| P95 | XX.XX μs | XX.XX μs | +/- X.X% | +25% | ✅/⚠️/❌ |
| P99 | XX.XX μs | XX.XX μs | +/- X.X% | - | - |
| Allocated | XXX B | XXX B | +/- X.X% | +30% | ✅/⚠️/❌ |
| Throughput | XXX msg/s | XXX msg/s | +/- X.X% | -15% | ✅/⚠️/❌ |

**Status Legend**:
- ✅ Within threshold (acceptable)
- ⚠️ WARNING threshold exceeded (review required)
- ❌ BLOCKER threshold exceeded (requires fix)

## Regression Analysis

### BLOCKER Regressions
[List any regressions exceeding BLOCKER thresholds]

### WARNING Regressions
[List any regressions exceeding WARNING thresholds]

## Optimization Opportunities
[Notes on potential improvements or explanations for regressions]

## Detailed Results
[Include full BenchmarkDotNet output or link to artifacts]
```

### 9.2 Report Storage Locations

**Directory Structure**:
```
docs/test/
├── unit/
│   ├── unit-test-2025-10-09-phase1-core.md
│   ├── unit-test-2025-10-16-phase2-operations.md
│   └── unit-test-2025-10-23-phase3-enrichers.md
├── integration/
│   ├── integration-test-2025-10-09-rabbitmq-3.11.md
│   ├── integration-test-2025-10-09-rabbitmq-3.12.md
│   └── integration-test-2025-10-16-ssl-tls.md
├── performance/
│   ├── performance-2025-10-09-net9-baseline.md
│   ├── performance-2025-10-16-net9-phase2.md
│   └── performance-2025-10-23-net9-final.md
└── security/
    ├── security-scan-2025-10-09-dotnet-list-package.md
    └── security-scan-2025-10-23-post-migration.md
```

### 9.3 Automated Report Generation

**GitHub Actions Reporting**:
```yaml
# .github/workflows/test-report.yml (partial)
- name: Generate Test Report
  if: always()
  run: bash scripts/generate-test-report.sh

- name: Publish Test Results
  if: always()
  uses: EnricoMi/publish-unit-test-result-action@v2
  with:
    files: TestResults/**/*.trx
    check_name: "Test Results"
    comment_title: "Test Results"

- name: Publish Coverage Summary
  uses: irongut/CodeCoverageSummary@v1.3.0
  with:
    filename: TestResults/CoverageReport/Cobertura.xml
    badge: true
    format: markdown
    output: both

- name: Comment PR
  if: github.event_name == 'pull_request'
  uses: actions/github-script@v7
  with:
    script: |
      const fs = require('fs');
      const coverage = fs.readFileSync('code-coverage-results.md', 'utf8');

      github.rest.issues.createComment({
        issue_number: context.issue.number,
        owner: context.repo.owner,
        repo: context.repo.repo,
        body: `## Test Results\n\n${coverage}`
      });
```

---

## 10. Conclusion

This comprehensive test strategy provides a systematic approach to validating the RawRabbit .NET 9 migration across all 32 projects in 6 phases. The strategy ensures:

**Quality Assurance**:
- 75%+ overall code coverage with component-specific targets
- Unit, integration, performance, and security testing at each phase
- Automated regression detection with BLOCKER and WARNING thresholds

**Risk Mitigation**:
- Early detection of performance regressions
- RabbitMQ compatibility validation across versions 3.11.x and 3.12.x
- Security validation of CVE fixes (Newtonsoft.Json, RabbitMQ.Client)

**Continuous Validation**:
- CI/CD integration with GitHub Actions
- Multi-platform testing (Linux, Windows, macOS)
- Multi-framework testing (.NET 6, 8, 9)

**Documentation & Reporting**:
- Standardized test report templates
- Automated report generation and artifact publishing
- Performance baseline tracking

**Success Criteria**:
- All tests pass on .NET 6, 8, 9
- Coverage requirements met (75%+ overall, 80%+ core, 70%+ operations, 60%+ enrichers, 50%+ DI)
- No BLOCKER performance regressions (+20% execution time, +25% P95 latency, -15% throughput)
- All CRITICAL and HIGH CVEs resolved
- RabbitMQ compatibility validated

This strategy aligns with the migration roadmap (6 phases over 8 weeks) and provides the quality gates necessary for a successful .NET 9 upgrade.

---

**Document Metadata**

**Status**: Complete
**Version**: 1.0
**Last Updated**: 2025-10-09
**Next Review**: After Phase 1 completion
**Approval Required**: Migration Architect, QA Lead, DevOps Engineer

**Related Documents**:
- [Migration Roadmap](../stage-1/migration-roadmap.md)
- [Security Baseline Report](../stage-1/security-baseline-report.md)
- [Test Report Standards](../test/README.md)

---

**End of Test Strategy Document**
