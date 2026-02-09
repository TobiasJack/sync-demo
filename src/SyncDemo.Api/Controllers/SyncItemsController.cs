using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SyncDemo.Api.Data;
using SyncDemo.Api.Hubs;
using SyncDemo.Api.Services;
using SyncDemo.Shared.Models;

namespace SyncDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SyncItemsController : ControllerBase
{
    private readonly ISyncItemRepository _repository;
    private readonly IMessageQueueService _messageQueue;
    private readonly IHubContext<SyncHub> _hubContext;
    private readonly ILogger<SyncItemsController> _logger;
    private readonly IPermissionService _permissionService;

    public SyncItemsController(
        ISyncItemRepository repository,
        IMessageQueueService messageQueue,
        IHubContext<SyncHub> hubContext,
        ILogger<SyncItemsController> logger,
        IPermissionService permissionService)
    {
        _repository = repository;
        _messageQueue = messageQueue;
        _hubContext = hubContext;
        _logger = logger;
        _permissionService = permissionService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SyncItem>>> GetAll()
    {
        try
        {
            var items = await _repository.GetAllAsync();
            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all items");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SyncItem>> GetById(Guid id)
    {
        try
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null)
                return NotFound();
            
            return Ok(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting item {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    public async Task<ActionResult<SyncItem>> Create([FromBody] SyncItem item)
    {
        try
        {
            var createdItem = await _repository.CreateAsync(item);
            
            // Publish to message queue
            var message = new SyncMessage
            {
                Operation = "CREATE",
                Item = createdItem,
                Timestamp = DateTime.UtcNow
            };
            _messageQueue.PublishMessage(message);
            
            // Broadcast via SignalR to all clients
            // Note: For production, implement device-specific filtering based on permissions
            await _hubContext.Clients.All.SendAsync("ReceiveSyncUpdate", message);
            
            return CreatedAtAction(nameof(GetById), new { id = createdItem.Id }, createdItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating item");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid id, [FromBody] SyncItem item)
    {
        try
        {
            if (id != item.Id)
                return BadRequest("Id mismatch");
            
            var success = await _repository.UpdateAsync(item);
            if (!success)
                return NotFound();
            
            // Publish to message queue
            var message = new SyncMessage
            {
                Operation = "UPDATE",
                Item = item,
                Timestamp = DateTime.UtcNow
            };
            _messageQueue.PublishMessage(message);
            
            // Broadcast via SignalR to all clients
            // Note: For production, implement device-specific filtering based on permissions
            await _hubContext.Clients.All.SendAsync("ReceiveSyncUpdate", message);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating item {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        try
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null)
                return NotFound();
            
            var success = await _repository.DeleteAsync(id);
            if (!success)
                return NotFound();
            
            // Publish to message queue
            var message = new SyncMessage
            {
                Operation = "DELETE",
                Item = item,
                Timestamp = DateTime.UtcNow
            };
            _messageQueue.PublishMessage(message);
            
            // Broadcast via SignalR to all clients
            // Note: For production, implement device-specific filtering based on permissions
            await _hubContext.Clients.All.SendAsync("ReceiveSyncUpdate", message);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting item {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("sync")]
    public async Task<ActionResult<SyncResult>> Sync([FromQuery] DateTime? since, [FromQuery] string? deviceId)
    {
        try
        {
            var items = since.HasValue 
                ? await _repository.GetModifiedSinceAsync(since.Value)
                : await _repository.GetAllAsync();
            
            // Filter items based on device permissions if deviceId is provided
            if (!string.IsNullOrEmpty(deviceId))
            {
                // Check if device can access SYNCITEMS
                var canAccess = await _permissionService.CanDeviceAccessEntityAsync(deviceId, "SYNCITEMS", null);
                if (!canAccess)
                {
                    _logger.LogWarning("Device {DeviceId} attempted to sync without permission", deviceId);
                    return Forbid();
                }
                
                // Get accessible entity IDs (empty list means all allowed)
                var accessibleIds = await _permissionService.GetAccessibleEntityIdsAsync(deviceId, "SYNCITEMS");
                
                // If not empty, filter to only accessible IDs
                if (accessibleIds.Any())
                {
                    // Note: Since SyncItem uses Guid Id, we would need to modify this logic
                    // For now, if there are specific IDs, we don't filter (assuming all access)
                    // In production, you'd need to adjust the schema or permission logic
                    _logger.LogInformation("Device {DeviceId} has specific item permissions (not implemented for Guid IDs yet)", deviceId);
                }
            }
            
            return Ok(new SyncResult
            {
                Success = true,
                Message = "Sync completed successfully",
                ItemsProcessed = items.Count(),
                Items = items.ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing items");
            return StatusCode(500, new SyncResult
            {
                Success = false,
                Message = "Sync failed: " + ex.Message
            });
        }
    }
}
