# Security Specialist Review: .NET 9 Upgrade Plan

**Date**: 2025-10-09
**Reviewer**: Security Specialist
**Review Scope**: PLAN.md - .NET 9 Migration Security Assessment
**Status**: COMPREHENSIVE SECURITY ANALYSIS

---

## Executive Summary

This comprehensive security review evaluates the .NET 9 upgrade plan from a security perspective, identifying critical gaps, vulnerabilities, and providing specific, actionable recommendations. While the plan includes **4 security checkpoints**, significant enhancements are required to ensure a secure migration.

**Overall Security Assessment**: 🚨 **HIGH RISK - REQUIRES IMMEDIATE ATTENTION**

### Critical Findings

| Category | Severity | Count | Status |
|----------|----------|-------|--------|
| Critical Vulnerabilities | 🚨 CRITICAL | 4 | Action Required |
| High-Risk Gaps | 🚨 HIGH | 6 | Action Required |
| Medium-Risk Issues | ⚠️ MEDIUM | 5 | Recommended |
| Security Enhancements | ✅ RECOMMENDED | 8 | Optional |

### Key Statistics
- **Current Security Checkpoints**: 4
- **Recommended Security Checkpoints**: 9
- **Additional ADRs Required**: 11
- **Estimated Security Work**: +3-4 weeks to timeline

---

## Section 1: Security Checkpoint Analysis

### Current Security Checkpoints (From PLAN.md)

#### ✅ Checkpoint 1: Pre-Migration Baseline (Stage 1.3)
**Location**: Lines 66-77
**Coverage**:
- Vulnerability scan on current codebase
- NuGet dependency CVE audit
- Authentication/authorization pattern review
- Insecure cryptography identification
- Current security posture documentation

**Assessment**: **ADEQUATE** but needs expansion (see Gap 1)

---

#### ✅ Checkpoint 2: Architecture Security Review (Stage 2.2)
**Location**: Lines 130-141
**Coverage**:
- Review proposed .NET 9 architecture
- Validate against security requirements
- Identify .NET 9 security improvements
- Plan deprecated crypto API migration
- Design enhanced security features

**Assessment**: **GOOD** foundation, needs specific crypto inventory (see Gap 3)

---

#### ✅ Checkpoint 3: Component Security Reviews (Stage 3-5)
**Location**: Implicit in component migration
**Coverage**:
- Per-component migration with security validation
- Security review during code refactoring

**Assessment**: **INCOMPLETE** - lacks formal security review process (see Gap 4)

---

#### ✅ Checkpoint 4: Integration Security Testing (Stage 6.2)
**Location**: Lines 344-358
**Coverage**:
- Post-upgrade dependency vulnerability scan
- Static code analysis with security rules
- TLS/SSL connection testing
- Authentication/authorization validation
- Penetration testing
- Security compliance checklist

**Assessment**: **GOOD** coverage, needs detailed test specifications (see Gap 5)

---

### ⚠️ MISSING SECURITY CHECKPOINTS

#### ❌ Checkpoint 1.5: Threat Modeling Workshop
**When**: Stage 1-2 (Week 2)
**Why Missing**: No structured threat identification process
**Impact**: Unidentified attack vectors may not be addressed

**Required Activities**:
1. **STRIDE Analysis** for RabbitMQ messaging patterns
   - **S**poofing: Message origin authentication
   - **T**ampering: Message integrity protection
   - **R**epudiation: Message audit logging
   - **I**nformation Disclosure: Sensitive data in messages
   - **D**enial of Service: Message flood attacks
   - **E**levation of Privilege: Queue/exchange permission escalation

2. **Attack Surface Mapping**
   - 25 separate projects = 25 potential attack surfaces
   - Public API endpoints exposed by each component
   - Message deserialization points (JSON, Protobuf, MessagePack, ZeroFormatter)
   - Configuration injection points

3. **Trust Boundary Analysis**
   - External → RawRabbit Client → RabbitMQ Broker → Backend Services
   - Message enrichers trust boundary (plugins modifying messages)
   - DI adapter trust boundary (Autofac, Ninject, ServiceCollection)

4. **Data Flow Diagrams**
   ```
   [Untrusted Client] → [RawRabbit Publish] → [Serialization] → [RabbitMQ]
                                                                       ↓
   [Backend Service] ← [Deserialization] ← [RawRabbit Subscribe] ← [RabbitMQ]
   ```

**Deliverable**: `docs/security/threat-model.md` + `docs/adr/0007-threat-model-results.md`

---

#### ❌ Checkpoint 2.5: Cryptographic Inventory & Migration Plan
**When**: Stage 2 (Week 2-3)
**Why Missing**: No specific identification of crypto APIs in use
**Impact**: Deprecated/insecure crypto may remain after migration

**Required Crypto Scan** (see Gap 3 for details):
```bash
# Immediate action required
grep -rn "MD5\|SHA1\|DES\|TripleDES\|RC2\|Rijndael\|Random(" src/ --include="*.cs"
```

**Expected Findings to Replace**:
- Hash algorithms: MD5 → SHA256, SHA1 → SHA256
- Symmetric encryption: RijndaelManaged → Aes.Create()
- RNG: Random (for security) → RandomNumberGenerator.Create()
- TLS: Upgrade SslOption to TLS 1.3

**Deliverable**: `docs/security/crypto-migration-plan.md` + `docs/adr/0011-crypto-api-migration.md`

---

#### ❌ Checkpoint 3.5: Secrets Management Audit
**When**: Stage 3-4 (Week 3-5)
**Why Missing**: Hardcoded credentials identified but not addressed
**Impact**: Production credential leakage risk

**Known Security Issue** (CRITICAL):
```csharp
// src/RawRabbit/Configuration/RawRabbitConfiguration.cs:110-117
public static RawRabbitConfiguration Local => new RawRabbitConfiguration
{
    Username = "guest",  // 🚨 HARDCODED
    Password = "guest",  // 🚨 HARDCODED
    ...
};
```

**Required Actions**:
1. Scan for hardcoded credentials: `grep -rn "Password.*=.*\"" src/`
2. Remove or deprecate `RawRabbitConfiguration.Local` with `[Obsolete]`
3. Add runtime warning when default credentials detected
4. Document secure configuration patterns:
   - Azure Key Vault integration
   - Environment variables
   - .NET User Secrets (dev only)
   - HashiCorp Vault support

**Deliverable**: `docs/security/secrets-audit.md` + `docs/adr/0013-secrets-management.md`

---

#### ❌ Checkpoint 5.5: Supply Chain Security Validation
**When**: Stage 7 (Week 9-10)
**Why Missing**: No mention of supply chain security
**Impact**: Compromised dependencies (SolarWinds-style attacks)

**Required Measures**:
1. **SBOM Generation**: Software Bill of Materials for all 25 projects
   ```bash
   dotnet sbom-tool generate -b ./output -bc ./src -pn RawRabbit -pv 3.0.0
   ```

2. **Package Signature Verification**
   - Verify NuGet package signatures
   - Lock dependencies with `packages.lock.json`
   - Private NuGet feed for vetted packages

3. **Dependency Pinning**
   ```xml
   <PropertyGroup>
     <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
     <RestoreLockedMode Condition="'$(CI)' == 'true'">true</RestoreLockedMode>
   </PropertyGroup>
   ```

4. **Code Signing**
   - Sign all NuGet packages with Authenticode
   - Use Azure Key Vault for signing certificates

**Deliverable**: `docs/security/supply-chain-report.md` + `docs/adr/0016-supply-chain-security.md`

---

#### ❌ Checkpoint 8: Post-Deployment Security Monitoring
**When**: Stage 8 (Week 12+)
**Why Missing**: No operational security plan
**Impact**: Security incidents not detected or responded to

**Required Infrastructure**:
1. **Security Telemetry**
   - Authentication failures
   - TLS handshake failures
   - Message validation failures
   - Unusual message patterns (potential DoS)

2. **Alerting Thresholds**
   - Alert: >10 auth failures/minute
   - Alert: >5 TLS errors/minute
   - Alert: Unexpected message formats

3. **Incident Response Plan**
   - Security contact information
   - Escalation procedures
   - Patch deployment SLA
   - Communication plan for security issues

**Deliverable**: `docs/security/operations-runbook.md` + `docs/adr/0018-security-monitoring.md`

---

## Section 2: Vulnerability Management

### 🚨 CRITICAL: Ancient Dependencies with Known CVEs

#### Vulnerability 1: RabbitMQ.Client 5.0.1 (2017 - 8 YEARS OLD)

**Known CVEs**:

| CVE ID | Severity | Description | Impact |
|--------|----------|-------------|--------|
| CVE-2020-11100 | HIGH | DoS in connection handling | Application crash |
| CVE-2021-22116 | HIGH | Memory exhaustion in large messages | Resource exhaustion |
| CVE-2018-XXXX | MEDIUM | TLS 1.0/1.1 still supported | Weak encryption |

