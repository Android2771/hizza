using HizzaCoinBackend.Models;
using HizzaCoinBackend.Services;
using Moq;

namespace HizzaCoinBackendTests;

public class StatisticsServiceTests
{
    [Fact]
    public async Task GetStats_aggregates_account_transaction_and_challenge_counts()
    {
        var accounts = new Mock<IAccountsService>();
        var transactions = new Mock<ITransactionsService>();
        var challenges = new Mock<IChallengesService>();
        accounts.Setup(x => x.GetAsync()).ReturnsAsync([
            new Account("one", 20, 0, DateTime.MinValue, 0),
            new Account("two", 30, 0, DateTime.MinValue, 0)
        ]);
        accounts.Setup(x => x.GetCount()).ReturnsAsync(2);
        accounts.Setup(x => x.GetActiveUsers()).ReturnsAsync(1);
        transactions.Setup(x => x.GetClaimsMade()).ReturnsAsync(4);
        transactions.Setup(x => x.GetExchangesMade()).ReturnsAsync(5);
        transactions.Setup(x => x.GetBetsPlaced()).ReturnsAsync(6);
        transactions.Setup(x => x.GetBetsWon()).ReturnsAsync(7);
        challenges.Setup(x => x.GetCount()).ReturnsAsync(8);

        var response = await new StatisticsService(accounts.Object, transactions.Object, challenges.Object).GetStats();

        Assert.NotNull(response);
        Assert.Equal(4, response.Claims);
        Assert.Equal(8, response.Challenges);
        Assert.Equal(5, response.Gives);
        Assert.Equal(6, response.RoulettesPlaced);
        Assert.Equal(7, response.RoulettesWon);
        Assert.Equal(50, response.HizzaCoin);
        Assert.Equal(2, response.Accounts);
        Assert.Equal(1, response.ActiveAccounts);
    }

    [Fact]
    public async Task GetStats_returns_zero_supply_for_no_accounts()
    {
        var accounts = new Mock<IAccountsService>();
        var transactions = new Mock<ITransactionsService>();
        var challenges = new Mock<IChallengesService>();
        accounts.Setup(x => x.GetAsync()).ReturnsAsync([]);
        accounts.Setup(x => x.GetCount()).ReturnsAsync(0);
        accounts.Setup(x => x.GetActiveUsers()).ReturnsAsync(0);
        transactions.Setup(x => x.GetClaimsMade()).ReturnsAsync(0);
        transactions.Setup(x => x.GetExchangesMade()).ReturnsAsync(0);
        transactions.Setup(x => x.GetBetsPlaced()).ReturnsAsync(0);
        transactions.Setup(x => x.GetBetsWon()).ReturnsAsync(0);
        challenges.Setup(x => x.GetCount()).ReturnsAsync(0);

        var response = await new StatisticsService(accounts.Object, transactions.Object, challenges.Object).GetStats();

        Assert.NotNull(response);
        Assert.Equal(0, response.HizzaCoin);
        Assert.Equal(0, response.Accounts);
        Assert.Equal(0, response.ActiveAccounts);
    }
}
