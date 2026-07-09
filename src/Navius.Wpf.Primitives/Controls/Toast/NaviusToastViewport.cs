using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Navius.Wpf.Primitives.Controls.Toast;

/// <summary>
/// The Control a consumer drops into a corner of their window (contract: NaviusToastViewport).
/// Manager-driven: assign <see cref="Manager"/> and this renders its
/// <see cref="ToastManager.VisibleToasts"/> as a stack of <see cref="NaviusToast"/> visuals with
/// C#-computed offsets (no XAML ItemsControl/panel-driven layout -- offsets depend on each
/// toast's actual rendered height, which is manager/queue state, not something an ItemsPanel can
/// derive on its own). Enter/exit uses plain WPF Storyboards (slide+fade, 150ms); this project
/// must not reference Navius.Wpf.Motion, so there is no spring/keyframe engine involved here.
/// </summary>
[TemplatePart(Name = PartPanel, Type = typeof(Canvas))]
public class NaviusToastViewport : Control
{
    private const string PartPanel = "PART_Panel";
    private static readonly TimeSpan EnterDuration = TimeSpan.FromMilliseconds(150);
    private static readonly TimeSpan ExitDuration = TimeSpan.FromMilliseconds(150);
    private const double SlideDistance = 24;

    public static readonly DependencyProperty ManagerProperty = DependencyProperty.Register(
        nameof(Manager), typeof(ToastManager), typeof(NaviusToastViewport),
        new PropertyMetadata(null, OnManagerChanged));

    public static readonly DependencyProperty AlignmentProperty = DependencyProperty.Register(
        nameof(Alignment), typeof(NaviusToastAlignment), typeof(NaviusToastViewport),
        new PropertyMetadata(NaviusToastAlignment.BottomRight, OnLayoutAffectingPropertyChanged));

    public static readonly DependencyProperty GapProperty = DependencyProperty.Register(
        nameof(Gap), typeof(double), typeof(NaviusToastViewport),
        new PropertyMetadata(16d, OnLayoutAffectingPropertyChanged));

