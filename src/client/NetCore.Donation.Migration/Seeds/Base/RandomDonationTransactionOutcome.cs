namespace NetCore.Donation.Migration.Seeds.Base;

public sealed class RandomDonationTransactionOutcome
    : NetCore.Donation.Application.Donation.CompleteDonationTransaction.IDonationTransactionOutcome
{
    public bool IsSuccess() => Random.Shared.Next(2) == 0;
}
