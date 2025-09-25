# RawRabbit .NET 9 Upgrade Plan

## Executive Summary
This document outlines a comprehensive plan to upgrade RawRabbit from its current legacy framework targets (netstandard1.5/net451) to modern .NET 9 targets. The upgrade will modernize the codebase, improve performance, and ensure long-term support.

## Current State Analysis

### Framework Versions
- **Core Libraries**: Multi-targeting `netstandard1.5;net451` (24 projects)
- **Specialized Libraries**: `netstandard1.6/2.0` with `net451` (3 projects)
- **Test Projects**: `net46` and `netcoreapp1.1`
- **Sample Applications**: `netcoreapp1.0` and `netcoreapp2.0`

### Key Dependencies
- RabbitMQ.Client: v5.0.1 (needs major upgrade to 6.8+)
- Newtonsoft.Json: v10.0.1
- Microsoft.Extensions.*: v1.0.2-2.0.0 (inconsistent versions)
- Various third-party packages (Autofac, Polly, Ninject) on older versions

## Upgrade Strategy

### Phase 1: Preparation (Week 1)
1. **Create Feature Branch**
   - Branch name: `feature/dotnet9-upgrade`
   - Ensure all current tests pass before starting

2. **Backup and Documentation**
   - Document current API surface
   - Create compatibility matrix
   - Set up CI/CD for both old and new branches

3. **Dependency Analysis**
   - Audit all NuGet packages for .NET 9 compatibility
   - Identify deprecated packages (e.g., ZeroFormatter)
   - Plan replacement strategies

### Phase 2: Core Framework Upgrade (Week 2-3)

#### Target Framework Strategy
```xml
<!-- Core Libraries -->
<TargetFrameworks>net9.0;net8.0;netstandard2.0</TargetFrameworks>

<!-- Test Projects -->
<TargetFramework>net9.0</TargetFramework>

<!-- Sample Applications -->
<TargetFramework>net9.0</TargetFramework>
```

#### Reasoning for Multi-targeting
- **net9.0**: Latest features and performance improvements
- **net8.0**: LTS support for enterprise customers
- **netstandard2.0**: Broad compatibility for legacy consumers

### Phase 3: Package Updates (Week 3-4)

#### Critical Updates Required
1. **RabbitMQ.Client**: 5.0.1 → 6.8.1+
   - Breaking changes in connection/channel management
   - New async API patterns
   - Update all usages of deprecated APIs

2. **Microsoft.Extensions.*** → 9.0.0
   - Configuration
   - DependencyInjection
   - Logging
   - Options

3. **Test Frameworks**
   - xunit: → 2.9.0+
   - Microsoft.NET.Test.Sdk: 15.0.0-preview → 17.11.0+
   - FluentAssertions: → 6.12.0+

4. **Third-party Libraries**
   - Autofac: 4.1.0 → 8.0.0+
   - Polly: 5.3.1 → 8.0.0+
   - Ninject: 3.3.4 → 3.3.6
   - protobuf-net: 2.1.0 → 3.2.0+
   - MessagePack: 1.7.3.4 → 2.5.0+

5. **Deprecated Package Replacements**
   - ZeroFormatter → MessagePack or System.Text.Json
   - Consider removing or replacing legacy serializers

### Phase 4: Code Migration (Week 4-5)

#### Breaking Changes to Address

1. **Async/Await Patterns**
   - Update all async methods to use ValueTask where appropriate
   - Implement IAsyncDisposable for resource management
   - Update channel/connection management for new RabbitMQ.Client APIs

2. **Nullable Reference Types**
   - Enable nullable reference types project-wide
   - Add nullable annotations to public APIs
   - Fix all nullable warnings

3. **Modern C# Features**
   - Update to C# 13 language version
   - Use file-scoped namespaces
   - Implement required members where appropriate
   - Use primary constructors for simple DTOs

4. **Performance Improvements**
   - Replace Newtonsoft.Json with System.Text.Json where possible
   - Use ReadOnlySpan<T> and Memory<T> for buffer management
   - Implement ISpanFormattable for custom types

