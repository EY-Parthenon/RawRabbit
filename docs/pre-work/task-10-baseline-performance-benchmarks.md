# Task 10: Baseline Performance Benchmark Design

**Role**: Performance Engineer
**Session ID**: dotnet9-upgrade
**Branch**: pre-work
**Date**: 2025-10-09

---

## Executive Summary

This document defines a comprehensive performance benchmark suite for RawRabbit to establish baseline metrics before the .NET 9 upgrade and validate that the migration does not introduce performance regressions.

**Key Findings**:
- **Existing Infrastructure**: RawRabbit has basic BenchmarkDotNet setup (v0.10.3) with 3 benchmark classes covering core operations
- **Coverage Gaps**: Missing benchmarks for serialization, channel pooling, connection management, and middleware pipeline
- **Critical Operations**: 12 identified operations requiring benchmarking across 4 categories
- **Upgrade Risk**: .NET 9 introduces changes to async/await, Task infrastructure, and System.Text.Json that could impact performance

---

## 1. Current Performance Testing State

### 1.1 Existing Benchmark Infrastructure

**Project**: `test/RawRabbit.PerformanceTest`

**Configuration**:
```xml
<TargetFramework>netcoreapp1.1</TargetFramework>
<PackageReference Include="BenchmarkDotNet" Version="0.10.3" />
```

**Status**:
- ⚠️ **OUTDATED**: BenchmarkDotNet 0.10.3 (March 2017) vs latest 0.14.0 (2024)
- ⚠️ **OLD FRAMEWORK**: netcoreapp1.1 (EOL June 2019)
- ✅ **GOOD FOUNDATION**: 3 benchmark classes with proper Setup/Cleanup

### 1.2 Existing Benchmark Classes

#### PubSubBenchmarks.cs
**Operations Covered**:
- `ConsumerAcknowledgements_Off` - Publish/subscribe with acks disabled
- `ConsumerAcknowledgements_On` - Publish/subscribe with acks enabled
- `DeliveryMode_NonPersistant` - Non-persistent message delivery
- `DeliveryMode_Persistant` - Persistent message delivery

**Assessment**: ✅ Good coverage of pub/sub scenarios

#### RpcBenchmarks.cs
**Operations Covered**:
- `DirectRpc` - Direct request/response pattern
- `NormalRpc` - Request/response with custom exchange/queue configuration

**Assessment**: ✅ Covers RPC patterns, but missing timeout scenarios

#### MessageContextBenchmarks.cs
**Operations Covered**:
- `MessageContext_FromFactory` - Message context enrichment enabled
- `MessageContext_None` - No message context (baseline)

**Assessment**: ✅ Good comparison of enrichment overhead

### 1.3 Coverage Gaps

**Missing Critical Operations**:
1. ❌ **Serialization Performance**
   - JSON serialization (Newtonsoft.Json)
   - JSON deserialization
   - Alternative serializers (Protobuf, MessagePack, ZeroFormatter)
   - Serializer comparison benchmarks

2. ❌ **Channel Management**
   - Channel creation time
   - Channel pooling overhead (AutoScalingChannelPool)
   - Channel scaling operations
   - Connection recovery time

3. ❌ **Middleware Pipeline**
   - Pipeline execution overhead
   - Middleware chaining performance
   - Context propagation cost

4. ❌ **Throughput Tests**
   - Bulk publish operations
   - Concurrent subscriber throughput
   - Connection saturation tests

5. ❌ **Memory & Allocation**
   - Memory allocations per operation
   - GC pressure measurements
   - Object pooling effectiveness

---

## 2. Critical Operations Requiring Benchmarking

### Category A: Message Operations (High Priority)

| Operation | Current Coverage | .NET 9 Risk | Priority |
|-----------|-----------------|-------------|----------|
| Publish (single) | ✅ Covered | Medium (async changes) | HIGH |
| Subscribe (single) | ✅ Covered | Medium (async changes) | HIGH |
| Request/Response | ✅ Covered | Medium (Task infrastructure) | HIGH |
| Bulk Publish (100 msgs) | ❌ Missing | High (async perf) | **CRITICAL** |
| Concurrent Subscribers | ❌ Missing | High (thread pool) | **CRITICAL** |

### Category B: Serialization (Critical Priority)

