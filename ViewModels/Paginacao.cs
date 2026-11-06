using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControleEstoque.Desktop.Models;

namespace ControleEstoque.Desktop.ViewModels;

/// <summary>Controle de página reutilizável (anterior / próxima) das listas paginadas da API.</summary>
public partial class Paginacao : ObservableObject
{
    public const int Tamanho = 20;
    private readonly Func<Task> _recarregar;

    public Paginacao(Func<Task> recarregar) => _recarregar = recarregar;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PodeAnterior))]
    [NotifyPropertyChangedFor(nameof(PodeProxima))]
    [NotifyPropertyChangedFor(nameof(Texto))]
    private int _pagina = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PodeProxima))]
    [NotifyPropertyChangedFor(nameof(Texto))]
    private int _totalPaginas = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Texto))]
    private int _total;

    public bool PodeAnterior => Pagina > 1;
    public bool PodeProxima => Pagina < TotalPaginas;
    public string Texto => $"Página {Pagina} de {Math.Max(TotalPaginas, 1)}  ·  {Total} registro(s)";

    public void Atualizar<T>(Paginado<T> resultado)
    {
        Pagina = resultado.Pagina;
        TotalPaginas = resultado.TotalPaginas;
        Total = resultado.Total;
    }

    public void Reiniciar() => Pagina = 1;

    [RelayCommand]
    private async Task AnteriorAsync()
    {
        if (!PodeAnterior) return;
        Pagina--;
        await _recarregar();
    }

    [RelayCommand]
    private async Task ProximaAsync()
    {
        if (!PodeProxima) return;
        Pagina++;
        await _recarregar();
    }
}
