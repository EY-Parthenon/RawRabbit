# Polly 8.x Migration Guide for RawRabbit.Enrichers.Polly

## Overview

RawRabbit.Enrichers.Polly has been upgraded from Polly 7.2.4 to Polly 8.6.4 as part of the .NET 9 migration. This guide explains the breaking changes and how to update your code.

## Breaking Changes

### 1. Package Reference Change

**Old (Polly 7.x):**
```xml
<PackageReference Include="Polly" Version="7.2.4" />
```

**New (Polly 8.x):**
```xml
<PackageReference Include="Polly.Core" Version="8.6.4" />
```

### 2. Policy Type Changes

Polly 8.x introduces `ResiliencePipeline` as a replacement for the `IAsyncPolicy` interface.

**Old API (Polly 7.x):**
```csharp
using Polly;

IAsyncPolicy policy = Policy
    .Handle<BrokerUnreachableException>()
    .WaitAndRetryAsync(3, retryAttempt =>
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

context.UsePolicy(policy, PolicyKeys.Connect);
```

**New API (Polly 8.x):**
```csharp
using Polly;
using Polly.Retry;

ResiliencePipeline pipeline = new ResiliencePipelineBuilder()
    .AddRetry(new RetryStrategyOptions
    {
        ShouldHandle = new PredicateBuilder()
            .Handle<BrokerUnreachableException>(),
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromSeconds(1),
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true
    })
    .Build();

context.UsePolicy(pipeline, PolicyKeys.Connect);
```

### 3. ConnectionPolicies Type Changes

**Old (Polly 7.x):**
```csharp
var policies = new ConnectionPolicies
{
    Connect = Policy.Handle<BrokerUnreachableException>()
        .WaitAndRetryAsync(retryCount: 5, sleepDurationProvider: _ => TimeSpan.FromSeconds(1)),
    CreateChannel = Policy.Handle<TimeoutException>()
        .WaitAndRetryAsync(retryCount: 3, sleepDurationProvider: _ => TimeSpan.FromMilliseconds(500)),
    GetConnection = Policy.NoOpAsync()
};
```

**New (Polly 8.x):**
```csharp
var policies = new ConnectionPolicies
{
    Connect = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<BrokerUnreachableException>(),
            MaxRetryAttempts = 5,
            Delay = TimeSpan.FromSeconds(1)
        })
        .Build(),
    CreateChannel = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<TimeoutException>(),
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(500)
        })
        .Build(),
    GetConnection = ResiliencePipeline.Empty  // Replaces Policy.NoOpAsync()
};
```

### 4. Context Data Removed

Polly 8.x no longer supports passing context data via `contextData` parameter. If you were using this feature for logging or diagnostics, you'll need to capture context through closures instead.

**Old (Polly 7.x):**
```csharp
await policy.ExecuteAsync(
    action: ct => DoWorkAsync(ct),
    contextData: new Dictionary<string, object>
    {
        ["RequestId"] = requestId,
        ["UserId"] = userId
    },
    cancellationToken: token
);
```

**New (Polly 8.x):**
```csharp
// Capture context in closure
var requestIdCopy = requestId;
var userIdCopy = userId;

await pipeline.ExecuteAsync(
    async ct =>
    {
        // Use captured variables for logging/diagnostics
        logger.LogInformation("Executing for RequestId: {RequestId}, UserId: {UserId}",
            requestIdCopy, userIdCopy);
        return await DoWorkAsync(ct);
    },
    token
);
```

## Migration Steps

### Step 1: Update Package References

In your `.csproj` file, replace Polly 7.x with Polly.Core 8.x:

```xml
<ItemGroup>
  <PackageReference Include="Polly.Core" Version="8.6.4" />
</ItemGroup>
```

### Step 2: Add Required Using Statements

Add the new Polly namespaces:

```csharp
using Polly;
using Polly.Retry;  // For retry strategies
```

### Step 3: Convert Policies to ResiliencePipelines

Replace all `Policy` declarations with `ResiliencePipelineBuilder`:

```csharp
// Before
var retryPolicy = Policy
    .Handle<Exception>()
    .WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(1));

// After
var retryPipeline = new ResiliencePipelineBuilder()
    .AddRetry(new RetryStrategyOptions
    {
        ShouldHandle = new PredicateBuilder().Handle<Exception>(),
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromSeconds(1)
    })
    .Build();
```

