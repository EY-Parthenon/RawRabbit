# ADR-0005: Test Coverage Strategy

**Status**: Proposed

**Date**: 2025-10-09

**Authors**: Architecture Specialist

**Reviewers**: Migration Architect, QA Lead

**Tags**: migration, testing, quality, coverage, automation

---

## Context

### Background

RawRabbit has existing test infrastructure across 4 test projects:
- **RawRabbit.Tests** (net46): Unit tests for core library
- **RawRabbit.IntegrationTests** (net46): Integration tests with RabbitMQ broker
- **RawRabbit.PerformanceTest** (netcoreapp1.1): Performance benchmarks
- **RawRabbit.Enrichers.Polly.Tests** (net46, old-style project): Polly enricher tests

From Stage 1 assessment (migration-roadmap.md), we currently have:
- Unknown baseline coverage (no metrics tracked)
- Test frameworks outdated (xUnit 2.3.0, Moq 4.7.137)
- Old project formats (net46, netcoreapp1.1)
- No automated coverage reporting

The .NET 9 migration introduces significant changes:
- RabbitMQ.Client 5.0.1 → 7.1.2 (sync → async patterns)
- Newtonsoft.Json → System.Text.Json (serialization behavior)
- Framework updates (net46 → net9.0)
- Dependency major version upgrades

### Problem Statement

We need to establish a comprehensive test coverage strategy that:
1. Defines realistic coverage targets per component type
2. Ensures regression prevention during migration
3. Validates behavior equivalence (v2.0.x vs v2.1.x)
4. Provides measurable quality gates
5. Automates coverage tracking in CI/CD

**Key Questions**:
- What coverage targets are appropriate for different component types?
- How do we prevent regressions during incremental migration?
- What testing methodology should guide implementation?
- How do we balance thorough testing with timeline constraints (6-8 weeks)?

### Constraints

**Timeline Constraints**:
- 6-8 week migration window
- Writing comprehensive tests competes with migration work
- Must maintain productivity without sacrificing quality

**Technical Constraints**:
- Test frameworks must support net8.0 and net9.0
- Integration tests require RabbitMQ broker (Docker)
- Performance tests need baseline comparisons
- Cannot test all combinations (32 projects × 2 targets × multiple configs)

**Resource Constraints**:
- 1-2 developers full-time
- CI/CD pipeline capacity (build minutes)
- RabbitMQ broker resources for parallel tests

### Assumptions

1. Existing tests provide adequate functional coverage
2. Migration will not intentionally change behavior
3. Code coverage is a useful but imperfect quality metric
4. Automated tests are more valuable than manual testing
5. Integration tests provide highest confidence for messaging library

---

## Decision

### Chosen Solution

**Tiered Test Coverage Strategy with Realistic Targets**

We will implement a **risk-based, tiered coverage approach** with different targets for component types:

#### Coverage Targets by Component Type

**Tier 1: Core Library (RawRabbit) - 85% Coverage**
- **Rationale**: Foundation for all other projects, highest risk
- **Focus**: Connection management, channel pooling, message routing
- **Critical Paths**: Publish, subscribe, request/response flows

**Tier 2: Operations & Critical Enrichers - 80% Coverage**
- **Projects**: Operations.*, Enrichers.Polly, Enrichers.MessageContext
- **Rationale**: High usage, complex logic, directly exposed to users
- **Focus**: Message handling, retry logic, context propagation

**Tier 3: Simple Enrichers & DI Adapters - 70% Coverage**
- **Projects**: Enrichers.Attributes, Enrichers.GlobalExecutionId, DI adapters
- **Rationale**: Simpler logic, lower risk, less user-facing
- **Focus**: Configuration, registration, basic middleware

**Tier 4: Serialization Enrichers - 75% Coverage**
- **Projects**: Enrichers.MessagePack, Enrichers.Protobuf, Enrichers.ZeroFormatter
- **Rationale**: Serialization bugs are critical but logic is contained
- **Focus**: Serialize/deserialize round-trip, edge cases (null, large objects)

**Tier 5: Samples & Compatibility - 60% Coverage**
- **Projects**: Sample applications, Compatibility.Legacy
- **Rationale**: Demonstrative code, lower risk, manual testing acceptable
- **Focus**: Basic functionality, integration points

**Overall Target: 75% Code Coverage** (weighted average)

#### Test Methodology: London School TDD + Integration Focus

