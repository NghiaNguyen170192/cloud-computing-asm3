using MediatR;
using NetCore.Donation.Domain.Storage;

namespace NetCore.Donation.Application.Receipt.GetReceiptDocument;

public sealed record GetReceiptDocumentQuery(Guid Id) : IRequest<ReceiptDocumentContent?>;
