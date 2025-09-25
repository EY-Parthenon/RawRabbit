# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

RawRabbit is a modern .NET framework for communication over RabbitMQ. It features a modular design with middleware-oriented architecture for high customization.

## Build Commands

### Building the Project
```powershell
# Windows - Build all projects
powershell .build/Build.ps1

# Alternative using dotnet CLI
dotnet restore
dotnet build -c Release
```

### Running Tests
```powershell
# Windows - Run all tests
powershell .build/Test.ps1

# Alternative using dotnet CLI - Run all tests
dotnet test -c Release --parallel none

# Run specific test project
dotnet test test/RawRabbit.Tests -c Release
dotnet test test/RawRabbit.IntegrationTests -c Release
```

### Creating NuGet Packages
```powershell
# Build and pack with version suffix
dotnet msbuild "/t:Restore;Pack" /p:VersionSuffix=rc2 /p:Configuration=Release
```

## Architecture Overview

### Core Components

**Middleware Pipeline Architecture**
- The framework uses a middleware pipeline pattern (`IPipeBuilder`, `Middleware`) for processing messages
- Middleware components are composed using the `PipeBuilder` class
- Each operation (publish, subscribe, request/response) goes through a customizable pipeline

**Key Abstractions**
- `IBusClient` - Main client interface for all RabbitMQ operations
- `IPipeContext` - Context object that flows through the middleware pipeline
- `IChannelFactory` / Channel Pools - Manages RabbitMQ channel lifecycle and pooling

### Project Structure

**Core Library** (`src/RawRabbit/`)
- `Pipe/` - Middleware pipeline infrastructure
- `Configuration/` - Configuration builders for various operations
- `Channel/` - Channel management and pooling
- `Consumer/` - Consumer creation and management
- `Instantiation/` - Client factory and dependency injection

**Enrichers** (`src/RawRabbit.Enrichers.*`)
- Additional functionality as plugins (Polly, Protobuf, MessageContext, etc.)
- Can be registered via `RawRabbitOptions.Plugins`

**Operations** (`src/RawRabbit.Operations.*`)
- Higher-level operations built on core functionality
- Subscribe, Publish, Request/Response patterns

**DI Integrations** (`src/RawRabbit.DependencyInjection.*`)
- Integrations with popular DI containers (Autofac, Ninject, ServiceCollection)

### Configuration System

The framework uses a builder pattern for configuration:
- `ConsumeConfiguration` - Consumer settings
- `PublishConfiguration` - Publisher settings  
- `ExchangeDeclaration` - Exchange topology
- `QueueDeclaration` - Queue topology

Configurations can be customized per-call using the context parameter:
```csharp
await client.PublishAsync(message, ctx => ctx
    .UsePublishConfiguration(cfg => cfg
        .OnExchange("custom_exchange")));
```

### Message Acknowledgment

RawRabbit provides first-class support for acknowledgments:
- `Ack` - Acknowledge successful processing
- `Nack` - Negative acknowledgment with requeue option
- `Reject` - Reject message
- `Retry` - Delayed retry mechanism

## Key Development Patterns

1. **Extending via Middleware**: New functionality should be added as middleware components inheriting from `Middleware`

2. **Configuration Builders**: Use the builder pattern for configuration objects

3. **Context Extensions**: Add extension methods to `IPipeContext` for clean API design

4. **Channel Management**: Always use channel pools rather than creating channels directly

5. **Testing**: Integration tests require a running RabbitMQ instance (scripts in `.build/` for setup)