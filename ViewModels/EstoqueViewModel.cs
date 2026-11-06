using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControleEstoque.Desktop.Helpers;
using ControleEstoque.Desktop.Models;
using ControleEstoque.Desktop.Services;

namespace ControleEstoque.Desktop.ViewModels;

public partial class EstoqueViewModel : ViewModelBase
{
    public ObservableCollection<Produto> Produtos { get; } = new();
    public ObservableCollection<Movimentacao> Historico { get; } = new();
    public Paginacao Pag { get; }

    // Entrada
    [ObservableProperty] private Produto? _produtoEntrada;
    [ObservableProperty] private string _quantidadeEntrada = "";
    [ObservableProperty] private string _obsEntrada = "";

    // Saída avulsa (perdas, consumo interno...)
    [ObservableProperty] private Produto? _produtoSaida;
    [ObservableProperty] private string _quantidadeSaida = "";
    [ObservableProperty] private string _obsSaida = "";

    // Filtros do histórico
    [ObservableProperty] private Produto? _produtoFiltro;
    [ObservableProperty] private DateTime? _dataInicio = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime? _dataFim = DateTime.Today;

    public EstoqueViewModel()
    {
        Pag = new Paginacao(() => ExecutarAsync(CarregarHistoricoAsync));
    }

    public override Task InicializarAsync() => ExecutarAsync(async () =>
    {
        await CarregarProdutosAsync();
        await CarregarHistoricoAsync();
    });

    private async Task CarregarProdutosAsync()
    {
        // Mantém o filtro escolhido depois que a lista é recarregada
        var filtroId = ProdutoFiltro?.Id;

        var lista = await CatalogoService.TodosProdutosAsync("api/Catalogo/produtos");
        Produtos.Clear();
        foreach (var p in lista.OrderBy(p => p.NomeProduto)) Produtos.Add(p);

        ProdutoFiltro = Produtos.FirstOrDefault(p => p.Id == filtroId);
    }

    private async Task CarregarHistoricoAsync()
    {
        string url;

        if (ProdutoFiltro != null)
        {
            url = $"api/MovimentacaoEstoque/movimentacao/produto/{ProdutoFiltro.Id}?";
        }
        else
        {
            var inicio = (DataInicio ?? DateTime.Today.AddDays(-30)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var fim = (DataFim ?? DateTime.Today).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            url = $"api/MovimentacaoEstoque/movimentacao/periodo?dataInicio={inicio}&dataFim={fim}&";
        }

        var resultado = await Sessao.Api.GetAsync<Paginado<Movimentacao>>(
            $"{url}pagina={Pag.Pagina}&tamanhoPagina={Paginacao.Tamanho}");

        Pag.Atualizar(resultado);
        Historico.Clear();
        foreach (var m in resultado.Itens) Historico.Add(m);
    }

    private static bool TentarQuantidade(string texto, out int quantidade) =>
        int.TryParse(texto, NumberStyles.Integer, CultureInfo.CurrentCulture, out quantidade) && quantidade > 0;

    [RelayCommand]
    private async Task RegistrarEntradaAsync()
    {
        if (ProdutoEntrada is null) { Erro("Selecione o produto da entrada."); return; }
        if (!TentarQuantidade(QuantidadeEntrada, out var quantidade)) { Erro("Informe uma quantidade inteira maior que zero."); return; }
        if (string.IsNullOrWhiteSpace(ObsEntrada)) { Erro("Informe a observação (ex.: nota fiscal ou fornecedor)."); return; }

        var corpo = new { produtoId = ProdutoEntrada.Id, quantidade, observacao = ObsEntrada.Trim() };
        var nome = ProdutoEntrada.NomeProduto;

        var ok = await ExecutarAsync(async () =>
        {
            await Sessao.Api.PostAsync<object>("api/MovimentacaoEstoque/entrada", corpo);
            await CarregarProdutosAsync();
            Pag.Reiniciar();
            await CarregarHistoricoAsync();
        });

        if (!ok) return;

        ProdutoEntrada = null;
        QuantidadeEntrada = ObsEntrada = "";
        Info($"Entrada de {quantidade} un. de \"{nome}\" registrada.");
    }

    [RelayCommand]
    private async Task RegistrarSaidaAsync()
    {
        if (ProdutoSaida is null) { Erro("Selecione o produto da saída."); return; }
        if (!TentarQuantidade(QuantidadeSaida, out var quantidade)) { Erro("Informe uma quantidade inteira maior que zero."); return; }
        if (string.IsNullOrWhiteSpace(ObsSaida)) { Erro("Informe o motivo da saída."); return; }

        var nome = ProdutoSaida.NomeProduto;
        if (!Dialogos.Confirmar($"Registrar a saída de {quantidade} un. de \"{nome}\"?")) return;

        var corpo = new
        {
            observacao = ObsSaida.Trim(),
            itens = new[] { new { produtoId = ProdutoSaida.Id, quantidade } }
        };

        var ok = await ExecutarAsync(async () =>
        {
            await Sessao.Api.PostAsync<object>("api/MovimentacaoEstoque/saida", corpo);
            await CarregarProdutosAsync();
            Pag.Reiniciar();
            await CarregarHistoricoAsync();
        });

        if (!ok) return;

        ProdutoSaida = null;
        QuantidadeSaida = ObsSaida = "";
        Info($"Saída de {quantidade} un. de \"{nome}\" registrada.");
    }

    [RelayCommand]
    private async Task BuscarAsync()
    {
        Pag.Reiniciar();
        await ExecutarAsync(CarregarHistoricoAsync);
    }

    [RelayCommand]
    private async Task LimparFiltroAsync()
    {
        ProdutoFiltro = null;
        DataInicio = DateTime.Today.AddDays(-30);
        DataFim = DateTime.Today;
        Pag.Reiniciar();
        await ExecutarAsync(CarregarHistoricoAsync);
    }
}
