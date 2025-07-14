namespace HizzaCoinBackend.Models.DTOs;

public class RouletteResponse
{
    public RouletteResponse(int rouletteNumber, int payout)
    {
        RouletteNumber = rouletteNumber;
        Payout = payout;
    }

    public int RouletteNumber { get; set; }
    public int Payout { get; set; }
}