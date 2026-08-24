using NetCore.Donation.WebClient;

namespace NetCore.Donation.Admin;

internal static class LookupQuery
{
    public static async Task<IReadOnlyList<T>> Values<T>(Task<ODataListResult<T>> query)
    {
        var result = await query;
        return result.Value;
    }
}