| Operation | Current Coverage | .NET 9 Risk | Priority |
|-----------|-----------------|-------------|----------|
| JSON Serialize (small) | ❌ Missing | **HIGH** (Newtonsoft.Json CVE) | **CRITICAL** |
| JSON Deserialize (small) | ❌ Missing | **HIGH** (potential migration) | **CRITICAL** |
| JSON Serialize (large) | ❌ Missing | **HIGH** (memory allocations) | **CRITICAL** |
| Protobuf Serialize | ❌ Missing | Medium (binary serialization) | HIGH |
| MessagePack Serialize | ❌ Missing | Medium (binary serialization) | HIGH |
| ZeroFormatter Serialize | ❌ Missing | **HIGH** (deprecated, no support) | **CRITICAL** |

**Rationale**:
- Newtonsoft.Json has CRITICAL CVEs (CVE-2024-21907, CVE-2024-21908)
- Potential migration to System.Text.Json in .NET 9
- ZeroFormatter deprecated (archived 2018) - need baseline before removal

### Category C: Infrastructure (High Priority)

| Operation | Current Coverage | .NET 9 Risk | Priority |
|-----------|-----------------|-------------|----------|
| Channel Creation | ❌ Missing | Medium (IModel changes) | HIGH |
| Channel Pool Get | ❌ Missing | Medium (concurrency) | HIGH |
| Connection Establish | ❌ Missing | Low (RabbitMQ.Client stable) | MEDIUM |
| Connection Recovery | ❌ Missing | Medium (async recovery) | HIGH |

### Category D: Advanced Patterns (Medium Priority)

| Operation | Current Coverage | .NET 9 Risk | Priority |
|-----------|-----------------|-------------|----------|
| Middleware Pipeline (3 stages) | ❌ Missing | Medium (async overhead) | MEDIUM |
| Message Context Enrichment | ✅ Covered | Low (covered) | MEDIUM |
| Queue Suffix Resolution | ❌ Missing | Low (string operations) | LOW |
| Retry Later Pattern | ❌ Missing | Medium (timing precision) | MEDIUM |

---

## 3. Proposed Benchmark Suite Design

### 3.1 Project Structure

```
test/
  RawRabbit.PerformanceTest/
    Benchmarks/
      MessageOperationBenchmarks.cs      (Category A - replaces PubSubBenchmarks)
      RpcBenchmarks.cs                   (Category A - keep & enhance)
      SerializationBenchmarks.cs         (Category B - NEW)
      ChannelManagementBenchmarks.cs     (Category C - NEW)
      MiddlewarePipelineBenchmarks.cs    (Category D - NEW)
      ThroughputBenchmarks.cs            (Category A - NEW)
    Harness.cs                           (Main entry point)
    RawRabbit.PerformanceTest.csproj
```

### 3.2 Enhanced Project Configuration

**Target Frameworks**: Multi-targeting for comparison
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <!-- Multi-target to compare current vs .NET 9 -->
    <TargetFrameworks>netcoreapp3.1;net6.0;net9.0</TargetFrameworks>
    <OutputType>Exe</OutputType>
    <PlatformTarget>AnyCPU</PlatformTarget>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <!-- Upgrade BenchmarkDotNet from 0.10.3 to 0.14.0 -->
    <PackageReference Include="BenchmarkDotNet" Version="0.14.0" />
    <PackageReference Include="BenchmarkDotNet.Diagnostics.Windows" Version="0.14.0" Condition="'$(OS)' == 'Windows_NT'" />

    <!-- Keep xUnit for harness tests -->
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.0" />
    <PackageReference Include="xunit" Version="2.9.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  </ItemGroup>

  <ItemGroup>
    <!-- Core dependencies -->
    <ProjectReference Include="..\..\src\RawRabbit\RawRabbit.csproj" />
    <ProjectReference Include="..\..\src\RawRabbit.Operations.Publish\RawRabbit.Operations.Publish.csproj" />
    <ProjectReference Include="..\..\src\RawRabbit.Operations.Subscribe\RawRabbit.Operations.Subscribe.csproj" />
    <ProjectReference Include="..\..\src\RawRabbit.Operations.Request\RawRabbit.Operations.Request.csproj" />
    <ProjectReference Include="..\..\src\RawRabbit.Operations.Respond\RawRabbit.Operations.Respond.csproj" />

    <!-- Serializers for comparison -->
    <ProjectReference Include="..\..\src\RawRabbit.Enrichers.Protobuf\RawRabbit.Enrichers.Protobuf.csproj" />
    <ProjectReference Include="..\..\src\RawRabbit.Enrichers.MessagePack\RawRabbit.Enrichers.MessagePack.csproj" />
    <ProjectReference Include="..\..\src\RawRabbit.Enrichers.ZeroFormatter\RawRabbit.Enrichers.ZeroFormatter.csproj" />

    <!-- Enrichers -->
    <ProjectReference Include="..\..\src\RawRabbit.Enrichers.MessageContext\RawRabbit.Enrichers.MessageContext.csproj" />
    <ProjectReference Include="..\..\src\RawRabbit.Enrichers.QueueSuffix\RawRabbit.Enrichers.QueueSuffix.csproj" />
  </ItemGroup>
