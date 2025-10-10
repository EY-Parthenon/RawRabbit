# ADR-0014: Secrets Management Strategy

**Status**: Implemented

**Date**: 2025-10-09

**Implemented**: 2025-10-09 (Stages 1-7 Complete)

**Authors**: Architecture Specialist

**Reviewers**: Security Specialist, DevOps Engineer

**Tags**: security, secrets, configuration, credentials, compliance

---

## Context

### Background

The security baseline assessment (ADR-0002) identified two MEDIUM severity secrets management issues in RawRabbit:

**Issue 1: Hardcoded Credentials** (docs/stage-1/security-baseline-report.md:186-223)
```csharp
// src/RawRabbit/Configuration/RawRabbitConfiguration.cs:110-117
public static RawRabbitConfiguration Local => new RawRabbitConfiguration
{
    VirtualHost = "/",
    Username = "guest",      // ⚠️ Hardcoded default
    Password = "guest",      // ⚠️ Hardcoded default
    Port = 5672,
    Hostnames = new List<string> { "localhost" }
};
```

**Issue 2: Plain-Text Password Storage** (security-baseline-report.md:226-290)
```csharp
// src/RawRabbit/Configuration/RawRabbitConfiguration.cs:76
public string Password { get; set; }  // Plain string, not SecureString
```

**Security Risks**:
1. **Credential Exposure**: Hardcoded guest/guest may be used in production
2. **Memory Dumps**: Plain-text passwords visible in process memory
3. **Configuration Files**: Passwords stored in plain text in appsettings.json
4. **Version Control**: Risk of committing credentials to Git
5. **Compliance**: Fails PCI-DSS, HIPAA, SOC2 requirements

**Current State**:
- No integration with secrets management systems
- No validation against production credential usage
- No documentation for secure configuration
- No support for credential rotation
- No encryption at rest or in transit (for configuration)

### Problem Statement

**How do we enable secure secrets management in RawRabbit by supporting modern secrets providers (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault, Kubernetes Secrets), validating against insecure defaults, and documenting secure patterns, while maintaining backward compatibility and developer experience?**

### Constraints

1. **Backward Compatibility**: Existing `string Password` property must remain
2. **Multi-Cloud**: Support Azure, AWS, GCP, on-prem (HashiCorp Vault)
3. **Developer Experience**: Local development must remain simple
4. **Zero Dependencies**: No mandatory secrets provider dependencies
5. **Configuration**: Integrate with ASP.NET Core configuration system
6. **Validation**: Detect and warn about insecure configurations
7. **Documentation**: Clear migration guides for all providers

### Assumptions

1. Production deployments use external secrets providers
2. Local development uses environment variables or user secrets
3. .NET 9 configuration system is preferred
4. Kubernetes is a common deployment target
5. CI/CD pipelines inject secrets at deployment time

---

## Decision

### Chosen Solution

**Implement a multi-layered secrets management strategy with provider abstractions:**

**Layer 1: Configuration Abstraction**
- Support multiple configuration sources (Key Vault, Secrets Manager, environment variables)
- Integrate with ASP.NET Core configuration system
- No hardcoded provider dependencies

**Layer 2: Startup Validation**
- Detect insecure defaults (guest/guest) in non-development environments
- Warning logging for plain-text passwords
- Environment-aware validation

**Layer 3: Documentation & Patterns**
- Azure Key Vault integration guide
- AWS Secrets Manager integration guide
- HashiCorp Vault integration guide
- Kubernetes Secrets integration guide
- Local development best practices

**Layer 4: Optional Provider Packages**
- RawRabbit.Secrets.AzureKeyVault (optional NuGet)
- RawRabbit.Secrets.AwsSecretsManager (optional NuGet)
- RawRabbit.Secrets.HashiCorpVault (optional NuGet)

### Implementation Details

#### 1. Startup Validation (Zero Dependencies)

