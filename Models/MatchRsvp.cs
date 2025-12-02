namespace FootballPointsApp.Models;

public class MatchRsvp
{
    public int Id { get; set; }

    public int MatchId { get; set; }
    public Match Match { get; set; } = default!;

    public int PlayerId { get; set; }
    public Player Player { get; set; } = default!;

    public RsvpStatus Status { get; set; } = RsvpStatus.None;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
