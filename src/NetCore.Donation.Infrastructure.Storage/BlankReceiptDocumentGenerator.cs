using System.Text;
using NetCore.Donation.Domain.Storage;

namespace NetCore.Donation.Infrastructure.Storage;

public sealed class BlankReceiptDocumentGenerator : IReceiptDocumentGenerator
{
    // Minimal valid single-page blank PDF. Replace later with a document-merge template.
    private static readonly byte[] BlankPdfBytes = Encoding.ASCII.GetBytes(
        "%PDF-1.1\n" +
        "1 0 obj<< /Type /Catalog /Pages 2 0 R >>endobj\n" +
        "2 0 obj<< /Type /Pages /Kids [3 0 R] /Count 1 >>endobj\n" +
        "3 0 obj<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>endobj\n" +
        "xref\n" +
        "0 4\n" +
        "0000000000 65535 f \n" +
        "0000000009 00000 n \n" +
        "0000000058 00000 n \n" +
        "0000000115 00000 n \n" +
        "trailer<< /Size 4 /Root 1 0 R >>\n" +
        "startxref\n" +
        "190\n" +
        "%%EOF\n");

    public Task<ReceiptDocumentContent> GenerateAsync(
        Guid receiptId,
        Guid contactId,
        Guid? transactionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var stream = new MemoryStream(BlankPdfBytes, writable: false);
        var fileName = $"receipt-{receiptId:N}.pdf";
        var content = new ReceiptDocumentContent(
            stream,
            "application/pdf",
            fileName,
            BlankPdfBytes.LongLength);

        return Task.FromResult(content);
    }
}
