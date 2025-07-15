namespace HizzaCoinBackend.Models.DTOs;

public class RouletteResponse
{
    public RouletteResponse(int rouletteNumber, int bet, int payout)
    {
        RouletteNumber = rouletteNumber;
        Bet = bet;
        Payout = payout;
    }

    public int RouletteNumber { get; set; }
    public int Bet { get; set; }
    public int Payout { get; set; }
}