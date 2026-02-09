# Device Permission System - Test Documentation

## Overview

This document describes how to test the device-specific access control system.

## Prerequisites

1. Start the infrastructure:
   ```bash
   docker compose up -d
   ```

2. Wait for Oracle database to initialize (2-3 minutes on first start)

3. Start the API:
   ```bash
   cd src/SyncDemo.Api
   dotnet run
   ```

## Test Scenarios

### Scenario 1: Device Registration

#### Admin User Registration
```bash
curl -X POST http://localhost:5000/api/device/register \
  -H "Content-Type: application/json" \
  -d '{
    "deviceId": "admin-device-001",
    "deviceName": "Admin Desktop",
    "deviceType": "WPF",
    "username": "admin"
  }'
```

**Expected Result:**
- Success: true
- Device registered with `userId` linked to admin user
- Permissions array contains `EntityType: "ALL"`, `PermissionType: "ALL"`

#### Regular User Registration
```bash
curl -X POST http://localhost:5000/api/device/register \
  -H "Content-Type: application/json" \
  -d '{
    "deviceId": "user-device-001",
    "deviceName": "User Desktop",
    "deviceType": "WPF",
    "username": "user1"
  }'
```

**Expected Result:**
- Success: true
- Device registered with `userId` linked to user1
- Permissions array contains `EntityType: "SYNCITEMS"`, `PermissionType: "READ"`

#### Viewer User Registration
```bash
curl -X POST http://localhost:5000/api/device/register \
  -H "Content-Type: application/json" \
  -d '{
    "deviceId": "viewer-device-001",
    "deviceName": "Viewer Desktop",
    "deviceType": "WPF",
    "username": "viewer"
  }'
```

**Expected Result:**
- Success: true
- Device registered with `userId` linked to viewer user
- Permissions array contains `EntityType: "SYNCITEMS"`, `PermissionType: "READ"`

### Scenario 2: Permission Verification

#### Get Device Permissions
```bash
curl -X GET http://localhost:5000/api/device/admin-device-001/permissions
```

**Expected Result:**
- Returns array of DevicePermission objects for the device
- Admin device should have `EntityType: "ALL"`
- User/Viewer devices should have `EntityType: "SYNCITEMS"`

### Scenario 3: Permission-Based Sync

#### Admin Device Sync
```bash
curl -X GET "http://localhost:5000/api/syncitems/sync?deviceId=admin-device-001"
```

**Expected Result:**
- Returns all sync items (admin has full access)
- Success: true
- Items array contains all SyncItems from database

#### User Device Sync
```bash
curl -X GET "http://localhost:5000/api/syncitems/sync?deviceId=user-device-001"
```

**Expected Result:**
- Returns all sync items (user has READ permission to SYNCITEMS)
- Success: true
- Items array contains all SyncItems

#### Viewer Device Sync
```bash
curl -X GET "http://localhost:5000/api/syncitems/sync?deviceId=viewer-device-001"
```

**Expected Result:**
- Returns all sync items (viewer has READ permission to SYNCITEMS)
- Success: true
- Items array contains all SyncItems

### Scenario 4: Re-registration

#### Re-register Existing Device
```bash
curl -X POST http://localhost:5000/api/device/register \
  -H "Content-Type: application/json" \
  -d '{
    "deviceId": "admin-device-001",
    "deviceName": "Admin Desktop",
    "deviceType": "WPF",
    "username": "admin"
  }'
```

**Expected Result:**
- Success: true
- Message: "Device already registered"
- Device object returned with updated `LastSeen` timestamp
- Existing permissions returned

### Scenario 5: Sync Without Device ID

#### Sync Without Permission Check
```bash
curl -X GET "http://localhost:5000/api/syncitems/sync"
```

**Expected Result:**
- Returns all sync items (no permission filtering when deviceId is not provided)
- This is for backward compatibility

## Automated Testing

Run all tests using the provided script:

```bash
./scripts/test-permissions.sh
```

## Database Verification

Connect to Oracle and verify the data:

```bash
docker exec -it syncdemo-oracle sqlplus syncuser/syncpass@XEPDB1
```

```sql
-- Check users
SELECT * FROM USERS;

-- Check devices
SELECT * FROM DEVICES;

-- Check permissions
SELECT * FROM DEVICE_PERMISSIONS;

-- Join query to see full picture
SELECT 
    d.DEVICE_ID,
    d.DEVICE_NAME,
    u.USERNAME,
    u.ROLE,
    dp.ENTITY_TYPE,
    dp.PERMISSION_TYPE
FROM DEVICES d
JOIN USERS u ON d.USER_ID = u.USER_ID
LEFT JOIN DEVICE_PERMISSIONS dp ON d.DEVICE_ID = dp.DEVICE_ID
ORDER BY u.USERNAME, d.DEVICE_ID;
```

## WPF Client Testing

1. Start the WPF application:
   ```bash
   cd src/SyncDemo.WpfApp
   dotnet run
   ```

2. Test different users:
   - **Username**: `admin` → Should get full access
   - **Username**: `user1` → Should get READ access to SYNCITEMS
   - **Username**: `viewer` → Should get READ access to SYNCITEMS

3. Verify in the application:
   - Connection status should show successful registration
   - Status text should display username and device ID
   - Items should sync based on permissions

## MAUI Client Testing

1. Build and run the MAUI application (Windows or macOS only)

2. Test device registration:
   - Click "Register Device" button
   - Enter username (admin, user1, or viewer)
   - Verify registration success message

3. Test sync:
   - Click "Sync with Server"
   - Verify items are loaded based on permissions

## Expected Permission Matrix

| User Role | Entity Type | Permission Type | Expected Behavior |
|-----------|-------------|-----------------|-------------------|
| ADMIN     | ALL         | ALL            | Full access to everything |
| USER      | SYNCITEMS   | READ           | Read-only access to SyncItems |
| VIEWER    | SYNCITEMS   | READ           | Read-only access to SyncItems |

## Troubleshooting

### Device Registration Fails
- Check if API is running on http://localhost:5000
- Verify Oracle database is running and accessible
- Check API logs for errors

### Permission Check Fails
- Verify device is registered in DEVICES table
- Check DEVICE_PERMISSIONS table for permission entries
- Ensure USERS table has the correct user

### Sync Returns Empty
- Verify SyncItems exist in database
- Check if deviceId is passed correctly
- Review API logs for permission check failures

## Future Extensions

To extend the system for Customers and Products:

1. Add new permission checks in respective controllers
2. Update `GrantDefaultPermissionsAsync` in DeviceController
3. Modify `PermissionService` to handle new entity types
4. Update test scripts to include new entities
