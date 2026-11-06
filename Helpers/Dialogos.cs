using System.Windows;

namespace ControleEstoque.Desktop.Helpers;

public static class Dialogos
{
    public static bool Confirmar(string mensagem) =>
        MessageBox.Show(mensagem, "Confirmação", MessageBoxButton.YesNo, MessageBoxImage.Question)
            == MessageBoxResult.Yes;
}
