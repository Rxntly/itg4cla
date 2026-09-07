namespace ITG_Cafeteria.Server.Models.Entities;

public class UserRole
{
    public int UserId { get; set; }
    public CafeteriaUser User { get; set; } = null!;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
}
