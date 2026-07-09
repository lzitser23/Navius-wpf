using System.IO;
using System.Windows;
using Navius.Wpf.Primitives.Controls.FileUpload;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

/// <summary>Pure FileUploadEngine tests: plain [Fact]s, no WPF statics, no filesystem, no dialogs.</summary>
public class FileUploadEngineTests
{
    private static FileEntry Png(string name = "a.png", long size = 100) =>
        new($"C:\\x\\{name}", name, size, "image/png");

    private static FileEntry Pdf(string name = "d.pdf", long size = 100) =>
        new($"C:\\x\\{name}", name, size, "application/pdf");

    // --- AcceptMatch ---

    [Fact]
    public void AcceptMatch_NullOrBlank_AcceptsEverything()
    {
        Assert.True(FileUploadEngine.AcceptMatch(Png(), null));
        Assert.True(FileUploadEngine.AcceptMatch(Png(), "  "));
    }

    [Fact]
    public void AcceptMatch_ExtensionEntry_MatchesByNameSuffix()
    {
        Assert.True(FileUploadEngine.AcceptMatch(Png(), ".png"));
        Assert.True(FileUploadEngine.AcceptMatch(Png("A.PNG"), ".png")); // case-insensitive.
        Assert.False(FileUploadEngine.AcceptMatch(Pdf(), ".png"));
    }

    [Fact]
    public void AcceptMatch_MimeWildcard_MatchesByTypePrefix()
    {
        Assert.True(FileUploadEngine.AcceptMatch(Png(), "image/*"));
        Assert.False(FileUploadEngine.AcceptMatch(Pdf(), "image/*"));
    }

    [Fact]
    public void AcceptMatch_ExactMime()
    {
        Assert.True(FileUploadEngine.AcceptMatch(Pdf(), "application/pdf"));
        Assert.False(FileUploadEngine.AcceptMatch(Png(), "application/pdf"));
    }

    [Fact]
    public void AcceptMatch_CommaList_AnyEntryWins()
    {
        Assert.True(FileUploadEngine.AcceptMatch(Pdf(), ".png, application/pdf"));
        Assert.True(FileUploadEngine.AcceptMatch(Png(), ".png, application/pdf"));
        Assert.False(FileUploadEngine.AcceptMatch(new FileEntry("x", "x.txt", 1, "text/plain"), ".png, application/pdf"));
    }

    // --- Process: validation + selection semantics ---

    [Fact]
    public void Process_WrongType_IsRejected()
    {
        var result = FileUploadEngine.Process(
            Array.Empty<FileEntry>(), new[] { Pdf() }, "image/*", 0, null, multiple: true);

        Assert.Empty(result.Accepted);
        Assert.Single(result.Rejected);
        Assert.Equal(FileRejectionReason.WrongType, result.Rejected[0].Reason);
        Assert.True(result.Invalid);
        Assert.Empty(result.Next);
    }

    [Fact]
    public void Process_TooLarge_IsRejected_ZeroMeansUnlimited()
    {
        var big = Png(size: 5000);

        var capped = FileUploadEngine.Process(Array.Empty<FileEntry>(), new[] { big }, null, 1000, null, true);
        Assert.Equal(FileRejectionReason.TooLarge, capped.Rejected[0].Reason);

        var unlimited = FileUploadEngine.Process(Array.Empty<FileEntry>(), new[] { big }, null, 0, null, true);
        Assert.Empty(unlimited.Rejected);
        Assert.Single(unlimited.Next);
    }

    [Fact]
    public void Process_TooMany_OnlyEnforcedWhenMultiple()
    {
        var current = new[] { Png("a.png") };

        var multi = FileUploadEngine.Process(current, new[] { Png("b.png") }, null, 0, maxFiles: 1, multiple: true);
        Assert.Equal(FileRejectionReason.TooMany, multi.Rejected[0].Reason);

        // Non-multiple: MaxFiles has no rejection path; the new file replaces the old.
        var single = FileUploadEngine.Process(current, new[] { Png("b.png") }, null, 0, maxFiles: 1, multiple: false);
        Assert.Empty(single.Rejected);
        Assert.Equal("b.png", single.Next[0].Name);
    }

