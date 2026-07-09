using System.Windows;
using Navius.Wpf.Primitives.Controls;

namespace Navius.Wpf.Tests;

public class AvatarTests
{
    static AvatarTests()
    {
        // pack://application URIs only resolve once an Application exists in the process.
        if (Application.Current is null)
        {
            try
            {
                _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            }
            catch (InvalidOperationException)
            {
                // Another test class's static ctor already created the process-wide Application.
            }
        }
    }

    [StaFact]
    public void DefaultState_IsIdleWithNoDelay()
    {
        var avatar = new NaviusAvatar();

        Assert.Equal(NaviusAvatarLoadStatus.Idle, avatar.Status);
        Assert.Null(avatar.Source);
        Assert.Equal(0, avatar.DelayMs);
    }

    [StaFact]
    public void IsFallbackVisible_TrueWhileNotLoadedAndNoDelay()
    {
        var avatar = new NaviusAvatar();

        Assert.True(avatar.IsFallbackVisible);
    }

    [StaFact]
    public void IsFallbackVisible_FalseWhenLoaded()
    {
        var avatar = new NaviusAvatar { Status = NaviusAvatarLoadStatus.Loaded };

        Assert.False(avatar.IsFallbackVisible);
    }

    [StaFact]
    public void IsFallbackVisible_DelaysUntilTimerElapsesWhenDelayMsSet()
    {
        // DelayMs > 0 without pumping the dispatcher: the fallback must not show immediately.
        var avatar = new NaviusAvatar { DelayMs = 5000, Status = NaviusAvatarLoadStatus.Loading };

        Assert.False(avatar.IsFallbackVisible);
    }

    [StaFact]
    public void Status_SettingRaisesLoadingStatusChanged()
    {
        var avatar = new NaviusAvatar();
        NaviusAvatarLoadStatus? observed = null;
        avatar.LoadingStatusChanged += (_, e) => observed = e.NewValue;

        avatar.Status = NaviusAvatarLoadStatus.Error;

        Assert.Equal(NaviusAvatarLoadStatus.Error, observed);
    }

    [StaFact]
    public void Source_EmptyStringResetsToIdle()
    {
        var avatar = new NaviusAvatar { Status = NaviusAvatarLoadStatus.Loaded };

        avatar.Source = string.Empty;

        Assert.Equal(NaviusAvatarLoadStatus.Idle, avatar.Status);
    }

    [StaFact]
    public void Source_MalformedUriTransitionsToError()
    {
        var avatar = new NaviusAvatar();

        // Unterminated IPv6 host literal: Uri's parser throws UriFormatException for this,
        // reliably and synchronously, regardless of network availability.
        avatar.Source = "http://[::1";

        Assert.Equal(NaviusAvatarLoadStatus.Error, avatar.Status);
    }

    [StaFact]
    public void Fallback_DefaultsToNull()
    {
        var avatar = new NaviusAvatar();

        Assert.Null(avatar.Fallback);
    }

    [StaFact]
    public void Fallback_CanBeSetToArbitraryContent()
    {
        var avatar = new NaviusAvatar { Fallback = "AB" };

        Assert.Equal("AB", avatar.Fallback);
    }
}
