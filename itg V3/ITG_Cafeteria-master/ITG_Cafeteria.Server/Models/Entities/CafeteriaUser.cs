namespace ITG_Cafeteria.Server.Models.Entities;

public class CafeteriaUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<WeeklyMenu> WeeklyMenus { get; set; } = new List<WeeklyMenu>();
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