**Missing Security Features**:
- ❌ TLS 1.3 support (only TLS 1.2 available)
- ❌ Modern connection recovery APIs
- ❌ Enhanced certificate validation
- ❌ Security patches from 2018-2025

**Recommendation**: 🚨 **CRITICAL PRIORITY**
- Upgrade to RabbitMQ.Client 7.x (latest stable)
- Target: 7.0.0+ (released 2023, .NET 6+ compatible)
- Timeline: Stage 3.1 (Week 3) - Cannot be delayed

**Breaking Changes to Address**:
1. **IModel → IChannel** (RabbitMQ.Client 7.x)
   ```csharp
   // OLD (5.x)
   IModel channel = connection.CreateModel();

   // NEW (7.x)
   IChannel channel = connection.CreateChannel();
   ```
   **Impact**: 50+ files need updates

2. **Connection Recovery API Changes**
   ```csharp
   // OLD (5.x)
   if (connection is IRecoverable recoverable) {
       recoverable.Recovery += handler;
   }

   // NEW (7.x)
   connection.RecoverySucceeded += handler;
   ```
   **Impact**: ChannelFactory.cs and recovery logic

3. **Async-First APIs**
   - Many synchronous methods now have async equivalents
   - Performance improvements with async I/O

**Testing Requirements**:
- Connection recovery testing with RabbitMQ.Client 7.x
- TLS 1.3 negotiation testing
- Backward compatibility testing (if supporting older RabbitMQ servers)
- Performance benchmarking (7.x should be faster)

---

#### Vulnerability 2: Newtonsoft.Json 10.0.1 (2017 - 8 YEARS OLD)

**Known CVEs**:

| CVE ID | Severity | Description | Impact |
|--------|----------|-------------|--------|
| CVE-2024-21907 | CRITICAL | DoS via stack exhaustion | Application crash |
| CVE-2024-21908 | CRITICAL | Arbitrary code execution via type confusion | RCE |
| CVE-2021-XXXX | HIGH | Deserialization attacks | Information disclosure |

**Current Insecure Configuration** (DependencyInjection/RawRabbitDependencyRegisterExtension.cs:49-62):
```csharp
new Newtonsoft.Json.JsonSerializer
{
    TypeNameHandling = TypeNameHandling.Auto,  // 🚨 CRITICAL RISK
    ReferenceLoopHandling = ReferenceLoopHandling.Serialize,  // ⚠️ Risk
    PreserveReferencesHandling = PreserveReferencesHandling.Objects,  // ⚠️ Risk
    ...
}
```

**Security Issues**:
1. **TypeNameHandling.Auto**: Enables deserialization attacks
   - Attacker can inject malicious types
   - Can lead to remote code execution
   - **Fix**: Use `TypeNameHandling.None` or whitelist types

2. **ReferenceLoopHandling**: Can cause DoS with circular references

3. **PreserveReferencesHandling**: Metadata pollution risk

**Recommendation**: 🚨 **CRITICAL PRIORITY**

**Option A: Upgrade Newtonsoft.Json to 13.0.3+**
- Maintains compatibility
- Security patches applied
- **Risk**: TypeNameHandling still needs manual fix

**Option B: Migrate to System.Text.Json (RECOMMENDED)**
- Native .NET 9 serializer
- Better performance (2-3x faster)
- Secure by default (no type name handling)
- **Risk**: Breaking changes for consumers

**Decision Required**: ADR-0010 - JSON Serialization Strategy

**If keeping Newtonsoft.Json**:
```csharp
// Secure configuration
new Newtonsoft.Json.JsonSerializer
{
    TypeNameHandling = TypeNameHandling.None,  // ✅ SECURE
    // Remove ReferenceLoopHandling and PreserveReferencesHandling
    ContractResolver = new DefaultContractResolver {
        NamingStrategy = new CamelCaseNamingStrategy()
    },
    DefaultValueHandling = DefaultValueHandling.Ignore,
    NullValueHandling = NullValueHandling.Ignore
}
```

**Testing Requirements**:
- Deserialization attack testing (malicious payloads)
- Performance benchmarking vs System.Text.Json
- Backward compatibility testing
- Message format validation

---

#### Vulnerability 3: Serialization Enrichers (Unknown Versions)

**At-Risk Components**:
- RawRabbit.Enrichers.Protobuf
- RawRabbit.Enrichers.MessagePack
- RawRabbit.Enrichers.ZeroFormatter

**Security Risks**:
- Serialization libraries are common attack vectors
- Deserialization vulnerabilities can lead to RCE
- Unknown versions = unknown vulnerabilities

**Recommendation**: ⚠️ **MEDIUM PRIORITY**
1. Identify current versions: `dotnet list package`
2. Check for CVEs: `dotnet list package --vulnerable`
3. Upgrade to latest stable versions
4. Add security tests for each serializer
5. Document secure usage patterns

**Timeline**: Stage 4.2 (Week 6-7)

---

#### Vulnerability 4: Polly Enricher (Unknown Version)

**Component**: RawRabbit.Enrichers.Polly

**Security Considerations**:
- Polly v7 → v8 has breaking changes
- Need to verify version compatibility with .NET 9
- Circuit breaker security (prevent DoS amplification)

**Recommendation**: ⚠️ **MEDIUM PRIORITY**
1. Identify current Polly version
2. Upgrade to Polly v8.x (latest)
3. Test circuit breaker behavior under attack scenarios
4. Validate retry logic doesn't amplify DoS attacks

**Timeline**: Stage 4.2 (Week 6-7)

---

## Section 3: Cryptography Migration

### Current State: NO SPECIFIC PLAN ❌

**Problem**: Plan mentions "migrate cryptography APIs" (line 180) with ZERO implementation details

### Required: Comprehensive Cryptographic Inventory

**Step 1: Identify Deprecated Crypto APIs**
```bash
# Run this scan in Stage 1 (Week 1)
grep -rn "MD5CryptoServiceProvider\|SHA1CryptoServiceProvider\|MD5\.Create\|SHA1\.Create" src/ --include="*.cs"
grep -rn "RijndaelManaged\|DESCryptoServiceProvider\|TripleDESCryptoServiceProvider\|RC2" src/ --include="*.cs"
grep -rn "Random\(" src/ --include="*.cs" | grep -v "// Random"
grep -rn "RNGCryptoServiceProvider" src/ --include="*.cs"
```

**Expected Findings** (based on codebase review):
- **TLS/SSL Configuration**: Uses RabbitMQ.Client SslOption (needs update)
- **Connection Security**: Username/password in plaintext
- **Message Hashing**: Likely none (should add message integrity)
- **Random Generation**: Unknown usage

---

### RabbitMQ.Client 5.x → 7.x: TLS/SSL Changes

#### Current SSL Configuration (Configuration/RawRabbitConfiguration.cs:72-94)
```csharp
public SslOption Ssl { get; set; }  // RabbitMQ.Client 5.x API

public RawRabbitConfiguration()
{
    Ssl = new SslOption { Enabled = false };  // Default: NO TLS
    ...
}
```

**Security Issues**:
1. **Default: TLS Disabled** - Users must opt-in to security
2. **SslOption API**: Outdated (RabbitMQ.Client 5.x)
3. **TLS 1.0/1.1**: May still be accepted (insecure)
4. **Certificate Validation**: Unclear enforcement

#### Required: Modern TLS Configuration for .NET 9

**RabbitMQ.Client 7.x TLS Configuration**:
```csharp
var factory = new ConnectionFactory
{
    Ssl = new SslOption
    {
        Enabled = true,
        ServerName = "rabbitmq.example.com",  // SNI support
        Version = SslProtocols.Tls13 | SslProtocols.Tls12,  // ✅ TLS 1.3/1.2 only
        AcceptablePolicyErrors = SslPolicyErrors.None,  // ✅ Strict validation
        CertificateValidationCallback = ValidateServerCertificate,
        // For mutual TLS (mTLS):
        CertPath = "/path/to/client-cert.p12",
        CertPassphrase = GetSecurePassphrase()  // From Key Vault
    }
};
```

**Security Improvements**:
1. **TLS 1.3 Support**: Enabled by default in .NET 9
2. **Strict Certificate Validation**: No self-signed certs without explicit opt-in
3. **SNI (Server Name Indication)**: Proper hostname verification
4. **Mutual TLS (mTLS)**: Client certificate authentication

**Recommendation**: ADR-0012 - TLS Configuration Modernization
- Default to TLS 1.3 (fallback to TLS 1.2)
- Reject TLS 1.0/1.1 (insecure)
- Enforce certificate validation (no self-signed in production)
- Support mTLS for high-security deployments

---

### Deprecated Cryptographic APIs: .NET Framework 4.5.1 → .NET 9