    [Fact]
    public void Process_CheckOrder_AcceptThenSizeThenCount()
    {
        // A wrong-type file that is ALSO too large reports WrongType (accept is checked first).
        var bad = new FileEntry("x", "x.pdf", 99999, "application/pdf");

        var result = FileUploadEngine.Process(Array.Empty<FileEntry>(), new[] { bad }, "image/*", 10, null, true);

        Assert.Equal(FileRejectionReason.WrongType, result.Rejected[0].Reason);
    }

    [Fact]
    public void Process_Duplicate_IsSilentlyDropped_WhenMultiple()
    {
        var current = new[] { Png("a.png", 100) };
        var dup = new FileEntry("elsewhere", "a.png", 100, "image/png"); // same name + size.

        var result = FileUploadEngine.Process(current, new[] { dup }, null, 0, null, multiple: true);

        // Neither accepted nor rejected: it must not inflate the status or fire either event.
        Assert.Empty(result.Accepted);
        Assert.Empty(result.Rejected);
        Assert.Single(result.Next);
    }

    [Fact]
    public void Process_SameNameDifferentSize_IsNotADuplicate()
    {
        var current = new[] { Png("a.png", 100) };

        var result = FileUploadEngine.Process(current, new[] { Png("a.png", 200) }, null, 0, null, true);

        Assert.Single(result.Accepted);
        Assert.Equal(2, result.Next.Count);
    }

    [Fact]
    public void Process_NonMultiple_KeepsTheLastAccepted()
    {
        var result = FileUploadEngine.Process(
            Array.Empty<FileEntry>(), new[] { Png("a.png"), Png("b.png") }, null, 0, null, multiple: false);

        Assert.Equal(2, result.Accepted.Count);
        Assert.Single(result.Next);
        Assert.Equal("b.png", result.Next[0].Name);
    }

    [Fact]
    public void Process_NonMultiple_AllRejectedSelection_KeepsTheExistingFile()
    {
        // The web quirk preserved deliberately: a fully rejected selection does not clear the
        // current single file.
        var current = new[] { Png("keep.png") };

        var result = FileUploadEngine.Process(current, new[] { Pdf() }, "image/*", 0, null, multiple: false);

        Assert.True(result.Invalid);
        Assert.Single(result.Next);
        Assert.Equal("keep.png", result.Next[0].Name);
    }

    [Fact]
    public void Process_MixedSelection_PartitionsAcceptedAndRejected()
    {
        var result = FileUploadEngine.Process(
            Array.Empty<FileEntry>(),
            new[] { Png("a.png"), Pdf("b.pdf"), Png("c.png") },
            "image/*", 0, null, multiple: true);

        Assert.Equal(2, result.Accepted.Count);
        Assert.Single(result.Rejected);
        Assert.Equal(2, result.Next.Count);
    }

    // --- Status + size formatting ---

    [Fact]
    public void BuildStatus_Wordings()
    {
        Assert.Equal("1 file added", FileUploadEngine.BuildStatus(1, 0));
        Assert.Equal("2 files added. 1 file rejected", FileUploadEngine.BuildStatus(2, 1));
        Assert.Equal("3 files rejected", FileUploadEngine.BuildStatus(0, 3));
        Assert.Equal("No files added", FileUploadEngine.BuildStatus(0, 0));
    }

    [Fact]
    public void FormatSize_HumanReadableUnits()
    {
        Assert.Equal("512 B", FileUploadEngine.FormatSize(512));
        Assert.Equal("1.5 KB", FileUploadEngine.FormatSize(1536));
        Assert.Equal("1 MB", FileUploadEngine.FormatSize(1024 * 1024));
    }

    [Fact]
    public void ContentTypeFor_MapsKnownExtensions_EmptyOtherwise()
    {
        Assert.Equal("image/png", FileUploadEngine.ContentTypeFor("photo.PNG"));
        Assert.Equal("application/pdf", FileUploadEngine.ContentTypeFor("doc.pdf"));
        Assert.Equal(string.Empty, FileUploadEngine.ContentTypeFor("mystery.xyz"));
        Assert.Equal(string.Empty, FileUploadEngine.ContentTypeFor("noextension"));
    }
}

