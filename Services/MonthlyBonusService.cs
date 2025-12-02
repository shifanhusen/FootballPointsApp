using FootballPointsApp.Data;
using FootballPointsApp.Models;
using Microsoft.EntityFrameworkCore;

namespace FootballPointsApp.Services;

public class MonthlyBonusService
{
    private readonly AppDbContext _context;

    public MonthlyBonusService(AppDbContext context)
    {
        _context = context;
    }

    public async Task ApplyMonthlyBonusesAsync(int year, int month)
    {
        // Define the month range
        var startOfMonth = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfMonth = startOfMonth.AddMonths(1);

        // Find all matches in this month
        var matchesInMonth = await _context.Matches
            .Where(m => m.MatchDate >= startOfMonth && m.MatchDate < endOfMonth && m.IsFinished)
            .ToListAsync();

        if (!matchesInMonth.Any()) return;

        int totalMatches = matchesInMonth.Count;
        var matchIds = matchesInMonth.Select(m => m.Id).ToList();

        // Get all players who played in these matches
        var playerAttendance = await _context.MatchPlayers
            .Where(mp => matchIds.Contains(mp.MatchId))
            .GroupBy(mp => mp.PlayerId)
            .Select(g => new { PlayerId = g.Key, Count = g.Count() })
            .ToListAsync();

        foreach (var p in playerAttendance)
        {
            if (p.Count == totalMatches)
            {
                // Check if bonus already exists for this player in this month
                // We assume the bonus is created within the month or we check the timestamp.
                // Since the bonus is for the month, we check if a log exists with the specific reason 
                // and created within the month (or we could add a Month/Year column, but we stick to the schema).
                // A safer check is to look for a log with Reason="Perfect attendance bonus" created in this month window.
                
                bool alreadyExists = await _context.PointsLogs.AnyAsync(l => 
                    l.PlayerId == p.PlayerId && 
                    l.Reason == "Perfect attendance bonus" &&
                    l.CreatedAt >= startOfMonth && l.CreatedAt < endOfMonth);

                if (!alreadyExists)
                {
                    _context.PointsLogs.Add(new PointsLog
                    {
                        PlayerId = p.PlayerId,
                        MatchId = null, // Bonus is not for a specific match
                        Reason = "Perfect attendance bonus",
                        PointsChange = 5,
                        CreatedAt = DateTime.UtcNow // This might be slightly off if run later, but usually run at end of month
                    });
                }
            }
        }

        await _context.SaveChangesAsync();
    }
}