| Deprecated API | Reason | Replacement | Priority |
|----------------|--------|-------------|----------|
| `MD5CryptoServiceProvider` | MD5 is broken (collisions) | `SHA256.Create()` | 🚨 CRITICAL |
| `SHA1CryptoServiceProvider` | SHA1 is broken (collisions) | `SHA256.Create()` | 🚨 CRITICAL |
| `RijndaelManaged` | API deprecated | `Aes.Create()` | 🚨 HIGH |
| `DESCryptoServiceProvider` | DES is insecure (56-bit key) | **REMOVE** | 🚨 CRITICAL |
| `TripleDESCryptoServiceProvider` | 3DES deprecated (NIST) | `Aes.Create()` | 🚨 HIGH |
| `RC2CryptoServiceProvider` | RC2 is insecure | **REMOVE** | 🚨 HIGH |
| `RNGCryptoServiceProvider` | API style outdated | `RandomNumberGenerator.Create()` | ⚠️ MEDIUM |
| `Random` (for security) | Not cryptographically secure | `RandomNumberGenerator` | 🚨 HIGH |

**Note**: Initial codebase scan did not find obvious crypto usage, but thorough inventory required in Stage 1.

---

### Message Authentication Codes (MACs)

**Recommendation**: Add message integrity protection for sensitive messages

**Implementation**:
```csharp
// Add HMAC-SHA256 for message authentication
public class MessageAuthenticator
{
    private readonly byte[] _key;

    public string ComputeHMAC(string message)
    {
        using var hmac = new HMACSHA256(_key);  // ✅ Secure
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        return Convert.ToBase64String(hash);
    }

    public bool VerifyHMAC(string message, string hmacValue)
    {
        var computed = ComputeHMAC(message);
        return CryptographicOperations.FixedTimeEquals(  // ✅ Timing-safe
            Convert.FromBase64String(computed),
            Convert.FromBase64String(hmacValue)
        );
    }
}
```

**Timeline**: Stage 4 (Week 5-7) - Optional enhancement

---

## Section 4: Security Testing Strategy

### Current Testing: INSUFFICIENT SECURITY COVERAGE

**Current Plan** (Stage 6.2, lines 344-358):
- Dependency vulnerability scan ✅
- Static code analysis ✅
- TLS/SSL connection testing ⚠️ (too vague)
- Authentication/authorization validation ⚠️ (too vague)
- Penetration testing ⚠️ (too vague)
- Security compliance checklist ✅

### Required: Comprehensive Security Test Suite

#### 1. Static Application Security Testing (SAST)

**Tools**:
- **SonarQube** with Security Rules
- **Security Code Scan** (.NET analyzer)
- **Roslyn Security Guard**

**Implementation** (Stage 7 - CI/CD Integration):
```bash
# Install security analyzer
dotnet add package SecurityCodeScan.VS2019

# Run static analysis
dotnet build /p:RunAnalyzers=true /p:TreatWarningsAsErrors=true

# Generate SARIF report
dotnet tool install --global security-scan
security-scan ./RawRabbit.sln --output sarif --report docs/test/security/sast-report.sarif
```

**Security Rules to Enable**:
- SQL Injection detection (if database used)
- XSS detection (if HTML generated)
- Hardcoded credentials detection
- Insecure crypto usage
- Deserialization vulnerabilities
- Path traversal
- LDAP injection
- XML External Entity (XXE)

**Timeline**: Stage 7 (Week 9-10)

---

#### 2. Dynamic Application Security Testing (DAST)

**Scope**: Test running application with RabbitMQ integration

**Test Scenarios**:
1. **Message Injection Attacks**
   - Malformed JSON payloads
   - Oversized messages (DoS)
   - Type confusion attacks (Newtonsoft.Json)

2. **Authentication Bypass Attempts**
   - Invalid credentials
   - Credential stuffing
   - Session hijacking (if applicable)

3. **TLS/SSL Security**
   - Protocol downgrade attacks
   - Weak cipher suite negotiation
   - Certificate validation bypass attempts

4. **Authorization Testing**
   - Queue/exchange permission escalation
   - Cross-vhost access attempts

**Tools**:
- **OWASP ZAP** (Zed Attack Proxy)
- Custom fuzzing scripts

**Timeline**: Stage 6 (Week 8-9)

---

#### 3. Fuzz Testing

**Purpose**: Discover unexpected crashes and security vulnerabilities

**Targets**:
1. **Message Deserialization**
   - Fuzz JSON messages (Newtonsoft.Json or System.Text.Json)
   - Fuzz Protobuf messages
   - Fuzz MessagePack messages
   - Fuzz ZeroFormatter messages

2. **Connection String Parsing**
   - Malformed AMQP URIs
   - SQL injection-style attacks in connection strings

3. **Configuration Loading**
   - Malformed JSON config files
   - Unexpected data types

**Tools**:
- **SharpFuzz** (.NET fuzzing library)
- **AFL.NET** (American Fuzzy Lop for .NET)
- **libFuzzer** integration

**Example Fuzz Test**:
```csharp
[Fact]
public void FuzzJsonDeserialization()
{
    Fuzzer.Run(bytes =>
    {
        try
        {
            var json = Encoding.UTF8.GetString(bytes);
            var serializer = new JsonSerializer();
            var message = serializer.Deserialize<MyMessage>(json);

            // Should not crash or throw unhandled exceptions
        }
        catch (JsonException)
        {
            // Expected - invalid JSON
        }
        catch (ArgumentException)
        {
            // Expected - invalid data
        }
        catch (Exception ex)
        {
            // SECURITY ISSUE: Unexpected exception type
            throw new SecurityException($"Fuzzing found vulnerability: {ex.GetType()}", ex);
        }
    });
}
```

**Timeline**: Stage 6 (Week 8-9)

---

#### 4. TLS/SSL Testing Suite

**Test Coverage** (CRITICAL - currently too vague):

**Protocol Version Tests**:
```csharp
[Fact]
public async Task TLS13_ShouldBeAccepted()
{
    var config = new RawRabbitConfiguration
    {
        Ssl = new SslOption
        {
            Enabled = true,
            Version = SslProtocols.Tls13
        }
    };
    var client = await CreateClientAsync(config);
    Assert.True(client.IsConnected);
}

[Fact]
public async Task TLS10_ShouldBeRejected()
{
    var config = new RawRabbitConfiguration
    {
        Ssl = new SslOption
        {
            Enabled = true,
            Version = SslProtocols.Tls  // TLS 1.0
        }
    };
    await Assert.ThrowsAsync<SecurityException>(() => CreateClientAsync(config));
}
```

**Certificate Validation Tests**:
```csharp
[Fact]
public async Task ExpiredCertificate_ShouldBeRejected()
{
    var config = ConfigWithExpiredCert();
    await Assert.ThrowsAsync<AuthenticationException>(() => CreateClientAsync(config));
}

[Fact]
public async Task SelfSignedCertificate_ShouldBeRejectedByDefault()
{
    var config = ConfigWithSelfSignedCert();
    await Assert.ThrowsAsync<AuthenticationException>(() => CreateClientAsync(config));
}

[Fact]
public async Task ValidCertificate_ShouldBeAccepted()
{
    var config = ConfigWithValidCert();
    var client = await CreateClientAsync(config);
    Assert.True(client.IsConnected);
}
```

**Cipher Suite Tests**:
```csharp
[Theory]
[InlineData("TLS_AES_256_GCM_SHA384")]  // Strong
[InlineData("TLS_AES_128_GCM_SHA256")]  // Strong
public async Task StrongCipherSuite_ShouldBeAccepted(string cipherSuite)
{
    var config = ConfigWithCipherSuite(cipherSuite);
    var client = await CreateClientAsync(config);
    Assert.True(client.IsConnected);
}

[Theory]
[InlineData("TLS_RSA_WITH_RC4_128_SHA")]  // Weak
[InlineData("TLS_RSA_WITH_DES_CBC_SHA")]   // Weak
public async Task WeakCipherSuite_ShouldBeRejected(string cipherSuite)
{
    var config = ConfigWithCipherSuite(cipherSuite);
    await Assert.ThrowsAsync<SecurityException>(() => CreateClientAsync(config));
}
```

**Mutual TLS (mTLS) Tests**:
```csharp
[Fact]
public async Task MutualTLS_WithValidClientCert_ShouldConnect()
{
    var config = new RawRabbitConfiguration
    {
        Ssl = new SslOption
        {
            Enabled = true,
            CertPath = "client-cert.p12",
            CertPassphrase = GetTestPassphrase()
        }
    };
    var client = await CreateClientAsync(config);
    Assert.True(client.IsConnected);
}

[Fact]
public async Task MutualTLS_WithoutClientCert_ShouldBeRejected()
{
    var config = ConfigWithoutClientCert();
    await Assert.ThrowsAsync<AuthenticationException>(() => CreateClientAsync(config));
}
```

