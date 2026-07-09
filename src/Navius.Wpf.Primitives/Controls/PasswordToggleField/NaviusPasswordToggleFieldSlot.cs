using System.Windows;

namespace Navius.Wpf.Primitives.Controls.PasswordToggleField;

/// <summary>
/// Like Icon, plus a fully custom render-prop path (ContentFactory), mirroring the web
/// contract's Render RenderFragment&lt;bool&gt; -- it receives the current revealed state and
/// takes precedence over VisibleContent/HiddenContent when set.
/// </summary>
public class NaviusPasswordToggleFieldSlot : PasswordToggleFieldSlotBase
{
    public static readonly DependencyProperty ContentFactoryProperty = DependencyProperty.Register(
        nameof(ContentFactory), typeof(Func<bool, object>), typeof(NaviusPasswordToggleFieldSlot), new PropertyMetadata(null, OnFactoryChanged));

    static NaviusPasswordToggleFieldSlot()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusPasswordToggleFieldSlot),
            new FrameworkPropertyMetadata(typeof(NaviusPasswordToggleFieldSlot)));
    }

    public Func<bool, object>? ContentFactory
    {
        get => (Func<bool, object>?)GetValue(ContentFactoryProperty);
        set => SetValue(ContentFactoryProperty, value);
    }

    protected override void ApplyContent()
    {
        if (ContentFactory is not null)
        {
            Content = ContentFactory(IsRevealed);
            return;
        }

        base.ApplyContent();
    }

    private static void OnFactoryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusPasswordToggleFieldSlot)d).ApplyContent();
}
