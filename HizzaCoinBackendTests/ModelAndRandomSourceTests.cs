using HizzaCoinBackend.Models;
using HizzaCoinBackend.Services;

namespace HizzaCoinBackendTests;

public class ModelAndRandomSourceTests
{
    [Fact]
    public void Challenge_supports_parameterless_construction()
    {
        Assert.NotNull(new Challenge());
    }

    [Fact]
    public void Database_settings_store_configured_values()
    {
        var settings = new HizzaCoinDatabaseSettings
        {
            ConnectionString = "mongodb://localhost",
            DatabaseName = "HizzaCoin",
            AccountsCollectionName = "Accounts"
        };

        Assert.Equal("mongodb://localhost", settings.ConnectionString);
        Assert.Equal("HizzaCoin", settings.DatabaseName);
        Assert.Equal("Accounts", settings.AccountsCollectionName);
    }

    [Fact]
    public void CoinRandomSource_returns_values_in_requested_ranges()
    {
        var random = new CoinRandomSource();

        Assert.InRange(random.GetInt32(5, 10), 5, 9);
        Assert.InRange(random.NextDouble(), 0, 0.9999999999999999);
    }
}