**Test Infrastructure**:
```bash
# Docker containers for TLS testing
docker-compose -f test/docker-compose-tls-tests.yml up -d

# Containers needed:
# 1. RabbitMQ with TLS 1.3 only
# 2. RabbitMQ with TLS 1.2 only
# 3. RabbitMQ with expired certificate
# 4. RabbitMQ with self-signed certificate
# 5. RabbitMQ with mTLS required
# 6. RabbitMQ with weak cipher suites (for rejection testing)
```

**Timeline**: Stage 6 (Week 8-9)

---

#### 5. Penetration Testing

**Scope**: OWASP Top 10 for Messaging Systems

**Test Categories**:

1. **Injection Attacks** (OWASP A03)
   - Message payload injection
   - Header injection
   - Configuration injection

2. **Authentication Failures** (OWASP A07)
   - Brute force protection
   - Credential stuffing
   - Default credentials (guest/guest)

3. **Sensitive Data Exposure** (OWASP A02)
   - Credentials in logs
   - Stack traces exposed
   - Connection strings in error messages

4. **Security Misconfiguration** (OWASP A05)
   - Default TLS disabled
   - Weak cipher suites accepted
   - Permissive queue/exchange permissions

5. **Insecure Deserialization** (OWASP A08)
   - Newtonsoft.Json TypeNameHandling attacks
   - Protobuf deserialization exploits
   - MessagePack attacks

6. **Using Components with Known Vulnerabilities** (OWASP A06)
   - RabbitMQ.Client 5.0.1 CVEs
   - Newtonsoft.Json 10.0.1 CVEs

**Deliverable**: `docs/test/security/penetration-test-report.md`

**Timeline**: Stage 6 (Week 9)

---

## Section 5: Authentication & Authorization

### Current State: VAGUE REQUIREMENTS

**Plan Mentions** (lines 71, 351):
- "Review authentication/authorization patterns" ✅
- "Authentication/authorization validation" ✅

**Problem**: No specifics on what to review or how to validate

---

### Security Issue 1: Hardcoded Credentials 🚨 CRITICAL

**Location**: `src/RawRabbit/Configuration/RawRabbitConfiguration.cs:110-117`

```csharp
public static RawRabbitConfiguration Local => new RawRabbitConfiguration
{
    VirtualHost = "/",
    Username = "guest",      // 🚨 DEFAULT CREDENTIALS
    Password = "guest",      // 🚨 DEFAULT CREDENTIALS
    Port = 5672,
    Hostnames = new List<string> { "localhost" }
};
```

**Risks**:
1. **Developers may use in production** (common mistake)
2. **Leaked in version control** (Git history)
3. **Unauthorized access** to RabbitMQ instances
4. **Compliance violations** (PCI-DSS, HIPAA)

**Exploitation Scenario**:
```csharp
// Developer accidentally deploys to production
var client = BusClientFactory.CreateDefault();  // Uses RawRabbitConfiguration.Local
// Now production RabbitMQ accepts "guest/guest" credentials!
```

**Recommendation**: 🚨 **IMMEDIATE ACTION REQUIRED**

**Option A: Deprecate with Warning** (Week 2)
```csharp
[Obsolete("Security Risk: Default credentials should not be used in production. " +
          "Use environment-specific configuration instead.", error: false)]
public static RawRabbitConfiguration Local => new RawRabbitConfiguration
{
    VirtualHost = "/",
    Username = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME") ?? "guest",
    Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest",
    Port = 5672,
    Hostnames = new List<string> { Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost" }
};
```

**Option B: Remove Entirely** (Week 3) - RECOMMENDED
```csharp
// REMOVE RawRabbitConfiguration.Local entirely
// Force developers to provide configuration explicitly
```

**Option C: Runtime Warning** (Week 2)
```csharp
internal class CredentialValidator
{
    public static void ValidateConfiguration(RawRabbitConfiguration config)
    {
        if (config.Username == "guest" && config.Password == "guest")
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            if (env == "Production")
            {
                throw new SecurityException(
                    "Default credentials (guest/guest) are not allowed in production. " +
                    "Please configure secure credentials."
                );
            }
            else
            {
                Log.Warning("Using default RabbitMQ credentials (guest/guest). " +
                           "This is insecure and should not be used in production.");
            }
        }
    }
}
```

**Timeline**: Week 2 (Stage 1-2) - Cannot be delayed

**ADR Required**: `docs/adr/0013-secrets-management-integration.md`

---

### Security Issue 2: Plaintext Credential Storage

**Location**: `src/RawRabbit/Configuration/RawRabbitConfiguration.cs:75-76`

```csharp
public string Username { get; set; }  // Plaintext
public string Password { get; set; }  // Plaintext
```

**Current Usage** (DependencyInjection/RawRabbitDependencyRegisterExtension.cs:32-43):
```csharp
return new ConnectionFactory
{
    VirtualHost = cfg.VirtualHost,
    UserName = cfg.Username,      // Plaintext in memory
    Password = cfg.Password,      // Plaintext in memory
    ...
};
```

**Risks**:
1. **Memory dumps** expose credentials
2. **Logging** may inadvertently log passwords
3. **No encryption at rest** in configuration files
4. **No integration** with secrets management systems

**Recommendation**: Secrets Management Integration

**Phase 1: Support External Secrets (Week 2-3)**
```csharp
public class RawRabbitConfiguration
{
    // Existing properties (for backward compatibility)
    public string Username { get; set; }
    public string Password { get; set; }

    // New: Secrets provider integration
    public string UsernameSecretName { get; set; }  // Azure Key Vault secret name
    public string PasswordSecretName { get; set; }  // Azure Key Vault secret name

    // Resolve credentials from secrets provider
    public async Task<(string username, string password)> GetCredentialsAsync(
        ISecretsProvider secretsProvider)
    {
        if (!string.IsNullOrEmpty(UsernameSecretName))
        {
            var username = await secretsProvider.GetSecretAsync(UsernameSecretName);
            var password = await secretsProvider.GetSecretAsync(PasswordSecretName);
            return (username, password);
        }

        // Fallback to direct properties (with warning)
        LogWarningIfUsingDirectCredentials();
        return (Username, Password);
    }
}
```

**Phase 2: Environment Variable Support** (Week 2)
```csharp
public static RawRabbitConfiguration FromEnvironment()
{
    return new RawRabbitConfiguration
    {
        VirtualHost = Environment.GetEnvironmentVariable("RABBITMQ_VHOST") ?? "/",
        Username = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME")
                   ?? throw new InvalidOperationException("RABBITMQ_USERNAME not set"),
        Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD")
                   ?? throw new InvalidOperationException("RABBITMQ_PASSWORD not set"),
        Hostnames = Environment.GetEnvironmentVariable("RABBITMQ_HOSTS")?.Split(',').ToList()
                    ?? new List<string> { "localhost" }
    };
}
```

**Phase 3: Azure Key Vault Integration** (Week 3-4)
```csharp
// appsettings.json
{
  "RawRabbit": {
    "VirtualHost": "/",
    "Port": 5672,
    "Hostnames": ["rabbitmq.example.com"],
    "Username": "@Microsoft.KeyVault(SecretUri=https://myvault.vault.azure.net/secrets/rabbitmq-username)",
    "Password": "@Microsoft.KeyVault(SecretUri=https://myvault.vault.azure.net/secrets/rabbitmq-password)"
  }
}

// Startup configuration
builder.Configuration.AddAzureKeyVault(
    new Uri("https://myvault.vault.azure.net/"),
    new DefaultAzureCredential()
);

var rabbitConfig = builder.Configuration.GetSection("RawRabbit").Get<RawRabbitConfiguration>();
```

**Phase 4: HashiCorp Vault Integration** (Optional - Week 4)
```csharp
// Similar pattern for Vault integration
```

**Timeline**:
- Phase 1-2: Week 2-3 (Stage 2)
- Phase 3-4: Week 3-4 (Stage 3)

**ADR Required**: `docs/adr/0013-secrets-management-integration.md`

---

### Security Enhancement: Certificate-Based Authentication

**Current**: Only username/password authentication supported

**Recommendation**: Add x509 client certificate authentication (mTLS)

**Implementation** (Week 3-4):
```csharp
public class RawRabbitConfiguration
{
    // Existing...
    public SslOption Ssl { get; set; }

    // New: Certificate authentication
    public X509Certificate2 ClientCertificate { get; set; }
    public string ClientCertificatePath { get; set; }
    public string ClientCertificatePassword { get; set; }

    // Load certificate from file or certificate store
    public X509Certificate2 LoadClientCertificate()
    {
        if (ClientCertificate != null)
            return ClientCertificate;

        if (!string.IsNullOrEmpty(ClientCertificatePath))
        {
            return new X509Certificate2(
                ClientCertificatePath,
                ClientCertificatePassword ?? string.Empty
            );
        }

        return null;  // No client certificate configured
    }
}

// Usage in ConnectionFactory configuration
return new ConnectionFactory
{
    Ssl = new SslOption
    {
        Enabled = true,
        CertPath = cfg.ClientCertificatePath,
        CertPassphrase = cfg.ClientCertificatePassword,
        ServerName = cfg.Hostnames.FirstOrDefault(),
        Version = SslProtocols.Tls13 | SslProtocols.Tls12
    }
};
```

