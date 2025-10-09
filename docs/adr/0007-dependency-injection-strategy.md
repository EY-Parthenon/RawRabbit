# ADR-0007: Dependency Injection Strategy

**Status**: Proposed

**Date**: 2025-10-09

**Authors**: Architecture Specialist

**Reviewers**: Migration Architect, Lead Developer

**Tags**: migration, dependency-injection, di, autofac, ninject, microsoft-extensions

---

## Context

### Background

RawRabbit currently supports **3 DI container adapters**:
1. **Microsoft.Extensions.DependencyInjection 1.0.2** (RawRabbit.DependencyInjection.ServiceCollection)
2. **Autofac 4.1.0** (RawRabbit.DependencyInjection.Autofac)
3. **Ninject 3.3.4** (RawRabbit.DependencyInjection.Ninject)

From ADR-0004 (Dependency Update Strategy), we need to update these containers to versions compatible with .NET 9. However, **Ninject has been unmaintained since 2017** (see ADR-0009 for deprecation strategy).

Microsoft.Extensions.DependencyInjection has become the **de facto standard** for .NET dependency injection, with wide adoption across ASP.NET Core, Azure Functions, and most modern .NET libraries.

### Problem Statement

What should be the strategic direction for DI container support in RawRabbit v2.1.0?

**Key Questions**:
1. Should we prioritize Microsoft.Extensions.DependencyInjection as primary?
2. Should we continue supporting Autofac (widely used in legacy applications)?
3. How do we handle Ninject deprecation (see ADR-0009)?
4. Should we leverage new .NET 8/9 DI features (keyed services, IServiceProviderFactory)?

### Constraints

**Technical Constraints**:
- Must support .NET 8 and .NET 9 (ADR-0003)
- Ninject 3.3.4 unmaintained since 2017 (security risk)
- Microsoft.Extensions.DependencyInjection 9.0.0 has breaking changes (keyed services)
- Autofac 8.1.0 has registration API changes

**Timeline Constraints**:
- DI adapter updates in Phase 4 (Week 5)
- Must not block Phase 1-3 (core library and operations)

### Assumptions

1. Most new .NET projects use Microsoft.Extensions.DependencyInjection
2. Legacy Autofac users need continued support
3. Ninject users can migrate to Microsoft.Extensions.DependencyInjection or Autofac
4. Users expect DI registration to be simple and intuitive

---

## Decision

### Chosen Solution

**Microsoft.Extensions.DependencyInjection as Primary, Autofac as Secondary, Ninject Deprecated**

#### Tier 1: Microsoft.Extensions.DependencyInjection (Primary)

**Strategic Direction**: Make Microsoft.Extensions.DependencyInjection the **recommended and best-supported** DI container.

**Upgrade**: 1.0.2 → **9.0.0**

**Rationale**:
- De facto standard for .NET ecosystem
- Built-in to ASP.NET Core, Azure Functions, .NET MAUI, etc.
- Microsoft-supported and actively developed
- Keyed services feature (new in .NET 8) enables advanced scenarios
- Wide community adoption

**Features to Leverage**:
- Keyed services for named RabbitMQ clients
- Service provider validation (detect missing dependencies)
- Scope validation (detect scope mismatches)
- IServiceProviderFactory<T> for integration with generic hosts

**Implementation**:
```csharp
// Extension method for IServiceCollection
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRawRabbit(
        this IServiceCollection services,
        Action<RawRabbitConfiguration> configureOptions = null)
    {
        services.AddSingleton<IConnectionFactory>(sp =>
        {
            var config = sp.GetRequiredService<RawRabbitConfiguration>();
            return new ConnectionFactory
            {
                HostName = config.HostName,
                UserName = config.UserName,
                Password = config.Password
            };
        });

        services.AddSingleton<IBusClient, BusClient>();

        // Configure options
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        return services;
    }

    // .NET 8+ Keyed Services for multiple RabbitMQ clients
    public static IServiceCollection AddRawRabbit(
        this IServiceCollection services,
        string key,
        Action<RawRabbitConfiguration> configureOptions)
    {
        services.AddKeyedSingleton<IBusClient>(key, (sp, key) =>
        {
            var config = new RawRabbitConfiguration();
            configureOptions(config);
            return BusClientFactory.CreateClient(config);
        });

        return services;
    }
}

// Usage (standard)
services.AddRawRabbit(cfg =>
{
    cfg.HostName = "localhost";
});

// Usage (keyed services - .NET 8+)
services.AddRawRabbit("rabbit1", cfg => cfg.HostName = "rabbit1.local");
services.AddRawRabbit("rabbit2", cfg => cfg.HostName = "rabbit2.local");

// Inject named clients
public class MyService
{
    public MyService(
        [FromKeyedServices("rabbit1")] IBusClient client1,
        [FromKeyedServices("rabbit2")] IBusClient client2)
    {
        // Use multiple RabbitMQ clients
    }
}
```

