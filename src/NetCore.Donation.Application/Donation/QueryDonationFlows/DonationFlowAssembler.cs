using NetCore.Donation.Application.Donation.DTOs;
using NetCore.Donation.Domain.Entities;
using NetCore.Donation.Domain.Enums;
using System.Globalization;
using System.Text.Json;

namespace NetCore.Donation.Application.Donation.QueryDonationFlows;

public static class DonationFlowAssembler
{
    private static readonly HashSet<string> MoneyFlowEvents =
    [
        "ContactCreated",
        "PaymentMethodCreated",
        "PaymentScheduleCreated",
        "TransactionCreated",
        "TransactionPending",
        "TransactionCompleted",
        "TransactionFailed",
        "JournalEntryCreated",
        "ReceiptCreated",
        "ReceiptGenerated",
    ];

    public static IReadOnlyList<QueryDonationFlowDto> Assemble(IEnumerable<OutboxMessage> messages)
    {
        var parsed = OrderPipeline(
                messages
                    .Select(TryParse)
                    .Where(item => item is not null)
                    .Cast<ParsedOutboxEvent>())
            .ToList();

        var scheduleByTransaction = new Dictionary<Guid, Guid>();
        var scheduleByCorrelation = new Dictionary<string, Guid>(StringComparer.Ordinal);

        foreach (var item in parsed)
        {
            if (item.PaymentScheduleId is not { } scheduleId)
            {
                continue;
            }

            if (item.TransactionId is { } transactionId)
            {
                scheduleByTransaction[transactionId] = scheduleId;
            }

            if (!string.IsNullOrWhiteSpace(item.CorrelationId))
            {
                scheduleByCorrelation[item.CorrelationId] = scheduleId;
            }
        }

        foreach (var item in parsed)
        {
            if (item.PaymentScheduleId is not null)
            {
                continue;
            }

            if (item.TransactionId is { } transactionId &&
                scheduleByTransaction.TryGetValue(transactionId, out var fromTransaction))
            {
                item.PaymentScheduleId = fromTransaction;
                continue;
            }

            if (!string.IsNullOrWhiteSpace(item.CorrelationId) &&
                scheduleByCorrelation.TryGetValue(item.CorrelationId, out var fromCorrelation))
            {
                item.PaymentScheduleId = fromCorrelation;
            }
        }

        var transactionByCorrelation = new Dictionary<string, Guid>(StringComparer.Ordinal);
        foreach (var item in parsed)
        {
            if (item.TransactionId is not { } transactionId || string.IsNullOrWhiteSpace(item.CorrelationId))
            {
                continue;
            }

            transactionByCorrelation[item.CorrelationId] = transactionId;
        }

        foreach (var item in parsed)
        {
            if (item.TransactionId is not null || string.IsNullOrWhiteSpace(item.CorrelationId))
            {
                continue;
            }

            if (transactionByCorrelation.TryGetValue(item.CorrelationId, out var fromCorrelationTransaction))
            {
                item.TransactionId = fromCorrelationTransaction;
            }
        }

        var groups = new Dictionary<string, List<ParsedOutboxEvent>>(StringComparer.Ordinal);
        foreach (var item in parsed)
        {
            var key = item.PaymentScheduleId?.ToString("N")
                ?? item.TransactionId?.ToString("N")
                ?? item.ContactId?.ToString("N")
                ?? item.ReceiptId?.ToString("N")
                ?? item.CorrelationId;
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (!groups.TryGetValue(key, out var bucket))
            {
                bucket = [];
                groups[key] = bucket;
            }

            bucket.Add(item);
        }

        return groups.Values
            .Select(BuildFlow)
            .Where(flow => flow is not null)
            .Cast<QueryDonationFlowDto>()
            .OrderByDescending(flow => flow.LastEventAtUtc)
            .ToList();
    }

