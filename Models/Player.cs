using System.ComponentModel.DataAnnotations;

namespace FootballPointsApp.Models;

public class Player
{
    public int Id { get; set; }
    
    [Required]
    public string Name { get; set; } = default!;
    
    public string? Nickname { get; set; }
    
    public bool IsActive { get; set; } = true;

    public ICollection<MatchPlayer> MatchPlayers { get; set; } = new List<MatchPlayer>();
    public ICollection<MatchRsvp> MatchRsvps { get; set; } = new List<MatchRsvp>();
    public ICollection<PointsLog> PointsLogs { get; set; } = new List<PointsLog>();
}
