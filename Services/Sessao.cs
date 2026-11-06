namespace ControleEstoque.Desktop.Services;

/// <summary>Usuário logado e cliente da API (só em memória: o token nunca é gravado em disco).</summary>
public static class Sessao
{
    public static ApiClient Api { get; } = new();

    public static int UsuarioId { get; set; }
    public static string Nome { get; set; } = "";
    public static string Perfil { get; set; } = "";

    public static bool EhGerente => Perfil == "Gerente";
    public static bool EhEstoquista => Perfil == "Estoquista";
    public static bool EhCaixa => Perfil == "Caixa";

    public static void Limpar()
    {
        Api.Token = null;
        UsuarioId = 0;
        Nome = "";
        Perfil = "";
    }
}