</Project>
```

### 3.3 Benchmark Configuration

**BenchmarkDotNet Config**:
```csharp
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Columns;

public class RawRabbitBenchmarkConfig : ManualConfig
{
    public RawRabbitBenchmarkConfig()
    {
        // Job configurations for multi-framework comparison
        AddJob(Job.Default
            .WithRuntime(CoreRuntime.Core31)
            .WithId(".NET Core 3.1"));

        AddJob(Job.Default
            .WithRuntime(CoreRuntime.Core60)
            .WithId(".NET 6.0"));

        AddJob(Job.Default
            .WithRuntime(CoreRuntime.Core90)
            .WithId(".NET 9.0"));

        // Diagnosers for detailed metrics
        AddDiagnoser(MemoryDiagnoser.Default);        // Memory allocations
        AddDiagnoser(ThreadingDiagnoser.Default);     // Thread pool usage

        // Exporters
        AddExporter(MarkdownExporter.GitHub);         // GitHub-friendly markdown
        AddExporter(CsvExporter.Default);             // CSV for analysis
        AddExporter(HtmlExporter.Default);            // HTML report
        AddExporter(JsonExporter.FullCompressed);     // JSON for tooling

        // Columns
        AddColumn(StatisticColumn.Mean);              // Average execution time
        AddColumn(StatisticColumn.StdDev);            // Standard deviation
        AddColumn(StatisticColumn.Median);            // Median execution time
        AddColumn(StatisticColumn.P95);               // 95th percentile
        AddColumn(RankColumn.Arabic);                 // Ranking

        // Options
        WithOptions(ConfigOptions.DisableOptimizationsValidator); // Allow unoptimized builds for debugging
    }
}
```

---

## 4. Detailed Benchmark Specifications

### 4.1 SerializationBenchmarks.cs (NEW - CRITICAL)

**Purpose**: Compare serialization performance and establish baseline before potential Newtonsoft.Json → System.Text.Json migration

```csharp
[Config(typeof(RawRabbitBenchmarkConfig))]
[MemoryDiagnoser]
public class SerializationBenchmarks
{
    private ISerializer _newtonsoftJson;
    private ISerializer _systemTextJson;  // For .NET 9 comparison
    private ISerializer _protobuf;
    private ISerializer _messagePack;
    private ISerializer _zeroFormatter;

    private SmallMessage _smallMsg;       // 10 properties, ~500 bytes
    private MediumMessage _mediumMsg;     // 50 properties, ~5KB
    private LargeMessage _largeMsg;       // 200 properties, ~50KB

    [GlobalSetup]
    public void Setup()
    {
        // Initialize serializers
        _newtonsoftJson = new JsonSerializer(new Newtonsoft.Json.JsonSerializer());
        _systemTextJson = new SystemTextJsonSerializer();  // NEW for .NET 9
        _protobuf = new ProtobufSerializer();
        _messagePack = new MessagePackSerializerWorker();
        _zeroFormatter = new ZeroFormatterSerializerWorker();

        // Initialize test messages
        _smallMsg = MessageFactory.CreateSmall();
        _mediumMsg = MessageFactory.CreateMedium();
        _largeMsg = MessageFactory.CreateLarge();
    }

    [Benchmark(Baseline = true)]
    public byte[] NewtonsoftJson_Serialize_Small()
        => _newtonsoftJson.Serialize(_smallMsg);

    [Benchmark]
    public byte[] SystemTextJson_Serialize_Small()
        => _systemTextJson.Serialize(_smallMsg);

    [Benchmark]
    public byte[] Protobuf_Serialize_Small()
        => _protobuf.Serialize(_smallMsg);

    [Benchmark]
    public byte[] MessagePack_Serialize_Small()
        => _messagePack.Serialize(_smallMsg);

    [Benchmark]
    public byte[] ZeroFormatter_Serialize_Small()
        => _zeroFormatter.Serialize(_smallMsg);