    private static readonly DependencyPropertyKey IndexPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
        "Index", typeof(int), typeof(NaviusToastViewport), new PropertyMetadata(0));

    public static readonly DependencyProperty IndexProperty = IndexPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey OffsetYPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
        "OffsetY", typeof(double), typeof(NaviusToastViewport), new PropertyMetadata(0d));

    public static readonly DependencyProperty OffsetYProperty = OffsetYPropertyKey.DependencyProperty;

    private readonly Dictionary<Guid, ToastVisualEntry> _visuals = new();
    private Canvas? _panel;
    private Window? _hookedWindow;

    static NaviusToastViewport()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusToastViewport), new FrameworkPropertyMetadata(typeof(NaviusToastViewport)));
        // Contract: tabindex="-1" -- reachable only via the F6 hotkey, not the normal tab order.
        FocusableProperty.OverrideMetadata(typeof(NaviusToastViewport), new FrameworkPropertyMetadata(true));
        KeyboardNavigation.IsTabStopProperty.OverrideMetadata(typeof(NaviusToastViewport), new FrameworkPropertyMetadata(false));
    }

    public NaviusToastViewport()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public ToastManager? Manager
    {
        get => (ToastManager?)GetValue(ManagerProperty);
        set => SetValue(ManagerProperty, value);
    }

    public NaviusToastAlignment Alignment
    {
        get => (NaviusToastAlignment)GetValue(AlignmentProperty);
        set => SetValue(AlignmentProperty, value);
    }

    /// <summary>Gap, in DIPs, between stacked toasts. Contract's ToastProviderContext.Gap default is 16.</summary>
    public double Gap
    {
        get => (double)GetValue(GapProperty);
        set => SetValue(GapProperty, value);
    }

    /// <summary>Read-only attached property: 0 = frontmost/newest. Published for custom templates; the
    /// viewport itself positions toasts on its Canvas directly (does not read this back).</summary>
    public static int GetIndex(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (int)element.GetValue(IndexProperty);
    }

    /// <summary>Read-only attached property: cumulative offset (DIPs) from the anchored edge. Published
    /// for custom templates alongside <see cref="GetIndex"/>.</summary>
    public static double GetOffsetY(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (double)element.GetValue(OffsetYProperty);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _panel = GetTemplateChild(PartPanel) as Canvas;
        Sync();
    }

    private static void OnManagerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var viewport = (NaviusToastViewport)d;
        if (e.OldValue is ToastManager oldManager)
        {
            oldManager.Changed -= viewport.HandleManagerChanged;
        }

        if (e.NewValue is ToastManager newManager)
        {
            newManager.Changed += viewport.HandleManagerChanged;
        }

        viewport.ResetVisuals();
        viewport.Sync();
    }

    private static void OnLayoutAffectingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusToastViewport)d).Reflow();

    private void HandleManagerChanged() => Sync();

    private void ResetVisuals()
    {
        if (_panel is not null)
        {
            foreach (var entry in _visuals.Values)
            {
                _panel.Children.Remove(entry.Element);
            }
        }

        _visuals.Clear();
    }

    /// <summary>Diffs Manager.VisibleToasts against the currently-tracked visuals: creates/removes
    /// NaviusToast elements, repositions the survivors, then plays enter animations for anything new
    /// (including replayed enters for toasts whose UpdateKey moved, e.g. a promise resolving).</summary>
    private void Sync()
    {
        if (Manager is null || _panel is null)
        {
            return;
        }

        var visible = Manager.VisibleToasts;
        var visibleIds = new HashSet<Guid>(visible.Select(t => t.Id));

        foreach (var stale in _visuals.Where(kvp => !visibleIds.Contains(kvp.Key)).ToList())
        {
            BeginExit(stale.Key, stale.Value);
        }

        var entering = new List<NaviusToast>();
        foreach (var toast in visible)
        {
            if (_visuals.TryGetValue(toast.Id, out var entry))
            {
                SyncContent(entry.Element, toast);
                if (entry.LastUpdateKey != toast.UpdateKey)
                {
                    entry.LastUpdateKey = toast.UpdateKey;
                    entering.Add(entry.Element);
                }
            }
            else
            {
                entry = CreateVisual(toast);
                _visuals[toast.Id] = entry;
                _panel.Children.Add(entry.Element);
                entering.Add(entry.Element);
            }
        }

        Reflow();

        foreach (var element in entering)
        {
            BeginEnter(element);
        }
    }

    private ToastVisualEntry CreateVisual(ToastObject toast)
    {
        var handle = new ToastHandle(Manager!, toast.Id);
        var element = new NaviusToast
        {
            RenderTransform = new System.Windows.Media.TranslateTransform(),
        };
        SyncContent(element, toast);

        element.MouseEnter += (_, _) => handle.Pause();
        element.MouseLeave += (_, _) => handle.Resume();
        element.IsKeyboardFocusWithinChanged += (_, e) =>
        {
            if ((bool)e.NewValue)
            {
                handle.Pause();
            }
            else
            {
                handle.Resume();
            }
        };

        element.CloseRequested += (_, _) => handle.Dismiss();
        element.ActionRequested += (_, _) =>
        {
            toast.Options.Action?.OnClick();
            handle.Dismiss();
        };

        return new ToastVisualEntry(element) { LastUpdateKey = toast.UpdateKey };
    }

    private static void SyncContent(NaviusToast element, ToastObject toast)
    {
        var options = toast.Options;
        element.Title = options.Title;
        element.Description = options.Description;
        element.Type = options.Type;
        element.Priority = options.Priority;
        element.ActionLabel = options.Action?.Label;
        element.ActionAltText = options.Action?.AltText ?? string.Empty;
    }

    /// <summary>Repositions every currently-visible visual on the Canvas from the manager's
    /// newest-first VisibleToasts order, publishing Index/OffsetY as attached properties too.</summary>
    private void Reflow()
    {
        if (_panel is null || Manager is null)
        {
            return;
        }

        // Force a synchronous layout pass so ActualHeight below reflects any content that just
        // changed (new toast added, or Title/Description updated).
        _panel.UpdateLayout();

        var gap = Gap;
        double cumulative = 0;
        var index = 0;
        foreach (var toast in Manager.VisibleToasts)
        {
            if (!_visuals.TryGetValue(toast.Id, out var entry))
            {
                continue;
            }

            var element = entry.Element;
            SetIndex(element, index);
            SetOffsetY(element, cumulative);
            PositionOnCanvas(element, cumulative);

            cumulative += element.ActualHeight + gap;
            index++;
        }
    }

    private void PositionOnCanvas(NaviusToast element, double offsetY)
    {
        var top = Alignment is NaviusToastAlignment.TopLeft or NaviusToastAlignment.TopCenter or NaviusToastAlignment.TopRight;
        if (top)
        {
            Canvas.SetTop(element, offsetY);
            element.ClearValue(Canvas.BottomProperty);
        }
        else
        {
            Canvas.SetBottom(element, offsetY);
            element.ClearValue(Canvas.TopProperty);
        }

        switch (Alignment)
        {
            case NaviusToastAlignment.TopLeft:
            case NaviusToastAlignment.BottomLeft:
                Canvas.SetLeft(element, 0);
                element.ClearValue(Canvas.RightProperty);
                break;
            case NaviusToastAlignment.TopRight:
            case NaviusToastAlignment.BottomRight:
                Canvas.SetRight(element, 0);
                element.ClearValue(Canvas.LeftProperty);
                break;
            default:
                var left = _panel is null ? 0 : Math.Max(0, (_panel.ActualWidth - element.ActualWidth) / 2);
                Canvas.SetLeft(element, left);
                element.ClearValue(Canvas.RightProperty);
                break;
        }
    }

    private bool IsTopAligned =>
        Alignment is NaviusToastAlignment.TopLeft or NaviusToastAlignment.TopCenter or NaviusToastAlignment.TopRight;

    private void BeginEnter(NaviusToast element)
    {
        if (element.RenderTransform is not System.Windows.Media.TranslateTransform transform)
        {
            return;
        }

        var fromY = IsTopAligned ? -SlideDistance : SlideDistance;
        var slide = new DoubleAnimation(fromY, 0, EnterDuration) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
        var fade = new DoubleAnimation(0, 1, EnterDuration);

        transform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slide);
        element.BeginAnimation(OpacityProperty, fade);
    }

    /// <summary>Starts the exit storyboard and removes the visual from the Canvas only once it
    /// completes (contract-adjacent: "exit completes before removal").</summary>
    private void BeginExit(Guid id, ToastVisualEntry entry)
    {
        _visuals.Remove(id);

        var element = entry.Element;
        var toY = IsTopAligned ? -SlideDistance : SlideDistance;

        if (element.RenderTransform is System.Windows.Media.TranslateTransform transform)
        {
            var slide = new DoubleAnimation(0, toY, ExitDuration) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };
            transform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slide);
        }

        var fade = new DoubleAnimation(1, 0, ExitDuration);
        fade.Completed += (_, _) =>
        {
            _panel?.Children.Remove(element);
            Reflow();
        };
        element.BeginAnimation(OpacityProperty, fade);
    }

    // --- F6 focuses the viewport when toasts exist. This is the "real" hotkey the parity spec
    // asked for in place of the web contract's documented-but-untrue claim: the web's `Hotkey`
    // param and per-hotkey LabelTemplate configurability are not ported (kept to a single
    // hardcoded F6), since a configurable multi-key surface has no consumer yet and would just be
    // speculative API. Attached/detached off the ancestor Window's PreviewKeyDown, mirroring
    // OverlayStack's window-level hook lifecycle.

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this);
        if (window is null || ReferenceEquals(window, _hookedWindow))
        {
            return;
        }

        DetachHotkey();
        window.PreviewKeyDown += OnWindowPreviewKeyDown;
        _hookedWindow = window;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => DetachHotkey();

    private void DetachHotkey()
    {
        if (_hookedWindow is null)
        {
            return;
        }

        _hookedWindow.PreviewKeyDown -= OnWindowPreviewKeyDown;
        _hookedWindow = null;
    }

    private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F6 || Manager is null || Manager.VisibleToasts.Count == 0)
        {
            return;
        }

        Focus();
        e.Handled = true;
    }

    private static void SetIndex(DependencyObject element, int value) => element.SetValue(IndexPropertyKey, value);

    private static void SetOffsetY(DependencyObject element, double value) => element.SetValue(OffsetYPropertyKey, value);

    private sealed class ToastVisualEntry(NaviusToast element)
    {
        public NaviusToast Element { get; } = element;
        public int LastUpdateKey { get; set; }
    }
}
