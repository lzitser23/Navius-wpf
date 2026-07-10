using System.Diagnostics;
using Xunit.Abstractions;

namespace Navius.Wpf.Cli.Tests;

/// <summary>
/// The M5 gate: this is the test that would have caught the defect class found while hardening
/// the sibling web CLI (Navius.Cli, in zits-helm) - registry items vendored without their full
/// dependency closure did not compile, because nothing ever compiled the CLI's output. This test
/// runs the real `navius-wpf add` command against a real temp WPF classlib project and shells
/// `dotnet build` on the result, so a missing file or a broken registryDependency edge shows up
/// as a build failure here instead of in a consumer's project.
///
/// Slow: each case shells `dotnet run` (to invoke the CLI) and `dotnet build` (to compile the
/// vendored output), so this can take the better part of a minute per case. Filter it out of a
/// fast inner loop with `dotnet test --filter "Category!=Vendoring"`.
/// </summary>
[Trait("Category", "Vendoring")]
public sealed class VendoringClosureTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly List<string> _tempDirs = new();

    public VendoringClosureTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    // button: a simple Tier A item with no registryDependencies.
    [InlineData("button")]
    // select: a composite item whose registryDependencies closure (core, anchored-popup) must
    // land alongside it, or the vendored output references types that were never copied.
    [InlineData("select")]
    public void Add_ThenBuild_Succeeds(string itemName)
    {
        var repoRoot = FindRepoRoot();
        var cliProject = Path.Combine(repoRoot, "tools", "Navius.Wpf.Cli", "Navius.Wpf.Cli.csproj");
        Assert.True(File.Exists(cliProject), $"CLI project not found: {cliProject}");

        var tempDir = Path.Combine(Path.GetTempPath(), "navius-wpf-cli-tests", $"{itemName}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        _tempDirs.Add(tempDir);

        // `dotnet new classlib` scaffolds ImplicitUsings=enable by default; the vendored source
        // relies on that (it targets the same modern-SDK defaults).
        var projectFile = Path.Combine(tempDir, "ClosureTest.csproj");
        File.WriteAllText(projectFile, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0-windows</TargetFramework>
                <UseWPF>true</UseWPF>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
              </PropertyGroup>
            </Project>
            """);

        var addArgs = $"run --project \"{cliProject}\" -- add {itemName} --to \"{tempDir}\" --namespace ClosureTest --root \"{repoRoot}\"";
        var addResult = RunProcess("dotnet", addArgs, repoRoot);
        _output.WriteLine($"$ dotnet {addArgs}\n{addResult.StdOut}\n{addResult.StdErr}");
        Assert.True(addResult.ExitCode == 0, $"'navius-wpf add {itemName}' failed (exit {addResult.ExitCode}):\n{addResult.StdOut}\n{addResult.StdErr}");

        var buildArgs = $"build \"{projectFile}\" -c Release";
        var buildResult = RunProcess("dotnet", buildArgs, tempDir);
        _output.WriteLine($"$ dotnet {buildArgs}\n{buildResult.StdOut}\n{buildResult.StdErr}");
        Assert.True(buildResult.ExitCode == 0, $"vendored '{itemName}' did not compile (exit {buildResult.ExitCode}):\n{buildResult.StdOut}\n{buildResult.StdErr}");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Navius.Wpf.sln")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        throw new InvalidOperationException($"could not find Navius.Wpf.sln above {AppContext.BaseDirectory}");
    }

    private static (int ExitCode, string StdOut, string StdErr) RunProcess(string fileName, string arguments, string workingDirectory)
    {
        var psi = new ProcessStartInfo(fileName, arguments)
        {
            WorkingDirectory = workingDirectory,
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

    public void Dispose()
    {
        foreach (var dir in _tempDirs)
        {
            try
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, recursive: true);
                }
            }
            catch
            {
                // best-effort cleanup; a leftover temp dir under the system temp path is harmless
            }
        }
    }
}
