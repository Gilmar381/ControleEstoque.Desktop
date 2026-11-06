using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControleEstoque.Desktop.Services;

namespace ControleEstoque.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public event Action? SairSolicitado;

    [ObservableProperty] private ViewModelBase? _atual;

    public string NomeUsuario => Sessao.Nome;
    public string PerfilUsuario => Sessao.Perfil;

    // O menu só mostra o que o perfil tem permissão de usar na API.
    public bool VeVenda => Sessao.EhCaixa || Sessao.EhGerente;
    public bool VeEstoque => Sessao.EhEstoquista || Sessao.EhGerente;
    public bool VeGerente => Sessao.EhGerente;

    public Task IniciarAsync() => IrAsync(VeEstoque ? "Produtos" : "Pdv");

    [RelayCommand]
    private async Task IrAsync(string? destino)
    {
        ViewModelBase tela = destino switch
        {
            "Pdv" => new VendaViewModel(),
            "Produtos" => new ProdutosViewModel(),
            "Estoque" => new EstoqueViewModel(),
            "Categorias" => new CategoriasViewModel(),
            "Fornecedores" => new FornecedoresViewModel(),
            "Vendas" => new VendasViewModel(),
            "Usuarios" => new UsuariosViewModel(),
            "Conta" => new ContaViewModel(),
            _ => throw new ArgumentOutOfRangeException(nameof(destino))
        };

        Atual = tela;
        await tela.InicializarAsync();
    }

    [RelayCommand]
    private void Sair() => SairSolicitado?.Invoke();
}
