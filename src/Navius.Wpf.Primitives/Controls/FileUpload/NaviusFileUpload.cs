using System.IO;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Navius.Wpf.Primitives.Controls.FileUpload;

/// <summary>
/// Tier B (custom lookless control). The web's 10 parts fold into one Control with four named
/// template parts: PART_Dropzone (a real ButtonBase, so the contract's role="button" dropzone gets
/// focus, Enter/Space activation, and an Invoke pattern for free), PART_Browse (the Trigger
/// button), PART_List (ItemsControl over <see cref="FileEntry"/> rows), and PART_Clear. The
/// Item/ItemName/ItemSize/ItemDelete parts are a DataTemplate over the FileEntry record
/// (the NumberField/TagInput folding minimalism).
///
/// The web's "hidden native input type=file is the a11y + form source of truth" architecture is
/// DOM-specific and deliberately NOT ported: the OS dialog is an injectable <see cref="IFilePicker"/>
/// (defaulting to Microsoft.Win32.OpenFileDialog) so unit tests never open a real dialog, and the
/// JS drag/drop relay is replaced with native WPF AllowDrop/DragOver/Drop over
/// <see cref="DataFormats.FileDrop"/>. Validation (accept/size/count, replace and duplicate
/// semantics) is the pure <see cref="FileUploadEngine"/>, run identically for dialog and drop
/// selections so rejection reporting works on both paths.
/// </summary>
[TemplatePart(Name = PartDropzone, Type = typeof(ButtonBase))]
[TemplatePart(Name = PartBrowse, Type = typeof(ButtonBase))]
[TemplatePart(Name = PartList, Type = typeof(ItemsControl))]
[TemplatePart(Name = PartClear, Type = typeof(ButtonBase))]
public class NaviusFileUpload : Control
{
    private const string PartDropzone = "PART_Dropzone";
    private const string PartBrowse = "PART_Browse";
    private const string PartList = "PART_List";
    private const string PartClear = "PART_Clear";

    /// <summary>Removes one file; the row's <see cref="FileEntry"/> rides as the command parameter.</summary>
    public static readonly RoutedCommand RemoveFileCommand = new(nameof(RemoveFileCommand), typeof(NaviusFileUpload));

