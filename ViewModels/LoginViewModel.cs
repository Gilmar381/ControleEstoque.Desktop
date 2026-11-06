using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControleEstoque.Desktop.Models;
using ControleEstoque.Desktop.Services;

namespace ControleEstoque.Desktop.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    public event Action? LoginConcluido;

    [ObservableProperty] private string _url = LerUrlPadrao();
    [ObservableProperty] private string _login = "";
    [ObservableProperty] private string _senha = "";

    public void Aviso(string texto) => Erro(texto);

    [RelayCommand]
    private async Task EntrarAsync()
    {
        if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrEmpty(Senha))
        {
            Erro("Informe login e senha.");
            return;
        }

        if (!Uri.TryCreate(Url, UriKind.Absolute, out _))
        {
            Erro("Endereço da API inválido. Exemplo: https://localhost:7096/");
            return;
        }

        Mensagem = null;

        var ok = await ExecutarAsync(async () =>
        {
            Sessao.Api.Configurar(Url);

            var resposta = await Sessao.Api.PostAsync<LoginResposta>(
                "api/Auth/Login", new { login = Login.Trim(), senha = Senha });

            Sessao.Api.Token = resposta.Token;
            Sessao.UsuarioId = resposta.UsuarioId;
            Sessao.Nome = resposta.Nome ?? Login;
            Sessao.Perfil = resposta.PerfilNome ?? "";
        });

        if (ok) LoginConcluido?.Invoke();
    }

    private static string LerUrlPadrao()
    {
        try
        {
            var caminho = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            using var doc = JsonDocument.Parse(File.ReadAllText(caminho));
            if (doc.RootElement.TryGetProperty("ApiBaseUrl", out var valor)
                && valor.GetString() is { Length: > 0 } url)
                return url;
        }
        catch { /* usa o padrão */ }

        return "https://localhost:7096/";
    }
}
