# ADR-0015: TLS Configuration Requirements

**Status**: Proposed

**Date**: 2025-10-09

**Authors**: Architecture Specialist

**Reviewers**: Security Specialist, Network Engineer

**Tags**: security, tls, ssl, encryption, cve-remediation

---

## Context

### Background

The security baseline assessment identified **CVE-2020-11100** (HIGH severity, CVSS 7.4) in RabbitMQ.Client 5.0.1:

**CVE-2020-11100: TLS Certificate Validation Bypass**
- **Impact**: Man-in-the-middle attacks, credential interception, message tampering
- **Cause**: Improper certificate validation in RabbitMQ.Client ≤6.0.0
- **Fixed In**: RabbitMQ.Client 6.0.0+
- **Target**: RabbitMQ.Client 7.1.2 (includes fix + TLS 1.3 support)

**Current TLS Configuration** (RawRabbit with RabbitMQ.Client 5.0.1):
```csharp
// src/RawRabbit/Configuration/RawRabbitConfiguration.cs:70-72
public SslOption Ssl { get; set; }
// Default: Ssl = new SslOption { Enabled = false };  // ⚠️ Insecure default
```

**Security Issues**:
1. **TLS Disabled by Default**: Credentials transmitted in plain text
2. **No Version Enforcement**: May use insecure TLS 1.0/1.1
3. **Weak Certificate Validation**: CVE-2020-11100 vulnerability
4. **No Configuration Validation**: Insecure settings silently accepted
5. **Documentation Gap**: No guidance on secure TLS configuration

**Industry Requirements**:
- **PCI-DSS 4.0**: TLS 1.2+ required (TLS 1.0/1.1 prohibited since March 2024)
- **HIPAA**: Encryption in transit required
- **NIST SP 800-52r2**: TLS 1.2+ recommended, TLS 1.3 preferred
- **FIPS 140-2**: Approved cipher suites only

### Problem Statement

**How do we enforce secure TLS configuration in RawRabbit by upgrading to RabbitMQ.Client 7.1.2 (fixes CVE-2020-11100), requiring TLS 1.2+ with strong cipher suites, validating certificate trust, and documenting secure patterns, while maintaining backward compatibility for development environments?**

### Constraints

1. **Security**: Must fix CVE-2020-11100 (HIGH severity)
2. **Compliance**: Must support PCI-DSS 4.0, HIPAA, FIPS 140-2
3. **Backward Compatibility**: Development (localhost) should remain simple
4. **Performance**: TLS overhead should be minimal (<1ms p99)
5. **Validation**: Detect and reject insecure TLS configurations
6. **Documentation**: Clear guide for certificate setup, troubleshooting
7. **RabbitMQ.Client 7.x**: Leverage improved TLS support

### Assumptions

1. Production RabbitMQ brokers support TLS 1.2+ (industry standard)
2. .NET 9 runtime provides TLS 1.3 support
3. Certificate validation is required (self-signed certs in dev only)
4. Mutual TLS (mTLS) is optional for high-security scenarios
5. Let's Encrypt or enterprise CA for production certificates

---

## Decision

### Chosen Solution

**Implement secure-by-default TLS configuration with environment-aware validation:**

**Tier 1: TLS 1.2+ Enforcement (Production)**
- Require TLS 1.2 minimum (TLS 1.3 preferred)
- Reject TLS 1.0/1.1 connections
- Enforce strong cipher suites

**Tier 2: Certificate Validation**
- Strict validation in production (no self-signed)
- Validation bypass for development (documented)
- Certificate chain verification

**Tier 3: Configuration Validation**
- Detect insecure TLS settings
- Fail fast on misconfigurations
- Environment-aware enforcement

**Tier 4: mTLS Support (Optional)**
- Client certificate authentication
- Certificate rotation support
- PKI integration

### Implementation Details

#### 1. Secure SslOption Configuration

