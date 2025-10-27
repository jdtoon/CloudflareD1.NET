using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CloudflareD1.NET;
using CloudflareD1.NET.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CloudflareD1.NET.Cli.E2E.Tests;

public class CliE2ETests
{
    private static string RepoRoot => FindRepoRoot();

    [Fact]
    public async Task AddMigration_CreatesFile_InTempWorkspace()
    {
        using var tmp = new TempWorkspace();

        var (exit, stdout, stderr) = await RunCliAsync(tmp.Path, "migrations", "add", "CreateWidgets");
        Assert.Equal(0, exit);
        Assert.Contains("Created migration", stdout);

        var migrationsDir = Path.Combine(tmp.Path, "Migrations");
        Assert.True(Directory.Exists(migrationsDir));

        var files = Directory.GetFiles(migrationsDir, "*_CreateWidgets.cs");
        Assert.Single(files);

        var content = await File.ReadAllTextAsync(files[0]);
        Assert.Contains("class Migration", content);
        Assert.Contains("public override void Up", content);
    }

    [Fact]
    public async Task Scaffold_Creates_Migration_And_Snapshot_FromExistingDb()
    {
        using var tmp = new TempWorkspace();

        // Arrange: create a local SQLite DB with a table
        var dbPath = Path.Combine(tmp.Path, "e2e.db");
        var options = Options.Create(new D1Options { UseLocalMode = true, LocalDatabasePath = dbPath });
        var client = new D1Client(options, NullLogger<D1Client>.Instance);
        await client.ExecuteAsync(@"CREATE TABLE widgets (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL,
            created_at TEXT
        );");

        // Act: run scaffold
        var (exit, stdout, stderr) = await RunCliAsync(tmp.Path, "migrations", "scaffold", "InitialWidgets", "--connection", dbPath);
        Assert.Equal(0, exit);
        Assert.Contains("Scaffolding migration", stdout);
        Assert.Contains("Created migration:", stdout);
        Assert.Contains("Schema snapshot saved", stdout);

        // Assert: file created and contains create table
        var migrationsDir = Path.Combine(tmp.Path, "Migrations");
        var files = Directory.GetFiles(migrationsDir, "*_InitialWidgets.cs");
        Assert.Single(files);
        var content = await File.ReadAllTextAsync(files[0]);
        Assert.Matches(new Regex("CreateTable\\(\\\"widgets\\\"", RegexOptions.IgnoreCase), content);

        // Snapshot exists
        var snapshot = Path.Combine(tmp.Path, ".migrations-snapshot.json");
        Assert.True(File.Exists(snapshot));
    }

    [Fact]
    public async Task ModelDiff_Creates_Migration_From_Context_Assembly()
    {
        using var tmp = new TempWorkspace();

        var repo = RepoRoot;
        // Use the repo's ModelDiffSample class library to supply a context assembly
        var sampleProj = Path.Combine(repo, "examples","ModelDiffSample","ModelDiffSample.csproj");
        var (exitBuild, outBuild, errBuild) = await RunWithOutput("dotnet", new[]{"build", sampleProj, "-c","Debug"}, repo);
        Assert.Equal(0, exitBuild);
        var dll = Path.Combine(repo, "examples","ModelDiffSample","bin","Debug","net8.0","ModelDiffSample.dll");

        // Run CLI migrations diff from a clean temp workspace
        var (exit, stdout, stderr) = await RunCliAsync(tmp.Path, "migrations","diff","InitialFromModel","--context","ModelDiffSample.AppDbContext","--assembly", dll);
        if (exit != 0)
        {
            throw new Exception($"CLI failed with exit {exit}\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
        }
        if (!stdout.Contains("Model diff migration generated"))
        {
            throw new Exception($"Unexpected CLI output.\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
        }

        // Assert: migration file exists and has CreateTable for users
        var migrationsDir = Path.Combine(tmp.Path, "Migrations");
        var files = Directory.GetFiles(migrationsDir, "*_InitialFromModel.cs");
        Assert.Single(files);
        var content = await File.ReadAllTextAsync(files[0]);
        Assert.Contains("CreateTable(\"users\"", content);

        // Snapshot saved
        Assert.True(File.Exists(Path.Combine(tmp.Path, ".migrations-snapshot.json")));
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

    private static async Task Run(string file, string[] args, string workingDir)
    {
        var (exit, _, err) = await RunWithOutput(file, args, workingDir);
        if (exit != 0) throw new Exception($"Command failed: {file} {string.Join(' ', args)}\n{err}");
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

    private static string FindRepoRoot()
    {
        // Start from current test base directory and walk up to find the 'tools/dotnet-d1' folder
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

        // Fallback to environment or known path during local dev
        var env = Environment.GetEnvironmentVariable("CFD1_REPO_ROOT");
        if (!string.IsNullOrEmpty(env) && Directory.Exists(Path.Combine(env, "tools", "dotnet-d1")))
        {
            return env;
        }

        throw new InvalidOperationException("Could not locate repository root containing tools/dotnet-d1");
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
            try { Directory.Delete(Path, recursive: true); } catch { /* ignore on CI */ }
        }
    }
}
