namespace HizzaCoinBackend.Models.DTOs;

public class CoinBalanceResponse
{
    public CoinBalanceResponse(long balance, long wageredBalance)
    {
        Balance = balance;
        WageredBalance = wageredBalance;
    }

    public long Balance { get; set; }
    public long WageredBalance { get; set; }
}