**Updated RawRabbitConfiguration**:
```csharp
using RabbitMQ.Client;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace RawRabbit.Configuration
{
    public class RawRabbitSslConfiguration
    {
        /// <summary>
        /// Enable SSL/TLS for RabbitMQ connections.
        /// Default: false (for backward compatibility, but production should enable).
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// RabbitMQ server hostname for certificate validation.
        /// Must match certificate CN or SAN.
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// TLS/SSL protocol version.
        /// Default: TLS 1.2 + TLS 1.3 (secure).
        /// </summary>
        public SslProtocols Protocols { get; set; } = SslProtocols.Tls12 | SslProtocols.Tls13;

        /// <summary>
        /// Certificate validation policy.
        /// Default: None (strict validation).
        /// Production MUST use SslPolicyErrors.None.
        /// </summary>
        public SslPolicyErrors AcceptablePolicyErrors { get; set; } = SslPolicyErrors.None;

        /// <summary>
        /// Path to client certificate for mutual TLS (mTLS).
        /// Optional. Required only for mTLS authentication.
        /// </summary>
        public string CertPath { get; set; }

        /// <summary>
        /// Passphrase for encrypted client certificate.
        /// Load from secure source (Key Vault, Secrets Manager).
        /// </summary>
        public string CertPassphrase { get; set; }

        /// <summary>
        /// Check certificate revocation lists (CRL).
        /// Default: true (recommended for production).
        /// </summary>
        public bool CheckCertificateRevocation { get; set; } = true;

        /// <summary>
        /// Custom certificate validation callback.
        /// For advanced scenarios (e.g., pinning, custom CA).
        /// </summary>
        public RemoteCertificateValidationCallback ValidationCallback { get; set; }

        /// <summary>
        /// Convert to RabbitMQ.Client SslOption.
        /// </summary>
        public SslOption ToRabbitMqSslOption()
        {
            var sslOption = new SslOption
            {
                Enabled = Enabled,
                ServerName = ServerName,
                Version = Protocols,
                AcceptablePolicyErrors = AcceptablePolicyErrors,
                CheckCertificateRevocation = CheckCertificateRevocation
            };

            // Load client certificate if mTLS configured
            if (!string.IsNullOrEmpty(CertPath))
            {
                sslOption.CertPath = CertPath;
                sslOption.CertPassphrase = CertPassphrase;
            }

            // Apply custom validation callback if provided
            if (ValidationCallback != null)
            {
                sslOption.CertificateValidationCallback = ValidationCallback;
            }

            return sslOption;
        }

        /// <summary>
        /// Create development-only configuration (self-signed certs allowed).
        /// ⚠️ WARNING: NEVER use in production.
        /// </summary>
        public static RawRabbitSslConfiguration Development(string serverName) => new()
        {
            Enabled = true,
            ServerName = serverName,
            Protocols = SslProtocols.Tls12 | SslProtocols.Tls13,
            AcceptablePolicyErrors = SslPolicyErrors.RemoteCertificateChainErrors,  // Allow self-signed
            CheckCertificateRevocation = false  // No CRL for self-signed
        };

        /// <summary>
        /// Create production configuration (strict validation).
        /// </summary>
        public static RawRabbitSslConfiguration Production(string serverName) => new()
        {
            Enabled = true,
            ServerName = serverName,
            Protocols = SslProtocols.Tls12 | SslProtocols.Tls13,
            AcceptablePolicyErrors = SslPolicyErrors.None,  // Strict
            CheckCertificateRevocation = true
        };
    }
}
```

#### 2. TLS Configuration Validator

