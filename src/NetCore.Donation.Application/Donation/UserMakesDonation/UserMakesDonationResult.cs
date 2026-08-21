namespace NetCore.Donation.Application.Donation.UserMakesDonation;

public sealed record UserMakesDonationResult(
    Guid ContactId,
    Guid PaymentMethodId,
    Guid? PaymentScheduleId,
    Guid? TransactionId,
    bool IsRecurring);
