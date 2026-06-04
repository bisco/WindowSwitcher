using System.Windows;

namespace WindowSwitcher;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        // Ctrl+C をコンソールで受け取ってシャットダウン
        System.Console.CancelKeyPress += (_, args) =>
        {
            args.Cancel = true;
            Dispatcher.Invoke(() => Shutdown());
        };
    }
}