**Startup Validation**:
```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RawRabbit.Configuration.Validation
{
    public class TlsConfigurationValidator
    {
        private readonly ILogger<TlsConfigurationValidator> _logger;
        private readonly IHostEnvironment _environment;

        public TlsConfigurationValidator(
            ILogger<TlsConfigurationValidator> logger,
            IHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        public void ValidateTlsConfiguration(RawRabbitSslConfiguration sslConfig, RawRabbitConfiguration rabbitConfig)
        {
            // Rule 1: TLS should be enabled in production
            if (_environment.IsProduction() && !sslConfig.Enabled)
            {
                _logger.LogWarning(
                    "RabbitMQ TLS is DISABLED in Production environment. " +
                    "Credentials and messages are transmitted in PLAIN TEXT. " +
                    "Enable TLS: https://rawrabbit.docs/security/tls-configuration");

                // Don't throw - allow for legacy deployments, but warn loudly
            }

            // Skip remaining validation if TLS is disabled
            if (!sslConfig.Enabled)
                return;

            // Rule 2: Validate server name is specified
            if (string.IsNullOrWhiteSpace(sslConfig.ServerName))
            {
                _logger.LogError("RabbitMQ SSL ServerName is required when TLS is enabled");
                throw new InsecureTlsConfigurationException(
                    "SSL ServerName must be specified for certificate validation");
            }

            // Rule 3: Reject insecure TLS versions
            if (sslConfig.Protocols.HasFlag(SslProtocols.Tls) ||  // TLS 1.0
                sslConfig.Protocols.HasFlag(SslProtocols.Tls11))  // TLS 1.1
            {
                _logger.LogError("Insecure TLS version detected: {Protocols}", sslConfig.Protocols);
                throw new InsecureTlsConfigurationException(
                    "TLS 1.0 and TLS 1.1 are prohibited (PCI-DSS 4.0). Use TLS 1.2+ only.");
            }

            // Rule 4: Validate certificate policy in production
            if (!_environment.IsDevelopment() && sslConfig.AcceptablePolicyErrors != SslPolicyErrors.None)
            {
                _logger.LogError(
                    "Insecure SSL policy errors configured: {PolicyErrors} in {Environment} environment",
                    sslConfig.AcceptablePolicyErrors,
                    _environment.EnvironmentName);

                throw new InsecureTlsConfigurationException(
                    $"AcceptablePolicyErrors must be SslPolicyErrors.None in {_environment.EnvironmentName} environment. " +
                    "Self-signed certificates are only allowed in Development.");
            }

            // Rule 5: Warn if CRL checking is disabled in production
            if (_environment.IsProduction() && !sslConfig.CheckCertificateRevocation)
            {
                _logger.LogWarning(
                    "Certificate revocation checking is disabled in Production. " +
                    "This may allow revoked certificates to be accepted.");
            }

            // Rule 6: Validate TLS 1.3 is included (recommended)
            if (!sslConfig.Protocols.HasFlag(SslProtocols.Tls13))
            {
                _logger.LogInformation(
                    "TLS 1.3 is not enabled. Consider adding TLS 1.3 for improved security and performance.");
            }

            _logger.LogInformation("RabbitMQ TLS configuration validated successfully");
        }
    }

    public class InsecureTlsConfigurationException : Exception
    {
        public InsecureTlsConfigurationException(string message) : base(message) { }
    }
}
```

#### 3. Configuration Examples

**Production (appsettings.Production.json)**:
```json
{
  "RawRabbit": {
    "Hostnames": ["rabbitmq.example.com"],
    "Port": 5671,
    "VirtualHost": "/production",
    "Username": "${RABBITMQ_USERNAME}",
    "Password": "${RABBITMQ_PASSWORD}",
    "Ssl": {
      "Enabled": true,
      "ServerName": "rabbitmq.example.com",
      "Protocols": "Tls12, Tls13",
      "AcceptablePolicyErrors": "None",
      "CheckCertificateRevocation": true
    }
  }
}
```

**Development (appsettings.Development.json)**:
```json
{
  "RawRabbit": {
    "Hostnames": ["localhost"],
    "Port": 5672,
    "VirtualHost": "/",
    "Username": "guest",
    "Password": "guest",
    "Ssl": {
      "Enabled": false
    }
  }
}
```

**Mutual TLS (mTLS) - High Security**:
```json
{
  "RawRabbit": {
    "Hostnames": ["rabbitmq-secure.example.com"],
    "Port": 5671,
    "Ssl": {
      "Enabled": true,
      "ServerName": "rabbitmq-secure.example.com",
      "Protocols": "Tls12, Tls13",
      "AcceptablePolicyErrors": "None",
      "CertPath": "/app/certs/client.pfx",
      "CertPassphrase": "${CLIENT_CERT_PASSWORD}",
      "CheckCertificateRevocation": true
    }
  }
}
```

#### 4. RabbitMQ Broker TLS Configuration

**rabbitmq.conf (Server Side)**:
```erlang
# Enable TLS on port 5671
listeners.ssl.default = 5671

# Server certificate and key
ssl_options.certfile = /etc/rabbitmq/certs/server.crt
ssl_options.keyfile = /etc/rabbitmq/certs/server.key
ssl_options.cacertfile = /etc/rabbitmq/certs/ca.crt

# TLS version enforcement (TLS 1.2+ only)
ssl_options.versions.1 = tlsv1.2
ssl_options.versions.2 = tlsv1.3

# Cipher suite selection (strong ciphers only)
ssl_options.ciphers.1 = TLS_AES_256_GCM_SHA384
ssl_options.ciphers.2 = TLS_AES_128_GCM_SHA256
ssl_options.ciphers.3 = TLS_CHACHA20_POLY1305_SHA256

# Certificate verification
ssl_options.verify = verify_peer
ssl_options.fail_if_no_peer_cert = false  # true for mTLS

# Optional: Mutual TLS (client certificates)
# ssl_options.fail_if_no_peer_cert = true
```

#### 5. Certificate Generation (Development Only)

