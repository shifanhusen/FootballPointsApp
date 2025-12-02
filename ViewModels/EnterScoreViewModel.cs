using System.ComponentModel.DataAnnotations;

namespace FootballPointsApp.ViewModels;

public class EnterScoreViewModel
{
    public int MatchId { get; set; }
    public DateTime MatchDate { get; set; }
    
    [Required]
    public int TeamAGoals { get; set; }
    
    [Required]
    public int TeamBGoals { get; set; }
}
