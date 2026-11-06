namespace ControleEstoque.Desktop.Models;

// Espelham as respostas JSON da API (o nome das propriedades não diferencia maiúsculas de minúsculas).

public record LoginResposta(bool Sucesso, string? Token, int UsuarioId, string? Nome, string? PerfilNome, string? Mensagem);

public record Paginado<T>(List<T> Itens, int Pagina, int TamanhoPagina, int Total, int TotalPaginas);

public record Categoria(int CategoriaId, string NomeCategoria);

public record Fornecedor(int Id, string NomeFornecedor, string CnpjFornecedor, string TelefoneFornecedor,
    string EmailFornecedor, string EnderecoFornecedor);

public record Produto(int Id, string NomeProduto, string? DescricaoProduto, decimal PrecoProduto,
    string NomeFornecedor, string NomeCategoria, int QuantidadeEstoque, bool Ativo,
    DateTime DataCadastro, DateTime UltimaAtualizacao);

public record Perfil(int Id, string Nome);

public record Usuario(int Id, string Nome, string Email, string Login, int PerfilId, string? PerfilNome,
    bool Ativo, DateTime DataCriacao, DateTime UltimoLogin);

public record Movimentacao(int Id, int ProdutoId, string ProdutoNome, int TipoMovimentacao, int Quantidade,
    DateTime DataMovimentacao, int UsuarioId, string UsuarioNome, string? Observacao);

public record ItemVenda(int Id, int VendaId, int ProdutoId, int Quantidade, decimal PrecoUnitario, decimal Subtotal);

public record Venda(int Id, DateTime DataVenda, int UsuarioId, decimal TotalVenda, int FormaPagamento,
    List<ItemVenda> ItensVenda);

// Modelos usados apenas na interface
public record ItemVendaExibicao(string ProdutoNome, int Quantidade, decimal PrecoUnitario, decimal Subtotal);

public record VendaLinha(int Id, DateTime Data, string Usuario, string Pagamento, decimal Total,
    List<ItemVendaExibicao> Itens);

public record FormaPagamentoOpcao(int Id, string Nome);

public static class Pagamentos
{
    public static readonly List<FormaPagamentoOpcao> Todas = new()
    {
        new(1, "Dinheiro"), new(2, "Cartão de crédito"), new(3, "Cartão de débito"),
        new(4, "Pix"), new(5, "Vale-alimentação"), new(6, "Outros")
    };

    public static string Nome(int id) => Todas.FirstOrDefault(p => p.Id == id)?.Nome ?? "—";
}
