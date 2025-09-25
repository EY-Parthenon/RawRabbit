# RawRabbit 2.0.0-net9 Release Notes

## Release Date
September 25, 2025

## Overview
This release brings full .NET 9 support to RawRabbit while maintaining backward compatibility with .NET Standard 2.0. This is a significant upgrade that modernizes the framework for the latest .NET runtime.

## Target Frameworks
- **.NET 9.0** - Full support with all optimizations
- **.NET Standard 2.0** - Maintained for backward compatibility

## Key Features

### .NET 9 Support
- All 32 projects upgraded to support .NET 9
- Multi-targeting strategy for maximum compatibility
- Modern async patterns with AsyncLocal<T>
- Performance improvements from .NET 9 runtime

### Package Updates
- **RabbitMQ.Client**: 5.2.0 (stable version maintained)
- **Newtonsoft.Json**: 13.0.3
- **Microsoft.Extensions.***: 9.0.0
- **Polly**: 7.2.4 (async API compatible)
- **xUnit**: 2.9.2
- **Moq**: 4.20.72

### Compatibility Improvements
- HttpContext enricher now supports .NET 9
- Fixed all conditional compilation for .NET 9
- Updated async context storage to use AsyncLocal<T>
- Resolved ambiguous method calls in .NET 9

## Breaking Changes
Minimal breaking changes for most users:
- `Assembly.CodeBase` replaced with `Assembly.Location` (internal change)
- ASP.NET Core samples may need updates for obsolete APIs
- Mock setups in tests need `clientProvidedName` parameter

## Migration Guide
See [MIGRATION_TO_NET9.md](./MIGRATION_TO_NET9.md) for detailed migration instructions.

## What's New

### Core Library
- Full .NET 9 support with performance optimizations
- Improved async/await patterns
- Better memory management
- Updated dependency injection support

### Enrichers
- **HttpContext**: Now supports .NET 9 with proper conditional compilation
- **Polly**: Updated to work with Polly 7.x async APIs
- **GlobalExecutionId**: Uses AsyncLocal<T> for .NET 9
- **MessageContext**: Updated for .NET 9 compatibility

### Testing
- All unit tests passing on .NET 9
- Integration tests ready for .NET 9
- Performance tests updated
- Mock frameworks updated for compatibility

## Installation

### NuGet Package Manager
```powershell
Install-Package RawRabbit -Version 2.0.0-net9
```

### .NET CLI
```bash
dotnet add package RawRabbit --version 2.0.0-net9
```

### PackageReference
```xml
<PackageReference Include="RawRabbit" Version="2.0.0-net9" />
```

## Tested Platforms
- .NET 9.0.100+
- .NET Standard 2.0 compatible frameworks
- Windows, Linux, macOS

## Known Issues
- ASP.NET sample uses some obsolete logging APIs (non-critical)
- MessagePack package has known vulnerabilities (consider updating)
- Some xUnit analyzer warnings in tests (non-blocking)

## Contributors
- RawRabbit community
- Automated upgrade assistance by Claude

## Documentation
- [README.md](./README.md) - Updated with .NET 9 information
- [MIGRATION_TO_NET9.md](./MIGRATION_TO_NET9.md) - Migration guide
- [DOTNET9_UPGRADE_REPORT.md](./DOTNET9_UPGRADE_REPORT.md) - Technical upgrade details

## Support
For issues or questions:
1. Check the migration guide
2. Review the upgrade report
3. Open an issue on GitHub

## Future Roadmap
- Consider upgrading to RabbitMQ.Client 6.x/7.x
- Upgrade to Polly 8.x with resilience pipelines
- Update ASP.NET samples to latest patterns
- Address remaining analyzer warnings

## License
Same as RawRabbit - see LICENSE file in repository

---
*This release represents a major step forward in modernizing RawRabbit for the latest .NET ecosystem.*