/// <summary>
/// Control-level wiring: the injectable picker (no real dialog ever opens), the public file-list
/// state machine, events, status text, and the UIA peer.
/// </summary>
public class FileUploadTests
{
    static FileUploadTests()
    {
        // pack://application URIs only resolve once an Application exists in the process. Guarded
        // try/catch because xunit runs test classes in parallel on separate STA threads: another
        // class's static ctor can win the race to create the process-wide Application.
        if (Application.Current is null)
        {
            try
            {
                _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            }
            catch (InvalidOperationException)
            {
                // Another test class already created the process-wide Application.
            }
        }
    }

    /// <summary>A dialog stub: records the call and returns canned paths. No OS dialog opens.</summary>
    private sealed class StubPicker : IFilePicker
    {
        public string? LastAccept;
        public bool? LastMultiple;
        public IReadOnlyList<string> Paths = Array.Empty<string>();

        public IReadOnlyList<string> PickFiles(string? accept, bool multiple)
        {
            LastAccept = accept;
            LastMultiple = multiple;
            return Paths;
        }
    }

    private static FileEntry Png(string name = "a.png", long size = 100) =>
        new($"C:\\x\\{name}", name, size, "image/png");

    [StaFact]
    public void Defaults_MatchTheContract()
    {
        var upload = new NaviusFileUpload();

        Assert.Null(upload.Files);
        Assert.False(upload.Multiple);
        Assert.Null(upload.MaxFiles);
        Assert.Equal(0L, upload.MaxSize);
        Assert.False(upload.Invalid);
        Assert.False(upload.IsDragging);
        Assert.True(upload.IsFileListEmpty);
        Assert.Equal("Drop files here or press Enter to browse", upload.DropzoneText);
        Assert.IsType<OpenFileDialogPicker>(upload.FilePicker);
    }

    [StaFact]
    public void AddFiles_Accepted_UpdatesListStatusAndEvents()
    {
        var upload = new NaviusFileUpload { Multiple = true };
        IReadOnlyList<FileEntry>? accepted = null;
        IReadOnlyList<FileEntry>? changed = null;
        upload.FilesAccepted += (_, f) => accepted = f;
        upload.FilesChanged += (_, f) => changed = f;

        upload.AddFiles(new[] { Png("a.png"), Png("b.png") });

        Assert.Equal(2, upload.Files!.Count);
        Assert.Equal(2, accepted!.Count);
        Assert.Equal(2, changed!.Count);
        Assert.False(upload.Invalid);
        Assert.False(upload.IsFileListEmpty);
        Assert.Equal("2 files added", upload.StatusText);
    }

    [StaFact]
    public void AddFiles_Rejected_SetsInvalidAndFiresRejectedEvent()
    {
        var upload = new NaviusFileUpload { Multiple = true, Accept = "image/*" };
        IReadOnlyList<NaviusFileRejection>? rejected = null;
        upload.FilesRejected += (_, r) => rejected = r;

        upload.AddFiles(new[] { new FileEntry("x", "x.pdf", 10, "application/pdf") });

        Assert.True(upload.Invalid);
        Assert.Single(rejected!);
        Assert.Equal(FileRejectionReason.WrongType, rejected![0].Reason);
        Assert.Equal("1 file rejected", upload.StatusText);
        Assert.True(upload.IsFileListEmpty);
    }

    [StaFact]
    public void RemoveFile_RemovesByInstance_AndAnnounces()
    {
        var upload = new NaviusFileUpload { Multiple = true };
        upload.AddFiles(new[] { Png("a.png"), Png("b.png") });
        var toRemove = upload.Files![0];

        upload.RemoveFile(toRemove);

        Assert.Single(upload.Files!);
        Assert.Equal("b.png", upload.Files![0].Name);
        Assert.Equal("Removed a.png", upload.StatusText);
        Assert.False(upload.Invalid);
    }

