namespace FootballPointsApp.ViewModels;

public class LeaderboardViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public List<LeaderboardEntryDto> Entries { get; set; } = new();
}

public class LeaderboardEntryDto
{
    public int Rank { get; set; }
    public string PlayerName { get; set; } = default!;
    public int MatchesPlayed { get; set; }
    
    // Detailed Points Breakdown
    public decimal AttendancePoints { get; set; }
    public decimal ResultPoints { get; set; } // Win/Draw
    public decimal GoalPoints { get; set; }
    public decimal BonusPoints { get; set; } // Monthly bonus etc
    
    // Penalties (should be displayed as negative)
    public decimal LatePenaltyPoints { get; set; }
    public decimal NoShowPenaltyPoints { get; set; }
    public decimal NoResponsePenaltyPoints { get; set; }
    
    public decimal TotalPoints { get; set; }
}
