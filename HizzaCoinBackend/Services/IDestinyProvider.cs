using HizzaCoinBackend.Models;

namespace HizzaCoinBackend.Services;

public interface IDestinyProvider
{
    Destiny GetDestiny();
}

public sealed class DestinyProvider(TimeProvider timeProvider) : IDestinyProvider
{
    public Destiny GetDestiny()
    {
        var date = timeProvider.GetLocalNow().DateTime;
        var seed = date.Day + date.Month - 1;

        if (seed % 3 == 0 || seed % 5 == 0)
            return Destiny.Big;
        if (seed % 17 == 0)
            return Destiny.Insane;
        if (seed % 4 == 0 || seed % 7 == 0)
            return Destiny.Somewhat;
        if (seed % 2 == 1)
            return Destiny.Very;

        return Destiny.Small;
    }
}
