using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Navius.Wpf.Ui.ButtonGroup;
using Navius.Wpf.Ui.Carousel;
using Navius.Wpf.Ui.InputGroup;
using Navius.Wpf.Ui.Internal;
using Navius.Wpf.Ui.Resizable;
using Navius.Wpf.Ui.SplitButton;
using Navius.Wpf.Primitives.Theming;
using Xunit;

namespace Navius.Wpf.Tests;

/// <summary>Covers the composite-tier items with no dedicated engine class of their own: ButtonGroup, InputGroup, SplitButton, Resizable, Carousel.</summary>
public class UiCompositeTests
{
    private static void EnsureApplication()
    {
        if (Application.Current is null)
        {
            try
            {
                _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            }
            catch (InvalidOperationException)
            {
            }
        }
    }

    // ----- ButtonGroup -----

    [Theory]
    [InlineData(0, 3, false)]
    [InlineData(1, 3, false)]
    [InlineData(2, 3, true)]
    [InlineData(0, 1, true)]
    public void ButtonGroup_IsLast_ComputesExpectedForIndexAndCount(int index, int count, bool expectedLast)
    {
        Assert.Equal(expectedLast, NaviusButtonGroup.IsLast(index, count));
    }

    [Fact]
    public void ButtonGroup_IsLast_EmptyOrNegativeIndex_IsFalse()
    {
        Assert.False(NaviusButtonGroup.IsLast(-1, 3));
        Assert.False(NaviusButtonGroup.IsLast(0, 0));
    }

    [StaFact]
    public void ButtonGroup_Orientation_DefaultsHorizontal()
    {
        var group = new NaviusButtonGroup();

        Assert.Equal(Orientation.Horizontal, group.Orientation);
    }

