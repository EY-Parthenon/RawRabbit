#!/bin/bash
set -e

# RawRabbit Test Environment Startup Script
# Provides easy commands to start different RabbitMQ configurations

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

cd "$PROJECT_ROOT"

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

print_header() {
    echo ""
    echo -e "${BLUE}================================${NC}"
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}================================${NC}"
    echo ""
}

print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_info() {
    echo -e "${YELLOW}ℹ️  $1${NC}"
}

show_usage() {
    echo "Usage: bash scripts/start-test-environment.sh [PROFILE]"
    echo ""
    echo "Available profiles:"
    echo "  default      - Single RabbitMQ 3.12 instance (default)"
    echo "  compatibility - RabbitMQ 3.11 + 3.12 for version testing"
    echo "  ssl          - Single RabbitMQ with SSL/TLS enabled"
    echo "  cluster      - 3-node RabbitMQ cluster for HA testing"
    echo "  all          - All profiles (for comprehensive testing)"
    echo ""
    echo "Examples:"
    echo "  bash scripts/start-test-environment.sh"
    echo "  bash scripts/start-test-environment.sh ssl"
    echo "  bash scripts/start-test-environment.sh cluster"
    echo ""
}

wait_for_rabbitmq() {
    local container=$1
    local port=$2
    local max_retries=30

    print_info "Waiting for RabbitMQ ($container) to be ready..."

    for i in $(seq 1 $max_retries); do
        if docker exec "$container" rabbitmq-diagnostics -q ping > /dev/null 2>&1; then
            print_success "RabbitMQ ($container) is ready!"
            return 0
        fi
        echo -n "."
        sleep 1
    done

    echo ""
    echo "❌ Error: RabbitMQ ($container) failed to start within ${max_retries} seconds"
    docker logs "$container" | tail -20
    return 1
}

setup_cluster() {
    print_header "Setting up RabbitMQ Cluster"

    print_info "Waiting for all nodes to start..."
    wait_for_rabbitmq "rawrabbit-cluster-node1" 15674
    wait_for_rabbitmq "rawrabbit-cluster-node2" 15675
    wait_for_rabbitmq "rawrabbit-cluster-node3" 15676

    print_info "Joining node2 to cluster..."
    docker exec rawrabbit-cluster-node2 rabbitmqctl stop_app
    docker exec rawrabbit-cluster-node2 rabbitmqctl reset
    docker exec rawrabbit-cluster-node2 rabbitmqctl join_cluster rabbit@rabbitmq-node1
    docker exec rawrabbit-cluster-node2 rabbitmqctl start_app

    print_info "Joining node3 to cluster..."
    docker exec rawrabbit-cluster-node3 rabbitmqctl stop_app
    docker exec rawrabbit-cluster-node3 rabbitmqctl reset
    docker exec rawrabbit-cluster-node3 rabbitmqctl join_cluster rabbit@rabbitmq-node1
    docker exec rawrabbit-cluster-node3 rabbitmqctl start_app

    print_success "Cluster setup complete!"
    echo ""
    docker exec rawrabbit-cluster-node1 rabbitmqctl cluster_status
}

generate_ssl_certs() {
    if [ ! -f "test/certificates/ca-cert.pem" ]; then
        print_info "SSL certificates not found. Generating..."
        bash test/certificates/generate-test-certs.sh
    else
        print_info "SSL certificates already exist."
    fi
}

start_default() {
    print_header "Starting Default Test Environment"
    print_info "RabbitMQ 3.12 (single node)"

    docker-compose up -d rabbitmq
    wait_for_rabbitmq "rawrabbit-test-rabbitmq" 15672

    echo ""
    print_success "Default environment ready!"
    print_info "AMQP Port: 5672"
    print_info "Management UI: http://localhost:15672"
    print_info "Username: guest"
    print_info "Password: guest"
}

start_compatibility() {
    print_header "Starting Compatibility Test Environment"
    print_info "RabbitMQ 3.11 (LTS) + 3.12 (Latest)"

    docker-compose --profile compatibility up -d rabbitmq rabbitmq-3-11

    wait_for_rabbitmq "rawrabbit-test-rabbitmq" 15672
    wait_for_rabbitmq "rawrabbit-test-rabbitmq-3-11" 15673

    echo ""
    print_success "Compatibility environment ready!"
    print_info "RabbitMQ 3.12:"
    print_info "  - AMQP Port: 5672"
    print_info "  - Management UI: http://localhost:15672"
    print_info "RabbitMQ 3.11:"
    print_info "  - AMQP Port: 5673"
    print_info "  - Management UI: http://localhost:15673"
}

start_ssl() {
    print_header "Starting SSL/TLS Test Environment"
    print_info "RabbitMQ 3.12 with SSL/TLS enabled"

    generate_ssl_certs

    docker-compose --profile ssl up -d rabbitmq-ssl
    wait_for_rabbitmq "rawrabbit-test-rabbitmq-ssl" 15671

    echo ""
    print_success "SSL environment ready!"
    print_info "AMQPS Port: 5671"
    print_info "Management UI: https://localhost:15671"
    print_info "Username: guest"
    print_info "Password: guest"
    print_info "Certificates: test/certificates/"
}

start_cluster() {
    print_header "Starting Cluster Test Environment"
    print_info "3-node RabbitMQ cluster"

    docker-compose --profile cluster up -d \
        rabbitmq-cluster-node1 \
        rabbitmq-cluster-node2 \
        rabbitmq-cluster-node3

    setup_cluster

    echo ""
    print_success "Cluster environment ready!"
    print_info "Node 1:"
    print_info "  - AMQP Port: 5674"
    print_info "  - Management UI: http://localhost:15674"
    print_info "Node 2:"
    print_info "  - AMQP Port: 5675"
    print_info "  - Management UI: http://localhost:15675"
    print_info "Node 3:"
    print_info "  - AMQP Port: 5676"
    print_info "  - Management UI: http://localhost:15676"
}

start_all() {
    print_header "Starting All Test Environments"

    generate_ssl_certs

    docker-compose --profile compatibility --profile ssl --profile cluster up -d

    print_info "Waiting for all services to be ready..."
    wait_for_rabbitmq "rawrabbit-test-rabbitmq" 15672
    wait_for_rabbitmq "rawrabbit-test-rabbitmq-3-11" 15673
    wait_for_rabbitmq "rawrabbit-test-rabbitmq-ssl" 15671

    setup_cluster

    echo ""
    print_success "All environments ready!"
}

# Main script
PROFILE=${1:-default}

case "$PROFILE" in
    default)
        start_default
        ;;
    compatibility)
        start_compatibility
        ;;
    ssl)
        start_ssl
        ;;
    cluster)
        start_cluster
        ;;
    all)
        start_all
        ;;
    help|-h|--help)
        show_usage
        exit 0
        ;;
    *)
        echo "❌ Error: Unknown profile '$PROFILE'"
        echo ""
        show_usage
        exit 1
        ;;
esac

echo ""
print_info "To stop the test environment: docker-compose down"
print_info "To view logs: docker-compose logs -f"
echo ""