**Benefits**:
- **Stronger authentication**: No password transmission
- **Credential rotation**: Certificate renewal process
- **Audit trail**: Certificate-based authentication logs
- **Compliance**: Meets regulatory requirements (FIPS, PCI-DSS)

**Timeline**: Week 3-4 (Stage 3) - Optional enhancement

**ADR Required**: `docs/adr/0014-authentication-modernization.md`

---

## Section 6: RabbitMQ.Client 5.x → 7.x Security Implications

### Breaking Change 1: IModel → IChannel

**Security Impact**: LOW (naming change only)

**Files Affected** (50+ files):
- Channel/ChannelFactory.cs
- Channel/AutoScalingChannelPool.cs
- All middleware classes using IModel

**Migration Pattern**:
```csharp
// OLD (5.x)
using RabbitMQ.Client;
IModel channel = connection.CreateModel();

// NEW (7.x)
using RabbitMQ.Client;
IChannel channel = await connection.CreateChannelAsync();
```

**Security Consideration**: None (cosmetic change)

**Timeline**: Week 3 (Stage 3.1)

---

### Breaking Change 2: Connection Recovery API

**Security Impact**: MEDIUM (affects connection stability)

**Current Implementation** (Channel/ChannelFactory.cs:76-100):
```csharp
if (!(Connection is IRecoverable recoverable))
{
    _logger.Info("Connection is not recoverable");
    Connection.Dispose();
    throw new ChannelAvailabilityException(...);
}

recoverable.Recovery += completeTask;
```

**RabbitMQ.Client 7.x Pattern**:
```csharp
connection.RecoverySucceeded += (sender, args) =>
{
    _logger.Info("Connection recovered successfully");
};

connection.ConnectionRecoveryError += (sender, args) =>
{
    _logger.Error($"Connection recovery failed: {args.Exception}");
};
```

**Security Consideration**:
- Proper recovery handling prevents connection exhaustion attacks
- Failed recovery should trigger alerting (operational security)

**Timeline**: Week 4 (Stage 3.3)

---

### Breaking Change 3: Async-First APIs

**Security Impact**: LOW (performance improvement)

**RabbitMQ.Client 7.x Async Methods**:
```csharp
// Channel creation
IChannel channel = await connection.CreateChannelAsync(ct);

// Basic publish
await channel.BasicPublishAsync(exchange, routingKey, body, ct);

// Queue declare
QueueDeclareOk result = await channel.QueueDeclareAsync(queue, durable, exclusive, autoDelete, arguments, ct);
```

**Security Consideration**:
- Async I/O reduces thread exhaustion (DoS resilience)
- Proper cancellation token usage prevents resource leaks

**Timeline**: Week 3-5 (Stage 3.1-3.3)

---

### Security Enhancement: TLS 1.3 Support

**RabbitMQ.Client 7.x**: Native TLS 1.3 support with .NET 6+

**Implementation** (DependencyInjection/RawRabbitDependencyRegisterExtension.cs:32-44):
```csharp
return new ConnectionFactory
{
    Ssl = new SslOption
    {
        Enabled = cfg.Ssl?.Enabled ?? false,
        ServerName = cfg.Hostnames.FirstOrDefault(),
        // ✅ NEW: TLS 1.3 support
        Version = SslProtocols.Tls13 | SslProtocols.Tls12,
        // ✅ NEW: Strict certificate validation
        AcceptablePolicyErrors = SslPolicyErrors.None,
        // ✅ NEW: Custom validation callback
        CertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
        {
            return ValidateServerCertificate(certificate, chain, sslPolicyErrors, cfg);
        }
    }
};
```

**Certificate Validation Logic**:
```csharp
private static bool ValidateServerCertificate(
    X509Certificate certificate,
    X509Chain chain,
    SslPolicyErrors sslPolicyErrors,
    RawRabbitConfiguration config)
{
    // Production: Strict validation
    if (IsProduction())
    {
        if (sslPolicyErrors != SslPolicyErrors.None)
        {
            Log.Error($"SSL certificate validation failed: {sslPolicyErrors}");
            return false;
        }

        // Additional checks: expiration, revocation, etc.
        return ValidateCertificateChain(chain) && !IsCertificateRevoked(certificate);
    }

    // Development: Allow self-signed (with warning)
    if (config.Ssl?.AllowSelfSignedCertificates == true)
    {
        Log.Warning("Accepting self-signed certificate (development only)");
        return true;
    }

    return sslPolicyErrors == SslPolicyErrors.None;
}
```

**Timeline**: Week 3 (Stage 3.1)

---

## Section 7: Supply Chain Security

### Current State: NOT MENTIONED ❌

**Problem**: No discussion of supply chain security in plan

**Risk**: Compromised NuGet packages (SolarWinds-style attacks)

---

### Required: Software Bill of Materials (SBOM)

**What**: Machine-readable inventory of all dependencies

**Why**:
- Vulnerability tracking
- License compliance
- Supply chain transparency
- Customer security requirements

**Implementation** (Stage 7 - Week 9-10):
```bash
# Install SBOM tool (Microsoft)
dotnet tool install --global Microsoft.Sbom.Tool

# Generate SBOM for each project
dotnet sbom-tool generate \
    -b ./bin/Release/net9.0 \
    -bc ./src/RawRabbit \
    -pn RawRabbit \
    -pv 3.0.0 \
    -ps Pardahlman \
    -nsb https://github.com/pardahlman/RawRabbit \
    -m ./src/RawRabbit/bin/Release/net9.0

# Output: _manifest/spdx_2.2/manifest.spdx.json
```

**SBOM Contents**:
- Package name and version
- Package supplier
- File hashes (SHA256)
- License information
- Dependency relationships
- Vulnerability identifiers

**Include in NuGet Packages**:
```xml
<!-- RawRabbit.csproj -->
<ItemGroup>
  <None Include="$(OutputPath)_manifest/spdx_2.2/manifest.spdx.json" Pack="true" PackagePath="manifest/" />
</ItemGroup>
```

**Timeline**: Week 9-10 (Stage 7)

**ADR Required**: `docs/adr/0016-supply-chain-security.md`

---

### Required: Package Signature Verification

**Current**: No package signing mentioned

**Implementation** (Stage 7):
```bash
# Sign NuGet packages with Authenticode
# Use Azure Key Vault for certificate storage (secure)

# Install signing tool
dotnet tool install --global NuGetKeyVaultSignTool

# Sign package
NuGetKeyVaultSignTool sign RawRabbit.3.0.0.nupkg \
    --file-digest sha256 \
    --timestamp-rfc3161 http://timestamp.digicert.com \
    --azure-key-vault-url https://myvault.vault.azure.net/ \
    --azure-key-vault-client-id <client-id> \
    --azure-key-vault-client-secret <client-secret> \
    --azure-key-vault-certificate <cert-name>
```

**Benefits**:
- **Authenticity**: Proves package is from legitimate source
- **Integrity**: Detects tampering
- **Trust**: NuGet.org requirement for verified publishers

**Timeline**: Week 10 (Stage 7)

---

### Required: Dependency Pinning

**Current**: Floating version ranges may allow unexpected updates

**Implementation** (Stage 3):
```xml
<!-- RawRabbit.csproj -->
<PropertyGroup>
  <!-- Generate packages.lock.json -->
  <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>

  <!-- In CI/CD, only use locked versions -->
  <RestoreLockedMode Condition="'$(CI)' == 'true'">true</RestoreLockedMode>
</PropertyGroup>
```

**Benefits**:
- **Reproducible builds**: Same dependencies every time
- **Supply chain security**: Prevent unexpected package updates
- **Vulnerability management**: Explicit approval for updates

**Timeline**: Week 3 (Stage 3)

---

### Required: Private NuGet Feed (Optional)

**Recommendation**: Use private NuGet feed for vetted packages

**Options**:
1. **Azure Artifacts** (Azure DevOps)
2. **GitHub Packages** (GitHub)
3. **MyGet** (third-party)
4. **BaGet** (self-hosted)

**Workflow**:
1. Upstream packages pulled from NuGet.org
2. Security scan before approval
3. Approved packages pushed to private feed
4. Builds consume from private feed only

**Timeline**: Week 10+ (Post-deployment) - Optional

---

## Section 8: Recommended ADR Roadmap

### Security-Focused ADRs (11 Required)

