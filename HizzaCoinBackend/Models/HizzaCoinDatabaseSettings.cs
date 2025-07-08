namespace HizzaCoinBackend.Models;

public class HizzaCoinDatabaseSettings
{
    public string ConnectionString { get; set; } = null!;

    public string DatabaseName { get; set; } = null!;

    public string AccountsCollectionName { get; set; } = null!;
}