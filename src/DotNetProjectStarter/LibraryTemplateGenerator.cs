using System;
using System.Globalization;
using System.IO;

internal sealed class LibraryTemplateGenerator : ITemplateGenerator
{
    public bool Generate(TemplateGenerationOptions options)
    {
        var projectName = options.Name ?? "LibraryProject";
        var outputDirectory = options.OutputDirectory ?? Directory.GetCurrentDirectory();
        var packageName = options.NuGetPackageName;
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
        File.WriteAllText(Path.Combine(githubWorkflowsPath, "release.yml"), string.Format(CultureInfo.InvariantCulture, LibraryTemplateConstants.ReleaseYML, packageName ?? "PACKAGE_NAME_GOES_HERE", nugetUsername ?? "NUGET_USERNAME_GOES_HERE"));

        var srcProjectDirectory = Path.Combine(outputDirectory, "src", projectName);
        Directory.CreateDirectory(srcProjectDirectory);
        File.WriteAllText(Path.Combine(srcProjectDirectory, $"{projectName}.csproj"), LibraryTemplateConstants.ProjectFile);

        var testsDirectory = Path.Combine(outputDirectory, "tests");
        var testsProjectDirectory = Path.Combine(testsDirectory, $"{projectName}.Tests");
        Directory.CreateDirectory(testsProjectDirectory);
        File.WriteAllText(Path.Combine(testsDirectory, $"Directory.Build.props"), LibraryTemplateConstants.TestDirectoryBuildProps);
        File.WriteAllText(Path.Combine(testsProjectDirectory, $"{projectName}.Tests.csproj"), LibraryTemplateConstants.TestProjectFile);

        File.WriteAllText(Path.Combine(outputDirectory, ".editorconfig"), LibraryTemplateConstants.EditorConfigFile);
        File.WriteAllText(Path.Combine(outputDirectory, ".gitattributes"), LibraryTemplateConstants.GitAttributesFile);
        File.WriteAllText(Path.Combine(outputDirectory, ".gitignore"), LibraryTemplateConstants.GitIgnoreFile);
        File.WriteAllText(Path.Combine(outputDirectory, ".globalconfig"), LibraryTemplateConstants.GlobalConfigFile);
        File.WriteAllText(Path.Combine(outputDirectory, "Directory.Build.props"), string.Format(CultureInfo.InvariantCulture, LibraryTemplateConstants.DirectoryBuildPropsFile, options.NuGetUserName ?? "ADD_PACKAGE_AUTHOR_HERE"));
        File.WriteAllText(Path.Combine(outputDirectory, "Directory.Packages.props"), LibraryTemplateConstants.DirectoryPackagesPropsFile);
        File.WriteAllText(Path.Combine(outputDirectory, "LICENSE"), string.Format(CultureInfo.InvariantCulture, LibraryTemplateConstants.LicenseFile, "ADD_COPYRIGHT_HOLDER_HERE"));
        File.WriteAllText(Path.Combine(outputDirectory, "README.md"), string.Format(CultureInfo.InvariantCulture, LibraryTemplateConstants.ReadmeFile, packageName ?? projectName));
        File.WriteAllText(Path.Combine(outputDirectory, $"{projectName}.slnx"), string.Format(CultureInfo.InvariantCulture, LibraryTemplateConstants.SlnxFile, projectName));
        File.WriteAllText(Path.Combine(outputDirectory, "global.json"), LibraryTemplateConstants.GlobalJsonFile);
        File.WriteAllText(Path.Combine(outputDirectory, "nuget.config"), LibraryTemplateConstants.NuGetConfigFile);

        return true;
    }
}
