using CommunityToolkit.Mvvm.ComponentModel;
using ControleEstoque.Desktop.Services;

namespace ControleEstoque.Desktop.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty] private bool _ocupado;
    [ObservableProperty] private string? _mensagem;
    [ObservableProperty] private bool _mensagemEhErro;

    /// <summary>Chamado ao abrir a tela (carrega os dados iniciais).</summary>
    public virtual Task InicializarAsync() => Task.CompletedTask;

    protected void Info(string texto) { Mensagem = texto; MensagemEhErro = false; }
    protected void Erro(string texto) { Mensagem = texto; MensagemEhErro = true; }

    /// <summary>Executa uma ação de API mostrando "ocupado" e convertendo falhas em mensagem na tela.</summary>
    protected async Task<bool> ExecutarAsync(Func<Task> acao)
    {
        Ocupado = true;
        try
        {
            await acao();
            return true;
        }
        catch (ApiException ex)
        {
            Erro(ex.Message);
            return false;
        }
        finally
        {
            Ocupado = false;
        }
    }
}
