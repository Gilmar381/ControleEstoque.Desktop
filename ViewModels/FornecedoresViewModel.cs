using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControleEstoque.Desktop.Helpers;
using ControleEstoque.Desktop.Models;
using ControleEstoque.Desktop.Services;

namespace ControleEstoque.Desktop.ViewModels;

public partial class FornecedoresViewModel : ViewModelBase
{
    public ObservableCollection<Fornecedor> Fornecedores { get; } = new();

    [ObservableProperty] private Fornecedor? _selecionado;
    [ObservableProperty] private string _nome = "";
    [ObservableProperty] private string _cnpj = "";
    [ObservableProperty] private string _telefone = "";
    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string _endereco = "";

    public bool Editando => Selecionado != null;

    partial void OnSelecionadoChanged(Fornecedor? value)
    {
        OnPropertyChanged(nameof(Editando));
        if (value is null) { LimparCampos(); return; }

        Nome = value.NomeFornecedor;
        Cnpj = value.CnpjFornecedor;
        Telefone = value.TelefoneFornecedor;
        Email = value.EmailFornecedor;
        Endereco = value.EnderecoFornecedor;
    }

    private void LimparCampos() => Nome = Cnpj = Telefone = Email = Endereco = "";

    public override Task InicializarAsync() => ExecutarAsync(CarregarAsync);

    private async Task CarregarAsync()
    {
        var lista = await Sessao.Api.GetAsync<List<Fornecedor>>("api/Fornecedor");
        Fornecedores.Clear();
        foreach (var f in lista) Fornecedores.Add(f);
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
        if (new[] { Nome, Cnpj, Telefone, Email, Endereco }.Any(string.IsNullOrWhiteSpace))
        {
            Erro("Preencha todos os campos do fornecedor.");
            return;
        }

        var corpo = new
        {
            nome = Nome.Trim(), cnpj = Cnpj.Trim(), telefone = Telefone.Trim(),
            email = Email.Trim(), endereco = Endereco.Trim()
        };
        var id = Selecionado?.Id;

        var ok = await ExecutarAsync(async () =>
        {
            if (id is null) await Sessao.Api.PostAsync<Fornecedor>("api/Fornecedor", corpo);
            else await Sessao.Api.PutAsync<Fornecedor>($"api/Fornecedor/{id}", corpo);

            await CarregarAsync();
        });

        if (!ok) return;

        Novo();
        Info("Fornecedor salvo.");
    }

    [RelayCommand]
    private async Task ExcluirAsync()
    {
        if (Selecionado is null) return;
        if (!Dialogos.Confirmar($"Excluir o fornecedor \"{Selecionado.NomeFornecedor}\"?")) return;

        var id = Selecionado.Id;
        var ok = await ExecutarAsync(async () =>
        {
            await Sessao.Api.DeleteAsync($"api/Fornecedor/{id}");
            await CarregarAsync();
        });

        if (!ok) return;

        Novo();
        Info("Fornecedor excluído.");
    }
}