We will use a **hybrid approach** combining:

**1. London School TDD (Mockist) for Unit Tests**:
```csharp
// Example: Test RabbitMQ channel operations with mocks
[Fact]
public async Task PublishAsync_ShouldCallBasicPublishAsync_WithCorrectParameters()
{
    // Arrange
    var channelMock = new Mock<IChannel>();
    var publisher = new MessagePublisher(channelMock.Object);
    var message = new TestMessage { Content = "Hello" };

    // Act
    await publisher.PublishAsync(message);

    // Assert
    channelMock.Verify(c => c.BasicPublishAsync(
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<bool>(),
        It.IsAny<BasicProperties>(),
        It.IsAny<ReadOnlyMemory<byte>>(),
        It.IsAny<CancellationToken>()),
        Times.Once);
}
```

**2. Integration Tests with Real RabbitMQ (Testcontainers)**:
```csharp
// Example: End-to-end message flow with real broker
[Fact]
public async Task PublishAndConsume_ShouldDeliverMessage_EndToEnd()
{
    // Arrange: Start RabbitMQ container
    await using var container = new RabbitMqBuilder()
        .WithImage("rabbitmq:3-management")
        .Build();
    await container.StartAsync();

    var client = RawRabbitFactory.CreateTestClient(container.GetConnectionString());
    var receivedMessage = null as TestMessage;
    var tcs = new TaskCompletionSource<TestMessage>();

    // Act: Subscribe and publish
    await client.SubscribeAsync<TestMessage>(msg =>
    {
        tcs.SetResult(msg);
        return Task.CompletedTask;
    });

    await client.PublishAsync(new TestMessage { Content = "Integration test" });

    // Assert: Message received
    receivedMessage = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
    Assert.NotNull(receivedMessage);
    Assert.Equal("Integration test", receivedMessage.Content);
}
```

**3. Property-Based Testing for Serialization**:
```csharp
// Example: Test serialization with random inputs
[Property]
public Property SerializeDeserialize_ShouldBeIdempotent(TestMessage message)
{
    var serializer = new SystemTextJsonSerializer();

    var bytes = serializer.Serialize(message);
    var deserialized = serializer.Deserialize<TestMessage>(bytes);

    return (deserialized.Equals(message)).ToProperty();
}
```

#### Regression Testing Strategy

**Behavior Snapshot Tests**:
```csharp
// Create behavior baseline from v2.0.x, validate in v2.1.x
[Fact]
public async Task PublishAsync_Behavior_MatchesV20Baseline()
{
    var baseline = BehaviorBaseline.Load("publish-async-baseline.json");
    var actual = await CapturePublishBehavior();

    baseline.AssertEquivalent(actual);
}
```

**Cross-Version Compatibility Tests**:
- v2.0.x publisher → v2.1.x consumer
- v2.1.x publisher → v2.0.x consumer
- Mixed environment tests

#### Performance Benchmarking

**Baseline Benchmarks (v2.0.x)**:
```csharp
[Benchmark(Baseline = true)]
public async Task PublishAsync_V20_Baseline()
{
    await _v20Client.PublishAsync(new TestMessage());
}

[Benchmark]
public async Task PublishAsync_V21_New()
{
    await _v21Client.PublishAsync(new TestMessage());
}
```

**Performance Regression Gates**:
- Throughput: ≥ 10,000 msg/sec (minimum, baseline from v2.0.x)
- Latency: p99 ≤ 10ms
- Memory: ≤ 100 MB for 1M messages
- No performance regression > 5% vs. v2.0.x

### Implementation Details

**Test Project Structure**:
```
tests/
├── RawRabbit.Tests/                   # Unit tests (Tier 1)
│   ├── Core/
│   ├── Operations/
│   └── Enrichers/
├── RawRabbit.IntegrationTests/        # Integration tests (all tiers)
│   ├── EndToEnd/
│   ├── RabbitMQ/
│   └── Compatibility/
├── RawRabbit.PerformanceTests/        # Benchmarks
│   ├── Baselines/
│   └── Throughput/
└── RawRabbit.PropertyTests/           # Property-based tests (new)
    └── Serialization/
```

