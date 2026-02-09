using Microsoft.AspNetCore.Mvc;
using SyncDemo.Api.Data;
using SyncDemo.Shared.DTOs;
using SyncDemo.Shared.Models;

namespace SyncDemo.Api.Controllers;

/// <summary>
/// Controller for device registration and management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DeviceController : ControllerBase
{
    private readonly IDeviceRepository _deviceRepo;
    private readonly IUserRepository _userRepo;
    private readonly IDevicePermissionRepository _permissionRepo;
    private readonly ILogger<DeviceController> _logger;

    public DeviceController(
        IDeviceRepository deviceRepo,
        IUserRepository userRepo,
        IDevicePermissionRepository permissionRepo,
        ILogger<DeviceController> logger)
    {
        _deviceRepo = deviceRepo;
        _userRepo = userRepo;
        _permissionRepo = permissionRepo;
        _logger = logger;
    }

    /// <summary>
    /// Register a new device or update existing device registration
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<DeviceRegistrationResponse>> RegisterDevice([FromBody] DeviceRegistrationRequest request)
    {
        try
        {
            // Check if user exists
            var user = await _userRepo.GetByUsernameAsync(request.Username);
            if (user == null)
            {
                // Auto-create user (in production: authentication required!)
                user = await _userRepo.CreateUserAsync(new User
                {
                    Username = request.Username,
                    Email = $"{request.Username}@example.com",
                    Role = "USER"
                });
            }

            // Check if device is already registered
            var existingDevice = await _deviceRepo.GetByIdAsync(request.DeviceId);
            if (existingDevice != null)
            {
                await _deviceRepo.UpdateLastSeenAsync(request.DeviceId);
                
                var existingPermissions = await _permissionRepo.GetPermissionsForDeviceAsync(request.DeviceId);
                
                return Ok(new DeviceRegistrationResponse
                {
                    Success = true,
                    Message = "Device already registered",
                    Device = existingDevice,
                    Permissions = existingPermissions
                });
            }

            // Register new device
            var device = await _deviceRepo.RegisterDeviceAsync(new Device
            {
                DeviceId = request.DeviceId,
                DeviceName = request.DeviceName,
                UserId = user.UserId,
                DeviceType = request.DeviceType,
                IsActive = true
            });

            // Grant default permissions based on user role
            await GrantDefaultPermissionsAsync(request.DeviceId, user.Role);

            var permissions = await _permissionRepo.GetPermissionsForDeviceAsync(request.DeviceId);

            _logger.LogInformation($"Device {request.DeviceId} registered for user {user.Username}");

            return Ok(new DeviceRegistrationResponse
            {
                Success = true,
                Message = "Device registered successfully",
                Device = device,
                Permissions = permissions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering device");
            return StatusCode(500, new DeviceRegistrationResponse
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Get permissions for a specific device
    /// </summary>
    [HttpGet("{deviceId}/permissions")]
    public async Task<ActionResult<List<DevicePermission>>> GetPermissions(string deviceId)
    {
        var permissions = await _permissionRepo.GetPermissionsForDeviceAsync(deviceId);
        return Ok(permissions);
    }

    /// <summary>
    /// Grant default permissions based on user role
    /// </summary>
    private async Task GrantDefaultPermissionsAsync(string deviceId, string userRole)
    {
        // ADMIN = Access to everything
        if (userRole == "ADMIN")
        {
            await _permissionRepo.GrantPermissionAsync(new DevicePermission
            {
                DeviceId = deviceId,
                EntityType = "ALL",
                PermissionType = "ALL"
            });
        }
        // USER = READ on SyncItems (for now, since we only have SyncItems)
        else if (userRole == "USER")
        {
            await _permissionRepo.GrantPermissionAsync(new DevicePermission
            {
                DeviceId = deviceId,
                EntityType = "SYNCITEMS",
                PermissionType = "READ"
            });
        }
        // VIEWER = READ only on SyncItems
        else if (userRole == "VIEWER")
        {
            await _permissionRepo.GrantPermissionAsync(new DevicePermission
            {
                DeviceId = deviceId,
                EntityType = "SYNCITEMS",
                PermissionType = "READ"
            });
        }
    }
}
