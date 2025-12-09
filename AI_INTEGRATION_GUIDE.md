# AI Integration Guide for FutPoints

## 🤖 Overview

This guide shows you how to integrate your AI API into the FutPoints application to add intelligent features like smart team balancing, match predictions, and player insights.

---

## 🎯 AI Feature Ideas

### 1. Smart Team Balancing (Recommended First)

**Goal:** Automatically create balanced teams based on player performance history.

**How It Works:**
1. User clicks "Auto-Balance Teams" button on the Team Assignment page
2. App sends player stats to your AI API
3. AI analyzes performance data and returns optimal team assignments
4. Teams are automatically assigned in the system

**Value:** Ensures fair, competitive matches without manual calculation.

---

### 2. Match Outcome Prediction

**Goal:** Predict the likely winner and score before the match starts.

**How It Works:**
1. After teams are assigned, show a "Prediction" section on Match Details
2. AI analyzes team compositions, player form, historical data
3. Display prediction: "Team A likely to win 4-2 (75% confidence)"
4. After match, compare prediction vs actual result

**Value:** Adds excitement and helps with betting/fun wagers.

---

### 3. Player Performance Insights

**Goal:** Give players personalized feedback and improvement suggestions.

**How It Works:**
1. On player profile page, show "AI Insights" section
2. AI generates natural language summary of performance
3. Suggests specific areas for improvement
4. Tracks progress over time

**Value:** Motivates players and makes the app more engaging.

---

### 4. RSVP Prediction

**Goal:** Predict which players are likely to attend based on patterns.

**How It Works:**
1. When creating a match, AI predicts attendance likelihood per player
2. Admin can prioritize reminders to players with low predicted attendance
3. Learn from RSVP patterns (day of week, time, weather, location)

**Value:** Better match planning and fewer no-shows.

---

### 5. Chatbot Assistant

**Goal:** Answer player questions in natural language.

**Examples:**
- "When is my next match?"
- "How many points do I need to reach top 3?"
- "Who has the best attendance rate?"

**Value:** Reduces admin workload, provides instant answers.

---

## 💻 Implementation Steps

### Step 1: Create an AI Service

Create `Services/AIService.cs`:

