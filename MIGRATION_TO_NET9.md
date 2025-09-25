# RawRabbit Migration Guide to .NET 9

## Overview
RawRabbit has been upgraded to support .NET 9.0 while maintaining backward compatibility with .NET Standard 2.0. This guide will help you migrate your existing RawRabbit applications to .NET 9.

## Prerequisites
- .NET 9 SDK (9.0.100 or later)
- Visual Studio 2022 17.12+ or VS Code with C# extension
- Existing RawRabbit 2.x application

## Migration Steps

### 1. Update Your Project Files

Update your `.csproj` files to target .NET 9:

```xml
<PropertyGroup>
  <TargetFramework>net9.0</TargetFramework>
</PropertyGroup>
```

For libraries that need backward compatibility:
```xml
<PropertyGroup>
  <TargetFrameworks>net9.0;netstandard2.0</TargetFrameworks>
</PropertyGroup>
```

### 2. Update NuGet Packages

Update all RawRabbit packages to the latest version that supports .NET 9:

```bash
dotnet add package RawRabbit
dotnet add package RawRabbit.Operations.Subscribe
dotnet add package RawRabbit.Operations.Publish
# Add other RawRabbit packages as needed
```

### 3. Package Compatibility Notes

#### RabbitMQ.Client
- RawRabbit currently uses RabbitMQ.Client 5.2.0 for stability
- If you're directly using RabbitMQ.Client, ensure version compatibility
- Future versions may upgrade to RabbitMQ.Client 6.x or 7.x

#### Polly
- If using RawRabbit.Enrichers.Polly, it uses Polly 7.2.4
- The async API has been updated to use `Context` parameters
- Example migration:
  ```csharp
  // Old (Polly 6.x)
  policy.ExecuteAsync(async () => await DoSomething());
  
  // New (Polly 7.x)
  policy.ExecuteAsync(async (ctx) => await DoSomething(), new Context());
  ```

### 4. ASP.NET Core Applications

If using RawRabbit with ASP.NET Core:

#### Update Logging Configuration
```csharp
// Old (.NET Core 2.x)
public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
{
    loggerFactory.AddConsole(Configuration.GetSection("Logging"));
}

// New (.NET 9)
// Configure logging in Program.cs or ConfigureServices
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
});
```

#### Update Hosting Environment
```csharp
// Old
IHostingEnvironment env

// New
IWebHostEnvironment env
```

#### HttpContext Enricher
The HttpContext enricher now supports .NET 9:
```csharp
using RawRabbit.Enrichers.HttpContext;

// Works with .NET 9
var httpContext = pipeContext.GetHttpContext();
```

### 5. Conditional Compilation

If you have code that uses conditional compilation:

```csharp
#if NET9_0
    // .NET 9 specific code
    private static readonly AsyncLocal<string> ContextData = new AsyncLocal<string>();
#elif NETSTANDARD2_0
    // .NET Standard 2.0 code
#endif
```

### 6. Testing Your Migration

After migration, run your test suite:

```bash
# Clean and rebuild
dotnet clean
dotnet build -c Release

# Run tests
dotnet test -c Release

# Run your application
dotnet run -c Release
```

## Breaking Changes

### Minimal Breaking Changes
- Assembly.CodeBase replaced with Assembly.Location (internal change)
- AsyncLocal<T> used instead of CallContext for async context storage
- Some obsolete ASP.NET Core APIs need updating in sample applications

### No Breaking Changes in Core APIs
- All publish/subscribe operations work identically
- Request/response patterns unchanged
- Message acknowledgment (Ack/Nack/Retry) unchanged
- Configuration and middleware pipeline unchanged

## Performance Improvements

.NET 9 brings several performance improvements:
- Better async/await performance
- Improved garbage collection
- Faster JSON serialization
- Better memory management

## Troubleshooting

### Issue: Missing GetHttpContext extension method
**Solution**: Ensure you have the latest RawRabbit.Enrichers.HttpContext package and proper using statement:
```csharp
using RawRabbit.Enrichers.HttpContext;
```

### Issue: Polly ExecuteAsync compilation errors
**Solution**: Update your Polly usage to include Context parameter:
```csharp
await policy.ExecuteAsync(
    action: async (ctx) => await YourMethod(),
    context: new Context()
);
```

### Issue: Test failures after migration
**Solution**: Ensure all mock setups match the actual method signatures, particularly for IConnectionFactory.CreateConnection which now requires two parameters.

## Support

For issues or questions about the .NET 9 migration:
1. Check the [DOTNET9_UPGRADE_REPORT.md](./DOTNET9_UPGRADE_REPORT.md) for technical details
2. Review the [README.md](./README.md) for updated documentation
3. Open an issue on GitHub if you encounter problems

## Next Steps

After successful migration:
1. Update your CI/CD pipelines to use .NET 9 SDK
2. Consider leveraging new .NET 9 features in your application
3. Monitor application performance improvements
4. Plan for future updates (RabbitMQ.Client 6.x/7.x, Polly 8.x)