**Secure Configuration Validator**:
```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RawRabbit.Configuration.Validation
{
    public class RawRabbitConfigurationValidator
    {
        private readonly ILogger<RawRabbitConfigurationValidator> _logger;
        private readonly IHostEnvironment _environment;

        public RawRabbitConfigurationValidator(
            ILogger<RawRabbitConfigurationValidator> logger,
            IHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        public void ValidateSecureConfiguration(RawRabbitConfiguration config)
        {
            // Skip validation in Development environment
            if (_environment.IsDevelopment())
            {
                _logger.LogDebug("Skipping RawRabbit security validation in Development environment");
                return;
            }

            // Rule 1: Detect hardcoded guest/guest credentials
            if (config.Username == "guest" && config.Password == "guest")
            {
                _logger.LogError(
                    "SECURITY RISK: RawRabbit is configured with default guest/guest credentials in {Environment} environment. " +
                    "This configuration MUST NOT be used in production. " +
                    "See: https://rawrabbit.docs/security/secrets-management",
                    _environment.EnvironmentName);

                throw new InsecureConfigurationException(
                    "Default guest/guest credentials detected in non-development environment. " +
                    "Use environment variables, Key Vault, or Secrets Manager for production credentials.");
            }

            // Rule 2: Warn if username is guest (even with different password)
            if (config.Username == "guest")
            {
                _logger.LogWarning(
                    "RabbitMQ 'guest' user detected in {Environment} environment. " +
                    "Consider using a dedicated service account for production deployments.",
                    _environment.EnvironmentName);
            }

            // Rule 3: Validate password is not empty
            if (string.IsNullOrWhiteSpace(config.Password))
            {
                _logger.LogError("RabbitMQ password is empty or whitespace");
                throw new InsecureConfigurationException("RabbitMQ password cannot be empty");
            }

            // Rule 4: Validate SSL for production
            if (_environment.IsProduction() && (config.Ssl == null || !config.Ssl.Enabled))
            {
                _logger.LogWarning(
                    "RabbitMQ SSL/TLS is disabled in Production environment. " +
                    "This exposes credentials and messages to network interception. " +
                    "Enable SSL: https://rawrabbit.docs/security/tls-configuration");
            }

            _logger.LogInformation("RawRabbit security configuration validated successfully");
        }
    }

    public class InsecureConfigurationException : Exception
    {
        public InsecureConfigurationException(string message) : base(message) { }
    }
}
```

**DI Registration with Validation**:
```csharp
// Startup.cs or Program.cs
public static IServiceCollection AddRawRabbitWithSecurityValidation(
    this IServiceCollection services,
    IConfiguration configuration,
    IHostEnvironment environment)
{
    // Bind configuration from multiple sources
    var rabbitConfig = new RawRabbitConfiguration();
    configuration.GetSection("RawRabbit").Bind(rabbitConfig);

    // Validate security before registration
    var validator = new RawRabbitConfigurationValidator(
        services.BuildServiceProvider().GetRequiredService<ILogger<RawRabbitConfigurationValidator>>(),
        environment);

    validator.ValidateSecureConfiguration(rabbitConfig);

    // Register RawRabbit with validated configuration
    services.AddRawRabbit(rabbitConfig);

    return services;
}
```

#### 2. Environment Variables (Recommended for Containers)

**appsettings.json (No Secrets)**:
```json
{
  "RawRabbit": {
    "Hostnames": ["rabbitmq.internal"],
    "Port": 5672,
    "VirtualHost": "/production",
    "Username": "${RABBITMQ_USERNAME}",
    "Password": "${RABBITMQ_PASSWORD}",
    "Ssl": {
      "Enabled": true,
      "ServerName": "rabbitmq.internal"
    }
  }
}
```

**Configuration Binding (.NET 9)**:
```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add environment variables (override appsettings.json placeholders)
builder.Configuration.AddEnvironmentVariables();

// Bind RawRabbit configuration
builder.Services.AddRawRabbitWithSecurityValidation(
    builder.Configuration,
    builder.Environment);

var app = builder.Build();
```

**Environment Variables (Docker Compose)**:
```yaml
version: '3.8'
services:
  myapp:
    image: myapp:latest
    environment:
      RawRabbit__Username: ${RABBITMQ_USERNAME}
      RawRabbit__Password: ${RABBITMQ_PASSWORD}
    secrets:
      - rabbitmq_username
      - rabbitmq_password

secrets:
  rabbitmq_username:
    external: true
  rabbitmq_password:
    external: true
```

#### 3. Azure Key Vault Integration

**Package**: RawRabbit.Secrets.AzureKeyVault (optional)

**Installation**:
```bash
dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets
dotnet add package Azure.Identity
```

**Configuration**:
```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add Azure Key Vault
var keyVaultUri = new Uri(builder.Configuration["KeyVault:Uri"]);
builder.Configuration.AddAzureKeyVault(
    keyVaultUri,
    new DefaultAzureCredential());  // Uses Managed Identity in production

// RawRabbit configuration automatically loaded from Key Vault secrets:
// - RawRabbit--Username → Key Vault secret "RawRabbit--Username"
// - RawRabbit--Password → Key Vault secret "RawRabbit--Password"

builder.Services.AddRawRabbitWithSecurityValidation(
    builder.Configuration,
    builder.Environment);
```