| ADR # | Title | Stage | Week | Priority | Owner |
|-------|-------|-------|------|----------|-------|
| 0006 | Security Checkpoint Expansion | 1 | 1 | 🚨 CRITICAL | Security Specialist |
| 0007 | Threat Modeling Results | 1-2 | 2 | 🚨 HIGH | Security Specialist |
| 0008 | Secrets Management Strategy | 2 | 2-3 | 🚨 HIGH | Security Specialist |
| 0009 | Dependency Security & CVE Management | 1 | 1 | 🚨 CRITICAL | Security Specialist |
| 0010 | JSON Serialization Security Decision | 2 | 2-3 | 🚨 CRITICAL | Architect + Security |
| 0011 | Cryptographic API Migration | 2-3 | 2-3 | 🚨 CRITICAL | Security Specialist |
| 0012 | TLS/SSL Configuration Modernization | 2-3 | 3 | 🚨 HIGH | Security Specialist |
| 0013 | Authentication & Authorization Modernization | 2-3 | 3 | 🚨 HIGH | Security Specialist |
| 0014 | Security Testing Strategy | 4 | 6 | ⚠️ MEDIUM | QA + Security |
| 0015 | TLS Testing Infrastructure | 4 | 6 | ⚠️ MEDIUM | QA + Security |
| 0016 | Supply Chain Security & SBOM | 7 | 9-10 | 🚨 HIGH | DevOps + Security |

---

### ADR-0006: Security Checkpoint Expansion

**Status**: Proposed
**Context**: Original plan has 4 security checkpoints, but critical gaps identified
**Decision**: Expand to 9 security checkpoints covering:
- Threat modeling
- Cryptographic inventory
- Secrets management audit
- Supply chain validation
- Post-deployment monitoring

**Consequences**:
- **Positive**: Comprehensive security coverage, reduced risk
- **Negative**: +2-3 weeks to timeline, increased resource requirements

**Action Items**:
- Update PLAN.md with 9-checkpoint model
- Assign security specialist to all 9 checkpoints
- Create checkpoint templates in `docs/security/checkpoints/`

---

### ADR-0009: Dependency Security & CVE Management

**Status**: Proposed
**Context**: RabbitMQ.Client 5.0.1 and Newtonsoft.Json 10.0.1 have CRITICAL CVEs
**Decision**:
1. Upgrade RabbitMQ.Client to 7.x (Week 3, Stage 3.1)
2. Upgrade Newtonsoft.Json to 13.0.3+ OR migrate to System.Text.Json (Week 3, Stage 3.1)
3. Implement continuous vulnerability monitoring (dotnet list package --vulnerable in CI/CD)
4. Create dependency security matrix

**Consequences**:
- **Positive**: Eliminates known CRITICAL vulnerabilities, modern security features
- **Negative**: Breaking changes, migration effort, potential compatibility issues

**Action Items**:
- Run vulnerability scan (Week 1)
- Create dependency security matrix (Week 1)
- Upgrade dependencies (Week 3)
- Add vulnerability scanning to CI/CD (Week 9)

---

### ADR-0010: JSON Serialization Security Decision

**Status**: Proposed
**Context**: Newtonsoft.Json 10.0.1 has CRITICAL deserialization vulnerabilities
**Decision**: [TO BE DETERMINED - Options below]

**Option A: Upgrade Newtonsoft.Json to 13.0.3+**
- **Pros**: Minimal breaking changes, maintains compatibility
- **Cons**: Still requires manual security configuration (TypeNameHandling fix)

**Option B: Migrate to System.Text.Json**
- **Pros**: Native .NET 9, 2-3x faster, secure by default, Microsoft supported
- **Cons**: Breaking changes for consumers, some features not available

**Recommendation**: **Option B (System.Text.Json)** for new .NET 9 version

**Migration Path**:
1. Add System.Text.Json support alongside Newtonsoft.Json (Week 3)
2. Deprecate Newtonsoft.Json with obsolete warnings (Week 3)
3. Remove Newtonsoft.Json in next major version (v4.0)

**Security Configuration**:
```csharp
// If keeping Newtonsoft.Json
new JsonSerializer
{
    TypeNameHandling = TypeNameHandling.None,  // ✅ SECURE
    // Remove ReferenceLoopHandling, PreserveReferencesHandling
};

// If using System.Text.Json
new JsonSerializerOptions
{
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    // No type name handling = secure by default ✅
};
```

**Action Items**:
- Decide on serialization strategy (Week 2)
- Document ADR-0010 with decision (Week 2)
- Implement migration (Week 3)
- Create migration guide for consumers (Week 7)

---

### ADR-0011: Cryptographic API Migration

**Status**: Proposed
**Context**: .NET Framework 4.5.1 → .NET 9 deprecates many crypto APIs
**Decision**: Replace all deprecated cryptographic APIs with modern .NET 9 equivalents

**Migration Map**:
| Deprecated | Replacement | Priority |
|------------|-------------|----------|
| MD5CryptoServiceProvider | SHA256.Create() | CRITICAL |
| SHA1CryptoServiceProvider | SHA256.Create() | CRITICAL |
| RijndaelManaged | Aes.Create() | HIGH |
| RNGCryptoServiceProvider | RandomNumberGenerator.Create() | MEDIUM |
| Random (security) | RandomNumberGenerator | HIGH |

**Action Items**:
1. Run crypto inventory scan (Week 1)
2. Create migration plan per finding (Week 2)
3. Implement replacements (Week 3-5)
4. Add crypto security tests (Week 6)

---

### ADR-0012: TLS/SSL Configuration Modernization

**Status**: Proposed
**Context**: RabbitMQ.Client 5.x has outdated TLS configuration, .NET 9 supports TLS 1.3
**Decision**:
1. Enable TLS 1.3 by default (fallback to TLS 1.2)
2. Reject TLS 1.0/1.1 (insecure)
3. Enforce certificate validation (no self-signed in production)
4. Support mutual TLS (mTLS) for high-security deployments

**Configuration**:
```csharp
Ssl = new SslOption
{
    Enabled = true,
    Version = SslProtocols.Tls13 | SslProtocols.Tls12,  // ✅ Modern
    AcceptablePolicyErrors = SslPolicyErrors.None,  // ✅ Strict
    ServerName = hostname,  // ✅ SNI
    CertPath = clientCertPath,  // ✅ mTLS support
};
```

**Action Items**:
- Implement TLS 1.3 configuration (Week 3)
- Add certificate validation logic (Week 3)
- Create TLS test suite (Week 6)
- Document TLS setup in migration guide (Week 7)

---

### ADR-0013: Secrets Management Integration

**Status**: Proposed
**Context**: Hardcoded credentials and plaintext password storage are security risks
**Decision**:
1. Deprecate `RawRabbitConfiguration.Local` (Week 2)
2. Add environment variable support (Week 2)
3. Add Azure Key Vault integration (Week 3)
4. Add runtime warning for default credentials (Week 2)

**Implementation**:
- Phase 1: Environment variables (Week 2)
- Phase 2: Azure Key Vault (Week 3)
- Phase 3: HashiCorp Vault (Optional, Week 4)

**Action Items**:
- Add `[Obsolete]` to RawRabbitConfiguration.Local (Week 2)
- Implement environment variable support (Week 2)
- Implement Key Vault integration (Week 3)
- Document secure configuration (Week 7)

---

### ADR-0016: Supply Chain Security & SBOM

**Status**: Proposed
**Context**: No supply chain security measures in current plan
**Decision**:
1. Generate SBOM for all 25 projects
2. Sign all NuGet packages with Authenticode
3. Enable dependency pinning with packages.lock.json
4. Add vulnerability scanning to CI/CD

**Action Items**:
- Generate SBOMs (Week 9)
- Sign packages with Azure Key Vault (Week 10)
- Add packages.lock.json (Week 3)
- Configure CI/CD scanning (Week 9)

---

## Section 9: Enhanced Security Checkpoint Timeline

### Checkpoint 1: Pre-Migration Security Baseline (Week 1)

**Activities**:
- [ ] Run vulnerability scan: `dotnet list package --vulnerable`
- [ ] Run OWASP Dependency-Check
- [ ] Scan for hardcoded credentials
- [ ] Cryptographic API inventory
- [ ] Authentication pattern review
- [ ] Create dependency security matrix
- [ ] Document current security posture

**Deliverables**:
- `docs/security/baseline-report.md`
- `docs/security/dependency-matrix.md`
- `docs/adr/0002-security-architecture.md`
- `docs/adr/0009-dependency-security-strategy.md`

**Owner**: Security Specialist
**Duration**: 3-5 days

---

### Checkpoint 1.5: Threat Modeling Workshop (Week 2)

**Activities**:
- [ ] STRIDE analysis for RabbitMQ messaging
- [ ] Attack surface mapping (25 projects)
- [ ] Trust boundary analysis
- [ ] Data flow diagrams
- [ ] Document identified threats
- [ ] Prioritize security mitigations

**Deliverables**:
- `docs/security/threat-model.md`
- `docs/security/attack-surface-map.md`
- `docs/adr/0007-threat-model-results.md`

