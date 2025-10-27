using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace CloudflareD1.NET.Cli.E2E.Tests;

public class OnModelCreatingE2ETests
{
    private static string RepoRoot => FindRepoRoot();

    [Fact]
    public async Task ModelDiff_Honors_OnModelCreating_Relationships()
    {
        using var tmp = new TempWorkspace();

        // Build the RelationshipsSample project
        var proj = Path.Combine(RepoRoot, "examples", "RelationshipsSample", "RelationshipsSample.csproj");
        var (exitBuild, outBuild, errBuild) = await RunWithOutput("dotnet", new[]{"build", proj, "-c","Debug"}, RepoRoot);
        Assert.Equal(0, exitBuild);
        var dll = Path.Combine(RepoRoot, "examples","RelationshipsSample","bin","Debug","net8.0","RelationshipsSample.dll");
        Assert.True(File.Exists(dll));

        // Run CLI migrations diff from a clean temp workspace, specifying context and assembly
        var (exit, stdout, stderr) = await RunCliAsync(tmp.Path, "migrations","diff","InitRelFromModel","--context","RelationshipsSample.AppDbContext","--assembly", dll);
        if (exit != 0)
        {
            throw new Exception($"CLI failed with exit {exit}\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
        }
        Assert.Contains("Model diff migration generated", stdout);

        // Assert: migration file exists and includes FK with CASCADE
        var migrationsDir = Path.Combine(tmp.Path, "Migrations");
        var files = Directory.GetFiles(migrationsDir, "*_InitRelFromModel.cs");
        Assert.Single(files);
        var content = await File.ReadAllTextAsync(files[0]);
        Assert.Contains("CreateTable(\"users\"", content);
        Assert.Contains("CreateTable(\"posts\"", content);
        Assert.Contains("t.ForeignKey(\"user_id\", \"users\", \"id\", \"CASCADE\")", content);

        // Snapshot saved
        Assert.True(File.Exists(Path.Combine(tmp.Path, ".migrations-snapshot.json")));
    }

    private sealed class TempWorkspace : IDisposable
    {
        public string Path { get; }
        public TempWorkspace()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"d1_e2e_{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }
        public void Dispose()
        {
            try { Directory.Delete(Path, recursive: true); } catch { /* ignore */ }
        }
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 8; i++)
        {
            var candidate = Path.Combine(dir, "tools", "dotnet-d1");
            if (Directory.Exists(candidate))
            {
                return dir;
            }
            var parent = Directory.GetParent(dir)?.FullName;
            if (parent == null) break;
            dir = parent;
        }
        var env = Environment.GetEnvironmentVariable("CFD1_REPO_ROOT");
        if (!string.IsNullOrEmpty(env) && Directory.Exists(Path.Combine(env, "tools", "dotnet-d1")))
        {
            return env;
        }
        throw new InvalidOperationException("Could not locate repository root containing tools/dotnet-d1");
    }

    private static async Task<(int exitCode, string stdout, string stderr)> RunCliAsync(string workingDir, params string[] args)
    {
        var project = Path.Combine(RepoRoot, "tools", "dotnet-d1", "dotnet-d1.csproj");
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            ArgumentList = { "run", "--project", project, "--" },
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var proc = Process.Start(psi)!;
        var stdout = await proc.StandardOutput.ReadToEndAsync();
        var stderr = await proc.StandardError.ReadToEndAsync();
        proc.WaitForExit();
        return (proc.ExitCode, stdout, stderr);
    }

    private static async Task<(int exitCode, string stdout, string stderr)> RunWithOutput(string file, string[] args, string workingDir)
    {
        var psi = new ProcessStartInfo
        {
            FileName = file,
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi)!;
        var o = await p.StandardOutput.ReadToEndAsync();
        var e = await p.StandardError.ReadToEndAsync();
        p.WaitForExit();
        return (p.ExitCode, o, e);
    }
}