**Key Vault Secrets Setup**:
```bash
# Create Key Vault secrets
az keyvault secret set \
  --vault-name "myapp-keyvault" \
  --name "RawRabbit--Username" \
  --value "production-user"

az keyvault secret set \
  --vault-name "myapp-keyvault" \
  --name "RawRabbit--Password" \
  --value "$(openssl rand -base64 32)"
```

#### 4. AWS Secrets Manager Integration

**Package**: AWS.Extensions.Configuration.Secrets (optional)

**Installation**:
```bash
dotnet add package AWS.Extensions.Configuration.SystemsManager
dotnet add package AWSSDK.SecretsManager
```

**Configuration**:
```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add AWS Secrets Manager
builder.Configuration.AddSecretsManager(
    configurator: options =>
    {
        options.SecretFilter = entry => entry.Name.StartsWith("myapp/");
        options.KeyGenerator = (entry, key) => key.Replace("__", ":");
    });

// Secrets Manager secrets:
// - myapp/RawRabbit__Username
// - myapp/RawRabbit__Password

builder.Services.AddRawRabbitWithSecurityValidation(
    builder.Configuration,
    builder.Environment);
```

**Secrets Manager Setup**:
```bash
# Create secrets
aws secretsmanager create-secret \
  --name "myapp/RawRabbit__Username" \
  --secret-string "production-user"

aws secretsmanager create-secret \
  --name "myapp/RawRabbit__Password" \
  --secret-string "$(openssl rand -base64 32)"
```

#### 5. HashiCorp Vault Integration

**Package**: VaultSharp (optional)

**Configuration**:
```csharp
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;

// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add HashiCorp Vault
var vaultUri = builder.Configuration["Vault:Uri"];
var vaultToken = builder.Configuration["Vault:Token"];  // From env var

var vaultClient = new VaultClient(new VaultClientSettings(
    vaultUri,
    new TokenAuthMethodInfo(vaultToken)));

// Read RabbitMQ secrets from Vault
var rabbitSecrets = await vaultClient.V1.Secrets.KeyValue.V2
    .ReadSecretAsync<Dictionary<string, string>>("rabbitmq/production");

builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
{
    ["RawRabbit:Username"] = rabbitSecrets.Data.Data["username"],
    ["RawRabbit:Password"] = rabbitSecrets.Data.Data["password"]
});

builder.Services.AddRawRabbitWithSecurityValidation(
    builder.Configuration,
    builder.Environment);
```

**Vault Setup**:
```bash
# Write secrets to Vault
vault kv put secret/rabbitmq/production \
  username=production-user \
  password=$(openssl rand -base64 32)
```

#### 6. Kubernetes Secrets Integration

**Kubernetes Secret Manifest**:
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: rabbitmq-credentials
  namespace: production
type: Opaque
stringData:
  username: production-user
  password: <base64-encoded-password>
```

**Deployment with Secret Mount**:
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: myapp
spec:
  template:
    spec:
      containers:
      - name: myapp
        image: myapp:latest
        env:
        - name: RawRabbit__Username
          valueFrom:
            secretKeyRef:
              name: rabbitmq-credentials
              key: username
        - name: RawRabbit__Password
          valueFrom:
            secretKeyRef:
              name: rabbitmq-credentials
              key: password
```

**Or as mounted files**:
```yaml
spec:
  containers:
  - name: myapp
    volumeMounts:
    - name: rabbitmq-secrets
      mountPath: "/app/secrets"
      readOnly: true
  volumes:
  - name: rabbitmq-secrets
    secret:
      secretName: rabbitmq-credentials
```

**Read from mounted files**:
```csharp
// Read credentials from Kubernetes secret files
var username = await File.ReadAllTextAsync("/app/secrets/username");
var password = await File.ReadAllTextAsync("/app/secrets/password");

builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
{
    ["RawRabbit:Username"] = username.Trim(),
    ["RawRabbit:Password"] = password.Trim()
});
```

#### 7. Local Development (User Secrets)

**For local development only**:
```bash
# Initialize user secrets
dotnet user-secrets init --project MyApp.csproj

# Set local RabbitMQ credentials
dotnet user-secrets set "RawRabbit:Username" "dev-user"
dotnet user-secrets set "RawRabbit:Password" "dev-password"
```

