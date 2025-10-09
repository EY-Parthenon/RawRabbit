# RawRabbit .NET 9 Migration Roadmap

**Stage**: 1.2 - Discovery & Analysis
**Date**: 2025-10-09
**Status**: Complete

## Executive Summary

Analyzed 32 projects in the RawRabbit solution to establish migration baseline for .NET 9 upgrade. The codebase currently targets legacy frameworks (net451, netstandard1.5-1.6, netcoreapp1.x-2.0) and requires systematic modernization.

## Project Inventory

### Core Library (1 project)
| Project | Current Targets | Type | Lines of Code Est. |
|---------|----------------|------|-------------------|
| RawRabbit | netstandard1.5, net451 | Library | Core |

### Compatibility Layer (1 project)
| Project | Current Targets | Type | Complexity |
|---------|----------------|------|-----------|
| RawRabbit.Compatibility.Legacy | netstandard1.5, net451 | Library | MEDIUM |

### Dependency Injection Adapters (3 projects)
| Project | Current Targets | Type | Complexity |
|---------|----------------|------|-----------|
| RawRabbit.DependencyInjection.Autofac | netstandard1.5, net451 | Library | SIMPLE |
| RawRabbit.DependencyInjection.Ninject | netstandard2.0, net451 | Library | SIMPLE |
| RawRabbit.DependencyInjection.ServiceCollection | netstandard1.5, net451 | Library | SIMPLE |

### Enrichers (11 projects)
| Project | Current Targets | Type | Complexity |
|---------|----------------|------|-----------|
| RawRabbit.Enrichers.Attributes | netstandard1.5, net451 | Library | SIMPLE |
| RawRabbit.Enrichers.GlobalExecutionId | netstandard1.5, net451 | Library | SIMPLE |
| RawRabbit.Enrichers.HttpContext | netstandard1.6, net451 | Library | COMPLEX |
| RawRabbit.Enrichers.MessageContext | netstandard1.5, net451 | Library | MEDIUM |
| RawRabbit.Enrichers.MessageContext.Respond | netstandard1.5, net451 | Library | SIMPLE |
| RawRabbit.Enrichers.MessageContext.Subscribe | netstandard1.5, net451 | Library | SIMPLE |
| RawRabbit.Enrichers.MessagePack | netstandard1.6, net451 | Library | SIMPLE |
| RawRabbit.Enrichers.Polly | netstandard1.5, net451 | Library | MEDIUM |
| RawRabbit.Enrichers.Protobuf | netstandard1.5, net451 | Library | SIMPLE |
| RawRabbit.Enrichers.QueueSuffix | netstandard1.5, net451 | Library | SIMPLE |
| RawRabbit.Enrichers.RetryLater | netstandard1.5, net451 | Library | SIMPLE |
| RawRabbit.Enrichers.ZeroFormatter | netstandard1.6, net451 | Library | SIMPLE |

### Operations (7 projects)
| Project | Current Targets | Type | Complexity |
|---------|----------------|------|-----------|
| RawRabbit.Operations.Get | netstandard1.5, net451 | Library | SIMPLE |
| RawRabbit.Operations.MessageSequence | netstandard1.5, net451 | Library | MEDIUM |
| RawRabbit.Operations.Publish | netstandard1.5, net451 | Library | SIMPLE |
| RawRabbit.Operations.Request | netstandard1.5, net451 | Library | SIMPLE |
| RawRabbit.Operations.Respond | netstandard1.5, net451 | Library | SIMPLE |
| RawRabbit.Operations.StateMachine | netstandard1.5, net451 | Library | MEDIUM |
| RawRabbit.Operations.Subscribe | netstandard1.5, net451 | Library | SIMPLE |
| RawRabbit.Operations.Tools | netstandard1.5, net451 | Library | SIMPLE |

