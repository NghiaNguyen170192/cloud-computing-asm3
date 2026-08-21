using MediatR;

namespace NetCore.Donation.Application.Donation.GenerateDonationReceipt;

public sealed record GenerateDonationReceiptCommand(Guid TransactionId) : IRequest<Guid>;
