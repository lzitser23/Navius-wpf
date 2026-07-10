using System.Linq;
using System.Windows.Input;
using Navius.Wpf.Ui.CommandPalette;
using Xunit;

namespace Navius.Wpf.Tests;

public class UiCommandPaletteTests
{
    private static CommandPaletteItem[] SampleItems() =>
    [
        new("Go to Dashboard", group: "Navigation"),
        new("Go to Settings", group: "Navigation"),
        new("New File", group: "Actions"),
        new("Delete File", group: "Actions"),
    ];

    [Fact]
    public void Filter_EmptyQuery_ReturnsAllItems()
    {
        var result = CommandPaletteEngine.Filter(SampleItems(), query: null);

        Assert.Equal(4, result.Count);
    }

    [Fact]
    public void Filter_MatchesLabelCaseInsensitive()
    {
        var result = CommandPaletteEngine.Filter(SampleItems(), "dashboard");

        Assert.Single(result);
        Assert.Equal("Go to Dashboard", result[0].Label);
    }

    [Fact]
    public void Filter_MatchesGroupToo()
    {
        var result = CommandPaletteEngine.Filter(SampleItems(), "actions");

        Assert.Equal(2, result.Count);
        Assert.All(result, item => Assert.Equal("Actions", item.Group));
    }

    [Fact]
    public void Filter_NoMatches_ReturnsEmpty()
    {
        var result = CommandPaletteEngine.Filter(SampleItems(), "nonexistent-command-xyz");

        Assert.Empty(result);
    }

    [Fact]
    public void MoveHighlight_DownFromNone_LandsOnFirst()
    {
        var next = CommandPaletteEngine.MoveHighlight(current: -1, count: 4, delta: +1);

        Assert.Equal(0, next);
    }

    [Fact]
    public void MoveHighlight_DoesNotWrapPastLast()
    {
        var next = CommandPaletteEngine.MoveHighlight(current: 3, count: 4, delta: +1);

        Assert.Equal(3, next);
    }

    [Fact]
    public void Execute_RunsCommandWithParameter_WhenExecutable()
    {
        object? received = null;
        var command = new RelayCommand(p => received = p, _ => true);
        var item = new CommandPaletteItem("Run", command: command, commandParameter: "payload");

        var ran = CommandPaletteEngine.Execute(item);

        Assert.True(ran);
        Assert.Equal("payload", received);
    }

    [Fact]
    public void Execute_ReturnsFalse_WhenCommandCannotExecute()
    {
        var command = new RelayCommand(_ => { }, _ => false);
        var item = new CommandPaletteItem("Run", command: command);

        var ran = CommandPaletteEngine.Execute(item);

        Assert.False(ran);
    }

    [Fact]
    public void Execute_ReturnsFalse_WhenItemHasNoCommand()
    {
        var item = new CommandPaletteItem("No-op");

        Assert.False(CommandPaletteEngine.Execute(item));
    }

    [StaFact]
    public void NaviusCommandPalette_Defaults_AreClosedAndModal()
    {
        EnsureApplication();

        var palette = new NaviusCommandPalette();

        Assert.False(palette.IsOpen);
        Assert.Equal(System.Windows.Visibility.Collapsed, palette.Visibility);
    }

    [StaFact]
    public void NaviusCommandPalette_IsOpen_WithoutHostWindow_RevertsToFalse()
    {
        EnsureApplication();

        var palette = new NaviusCommandPalette();

        palette.IsOpen = true;

        Assert.False(palette.IsOpen);
    }

    // Same guarded-static-Application pattern as DialogTests/AutocompleteTests: xunit may run
    // test classes on separate STA threads in parallel, so creating the process Application is
    // a best-effort race rather than an assumed single owner.
    private static void EnsureApplication()
    {
        if (System.Windows.Application.Current is null)
        {
            try
            {
                _ = new System.Windows.Application { ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown };
            }
            catch (System.InvalidOperationException)
            {
            }
        }
    }

    private sealed class RelayCommand : ICommand
    {
        private readonly System.Action<object?> _execute;
        private readonly System.Func<object?, bool> _canExecute;

        public RelayCommand(System.Action<object?> execute, System.Func<object?, bool> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event System.EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _canExecute(parameter);

        public void Execute(object? parameter) => _execute(parameter);
    }
}
