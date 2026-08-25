using NetCore.Donation.Domain.Storage;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NetCore.Donation.Infrastructure.Storage;

public sealed class ReceiptPdfDocumentGenerator : IReceiptDocumentGenerator
{
    public const string ContentType = "application/pdf";

    private static readonly object LicenseGate = new();
    private static bool licenseInitialized;

    public Task<ReceiptDocumentContent> GenerateAsync(
        string fileName,
        string body,
        CancellationToken cancellationToken = default)
    {
        EnsureCommunityLicense();
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);

        var pdfFileName = fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
            ? fileName
            : $"{fileName}.pdf";

        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(54);
                page.MarginVertical(50);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(text => text.FontSize(11).LineHeight(1.4f));

                page.Content().Column(column =>
                {
                    var headingWritten = false;
                    foreach (var rawLine in body.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (string.IsNullOrWhiteSpace(rawLine))
                        {
                            column.Item().Height(10);
                            continue;
                        }

                        var line = rawLine.Trim();
                        if (!headingWritten)
                        {
                            column.Item().Text(line).FontSize(18).SemiBold();
                            headingWritten = true;
                            continue;
                        }

                        column.Item().Text(line);
                    }
                });
            });
        }).GeneratePdf();

        return Task.FromResult(new ReceiptDocumentContent(
            new MemoryStream(bytes, writable: false),
            ContentType,
            pdfFileName,
            bytes.LongLength));
    }

    private static void EnsureCommunityLicense()
    {
        if (licenseInitialized)
        {
            return;
        }

        lock (LicenseGate)
        {
            if (licenseInitialized)
            {
                return;
            }

            try
            {
                QuestPDF.Settings.License = LicenseType.Community;
                licenseInitialized = true;
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    "QuestPDF failed to initialize. Native QuestPdfSkia assets must be present next to the running app. " +
                    exception.GetBaseException().Message,
                    exception);
            }
        }
    }
}

