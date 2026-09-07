using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ITG_Cafeteria.Server.Authorization;
using ITG_Cafeteria.Server.Configuration;
using ITG_Cafeteria.Server.Data;
using ITG_Cafeteria.Server.Models.DTOs;
using ITG_Cafeteria.Server.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ITG_Cafeteria.Server.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<CafeteriaUser?> GetUserByIdAsync(int userId);
}

public class AuthService : IAuthService
{
    private readonly CafeteriaDbContext _db;
    private readonly JwtSettings _jwtSettings;

    public AuthService(CafeteriaDbContext db, IOptions<JwtSettings> jwtSettings)
    {
        _db = db;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var username = request.Username.Trim();
        var user = await _db.CafeteriaUsers
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

        return new LoginResponse
        {
            Token = GenerateToken(user, roles),
            Username = user.Username,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Roles = roles
        };
    }

    public Task<CafeteriaUser?> GetUserByIdAsync(int userId) =>
        _db.CafeteriaUsers
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

    private string GenerateToken(CafeteriaUser user, IReadOnlyList<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(_jwtSettings.ExpirationHours);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