    public static IReadOnlyList<QueryDonationFlowDto> IncludePostedTransactions(
        IReadOnlyList<QueryDonationFlowDto> flows,
        IEnumerable<Domain.Entities.Transaction> transactions,
        IEnumerable<Domain.Entities.Journal> journals,
        IEnumerable<Domain.Entities.Receipt> receipts,
        IReadOnlyDictionary<Guid, string> scheduleIdentifiers,
        IReadOnlyDictionary<Guid, string> methodNames)
    {
        var journalByTransaction = journals
            .GroupBy(item => item.TransactionId)
            .ToDictionary(group => group.Key, group => group.OrderBy(item => item.CreatedDate).First());
        var receiptByTransaction = receipts
            .Where(item => item.TransactionId is not null)
            .GroupBy(item => item.TransactionId!.Value)
            .ToDictionary(group => group.Key, group => group.OrderBy(item => item.CreatedDate).First());

        var merged = flows.ToList();
        foreach (var transaction in transactions)
        {
            journalByTransaction.TryGetValue(transaction.Id, out var journal);
            receiptByTransaction.TryGetValue(transaction.Id, out var receipt);
            var existing = FindExistingFlow(merged, transaction);
            if (existing is not null)
            {
                AttachTransaction(existing, transaction, journal, receipt, scheduleIdentifiers, methodNames);
                continue;
            }

            merged.Add(FromPostedTransaction(transaction, journal, receipt, scheduleIdentifiers, methodNames));
        }

        return merged
            .OrderByDescending(flow => flow.LastEventAtUtc)
            .ToList();
    }

    public static string ToEventName(string messageType)
    {
        var typeName = messageType.Split(',')[0].Trim();
        var shortName = typeName.Contains('.', StringComparison.Ordinal)
            ? typeName[(typeName.LastIndexOf('.') + 1)..]
            : typeName;
        return shortName.EndsWith("DomainEvent", StringComparison.Ordinal)
            ? shortName[..^"DomainEvent".Length]
            : shortName;
    }

    public static int CanonicalSequence(string eventName)
    {
        return eventName switch
        {
            "ContactCreated" => 0,
            "PaymentMethodCreated" => 1,
            "PaymentScheduleCreated" => 2,
            "TransactionCreated" => 3,
            "TransactionPending" => 4,
            "TransactionCompleted" or "TransactionFailed" => 5,
            "JournalEntryCreated" => 6,
            "ReceiptCreated" => 7,
            "ReceiptGenerated" => 8,
            _ => 50,
        };
    }

