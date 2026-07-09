using System.Windows;
using System.Windows.Controls;
using Navius.Wpf.Gallery.Pages;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Gallery;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Nav.SelectedIndex = 0;
    }

    private void OnNavChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Host is null || Nav.SelectedItem is not ListBoxItem item)
        {
            return;
        }

        Host.Content = (string)item.Content switch
        {
            "Gate" => new GatePage(),
            "Toggle" => new TogglePage(),
            "Checkbox" => new CheckboxPage(),
            "RadioGroup" => new RadioGroupPage(),
            "Label" => new LabelPage(),
            "Slider" => new SliderPage(),
            "Progress" => new ProgressPage(),
            "Separator" => new SeparatorPage(),
            "Positioning" => new PositioningPage(),
            "Overlay" => new OverlayPage(),
            "Tooltip" => new TooltipPage(),
            "Popover" => new PopoverPage(),
            "PreviewCard" => new PreviewCardPage(),
            "Dialog" => new DialogPage(),
            "AlertDialog" => new AlertDialogPage(),
            "Drawer" => new DrawerPage(),
            "Menu" => new MenuPage(),
            "ContextMenu" => new ContextMenuPage(),
            "Menubar" => new MenubarPage(),
            "NavigationMenu" => new NavigationMenuPage(),
            "Toast" => new ToastPage(),
            "Select" => new SelectPage(),
            "Combobox" => new ComboboxPage(),
            "Autocomplete" => new AutocompletePage(),
            "Tabs" => new TabsPage(),
            "Accordion" => new AccordionPage(),
            "Collapsible" => new CollapsiblePage(),
            "ToggleGroup" => new ToggleGroupPage(),
            "Switch" => new SwitchPage(),
            "NumberField" => new NumberFieldPage(),
            "Rating" => new RatingPage(),
            "Meter" => new MeterPage(),
            "ScrollArea" => new ScrollAreaPage(),
            "OneTimePasswordField" => new OneTimePasswordFieldPage(),
            "PasswordToggleField" => new PasswordToggleFieldPage(),
            "Field" => new FieldPage(),
            "Form" => new FormPage(),
            "Calendar" => new CalendarPage(),
            "DatePicker" => new DatePickerPage(),
            "DateRangePicker" => new DateRangePickerPage(),
            "DateInput" => new DateInputPage(),
            "TimeInput" => new TimeInputPage(),
            "TimePicker" => new TimePickerPage(),
            "MaskedInput" => new MaskedInputPage(),
            "CurrencyInput" => new CurrencyInputPage(),
            "TagInput" => new TagInputPage(),
            "FileUpload" => new FileUploadPage(),
            "Tree" => new TreePage(),
            "Sortable" => new SortablePage(),
            "DataGrid" => new DataGridPage(),
            "MessageScroller" => new MessageScrollerPage(),
            _ => null,
        };
    }

    private void OnToggleTheme(object sender, RoutedEventArgs e)
    {
        var next = ThemeManager.Current == NaviusTheme.Light ? NaviusTheme.Dark : NaviusTheme.Light;
        ThemeManager.Apply(next);
        ThemeLabel.Text = next.ToString();
    }
}
