namespace ITG_Cafeteria.Server.Authorization;

public static class RoleNames
{
    public const string SystemAdmin = "SystemAdmin";
    public const string CafeteriaEditor = "CafeteriaEditor";
    public const string CafeteriaPublisher = "CafeteriaPublisher";

    public const string MenuStaff = $"{CafeteriaEditor},{CafeteriaPublisher},{SystemAdmin}";
    public const string PublisherOnly = $"{CafeteriaPublisher},{SystemAdmin}";
    public const string AdminOnly = SystemAdmin;

    public static readonly string[] AllRoles = [SystemAdmin, CafeteriaEditor, CafeteriaPublisher];
    public static readonly string[] CafeteriaRoles = [CafeteriaEditor, CafeteriaPublisher];
}