    [StaFact]
    public void ButtonGroup_PrepareContainer_StampsIsLastItem()
    {
        // ItemsControl only realizes containers through a real ItemsPresenter + layout pass, which
        // needs a merged theme + a live visual tree; PrepareContainerForItemOverride is invoked
        // directly instead (same precedent as AutocompleteTests reflecting into
        // OnInputPreviewKeyDown for headless coverage of protected WPF glue).
        var group = new NaviusButtonGroup();
        var first = new NaviusButtonGroupItem();
        var second = new NaviusButtonGroupItem();
        group.Items.Add(first);
        group.Items.Add(second);

        var prepare = typeof(NaviusButtonGroup).GetMethod(
            "PrepareContainerForItemOverride", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        prepare.Invoke(group, new object[] { first, first });
        prepare.Invoke(group, new object[] { second, second });

        Assert.False(NaviusButtonGroup.GetIsLastItem(first));
        Assert.True(NaviusButtonGroup.GetIsLastItem(second));
    }

    // ----- InputGroup -----

    [StaFact]
    public void InputGroup_Defaults_EmptyTextAndNoAdornments()
    {
        var group = new NaviusInputGroup();

        Assert.Equal(string.Empty, group.Text);
        Assert.Null(group.LeadingContent);
        Assert.Null(group.TrailingContent);
        Assert.False(group.IsReadOnly);
    }

    [StaFact]
    public void InputGroup_TextAndAdornments_RoundTrip()
    {
        var group = new NaviusInputGroup { Text = "hello", Placeholder = "Search...", LeadingContent = "$", TrailingContent = "kg" };

        Assert.Equal("hello", group.Text);
        Assert.Equal("Search...", group.Placeholder);
        Assert.Equal("$", group.LeadingContent);
        Assert.Equal("kg", group.TrailingContent);
    }

    // ----- SplitButton -----

    [StaFact]
    public void SplitButton_Defaults_NoCommandOrMenu()
    {
        var splitButton = new NaviusSplitButton();

        Assert.Null(splitButton.Command);
        Assert.Null(splitButton.Menu);
    }

    [StaFact]
    public void SplitButton_ClickEvent_RaisesOnPrimaryButtonClick()
    {
        // Needs the real Themes/SplitButton.xaml template applied (PART_Primary only exists once
        // it does), so this builds a themed instance the same way AutocompleteTests.CreateThemed
        // does: resolve the implicit style from a merged scope directly rather than relying on
        // automatic resolution timing outside a live visual tree.
        EnsureApplication();
        var scope = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, scope);
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Ui;component/Themes/SplitButton.xaml"),
        });

        var splitButton = new NaviusSplitButton
        {
            Resources = scope,
            Style = (Style)scope[typeof(NaviusSplitButton)],
        };
        splitButton.ApplyTemplate();

        var getTemplateChild = typeof(FrameworkElement).GetMethod(
            "GetTemplateChild", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var primary = (ButtonBase)getTemplateChild.Invoke(splitButton, new object[] { "PART_Primary" })!;
        var raised = false;
        splitButton.Click += (_, _) => raised = true;

        primary.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

        Assert.True(raised);
    }

    // ----- Resizable -----

    [StaFact]
    public void ResizablePanelGroup_ExpandsPanesWithSplittersBetween()
    {
        var group = new NaviusResizablePanelGroup();
        group.Children.Add(new Border());
        group.Children.Add(new Border());
        group.Children.Add(new Border());

        group.EnsureExpanded();

        // 3 panes + 2 splitters = 5 children; 5 column definitions (star, auto, star, auto, star).
        Assert.Equal(5, group.Children.Count);
        Assert.Equal(5, group.ColumnDefinitions.Count);
        Assert.IsType<GridSplitter>(group.Children[1]);
        Assert.IsType<GridSplitter>(group.Children[3]);
    }

    [StaFact]
    public void ResizablePanelGroup_VerticalOrientation_UsesRowDefinitions()
    {
        var group = new NaviusResizablePanelGroup { Orientation = Orientation.Vertical };
        group.Children.Add(new Border());
        group.Children.Add(new Border());

        group.EnsureExpanded();

        Assert.Equal(3, group.RowDefinitions.Count);
        Assert.Empty(group.ColumnDefinitions);
    }

    [StaFact]
    public void ResizablePanelGroup_EnsureExpanded_IsIdempotent()
    {
        var group = new NaviusResizablePanelGroup();
        group.Children.Add(new Border());
        group.Children.Add(new Border());

        group.EnsureExpanded();
        var countAfterFirst = group.Children.Count;
        group.EnsureExpanded();

        Assert.Equal(countAfterFirst, group.Children.Count);
    }

    // ----- Carousel -----

    [Theory]
    [InlineData(0, 3, +1, true, 1)]
    [InlineData(2, 3, +1, true, 0)] // loops past the last slide
    [InlineData(0, 3, -1, true, 2)] // loops before the first slide
    [InlineData(2, 3, +1, false, 2)] // no-loop clamps at the last slide
    [InlineData(0, 3, -1, false, 0)] // no-loop clamps at the first slide
    public void CarouselEngine_MoveIndex_ComputesExpected(int current, int count, int delta, bool loop, int expected)
    {
        Assert.Equal(expected, CarouselEngine.MoveIndex(current, count, delta, loop));
    }

    [Fact]
    public void CarouselEngine_MoveIndex_NoSlides_ReturnsNegativeOne()
    {
        Assert.Equal(-1, CarouselEngine.MoveIndex(0, 0, +1));
    }

    [Theory]
    [InlineData(false, false, -1)]
    [InlineData(false, true, 1)]
    [InlineData(true, false, 1)]
    [InlineData(true, true, -1)]
    public void CarouselEngine_DirectionDelta_MirrorsUnderRtl(bool rightToLeft, bool towardRight, int expected)
    {
        Assert.Equal(expected, CarouselEngine.DirectionDelta(rightToLeft, towardRight));
    }

    [Fact]
    public void CarouselSlideNameConverter_UsesOneBasedAccessibleName()
    {
        var converter = new CarouselSlideNameConverter();

        Assert.Equal("Slide 1", converter.Convert(0, typeof(string), null, CultureInfo.InvariantCulture));
    }

    [StaFact]
    public void Carousel_Defaults_LoopsByDefault()
    {
        var carousel = new NaviusCarousel();

        Assert.True(carousel.Loop);
        Assert.Equal(-1, carousel.SelectedIndex);
    }

    [StaFact]
    public void Carousel_AddingItems_SelectsFirstSlideAutomatically()
    {
        var carousel = new NaviusCarousel();

        carousel.Items.Add(new Border());
        carousel.Items.Add(new Border());

        Assert.Equal(0, carousel.SelectedIndex);
    }

    [StaFact]
    public void Carousel_ReducedMotion_DisablesSlideAnimation()
    {
        ReducedMotion.SetTestOverride(() => false);
        try
        {
            var carousel = new NaviusCarousel();

            Assert.False(carousel.ShouldAnimate);
        }
        finally
        {
            ReducedMotion.SetTestOverride(null);
        }
    }

    [StaFact]
    public void Carousel_ReducedMotion_CollapsesInactiveSlideImmediately()
    {
        ReducedMotion.SetTestOverride(() => false);
        Window? window = null;
        try
        {
            EnsureApplication();
            var scope = new ResourceDictionary();
            ThemeManager.Apply(NaviusTheme.Light, scope);
            scope.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/Navius.Wpf.Ui;component/Themes/Carousel.xaml"),
            });

            var first = new Border();
            var second = new Border();
            var carousel = new NaviusCarousel
            {
                Resources = scope,
                Style = (Style)scope[typeof(NaviusCarousel)],
            };
            carousel.Items.Add(first);
            carousel.Items.Add(second);

            window = new Window { Content = carousel, Width = 400, Height = 240, ShowInTaskbar = false };
            window.Show();
            carousel.ApplyTemplate();
            carousel.UpdateLayout();
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

            Assert.Equal(Visibility.Visible, first.Visibility);
            Assert.Equal(Visibility.Collapsed, second.Visibility);

            carousel.SelectedIndex = 1;
            carousel.UpdateLayout();
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

            Assert.Equal(Visibility.Collapsed, first.Visibility);
            Assert.Equal(Visibility.Visible, second.Visibility);
        }
        finally
        {
            window?.Close();
            ReducedMotion.SetTestOverride(null);
        }
    }

    [StaFact]
    public void Carousel_GoToSlideCommand_SelectsRequestedIndex()
    {
        var carousel = new NaviusCarousel();
        carousel.Items.Add(new Border());
        carousel.Items.Add(new Border());
        carousel.Items.Add(new Border());

        NaviusCarousel.GoToSlideCommand.Execute(2, carousel);

        Assert.Equal(2, carousel.SelectedIndex);
    }
}
