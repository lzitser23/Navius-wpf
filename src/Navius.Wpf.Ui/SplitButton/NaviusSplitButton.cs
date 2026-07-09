using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Navius.Wpf.Primitives.Controls.Menus;

namespace Navius.Wpf.Ui.SplitButton;

/// <summary>
/// A primary action button fused to a chevron that opens a dropdown, sharing one hairline border
/// and one outer radius envelope split by a single vertical divider. The dropdown is a
/// <see cref="NaviusMenuPopup"/> from Navius.Wpf.Primitives' Menu family, opened by an embedded
/// <see cref="NaviusMenuTrigger"/> template part (PART_Chevron in Themes/SplitButton.xaml): this
/// control consumes that primitive's existing open/close/placement logic as-is rather than
/// reimplementing it, only re-skinning the trigger's chrome locally (flush-right corners, no left
/// border) to fuse visually with the primary segment.
/// </summary>
[TemplatePart(Name = PartPrimary, Type = typeof(ButtonBase))]
[TemplatePart(Name = PartChevron, Type = typeof(NaviusMenuTrigger))]
public class NaviusSplitButton : ContentControl
{
    private const string PartPrimary = "PART_Primary";
    private const string PartChevron = "PART_Chevron";

    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
        nameof(Command), typeof(ICommand), typeof(NaviusSplitButton), new PropertyMetadata(null));

    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
        nameof(CommandParameter), typeof(object), typeof(NaviusSplitButton), new PropertyMetadata(null));

    public static readonly DependencyProperty CommandTargetProperty = DependencyProperty.Register(
        nameof(CommandTarget), typeof(IInputElement), typeof(NaviusSplitButton), new PropertyMetadata(null));

    /// <summary>The dropdown opened by the chevron. Build it exactly like a NaviusMenuTrigger.Menu (NaviusMenuItem children).</summary>
    public static readonly DependencyProperty MenuProperty = DependencyProperty.Register(
        nameof(Menu), typeof(NaviusMenuPopup), typeof(NaviusSplitButton), new PropertyMetadata(null));

    public static readonly RoutedEvent ClickEvent = EventManager.RegisterRoutedEvent(
        nameof(Click), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(NaviusSplitButton));

    private ButtonBase? _primaryPart;

    static NaviusSplitButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusSplitButton),
            new FrameworkPropertyMetadata(typeof(NaviusSplitButton)));
    }

    /// <summary>Raised when the primary segment (not the chevron) is activated.</summary>
    public event RoutedEventHandler Click
    {
        add => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public IInputElement? CommandTarget
    {
        get => (IInputElement?)GetValue(CommandTargetProperty);
        set => SetValue(CommandTargetProperty, value);
    }

    public NaviusMenuPopup? Menu
    {
        get => (NaviusMenuPopup?)GetValue(MenuProperty);
        set => SetValue(MenuProperty, value);
    }

    public override void OnApplyTemplate()
    {
        if (_primaryPart is not null)
        {
            _primaryPart.Click -= OnPrimaryClick;
        }

        base.OnApplyTemplate();

        _primaryPart = GetTemplateChild(PartPrimary) as ButtonBase;
        if (_primaryPart is not null)
        {
            _primaryPart.Click += OnPrimaryClick;
        }
    }

    private void OnPrimaryClick(object sender, RoutedEventArgs e) => RaiseEvent(new RoutedEventArgs(ClickEvent, this));
}
