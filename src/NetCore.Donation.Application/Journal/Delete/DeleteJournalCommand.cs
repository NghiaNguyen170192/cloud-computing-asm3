using MediatR;

namespace NetCore.Donation.Application.Journal.Delete;

public sealed record DeleteJournalCommand(Guid Id) : IRequest<bool>;
