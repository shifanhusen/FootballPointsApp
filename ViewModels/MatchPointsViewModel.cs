namespace FootballPointsApp.ViewModels;

public class MatchPointsViewModel
{
    public int MatchId { get; set; }
    public DateTime MatchDate { get; set; }
    public string? Location { get; set; }
    public List<MatchPlayerPointsDto> PlayerPoints { get; set; } = new();
}

public class MatchPlayerPointsDto
{
    public string PlayerName { get; set; } = default!;
    public string Team { get; set; } = default!;
    public bool IsLate { get; set; }
    public decimal TotalPoints { get; set; }
    public List<string> PointDetails { get; set; } = new();
}