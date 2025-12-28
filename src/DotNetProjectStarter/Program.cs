using System.CommandLine;
using System.Threading.Tasks;

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

var nugetUsernameOption = new Option<string>("--nuget-username")
{
    Description = "The NuGet username for package publishing",
    Arity = ArgumentArity.ExactlyOne,
};

var rootCommand = new RootCommand("Creates a new .NET project template");
rootCommand.Options.Add(nameOption);
rootCommand.Options.Add(outputDirectoryOption);
rootCommand.Options.Add(nugetUsernameOption);
rootCommand.SetAction(parseResult =>
{
    var name = parseResult.GetValue(nameOption);
    var outputDirectory = parseResult.GetValue(outputDirectoryOption);
    var nugetUsername = parseResult.GetValue(nugetUsernameOption);

    var options = new TemplateGenerationOptions(name, outputDirectory, nugetUsername);

    var generator = new LibraryTemplateGenerator();
    return Task.FromResult(generator.Generate(options) ? 0 : 1);
});

var parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync();
