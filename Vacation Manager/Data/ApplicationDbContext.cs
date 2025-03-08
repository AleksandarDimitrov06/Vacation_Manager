using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Vacation_Manager.Models;


namespace Vacation_Manager.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<VacationRequest> VacationRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships

            // 1. Users to Roles (Many-to-One)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId);

            // Seed the fixed roles
            modelBuilder.Entity<Role>().HasData(
                new Role { RoleId = 1, RoleName = "CEO" },
                new Role { RoleId = 2, RoleName = "Developer" },
                new Role { RoleId = 3, RoleName = "Team Lead" },
                new Role { RoleId = 4, RoleName = "Unassigned" }
            );
            modelBuilder.Entity<User>()
                .HasOne(u => u.Team)
                .WithMany(r => r.Members)
                .HasForeignKey(u => u.TeamId);

            modelBuilder.Entity<Team>()
                .HasOne(t => t.Project)
                .WithMany(p => p.Teams)
                .HasForeignKey(t =>  t.ProjectId);

            modelBuilder.Entity<Team>()
                .HasOne(t => t.TeamLeader)
                .WithOne(u => u.LedTeam)
                .HasForeignKey<Team>(t => t.TeamLeaderId);

        }
    }
}
