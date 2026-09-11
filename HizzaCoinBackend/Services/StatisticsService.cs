using HizzaCoinBackend.Models.DTOs;

namespace HizzaCoinBackend.Services;

public class StatisticsService
{
    private readonly AccountsService _accountsService;
    private readonly TransactionsService _transactionsService;
    private readonly ChallengesService _challengesService;
    
    public StatisticsService(AccountsService accountsService, TransactionsService transactionsService, ChallengesService challengesService)
    {
        _accountsService = accountsService;
        _transactionsService = transactionsService;
        _challengesService = challengesService;
    }
    
    public async Task<StatisticsResponse?> GetStats()
    {
        var claims = await _transactionsService.GetClaimsMade();
        var challenges = await _challengesService.GetCount();
        var gives = await _transactionsService.GetExchangesMade();
        var roulettesPlaced = await _transactionsService.GetBetsPlaced();
        var roulettesWon = await _transactionsService.GetBetsWon();
        var hizzaCoin = (await _accountsService.GetAsync()).Sum(account => account.Balance);
        var accounts = await _accountsService.GetCount();
        var activeAccounts = await _accountsService.GetActiveUsers();

        return new StatisticsResponse(claims, challenges, gives, roulettesPlaced, roulettesWon, hizzaCoin, accounts, activeAccounts);
    }
}