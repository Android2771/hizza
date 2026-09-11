namespace HizzaCoinBackend.Models.DTOs;

public class StatisticsResponse
{
    public StatisticsResponse(long claims, long challenges, long gives, long roulettesPlaced, long roulettesWon, long hizzaCoin, long accounts)
    {
        Claims = claims;
        Challenges = challenges;
        Gives = gives;
        RoulettesPlaced = roulettesPlaced;
        RoulettesWon = roulettesWon;
        HizzaCoin = hizzaCoin;
        Accounts = accounts;
    }

    public long Claims { get; set; }
    public long Challenges { get; set; }
    public long Gives { get; set; }
    public long RoulettesPlaced { get; set; }
    
    public long RoulettesWon { get; set; }
    public long HizzaCoin { get; set; }
    public long Accounts { get; set; }
}