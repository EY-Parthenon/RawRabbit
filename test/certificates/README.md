# Test Certificates for SSL/TLS Testing

This directory contains self-signed certificates for testing RabbitMQ SSL/TLS connections.

**IMPORTANT**: These certificates are for **TESTING ONLY** and should NEVER be used in production.

## Generating Test Certificates

Run the certificate generation script to create test certificates:

```bash
bash test/certificates/generate-test-certs.sh
```

This will generate:
- `ca-cert.pem` - Certificate Authority certificate
- `ca-key.pem` - CA private key
- `server-cert.pem` - RabbitMQ server certificate
- `server-key.pem` - Server private key
- `client-cert.pem` - Client certificate (for client cert authentication)
- `client-key.pem` - Client private key

## Using SSL/TLS Test Environment

Start RabbitMQ with SSL/TLS enabled:

```bash
# Start SSL-enabled RabbitMQ
docker-compose --profile ssl up -d rabbitmq-ssl

# Verify RabbitMQ is running
docker logs rawrabbit-test-rabbitmq-ssl

# Access Management UI (HTTPS)
# https://localhost:15671
# Username: guest
# Password: guest
```

## Certificate Details

**Certificate Authority (CA)**:
- Subject: CN=Test CA
- Validity: 365 days
- Key Size: 2048 bits

**Server Certificate**:
- Subject: CN=localhost
- Issuer: Test CA
- Validity: 365 days
- SAN: DNS:localhost, DNS:rabbitmq-ssl, IP:127.0.0.1

**Client Certificate**:
- Subject: CN=test-client
- Issuer: Test CA
- Validity: 365 days

## Testing TLS Connections

**C# Example**:
```csharp
var config = new RawRabbitConfiguration
{
    Hostnames = new List<string> { "localhost" },
    Port = 5671, // AMQPS port
    Ssl = new SslOption
    {
        Enabled = true,
        ServerName = "localhost",
        CertPath = "test/certificates/client-cert.pem",
        CertPassphrase = "",
        Version = SslProtocols.Tls12 | SslProtocols.Tls13,
        AcceptablePolicyErrors = SslPolicyErrors.None // Strict validation
    }
};

var client = RawRabbitFactory.CreateSingleton(config);
await client.PublishAsync(new TestMessage());
```

## Troubleshooting

**Connection Refused**:
- Ensure RabbitMQ SSL container is running: `docker ps | grep rabbitmq-ssl`
- Check logs: `docker logs rawrabbit-test-rabbitmq-ssl`

**Certificate Validation Failed**:
- Verify certificates exist in `test/certificates/` directory
- Check certificate permissions (should be readable)
- Ensure ServerName matches certificate CN (localhost)

**Self-Signed Certificate Error**:
- This is expected with self-signed certs
- For testing, you may need to set `AcceptablePolicyErrors = SslPolicyErrors.RemoteCertificateChainErrors`
- NEVER do this in production!

## Security Notes

1. **Never commit private keys to git** - `.gitignore` excludes `*.pem` and `*.key` files
2. **Regenerate certificates regularly** - Rotate test certificates every 6 months
3. **Use production-grade certificates in production** - Obtain certificates from a trusted CA
4. **Protect CA private key** - Store securely, never share

## References

- [RabbitMQ TLS Support](https://www.rabbitmq.com/ssl.html)
- [OpenSSL Certificate Generation](https://www.openssl.org/docs/man1.1.1/man1/openssl-req.html)
- [.NET TLS Configuration](https://learn.microsoft.com/en-us/dotnet/framework/network-programming/tls)
