using System.Windows.Controls;
using Navius.Wpf.Gallery.Pages;

namespace Navius.Wpf.Captures;

/// <summary>
/// The single nav-label to page-instance mapping used by the capture run. Kept as one table,
/// ported from (not shared with) apps/Navius.Wpf.Gallery/MainWindow.xaml.cs's OnNavChanged
/// switch expression -- update both places if a page is added to the Gallery.
/// </summary>
internal static class PageCatalog
{
    public static readonly IReadOnlyList<(string Label, Func<UserControl> Factory)> Pages = new (string, Func<UserControl>)[]
    {
        ("Gate", () => new GatePage()),
        ("Toggle", () => new TogglePage()),
        ("Checkbox", () => new CheckboxPage()),
        ("RadioGroup", () => new RadioGroupPage()),
        ("Label", () => new LabelPage()),
        ("Slider", () => new SliderPage()),
        ("Progress", () => new ProgressPage()),
        ("Separator", () => new SeparatorPage()),
        ("Positioning", () => new PositioningPage()),
        ("Overlay", () => new OverlayPage()),
        ("Tooltip", () => new TooltipPage()),
        ("Popover", () => new PopoverPage()),
        ("PreviewCard", () => new PreviewCardPage()),
        ("Dialog", () => new DialogPage()),
        ("AlertDialog", () => new AlertDialogPage()),
        ("Drawer", () => new DrawerPage()),
        ("Menu", () => new MenuPage()),
        ("ContextMenu", () => new ContextMenuPage()),
        ("Menubar", () => new MenubarPage()),
        ("NavigationMenu", () => new NavigationMenuPage()),
        ("Toast", () => new ToastPage()),
        ("Select", () => new SelectPage()),
        ("Combobox", () => new ComboboxPage()),
        ("Autocomplete", () => new AutocompletePage()),
        ("Tabs", () => new TabsPage()),
        ("Accordion", () => new AccordionPage()),
        ("Collapsible", () => new CollapsiblePage()),
        ("ToggleGroup", () => new ToggleGroupPage()),
        ("Switch", () => new SwitchPage()),
        ("NumberField", () => new NumberFieldPage()),
        ("Rating", () => new RatingPage()),
        ("Meter", () => new MeterPage()),
        ("ScrollArea", () => new ScrollAreaPage()),
        ("OneTimePasswordField", () => new OneTimePasswordFieldPage()),
        ("PasswordToggleField", () => new PasswordToggleFieldPage()),
        ("Field", () => new FieldPage()),
        ("Form", () => new FormPage()),
        ("Calendar", () => new CalendarPage()),
        ("DatePicker", () => new DatePickerPage()),
        ("DateRangePicker", () => new DateRangePickerPage()),
        ("DateInput", () => new DateInputPage()),
        ("TimeInput", () => new TimeInputPage()),
        ("TimePicker", () => new TimePickerPage()),
        ("MaskedInput", () => new MaskedInputPage()),
        ("CurrencyInput", () => new CurrencyInputPage()),
        ("TagInput", () => new TagInputPage()),
        ("FileUpload", () => new FileUploadPage()),
        ("Tree", () => new TreePage()),
        ("Sortable", () => new SortablePage()),
        ("DataGrid", () => new DataGridPage()),
        ("MessageScroller", () => new MessageScrollerPage()),
        ("Avatar", () => new AvatarPage()),
        ("AspectRatio", () => new AspectRatioPage()),
        ("ColorPicker", () => new ColorPickerPage()),
        ("Charts", () => new ChartsPage()),
        ("UiDisplay", () => new UiDisplayPage()),
        ("UiComposite", () => new UiCompositePage()),
        ("Toolbar", () => new ToolbarPage()),
        ("Rtl", () => new RtlPage()),
        ("HighContrast", () => new HighContrastPage()),
    };
}
