using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Ui.Resizable;

/// <summary>
/// Convenience host for a row of (or column of) resizable panes: the consumer declares each pane
/// as a plain child element, exactly like a StackPanel --
/// <c>&lt;ui:NaviusResizablePanelGroup&gt;&lt;Border/&gt;&lt;Border/&gt;&lt;/ui:NaviusResizablePanelGroup&gt;</c>
/// -- and this control does the one-time expansion into a real Grid with star-sized
/// ColumnDefinitions/RowDefinitions plus an auto-inserted, keyed-styled GridSplitter between every
/// adjacent pair (Themes/Resizable.xaml's Navius.Splitter.Horizontal/Vertical). Derives from Grid
/// directly rather than reimplementing star-sizing and drag-resize math Grid/GridSplitter already
/// provide.
/// </summary>
public class NaviusResizablePanelGroup : Grid
{
    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation), typeof(Orientation), typeof(NaviusResizablePanelGroup),
        new PropertyMetadata(Orientation.Horizontal));

    public static readonly DependencyProperty SplitterThicknessProperty = DependencyProperty.Register(
        nameof(SplitterThickness), typeof(double), typeof(NaviusResizablePanelGroup),
        new PropertyMetadata(9d));

    /// <summary>Attach to a pane to set its initial star-weight (proportional size). Default 1.</summary>
    public static readonly DependencyProperty InitialSizeProperty = DependencyProperty.RegisterAttached(
        "InitialSize", typeof(double), typeof(NaviusResizablePanelGroup), new PropertyMetadata(1d));

    /// <summary>Attach to a pane to give it a minimum width (Horizontal) or height (Vertical).</summary>
    public static readonly DependencyProperty MinSizeProperty = DependencyProperty.RegisterAttached(
        "MinSize", typeof(double), typeof(NaviusResizablePanelGroup), new PropertyMetadata(48d));

    private bool _expanded;

    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public double SplitterThickness
    {
        get => (double)GetValue(SplitterThicknessProperty);
        set => SetValue(SplitterThicknessProperty, value);
    }

    public static double GetInitialSize(DependencyObject obj) => (double)obj.GetValue(InitialSizeProperty);

    public static void SetInitialSize(DependencyObject obj, double value) => obj.SetValue(InitialSizeProperty, value);

    public static double GetMinSize(DependencyObject obj) => (double)obj.GetValue(MinSizeProperty);

    public static void SetMinSize(DependencyObject obj, double value) => obj.SetValue(MinSizeProperty, value);

    protected override void OnInitialized(System.EventArgs e)
    {
        base.OnInitialized(e);
        EnsureExpanded();
    }

    /// <summary>
    /// One-time transform: captures the panes declared as direct children, clears them, then
    /// rebuilds Children/ColumnDefinitions (or RowDefinitions) interleaving a styled GridSplitter
    /// between every adjacent pair. Runs automatically on <see cref="OnInitialized"/>, which -
    /// unlike XAML-declared usage, where children already exist before EndInit fires - runs
    /// immediately for a plain `new NaviusResizablePanelGroup()` with no children yet added; that
    /// first, empty pass intentionally does NOT latch <see cref="_expanded"/>, so calling this again
    /// once panes exist (the code-behind-construction path) still runs.
    /// </summary>
    public void EnsureExpanded()
    {
        if (_expanded)
        {
            return;
        }

        var panes = Children.OfType<UIElement>().ToList();
        if (panes.Count == 0)
        {
            return;
        }

        _expanded = true;

        Children.Clear();
        ColumnDefinitions.Clear();
        RowDefinitions.Clear();

        var horizontal = Orientation == Orientation.Horizontal;
        var slot = 0;

        for (var i = 0; i < panes.Count; i++)
        {
            var pane = panes[i];
            var star = new GridLength(GetInitialSize(pane), GridUnitType.Star);

            if (horizontal)
            {
                ColumnDefinitions.Add(new ColumnDefinition { Width = star, MinWidth = GetMinSize(pane) });
                SetColumn(pane, slot);
            }
            else
            {
                RowDefinitions.Add(new RowDefinition { Height = star, MinHeight = GetMinSize(pane) });
                SetRow(pane, slot);
            }

            Children.Add(pane);
            slot++;

            var isLast = i == panes.Count - 1;
            if (isLast)
            {
                continue;
            }

            var splitter = new GridSplitter
            {
                ResizeDirection = horizontal ? GridResizeDirection.Columns : GridResizeDirection.Rows,
                ResizeBehavior = GridResizeBehavior.PreviousAndNext,
                HorizontalAlignment = horizontal ? HorizontalAlignment.Center : HorizontalAlignment.Stretch,
                VerticalAlignment = horizontal ? VerticalAlignment.Stretch : VerticalAlignment.Center,
            };

            splitter.SetResourceReference(
                StyleProperty,
                horizontal ? "Navius.Splitter.Horizontal" : "Navius.Splitter.Vertical");

            if (horizontal)
            {
                ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(SplitterThickness) });
                SetColumn(splitter, slot);
            }
            else
            {
                RowDefinitions.Add(new RowDefinition { Height = new GridLength(SplitterThickness) });
                SetRow(splitter, slot);
            }

            Children.Add(splitter);
            slot++;
        }
    }
}
