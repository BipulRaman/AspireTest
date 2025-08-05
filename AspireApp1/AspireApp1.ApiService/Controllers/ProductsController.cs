using AspireApp1.CorrelationId;
using Microsoft.AspNetCore.Mvc;

namespace AspireApp1.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private static readonly List<Product> Products = new()
    {
        new Product { Id = 1, Name = "Laptop", Price = 999.99m, Category = "Electronics", InStock = true },
        new Product { Id = 2, Name = "Smartphone", Price = 599.99m, Category = "Electronics", InStock = true },
        new Product { Id = 3, Name = "Desk Chair", Price = 199.99m, Category = "Furniture", InStock = false },
        new Product { Id = 4, Name = "Coffee Mug", Price = 12.99m, Category = "Kitchen", InStock = true },
        new Product { Id = 5, Name = "Book", Price = 24.99m, Category = "Books", InStock = true }
    };

    private readonly ILogger<ProductsController> _logger;
    private readonly ICorrelationIdService _correlationIdService;

    public ProductsController(ILogger<ProductsController> logger, ICorrelationIdService correlationIdService)
    {
        _logger = logger;
        _correlationIdService = correlationIdService;
    }

    [HttpGet]
    public ActionResult<IEnumerable<Product>> GetAll()
    {
        _logger.LogInformation("Getting all products");
        
        _logger.LogDebug("Retrieved {ProductCount} products", Products.Count);
        return Ok(Products);
    }

    [HttpGet("{id:int}")]
    public ActionResult<Product> GetById(int id)
    {
        _logger.LogInformation("Getting product by ID: {ProductId}", id);
        
        var product = Products.FirstOrDefault(p => p.Id == id);
        if (product == null)
        {
            _logger.LogWarning("Product with ID {ProductId} not found", id);
            return NotFound($"Product with ID {id} not found");
        }
        
        _logger.LogDebug("Found product: {ProductName}", product.Name);
        return Ok(product);
    }

    [HttpGet("category/{category}")]
    public ActionResult<IEnumerable<Product>> GetByCategory(string category)
    {
        var products = Products.Where(p => 
            p.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
        
        if (!products.Any())
        {
            return NotFound($"No products found in category '{category}'");
        }

        return Ok(products);
    }

    [HttpGet("search")]
    public ActionResult<IEnumerable<Product>> Search([FromQuery] string? name, [FromQuery] decimal? maxPrice)
    {
        var query = Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(p => p.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }

        return Ok(query.ToList());
    }

    [HttpPost]
    public ActionResult<Product> Create([FromBody] CreateProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Product name is required");
        }

        var newId = Products.Max(p => p.Id) + 1;
        var product = new Product
        {
            Id = newId,
            Name = request.Name,
            Price = request.Price,
            Category = request.Category ?? "General",
            InStock = request.InStock
        };

        Products.Add(product);
        return CreatedAtAction(nameof(GetById), new { id = newId }, product);
    }

    [HttpPut("{id:int}")]
    public ActionResult<Product> Update(int id, [FromBody] UpdateProductRequest request)
    {
        var product = Products.FirstOrDefault(p => p.Id == id);
        if (product == null)
        {
            return NotFound($"Product with ID {id} not found");
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
            product.Name = request.Name;
        
        if (request.Price.HasValue)
            product.Price = request.Price.Value;
        
        if (!string.IsNullOrWhiteSpace(request.Category))
            product.Category = request.Category;
        
        if (request.InStock.HasValue)
            product.InStock = request.InStock.Value;

        return Ok(product);
    }

    [HttpDelete("{id:int}")]
    public ActionResult Delete(int id)
    {
        var product = Products.FirstOrDefault(p => p.Id == id);
        if (product == null)
        {
            return NotFound($"Product with ID {id} not found");
        }

        Products.Remove(product);
        return NoContent();
    }
}

public class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
    public required string Category { get; set; }
    public bool InStock { get; set; }
}

public class CreateProductRequest
{
    public required string Name { get; set; }
    public decimal Price { get; set; }
    public string? Category { get; set; }
    public bool InStock { get; set; } = true;
}

public class UpdateProductRequest
{
    public string? Name { get; set; }
    public decimal? Price { get; set; }
    public string? Category { get; set; }
    public bool? InStock { get; set; }
}
