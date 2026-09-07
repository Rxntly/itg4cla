using ITG_Cafeteria.Server.Authorization;
using ITG_Cafeteria.Server.Data;
using ITG_Cafeteria.Server.Models.DTOs;
using ITG_Cafeteria.Server.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ITG_Cafeteria.Server.Services;

public interface IUserService
{
    Task<List<UserDto>> GetAllUsersAsync();
    Task<UserDto?> GetUserByIdAsync(int id);
    Task<(UserDto? User, string? Error)> CreateUserAsync(CreateUserRequest request);
    Task<(UserDto? User, string? Error)> UpdateUserAsync(int id, UpdateUserRequest request);
    Task<(bool Success, string? Error)> DeactivateUserAsync(int id);
}

public class UserService : IUserService
{
    private readonly CafeteriaDbContext _db;

    public UserService(CafeteriaDbContext db)
    {
        _db = db;
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        var users = await _db.CafeteriaUsers
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .OrderBy(u => u.Username)
            .ToListAsync();

        return users.Select(ToDto).ToList();
    }

    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        var user = await _db.CafeteriaUsers
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id);

        return user == null ? null : ToDto(user);
    }

    public async Task<(UserDto? User, string? Error)> CreateUserAsync(CreateUserRequest request)
    {
        var username = request.Username.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(username))
            return (null, "Username is required.");
        if (string.IsNullOrWhiteSpace(request.Password))
            return (null, "Password is required.");
        if (string.IsNullOrWhiteSpace(request.DisplayName))
            return (null, "Display name is required.");

        var roleError = await ValidateRolesAsync(request.Roles, allowSystemAdmin: false);
        if (roleError != null)
            return (null, roleError);

        if (await _db.CafeteriaUsers.AnyAsync(u => u.Username == username))
            return (null, "Username is already taken.");
        if (await _db.CafeteriaUsers.AnyAsync(u => u.Email == email))
            return (null, "Email is already in use.");

        var roles = await GetRolesByNamesAsync(request.Roles);
        var user = new CafeteriaUser
        {
            Username = username,
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsActive = true
        };

        foreach (var role in roles)
        {
            user.UserRoles.Add(new UserRole { Role = role });
        }

        _db.CafeteriaUsers.Add(user);
        await _db.SaveChangesAsync();

        return (await GetUserByIdAsync(user.Id), null);
    }

    public async Task<(UserDto? User, string? Error)> UpdateUserAsync(int id, UpdateUserRequest request)
    {
        var user = await _db.CafeteriaUsers
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return (null, "User not found.");

        if (user.Username == "SystemAdmin")
            return (null, "The System Administrator account cannot be modified.");

        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(request.DisplayName))
            return (null, "Display name is required.");

        var roleError = await ValidateRolesAsync(request.Roles, allowSystemAdmin: false);
        if (roleError != null)
            return (null, roleError);

        if (await _db.CafeteriaUsers.AnyAsync(u => u.Email == email && u.Id != id))
            return (null, "Email is already in use.");

        user.Email = email;
        user.DisplayName = request.DisplayName.Trim();

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        var roles = await GetRolesByNamesAsync(request.Roles);
        _db.UserRoles.RemoveRange(user.UserRoles);
        user.UserRoles.Clear();

        foreach (var role in roles)
        {
            user.UserRoles.Add(new UserRole { RoleId = role.Id, UserId = user.Id });
        }

        await _db.SaveChangesAsync();
        return (await GetUserByIdAsync(id), null);
    }

    public async Task<(bool Success, string? Error)> DeactivateUserAsync(int id)
    {
        var user = await _db.CafeteriaUsers.FindAsync(id);
        if (user == null)
            return (false, "User not found.");

        if (user.Username == "SystemAdmin")
            return (false, "The System Administrator account cannot be deactivated.");

        user.IsActive = false;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    private Task<string?> ValidateRolesAsync(IReadOnlyList<string> roleNames, bool allowSystemAdmin)
    {
        if (roleNames.Count == 0)
            return Task.FromResult<string?>("At least one role must be assigned.");

        var invalid = roleNames.Where(r => !RoleNames.AllRoles.Contains(r)).ToList();
        if (invalid.Count > 0)
            return Task.FromResult<string?>($"Invalid role(s): {string.Join(", ", invalid)}.");

        if (!allowSystemAdmin && roleNames.Contains(RoleNames.SystemAdmin))
            return Task.FromResult<string?>("The System Administrator role cannot be assigned through user management.");

        if (roleNames.Contains(RoleNames.SystemAdmin) && roleNames.Count > 1)
            return Task.FromResult<string?>("System Administrator cannot be combined with other roles.");

        return Task.FromResult<string?>(null);
    }

    private async Task<List<Role>> GetRolesByNamesAsync(IReadOnlyList<string> roleNames)
    {
        return await _db.Roles.Where(r => roleNames.Contains(r.Name)).ToListAsync();
    }

    private static UserDto ToDto(CafeteriaUser user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        DisplayName = user.DisplayName,
        IsActive = user.IsActive,
        Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList(),
        CreatedAt = user.CreatedAt
    };
}