### Step 4: Update Method Signatures

Change all `IAsyncPolicy` references to `ResiliencePipeline`:

```csharp
// Before
public void ConfigurePolly(IAsyncPolicy policy)
{
    context.UsePolicy(policy);
}

// After
public void ConfigurePolly(ResiliencePipeline pipeline)
{
    context.UsePolicy(pipeline);
}
```

### Step 5: Replace No-Op Policies

Replace `Policy.NoOpAsync()` with `ResiliencePipeline.Empty`:

```csharp
// Before
var noOp = Policy.NoOpAsync();

// After
var noOp = ResiliencePipeline.Empty;
```

## Common Retry Patterns

### Exponential Backoff with Jitter

```csharp
var pipeline = new ResiliencePipelineBuilder()
    .AddRetry(new RetryStrategyOptions
    {
        ShouldHandle = new PredicateBuilder().Handle<Exception>(),
        MaxRetryAttempts = 5,
        Delay = TimeSpan.FromSeconds(1),
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true  // Adds randomness to prevent thundering herd
    })
    .Build();
```

### Fixed Delay Retry

```csharp
var pipeline = new ResiliencePipelineBuilder()
    .AddRetry(new RetryStrategyOptions
    {
        ShouldHandle = new PredicateBuilder().Handle<TimeoutException>(),
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromMilliseconds(500),
        BackoffType = DelayBackoffType.Constant
    })
    .Build();
```

### Retry with OnRetry Callback

```csharp
var pipeline = new ResiliencePipelineBuilder()
    .AddRetry(new RetryStrategyOptions
    {
        ShouldHandle = new PredicateBuilder().Handle<Exception>(),
        MaxRetryAttempts = 3,
        OnRetry = args =>
        {
            logger.LogWarning("Retry {AttemptNumber} after {Delay}",
                args.AttemptNumber, args.RetryDelay);
            return ValueTask.CompletedTask;
        }
    })
    .Build();
```

### Combining Multiple Exception Types

```csharp
var pipeline = new ResiliencePipelineBuilder()
    .AddRetry(new RetryStrategyOptions
    {
        ShouldHandle = new PredicateBuilder()
            .Handle<BrokerUnreachableException>()
            .Handle<TimeoutException>()
            .Handle<IOException>(),
        MaxRetryAttempts = 5,
        Delay = TimeSpan.FromSeconds(1),
        BackoffType = DelayBackoffType.Exponential
    })
    .Build();
```

## Testing with Polly 8.x

Update your unit tests to use the new API:

```csharp
[Fact]
public async Task Should_Retry_On_Failure()
{
    var callCount = 0;
    var pipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<Exception>(),
            MaxRetryAttempts = 3,
            Delay = TimeSpan.Zero  // No delay in tests
        })
        .Build();

    await pipeline.ExecuteAsync(async ct =>
    {
        callCount++;
        if (callCount < 3)
            throw new Exception("Transient failure");
        return await Task.FromResult(true);
    });

    Assert.Equal(3, callCount);
}
```

## Additional Resources

- [Polly 8.0 Official Migration Guide](https://www.pollydocs.org/migration/v8)
- [Polly 8.0 Documentation](https://www.pollydocs.org/)
- [ResiliencePipeline API Reference](https://www.pollydocs.org/pipelines/resilience-pipeline)

## Support

If you encounter issues during migration, please:

1. Check this migration guide
2. Review the [Polly 8.0 breaking changes documentation](https://github.com/App-vNext/Polly/blob/main/CHANGELOG.md)
3. Open an issue on the [RawRabbit GitHub repository](https://github.com/pardahlman/RawRabbit/issues)

## Summary

The migration from Polly 7.x to 8.x brings:

- **Better Performance**: ResiliencePipeline is more efficient than the old Policy-based API
- **Simplified API**: More intuitive builder pattern
- **Better Async Support**: Native async/await throughout
- **Modern .NET Support**: Full compatibility with .NET 9 and future versions

While the API changes require code updates, the new Polly 8.x API is cleaner, more performant, and better aligned with modern .NET practices.
