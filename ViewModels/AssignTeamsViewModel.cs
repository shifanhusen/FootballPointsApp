using FootballPointsApp.Models;

namespace FootballPointsApp.ViewModels;

public class AssignTeamsViewModel
{
    public int MatchId { get; set; }
    public DateTime MatchDate { get; set; }
    public List<PlayerTeamDto> Players { get; set; } = new();
}

public class PlayerTeamDto
{
    public int PlayerId { get; set; }
    public string PlayerName { get; set; } = default!;
    public TeamSide? Team { get; set; } // Null = Unassigned
    public bool IsLate { get; set; }
}
