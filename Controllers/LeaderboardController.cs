using FootballPointsApp.Data;
using FootballPointsApp.Services;
using FootballPointsApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FootballPointsApp.Controllers;

public class LeaderboardController : Controller
{
    private readonly AppDbContext _context;
    private readonly MonthlyBonusService _bonusService;

    public LeaderboardController(AppDbContext context, MonthlyBonusService bonusService)
    {
        _context = context;
        _bonusService = bonusService;
    }

    public async Task<IActionResult> Index(int? year, int? month)
    {
        int y = year ?? DateTime.UtcNow.Year;
        int m = month ?? DateTime.UtcNow.Month;

        // Ensure bonuses are applied
        await _bonusService.ApplyMonthlyBonusesAsync(y, m);

        var startOfMonth = new DateTime(y, m, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfMonth = startOfMonth.AddMonths(1);

        // Get logs for this month
        var logs = await _context.PointsLogs
            .Include(l => l.Player)
            .Where(l => l.CreatedAt >= startOfMonth && l.CreatedAt < endOfMonth)
            .ToListAsync();

        // Group by player
        var grouped = logs.GroupBy(l => l.PlayerId)
            .Select(g => new 
            {
                PlayerId = g.Key,
                PlayerName = g.First().Player.Name,
                TotalPoints = g.Sum(x => x.PointsChange),
                MatchesPlayed = g.Where(x => x.MatchId != null && x.Reason == "Attendance").Select(x => x.MatchId).Distinct().Count(),
                Wins = g.Count(x => x.Reason == "Team win"),
                Draws = g.Count(x => x.Reason == "Team draw"),
                // Losses are not explicitly logged as 0 points, so we calculate them: Matches - Wins - Draws
                // Note: This approximation assumes every match played results in a win, draw, or loss log.
                // However, "Attendance" is logged for every match played.
                // Let's count "Attendance" logs as matches played.
                LateArrivals = g.Count(x => x.Reason.Contains("Late")), // Assuming "Late" is part of the reason string if implemented, or we need to check MatchPlayer.IsLate
                NoShows = g.Count(x => x.Reason == "No-show after Will attend"),
                BonusPoints = g.Where(x => x.Reason.Contains("Bonus") || x.Reason.Contains("goals")).Sum(x => x.PointsChange)
            })
            .ToList(); // Materialize first to do complex calculations in memory if needed

        var entries = new List<LeaderboardEntryDto>();
        
        // We need to fetch Late info from MatchPlayers because it's not in PointsLog explicitly as "Late" reason usually
        // Actually, let's check PointsCalculatorService. It doesn't seem to log "Late" penalty explicitly in the snippet I read.
        // Wait, I need to check if "Late" penalty is logged.
        // The snippet showed:
        // if (rsvpStatus == RsvpStatus.WillAttend || rsvpStatus == RsvpStatus.None) -> Attendance +2
        // It didn't show a "Late" penalty log.
        // However, MatchPlayer has "IsLate".
        
        // To get accurate "Late" counts, we should query MatchPlayers directly for this month.
        var matchPlayers = await _context.MatchPlayers
            .Include(mp => mp.Match)
            .Where(mp => mp.Match.MatchDate >= startOfMonth && mp.Match.MatchDate < endOfMonth)
            .ToListAsync();

        int rank = 1;
        foreach (var item in grouped.OrderByDescending(x => x.TotalPoints))
        {
            var playerLateCount = matchPlayers.Count(mp => mp.PlayerId == item.PlayerId && mp.IsLate);
            var losses = item.MatchesPlayed - item.Wins - item.Draws;
            
            entries.Add(new LeaderboardEntryDto
            {
                Rank = rank++,
                PlayerName = item.PlayerName,
                MatchesPlayed = item.MatchesPlayed,
                Wins = item.Wins,
                Draws = item.Draws,
                Losses = Math.Max(0, losses),
                LateArrivals = playerLateCount,
                NoShows = item.NoShows,
                BonusPoints = item.BonusPoints,
                TotalPoints = item.TotalPoints
            });
        }

        var viewModel = new LeaderboardViewModel
        {
            Year = y,
            Month = m,
            Entries = entries
        };

        return View(viewModel);
    }
}