    // Repeat for Medium and Large message sizes
    // Deserialize benchmarks for each serializer
}
```

**Expected Baseline Results** (netcoreapp3.1):
- Newtonsoft.Json Small: ~5-10 μs, ~2KB allocations
- Protobuf Small: ~2-4 μs, ~500B allocations
- MessagePack Small: ~3-5 μs, ~800B allocations
- ZeroFormatter Small: ~1-2 μs, ~200B allocations (fastest but deprecated)

**Critical Metrics**:
- ⚠️ **Serialization time increase > 20%** = REGRESSION
- ⚠️ **Memory allocations increase > 30%** = REGRESSION
- ✅ **System.Text.Json within 10% of Newtonsoft.Json** = ACCEPTABLE for migration

### 4.2 ThroughputBenchmarks.cs (NEW - CRITICAL)

**Purpose**: Measure bulk operation performance and concurrent throughput

```csharp
[Config(typeof(RawRabbitBenchmarkConfig))]
[MemoryDiagnoser]
public class ThroughputBenchmarks
{
    private IBusClient _busClient;
    private List<Message> _messages100;
    private List<Message> _messages1000;

    [GlobalSetup]
    public void Setup()
    {
        _busClient = RawRabbitFactory.CreateSingleton();
        _messages100 = Enumerable.Range(0, 100).Select(i => new Message { Id = i }).ToList();
        _messages1000 = Enumerable.Range(0, 1000).Select(i => new Message { Id = i }).ToList();

        // Setup subscriber
        _busClient.SubscribeAsync<Message>(msg => Task.CompletedTask);
    }

    [Benchmark]
    [Arguments(10)]
    [Arguments(100)]
    [Arguments(1000)]
    public async Task BulkPublish(int messageCount)
    {
        var messages = _messages100.Take(messageCount);
        var tasks = messages.Select(msg => _busClient.PublishAsync(msg));
        await Task.WhenAll(tasks);
    }

    [Benchmark]
    [Arguments(1)]
    [Arguments(5)]
    [Arguments(10)]
    public async Task ConcurrentSubscribers(int subscriberCount)
    {
        var subscribers = Enumerable.Range(0, subscriberCount)
            .Select(_ => _busClient.SubscribeAsync<Message>(msg => Task.CompletedTask))
            .ToList();

        await Task.WhenAll(subscribers);

        // Publish 100 messages
        var publishTasks = _messages100.Select(msg => _busClient.PublishAsync(msg));
        await Task.WhenAll(publishTasks);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _busClient.DeleteQueueAsync<Message>();
        (_busClient as IDisposable)?.Dispose();
    }
}
```

**Expected Baseline Results**:
- 10 messages: ~50-100 ms
- 100 messages: ~300-500 ms
- 1000 messages: ~2-5 seconds
- 5 concurrent subscribers: ~400-800 ms for 100 messages

**Critical Metrics**:
- ⚠️ **Throughput decrease > 15%** = REGRESSION
- ⚠️ **Latency increase (P95) > 25%** = REGRESSION

### 4.3 ChannelManagementBenchmarks.cs (NEW - HIGH PRIORITY)

**Purpose**: Measure channel pooling and connection management overhead

```csharp
[Config(typeof(RawRabbitBenchmarkConfig))]
[MemoryDiagnoser]
public class ChannelManagementBenchmarks
{
    private IChannelFactory _channelFactory;
    private AutoScalingChannelPool _channelPool;

    [GlobalSetup]
    public void Setup()
    {
        var config = new RawRabbitConfiguration { /* config */ };
        var connectionFactory = new ConnectionFactory();
        _channelFactory = new ChannelFactory(connectionFactory, config);

        var poolOptions = AutoScalingOptions.Default;
        _channelPool = new AutoScalingChannelPool(_channelFactory, poolOptions);
    }

    [Benchmark]
    public async Task<IModel> CreateChannel()
    {
        return await _channelFactory.CreateChannelAsync();
    }

    [Benchmark]
    public async Task<IModel> GetChannelFromPool()
    {
        return await _channelPool.GetAsync();
    }

    [Benchmark]
    public async Task<IConnection> ConnectionRecovery()
    {
        // Simulate connection loss and recovery
        var connection = await _channelFactory.ConnectAsync();
        // Trigger recovery
        return connection;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _channelPool?.Dispose();
        _channelFactory?.Dispose();
    }
}
```

**Expected Baseline Results**:
- Create Channel: ~5-15 ms
- Get from Pool (warm): ~50-200 μs
- Connection Recovery: ~100-500 ms

**Critical Metrics**:
- ⚠️ **Channel creation > 20 ms** = PERFORMANCE ISSUE
- ⚠️ **Pool overhead > 500 μs** = REGRESSION

### 4.4 MiddlewarePipelineBenchmarks.cs (NEW - MEDIUM PRIORITY)

**Purpose**: Measure middleware pipeline execution overhead

```csharp
[Config(typeof(RawRabbitBenchmarkConfig))]
[MemoryDiagnoser]
public class MiddlewarePipelineBenchmarks
{
    private IPipeBuilder _pipeBuilder;
    private IPipeContext _context;

