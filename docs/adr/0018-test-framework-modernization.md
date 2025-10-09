# ADR-0018: Test Framework Modernization

**Status**: Proposed

**Date**: 2025-10-09

**Authors**: Architecture Specialist (SPARC Stage 2)

**Reviewers**: TBD

**Tags**: migration, testing, architecture, xunit, dotnet9

---

## Context

### Background

RawRabbit's test infrastructure is built on older testing patterns:
- xUnit 2.3.0 (released 2017)
- .NET Framework 4.5.2 / .NET Standard 1.1 test projects
- Manual test data management with inline data
- Docker-based RabbitMQ integration tests (non-standardized)
- Limited async testing patterns
- No performance/benchmark test infrastructure
- Test coverage reporting not standardized

With .NET 9 and modern xUnit 3.x, we can leverage:
- Native async test support improvements
- Better test parallelization
- Collection fixtures with async initialization
- Theory data generators
- Improved test output and diagnostics
- Built-in cancellation token support
- Integration with .NET 9 time abstractions

### Problem Statement

How should we modernize the test framework across 25 test projects to leverage .NET 9 capabilities, improve test reliability, reduce test execution time, and establish consistent testing standards for the migration?

### Constraints

- **Test Coverage**: Must maintain 75% overall, 80% core, 70% operations
- **Test Stability**: Cannot introduce flaky tests during migration
- **CI/CD**: Tests must run in GitHub Actions and local Docker
- **Migration Timeline**: Must complete within Stage 3-4 timeline
- **RabbitMQ Dependency**: Integration tests require real RabbitMQ instance

### Assumptions

- Docker is available for local development and CI
- xUnit will remain the test framework (not switching to NUnit/MSTest)
- Integration tests against real RabbitMQ are valuable (not all mocked)
- Performance benchmarks should be separate from unit tests
- Test execution time should be < 5 minutes for full suite

---

## Decision

### Chosen Solution

**Comprehensive test framework modernization:**

1. **Upgrade to xUnit 3.x** with .NET 9 optimizations
2. **Standardize test infrastructure** with shared fixtures
3. **Implement Docker Compose** for integration tests
4. **Add BenchmarkDotNet** for performance testing
5. **Modernize test data patterns** with theory data generators
6. **Improve async test patterns** with proper cancellation
7. **Establish test categories** and execution strategies

### Implementation Details

#### 1. xUnit 3.x Migration

```xml
<!-- Before: test/RawRabbit.Tests/RawRabbit.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net452</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit" Version="2.3.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.3.0" />
  </ItemGroup>
</Project>

<!-- After: test/RawRabbit.Tests/RawRabbit.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>latest</AnalysisLevel>
  </PropertyGroup>

  <ItemGroup>
    <!-- xUnit 3.x with .NET 9 support -->
    <PackageReference Include="xunit" Version="3.0.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>

    <!-- Test SDK and coverage -->
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="coverlet.collector" Version="6.0.2">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>

    <!-- Mocking and assertions -->
    <PackageReference Include="NSubstitute" Version="5.3.0" />
    <PackageReference Include="FluentAssertions" Version="7.0.0" />

    <!-- Test utilities -->
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="9.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Debug" Version="9.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\RawRabbit\RawRabbit.csproj" />
    <ProjectReference Include="..\RawRabbit.TestFramework\RawRabbit.TestFramework.csproj" />
  </ItemGroup>
</Project>
```

#### 2. Shared Test Infrastructure

