# RawRabbit 3.0 - Modernized Fork

⚠️ **IMPORTANT**: This is a **modernized fork** of the original RawRabbit project (last updated June 2018). This version targets **.NET 8** and includes 7 years of security updates and dependency modernization.

[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![Status](https://img.shields.io/badge/status-in--development-yellow.svg)](#project-status)

---

## 🚨 Project Status: IN DEVELOPMENT (45% Complete)

**Current Phase**: Code Migration (NOT STARTED)
**Build Status**: ❌ Does not build (RabbitMQ.Client 6.x breaking changes)
**Estimated Completion**: 21-32 days remaining

**DO NOT USE IN PRODUCTION** - This version is undergoing active modernization. Use the original [RawRabbit 2.x](https://github.com/pardahlman/RawRabbit) or consider [MassTransit](https://masstransit.io/) for production use.

---

## What Changed in 3.0?

### ✅ Complete (Phase 1)
- **Framework**: Migrated from .NET Standard 1.5 / .NET Framework 4.5.1 → **.NET 8**
- **Dependencies**: Updated 29 packages (7 years of updates)
- **Security**: Fixed CVE-2018-11093 and hundreds of other vulnerabilities
- **Version**: Bumped to 3.0.0 (major breaking changes)
- **Removed**: RawRabbit.Enrichers.ZeroFormatter (abandoned dependency)

### ⚠️ In Progress (Phase 2) - NOT COMPLETE
- **RabbitMQ.Client**: Dependency updated to 6.8.1, **but code NOT migrated**
- **Polly**: Dependency updated to 8.4.2, **but code NOT migrated**
- **Build**: Solution does NOT compile until code migration complete

### ⏳ Remaining Work
- Code migration: ~68 files (21-32 days estimated)
- Testing: 156+ tests (4-6 days)
- Release preparation: 2-3 days

**See [MODERNIZATION-STATUS.md](docs/MODERNIZATION-STATUS.md) for detailed status.**

---

## Quick Introduction

`RawRabbit` is a modern .NET framework for communication over [RabbitMQ](http://rabbitmq.com/). The modular design and middleware oriented architecture makes the client highly customizable while providing sensible defaults for topology, routing, and more.

### Key Features

- 🔄 **Pipe-Based Middleware Architecture** - Similar to ASP.NET Core middleware
- 🔌 **Plugin System** - Enrichers for serialization, retry policies, message context
- 📦 **Multiple Operations** - Publish/Subscribe, Request/Response, Message Sequences, State Machines
- 🎯 **Strongly Typed** - Full generic type support for messages
- ⚙️ **Channel Pooling** - Multiple strategies (Static, Dynamic, AutoScaling, Resilient)
- 🔧 **DI Integration** - Autofac, Ninject, Microsoft.Extensions.DependencyInjection

---

## Installation (AFTER 3.0 Release)

⚠️ **NOT YET AVAILABLE** - RawRabbit 3.0 is not published to NuGet.

When released, install via NuGet:

```bash
# Core library
dotnet add package RawRabbit --version 3.0.0

# Operations (choose what you need)
dotnet add package RawRabbit.Operations.Publish --version 3.0.0
dotnet add package RawRabbit.Operations.Subscribe --version 3.0.0
dotnet add package RawRabbit.Operations.Request --version 3.0.0
dotnet add package RawRabbit.Operations.Respond --version 3.0.0

# Enrichers (optional)
dotnet add package RawRabbit.Enrichers.Polly --version 3.0.0
dotnet add package RawRabbit.Enrichers.MessagePack --version 3.0.0
dotnet add package RawRabbit.Enrichers.Protobuf --version 3.0.0

# DI Integration (choose one)
dotnet add package RawRabbit.DependencyInjection.ServiceCollection --version 3.0.0
```

---

## Prerequisites

- **.NET 8.0 SDK** or later
- **RabbitMQ 3.x** server
- **Visual Studio 2022** / **Rider** / **VS Code** with C# support

---

## Basic Usage (3.0 - After Code Migration)

### Configure and Create Client

```csharp
using RawRabbit;
using RawRabbit.Instantiation;

var client = RawRabbitFactory.CreateSingleton(new RawRabbitOptions
{
    ClientConfiguration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("rawrabbit.json")
        .Build()
        .Get<RawRabbitConfiguration>(),

    // Optional: Add enrichers
    Plugins = p => p
        .UseMessagePack()  // Serialization
        .UsePolly(ctx =>   // Retry policies (Polly 8.x API)
        {
            var pipeline = new ResiliencePipelineBuilder()
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    ShouldHandle = new PredicateBuilder()
                        .Handle<BrokerUnreachableException>()
                })
                .Build();
            ctx.Properties.Add(PolicyKeys.BasicPublish, pipeline);
        }),

    // Optional: Custom services
    DependencyInjection = ioc => ioc
        .AddSingleton<IChannelFactory, CustomChannelFactory>()
});
```

### Publish/Subscribe

```csharp
// Define message
public class BasicMessage
{
    public string Prop { get; set; }
}

// Subscribe
await client.SubscribeAsync<BasicMessage>(async msg =>
{
    Console.WriteLine($"Received: {msg.Prop}");
});

// Publish
await client.PublishAsync(new BasicMessage
{
    Prop = "Hello, RawRabbit 3.0!"
});
```

### Request/Response (RPC)

```csharp
// Define request/response
public class Request { public int Value { get; set; } }
public class Response { public int Result { get; set; } }

// Respond (server)
await client.RespondAsync<Request, Response>(async request =>
{
    return new Response { Result = request.Value * 2 };
});

// Request (client)
var response = await client.RequestAsync<Request, Response>(
    new Request { Value = 21 }
);
Console.WriteLine($"Result: {response.Result}"); // 42
```

---

## Breaking Changes in 3.0

### ⚠️ CRITICAL: Read Before Upgrading

**If you're upgrading from RawRabbit 2.x, you MUST read**:
1. **[CHANGELOG.md](CHANGELOG.md)** - Complete list of breaking changes
2. **[MIGRATION-GUIDE.md](MIGRATION-GUIDE.md)** - 1,800+ line step-by-step guide

### Major Breaking Changes

#### 1. Framework Requirements
- ❌ **Dropped**: .NET Framework 4.5.1 support
- ❌ **Dropped**: .NET Standard 1.5/1.6 support
- ✅ **Required**: .NET 8.0 or later

#### 2. Removed Packages
- ❌ **RawRabbit.Enrichers.ZeroFormatter** - Removed (abandoned dependency)
- **Migration Path**: Use `MessagePack` or `Protobuf` enrichers instead

#### 3. Dependency Updates (Breaking)
- **RabbitMQ.Client**: 5.0.1 → 6.8.1 (major API changes)
- **Polly**: 5.3.1 → 8.4.2 (complete API redesign)
- **Autofac**: 4.1.0 → 8.1.0
- **All others**: See CHANGELOG.md

#### 4. Polly Integration (Breaking)

**BEFORE (2.x - Polly 5.x)**:
```csharp
.UsePolly(c => c
    .UsePolicy(Policy
        .Handle<BrokerUnreachableException>()
        .RetryAsync(3), PolicyKeys.BasicPublish)
)
```

**AFTER (3.0 - Polly 8.x)**:
```csharp
.UsePolly(ctx =>
{
    var pipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            ShouldHandle = new PredicateBuilder()
                .Handle<BrokerUnreachableException>()
        })
        .Build();
    ctx.Properties.Add(PolicyKeys.BasicPublish, pipeline);
})
```

---

## Documentation

### User Documentation
- **[CHANGELOG.md](CHANGELOG.md)** - What changed in 3.0
- **[MIGRATION-GUIDE.md](MIGRATION-GUIDE.md)** - Step-by-step upgrade guide (1,800+ lines)

### Developer Documentation
- **[MODERNIZATION-STATUS.md](docs/MODERNIZATION-STATUS.md)** - Current project status
- **[RABBITMQ-CLIENT-6-MIGRATION.md](docs/RABBITMQ-CLIENT-6-MIGRATION.md)** - Implementation guide
- **[POLLY-8-MIGRATION.md](docs/POLLY-8-MIGRATION.md)** - Polly migration guide

### Architecture Documentation
- **[docs/adr/](docs/adr/)** - Architecture Decision Records (5 ADRs)
  - ADR-001: Target Framework Selection
  - ADR-002: RabbitMQ.Client Migration Strategy
  - ADR-003: ZeroFormatter Enricher Removal
  - ADR-004: Dependency Update Strategy
  - ADR-005: Versioning Strategy

### Project Management
- **[ASSESSMENT.md](ASSESSMENT.md)** - Initial project assessment (62/100 score)
- **[PLAN.md](PLAN.md)** - Modernization plan
- **[HISTORY.md](HISTORY.md)** - Complete audit trail

---

## Building from Source

⚠️ **IMPORTANT**: The solution currently **DOES NOT BUILD** due to incomplete code migration.

### Prerequisites
```bash
# Install .NET 8 SDK
# Windows: Download from https://dotnet.microsoft.com/download/dotnet/8.0
# macOS: brew install dotnet-sdk
# Linux: See https://learn.microsoft.com/en-us/dotnet/core/install/linux

# Verify installation
dotnet --version  # Should show 8.0.x
```

### Build (When Code Migration Complete)
```bash
# Clone repository
git clone https://github.com/yourusername/RawRabbit.git
cd RawRabbit

# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test
```

### Current Build Status
```bash
# Attempting to build will result in compilation errors:
# - RabbitMQ.Client 6.x API incompatibilities (~60 files)
# - Polly 8.x API incompatibilities (~8 files)

# Estimated effort to fix: 21-32 days
```

---

## Testing

### Unit Tests
```bash
# Run all unit tests (after code migration)
dotnet test

# Run specific test project
dotnet test test/RawRabbit.Tests/RawRabbit.Tests.csproj
```

### Integration Tests

**Setup RabbitMQ with Docker**:
```bash
# Start RabbitMQ
docker run -d --name rabbitmq \
  -p 5672:5672 \
  -p 15672:15672 \
  rabbitmq:3-management

# Access management UI: http://localhost:15672
# Default credentials: guest/guest
```

**Run Integration Tests**:
```bash
dotnet test test/RawRabbit.IntegrationTests/RawRabbit.IntegrationTests.csproj
```

---

## Contributing

This is a **fork for modernization purposes**. The original RawRabbit project is at:
https://github.com/pardahlman/RawRabbit

### How to Contribute to This Fork

1. **Code Migration** (NEEDED):
   - Follow [RABBITMQ-CLIENT-6-MIGRATION.md](docs/RABBITMQ-CLIENT-6-MIGRATION.md)
   - Update RabbitMQ.Client 5.x → 6.x code (~60 files)
   - Update Polly 5.x → 8.x code (~8 files)

2. **Testing**:
   - Run full test suite
   - Add integration tests
   - Performance benchmarking

3. **Documentation**:
   - Fix typos or improve clarity
   - Add code examples
   - Update guides

### Contribution Guidelines
- Follow existing code style
- Add tests for new features
- Update documentation
- Create pull requests with clear descriptions

---

## Alternatives

### Consider MassTransit Instead

If you're starting a new project or can migrate, consider **[MassTransit](https://masstransit.io/)**:

| Feature | RawRabbit 3.0 | MassTransit |
|---------|---------------|-------------|
| **Status** | In development (45% complete) | ✅ Production ready |
| **Maintenance** | You own it (forked) | ✅ Community maintained |
| **.NET 8 Support** | ✅ (when complete) | ✅ Yes |
| **RabbitMQ Support** | ✅ Yes | ✅ Yes |
| **Other Brokers** | ❌ No | ✅ Azure SB, AWS SQS, etc. |
| **Documentation** | ⚠️ Frozen at 2018 | ✅ Excellent, current |
| **Community** | ❌ None (abandoned) | ✅ Large, active |
| **Migration Effort** | 21-32 days (code) + ongoing maintenance | 10-20 days (one-time) |

**Recommendation**: Use MassTransit for new projects. Only modernize RawRabbit if you're already heavily invested in it.

---

## License

Same license as original RawRabbit project (MIT License assumed).

---

## Acknowledgments

- **Original Author**: [pardahlman](https://github.com/pardahlman) - Excellent middleware architecture
- **Contributors**: All RawRabbit 1.x and 2.x contributors
- **Modernization**: Claude Code + Human Team (2025)

---

## Support

### For RawRabbit 3.0 (This Fork)
- Read the documentation (especially MIGRATION-GUIDE.md)
- Check [MODERNIZATION-STATUS.md](docs/MODERNIZATION-STATUS.md) for current status
- Review ADRs in [docs/adr/](docs/adr/) for architectural decisions

### For Original RawRabbit 2.x
- Original repository: https://github.com/pardahlman/RawRabbit
- Documentation: https://rawrabbit.readthedocs.org/
- **Note**: Project abandoned since June 2018

### For Production Use
- Consider [MassTransit](https://masstransit.io/) (actively maintained)
- Consider [NServiceBus](https://particular.net/nservicebus) (commercial support)
- Consider [RabbitMQ.Client](https://www.rabbitmq.com/dotnet-api-guide.html) directly

---

## Roadmap

### Current Phase: Code Migration (0% Complete)
- ⚠️ RabbitMQ.Client 6.x migration (~60 files, 12-18 days)
- ⚠️ Polly 8.x migration (~8 files, 3-5 days)
- ⚠️ Testing and validation (4-6 days)
- ⚠️ Release preparation (2-3 days)

### Future (If Project Continues)
- Nullable reference type annotations
- ValueTask optimizations
- Span<T> / Memory<T> optimizations
- Modern C# 12+ features
- Potential .NET 9 support

### Version 4.0 (Speculative)
- Consider System.Text.Json migration
- RabbitMQ.Client 7.x
- Drop .NET 8, target .NET 9+ only

---

## FAQ

### Q: Is this production-ready?
**A**: NO. This is 45% complete. Use original RawRabbit 2.x or MassTransit instead.

### Q: When will 3.0 be released?
**A**: Unknown. Depends on resource allocation (21-32 days of development remaining).

### Q: Can I use this now?
**A**: NO. The solution does not build. Code migration is incomplete.

### Q: Should I modernize RawRabbit or migrate to MassTransit?
**A**: **MassTransit is recommended** unless you're heavily invested in RawRabbit's architecture. See [ASSESSMENT.md](ASSESSMENT.md) for detailed analysis (5x cheaper over 5 years).

### Q: What's the biggest challenge?
**A**: RabbitMQ.Client 5.0.1 → 6.8.1 migration (~60 files, 12-18 days). See [RABBITMQ-CLIENT-6-MIGRATION.md](docs/RABBITMQ-CLIENT-6-MIGRATION.md).

### Q: Why was this forked?
**A**: Original project abandoned in June 2018. This fork modernizes dependencies and addresses 7 years of security vulnerabilities.

### Q: Can I contribute?
**A**: Yes! See [Contributing](#contributing) section. Code migration help is most needed.

---

**Last Updated**: 2025-11-09
**Project Status**: IN DEVELOPMENT (45% Complete)
**Next Review**: After Phase 2 (Code Migration) begins

---

_For detailed status, see [MODERNIZATION-STATUS.md](docs/MODERNIZATION-STATUS.md)_
