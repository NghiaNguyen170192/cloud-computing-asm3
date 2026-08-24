using NetCore.Donation.WebClient;
using Radzen;
using Radzen.Blazor;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace NetCore.Donation.Admin;

public static class RadzenODataQuery
{
    private static readonly Regex CultureDateLiteral = new(
        @"(?<property>[A-Za-z_][A-Za-z0-9_]*)\s+(?<op>eq|ne|gt|ge|lt|le)\s+(?<value>\d{1,2}/\d{1,2}/\d{4}(?:\s+\d{1,2}:\d{2}(?::\d{2})?(?:\s*[AaPp][Mm])?)?)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static ODataListRequest From<TItem>(LoadDataArgs args, RadzenDataFilter<TItem>? filter, string select)
    {
        var fromFilter = filter?.ToODataFilterString();
        string? odataFilter = IsMeaningful(fromFilter)
            ? ToODataDateLiterals(fromFilter!.Trim(), typeof(TItem))
            : null;
        var orderBy = string.IsNullOrWhiteSpace(args.OrderBy) ? null : args.OrderBy.Trim();

        return new ODataListRequest
        {
            Select = string.IsNullOrWhiteSpace(select) ? null : select,
            Filter = odataFilter,
            Top = args.Top is > 0 ? args.Top : 20,
            Skip = args.Skip ?? 0,
            OrderBy = orderBy,
            Count = true,
        };
    }

    public static string ShortId(Guid id) => id.ToString("N")[..8];

    public static string ShortId(Guid? id) => id is { } value && value != Guid.Empty ? ShortId(value) : string.Empty;

    public static string IdentifierOrShort(string? identifier, Guid? id) =>
        string.IsNullOrWhiteSpace(identifier) ? ShortId(id) : identifier;

    private static bool IsMeaningful(string? filter) =>
        !string.IsNullOrWhiteSpace(filter) &&
        !string.Equals(filter.Trim(), "null", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(filter.Trim(), "(null)", StringComparison.OrdinalIgnoreCase);

    private static string ToODataDateLiterals(string filter, Type itemType)
    {
        return CultureDateLiteral.Replace(filter, match =>
        {
            var property = match.Groups["property"].Value;
            var op = match.Groups["op"].Value;
            var value = match.Groups["value"].Value;
            if (!TryParseCultureDate(value, out var dateTime))
            {
                return match.Value;
            }

            return $"{property} {op} {FormatODataDate(dateTime, PropertyType(itemType, property))}";
        });
    }

    private static bool TryParseCultureDate(string value, out DateTime dateTime)
    {
        if (DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out dateTime))
        {
            return true;
        }

        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out dateTime);
    }

    private static Type? PropertyType(Type itemType, string propertyName)
    {
        var property = itemType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (property is null)
        {
            return null;
        }

        return Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
    }

    private static string FormatODataDate(DateTime value, Type? propertyType)
    {
        if (propertyType == typeof(DateOnly) || (propertyType is null && value.TimeOfDay == TimeSpan.Zero))
        {
            return value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
        return utc.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
    }
}

