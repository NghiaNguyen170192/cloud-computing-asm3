namespace NetCore.Donation.Application.Donation.CompleteDonationTransaction;

public sealed class SucceededDonationTransactionOutcome : IDonationTransactionOutcome
{
    public bool IsSuccess() => true;
}
