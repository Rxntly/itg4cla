using ITG_Cafeteria.Server.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ITG_Cafeteria.Server.Data;

public class CafeteriaDbContext : DbContext
{
    public CafeteriaDbContext(DbContextOptions<CafeteriaDbContext> options) : base(options)
    {
    }

    public DbSet<CafeteriaUser> CafeteriaUsers => Set<CafeteriaUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<WeeklyMenu> WeeklyMenus => Set<WeeklyMenu>();
    public DbSet<MenuDay> MenuDays => Set<MenuDay>();
    public DbSet<MenuNode> MenuNodes => Set<MenuNode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CafeteriaUser>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Username).HasMaxLength(100);
            entity.Property(u => u.Email).HasMaxLength(256);
            entity.Property(u => u.DisplayName).HasMaxLength(200);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(r => r.Name).IsUnique();
            entity.Property(r => r.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(ur => new { ur.UserId, ur.RoleId });
            entity.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WeeklyMenu>(entity =>
        {
            entity.HasIndex(m => m.WeekStartDate);
            entity.HasOne(m => m.CreatedByUser)
                .WithMany(u => u.WeeklyMenus)
                .HasForeignKey(m => m.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MenuDay>(entity =>
        {
            entity.HasIndex(d => new { d.WeeklyMenuId, d.DayOfWeek }).IsUnique();
            entity.HasOne(d => d.WeeklyMenu)
                .WithMany(m => m.Days)
                .HasForeignKey(d => d.WeeklyMenuId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MenuNode>(entity =>
        {
            entity.Property(n => n.Label).HasMaxLength(500);
            entity.HasIndex(n => new { n.MenuDayId, n.ParentId, n.SortOrder });
            entity.HasOne(n => n.MenuDay)
                .WithMany(d => d.Nodes)
                .HasForeignKey(n => n.MenuDayId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(n => n.Parent)
                .WithMany(n => n.Children)
                .HasForeignKey(n => n.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