### Test Projects (4 projects)
| Project | Current Targets | Type | Complexity |
|---------|----------------|------|-----------|
| RawRabbit.Enrichers.Polly.Tests | net46 (old-style) | Test | MEDIUM |
| RawRabbit.IntegrationTests | net46 | Test | MEDIUM |
| RawRabbit.PerformanceTest | netcoreapp1.1 | Test | SIMPLE |
| RawRabbit.Tests | net46 | Test | MEDIUM |

### Sample Projects (3 projects)
| Project | Current Targets | Type | Complexity |
|---------|----------------|------|-----------|
| RawRabbit.AspNet.Sample | netcoreapp2.0 | App | SIMPLE |
| RawRabbit.ConsoleApp.Sample | netcoreapp1.0 | App | SIMPLE |
| RawRabbit.Messages.Sample | netstandard1.5, net451 | Library | SIMPLE |

## Current vs. Target Framework Matrix

### Current State
- **net451**: 27 projects (legacy .NET Framework 4.5.1)
- **netstandard1.5**: 21 projects (outdated)
- **netstandard1.6**: 3 projects (outdated)
- **netstandard2.0**: 1 project (outdated but closer to modern)
- **net46**: 3 test projects
- **netcoreapp1.0**: 1 sample (severely outdated)
- **netcoreapp1.1**: 1 test (severely outdated)
- **netcoreapp2.0**: 1 sample (outdated)

### Target State
- **net9.0**: All applications and test projects
- **netstandard2.0**: Library projects (for backward compatibility)
- **net8.0**: Optional multi-targeting for libraries
- **Dropped**: net451, netstandard1.x, netcoreapp1.x, netcoreapp2.x

## Deprecated APIs Identified

### Critical Issues

1. **System.Web.HttpContext (2 files)**
   - Location: `RawRabbit.Enrichers.HttpContext`
   - Files: `NetFxHttpContextMiddleware.cs`, `PipeContextHttpExtensions.cs`
   - Impact: HIGH
   - Solution: Conditional compilation already exists for ASP.NET Core
   - Action: Remove net451 target, keep netstandard2.0+ only

2. **Old-Style Project Format (1 project)**
   - Project: `RawRabbit.Enrichers.Polly.Tests`
   - Format: Legacy .csproj (non-SDK style)
   - Impact: MEDIUM
   - Solution: Convert to SDK-style project file

### Low Priority Issues

3. **Legacy Package References**
   - packages.config in test projects
   - Should migrate to PackageReference format

## Migration Complexity Assessment

### SIMPLE (20 projects)
**Characteristics**: No deprecated APIs, straightforward framework update
**Effort**: 1-2 hours each
**Projects**:
- All DependencyInjection adapters (3)
- Most Enrichers (9)
- Most Operations (6)
- Sample projects (3)

**Migration Steps**:
1. Update TargetFrameworks to net9.0;netstandard2.0
2. Update NuGet packages
3. Verify builds
4. Run tests

### MEDIUM (8 projects)
**Characteristics**: Complex dependencies, some conditional compilation
**Effort**: 3-4 hours each
**Projects**:
- RawRabbit.Compatibility.Legacy
- RawRabbit.Enrichers.MessageContext
- RawRabbit.Enrichers.Polly
- RawRabbit.Operations.MessageSequence
- RawRabbit.Operations.StateMachine
- RawRabbit.Enrichers.Polly.Tests (project format conversion)
- RawRabbit.IntegrationTests
- RawRabbit.Tests

**Migration Steps**:
1. Analyze transitive dependencies
2. Update TargetFrameworks
3. Update NuGet packages with version compatibility checks
4. Refactor conditional compilation if needed
5. Run comprehensive tests

### COMPLEX (3 projects)
**Characteristics**: System.Web usage, multi-platform concerns
**Effort**: 6-8 hours each
**Projects**:
- RawRabbit (core library - critical path)
- RawRabbit.Enrichers.HttpContext (System.Web removal)

**Migration Steps**:
1. Remove net451 target completely
2. Update System.Web.HttpContext to ASP.NET Core HttpContext
3. Update all NuGet packages
4. Extensive testing required
5. Update documentation

## Recommended Migration Order