**Coverage Tool Configuration**:
```xml
<!-- Directory.Build.props -->
<PropertyGroup>
  <GenerateCoverageReport>true</GenerateCoverageReport>
  <CoverletOutputFormat>opencover,cobertura,json</CoverletOutputFormat>
  <Exclude>[*.Tests]*,[*]*.Samples.*</Exclude>
  <ExcludeByFile>**/*Designer.cs</ExcludeByFile>
  <ThresholdType>line,branch,method</ThresholdType>
  <Threshold>75</Threshold>
</PropertyGroup>
```

**CI/CD Integration**:
```yaml
# .github/workflows/test.yml
name: Test & Coverage
on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    services:
      rabbitmq:
        image: rabbitmq:3-management
        ports:
          - 5672:5672
        options: --health-cmd "rabbitmq-diagnostics -q ping" --health-interval 10s

    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET 9
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Run Unit Tests with Coverage
        run: |
          dotnet test --collect:"XPlat Code Coverage" \
            --results-directory ./coverage \
            --configuration Release

      - name: Generate Coverage Report
        run: |
          dotnet tool install -g dotnet-reportgenerator-globaltool
          reportgenerator -reports:./coverage/**/*.xml \
            -targetdir:./coverage/report \
            -reporttypes:Html;Badges

      - name: Upload Coverage to Codecov
        uses: codecov/codecov-action@v4
        with:
          directory: ./coverage

      - name: Check Coverage Threshold
        run: |
          # Fail if coverage < 75%
          dotnet test --collect:"XPlat Code Coverage" \
            /p:Threshold=75 /p:ThresholdType=line
```

### Rationale

**Why Tiered Coverage Targets?**
1. **Realistic**: 100% coverage unrealistic for 32 projects in 6-8 weeks
2. **Risk-Based**: Higher coverage for higher-risk components
3. **Pragmatic**: Balances quality with timeline constraints
4. **Measurable**: Clear targets enable progress tracking

**Why 75% Overall Target?**
- Industry standard for mature libraries (70-85%)
- Achievable within 6-8 week timeline
- Provides strong confidence without perfection paralysis
- Room for uncoverable code (error handlers, edge cases)

