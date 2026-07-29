using Microsoft.EntityFrameworkCore;
using RuleGate.DocumentApproval.Api.Domain;

namespace RuleGate.DocumentApproval.Api.Data;

public sealed class SampleDbContext(DbContextOptions<SampleDbContext> options)
    : DbContext(options)
{
    public DbSet<DocumentRecord> Documents => Set<DocumentRecord>();

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserProfile>().HasKey(profile => profile.Username);
        modelBuilder.Entity<DocumentRecord>().Property(document => document.Title).HasMaxLength(200);
        modelBuilder.Entity<DocumentRecord>().Property(document => document.Status).HasMaxLength(32);
    }
}
