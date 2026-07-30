using Microsoft.EntityFrameworkCore;
using RuleGate.DocumentApproval.Api.Domain;

namespace RuleGate.DocumentApproval.Api.Data;

public sealed class SampleDbContext(DbContextOptions<SampleDbContext> options)
    : DbContext(options)
{
    public DbSet<DocumentRecord> Documents => Set<DocumentRecord>();

    public DbSet<OrganizationSchedule> OrganizationSchedules => Set<OrganizationSchedule>();

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserProfile>().HasKey(profile => profile.Username);
        modelBuilder.Entity<OrganizationSchedule>().HasKey(schedule => schedule.OrganizationId);
        modelBuilder.Entity<OrganizationSchedule>().Property(schedule => schedule.OrganizationId)
            .HasMaxLength(64);
        modelBuilder.Entity<OrganizationSchedule>().Property(schedule => schedule.TimeZoneId)
            .HasMaxLength(128);
        modelBuilder.Entity<DocumentRecord>().Property(document => document.Title).HasMaxLength(200);
        modelBuilder.Entity<DocumentRecord>().Property(document => document.Status).HasMaxLength(32);
    }
}
