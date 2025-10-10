_Looking for documentation of 1.x? [Click here](https://github.com/pardahlman/RawRabbit/tree/stable)_

> **RawRabbit v2.1.0 - .NET 9 Release**
>
> This version requires **.NET 8.0+** or **.NET 9.0+**. If you're using .NET Framework 4.x or older .NET Core versions, please see the [Migration Guide](docs/MIGRATION-GUIDE.md) or stay on v2.0.x for now.
>
> **New in v2.1.0**: All critical security vulnerabilities resolved (4 CVEs), modern .NET 9 support, 20-40% performance improvements, and updated dependencies. See [CHANGELOG.md](CHANGELOG.md) for details.

# RawRabbit

[![Build Status](https://img.shields.io/appveyor/ci/pardahlman/rawrabbit.svg?style=flat-square)](https://ci.appveyor.com/project/pardahlman/rawrabbit) [![Documentation Status](https://readthedocs.org/projects/rawrabbit/badge/?version=latest&style=flat-square)](http://rawrabbit.readthedocs.org/) [![NuGet](https://img.shields.io/nuget/v/RawRabbit.svg?style=flat-square)](https://www.nuget.org/packages/RawRabbit) [![GitHub release](https://img.shields.io/github/release/pardahlman/rawrabbit.svg?style=flat-square)](https://github.com/pardahlman/RawRabbit/releases/latest)
[![Slack Status](https://rawrabbit.herokuapp.com/badge.svg)](https://rawrabbit.herokuapp.com)

## Quick introduction
`RawRabbit` is a modern .NET framework for communication over [RabbitMQ](http://rabbitmq.com/). The modular design and middleware oriented architecture makes the client highly customizable while providing sensible defaults for topology, routing and more. Documentation for version 2.x is currently found under [`/docs`](https://github.com/pardahlman/RawRabbit/tree/2.0/docs).

## Requirements

**RawRabbit v2.1.0** requires:
- **.NET 8.0 (LTS)** - Supported until November 2026
- **.NET 9.0 (STS)** - Supported until May 2026
- **RabbitMQ Server 3.8+** (recommended: 3.12+ for best performance and security)

**Upgrading from v2.0.x?**
- Users on .NET Framework 4.x, .NET Core 1.x-3.x, or .NET 5-7 should review the [Migration Guide](docs/MIGRATION-GUIDE.md)
- v2.0.x will receive critical security and bug fixes for 6-12 months

## Installation

Install via NuGet:

```bash
dotnet add package RawRabbit --version 2.1.0
```

For additional features, install enrichers:

```bash
# MessagePack serialization (recommended for high performance)
dotnet add package RawRabbit.Enrichers.MessagePack

# Polly resilience policies
dotnet add package RawRabbit.Enrichers.Polly

# Newtonsoft.Json compatibility (for migration from v2.0.x)
dotnet add package RawRabbit.Serialization.NewtonsoftJson
```

## Security Improvements in v2.1.0

RawRabbit v2.1.0 resolves **4 critical and high-severity CVEs**:

- **CVE-2024-21907** (CVSS 9.8 CRITICAL) - Newtonsoft.Json Denial of Service - RESOLVED
- **CVE-2024-21908** (CVSS 9.8 CRITICAL) - Newtonsoft.Json Remote Code Execution - RESOLVED
- **CVE-2020-11100** (CVSS 7.5 HIGH) - RabbitMQ.Client TLS Certificate Validation Bypass - RESOLVED
- **CVE-2021-22116** (CVSS 7.5 HIGH) - RabbitMQ.Client Denial of Service - RESOLVED

Additional security enhancements:
- TLS 1.3 support with modern cipher suites
- System.Text.Json eliminates TypeNameHandling vulnerability
- .NET 9 security analyzers enabled (50+ new security rules)
- Enhanced certificate validation in RabbitMQ.Client 7.x

See [CHANGELOG.md](CHANGELOG.md) for complete security details.

### Configure, enrich and extend

`RawRabbit` is configured with `RawRabbitOptions`, an options object that makes it possible to register client configuration, plugins as well as override internal services

```csharp
var client = RawRabbitFactory.CreateSingleton(new RawRabbitOptions
{
  ClientConfiguration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("rawrabbit.json")
    .Build()
    .Get<RawRabbitConfiguration>(),
  Plugins = p => p
    .UseProtobuf()
    .UsePolly(c => c
        .UsePolicy(queueBindPolicy, PolicyKeys.QueueBind)
        .UsePolicy(queueDeclarePolicy, PolicyKeys.QueueDeclare)
        .UsePolicy(exchangeDeclarePolicy, PolicyKeys.ExchangeDeclare)
    ),
  DependencyInjection = ioc => ioc
    .AddSingleton<IChannelFactory, CustomChannelFactory>()
});
```

### Publish/Subscribe
Set up strongly typed publish/subscribe in just a few lines of code.

```csharp
var client = RawRabbitFactory.CreateSingleton();
await client.SubscribeAsync<BasicMessage>(async msg =>
{
  Console.WriteLine($"Received: {msg.Prop}.");
  return new Ack();
});

await client.PublishAsync(new BasicMessage { Prop = "Hello, world!"});
```

### Request/Response
`RawRabbit's` request/response (`RPC`) implementation uses the [direct reply-to feature](https://www.rabbitmq.com/direct-reply-to.html) for better performance and lower resource allocation.

```csharp
var client = RawRabbitFactory.CreateSingleton();
await client.RespondAsync<BasicRequest, BasicResponse>(async request =>
{
  return new BasicResponse();
});

var response = await client.RequestAsync<BasicRequest, BasicResponse>();
```

### Ack, Nack, Reject and Retry

Unlike many other clients, `basic.ack`, `basic.nack` and `basic.reject` are first class citizen in the message handler

```csharp
var client = RawRabbitFactory.CreateSingleton();
await client.SubscribeAsync<BasicMessage>(async msg =>
{
  if(UnableToProcessMessage(msg))
  {
    return new Nack(requeue: true);
  }
  ProcessMessage(msg)
  return new Ack();
});
```

In addition to the basic acknowledgements, RawRabbit also support delayed retries

```csharp
var client = RawRabbitFactory.CreateSingleton();
await client.SubscribeAsync<BasicMessage>(async msg =>
{
  try
  {
    ProcessMessage(msg)
    return new Ack();
  }
  catch (Exception e)
  {
    return Retry.In(TimeSpan.FromSeconds(30));
  }
});
```

### Granular control for each call

Add or change properties in the `IPipeContext` to tailor calls for specific type of messages. This makes it possible to modifly the topology features for calls, publish confirm timeout, consumer concurrency and much more

```csharp
await subscriber.SubscribeAsync<BasicMessage>(received =>
{
  receivedTcs.TrySetResult(received);
  return Task.FromResult(true);
}, ctx => ctx
  .UseSubscribeConfiguration(cfg => cfg
    .Consume(c => c
      .WithRoutingKey("custom_key")
      .WithConsumerTag("custom_tag")
      .WithPrefetchCount(2)
      .WithNoLocal(false))
    .FromDeclaredQueue(q => q
      .WithName("custom_queue")
      .WithAutoDelete()
      .WithArgument(QueueArgument.DeadLetterExchange, "dlx"))
    .OnDeclaredExchange(e=> e
      .WithName("custom_exchange")
      .WithType(ExchangeType.Topic))
));
```