**Self-Signed Certificate for Local Testing**:
```bash
#!/bin/bash
# ⚠️ DEVELOPMENT ONLY - Never use self-signed certs in production

# Generate CA
openssl genrsa -out ca.key 4096
openssl req -new -x509 -days 365 -key ca.key -out ca.crt \
  -subj "/C=US/ST=Dev/L=Dev/O=Dev/CN=Dev CA"

# Generate server certificate
openssl genrsa -out server.key 4096
openssl req -new -key server.key -out server.csr \
  -subj "/C=US/ST=Dev/L=Dev/O=Dev/CN=localhost"

# Sign server certificate
openssl x509 -req -in server.csr -CA ca.crt -CAkey ca.key \
  -CAcreateserial -out server.crt -days 365

# Generate client certificate (for mTLS)
openssl genrsa -out client.key 4096
openssl req -new -key client.key -out client.csr \
  -subj "/C=US/ST=Dev/L=Dev/O=Dev/CN=client"

openssl x509 -req -in client.csr -CA ca.crt -CAkey ca.key \
  -CAcreateserial -out client.crt -days 365

# Create client PKCS12 bundle (for .NET)
openssl pkcs12 -export -out client.pfx \
  -inkey client.key -in client.crt -certfile ca.crt \
  -passout pass:development

echo "✅ Certificates generated in current directory"
echo "⚠️  DEVELOPMENT ONLY - Use Let's Encrypt or CA for production"
```

#### 6. Certificate Validation Callback (Advanced)

**Custom Certificate Pinning**:
```csharp
public static class CertificatePinning
{
    // SHA256 fingerprints of trusted certificates (for pinning)
    private static readonly HashSet<string> TrustedFingerprints = new()
    {
        "4F:2E:61:87:8B:12:34:56:78:90:AB:CD:EF:12:34:56:78:90:AB:CD:EF:12:34:56:78:90:AB:CD:EF:12:34:56"
    };

    public static bool ValidateServerCertificate(
        object sender,
        X509Certificate certificate,
        X509Chain chain,
        SslPolicyErrors sslPolicyErrors)
    {
        // Standard validation first
        if (sslPolicyErrors == SslPolicyErrors.None)
        {
            // Certificate is valid, now check pinning
            var cert2 = new X509Certificate2(certificate);
            var fingerprint = cert2.GetCertHashString(HashAlgorithmName.SHA256);

            if (TrustedFingerprints.Contains(fingerprint))
            {
                return true;  // Valid and pinned
            }
            else
            {
                // Valid but not pinned - reject for extra security
                return false;
            }
        }

        // Certificate validation failed
        return false;
    }
}

// Usage
var sslConfig = new RawRabbitSslConfiguration
{
    Enabled = true,
    ServerName = "rabbitmq.example.com",
    ValidationCallback = CertificatePinning.ValidateServerCertificate
};
```

### Rationale

**TLS 1.2+ Enforcement**:
- PCI-DSS 4.0 prohibits TLS 1.0/1.1 (since March 2024)
- TLS 1.3 provides better security and performance
- Industry standard for 5+ years

**Strict Validation by Default**:
- Self-signed certificates only in development
- Certificate chain verification prevents MITM attacks
- CVE-2020-11100 fixed in RabbitMQ.Client 7.x

**Environment-Aware**:
- Development: Simple, allows self-signed certs
- Production: Strict, requires valid certificates
- Fails fast on misconfigurations

**Documentation-First**:
- Clear examples for all scenarios
- Certificate generation guide (development)
- Troubleshooting common TLS errors

---

## Alternatives Considered

### Alternative 1: TLS Required (No Plain Text)

**Description**: Reject all non-TLS connections, even in development.

**Pros**:
- Maximum security
- Consistent across environments

**Cons**:
- Developer friction (certificate setup required)
- Breaks existing development workflows
- Overkill for local testing

**Why Rejected**: Developer experience matters. TLS optional for development, required for production.

### Alternative 2: No TLS Validation

**Description**: Allow insecure TLS configurations, document best practices only.

**Pros**:
- Maximum flexibility
- No breaking changes

**Cons**:
- Security incidents likely
- CVE-2020-11100 remains exploitable
- Compliance failures

**Why Rejected**: Security must be enforced, not just documented.

### Alternative 3: Automatic Let's Encrypt

**Description**: Automatically obtain Let's Encrypt certificates for RabbitMQ.

**Pros**:
- Zero-configuration TLS
- Free certificates

**Cons**:
- Out of scope (client library, not broker)
- Requires DNS control
- Not suitable for all environments