    [GlobalSetup]
    public void Setup()
    {
        _pipeBuilder = /* create pipe builder */;
        _context = /* create context */;
    }

    [Benchmark(Baseline = true)]
    public async Task Pipeline_NoMiddleware()
    {
        var pipe = _pipeBuilder.Build();
        await pipe.InvokeAsync(_context, CancellationToken.None);
    }

    [Benchmark]
    public async Task Pipeline_3Middleware()
    {
        var pipe = _pipeBuilder
            .Use<BodySerializationMiddleware>()
            .Use<HeaderSerializationMiddleware>()
            .Use<BasicPropertiesMiddleware>()
            .Build();
        await pipe.InvokeAsync(_context, CancellationToken.None);
    }

    [Benchmark]
    public async Task Pipeline_10Middleware()
    {
        var pipe = _pipeBuilder
            .Use<BodySerializationMiddleware>()
            .Use<HeaderSerializationMiddleware>()
            .Use<BasicPropertiesMiddleware>()
            .Use<ChannelCreationMiddleware>()
            .Use<ExchangeDeclareMiddleware>()
            .Use<QueueDeclareMiddleware>()
            .Use<QueueBindMiddleware>()
            .Use<PublishAcknowledgeMiddleware>()
            .Use<ExplicitAckMiddleware>()
            .Use<MessageContextMiddleware>()
            .Build();
        await pipe.InvokeAsync(_context, CancellationToken.None);
    }
}
```

**Expected Baseline Results**:
- No middleware: ~10-50 μs
- 3 middleware: ~50-150 μs
- 10 middleware: ~200-500 μs

**Critical Metrics**:
- ⚠️ **Per-middleware overhead > 50 μs** = REGRESSION

---

## 5. Baseline Metrics to Capture

### 5.1 Primary Metrics

**Execution Time**:
- Mean (average)
- Median (50th percentile)
- P95 (95th percentile)
- P99 (99th percentile)
- Standard Deviation

**Memory**:
- Allocations per operation (bytes)
- Gen0 collections
- Gen1 collections
- Gen2 collections (critical)

**Throughput**:
- Operations per second
- Messages per second
- Bytes processed per second

### 5.2 Regression Thresholds

| Metric | Threshold | Severity |
|--------|-----------|----------|
| Mean execution time increase | > 20% | 🔴 BLOCKER |
| P95 latency increase | > 25% | 🔴 BLOCKER |
| Memory allocations increase | > 30% | 🟡 WARNING |
| Throughput decrease | > 15% | 🔴 BLOCKER |
| Gen2 collections increase | > 50% | 🟡 WARNING |

### 5.3 Baseline Report Format

**Output Directory**: `BenchmarkDotNet.Artifacts/results/`

**Generated Files**:
- `RawRabbit.PerformanceTest.SerializationBenchmarks-report.md` (GitHub markdown)
- `RawRabbit.PerformanceTest.SerializationBenchmarks-report.html` (HTML)
- `RawRabbit.PerformanceTest.SerializationBenchmarks-report.csv` (CSV)
- `RawRabbit.PerformanceTest.SerializationBenchmarks-report-full.json` (JSON)

**Baseline Archive**:
- Commit baseline results to `docs/benchmarks/baselines/netcoreapp3.1/`
- Include hardware specs, OS version, .NET runtime version
- Tag with git: `git tag baseline-pre-net9 -m "Performance baseline before .NET 9 migration"`

---

## 6. BenchmarkDotNet Setup Instructions

### 6.1 Prerequisites

**Local Development**:
1. .NET 9 SDK installed
2. Docker installed (for RabbitMQ container)
3. Admin/sudo access (for hardware counters on Linux)

**CI/CD**:
1. Dedicated benchmark runner (consistent hardware)
2. No competing processes during benchmarks
3. Sufficient memory (8GB+ recommended)

### 6.2 Running Benchmarks

**Command Line**:
```bash
# Navigate to benchmark project
cd test/RawRabbit.PerformanceTest

# Build in Release mode (REQUIRED for accurate results)
dotnet build -c Release

# Run all benchmarks
dotnet run -c Release --framework net9.0

# Run specific benchmark class
dotnet run -c Release --framework net9.0 --filter "*SerializationBenchmarks*"

# Run with specific job (framework comparison)
dotnet run -c Release --runtimes net6.0 net9.0

