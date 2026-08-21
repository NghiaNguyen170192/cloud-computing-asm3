namespace NetCore.Donation.WebClient;

public sealed class ODataListRequest
{
    public string? Select { get; init; }

    public string? Filter { get; init; }

    public int? Top { get; init; }

    public int? Skip { get; init; }

    public string? OrderBy { get; init; }

    public bool Count { get; init; } = true;

    public string AppendTo(string path)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(Select))
        {
            parts.Add("$select=" + Uri.EscapeDataString(Select));
        }

        if (!string.IsNullOrWhiteSpace(Filter))
        {
            parts.Add("$filter=" + Uri.EscapeDataString(Filter));
        }

        if (Top is { } top)
        {
            parts.Add("$top=" + top.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        if (Skip is { } skip)
        {
            parts.Add("$skip=" + skip.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrWhiteSpace(OrderBy))
        {
            parts.Add("$orderby=" + Uri.EscapeDataString(OrderBy));
        }

        if (Count)
        {
            parts.Add("$count=true");
        }

        if (parts.Count == 0)
        {
            return path;
        }

        var separator = path.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return path + separator + string.Join("&", parts);
    }
}

public sealed class ODataListResult<T>
{
    public IReadOnlyList<T> Value { get; init; } = [];

    public int Count { get; init; }
}
