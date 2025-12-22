using Microsoft.Extensions.Configuration;

namespace julkort2025;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        // Build configuration with user secrets
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddUserSecrets<Program>()
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        // Bind configuration to class
        var appConfig = new AppConfiguration();
        configuration.Bind(appConfig);

        Console.WriteLine($"OpenAIApiKey: {appConfig.OpenAIApiKey}");
        Console.WriteLine($"InputFolder: {appConfig.InputFolder}");
        Console.WriteLine($"OutputFolder: {appConfig.OutputFolder}"); 
        
        var imageEditor = new AIImageEditor(appConfig);
        await imageEditor.CreateXmasCard();
    }
}

