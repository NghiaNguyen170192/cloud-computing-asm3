using MediatR;
using NetCore.Donation.Application.Journal.DTOs;

namespace NetCore.Donation.Application.Journal.GetJournal;

public sealed record GetJournalQuery(Guid Id) : IRequest<QueryJournalDto?>;
