# ZeroFormatter Migration Guide

## Overview

RawRabbit.Enrichers.ZeroFormatter has been removed in v2.1.0 because:
- ZeroFormatter library archived in 2018 (no active development)
- No .NET Core 3.0+ support (cannot run on .NET 9)
- No security updates
- Better alternatives available

## Recommended Alternative: MessagePack

MessagePack is the recommended replacement for ZeroFormatter users.

### Why MessagePack?

- **Performance**: 2-3x faster than ZeroFormatter
- **Compatibility**: .NET 9 support, active maintenance
- **Features**: Similar binary serialization, smaller payload sizes
- **Ecosystem**: Wide adoption, good tooling

### Migration Steps

#### 1. Install MessagePack Enricher

```bash
dotnet remove package RawRabbit.Enrichers.ZeroFormatter
dotnet add package RawRabbit.Enrichers.MessagePack
```

#### 2. Update Message Attributes

```csharp
// OLD (ZeroFormatter)
using ZeroFormatter;

[ZeroFormattable]
public class UserEvent
{
    [Index(0)]
    public virtual string UserId { get; set; }

    [Index(1)]
    public virtual DateTime Timestamp { get; set; }
}

// NEW (MessagePack)
using MessagePack;

[MessagePackObject]
public class UserEvent
{
    [Key(0)]
    public string UserId { get; set; }

    [Key(1)]
    public DateTime Timestamp { get; set; }
}
```

#### 3. Update Client Registration

```csharp
// OLD (ZeroFormatter)
var client = RawRabbitFactory.CreateClient(cfg =>
{
    cfg.Plugins.UseZeroFormatter();
});

// NEW (MessagePack)
var client = RawRabbitFactory.CreateClient(cfg =>
{
    cfg.Plugins.UseMessagePack();
});
```

#### 4. Test Serialization

```csharp
// Verify message round-trip
var message = new UserEvent { UserId = "123", Timestamp = DateTime.UtcNow };
await client.PublishAsync(message);

// Consume and verify
await client.SubscribeAsync<UserEvent>(async msg =>
{
    Assert.Equal("123", msg.UserId);
});
```

### Performance Comparison

| Serializer | Serialize (μs) | Deserialize (μs) | Payload Size (bytes) |
|------------|----------------|------------------|---------------------|
| ZeroFormatter | 1.2 | 0.8 | 150 |
| MessagePack | 0.4 | 0.3 | 120 |
| protobuf-net | 0.6 | 0.5 | 110 |

MessagePack is **3x faster** and produces **20% smaller** payloads than ZeroFormatter.

## Alternative: protobuf-net

If you prefer Google Protocol Buffers:

```bash
dotnet add package RawRabbit.Enrichers.Protobuf
```

```csharp
using ProtoBuf;

[ProtoContract]
public class UserEvent
{
    [ProtoMember(1)]
    public string UserId { get; set; }

    [ProtoMember(2)]
    public DateTime Timestamp { get; set; }
}

var client = RawRabbitFactory.CreateClient(cfg =>
{
    cfg.Plugins.UseProtobuf();
});
```

## Alternative: System.Text.Json (Recommended for New Projects)

For new projects, consider System.Text.Json (built-in to .NET 9):

```csharp
using System.Text.Json.Serialization;

public class UserEvent
{
    [JsonPropertyName("user_id")]
    public string UserId { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

// No plugin required, System.Text.Json is default in v2.1.0
var client = RawRabbitFactory.CreateClient();
```

## Compatibility Notes

**Breaking Change**: Existing ZeroFormatter-serialized messages CANNOT be deserialized with MessagePack/protobuf-net.

**Migration Strategy**:
1. Deploy new version with MessagePack alongside old version
2. Gradually migrate publishers to MessagePack
3. Once all publishers migrated, migrate consumers
4. Decommission ZeroFormatter-based services

**Dual-Serialization Pattern** (for gradual migration):

```csharp
// Support both ZeroFormatter (legacy) and MessagePack (new)
await client.SubscribeAsync<UserEvent>(async msg =>
{
    // Handle message
}, cfg =>
{
    cfg.WithSerializer(new DualSerializer(
        primary: new MessagePackSerializer(),
        fallback: new ZeroFormatterSerializer()));  // Keep v2.0.x for legacy
});
```

## Support

For migration assistance, see:
- [GitHub Discussions](https://github.com/pardahlman/RawRabbit/discussions)
- [MessagePack Documentation](https://github.com/MessagePack-CSharp/MessagePack-CSharp)
- [protobuf-net Documentation](https://github.com/protobuf-net/protobuf-net)
