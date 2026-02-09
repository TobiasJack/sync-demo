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

    public SyncItemsController(
        ISyncItemRepository repository,
        IMessageQueueService messageQueue,
        IHubContext<SyncHub> hubContext,
        ILogger<SyncItemsController> logger)
    {
        _repository = repository;
        _messageQueue = messageQueue;
        _hubContext = hubContext;
        _logger = logger;
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
            
            // Broadcast via SignalR
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
            
            // Broadcast via SignalR
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
            
            // Broadcast via SignalR
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
    public async Task<ActionResult<SyncResult>> Sync([FromQuery] DateTime? since)
    {
        try
        {
            var items = since.HasValue 
                ? await _repository.GetModifiedSinceAsync(since.Value)
                : await _repository.GetAllAsync();
            
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
