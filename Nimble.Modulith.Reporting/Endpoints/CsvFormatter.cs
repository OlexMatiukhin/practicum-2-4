using System.Globalization;
using System.Reflection;
using System.Text;

namespace Nimble.Modulith.Reporting.Endpoints;

public static class CsvFormatter
{
    public static string Format<T>(IEnumerable<T> rows)
    {
        var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        var builder = new StringBuilder();

        builder.AppendLine(string.Join(",", properties.Select(p => Escape(p.Name))));

        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(",", properties.Select(p => Escape(ToStringValue(p.GetValue(row))))));
        }

        return builder.ToString();
    }

    private static string ToStringValue(object? value) =>
        value switch
        {
            null => string.Empty,
            DateTime dateTime => dateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DateOnly dateOnly => dateOnly.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            decimal decimalValue => decimalValue.ToString(CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
        };

    private static string Escape(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
