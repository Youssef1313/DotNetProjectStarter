using System;
using System.Globalization;
using System.IO;

internal sealed class LibraryTemplateGenerator : ITemplateGenerator
{
    public bool Generate(TemplateGenerationOptions options)
    {
        var projectName = options.Name ?? "LibraryProject";
        var outputDirectory = options.OutputDirectory ?? Directory.GetCurrentDirectory();
        var nugetUsername = options.NuGetUserName;

        outputDirectory = Path.Combine(outputDirectory, projectName);

        if (Directory.Exists(outputDirectory))
        {
            Console.WriteLine($"The directory '{outputDirectory}' already exists. Project will not be generated.");
            return false;
        }

        var githubWorkflowsPath = Path.Combine(outputDirectory, ".github", "workflows");
        Directory.CreateDirectory(githubWorkflowsPath);
        File.WriteAllText(Path.Combine(githubWorkflowsPath, "ci.yml"), LibraryTemplateConstants.LibraryTemplateCIYML);
        File.WriteAllText(Path.Combine(githubWorkflowsPath, "release.yml"), string.Format(CultureInfo.InvariantCulture, LibraryTemplateConstants.ReleaseYML, nugetUsername ?? "NUGET_USERNAME_GOES_HERE"));

        var srcProjectDirectory = Path.Combine(outputDirectory, "src", projectName);
        Directory.CreateDirectory(srcProjectDirectory);
        File.WriteAllText(Path.Combine(srcProjectDirectory, $"{projectName}.csproj"), LibraryTemplateConstants.ProjectFile);

        var testsDirectory = Path.Combine(outputDirectory, "tests");
        var testsProjectDirectory = Path.Combine(testsDirectory, $"{projectName}.Tests");
        Directory.CreateDirectory(testsProjectDirectory);
        File.WriteAllText(Path.Combine(testsDirectory, $"Directory.Build.props"), LibraryTemplateConstants.TestDirectoryBuildProps);
        File.WriteAllText(Path.Combine(testsProjectDirectory, $"{projectName}.Tests.csproj"), LibraryTemplateConstants.TestProjectFile);
        File.WriteAllText(Path.Combine(testsProjectDirectory, "AssemblyInfo.cs"), LibraryTemplateConstants.TestProjectAssemblyInfoFile);

        File.WriteAllText(Path.Combine(outputDirectory, ".editorconfig"), LibraryTemplateConstants.EditorConfigFile);
        File.WriteAllText(Path.Combine(outputDirectory, ".gitattributes"), LibraryTemplateConstants.GitAttributesFile);
        File.WriteAllText(Path.Combine(outputDirectory, ".gitignore"), LibraryTemplateConstants.GitIgnoreFile);
        File.WriteAllText(Path.Combine(outputDirectory, ".globalconfig"), LibraryTemplateConstants.GlobalConfigFile);
        File.WriteAllText(Path.Combine(outputDirectory, "Directory.Build.props"), string.Format(CultureInfo.InvariantCulture, LibraryTemplateConstants.DirectoryBuildPropsFile, options.NuGetUserName ?? "ADD_PACKAGE_AUTHOR_HERE"));
        File.WriteAllText(Path.Combine(outputDirectory, "Directory.Packages.props"), LibraryTemplateConstants.DirectoryPackagesPropsFile);
        File.WriteAllText(Path.Combine(outputDirectory, "LICENSE"), string.Format(CultureInfo.InvariantCulture, LibraryTemplateConstants.LicenseFile, "ADD_COPYRIGHT_HOLDER_HERE"));
        File.WriteAllText(Path.Combine(outputDirectory, "README.md"), string.Format(CultureInfo.InvariantCulture, LibraryTemplateConstants.ReadmeFile, projectName));
        File.WriteAllText(Path.Combine(outputDirectory, $"{projectName}.slnx"), string.Format(CultureInfo.InvariantCulture, LibraryTemplateConstants.SlnxFile, projectName));
        File.WriteAllText(Path.Combine(outputDirectory, "global.json"), LibraryTemplateConstants.GlobalJsonFile);
        File.WriteAllText(Path.Combine(outputDirectory, "nuget.config"), LibraryTemplateConstants.NuGetConfigFile);

        Console.WriteLine("Post-template creation instructions:");
        Console.WriteLine("- Create a key.snk file at repository root using 'sn –k private.snk'.");
        Console.WriteLine("- Update Directory.Build.props with package description.");
        Console.WriteLine("- Ensure Directory.Build.props has the correct package version you want to publish.");
        if (options.NuGetUserName is null)
        {
            Console.WriteLine("- Update Directory.Build.props with package author.");
            Console.WriteLine("- Update .github/workflows/release.yml with NuGet username.");
        }
        Console.WriteLine("- Update LICENSE with license copyright holder.");
        Console.WriteLine("- Login to nuget.org with your account and set up trusted publishing via <https://www.nuget.org/account/trustedpublishing>. Set the Workflow File to 'release.yml'.");
        return true;
    }
}
