using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControleEstoque.Desktop.Helpers;
using ControleEstoque.Desktop.Models;
using ControleEstoque.Desktop.Services;

namespace ControleEstoque.Desktop.ViewModels;

/// <summary>Linha do carrinho. A quantidade nunca passa do estoque disponível.</summary>
public partial class ItemCarrinho : ObservableObject
{
    public ItemCarrinho(Produto produto, int quantidade)
    {
        Produto = produto;
        _quantidade = quantidade;
    }

    public Produto Produto { get; }
    public string Nome => Produto.NomeProduto;
    public decimal PrecoUnitario => Produto.PrecoProduto;
    public int Estoque => Produto.QuantidadeEstoque;
    public decimal Subtotal => Quantidade * PrecoUnitario;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Subtotal))]
    private int _quantidade;

    partial void OnQuantidadeChanged(int value)
    {
        var ajustada = Math.Clamp(value, 1, Math.Max(1, Estoque));
        if (ajustada != value) Quantidade = ajustada;
    }
}

/// <summary>Tela de venda (PDV) para Caixa e Gerente.</summary>
public partial class VendaViewModel : ViewModelBase
{
    public ObservableCollection<Produto> Catalogo { get; } = new();
    public ICollectionView CatalogoView { get; }
    public ObservableCollection<ItemCarrinho> Carrinho { get; } = new();
    public List<FormaPagamentoOpcao> FormasPagamento => Pagamentos.Todas;

    [ObservableProperty] private string _filtro = "";
    [ObservableProperty] private Produto? _produtoSel;
    [ObservableProperty] private string _quantidadeAdicionar = "1";
    [ObservableProperty] private FormaPagamentoOpcao? _pagamentoSel = Pagamentos.Todas[0];
    [ObservableProperty] private decimal _total;

    public VendaViewModel()
    {
        CatalogoView = CollectionViewSource.GetDefaultView(Catalogo);
        CatalogoView.Filter = item => item is Produto p
            && (string.IsNullOrWhiteSpace(Filtro)
                || p.NomeProduto.Contains(Filtro, StringComparison.OrdinalIgnoreCase));
    }

    partial void OnFiltroChanged(string value) => CatalogoView.Refresh();

    public override Task InicializarAsync() => ExecutarAsync(CarregarCatalogoAsync);

    private async Task CarregarCatalogoAsync()
    {
        // A rota /Catalogo é liberada para Caixa, Estoquista e Gerente (só produtos ativos)
        var lista = await CatalogoService.TodosProdutosAsync("api/Catalogo/produtos");

        Catalogo.Clear();
        foreach (var p in lista.Where(p => p.QuantidadeEstoque > 0).OrderBy(p => p.NomeProduto))
            Catalogo.Add(p);
    }

    private void RecalcularTotal() => Total = Carrinho.Sum(i => i.Subtotal);

    [RelayCommand]
    private void Adicionar()
    {
        if (ProdutoSel is null) { Erro("Selecione um produto na lista."); return; }

        if (!int.TryParse(QuantidadeAdicionar, NumberStyles.Integer, CultureInfo.CurrentCulture, out var quantidade)
            || quantidade <= 0)
        {
            Erro("Informe uma quantidade inteira maior que zero.");
            return;
        }

        var existente = Carrinho.FirstOrDefault(i => i.Produto.Id == ProdutoSel.Id);
        var jaNoCarrinho = existente?.Quantidade ?? 0;

        if (jaNoCarrinho + quantidade > ProdutoSel.QuantidadeEstoque)
        {
            Erro($"Estoque insuficiente: há {ProdutoSel.QuantidadeEstoque} un. de \"{ProdutoSel.NomeProduto}\".");
            return;
        }

        if (existente != null)
        {
            existente.Quantidade += quantidade;
        }
        else
        {
            var item = new ItemCarrinho(ProdutoSel, quantidade);
            item.PropertyChanged += (_, _) => RecalcularTotal();
            Carrinho.Add(item);
        }

        RecalcularTotal();
        QuantidadeAdicionar = "1";
        Mensagem = null;
    }

    [RelayCommand]
    private void Remover(ItemCarrinho? item)
    {
        if (item is null) return;
        Carrinho.Remove(item);
        RecalcularTotal();
    }

    [RelayCommand]
    private void Cancelar()
    {
        Carrinho.Clear();
        RecalcularTotal();
        Mensagem = null;
    }

    [RelayCommand]
    private async Task FinalizarAsync()
    {
        if (Carrinho.Count == 0) { Erro("Adicione ao menos um item à venda."); return; }
        if (PagamentoSel is null) { Erro("Selecione a forma de pagamento."); return; }
        if (!Dialogos.Confirmar($"Finalizar a venda de {Total:C} ({PagamentoSel.Nome})?")) return;

        // O usuário e a data são definidos pela API (token e relógio do servidor)
        var corpo = new
        {
            formaPagamento = PagamentoSel.Id,
            itensVenda = Carrinho.Select(i => new { produtoId = i.Produto.Id, quantidade = i.Quantidade }).ToList()
        };

        Venda? venda = null;

        var ok = await ExecutarAsync(async () =>
        {
            venda = await Sessao.Api.PostAsync<Venda>("api/Venda", corpo);
            Carrinho.Clear();
            RecalcularTotal();
            await CarregarCatalogoAsync(); // estoque atualizado
        });

        if (ok && venda != null)
            Info($"Venda nº {venda.Id} registrada: {venda.TotalVenda:C}.");
    }
}