```csharp
using System.Text.Json;

namespace FootballPointsApp.Services;

public class AIService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _apiEndpoint;

    public AIService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["AI:ApiKey"] ?? throw new ArgumentNullException("AI:ApiKey");
        _apiEndpoint = configuration["AI:Endpoint"] ?? throw new ArgumentNullException("AI:Endpoint");
        
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
    }

    // Feature 1: Smart Team Balancing
    public async Task<TeamBalanceResult> BalanceTeams(List<PlayerStatsDto> players)
    {
        var request = new
        {
            players = players,
            objective = "balance_skill_levels"
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{_apiEndpoint}/balance-teams",
            request
        );

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TeamBalanceResult>()
            ?? throw new Exception("Failed to parse AI response");
    }

    // Feature 2: Match Prediction
    public async Task<MatchPrediction> PredictMatch(MatchPredictionRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"{_apiEndpoint}/predict-match",
            request
        );

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MatchPrediction>()
            ?? throw new Exception("Failed to parse AI response");
    }

    // Feature 3: Player Insights
    public async Task<PlayerInsights> AnalyzePlayer(int playerId, List<MatchPerformance> history)
    {
        var request = new
        {
            playerId,
            matchHistory = history
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{_apiEndpoint}/analyze-player",
            request
        );

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PlayerInsights>()
            ?? throw new Exception("Failed to parse AI response");
    }

    // Feature 4: RSVP Prediction
    public async Task<Dictionary<int, double>> PredictAttendance(int matchId, List<int> playerIds)
    {
        var request = new
        {
            matchId,
            playerIds
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{_apiEndpoint}/predict-attendance",
            request
        );

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Dictionary<int, double>>()
            ?? throw new Exception("Failed to parse AI response");
    }

    // Feature 5: Chatbot
    public async Task<string> Chat(string userMessage, int? playerId = null)
    {
        var request = new
        {
            message = userMessage,
            playerId,
            context = "football_points_app"
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{_apiEndpoint}/chat",
            request
        );

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ChatResponse>();
        return result?.Message ?? "I couldn't understand that.";
    }
}

// DTOs
public class PlayerStatsDto
{
    public int PlayerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double AveragePoints { get; set; }
    public int MatchesPlayed { get; set; }
    public double WinRate { get; set; }
    public int GoalsScored { get; set; }
}

public class TeamBalanceResult
{
    public List<int> TeamA { get; set; } = new();
    public List<int> TeamB { get; set; } = new();
    public double BalanceScore { get; set; } // 0-100, higher is more balanced
    public string Explanation { get; set; } = string.Empty;
}

public class MatchPredictionRequest
{
    public List<int> TeamA { get; set; } = new();
    public List<int> TeamB { get; set; } = new();
    public DateTime MatchDate { get; set; }
    public string Location { get; set; } = string.Empty;
}

public class MatchPrediction
{
    public string Winner { get; set; } = string.Empty; // "TeamA", "TeamB", or "Draw"
    public int PredictedScoreA { get; set; }
    public int PredictedScoreB { get; set; }
    public double Confidence { get; set; } // 0-1
    public string Reasoning { get; set; } = string.Empty;
}

public class PlayerInsights
{
    public int PlayerId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public List<string> Strengths { get; set; } = new();
    public List<string> AreasForImprovement { get; set; } = new();
    public double TrendDirection { get; set; } // -1 to 1 (declining to improving)
}

public class MatchPerformance
{
    public DateTime MatchDate { get; set; }
    public double PointsEarned { get; set; }
    public bool Won { get; set; }
    public int GoalsScored { get; set; }
    public bool WasLate { get; set; }
}

public class ChatResponse
{
    public string Message { get; set; } = string.Empty;
}
```

---

### Step 2: Register the Service

Update `Program.cs`:

```csharp
// Add HTTP Client for AI Service
builder.Services.AddHttpClient<AIService>();

// Register AI Service
builder.Services.AddScoped<AIService>();
```

---

### Step 3: Add Configuration

Update `appsettings.json`:

```json
{
  "AI": {
    "ApiKey": "your-api-key-here",
    "Endpoint": "https://your-ai-api.com/v1"
  }
}
```

For production (environment variables):
```bash
AI__ApiKey=your-api-key
AI__Endpoint=https://your-ai-api.com/v1
```

---

### Step 4: Update Controllers

**Example: Smart Team Balancing**

Update `MatchesController.cs`:

```csharp
private readonly AIService _aiService;

public MatchesController(
    AppDbContext context, 
    PointsCalculatorService pointsService, 
    TimeService timeService,
    AIService aiService)
{
    _context = context;
    _pointsService = pointsService;
    _timeService = timeService;
    _aiService = aiService;
}

// GET: Matches/AutoBalanceTeams/5
[Microsoft.AspNetCore.Authorization.Authorize]
public async Task<IActionResult> AutoBalanceTeams(int matchId)
{
    var match = await _context.Matches.FindAsync(matchId);
    if (match == null) return NotFound();

    // Get active players with stats
    var players = await _context.Players
        .Where(p => p.IsActive)
        .Include(p => p.PointsLogs)
        .Include(p => p.MatchPlayers)
        .ToListAsync();

    var playerStats = players.Select(p => new PlayerStatsDto
    {
        PlayerId = p.Id,
        Name = p.Name,
        AveragePoints = p.PointsLogs.Any() 
            ? p.PointsLogs.Average(l => l.PointsChange) 
            : 0,
        MatchesPlayed = p.MatchPlayers.Count,
        WinRate = CalculateWinRate(p),
        GoalsScored = CalculateGoalsScored(p)
    }).ToList();

    try
    {
        var balanceResult = await _aiService.BalanceTeams(playerStats);

        // Auto-assign teams
        var existingAssignments = await _context.MatchPlayers
            .Where(mp => mp.MatchId == matchId)
            .ToListAsync();
        _context.MatchPlayers.RemoveRange(existingAssignments);

        foreach (var playerId in balanceResult.TeamA)
        {
            _context.MatchPlayers.Add(new MatchPlayer
            {
                MatchId = matchId,
                PlayerId = playerId,
                Team = TeamSide.TeamA,
                IsLate = false
            });
        }

        foreach (var playerId in balanceResult.TeamB)
        {
            _context.MatchPlayers.Add(new MatchPlayer
            {
                MatchId = matchId,
                PlayerId = playerId,
                Team = TeamSide.TeamB,
                IsLate = false
            });
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Teams balanced automatically! Balance Score: {balanceResult.BalanceScore:F1}/100. {balanceResult.Explanation}";
        return RedirectToAction(nameof(AssignTeams), new { matchId });
    }
    catch (Exception ex)
    {
        TempData["ErrorMessage"] = $"AI balancing failed: {ex.Message}";
        return RedirectToAction(nameof(AssignTeams), new { matchId });
    }
}

private double CalculateWinRate(Player player)
{
    var matches = player.MatchPlayers
        .Where(mp => mp.Match.IsFinished)
        .ToList();

    if (!matches.Any()) return 0;

    var wins = matches.Count(mp =>
    {
        var match = mp.Match;
        return (mp.Team == TeamSide.TeamA && match.TeamAGoals > match.TeamBGoals) ||
               (mp.Team == TeamSide.TeamB && match.TeamBGoals > match.TeamAGoals);
    });

    return (double)wins / matches.Count;
}

private int CalculateGoalsScored(Player player)
{
    // If you implement individual goals, use that
    // For now, estimate based on team goals
    return player.MatchPlayers
        .Where(mp => mp.Match.IsFinished)
        .Sum(mp => mp.Team == TeamSide.TeamA 
            ? mp.Match.TeamAGoals ?? 0 
            : mp.Match.TeamBGoals ?? 0) / 
        Math.Max(player.MatchPlayers.Count, 1);
}
```

---

### Step 5: Update the View

Add a button in `Views/Matches/AssignTeams.cshtml`:

```html
<div class="mb-6">
    <div class="flex justify-between items-center">
        <h1 class="text-3xl font-bold text-secondary">Assign Teams</h1>
        @if (User.Identity?.IsAuthenticated == true)
        {
            <a asp-action="AutoBalanceTeams" asp-route-matchId="@Model.MatchId" 
               class="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-purple-600 hover:bg-purple-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-purple-500 shadow-sm">
                <i class="bi bi-robot mr-2"></i> AI Auto-Balance
            </a>
        }
    </div>
    <p class="text-gray-600">Match Date: @Model.MatchDate.ToShortDateString()</p>
</div>
```

---

## 📊 Sample AI API Request/Response

### Smart Team Balancing

**Request:**
```json
POST /balance-teams
{
  "players": [
    {
      "playerId": 1,
      "name": "Shifan",
      "averagePoints": 7.5,
      "matchesPlayed": 12,
      "winRate": 0.67,
      "goalsScored": 8
    },
    {
      "playerId": 2,
      "name": "Shimaah",
      "averagePoints": 6.2,
      "matchesPlayed": 10,
      "winRate": 0.50,
      "goalsScored": 5
    }
    // ... more players
  ],
  "objective": "balance_skill_levels"
}
```

**Response:**
```json
{
  "teamA": [1, 3, 5, 7],
  "teamB": [2, 4, 6, 8],
  "balanceScore": 87.5,
  "explanation": "Teams are balanced with similar average points (7.1 vs 7.3) and win rates (0.58 vs 0.62). Shifan's high win rate is balanced by Samooh on Team B."
}
```

---

### Match Prediction