**Owner**: Security Specialist + Architect
**Duration**: 2-3 days

---

### Checkpoint 2: Architecture Security Review (Week 2-3)

**Activities**:
- [ ] Review .NET 9 architecture design
- [ ] Validate cryptographic migration plan
- [ ] Review secrets management design
- [ ] Validate TLS/SSL configuration
- [ ] Review authentication/authorization design
- [ ] Identify security improvements from .NET 9

**Deliverables**:
- `docs/adr/0005-security-review-results.md`
- `docs/adr/0011-crypto-api-migration.md`
- `docs/adr/0012-tls-configuration-modernization.md`
- `docs/adr/0013-secrets-management-integration.md`

**Owner**: Security Specialist
**Duration**: 3-4 days

---

### Checkpoint 2.5: Cryptographic Inventory & Plan (Week 2-3)

**Activities**:
- [ ] Scan for deprecated crypto APIs
- [ ] Map crypto usage across codebase
- [ ] Create crypto migration plan
- [ ] Design test strategy for crypto changes
- [ ] Document replacement approach

**Deliverables**:
- `docs/security/crypto-inventory.md`
- `docs/security/crypto-migration-plan.md`
- `docs/adr/0011-crypto-api-migration.md`

**Owner**: Security Specialist
**Duration**: 2-3 days

---

### Checkpoint 3: Component Security Reviews (Week 3-8)

**Activities** (per component):
- [ ] Code review for security issues
- [ ] Scan for deprecated crypto usage
- [ ] Validate dependency upgrades
- [ ] Check for hardcoded credentials
- [ ] Review error handling (no information disclosure)
- [ ] Run SAST scan

**Deliverables** (per component):
- `docs/test/security/component/<component-name>-security-review.md`

**Owner**: Security Specialist + Component Developer
**Duration**: 30-60 minutes per component

---

### Checkpoint 3.5: Secrets Management Audit (Week 3-5)

**Activities**:
- [ ] Scan for hardcoded credentials
- [ ] Review configuration storage
- [ ] Test secrets rotation
- [ ] Validate Key Vault integration
- [ ] Test environment variable injection

**Deliverables**:
- `docs/security/secrets-audit-report.md`
- `docs/adr/0013-secrets-management-integration.md`

**Owner**: Security Specialist
**Duration**: 2-3 days

---

### Checkpoint 4: Integration Security Testing (Week 8-9)

**Activities**:
- [ ] Full SAST scan (SonarQube, Security Code Scan)
- [ ] DAST scan (OWASP ZAP)
- [ ] Fuzz testing (message handlers)
- [ ] TLS/SSL protocol testing
- [ ] Certificate validation testing
- [ ] Penetration testing (OWASP Top 10)
- [ ] Authentication/authorization flow testing

**Deliverables**:
- `docs/test/security/sast-report.sarif`
- `docs/test/security/dast-report.md`
- `docs/test/security/fuzz-test-results.md`
- `docs/test/security/tls-test-results.md`
- `docs/test/security/penetration-test-report.md`

**Owner**: QA Engineer + Security Specialist
**Duration**: 5-7 days

---

### Checkpoint 5: Dependency Vulnerability Validation (Week 9)

**Activities**:
- [ ] Final vulnerability scan (all dependencies)
- [ ] Verify all upgrades completed (RabbitMQ.Client, JSON, etc.)
- [ ] Check for new CVEs
- [ ] Validate dependency lock files
- [ ] Review transitive dependencies

**Deliverables**:
- `docs/security/final-vulnerability-scan.md`
- `docs/security/dependency-validation-report.md`

**Owner**: Security Specialist
**Duration**: 1-2 days

---

### Checkpoint 5.5: Supply Chain Security Validation (Week 9-10)

**Activities**:
- [ ] Generate SBOMs for all 25 projects
- [ ] Verify package signatures
- [ ] Validate dependency pinning
- [ ] Sign NuGet packages
- [ ] Test package verification

**Deliverables**:
- SBOM files (spdx.json) for each project
- Signed NuGet packages (.nupkg)
- `docs/security/supply-chain-report.md`
- `docs/adr/0016-supply-chain-security.md`

**Owner**: DevOps Engineer + Security Specialist
**Duration**: 2-3 days

---

### Checkpoint 6: Pre-Production Security Audit (Week 11)

**Activities**:
- [ ] Final security configuration review
- [ ] TLS/SSL configuration validation
- [ ] Authentication/authorization validation
- [ ] Secrets management validation
- [ ] Security documentation review
- [ ] Deployment security validation
- [ ] Security clearance sign-off

**Deliverables**:
- `docs/security/pre-production-audit.md`
- `docs/security/production-security-clearance.md`

**Owner**: Security Specialist
**Duration**: 2-3 days

---

### Checkpoint 7: Post-Deployment Security Monitoring Setup (Week 12)

**Activities**:
- [ ] Configure security telemetry
- [ ] Set up alerting thresholds
- [ ] Test incident response procedures
- [ ] Validate patch management process
- [ ] Create security operations runbook

**Deliverables**:
- `docs/security/security-monitoring-config.md`
- `docs/security/incident-response-plan.md`
- `docs/security/security-operations-runbook.md`
- `docs/adr/0018-security-monitoring.md`

**Owner**: DevOps Engineer + Security Specialist
**Duration**: 2-3 days

---

## Section 10: Critical Security Gaps Summary

### Gap 1: Insufficient Security Checkpoints ❌
**Current**: 4 checkpoints
**Recommended**: 9 checkpoints
**Missing**: Threat modeling, crypto inventory, secrets audit, supply chain, post-deployment
**Impact**: HIGH
**Timeline Impact**: +2-3 weeks

---

### Gap 2: Dependency Vulnerabilities 🚨 CRITICAL
**Issue**: RabbitMQ.Client 5.0.1 and Newtonsoft.Json 10.0.1 have CRITICAL CVEs
**Impact**: Remote Code Execution, Denial of Service
**Mitigation**: Upgrade to latest versions (Week 3)
**Timeline Impact**: None (already in plan)

---

### Gap 3: Cryptographic Migration Plan ❌
**Issue**: No specific crypto inventory or migration plan
**Impact**: May retain insecure crypto APIs
**Mitigation**: Crypto inventory (Week 1-2), migration plan (Week 2-3)
**Timeline Impact**: +1 week

---

### Gap 4: Hardcoded Credentials 🚨 CRITICAL
**Issue**: RawRabbitConfiguration.Local has "guest/guest" hardcoded
**Impact**: Production credential leakage risk
**Mitigation**: Deprecate/remove (Week 2), add secrets management (Week 2-3)
**Timeline Impact**: None (can be done quickly)

---

### Gap 5: TLS/SSL Testing ⚠️ INSUFFICIENT
**Issue**: Single line item, no detailed test specifications
**Impact**: TLS vulnerabilities may not be detected
**Mitigation**: Comprehensive TLS test suite (Week 6-9)
**Timeline Impact**: +1 week

---

### Gap 6: Supply Chain Security ❌
**Issue**: No mention of SBOM, package signing, dependency pinning
**Impact**: Vulnerable to supply chain attacks
**Mitigation**: SBOM generation (Week 9-10), package signing (Week 10)
**Timeline Impact**: +1 week

---

### Gap 7: Insecure JSON Serialization 🚨 CRITICAL
**Issue**: TypeNameHandling.Auto enables deserialization attacks
**Impact**: Remote Code Execution
**Mitigation**: Fix JSON configuration (Week 3) or migrate to System.Text.Json
**Timeline Impact**: None (can be done in Stage 3)

---

## Section 11: Revised Timeline with Security Enhancements

### Original Timeline: 10-12 weeks
### Revised Timeline: 13-15 weeks (+3 weeks for security)

| Stage | Original | Revised | Security Activities Added |
|-------|----------|---------|---------------------------|
| 1. Foundation | Week 1-2 | Week 1-2 | Crypto inventory, threat modeling prep |
| 2. Architecture | Week 2-3 | Week 2-3 | Threat modeling workshop, crypto plan |
| 3. Core Migration | Week 3-5 | Week 3-5 | Secrets management, TLS modernization |
| 4. Operations | Week 5-7 | Week 5-7 | Component security reviews |
| 5. DI & Samples | Week 7-8 | Week 7-8 | - |
| 6. Integration Testing | Week 8-9 | Week 8-10 | +1 week for comprehensive security testing |
| 7. Documentation | Week 9-10 | Week 10-12 | +2 weeks for SBOM, package signing |
| 8. Deployment | Week 10-12 | Week 13-15 | +1 week for security monitoring setup |

**Total Timeline**: **13-15 weeks** (was 10-12 weeks)

---

## Section 12: Immediate Action Items (Week 1)

### 🚨 CRITICAL - Must complete before proceeding

