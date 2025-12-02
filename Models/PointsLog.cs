using System.ComponentModel.DataAnnotations.Schema;

namespace FootballPointsApp.Models;

public class PointsLog
{
    public int Id { get; set; }

    public int PlayerId { get; set; }
    public Player Player { get; set; } = default!;

    public int? MatchId { get; set; }
    public Match? Match { get; set; }

    public string Reason { get; set; } = default!;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal PointsChange { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
