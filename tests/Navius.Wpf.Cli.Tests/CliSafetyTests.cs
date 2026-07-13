using System.Diagnostics;
using System.Text.Json;

namespace Navius.Wpf.Cli.Tests;

public sealed class CliSafetyTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "navius-wpf-cli-safety", Guid.NewGuid().ToString("N"));

    [Fact]
    public void Add_rejects_a_target_that_escapes_the_consumer_root()
    {
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(Path.Combine(_tempDir, "source.cs"), "namespace Navius.Wpf.Primitives; public class SafeSource { }");
        var registry = WriteRegistry("source.cs", "../escaped.cs");
        var destination = Path.Combine(_tempDir, "consumer");

        var result = RunCli("add unsafe", $"--to \"{destination}\" --namespace Consumer --root \"{_tempDir}\" --registry \"{registry}\"");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("target path escapes its root", result.StdErr);
        Assert.False(File.Exists(Path.Combine(_tempDir, "escaped.cs")));
    }

    [Fact]
    public void Add_rejects_a_source_that_escapes_the_registry_root()
    {
        Directory.CreateDirectory(_tempDir);
        var outsideSource = Path.Combine(Path.GetDirectoryName(_tempDir)!, "outside-" + Guid.NewGuid().ToString("N") + ".cs");
        File.WriteAllText(outsideSource, "public class Outside { }");
        try
        {
            var registry = WriteRegistry("../" + Path.GetFileName(outsideSource), "safe.cs");

            var result = RunCli("add unsafe", $"--to \"{Path.Combine(_tempDir, "consumer")}\" --namespace Consumer --root \"{_tempDir}\" --registry \"{registry}\"");

            Assert.NotEqual(0, result.ExitCode);
            Assert.Contains("source path escapes its root", result.StdErr);
        }
        finally
        {
            File.Delete(outsideSource);
        }
    }

    [Fact]
    public void Missing_option_value_returns_a_clear_error()
    {
        var result = RunCli("list", "--registry");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("missing value for --registry", result.StdErr);
        Assert.DoesNotContain("Unhandled exception", result.StdErr);
    }

    [Fact]
    public void Add_does_not_write_a_partial_closure_when_a_source_is_missing()
    {
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(Path.Combine(_tempDir, "present.cs"), "public class Present { }");
        var registryPath = Path.Combine(_tempDir, "registry.json");
        var registry = new
        {
            name = "atomic-test",
            items = new[]
            {
                new
                {
                    name = "incomplete",
                    type = "registry:primitive",
                    title = "Incomplete",
                    dependencies = Array.Empty<string>(),
                    registryDependencies = Array.Empty<string>(),
                    files = new[]
                    {
                        new { path = "present.cs", type = "registry:code", target = "present.cs" },
                        new { path = "missing.cs", type = "registry:code", target = "missing.cs" },
                    },
                },
            },
        };
        File.WriteAllText(registryPath, JsonSerializer.Serialize(registry));
        var destination = Path.Combine(_tempDir, "consumer");

        var result = RunCli("add incomplete", $"--to \"{destination}\" --namespace Consumer --root \"{_tempDir}\" --registry \"{registryPath}\"");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("No files were written", result.StdErr);
        Assert.False(Directory.Exists(destination));
    }

    private string WriteRegistry(string source, string target)
    {
        var path = Path.Combine(_tempDir, "registry.json");
        var registry = new
        {
            name = "safety-test",
            items = new[]
            {
                new
                {
                    name = "unsafe",
                    type = "registry:primitive",
                    title = "Unsafe",
                    dependencies = Array.Empty<string>(),
                    registryDependencies = Array.Empty<string>(),
                    files = new[] { new { path = source, type = "registry:code", target } },
                },
            },
        };
        File.WriteAllText(path, JsonSerializer.Serialize(registry));
        return path;
    }

    private static (int ExitCode, string StdOut, string StdErr) RunCli(string command, string options)
    {
        var repoRoot = FindRepoRoot();
        var project = Path.Combine(repoRoot, "tools", "Navius.Wpf.Cli", "Navius.Wpf.Cli.csproj");
        var arguments = $"run --project \"{project}\" -- {command} {options}";
        var psi = new ProcessStartInfo("dotnet", arguments)
        {
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var process = Process.Start(psi)!;
        var stdOut = process.StandardOutput.ReadToEnd();
        var stdErr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, stdOut, stdErr);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Navius.Wpf.sln"))) return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("could not find repo root");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
    }
}
