using System.Net.Http;
using System.Windows;
using QuickTrans.App.Services;
using QuickTrans.App.ViewModels;
using QuickTrans.App.Views;

namespace QuickTrans.App;

public partial class App : Application
{
    private HttpClient? _httpClient;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        var translationService = new GoogleTranslateService(_httpClient);
        var viewModel = new MainWindowViewModel(translationService);
        var mainWindow = new MainWindow(viewModel);

        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _httpClient?.Dispose();
        base.OnExit(e);
    }
}
