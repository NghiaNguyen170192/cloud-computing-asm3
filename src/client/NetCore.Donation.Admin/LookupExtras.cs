using NetCore.Donation.WebClient;

namespace NetCore.Donation.Admin;

internal static class LookupExtras
{
    public static async Task<string?> ForAsync<T>(
        Guid id,
        string? provided,
        Guid cachedId,
        string? cached,
        Func<Guid, Task<T?>> load,
        Func<T?, string?> extra)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(provided))
        {
            return provided;
        }

        if (cachedId == id && cached is not null)
        {
            return cached;
        }

        try
        {
            return extra(await load(id));
        }
        catch (Exception)
        {
            return cachedId == id ? cached : null;
        }
    }

    public static string? Describe(TransactionDto? transaction)
    {
        if (transaction is null)
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(transaction.ContactFullName)
            ? transaction.Status.ToString()
            : $"{transaction.ContactFullName} · {transaction.Status}";
    }
}
