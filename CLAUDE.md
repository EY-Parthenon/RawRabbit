# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

RawRabbit is a modern .NET framework for communication over RabbitMQ with a modular, middleware-oriented architecture. The codebase is built on .NET Standard 1.5 and .NET Framework 4.5.1, targeting version 2.x of the library.

## Building and Testing

### Build
```powershell
# Build all projects and create NuGet packages
.build/Build.ps1
```

This script:
- Cleans the `artifacts` directory
- Restores dependencies with `dotnet restore`
- Packages all projects in `src/` with `dotnet msbuild /t:Restore;Pack`
- Outputs to `artifacts` directory

### Run Tests
```powershell
# Run all tests sequentially
.build/Test.ps1
```

This runs `dotnet test -c Release -parallel none` across all projects in the `test/` directory.

### Run Single Test
```bash
# Run tests for a specific project
dotnet test test/RawRabbit.Tests/RawRabbit.Tests.csproj
```

## Architecture

### Core Concepts

**Pipe-Based Middleware Architecture**: RawRabbit uses a middleware pipeline pattern where operations are composed of multiple middleware components that execute sequentially. Each operation (Publish, Subscribe, Request, Respond) is defined as a series of middleware stages.

- **IPipeBuilder**: Builds middleware pipelines for operations
- **IPipeContext**: Carries state through the pipeline using a dictionary-like structure (`PipeKey` constants for keys)
- **Middleware**: Individual processing components that can be chained together using `.Use<TMiddleware>()`

### Project Structure

The codebase is organized into three main directories:

- **`src/RawRabbit/`**: Core library containing the fundamental bus client, pipe infrastructure, channel management, and configuration
- **`src/RawRabbit.Operations.*/`**: Separate packages for different messaging operations (Publish, Subscribe, Request, Respond, Get, MessageSequence, StateMachine, Tools)
- **`src/RawRabbit.Enrichers.*/`**: Plugin packages that extend functionality (Polly retry policies, serialization formats like Protobuf/MessagePack, MessageContext, GlobalExecutionId, HttpContext, Attributes, QueueSuffix, RetryLater)
- **`src/RawRabbit.DependencyInjection.*/`**: Integration packages for DI containers (Autofac, Ninject, ServiceCollection)
- **`test/`**: Unit tests, integration tests, and performance tests
- **`sample/`**: Example applications

### Key Components

**BusClient**: The main entry point (`src/RawRabbit/BusClient.cs`) that accepts pipe configuration and context configuration to invoke operations.

**RawRabbitFactory**: Factory class for creating client instances (`src/RawRabbit/Instantiation/RawRabbitFactory.cs`). Use `CreateSingleton()` for a disposable client or `CreateInstanceFactory()` for managing multiple instances.

**Channel Management**: Multiple strategies available:
- `StaticChannelPool`: Fixed pool size
- `DynamicChannelPool`: Grows as needed
- `AutoScalingChannelPool`: Scales based on demand
- `ResilientChannelPool`: Handles connection failures

**Operations as Extensions**: Operations like `PublishAsync`, `SubscribeAsync`, `RequestAsync`, `RespondAsync` are implemented as extension methods on `IBusClient`. Each operation defines its own middleware pipeline (see `PublishPipeAction` and `SubscribePipe` in operation files).

**Enrichers as Plugins**: Enrichers modify the pipeline to add cross-cutting concerns. Examples:
- `UsePolly()`: Adds retry policies using Polly
- `UseProtobuf()`: Changes serialization to Protobuf
- `UseMessageContext()`: Adds message context handling
- `UseGlobalExecutionId()`: Tracks operations across distributed systems

### Configuration

Client configuration is provided through `RawRabbitOptions`:
- **ClientConfiguration**: Connection settings, timeouts, topology defaults (exchange/queue configuration)
- **Plugins**: Register enrichers via lambda (e.g., `p => p.UsePolly().UseProtobuf()`)
- **DependencyInjection**: Override internal services (e.g., custom `IChannelFactory`)

Default configuration uses `RawRabbitConfiguration.Local` (guest@localhost:5672 on vhost `/`).

### Acknowledgements

Unlike typical RabbitMQ clients, acknowledgements are returned from message handlers:
- `return new Ack()`: Basic acknowledgement
- `return new Nack(requeue: true)`: Negative acknowledgement with requeue
- `return Retry.In(TimeSpan.FromSeconds(30))`: Delayed retry using the RetryLater enricher

## Development Notes

- The solution file is `RawRabbit.sln` at the root
- Main branch for PRs: `2.0`
- Target frameworks: `netstandard1.5` and `net451`
- RabbitMQ.Client version: 5.0.1
- Uses LibLog for logging abstraction (`src/RawRabbit/Logging/LibLog.cs`)
- Pipeline stages are marked with `StageMarker` constants for debugging/extensibility
- Context operations use `IPipeContext.Properties.Add()` to store values and `ctx.Get<T>(PipeKey.X)` to retrieve them
