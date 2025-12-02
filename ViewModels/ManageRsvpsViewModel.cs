using FootballPointsApp.Models;

namespace FootballPointsApp.ViewModels;

public class ManageRsvpsViewModel
{
    public int MatchId { get; set; }
    public DateTime MatchDate { get; set; }
    public DateTime RsvpDeadline { get; set; }
    public List<PlayerRsvpDto> PlayerRsvps { get; set; } = new();
}

public class PlayerRsvpDto
{
    public int PlayerId { get; set; }
    public string PlayerName { get; set; } = default!;
    public RsvpStatus Status { get; set; }
}
