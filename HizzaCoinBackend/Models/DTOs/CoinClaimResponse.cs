namespace HizzaCoinBackend.Models.DTOs;

public class CoinClaimResponse
{
    public CoinClaimResponse(string discordId, int baseClaim, int streak, double multiplier, Reward claimedReward, Reward nextReward, int totalClaim, bool claimed)
    {
        DiscordId = discordId;
        BaseClaim = baseClaim;
        Streak = streak;
        Multiplier = multiplier;
        ClaimedReward = claimedReward;
        NextReward = nextReward;
        TotalClaim = totalClaim;
        Claimed = claimed;
    }

    public string DiscordId { get; set; }
    public int BaseClaim { get; set; }
    public int Streak { get; set; }
    public double Multiplier { get; set; }
    public Reward ClaimedReward { get; set; }
    public Reward NextReward { get; set; }
    public int TotalClaim { get; set; }

    public bool Claimed { get; set; }
}