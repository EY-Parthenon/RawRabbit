# Security Scan Report: Code Security Analysis

**Date**: 2025-10-09
**Stage**: 1.5 - Security Baseline Scans
**Scan Type**: Static Code Security Analysis
**Projects Scanned**: 25 projects (19 source, 3 samples, 3 tests)

## Executive Summary

- Total Findings: **5**
- CRITICAL: **1** (TypeNameHandling.Auto RCE)
- HIGH: **0**
- MEDIUM: **3** (Hardcoded credentials, plain-text passwords)
- LOW: **1** (Non-cryptographic Random in samples)

**Critical Code Security Issues**:
1. TypeNameHandling.Auto enables remote code execution
2. Hardcoded guest/guest credentials in default configuration
3. Plain-text password storage in configuration class
4. Non-cryptographic Random() in sample code

**Risk Assessment**: **CRITICAL** - Immediate code changes required

## Scan Methodology

### Tools and Techniques

**Static Analysis**:
- Manual code review of 150+ .cs files
- Grep pattern matching for security anti-patterns
- Configuration file inspection
- JSON serialization security audit

**Search Patterns**:
```bash
# Credential patterns
grep -rn '(Password|password|pwd).*=.*["'\'']' src/ --include="*.cs"
grep -rn 'guest' src/ --include="*.cs" -i
grep -rn 'ConnectionString' src/ --include="*.cs"

# Insecure crypto
grep -rn 'System.Security.Cryptography' src/ --include="*.cs"
grep -rn 'new Random\(' src/ --include="*.cs"

# Insecure serialization
grep -rn 'TypeNameHandling' src/ --include="*.cs"

# TLS/SSL issues
grep -rn 'SslOption|Ssl\s*\{|TLS|Certificate' src/ --include="*.cs"
```

### Scope
- All source code in `src/` directory
- Configuration classes
- Serialization/deserialization code
- Sample applications (for pattern analysis)
- Test code (for best practice validation)

### Exclusions
- Third-party library code (analyzed separately in dependency scan)
- Generated code
- Build artifacts

## Detailed Findings

### CRITICAL-001: Insecure JSON Deserialization (TypeNameHandling.Auto)

**Severity**: CRITICAL
**CWE**: CWE-502 (Deserialization of Untrusted Data)
**CVSS Score**: 9.8 (Critical)

**Location**:
```
src/RawRabbit/DependencyInjection/RawRabbitDependencyRegisterExtension.cs:57
```

**Vulnerable Code**:
```csharp
.AddSingleton<ISerializer>(resolver => new Serialization.JsonSerializer(new Newtonsoft.Json.JsonSerializer
{
    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
    Formatting = Formatting.None,
    CheckAdditionalContent = true,
    ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() },
    ObjectCreationHandling = ObjectCreationHandling.Auto,
    DefaultValueHandling = DefaultValueHandling.Ignore,
    TypeNameHandling = TypeNameHandling.Auto,  // ⚠️ CRITICAL RCE VULNERABILITY
    ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
    MissingMemberHandling = MissingMemberHandling.Ignore,
    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
    NullValueHandling = NullValueHandling.Ignore
}))
```

**Description**:
The JSON serializer configuration uses `TypeNameHandling.Auto`, which enables automatic type resolution during deserialization. This is a well-known insecure deserialization vulnerability that allows remote code execution through gadget chain attacks.

**Attack Vector**:
1. Attacker publishes malicious JSON message to RabbitMQ exchange
2. RawRabbit subscriber receives message
3. JsonSerializer with TypeNameHandling.Auto deserializes message
4. Malicious $type directive triggers arbitrary .NET type instantiation
5. Gadget chain executes attacker-controlled code

**Proof of Concept**:
```json
{
  "$type": "System.Windows.Data.ObjectDataProvider, PresentationFramework",
  "MethodName": "Start",
  "MethodParameters": {
    "$type": "System.Collections.ArrayList, mscorlib",
    "$values": ["cmd.exe", "/c calc.exe"]
  },
  "ObjectInstance": {
    "$type": "System.Diagnostics.Process, System"
  }
}
```

**Impact**:
- **Remote Code Execution** with application privileges
- Complete system compromise
- Data exfiltration
- Lateral movement in network
- Ransomware deployment

