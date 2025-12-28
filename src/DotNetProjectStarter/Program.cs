using System.CommandLine;

var nameOption = new Option<string>("--name")
{
    Description = "The name of the project to create",
    Arity = ArgumentArity.ExactlyOne,
};

var outputDirectoryOption = new Option<string>("--output-directory")
{
    Description = "The directory where the project will be created",
    Arity = ArgumentArity.ExactlyOne,
};

var nugetPackageNameOption = new Option<string>("--nuget-package-name")
{
    Description = "The name of the NuGet package for package publishing",
    Arity = ArgumentArity.ExactlyOne,
};

var nugetUsernameOption = new Option<string>("--nuget-username")
{
    Description = "The NuGet username for package publishing",
    Arity = ArgumentArity.ExactlyOne,
};

var rootCommand = new RootCommand("Creates a new .NET project template");
rootCommand.Options.Add(nameOption);
rootCommand.Options.Add(outputDirectoryOption);
rootCommand.Options.Add(nugetPackageNameOption);
rootCommand.Options.Add(nugetUsernameOption);

var parseResult = rootCommand.Parse(args);
var name = parseResult.GetValue(nameOption);
var outputDirectory = parseResult.GetValue(outputDirectoryOption);
var nugetPackageName = parseResult.GetValue(nugetPackageNameOption);
var nugetUsername = parseResult.GetValue(nugetUsernameOption);

var options = new TemplateGenerationOptions(name, outputDirectory, nugetPackageName, nugetUsername);

var generator = new LibraryTemplateGenerator();
generator.Generate(options);