#### Tier 2: Autofac (Secondary, Continued Support)

**Strategic Direction**: Continue supporting Autofac for **legacy applications** that cannot migrate to Microsoft.Extensions.DependencyInjection.

**Upgrade**: 4.1.0 → **8.1.0**

**Rationale**:
- Widely used in enterprise .NET Framework applications
- Advanced DI features (interceptors, decorators, lifetime scopes)
- Users migrating from .NET Framework may require Autofac
- Breaking API changes manageable with adapter pattern

**Implementation**:
```csharp
// Autofac module
public class RawRabbitModule : Module
{
    private readonly RawRabbitConfiguration _configuration;

    public RawRabbitModule(RawRabbitConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override void Load(ContainerBuilder builder)
    {
        // Register configuration
        builder.RegisterInstance(_configuration).AsSelf();

        // Register connection factory
        builder.Register(c => new ConnectionFactory
        {
            HostName = _configuration.HostName,
            UserName = _configuration.UserName,
            Password = _configuration.Password
        }).As<IConnectionFactory>().SingleInstance();

        // Register bus client
        builder.RegisterType<BusClient>()
            .As<IBusClient>()
            .SingleInstance();
    }
}

// Usage
var builder = new ContainerBuilder();
builder.RegisterModule(new RawRabbitModule(new RawRabbitConfiguration
{
    HostName = "localhost"
}));

var container = builder.Build();
var client = container.Resolve<IBusClient>();
```

**Migration Guide** (Autofac 4.x → 8.x):
```csharp
// Autofac 4.x (OLD)
builder.Register(c => new BusClient(...)).SingleInstance();

// Autofac 8.x (NEW - same API)
builder.Register(c => new BusClient(...)).SingleInstance();
// API is mostly backward compatible
```

#### Tier 3: Ninject (Deprecated)

See **ADR-0009: Ninject Deprecation Strategy** for full details.

**Strategic Direction**: Deprecate Ninject support, mark as obsolete in v2.1.0, remove in v3.0.0.

**Rationale**:
- Unmaintained since 2017 (7+ years)
- No .NET 9 official support
- Security risk (no updates)
- Users should migrate to Microsoft.Extensions.DependencyInjection or Autofac

**Deprecation Timeline**:
- v2.1.0: Mark [Obsolete], add deprecation warning
- v2.2.0: Remove from samples and documentation
- v3.0.0: Remove RawRabbit.DependencyInjection.Ninject entirely

### Rationale

**Why Prioritize Microsoft.Extensions.DependencyInjection?**

1. **Ecosystem Alignment**: De facto standard for .NET
2. **Modern Features**: Keyed services, scope validation, IServiceProviderFactory
3. **Best Documentation**: Microsoft Learn, extensive samples
4. **Community Support**: Largest community, most StackOverflow answers
5. **Future-Proof**: Microsoft's strategic investment

**Why Continue Autofac Support?**

1. **Enterprise Usage**: Many large .NET Framework migrations use Autofac
2. **Advanced Features**: Interceptors, decorators, advanced lifetime management
3. **Stable**: Well-maintained, active development
4. **Migration Path**: Easier transition from .NET Framework to .NET 9 with Autofac

**Why Deprecate Ninject?**

1. **Unmaintained**: 7+ years without updates
2. **Security Risk**: No security patches
3. **Low Adoption**: Declining usage in .NET ecosystem
4. **Better Alternatives**: Microsoft.Extensions.DependencyInjection or Autofac provide same capabilities

---

## Alternatives Considered

### Alternative 1: Microsoft.Extensions.DependencyInjection Only

**Description**: Support only Microsoft.Extensions.DependencyInjection, drop Autofac and Ninject

**Pros**:
- Simplest maintenance
- Clear strategic direction
- Single DI path
- Smallest package footprint

**Cons**:
- **Breaking Change**: Autofac users forced to migrate
- **Enterprise Friction**: Large applications may resist
- **Migration Burden**: Users must rewrite DI registrations

**Why Rejected**: Too disruptive for enterprise users. Autofac is stable and well-maintained, no reason to force migration immediately. Can revisit for v3.0.0.

### Alternative 2: Support All Three (Autofac, Ninject, Microsoft)

**Description**: Continue supporting all three DI containers in v2.1.0

**Pros**:
- Maximum compatibility
- No forced migrations
- All users supported

**Cons**:
- **Ninject Security Risk**: Unmaintained library
- **3x Maintenance**: Three adapter projects to maintain
- **Testing Burden**: 3x test matrix
- **Confusing Message**: No clear recommendation

**Why Rejected**: Ninject is a security risk and should not be supported long-term. Better to deprecate with clear migration path.