**Request:**
```json
POST /predict-match
{
  "teamA": [1, 3, 5, 7],
  "teamB": [2, 4, 6, 8],
  "matchDate": "2025-12-10T18:00:00Z",
  "location": "Central Park"
}
```

**Response:**
```json
{
  "winner": "TeamA",
  "predictedScoreA": 4,
  "predictedScoreB": 2,
  "confidence": 0.73,
  "reasoning": "Team A has a higher average points per match (7.1 vs 6.5) and better recent form. However, Team B has Samooh who tends to perform well at Central Park."
}
```

---

## 🎨 UI Enhancements

### Display AI Prediction on Match Details

Update `Views/Matches/Details.cshtml`:

```html
@if (Model.Prediction != null)
{
    <div class="bg-purple-50 border-l-4 border-purple-500 p-4 mb-6">
        <div class="flex items-center mb-2">
            <i class="bi bi-robot text-purple-600 text-2xl mr-3"></i>
            <h3 class="text-lg font-bold text-purple-900">AI Prediction</h3>
        </div>
        <p class="text-purple-800 mb-2">
            <strong>Predicted Result:</strong> 
            Team A @Model.Prediction.PredictedScoreA - @Model.Prediction.PredictedScoreB Team B
        </p>
        <p class="text-purple-700 text-sm">
            <strong>Winner:</strong> @Model.Prediction.Winner 
            (Confidence: @((Model.Prediction.Confidence * 100).ToString("F0"))%)
        </p>
        <p class="text-purple-600 text-sm mt-2">
            <em>@Model.Prediction.Reasoning</em>
        </p>
    </div>
}
```

---

## 💰 Cost Optimization Tips

1. **Cache AI Responses:** Store predictions in the database to avoid repeated API calls
2. **Rate Limiting:** Limit AI features to admins or once per match
3. **Fallback Mode:** If AI API fails, continue without predictions
4. **Batch Requests:** If analyzing multiple players, batch them in one API call
5. **Local ML Models:** For simple predictions (RSVP), use local models instead of API

---

## 🧪 Testing AI Integration

### 1. Mock AI Service (for development)

Create `Services/MockAIService.cs`:

```csharp
public class MockAIService : AIService
{
    public override async Task<TeamBalanceResult> BalanceTeams(List<PlayerStatsDto> players)
    {
        await Task.Delay(500); // Simulate API call
        
        var half = players.Count / 2;
        return new TeamBalanceResult
        {
            TeamA = players.Take(half).Select(p => p.PlayerId).ToList(),
            TeamB = players.Skip(half).Select(p => p.PlayerId).ToList(),
            BalanceScore = 75.0,
            Explanation = "[MOCK] Teams randomly balanced for testing"
        };
    }
}
```

Register in `Program.cs`:

```csharp
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<AIService, MockAIService>();
}
else
{
    builder.Services.AddScoped<AIService>();
}
```

---

## 📈 Monitoring AI Usage

Add logging to track AI API calls:

```csharp
public async Task<TeamBalanceResult> BalanceTeams(List<PlayerStatsDto> players)
{
    _logger.LogInformation("AI: Balancing teams for {PlayerCount} players", players.Count);
    var stopwatch = Stopwatch.StartNew();
    
    try
    {
        var result = await _httpClient.PostAsJsonAsync(...);
        stopwatch.Stop();
        
        _logger.LogInformation("AI: Team balancing completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
        return result;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "AI: Team balancing failed");
        throw;
    }
}
```

---

## 🚀 Deployment Checklist

- [ ] AI API key added to environment variables
- [ ] AI endpoint configured correctly
- [ ] Error handling implemented for API failures
- [ ] Fallback logic in place if AI is unavailable
- [ ] Cost tracking enabled (if applicable)
- [ ] Rate limiting configured
- [ ] Caching strategy implemented
- [ ] Logging and monitoring set up

---

**Version:** 1.0  
**Last Updated:** December 2025  
**Ready for Integration:** ✅
