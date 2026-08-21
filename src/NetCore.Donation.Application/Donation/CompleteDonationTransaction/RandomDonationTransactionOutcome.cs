namespace NetCore.Donation.Application.Donation.CompleteDonationTransaction;

public sealed class RandomDonationTransactionOutcome : IDonationTransactionOutcome
{
    public bool IsSuccess() => Random.Shared.Next(2) == 0;
}