**Real-World Exploitability**: **VERY HIGH**
- Public exploits available (Blackhat USA 2017, ysoserial.net)
- No authentication required (network-accessible RabbitMQ)
- Applicable to ALL message handlers in RawRabbit applications

**Remediation** (URGENT):
```csharp
// REQUIRED FIX (Stage 2 - Week 2):
TypeNameHandling = TypeNameHandling.None,  // SAFE: Disables type resolution
```

**Additional Secure Configuration**:
```csharp
// If polymorphic type handling is REQUIRED (use with caution):
TypeNameHandling = TypeNameHandling.Objects,  // Safer: Only for $type on objects
SerializationBinder = new KnownTypesBinder(allowedTypes),  // Whitelist types
```

**Testing**:
```csharp
// Add unit test (Stage 2):
[Fact]
public void JsonSerializer_Should_Not_Use_TypeNameHandling_Auto()
{
    var serializer = _serviceProvider.GetService<ISerializer>();
    var jsonSerializer = (Newtonsoft.Json.JsonSerializer)serializer.GetType()
        .GetField("_serializer", BindingFlags.NonPublic | BindingFlags.Instance)
        .GetValue(serializer);

    Assert.NotEqual(TypeNameHandling.Auto, jsonSerializer.TypeNameHandling);
    Assert.NotEqual(TypeNameHandling.All, jsonSerializer.TypeNameHandling);
}
```

**Timeline**: Stage 2 (Week 2) - **URGENT FIX**

**References**:
- CWE-502: https://cwe.mitre.org/data/definitions/502.html
- OWASP Deserialization: https://owasp.org/www-community/vulnerabilities/Deserialization_of_untrusted_data
- Blackhat USA 2017: "Friday the 13th: JSON Attacks"
- ysoserial.net: https://github.com/pwntester/ysoserial.net
- Related CVE: CVE-2024-21908 (Newtonsoft.Json)

---

### MEDIUM-001: Hardcoded Credentials in Default Configuration

**Severity**: MEDIUM
**CWE**: CWE-798 (Use of Hard-coded Credentials)
**CVSS Score**: 5.3 (Medium)

**Location**:
```
src/RawRabbit/Configuration/RawRabbitConfiguration.cs:110-117
```

**Vulnerable Code**:
```csharp
public static RawRabbitConfiguration Local => new RawRabbitConfiguration
{
    VirtualHost = "/",
    Username = "guest",      // ⚠️ Hardcoded default credential
    Password = "guest",      // ⚠️ Hardcoded default credential
    Port = 5672,
    Hostnames = new List<string> { "localhost" }
};
```

**Grep Output**:
```
src/RawRabbit/Configuration/RawRabbitConfiguration.cs:113:   Username = "guest",
src/RawRabbit/Configuration/RawRabbitConfiguration.cs:114:   Password = "guest",
```

**Description**:
The `RawRabbitConfiguration.Local` static property provides a pre-configured instance with hardcoded "guest/guest" credentials. While intended for development/testing, this pattern creates risk if developers copy it to production or fail to override credentials.

**Issues**:
1. Hardcoded credentials in source code
2. No warnings against production use
3. No validation to detect guest/guest in non-dev environments
4. Pattern propagation risk (developers copying example code)

**Impact**:
- **Credential exposure** if configuration used in production
- Unauthorized RabbitMQ access
- Message interception and tampering
- Service denial through connection exhaustion

**Risk Assessment**:
- **Likelihood**: MEDIUM (developers may use default config in production)
- **Impact**: HIGH (full RabbitMQ compromise)
- **Overall Risk**: MEDIUM

**Current Usage Analysis**:
```bash
# Check for usage in codebase:
grep -rn "RawRabbitConfiguration.Local" src/ sample/ test/
```

**Found in**:
- Sample applications (appropriate)
- Test fixtures (appropriate)
- Documentation examples (requires warning)

**Remediation**:

**Option 1: Add Warning Documentation** (Stage 2 - Week 2):
```csharp
/// <summary>
/// ⚠️ DEVELOPMENT ONLY - DO NOT USE IN PRODUCTION
///
/// Provides a default configuration for local development with RabbitMQ running
/// on localhost with default guest/guest credentials.
///
/// SECURITY WARNING: Never use this configuration in production environments.
/// Always load credentials from secure configuration sources:
/// - Azure Key Vault
/// - AWS Secrets Manager
/// - Environment variables
/// - Kubernetes Secrets
///
/// See: https://github.com/pardahlman/RawRabbit/wiki/Secure-Configuration
/// </summary>
public static RawRabbitConfiguration Local => new RawRabbitConfiguration
{
    VirtualHost = "/",
    Username = "guest",  // Default RabbitMQ credentials (dev only)
    Password = "guest",  // Default RabbitMQ credentials (dev only)
    Port = 5672,
    Hostnames = new List<string> { "localhost" }
};
```

