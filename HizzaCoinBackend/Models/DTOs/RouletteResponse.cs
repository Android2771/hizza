namespace HizzaCoinBackend.Models.DTOs;

public class RouletteResponse
{
    public RouletteResponse(long rouletteNumber, long bet, long payout, bool destinyIntervened)
    {
        RouletteNumber = rouletteNumber;
        Bet = bet;
        Payout = payout;
        DestinyIntervened = destinyIntervened;
    }

    public long RouletteNumber { get; set; }
    public long Bet { get; set; }
    public long Payout { get; set; }
    public bool DestinyIntervened { get; set; }
}