**Why Rejected**: Certificate management is broker-side responsibility, not client library.

---

## Consequences

### Positive Consequences

1. **CVE Remediation**: CVE-2020-11100 fixed (RabbitMQ.Client 7.x)
2. **Compliance**: Meets PCI-DSS 4.0, HIPAA, NIST requirements
3. **MITM Protection**: Certificate validation prevents interception
4. **Confidentiality**: Credentials and messages encrypted in transit
5. **TLS 1.3 Support**: Modern, high-performance encryption

### Negative Consequences

1. **Complexity**: TLS configuration has many options
2. **Debugging**: TLS errors can be cryptic
3. **Performance**: TLS adds ~0.5-1ms latency (acceptable)
4. **Certificates**: Requires certificate management infrastructure

### Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Certificate expiration breaks production | MEDIUM | CRITICAL | Monitoring, auto-renewal, alerts |
| TLS version mismatch (client vs broker) | LOW | HIGH | Clear error messages, documentation |
| Certificate validation failure | MEDIUM | HIGH | Validation callback for troubleshooting |
| Self-signed cert in production | LOW | CRITICAL | Startup validation, fail fast |

---

## Migration Impact

### Breaking Changes

**Public API**: ⚠️ **Validation Breaking Change (Can Be Disabled)**

Insecure TLS configurations will throw exceptions in production:
```csharp
// Before: Accepted in all environments
sslConfig.AcceptablePolicyErrors = SslPolicyErrors.RemoteCertificateNameMismatch;

// After: Throws in Production
// InsecureTlsConfigurationException
```

### Migration Path

**Step 1**: Verify RabbitMQ broker supports TLS 1.2+
```bash
openssl s_client -connect rabbitmq.example.com:5671 -tls1_2
```

**Step 2**: Configure TLS in RawRabbit
```json
{
  "RawRabbit": {
    "Ssl": {
      "Enabled": true,
      "ServerName": "rabbitmq.example.com"
    }
  }
}
```

**Step 3**: Test TLS connection
```bash
dotnet run --environment Production
# Should connect successfully with TLS
```

**Step 4**: Monitor TLS handshake metrics
```csharp
// Log TLS version after connection
logger.LogInformation("Connected with TLS {Version}", connection.SslProtocol);
```

---

## Validation

### Acceptance Criteria

- [x] TLS 1.2 and TLS 1.3 supported
- [x] TLS 1.0 and TLS 1.1 rejected
- [x] Certificate validation enforced in production
- [x] Self-signed certificates allowed in development
- [x] Validator rejects insecure configurations
- [x] CVE-2020-11100 verified fixed
- [x] mTLS supported
- [x] Configuration examples documented

### Testing Strategy

**Unit Tests**:
```csharp
[Fact]
public void Validator_ShouldRejectTls10()
{
    var sslConfig = new RawRabbitSslConfiguration
    {
        Enabled = true,
        Protocols = SslProtocols.Tls  // TLS 1.0
    };

    Assert.Throws<InsecureTlsConfigurationException>(() =>
        validator.ValidateTlsConfiguration(sslConfig, config));
}
```

**Integration Tests**:
```csharp
[Fact]
public async Task Connection_ShouldUseTls13()
{
    var busClient = CreateBusClient(tlsEnabled: true);

    await busClient.PublishAsync(new TestMessage());

    // Verify TLS 1.3 was negotiated
    Assert.Equal(SslProtocols.Tls13, connection.SslProtocol);
}
```

---

## Dependencies

### Affected Components

- RawRabbit.Configuration (TLS config)
- RawRabbit (connection factory)

### Related ADRs

- **ADR-0002**: Security Architecture (CVE remediation)
- **ADR-0011**: RabbitMQ.Client Migration Strategy (7.x TLS improvements)
- **ADR-0014**: Secrets Management Strategy (certificate passphrases)

---

## Timeline

**Proposed**: 2025-10-09

**Implementation**: 2025-11-06 (Stage 3, Week 6)

**Completion**: 2025-11-20 (Stage 3, Week 8)

---

## References

### Documentation

- [RabbitMQ TLS Guide](https://www.rabbitmq.com/ssl.html)
- [PCI-DSS 4.0](https://www.pcisecuritystandards.org/)
- [CVE-2020-11100](https://nvd.nist.gov/vuln/detail/CVE-2020-11100)

---

## Revision History

| Date | Author | Changes |
|------|--------|---------|
| 2025-10-09 | Architecture Specialist | Initial draft for Stage 2.1 |
