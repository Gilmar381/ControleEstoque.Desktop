using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ControleEstoque.Desktop.Models;
using ControleEstoque.Desktop.Services;

namespace ControleEstoque.Desktop.ViewModels;

/// <summary>Histórico de vendas (somente Gerente).</summary>
public partial class VendasViewModel : ViewModelBase
{
    private Dictionary<int, string> _produtos = new();
    private Dictionary<int, string> _usuarios = new();

    public ObservableCollection<VendaLinha> Vendas { get; } = new();
    public ObservableCollection<ItemVendaExibicao> Itens { get; } = new();
    public Paginacao Pag { get; }

    [ObservableProperty] private VendaLinha? _selecionada;

    public VendasViewModel()
    {
        Pag = new Paginacao(() => ExecutarAsync(CarregarPaginaAsync));
    }

    partial void OnSelecionadaChanged(VendaLinha? value)
    {
        Itens.Clear();
        if (value is null) return;
        foreach (var i in value.Itens) Itens.Add(i);
    }

    public override Task InicializarAsync() => ExecutarAsync(async () =>
    {
        // Nomes de produtos e usuários, para exibir no lugar dos números (IDs)
        var produtos = await CatalogoService.TodosProdutosAsync("api/Produtos?incluirInativos=true");
        _produtos = produtos.ToDictionary(p => p.Id, p => p.NomeProduto);

        var usuarios = await Sessao.Api.GetAsync<List<Usuario>>("api/Usuario");
        _usuarios = usuarios.ToDictionary(u => u.Id, u => u.Nome);

        await CarregarPaginaAsync();
    });

    private async Task CarregarPaginaAsync()
    {
        var resultado = await Sessao.Api.GetAsync<Paginado<Venda>>(
            $"api/Venda?pagina={Pag.Pagina}&tamanhoPagina={Paginacao.Tamanho}");

        Pag.Atualizar(resultado);

        Vendas.Clear();
        foreach (var v in resultado.Itens)
        {
            var itens = v.ItensVenda
                .Select(i => new ItemVendaExibicao(
                    _produtos.GetValueOrDefault(i.ProdutoId, $"Produto #{i.ProdutoId}"),
                    i.Quantidade, i.PrecoUnitario, i.Subtotal))
                .ToList();

            Vendas.Add(new VendaLinha(
                v.Id, v.DataVenda,
                _usuarios.GetValueOrDefault(v.UsuarioId, $"Usuário #{v.UsuarioId}"),
                Pagamentos.Nome(v.FormaPagamento), v.TotalVenda, itens));
        }
    }
}
