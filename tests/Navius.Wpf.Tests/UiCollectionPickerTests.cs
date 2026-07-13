using System;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Navius.Wpf.Primitives.Theming;
using Navius.Wpf.Ui.CollectionPicker;

namespace Navius.Wpf.Tests;

public class UiCollectionPickerTests
{
    static UiCollectionPickerTests()
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

    private static ResourceDictionary CreateResources(NaviusTheme theme)
    {
        var resources = new ResourceDictionary();
        ThemeManager.Apply(theme, resources);
        resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Ui;component/Themes/CollectionPicker.xaml"),
        });
        return resources;
    }

    [StaFact]
    public void Defaults_UseNativeSingleSelectionAndWrapPanel()
    {
        var resources = CreateResources(NaviusTheme.Light);
        var picker = new TestCollectionPicker
        {
            Resources = resources,
            Style = (Style)resources[typeof(NaviusCollectionPicker)],
        };

        Assert.Equal(SelectionMode.Single, picker.SelectionMode);
        Assert.IsType<WrapPanel>(picker.ItemsPanel.LoadContent());
    }

    [StaFact]
    public void GeneratedContainer_IsCollectionPickerItem()
    {
        var picker = new TestCollectionPicker();

        Assert.IsType<NaviusCollectionPickerItem>(picker.CreateContainer());
        Assert.True(picker.IsOwnContainer(new NaviusCollectionPickerItem()));
        Assert.False(picker.IsOwnContainer("theme"));
    }

    private sealed class NullTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate? SelectTemplate(object? item, DependencyObject container) => null;
    }

    [StaFact]
    public void PickerItem_ForwardsTheFullContentContract_ToItsPresenter()
    {
        var resources = CreateResources(NaviusTheme.Light);
        var selector = new NullTemplateSelector();
        var item = new NaviusCollectionPickerItem
        {
            Resources = resources,
            Style = (Style)resources[typeof(NaviusCollectionPickerItem)],
            Content = 42,
            ContentTemplateSelector = selector,
            ContentStringFormat = "#{0}",
        };

        item.Measure(new Size(500, 500));
        item.Arrange(new Rect(0, 0, 500, 500));
        item.UpdateLayout();

        var border = Assert.IsType<Border>(VisualTreeHelper.GetChild(item, 0));
        var presenter = Assert.IsType<ContentPresenter>(border.Child);
        Assert.Same(selector, presenter.ContentTemplateSelector);
        Assert.Equal("#{0}", presenter.ContentStringFormat);
    }

    [StaFact]
    public void ItemsSourceAndSelectedItem_UseNativeListBoxContract()
    {
        var themes = new[] { "Light", "Dark", "System" };
        var picker = new NaviusCollectionPicker { ItemsSource = themes, SelectedIndex = 1 };

        Assert.Equal("Dark", picker.SelectedItem);

        picker.SelectedItem = "System";

        Assert.Equal(2, picker.SelectedIndex);
    }

    [StaFact]
    public void SelectedItem_UsesThemeTokensAndRethemesLive()
    {
        var resources = CreateResources(NaviusTheme.Light);
        var item = new NaviusCollectionPickerItem
        {
            Resources = resources,
            IsSelected = true,
        };
        item.Style = (Style)resources[typeof(NaviusCollectionPickerItem)];

        var lightBorder = Assert.IsType<SolidColorBrush>(item.BorderBrush);
        Assert.Equal((Color)ColorConverter.ConvertFromString("#171614"), lightBorder.Color);

        ThemeManager.Apply(NaviusTheme.Dark, resources);

        var darkBorder = Assert.IsType<SolidColorBrush>(item.BorderBrush);
        Assert.Equal((Color)ColorConverter.ConvertFromString("#F0EFEC"), darkBorder.Color);
    }

    [StaFact]
    public void RightArrow_MovesNativeSelectionAcrossWrapRow()
    {
        var picker = new TestCollectionPicker
        {
            Resources = CreateResources(NaviusTheme.Light),
            SelectedIndex = 0,
        };
        picker.Style = (Style)picker.Resources[typeof(NaviusCollectionPicker)];
        picker.Items.Add("Light");
        picker.Items.Add("Dark");

        var window = new Window { Content = picker, Width = 320, Height = 180, ShowInTaskbar = false };
        try
        {
            window.Show();
            window.Activate();
            picker.UpdateLayout();
            var first = Assert.IsAssignableFrom<ListBoxItem>(picker.ItemContainerGenerator.ContainerFromIndex(0));
            Keyboard.Focus(first);
            Assert.Same(first, Keyboard.FocusedElement);

            picker.SendKey(Key.Right, first);

            Assert.Equal(1, picker.SelectedIndex);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Automation_UsesNativeListAndSelectionPatterns()
    {
        var picker = new TestCollectionPicker();
        var item = new NaviusCollectionPickerItem();
        var pickerPeer = picker.CreatePeer();
        var itemPeer = new ListBoxItemAutomationPeer(item, (SelectorAutomationPeer)pickerPeer);

        Assert.IsType<ListBoxAutomationPeer>(pickerPeer);
        Assert.Equal(AutomationControlType.List, pickerPeer.GetAutomationControlType());
        Assert.IsAssignableFrom<ISelectionProvider>(pickerPeer.GetPattern(PatternInterface.Selection));
        Assert.Equal(AutomationControlType.ListItem, itemPeer.GetAutomationControlType());
        Assert.IsAssignableFrom<ISelectionItemProvider>(itemPeer.GetPattern(PatternInterface.SelectionItem));
    }

    private sealed class TestCollectionPicker : NaviusCollectionPicker
    {
        public DependencyObject CreateContainer() => GetContainerForItemOverride();

        public bool IsOwnContainer(object item) => IsItemItsOwnContainerOverride(item);

        public AutomationPeer CreatePeer() => OnCreateAutomationPeer();

        public void SendKey(Key key, Visual source) => OnKeyDown(new KeyEventArgs(
            Keyboard.PrimaryDevice,
            PresentationSource.FromVisual(source),
            0,
            key)
        {
            RoutedEvent = Keyboard.KeyDownEvent,
        });
    }
}
