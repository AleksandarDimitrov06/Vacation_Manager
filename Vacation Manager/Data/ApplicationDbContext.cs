using Microsoft.AspNetCore.Identity;
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
       
        
        public DbSet<Team> Teams { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<VacationRequest> VacationRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            


            modelBuilder.Entity<IdentityRole>().HasData(
                new IdentityRole { Name = "CEO", NormalizedName = "CEO" },
                new IdentityRole { Name = "Developer", NormalizedName = "DEVELOPER" },
                new IdentityRole { Name = "Team Lead", NormalizedName = "TEAMLEAD" },
                new IdentityRole { Name = "Unassigned", NormalizedName = "UNASSIGNED" }
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

            modelBuilder.Entity<VacationRequest>()
                .HasOne(v => v.Requester)
                .WithMany(u => u.RequestedVacations)
                .HasForeignKey(v => v.RequesterId);

            modelBuilder.Entity<VacationRequest>()
                .HasOne(v => v.Approver)
                .WithMany(u => u.ApprovedVacations)
                .HasForeignKey(v => v.ApproverId);

        }
    }
}
