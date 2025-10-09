# ADR-0009: Ninject Deprecation Strategy

**Status**: Proposed

**Date**: 2025-10-09

**Authors**: Architecture Specialist

**Reviewers**: Migration Architect, Lead Developer

**Tags**: migration, deprecation, dependency-injection, ninject, breaking-change

---

## Context

### Background

RawRabbit includes **RawRabbit.DependencyInjection.Ninject**, which provides integration with the **Ninject** DI container (version 3.3.4).

From Stage 1 assessment (dependency-matrix.md):
- **Ninject** has been **unmaintained since 2017** (7+ years)
- Last commit: August 2017
- No .NET 9 official support
- No security updates
- GitHub repository has minimal activity

**Current Status**:
- Ninject 3.3.6 available on NuGet (minor update from 3.3.4)
- Community forks exist but lack official status
- Most .NET projects have migrated to Microsoft.Extensions.DependencyInjection

**Alternatives**:
- **Microsoft.Extensions.DependencyInjection**: De facto standard (.NET 9 recommended)
- **Autofac**: Enterprise-grade, actively maintained

### Problem Statement

Should RawRabbit continue to support an **unmaintained DI container** with potential security risks?

**Key Questions**:
1. Should we deprecate Ninject support immediately or gradually?
2. What is the migration path for existing Ninject users?
3. What is the timeline for full removal?
4. What warning/documentation should we provide?

### Constraints

**Security Constraints**:
- Unmaintained library is security risk (no patches)
- Timeline: Stage 2 decision, Phase 4 implementation (Week 5)

**User Impact Constraints**:
- Some users may still use Ninject
- Enterprise applications may be slow to migrate
- Breaking change requires clear communication

### Assumptions

1. Most Ninject users can migrate to Microsoft.Extensions.DependencyInjection
2. Ninject usage in RawRabbit community is LOW
3. 12-month deprecation timeline is sufficient

---

## Decision

### Chosen Solution

**Gradual Deprecation: Mark [Obsolete] in v2.1.0, Remove in v3.0.0**

**Timeline**:
- **v2.1.0** (2025-11): Mark [Obsolete], add deprecation warning, update to 3.3.6
- **v2.2.0** (2026 Q2): Remove from samples and primary documentation
- **v3.0.0** (2026 Q4): Remove RawRabbit.DependencyInjection.Ninject entirely

**Rationale**:
- Gradual deprecation gives users time to migrate (12-18 months)
- [Obsolete] warning alerts users immediately
- Minimal maintenance burden (keep working until v3.0.0)
- Clear migration path to Microsoft.Extensions.DependencyInjection

### Implementation Details

#### v2.1.0: Mark [Obsolete]

```csharp
// NinjectExtensions.cs
namespace RawRabbit.DependencyInjection.Ninject
{
    [Obsolete(
        "Ninject has been unmaintained since 2017. " +
        "RawRabbit.DependencyInjection.Ninject will be removed in v3.0.0. " +
        "Migrate to Microsoft.Extensions.DependencyInjection or Autofac. " +
        "See https://github.com/pardahlman/RawRabbit/docs/migration-guides/ninject-migration.md",
        false  // Warning, not error
    )]
    public static class NinjectExtensions
    {
        [Obsolete("Use Microsoft.Extensions.DependencyInjection instead. See migration guide.")]
        public static void RegisterRawRabbit(
            this IKernel kernel,
            RawRabbitConfiguration configuration)
        {
            // Existing implementation
        }
    }
}
```

**README.md Update**:
```markdown
## Dependency Injection

RawRabbit supports multiple DI containers:

### ✅ Recommended: Microsoft.Extensions.DependencyInjection

```csharp
services.AddRawRabbit(cfg => cfg.HostName = "localhost");
```

### ✅ Supported: Autofac

```csharp
builder.RegisterModule(new RawRabbitModule(config));
```

### ⚠️ DEPRECATED: Ninject

**Ninject has been unmaintained since 2017 and will be removed in v3.0.0.**

For migration guide, see: [Ninject Migration Guide](docs/migration-guides/ninject-migration.md)
```

#### Migration Guide

**docs/migration-guides/ninject-migration.md**:
```markdown
# Ninject Deprecation & Migration Guide

## Overview

RawRabbit.DependencyInjection.Ninject is **deprecated** and will be removed in v3.0.0.

**Reason**: Ninject has been unmaintained since 2017 (7+ years), posing security risks.

**Timeline**:
- v2.1.0 (2025-11): Marked [Obsolete], deprecation warning
- v2.2.0 (2026 Q2): Removed from documentation
- v3.0.0 (2026 Q4): Package removed entirely

## Recommended Migration: Microsoft.Extensions.DependencyInjection

Microsoft.Extensions.DependencyInjection is the recommended replacement for Ninject users.

### Why Microsoft.Extensions.DependencyInjection?

- **De facto standard**: Used by ASP.NET Core, Azure Functions, .NET MAUI
- **Active maintenance**: Regular updates, .NET 9 support
- **Modern features**: Keyed services, scope validation
- **Best documentation**: Microsoft Learn, extensive samples

### Migration Steps

#### 1. Install Package

```bash
dotnet remove package RawRabbit.DependencyInjection.Ninject
dotnet add package RawRabbit.DependencyInjection.ServiceCollection
```

#### 2. Update Registration Code

```csharp
// OLD (Ninject)
using Ninject;

