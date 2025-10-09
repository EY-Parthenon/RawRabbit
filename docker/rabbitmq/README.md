# RabbitMQ Test Environment

This Docker Compose environment provides two RabbitMQ instances for testing RawRabbit during the .NET 9 migration.

## Services

- **RabbitMQ 3.12** (latest stable)
  - AMQP Port: `5672`
  - Management UI: `http://localhost:15672`
  - Container: `rawrabbit-test-rabbitmq-3.12`

- **RabbitMQ 3.11** (previous stable)
  - AMQP Port: `5673`
  - Management UI: `http://localhost:15673`
  - Container: `rawrabbit-test-rabbitmq-3.11`

## Quick Start

Start both RabbitMQ instances:

```bash
cd /home/laird/src/EYP/RawRabbit/docker/rabbitmq
docker-compose up -d
```

Wait for services to be healthy (check with `docker-compose ps`).

## Access Management UI

Default credentials: `guest` / `guest`

- RabbitMQ 3.12: http://localhost:15672
- RabbitMQ 3.11: http://localhost:15673

## Verify Services

Check if services are running:

```bash
# Check container status
docker-compose ps

# Check RabbitMQ 3.12 logs
docker logs rawrabbit-test-rabbitmq-3.12

# Check RabbitMQ 3.11 logs
docker logs rawrabbit-test-rabbitmq-3.11

# Test management API
curl http://localhost:15672
curl http://localhost:15673
```

## Stop Services

```bash
docker-compose down
```

## Clean Up (Remove Volumes)

```bash
docker-compose down -v
```

## Health Checks

Both containers include health checks that verify RabbitMQ is ready:

- Check interval: 10 seconds
- Timeout: 5 seconds
- Retries: 5

## Testing with RawRabbit

### Connect to RabbitMQ 3.12

```csharp
var factory = new ConnectionFactory { HostName = "localhost", Port = 5672 };
```

### Connect to RabbitMQ 3.11

```csharp
var factory = new ConnectionFactory { HostName = "localhost", Port = 5673 };
```

## Troubleshooting

### Ports already in use

If ports 5672, 5673, 15672, or 15673 are already in use:

1. Stop existing RabbitMQ services
2. Or modify the `docker-compose.yml` port mappings

### Services not starting

```bash
# Check logs for errors
docker-compose logs

# Restart services
docker-compose restart

# Full reset
docker-compose down -v
docker-compose up -d
```

### Connection refused

Wait for health checks to pass. You can monitor health status:

```bash
docker-compose ps
```

Both services should show "healthy" status.

## Notes

- Uses Alpine-based images for smaller size
- Management plugin is pre-enabled
- Data is stored in Docker volumes
- Use `-v` flag with `docker-compose down` to remove data between tests