    public static readonly DependencyProperty FilesProperty = DependencyProperty.Register(
        nameof(Files), typeof(IReadOnlyList<FileEntry>), typeof(NaviusFileUpload),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnFilesChanged));

    public static readonly DependencyProperty AcceptProperty = DependencyProperty.Register(
        nameof(Accept), typeof(string), typeof(NaviusFileUpload), new PropertyMetadata(null));

    public static readonly DependencyProperty MultipleProperty = DependencyProperty.Register(
        nameof(Multiple), typeof(bool), typeof(NaviusFileUpload), new PropertyMetadata(false));

    public static readonly DependencyProperty MaxFilesProperty = DependencyProperty.Register(
        nameof(MaxFiles), typeof(int?), typeof(NaviusFileUpload), new PropertyMetadata(null));

    public static readonly DependencyProperty MaxSizeProperty = DependencyProperty.Register(
        nameof(MaxSize), typeof(long), typeof(NaviusFileUpload), new PropertyMetadata(0L));

    public static readonly DependencyProperty DropzoneTextProperty = DependencyProperty.Register(
        nameof(DropzoneText), typeof(string), typeof(NaviusFileUpload),
        new PropertyMetadata("Drop files here or press Enter to browse"));

    private static readonly DependencyPropertyKey InvalidPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(Invalid), typeof(bool), typeof(NaviusFileUpload), new PropertyMetadata(false));

    public static readonly DependencyProperty InvalidProperty = InvalidPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey IsDraggingPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsDragging), typeof(bool), typeof(NaviusFileUpload), new PropertyMetadata(false));

    public static readonly DependencyProperty IsDraggingProperty = IsDraggingPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey StatusTextPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(StatusText), typeof(string), typeof(NaviusFileUpload), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty StatusTextProperty = StatusTextPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey IsFileListEmptyPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsFileListEmpty), typeof(bool), typeof(NaviusFileUpload), new PropertyMetadata(true));

    public static readonly DependencyProperty IsFileListEmptyProperty = IsFileListEmptyPropertyKey.DependencyProperty;

    private ButtonBase? _dropzone;
    private ButtonBase? _browse;
    private ButtonBase? _clear;
    private bool _syncingFiles;

    static NaviusFileUpload()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusFileUpload), new FrameworkPropertyMetadata(typeof(NaviusFileUpload)));
    }

    public NaviusFileUpload()
    {
        CommandBindings.Add(new CommandBinding(RemoveFileCommand, OnRemoveFileExecuted));
    }

    /// <summary>Raised on every committed mutation (add/remove/clear) with the new list.</summary>
    public event EventHandler<IReadOnlyList<FileEntry>>? FilesChanged;

    /// <summary>Raised when a selection produces at least one accepted file (duplicates excluded).</summary>
    public event EventHandler<IReadOnlyList<FileEntry>>? FilesAccepted;

    /// <summary>Raised when a selection produces at least one rejection, each paired to its reason.</summary>
    public event EventHandler<IReadOnlyList<NaviusFileRejection>>? FilesRejected;

    /// <summary>
    /// The OS dialog seam. Defaults to <see cref="OpenFileDialogPicker"/>; tests inject a stub so
    /// no real dialog ever opens headless.
    /// </summary>
    public IFilePicker FilePicker { get; set; } = new OpenFileDialogPicker();

    /// <summary>The selected files (the contract's controlled Files).</summary>
    public IReadOnlyList<FileEntry>? Files
    {
        get => (IReadOnlyList<FileEntry>?)GetValue(FilesProperty);
        set => SetValue(FilesProperty, value);
    }

    /// <summary>Comma-separated MIME types / extensions (the native accept syntax).</summary>
    public string? Accept
    {
        get => (string?)GetValue(AcceptProperty);
        set => SetValue(AcceptProperty, value);
    }

    public bool Multiple
    {
        get => (bool)GetValue(MultipleProperty);
        set => SetValue(MultipleProperty, value);
    }

    /// <summary>Cap on the total number of files (null = unlimited); only enforced when <see cref="Multiple"/>.</summary>
    public int? MaxFiles
    {
        get => (int?)GetValue(MaxFilesProperty);
        set => SetValue(MaxFilesProperty, value);
    }

    /// <summary>Per-file byte ceiling (0 = unlimited).</summary>
    public long MaxSize
    {
        get => (long)GetValue(MaxSizeProperty);
        set => SetValue(MaxSizeProperty, value);
    }

    /// <summary>The dropzone's visible text and accessible name (the contract's AriaLabel default).</summary>
    public string DropzoneText
    {
        get => (string)GetValue(DropzoneTextProperty);
        set => SetValue(DropzoneTextProperty, value);
    }

    /// <summary>True after the most recent selection produced at least one rejection (data-invalid).</summary>
    public bool Invalid => (bool)GetValue(InvalidProperty);

    /// <summary>True while a file drag hovers the dropzone (data-dragging).</summary>
    public bool IsDragging => (bool)GetValue(IsDraggingProperty);

    /// <summary>The polite status line (added/rejected/removed counts), mirrored to a live region.</summary>
    public string StatusText => (string)GetValue(StatusTextProperty);

    /// <summary>True when no files are selected (gates the Clear button, the web's data-disabled on Clear).</summary>
    public bool IsFileListEmpty => (bool)GetValue(IsFileListEmptyProperty);

    private IReadOnlyList<FileEntry> CurrentFiles => Files ?? Array.Empty<FileEntry>();

    public override void OnApplyTemplate()
    {
        if (_dropzone is not null)
        {
            _dropzone.Click -= OnDropzoneClick;
            _dropzone.DragEnter -= OnDropzoneDragEnter;
            _dropzone.DragOver -= OnDropzoneDragOver;
            _dropzone.DragLeave -= OnDropzoneDragLeave;
            _dropzone.Drop -= OnDropzoneDrop;
        }

        if (_browse is not null)
        {
            _browse.Click -= OnBrowseClick;
        }

        if (_clear is not null)
        {
            _clear.Click -= OnClearClick;
        }

        base.OnApplyTemplate();

        _dropzone = GetTemplateChild(PartDropzone) as ButtonBase;
        _browse = GetTemplateChild(PartBrowse) as ButtonBase;
        _clear = GetTemplateChild(PartClear) as ButtonBase;

        if (_dropzone is not null)
        {
            _dropzone.AllowDrop = true;
            _dropzone.Click += OnDropzoneClick;
            _dropzone.DragEnter += OnDropzoneDragEnter;
            _dropzone.DragOver += OnDropzoneDragOver;
            _dropzone.DragLeave += OnDropzoneDragLeave;
            _dropzone.Drop += OnDropzoneDrop;
        }

        if (_browse is not null)
        {
            _browse.Click += OnBrowseClick;
        }

        if (_clear is not null)
        {
            _clear.Click += OnClearClick;
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusFileUploadAutomationPeer(this);

    // ---- Public state machine (directly unit-testable without a template or dialog) ----

    /// <summary>Opens the injectable file picker and runs any chosen paths through validation.</summary>
    public void Browse()
    {
        if (!IsEnabled)
        {
            return;
        }

        var paths = FilePicker.PickFiles(Accept, Multiple);
        if (paths.Count > 0)
        {
            AddPaths(paths);
        }
    }

    /// <summary>Builds entries from filesystem paths and validates them (the drop + dialog funnel).</summary>
    public void AddPaths(IEnumerable<string> paths) =>
        AddFiles(paths.Select(FileEntry.FromPath).ToList());

    /// <summary>Validates <paramref name="incoming"/> against Accept/MaxSize/MaxFiles and commits the result.</summary>
    public void AddFiles(IReadOnlyList<FileEntry> incoming)
    {
        if (!IsEnabled)
        {
            return;
        }

        var result = FileUploadEngine.Process(CurrentFiles, incoming, Accept, MaxSize, MaxFiles, Multiple);

        SetValue(InvalidPropertyKey, result.Invalid);
        SetValue(StatusTextPropertyKey, FileUploadEngine.BuildStatus(result.Accepted.Count, result.Rejected.Count));
        SetFilesInternal(result.Next);

        if (result.Accepted.Count > 0)
        {
            FilesAccepted?.Invoke(this, result.Accepted);
        }

        if (result.Rejected.Count > 0)
        {
            FilesRejected?.Invoke(this, result.Rejected);
        }
    }

    /// <summary>Removes one file by instance identity (the per-row delete button path).</summary>
    public void RemoveFile(FileEntry file)
    {
        if (!IsEnabled)
        {
            return;
        }

        var next = CurrentFiles.Where(f => !ReferenceEquals(f, file)).ToList();
        if (next.Count == CurrentFiles.Count)
        {
            return;
        }

        SetValue(InvalidPropertyKey, false);
        SetValue(StatusTextPropertyKey, $"Removed {file.Name}");
        SetFilesInternal(next);
    }

    /// <summary>Clears all selected files.</summary>
    public void ClearFiles()
    {
        if (!IsEnabled)
        {
            return;
        }

        SetValue(InvalidPropertyKey, false);
        SetValue(StatusTextPropertyKey, "Cleared all files");
        SetFilesInternal(Array.Empty<FileEntry>());
    }

    // ---- Dropzone drag/drop (native WPF, replacing the web's JS relay) ----

    private void OnDropzoneDragEnter(object sender, DragEventArgs e) => OnDropzoneDragOver(sender, e);

    private void OnDropzoneDragOver(object sender, DragEventArgs e)
    {
        e.Effects = DragDropEffects.None;
        e.Handled = true;

        if (!IsEnabled || !e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            SetValue(IsDraggingPropertyKey, false);
            return;
        }

        // Accept-filter validation during hover: the copy cursor only shows when at least one
        // dragged file would pass the accept filter (name-based; sizes are checked on drop).
        var paths = (string[]?)e.Data.GetData(DataFormats.FileDrop) ?? Array.Empty<string>();
        var anyAcceptable = paths.Any(p =>
        {
            var name = Path.GetFileName(p);
            return FileUploadEngine.AcceptMatch(
                new FileEntry(p, name, 0, FileUploadEngine.ContentTypeFor(name)), Accept);
        });

        if (anyAcceptable)
        {
            e.Effects = DragDropEffects.Copy;
        }

        SetValue(IsDraggingPropertyKey, true);
    }

    private void OnDropzoneDragLeave(object sender, DragEventArgs e) =>
        SetValue(IsDraggingPropertyKey, false);

    private void OnDropzoneDrop(object sender, DragEventArgs e)
    {
        SetValue(IsDraggingPropertyKey, false);

        if (!IsEnabled || !e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return;
        }

        e.Handled = true;
        var paths = (string[]?)e.Data.GetData(DataFormats.FileDrop) ?? Array.Empty<string>();
        if (paths.Length > 0)
        {
            AddPaths(paths);
        }
    }

    // ---- Buttons ----

    private void OnDropzoneClick(object sender, RoutedEventArgs e) => Browse();

    private void OnBrowseClick(object sender, RoutedEventArgs e) => Browse();

    private void OnClearClick(object sender, RoutedEventArgs e) => ClearFiles();

    private void OnRemoveFileExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Parameter is FileEntry file)
        {
            RemoveFile(file);
        }
    }

    // ---- Internals ----

    private void SetFilesInternal(IReadOnlyList<FileEntry> next)
    {
        _syncingFiles = true;
        try
        {
            Files = next;
        }
        finally
        {
            _syncingFiles = false;
        }

        SetValue(IsFileListEmptyPropertyKey, next.Count == 0);
        FilesChanged?.Invoke(this, next);
    }

    private static void OnFilesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (NaviusFileUpload)d;
        if (control._syncingFiles)
        {
            return;
        }

        // External (binding-driven) change: refresh the empty flag only; status/invalid untouched.
        control.SetValue(
            IsFileListEmptyPropertyKey,
            ((IReadOnlyList<FileEntry>?)e.NewValue)?.Count is not > 0);
    }
}

/// <summary>
/// Root peer: ControlType.Group with a read-only ValuePattern surfacing the selected file names
/// (the Select-peer / M3-gate precedent: state living in template text must be readable over UIA).
/// The dropzone/browse/clear/remove buttons are real ButtonBase parts, so they carry their own
/// native Invoke peers; the status TextBlock carries LiveSetting=Polite in the theme.
/// </summary>
internal sealed class NaviusFileUploadAutomationPeer : FrameworkElementAutomationPeer,
    System.Windows.Automation.Provider.IValueProvider
{
    public NaviusFileUploadAutomationPeer(NaviusFileUpload owner) : base(owner)
    {
    }

    private NaviusFileUpload Root => (NaviusFileUpload)Owner;

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    protected override string GetClassNameCore() => nameof(NaviusFileUpload);

    public override object? GetPattern(PatternInterface patternInterface) =>
        patternInterface == PatternInterface.Value ? this : base.GetPattern(patternInterface);

    public bool IsReadOnly => true;

    public string Value => string.Join(", ", (Root.Files ?? Array.Empty<FileEntry>()).Select(f => f.Name));

    public void SetValue(string value) =>
        throw new InvalidOperationException("NaviusFileUpload is read-only over ValuePattern; select files via the dialog or drop.");
}