**Option 2: Add Runtime Validation** (Stage 4):
```csharp
// In startup/configuration validation:
public static void ValidateProductionConfiguration(RawRabbitConfiguration config)
{
    if (!IsDevEnvironment())
    {
        if (config.Username == "guest" && config.Password == "guest")
        {
            throw new InvalidOperationException(
                "SECURITY ERROR: Default guest/guest credentials detected in production. " +
                "Load credentials from secure configuration source."
            );
        }
    }
}
```

**Option 3: Environment Variable Support** (Stage 4):
```csharp
public static RawRabbitConfiguration FromEnvironment() => new RawRabbitConfiguration
{
    VirtualHost = Environment.GetEnvironmentVariable("RABBITMQ_VHOST") ?? "/",
    Username = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME")
        ?? throw new InvalidOperationException("RABBITMQ_USERNAME not set"),
    Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD")
        ?? throw new InvalidOperationException("RABBITMQ_PASSWORD not set"),
    Port = int.Parse(Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? "5672"),
    // ...
};
```

**Timeline**:
- Stage 2 (Week 2-3): Documentation warnings
- Stage 4 (Week 9-12): Runtime validation and environment variable support

**References**:
- CWE-798: https://cwe.mitre.org/data/definitions/798.html
- OWASP Secrets Management: https://cheatsheetseries.owasp.org/cheatsheets/Secrets_Management_Cheat_Sheet.html
- Related ADR: docs/adr/0010-secrets-management.md (pending)

---

### MEDIUM-002: Plain-Text Password Storage

**Severity**: MEDIUM
**CWE**: CWE-256 (Unprotected Storage of Credentials)
**CVSS Score**: 4.4 (Medium)

**Location**:
```
src/RawRabbit/Configuration/RawRabbitConfiguration.cs:76
```

**Vulnerable Code**:
```csharp
public string Password { get; set; }  // Plain string, not SecureString
```

**Description**:
The `Password` property is declared as a plain `string`, which is immutable and persists in memory until garbage collected. This exposes credentials to:
- Memory dumps
- Debugging sessions
- Crash dumps
- Process inspection tools

**Issues**:
1. Password stored as immutable string (cannot be zeroed)
2. Vulnerable to memory scraping
3. Persists in heap until GC
4. Visible in debugger watch windows
5. Included in crash dumps

**Impact**:
- **Credential exposure** in memory dumps
- Forensic password recovery
- Debugging session leaks
- Crash report exposure

**Risk Assessment**:
- **Likelihood**: LOW (requires memory access or crash dump)
- **Impact**: MEDIUM (credential exposure)
- **Overall Risk**: MEDIUM

**Remediation Options**:

**Option A: Document Limitation** (Stage 2 - Recommended):
```csharp
/// <summary>
/// RabbitMQ password.
///
/// SECURITY NOTE: Stored as plain string for RabbitMQ.Client compatibility.
/// String values persist in memory until garbage collected and may appear
/// in memory dumps or debugging sessions.
///
/// Best Practices:
/// 1. Load from secure storage (Azure Key Vault, AWS Secrets Manager)
/// 2. Never hardcode in source code
/// 3. Use short-lived credentials where possible
/// 4. Rotate credentials regularly
///
/// Alternative: Use connection string parsing to avoid in-memory storage:
/// var config = ConnectionStringParser.Parse(
///     Environment.GetEnvironmentVariable("RABBITMQ_CONNECTION_STRING")
/// );
/// </summary>
public string Password { get; set; }
```

**Option B: Add SecureString Support** (Stage 4 - Complex):
```csharp
// Requires RabbitMQ.Client compatibility check
public SecureString SecurePassword { get; set; }

// Fallback for backward compatibility
public string Password
{
    get => _password;
    set
    {
        _password = value;
        // Warn if setting plain password
        if (!string.IsNullOrEmpty(value))
        {
            System.Diagnostics.Debug.WriteLine(
                "WARNING: Setting plain-text password. " +
                "Consider using SecurePassword property instead."
            );
        }
    }
}
```