var kernel = new StandardKernel();
kernel.Bind<IBusClient>().To<BusClient>().InSingletonScope();
kernel.RegisterRawRabbit(new RawRabbitConfiguration
{
    HostName = "localhost"
});

var client = kernel.Get<IBusClient>();

// NEW (Microsoft.Extensions.DependencyInjection)
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddRawRabbit(cfg =>
{
    cfg.HostName = "localhost";
});

var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<IBusClient>();
```

#### 3. Update ASP.NET Core Startup

```csharp
// OLD (Ninject in ASP.NET Core)
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Ninject adapter for ASP.NET Core
        var kernel = new StandardKernel();
        kernel.RegisterRawRabbit(config);
        services.AddSingleton(kernel);
    }
}

// NEW (Native Microsoft.Extensions.DependencyInjection)
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRawRabbit(cfg => cfg.HostName = "localhost");
    }
}
```

#### 4. Update Injection Points

```csharp
// No changes needed - same IBusClient interface
public class MyService
{
    private readonly IBusClient _busClient;

    public MyService(IBusClient busClient)
    {
        _busClient = busClient;
    }
}
```

### Feature Comparison

| Feature | Ninject | Microsoft.Extensions.DI |
|---------|---------|------------------------|
| Basic DI | ✅ | ✅ |
| Constructor injection | ✅ | ✅ |
| Property injection | ✅ | ❌ (by design) |
| Named services | ✅ | ✅ (keyed services, .NET 8+) |
| Lifetime scopes | ✅ | ✅ |
| Interceptors | ✅ | ❌ (use middleware) |
| Active maintenance | ❌ | ✅ |
| .NET 9 support | ⚠️ Unofficial | ✅ Official |

### Named Services (Ninject → Keyed Services)

```csharp
// OLD (Ninject)
kernel.Bind<IBusClient>().To<BusClient>()
    .InSingletonScope()
    .Named("rabbit1");

kernel.Bind<IBusClient>().To<BusClient>()
    .InSingletonScope()
    .Named("rabbit2");

var client1 = kernel.Get<IBusClient>("rabbit1");
var client2 = kernel.Get<IBusClient>("rabbit2");

// NEW (Microsoft.Extensions.DI - .NET 8+)
services.AddKeyedSingleton<IBusClient>("rabbit1", (sp, key) =>
    BusClientFactory.CreateClient(config1));

services.AddKeyedSingleton<IBusClient>("rabbit2", (sp, key) =>
    BusClientFactory.CreateClient(config2));

// Inject named clients
public class MyService(
    [FromKeyedServices("rabbit1")] IBusClient client1,
    [FromKeyedServices("rabbit2")] IBusClient client2)
{
}
```

## Alternative Migration: Autofac

If you require advanced DI features (interceptors, decorators), consider Autofac:

```bash
dotnet remove package RawRabbit.DependencyInjection.Ninject
dotnet add package RawRabbit.DependencyInjection.Autofac
```

```csharp
using Autofac;

var builder = new ContainerBuilder();
builder.RegisterModule(new RawRabbitModule(new RawRabbitConfiguration
{
    HostName = "localhost"
}));

var container = builder.Build();
var client = container.Resolve<IBusClient>();
```

## Interceptor Pattern Migration

Ninject's interceptor pattern can be replaced with middleware:

```csharp
// OLD (Ninject Interceptor)
kernel.Intercept(x => x.Service<IBusClient>())
    .With<LoggingInterceptor>();

// NEW (RawRabbit Middleware)
client.UseMiddleware<LoggingMiddleware>();
```

## Support

For migration assistance:
- [GitHub Discussions](https://github.com/pardahlman/RawRabbit/discussions)
- [Microsoft.Extensions.DI Documentation](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection)
```

### Rationale

**Why Gradual Deprecation (Not Immediate Removal)?**

1. **User Impact**: Give users 12-18 months to migrate
2. **Enterprise Reality**: Large applications need time to plan migrations
3. **Community Goodwill**: Abrupt removal alienates users
4. **Risk Mitigation**: Gradual deprecation reduces backlash

**Why 3.3.6 Update in v2.1.0?**

1. **Minimal Effort**: Minor version bump (3.3.4 → 3.3.6)
2. **Functional**: Keeps adapter working for deprecation period
3. **Good Faith**: Shows we're not abandoning users immediately