### Alternative 3: Recommend DIY DI Registration

**Description**: Remove all DI adapter projects, document manual registration patterns

**Pros**:
- Zero maintenance burden
- Users have full control
- Simplest codebase

**Cons**:
- **User Burden**: Every user must write registration code
- **Inconsistency**: Different registration patterns across applications
- **Poor DX**: Worse developer experience vs. built-in extensions

**Why Rejected**: DI extension methods are core value-add for library. Removing them would hurt user experience significantly.

---

## Consequences

### Positive Consequences

**Microsoft.Extensions.DependencyInjection as Primary**:
- Clear strategic direction for new projects
- Access to modern .NET 8/9 DI features
- Best-in-class documentation and community support
- Keyed services enable advanced scenarios (multiple RabbitMQ clients)

**Autofac Continued Support**:
- Enterprise users have migration path
- Advanced DI features available
- Stable upgrade path (4.x → 8.x)

**Ninject Deprecation**:
- Eliminates security risk
- Reduces maintenance burden
- Clears up confusion about which DI container to use

### Negative Consequences

**Ninject Users Forced to Migrate**:
- Breaking change for Ninject users
- Migration effort required
- Potential adoption resistance

**Autofac Maintenance Burden**:
- Must maintain two DI adapter projects (Microsoft + Autofac)
- 2x testing matrix
- Documentation for both paths

### Risks

**Risk 1: Ninject User Backlash**
- **Likelihood**: LOW-MEDIUM
- **Impact**: MEDIUM (negative feedback, slow adoption)
- **Mitigation**:
  - Clear communication about security risks
  - Provide migration guide (Ninject → Microsoft.Extensions.DependencyInjection)
  - 12-month deprecation timeline (v2.1.0 → v3.0.0)

**Risk 2: Autofac API Breaking Changes**
- **Likelihood**: LOW
- **Impact**: MEDIUM (adapter update required)
- **Mitigation**:
  - Autofac 8.x is mostly backward compatible with 4.x
  - Test adapter thoroughly
  - Document any breaking changes

### Technical Debt

**Created**:
- Dual DI adapter maintenance (Microsoft + Autofac)
- Ninject deprecation period (12 months)

**Addressed**:
- Ninject security risk eliminated (eventually)
- Modern DI patterns adopted (keyed services)

---

## Migration Impact

### Breaking Changes

**Ninject Users**:
- **BREAKING**: Ninject marked [Obsolete] in v2.1.0
- **BREAKING**: Removed entirely in v3.0.0

### Migration Path

**Ninject → Microsoft.Extensions.DependencyInjection**:
```csharp
// OLD (Ninject)
var kernel = new StandardKernel();
kernel.Bind<IBusClient>().To<BusClient>().InSingletonScope();

// NEW (Microsoft.Extensions.DependencyInjection)
var services = new ServiceCollection();
services.AddRawRabbit(cfg => cfg.HostName = "localhost");
var provider = services.BuildServiceProvider();
```

**Autofac 4.x → 8.x**:
```csharp
// Mostly backward compatible, minimal changes required
// Update package reference to 8.1.0
```

---

## Validation

### Acceptance Criteria

- [ ] Microsoft.Extensions.DependencyInjection 9.0.0 adapter working
- [ ] Keyed services feature functional (.NET 8+)
- [ ] Autofac 8.1.0 adapter working
- [ ] Ninject marked [Obsolete] with clear warning
- [ ] Migration guide published

### Testing Strategy

**Unit Tests**:
- DI registration tests (all containers)
- Service resolution tests
- Lifetime scope tests

**Integration Tests**:
- End-to-end with real RabbitMQ
- Multiple client scenarios (keyed services)

---

## Dependencies

### Affected Components

- RawRabbit.DependencyInjection.ServiceCollection
- RawRabbit.DependencyInjection.Autofac
- RawRabbit.DependencyInjection.Ninject (deprecated)

### Related ADRs

- **ADR-0004**: Dependency Update Strategy
- **ADR-0009**: Ninject Deprecation Strategy

### External Dependencies

- Microsoft.Extensions.DependencyInjection 9.0.0
- Autofac 8.1.0
- Ninject 3.3.6 (deprecated)

---

## Timeline

**Proposed**: 2025-10-09
**Implementation**: Phase 4 (Week 5)
**Target Completion**: 2025-11-15

---

## References

- [Microsoft.Extensions.DependencyInjection Documentation](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection)
- [Autofac Documentation](https://autofac.readthedocs.io/)
- [Dependency Matrix](../stage-1/dependency-matrix.md)

---

## Revision History

| Date | Author | Changes |
|------|--------|---------|
| 2025-10-09 | Architecture Specialist | Initial draft (Stage 2.1) |
