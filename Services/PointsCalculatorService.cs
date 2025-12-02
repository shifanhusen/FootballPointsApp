using FootballPointsApp.Data;
using FootballPointsApp.Models;
using Microsoft.EntityFrameworkCore;

namespace FootballPointsApp.Services;

public class PointsCalculatorService
{
    private readonly AppDbContext _context;

    public PointsCalculatorService(AppDbContext context)
    {
        _context = context;
    }

    public async Task CalculateAndPersistPointsForMatchAsync(int matchId)
    {
        var match = await _context.Matches
            .Include(m => m.MatchPlayers)
            .Include(m => m.MatchRsvps)
            .FirstOrDefaultAsync(m => m.Id == matchId);

        if (match == null) throw new ArgumentException("Match not found");
        
        // Clear existing points logs for this match to allow recalculation
        var existingLogs = await _context.PointsLogs.Where(l => l.MatchId == matchId).ToListAsync();
        if (existingLogs.Any())
        {
            _context.PointsLogs.RemoveRange(existingLogs);
        }

        // We need all players to check for "No response"
        var allPlayers = await _context.Players.Where(p => p.IsActive).ToListAsync();

        // 1. RSVP Rules
        foreach (var player in allPlayers)
        {
            var rsvp = match.MatchRsvps.FirstOrDefault(r => r.PlayerId == player.Id);
            var played = match.MatchPlayers.Any(mp => mp.PlayerId == player.Id);
            var rsvpStatus = rsvp?.Status ?? RsvpStatus.None;

            // If a player has no RSVP (Status == None) before the deadline
            // Note: We assume this calculation happens AFTER the match, so definitely after deadline.
            if (rsvpStatus == RsvpStatus.None)
            {
                _context.PointsLogs.Add(new PointsLog
                {
                    PlayerId = player.Id,
                    MatchId = match.Id,
                    Reason = "No response before deadline",
                    PointsChange = -1,
                    CreatedAt = DateTime.UtcNow
                });
            }
            // If a player selects “Will attend” but does not appear in MatchPlayer
            else if (rsvpStatus == RsvpStatus.WillAttend && !played)
            {
                _context.PointsLogs.Add(new PointsLog
                {
                    PlayerId = player.Id,
                    MatchId = match.Id,
                    Reason = "No-show after Will attend",
                    PointsChange = -2,
                    CreatedAt = DateTime.UtcNow
                });
            }
            // Maybe/CantJoin and not played -> 0 points (no log needed)
        }

        // 2, 3, 4. Points for players who played
        foreach (var mp in match.MatchPlayers)
        {
            var rsvp = match.MatchRsvps.FirstOrDefault(r => r.PlayerId == mp.PlayerId);
            var rsvpStatus = rsvp?.Status ?? RsvpStatus.None;

            // 2. Attendance Points
            // If RSVP “Will attend” OR "None" -> +2
            // If "Maybe" or "Can't join" -> 0
            if (rsvpStatus == RsvpStatus.WillAttend || rsvpStatus == RsvpStatus.None)
            {
                _context.PointsLogs.Add(new PointsLog
                {
                    PlayerId = mp.PlayerId,
                    MatchId = match.Id,
                    Reason = "Attendance",
                    PointsChange = 2,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Late Arrival Penalty
            if (mp.IsLate)
            {
                _context.PointsLogs.Add(new PointsLog
                {
                    PlayerId = mp.PlayerId,
                    MatchId = match.Id,
                    Reason = "Late arrival",
                    PointsChange = -1,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // 3. Match Result Points
            int teamAGoals = match.TeamAGoals ?? 0;
            int teamBGoals = match.TeamBGoals ?? 0;
            
            bool isTeamA = mp.Team == TeamSide.TeamA;
            bool isTeamB = mp.Team == TeamSide.TeamB;

            // Determine result for this player
            if ((isTeamA && teamAGoals > teamBGoals) || (isTeamB && teamBGoals > teamAGoals))
            {
                _context.PointsLogs.Add(new PointsLog
                {
                    PlayerId = mp.PlayerId,
                    MatchId = match.Id,
                    Reason = "Team win",
                    PointsChange = 3,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else if (teamAGoals == teamBGoals)
            {
                _context.PointsLogs.Add(new PointsLog
                {
                    PlayerId = mp.PlayerId,
                    MatchId = match.Id,
                    Reason = "Team draw",
                    PointsChange = 1,
                    CreatedAt = DateTime.UtcNow
                });
            }
            // Loss = 0

            // 4. Team Goals
            int myTeamGoals = isTeamA ? teamAGoals : teamBGoals;
            if (myTeamGoals > 0)
            {
                decimal goalPoints = (decimal)myTeamGoals * 0.5m;
                _context.PointsLogs.Add(new PointsLog
                {
                    PlayerId = mp.PlayerId,
                    MatchId = match.Id,
                    Reason = "Team goals scored",
                    PointsChange = goalPoints,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        match.IsFinished = true;
        await _context.SaveChangesAsync();
    }
}