# Generate baseline (first run)
dotnet run -c Release --exporters json markdown csv html
```

**Docker RabbitMQ Setup**:
```bash
# Start RabbitMQ for benchmarks
docker run -d --name rabbitmq-benchmark \
  -p 5672:5672 \
  -p 15672:15672 \
  -e RABBITMQ_DEFAULT_USER=guest \
  -e RABBITMQ_DEFAULT_PASS=guest \
  rabbitmq:3-management

# Verify RabbitMQ is ready
docker logs rabbitmq-benchmark | grep "Server startup complete"

# Stop after benchmarks
docker stop rabbitmq-benchmark
docker rm rabbitmq-benchmark
```

### 6.3 Benchmark Harness for xUnit

**Purpose**: Run benchmarks as part of test suite (not for performance measurement, just validation)

```csharp
public class Harness
{
    [Fact]
    public void SerializationBenchmarks()
    {
        var result = BenchmarkRunner.Run<SerializationBenchmarks>();
        Assert.NotEqual(TimeSpan.Zero, result.TotalTime);
    }

    [Fact]
    public void ThroughputBenchmarks()
    {
        var result = BenchmarkRunner.Run<ThroughputBenchmarks>();
        Assert.NotEqual(TimeSpan.Zero, result.TotalTime);
    }

    [Fact]
    public void ChannelManagementBenchmarks()
    {
        var result = BenchmarkRunner.Run<ChannelManagementBenchmarks>();
        Assert.NotEqual(TimeSpan.Zero, result.TotalTime);
    }

