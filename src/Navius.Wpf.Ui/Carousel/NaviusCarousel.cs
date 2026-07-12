using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Navius.Wpf.Ui.Internal;

namespace Navius.Wpf.Ui.Carousel;

/// <summary>
/// Item host with prev/next chevrons, dot indicators, and ArrowLeft/ArrowRight navigation.
/// Derives <see cref="Selector"/> directly (the same WPF base ListBox/TabControl use) rather than a
/// bespoke "current index" property: Selector already gives SelectedIndex/SelectedItem as bindable
/// DPs and, via its <c>Selector.IsSelectedProperty</c> attached property, generic per-container
/// selection state that Themes/Carousel.xaml's ItemContainerStyle uses to show only the current
/// slide (cross-faded) and to light the matching dot.
///
/// Pointer-swipe physics are explicitly out of scope for this pass; only the chevrons, dots, and
/// keyboard arrows drive navigation (see the composite-tier task notes).
/// </summary>
public class NaviusCarousel : Selector
{
    private static readonly DependencyPropertyKey ShouldAnimatePropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(ShouldAnimate), typeof(bool), typeof(NaviusCarousel), new PropertyMetadata(true));

    public static readonly DependencyProperty ShouldAnimateProperty = ShouldAnimatePropertyKey.DependencyProperty;

    public static readonly DependencyProperty LoopProperty = DependencyProperty.Register(
        nameof(Loop), typeof(bool), typeof(NaviusCarousel), new PropertyMetadata(true));

    public static readonly RoutedCommand PreviousCommand = new(nameof(PreviousCommand), typeof(NaviusCarousel));
    public static readonly RoutedCommand NextCommand = new(nameof(NextCommand), typeof(NaviusCarousel));
    public static readonly RoutedCommand GoToSlideCommand = new(nameof(GoToSlideCommand), typeof(NaviusCarousel));

    static NaviusCarousel()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusCarousel),
            new FrameworkPropertyMetadata(typeof(NaviusCarousel)));

        CommandManager.RegisterClassCommandBinding(
            typeof(NaviusCarousel),
            new CommandBinding(PreviousCommand, (s, _) => Move((NaviusCarousel)s, -1)));

        CommandManager.RegisterClassCommandBinding(
            typeof(NaviusCarousel),
            new CommandBinding(NextCommand, (s, _) => Move((NaviusCarousel)s, +1)));

        CommandManager.RegisterClassCommandBinding(
            typeof(NaviusCarousel),
            new CommandBinding(GoToSlideCommand, OnGoToSlideExecuted));
    }

    public NaviusCarousel()
    {
        Focusable = true;
        SetValue(ShouldAnimatePropertyKey, ReducedMotion.AnimationsEnabled);
    }

    /// <summary>Whether slide transitions should animate under the current system motion preference.</summary>
    public bool ShouldAnimate => (bool)GetValue(ShouldAnimateProperty);

    /// <summary>Whether Next from the last slide wraps to the first (and Previous from the first wraps to the last). Default true.</summary>
    public bool Loop
    {
        get => (bool)GetValue(LoopProperty);
        set => SetValue(LoopProperty, value);
    }

    protected override DependencyObject GetContainerForItemOverride() => new ContentPresenter();

    protected override bool IsItemItsOwnContainerOverride(object item) => item is UIElement;

    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(e);

        // A carousel with slides and nothing selected reads as broken (blank host, dead dots);
        // default to the first slide the first time items appear, same "no empty default state"
        // expectation the rest of this composite tier follows.
        if (SelectedIndex < 0 && Items.Count > 0)
        {
            SetCurrentValue(SelectedIndexProperty, 0);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Handled)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Left:
                Move(this, CarouselEngine.DirectionDelta(FlowDirection == FlowDirection.RightToLeft, towardRight: false));
                e.Handled = true;
                break;
            case Key.Right:
                Move(this, CarouselEngine.DirectionDelta(FlowDirection == FlowDirection.RightToLeft, towardRight: true));
                e.Handled = true;
                break;
        }
    }

    private static void Move(NaviusCarousel carousel, int delta)
    {
        var count = carousel.Items.Count;
        var next = CarouselEngine.MoveIndex(carousel.SelectedIndex, count, delta, carousel.Loop);
        if (next >= 0)
        {
            carousel.SetCurrentValue(SelectedIndexProperty, next);
        }
    }

    private static void OnGoToSlideExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (sender is NaviusCarousel carousel && e.Parameter is int index && index >= 0 && index < carousel.Items.Count)
        {
            carousel.SetCurrentValue(SelectedIndexProperty, index);
        }
    }
}
