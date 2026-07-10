using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Captures;

/// <summary>
/// Renders every Navius Gallery page in every theme via RenderTargetBitmap on the WPF visual
/// tree. Deliberately avoids OS-level screen capture: this keeps working even when the machine's
/// screen is locked, because the render is driven entirely in-process against the visual tree
/// rather than compositing whatever is shown on the desktop.
/// </summary>
internal static class CaptureRunner
{
    private const int WindowWidth = 960;
    private const int WindowHeight = 640;
    private const int RenderScale = 2;

    private static readonly NaviusTheme[] Themes =
    {
        NaviusTheme.Light,
        NaviusTheme.Dark,
        NaviusTheme.HighContrast,
    };

    public static int Run(string outDir, IReadOnlyCollection<string>? pageFilter)
    {
        Directory.CreateDirectory(outDir);

        // Forces WPF's own software rasterizer instead of the GPU/DWM compositing path, so the
        // render keeps working when the session is locked and the desktop is not composited.
        RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

        var app = new Application();

        var pageHost = new ContentControl();
        var root = new Border
        {
            Width = WindowWidth,
            Height = WindowHeight,
            Child = pageHost,
        };
        root.SetResourceReference(Border.BackgroundProperty, "Navius.Background");

        var window = new Window
        {
            Content = root,
            Width = WindowWidth,
            Height = WindowHeight,
            Left = -32000,
            Top = -32000,
            WindowStartupLocation = WindowStartupLocation.Manual,
            WindowStyle = WindowStyle.None,
            ResizeMode = ResizeMode.NoResize,
            ShowActivated = false,
            ShowInTaskbar = false,
        };
        window.Show();

        var catalog = PageCatalog.Pages;
        if (pageFilter is { Count: > 0 })
        {
            var wanted = new HashSet<string>(pageFilter);
            catalog = catalog.Where(p => wanted.Contains(p.Label)).ToList();
        }

        var results = new List<(string Label, string Theme, bool Ok, string Detail)>();

        foreach (var (label, factory) in catalog)
        {
            UserControl page;
            try
            {
                page = factory();
            }
            catch (Exception ex)
            {
                foreach (var theme in Themes)
                {
                    results.Add((label, Slug.From(theme.ToString()), false, $"construction failed: {ex.Message}"));
                }

                continue;
            }

            foreach (var theme in Themes)
            {
                var themeSlug = Slug.From(theme.ToString());
                try
                {
                    ThemeManager.Apply(theme);
                    pageHost.Content = page;
                    Settle(root);

                    var pixelWidth = (int)Math.Ceiling(root.ActualWidth * RenderScale);
                    var pixelHeight = (int)Math.Ceiling(root.ActualHeight * RenderScale);
                    var bitmap = new RenderTargetBitmap(
                        pixelWidth,
                        pixelHeight,
                        96 * RenderScale,
                        96 * RenderScale,
                        PixelFormats.Pbgra32);
                    bitmap.Render(root);

                    var fileName = $"{Slug.From(label)}-{themeSlug}.png";
                    var path = Path.Combine(outDir, fileName);
                    SavePng(bitmap, path);

                    results.Add((label, themeSlug, true, path));
                }
                catch (Exception ex)
                {
                    results.Add((label, themeSlug, false, ex.Message));
                }
            }
        }

        window.Close();
        app.Shutdown();

        PrintSummary(results);

        return results.All(r => r.Ok) ? 0 : 1;
    }

    /// <summary>
    /// Forces layout, flushes the dispatcher down to ApplicationIdle a couple of times, then adds
    /// a short fixed delay for pages that animate on load, and forces layout once more before the
    /// caller renders.
    /// </summary>
    private static void Settle(FrameworkElement root)
    {
        root.UpdateLayout();
        DoEvents();
        DoEvents();
        Thread.Sleep(250);
        root.UpdateLayout();
    }

    private static void DoEvents() =>
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

    private static void SavePng(BitmapSource bitmap, string path)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }

    private static void PrintSummary(List<(string Label, string Theme, bool Ok, string Detail)> results)
    {
        Console.WriteLine();
        Console.WriteLine("Capture summary:");
        foreach (var r in results)
        {
            var status = r.Ok ? "OK" : "FAIL";
            Console.WriteLine($"  [{status}] {r.Label} ({r.Theme}) - {r.Detail}");
        }

        var okCount = results.Count(r => r.Ok);
        Console.WriteLine();
        Console.WriteLine($"{okCount}/{results.Count} captures succeeded.");
    }
}