    [StaFact]
    public void ClearFiles_EmptiesTheList_AndAnnounces()
    {
        var upload = new NaviusFileUpload { Multiple = true };
        upload.AddFiles(new[] { Png() });

        upload.ClearFiles();

        Assert.Empty(upload.Files!);
        Assert.True(upload.IsFileListEmpty);
        Assert.Equal("Cleared all files", upload.StatusText);
    }

    [StaFact]
    public void Disabled_BlocksEveryMutation()
    {
        var upload = new NaviusFileUpload { IsEnabled = false };
        var picker = new StubPicker { Paths = new[] { "C:\\x\\a.png" } };
        upload.FilePicker = picker;

        upload.AddFiles(new[] { Png() });
        upload.Browse();
        upload.ClearFiles();

        Assert.Null(upload.Files);
        Assert.Null(picker.LastAccept); // Browse never reached the picker.
    }

    [StaFact]
    public void Browse_UsesTheInjectablePicker_NeverARealDialog()
    {
        var dir = Path.Combine(Path.GetTempPath(), "navius-fileupload-tests");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "picked.txt");
        File.WriteAllText(path, "hello");

        var upload = new NaviusFileUpload { Accept = ".txt", Multiple = true };
        var picker = new StubPicker { Paths = new[] { path } };
        upload.FilePicker = picker;

        upload.Browse();

        Assert.Equal(".txt", picker.LastAccept);
        Assert.True(picker.LastMultiple);
        Assert.Single(upload.Files!);
        Assert.Equal("picked.txt", upload.Files![0].Name);
        Assert.Equal(5, upload.Files![0].Size); // real size probed from disk.
        Assert.Equal("text/plain", upload.Files![0].ContentType);
    }

    [StaFact]
    public void Browse_CancelledPicker_ChangesNothing()
    {
        var upload = new NaviusFileUpload();
        upload.FilePicker = new StubPicker(); // returns no paths.

        upload.Browse();

        Assert.Null(upload.Files);
        Assert.Equal(string.Empty, upload.StatusText);
    }

    [StaFact]
    public void NonMultiple_ReplacesTheSingleFile()
    {
        var upload = new NaviusFileUpload();
        upload.AddFiles(new[] { Png("first.png") });
        upload.AddFiles(new[] { Png("second.png") });

        Assert.Single(upload.Files!);
        Assert.Equal("second.png", upload.Files![0].Name);
    }

    [StaFact]
    public void MaxSizeAndMaxFiles_FlowThroughToTheEngine()
    {
        var upload = new NaviusFileUpload { Multiple = true, MaxFiles = 1, MaxSize = 50 };
        IReadOnlyList<NaviusFileRejection>? rejected = null;
        upload.FilesRejected += (_, r) => rejected = r;

        upload.AddFiles(new[] { Png("big.png", 100) });
        Assert.Equal(FileRejectionReason.TooLarge, rejected![0].Reason);

        upload.AddFiles(new[] { Png("ok.png", 10) });
        upload.AddFiles(new[] { Png("over.png", 10) });
        Assert.Equal(FileRejectionReason.TooMany, rejected![0].Reason);
        Assert.Single(upload.Files!);
    }

    [StaFact]
    public void UiaPeer_SurfacesFileNamesOverReadOnlyValuePattern()
    {
        var upload = new NaviusFileUpload { Multiple = true };
        upload.AddFiles(new[] { Png("a.png"), Png("b.png") });

        var peer = System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(upload);
        var provider = (System.Windows.Automation.Provider.IValueProvider)peer.GetPattern(
            System.Windows.Automation.Peers.PatternInterface.Value)!;

        Assert.True(provider.IsReadOnly);
        Assert.Equal("a.png, b.png", provider.Value);
        Assert.Throws<InvalidOperationException>(() => provider.SetValue("x"));
    }

    [StaFact]
    public void ApplyTemplate_WithThemeLoaded_Succeeds()
    {
        var scope = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, scope);
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/FileUpload.xaml"),
        });

        var upload = new NaviusFileUpload();
        _ = new Window { Resources = scope, Content = upload };

        Assert.True(upload.ApplyTemplate());
    }
}
