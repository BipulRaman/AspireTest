using AspireApp1.CorrelationId;
using Microsoft.AspNetCore.Mvc;

namespace AspireApp1.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private static readonly List<User> Users = new()
    {
        new User { Id = 1, Name = "John Doe", Email = "john.doe@example.com", Age = 30, Department = "Engineering" },
        new User { Id = 2, Name = "Jane Smith", Email = "jane.smith@example.com", Age = 28, Department = "Marketing" },
        new User { Id = 3, Name = "Bob Johnson", Email = "bob.johnson@example.com", Age = 35, Department = "Sales" },
        new User { Id = 4, Name = "Alice Brown", Email = "alice.brown@example.com", Age = 32, Department = "Engineering" },
        new User { Id = 5, Name = "Charlie Wilson", Email = "charlie.wilson@example.com", Age = 29, Department = "HR" }
    };

    private readonly ILogger<UsersController> _logger;
    private readonly ICorrelationIdService _correlationIdService;

    public UsersController(ILogger<UsersController> logger, ICorrelationIdService correlationIdService)
    {
        _logger = logger;
        _correlationIdService = correlationIdService;
    }

    [HttpGet]
    public ActionResult<IEnumerable<User>> GetAll([FromQuery] string? department = null)
    {
        _logger.LogInformation("Getting users with department filter: {Department}", department ?? "none");
        
        var users = CorrelationIdHelper.ExecuteWithCorrelationId(_correlationIdService, () =>
        {
            var filteredUsers = string.IsNullOrWhiteSpace(department) 
                ? Users 
                : Users.Where(u => u.Department.Equals(department, StringComparison.OrdinalIgnoreCase));
            
            _logger.LogDebug("Found {UserCount} users", filteredUsers.Count());
            return filteredUsers;
        });

        return Ok(users);
    }

    [HttpGet("{id:int}")]
    public ActionResult<User> GetById(int id)
    {
        var user = Users.FirstOrDefault(u => u.Id == id);
        if (user == null)
        {
            return NotFound($"User with ID {id} not found");
        }
        return Ok(user);
    }

    [HttpGet("email/{email}")]
    public ActionResult<User> GetByEmail(string email)
    {
        var user = Users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        if (user == null)
        {
            return NotFound($"User with email '{email}' not found");
        }
        return Ok(user);
    }

    [HttpPost]
    public ActionResult<User> Create([FromBody] CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest("Name and Email are required");
        }

        if (Users.Any(u => u.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase)))
        {
            return Conflict($"User with email '{request.Email}' already exists");
        }

        var newId = Users.Max(u => u.Id) + 1;
        var user = new User
        {
            Id = newId,
            Name = request.Name,
            Email = request.Email,
            Age = request.Age,
            Department = request.Department ?? "General"
        };

        Users.Add(user);
        return CreatedAtAction(nameof(GetById), new { id = newId }, user);
    }

    [HttpPut("{id:int}")]
    public ActionResult<User> Update(int id, [FromBody] UpdateUserRequest request)
    {
        var user = Users.FirstOrDefault(u => u.Id == id);
        if (user == null)
        {
            return NotFound($"User with ID {id} not found");
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
            user.Name = request.Name;
        
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            if (Users.Any(u => u.Id != id && u.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase)))
            {
                return Conflict($"Another user with email '{request.Email}' already exists");
            }
            user.Email = request.Email;
        }
        
        if (request.Age.HasValue)
            user.Age = request.Age.Value;
        
        if (!string.IsNullOrWhiteSpace(request.Department))
            user.Department = request.Department;

        return Ok(user);
    }

    [HttpDelete("{id:int}")]
    public ActionResult Delete(int id)
    {
        var user = Users.FirstOrDefault(u => u.Id == id);
        if (user == null)
        {
            return NotFound($"User with ID {id} not found");
        }

        Users.Remove(user);
        return NoContent();
    }
}

public class User
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public int Age { get; set; }
    public required string Department { get; set; }
}

public class CreateUserRequest
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    public int Age { get; set; }
    public string? Department { get; set; }
}

public class UpdateUserRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public int? Age { get; set; }
    public string? Department { get; set; }
}
