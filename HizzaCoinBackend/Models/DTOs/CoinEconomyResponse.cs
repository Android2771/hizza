namespace HizzaCoinBackend.Models.DTOs;

public class CoinEconomyResponse
{
    public CoinEconomyResponse(int totalHizzaCoinAmount, int totalHizzaCoinAccounts, int leaderboardPlace, double percentageEconomy)
    {
        TotalHizzaCoinAmount = totalHizzaCoinAmount;
        TotalHizzaCoinAccounts = totalHizzaCoinAccounts;
        LeaderboardPlace = leaderboardPlace;
        PercentageEconomy = percentageEconomy;
    }

    public int TotalHizzaCoinAmount { get; set; }
    public int TotalHizzaCoinAccounts { get; set; }
    public int LeaderboardPlace { get; set; }
    public double PercentageEconomy { get; set; }
}