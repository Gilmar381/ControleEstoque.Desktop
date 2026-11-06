using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControleEstoque.Desktop.Helpers;
using ControleEstoque.Desktop.Models;
using ControleEstoque.Desktop.Services;

namespace ControleEstoque.Desktop.ViewModels;

public partial class CategoriasViewModel : ViewModelBase
{
    public ObservableCollection<Categoria> Categorias { get; } = new();

    [ObservableProperty] private Categoria? _selecionada;
    [ObservableProperty] private string _nome = "";

    public bool Editando => Selecionada != null;

    partial void OnSelecionadaChanged(Categoria? value)
    {
        Nome = value?.NomeCategoria ?? "";
        OnPropertyChanged(nameof(Editando));
    }

    public override Task InicializarAsync() => ExecutarAsync(CarregarAsync);

    private async Task CarregarAsync()
    {
        var lista = await Sessao.Api.GetAsync<List<Categoria>>("api/Categoria");
        Categorias.Clear();
        foreach (var c in lista) Categorias.Add(c);
    }

    [RelayCommand]
    private void Novo()
    {
        Selecionada = null;
        Nome = "";
    }

    [RelayCommand]
    private async Task SalvarAsync()
    {
        if (string.IsNullOrWhiteSpace(Nome)) { Erro("Informe o nome da categoria."); return; }

        var corpo = new { nomeCategoria = Nome.Trim() };
        var id = Selecionada?.CategoriaId;

        var ok = await ExecutarAsync(async () =>
        {
            if (id is null) await Sessao.Api.PostAsync<Categoria>("api/Categoria", corpo);
            else await Sessao.Api.PutAsync<Categoria>($"api/Categoria/{id}", corpo);

            await CarregarAsync();
        });

        if (!ok) return;

        Novo();
        Info("Categoria salva.");
    }

    [RelayCommand]
    private async Task ExcluirAsync()
    {
        if (Selecionada is null) return;
        if (!Dialogos.Confirmar($"Excluir a categoria \"{Selecionada.NomeCategoria}\"?")) return;

        var id = Selecionada.CategoriaId;
        var ok = await ExecutarAsync(async () =>
        {
            await Sessao.Api.DeleteAsync($"api/Categoria/{id}");
            await CarregarAsync();
        });

        if (!ok) return;

        Novo();
        Info("Categoria excluída.");
    }
}
