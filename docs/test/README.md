# Test Reports and Documentation

This directory contains test reports, validation results, and test documentation for the RawRabbit .NET 9 upgrade project.

## Directory Structure

```
docs/test/
├── unit/           # Unit test reports and coverage
├── integration/    # Integration test reports
├── performance/    # Performance benchmarks and comparisons
├── security/       # Security test reports
└── README.md       # This file
```

## Test Report Organization

### Unit Tests (`unit/`)

Contains reports from unit test execution:

**Naming Convention**: `unit-test-YYYY-MM-DD-[description].md`

**Report Format**:
```markdown
# Unit Test Report: [Description]

**Date**: YYYY-MM-DD
**Target Framework**: net6.0 / net8.0 / net9.0
**Test Framework**: xUnit 2.x

## Summary
- Total Tests: XXX
- Passed: XXX
- Failed: XXX
- Skipped: XXX
- Duration: XX.XXs

## Coverage
- Overall: XX%
- RawRabbit: XX%
- RawRabbit.Operations: XX%
- [Other projects...]

## Failed Tests
[Details of any failed tests]

## Notes
[Any relevant observations]
```

**Coverage Requirements**:
- **Overall**: 75%+ (target)
- **Core Library (RawRabbit)**: 80%+
- **Extensions**: 70%+
- **Integration packages**: 60%+

### Integration Tests (`integration/`)

Contains reports from integration test execution:

**Naming Convention**: `integration-test-YYYY-MM-DD-[component].md`

**Report Format**:
```markdown
# Integration Test Report: [Component]

**Date**: YYYY-MM-DD
**Target Framework**: net6.0 / net8.0 / net9.0
**RabbitMQ Version**: X.X.X

## Test Environment
- OS: [Operating System]
- .NET SDK: [Version]
- RabbitMQ: [Version and setup]

## Test Scenarios
1. [Scenario 1]
   - Status: PASS/FAIL
   - Duration: XXs
   - Notes: [Any observations]

2. [Scenario 2]
   - Status: PASS/FAIL
   - Duration: XXs
   - Notes: [Any observations]

## Issues Found
[Details of any issues]

## Notes
[Any relevant observations]
```

**Critical Scenarios**:
- Basic publish/subscribe
- Request/response pattern
- Connection recovery
- Message acknowledgment
- Queue/exchange topology
- SSL/TLS connections
- Multi-threading scenarios

### Performance Tests (`performance/`)

Contains performance benchmarks and comparisons:

**Naming Convention**: `performance-YYYY-MM-DD-[framework]-[description].md`

**Report Format**:
```markdown
# Performance Benchmark Report: [Description]

**Date**: YYYY-MM-DD
**Framework**: netcoreapp3.1 / net6.0 / net8.0 / net9.0
**BenchmarkDotNet Version**: X.X.X

## System Configuration
- OS: [Operating System]
- CPU: [Processor]
- Memory: [RAM]
- .NET Runtime: [Version]

## Benchmark Results

### [Operation Name]
| Metric | netcoreapp3.1 | net6.0 | net9.0 | Change |
|--------|---------------|--------|--------|--------|
| Mean   | XX.XX μs      | XX.XX μs | XX.XX μs | +/- X% |
| P95    | XX.XX μs      | XX.XX μs | XX.XX μs | +/- X% |
| Allocated | XXX B    | XXX B  | XXX B  | +/- X% |

## Regression Analysis
[Analysis of any regressions > 20%]

## Optimization Opportunities
[Notes on potential improvements]
```

**Regression Thresholds**:
- **BLOCKER**: Mean execution time > +20%, P95 latency > +25%, Throughput < -15%
- **WARNING**: Memory allocations > +30%, Gen2 collections > +50%
- **ACCEPTABLE**: System.Text.Json within 10% of Newtonsoft.Json

### Security Tests (`security/`)

Contains security scanning and audit reports:

**Naming Convention**: `security-scan-YYYY-MM-DD-[tool].md`

