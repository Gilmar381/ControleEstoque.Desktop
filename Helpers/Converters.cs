using System.Globalization;
using System.Windows.Data;


namespace ControleEstoque.Desktop.Helpers;

public class TipoMovimentacaoConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        1 => "Entrada",
        2 => "Saída",
        3 => "Ajuste",
        _ => "—"
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>A API grava datas em UTC. Aqui elas são exibidas no horário local.</summary>
public class DataLocalConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTime data || data.Year < 2000) return "—";
        var utc = data.Kind == DateTimeKind.Utc ? data : DateTime.SpecifyKind(data, DateTimeKind.Utc);
        return utc.ToLocalTime().ToString("dd/MM/yyyy HH:mm", culture);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
