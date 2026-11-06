using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControleEstoque.Desktop.Helpers;
using ControleEstoque.Desktop.Models;
using ControleEstoque.Desktop.Services;

namespace ControleEstoque.Desktop.ViewModels;

public partial class UsuariosViewModel : ViewModelBase
{
    public ObservableCollection<Usuario> Usuarios { get; } = new();
    public ObservableCollection<Perfil> Perfis { get; } = new();

    [ObservableProperty] private Usuario? _selecionado;
    [ObservableProperty] private string _nome = "";
    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string _login = "";
    [ObservableProperty] private string _senha = "";
    [ObservableProperty] private Perfil? _perfilSel;
    [ObservableProperty] private bool _ativo = true;

    /// <summary>Login e senha só podem ser definidos na criação.</summary>
    public bool ModoCriacao => Selecionado == null;
    public bool Editando => Selecionado != null;

    partial void OnSelecionadoChanged(Usuario? value)
    {
        OnPropertyChanged(nameof(ModoCriacao));
        OnPropertyChanged(nameof(Editando));
        if (value is null) { LimparCampos(); return; }

        Nome = value.Nome;
        Email = value.Email;
        Login = value.Login;
        Senha = "";
        Ativo = value.Ativo;
        PerfilSel = Perfis.FirstOrDefault(p => p.Id == value.PerfilId);
    }

    private void LimparCampos()
    {
        Nome = Email = Login = Senha = "";
        Ativo = true;
        PerfilSel = null;
    }

    public override Task InicializarAsync() => ExecutarAsync(async () =>
    {
        var perfis = await Sessao.Api.GetAsync<List<Perfil>>("api/Perfil");
        Perfis.Clear();
        foreach (var p in perfis) Perfis.Add(p);

        await CarregarAsync();
    });

    private async Task CarregarAsync()
    {
        var lista = await Sessao.Api.GetAsync<List<Usuario>>("api/Usuario");
        Usuarios.Clear();
        foreach (var u in lista) Usuarios.Add(u);
    }

    [RelayCommand]
    private void Novo()
    {
        Selecionado = null;
        LimparCampos();
    }

    [RelayCommand]
    private async Task SalvarAsync()
    {
        if (string.IsNullOrWhiteSpace(Nome) || string.IsNullOrWhiteSpace(Email))
        {
            Erro("Informe nome e e-mail.");
            return;
        }

        if (PerfilSel is null) { Erro("Selecione o perfil."); return; }

        var id = Selecionado?.Id;

        if (id is null)
        {
            if (string.IsNullOrWhiteSpace(Login)) { Erro("Informe o login."); return; }
            if (Senha.Length < 8 || Senha.Length > 72) { Erro("A senha deve ter de 8 a 72 caracteres."); return; }
        }
        else if (id == Sessao.UsuarioId && !Ativo)
        {
            Erro("Você não pode desativar o seu próprio usuário.");
            return;
        }

        var perfilId = PerfilSel.Id;

        var ok = await ExecutarAsync(async () =>
        {
            if (id is null)
            {
                await Sessao.Api.PostAsync<object>("api/Usuario", new
                {
                    nome = Nome.Trim(), email = Email.Trim(), login = Login.Trim(),
                    senha = Senha, perfilId, ativo = Ativo
                });
            }
            else
            {
                await Sessao.Api.PutAsync<object>($"api/Usuario/{id}", new
                {
                    nome = Nome.Trim(), email = Email.Trim(), perfilId, ativo = Ativo
                });
            }

            await CarregarAsync();
        });

        if (!ok) return;

        Novo();
        Info("Usuário salvo.");
    }

    [RelayCommand]
    private async Task DesativarAsync()
    {
        if (Selecionado is null) return;

        if (Selecionado.Id == Sessao.UsuarioId)
        {
            Erro("Você não pode desativar o seu próprio usuário.");
            return;
        }

        if (!Dialogos.Confirmar($"Desativar o usuário \"{Selecionado.Nome}\"? Ele não poderá mais entrar no sistema."))
            return;

        var id = Selecionado.Id;
        var ok = await ExecutarAsync(async () =>
        {
            await Sessao.Api.DeleteAsync($"api/Usuario/{id}");
            await CarregarAsync();
        });

        if (!ok) return;

        Novo();
        Info("Usuário desativado.");
    }
}