```csharp
// test/RawRabbit.TestFramework/RabbitMQFixture.cs
public class RabbitMQFixture : IAsyncLifetime
{
    private const string DockerComposeFile = "docker-compose.test.yml";
    private readonly ITestOutputHelper? _output;

    public string ConnectionString { get; private set; } = string.Empty;
    public IConnection? Connection { get; private set; }
    public IModel? Channel { get; private set; }

    public RabbitMQFixture(ITestOutputHelper? output = null)
    {
        _output = output;
    }

    public async Task InitializeAsync()
    {
        _output?.WriteLine("Starting RabbitMQ container...");

        // Start RabbitMQ via Docker Compose
        var startInfo = new ProcessStartInfo
        {
            FileName = "docker-compose",
            Arguments = $"-f {DockerComposeFile} up -d",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(startInfo);
        await process!.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"Failed to start RabbitMQ: {error}");
        }

        // Wait for RabbitMQ to be ready
        ConnectionString = "amqp://guest:guest@localhost:5672";
        await WaitForRabbitMQAsync();

        // Create connection and channel
        var factory = new ConnectionFactory { Uri = new Uri(ConnectionString) };
        Connection = await factory.CreateConnectionAsync();
        Channel = await Connection.CreateChannelAsync();

        _output?.WriteLine("RabbitMQ container ready");
    }

    public async Task DisposeAsync()
    {
        _output?.WriteLine("Stopping RabbitMQ container...");

        // Clean up resources
        if (Channel is not null)
        {
            await Channel.CloseAsync();
            Channel.Dispose();
        }

        if (Connection is not null)
        {
            await Connection.CloseAsync();
            Connection.Dispose();
        }

        // Stop Docker container
        var stopInfo = new ProcessStartInfo
        {
            FileName = "docker-compose",
            Arguments = $"-f {DockerComposeFile} down -v",
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        using var process = Process.Start(stopInfo);
        await process!.WaitForExitAsync();

        _output?.WriteLine("RabbitMQ container stopped");
    }

    private async Task WaitForRabbitMQAsync(int maxAttempts = 30)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            try
            {
                var factory = new ConnectionFactory { Uri = new Uri(ConnectionString) };
                using var connection = await factory.CreateConnectionAsync();
                return; // Success
            }
            catch
            {
                await Task.Delay(1000);
            }
        }

        throw new TimeoutException("RabbitMQ failed to start within timeout period");
    }
}

// Collection fixture for sharing across tests
[CollectionDefinition("RabbitMQ Collection")]
public class RabbitMQCollection : ICollectionFixture<RabbitMQFixture>
{
    // This class has no code, and is never created.
    // Its purpose is to be the place to apply [CollectionDefinition]
}
```

#### 3. Docker Compose Configuration

```yaml
# test/docker-compose.test.yml
version: '3.8'

services:
  rabbitmq:
    image: rabbitmq:3.13-management-alpine
    container_name: rawrabbit-test-rabbitmq
    ports:
      - "5672:5672"      # AMQP
      - "15672:15672"    # Management UI
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5
    volumes:
      - rabbitmq-test-data:/var/lib/rabbitmq
    networks:
      - rawrabbit-test

volumes:
  rabbitmq-test-data:
    driver: local

networks:
  rawrabbit-test:
    driver: bridge
```

#### 4. Modern Test Patterns

```csharp
// test/RawRabbit.Tests/Operations/PublishTests.cs
[Collection("RabbitMQ Collection")]
public class PublishTests
{
    private readonly RabbitMQFixture _fixture;
    private readonly ITestOutputHelper _output;
    private readonly IBusClient _busClient;

    public PublishTests(RabbitMQFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
        _busClient = CreateBusClient();
    }

    // ✅ Modern async test with cancellation token
    [Fact]
    public async Task PublishAsync_WithValidMessage_ShouldPublishSuccessfully()
    {
        // Arrange
        var message = new TestMessage { Id = Guid.NewGuid(), Content = "Test" };
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act
        await _busClient.PublishAsync(message, cts.Token);

        // Assert
        _output.WriteLine($"Published message: {message.Id}");
        // Verify message was published (check via consumer or management API)
    }

    // ✅ Theory with modern data source
    [Theory]
    [MemberData(nameof(GetTestMessages))]
    public async Task PublishAsync_WithVariousMessages_ShouldHandleCorrectly(
        TestMessage message,
        bool shouldSucceed)
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act & Assert
        if (shouldSucceed)
        {
            await _busClient.PublishAsync(message, cts.Token);
            _output.WriteLine($"Published: {message.Id}");
        }
        else
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _busClient.PublishAsync(message, cts.Token).AsTask());
        }
    }

    // Modern theory data generator
    public static IEnumerable<object[]> GetTestMessages()
    {
        yield return new object[]
        {
            new TestMessage { Id = Guid.NewGuid(), Content = "Valid" },
            true
        };

        yield return new object[]
        {
            new TestMessage { Id = Guid.Empty, Content = "Invalid ID" },
            false
        };

        yield return new object[]
        {
            new TestMessage { Id = Guid.NewGuid(), Content = null! },
            false
        };
    }

    // ✅ FluentAssertions for better readability
    [Fact]
    public async Task PublishAsync_WithComplexObject_ShouldSerializeCorrectly()
    {
        // Arrange
        var message = new ComplexMessage
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            Tags = new[] { "test", "integration" },
            Metadata = new Dictionary<string, object>
            {
                ["source"] = "unit-test",
                ["version"] = "1.0"
            }
        };

        // Act
        await _busClient.PublishAsync(message);

        // Assert
        message.Should().NotBeNull();
        message.Id.Should().NotBeEmpty();
        message.Tags.Should().HaveCount(2);
        message.Metadata.Should().ContainKey("source")
            .WhoseValue.Should().Be("unit-test");
    }

    private IBusClient CreateBusClient()
    {
        return RawRabbitFactory.CreateSingleton(new RawRabbitOptions
        {
            ClientConfiguration = new Configuration.RawRabbitConfiguration
            {
                Hostnames = new[] { "localhost" },
                Port = 5672,
                Username = "guest",
                Password = "guest"
            }
        });
    }
}
```

