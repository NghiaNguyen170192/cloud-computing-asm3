using MediatR;
using NetCore.Donation.Application.Receipt;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.Storage;

namespace NetCore.Donation.Application.Receipt.GetReceiptDocument;

public class GetReceiptDocumentQueryHandler(
    IReceiptRepository receiptRepository,
    IReceiptDocumentStorage documentStorage)
    : IRequestHandler<GetReceiptDocumentQuery, ReceiptDocumentContent?>
{
    public async Task<ReceiptDocumentContent?> Handle(
        GetReceiptDocumentQuery request,
        CancellationToken cancellationToken)
    {
        var receipt = await receiptRepository.FindByIdAsync(request.Id, cancellationToken);
        if (receipt is null || string.IsNullOrWhiteSpace(receipt.DocumentObjectKey))
        {
            return null;
        }

        var stream = await documentStorage.GetAsync(receipt.DocumentObjectKey, cancellationToken);
        if (stream is null)
        {
            return null;
        }

        var fileName = string.IsNullOrWhiteSpace(receipt.DocumentFileName)
            ? ReceiptDocumentService.PdfFileName(receipt.Identifier)
            : receipt.DocumentFileName;
        var contentType = string.IsNullOrWhiteSpace(receipt.DocumentContentType)
            ? ReceiptDocumentService.PdfContentType
            : receipt.DocumentContentType;
        var sizeBytes = receipt.DocumentSizeBytes ?? stream.Length;

        return new ReceiptDocumentContent(stream, contentType, fileName, sizeBytes);
    }
}
