using ITG_Cafeteria.Server.Authorization;
using ITG_Cafeteria.Server.Models.DTOs;
using ITG_Cafeteria.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITG_Cafeteria.Server.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = RoleNames.AdminOnly)]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAll()
    {
        return Ok(await _userService.GetAllUsersAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> GetById(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        return user == null ? NotFound() : Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserRequest request)
    {
        var (user, error) = await _userService.CreateUserAsync(request);
        if (error != null)
            return BadRequest(new { message = error });

        return CreatedAtAction(nameof(GetById), new { id = user!.Id }, user);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserDto>> Update(int id, [FromBody] UpdateUserRequest request)
    {
        var (user, error) = await _userService.UpdateUserAsync(id, request);
        if (error == "User not found")
            return NotFound(new { message = error });
        if (error != null)
            return BadRequest(new { message = error });

        return Ok(user);
    }

    [HttpPost("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var (success, error) = await _userService.DeactivateUserAsync(id);
        if (!success && error == "User not found")
            return NotFound(new { message = error });
        if (!success)
            return BadRequest(new { message = error });

        return NoContent();
    }
}
