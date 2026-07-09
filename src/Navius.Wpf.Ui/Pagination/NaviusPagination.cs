using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Navius.Wpf.Ui.Pagination;

/// <summary>
/// Prev/next chevrons plus a computed strip of page numbers and ellipsis gaps (see
/// <see cref="PaginationEngine"/> for the pure layout math). The current page renders as a pressed
/// toggle (reuses <see cref="Primitives.Controls.ToggleGroup.NaviusToggleGroupItem"/> verbatim from
/// Navius.Wpf.Primitives for its data-pressed chrome rather than a new button type), navigated via
/// three class <see cref="RoutedCommand"/>s so Themes/Pagination.xaml can wire buttons declaratively
/// (same CommandManager.RegisterClassCommandBinding precedent as
/// NaviusOverlaySurfaceBase.CloseCommand).
/// </summary>
public class NaviusPagination : Control
{
    public static readonly DependencyProperty TotalPagesProperty = DependencyProperty.Register(
        nameof(TotalPages), typeof(int), typeof(NaviusPagination),
        new PropertyMetadata(1, OnLayoutInputChanged));

    public static readonly DependencyProperty CurrentPageProperty = DependencyProperty.Register(
        nameof(CurrentPage), typeof(int), typeof(NaviusPagination),
        new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnLayoutInputChanged));

    public static readonly DependencyProperty SiblingCountProperty = DependencyProperty.Register(
        nameof(SiblingCount), typeof(int), typeof(NaviusPagination),
        new PropertyMetadata(1, OnLayoutInputChanged));

    public static readonly DependencyProperty BoundaryCountProperty = DependencyProperty.Register(
        nameof(BoundaryCount), typeof(int), typeof(NaviusPagination),
        new PropertyMetadata(1, OnLayoutInputChanged));

    private static readonly DependencyPropertyKey PagesPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(Pages), typeof(IReadOnlyList<PaginationPageToken>), typeof(NaviusPagination),
        new PropertyMetadata(Array.Empty<PaginationPageToken>()));

    public static readonly DependencyProperty PagesProperty = PagesPropertyKey.DependencyProperty;

    public static readonly RoutedCommand PreviousCommand = new(nameof(PreviousCommand), typeof(NaviusPagination));
    public static readonly RoutedCommand NextCommand = new(nameof(NextCommand), typeof(NaviusPagination));
    public static readonly RoutedCommand GoToPageCommand = new(nameof(GoToPageCommand), typeof(NaviusPagination));

    static NaviusPagination()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusPagination),
            new FrameworkPropertyMetadata(typeof(NaviusPagination)));

        CommandManager.RegisterClassCommandBinding(
            typeof(NaviusPagination),
            new CommandBinding(PreviousCommand, OnPreviousExecuted, OnPreviousCanExecute));

        CommandManager.RegisterClassCommandBinding(
            typeof(NaviusPagination),
            new CommandBinding(NextCommand, OnNextExecuted, OnNextCanExecute));

        CommandManager.RegisterClassCommandBinding(
            typeof(NaviusPagination),
            new CommandBinding(GoToPageCommand, OnGoToPageExecuted, OnGoToPageCanExecute));
    }

    public NaviusPagination()
    {
        Rebuild();
    }

    public int TotalPages
    {
        get => (int)GetValue(TotalPagesProperty);
        set => SetValue(TotalPagesProperty, value);
    }

    public int CurrentPage
    {
        get => (int)GetValue(CurrentPageProperty);
        set => SetValue(CurrentPageProperty, value);
    }

    public int SiblingCount
    {
        get => (int)GetValue(SiblingCountProperty);
        set => SetValue(SiblingCountProperty, value);
    }

    public int BoundaryCount
    {
        get => (int)GetValue(BoundaryCountProperty);
        set => SetValue(BoundaryCountProperty, value);
    }

    /// <summary>The computed strip of page/ellipsis tokens the ItemsControl in the template binds to.</summary>
    public IReadOnlyList<PaginationPageToken> Pages => (IReadOnlyList<PaginationPageToken>)GetValue(PagesProperty);

    private void Rebuild()
    {
        var pages = PaginationEngine.BuildPageList(
            Math.Max(1, TotalPages),
            CurrentPage,
            SiblingCount,
            BoundaryCount);

        SetValue(PagesPropertyKey, pages);
    }

    private static void OnLayoutInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusPagination)d).Rebuild();

    private static void OnPreviousCanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = sender is NaviusPagination { CurrentPage: > 1 };
        e.Handled = true;
    }

    private static void OnPreviousExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (sender is NaviusPagination pagination && pagination.CurrentPage > 1)
        {
            pagination.SetCurrentValue(CurrentPageProperty, pagination.CurrentPage - 1);
        }
    }

    private static void OnNextCanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = sender is NaviusPagination pagination && pagination.CurrentPage < Math.Max(1, pagination.TotalPages);
        e.Handled = true;
    }

    private static void OnNextExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (sender is NaviusPagination pagination && pagination.CurrentPage < Math.Max(1, pagination.TotalPages))
        {
            pagination.SetCurrentValue(CurrentPageProperty, pagination.CurrentPage + 1);
        }
    }

    private static void OnGoToPageCanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = sender is NaviusPagination && e.Parameter is int page && page >= 1;
        e.Handled = true;
    }

    private static void OnGoToPageExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (sender is NaviusPagination pagination && e.Parameter is int page)
        {
            pagination.SetCurrentValue(CurrentPageProperty, Math.Clamp(page, 1, Math.Max(1, pagination.TotalPages)));
        }
    }
}
