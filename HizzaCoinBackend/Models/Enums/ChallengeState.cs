namespace HizzaCoinBackend.Models;

public enum ChallengeState
{
    InProgress = 0,
    PlayerOneWin = 1,
    PlayerTwoWin = 2,
    Draw = 3,
    Expired = 4
}