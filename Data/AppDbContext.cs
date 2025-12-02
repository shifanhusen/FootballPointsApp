using FootballPointsApp.Models;
using Microsoft.EntityFrameworkCore;

namespace FootballPointsApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Player> Players { get; set; }
    public DbSet<Match> Matches { get; set; }
    public DbSet<MatchRsvp> MatchRsvps { get; set; }
    public DbSet<MatchPlayer> MatchPlayers { get; set; }
    public DbSet<PointsLog> PointsLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure relationships if needed, but conventions should work for most.
        
        // Ensure enums are stored as integers or strings? 
        // EF Core defaults to int for enums, which matches the user's enum definitions (TeamA=1, etc).
        
        // Unique constraint on MatchRsvp (Player + Match)
        modelBuilder.Entity<MatchRsvp>()
            .HasIndex(r => new { r.MatchId, r.PlayerId })
            .IsUnique();

        // Unique constraint on MatchPlayer (Player + Match)
        modelBuilder.Entity<MatchPlayer>()
            .HasIndex(mp => new { mp.MatchId, mp.PlayerId })
            .IsUnique();
    }
}
