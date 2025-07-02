namespace HizzaCoinBackend.Models.DTOs;

public class CoinClaimResponse
{
    public CoinClaimResponse(string discordId, int claimedAmount, int streak, double multiplier, Reward claimedReward, Reward nextReward, bool claimed)
    {
        DiscordId = discordId;
        ClaimedAmount = claimedAmount;
        Streak = streak;
        Multiplier = multiplier;
        ClaimedReward = claimedReward;
        NextReward = nextReward;
        Claimed = claimed;
    }

    public string DiscordId { get; set; }
    public int ClaimedAmount { get; set; }
    public int Streak { get; set; }
    public double Multiplier { get; set; }
    public Reward ClaimedReward { get; set; }
    public Reward NextReward { get; set; }

    public bool Claimed { get; set; }
}