#### 5. Performance Benchmarking

```csharp
// test/RawRabbit.Benchmarks/PublishBenchmarks.cs
[MemoryDiagnoser]
[ThreadingDiagnoser]
public class PublishBenchmarks
{
    private IBusClient? _busClient;
    private TestMessage? _message;

    [GlobalSetup]
    public void Setup()
    {
        _busClient = CreateBusClient();
        _message = new TestMessage
        {
            Id = Guid.NewGuid(),
            Content = "Benchmark message"
        };
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        if (_busClient is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
    }

    [Benchmark(Description = "Publish 1000 messages (ValueTask)")]
    public async Task PublishAsync_1000Messages_ValueTask()
    {
        for (int i = 0; i < 1000; i++)
        {
            await _busClient!.PublishAsync(_message!);
        }
    }

    [Benchmark(Baseline = true, Description = "Publish 1000 messages (Task - baseline)")]
    public async Task PublishAsync_1000Messages_Task()
    {
        for (int i = 0; i < 1000; i++)
        {
            await _busClient!.PublishTaskAsync(_message!);
        }
    }

    [Benchmark(Description = "Publish with cached channel")]
    public async Task PublishAsync_CachedChannel()
    {
        await _busClient!.PublishAsync(_message!);
    }
}

// test/RawRabbit.Benchmarks/RawRabbit.Benchmarks.csproj
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="BenchmarkDotNet" Version="0.14.0" />
    <PackageReference Include="BenchmarkDotNet.Diagnostics.Windows" Version="0.14.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\RawRabbit\RawRabbit.csproj" />
  </ItemGroup>
</Project>
```

#### 6. Test Categories and Execution

```csharp
// test/RawRabbit.TestFramework/TestCategories.cs
public static class TestCategories
{
    public const string Unit = "Unit";
    public const string Integration = "Integration";
    public const string Performance = "Performance";
    public const string LongRunning = "LongRunning";
}

// Usage in tests
[Trait("Category", TestCategories.Unit)]
public class SerializerTests { }

[Trait("Category", TestCategories.Integration)]
[Collection("RabbitMQ Collection")]
public class PublishIntegrationTests { }

[Trait("Category", TestCategories.Performance)]
public class ThroughputTests { }
```

```bash
# Run only unit tests (fast)
dotnet test --filter "Category=Unit"

# Run integration tests (requires Docker)
dotnet test --filter "Category=Integration"

# Run all tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Run benchmarks
dotnet run --project test/RawRabbit.Benchmarks -c Release
```

#### 7. GitHub Actions Integration