**Why London School TDD (Mockist)?**
- Fast feedback loop (mocks don't require RabbitMQ broker)
- Isolates units under test
- Documents dependencies clearly
- Enables TDD workflow during migration

**Why Integration Tests Critical?**
- Messaging libraries MUST test with real broker
- Mocks cannot catch async/threading bugs
- Cross-version compatibility requires real message exchange
- User confidence requires end-to-end validation

**Why Property-Based Testing for Serialization?**
- Serialization bugs often appear with edge cases (null, large values, unicode)
- Property-based testing generates thousands of test cases automatically
- Complements example-based tests

---

## Alternatives Considered

### Alternative 1: 100% Code Coverage Target

**Description**: Mandate 100% line and branch coverage for all projects

**Pros**:
- Highest confidence
- Forces testing of all code paths
- No untested code

**Cons**:
- **Unrealistic Timeline**: Would require 12-16 weeks, not 6-8
- **Diminishing Returns**: 95-100% coverage has low ROI
- **False Confidence**: Coverage ≠ quality
- **Developer Burden**: Testing trivial code (getters/setters) wastes time

**Why Rejected**: Timeline constraint makes this impossible. Better to focus on 75% coverage with high-quality tests than 100% coverage with low-quality tests.

### Alternative 2: Minimal Testing (50% Coverage)

**Description**: Write tests only for critical paths, accept 50% coverage

**Pros**:
- Fastest migration (more time for feature work)
- Low testing overhead
- Focuses on "important" code

**Cons**:
- **High Risk**: Regressions likely in untested code
- **User Trust**: Low coverage signals poor quality
- **Technical Debt**: Will need to backfill tests later
- **Deployment Risk**: Production bugs increase

**Why Rejected**: Too risky for a library with unknown downstream consumers. 50% coverage inadequate for critical infrastructure component.

### Alternative 3: Classical TDD (Classicist) Only

**Description**: Use classical TDD (Detroit School) with no mocks, real dependencies

**Pros**:
- Tests closer to real usage
- Detects integration issues earlier
- Less brittle tests (no mock coupling)

**Cons**:
- **Slower Feedback**: Every test requires RabbitMQ broker
- **CI/CD Overhead**: 10x longer build times
- **Harder to Isolate**: Failures harder to debug
- **Resource Intensive**: Requires RabbitMQ containers for all tests

**Why Rejected**: While integration tests are critical, requiring RabbitMQ for ALL tests is impractical. Hybrid approach (mocks for units, real broker for integration) is optimal balance.

### Alternative 4: Manual Testing Only

**Description**: Skip automated testing, rely on manual QA

**Pros**:
- Zero test development time
- Flexibility to test "real scenarios"
- No test maintenance burden

**Cons**:
- **No Regression Detection**: Manual tests not repeatable
- **Slow Feedback**: Hours/days vs. seconds
- **Incomplete Coverage**: Humans miss edge cases
- **Non-Scalable**: Cannot test 32 projects manually

**Why Rejected**: Unacceptable for modern software development. Automated testing is table stakes.

---

## Consequences

### Positive Consequences

**Quality Assurance**:
- 75% code coverage provides strong confidence in migration
- Regression tests prevent behavior changes
- Integration tests validate real-world usage
- Performance benchmarks detect regressions

**Developer Confidence**:
- Tests enable fearless refactoring
- Fast feedback loop (unit tests < 10s)
- Clear acceptance criteria per ADR/feature
- CI/CD gate prevents broken merges

**User Confidence**:
- Visible coverage badges (README.md)
- Comprehensive test suite signals quality
- Compatibility tests demonstrate v2.0.x → v2.1.x path
- Performance benchmarks prove no regressions

**Long-Term Maintainability**:
- Test suite documents expected behavior
- Easier to onboard contributors
- Safe to refactor with test safety net
- Reduces bug fix time (reproduction tests)

### Negative Consequences

**Time Investment**:
- 30-40% of development time spent on testing
- Test maintenance overhead ongoing
- CI/CD pipeline runtime increases (5-10 min builds)

**Partial Coverage**:
- 75% coverage means 25% untested
- Risk of bugs in untested paths
- Pressure to skip tests to hit timeline

**Complexity**:
- Test infrastructure (Testcontainers, mocks) adds complexity
- Property-based tests require learning curve
- Maintaining baselines for regression tests

### Risks

**Risk 1: Timeline Pressure → Skipped Tests**
- **Likelihood**: MEDIUM
- **Impact**: HIGH (regressions, deployment issues)
- **Mitigation**:
  - Make coverage a CI/CD gate (cannot merge < 75%)
  - Prioritize tests for Tier 1-2 components
  - Allow Tier 5 (samples) to have lower coverage

**Risk 2: False Confidence from High Coverage**
- **Likelihood**: MEDIUM
- **Impact**: MEDIUM (bugs slip through despite tests)
- **Mitigation**:
  - Focus on meaningful tests, not coverage numbers
  - Code review tests for quality
  - Integration tests mandatory for critical paths

**Risk 3: Flaky Integration Tests**
- **Likelihood**: MEDIUM
- **Impact**: MEDIUM (CI/CD failures, developer frustration)
- **Mitigation**:
  - Use Testcontainers for isolated RabbitMQ instances
  - Add retry logic for timing-sensitive tests
  - Monitor flaky test rate (< 1% acceptable)

### Technical Debt

**Created**:
- Test infrastructure maintenance (Testcontainers, Docker)
- Baseline snapshots require periodic updates
- Mock maintenance when APIs change

**Addressed**:
- No existing coverage tracking (new infrastructure)
- Old test frameworks updated (xUnit 2.9.0, Moq 4.20.70)
- Integration test gaps filled (RabbitMQ 7.x patterns)

---

## Migration Impact

### Breaking Changes

**Test Framework Updates**:
- xUnit 2.3.0 → 2.9.0 (compatible upgrade)
- Moq 4.7.137 → 4.20.70 (compatible upgrade)
- BenchmarkDotNet 0.10.3 → 0.14.0 (compatible upgrade)

**Test Project Format**:
- net46 → net9.0 (TargetFramework change)
- Old-style csproj → SDK-style (RawRabbit.Enrichers.Polly.Tests)

### Migration Path

**For Contributors**:
1. Install .NET 9 SDK
2. Install Docker (for integration tests)
3. Run `dotnet test` (all tests should pass)
4. Coverage report generated automatically

**For CI/CD**:
1. Add RabbitMQ service container
2. Enable coverage collection
3. Upload to Codecov
4. Enforce 75% threshold

### Backward Compatibility

**Test Infrastructure**:
- v2.0.x tests remain functional (archived)
- v2.1.x tests use modern patterns
- Cross-version compatibility tests bridge gap

---

## Validation

### Acceptance Criteria

**Coverage Metrics**:
- [ ] Overall coverage ≥ 75% (line coverage)
- [ ] Tier 1 (Core) ≥ 85%
- [ ] Tier 2 (Operations) ≥ 80%
- [ ] Tier 3 (Enrichers) ≥ 70%
- [ ] Tier 4 (Serialization) ≥ 75%
- [ ] Tier 5 (Samples) ≥ 60%

**Test Quality**:
- [ ] All existing tests migrated and passing
- [ ] Integration tests for RabbitMQ.Client 7.x async patterns
- [ ] Regression tests for v2.0.x → v2.1.x compatibility
- [ ] Performance baselines documented
- [ ] Property-based tests for serialization

**CI/CD Integration**:
- [ ] Coverage tracked in Codecov
- [ ] Coverage badge in README.md
- [ ] CI/CD gate enforces 75% threshold
- [ ] Flaky test rate < 1%

### Testing Strategy

**Unit Tests**:
- London School TDD (mockist)
- Fast feedback (< 10s for full suite)
- 70-85% of test suite

**Integration Tests**:
- Testcontainers for RabbitMQ
- End-to-end message flows
- 20-25% of test suite

**Performance Tests**:
- BenchmarkDotNet
- Baseline comparisons
- 5% of test suite

**Property-Based Tests**:
- FsCheck or Bogus
- Serialization round-trips
- 5% of test suite

### Rollback Plan

**If Coverage Targets Unachievable**:
1. Lower Tier 3-5 targets (60%, 65%, 50%)
2. Maintain Tier 1-2 targets (85%, 80%)
3. Document technical debt for future improvement

**If Timeline Slips**:
1. Prioritize Tier 1-2 tests (core functionality)
2. Accept lower coverage for Tier 3-5
3. Plan backfill tests in v2.1.1 patch

---

## Dependencies

### Affected Components

**Test Projects** (4 projects):
- RawRabbit.Tests
- RawRabbit.IntegrationTests
- RawRabbit.PerformanceTests
- RawRabbit.Enrichers.Polly.Tests

**All Source Projects** (25 projects):
- All must achieve coverage targets

### Related ADRs

- **ADR-0001**: Migration Strategy (incremental approach enables incremental testing)
- **ADR-0003**: Target Framework Selection (net9.0 for tests)
- **ADR-0004**: Dependency Update Strategy (test frameworks updated)

### External Dependencies

- **xUnit** 2.9.0 (test framework)
- **Moq** 4.20.70 (mocking library)
- **BenchmarkDotNet** 0.14.0 (performance benchmarking)
- **Testcontainers** (RabbitMQ integration tests)
- **Coverlet** (coverage collection)
- **ReportGenerator** (coverage reports)

---

## Timeline

**Proposed**: 2025-10-09

**Implementation**:
- **Phase 1-2** (Weeks 1-3): Core and operations unit tests
- **Phase 3** (Weeks 4-5): Enricher tests
- **Phase 4** (Week 5): Integration tests
- **Phase 5** (Week 6): Full test suite validation
- **Phase 6** (Week 7): Performance benchmarks

**Target Completion**: 2025-11-22

---

## References

### Documentation

- [London School TDD](http://www.growing-object-oriented-software.com/)
- [Testcontainers](https://dotnet.testcontainers.org/)
- [FsCheck Property Testing](https://fscheck.github.io/FsCheck/)
- [Coverlet Documentation](https://github.com/coverlet-coverage/coverlet)

### Research

- [Migration Roadmap](../stage-1/migration-roadmap.md)
- [Google Testing Blog: Test Coverage](https://testing.googleblog.com/2020/08/code-coverage-best-practices.html)

### Related Work

- [ADR-0001: Migration Strategy](./0001-migration-strategy.md)
- [ADR-0004: Dependency Update Strategy](./0004-dependency-update-strategy.md)

---

## Notes

**Test Coverage Philosophy**:
- Coverage is a metric, not a goal
- 75% coverage with high-quality tests > 100% coverage with low-quality tests
- Integration tests provide highest confidence for messaging libraries
- Property-based testing catches edge cases missed by example-based tests

---

## Revision History

| Date | Author | Changes |
|------|--------|---------|
| 2025-10-09 | Architecture Specialist | Initial draft (Stage 2.1) |
