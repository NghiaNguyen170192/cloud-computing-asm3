using NetCore.Donation.WebClient;

namespace NetCore.Donation.Admin;

public static class AdminLookups
{
    public const int SearchMinLength = 3;

    public const int ContactSearchMinLength = SearchMinLength;

    public static ODataListRequest PaymentMethods { get; } = new()
    {
        Select = "Id,DisplayName,ContactId,PaymentType",
        OrderBy = "DisplayName",
        Top = 500,
        Count = false,
    };

    public static ODataListRequest PaymentSchedules { get; } = new()
    {
        Select = "Id,Identifier,ContactId,PaymentMethodId",
        OrderBy = "Identifier",
        Top = 500,
        Count = false,
    };

    public static ODataListRequest Transactions { get; } = new()
    {
        Select = "Id,Identifier,ContactId,Status",
        OrderBy = "Identifier desc",
        Top = 500,
        Count = false,
    };

    public static ODataListRequest Related(string select, string? filter = null, string? orderBy = null) => new()
    {
        Select = select,
        Filter = filter,
        OrderBy = orderBy,
        Top = 20,
        Count = true,
    };

    public static string Eq(string property, Guid id) =>
        $"{property} eq {id:D}";

    public static ODataListRequest ContactSearch(string term) =>
        Search(
            "Id,FullName,FirstName,LastName,Email",
            Or(Contains("FirstName", term), Contains("LastName", term), Contains("Email", term)),
            "LastName,FirstName");

    public static ODataListRequest CountrySearch(string term) =>
        Search(
            "Id,Name,Alpha2,CountryCode",
            Or(Contains("Name", term), Contains("Alpha2", term), Contains("CountryCode", term), Contains("Alpha3", term)),
            "Name");

    public static ODataListRequest PaymentMethodSearch(string term) =>
        Search(
            "Id,DisplayName,PaymentType,ContactId,ContactFullName",
            Contains("DisplayName", term),
            "DisplayName");

    public static ODataListRequest PaymentScheduleSearch(string term) =>
        Search(
            "Id,Identifier,ContactId,PaymentMethodId,PaymentType,PaymentMethodDisplayName",
            Or(Contains("Identifier", term), Contains("PaymentMethodDisplayName", term)),
            "Identifier");

    public static ODataListRequest TransactionSearch(string term) =>
        Search(
            "Id,Identifier,ContactId,ContactFullName,Status,PaymentType,PaymentMethodId,PaymentMethodDisplayName,PaymentScheduleId,PaymentScheduleIdentifier",
            Or(Contains("Identifier", term), Contains("ContactFullName", term)),
            "Identifier desc");

    public static ODataListRequest Search(string select, string filter, string? orderBy = null) => new()
    {
        Select = select,
        Filter = filter,
        OrderBy = orderBy,
        Top = 20,
        Count = false,
    };

    public static string Contains(string property, string term)
    {
        var escaped = term.Trim().ToLowerInvariant().Replace("'", "''", StringComparison.Ordinal);
        return $"contains(tolower({property}),'{escaped}')";
    }

    public static string Or(params string[] parts) => string.Join(" or ", parts);
}