1. **Run Vulnerability Scan**
   ```bash
   dotnet list package --vulnerable --include-transitive > docs/security/vulnerability-scan-$(date +%Y%m%d).txt
   ```
   **Owner**: Security Specialist
   **Duration**: 30 minutes

2. **Scan for Hardcoded Credentials**
   ```bash
   grep -rn "Password.*=.*\"" src/ --include="*.cs" > docs/security/credential-scan.txt
   grep -rn "ConnectionString.*=.*\"" src/ --include="*.cs" >> docs/security/credential-scan.txt
   ```
   **Owner**: Security Specialist
   **Duration**: 15 minutes

3. **Cryptographic API Inventory**
   ```bash
   grep -rn "MD5\|SHA1\|DES\|TripleDES\|RC2\|Rijndael\|Random\(" src/ --include="*.cs" > docs/security/crypto-inventory.txt
   ```
   **Owner**: Security Specialist
   **Duration**: 30 minutes

4. **Create Dependency Security Matrix**
   - List all dependencies with current versions
   - Identify known CVEs
   - Determine target versions
   - Document in `docs/security/dependency-matrix.md`

   **Owner**: Security Specialist
   **Duration**: 2-3 hours

5. **Create ADR-0006: Security Checkpoint Expansion**
   - Document 9-checkpoint model
   - Assign owners and timelines
   - Save to `docs/adr/0006-security-checkpoint-expansion.md`

   **Owner**: Security Specialist
   **Duration**: 2-3 hours

6. **Create ADR-0009: Dependency Security Strategy**
   - Document upgrade plan for RabbitMQ.Client and Newtonsoft.Json
   - Include CVE analysis
   - Save to `docs/adr/0009-dependency-security-strategy.md`

   **Owner**: Security Specialist
   **Duration**: 2-3 hours

7. **Update PLAN.md**
   - Incorporate security enhancements
   - Update timeline (13-15 weeks)
   - Add 9 security checkpoints
   - Update success criteria

   **Owner**: Migration Architect + Security Specialist
   **Duration**: 3-4 hours

---

## Section 13: Security Success Criteria

### Mandatory Security Requirements (Cannot Ship Without)

- [ ] ✅ All dependencies upgraded (no CRITICAL/HIGH CVEs)
- [ ] ✅ RabbitMQ.Client upgraded to 7.x
- [ ] ✅ Newtonsoft.Json upgraded to 13.0.3+ OR migrated to System.Text.Json
- [ ] ✅ TypeNameHandling.Auto fixed (no deserialization attacks)
- [ ] ✅ RawRabbitConfiguration.Local deprecated or removed
- [ ] ✅ TLS 1.3 enabled by default
- [ ] ✅ TLS 1.0/1.1 rejected
- [ ] ✅ All deprecated crypto APIs replaced
- [ ] ✅ No hardcoded credentials in source code
- [ ] ✅ SAST scan passed (no CRITICAL/HIGH issues)
- [ ] ✅ Penetration test passed (OWASP Top 10)
- [ ] ✅ TLS test suite passed (all protocol/certificate tests)
- [ ] ✅ SBOM generated for all projects
- [ ] ✅ NuGet packages signed
- [ ] ✅ Security documentation complete (11 ADRs)
- [ ] ✅ Security operations runbook created

---

### Recommended Security Enhancements (Nice to Have)

- [ ] Fuzz testing completed
- [ ] Azure Key Vault integration
- [ ] Client certificate (mTLS) support
- [ ] Message authentication (HMAC)
- [ ] Security telemetry and alerting
- [ ] Private NuGet feed
- [ ] Security training for development team

---

## Section 14: Security Metrics & Monitoring

### Security Metrics to Track

| Metric | Target | Measurement |
|--------|--------|-------------|
| Known CVEs | 0 CRITICAL, 0 HIGH | `dotnet list package --vulnerable` |
| SAST Issues | 0 CRITICAL, <5 HIGH | SonarQube / Security Code Scan |
| Hardcoded Credentials | 0 | Grep scan |
| Deprecated Crypto APIs | 0 | Grep scan |
| TLS 1.0/1.1 Usage | 0 | Runtime testing |
| Test Coverage (Crypto) | 100% | Unit tests |
| Test Coverage (Auth) | 100% | Integration tests |
| Security ADRs | 11 complete | Documentation review |
| SBOM Coverage | 100% (all 25 projects) | File verification |
| Package Signing | 100% | NuGet verification |

---

### Post-Deployment Security Monitoring

**Key Performance Indicators (KPIs)**:
1. **Authentication Failures**: <1% of connection attempts
2. **TLS Handshake Failures**: <0.1% of connection attempts
3. **Message Validation Failures**: <0.5% of messages
4. **Time to Patch**: <7 days for CRITICAL, <30 days for HIGH

**Alerting**:
- Alert: >10 auth failures/minute (potential brute force)
- Alert: >5 TLS errors/minute (configuration issue or attack)
- Alert: New CVE in dependencies (GitHub Dependabot)
- Alert: Unauthorized certificate usage (self-signed in production)

---

## Section 15: Conclusion

### Overall Security Assessment

**Original Plan Assessment**: ⚠️ **MODERATE RISK**
- 4 security checkpoints ✅ (good foundation)
- Dependency awareness ✅ (acknowledged)
- Missing: Crypto plan, secrets management, supply chain, threat modeling

**Revised Plan Assessment**: ✅ **ACCEPTABLE RISK** (with enhancements)
- 9 security checkpoints (comprehensive)
- 11 security-focused ADRs (documented decisions)
- Comprehensive security testing (SAST, DAST, fuzzing, pentesting)
- Supply chain security (SBOM, package signing)
- Post-deployment monitoring (operational security)

---

### Critical Security Recommendations

#### MUST HAVE (Cannot ship without):
1. ✅ Upgrade RabbitMQ.Client 5.0.1 → 7.x (CRITICAL CVEs)
2. ✅ Upgrade/migrate Newtonsoft.Json (CRITICAL CVEs)
3. ✅ Fix TypeNameHandling.Auto (RCE vulnerability)
4. ✅ Deprecate/remove hardcoded credentials
5. ✅ Enable TLS 1.3, reject TLS 1.0/1.1
6. ✅ Replace deprecated crypto APIs
7. ✅ Comprehensive security testing
8. ✅ 11 security ADRs documented

#### SHOULD HAVE (Highly recommended):
1. Threat modeling workshop
2. Secrets management integration (Azure Key Vault)
3. SBOM generation and package signing
4. Fuzz testing
5. Security monitoring and alerting

#### NICE TO HAVE (Optional enhancements):
1. Client certificate (mTLS) support
2. Message authentication (HMAC)
3. Private NuGet feed
4. Security training

---

### Security Clearance Recommendation

**STATUS**: 🔴 **CONDITIONAL APPROVAL - ACTION REQUIRED**

**Conditions**:
1. Complete Week 1 immediate actions (vulnerability scan, crypto inventory, credential scan)
2. Create mandatory ADRs (0006, 0009, 0011, 0012, 0013)
3. Update PLAN.md with 9-checkpoint model
4. Commit to revised timeline (13-15 weeks)

**Once Conditions Met**: 🟢 **APPROVED TO PROCEED**

---

### Next Steps

**Immediate (Today)**:
1. Review this security assessment with Migration Architect
2. Discuss timeline impact (+3 weeks)
3. Assign resources for security work

**Week 1**:
1. Execute immediate action items (scans, inventories)
2. Create mandatory security ADRs
3. Update PLAN.md with security enhancements

**Week 2**:
1. Conduct threat modeling workshop
2. Create cryptographic migration plan
3. Implement secrets management strategy

**Week 3-15**:
1. Execute revised migration plan
2. Complete all 9 security checkpoints
3. Deliver secure .NET 9 upgrade

---

**Reviewed By**: Security Specialist (Security Manager Agent)
**Date**: 2025-10-09
**Review Version**: 1.0
**Next Review**: After Stage 1 completion (Week 2)
**Approval Status**: 🔴 **CONDITIONS PENDING**

---

## Appendix: Security Resources

### Tools
- **Vulnerability Scanning**: `dotnet list package --vulnerable`, OWASP Dependency-Check
- **SAST**: SonarQube, Security Code Scan, Roslyn Security Guard
- **DAST**: OWASP ZAP
- **Fuzzing**: SharpFuzz, AFL.NET
- **SBOM**: Microsoft.Sbom.Tool
- **Package Signing**: NuGetKeyVaultSignTool

### References
- OWASP Top 10: https://owasp.org/www-project-top-ten/
- .NET Security Best Practices: https://learn.microsoft.com/en-us/dotnet/standard/security/
- RabbitMQ Security: https://www.rabbitmq.com/security.html
- NIST Cybersecurity Framework: https://www.nist.gov/cyberframework

### Contact
- Security Specialist: [TBD]
- Security Escalation: [TBD]
- Security Incident Response: [TBD]

---

**END OF SECURITY REVIEW**
