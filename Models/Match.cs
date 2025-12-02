using System.ComponentModel.DataAnnotations;

namespace FootballPointsApp.Models;

public class Match
{
    public int Id { get; set; }
    
    public DateTime MatchDate { get; set; }
    
    public string? Location { get; set; }
    
    public string? Notes { get; set; }

    public DateTime RsvpDeadline { get; set; }

    public int? TeamAGoals { get; set; }
    public int? TeamBGoals { get; set; }
    
    public bool IsFinished { get; set; }

    public ICollection<MatchPlayer> MatchPlayers { get; set; } = new List<MatchPlayer>();
    public ICollection<MatchRsvp> MatchRsvps { get; set; } = new List<MatchRsvp>();
}
