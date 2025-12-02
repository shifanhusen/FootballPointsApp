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
                // Count matches played: distinct MatchId where MatchId is not null
                // Note: Bonus logs have MatchId = null.
                MatchesPlayed = g.Where(x => x.MatchId != null).Select(x => x.MatchId).Distinct().Count()
            })
            .OrderByDescending(x => x.TotalPoints)
            .ToList();

        var entries = new List<LeaderboardEntryDto>();
        int rank = 1;
        foreach (var item in grouped)
        {
            entries.Add(new LeaderboardEntryDto
            {
                Rank = rank++,
                PlayerName = item.PlayerName,
                MatchesPlayed = item.MatchesPlayed,
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