### Phase 5: Testing and Validation (Week 5-6)

1. **Unit Tests**
   - Ensure all existing tests pass
   - Add tests for new async patterns
   - Validate nullable reference type annotations

2. **Integration Tests**
   - Test against RabbitMQ 3.13+ (latest)
   - Validate channel pooling and connection recovery
   - Performance benchmarks comparing old vs new

3. **Compatibility Tests**
   - Test consumers using older framework versions
   - Validate backward compatibility for public APIs
   - Document any breaking changes

### Phase 6: Documentation and Release (Week 6)

1. **Update Documentation**
   - Update README with new requirements
   - Document breaking changes
   - Update sample applications
   - Create migration guide for consumers

2. **Version Strategy**
   - Bump major version to 3.0.0 (from 2.x)
   - Follow semantic versioning
   - Create release notes

## Step-by-Step Upgrade Instructions

### Step 1: Update Global Configuration
```bash
# Create global.json to pin SDK version
cat > global.json << 'EOF'
{
  "sdk": {
    "version": "9.0.100",
    "rollForward": "latestMinor"
  }
}
EOF
```

### Step 2: Create Directory.Build.props
```xml
<Project>
  <PropertyGroup>
    <LangVersion>13.0</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);CS1591</NoWarn>
  </PropertyGroup>

  <PropertyGroup Condition="'$(IsTestProject)' == 'true'">
    <TargetFramework>net9.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <PropertyGroup Condition="'$(IsTestProject)' != 'true' AND '$(IsSample)' != 'true'">
    <TargetFrameworks>net9.0;net8.0;netstandard2.0</TargetFrameworks>
  </PropertyGroup>

  <ItemGroup Condition="'$(TargetFramework)' == 'net9.0' OR '$(TargetFramework)' == 'net8.0'">
    <FrameworkReference Include="Microsoft.AspNetCore.App" Condition="'$(UseAspNetCore)' == 'true'" />
  </ItemGroup>
</Project>
```

### Step 3: Update Core Library (RawRabbit.csproj)
```bash
# Update the main RawRabbit project
dotnet remove package RabbitMQ.Client
dotnet add package RabbitMQ.Client --version 6.8.1
dotnet remove package Newtonsoft.Json
dotnet add package System.Text.Json --version 9.0.0
```

### Step 4: Bulk Update All Projects
```powershell
# PowerShell script to update all projects
Get-ChildItem -Path . -Filter *.csproj -Recurse | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    
    # Update target frameworks
    $content = $content -replace '<TargetFrameworks?>netstandard1\.[56];net451</TargetFrameworks?>', '<TargetFrameworks>net9.0;net8.0;netstandard2.0</TargetFrameworks>'
    $content = $content -replace '<TargetFramework>net46</TargetFramework>', '<TargetFramework>net9.0</TargetFramework>'
    $content = $content -replace '<TargetFramework>netcoreapp[12]\.[01]</TargetFramework>', '<TargetFramework>net9.0</TargetFramework>'
    
    # Remove deprecated properties
    $content = $content -replace '<PackageTargetFallback>.*?</PackageTargetFallback>', ''
    $content = $content -replace '<RuntimeFrameworkVersion>.*?</RuntimeFrameworkVersion>', ''
    
    Set-Content $_.FullName $content
}
```

### Step 5: Update Package References
```bash
# Update all package references using dotnet CLI
find . -name "*.csproj" -exec dotnet restore {} \;
find . -name "*.csproj" -exec dotnet list {} package --outdated \;
```

### Step 6: Fix Compilation Errors
1. Update namespace imports for moved types
2. Fix nullable reference type warnings
3. Update async method signatures
4. Replace deprecated API calls

### Step 7: Run Tests
```bash
# Build the solution
dotnet build -c Release

# Run all tests
dotnet test -c Release --logger "console;verbosity=detailed"

# Run performance benchmarks
dotnet run -c Release --project test/RawRabbit.PerformanceTest
```

## Breaking Changes and Migration Guide

### For Library Consumers