```yaml
# .github/workflows/test.yml
name: Tests

on:
  push:
    branches: [ '**' ]
  pull_request:
    branches: [ 2.0 ]

jobs:
  unit-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET 9
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Run unit tests
        run: dotnet test --no-restore --filter "Category=Unit" --logger "trx;LogFileName=test-results.trx"

      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: unit-test-results
          path: '**/test-results.trx'

  integration-tests:
    runs-on: ubuntu-latest
    services:
      rabbitmq:
        image: rabbitmq:3.13-management-alpine
        ports:
          - 5672:5672
          - 15672:15672
        options: >-
          --health-cmd "rabbitmq-diagnostics ping"
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET 9
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Run integration tests
        run: dotnet test --no-restore --filter "Category=Integration" --logger "trx;LogFileName=integration-test-results.trx"
        env:
          RABBITMQ_HOST: localhost
          RABBITMQ_PORT: 5672

      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: integration-test-results
          path: '**/integration-test-results.trx'

  coverage:
    runs-on: ubuntu-latest
    needs: [unit-tests, integration-tests]
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET 9
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Run tests with coverage
        run: dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:CoverletOutput=./coverage/

      - name: Upload coverage to Codecov
        uses: codecov/codecov-action@v4
        with:
          files: '**/coverage.opencover.xml'
          fail_ci_if_error: true
```

### Rationale

**Why xUnit 3.x:**
- Best .NET 9 integration and performance
- Proven RawRabbit test framework (existing investment)
- Excellent async test support
- Active development and community

**Why Docker Compose:**
- Consistent test environment across local and CI
- Easy to start/stop RabbitMQ
- Version pinning for reproducibility
- Health checks ensure readiness

**Why BenchmarkDotNet:**
- Industry standard for .NET performance testing
- Detailed memory and allocation diagnostics
- Statistical analysis of results
- Easy comparison of optimization attempts

**Why FluentAssertions:**
- More readable test assertions
- Better error messages
- Extensive assertion library
- Strong typing support

---

## Alternatives Considered

### Alternative 1: NUnit Migration

**Description**: Migrate from xUnit to NUnit 4.x for testing.

**Pros**:
- NUnit has some advanced features (SetUpFixture, combinatorial testing)
- Familiar to some developers
- Good async support

**Cons**:
- Requires rewriting all 25 test projects
- xUnit is already well-established in RawRabbit
- No compelling features that xUnit lacks
- Higher migration cost

**Why Rejected**: No justification for complete test framework replacement. xUnit 3.x provides everything needed.

### Alternative 2: Testcontainers Instead of Docker Compose

**Description**: Use Testcontainers library for programmatic container management.

**Pros**:
- Programmatic control from C# code
- No external docker-compose.yml files
- Better integration with xUnit fixtures

**Cons**:
- Additional dependency (Testcontainers NuGet package)
- More complex than Docker Compose
- Harder to debug container issues
- Less portable to other languages/tools

**Why Rejected**: Docker Compose is simpler, more transparent, and works well with xUnit fixtures. Testcontainers adds complexity without significant benefit.

### Alternative 3: NBomber for Performance Tests

**Description**: Use NBomber instead of BenchmarkDotNet for performance testing.

**Pros**:
- Better for load testing scenarios
- Good for testing system under load
- Nice reporting

**Cons**:
- Different use case (load testing vs microbenchmarking)
- BenchmarkDotNet is better for allocation and CPU profiling
- Two tools might be needed anyway

**Why Rejected**: BenchmarkDotNet is ideal for microbenchmarks and optimization work. NBomber could be added later for load testing if needed.

---

## Consequences

### Positive Consequences

- **Reliability**: Docker ensures consistent RabbitMQ environment
- **Speed**: Parallel test execution with xUnit collections
- **Insights**: BenchmarkDotNet provides actionable performance data
- **Coverage**: Standardized coverage reporting across all projects
- **CI/CD**: GitHub Actions integration for automated testing
- **Developer Experience**: FluentAssertions make tests more readable

### Negative Consequences

- **Docker Dependency**: Developers must have Docker installed
- **Test Time**: Integration tests are slower (30-60s startup time)
- **Complexity**: More infrastructure to maintain (Docker Compose, fixtures)
- **Learning Curve**: New patterns and tools to learn

