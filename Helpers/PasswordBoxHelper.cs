using System.Windows;
using System.Windows.Controls;

namespace ControleEstoque.Desktop.Helpers;

/// <summary>Permite fazer binding da senha do PasswordBox (que não tem binding nativo).</summary>
public static class PasswordBoxHelper
{
    public static readonly DependencyProperty SenhaProperty =
        DependencyProperty.RegisterAttached(
            "Senha", typeof(string), typeof(PasswordBoxHelper),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, AoMudar));

    public static string? GetSenha(DependencyObject d) => (string?)d.GetValue(SenhaProperty);
    public static void SetSenha(DependencyObject d, string? valor) => d.SetValue(SenhaProperty, valor);

    private static void AoMudar(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox caixa) return;

        caixa.PasswordChanged -= AoDigitar;
        var novo = e.NewValue as string ?? "";
        if (caixa.Password != novo) caixa.Password = novo;
        caixa.PasswordChanged += AoDigitar;
    }

    private static void AoDigitar(object sender, RoutedEventArgs e)
    {
        var caixa = (PasswordBox)sender;
        SetSenha(caixa, caixa.Password);
    }
}