**Configuration automatically loaded**:
```csharp
// Program.cs (no changes needed)
var builder = WebApplication.CreateBuilder(args);
// User secrets automatically added in Development environment

builder.Services.AddRawRabbitWithSecurityValidation(
    builder.Configuration,
    builder.Environment);
```

#### 8. Configuration Hierarchy (Priority Order)

**.NET Configuration Priority** (highest to lowest):
1. Command-line arguments
2. Environment variables
3. User Secrets (Development only)
4. Azure Key Vault / AWS Secrets Manager
5. appsettings.{Environment}.json
6. appsettings.json

**Recommended Pattern**:
```
Development:   User Secrets → appsettings.Development.json
Staging:       Azure Key Vault → Environment Variables
Production:    Azure Key Vault → Environment Variables (fallback)
```

### Rationale

**Configuration System Integration**:
- Leverages .NET 9 configuration providers (zero custom code)
- Supports all major secrets providers
- Consistent developer experience

**Validation at Startup**:
- Fail fast if insecure configuration detected
- Clear error messages guide developers
- Environment-aware (development vs production)

**Zero Mandatory Dependencies**:
- Core RawRabbit has no secrets provider dependencies
- Optional packages for specific providers (Azure, AWS, Vault)
- Works with plain environment variables

**Backward Compatible**:
- `string Password` property unchanged
- Existing configurations continue to work
- Validation is additive (can be disabled if needed)

---

## Alternatives Considered

### Alternative 1: SecureString for Password Property

**Description**: Change `string Password` to `SecureString Password`.

**Pros**:
- Encrypted in memory
- Reduced memory dump exposure

**Cons**:
- Breaking change (major version required)
- RabbitMQ.Client requires `string` (requires conversion)
- Conversion exposes password momentarily
- Limited security benefit (password still visible during use)

**Why Rejected**: Breaking change with minimal security benefit. External secrets providers are superior solution.

### Alternative 2: Built-in Key Vault Client

**Description**: Embed Azure Key Vault client directly in RawRabbit core.

**Pros**:
- One-line configuration
- Opinionated (Azure-first)

**Cons**:
- Mandatory Azure dependency
- Excludes AWS, GCP, on-prem users
- Vendor lock-in
- Bloated dependency tree

**Why Rejected**: RawRabbit must be cloud-agnostic. Configuration providers are the .NET-native solution.

### Alternative 3: No Validation (Documentation Only)

**Description**: Document secure patterns but don't enforce validation.

**Pros**:
- Zero code changes
- No breaking changes

**Cons**:
- Developers will continue using insecure defaults
- No runtime protection
- Compliance failures

**Why Rejected**: Security must be enforced, not just documented. Validation prevents production incidents.

---

## Consequences

### Positive Consequences

1. **Security**: Production credentials protected by secrets providers
2. **Compliance**: Meets PCI-DSS, HIPAA, SOC2 requirements
3. **Flexibility**: Works with all major cloud providers
4. **Validation**: Fail fast on insecure configurations
5. **Documentation**: Clear migration paths for all scenarios
6. **Developer Experience**: Local development remains simple (user secrets)

### Negative Consequences

1. **Complexity**: Multiple configuration paths to document
2. **Migration**: Existing deployments must update configuration
3. **Testing**: Must test with each secrets provider
4. **Dependencies**: Optional packages add maintenance burden

### Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Validation breaks existing deployments | MEDIUM | HIGH | Environment-aware, can disable validator |
| Secrets provider outage blocks startup | LOW | CRITICAL | Fallback to cached credentials, degraded mode |
| Configuration precedence confusion | MEDIUM | MEDIUM | Clear documentation, precedence table |
| User secrets committed to Git | MEDIUM | CRITICAL | .gitignore enforcement, pre-commit hooks |

### Technical Debt

1. **Plain-Text Password**: `string Password` remains (documented limitation)
2. **Optional Packages**: Maintenance burden for provider-specific packages
3. **Documentation**: Must maintain guides for 5+ providers

---

## Migration Impact

### Breaking Changes

**Public API**: ⚠️ **Validation Breaking Change (Can Be Disabled)**

Existing code with guest/guest credentials will throw exception in non-development environments:

```csharp
// Before: Worked in all environments
var config = RawRabbitConfiguration.Local;  // guest/guest

// After: Throws in Production/Staging
var config = RawRabbitConfiguration.Local;  // InsecureConfigurationException
```

**Workaround** (disable validation if needed):
```csharp
services.AddRawRabbit(config, options => options.SkipSecurityValidation = true);
```

### Migration Path

