using FootballPointsApp.Data;
using FootballPointsApp.Models;
using FootballPointsApp.Services;
using FootballPointsApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FootballPointsApp.Controllers;

public class MatchesController : Controller
{
    private readonly AppDbContext _context;
    private readonly PointsCalculatorService _pointsService;
    private readonly TimeService _timeService;

    public MatchesController(AppDbContext context, PointsCalculatorService pointsService, TimeService timeService)
    {
        _context = context;
        _pointsService = pointsService;
        _timeService = timeService;
    }

    // GET: Matches
    public async Task<IActionResult> Index()
    {
        return View(await _context.Matches.OrderByDescending(m => m.MatchDate).ToListAsync());
    }

    // GET: Matches/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var match = await _context.Matches
            .Include(m => m.MatchPlayers).ThenInclude(mp => mp.Player)
            .Include(m => m.MatchRsvps).ThenInclude(mr => mr.Player)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (match == null) return NotFound();

        return View(match);
    }

    // GET: Matches/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Matches/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,MatchDate,Location,Notes,RsvpDeadline")] Match match)
    {
        if (ModelState.IsValid)
        {
            // Ensure dates are converted from App Timezone to UTC
            match.MatchDate = _timeService.ToUtc(match.MatchDate);
            match.RsvpDeadline = _timeService.ToUtc(match.RsvpDeadline);

            _context.Add(match);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ManageRsvps), new { matchId = match.Id });
        }
        return View(match);
    }

    // GET: Matches/ManageRsvps/5
    public async Task<IActionResult> ManageRsvps(int matchId)
    {
        var match = await _context.Matches
            .Include(m => m.MatchRsvps)
            .FirstOrDefaultAsync(m => m.Id == matchId);

        if (match == null) return NotFound();

        var players = await _context.Players.Where(p => p.IsActive).ToListAsync();
        
        var viewModel = new ManageRsvpsViewModel
        {
            MatchId = match.Id,
            MatchDate = match.MatchDate,
            RsvpDeadline = match.RsvpDeadline,
            PlayerRsvps = players.Select(p => new PlayerRsvpDto
            {
                PlayerId = p.Id,
                PlayerName = p.Name,
                Status = match.MatchRsvps.FirstOrDefault(r => r.PlayerId == p.Id)?.Status ?? RsvpStatus.None
            }).ToList()
        };

        return View(viewModel);
    }

    // POST: Matches/ManageRsvps
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManageRsvps(ManageRsvpsViewModel model)
    {
        var match = await _context.Matches.Include(m => m.MatchRsvps).FirstOrDefaultAsync(m => m.Id == model.MatchId);
        if (match == null) return NotFound();

        foreach (var dto in model.PlayerRsvps)
        {
            var existingRsvp = match.MatchRsvps.FirstOrDefault(r => r.PlayerId == dto.PlayerId);
            if (existingRsvp != null)
            {
                existingRsvp.Status = dto.Status;
            }
            else if (dto.Status != RsvpStatus.None)
            {
                match.MatchRsvps.Add(new MatchRsvp
                {
                    MatchId = match.Id,
                    PlayerId = dto.PlayerId,
                    Status = dto.Status
                });
            }
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: Matches/MatchPoints/5
    public async Task<IActionResult> MatchPoints(int matchId)
    {
        var match = await _context.Matches
            .Include(m => m.MatchPlayers).ThenInclude(mp => mp.Player)
            .FirstOrDefaultAsync(m => m.Id == matchId);

        if (match == null) return NotFound();

        var logs = await _context.PointsLogs
            .Where(l => l.MatchId == matchId)
            .Include(l => l.Player)
            .ToListAsync();

        var viewModel = new MatchPointsViewModel
        {
            MatchId = match.Id,
            MatchDate = match.MatchDate,
            Location = match.Location,
            PlayerPoints = logs.GroupBy(l => l.PlayerId)
                .Select(g => {
                    var player = g.First().Player;
                    var matchPlayer = match.MatchPlayers.FirstOrDefault(mp => mp.PlayerId == player.Id);
                    return new MatchPlayerPointsDto
                    {
                        PlayerName = player.Name,
                        Team = matchPlayer != null ? matchPlayer.Team.ToString() : "N/A",
                        IsLate = matchPlayer?.IsLate ?? false,
                        TotalPoints = g.Sum(x => x.PointsChange),
                        PointDetails = g.Select(x => $"{x.Reason}: {x.PointsChange}").ToList()
                    };
                })
                .OrderByDescending(x => x.TotalPoints)
                .ToList()
        };

        return View(viewModel);
    }

    // GET: Matches/AssignTeams/5
    public async Task<IActionResult> AssignTeams(int matchId)
    {
        var match = await _context.Matches
            .Include(m => m.MatchPlayers)
            .FirstOrDefaultAsync(m => m.Id == matchId);

        if (match == null) return NotFound();

        // We only care about players who have RSVP'd WillAttend or Maybe, OR who are already assigned.
        // But simpler to show all active players or those with RSVP.
        // Let's show all active players to allow walk-ins.
        var players = await _context.Players.Where(p => p.IsActive).ToListAsync();

        var viewModel = new AssignTeamsViewModel
        {
            MatchId = match.Id,
            MatchDate = match.MatchDate,
            Players = players.Select(p => {
                var mp = match.MatchPlayers.FirstOrDefault(x => x.PlayerId == p.Id);
                return new PlayerTeamDto
                {
                    PlayerId = p.Id,
                    PlayerName = p.Name,
                    Team = mp?.Team, // Null if not in MatchPlayers
                    IsLate = mp?.IsLate ?? false
                };
            }).ToList()
        };

        return View(viewModel);
    }

    // POST: Matches/AssignTeams
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignTeams(AssignTeamsViewModel model)
    {
        var match = await _context.Matches.Include(m => m.MatchPlayers).FirstOrDefaultAsync(m => m.Id == model.MatchId);
        if (match == null) return NotFound();

        // Clear existing players and re-add based on selection? 
        // Or update. Updating is better to preserve IDs if needed, but re-adding is simpler for logic.
        // Let's update/add/remove.

        foreach (var dto in model.Players)
        {
            var existingMp = match.MatchPlayers.FirstOrDefault(mp => mp.PlayerId == dto.PlayerId);

            if (dto.Team.HasValue)
            {
                if (existingMp != null)
                {
                    existingMp.Team = dto.Team.Value;
                    existingMp.IsLate = dto.IsLate;
                }
                else
                {
                    match.MatchPlayers.Add(new MatchPlayer
                    {
                        MatchId = match.Id,
                        PlayerId = dto.PlayerId,
                        Team = dto.Team.Value,
                        IsLate = dto.IsLate
                    });
                }
            }
            else
            {
                // If Team is null, remove from MatchPlayers if exists
                if (existingMp != null)
                {
                    _context.MatchPlayers.Remove(existingMp);
                }
            }
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: Matches/EnterScore/5
    public async Task<IActionResult> EnterScore(int matchId)
    {
        var match = await _context.Matches.FindAsync(matchId);
        if (match == null) return NotFound();
        if (match.IsFinished) return RedirectToAction(nameof(Details), new { id = matchId });

        return View(new EnterScoreViewModel
        {
            MatchId = match.Id,
            MatchDate = match.MatchDate,
            TeamAGoals = match.TeamAGoals ?? 0,
            TeamBGoals = match.TeamBGoals ?? 0
        });
    }

    // POST: Matches/EnterScore
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnterScore(EnterScoreViewModel model)
    {
        var match = await _context.Matches.FindAsync(model.MatchId);
        if (match == null) return NotFound();

        match.TeamAGoals = model.TeamAGoals;
        match.TeamBGoals = model.TeamBGoals;
        
        await _context.SaveChangesAsync();

        // Calculate points
        await _pointsService.CalculateAndPersistPointsForMatchAsync(match.Id);

        return RedirectToAction(nameof(Index));
    }
}
