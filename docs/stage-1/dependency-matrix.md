# RawRabbit .NET 9 Dependency Matrix

**Stage**: 1.2 - Discovery & Analysis
**Date**: 2025-10-09
**Status**: Complete

## Executive Summary

Complete inventory of NuGet packages across 32 projects with upgrade paths to .NET 9 compatible versions. This matrix identifies version constraints, compatibility issues, and upgrade strategies.

## Critical Dependencies

### RabbitMQ.Client (Core Messaging)

| Current Version | Target Version | Compatibility | Risk Level | Notes |
|----------------|----------------|---------------|-----------|--------|
| 5.0.1 | 6.8.1+ | BREAKING | HIGH | Major API changes in v6+ |

**Projects Using**:
- RawRabbit (core)
- RawRabbit.Enrichers.Polly.Tests

**Known Issues**:
- Connection factory API changed
- IModel renamed to IChannel in v7+
- Async/await patterns introduced
- Event model changed

**Migration Strategy**:
- Target 6.8.1 first (more stable)
- Consider 7.x for future-proofing
- Extensive testing required
- Update all connection/channel code

**References**:
- [RabbitMQ .NET Client 6.0 Release Notes](https://www.rabbitmq.com/dotnet-api-guide.html)
- [Migration Guide v5 to v6](https://github.com/rabbitmq/rabbitmq-dotnet-client/releases/tag/v6.0.0)

### Newtonsoft.Json (Serialization)

| Current Version | Target Version | Compatibility | Risk Level | Notes |
|----------------|----------------|---------------|-----------|--------|
| 10.0.1 | 13.0.3 | COMPATIBLE | LOW | Stable API |

**Projects Using**:
- RawRabbit (core)

**Known Issues**:
- None significant - API very stable
- Consider System.Text.Json for new code

**Migration Strategy**:
- Straightforward version update
- Validate serialization tests
- Consider System.Text.Json evaluation for future

## Dependency Injection Containers

### Autofac

| Current Version | Target Version | Compatibility | Risk Level | Notes |
|----------------|----------------|---------------|-----------|--------|
| 4.1.0 | 8.0.0+ | BREAKING | MEDIUM | Registration API changes |

**Projects Using**:
- RawRabbit.DependencyInjection.Autofac

**Known Issues**:
- ContainerBuilder API changed
- Module registration patterns updated
- Lifetime scope semantics refined

**Migration Strategy**:
- Update to 8.0.0+
- Review registration code
- Test all DI scenarios
- Update documentation

### Ninject

| Current Version | Target Version | Compatibility | Risk Level | Notes |
|----------------|----------------|---------------|-----------|--------|
| 3.3.4 | 3.3.6 | COMPATIBLE | LOW | Minimal changes |

**Projects Using**:
- RawRabbit.DependencyInjection.Ninject

**Known Issues**:
- Project less actively maintained
- Consider migration path to Microsoft.Extensions.DependencyInjection

**Migration Strategy**:
- Update to 3.3.6
- Evaluate long-term viability
- Consider deprecation notice

### Microsoft.Extensions.DependencyInjection

| Current Version | Target Version | Compatibility | Risk Level | Notes |
|----------------|----------------|---------------|-----------|--------|
| 1.0.2 | 9.0.0 | BREAKING | MEDIUM | Core DI changes |

**Projects Using**:
- RawRabbit.DependencyInjection.ServiceCollection

**Known Issues**:
- Service provider validation
- Scope validation changes
- Keyed services introduced in 8.0

**Migration Strategy**:
- Update to 9.0.0
- Leverage new keyed services feature
- Test all service resolution scenarios

## Serialization Libraries

### MessagePack

| Current Version | Target Version | Compatibility | Risk Level | Notes |
|----------------|----------------|---------------|-----------|--------|
| 1.7.3.4 | 2.5.140+ | BREAKING | MEDIUM | Major version change |

**Projects Using**:
- RawRabbit.Enrichers.MessagePack

**Known Issues**:
- API completely rewritten in v2
- Performance improvements
- Attribute system changed

**Migration Strategy**:
- Major code refactoring required
- Update all MessagePack attributes
- Extensive testing
- Consider C# 9 source generators

### protobuf-net

| Current Version | Target Version | Compatibility | Risk Level | Notes |
|----------------|----------------|---------------|-----------|--------|
| 2.3.2 | 3.2.30+ | COMPATIBLE | LOW | Stable upgrade path |

**Projects Using**:
- RawRabbit.Enrichers.Protobuf

**Known Issues**:
- Minimal breaking changes
- Enhanced performance in v3

**Migration Strategy**:
- Update to 3.2.30
- Validate serialization contracts
- Test edge cases

### ZeroFormatter

| Current Version | Target Version | Compatibility | Risk Level | Notes |
|----------------|----------------|---------------|-----------|--------|
| 1.6.4 | ARCHIVED | OBSOLETE | HIGH | Project abandoned |

**Projects Using**:
- RawRabbit.Enrichers.ZeroFormatter

**Known Issues**:
- Project no longer maintained
- No .NET 9 support
- Security vulnerabilities possible

**Migration Strategy**:
- DEPRECATE this enricher
- Recommend migration to MessagePack or protobuf-net
- Add obsolete warnings
- Remove in major version bump

## Resilience & Retry

### Polly

| Current Version | Target Version | Compatibility | Risk Level | Notes |
|----------------|----------------|---------------|-----------|--------|
| 5.3.1 | 8.4.1+ | BREAKING | HIGH | Complete API rewrite |

**Projects Using**:
- RawRabbit.Enrichers.Polly
- RawRabbit.Enrichers.Polly.Tests

**Known Issues**:
- v8 uses new Resilience Pipeline API
- Policy<T> patterns changed
- Context object completely different
- Breaking changes from v7→v8

**Migration Strategy**:
- Consider staying on v7.x temporarily
- Plan major refactor to v8 Resilience Pipelines
- Extensive testing required
- Update all policy definitions
- Document breaking changes

**Alternative**:
- Use Microsoft.Extensions.Resilience (new)
- Built on Polly v8
- Better DI integration

### Stateless

| Current Version | Target Version | Compatibility | Risk Level | Notes |
|----------------|----------------|---------------|-----------|--------|
| 3.0.0 | 5.16.0+ | COMPATIBLE | LOW | Stable upgrade |

**Projects Using**:
- RawRabbit.Operations.StateMachine

**Known Issues**:
- Minor API additions
- No breaking changes

**Migration Strategy**:
- Straightforward update
- Test state machine transitions
- Validate persistence if used

## ASP.NET Core Integration

### Microsoft.AspNetCore.Mvc.Core

| Current Version | Target Version | Compatibility | Risk Level | Notes |
|----------------|----------------|---------------|-----------|--------|
| 1.0.3 | 9.0.0 | BREAKING | HIGH | Major framework change |

**Projects Using**:
- RawRabbit.Enrichers.HttpContext

**Known Issues**:
- Massive API changes
- HttpContext moved to Microsoft.AspNetCore.Http
- Middleware pipeline changed

**Migration Strategy**:
- Update to 9.0.0
- Remove System.Web completely
- Test ASP.NET Core integration
- Update middleware

### Microsoft.AspNetCore.* (Sample)

| Current Version | Target Version | Compatibility | Risk Level | Notes |
|----------------|----------------|---------------|-----------|--------|
| 2.0.0 | 9.0.0 | BREAKING | MEDIUM | Framework upgrade |

**Projects Using**:
- RawRabbit.AspNet.Sample

**Known Issues**:
- Hosting model changed
- Startup pattern evolved
- Minimal API patterns available

**Migration Strategy**:
- Update to 9.0.0
- Modernize Startup.cs or use Minimal APIs
- Update middleware registration
- Test sample application

## Testing Frameworks

### xUnit

| Current Version | Target Version | Compatibility | Risk Level | Notes |
|----------------|----------------|---------------|-----------|--------|
| 2.3.0 | 2.9.0+ | COMPATIBLE | LOW | Stable framework |

**Projects Using**:
- All test projects (4)

**Known Issues**:
- None significant
- v3 in development but not released

**Migration Strategy**:
- Update to 2.9.0
- Update test SDK references
- Validate all tests run

### Moq

| Current Version | Target Version | Compatibility | Risk Level | Notes |
|----------------|----------------|---------------|-----------|--------|
| 4.7.137 | 4.20.70 | COMPATIBLE | LOW | Stable API |

**Projects Using**:
- RawRabbit.IntegrationTests
- RawRabbit.Tests
- RawRabbit.Enrichers.Polly.Tests

**Known Issues**:
- Minor API additions
- Setup/verification enhancements

**Migration Strategy**:
- Update to 4.20.70
- Review new mocking capabilities
- Validate mock behaviors

### BenchmarkDotNet

| Current Version | Target Version | Compatibility | Risk Level | Notes |
|----------------|----------------|---------------|-----------|--------|
| 0.10.3 | 0.14.0+ | COMPATIBLE | LOW | API stable |

**Projects Using**:
- RawRabbit.PerformanceTest

**Known Issues**:
- Improved .NET 9 support
- Better diagnostics

**Migration Strategy**:
- Update to 0.14.0
- Re-baseline benchmarks
- Document performance changes

## Logging & Diagnostics

### Serilog

| Current Version | Target Version | Compatibility | Risk Level | Notes |
|----------------|----------------|---------------|-----------|--------|
| 2.0.2-3.2.0 | 4.0.0+ | COMPATIBLE | LOW | Stable logger |

**Projects Using**:
- RawRabbit.AspNet.Sample
- RawRabbit.ConsoleApp.Sample

**Known Issues**:
- Configuration API enhanced
- Better structured logging

**Migration Strategy**:
- Update to 4.0.0+
- Update sink packages
- Review configuration

### Microsoft.Extensions.Logging

| Current Version | Target Version | Compatibility | Risk Level | Notes |
|----------------|----------------|---------------|-----------|--------|
| 2.0.0 | 9.0.0 | COMPATIBLE | LOW | Framework logging |

**Projects Using**:
- RawRabbit.AspNet.Sample

**Known Issues**:
- Log levels refined
- Performance improvements

**Migration Strategy**:
- Update to 9.0.0
- Validate log output
- Test log filtering

## Complete Dependency Upgrade Matrix

| Package | Current | Target | Risk | Projects | Priority |
|---------|---------|--------|------|----------|----------|
| RabbitMQ.Client | 5.0.1 | 6.8.1 | HIGH | 2 | CRITICAL |
| Newtonsoft.Json | 10.0.1 | 13.0.3 | LOW | 1 | HIGH |
| Autofac | 4.1.0 | 8.0.0 | MEDIUM | 1 | MEDIUM |
| Ninject | 3.3.4 | 3.3.6 | LOW | 1 | LOW |
| Microsoft.Extensions.DependencyInjection | 1.0.2 | 9.0.0 | MEDIUM | 1 | HIGH |
| MessagePack | 1.7.3.4 | 2.5.140 | MEDIUM | 1 | MEDIUM |
| protobuf-net | 2.3.2 | 3.2.30 | LOW | 1 | MEDIUM |
| ZeroFormatter | 1.6.4 | DEPRECATE | HIGH | 1 | CRITICAL |
| Polly | 5.3.1 | 8.4.1 | HIGH | 2 | HIGH |
| Stateless | 3.0.0 | 5.16.0 | LOW | 1 | MEDIUM |
| Microsoft.AspNetCore.Mvc.Core | 1.0.3 | 9.0.0 | HIGH | 1 | HIGH |
| Microsoft.AspNetCore.* | 2.0.0 | 9.0.0 | MEDIUM | 1 | MEDIUM |
| xunit | 2.3.0 | 2.9.0 | LOW | 4 | MEDIUM |
| Moq | 4.7.137 | 4.20.70 | LOW | 3 | MEDIUM |
| BenchmarkDotNet | 0.10.3 | 0.14.0 | LOW | 1 | LOW |
| Serilog.* | 2.x-3.x | 4.0.0 | LOW | 2 | LOW |
| Microsoft.Extensions.Logging | 2.0.0 | 9.0.0 | LOW | 1 | MEDIUM |

## Transitive Dependency Analysis

### Known Conflicts
1. **RabbitMQ.Client 5.0.1 → 6.8.1**
   - May conflict with System.Memory versions
   - Requires System.Threading.Channels

2. **Polly 5.3.1 → 8.4.1**
   - Completely different dependency tree
   - New Microsoft.Extensions.* dependencies

3. **MessagePack 1.x → 2.x**
   - Different System.Buffers requirements
   - Memory<T> usage patterns

### Resolution Strategy
1. Update core dependencies first (RabbitMQ.Client, Newtonsoft.Json)
2. Update framework packages (Microsoft.Extensions.*)
3. Update integration libraries (Polly, MessagePack)
4. Update test frameworks last
5. Run comprehensive dependency tree analysis at each stage

## Package Deprecation Recommendations

### Immediate Deprecation
1. **ZeroFormatter** - Project abandoned, no .NET 9 support
   - Remove RawRabbit.Enrichers.ZeroFormatter from solution
   - Add deprecation notice to README
   - Recommend MessagePack migration

### Future Deprecation (v3.0)
1. **Ninject integration** - Less maintained than alternatives
   - Mark as obsolete in v2.1
   - Remove in v3.0
   - Recommend Microsoft.Extensions.DependencyInjection

2. **net451 support** - Legacy framework
   - Drop in v2.1
   - netstandard2.0 minimum

## Version Pinning Strategy

### Pin Exact Versions (High Risk)
- RabbitMQ.Client: 6.8.1 (test thoroughly before upgrading)
- Polly: 7.2.4 (defer v8 upgrade to separate phase)

### Allow Minor Updates (Medium Risk)
- Autofac: 8.0.*
- Microsoft.Extensions.*: 9.0.*
- MessagePack: 2.5.*

### Allow Patch Updates (Low Risk)
- Newtonsoft.Json: 13.0.*
- xunit: 2.9.*
- Moq: 4.20.*

## Testing Strategy Per Dependency

### RabbitMQ.Client
- Unit tests: Connection factory, model creation
- Integration tests: Full message round-trip
- Performance tests: Throughput benchmarks
- Load tests: Connection pooling

### Serialization Libraries
- Unit tests: Serialize/deserialize all message types
- Integration tests: Cross-version compatibility
- Performance tests: Throughput and memory
- Edge cases: Null values, large objects

### Polly
- Unit tests: All policy configurations
- Integration tests: Actual retry scenarios
- Chaos tests: Simulated failures
- Performance tests: Policy overhead

## Rollback Strategy

### Per-Package Rollback
1. Maintain current package versions in git tags
2. Document known-good version combinations
3. Create rollback branch before each upgrade
4. Automated rollback tests

### Compatibility Matrix
```
RabbitMQ.Client  Polly   Autofac  Result
5.0.1           5.3.1   4.1.0    CURRENT (WORKS)
6.8.1           5.3.1   4.1.0    TEST PHASE 1
6.8.1           7.2.4   4.1.0    TEST PHASE 2
6.8.1           7.2.4   8.0.0    TEST PHASE 3
6.8.1           8.4.1   8.0.0    TARGET (FUTURE)
```

## Success Metrics

### Build Success
- All packages restore without conflicts
- No version resolution errors
- Clean build with no warnings

### Runtime Success
- All tests pass
- No runtime exceptions from dependency changes
- Performance within 5% of baseline

### Compatibility Success
- Existing consumers can upgrade without code changes
- Sample applications work correctly
- Documentation updated

## Next Steps

1. **Immediate**: Create feature branch for dependency updates
2. **Week 1**: Update RabbitMQ.Client and test thoroughly
3. **Week 2**: Update serialization libraries
4. **Week 3**: Update DI containers
5. **Week 4**: Update Polly (consider v7 vs v8)
6. **Week 5**: Integration testing
7. **Week 6**: Performance validation

## References

- [.NET 9 Breaking Changes](https://docs.microsoft.com/dotnet/core/compatibility/9.0)
- [NuGet Package Compatibility Guide](https://docs.microsoft.com/nuget/concepts/package-versioning)
- [RabbitMQ .NET Client Documentation](https://www.rabbitmq.com/dotnet-api-guide.html)
- [Polly v8 Migration Guide](https://www.thepollyproject.org/)
