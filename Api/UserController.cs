using MessengerApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace MessengerApi.Api;

[ApiController]
[Route("users")]
public class UsersController : ControllerBase
{
    private readonly UserService _users;

    public UsersController(UserService users) => _users = users;

    //Create a new user.
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var user = await _users.CreateUserAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    //Get all users.
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _users.GetAllUsersAsync());

    //Get a user by ID.
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id) =>
        Ok(await _users.GetUserAsync(id));
}