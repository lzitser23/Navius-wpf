using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Navius.Wpf.Ui.CommandPalette;

namespace Navius.Wpf.Gallery.Pages;

/// <summary>
/// Self-contained showcase for all nine Navius.Wpf.Ui composite items: Breadcrumb, Pagination,
/// InputGroup, ButtonGroup, SplitButton, Sidebar, Resizable, Carousel, CommandPalette. No
/// navigation wiring; this page just demonstrates each control in isolation.
/// </summary>
public partial class UiCompositePage : UserControl
{
    public UiCompositePage()
    {
        InitializeComponent();

        CommandPalette.Items = new[]
        {
            new CommandPaletteItem("Go to Dashboard", group: "Navigation", command: new RelayCommand(() => RunCommand("Go to Dashboard"))),
            new CommandPaletteItem("Go to Settings", group: "Navigation", command: new RelayCommand(() => RunCommand("Go to Settings"))),
            new CommandPaletteItem("New File", group: "Actions", command: new RelayCommand(() => RunCommand("New File"))),
            new CommandPaletteItem("Delete File", group: "Actions", command: new RelayCommand(() => RunCommand("Delete File"))),
            new CommandPaletteItem("Toggle Theme", group: "Actions", command: new RelayCommand(() => RunCommand("Toggle Theme"))),
        };

        PreviewKeyDown += OnPreviewKeyDown;
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.K && Keyboard.Modifiers == ModifierKeys.Control)
        {
            CommandPalette.Open();
            e.Handled = true;
        }
    }

    private void OnOpenPaletteClick(object sender, RoutedEventArgs e) => CommandPalette.Open();

    private void OnDeployClick(object sender, RoutedEventArgs e) => DeployStatus.Text = "Deploy started.";

    private void OnDeployMenuItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Header: string header })
        {
            DeployStatus.Text = $"{header} selected.";
        }
    }

    private void RunCommand(string label) => PaletteStatus.Text = $"Ran: {label}";

    private sealed class RelayCommand : ICommand
    {
        private readonly Action _execute;

        public RelayCommand(Action execute) => _execute = execute;

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _execute();
    }
}
