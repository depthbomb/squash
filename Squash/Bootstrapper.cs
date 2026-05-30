using Squash.Interop;
using Microsoft.Extensions.Hosting;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Extensions.DependencyInjection;

namespace Squash;

internal static class Bootstrapper
{
    [STAThread]
    private static void Main()
    {
        Settings.Default.Upgrade();

        #region App Instance Setup
        var instance = AppInstance.FindOrRegisterForKey(GlobalShared.MutexName);
        if (!instance.IsCurrent)
        {
            var args = AppInstance.GetCurrent().GetActivatedEventArgs();

            _ = instance.RedirectActivationToAsync(args);

            Application.Exit();
            return;
        }
        #endregion

        Native.SetCurrentProcessExplicitAppUserModelId(GlobalShared.Product.AppUserModelId);

        var host = Host.CreateDefaultBuilder()
                       .ConfigureServices(s =>
                       {
                           // Forms
                           s.AddSingleton<MainFormV2>();
                           // Controls
                           s.AddSingleton<EncodingQueuePanel>();
                           s.AddSingleton<SettingsPanel>();
                           s.AddSingleton<AboutPanel>();
                           // Services
                           s.AddSingleton<BinaryLocatorService>();
                           s.AddSingleton<PersistentStateService>();
                           s.AddSingleton<DownloadService>();
                           s.AddSingleton<ExtractService>();
                           s.AddSingleton<ThumbnailService>();
                           s.AddSingleton<EncodeService>();
                           s.AddTransient<MissingBinariesTaskDialogService>();
                           s.AddTransient<FirstRunTaskDialogService>();
                           // HTTP clients
                           s.AddHttpClient("Default", c => c.DefaultRequestHeaders.Add("User-Agent", "Squash"));
                       })
                       .Build();

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
                Icon    = TaskDialogIcon.Error,
                Buttons = { exitButton, restartButton, continueButton }
            });
            if (res == exitButton)
            {
                Application.Exit();
            }

            if (res == restartButton)
            {
                Application.Restart();
            }
        };
        Application.ApplicationExit += (_, _) =>
        {
            Settings.Default.Save();
            instance.UnregisterKey();
        };

        instance.Activated += (_, _) => host.Services.GetRequiredService<MainFormV2>().BringToFrontFromActivation();

        ApplicationConfiguration.Initialize();
        Application.Run(host.Services.GetRequiredService<MainFormV2>());
    }
}
