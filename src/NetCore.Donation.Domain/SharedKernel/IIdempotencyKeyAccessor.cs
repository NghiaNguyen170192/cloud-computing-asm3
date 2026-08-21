namespace NetCore.Donation.Domain.SharedKernel;

public interface IIdempotencyKeyAccessor
{
    string? IdempotencyKey { get; }
}
