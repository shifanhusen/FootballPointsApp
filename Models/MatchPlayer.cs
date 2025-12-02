namespace FootballPointsApp.Models;

public class MatchPlayer
{
    public int Id { get; set; }

    public int MatchId { get; set; }
    public Match Match { get; set; } = default!;

    public int PlayerId { get; set; }
    public Player Player { get; set; } = default!;

    public TeamSide Team { get; set; }

    // Late arrival flag (no points for this now, just information)
    public bool IsLate { get; set; }
}
