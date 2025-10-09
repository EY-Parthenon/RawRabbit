#!/bin/bash
set -e

# RawRabbit Test Certificate Generation Script
# Generates self-signed certificates for RabbitMQ SSL/TLS testing
# IMPORTANT: These certificates are for TESTING ONLY

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo "================================"
echo "RawRabbit Test Certificate Generation"
echo "================================"
echo ""
echo "⚠️  WARNING: These certificates are for TESTING ONLY!"
echo "    Never use these certificates in production."
echo ""

# Check if OpenSSL is available
if ! command -v openssl &> /dev/null; then
    echo "❌ Error: OpenSSL is not installed"
    echo "   Install OpenSSL: apt-get install openssl (Ubuntu/Debian)"
    echo "                    brew install openssl (macOS)"
    exit 1
fi

# Cleanup existing certificates
echo "🧹 Cleaning up existing certificates..."
rm -f *.pem *.key *.csr *.srl

# 1. Generate Certificate Authority (CA)
echo ""
echo "📝 Step 1: Generating Certificate Authority (CA)..."
openssl genrsa -out ca-key.pem 2048
openssl req -new -x509 -key ca-key.pem -out ca-cert.pem -days 365 \
    -subj "/C=US/ST=Test/L=Test/O=RawRabbit/OU=Testing/CN=Test CA"
echo "✅ CA certificate generated: ca-cert.pem"

# 2. Generate Server Certificate
echo ""
echo "📝 Step 2: Generating RabbitMQ Server Certificate..."
openssl genrsa -out server-key.pem 2048
openssl req -new -key server-key.pem -out server-req.csr \
    -subj "/C=US/ST=Test/L=Test/O=RawRabbit/OU=Testing/CN=localhost"

# Create SAN configuration
cat > server-san.cnf << EOF
[req]
distinguished_name = req_distinguished_name
req_extensions = v3_req

[req_distinguished_name]

[v3_req]
basicConstraints = CA:FALSE
keyUsage = nonRepudiation, digitalSignature, keyEncipherment
subjectAltName = @alt_names

[alt_names]
DNS.1 = localhost
DNS.2 = rabbitmq-ssl
DNS.3 = rawrabbit-test-rabbitmq-ssl
IP.1 = 127.0.0.1
EOF

openssl x509 -req -in server-req.csr -CA ca-cert.pem -CAkey ca-key.pem \
    -CAcreateserial -out server-cert.pem -days 365 \
    -extensions v3_req -extfile server-san.cnf
echo "✅ Server certificate generated: server-cert.pem"

# 3. Generate Client Certificate
echo ""
echo "📝 Step 3: Generating Client Certificate..."
openssl genrsa -out client-key.pem 2048
openssl req -new -key client-key.pem -out client-req.csr \
    -subj "/C=US/ST=Test/L=Test/O=RawRabbit/OU=Testing/CN=test-client"
openssl x509 -req -in client-req.csr -CA ca-cert.pem -CAkey ca-key.pem \
    -CAcreateserial -out client-cert.pem -days 365
echo "✅ Client certificate generated: client-cert.pem"

# 4. Verify Certificates
echo ""
echo "📝 Step 4: Verifying Certificates..."
openssl verify -CAfile ca-cert.pem server-cert.pem
openssl verify -CAfile ca-cert.pem client-cert.pem

# 5. Display Certificate Information
echo ""
echo "📋 Certificate Information:"
echo ""
echo "CA Certificate:"
openssl x509 -in ca-cert.pem -noout -subject -issuer -dates
echo ""
echo "Server Certificate:"
openssl x509 -in server-cert.pem -noout -subject -issuer -dates
echo ""
echo "Client Certificate:"
openssl x509 -in client-cert.pem -noout -subject -issuer -dates

# Cleanup temporary files
echo ""
echo "🧹 Cleaning up temporary files..."
rm -f *.csr *.srl server-san.cnf

# Set permissions
echo ""
echo "🔒 Setting file permissions..."
chmod 600 *.key
chmod 644 *.pem

# Summary
echo ""
echo "================================"
echo "✅ Certificate Generation Complete!"
echo "================================"
echo ""
echo "Generated files:"
echo "  - ca-cert.pem       (CA certificate - distribute to clients)"
echo "  - ca-key.pem        (CA private key - KEEP SECURE)"
echo "  - server-cert.pem   (RabbitMQ server certificate)"
echo "  - server-key.pem    (Server private key - KEEP SECURE)"
echo "  - client-cert.pem   (Client certificate for authentication)"
echo "  - client-key.pem    (Client private key - KEEP SECURE)"
echo ""
echo "Next steps:"
echo "  1. Start RabbitMQ with SSL: docker-compose --profile ssl up -d rabbitmq-ssl"
echo "  2. Run SSL integration tests: dotnet test --filter 'Category=SSL'"
echo ""
echo "⚠️  REMINDER: These certificates are for TESTING ONLY!"
echo ""