**Report Format**:
```markdown
# Security Scan Report: [Tool]

**Date**: YYYY-MM-DD
**Tool**: [Tool name and version]
**Scan Type**: Vulnerability / Static Analysis / Secret Detection

## Summary
- Vulnerabilities Found: XXX
- Critical: XXX
- High: XXX
- Medium: XXX
- Low: XXX

## Critical Issues
### CVE-XXXX-XXXXX: [Vulnerability Name]
- **Package**: [Package name and version]
- **Severity**: CRITICAL/HIGH
- **Description**: [Description]
- **Remediation**: [Fix recommendation]

## Recommendations
[Overall recommendations]

## Scan Output
[Detailed scan results]
```

**Required Scans**:
- Dependency vulnerability scan (dotnet list package --vulnerable)
- OWASP Dependency-Check
- GitHub Dependabot
- GitHub CodeQL
- Secret scanning
- SBOM generation

## Test Execution Guidelines

### Running Unit Tests

```bash
# Run all unit tests
dotnet test --configuration Release

# Run with coverage
dotnet test --configuration Release --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test test/RawRabbit.Tests/RawRabbit.Tests.csproj
```

### Running Integration Tests

```bash
# Start RabbitMQ (Docker)
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3.13-management

# Run integration tests
dotnet test test/RawRabbit.IntegrationTests/RawRabbit.IntegrationTests.csproj

# Cleanup
docker stop rabbitmq && docker rm rabbitmq
```

### Running Performance Benchmarks

```bash
# Run all benchmarks
cd benchmark/RawRabbit.Benchmarks
dotnet run -c Release --framework net9.0

# Run specific benchmark
dotnet run -c Release --framework net9.0 --filter "*PublishBenchmarks*"

# Compare frameworks
dotnet run -c Release --runtimes netcoreapp3.1 net6.0 net9.0
```

### Running Security Scans

```bash
# Vulnerability scan
dotnet list package --vulnerable --include-transitive

# OWASP Dependency-Check
docker run --rm -v "$(pwd)":/src owasp/dependency-check --scan /src --format ALL

# GitHub Advanced Security (via GitHub Actions)
# See .github/workflows/security.yml
```

## Test Documentation Standards

### 1. Always Document
- Test failures (with reproduction steps)
- Performance regressions (with before/after metrics)
- Security findings (with severity and remediation)
- Test environment changes

### 2. Include Metrics
- Execution time
- Pass/fail counts
- Coverage percentages
- Performance numbers (mean, P95, allocations)

### 3. Provide Context
- What was tested?
- Why was it tested?
- What were the results?
- What do the results mean?
- What action is required?

### 4. Link to Issues
- Create GitHub issues for test failures
- Link to issues in test reports
- Track resolution status

### 5. Track Over Time
- Compare to previous reports
- Identify trends
- Document improvements

## Test Validation Checklist

### Before Stage Completion

- [ ] All unit tests pass on target framework
- [ ] Code coverage meets requirements (75%+ overall)
- [ ] All integration tests pass with RabbitMQ X.X
- [ ] Performance benchmarks show no regressions > 20%
- [ ] Security scans show no new CRITICAL/HIGH vulnerabilities
- [ ] Test reports generated and committed
- [ ] Any failures documented with GitHub issues

### Before Release

- [ ] All tests pass on .NET 6, 8, 9
- [ ] Coverage requirements met
- [ ] Performance validated against baseline
- [ ] Security audit complete
- [ ] Test documentation up to date
- [ ] CI/CD pipeline green

## Related Documentation

- [PLAN.md](../planning/PLAN.md) - Test coverage requirements per stage
- [HISTORY.md](../HISTORY.md) - Test execution history
- [CONTRIBUTING.md](../CONTRIBUTING.md) - How to document test results

## References

- [xUnit Documentation](https://xunit.net/)
- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [Coverlet Documentation](https://github.com/coverlet-coverage/coverlet)
- [GitHub Advanced Security](https://docs.github.com/en/code-security)
