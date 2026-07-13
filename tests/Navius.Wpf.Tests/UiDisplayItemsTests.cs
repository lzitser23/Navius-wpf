using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Navius.Wpf.Ui.Alert;
using Navius.Wpf.Ui.Badge;
using Navius.Wpf.Ui.Card;
using Navius.Wpf.Ui.Empty;
using Navius.Wpf.Ui.Internal;
using Navius.Wpf.Ui.Item;
using Navius.Wpf.Ui.Kbd;
using Navius.Wpf.Ui.Skeleton;
using Navius.Wpf.Ui.Spinner;
using Navius.Wpf.Ui.Timeline;

namespace Navius.Wpf.Tests;

/// <summary>
/// Covers the eleven Navius.Wpf.Ui display items: template application (each item's own
/// Themes/&lt;Item&gt;.xaml, loaded the same way a consumer would via a pack URI), variant
/// switching for the items that have variants, and the Skeleton/Spinner reduced-motion guard
/// (ReducedMotion.SetTestOverride, so the OS SystemParameters.ClientAreaAnimation setting is
/// never touched by the suite).
/// </summary>
public class UiDisplayItemsTests
{
    static UiDisplayItemsTests()
    {
        // pack://application URIs only resolve once an Application exists in the process.
        // Guarded try/catch (rather than a bare null-check) because xunit runs test classes in
        // parallel on separate STA threads: another test class's static ctor can win the race.
        if (Application.Current is null)
        {
            try
            {
                _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            }
            catch (InvalidOperationException)
            {
                // Another test class's static ctor already created the process-wide Application.
            }
        }

        // Unlike NaviusButton/NaviusProgress etc., these items' own dictionaries only define the
        // Style/Template; the {DynamicResource Navius.*} brushes they reference come from
        // Tokens.Light.xaml (normally supplied by ThemeManager at app startup). Merge it once,
        // process-wide, so variant-switching assertions can check resolved colors, not just that
        // a Style applied. Guarded on a known key rather than nesting inside the block above:
        // whichever test class's static ctor runs first still needs to merge tokens even when it
        // lost the Application-creation race.
        var application = Application.Current
            ?? throw new InvalidOperationException("A WPF Application is required for UI display tests.");
        if (!application.Resources.Contains("Navius.Border"))
        {
            application.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Tokens.Light.xaml", UriKind.Absolute),
            });
        }
    }

    private static ResourceDictionary MergeTheme(string fileName)
    {
        var dictionary = new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/Navius.Wpf.Ui;component/Themes/{fileName}", UriKind.Absolute),
        };
        Application.Current.Resources.MergedDictionaries.Add(dictionary);
        return dictionary;
    }

    private static void ApplyStyleAndTemplate(FrameworkElement element, Type styleKeyType)
    {
        // Elements outside a live visual/logical tree don't automatically pick up an implicit
        // (TargetType-keyed) style; wire it explicitly, same as WPF does internally once an
        // element is parented (mirrors ProgressTests/MeterTests in this project).
        element.SetResourceReference(FrameworkElement.StyleProperty, styleKeyType);
        element.ApplyTemplate();
    }

    // --- Card ---

    [StaFact]
    public void Card_TemplateApplies_AndPartsCompose()
    {
        var dictionary = MergeTheme("Card.xaml");
        try
        {
            var card = new NaviusCard
            {
                Content = new StackPanel
                {
                    Children =
                    {
                        new NaviusCardHeader { Content = new NaviusCardTitle { Content = "Title" } },
                        new NaviusCardContent { Content = "Body" },
                        new NaviusCardFooter { Content = "Footer" },
                    },
                },
            };
            ApplyStyleAndTemplate(card, typeof(NaviusCard));

            Assert.NotNull(card.Template);
        }
        finally
        {
            Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        }
    }

    [StaFact]
    public void Card_Padding_ReachesTemplateRootBorder()
    {
        var dictionary = MergeTheme("Card.xaml");
        try
        {
            var card = new NaviusCard { Padding = new Thickness(16) };
            ApplyStyleAndTemplate(card, typeof(NaviusCard));

            var border = Assert.IsType<Border>(VisualTreeHelper.GetChild(card, 0));
            Assert.Equal(new Thickness(16), border.Padding);
        }
        finally
        {
            Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        }
    }

    [StaFact]
    public void Card_DefaultPadding_IsZeroOnTemplateRootBorder()
    {
        var dictionary = MergeTheme("Card.xaml");
        try
        {
            var card = new NaviusCard();
            ApplyStyleAndTemplate(card, typeof(NaviusCard));

            var border = Assert.IsType<Border>(VisualTreeHelper.GetChild(card, 0));
            Assert.Equal(new Thickness(0), border.Padding);
        }
        finally
        {
            Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        }
    }

    // --- Alert ---

    [StaFact]
    public void Alert_DefaultVariant_UsesNeutralBorder()
    {
        var dictionary = MergeTheme("Alert.xaml");
        try
        {
            var alert = new NaviusAlert { Variant = NaviusAlertVariant.Default };
            ApplyStyleAndTemplate(alert, typeof(NaviusAlert));

            var borderBrush = Assert.IsType<SolidColorBrush>(alert.BorderBrush);
            Assert.Equal((Color)ColorConverter.ConvertFromString("#E6E4DE"), borderBrush.Color);
        }
        finally
        {
            Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        }
    }

    [StaFact]
    public void Alert_DestructiveVariant_SwitchesBorderAndForegroundToDestructive()
    {
        var dictionary = MergeTheme("Alert.xaml");
        try
        {
            var alert = new NaviusAlert { Variant = NaviusAlertVariant.Destructive };
            ApplyStyleAndTemplate(alert, typeof(NaviusAlert));

            var borderBrush = Assert.IsType<SolidColorBrush>(alert.BorderBrush);
            var foreground = Assert.IsType<SolidColorBrush>(alert.Foreground);
            Assert.Equal((Color)ColorConverter.ConvertFromString("#E7000B"), borderBrush.Color);
            Assert.Equal((Color)ColorConverter.ConvertFromString("#E7000B"), foreground.Color);
        }
        finally
        {
            Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        }
    }

    [StaFact]
    public void Alert_WarningVariant_SwitchesBorderAndForegroundToWarning()
    {
        var dictionary = MergeTheme("Alert.xaml");
        try
        {
            var alert = new NaviusAlert { Variant = NaviusAlertVariant.Warning };
            ApplyStyleAndTemplate(alert, typeof(NaviusAlert));

            var borderBrush = Assert.IsType<SolidColorBrush>(alert.BorderBrush);
            var foreground = Assert.IsType<SolidColorBrush>(alert.Foreground);
            Assert.Equal((Color)ColorConverter.ConvertFromString("#9A6700"), borderBrush.Color);
            Assert.Equal((Color)ColorConverter.ConvertFromString("#9A6700"), foreground.Color);
        }
        finally
        {
            Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        }
    }

    // --- Badge ---

    [StaFact]
    public void Badge_DefaultVariant_UsesPrimaryBackground()
    {
        var dictionary = MergeTheme("Badge.xaml");
        try
        {
            var badge = new NaviusBadge { Variant = NaviusBadgeVariant.Default };
            ApplyStyleAndTemplate(badge, typeof(NaviusBadge));

            var background = Assert.IsType<SolidColorBrush>(badge.Background);
            Assert.Equal((Color)ColorConverter.ConvertFromString("#171614"), background.Color);
        }
        finally
        {
            Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        }
    }

    [StaFact]
    public void Badge_PillRadiusTracksItsRenderedHeight_AtAnyFontSize()
    {
        var dictionary = MergeTheme("Badge.xaml");
        try
        {
            foreach (var fontSize in new[] { 11d, 24d })
            {
                var badge = new NaviusBadge { Content = "Live", FontSize = fontSize };
                ApplyStyleAndTemplate(badge, typeof(NaviusBadge));
                badge.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                badge.Arrange(new Rect(badge.DesiredSize));
                badge.UpdateLayout();

                var border = Assert.IsType<Border>(VisualTreeHelper.GetChild(badge, 0));
                // A capsule, not an ellipse: circular ends of exactly half the rendered height,
                // with straight horizontal sides left between them.
                Assert.True(border.ActualHeight > 0);
                Assert.Equal(border.ActualHeight / 2, border.CornerRadius.TopLeft, 3);
                Assert.Equal(border.CornerRadius.TopLeft, border.CornerRadius.BottomRight, 3);
                Assert.True(border.ActualWidth > border.ActualHeight);
            }
        }
        finally
        {
            Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        }
    }

    [StaFact]
    public void Badge_DestructiveVariant_SwitchesToDestructiveBackground()
    {
        var dictionary = MergeTheme("Badge.xaml");
        try
        {
            var badge = new NaviusBadge { Variant = NaviusBadgeVariant.Destructive };
            ApplyStyleAndTemplate(badge, typeof(NaviusBadge));

            var background = Assert.IsType<SolidColorBrush>(badge.Background);
            Assert.Equal((Color)ColorConverter.ConvertFromString("#E7000B"), background.Color);
        }
        finally
        {
            Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        }
    }

    [StaFact]
    public void Badge_OutlineVariant_IsTransparentWithBorder()
    {
        var dictionary = MergeTheme("Badge.xaml");
        try
        {
            var badge = new NaviusBadge { Variant = NaviusBadgeVariant.Outline };
            ApplyStyleAndTemplate(badge, typeof(NaviusBadge));

            Assert.Equal(Colors.Transparent, Assert.IsType<SolidColorBrush>(badge.Background).Color);
        }
        finally
        {
            Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        }
    }

    // --- Skeleton (reduced motion) ---

    [StaFact]
    public void Skeleton_ShouldAnimate_TrueWhenReducedMotionOverrideAllowsAnimation()
    {
        ReducedMotion.SetTestOverride(() => true);
        try
        {
            var skeleton = new NaviusSkeleton();
            Assert.True(skeleton.ShouldAnimate);
        }
        finally
        {
            ReducedMotion.SetTestOverride(null);
        }
    }

    [StaFact]
    public void Skeleton_ShouldAnimate_FalseWhenReducedMotionOverrideDisablesAnimation()
    {
        ReducedMotion.SetTestOverride(() => false);
        try
        {
            var skeleton = new NaviusSkeleton();
            Assert.False(skeleton.ShouldAnimate);
        }
        finally
        {
            ReducedMotion.SetTestOverride(null);
        }
    }

    [StaFact]
    public void Skeleton_TemplateApplies()
    {
        var dictionary = MergeTheme("Skeleton.xaml");
        try
        {
            ReducedMotion.SetTestOverride(() => false);
            var skeleton = new NaviusSkeleton { Width = 120, Height = 16 };
            ApplyStyleAndTemplate(skeleton, typeof(NaviusSkeleton));

            Assert.NotNull(skeleton.Template);
        }
        finally
        {
            ReducedMotion.SetTestOverride(null);
            Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        }
    }

    // --- Spinner (reduced motion) ---

    [StaFact]
    public void Spinner_ShouldAnimate_TrueWhenReducedMotionOverrideAllowsAnimation()
    {
        ReducedMotion.SetTestOverride(() => true);
        try
        {
            var spinner = new NaviusSpinner();
            Assert.True(spinner.ShouldAnimate);
        }
        finally
        {
            ReducedMotion.SetTestOverride(null);
        }
    }

    [StaFact]
    public void Spinner_ShouldAnimate_FalseWhenReducedMotionOverrideDisablesAnimation()
    {
        ReducedMotion.SetTestOverride(() => false);
        try
        {
            var spinner = new NaviusSpinner();
            Assert.False(spinner.ShouldAnimate);
        }
        finally
        {
            ReducedMotion.SetTestOverride(null);
        }
    }

    [StaFact]
    public void Spinner_TemplateApplies()
    {
        var dictionary = MergeTheme("Spinner.xaml");
        try
        {
            ReducedMotion.SetTestOverride(() => false);
            var spinner = new NaviusSpinner();
            ApplyStyleAndTemplate(spinner, typeof(NaviusSpinner));

            Assert.NotNull(spinner.Template);
        }
        finally
        {
            ReducedMotion.SetTestOverride(null);
            Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        }
    }

    // --- Kbd ---

    [StaFact]
    public void Kbd_TemplateApplies()
    {
        var dictionary = MergeTheme("Kbd.xaml");
        try
        {
            var kbd = new NaviusKbd { Content = "Ctrl" };
            ApplyStyleAndTemplate(kbd, typeof(NaviusKbd));

            Assert.NotNull(kbd.Template);
        }
        finally
        {
            Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        }
    }

    [StaFact]
    public void KbdGroup_TemplateApplies_AndHostsMultipleKeys()
    {
        var dictionary = MergeTheme("Kbd.xaml");
        try
        {
            var group = new NaviusKbdGroup
            {
                ItemsSource = new object[] { new NaviusKbd { Content = "Ctrl" }, new NaviusKbd { Content = "K" } },
            };
            ApplyStyleAndTemplate(group, typeof(NaviusKbdGroup));

            Assert.NotNull(group.Template);
            Assert.Equal(2, group.Items.Count);
        }
        finally
        {
            Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        }
    }

    // --- Typography ---

    [StaFact]
    public void Typography_H1Style_SetsExpectedFontSizeAndWeight()
    {
        var dictionary = MergeTheme("Typography.xaml");
        try
        {
            var style = Assert.IsType<Style>(Application.Current.Resources["Navius.Typography.H1"]);
            var textBlock = new TextBlock { Style = style };
            textBlock.ApplyTemplate();

            Assert.Equal(34, textBlock.FontSize);
            Assert.Equal(FontWeights.ExtraBold, textBlock.FontWeight);
        }
        finally
        {
            Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        }
    }

    [StaFact]
    public void Typography_MutedStyle_UsesMutedForeground()
    {
        var dictionary = MergeTheme("Typography.xaml");
        try
        {
            var style = Assert.IsType<Style>(Application.Current.Resources["Navius.Typography.Muted"]);
            var textBlock = new TextBlock { Style = style };
            textBlock.ApplyTemplate();

            var foreground = Assert.IsType<SolidColorBrush>(textBlock.Foreground);
            Assert.Equal((Color)ColorConverter.ConvertFromString("#737270"), foreground.Color);
        }
        finally
        {
            Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        }
    }

    // --- Empty ---

    [StaFact]
    public void Empty_TemplateApplies_AndPartsCompose()
    {
        var dictionary = MergeTheme("Empty.xaml");
        try
        {
            var empty = new NaviusEmpty
            {
                Content = new StackPanel
                {
                    Children =
                    {
                        new NaviusEmptyMedia { Variant = NaviusEmptyMediaVariant.Icon, Content = "!" },
                        new NaviusEmptyTitle { Content = "No results" },
                        new NaviusEmptyDescription { Content = "Try a different search." },
                    },
                },
            };
            ApplyStyleAndTemplate(empty, typeof(NaviusEmpty));

            Assert.NotNull(empty.Template);
        }
        finally
        {
            Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        }
    }

    [StaFact]
    public void EmptyMedia_IconVariant_TemplateApplies()
    {
        var dictionary = MergeTheme("Empty.xaml");
        try
        {
            var media = new NaviusEmptyMedia { Variant = NaviusEmptyMediaVariant.Icon };
            ApplyStyleAndTemplate(media, typeof(NaviusEmptyMedia));

            Assert.NotNull(media.Template);
        }
        finally
        {
            Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        }
    }

    // --- Item ---

    [StaFact]
    public void Item_DefaultVariant_TemplateApplies()
    {
        var dictionary = MergeTheme("Item.xaml");
        try
        {
            var item = new NaviusItem
            {
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Children =
                    {
                        new NaviusItemMedia { Variant = NaviusItemMediaVariant.Icon },
                        new NaviusItemContent
                        {
                            Children =
                            {
                                new NaviusItemTitle { Content = "Title" },
                                new NaviusItemDescription { Content = "Description" },
                            },
                        },
                    },
                },
            };
            ApplyStyleAndTemplate(item, typeof(NaviusItem));

            Assert.NotNull(item.Template);
        }
        finally
        {
            Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        }
    }

    [StaFact]
    public void Item_MutedVariant_SwitchesBackgroundToMuted()
    {
        var dictionary = MergeTheme("Item.xaml");
        try
        {
            var item = new NaviusItem { Variant = NaviusItemVariant.Muted };
            ApplyStyleAndTemplate(item, typeof(NaviusItem));

            var background = Assert.IsType<SolidColorBrush>(item.Background);
            Assert.Equal((Color)ColorConverter.ConvertFromString("#F1F1F0"), background.Color);
        }
        finally
        {
            Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        }
    }

    [StaFact]
    public void Item_SmallSize_ReducesPadding()
    {
        var dictionary = MergeTheme("Item.xaml");
        try
        {
            var defaultItem = new NaviusItem { Size = NaviusItemSize.Default };
            ApplyStyleAndTemplate(defaultItem, typeof(NaviusItem));

            var smallItem = new NaviusItem { Size = NaviusItemSize.Small };
            ApplyStyleAndTemplate(smallItem, typeof(NaviusItem));

            Assert.True(smallItem.Padding.Top < defaultItem.Padding.Top);
        }
        finally
        {
            Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        }
    }

    // --- Table ---

    [StaFact]
    public void Table_KeyedStyles_ResolveWithExpectedTargetTypes()
    {
        var dictionary = MergeTheme("Table.xaml");
        try
        {
            var tableStyle = Assert.IsType<Style>(Application.Current.Resources["Navius.Table"]);
            var headerStyle = Assert.IsType<Style>(Application.Current.Resources["Navius.Table.ColumnHeader"]);
            var rowStyle = Assert.IsType<Style>(Application.Current.Resources["Navius.Table.Row"]);

            Assert.Equal(typeof(ListView), tableStyle.TargetType);
            Assert.Equal(typeof(GridViewColumnHeader), headerStyle.TargetType);
            Assert.Equal(typeof(ListViewItem), rowStyle.TargetType);
        }
        finally
        {
            Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        }
    }

    [StaFact]
    public void Table_RowStyle_TemplateApplies()
    {
        var dictionary = MergeTheme("Table.xaml");
        try
        {
            var rowStyle = (Style)Application.Current.Resources["Navius.Table.Row"];
            var row = new ListViewItem { Style = rowStyle, Content = "cell" };
            row.ApplyTemplate();

            Assert.NotNull(row.Template);
        }
        finally
        {
            Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        }
    }

    [StaFact]
    public void Table_ColumnHeaderStyle_TemplateApplies()
    {
        var dictionary = MergeTheme("Table.xaml");
        try
        {
            var headerStyle = (Style)Application.Current.Resources["Navius.Table.ColumnHeader"];
            var header = new GridViewColumnHeader { Style = headerStyle, Content = "Name" };
            header.ApplyTemplate();

            Assert.NotNull(header.Template);
        }
        finally
        {
            Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        }
    }

    // --- Timeline ---

    [StaFact]
    public void Timeline_TemplateApplies_AndHostsItems()
    {
        var dictionary = MergeTheme("Timeline.xaml");
        try
        {
            var timeline = new NaviusTimeline
            {
                ItemsSource = new object[]
                {
                    new NaviusTimelineItem
                    {
                        Header = new NaviusTimelineDot(),
                        Content = new NaviusTimelineContent
                        {
                            Children = { new NaviusTimelineTitle { Content = "Step 1" } },
                        },
                    },
                },
            };
            ApplyStyleAndTemplate(timeline, typeof(NaviusTimeline));

            Assert.NotNull(timeline.Template);
            Assert.Single(timeline.Items);
        }
        finally
        {
            Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        }
    }

    [StaFact]
    public void TimelineItem_TemplateApplies_WithHeaderAndContentSlots()
    {
        var dictionary = MergeTheme("Timeline.xaml");
        try
        {
            var item = new NaviusTimelineItem { Header = new NaviusTimelineDot(), Content = "Body" };
            ApplyStyleAndTemplate(item, typeof(NaviusTimelineItem));

            Assert.NotNull(item.Template);
        }
        finally
        {
            Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        }
    }

    [StaFact]
    public void TimelineDot_DefaultVariant_UsesPrimaryBackground()
    {
        var dictionary = MergeTheme("Timeline.xaml");
        try
        {
            var dot = new NaviusTimelineDot { Variant = NaviusTimelineDotVariant.Default };
            ApplyStyleAndTemplate(dot, typeof(NaviusTimelineDot));

            var background = Assert.IsType<SolidColorBrush>(dot.Background);
            Assert.Equal((Color)ColorConverter.ConvertFromString("#171614"), background.Color);
        }
        finally
        {
            Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        }
    }

    [StaFact]
    public void TimelineDot_DestructiveVariant_SwitchesToDestructiveBackground()
    {
        var dictionary = MergeTheme("Timeline.xaml");
        try
        {
            var dot = new NaviusTimelineDot { Variant = NaviusTimelineDotVariant.Destructive };
            ApplyStyleAndTemplate(dot, typeof(NaviusTimelineDot));

            var background = Assert.IsType<SolidColorBrush>(dot.Background);
            Assert.Equal((Color)ColorConverter.ConvertFromString("#E7000B"), background.Color);
        }
        finally
        {
            Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        }
    }

    [StaFact]
    public void TimelineConnector_TemplateApplies()
    {
        var dictionary = MergeTheme("Timeline.xaml");
        try
        {
            var connector = new NaviusTimelineConnector();
            ApplyStyleAndTemplate(connector, typeof(NaviusTimelineConnector));

            Assert.NotNull(connector.Template);
        }
        finally
        {
            Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        }
    }
}
