namespace NetCore.Donation.Domain.SharedKernel;

public static class RecordIdentifier
{
    public const int MaxLength = 32;

    public static string PaymentSchedule(DateOnly date, Guid id) => Format("PS", date, id);

    public static string Transaction(DateOnly date, Guid id) => Format("TXN", date, id);

    public static string Journal(DateOnly date, Guid id) => Format("JN", date, id);

    public static string Receipt(DateOnly date, Guid id) => Format("RC", date, id);

    private static string Format(string prefix, DateOnly date, Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Id is required.", nameof(id));
        }

        return $"{prefix}-{date:yyyyMMdd}-{id.ToString("N")[..8]}";
    }
}