**Option C: External Secrets Management** (Recommended Long-Term):
```csharp
// Document pattern in ADR:
// 1. Azure Key Vault integration
// 2. AWS Secrets Manager integration
// 3. HashiCorp Vault integration
// 4. Kubernetes Secrets integration
```

**Recommendation**: **Option A + Option C**
- Document limitation (Stage 2)
- Promote external secrets management (Stage 4+)
- Keep string type for RabbitMQ.Client compatibility

**Timeline**:
- Stage 2: Documentation
- Stage 4+: External secrets integration examples

**References**:
- CWE-256: https://cwe.mitre.org/data/definitions/256.html
- Microsoft SecureString: https://docs.microsoft.com/en-us/dotnet/api/system.security.securestring
- OWASP Secrets Management: https://cheatsheetseries.owasp.org/cheatsheets/Secrets_Management_Cheat_Sheet.html

---

### MEDIUM-003: No Connection String Validation

**Severity**: MEDIUM
**CWE**: CWE-20 (Improper Input Validation)

**Location**:
```
src/RawRabbit/Common/ConnectionStringParser.cs:9
```

**Grep Output**:
```
src/RawRabbit/Common/ConnectionStringParser.cs:9:  public class ConnectionStringParser
```

**Description**:
A `ConnectionStringParser` class exists but requires deeper inspection to validate its security controls. Connection string parsing is a common source of injection vulnerabilities if not properly validated.

**Potential Issues**:
1. Insufficient validation of connection string format
2. Lack of sanitization of user-provided values
3. Potential for injection attacks
4. Missing bounds checking on parsed values

**Recommendation**:
- **Stage 2**: Manual code review of ConnectionStringParser implementation
- **Stage 4**: Add input validation tests
- **Stage 4**: Add fuzzing tests for connection string parsing

**Timeline**: Stage 2 (code review), Stage 4 (testing)

---

### LOW-001: Non-Cryptographic Random in Sample Code

**Severity**: LOW
**CWE**: CWE-338 (Use of Cryptographically Weak PRNG)
**CVSS Score**: 2.0 (Low)

**Location**:
```
sample/RawRabbit.AspNet.Sample/Controllers/ValuesController.cs:23
```

**Vulnerable Code**:
```csharp
private readonly Random _random;

public ValuesController(IBusClient legacyBusClient, ILoggerFactory loggerFactory)
{
    _busClient = legacyBusClient;
    _logger = loggerFactory.CreateLogger<ValuesController>();
    _random = new Random();  // Line 23 - Not cryptographically secure
}

// Usage:
NumberOfValues = _random.Next(1, 10)  // Line 34 - Demo purposes only
```

**Grep Output**:
```
sample/RawRabbit.AspNet.Sample/Controllers/ValuesController.cs:23:  _random = new Random();
```

**Description**:
Sample code uses `System.Random` for generating random values. While appropriate for the demo use case (generating count of values), this pattern may be copied by developers for security-sensitive operations.

**Current Assessment**:
- **Context**: Sample application demonstration code
- **Purpose**: Generate random count (1-10) of demo values
- **Security Risk**: NONE (not security-sensitive)
- **Pattern Risk**: MEDIUM (developers may copy for tokens, IDs, nonces)

**Impact**:
- **Direct**: None (demo code only)
- **Indirect**: Pattern propagation to security-sensitive contexts

**Remediation** (Stage 2):
```csharp
// ⚠️ DEMO ONLY: System.Random is NOT cryptographically secure.
//
// For demo/testing purposes: Use System.Random (faster, sufficient)
// For security-sensitive operations (tokens, IDs, nonces, salts):
//   .NET 6+: RandomNumberGenerator.GetInt32(1, 10)
//   .NET <6: RandomNumberGenerator.Create().GetBytes()
//
// See: https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.randomnumbergenerator
private readonly Random _random = new Random();
```

**Timeline**: Stage 2 (Week 2-3) - Add code comment

**References**:
- CWE-338: https://cwe.mitre.org/data/definitions/338.html
- .NET RandomNumberGenerator: https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.randomnumbergenerator

---

## Code Security Strengths

### Positive Findings

