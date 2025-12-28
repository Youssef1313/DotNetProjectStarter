using System;

string? name = null;
string? outputDirectory = null;
string? nugetPackageName = null;
string? nugetUsername = null;

// TODO: Use System.CommandLine, or at least write a better parser if adding a dependency is problematic.
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--name")
    {
        name = GetArgumentValue(i, args);
    }
    else if (args[i] == "--output-directory")
    {
        outputDirectory = GetArgumentValue(i, args);
    }
    else if (args[i] == "--nuget-username")
    {
        nugetUsername = GetArgumentValue(i, args);
    }
}

var options = new TemplateGenerationOptions(name, outputDirectory, nugetPackageName, nugetUsername);

var generator = new LibraryTemplateGenerator();
generator.Generate(options);

static string GetArgumentValue(int i, string[] args)
{
    if (args.Length < i + 2 || args[i + 1].StartsWith("--", StringComparison.Ordinal))
    {
        throw new InvalidOperationException($"A value for '{args[i]}' was not provided.");
    }

    return args[i + 1];
}