#### 1. Minimum Framework Requirements
- **.NET Framework**: Minimum 4.6.2 (was 4.5.1)
- **.NET Core**: Minimum .NET 6.0 (was .NET Core 1.0)
- **.NET Standard**: Minimum 2.0 (was 1.5)

#### 2. API Changes
```csharp
// Old
public Task PublishAsync<T>(T message, Action<IPublishContext> context = null)

// New - with CancellationToken support
public ValueTask PublishAsync<T>(T message, Action<IPublishContext>? context = null, CancellationToken cancellationToken = default)
```

#### 3. Configuration Changes
```csharp
// Old
var config = new RawRabbitConfiguration
{
    Username = "guest",
    Password = "guest"
};

// New - using options pattern
services.Configure<RawRabbitOptions>(options =>
{
    options.Connection.UserName = "guest";
    options.Connection.Password = "guest";
});
```

#### 4. Dependency Injection Changes
```csharp
// Old
services.AddRawRabbit(cfg => cfg
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("rawrabbit.json"));

// New
services.AddRawRabbit(builder => builder
    .ConfigureOptions(options => { })
    .ConfigureConnection(conn => { })
    .ConfigureMiddleware(pipe => { }));
```

## Risk Assessment and Mitigation

### High-Risk Areas
1. **RabbitMQ.Client Upgrade**: Major version jump with breaking changes
   - Mitigation: Extensive integration testing, gradual rollout

2. **Serialization Changes**: Moving from Newtonsoft.Json to System.Text.Json
   - Mitigation: Provide compatibility layer, migration tools

3. **Async Pattern Changes**: ValueTask adoption may affect consumers
   - Mitigation: Provide extension methods for backward compatibility

### Medium-Risk Areas
1. **Multi-targeting Complexity**: Managing multiple framework targets
   - Mitigation: Automated testing matrix, clear documentation

2. **Performance Characteristics**: New runtime may have different behavior
   - Mitigation: Comprehensive benchmarking, performance tests

## Success Criteria

1. **All Tests Pass**: 100% of existing tests pass on .NET 9
2. **Performance**: No regression in throughput or latency
3. **Compatibility**: Existing consumers can upgrade with minimal changes
4. **Documentation**: Complete migration guide and updated samples
5. **Package Size**: NuGet package size remains reasonable (<1MB)

## Timeline Summary

- **Week 1**: Preparation and planning
- **Week 2-3**: Core framework upgrade
- **Week 3-4**: Package updates and dependency resolution
- **Week 4-5**: Code migration and modernization
- **Week 5-6**: Testing, validation, and documentation
- **Week 6**: Release preparation

Total estimated time: 6 weeks for complete upgrade

## Post-Upgrade Considerations

1. **Long-term Support Strategy**
   - Maintain 2.x branch for critical fixes only
   - Focus development on 3.x (NET 9) branch
   - Plan for .NET 10 upgrade in 2025

2. **Performance Monitoring**
   - Set up benchmarking CI/CD pipeline
   - Monitor package download statistics
   - Track issue reports for upgrade problems

3. **Community Communication**
   - Blog post announcing upgrade
   - Update NuGet package description
   - Engage with community for feedback

## Appendix A: Useful Commands

```bash
# Check current .NET SDKs installed
dotnet --list-sdks

# Install .NET 9 SDK
# Windows
winget install Microsoft.DotNet.SDK.9

# Linux/Mac
# Visit https://dotnet.microsoft.com/download/dotnet/9.0

# Update all NuGet packages in solution
dotnet restore
dotnet list package --outdated
dotnet add package [PackageName] --version [Version]

# Build with specific framework
dotnet build -f net9.0
dotnet build -f netstandard2.0

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

# Pack NuGet packages
dotnet pack -c Release -o ./artifacts
```

## Appendix B: Tooling Requirements

- Visual Studio 2022 17.12+ or Visual Studio Code with C# Dev Kit
- .NET 9 SDK (9.0.100+)
- RabbitMQ 3.13+ for testing
- Docker for containerized testing
- PowerShell Core 7+ for build scripts