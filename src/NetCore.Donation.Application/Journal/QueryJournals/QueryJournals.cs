using MediatR;
using NetCore.Donation.Application.Journal.DTOs;

namespace NetCore.Donation.Application.Journal.QueryJournals;

public sealed record QueryJournals : IRequest<IQueryable<QueryJournalDto>>;
