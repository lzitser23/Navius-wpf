using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Navius.Wpf.Primitives.Controls.Toast;

namespace Navius.Wpf.Gallery.Pages;

/// <summary>
/// Demonstrates ToastManager + NaviusToastViewport: a page-owned manager (Limit=2, so a third
/// toast queues), info/success/error/action toasts, the promise-style loading-&gt;success
/// pattern (ToastHandle.Update rearming the auto-dismiss timer), and the queue-promotion path
/// when a visible slot frees up.
/// </summary>
public partial class ToastPage : UserControl
{
    private readonly ToastManager _manager = new(limit: 2, defaultDuration: TimeSpan.FromSeconds(4));

    public ToastPage()
    {
        InitializeComponent();
        Viewport.Manager = _manager;
    }

    private void OnShowInfoClick(object sender, RoutedEventArgs e) =>
        _manager.Add(new ToastOptions { Title = "Heads up", Description = "This is an informational toast." });

    private void OnShowSuccessClick(object sender, RoutedEventArgs e) =>
        _manager.Add(new ToastOptions { Title = "Saved", Description = "Your changes were saved.", Type = ToastType.Success });

    private void OnShowErrorClick(object sender, RoutedEventArgs e) =>
        _manager.Add(new ToastOptions
        {
            Title = "Something went wrong",
            Description = "The request could not be completed.",
            Type = ToastType.Error,
            Priority = ToastPriority.High,
        });

    private void OnShowActionClick(object sender, RoutedEventArgs e) =>
        _manager.Add(new ToastOptions
        {
            Title = "Item deleted",
            Action = new ToastActionSpec("Undo", () => _manager.Add(new ToastOptions { Title = "Restored" }), AltText: "Undo delete"),
        });

    private async void OnSimulatePromiseClick(object sender, RoutedEventArgs e)
    {
        var handle = _manager.Add(new ToastOptions
        {
            Title = "Saving...",
            Type = ToastType.Loading,
            Duration = TimeSpan.Zero, // sticky while pending
        });

        await Task.Delay(TimeSpan.FromSeconds(1.5));

        // Regression coverage for this rearm lives in ToastTests; this is the same call live.
        handle.Update(new ToastOptions { Title = "Saved", Type = ToastType.Success });
    }

    private void OnQueueDemoClick(object sender, RoutedEventArgs e)
    {
        for (var i = 1; i <= 4; i++)
        {
            var index = i;
            _manager.Add(new ToastOptions { Title = $"Toast {index}", Description = "Limit=2: watch two queue." });
        }
    }

    private void OnClearClick(object sender, RoutedEventArgs e) => _manager.Clear();
}
