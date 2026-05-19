using Microsoft.Extensions.DependencyInjection;

namespace Squash;

internal static class Bootstrapper
{
    [STAThread]
    private static void Main()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddHttpClient("Default", c => c.DefaultRequestHeaders.Add("User-Agent", "Squash"));
        // Forms
        serviceCollection.AddSingleton<MainFormV2>();
        // Controls
        serviceCollection.AddSingleton<EncodingQueuePanel>();
        serviceCollection.AddSingleton<AboutPanel>();
        // Services
        serviceCollection.AddSingleton<BinaryLocatorService>();
        serviceCollection.AddSingleton<PersistentStateService>();
        serviceCollection.AddSingleton<DownloadService>();
        serviceCollection.AddSingleton<ExtractService>();
        serviceCollection.AddSingleton<EncodeService>();
        serviceCollection.AddSingleton<Win32Service>();
        serviceCollection.AddTransient<MissingBinariesTaskDialogService>();
        serviceCollection.AddTransient<FirstRunTaskDialogService>();
        
        var services = serviceCollection.BuildServiceProvider();

        Application.ThreadException += (_, args) =>
        {
            var exception      = args.Exception;
            var exitButton     = new TaskDialogButton("&Exit");
            var restartButton  = new TaskDialogButton("&Restart");
            var continueButton = new TaskDialogButton("&Continue");
            var res = TaskDialog.ShowDialog(new TaskDialogPage
            {
                Caption = "Squash",
                Heading = "Squash encountered an error",
                Text    = "It is recommended to restart the application to avoid further issues.\nIf this problem persists, please report it!",
                Expander = new TaskDialogExpander
                {
                    Text = exception.ToString()
                },
                Icon = TaskDialogIcon.Error,
                Buttons = { exitButton, restartButton, continueButton }
            });
            if (res == exitButton)
            {
                Application.Exit();
            }
            
            if (res == restartButton)
            {
                Application.Restart();
                Application.Exit();
            }
        };
        
        ApplicationConfiguration.Initialize();
        Application.Run(services.GetRequiredService<MainFormV2>());
    }
}