    [Fact]
    public void MiddlewarePipelineBenchmarks()
    {
        var result = BenchmarkRunner.Run<MiddlewarePipelineBenchmarks>();
        Assert.NotEqual(TimeSpan.Zero, result.TotalTime);
    }
}
```

⚠️ **WARNING**: xUnit tests run in Debug mode and don't produce accurate performance metrics. Use `dotnet run -c Release` for real benchmarking.

---

## 7. .NET Current vs .NET 9 Comparison Plan

### 7.1 Multi-Framework Comparison

**Strategy**: Use BenchmarkDotNet's multi-framework support to run identical benchmarks on multiple runtimes

```xml
<TargetFrameworks>netcoreapp3.1;net6.0;net9.0</TargetFrameworks>
```

**Execution**:
```bash
dotnet run -c Release --runtimes netcoreapp3.1 net6.0 net9.0
```

**Output**: Side-by-side comparison table
```
|                   Method |        Runtime |       Mean |     Error |    StdDev |
|------------------------- |--------------- |-----------:|----------:|----------:|
| Newtonsoft_Serialize_Sml | .NET Core 3.1  |   7.234 μs | 0.0145 μs | 0.0136 μs |
| Newtonsoft_Serialize_Sml | .NET 6.0       |   6.892 μs | 0.0132 μs | 0.0123 μs |
| Newtonsoft_Serialize_Sml | .NET 9.0       |   6.543 μs | 0.0127 μs | 0.0119 μs |
```

### 7.2 .NET 9 Performance Expectations

**Expected Improvements**:
- ✅ **Async/Await**: 10-15% faster async operations (improved Task infrastructure)
- ✅ **LINQ**: 5-10% faster LINQ queries (optimized iterators)
- ✅ **Collections**: 5-20% faster collection operations (Span<T>, Memory<T>)
- ✅ **JSON (System.Text.Json)**: 20-40% faster than Newtonsoft.Json
- ✅ **GC**: Reduced GC pauses (improved generational GC)

**Potential Regressions**:
- ⚠️ **RabbitMQ.Client API changes**: Possible overhead from API adaptation
- ⚠️ **Breaking changes in Task infrastructure**: May require code changes
- ⚠️ **Deprecated APIs**: Removal of obsolete APIs may force less optimal alternatives

### 7.3 Comparison Report Structure

**Report Sections**:
1. **Executive Summary**: Overall performance change (+X% faster or -X% slower)
2. **Category Breakdown**: Performance change per benchmark category
3. **Regressions**: Detailed analysis of any operations slower in .NET 9
4. **Improvements**: Detailed analysis of operations faster in .NET 9
5. **Recommendations**: Code changes to leverage .NET 9 optimizations

**Deliverable**: `docs/benchmarks/net9-comparison-report.md`

---

## 8. Integration with .NET 9 Upgrade Stages

### Stage 6: Performance Validation (Week 13)

**Benchmark Execution Schedule**:

1. **Pre-Migration Baseline** (Week 1 - IMMEDIATE):
   - ✅ Run all benchmarks on netcoreapp3.1
   - ✅ Archive baseline results
   - ✅ Tag git commit: `baseline-pre-net9`

2. **Post-Migration Validation** (Week 13):
   - ✅ Run all benchmarks on net9.0
   - ✅ Generate comparison report
   - ✅ Identify regressions > 20%
   - ✅ Investigate and fix critical regressions

3. **Continuous Monitoring** (Week 14-15):
   - ✅ Run benchmarks after each optimization
   - ✅ Track performance improvements
   - ✅ Document optimization decisions in ADRs

**Success Criteria**:
- ❌ FAIL if any operation regresses > 20%
- ⚠️ WARN if any operation regresses > 10%
- ✅ PASS if overall performance within 10% or improved

### Stage 7: Security & Hardening (Week 14)

**Serialization Benchmark Focus**:
- ✅ Benchmark System.Text.Json as Newtonsoft.Json replacement
- ✅ Validate performance of secure deserialization (CVE mitigation)
- ✅ Measure overhead of added input validation

### Stage 8: Final Validation (Week 15)

**Full Suite Execution**:
- ✅ Run complete benchmark suite
- ✅ Generate final performance report
- ✅ Compare against pre-migration baseline
- ✅ Document performance improvements in CHANGELOG

---

## 9. Hardware & Environment Specifications

### 9.1 Benchmark Environment Requirements

**Minimum Requirements**:
- CPU: 4 cores, 2.5 GHz+
- RAM: 8 GB
- Disk: SSD (NVMe preferred)
- Network: Loopback (localhost) for RabbitMQ

**Recommended Configuration**:
- CPU: 8 cores, 3.5 GHz+
- RAM: 16 GB
- Disk: NVMe SSD with 500 MB/s+ write speed
- Network: 1 Gbps

**Environment Isolation**:
- ❌ NO competing processes (close browsers, IDEs, etc.)
- ❌ NO background tasks (Windows Update, antivirus scans)
- ❌ NO power saving mode (set to High Performance)
- ✅ Use dedicated benchmark runner in CI/CD

### 9.2 Baseline Metadata

**Record with Baseline**:
```json
{
  "timestamp": "2025-10-09T15:55:00Z",
  "git_commit": "2dcd9e1",
  "git_branch": "pre-work",
  "hardware": {
    "cpu": "Intel Core i7-9700K @ 3.6GHz",
    "ram": "32 GB DDR4-3200",
    "disk": "Samsung 970 EVO NVMe SSD",
    "os": "Ubuntu 22.04.3 LTS (Linux 6.16.10)"
  },
  "software": {
    "dotnet_sdk": "9.0.100",
    "dotnet_runtime": "9.0.0",
    "benchmarkdotnet": "0.14.0",
    "rabbitmq": "3.12.0",
    "rabbitmq_client": "5.0.1"
  },
  "configuration": {
    "build_mode": "Release",
    "optimization": true,
    "assertions": false
  }
}
```

---

## 10. Risk Mitigation

### 10.1 Known Performance Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Newtonsoft.Json → System.Text.Json migration** | HIGH (20-30% slower for complex types) | Benchmark both; optimize DTOs; consider protobuf for hot paths |
| **RabbitMQ.Client 5.x → 7.x breaking changes** | MEDIUM (API overhead) | Benchmark channel operations before/after; profile hot paths |
| **ZeroFormatter removal** | MEDIUM (fastest serializer lost) | Benchmark alternatives; recommend MessagePack as replacement |
| **Async/await overhead in .NET 9** | LOW (usually improved) | Benchmark throughput tests; profile Task allocations |
| **Channel pooling under .NET 9** | LOW (GC improvements) | Benchmark pool operations; validate scaling behavior |

### 10.2 Performance Regression Response Plan

**If regression > 20% detected**:

1. **Triage** (1 hour):
   - Verify benchmark configuration (Release mode, no debugger)
   - Re-run benchmark 3 times to confirm consistency
   - Check hardware/environment changes

2. **Investigation** (1 day):
   - Profile with dotnet-trace or PerfView
   - Identify hot path causing regression
   - Check .NET 9 breaking changes documentation

3. **Remediation** (2-5 days):
   - Option A: Code optimization to restore performance
   - Option B: API usage change to avoid slow path
   - Option C: Escalate to .NET team if runtime bug

4. **Validation** (1 day):
   - Re-run benchmarks after fix
   - Verify regression resolved
   - Document fix in ADR

**Total Timeline**: 4-7 days per critical regression

---

## 11. Next Steps

### Immediate Actions (Week 1)

1. **Upgrade BenchmarkDotNet** (1 hour):
   ```bash
   cd test/RawRabbit.PerformanceTest
   # Update .csproj: BenchmarkDotNet 0.10.3 → 0.14.0
   dotnet restore
   dotnet build -c Release
   ```

2. **Run Baseline Benchmarks** (2 hours):
   ```bash
   docker run -d --name rabbitmq-benchmark -p 5672:5672 rabbitmq:3-management
   dotnet run -c Release --framework netcoreapp3.1
   # Archive results to docs/benchmarks/baselines/netcoreapp3.1/
   git tag baseline-pre-net9 -m "Performance baseline before .NET 9 migration"
   ```

3. **Create Missing Benchmarks** (Stage 6 - Week 13):
   - SerializationBenchmarks.cs
   - ThroughputBenchmarks.cs
   - ChannelManagementBenchmarks.cs
   - MiddlewarePipelineBenchmarks.cs

4. **Document Baseline** (1 hour):
   - Create `docs/benchmarks/baseline-netcoreapp3.1.md`
   - Include hardware specs, OS version, software versions
   - Commit to git

### Stage 6 Actions (Week 13)

1. **Run .NET 9 Benchmarks** (2 hours):
   ```bash
   dotnet run -c Release --framework net9.0
   ```

2. **Generate Comparison Report** (4 hours):
   - Parse JSON results
   - Calculate percentage changes
   - Identify regressions > 20%
   - Create comparison report

3. **Investigate Regressions** (2-5 days per regression):
   - Profile with dotnet-trace
   - Analyze hot paths
   - Implement optimizations

4. **Validate Fixes** (1 day):
   - Re-run benchmarks
   - Verify regressions resolved

---

## 12. Success Criteria

### Benchmark Suite Completion

- ✅ BenchmarkDotNet upgraded to 0.14.0
- ✅ Multi-framework support (netcoreapp3.1, net6.0, net9.0)
- ✅ 4 new benchmark classes created (12 operations covered)
- ✅ Baseline results archived for netcoreapp3.1
- ✅ Git tag created: `baseline-pre-net9`

### Performance Validation

- ✅ No critical regressions (> 20%) in .NET 9
- ✅ Serialization performance within 10% of baseline
- ✅ Throughput performance within 15% of baseline
- ✅ Channel management overhead < 500 μs
- ✅ Comparison report generated and reviewed

### Documentation

- ✅ This design document (task-10-baseline-performance-benchmarks.md)
- ✅ Baseline results documented
- ✅ Comparison report (post-migration)
- ✅ HISTORY.md updated

---

## 13. Appendix: Benchmark Code Templates

### A. Message Factory

```csharp
public static class MessageFactory
{
    public static SmallMessage CreateSmall() => new SmallMessage
    {
        Id = Guid.NewGuid(),
        Name = "Test Message",
        Value = 42,
        Timestamp = DateTime.UtcNow,
        IsActive = true,
        Tags = new[] { "tag1", "tag2", "tag3" },
        Metadata = new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2"
        }
    };

    public static MediumMessage CreateMedium() => new MediumMessage
    {
        // 50 properties, ~5KB serialized
        // ...
    };

    public static LargeMessage CreateLarge() => new LargeMessage
    {
        // 200 properties, ~50KB serialized
        // Nested objects, collections, etc.
        // ...
    };
}
```

### B. System.Text.Json Serializer Adapter

```csharp
using System;
using System.Text;
using System.Text.Json;
using RawRabbit.Serialization;

public class SystemTextJsonSerializer : ISerializer
{
    private readonly JsonSerializerOptions _options;

    public string ContentType => "application/json";

    public SystemTextJsonSerializer()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public byte[] Serialize(object obj)
    {
        return JsonSerializer.SerializeToUtf8Bytes(obj, _options);
    }

    public object Deserialize(Type type, byte[] bytes)
    {
        return JsonSerializer.Deserialize(bytes, type, _options);
    }
}
```

---

## 14. References

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/articles/overview.html)
- [.NET 9 Performance Improvements](https://devblogs.microsoft.com/dotnet/performance-improvements-in-net-9/)
- [RawRabbit Documentation](https://github.com/pardahlman/RawRabbit)
- [RabbitMQ.Client Performance Guide](https://www.rabbitmq.com/dotnet-api-guide.html#performance)

---

**Document Status**: ✅ COMPLETE
**Next Task**: Update HISTORY.md
**Blocked By**: None
**Dependencies**: Docker (for RabbitMQ), .NET 9 SDK
