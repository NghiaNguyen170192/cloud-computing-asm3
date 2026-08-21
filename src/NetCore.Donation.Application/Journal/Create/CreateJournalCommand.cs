using MediatR;

namespace NetCore.Donation.Application.Journal.Create;

public sealed record CreateJournalCommand(Guid TransactionId) : IRequest<Guid>;