1. ✅ **No Direct Cryptography**: Zero usage of `System.Security.Cryptography` namespace
   - Delegates all cryptographic operations to RabbitMQ.Client
   - Reduces attack surface
   - No deprecated crypto algorithms

2. ✅ **No SQL Injection Risk**: No database operations in codebase
   - Message-based architecture
   - No SQL query construction

3. ✅ **No OS Command Injection**: No System.Diagnostics.Process usage (except sample code)
   - No shell command execution
   - No file system operations

4. ✅ **No Binary Serialization**: No BinaryFormatter usage
   - Avoids .NET binary serialization vulnerabilities
   - Uses JSON serialization (with noted TypeNameHandling issue)

5. ✅ **Modern Async Patterns**: Async/await used correctly
   - No thread pool exhaustion risks
   - Proper async cancellation support

6. ✅ **Interface-Based Design**: Testable architecture
   - Dependency injection throughout
   - Easy to validate security properties

## Validation Against Stage 1.3

### Confirmed Findings

✅ **All Stage 1.3 code findings confirmed**:
1. TypeNameHandling.Auto (CRITICAL) - CONFIRMED at line 57
2. Hardcoded guest/guest credentials (MEDIUM) - CONFIRMED at lines 113-114
3. Plain-text password storage (MEDIUM) - CONFIRMED at line 76
4. Non-cryptographic Random (LOW) - CONFIRMED in sample code

### New Findings

1. **ConnectionStringParser** requires deeper security review
2. Additional serialization configuration settings need validation:
   - `PreserveReferencesHandling.Objects` - potential DoS risk
   - `ReferenceLoopHandling.Serialize` - potential infinite loop
   - `ObjectCreationHandling.Auto` - potential type confusion

### Resolved Findings

None - all Stage 1.3 findings remain valid.

## Next Steps

### Immediate Actions (Stage 2 - Week 2)

**CRITICAL**:
- [ ] Change TypeNameHandling.Auto → TypeNameHandling.None
- [ ] Add unit test for TypeNameHandling validation
- [ ] Code review PR for security impact

**HIGH**:
- [ ] Add XML documentation warnings to RawRabbitConfiguration.Local
- [ ] Document secure credential management patterns
- [ ] Create ADR-0002: Security Architecture
- [ ] Create ADR-0010: Secrets Management

**MEDIUM**:
- [ ] Add code comments to Random() usage in samples
- [ ] Document plain-text password limitation
- [ ] Manual review of ConnectionStringParser

### Stage 4 Actions (Week 9-12)

**Testing**:
- [ ] Add security unit tests (TypeNameHandling, credentials)
- [ ] Add fuzzing tests for ConnectionStringParser
- [ ] Add integration tests for secure configuration validation

**Features**:
- [ ] Runtime validation for guest/guest in production
- [ ] Environment variable configuration support
- [ ] Examples for Azure Key Vault, AWS Secrets Manager

### Continuous Monitoring

**Code Quality**:
- [ ] Enable GitHub CodeQL for automatic security scanning
- [ ] Add pre-commit hooks for security anti-pattern detection
- [ ] Configure SonarQube/SonarCloud for static analysis

**Documentation**:
- [ ] Create secure coding guidelines
- [ ] Document security code review checklist
- [ ] Add security section to CONTRIBUTING.md

## References

### Security Standards
- OWASP Top 10 (2021): https://owasp.org/Top10/
- CWE Top 25 (2024): https://cwe.mitre.org/top25/
- OWASP Secure Coding Practices: https://owasp.org/www-project-secure-coding-practices-quick-reference-guide/

### .NET Security
- .NET Security Guidelines: https://docs.microsoft.com/en-us/dotnet/standard/security/
- Secure Coding in C#: https://docs.microsoft.com/en-us/dotnet/standard/security/secure-coding-guidelines
- Newtonsoft.Json Security: https://www.newtonsoft.com/json/help/html/SerializationSettings.htm

### Internal Documentation
- Stage 1.3 Security Baseline: docs/stage-1/security-baseline-report.md
- ADR-0002: Security Architecture (pending)
- ADR-0010: Secrets Management (pending)

---

**Report Status**: ✅ Complete
**Next Review**: After Stage 2 TypeNameHandling fix
**Approval Required**: Migration Architect, Security Lead

**Classification**: Internal Use
**Retention**: 7 years
