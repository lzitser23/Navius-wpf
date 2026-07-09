using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Navius.Wpf.Primitives.Controls.MessageScroller;

namespace Navius.Wpf.Gallery.Pages;

/// <summary>
/// Demonstrates NaviusMessageScroller's "never move the reader" contract: append while at the
/// live edge (auto-follow), scroll up and append again (the reader stays put, the JumpToLatest
/// button appears with the unseen count), and prepend older history (the reading position holds).
/// Self-contained: not wired into any navigation.
/// </summary>
public partial class MessageScrollerPage : UserControl
{
    private readonly ObservableCollection<string> _messages = new();
    private int _nextMessage = 1;
    private int _nextHistory = 1;

    public MessageScrollerPage()
    {
        InitializeComponent();
        Scroller.ItemsSource = _messages;

        for (var i = 0; i < 12; i++)
        {
            AppendMessage();
        }

        var followingDescriptor = DependencyPropertyDescriptor.FromProperty(
            NaviusMessageScroller.IsFollowingProperty, typeof(NaviusMessageScroller));
        followingDescriptor.AddValueChanged(Scroller, (_, _) => UpdateStateText());
        var countDescriptor = DependencyPropertyDescriptor.FromProperty(
            NaviusMessageScroller.NewMessageCountProperty, typeof(NaviusMessageScroller));
        countDescriptor.AddValueChanged(Scroller, (_, _) => UpdateStateText());
        UpdateStateText();
    }

    private void AppendMessage() =>
        _messages.Add($"Message {_nextMessage++}: the quick brown fox jumps over the lazy dog.");

    private void OnAppendClick(object sender, RoutedEventArgs e) => AppendMessage();

    private void OnAppendBurstClick(object sender, RoutedEventArgs e)
    {
        for (var i = 0; i < 5; i++)
        {
            AppendMessage();
        }
    }

    private void OnPrependClick(object sender, RoutedEventArgs e) =>
        _messages.Insert(0, $"Older history {_nextHistory++}: loaded above the reader's anchor.");

    private void OnClearClick(object sender, RoutedEventArgs e)
    {
        _messages.Clear();
        _nextMessage = 1;
        _nextHistory = 1;
    }

    private void UpdateStateText() =>
        StateText.Text = $"IsFollowing: {Scroller.IsFollowing}   NewMessageCount: {Scroller.NewMessageCount}";
}
