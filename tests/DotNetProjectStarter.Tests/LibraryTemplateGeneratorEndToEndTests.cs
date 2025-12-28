using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetProjectStarter.Tests;

[TestClass]
public sealed class LibraryTemplateGeneratorEndToEndTests
{
    private readonly string _outputDirectory;

    public LibraryTemplateGeneratorEndToEndTests()
        => _outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    [TestCleanup]
    public void TestCleanup()
        // TODO: Consider not deleting on failure, and adding to attachments?
        => Directory.Delete(_outputDirectory, recursive: true);

    [TestMethod]
    public async Task CreateProjectShouldSucceed()
    {
        var result = await RunToolWithArgumentsAsync(["--name", "CreateProjectShouldSucceedProjectName", "--output-directory", _outputDirectory]);
        Assert.AreEqual(0, result.ExitCode);

        var buildResult = await RunDotNetBuildWithArgumentsAsync([], Path.Combine(_outputDirectory, "CreateProjectShouldSucceedProjectName"));
        Assert.AreEqual(0, buildResult.ExitCode);
    }

    [TestMethod]
    public async Task CreateProjectShouldSucceedWithTreatWarningsAsErrors()
    {
        var result = await RunToolWithArgumentsAsync(["--name", "CreateProjectShouldSucceedWithTreatWarningsAsErrorsProjectName", "--output-directory", _outputDirectory]);
        Assert.AreEqual(0, result.ExitCode);

        var buildResult = await RunDotNetBuildWithArgumentsAsync(["-p:TreatWarningsAsErrors=true"], Path.Combine(_outputDirectory, "CreateProjectShouldSucceedWithTreatWarningsAsErrorsProjectName"));
        Assert.AreEqual(0, buildResult.ExitCode);
    }

    private static async Task<ToolResult> RunToolWithArgumentsAsync(string[] arguments, string? workingDirectory = null)
        => await RunProcessWithArgumentsAsync(
            Path.Combine(Directory.GetCurrentDirectory(), "DotNetProjectStarter" + (OperatingSystem.IsWindows() ? ".exe" : string.Empty)),
            arguments,
            workingDirectory: workingDirectory ?? Directory.GetCurrentDirectory());

    private static async Task<ToolResult> RunDotNetBuildWithArgumentsAsync(string[] arguments, string? workingDirectory = null)
        => await RunProcessWithArgumentsAsync(
            "dotnet",
            ["build", .. arguments],
            workingDirectory: workingDirectory ?? Directory.GetCurrentDirectory());

    private static async Task<ToolResult> RunProcessWithArgumentsAsync(string filePath, string[] arguments, string workingDirectory)
    {
        var processStartInfo = new ProcessStartInfo(filePath, arguments)
        {
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            WorkingDirectory = workingDirectory
        };

        using var process = Process.Start(processStartInfo);
        Assert.IsNotNull(process);

        var stdoutTask = Task.Factory.StartNew(() => process.StandardOutput.ReadToEnd());
        var stderrTask = Task.Factory.StartNew(() => process.StandardError.ReadToEnd());

        var standardOutputAndError = await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);

        await process.WaitForExitAsync();

        return new ToolResult(process.ExitCode, standardOutputAndError[0], standardOutputAndError[1]);
    }

    private sealed record ToolResult(int ExitCode, string StandardOutput, string StandardError);
}
