using ControleEstoque.Desktop.Models;

namespace ControleEstoque.Desktop.Services;

public static class CatalogoService
{
    /// <summary>Percorre todas as páginas de uma rota paginada de produtos.</summary>
    public static async Task<List<Produto>> TodosProdutosAsync(string rota)
    {
        var todos = new List<Produto>();
        var pagina = 1;
        int totalPaginas;

        do
        {
            var separador = rota.Contains('?') ? "&" : "?";
            var resultado = await Sessao.Api.GetAsync<Paginado<Produto>>(
                $"{rota}{separador}pagina={pagina}&tamanhoPagina=100");

            todos.AddRange(resultado.Itens);
            totalPaginas = resultado.TotalPaginas;
            pagina++;
        } while (pagina <= totalPaginas && pagina <= 100);

        return todos;
    }
}
