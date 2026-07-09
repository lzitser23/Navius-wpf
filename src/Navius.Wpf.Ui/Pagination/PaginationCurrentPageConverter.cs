using System;
using System.Globalization;
using System.Windows.Data;

namespace Navius.Wpf.Ui.Pagination;

/// <summary>Bridges a page token's Page number and the pagination control's CurrentPage into a single IsChecked bool for the token's toggle.</summary>
public sealed class PaginationCurrentPageConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is [int tokenPage, int currentPage])
        {
            return tokenPage == currentPage;
        }

        return false;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
