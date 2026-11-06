using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using ControleEstoque.Desktop.Services;
using ControleEstoque.Desktop.ViewModels;
using ControleEstoque.Desktop.Views;

namespace ControleEstoque.Desktop;

public partial class App : Application
{
    private LoginWindow? _login;
    private MainWindow? _main;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Formatos de moeda e data em português do Brasil, também nos bindings do WPF
        var cultura = new CultureInfo("pt-BR");
        CultureInfo.DefaultThreadCurrentCulture = cultura;
        CultureInfo.DefaultThreadCurrentUICulture = cultura;
        Thread.CurrentThread.CurrentCulture = cultura;
        Thread.CurrentThread.CurrentUICulture = cultura;
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(cultura.IetfLanguageTag)));

        // Token recusado pela API (expirado, usuário desativado...) volta para o login
        Sessao.Api.SessaoExpirada += () =>
            Dispatcher.Invoke(() => MostrarLogin("Sua sessão expirou. Entre novamente."));

        MostrarLogin();
    }

    private void MostrarLogin(string? aviso = null)
    {
        if (_login != null) return;

        Sessao.Limpar();

        var vm = new LoginViewModel();
        if (aviso != null) vm.Aviso(aviso);

        _login = new LoginWindow { DataContext = vm };
        vm.LoginConcluido += () =>
        {
            var mainVm = new MainViewModel();
            mainVm.SairSolicitado += () => MostrarLogin();

            _main = new MainWindow { DataContext = mainVm };
            _main.Show();

            var login = _login;
            _login = null;
            login?.Close();

            _ = mainVm.IniciarAsync();
        };

        var anterior = _main;
        _main = null;
        _login.Show();
        anterior?.Close();
    }
}
