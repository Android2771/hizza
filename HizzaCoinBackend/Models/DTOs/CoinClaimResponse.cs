namespace HizzaCoinBackend.Models.DTOs;

public class CoinClaimResponse
{
    public CoinClaimResponse(string discordId, long baseClaim, long streak, double multiplier, Reward claimedReward, Reward nextReward, long totalClaim, bool claimed)
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
    public long BaseClaim { get; set; }
    public long Streak { get; set; }
    public double Multiplier { get; set; }
    public Reward ClaimedReward { get; set; }
    public Reward NextReward { get; set; }
    public long TotalClaim { get; set; }

    public bool Claimed { get; set; }
}