### Phase 1: Foundation (Core Library)
**Duration**: 1-2 weeks
**Priority**: CRITICAL
1. RawRabbit (core) - Must be first
2. RawRabbit.Operations.Tools - Used by many operations

### Phase 2: Simple Operations & Enrichers (Batch 1)
**Duration**: 1 week
**Priority**: HIGH
- All SIMPLE Operations (6 projects)
- Simple Enrichers without dependencies (5 projects)

### Phase 3: Complex Operations & Enrichers (Batch 2)
**Duration**: 1-2 weeks
**Priority**: HIGH
- RawRabbit.Enrichers.HttpContext (COMPLEX - System.Web removal)
- RawRabbit.Enrichers.Polly (MEDIUM)
- RawRabbit.Operations.MessageSequence (MEDIUM)
- RawRabbit.Operations.StateMachine (MEDIUM)
- Remaining enrichers (6 projects)

### Phase 4: Dependency Injection & Compatibility
**Duration**: 3-5 days
**Priority**: MEDIUM
- RawRabbit.DependencyInjection.Autofac
- RawRabbit.DependencyInjection.Ninject
- RawRabbit.DependencyInjection.ServiceCollection
- RawRabbit.Compatibility.Legacy

### Phase 5: Test Projects
**Duration**: 1 week
**Priority**: HIGH
- RawRabbit.Enrichers.Polly.Tests (convert project format)
- RawRabbit.IntegrationTests
- RawRabbit.Tests
- RawRabbit.PerformanceTest

### Phase 6: Samples & Documentation
**Duration**: 3-5 days
**Priority**: LOW
- RawRabbit.AspNet.Sample
- RawRabbit.ConsoleApp.Sample
- RawRabbit.Messages.Sample
- Update all documentation

## Project Interdependencies

### Dependency Graph (Core Dependencies)
```
RawRabbit (CORE)
├── Operations.Tools
│   ├── Operations.Get
│   └── Operations.MessageSequence
├── Operations.Publish
│   └── Operations.MessageSequence
├── Operations.Subscribe
│   ├── Operations.StateMachine
│   └── Operations.MessageSequence
├── Operations.Request
├── Operations.Respond
│   └── Enrichers.MessageContext.Respond
├── Enrichers.GlobalExecutionId
│   └── Operations.MessageSequence
├── Enrichers.MessageContext
│   ├── Enrichers.MessageContext.Subscribe
│   ├── Enrichers.MessageContext.Respond
│   └── Compatibility.Legacy
└── All other Enrichers (direct dependency on RawRabbit core)
```

## Risk Assessment

### High Risk
1. **RawRabbit.Enrichers.HttpContext** - System.Web removal may break existing code
2. **RabbitMQ.Client** - Version 5.0.1 → latest may have breaking changes
3. **Test Project Conversion** - Old-style project format conversion

### Medium Risk
1. **Dependency version conflicts** - Multiple serialization libraries
2. **Breaking changes in DI containers** - Autofac, Ninject versions
3. **Polly version update** - Policy API may have changed

### Low Risk
1. **Simple framework updates** - Most projects are straightforward
2. **Sample projects** - Can be rewritten if needed

## Success Criteria

### Build Success
- All 32 projects build without errors on .NET 9
- No warnings for deprecated APIs
- All multi-targeting scenarios work correctly

### Test Success
- All unit tests pass
- All integration tests pass
- Performance tests show no regression

### Compatibility Success
- Backward compatibility maintained where feasible
- Migration guide created for breaking changes
- Sample code updated and tested

## Next Steps

1. **Stage 2**: Architecture review and design decisions
2. **Stage 3**: Begin Phase 1 migration (core library)
3. **Stage 4**: Systematic migration of remaining components
4. **Stage 5**: Integration testing and validation
5. **Stage 6**: Documentation and release preparation

## Notes

- Total estimated effort: 6-8 weeks for complete migration
- Critical path: Core library → Operations → Enrichers → Tests → Samples
- Parallel work possible after core library completion
- Comprehensive testing required at each phase