**Why Full Removal in v3.0.0?**

1. **Security**: Unmaintained library is long-term risk
2. **Maintenance**: Reduces testing matrix and support burden
3. **Clear Break**: v3.0.0 is major version (breaking changes expected)

---

## Alternatives Considered

### Alternative 1: Immediate Removal (v2.1.0)

**Description**: Remove Ninject adapter in v2.1.0 without deprecation period

**Pros**:
- Eliminates security risk immediately
- Clearest message
- Simplest implementation

**Cons**:
- **User Backlash**: Abrupt breaking change
- **Adoption Risk**: Users may not upgrade to v2.1.0
- **Poor UX**: Forced migration without warning

**Why Rejected**: Too disruptive. Gradual deprecation is more respectful to users.

### Alternative 2: Maintain Indefinitely

**Description**: Keep Ninject adapter, update to 3.3.6, maintain indefinitely

**Pros**:
- No breaking change
- Maximum compatibility
- No user migration required

**Cons**:
- **Security Risk**: Unmaintained dependency
- **Maintenance Burden**: Must support 3 DI containers
- **False Promise**: Suggests Ninject is viable long-term

**Why Rejected**: Security risk unacceptable. Better to deprecate with clear timeline.

### Alternative 3: Community Fork

**Description**: Maintain RawRabbit fork of Ninject

**Pros**:
- Backward compatibility
- Could add .NET 9 support

**Cons**:
- **High Burden**: Must maintain entire DI container
- **Low Value**: Microsoft.Extensions.DI is superior
- **Opportunity Cost**: Time better spent on RawRabbit features

**Why Rejected**: Not worth maintaining DI container when excellent alternatives exist.

---

## Consequences

### Positive Consequences

- **Reduces Security Risk**: Unmaintained dependency phased out
- **Clearer Strategy**: Microsoft.Extensions.DI as primary, Autofac as secondary
- **Reduced Maintenance**: One less DI adapter to support (eventually)
- **User Guidance**: Clear migration path

### Negative Consequences

- **Breaking Change**: Ninject users must migrate (eventually)
- **Migration Effort**: Users must update code
- **Short-Term Maintenance**: Must support Ninject until v3.0.0

### Risks

**Risk 1: User Backlash**
- **Likelihood**: LOW (Ninject usage declining)
- **Impact**: MEDIUM (negative feedback)
- **Mitigation**:
  - 12-18 month deprecation timeline
  - Comprehensive migration guide
  - Clear communication about security risks

**Risk 2: Delayed Migration**
- **Likelihood**: MEDIUM (users may ignore [Obsolete])
- **Impact**: LOW (they can stay on v2.x.x)
- **Mitigation**:
  - Visible deprecation warnings in builds
  - Documentation emphasizes risks
  - v3.0.0 release notes clearly state removal

### Technical Debt

**Created**:
- Must maintain Ninject adapter until v3.0.0

**Addressed**:
- Security risk eliminated (in v3.0.0)
- Testing matrix simplified (in v3.0.0)

---

## Migration Impact

### Breaking Changes

- **v2.1.0**: [Obsolete] warning (build warning)
- **v3.0.0**: RawRabbit.DependencyInjection.Ninject package removed

### Migration Path

See Migration Guide above.

---

## Validation

### Acceptance Criteria

**v2.1.0**:
- [ ] Ninject adapter marked [Obsolete]
- [ ] Deprecation warning visible in builds
- [ ] Migration guide published
- [ ] README.md updated with deprecation notice
- [ ] CHANGELOG documents deprecation

**v3.0.0**:
- [ ] RawRabbit.DependencyInjection.Ninject removed from solution
- [ ] Documentation no longer references Ninject
- [ ] Samples use Microsoft.Extensions.DI or Autofac

---

## Dependencies

### Affected Components

- RawRabbit.DependencyInjection.Ninject (deprecated)

### Related ADRs

- **ADR-0004**: Dependency Update Strategy
- **ADR-0007**: Dependency Injection Strategy

---

## Timeline

**Proposed**: 2025-10-09

**v2.1.0** (Deprecation): 2025-11-22
- Mark [Obsolete]
- Update to Ninject 3.3.6
- Publish migration guide

**v2.2.0** (Documentation): 2026 Q2
- Remove Ninject from primary documentation
- Remove Ninject samples

**v3.0.0** (Removal): 2026 Q4
- Remove RawRabbit.DependencyInjection.Ninject entirely

---

## References

- [Ninject GitHub](https://github.com/ninject/Ninject)
- [Microsoft.Extensions.DI Documentation](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection)
- [Dependency Matrix](../stage-1/dependency-matrix.md)

---

## Revision History

| Date | Author | Changes |
|------|--------|---------|
| 2025-10-09 | Architecture Specialist | Initial draft (Stage 2.1) |
