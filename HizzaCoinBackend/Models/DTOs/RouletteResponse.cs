namespace HizzaCoinBackend.Models.DTOs;

public class RouletteResponse
{
    public RouletteResponse(long rouletteNumber, long bet, long payout)
    {
        RouletteNumber = rouletteNumber;
        Bet = bet;
        Payout = payout;
    }

    public long RouletteNumber { get; set; }
    public long Bet { get; set; }
    public long Payout { get; set; }
}