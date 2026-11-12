using System.Security.Cryptography;

namespace HizzaCoinBackend.Services;

public interface ICoinRandomSource
{
    int GetInt32(int fromInclusive, int toExclusive);
    double NextDouble();
}

public sealed class CoinRandomSource : ICoinRandomSource
{
    public int GetInt32(int fromInclusive, int toExclusive) =>
        RandomNumberGenerator.GetInt32(fromInclusive, toExclusive);

    public double NextDouble() => Random.Shared.NextDouble();
}
