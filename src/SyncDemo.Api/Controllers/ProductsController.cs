using Microsoft.AspNetCore.Mvc;
using SyncDemo.Api.Data;
using SyncDemo.Shared.Models;

namespace SyncDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _repository;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IProductRepository repository,
        ILogger<ProductsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<Product>>> GetAll()
    {
        var products = await _repository.GetAllAsync();
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetById(int id)
    {
        var product = await _repository.GetByIdAsync(id);
        if (product == null) return NotFound();
        return Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] Product product)
    {
        _logger.LogInformation($"Creating product: {product.Name}");
        
        // Only DB operation - Oracle Trigger + AQ handle the rest!
        var id = await _repository.CreateAsync(product);
        
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, [FromBody] Product product)
    {
        _logger.LogInformation($"Updating product: {id}");
        
        product.Id = id;
        
        // Only DB operation - Oracle Trigger + AQ handle the rest!
        await _repository.UpdateAsync(product);
        
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        _logger.LogInformation($"Deleting product: {id}");
        
        // Only DB operation - Oracle Trigger + AQ handle the rest!
        await _repository.DeleteAsync(id);
        
        return NoContent();
    }
}
