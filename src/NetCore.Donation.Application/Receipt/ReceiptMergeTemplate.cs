using System.Globalization;

namespace NetCore.Donation.Application.Receipt;

public sealed record ReceiptMergeFields(
    string ReceiptNumber,
    string DonorName,
    string DonationAmount,
    string DonationDate,
    string PaymentMethod);

public static class ReceiptMergeTemplate
{
    public const string OrganisationName = "Hope and Help";
    public const string MissionStatement = "support communities in need through practical help and care";
    public const string Unspecified = "—";

    public static string Render(ReceiptMergeFields fields)
    {
        ArgumentNullException.ThrowIfNull(fields);

        return
            $"""
            Thank You for Your Generous Donation

            Dear {fields.DonorName},
            Thank you for your generous donation of {fields.DonationAmount} to {OrganisationName}.

            Your support makes a meaningful difference and helps us continue our work to {MissionStatement}. We are truly grateful for your generosity and commitment to our cause.

            Please find your donation receipt below for your records:

            Donation Receipt

            Receipt Number: {fields.ReceiptNumber}
            Donation Date: {fields.DonationDate}
            Donor: {fields.DonorName}
            Amount: {fields.DonationAmount}
            Payment Method: {fields.PaymentMethod}

            Thank you once again for supporting {OrganisationName}. We sincerely appreciate your contribution and the difference it helps us make in the community.

            With gratitude,

            {OrganisationName}
            """;
    }

    public static string FormatAmount(decimal amount) =>
        amount.ToString("C", CultureInfo.GetCultureInfo("en-AU"));

    public static string FormatDate(DateOnly date) =>
        date.ToString("d MMMM yyyy", CultureInfo.GetCultureInfo("en-AU"));
}
