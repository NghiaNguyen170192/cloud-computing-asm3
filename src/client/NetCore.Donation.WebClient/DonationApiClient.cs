using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace NetCore.Donation.WebClient;

public sealed class DonationApiClient(HttpClient http)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public Task<IReadOnlyList<CountryDto>> GetCountriesAsync(CancellationToken cancellationToken = default) =>
        GetListAsync<CountryDto>("api/v1/countries", cancellationToken);

    public Task<ODataListResult<ContactDto>> QueryContactsAsync(
        ODataListRequest? request = null,
        CancellationToken cancellationToken = default) =>
        QueryODataAsync<ContactDto>("api/v1/contacts", request, cancellationToken);

    public Task<IReadOnlyList<ContactDto>> GetContactsAsync(CancellationToken cancellationToken = default) =>
        GetListAsync<ContactDto>("api/v1/contacts", cancellationToken);

    public Task<ContactDto?> GetContactAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetAsync<ContactDto>($"api/v1/contacts/{id}", cancellationToken);

    public Task<ODataListResult<PaymentMethodDto>> QueryPaymentMethodsAsync(
        ODataListRequest? request = null,
        Guid? contactId = null,
        CancellationToken cancellationToken = default) =>
        QueryODataAsync<PaymentMethodDto>(WithContact("api/v1/payment-methods", contactId), request, cancellationToken);

    public Task<PaymentMethodDto?> GetPaymentMethodAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetAsync<PaymentMethodDto>($"api/v1/payment-methods/{id}", cancellationToken);

    public Task<Guid> CreateContactAsync(
        string firstName,
        string lastName,
        DateOnly dateOfBirth,
        string addressLine,
        string email,
        string phoneNumber,
        Guid countryId,
        bool doNotEmail,
        bool doNotSms,
        CancellationToken cancellationToken = default) =>
        PostIdAsync(
            "api/v1/contacts",
            new
            {
                firstName,
                lastName,
                dateOfBirth,
                addressLine,
                email,
                phoneNumber,
                countryId,
                doNotEmail,
                doNotSms,
            },
            cancellationToken);

    public Task SetContactPreferencesAsync(
        Guid id,
        bool doNotEmail,
        bool doNotSms,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            HttpMethod.Patch,
            $"api/v1/contacts/{id}/preferences",
            new { id, doNotEmail, doNotSms },
            cancellationToken);

    public Task<IReadOnlyList<PaymentMethodDto>> GetPaymentMethodsAsync(
        Guid? contactId = null,
        CancellationToken cancellationToken = default) =>
        GetListAsync<PaymentMethodDto>(WithContact("api/v1/payment-methods", contactId), cancellationToken);

    public Task<Guid> CreatePaymentMethodAsync(
        Guid contactId,
        string displayName,
        CancellationToken cancellationToken = default) =>
        PostIdAsync("api/v1/payment-methods", new { contactId, displayName }, cancellationToken);

    public Task<ODataListResult<PaymentScheduleDto>> QueryPaymentSchedulesAsync(
        ODataListRequest? request = null,
        Guid? contactId = null,
        CancellationToken cancellationToken = default) =>
        QueryODataAsync<PaymentScheduleDto>(WithContact("api/v1/payment-schedules", contactId), request, cancellationToken);

    public Task<IReadOnlyList<PaymentScheduleDto>> GetPaymentSchedulesAsync(
        Guid? contactId = null,
        CancellationToken cancellationToken = default) =>
        GetListAsync<PaymentScheduleDto>(WithContact("api/v1/payment-schedules", contactId), cancellationToken);

    public Task<PaymentScheduleDto?> GetPaymentScheduleAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetAsync<PaymentScheduleDto>($"api/v1/payment-schedules/{id}", cancellationToken);

    public Task<Guid> CreatePaymentScheduleAsync(
        Guid contactId,
        Guid paymentMethodId,
        decimal amount,
        DateOnly bookDate,
        RecurringInterval recurringInterval,
        CancellationToken cancellationToken = default) =>
        PostIdAsync(
            "api/v1/payment-schedules",
            new { contactId, paymentMethodId, amount, bookDate, recurringInterval },
            cancellationToken);

    public Task<ODataListResult<TransactionDto>> QueryTransactionsAsync(
        ODataListRequest? request = null,
        Guid? contactId = null,
        Guid? paymentScheduleId = null,
        CancellationToken cancellationToken = default)
    {
        var path = "api/v1/transactions";
        if (contactId is { } contact)
        {
            path = WithContact(path, contact);
        }

        if (paymentScheduleId is { } schedule)
        {
            path += path.Contains('?', StringComparison.Ordinal) ? "&" : "?";
            path += $"paymentScheduleId={schedule}";
        }

        return QueryODataAsync<TransactionDto>(path, request, cancellationToken);
    }

    public Task<TransactionDto?> GetTransactionAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetAsync<TransactionDto>($"api/v1/transactions/{id}", cancellationToken);

    public async Task<IReadOnlyList<TransactionDto>> GetTransactionsAsync(
        Guid? contactId = null,
        Guid? paymentScheduleId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await QueryTransactionsAsync(null, contactId, paymentScheduleId, cancellationToken);
        return result.Value;
    }

    public async Task<UserMakesDonationResponse> MakeDonationAsync(
        string firstName,
        string lastName,
        DateOnly dateOfBirth,
        string addressLine,
        string email,
        string phoneNumber,
        Guid countryId,
        decimal amount,
        string paymentMethodName,
        PaymentType paymentType,
        bool isRecurring,
        RecurringInterval recurringInterval,
        Gender gender,
        bool doNotEmail,
        bool doNotSms,
        CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsJsonAsync(
            "api/v1/donations",
            new
            {
                firstName,
                lastName,
                dateOfBirth,
                addressLine,
                email,
                phoneNumber,
                countryId,
                amount,
                paymentMethodName,
                paymentType,
                isRecurring,
                recurringInterval,
                gender,
                doNotEmail,
                doNotSms,
            },
            JsonOptions,
            cancellationToken);
        await EnsureSuccessAsync(response);
        var created = await response.Content.ReadFromJsonAsync<UserMakesDonationResponse>(JsonOptions, cancellationToken);
        if (created is null || created.ContactId == Guid.Empty || created.PaymentMethodId == Guid.Empty)
        {
            throw new HttpRequestException("The API did not return a donation result.");
        }

        if (created.IsRecurring && created.PaymentScheduleId is null)
        {
            throw new HttpRequestException("The API did not return a payment schedule for the recurring donation.");
        }

        if (!created.IsRecurring && created.TransactionId is null)
        {
            throw new HttpRequestException("The API did not return a transaction for the one-time donation.");
        }

        return created;
    }

    public Task<Guid> CreateTransactionAsync(
        decimal amount,
        Guid paymentScheduleId,
        Guid contactId,
        Guid paymentMethodId,
        PaymentType paymentType,
        DateOnly bookDate,
        DateOnly receivedDate,
        CancellationToken cancellationToken = default) =>
        PostIdAsync(
            "api/v1/transactions",
            new
            {
                amount,
                paymentScheduleId,
                contactId,
                paymentMethodId,
                paymentType,
                bookDate,
                receivedDate,
            },
            cancellationToken);

    public Task<ODataListResult<JournalDto>> QueryJournalsAsync(
        ODataListRequest? request = null,
        CancellationToken cancellationToken = default) =>
        QueryODataAsync<JournalDto>("api/v1/journals", request, cancellationToken);

    public Task<IReadOnlyList<JournalDto>> GetJournalsAsync(CancellationToken cancellationToken = default) =>
        GetListAsync<JournalDto>("api/v1/journals", cancellationToken);

    public Task<JournalDto?> GetJournalAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetAsync<JournalDto>($"api/v1/journals/{id}", cancellationToken);

    public Task<Guid> CreateJournalAsync(Guid transactionId, CancellationToken cancellationToken = default) =>
        PostIdAsync("api/v1/journals", new { transactionId }, cancellationToken);

    public Task<ODataListResult<ReceiptDto>> QueryReceiptsAsync(
        ODataListRequest? request = null,
        Guid? contactId = null,
        CancellationToken cancellationToken = default) =>
        QueryODataAsync<ReceiptDto>(WithContact("api/v1/receipts", contactId), request, cancellationToken);

    public Task<IReadOnlyList<ReceiptDto>> GetReceiptsAsync(
        Guid? contactId = null,
        CancellationToken cancellationToken = default) =>
        GetListAsync<ReceiptDto>(WithContact("api/v1/receipts", contactId), cancellationToken);

    public Task<ReceiptDto?> GetReceiptAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetAsync<ReceiptDto>($"api/v1/receipts/{id}", cancellationToken);

    public Task<Guid> CreateReceiptAsync(
        Guid contactId,
        Guid? transactionId,
        CancellationToken cancellationToken = default) =>
        PostIdAsync("api/v1/receipts", new { contactId, transactionId }, cancellationToken);

    public async Task<byte[]> GetReceiptPdfAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/receipts/{id}");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/pdf"));
        using var response = await http.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    public Task<ODataListResult<DonationFlowDto>> QueryDonationFlowsAsync(
        ODataListRequest? request = null,
        CancellationToken cancellationToken = default) =>
        QueryODataAsync<DonationFlowDto>("api/v1/donation-flows", request, cancellationToken);

    private static string WithContact(string path, Guid? contactId) =>
        contactId is { } id ? $"{path}?contactId={id}" : path;

    private async Task<IReadOnlyList<T>> GetListAsync<T>(string path, CancellationToken cancellationToken)
    {
        var result = await QueryODataAsync<T>(path, new ODataListRequest { Count = false }, cancellationToken);
        return result.Value;
    }

    private async Task<ODataListResult<T>> QueryODataAsync<T>(
        string path,
        ODataListRequest? request,
        CancellationToken cancellationToken)
    {
        var url = (request ?? new ODataListRequest()).AppendTo(path);
        using var httpRequest = JsonGet(url);
        using var response = await http.SendAsync(httpRequest, cancellationToken);
        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(ODataJson.ToKebabCaseProperties(json));
        var root = document.RootElement;
        var array = root.ValueKind == JsonValueKind.Array
            ? root
            : root.TryGetProperty("value", out var value) ? value : root;
        var items = array.Deserialize<List<T>>(JsonOptions) ?? [];
        var count = items.Count;
        if (root.ValueKind == JsonValueKind.Object)
        {
            if (root.TryGetProperty("@odata.count", out var odataCount) && odataCount.TryGetInt32(out var odataTotal))
            {
                count = odataTotal;
            }
            else if (root.TryGetProperty("count", out var plainCount) && plainCount.TryGetInt32(out var total))
            {
                count = total;
            }
        }

        return new ODataListResult<T> { Value = items, Count = count };
    }

    private async Task<T?> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var request = JsonGet(path);
        using var response = await http.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return default;
        }

        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<T>(ODataJson.ToKebabCaseProperties(json), JsonOptions);
    }

    private async Task<Guid> PostIdAsync(string path, object body, CancellationToken cancellationToken)
    {
        using var response = await http.PostAsJsonAsync(path, body, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response);
        var created = await response.Content.ReadFromJsonAsync<IdResponse>(JsonOptions, cancellationToken);
        if (created is null || created.Id == Guid.Empty)
        {
            throw new HttpRequestException("The API did not return a resource identifier.");
        }

        return created.Id;
    }

    private async Task SendAsync(HttpMethod method, string path, object body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path)
        {
            Content = JsonContent.Create(body, options: JsonOptions),
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await http.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
    }

    private static HttpRequestMessage JsonGet(string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var detail = await TryReadProblemDetailAsync(response);
        throw new HttpRequestException(
            string.IsNullOrWhiteSpace(detail)
                ? $"{(int)response.StatusCode} {response.ReasonPhrase}"
                : detail);
    }

    private static async Task<string?> TryReadProblemDetailAsync(HttpResponseMessage response)
    {
        try
        {
            var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
            if (payload is not null && payload.TryGetValue("detail", out var detail))
            {
                return detail.GetString();
            }
        }
        catch (JsonException)
        {
            // Fall back to the status line.
        }

        return null;
    }
}