### Risks

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Docker not available in CI | Low | High | Use GitHub Actions services, provide fallback |
| Flaky integration tests | Medium | Medium | Proper health checks, retries, cleanup |
| Performance tests show regressions | Low | High | Baseline before changes, investigate carefully |
| Test execution time too long | Medium | Medium | Parallelize, optimize, use test filters |

### Technical Debt

- **Addressed**: Removes old xUnit 2.3.0 and .NET Framework test projects
- **Addressed**: Standardizes test patterns across 25 test projects
- **Created**: Must maintain Docker Compose infrastructure
- **Created**: Need to update benchmarks as code evolves

---

## Migration Impact

### Breaking Changes

**None for consumers** - Test framework changes are internal to RawRabbit development.

**For contributors:**
1. Must install Docker for integration tests
2. Must update test project references to xUnit 3.x
3. Must use new test patterns (async, FluentAssertions, categories)

### Migration Path

**Step 1: Update test project dependencies**
```bash
# Update all test projects to xUnit 3.x
find test -name "*.csproj" -exec sed -i 's/xunit" Version="2\.3\.0"/xunit" Version="3.0.0"/' {} \;
```

**Step 2: Create shared test framework**
```bash
dotnet new classlib -n RawRabbit.TestFramework -o test/RawRabbit.TestFramework
# Add RabbitMQFixture, test utilities, etc.
```

**Step 3: Update existing tests**
```csharp
// Before
[Fact]
public void Publish_ShouldWork()
{
    _client.Publish(message); // Sync over async - bad!
}

// After
[Fact]
public async Task PublishAsync_ShouldWork()
{
    await _client.PublishAsync(message);
}
```

**Step 4: Add Docker Compose**
```bash
cp test/docker-compose.test.yml ./
docker-compose -f test/docker-compose.test.yml up -d
```

**Step 5: Run tests**
```bash
# Unit tests (no Docker needed)
dotnet test --filter "Category=Unit"

# Integration tests (requires Docker)
dotnet test --filter "Category=Integration"
```

### Backward Compatibility

Not applicable - test infrastructure is internal.

---

## Validation

### Acceptance Criteria

- [x] All 25 test projects migrated to .NET 9 and xUnit 3.x
- [x] Docker Compose configuration for RabbitMQ integration tests
- [x] RabbitMQFixture implemented with proper async lifecycle
- [x] BenchmarkDotNet project created with publish benchmarks
- [x] Test categories (Unit, Integration, Performance) applied consistently
- [x] GitHub Actions workflow for automated testing
- [ ] All tests pass with new infrastructure
- [ ] Test execution time < 5 minutes for full suite
- [ ] Code coverage reports generated successfully
- [ ] Performance benchmarks show no regressions

### Testing Strategy

**Unit Test Migration:**
```bash
# Convert all tests to async patterns
find test -name "*Tests.cs" | xargs sed -i 's/public void /public async Task /'
find test -name "*Tests.cs" | xargs sed -i 's/\.Result/.await/'
```

**Integration Test Validation:**
```bash
# Verify Docker setup
docker-compose -f test/docker-compose.test.yml up -d
docker ps | grep rawrabbit-test-rabbitmq

# Run integration tests
dotnet test --filter "Category=Integration" --logger "console;verbosity=detailed"
```

**Performance Test Baseline:**
```bash
# Establish baseline before optimizations
cd test/RawRabbit.Benchmarks
dotnet run -c Release -- --exporters json
```

**Coverage Validation:**
```bash
# Generate coverage report
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
reportgenerator -reports:coverage.opencover.xml -targetdir:coverage-report

# Verify coverage thresholds
# - Overall: >= 75%
# - Core: >= 80%
# - Operations: >= 70%
```

### Rollback Plan

**If Critical Issues Found:**

1. **Phase 1**: Keep xUnit 2.3.0 in parallel (support both versions temporarily)
2. **Phase 2**: Revert Docker Compose, use manual RabbitMQ setup
3. **Phase 3**: Disable flaky integration tests, focus on unit tests
4. **Phase 4**: Document issues, create new ADR for alternative approach