    private static QueryDonationFlowDto? BuildFlow(List<ParsedOutboxEvent> events)
    {
        if (!events.Any(item => item.EventName is
            "ContactCreated" or
            "PaymentMethodCreated" or
            "PaymentScheduleCreated" or
            "TransactionCreated" or
            "TransactionPending" or
            "TransactionCompleted" or
            "TransactionFailed" or
            "JournalEntryCreated" or
            "ReceiptCreated" or
            "ReceiptGenerated"))
        {
            return null;
        }

        var ordered = OrderPipeline(events).ToList();
        var failed = ordered.Any(item => item.EventName == "TransactionFailed");
        var succeeded = ordered.Any(item => item.EventName is "TransactionCompleted" or "TransactionSucceeded");
        var pending = ordered.Any(item => item.EventName == "TransactionPending");
        var created = ordered.Any(item => item.EventName == "TransactionCreated");
        var status = failed
            ? "Failed"
            : succeeded || (created && !pending)
                ? "Succeeded"
                : pending
                    ? "Pending"
                    : "InProgress";

        var scheduleId = ordered.Select(item => item.PaymentScheduleId).FirstOrDefault(id => id is not null);
        var transactionId = ordered.Select(item => item.TransactionId).FirstOrDefault(id => id is not null);
        var receiptId = ordered.Select(item => item.ReceiptId).FirstOrDefault(id => id is not null);
        var contactId = ordered.Select(item => item.ContactId).FirstOrDefault(id => id is not null);
        var flowId = scheduleId ?? transactionId ?? contactId ?? receiptId ?? ordered[0].MessageId;

        return new QueryDonationFlowDto
        {
            Id = flowId,
            PaymentScheduleId = scheduleId,
            PaymentScheduleIdentifier = FirstIdentifier(ordered, "PaymentScheduleCreated"),
            ContactId = contactId,
            ContactEmail = ordered
                .Select(item => item.ContactEmail)
                .FirstOrDefault(email => !string.IsNullOrWhiteSpace(email)),
            PaymentMethodId = ordered.Select(item => item.PaymentMethodId).FirstOrDefault(id => id is not null),
            PaymentMethodDisplayName = ordered
                .Select(item => item.PaymentMethodDisplayName)
                .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)),
            TransactionId = transactionId,
            TransactionIdentifier = FirstIdentifier(ordered, "TransactionCreated", "TransactionPending", "TransactionCompleted", "TransactionFailed"),
            JournalId = ordered.Select(item => item.JournalId).FirstOrDefault(id => id is not null),
            JournalIdentifier = FirstIdentifier(ordered, "JournalEntryCreated"),
            ReceiptId = ordered.Select(item => item.ReceiptId).FirstOrDefault(id => id is not null),
            ReceiptIdentifier = FirstIdentifier(ordered, "ReceiptCreated", "ReceiptGenerated"),
            Amount = ordered.Select(item => item.Amount).LastOrDefault(amount => amount is not null),
            Status = status,
            MoneyPath = BuildMoneyPath(
                scheduleId is not null,
                created || transactionId is not null,
                failed,
                succeeded || (created && !pending && !failed),
                pending,
                ordered.Any(item => item.JournalId is not null),
                ordered.Any(item => item.ReceiptId is not null)),
            StartedAtUtc = ordered[0].OccurredAtUtc,
            LastEventAtUtc = ordered[^1].OccurredAtUtc,
            Steps = ordered.Select(item => new QueryDonationFlowStepDto
            {
                EventName = item.EventName,
                OccurredAtUtc = item.OccurredAtUtc,
                ProcessedAtUtc = item.ProcessedAtUtc,
                Summary = item.Summary,
                CorrelationId = item.CorrelationId,
            }).ToList(),
        };
    }

    private static QueryDonationFlowDto? FindExistingFlow(
        IEnumerable<QueryDonationFlowDto> flows,
        Domain.Entities.Transaction transaction)
    {
        var byTransaction = flows.FirstOrDefault(flow => flow.TransactionId == transaction.Id);
        if (byTransaction is not null)
        {
            return byTransaction;
        }

        if (transaction.PaymentScheduleId is not { } scheduleId)
        {
            return null;
        }

        return flows.FirstOrDefault(flow =>
            flow.PaymentScheduleId == scheduleId && flow.TransactionId is null);
    }

    private static void AttachTransaction(
        QueryDonationFlowDto flow,
        Domain.Entities.Transaction transaction,
        Domain.Entities.Journal? journal,
        Domain.Entities.Receipt? receipt,
        IReadOnlyDictionary<Guid, string> scheduleIdentifiers,
        IReadOnlyDictionary<Guid, string> methodNames)
    {
        flow.TransactionId ??= transaction.Id;
        flow.TransactionIdentifier ??= transaction.Identifier;
        flow.ContactId ??= transaction.ContactId;
        flow.PaymentMethodId ??= transaction.PaymentMethodId;
        flow.Amount ??= transaction.Amount;
        flow.PaymentScheduleId ??= transaction.PaymentScheduleId;
        if (string.IsNullOrWhiteSpace(flow.PaymentScheduleIdentifier) &&
            transaction.PaymentScheduleId is { } scheduleId &&
            scheduleIdentifiers.TryGetValue(scheduleId, out var scheduleIdentifier))
        {
            flow.PaymentScheduleIdentifier = scheduleIdentifier;
        }

        if (string.IsNullOrWhiteSpace(flow.PaymentMethodDisplayName) &&
            methodNames.TryGetValue(transaction.PaymentMethodId, out var methodName))
        {
            flow.PaymentMethodDisplayName = methodName;
        }

        if (journal is not null)
        {
            flow.JournalId ??= journal.Id;
            flow.JournalIdentifier ??= journal.Identifier;
        }

        if (receipt is not null)
        {
            flow.ReceiptId ??= receipt.Id;
            flow.ReceiptIdentifier ??= receipt.Identifier;
        }

        if (flow.Status is "InProgress" or "")
        {
            flow.Status = StatusOf(transaction);
        }

        flow.MoneyPath = BuildMoneyPath(
            flow.PaymentScheduleId is not null,
            true,
            transaction.Status == TransactionStatus.Failed,
            transaction.Status == TransactionStatus.Succeeded,
            transaction.Status == TransactionStatus.Pending,
            flow.JournalId is not null,
            flow.ReceiptId is not null);

        if (!flow.Steps.Any(step => step.EventName is
            "TransactionCreated" or "TransactionPending" or "TransactionCompleted" or "TransactionFailed"))
        {
            flow.Steps.Add(TransactionStep(transaction));
            flow.LastEventAtUtc = flow.Steps.Max(step => step.OccurredAtUtc);
        }
    }

    private static QueryDonationFlowDto FromPostedTransaction(
        Domain.Entities.Transaction transaction,
        Domain.Entities.Journal? journal,
        Domain.Entities.Receipt? receipt,
        IReadOnlyDictionary<Guid, string> scheduleIdentifiers,
        IReadOnlyDictionary<Guid, string> methodNames)
    {
        var steps = new List<QueryDonationFlowStepDto> { TransactionStep(transaction) };
        if (journal is not null)
        {
            steps.Add(new QueryDonationFlowStepDto
            {
                EventName = "JournalEntryCreated",
                OccurredAtUtc = journal.CreatedDate,
                ProcessedAtUtc = journal.ModifiedDate,
                Summary = $"Journal line {journal.Identifier} recorded for the posted gift",
            });
        }

        if (receipt is not null)
        {
            steps.Add(new QueryDonationFlowStepDto
            {
                EventName = "ReceiptCreated",
                OccurredAtUtc = receipt.CreatedDate,
                ProcessedAtUtc = receipt.ModifiedDate,
                Summary = $"Receipt {receipt.Identifier} created",
            });
            if (receipt.HasDocument)
            {
                steps.Add(new QueryDonationFlowStepDto
                {
                    EventName = "ReceiptGenerated",
                    OccurredAtUtc = receipt.DocumentGeneratedAtUtc ?? receipt.CreatedDate,
                    ProcessedAtUtc = receipt.ModifiedDate,
                    Summary = $"Digital receipt {receipt.Identifier} stored for the donor",
                });
            }
        }

        var ordered = steps.OrderBy(step => step.OccurredAtUtc).ToList();
        methodNames.TryGetValue(transaction.PaymentMethodId, out var methodName);
        string? scheduleIdentifier = null;
        if (transaction.PaymentScheduleId is { } scheduleId)
        {
            scheduleIdentifiers.TryGetValue(scheduleId, out scheduleIdentifier);
        }

        return new QueryDonationFlowDto
        {
            Id = transaction.PaymentScheduleId ?? transaction.Id,
            PaymentScheduleId = transaction.PaymentScheduleId,
            PaymentScheduleIdentifier = scheduleIdentifier,
            ContactId = transaction.ContactId,
            PaymentMethodId = transaction.PaymentMethodId,
            PaymentMethodDisplayName = methodName,
            TransactionId = transaction.Id,
            TransactionIdentifier = transaction.Identifier,
            JournalId = journal?.Id,
            JournalIdentifier = journal?.Identifier,
            ReceiptId = receipt?.Id,
            ReceiptIdentifier = receipt?.Identifier,
            Amount = transaction.Amount,
            Status = StatusOf(transaction),
            MoneyPath = BuildMoneyPath(
                transaction.PaymentScheduleId is not null,
                true,
                transaction.Status == TransactionStatus.Failed,
                transaction.Status == TransactionStatus.Succeeded,
                transaction.Status == TransactionStatus.Pending,
                journal is not null,
                receipt is not null),
            StartedAtUtc = transaction.CreatedDate,
            LastEventAtUtc = ordered[^1].OccurredAtUtc,
            Steps = ordered,
        };
    }

    private static QueryDonationFlowStepDto TransactionStep(Domain.Entities.Transaction transaction)
    {
        var eventName = transaction.Status switch
        {
            TransactionStatus.Failed => "TransactionFailed",
            TransactionStatus.Pending => "TransactionPending",
            _ => "TransactionCreated",
        };
        var verb = transaction.Status switch
        {
            TransactionStatus.Failed => "Payment failed",
            TransactionStatus.Pending => "Transaction pending",
            _ => "Transaction created",
        };

        return new QueryDonationFlowStepDto
        {
            EventName = eventName,
            OccurredAtUtc = transaction.CreatedDate,
            ProcessedAtUtc = transaction.ModifiedDate,
            Summary = $"{verb} {transaction.Amount.ToString("0.00", CultureInfo.InvariantCulture)} ({transaction.Identifier})",
        };
    }

    private static string StatusOf(Domain.Entities.Transaction transaction)
    {
        return transaction.Status switch
        {
            TransactionStatus.Failed => "Failed",
            TransactionStatus.Pending => "Pending",
            _ => "Succeeded",
        };
    }

    private static string BuildMoneyPath(
        bool hasSchedule,
        bool hasTransaction,
        bool failed,
        bool succeeded,
        bool pending,
        bool hasJournal,
        bool hasReceipt)
    {
        var parts = new List<string> { "Donor" };
        if (hasSchedule)
        {
            parts.Add("Payment schedule");
        }

        if (hasTransaction)
        {
            parts.Add(failed ? "Transaction (failed)" : succeeded ? "Transaction (posted)" : pending ? "Transaction (pending)" : "Transaction");
        }

        if (hasJournal)
        {
            parts.Add("Journal");
        }

        if (hasReceipt)
        {
            parts.Add("Receipt");
        }

        return string.Join(" → ", parts);
    }

    private static ParsedOutboxEvent? TryParse(OutboxMessage message)
    {
        var eventName = ToEventName(message.MessageType);
        if (!MoneyFlowEvents.Contains(eventName))
        {
            return null;
        }

        using var document = JsonDocument.Parse(message.Payload);
        var root = document.RootElement;
        var amount = GetDecimal(root, "amount");
        var identifier = GetString(root, "identifier");
        var parsed = new ParsedOutboxEvent
        {
            MessageId = message.Id,
            EventName = eventName,
            CorrelationId = message.CorrelationId,
            OccurredAtUtc = message.OccurredAtUtc,
            ProcessedAtUtc = message.ProcessedAtUtc,
            PaymentScheduleId = GetGuid(root, "paymentScheduleId"),
            ContactId = GetGuid(root, "contactId"),
            ContactEmail = GetString(root, "email"),
            PaymentMethodId = GetGuid(root, "paymentMethodId"),
            PaymentMethodDisplayName = GetString(root, "displayName"),
            TransactionId = GetGuid(root, "transactionId"),
            JournalId = GetGuid(root, "journalId"),
            ReceiptId = GetGuid(root, "receiptId"),
            Identifier = identifier,
            Amount = amount,
            Summary = BuildSummary(eventName, amount, identifier, root),
        };

        return parsed;
    }

    private static string? FirstIdentifier(IEnumerable<ParsedOutboxEvent> events, params string[] eventNames)
    {
        return events
            .Where(item => eventNames.Contains(item.EventName) && !string.IsNullOrWhiteSpace(item.Identifier))
            .Select(item => item.Identifier)
            .FirstOrDefault();
    }

    private static string BuildSummary(string eventName, decimal? amount, string identifier, JsonElement root)
    {
        var amountText = amount is { } value
            ? value.ToString("0.00", CultureInfo.InvariantCulture)
            : null;

        return eventName switch
        {
            "ContactCreated" => $"Donor profile {GetString(root, "email")}".Trim(),
            "PaymentMethodCreated" => $"Payment method {GetString(root, "displayName")}".Trim(),
            "PaymentScheduleCreated" => DescribeAmount("Payment schedule created", "Payment schedule created for {0}", "Payment schedule {1} created for {0}", amountText, identifier),
            "TransactionCreated" => DescribeAmount("Transaction created", "Transaction created for {0}", "Transaction {1} created for {0}", amountText, identifier),
            "TransactionPending" => DescribeAmount("Transaction pending", "Transaction pending for {0}", "Transaction pending {1} for {0}", amountText, identifier),
            "TransactionCompleted" => DescribeAmount("Transaction completed", "Transaction completed for {0}", "Transaction {1} completed for {0}", amountText, identifier),
            "TransactionFailed" => DescribeAmount("Transaction failed", "Payment failed for {0}", "Payment failed {1} for {0}", amountText, identifier),
            "JournalEntryCreated" => string.IsNullOrWhiteSpace(identifier)
                ? "Journal line recorded for the posted gift"
                : $"Journal line {identifier} recorded for the posted gift",
            "ReceiptCreated" => string.IsNullOrWhiteSpace(identifier)
                ? "Receipt created"
                : $"Receipt {identifier} created",
            "ReceiptGenerated" => string.IsNullOrWhiteSpace(identifier)
                ? "Digital receipt stored for the donor"
                : $"Digital receipt {identifier} stored for the donor",
            _ => eventName,
        };
    }

    private static string DescribeAmount(string withoutAmount, string amountOnlyFormat, string amountAndIdFormat, string? amountText, string identifier)
    {
        if (amountText is null)
        {
            return string.IsNullOrWhiteSpace(identifier) ? withoutAmount : $"{withoutAmount} {identifier}";
        }

        return string.IsNullOrWhiteSpace(identifier)
            ? string.Format(CultureInfo.InvariantCulture, amountOnlyFormat, amountText)
            : string.Format(CultureInfo.InvariantCulture, amountAndIdFormat, amountText, identifier);
    }

    private static IOrderedEnumerable<ParsedOutboxEvent> OrderPipeline(IEnumerable<ParsedOutboxEvent> events)
    {
        return events
            .OrderBy(item => item.OccurredAtUtc)
            .ThenBy(item => CanonicalSequence(item.EventName))
            .ThenBy(item => item.MessageId);
    }

    private static Guid? GetGuid(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var property) ||
            property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.String && Guid.TryParse(property.GetString(), out var parsed))
        {
            return parsed;
        }

        return property.TryGetGuid(out var guid) ? guid : null;
    }

    private static decimal? GetDecimal(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var property) ||
            property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return property.TryGetDecimal(out var value) ? value : null;
    }

    private static string GetString(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }

    private sealed class ParsedOutboxEvent
    {
        public Guid MessageId { get; init; }

        public string EventName { get; init; } = string.Empty;

        public string CorrelationId { get; init; } = string.Empty;

        public DateTime OccurredAtUtc { get; init; }

        public DateTime? ProcessedAtUtc { get; init; }

        public Guid? PaymentScheduleId { get; set; }

        public Guid? ContactId { get; init; }

        public string? ContactEmail { get; init; }

        public Guid? PaymentMethodId { get; init; }

        public string? PaymentMethodDisplayName { get; init; }

        public Guid? TransactionId { get; set; }

        public Guid? JournalId { get; init; }

        public Guid? ReceiptId { get; init; }

        public string? Identifier { get; init; }

        public decimal? Amount { get; init; }

        public string Summary { get; init; } = string.Empty;
    }
}