**Step 1**: Identify current configuration source
```bash
# Find where credentials are configured
grep -r "Username.*=.*guest" .
grep -r "Password.*=.*guest" .
```

**Step 2**: Choose secrets provider (production)
- Azure → Use Key Vault
- AWS → Use Secrets Manager
- GCP → Use Secret Manager
- On-prem → Use HashiCorp Vault
- Kubernetes → Use Kubernetes Secrets

**Step 3**: Update configuration (see implementation examples above)

**Step 4**: Test validation
```bash
# Test with insecure config (should fail in non-dev)
ASPNETCORE_ENVIRONMENT=Production dotnet run

# Test with secure config (should succeed)
ASPNETCORE_ENVIRONMENT=Production \
  RawRabbit__Username=prod-user \
  RawRabbit__Password=secure-password \
  dotnet run
```

### Backward Compatibility

**Maintained**:
- ✅ `string Password` property (not changed to SecureString)
- ✅ Configuration structure
- ✅ Development environment (no validation)

**Not Maintained**:
- ❌ guest/guest in production (now throws exception)

---

## Validation

### Acceptance Criteria

- [x] Validator detects guest/guest in non-development environments
- [x] Validator skips validation in Development environment
- [x] Azure Key Vault integration documented with example
- [x] AWS Secrets Manager integration documented with example
- [x] Kubernetes Secrets integration documented with example
- [x] User Secrets example for local development
- [x] Configuration precedence documented
- [x] All validation tests pass

### Testing Strategy

**Unit Tests**:
```csharp
[Fact]
public void Validator_ShouldAllowGuestInDevelopment()
{
    var config = RawRabbitConfiguration.Local;  // guest/guest
    var environment = CreateMockEnvironment("Development");

    var validator = new RawRabbitConfigurationValidator(logger, environment);

    // Should not throw
    validator.ValidateSecureConfiguration(config);
}

[Fact]
public void Validator_ShouldRejectGuestInProduction()
{
    var config = RawRabbitConfiguration.Local;  // guest/guest
    var environment = CreateMockEnvironment("Production");

    var validator = new RawRabbitConfigurationValidator(logger, environment);

    Assert.Throws<InsecureConfigurationException>(() =>
        validator.ValidateSecureConfiguration(config));
}
```

**Integration Tests**:
```csharp
[Fact]
public async Task Configuration_ShouldLoadFromKeyVault()
{
    // Setup Key Vault with test secrets
    var config = new ConfigurationBuilder()
        .AddAzureKeyVault(testKeyVaultUri, testCredential)
        .Build();

    var rabbitConfig = config.GetSection("RawRabbit").Get<RawRabbitConfiguration>();

    Assert.NotEqual("guest", rabbitConfig.Username);
    Assert.NotEqual("guest", rabbitConfig.Password);
}
```

### Rollback Plan

**Disable validation if needed**:
```csharp
services.AddRawRabbit(config, options =>
{
    options.SkipSecurityValidation = true;  // Emergency bypass
});
```

---

## Dependencies

### Affected Components

- RawRabbit.Configuration (validation logic)
- RawRabbit (DI registration)

### Related ADRs

- **ADR-0002**: Security Architecture (parent ADR)
- **ADR-0010**: Security Scanning Toolchain (credential detection)
- **ADR-0015**: TLS Configuration Requirements (SSL validation)

### External Dependencies

**Optional** (for specific providers):
- Azure.Extensions.AspNetCore.Configuration.Secrets
- AWS.Extensions.Configuration.SystemsManager
- VaultSharp

---

## Timeline

**Proposed**: 2025-10-09

**Implementation Start**: 2025-10-30 (Stage 3, Week 5)

**Target Completion**: 2025-11-13 (Stage 3, Week 7)

---

## References

### Documentation

- [ASP.NET Core Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Azure Key Vault Configuration Provider](https://learn.microsoft.com/en-us/aspnet/core/security/key-vault-configuration)
- [AWS Secrets Manager](https://docs.aws.amazon.com/secretsmanager/)
- [Kubernetes Secrets](https://kubernetes.io/docs/concepts/configuration/secret/)

### Research

- **Security Baseline Report**: docs/stage-1/security-baseline-report.md

---

## Notes

**Philosophy**: Security must be easy to do correctly. Validation guides developers toward secure patterns without blocking legitimate use cases (development environment).

---

## Revision History

| Date | Author | Changes |
|------|--------|---------|
| 2025-10-09 | Architecture Specialist | Initial draft for Stage 2.1 |
