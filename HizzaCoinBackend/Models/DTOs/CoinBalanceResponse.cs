namespace HizzaCoinBackend.Models.DTOs;

public class CoinBalanceResponse
{
    public CoinBalanceResponse(int balance, int wageredBalance)
    {
        Balance = balance;
        WageredBalance = wageredBalance;
    }

    public int Balance { get; set; }
    public int WageredBalance { get; set; }
}