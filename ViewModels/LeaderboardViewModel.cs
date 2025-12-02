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
    public int Wins { get; set; }
    public int Draws { get; set; }
    public int Losses { get; set; }
    public int LateArrivals { get; set; }
    public int NoShows { get; set; }
    public decimal BonusPoints { get; set; }
    public decimal TotalPoints { get; set; }
}
