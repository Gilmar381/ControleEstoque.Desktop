using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControleEstoque.Desktop.Services;

namespace ControleEstoque.Desktop.ViewModels;

public partial class ContaViewModel : ViewModelBase
{
    public string Nome => Sessao.Nome;
    public string Perfil => Sessao.Perfil;

    [ObservableProperty] private string _senhaAtual = "";
    [ObservableProperty] private string _novaSenha = "";
    [ObservableProperty] private string _confirmar = "";

    [RelayCommand]
    private async Task AlterarAsync()
    {
        if (string.IsNullOrEmpty(SenhaAtual)) { Erro("Informe a senha atual."); return; }
        if (NovaSenha.Length < 8 || NovaSenha.Length > 72) { Erro("A nova senha deve ter de 8 a 72 caracteres."); return; }
        if (NovaSenha != Confirmar) { Erro("A confirmação não confere com a nova senha."); return; }

        var ok = await ExecutarAsync(() =>
            Sessao.Api.PutAsync<object>("api/Auth/AlterarSenha", new { senhaAtual = SenhaAtual, novaSenha = NovaSenha }));

        if (!ok) return;

        SenhaAtual = NovaSenha = Confirmar = "";
        Info("Senha alterada com sucesso.");
    }
}