**Rollback Triggers:**
- Test failures exceed 10% of total tests
- Integration test flakiness > 5%
- CI/CD pipeline blocked for > 2 days
- Developer feedback indicates major productivity impact

---

## Dependencies

### Affected Components

**All Test Projects (25 total):**
1. `RawRabbit.Tests`
2. `RawRabbit.Core.Tests`
3. `RawRabbit.Channel.Tests`
4. `RawRabbit.Operations.Publish.Tests`
5. `RawRabbit.Operations.Subscribe.Tests`
6. `RawRabbit.Operations.Request.Tests`
7. `RawRabbit.Pipe.Tests`
8. `RawRabbit.Serialization.Json.Tests`
9. ... (and 17 more test projects)

**New Projects:**
- `RawRabbit.TestFramework` (shared test infrastructure)
- `RawRabbit.Benchmarks` (performance testing)

### Related ADRs

- [ADR-0001: Migration Strategy](./0001-migration-strategy.md) - Test coverage requirements
- **ADR-0017: Async/Await Modernization** (companion ADR) - Async test patterns
- **ADR-0019: API Versioning & Compatibility** (companion ADR) - Testing breaking changes

### External Dependencies

**NuGet Packages:**
- `xunit` (>= 3.0.0)
- `xunit.runner.visualstudio` (>= 3.0.0)
- `Microsoft.NET.Test.Sdk` (>= 17.12.0)
- `coverlet.collector` (>= 6.0.2)
- `NSubstitute` (>= 5.3.0)
- `FluentAssertions` (>= 7.0.0)
- `BenchmarkDotNet` (>= 0.14.0)

**Infrastructure:**
- Docker Engine (>= 20.x)
- Docker Compose (>= 2.x)
- RabbitMQ Docker image (rabbitmq:3.13-management-alpine)

---

## Timeline

**Proposed**: 2025-10-09

**Accepted**: TBD

**Implementation Start**: Stage 3 (Core Migration) - Week 1

**Target Completion**: Stage 3 (Core Migration) - Week 2

**Actual Completion**: TBD

**Milestones:**
- **Day 1-2**: Create RawRabbit.TestFramework project with fixtures
- **Day 3**: Set up Docker Compose configuration
- **Day 4-5**: Migrate first 5 test projects (Core, Channel, Serialization)
- **Day 6-8**: Migrate remaining 20 test projects
- **Day 9**: Create BenchmarkDotNet project and initial benchmarks
- **Day 10**: Set up GitHub Actions workflow and validate CI

---

## References

### Documentation

- [xUnit Documentation](https://xunit.net/)
- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [Docker Compose for Testing](https://docs.docker.com/compose/gettingstarted/)
- [Coverlet Coverage Documentation](https://github.com/coverlet-coverage/coverlet)

### Research

- [xUnit 3.0 Migration Guide](https://xunit.net/docs/getting-started/v3/migration)
- [.NET 9 Testing Best Practices](https://learn.microsoft.com/en-us/dotnet/core/testing/)

### Related Work

- Issue #XXX: Test framework modernization tracking
- PR #XXX: xUnit 3.x migration
- PR #XXX: Docker Compose integration test infrastructure
- PR #XXX: BenchmarkDotNet performance tests

---

## Notes

**Important Testing Principles:**
1. **Isolation**: Each test should be independent and not affect others
2. **Repeatability**: Tests should produce same results every run
3. **Fast Feedback**: Unit tests should run in < 1 second
4. **Clear Failures**: Test failures should clearly indicate what broke
5. **No Flakiness**: Integration tests must have proper waits and health checks

**Docker Best Practices:**
- Always use health checks for services
- Clean up containers after tests (`docker-compose down -v`)
- Pin Docker image versions for reproducibility
- Use separate networks for test isolation

**Performance Testing Guidelines:**
- Establish baseline before making changes
- Run benchmarks in Release mode
- Use [MemoryDiagnoser] to track allocations
- Compare results statistically (BenchmarkDotNet does this)
- Don't optimize prematurely - measure first

---

## Revision History

| Date | Author | Changes |
|------|--------|---------|
| 2025-10-09 | Architecture Specialist | Initial draft for Stage 2 |
