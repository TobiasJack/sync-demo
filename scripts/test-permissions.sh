#!/bin/bash

# Test script for Device-Specific Access Control
# This script tests the device registration and permission system

API_URL="http://localhost:5000"

echo "================================"
echo "Device Permission System Tests"
echo "================================"
echo ""

# Test 1: Register Admin Device
echo "Test 1: Register Admin Device"
echo "------------------------------"
curl -X POST "${API_URL}/api/device/register" \
  -H "Content-Type: application/json" \
  -d '{
    "deviceId": "admin-device-001",
    "deviceName": "Admin Desktop",
    "deviceType": "WPF",
    "username": "admin"
  }'
echo ""
echo ""

# Test 2: Register User Device
echo "Test 2: Register User Device"
echo "------------------------------"
curl -X POST "${API_URL}/api/device/register" \
  -H "Content-Type: application/json" \
  -d '{
    "deviceId": "user-device-001",
    "deviceName": "User Desktop",
    "deviceType": "WPF",
    "username": "user1"
  }'
echo ""
echo ""

# Test 3: Register Viewer Device
echo "Test 3: Register Viewer Device"
echo "------------------------------"
curl -X POST "${API_URL}/api/device/register" \
  -H "Content-Type: application/json" \
  -d '{
    "deviceId": "viewer-device-001",
    "deviceName": "Viewer Desktop",
    "deviceType": "WPF",
    "username": "viewer"
  }'
echo ""
echo ""

# Test 4: Get Permissions for Admin Device
echo "Test 4: Get Permissions for Admin Device"
echo "-----------------------------------------"
curl -X GET "${API_URL}/api/device/admin-device-001/permissions"
echo ""
echo ""

# Test 5: Get Permissions for User Device
echo "Test 5: Get Permissions for User Device"
echo "----------------------------------------"
curl -X GET "${API_URL}/api/device/user-device-001/permissions"
echo ""
echo ""

# Test 6: Get Permissions for Viewer Device
echo "Test 6: Get Permissions for Viewer Device"
echo "------------------------------------------"
curl -X GET "${API_URL}/api/device/viewer-device-001/permissions"
echo ""
echo ""

# Test 7: Sync with Admin Device (should succeed)
echo "Test 7: Sync with Admin Device (should succeed)"
echo "-----------------------------------------------"
curl -X GET "${API_URL}/api/syncitems/sync?deviceId=admin-device-001"
echo ""
echo ""

# Test 8: Sync with User Device (should succeed)
echo "Test 8: Sync with User Device (should succeed)"
echo "----------------------------------------------"
curl -X GET "${API_URL}/api/syncitems/sync?deviceId=user-device-001"
echo ""
echo ""

# Test 9: Sync with Viewer Device (should succeed)
echo "Test 9: Sync with Viewer Device (should succeed)"
echo "------------------------------------------------"
curl -X GET "${API_URL}/api/syncitems/sync?deviceId=viewer-device-001"
echo ""
echo ""

# Test 10: Re-register existing device (should return existing device)
echo "Test 10: Re-register Existing Device"
echo "------------------------------------"
curl -X POST "${API_URL}/api/device/register" \
  -H "Content-Type: application/json" \
  -d '{
    "deviceId": "admin-device-001",
    "deviceName": "Admin Desktop",
    "deviceType": "WPF",
    "username": "admin"
  }'
echo ""
echo ""

echo "================================"
echo "All tests completed!"
echo "================================"
