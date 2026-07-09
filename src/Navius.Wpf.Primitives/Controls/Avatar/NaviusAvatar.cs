using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// Tier B (custom lookless control): folds the contract's three parts (NaviusAvatar,
/// NaviusAvatarFallback, NaviusAvatarImage) into one Control whose ControlTemplate overlays a
/// template Image over a Fallback ContentPresenter, switching visibility off a Status
/// dependency property -- the same "owns its parts centrally via named template parts" shape
/// NaviusRating uses for its star items (see docs/parity/avatar.md "WPF strategy").
///
/// Deviation/resolution of two of the doc's open questions: (1) Status is exposed as a normal
/// public, settable DependencyProperty (like NaviusRating.Value) rather than internal-only, so
/// template triggers and tests can drive/observe it directly; (2) the family renders no
/// data-state attribute in the source, and this port likewise adds no extra state marker beyond
/// Status itself.
///
/// Loading is wired off BitmapImage.DownloadCompleted/DownloadFailed (the WPF async-image
/// analog of the contract's native onload/onerror) plus the template Image's own ImageFailed
/// routed event as a second failure path (e.g. malformed image data after a successful
/// download). WPF's Image type has no "ImageOpened" event to mirror 1:1; DownloadCompleted is
/// the correct equivalent signal.
/// </summary>
[TemplatePart(Name = PartImage, Type = typeof(Image))]
public class NaviusAvatar : Control
{
    private const string PartImage = "PART_Image";

    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source), typeof(string), typeof(NaviusAvatar), new PropertyMetadata(null, OnSourceChanged));

    public static readonly DependencyProperty DelayMsProperty = DependencyProperty.Register(
        nameof(DelayMs), typeof(int), typeof(NaviusAvatar), new PropertyMetadata(0));

    public static readonly DependencyProperty FallbackProperty = DependencyProperty.Register(
        nameof(Fallback), typeof(object), typeof(NaviusAvatar), new PropertyMetadata(null));

    public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(
        nameof(Status), typeof(NaviusAvatarLoadStatus), typeof(NaviusAvatar),
        new PropertyMetadata(NaviusAvatarLoadStatus.Idle, OnStatusChanged));

    private static readonly DependencyPropertyKey IsFallbackVisiblePropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsFallbackVisible), typeof(bool), typeof(NaviusAvatar), new PropertyMetadata(true));

    public static readonly DependencyProperty IsFallbackVisibleProperty = IsFallbackVisiblePropertyKey.DependencyProperty;

    public static readonly RoutedEvent LoadingStatusChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(LoadingStatusChanged), RoutingStrategy.Bubble,
        typeof(RoutedPropertyChangedEventHandler<NaviusAvatarLoadStatus>), typeof(NaviusAvatar));

    private Image? _image;
    private BitmapImage? _bitmap;
    private DispatcherTimer? _fallbackDelayTimer;
    private bool _delayElapsed;

    static NaviusAvatar()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusAvatar), new FrameworkPropertyMetadata(typeof(NaviusAvatar)));
    }

    /// <summary>Image URI/path. A (re)assigned non-empty value transitions Status to Loading until resolved.</summary>
    public string? Source
    {
        get => (string?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <summary>If &lt;= 0, the fallback may render immediately; otherwise it waits DelayMs before it is allowed to show.</summary>
    public int DelayMs
    {
        get => (int)GetValue(DelayMsProperty);
        set => SetValue(DelayMsProperty, value);
    }

    /// <summary>Content shown while idle/loading/error, or before the DelayMs window elapses.</summary>
    public object? Fallback
    {
        get => GetValue(FallbackProperty);
        set => SetValue(FallbackProperty, value);
    }

    /// <summary>Current load status. Normally driven internally by Source/image events; publicly settable for template triggers and tests.</summary>
    public NaviusAvatarLoadStatus Status
    {
        get => (NaviusAvatarLoadStatus)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    /// <summary>True when the fallback should be shown: Status != Loaded and the DelayMs window (if any) has elapsed.</summary>
    public bool IsFallbackVisible => (bool)GetValue(IsFallbackVisibleProperty);

    /// <summary>Fires on every Status transition, mirroring NaviusAvatarImage.OnLoadingStatusChange.</summary>
    public event RoutedPropertyChangedEventHandler<NaviusAvatarLoadStatus> LoadingStatusChanged
    {
        add => AddHandler(LoadingStatusChangedEvent, value);
        remove => RemoveHandler(LoadingStatusChangedEvent, value);
    }

    public override void OnApplyTemplate()
    {
        if (_image is not null)
        {
            _image.ImageFailed -= OnImageFailed;
        }

        base.OnApplyTemplate();

        _image = GetTemplateChild(PartImage) as Image;
        if (_image is not null)
        {
            _image.ImageFailed += OnImageFailed;
        }

        RestartFallbackDelay();
    }

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusAvatar)d).LoadSource((string?)e.NewValue);

    private void LoadSource(string? source)
    {
        DetachBitmap();

        if (string.IsNullOrEmpty(source))
        {
            Status = NaviusAvatarLoadStatus.Idle;
            return;
        }

        Status = NaviusAvatarLoadStatus.Loading;

        Uri uri;
        try
        {
            uri = new Uri(source, UriKind.RelativeOrAbsolute);
        }
        catch (UriFormatException)
        {
            Status = NaviusAvatarLoadStatus.Error;
            return;
        }

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CreateOptions = BitmapCreateOptions.DelayCreation;
        bitmap.CacheOption = BitmapCacheOption.OnDemand;
        bitmap.DownloadCompleted += OnDownloadCompleted;
        bitmap.DownloadFailed += OnDownloadFailed;
        bitmap.UriSource = uri;

        try
        {
            bitmap.EndInit();
        }
        catch (Exception)
        {
            // Malformed/unreachable source resolved synchronously (e.g. a local file that doesn't exist).
            bitmap.DownloadCompleted -= OnDownloadCompleted;
            bitmap.DownloadFailed -= OnDownloadFailed;
            Status = NaviusAvatarLoadStatus.Error;
            return;
        }

        _bitmap = bitmap;
        if (_image is not null)
        {
            _image.Source = bitmap;
        }

        // Non-progressive sources (pack/file URIs with no async download) are already complete
        // by the time EndInit returns; IsDownloading stays false and DownloadCompleted never fires.
        if (!bitmap.IsDownloading)
        {
            Status = NaviusAvatarLoadStatus.Loaded;
        }
    }

    private void OnDownloadCompleted(object? sender, EventArgs e) => Status = NaviusAvatarLoadStatus.Loaded;

    private void OnDownloadFailed(object? sender, ExceptionEventArgs e) => Status = NaviusAvatarLoadStatus.Error;

    private void OnImageFailed(object? sender, ExceptionRoutedEventArgs e) => Status = NaviusAvatarLoadStatus.Error;

    private void DetachBitmap()
    {
        if (_bitmap is null)
        {
            return;
        }

        _bitmap.DownloadCompleted -= OnDownloadCompleted;
        _bitmap.DownloadFailed -= OnDownloadFailed;
        _bitmap = null;
    }

    private static void OnStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var avatar = (NaviusAvatar)d;
        avatar.RestartFallbackDelay();
        avatar.RaiseEvent(new RoutedPropertyChangedEventArgs<NaviusAvatarLoadStatus>(
            (NaviusAvatarLoadStatus)e.OldValue, (NaviusAvatarLoadStatus)e.NewValue, LoadingStatusChangedEvent));
    }

    private void RestartFallbackDelay()
    {
        _fallbackDelayTimer?.Stop();
        _fallbackDelayTimer = null;

        if (DelayMs <= 0)
        {
            _delayElapsed = true;
        }
        else if (Status != NaviusAvatarLoadStatus.Loaded)
        {
            _delayElapsed = false;
            _fallbackDelayTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(DelayMs) };
            _fallbackDelayTimer.Tick += OnFallbackDelayElapsed;
            _fallbackDelayTimer.Start();
        }

        UpdateFallbackVisibility();
    }

    private void OnFallbackDelayElapsed(object? sender, EventArgs e)
    {
        _fallbackDelayTimer?.Stop();
        _fallbackDelayTimer = null;
        _delayElapsed = true;
        UpdateFallbackVisibility();
    }

    private void UpdateFallbackVisibility() =>
        SetValue(IsFallbackVisiblePropertyKey, Status != NaviusAvatarLoadStatus.Loaded && _delayElapsed);
}
