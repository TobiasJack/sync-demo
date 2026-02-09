using Microsoft.AspNetCore.Mvc;
using SyncDemo.Api.Data;
using SyncDemo.Shared.Models;

namespace SyncDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerRepository _repository;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(
        ICustomerRepository repository,
        ILogger<CustomersController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<Customer>>> GetAll()
    {
        var customers = await _repository.GetAllAsync();
        return Ok(customers);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Customer>> GetById(int id)
    {
        var customer = await _repository.GetByIdAsync(id);
        if (customer == null) return NotFound();
        return Ok(customer);
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] Customer customer)
    {
        _logger.LogInformation($"Creating customer: {customer.Name}");
        
        // Only DB operation - Oracle Trigger + AQ handle the rest!
        var id = await _repository.CreateAsync(customer);
        
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, [FromBody] Customer customer)
    {
        _logger.LogInformation($"Updating customer: {id}");
        
        customer.Id = id;
        
        // Only DB operation - Oracle Trigger + AQ handle the rest!
        await _repository.UpdateAsync(customer);
        
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        _logger.LogInformation($"Deleting customer: {id}");
        
        // Only DB operation - Oracle Trigger + AQ handle the rest!
        await _repository.DeleteAsync(id);
        
        return NoContent();
    }
}
