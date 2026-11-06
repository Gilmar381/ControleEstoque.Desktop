using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControleEstoque.Desktop.Helpers;
using ControleEstoque.Desktop.Models;
using ControleEstoque.Desktop.Services;

namespace ControleEstoque.Desktop.ViewModels;

public partial class ProdutosViewModel : ViewModelBase
{
    private List<Produto> _paginaAtual = new();

    public ObservableCollection<Produto> Produtos { get; } = new();
    public ObservableCollection<Categoria> Categorias { get; } = new();
    public ObservableCollection<Fornecedor> Fornecedores { get; } = new();
    public Paginacao Pag { get; }

    [ObservableProperty] private Produto? _selecionado;
    [ObservableProperty] private string _filtro = "";
    [ObservableProperty] private bool _incluirInativos;

    [ObservableProperty] private string _nome = "";
    [ObservableProperty] private string _descricao = "";
    [ObservableProperty] private string _preco = "";
    [ObservableProperty] private Categoria? _categoriaSel;
    [ObservableProperty] private Fornecedor? _fornecedorSel;
    [ObservableProperty] private bool _ativo = true;

    public bool Editando => Selecionado != null;

    public ProdutosViewModel()
    {
        Pag = new Paginacao(() => ExecutarAsync(CarregarPaginaAsync));
    }

    public override Task InicializarAsync() => ExecutarAsync(async () =>
    {
        var categorias = await Sessao.Api.GetAsync<List<Categoria>>("api/Categoria");
        Categorias.Clear();
        foreach (var c in categorias) Categorias.Add(c);

        try
        {
            var fornecedores = await Sessao.Api.GetAsync<List<Fornecedor>>("api/Catalogo/fornecedores");
            Fornecedores.Clear();
            foreach (var f in fornecedores) Fornecedores.Add(f);
        }
        catch (ApiException ex)
        {
            Erro("Não foi possível carregar os fornecedores: " + ex.Message);
        }

        await CarregarPaginaAsync();
    });

    private async Task CarregarPaginaAsync()
    {
        var inativos = IncluirInativos ? "true" : "false";
        var url = $"api/Produtos?pagina={Pag.Pagina}&tamanhoPagina={Paginacao.Tamanho}&incluirInativos={inativos}";

        var resultado = await Sessao.Api.GetAsync<Paginado<Produto>>(url);
        Pag.Atualizar(resultado);
        _paginaAtual = resultado.Itens;
        AplicarFiltro();
    }

    // O filtro de texto atua sobre a página exibida (a API não tem busca por nome).
    private void AplicarFiltro()
    {
        Produtos.Clear();
        foreach (var p in _paginaAtual.Where(p =>
                     string.IsNullOrWhiteSpace(Filtro)
                     || p.NomeProduto.Contains(Filtro, StringComparison.OrdinalIgnoreCase)))
            Produtos.Add(p);
    }

    partial void OnFiltroChanged(string value) => AplicarFiltro();

    partial void OnIncluirInativosChanged(bool value)
    {
        Pag.Reiniciar();
        _ = ExecutarAsync(CarregarPaginaAsync);
    }

    partial void OnSelecionadoChanged(Produto? value)
    {
        OnPropertyChanged(nameof(Editando));
        if (value is null) { LimparCampos(); return; }

        Nome = value.NomeProduto;
        Descricao = value.DescricaoProduto ?? "";
        Preco = value.PrecoProduto.ToString("N2");
        Ativo = value.Ativo;
        // A API devolve só os nomes; localizamos o item correspondente nas listas
        CategoriaSel = Categorias.FirstOrDefault(c => c.NomeCategoria == value.NomeCategoria);
        FornecedorSel = Fornecedores.FirstOrDefault(f => f.NomeFornecedor == value.NomeFornecedor);
    }

    private void LimparCampos()
    {
        Nome = Descricao = Preco = "";
        CategoriaSel = null;
        FornecedorSel = null;
        Ativo = true;
    }

    private bool TentarPreco(out decimal preco) =>
        decimal.TryParse(Preco, NumberStyles.Number, CultureInfo.CurrentCulture, out preco) && preco > 0;

    [RelayCommand]
    private void Novo()
    {
        Selecionado = null;
        LimparCampos();
    }

    [RelayCommand]
    private async Task SalvarAsync()
    {
        if (string.IsNullOrWhiteSpace(Nome)) { Erro("Informe o nome do produto."); return; }
        if (string.IsNullOrWhiteSpace(Descricao)) { Erro("Informe a descrição do produto."); return; }
        if (!TentarPreco(out var preco)) { Erro("Informe um preço válido, maior que zero."); return; }
        if (CategoriaSel is null) { Erro("Selecione a categoria."); return; }

        var id = Selecionado?.Id;
        if (id is null && FornecedorSel is null) { Erro("Selecione o fornecedor."); return; }

        var ok = await ExecutarAsync(async () =>
        {
            if (id is null)
            {
                await Sessao.Api.PostAsync<object>("api/Produtos", new
                {
                    nomeProduto = Nome.Trim(),
                    descricaoProduto = Descricao.Trim(),
                    categoriaId = CategoriaSel.CategoriaId,
                    fornecedorId = FornecedorSel!.Id,
                    precoProduto = preco
                });
            }
            else
            {
                // Atualização parcial: campos nulos não são alterados pela API
                await Sessao.Api.PutAsync<object>($"api/Produtos/{id}", new
                {
                    nomeProduto = Nome.Trim(),
                    descricaoProduto = Descricao.Trim(),
                    categoriaId = CategoriaSel.CategoriaId,
                    fornecedorId = FornecedorSel?.Id,
                    precoProduto = preco,
                    ativo = Ativo
                });
            }

            await CarregarPaginaAsync();
        });

        if (!ok) return;

        Novo();
        Info("Produto salvo.");
    }

    [RelayCommand]
    private async Task DesativarAsync()
    {
        if (Selecionado is null) return;
        if (!Dialogos.Confirmar($"Desativar o produto \"{Selecionado.NomeProduto}\"? O histórico de vendas e estoque é mantido."))
            return;

        var id = Selecionado.Id;
        var ok = await ExecutarAsync(async () =>
        {
            await Sessao.Api.DeleteAsync($"api/Produtos/{id}");
            await CarregarPaginaAsync();
        });

        if (!ok) return;

        Novo();
        Info("Produto desativado.");
    }
}
