# Oracle AQ Event-Driven Architecture - Testing Guide

## Overview

This guide provides instructions for testing the new Oracle Advanced Queuing event-driven architecture.

## Prerequisites

- Docker Desktop running
- .NET 8 SDK installed
- curl or Postman for API testing

## Setup

### 1. Start Infrastructure

```bash
# Start Oracle DB, RabbitMQ, and API
docker-compose up -d

# Wait for Oracle to be ready (first start may take 2-3 minutes)
docker-compose logs -f oracle

# Verify all services are running
docker-compose ps
```

Expected services:
- `syncdemo-oracle` - Healthy
- `syncdemo-rabbitmq` - Healthy
- `syncdemo-api` - Running

### 2. Verify Oracle AQ Setup

Connect to Oracle and check the queue:

```bash
docker exec -it syncdemo-oracle sqlplus syncuser/syncpass@XEPDB1

-- Check if queue is created and started
SELECT queue_name, queue_table, enqueue_enabled, dequeue_enabled 
FROM user_queues;

-- Check queue table
SELECT * FROM AQ$SYNC_CHANGES_QUEUE_TABLE;

-- Exit
exit;
```

### 3. Check API Logs

```bash
docker-compose logs -f api
```

Look for:
- `OracleQueueService starting - Event-Driven Architecture`
- `Oracle AQ Listener started`

## Test Scenarios

### Test 1: Create Customer (Event-Driven)

```bash
# Create a new customer
curl -X POST http://localhost:5000/api/customers \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test Customer",
    "email": "test@example.com",
    "phone": "+1-555-0100"
  }'
```

**Expected Behavior:**
1. API responds with customer ID
2. Oracle trigger fires and enqueues message to AQ
3. OracleQueueService receives message (check logs)
4. Message is processed and distributed

**Check Logs:**
```bash
docker-compose logs api | grep -E "Creating customer|Received AQ message|Processing change"
```

Expected output:
```
Creating customer: Test Customer
Received AQ message: CUSTOMERS - INSERT (ID: 1)
Processing change: CUSTOMERS (INSERT) - Record ID: 1
Broadcasting to X authorized devices
```

### Test 2: Update Customer

```bash
# Update the customer
curl -X PUT http://localhost:5000/api/customers/1 \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Updated Customer",
    "email": "updated@example.com",
    "phone": "+1-555-0200"
  }'
```

**Expected Behavior:**
- UPDATE operation captured by Oracle trigger
- Message enqueued and processed
- Check logs for "CUSTOMERS - UPDATE"

### Test 3: Create Product

```bash
# Create a new product
curl -X POST http://localhost:5000/api/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test Product",
    "description": "A test product",
    "price": 99.99,
    "stock": 10
  }'
```

**Expected Behavior:**
- Product created
- Oracle trigger fires for PRODUCTS table
- Event processed by OracleQueueService

### Test 4: Delete Operations

```bash
# Delete customer
curl -X DELETE http://localhost:5000/api/customers/1

# Delete product
curl -X DELETE http://localhost:5000/api/products/1
```

**Expected Behavior:**
- DELETE operations captured
- Messages with operation="DELETE" processed

### Test 5: Verify Data Consistency

```bash
# Check database directly
docker exec -it syncdemo-oracle sqlplus syncuser/syncpass@XEPDB1

-- Check customers
SELECT * FROM CUSTOMERS;

-- Check products
SELECT * FROM PRODUCTS;

-- Check sync changes audit log
SELECT * FROM SYNC_CHANGES ORDER BY CHANGE_TIMESTAMP DESC;

exit;
```

### Test 6: Monitor Queue Messages

```bash
docker exec -it syncdemo-oracle sqlplus syncuser/syncpass@XEPDB1

-- Count messages in queue
SELECT COUNT(*) FROM AQ$SYNC_CHANGES_QUEUE_TABLE;

-- View message details (if any pending)
SELECT msg_id, enq_time, msg_state, deq_time 
FROM AQ$SYNC_CHANGES_QUEUE_TABLE 
ORDER BY enq_time DESC;

exit;
```

## Performance Testing

### Test 7: Bulk Operations

```bash
# Create multiple customers rapidly
for i in {1..10}; do
  curl -X POST http://localhost:5000/api/customers \
    -H "Content-Type: application/json" \
    -d "{\"name\": \"Customer $i\", \"email\": \"customer$i@example.com\"}"
  echo ""
done
```

**Monitor:**
- API logs for processing speed
- Queue processing time
- No message loss

### Test 8: Concurrent Operations

Run multiple operations simultaneously:

```bash
# Terminal 1 - Create customers
while true; do
  curl -X POST http://localhost:5000/api/customers \
    -H "Content-Type: application/json" \
    -d '{"name": "Concurrent Customer", "email": "concurrent@example.com"}'
  sleep 1
done

# Terminal 2 - Create products
while true; do
  curl -X POST http://localhost:5000/api/products \
    -H "Content-Type: application/json" \
    -d '{"name": "Concurrent Product", "price": 49.99, "stock": 5}'
  sleep 1
done
```

**Monitor:**
- All messages processed in order
- No duplicate processing
- No errors in logs

## Troubleshooting

### Issue: Oracle AQ Listener not starting

```bash
# Check Oracle connection string
docker-compose logs api | grep "Oracle"

# Verify Oracle is healthy
docker exec syncdemo-oracle sqlplus -s sys/OraclePwd123@localhost:1521/XEPDB1 as sysdba <<< 'SELECT 1 FROM DUAL;'
```

### Issue: Messages not being processed

```bash
# Check if OracleQueueService is running
docker-compose logs api | grep "OracleQueueService"

# Check queue status
docker exec -it syncdemo-oracle sqlplus syncuser/syncpass@XEPDB1 <<EOF
SELECT queue_name, enqueue_enabled, dequeue_enabled 
FROM user_queues 
WHERE queue_name = 'SYNC_CHANGES_QUEUE';
EOF
```

### Issue: No messages in queue

This is expected! Messages are dequeued immediately by the listener. To see messages:

1. Stop the API: `docker-compose stop api`
2. Create a customer via direct SQL
3. Check queue: Should now contain message
4. Start API: `docker-compose start api`
5. Message should be processed immediately

## Swagger UI Testing

Alternative to curl, use Swagger UI:

1. Open http://localhost:5000/swagger
2. Test endpoints:
   - **POST /api/customers** - Create customer
   - **GET /api/customers** - List customers
   - **PUT /api/customers/{id}** - Update customer
   - **DELETE /api/customers/{id}** - Delete customer
   - Same for **/api/products**

## RabbitMQ Monitoring

Check offline device queues:

1. Open http://localhost:15672
2. Login: guest / guest
3. Navigate to "Queues" tab
4. Look for device-specific queues (e.g., `device-xxx`)

## Success Criteria

✅ All API endpoints respond correctly  
✅ Oracle triggers fire on INSERT/UPDATE/DELETE  
✅ Messages enqueued to Oracle AQ  
✅ OracleQueueService processes messages in real-time  
✅ No messages lost or duplicated  
✅ No errors in application logs  
✅ SYNC_CHANGES table populated with audit trail  
✅ Performance is good (< 100ms processing time per message)

## Cleanup

```bash
# Stop all services
docker-compose down

# Remove volumes (resets database)
docker-compose down -v
```

## Next Steps

After successful testing:

1. Implement client-side SignalR handlers for `ReceiveUpdate` event
2. Add device registration and permission setup
3. Test with actual WPF/MAUI clients
4. Load testing with many concurrent operations
5. Production deployment considerations
