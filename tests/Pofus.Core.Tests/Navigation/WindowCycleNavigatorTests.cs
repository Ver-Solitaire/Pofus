using Pofus.Core.Accounts;
using Pofus.Core.Navigation;

namespace Pofus.Core.Tests.Navigation;

public class WindowCycleNavigatorTests
{
    [Fact]
    public void GetNext_ReturnsNull_WhenNoActiveAccounts()
    {
        Assert.Null(WindowCycleNavigator.GetNext([], currentWindowHandle: 1));
    }

    [Fact]
    public void GetNext_LoopsBackToFirst_AfterLastAccount()
    {
        var sessions = ThreeAccounts();

        var next = WindowCycleNavigator.GetNext(sessions, currentWindowHandle: 3);

        Assert.Equal(1, next);
    }

    [Fact]
    public void GetPrevious_LoopsBackToLast_FromFirstAccount()
    {
        var sessions = ThreeAccounts();

        var previous = WindowCycleNavigator.GetPrevious(sessions, currentWindowHandle: 1);

        Assert.Equal(3, previous);
    }

    [Fact]
    public void GetNext_ReturnsSameAccount_WhenOnlyOneActiveAccount()
    {
        var sessions = new List<AccountSession>
        {
            MakeAccount("Solo", handle: 1),
        };

        var next = WindowCycleNavigator.GetNext(sessions, currentWindowHandle: 1);

        Assert.Equal(1, next);
    }

    [Fact]
    public void GetNext_StartsAtFirstAccount_WhenForegroundWindowIsUnknown()
    {
        var sessions = ThreeAccounts();

        var next = WindowCycleNavigator.GetNext(sessions, currentWindowHandle: 999);

        Assert.Equal(1, next);
    }

    [Fact]
    public void GetLeader_ReturnsNull_WhenNoAccountIsLeader()
    {
        var sessions = ThreeAccounts();

        Assert.Null(WindowCycleNavigator.GetLeader(sessions));
    }

    [Fact]
    public void GetLeader_ReturnsLeaderWindowHandle()
    {
        var sessions = new List<AccountSession>
        {
            MakeAccount("A", handle: 1),
            MakeAccount("B", handle: 2, isLeader: true),
        };

        Assert.Equal(2, WindowCycleNavigator.GetLeader(sessions));
    }

    private static List<AccountSession> ThreeAccounts() =>
    [
        MakeAccount("A", handle: 1),
        MakeAccount("B", handle: 2),
        MakeAccount("C", handle: 3),
    ];

    private static AccountSession MakeAccount(string pseudo, nint handle, bool isLeader = false) => new()
    {
        Pseudo = pseudo,
        ClassName = "Iop",
        WindowHandle = handle,
        IsActive = true,
        Team = "Équipe 1",
        IsLeader = isLeader,
    };
}
