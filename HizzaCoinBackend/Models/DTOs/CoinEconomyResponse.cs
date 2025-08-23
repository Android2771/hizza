namespace HizzaCoinBackend.Models.DTOs;

public class CoinEconomyResponse
{
    public CoinEconomyResponse(long totalHizzaCoinAmount, long totalHizzaCoinAccounts, long leaderboardPlace, double percentageEconomy)
    {
        TotalHizzaCoinAmount = totalHizzaCoinAmount;
        TotalHizzaCoinAccounts = totalHizzaCoinAccounts;
        LeaderboardPlace = leaderboardPlace;
        PercentageEconomy = percentageEconomy;
    }

    public long TotalHizzaCoinAmount { get; set; }
    public long TotalHizzaCoinAccounts { get; set; }
    public long LeaderboardPlace { get; set; }
    public double PercentageEconomy { get; set; }
}